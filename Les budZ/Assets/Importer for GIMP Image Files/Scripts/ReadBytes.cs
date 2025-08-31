namespace ImporterForGIMPImageFiles {
    using System;
    using System.Collections.Generic;
    using System.Text;

    internal static class ReadBytes {

        //Ensure bytes are read in the correct order.
        static byte[] getBytesInCorrectOrder(byte[] bytes, ref int offset, int count) {
            byte[] bytesToConvert = new byte[count];
            for (int i = 0; i < count; i++)
                bytesToConvert[i] = bytes[offset + (BitConverter.IsLittleEndian ? count - i - 1 : i)];
            offset += count;
            return bytesToConvert;
        }
        static byte[] getBytesInCorrectOrder(byte[] bytes, ref long offset, int count) {
            byte[] bytesToConvert = new byte[count];
            for (int i = 0; i < count; i++)
                bytesToConvert[i] = bytes[offset + (BitConverter.IsLittleEndian ? count - i - 1 : i)];
            offset += count;
            return bytesToConvert;
        }

        //Read an unsigned 16-bit integer.
        public static ushort readUInt16(byte[] bytes, ref int offset) => BitConverter.ToUInt16(getBytesInCorrectOrder(bytes, ref offset, 2), 0);
        public static ushort readUInt16(byte[] bytes, ref long offset) => BitConverter.ToUInt16(getBytesInCorrectOrder(bytes, ref offset, 2), 0);

        //Read an unsigned 32-bit integer.
        public static uint readUInt32(byte[] bytes, ref int offset) => BitConverter.ToUInt32(getBytesInCorrectOrder(bytes, ref offset, 4), 0);
        public static uint readUInt32(byte[] bytes, ref long offset) => BitConverter.ToUInt32(getBytesInCorrectOrder(bytes, ref offset, 4), 0);

        //Read a signed 32-bit integer.
        public static int readInt32(byte[] bytes, ref int offset) => BitConverter.ToInt32(getBytesInCorrectOrder(bytes, ref offset, 4), 0);
        public static int readInt32(byte[] bytes, ref long offset) => BitConverter.ToInt32(getBytesInCorrectOrder(bytes, ref offset, 4), 0);

        //Read a floating point number.
        public static float readFloat(byte[] bytes, ref int offset) => BitConverter.ToSingle(getBytesInCorrectOrder(bytes, ref offset, 4), 0);
        public static float readFloat(byte[] bytes, ref long offset) => BitConverter.ToSingle(getBytesInCorrectOrder(bytes, ref offset, 4), 0);

        //Read a pointer.
        public static long readPointer(int fileVersionNumber, byte[] bytes, ref int offset) {
            if (fileVersionNumber >= 11)
                return BitConverter.ToInt64(getBytesInCorrectOrder(bytes, ref offset, 8), 0);
            else
                return BitConverter.ToInt32(getBytesInCorrectOrder(bytes, ref offset, 4), 0);
        }
        public static long readPointer(int fileVersionNumber, byte[] bytes, ref long offset) {
            if (fileVersionNumber >= 11)
                return BitConverter.ToInt64(getBytesInCorrectOrder(bytes, ref offset, 8), 0);
            else
                return BitConverter.ToInt32(getBytesInCorrectOrder(bytes, ref offset, 4), 0);
        }

        //Read a string.
        public static string readString(byte[] bytes, ref long offset) {
            uint stringLength = readUInt32(bytes, ref offset) - 1;
            List<byte> stringBytes = new List<byte>();
            for (long i = offset; i < offset + stringLength; i++)
                stringBytes.Add(bytes[i]);
            string s = Encoding.UTF8.GetString(stringBytes.ToArray());
            offset += stringLength;
            if (bytes[offset++] != 0)
                throw new ImporterForGIMPImageFilesException(31, "String doesn't end with a 0");
            return s;
        }
    }
}