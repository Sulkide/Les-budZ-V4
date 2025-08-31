namespace ImporterForGIMPImageFiles {
    internal static class Decoding {

        //RLE decoding.
        public static byte[] decodeRLEStream(byte[] bytes, ref long offset, int length) {

            //Set up the array of decoded bytes.
            byte[] decodedBytes = new byte[length];

            //Loop over the bytes, populating the decoded bytes array according to the RLE algorithm.
            int index = 0;
            while (index < length) {
                byte operation = bytes[offset++];
                if (operation <= 126) {
                    byte repeatedByte = bytes[offset++];
                    for (int i = 0; i < operation + 1; i++)
                        decodedBytes[index++] = repeatedByte;
                }
                else if (operation == 127 || operation == 128) {
                    byte MSB = bytes[offset++];
                    byte LSB = bytes[offset++];
                    int iterations = (MSB * 256) + LSB;
                    if (operation == 127) {
                        byte repeatedByte = bytes[offset++];
                        for (int i = 0; i < iterations; i++)
                            decodedBytes[index++] = repeatedByte;
                    }
                    else
                        for (int i = 0; i < iterations; i++)
                            decodedBytes[index++] = bytes[offset++];
                }
                else
                    for (int i = 0; i < 256 - operation; i++)
                        decodedBytes[index++] = bytes[offset++];
            }

            //Throw an exception if the stream doesn't end as expected.
            if (index != length)
                throw new ImporterForGIMPImageFilesException(5, "Invalid RLE Encoding Stream");

            //Return the decoded bytes.
            return decodedBytes;
        }

        //Zlib decoding.
        public static byte[] decodeZlibStream(byte[] data, ref long offset, int length) =>
                Ionic.Zlib.ImporterForGIMPImageFiles_DeflateStream.UncompressBuffer(data, ref offset);
    }
}