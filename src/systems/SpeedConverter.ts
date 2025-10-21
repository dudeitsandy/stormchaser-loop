/**
 * Converts between game units and real-world speeds (MPH)
 * Scale: 1 game unit = ~0.3 MPH for realistic speeds
 */
export class SpeedConverter {
  private static readonly UNITS_TO_MPH = 0.3

  static toMPH(gameUnits: number): number {
    return Math.abs(gameUnits * this.UNITS_TO_MPH)
  }

  static toGameUnits(mph: number): number {
    return mph / this.UNITS_TO_MPH
  }

  static formatSpeed(gameUnits: number): string {
    const mph = this.toMPH(gameUnits)
    return `${Math.round(mph)} mph`
  }
}

// Real-world tornado speeds (in game units)
export const TORNADO_SPEEDS = {
  EF0: SpeedConverter.toGameUnits(15),  // 15 mph
  EF1: SpeedConverter.toGameUnits(20),  // 20 mph
  EF2: SpeedConverter.toGameUnits(30),  // 30 mph
  EF3: SpeedConverter.toGameUnits(40),  // 40 mph
  EF4: SpeedConverter.toGameUnits(55),  // 55 mph
  EF5: SpeedConverter.toGameUnits(70),  // 70 mph
}

// Vehicle max speeds (in game units)
export const VEHICLE_SPEEDS = {
  MAX_SPEED: SpeedConverter.toGameUnits(90),      // 90 mph
  HIGHWAY_SPEED: SpeedConverter.toGameUnits(75),  // 75 mph on highway
  OFFROAD_SPEED: SpeedConverter.toGameUnits(35),  // 35 mph off-road
  REVERSE_SPEED: SpeedConverter.toGameUnits(25),  // 25 mph reverse
}

