export const WATER_LEVELS = [
  { value: 'Ankle', label: 'Até o tornozelo', icon: 'mdi-gauge-empty', color: '#FBC02D', textColor: '#1D1B20', markerRadius: 7 },
  { value: 'Knee', label: 'Até o joelho', icon: 'mdi-gauge-low', color: '#F57C00', textColor: '#1D1B20', markerRadius: 9 },
  { value: 'Waist', label: 'Até a cintura', icon: 'mdi-gauge', color: '#C43E12', textColor: '#FFFFFF', markerRadius: 11 },
  { value: 'AboveWaist', label: 'Acima da cintura', icon: 'mdi-gauge-full', color: '#B71C1C', textColor: '#FFFFFF', markerRadius: 13 }
]

export const waterLevelByValue = (value) => WATER_LEVELS.find((level) => level.value === value) ?? WATER_LEVELS[0]
