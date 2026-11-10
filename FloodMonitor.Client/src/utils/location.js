import { formatDateTime } from '@/utils/formatters'

export function locationQuery(location) {
  switch (location?.type) {
    case 'state':
      return { state: location.state }
    case 'city':
      return { city: location.city, state: location.state }
    case 'neighborhood':
      return { neighborhood: location.neighborhood, city: location.city, state: location.state }
    case 'address':
      return { lat: location.lat, lon: location.lon, radius: location.radius }
    default:
      return {}
  }
}

const joinParts = (...parts) => parts.filter(Boolean).join(', ')

export function placeName({ neighborhood, city, state }) {
  return joinParts(neighborhood, city, state)
}

export function cityStateAndDate(occurrence) {
  return [joinParts(occurrence.city, occurrence.state), formatDateTime(occurrence.occurredAt)]
    .filter(Boolean)
    .join(' · ')
}

export function formatRadius(meters) {
  return meters >= 1000 ? `${meters / 1000} km` : `${meters} m`
}
