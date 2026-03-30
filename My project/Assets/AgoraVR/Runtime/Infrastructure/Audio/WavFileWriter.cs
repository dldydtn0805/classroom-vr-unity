using System;
using System.IO;
using System.Text;

namespace AgoraVR.Infrastructure.Audio
{
    internal static class WavFileWriter
    {
        public static void Write(string filePath, float[] samples, int channelCount, int sampleRate)
        {
            short[] int16Buffer = ConvertSamples(samples);
            byte[] byteBuffer = new byte[int16Buffer.Length * sizeof(short)];
            Buffer.BlockCopy(int16Buffer, 0, byteBuffer, 0, byteBuffer.Length);

            using (FileStream stream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            using (BinaryWriter writer = new BinaryWriter(stream, Encoding.ASCII))
            {
                int bitsPerSample = 16;
                int byteRate = sampleRate * channelCount * bitsPerSample / 8;
                short blockAlign = (short)(channelCount * bitsPerSample / 8);

                writer.Write(Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(36 + byteBuffer.Length);
                writer.Write(Encoding.ASCII.GetBytes("WAVE"));
                writer.Write(Encoding.ASCII.GetBytes("fmt "));
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)channelCount);
                writer.Write(sampleRate);
                writer.Write(byteRate);
                writer.Write(blockAlign);
                writer.Write((short)bitsPerSample);
                writer.Write(Encoding.ASCII.GetBytes("data"));
                writer.Write(byteBuffer.Length);
                writer.Write(byteBuffer);
            }
        }

        private static short[] ConvertSamples(float[] samples)
        {
            short[] int16Buffer = new short[samples.Length];
            for (int i = 0; i < samples.Length; i++)
            {
                float clampedValue = Math.Max(-1f, Math.Min(1f, samples[i]));
                int16Buffer[i] = (short)Math.Round(clampedValue * short.MaxValue);
            }

            return int16Buffer;
        }
    }
}
