using System;
using System.IO;

namespace PEAKNucleus.Objects
{
    internal class StreamInt
    {
        private Stream ioStream;

        public StreamInt(Stream ioStream)
        {
            this.ioStream = ioStream;
        }

        public int ReadInt()
        {
            var inBuffer = new byte[sizeof(int)];
            ioStream.Read(inBuffer, 0, sizeof(int));
            return BitConverter.ToInt32(inBuffer);
        }

        public int WriteInt(int intToWrite)
        {
            ioStream.Write(BitConverter.GetBytes(intToWrite));
            ioStream.Flush();

            return sizeof(int);
        }
    }
}
