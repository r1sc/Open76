using System;

namespace Assets.Scripts.System.Fileparsers
{
    public class EfaParser
    {
        private int _outputOffset;
        private int _inputOffset;
        private readonly byte[] _inputData;
        private byte[] _outputData;

        private EfaParser(byte[] efaData)
        {
            _inputData = efaData;
            _inputOffset = 4;
        }

        public static byte[] ParseEfa(byte[] efaData)
        {
            EfaParser parser = new EfaParser(efaData);
            return parser.Decode();
        }

        private void DecodeCodeWord()
        {
            if (_inputOffset + 2 > _inputData.Length || _outputOffset > _outputData.Length)
            {
                return;
            }

            ushort code = BitConverter.ToUInt16(_inputData, _inputOffset);
            int outCount = (code >> 12) + 3;
            short dist = (short)(code & 4095);

            int i = 0;
            while (i < outCount && _outputOffset < _outputData.Length)
            {
                int maskedOutputOffset = 0;
                if (_outputOffset > 4096)
                {
                    maskedOutputOffset = (_outputOffset & (4096 + 8192 + 16384 + 32768 + 65536 + 131072 + 262144));
                    if (_outputOffset - maskedOutputOffset < dist)
                    {
                        maskedOutputOffset -= 4096;
                    }
                }
                int cpOfs = maskedOutputOffset + dist + i;
                byte b = 0;
                if (cpOfs < _outputOffset)
                {
                    b = _outputData[cpOfs];
                }

                if (_outputOffset < _outputData.Length)
                {
                    _outputData[_outputOffset] = b;
                }

                _outputOffset++;
                i++;
            }

            _inputOffset += 2;
        }

        // Copy input to output
        private void Copy(int count)
        {
            if (_inputOffset > _inputData.Length || _outputOffset > _outputData.Length)
            {
                return;
            }

            if (_inputOffset + count > _inputData.Length)
            {
                count = _inputData.Length - _inputOffset;
            }

            if (_outputOffset + count > _outputData.Length)
            {
                count = _outputData.Length - _outputOffset;
            }

            Buffer.BlockCopy(_inputData, _inputOffset, _outputData, _outputOffset, count);
            _inputOffset += count;
            _outputOffset += count;
        }

        // Decode control nibble
        private void DecodeControlByte(byte cb)
        {
            switch (cb)
            {
                case 0:
                    // Decode 8 bytes
                    DecodeCodeWord();
                    DecodeCodeWord();
                    DecodeCodeWord();
                    DecodeCodeWord();
                    break;
                case 1:
                    // Copy one, decode six (totally read: 7)
                    Copy(1);
                    DecodeCodeWord();
                    DecodeCodeWord();
                    DecodeCodeWord();
                    break;
                case 2:
                    // Decode two, Copy one, decode 4
                    DecodeCodeWord();
                    Copy(1);
                    DecodeCodeWord();
                    DecodeCodeWord();
                    break;
                case 3:
                    // Copy two, decode four
                    Copy(2);
                    DecodeCodeWord();
                    DecodeCodeWord();
                    break;
                case 4:
                    // Decode four bytes, Copy one, decode two
                    DecodeCodeWord();
                    DecodeCodeWord();
                    Copy(1);
                    DecodeCodeWord();
                    break;
                case 5:
                    // Copy one, decode two, Copy one, decode two
                    Copy(1);
                    DecodeCodeWord();
                    Copy(1);
                    DecodeCodeWord();
                    break;
                case 6:
                    //  Decode two, Copy two, decode two
                    DecodeCodeWord();
                    Copy(2);
                    DecodeCodeWord();
                    break;
                case 7:
                    // Copy 3 bytes, then decompress final two bytes
                    Copy(3);
                    DecodeCodeWord();
                    break;
                case 8:
                    // Decode six, Copy one
                    DecodeCodeWord();
                    DecodeCodeWord();
                    DecodeCodeWord();
                    Copy(1);
                    break;
                case 9:
                    // Copy one, decode four, Copy one
                    Copy(1);
                    DecodeCodeWord();
                    DecodeCodeWord();
                    Copy(1);
                    break;
                case 0xA:
                    // Decode two, Copy one, decode two, Copy one
                    DecodeCodeWord();
                    Copy(1);
                    DecodeCodeWord();
                    Copy(1);
                    break;
                case 0xB:
                    // Copy 2 bytes, decode two, Copy the last byte
                    Copy(2);
                    DecodeCodeWord();
                    Copy(1);
                    break;
                case 0xC:
                    // Decode four, Copy two
                    DecodeCodeWord();
                    DecodeCodeWord();
                    Copy(2);
                    break;
                case 0xD:
                    // Copy 1 byte, decode 2, Copy another 2
                    Copy(1);
                    DecodeCodeWord();
                    Copy(2);
                    break;
                case 0xE:
                    // Decode 2, then Copy 3
                    DecodeCodeWord();
                    Copy(3);
                    break;
                case 0xF:
                    // Copy 4 bytes to output
                    Copy(4);
                    break;
            }
        }

        private byte[] Decode()
        {
            int decompressedSize = BitConverter.ToInt32(_inputData, 0);
            _outputData = new byte[decompressedSize];

            while (_inputOffset < _inputData.Length)
            {
                byte cb = _inputData[_inputOffset];
                _inputOffset++;
                DecodeControlByte((byte)(cb & 15));
                DecodeControlByte((byte)(cb >> 4));
            }

            Array.Resize(ref _outputData, _outputOffset);
            return _outputData;
        }
    }
}