export async function shrinkPhotoWithoutMetadata(file, maxSide = 1600, quality = 0.8) {
  const bitmap = await createImageBitmap(file)
  const scale = Math.min(1, maxSide / Math.max(bitmap.width, bitmap.height))

  const canvas = document.createElement('canvas')
  canvas.width = Math.round(bitmap.width * scale)
  canvas.height = Math.round(bitmap.height * scale)
  canvas.getContext('2d').drawImage(bitmap, 0, 0, canvas.width, canvas.height)
  bitmap.close()

  const blob = await new Promise((resolve, reject) =>
    canvas.toBlob(
      (result) => (result ? resolve(result) : reject(new Error('Não foi possível processar a foto.'))),
      'image/jpeg',
      quality
    )
  )
  return new File([blob], 'photo.jpg', { type: 'image/jpeg' })
}
