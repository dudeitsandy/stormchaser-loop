import { createNoise2D } from 'simplex-noise'

export interface TerrainTile {
  x: number
  y: number
  type: 'grass' | 'field' | 'forest' | 'road' | 'highway' | 'building' | 'tree' | 'water' | 'mud' | 'desert' | 'house' | 'farm' | 'barn' | 'silo' | 'office' | 'gas_station' | 'parking' | 'sidewalk' | 'road_horizontal' | 'road_vertical' | 'highway_horizontal' | 'highway_vertical'
  color: number
  collidable: boolean  // Can vehicles pass through?
  speedModifier: number  // 1.0 = normal, 0.5 = half speed, 1.5 = faster
  buildingType?: string // Specific building type for detailed rendering
}

export class TerrainGenerator {
  private noise1: ReturnType<typeof createNoise2D>
  private noise2: ReturnType<typeof createNoise2D>
  private noise3: ReturnType<typeof createNoise2D>
  private tileSize: number
  private seed: number

  constructor(seed: number = Math.random(), tileSize: number = 50) {
    // Create multiple noise functions for layering
    this.seed = seed
    this.tileSize = tileSize
    this.noise1 = createNoise2D(() => seed)
    this.noise2 = createNoise2D(() => seed + 1)
    this.noise3 = createNoise2D(() => seed + 2)
  }

  /**
   * Generate terrain for the entire world with proper town layout
   */
  generateTerrain(worldWidth: number, worldHeight: number): TerrainTile[] {
    const tiles: TerrainTile[] = []
    const tilesX = Math.ceil(worldWidth / this.tileSize)
    const tilesY = Math.ceil(worldHeight / this.tileSize)

    // Create town layout with districts
    const layout = this.generateTownLayout(tilesX, tilesY)
    
    for (let ty = 0; ty < tilesY; ty++) {
      for (let tx = 0; tx < tilesX; tx++) {
        const x = tx * this.tileSize + this.tileSize / 2
        const y = ty * this.tileSize + this.tileSize / 2
        
        const tile = this.createTileFromLayout(layout[ty][tx], tx, ty)
        tile.x = x
        tile.y = y
        tiles.push(tile)
      }
    }

    return tiles
  }

  /**
   * Generate a proper town layout using L-system road generation
   */
  private generateTownLayout(width: number, height: number): string[][] {
    const layout: string[][] = []
    
    // Initialize with grass
    for (let y = 0; y < height; y++) {
      layout[y] = []
      for (let x = 0; x < width; x++) {
        layout[y][x] = 'grass'
      }
    }
    
    // Generate road network using L-system approach
    this.generateRoadNetwork(layout, width, height)
    
    // Create districts based on road proximity
    this.createDistricts(layout, width, height)
    
    // Add parks and water features
    this.addParksAndWater(layout, width, height)
    
    // Add sidewalks next to roads
    this.addSidewalks(layout, width, height)
    
    return layout
  }

  /**
   * Generate road network using L-system algorithm
   */
  private generateRoadNetwork(layout: string[][], width: number, height: number) {
    // Main highways (arterial roads)
    this.createMainHighways(layout, width, height)
    
    // Secondary roads (collector roads)
    this.createSecondaryRoads(layout, width, height)
    
    // Local streets (residential roads)
    this.createLocalStreets(layout, width, height)
  }

  /**
   * Create main highways (arterial roads)
   */
  private createMainHighways(layout: string[][], width: number, height: number) {
    // Main horizontal highway
    const highwayY = Math.floor(height * 0.5)
    this.drawRoad(layout, 0, highwayY, width, highwayY, 'highway', 'horizontal')
    
    // Main vertical highway
    const highwayX = Math.floor(width * 0.4)
    this.drawRoad(layout, highwayX, 0, highwayX, height, 'highway', 'vertical')
    
    // Secondary arterial roads
    const arterialY1 = Math.floor(height * 0.25)
    const arterialY2 = Math.floor(height * 0.75)
    const arterialX1 = Math.floor(width * 0.2)
    const arterialX2 = Math.floor(width * 0.6)
    
    this.drawRoad(layout, 0, arterialY1, width, arterialY1, 'highway', 'horizontal')
    this.drawRoad(layout, 0, arterialY2, width, arterialY2, 'highway', 'horizontal')
    this.drawRoad(layout, arterialX1, 0, arterialX1, height, 'highway', 'vertical')
    this.drawRoad(layout, arterialX2, 0, arterialX2, height, 'highway', 'vertical')
  }

  /**
   * Create secondary roads (collector roads)
   */
  private createSecondaryRoads(layout: string[][], width: number, height: number) {
    const spacing = 12 // Distance between collector roads
    
    // Horizontal collector roads
    for (let y = spacing; y < height; y += spacing) {
      if (this.isValidPosition(layout, 0, y) && layout[y][0] === 'grass') {
        this.drawRoad(layout, 0, y, width, y, 'road', 'horizontal')
      }
    }
    
    // Vertical collector roads
    for (let x = spacing; x < width; x += spacing) {
      if (this.isValidPosition(layout, x, 0) && layout[0][x] === 'grass') {
        this.drawRoad(layout, x, 0, x, height, 'road', 'vertical')
      }
    }
  }

  /**
   * Create local streets (residential roads) with strategic open spaces
   */
  private createLocalStreets(layout: string[][], width: number, height: number) {
    // Create local streets in residential areas with larger spacing for open areas
    const residentialAreas = [
      { x: 0, y: 0, width: Math.floor(width * 0.4), height: Math.floor(height * 0.4) },
      { x: Math.floor(width * 0.6), y: 0, width: Math.floor(width * 0.4), height: Math.floor(height * 0.4) }
    ]
    
    residentialAreas.forEach(area => {
      const streetSpacing = 12 // Increased spacing for more open areas
      for (let y = area.y + streetSpacing; y < area.y + area.height; y += streetSpacing) {
        for (let x = area.x; x < area.x + area.width; x++) {
          if (this.isValidPosition(layout, x, y) && layout[y][x] === 'grass') {
            layout[y][x] = 'road_horizontal'
          }
        }
      }
      
      for (let x = area.x + streetSpacing; x < area.x + area.width; x += streetSpacing) {
        for (let y = area.y; y < area.y + area.height; y++) {
          if (this.isValidPosition(layout, x, y) && layout[y][x] === 'grass') {
            layout[y][x] = 'road_vertical'
          }
        }
      }
    })

    // Create strategic open areas for tornado chasing
    this.createOpenChaseAreas(layout, width, height)
  }

  /**
   * Create strategic open areas for tornado chasing
   */
  private createOpenChaseAreas(layout: string[][], width: number, height: number) {
    // Large open field in center for strategic positioning
    const centerX = Math.floor(width * 0.5)
    const centerY = Math.floor(height * 0.5)
    const fieldSize = 8

    for (let y = centerY - fieldSize; y <= centerY + fieldSize; y++) {
      for (let x = centerX - fieldSize; x <= centerX + fieldSize; x++) {
        if (this.isValidPosition(layout, x, y) && layout[y][x] === 'grass') {
          layout[y][x] = 'field' // Open field for chasing
        }
      }
    }

    // Additional open areas near highways for strategic positioning
    const highwayOpenAreas = [
      { x: Math.floor(width * 0.15), y: Math.floor(height * 0.5), size: 6 },
      { x: Math.floor(width * 0.65), y: Math.floor(height * 0.5), size: 6 },
      { x: Math.floor(width * 0.4), y: Math.floor(height * 0.25), size: 5 },
      { x: Math.floor(width * 0.4), y: Math.floor(height * 0.75), size: 5 }
    ]

    highwayOpenAreas.forEach(area => {
      for (let y = area.y - area.size; y <= area.y + area.size; y++) {
        for (let x = area.x - area.size; x <= area.x + area.size; x++) {
          if (this.isValidPosition(layout, x, y) && layout[y][x] === 'grass') {
            layout[y][x] = 'field' // Strategic open areas
          }
        }
      }
    })
  }

  /**
   * Draw a road segment with proper orientation
   */
  private drawRoad(layout: string[][], x1: number, y1: number, x2: number, y2: number, roadType: string, direction: 'horizontal' | 'vertical') {
    if (direction === 'horizontal') {
      for (let x = Math.min(x1, x2); x <= Math.max(x1, x2); x++) {
        if (this.isValidPosition(layout, x, y1)) {
          layout[y1][x] = roadType === 'highway' ? 'highway_horizontal' : 'road_horizontal'
        }
      }
    } else {
      for (let y = Math.min(y1, y2); y <= Math.max(y1, y2); y++) {
        if (this.isValidPosition(layout, x1, y)) {
          layout[y][x1] = roadType === 'highway' ? 'highway_vertical' : 'road_vertical'
        }
      }
    }
  }

  /**
   * Create districts based on road proximity and zoning rules
   */
  private createDistricts(layout: string[][], width: number, height: number) {
    for (let y = 0; y < height; y++) {
      for (let x = 0; x < width; x++) {
        if (this.isValidPosition(layout, x, y) && layout[y][x] === 'grass') {
          const buildingType = this.determineBuildingType(layout, x, y, width, height)
          if (buildingType !== 'grass') {
            layout[y][x] = buildingType
          }
        }
      }
    }
  }

  /**
   * Determine building type based on location and road proximity
   */
  private determineBuildingType(layout: string[][], x: number, y: number, width: number, height: number): string {
    // Check distance to nearest road
    const roadDistance = this.getDistanceToNearestRoad(layout, x, y, width, height)
    
    // Check if near highway (commercial/industrial)
    const highwayDistance = this.getDistanceToNearestHighway(layout, x, y, width, height)
    
    // Determine zone based on position and road proximity
    const zone = this.determineZone(x, y, width, height)
    
    // Apply zoning rules
    switch (zone) {
      case 'residential':
        return this.getResidentialBuilding(roadDistance, x, y)
      case 'commercial':
        return this.getCommercialBuilding(roadDistance, highwayDistance, x, y)
      case 'industrial':
        return this.getIndustrialBuilding(roadDistance, highwayDistance, x, y)
      case 'rural':
        return this.getRuralBuilding(roadDistance, x, y)
      default:
        return 'grass'
    }
  }

  /**
   * Determine zone based on position
   */
  private determineZone(x: number, y: number, width: number, height: number): string {
    const centerX = width * 0.5
    const centerY = height * 0.5
    
    // Rural area (east side)
    if (x > width * 0.7) return 'rural'
    
    // Industrial area (south)
    if (y > height * 0.6) return 'industrial'
    
    // Commercial area (near center and highways)
    if (x > width * 0.3 && x < width * 0.7 && y < height * 0.4) return 'commercial'
    
    // Residential (default)
    return 'residential'
  }

  /**
   * Get residential building type
   */
  private getResidentialBuilding(roadDistance: number, x: number, y: number): string {
    // Houses near roads - REDUCED DENSITY for more open space
    if (roadDistance <= 2) {
      return Math.random() < 0.5 ? 'house' : 'grass' // Reduced from 0.8 to 0.5
    }
    
    // Parks and green spaces away from roads
    if (roadDistance > 4) {
      return Math.random() < 0.4 ? 'field' : 'grass' // Increased parks
    }
    
    return 'grass'
  }

  /**
   * Get commercial building type
   */
  private getCommercialBuilding(roadDistance: number, highwayDistance: number, x: number, y: number): string {
    // Gas stations near highways - REDUCED DENSITY
    if (highwayDistance <= 1 && roadDistance <= 2) {
      return Math.random() < 0.2 ? 'gas_station' : 'office' // Reduced from 0.3
    }
    
    // Offices near main roads - REDUCED DENSITY
    if (roadDistance <= 3) {
      return Math.random() < 0.4 ? 'office' : 'building' // Reduced from 0.7
    }
    
    // Parking lots - REDUCED DENSITY
    if (roadDistance <= 5) {
      return Math.random() < 0.2 ? 'parking' : 'grass' // Reduced from 0.4
    }
    
    return 'grass'
  }

  /**
   * Get industrial building type
   */
  private getIndustrialBuilding(roadDistance: number, highwayDistance: number, x: number, y: number): string {
    // Large buildings near highways - REDUCED DENSITY
    if (highwayDistance <= 2) {
      return Math.random() < 0.5 ? 'building' : 'grass' // Reduced from 0.8
    }
    
    // Industrial buildings near roads - REDUCED DENSITY
    if (roadDistance <= 4) {
      return Math.random() < 0.3 ? 'building' : 'grass' // Reduced from 0.6
    }
    
    return 'grass'
  }

  /**
   * Get rural building type
   */
  private getRuralBuilding(roadDistance: number, x: number, y: number): string {
    // Farms and fields
    const noise = this.noise1(x * 0.1, y * 0.1)
    
    if (noise > 0.4) {
      return 'field'
    } else if (noise > 0.1) {
      return 'farm'
    } else if (noise > -0.2) {
      return 'barn'
    } else if (noise > -0.4) {
      return 'silo'
    }
    
    return 'grass'
  }

  /**
   * Get distance to nearest road
   */
  private getDistanceToNearestRoad(layout: string[][], x: number, y: number, width: number, height: number): number {
    let minDistance = Infinity
    
    for (let dy = -5; dy <= 5; dy++) {
      for (let dx = -5; dx <= 5; dx++) {
        const nx = x + dx
        const ny = y + dy
        if (this.isValidPosition(layout, nx, ny)) {
          const tileType = layout[ny][nx]
          if (tileType.includes('road') || tileType.includes('highway')) {
            const distance = Math.abs(dx) + Math.abs(dy)
            minDistance = Math.min(minDistance, distance)
          }
        }
      }
    }
    
    return minDistance === Infinity ? 10 : minDistance
  }

  /**
   * Get distance to nearest highway
   */
  private getDistanceToNearestHighway(layout: string[][], x: number, y: number, width: number, height: number): number {
    let minDistance = Infinity
    
    for (let dy = -3; dy <= 3; dy++) {
      for (let dx = -3; dx <= 3; dx++) {
        const nx = x + dx
        const ny = y + dy
        if (this.isValidPosition(layout, nx, ny)) {
          const tileType = layout[ny][nx]
          if (tileType.includes('highway')) {
            const distance = Math.abs(dx) + Math.abs(dy)
            minDistance = Math.min(minDistance, distance)
          }
        }
      }
    }
    
    return minDistance === Infinity ? 10 : minDistance
  }

  /**
   * Add parks and water features
   */
  private addParksAndWater(layout: string[][], width: number, height: number) {
    // Add a central park
    const parkX = Math.floor(width * 0.5)
    const parkY = Math.floor(height * 0.6)
    const parkSize = 6
    
    for (let y = parkY; y < parkY + parkSize && y < height; y++) {
      for (let x = parkX; x < parkX + parkSize && x < width; x++) {
        if (layout[y][x] === 'grass') {
          layout[y][x] = 'field' // Park area
        }
      }
    }
    
    // Add some water features
    const waterX = Math.floor(width * 0.8)
    const waterY = Math.floor(height * 0.2)
    const waterSize = 4
    
    for (let y = waterY; y < waterY + waterSize && y < height; y++) {
      for (let x = waterX; x < waterX + waterSize && x < width; x++) {
        if (layout[y][x] === 'grass') {
          layout[y][x] = 'water'
        }
      }
    }
  }

  /**
   * Add sidewalks next to roads
   */
  private addSidewalks(layout: string[][], width: number, height: number) {
    for (let y = 0; y < height; y++) {
      for (let x = 0; x < width; x++) {
        if (layout[y][x] === 'road' || layout[y][x] === 'highway') {
          // Add sidewalks around roads
          for (let dy = -1; dy <= 1; dy++) {
            for (let dx = -1; dx <= 1; dx++) {
              const nx = x + dx
              const ny = y + dy
              if (this.isValidPosition(layout, nx, ny) && layout[ny][nx] === 'grass') {
                layout[ny][nx] = 'sidewalk'
              }
            }
          }
        }
      }
    }
  }

  /**
   * Get random building type for district
   */
  private getRandomBuildingType(district: 'residential' | 'commercial' | 'industrial'): string {
    const types = {
      residential: ['house', 'house', 'house', 'grass', 'grass'],
      commercial: ['office', 'gas_station', 'building', 'parking', 'grass'],
      industrial: ['building', 'building', 'building', 'grass', 'grass']
    }
    
    const districtTypes = types[district]
    return districtTypes[Math.floor(Math.random() * districtTypes.length)]
  }

  /**
   * Check if position is valid in layout
   */
  private isValidPosition(layout: string[][], x: number, y: number): boolean {
    return y >= 0 && y < layout.length && x >= 0 && x < layout[y].length
  }

  /**
   * Create tile from layout data
   */
  private createTileFromLayout(type: string, tx: number, ty: number): TerrainTile {
    const tile: TerrainTile = {
      x: 0, // Will be set by caller
      y: 0, // Will be set by caller
      type: type as any,
      color: this.getTileColor(type),
      collidable: this.getTileCollidable(type),
      speedModifier: this.getTileSpeedModifier(type),
      buildingType: type
    }
    
    return tile
  }

  /**
   * Get tile color based on type
   */
  private getTileColor(type: string): number {
    const colors = {
      grass: 0x4a7c59,
      field: 0x8b7355,
      forest: 0x2d5016,
      road: 0x404040,
      highway: 0x2a2a2a,
      road_horizontal: 0x404040,
      road_vertical: 0x404040,
      highway_horizontal: 0x2a2a2a,
      highway_vertical: 0x2a2a2a,
      building: 0x654321,
      tree: 0x2d5016,
      water: 0x4682b4,
      mud: 0x8b7355,
      desert: 0xf4a460,
      house: 0x8b4513,
      farm: 0x9acd32,
      barn: 0xa0522d,
      silo: 0x696969,
      office: 0x708090,
      gas_station: 0xff6347,
      parking: 0x708090,
      sidewalk: 0x696969
    }
    
    return colors[type as keyof typeof colors] || 0x4a7c59
  }

  /**
   * Get tile collision properties
   */
  private getTileCollidable(type: string): boolean {
    const collidable = ['building', 'tree', 'house', 'barn', 'silo', 'office', 'gas_station']
    return collidable.includes(type)
  }

  /**
   * Get tile speed modifier
   */
  private getTileSpeedModifier(type: string): number {
    const modifiers = {
      highway: 1.3,
      highway_horizontal: 1.3,
      highway_vertical: 1.3,
      road: 1.1,
      road_horizontal: 1.1,
      road_vertical: 1.1,
      sidewalk: 0.9,
      grass: 0.8,
      field: 0.7,
      mud: 0.5,
      water: 0.3,
      parking: 0.9,
      building: 0.0, // Can't drive through
      tree: 0.0,
      house: 0.0,
      barn: 0.0,
      silo: 0.0,
      office: 0.0,
      gas_station: 0.0
    }
    
    return modifiers[type as keyof typeof modifiers] || 1.0
  }

  /**
   * Get terrain properties at a world position
   */
  getTerrainAt(worldX: number, worldY: number): TerrainTile {
    const tx = Math.floor(worldX / this.tileSize)
    const ty = Math.floor(worldY / this.tileSize)
    return this.createTileFromLayout('grass', tx, ty) // Fallback
  }

  setTerrainAt(worldX: number, worldY: number, newType: string): void {
    const tx = Math.floor(worldX / this.tileSize)
    const ty = Math.floor(worldY / this.tileSize)
    
    // This would need to be implemented to update the layout
    // For now, just a placeholder
    console.log(`Setting terrain at (${tx}, ${ty}) to ${newType}`)
  }
}

