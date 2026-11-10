const dateTimeFormat = new Intl.DateTimeFormat('pt-BR', { dateStyle: 'short', timeStyle: 'short' })
const dateFormat = new Intl.DateTimeFormat('pt-BR', { dateStyle: 'short' })
const monthFormat = new Intl.DateTimeFormat('pt-BR', { month: 'short' })

export const formatDateTime = (iso) => dateTimeFormat.format(new Date(iso))
export const formatDate = (iso) => dateFormat.format(new Date(iso))

export const formatDay = (day) => day.split('-').reverse().join('/')

export const monthLabel = (year, month) =>
  `${monthFormat.format(new Date(year, month - 1, 1)).replace('.', '')}/${String(year).slice(-2)}`

export const PERIODS = [
  { value: '7d', label: '7 dias' },
  { value: '30d', label: '30 dias' },
  { value: '12m', label: '12 meses' },
  { value: 'all', label: 'Tudo' }
]

export function periodStart(period) {
  const start = new Date()
  if (period === '7d') start.setDate(start.getDate() - 7)
  else if (period === '30d') start.setDate(start.getDate() - 30)
  else if (period === '12m') start.setFullYear(start.getFullYear() - 1)
  else return null
  return start.toISOString()
}

export function nowForLocalInput() {
  const now = new Date()
  now.setMinutes(now.getMinutes() - now.getTimezoneOffset())
  return now.toISOString().slice(0, 16)
}
