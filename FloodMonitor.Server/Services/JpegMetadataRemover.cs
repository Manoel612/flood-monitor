namespace FloodMonitor.Server.Services;

public static class JpegMetadataRemover
{
    private const byte MarkerPrefix = 0xFF;
    private const byte StartOfImage = 0xD8;
    private const byte StartOfScan = 0xDA;
    private const byte ExifOrXmp = 0xE1;
    private const byte Iptc = 0xED;
    private const byte Comment = 0xFE;

    public static bool IsJpeg(ReadOnlySpan<byte> bytes) =>
        bytes.Length >= 3 && bytes[0] == MarkerPrefix && bytes[1] == StartOfImage && bytes[2] == MarkerPrefix;

    public static byte[]? Remove(byte[] jpeg)
    {
        if (!IsJpeg(jpeg))
            return null;

        using var output = new MemoryStream(jpeg.Length);
        output.Write(jpeg, 0, 2);
        var position = 2;

        while (position < jpeg.Length)
        {
            if (jpeg[position] != MarkerPrefix)
                return null;

            while (position < jpeg.Length && jpeg[position] == MarkerPrefix)
                position++;
            if (position >= jpeg.Length)
                return null;

            var marker = jpeg[position++];

            if (marker == StartOfScan)
            {
                output.WriteByte(MarkerPrefix);
                output.Write(jpeg, position - 1, jpeg.Length - position + 1);
                return output.ToArray();
            }

            if (HasNoLength(marker))
            {
                output.WriteByte(MarkerPrefix);
                output.WriteByte(marker);
                continue;
            }

            if (position + 2 > jpeg.Length)
                return null;
            var length = (jpeg[position] << 8) | jpeg[position + 1];
            var segmentEnd = position + length;
            if (length < 2 || segmentEnd > jpeg.Length)
                return null;

            if (marker is not (ExifOrXmp or Iptc or Comment))
            {
                output.WriteByte(MarkerPrefix);
                output.Write(jpeg, position - 1, segmentEnd - position + 1);
            }

            position = segmentEnd;
        }

        return null;
    }

    private static bool HasNoLength(byte marker) => marker is 0x01 or (>= 0xD0 and <= 0xD7);
}
