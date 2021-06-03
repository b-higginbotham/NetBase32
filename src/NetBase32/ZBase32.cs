using System;

namespace NetBase32
{
    /// <summary>
    /// Provides encoding and decoding for converting between binary data and z-base-32 strings.
    /// </summary>
    public class ZBase32
    {
        //The alphabet for z-Base-32, as well as the reasoning for choosing it, is described here:
        //https://philzimmermann.com/docs/human-oriented-base-32-encoding.txt
        private static readonly char[] alphabet =
            {'y', 'b', 'n', 'd', 'r', 'f', 'g', '8', 'e', 'j', 'k', 'm', 'c', 'p', 'q', 'x', 'o', 't', '1', 'u', 'w', 'i', 's', 'z', 'a', '3', '4', '5', 'h', '7', '6', '9'};

        private static readonly byte[] decoder = new byte[256];

        static ZBase32()
        {
            //Initialize all elements to a value outside the range of the encoder.
            //This will be used to validate input.
            for (int i = 0; i < decoder.Length; i++)
            {
                decoder[i] = 0xFF;
            }

            for (byte i = 0; i < alphabet.Length; i++)
            {
                decoder[alphabet[i]] = i;
            }

            //Map common transcription errors to their closest valid characters.
            decoder['0'] = decoder['o'];
            decoder['2'] = decoder['z'];
            decoder['l'] = decoder['1'];
            decoder['|'] = decoder['1'];
            decoder['v'] = decoder['u'];

            //Map uppercase characters to their corresponding lowercase characters.
            decoder['Y'] = decoder['y'];
            decoder['B'] = decoder['b'];
            decoder['N'] = decoder['n'];
            decoder['D'] = decoder['d'];
            decoder['R'] = decoder['r'];
            decoder['F'] = decoder['f'];
            decoder['G'] = decoder['g'];
            decoder['E'] = decoder['e'];
            decoder['J'] = decoder['j'];
            decoder['K'] = decoder['k'];
            decoder['M'] = decoder['m'];
            decoder['C'] = decoder['c'];
            decoder['P'] = decoder['p'];
            decoder['Q'] = decoder['q'];
            decoder['X'] = decoder['x'];
            decoder['O'] = decoder['o'];
            decoder['T'] = decoder['t'];
            decoder['U'] = decoder['u'];
            decoder['W'] = decoder['w'];
            decoder['I'] = decoder['i'];
            decoder['S'] = decoder['s'];
            decoder['Z'] = decoder['z'];
            decoder['A'] = decoder['a'];
            decoder['H'] = decoder['h'];

            //Although 'v' and 'l' are not part of the alphabet, they are mapped as potential
            //transcription errors, so the uppercase characters can safely be mapped to the
            //lowercase mapping as well.
            decoder['L'] = decoder['l'];
            decoder['V'] = decoder['u'];

            //'-' is used as a formatting character for readability.
            //Decoding it to a value that is not mapped by the encoder will allow it to pass input
            //validation without affecting the decoded output.
            decoder['-'] = 0xF0;
        }

        /// <summary>
        /// Converts the value of an array of 8-bit unsigned integers to its equivalent string
        /// representation that is encoded in z-base-32.
        /// </summary>
        /// <param name="data">
        /// An array of 8-bit unsigned integers.
        /// </param>
        /// <param name="formatOptions">
        /// <see cref="FormatOptions.IncludeDashes"/> to include a "-" character after every 8
        /// characters or <see cref="FormatOptions.None"/> to exclude dashes.
        /// </param>
        /// <returns>
        /// The string representation in z-base-32 of <paramref name="data"/>.
        /// </returns>
        public static string Encode(byte[] data, FormatOptions formatOptions = FormatOptions.IncludeDashes)
        {
            if (data.Length == 0)
            {
                return string.Empty;
            }

            var length = (int)Math.Ceiling(data.Length * 8 / 5.0);

            var includeFormatting = formatOptions == FormatOptions.IncludeDashes;

            if (includeFormatting)
            {
                var dashes = length / 8;

                if (length % 8 == 0)
                {
                    dashes--;
                }

                length += dashes;
            }

            var result = new char[length];

            var j = 0;

            var increment = includeFormatting ? 9 : 8;

            for (int i = 0; i < data.Length; i += 5)
            {
                result[j] = alphabet[(data[i]) >> 3];

                if (i + 1 >= data.Length)
                {
                    result[j + 1] = alphabet[(data[i] & 0x07) << 2];
                    break;
                }

                result[j + 1] = alphabet[((data[i] & 0x07) << 2) | ((data[i + 1] & 0xC0) >> 6)];
                result[j + 2] = alphabet[(data[i + 1] & 0x3E) >> 1];

                if (i + 2 >= data.Length)
                {
                    result[j + 3] = alphabet[(data[i + 1] & 0x01) << 4];
                    break;
                }

                result[j + 3] = alphabet[((data[i + 1] & 0x01) << 4) | (data[i + 2] >> 4)];

                if (i + 3 >= data.Length)
                {
                    result[j + 4] = alphabet[(data[i + 2] & 0x0F) << 1];
                    break;
                }

                result[j + 4] = alphabet[((data[i + 2] & 0x0F) << 1) | ((data[i + 3] & 0x80) >> 7)];
                result[j + 5] = alphabet[(data[i + 3] & 0x7C) >> 2];

                if (i + 4 >= data.Length)
                {
                    result[j + 6] = alphabet[(data[i + 3] & 0x03) << 3];
                    break;
                }

                result[j + 6] = alphabet[((data[i + 3] & 0x03) << 3) | (data[i + 4] >> 5)];
                result[j + 7] = alphabet[data[i + 4] & 0x1F];

                //Every 5 bytes will output 8 characters.
                //Append a '-' at the end of each 8 character block for readability.
                if (i + 5 < data.Length && includeFormatting)
                {
                    result[j + 8] = '-';
                }

                j += increment;
            }

            return new string(result);
        }

        /// <summary>
        /// Converts the specified string, which represents binary data encoded in z-base-32, to an
        /// equivalent 8-bit unsigned integer array.
        /// </summary>
        /// <param name="data">
        /// The string to decode.
        /// </param>
        /// <returns>
        /// An array of 8-bit unsigned integers that is equivalent to <paramref name="data"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="data"/> is not a valid z-base-32 encoded string.
        /// </exception>
        public static byte[] Decode(string data)
        {
            if (data.Length == 0)
            {
                return Array.Empty<byte>();
            }

            //Indexing is faster on char[] than it is on string, so convert the input to a char array before processing.
            var inputChars = data.ToCharArray();

            Validate(inputChars);

            var inputLength = inputChars.Length;

            int outputLength = inputLength * 5 / 8;

            //All characters are data characters by default.
            int increment = 8;

            if (inputLength >= 9 && inputChars[8] == '-')
            {
                //Base 32 causes a 8/5 expansion. We also have to account for the '-' character that is
                //appended after every 8 characters of data. 
                outputLength = (inputLength - (inputLength / 9)) * 5 / 8;

                //Every 9th character is a non-data character added for readabiltiy so use 8
                //characters and skip the 9th.
                increment = 9;
            }

            var result = new byte[outputLength];

            var j = 0;

            for (int i = 0; i < outputLength; i += 5)
            {
                result[i] = (byte)(decoder[inputChars[j]] << 3);

                if (j + 1 >= inputLength)
                    break;

                result[i] |= (byte)(decoder[inputChars[j + 1]] >> 2);

                if (i + 1 >= outputLength)
                    break;

                result[i + 1] = (byte)(decoder[inputChars[j + 1]] << 6);

                if (j + 2 >= inputLength)
                    break;

                result[i + 1] |= (byte)(decoder[inputChars[j + 2]] << 1);

                if (j + 3 >= inputLength)
                    break;

                result[i + 1] |= (byte)(decoder[inputChars[j + 3]] >> 4);

                if (i + 2 >= outputLength)
                    break;

                result[i + 2] = (byte)(decoder[inputChars[j + 3]] << 4);

                if (j + 4 >= inputLength)
                    break;

                result[i + 2] |= (byte)(decoder[inputChars[j + 4]] >> 1);

                if (i + 3 >= outputLength)
                    break;

                result[i + 3] = (byte)(decoder[inputChars[j + 4]] << 7);

                if (j + 5 >= inputLength)
                    break;

                result[i + 3] |= (byte)(decoder[inputChars[j + 5]] << 2);

                if (j + 6 >= inputLength)
                    break;

                result[i + 3] |= (byte)(decoder[inputChars[j + 6]] >> 3);

                if (i + 4 >= outputLength)
                    break;

                result[i + 4] = (byte)(decoder[inputChars[j + 6]] << 5);

                if (j + 7 >= inputLength)
                    break;

                result[i + 4] |= decoder[inputChars[j + 7]];

                j += increment;
            }

            return result;
        }


        /// <summary>
        /// Checks a string to ensure that it contains valid z-base-32 encoded data.
        /// </summary>
        /// <param name="data">
        /// The string to validate.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="data"/> is not a valid z-base-32 encoded string.
        /// </exception>
        public static void Validate(string data) => Validate(data.ToCharArray());

        private static void Validate(char[] data)
        {
            //All values in the decoder were initialized to 0xFF. Any valid characters will
            //override this value in the decoder with the value mapped from the alphabet or with
            //values mapped for error correction. Any character that still maps to a value of 0xFF
            //is invalid.
            for (int i = 0; i < data.Length; i++)
            {
                if (decoder[data[i]] == 0xFF)
                {
                    throw new ArgumentException($"Encoded value: \"{new string(data)}\" contains invalid character: \"{data[i]}\" in position: {i + 1}.");
                }
            }
        }
    }
}
