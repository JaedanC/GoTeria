tool
extends Node

class Cell:
	var cell_colour: Color
	var neighbours: Array
	var x: int
	var y: int
	func _init(_x: int, _y: int, _cell_colour: Color):
		self.cell_colour = _cell_colour
		self.neighbours = []
		self.x = _x
		self.y = _y
	
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
	func _init(_size: Vector2, _cells: Array, include_diagonal: bool):
		self.cells = _cells
		self.size = _size
		
		# Link them together
		for index in range(self.size.x * self.size.y):
			var cell = self.cells[index]
			var i = index / int(self.size.y)
			var j = index % int(self.size.y)
			var colour: Color = cell.get_colour()
			
			if include_diagonal:
				cell.add_neighbour(get_cellv_if_match(colour, i - 1, j - 1))
				cell.add_neighbour(get_cellv_if_match(colour, i - 1, j + 1))
				cell.add_neighbour(get_cellv_if_match(colour, i + 1, j - 1))
				cell.add_neighbour(get_cellv_if_match(colour, i + 1, j + 1))
			
			cell.add_neighbour(get_cellv_if_match(colour, i - 1, j))
			cell.add_neighbour(get_cellv_if_match(colour, i, j - 1))
			cell.add_neighbour(get_cellv_if_match(colour, i, j + 1))
			cell.add_neighbour(get_cellv_if_match(colour, i + 1, j))
	
	func get_cellv_if_match(colour: Color, x: int, y: int):
		var cell = self.get_cell(x, y)
		if cell == null:
			return null
		
		if cell.get_colour() == colour:
			return cell
		
		return null
	
	func get_cell(x: int, y: int):
		if (x < 0 or y < 0 or x >= self.size.x or y >= self.size.y):
			return null
		return self.cells[x * size.y + y]
		
	func get_size():
		return self.size

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
			var cell_colour = image.get_pixel(i, j)
			cells.append(Cell.new(i, j, cell_colour))
	
	var cell_grid: CellGrid = CellGrid.new(image.get_size(), cells, include_diagonal)
	
	# Perform a Depth First Search to generate the Cell Arrays
	var explored_roots = {}
	var explored_cells = {}
	for i in range(image.get_width()):
		for j in range(image.get_height()):
			var root: Cell = cell_grid.get_cell(i, j)
			
			if !explored_cells.has(root):
				# Initialise this group
				explored_roots[root] = [root]
				explored_cells[root] = null
				
				# Now continue the DFS
				var fringe = root.get_neighbours()
					
				while fringe.size() > 0:
					# Expand the next node
					var cell: Cell = fringe.pop_back()
					
					# Check if we're already been explored
					if explored_cells.has(cell):
						continue
						
					# Add this cell to the root counter
					explored_roots[root].append(cell)
					explored_cells[cell] = null
					
					# Add all neighbours to the fringe
					for neighbour in cell.get_neighbours():
						if not explored_cells.has(neighbour):
							fringe.append(neighbour)
	
	new_image.lock()
	for region in explored_roots.values():
		if ((largest_island > 0 and region.size() > largest_island) or
				(smallest_island > 0 and region.size() < smallest_island)):
			continue
		
		for cell in region:
			new_image.set_pixel(cell.x, cell.y, cell.get_colour())
	new_image.unlock()
	image.unlock()
	
	return new_image
