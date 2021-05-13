#class_name ThreadPool
extends Node
# A thread pool designed to perform your tasks efficiently

signal task_finished(task_tag)
signal task_discarded(task)

# This is set to false
export var discard_finished_tasks: bool = false

var __tasks: Array = []
var __started = false
var __finished = false
var __tasks_lock: Mutex = Mutex.new()
var __tasks_wait: Semaphore = Semaphore.new()
var __finished_tasks: Array = []
var __finished_tasks_lock: Mutex = Mutex.new()

onready var __pool = __create_pool()

func _notification(what: int):
	if what == NOTIFICATION_PREDELETE:
		__wait_for_shutdown()


func queue_free() -> void:
	shutdown()
	.queue_free()


func submit_task(instance: Object, method: String, parameter, task_tag = null, task_tag_specific = null) -> void:
	__enqueue_task(instance, method, parameter, task_tag, task_tag_specific, false, false)


func submit_task_unparameterized(instance: Object, method: String, task_tag = null, task_tag_specific = null) -> void:
	__enqueue_task(instance, method, null, task_tag, task_tag_specific, true, false)


func submit_task_array_parameterized(instance: Object, method: String, parameter: Array, task_tag = null, task_tag_specific = null) -> void:
	__enqueue_task(instance, method, parameter, task_tag, task_tag_specific, false, true)


func shutdown():
	__finished = true
	for i in __pool:
		__tasks_wait.post()
	__tasks_lock.lock()
	__tasks.clear()
	__tasks_lock.unlock()


func fetch_finished_tasks() -> Array:
	__finished_tasks_lock.lock()
	var result = __finished_tasks
	__finished_tasks = []
	__finished_tasks_lock.unlock()
	return result


func fetch_finished_tasks_by_tag(tag) -> Array:
	__finished_tasks_lock.lock()
	var result = []
	var new_finished_tasks = []
	for t in __finished_tasks.size():
		var task = __finished_tasks[t]
		match task.tag:
			tag:
				result.append(task)
			_:
				new_finished_tasks.append(task)
	__finished_tasks = new_finished_tasks
	__finished_tasks_lock.unlock()
	return result


func do_nothing(arg) -> void:
	#print("doing nothing")
	OS.delay_msec(1) # if there is nothing to do, go sleep


func __enqueue_task(instance: Object, method: String, parameter = null, task_tag = null, task_tag_specific = null, no_argument = false, array_argument = false) -> void:
	if __finished:
		return
	__tasks_lock.lock()
	__tasks.push_front(Task.new(instance, method, parameter, task_tag, task_tag_specific, no_argument, array_argument))
	print("Tasks size:" + str(__tasks.size()))
	__tasks_wait.post()
	__start()
	__tasks_lock.unlock()


func __wait_for_shutdown():
	shutdown()
	for t in __pool:
		if t.is_active():
			t.wait_to_finish()


func __create_pool():
	var result = []
	for c in range(OS.get_processor_count()):
		result.append(Thread.new())
	return result


func __start() -> void:
	if not __started:
		for t in __pool:
			(t as Thread).start(self, "__execute_tasks", t)
		__started = true

func wait_for_task_specific(tag_specific) -> void:
	# I hate this
#	if __finished_tasks.size() == 0:
#		return
	print("Force waiting for " + str(tag_specific) + " thread to finish")
	
	# Chuck the task on the front
#	__tasks_lock.lock()
#	var i = 0
#	for task in __tasks:
#		if task.tag_specific == tag_specific:
#			__tasks.remove(i)
#			__tasks.append(task)
#		i += 1
#	__tasks_lock.unlock()
	
	while true:
		for task in __finished_tasks:
			if task.tag_specific == tag_specific:
				print("Found")
				return
		OS.delay_msec(1)

func __drain_task() -> Task:
	__tasks_lock.lock()
	var result
	if __tasks.empty():
		result = Task.new(self, "do_nothing", null, null, null, true, false)# normally, this is not expected, but better safe than sorry
		result.tag = result
	else:
		result = __tasks.pop_back()
	__tasks_lock.unlock()
	return result;


func __execute_tasks(arg_thread) -> void:
#	print_debug(arg_thread)
	while not __finished:
		__tasks_wait.wait()
		if __finished:
			return
		var task: Task = __drain_task()
		task.__execute_task()
		if not (task.tag is Task):# tasks tagged this way are considered hidden
			if discard_finished_tasks:
				call_deferred("emit_signal", "task_discarded", task)
			else:
				__finished_tasks_lock.lock()
				__finished_tasks.append(task)
				__finished_tasks_lock.unlock()
				call_deferred("emit_signal", "task_finished", task.tag)


class Task:
	var target_instance: Object
	var target_method: String
	var target_argument
	var result
	var tag
	var tag_specific
	var __no_argument: bool
	var __array_argument: bool

	func _init(instance: Object, method: String, parameter, task_tag, task_tag_specific, no_argument: bool, array_argument: bool):
		target_instance = instance
		target_method = method
		target_argument = parameter
		result = null
		tag = task_tag
		tag_specific = task_tag_specific
		__no_argument = no_argument
		__array_argument = array_argument

	func get_argument():
		return self.target_argument
	
	func get_result():
		return self.result

	func __execute_task():
		if __no_argument:
			result = target_instance.call(target_method)
		elif __array_argument:
			result = target_instance.callv(target_method, target_argument)
		else:
			result = target_instance.call(target_method, target_argument)
