tool
extends Node

class Cell:
	var cell_position: Vector2
	var cell_colour: Color
	var neighbours: Array
	func _init(_cell_position: Vector2, _cell_colour: Color):
		self.cell_position = _cell_position
		self.cell_colour = _cell_colour
		self.neighbours = []
	
	func get_position() -> Vector2:
		return self.cell_position
	
	func get_colour() -> Color:
		return self.cell_colour
	
	func get_neighbours() -> Array:
		return self.neighbours
	
	func add_neighbour(cell):
		if cell == null:
			return
		self.neighbours.append(cell)

class CellGrid:
	var cells: Array
	var size: Vector2
	func _init(_size: Vector2, _cells: Array):
		self.cells = _cells
		self.size = _size
	
	func get_cellv_if_match(colour: Color, cell_position: Vector2):
		var cell = self.get_cellv(cell_position)
		if cell == null:
			return null
		
		if cell.get_colour() == colour:
			return cell
		
		return null
	
	func get_cellv(cell_position: Vector2):
		if (cell_position.x < 0 or cell_position.y < 0 or
			cell_position.x >= size.x or cell_position.y >= size.y):
			return null
			
		var index = size.y * cell_position.x + cell_position.y
		return self.cells[index]
		
	func get_cell(index: int):
		var cell_position = Vector2(
			index / int(self.size.y),
			index % int(self.size.x)
		)
		return self.get_cellv(cell_position)

func get_islands(image: Image, smallest_island: int, largest_island: int, include_diagonal: bool) -> Image:
	"""
	This algorithm returns a new image that only contains islands of pixels that
	fit the parameters of the method. Islands are 'fill buckets' of pixels. These
	fill buckets will include diagonals if the include_diagonals parameter is
	true.
	
	The algorithm works by giving each pixel a Cell. Then it connects these
	cells in the eight directions through a reference if the colours are the same.
	This step is creating a multiple graph trees.
	
	A DFS is then performed on this forest to turn each isolated graph into an
	Array of nodes. The length of this array of nodes is the size of each graph.
	Array's.size() that do not fit between smallest_island and largest_island are
	then ignored. Ignored pixels are blank (alpha=0). The remaining groups remain
	on the new image in their original colour.
	
	This information can be used in a variety of ways. One way is to remove small
	islands by inverting the results and merging it over the original image.
	"""
	var new_image: Image = Image.new()
	new_image.create(image.get_width(), image.get_height(), false, Image.FORMAT_RGBA8)
#	new_image.fill(Color.pink)
	
	if (smallest_island <= 0 and largest_island <= 0):
		# Do nothing
		return new_image
	image.lock()
	
	# Create the cell grid
	var cells: Array = []
	for i in range(image.get_width()):
		for j in range(image.get_height()):
			cells.append(Cell.new(Vector2(i, j), image.get_pixel(i, j)))
	
	var cell_grid: CellGrid = CellGrid.new(image.get_size(), cells)
	
	# Link them together
	for index in range(cells.size()):
		var cell: Cell = cell_grid.get_cell(index)
		var cell_position: Vector2 = cell.get_position()
		var colour: Color = cell.get_colour()
		var i = cell_position.x
		var j = cell_position.y
		
		if include_diagonal:
			cell.add_neighbour(cell_grid.get_cellv_if_match(colour, Vector2(i - 1, j - 1)))
			cell.add_neighbour(cell_grid.get_cellv_if_match(colour, Vector2(i - 1, j + 1)))
			cell.add_neighbour(cell_grid.get_cellv_if_match(colour, Vector2(i + 1, j - 1)))
			cell.add_neighbour(cell_grid.get_cellv_if_match(colour, Vector2(i + 1, j + 1)))
		
		cell.add_neighbour(cell_grid.get_cellv_if_match(colour, Vector2(i - 1, j)))
		cell.add_neighbour(cell_grid.get_cellv_if_match(colour, Vector2(i, j - 1)))
		cell.add_neighbour(cell_grid.get_cellv_if_match(colour, Vector2(i, j + 1)))
		cell.add_neighbour(cell_grid.get_cellv_if_match(colour, Vector2(i + 1, j)))
	
	# Perform a Depth First Search to generate the Cell Arrays
	var explored_roots = {}
	var explored_cells = {}
	for i in range(image.get_width()):
		for j in range(image.get_height()):
			var root: Cell = cell_grid.get_cellv(Vector2(i, j))
			
			if !explored_cells.has(root):
				# Initialise this group
				explored_roots[root] = []
				explored_roots[root].append(root)
				
				explored_cells[root] = null
				
				var current_fringe = []
				
				# Now continue the DFS
				current_fringe = root.get_neighbours()
					
				while current_fringe.size() > 0:
					# Expand the next node
					var cell: Cell = current_fringe.pop_back()
					
					# Check if we're already been explored
					if explored_cells.has(cell):
						continue
					explored_cells[cell] = null
						
					# Add this cell to the root counter
					explored_roots[root].append(cell)
					
					# Add all neighbours to the fringe
					for neighbour in cell.get_neighbours():
						if not explored_cells.has(neighbour):
							current_fringe.append(neighbour)
	
	# We now have a Dictionary of lists of cells
	# Extract the pixel positions
	var group_locations = {}
	for root in explored_roots.keys():
		group_locations[root.get_position()] = []
		for cell in explored_roots[root]:
			group_locations[root.get_position()].append(cell)
	
	
	new_image.lock()
	# Now create a new image with the group data filtered according to the
	# parameters
	for root_point in group_locations.keys():
		var group_cells = group_locations[root_point]
		
		var colour = null
		if (largest_island > 0 and group_cells.size() > largest_island):
#			colour = Color(1, 1, 1)
			continue
		if (smallest_island > 0 and group_cells.size() < smallest_island):
			continue
		for cell in group_cells:
			if colour == null:
				new_image.set_pixelv(cell.get_position(), cell.get_colour())
			else:
				new_image.set_pixelv(cell.get_position(), colour)
	
	new_image.unlock()
	image.unlock()
	
	return new_image

class Set:
	var data := {}
	var colour: Color
	func _init(element, _colour: Color):
		self.data[element] = null
		self.colour = _colour
	
	func union_array(array: Array):
		for element in array:
			self.data[element] = null
	
	func union(element):
		self.data[element] = null
	
	func has(element) -> bool:
		return self.data.has(element)
	
	func to_string():
		return self.data.keys()
	
	func find():
		return self.data.keys().min()
	
	func get_colour() -> Color:
		return self.colour

func get_islands_faster(image: Image, smallest_island: int, largest_island: int, include_diagonal: bool):
	var new_image: Image = Image.new()
	new_image.create(image.get_width(), image.get_height(), false, Image.FORMAT_RGBA8)
#	new_image.fill(Color.pink)
	
	if (smallest_island <= 0 and largest_island <= 0):
		# Do nothing
		return new_image
	
	# Create a 2D Array the same size as the image
	var linked: Dictionary = {}
	var labels: Array = []
	for i in range(image.get_width()):
	    labels.append([])
	    for _j in range(image.get_height()):
	        labels[i].append(null)
	
	image.lock()
	var next_label = 0
	for j in range(image.get_height()):
		for i in range(image.get_width()):
			var neighbours = {}
			var my_colour: Color = image.get_pixel(i, j)
			
			# Top Right
			var top_right_i = i + 1
			var top_right_j = j - 1
			if top_right_i >= image.get_width() or top_right_j < 0:
				pass
			else:
				var colour: Color = image.get_pixel(top_right_i, top_right_j)
				if colour == my_colour:
					neighbours[Vector2(top_right_i, top_right_j)] = labels[top_right_i][top_right_j]
			
			# Top left
			var top_left_i = i - 1
			var top_left_j = j - 1
			if top_left_i < 0 or top_left_j < 0:
				pass
			else:
				var colour: Color = image.get_pixel(top_left_i, top_left_j)
				if colour == my_colour:
					neighbours[Vector2(top_left_i, top_left_j)] = labels[top_left_i][top_left_j]
				
			# Top
			var top_i = i
			var top_j = j - 1
			if top_i < 0 or top_j < 0:
				pass
			else:
				var colour: Color = image.get_pixel(top_i, top_j)
				if colour == my_colour:
					neighbours[Vector2(top_i, top_j)] = labels[top_i][top_j]
			
			# Left
			var left_i = i - 1
			var left_j = j
			if left_i < 0 or left_j < 0:
				pass
			else:
				var colour: Color = image.get_pixel(left_i, left_j)
				if colour == my_colour:
					neighbours[Vector2(left_i, left_j)] = labels[left_i][left_j]
			
			if neighbours.size() == 0:
				linked[next_label] = Set.new(next_label, my_colour)
				labels[i][j] = next_label
				next_label += 1
			else:
				labels[i][j] = neighbours.values().min()
				for label in neighbours.values():
					linked[label].union_array(neighbours.values())
	image.unlock()
	
	# Create the final regions using the resultant data
	var regions := {}
	for j in range(image.get_height()):
		for i in range(image.get_width()):
			var naive_label: int = labels[i][j]
			var my_local_set: Set = linked[naive_label]
			var my_true_set: Set = linked[my_local_set.find()]
			var label: int = my_true_set.find()
			if not regions.has(label):
				regions[label] = []
			regions[label].append(Vector2(i, j))
	
	# Remove regions that don't fit the criteria
	for label in regions.keys():
		if ((smallest_island > 0 and regions[label].size() < smallest_island) or
				(largest_island > 0 and regions[label].size() > largest_island)):
			regions.erase(label)
	
	# Create the final image
	new_image.lock()
	for label in regions.keys():
		var colour = linked[label].get_colour()
		for pixel in regions[label]:
			new_image.set_pixelv(pixel, colour)
	new_image.unlock()
	
#	print("Linked size", linked.size())
#	for key in regions.keys():
#		var region_size = regions[key].size()
#		if region_size > 10:
#			print("Region size", regions[key].size())
	
	return new_image

func get_islands_naive_fastest(image: Image, smallest_island: int, largest_island: int, include_diagonal: bool):
	var new_image: Image = Image.new()
	new_image.create(image.get_width(), image.get_height(), false, Image.FORMAT_RGBA8)
	
	if (smallest_island <= 0 and largest_island <= 0):
		# Do nothing
		return new_image
	
	var ray_percentage = 0.01
	print(image.get_width() * image.get_height())
	var rays = image.get_width() * image.get_height() * ray_percentage
	print(rays)
	
	image.lock()
	# Create the cell grid
	var cells: Array = []
	for i in range(image.get_width()):
		for j in range(image.get_height()):
			cells.append(Cell.new(Vector2(i, j), image.get_pixel(i, j)))
	
	var cell_grid: CellGrid = CellGrid.new(image.get_size(), cells)
	
	# Link them together
	for index in range(cells.size()):
		var cell: Cell = cell_grid.get_cell(index)
		var cell_position: Vector2 = cell.get_position()
		var colour: Color = cell.get_colour()
		var i = cell_position.x
		var j = cell_position.y
		
		if include_diagonal:
			cell.add_neighbour(cell_grid.get_cellv_if_match(colour, Vector2(i - 1, j - 1)))
			cell.add_neighbour(cell_grid.get_cellv_if_match(colour, Vector2(i - 1, j + 1)))
			cell.add_neighbour(cell_grid.get_cellv_if_match(colour, Vector2(i + 1, j - 1)))
			cell.add_neighbour(cell_grid.get_cellv_if_match(colour, Vector2(i + 1, j + 1)))
		
		cell.add_neighbour(cell_grid.get_cellv_if_match(colour, Vector2(i - 1, j)))
		cell.add_neighbour(cell_grid.get_cellv_if_match(colour, Vector2(i, j - 1)))
		cell.add_neighbour(cell_grid.get_cellv_if_match(colour, Vector2(i, j + 1)))
		cell.add_neighbour(cell_grid.get_cellv_if_match(colour, Vector2(i + 1, j)))
	
	var explored_roots = {}
	var explored_cells = {}
	
	# Only explore a random number of rays that hit the screen
	for ray in range(rays):
		var ray_location = Vector2(
			randi() % image.get_width(),
			randi() % image.get_height()
		)
		
		var root: Cell = cell_grid.get_cellv(ray_location)
		
		if explored_cells.has(root):
			continue
			
		# Found a new starting point
		explored_roots[root] = [root]
		explored_cells[root] = null
		
		# Add neighbours to fringe
		var fringe = root.get_neighbours()
		
		while fringe.size() > 0:
			var current_cell = fringe.pop_back()
			
			# Add to the explored cells
			explored_roots[root].append(current_cell)
			explored_cells[current_cell] = null
			
			# Add neighbours that haven't been expanded
			for neighbour in current_cell.get_neighbours():
				if not explored_cells.has(neighbour):
					fringe.append(neighbour)
	
	new_image.lock()
	for region in explored_roots.values():
		if ((largest_island > 0 and region.size() > largest_island) or
				(smallest_island > 0 and region.size() < smallest_island)):
			continue
		
		for cell in region:
			new_image.set_pixelv(cell.get_position(), cell.get_colour())
	new_image.unlock()
	
	image.unlock()
	return new_image










