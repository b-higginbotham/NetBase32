using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Security.Cryptography;
using NetBase32;

namespace Base32EncodingTests
{
    [TestClass]
    public class ZBase32Tests
    {
        [TestMethod]
        public void When_encoding_and_decoding_random_data_with_formatting()
        {
            for (int i = 0; i < 100; i++)
            {
                var expected = GenerateData();

                var encoded = ZBase32.Encode(expected);

                var actual = ZBase32.Decode(encoded);

                Assert.IsTrue(Compare(expected, actual));
            }
        }

        [TestMethod]
        public void When_encoding_and_decoding_random_data_with_uppercase()
        {
            for (int i = 0; i < 100; i++)
            {
                var expected = GenerateData();

                var encoded = ZBase32.Encode(expected).ToUpperInvariant();

                var actual = ZBase32.Decode(encoded);

                Assert.IsTrue(Compare(expected, actual));
            }
        }

        [TestMethod]
        public void When_decoding_random_data_without_formatting()
        {
            for (int i = 0; i < 100; i++)
            {
                var expected = GenerateData();

                var encoded = ZBase32.Encode(expected, FormatOptions.None);

                Assert.IsFalse(encoded.Contains('-'));

                var actual = ZBase32.Decode(encoded);

                Assert.IsTrue(Compare(expected, actual));
            }
        }

        [TestMethod]
        public void When_validating_invalid_characters()
        {
            for (int i = 0; i < 100; i++)
            {
                var bytes = GenerateData();

                var encoded = ZBase32.Encode(bytes);

                var index = GetRandomIndex(encoded.Length);

                var invalidChar = GenerateInvalidCharacter();

                var chars = encoded.ToCharArray();

                chars[index] = invalidChar;

                encoded = new string(chars);

                Assert.ThrowsException<ArgumentException>(() => ZBase32.Validate(encoded));
            }
        }

        [TestMethod]
        public void When_encoding_empty_array()
        {
            var bytes = Array.Empty<byte>();

            var encoded = ZBase32.Encode(bytes);

            Assert.AreEqual(encoded, string.Empty);
        }

        [TestMethod]
        public void When_decoding_empty_string()
        {
            var encoded = string.Empty;

            var bytes = ZBase32.Decode(encoded);

            Assert.IsTrue(Compare(bytes, Array.Empty<byte>()));
        }

        [TestMethod]
        public void When_decoding_random_data_with_transcription_errors()
        {
            for (int i = 0; i < 100; i++)
            {
                var expected = GenerateData();

                var encoded = ZBase32.Encode(expected);

                encoded = encoded
                    .Replace('o', '0')
                    .Replace('u', 'v')
                    .Replace('z', '2')
                    .Replace('1', encoded.IndexOf('1') % 2 == 0 ? 'l' : '|');

                var actual = ZBase32.Decode(encoded);

                Assert.IsTrue(Compare(expected, actual));
            }
        }

        static byte[] GenerateData()
        {
            using var rng = new RNGCryptoServiceProvider();

            var length = new byte[1];

            rng.GetNonZeroBytes(length);

            var bytes = new byte[length[0]];

            rng.GetBytes(bytes);

            return bytes;
        }

        static int GetRandomIndex(int length)
        {
            using var rng = new RNGCryptoServiceProvider();

            var buffer = new byte[1];

            rng.GetBytes(buffer);

            return buffer[0] % length;
        }

        static char GenerateInvalidCharacter()
        {
            const string sample = ",./;'[]\\`= \t\r\n~!@#$%^&*()_+:\"<>?";

            var index = GetRandomIndex(sample.Length);

            return sample[index];
        }

        static bool Compare(byte[] b1, byte[] b2)
        {
            if (b1 == null || b2 == null)
            {
                return false;
            }

            if (b1.Length != b2.Length)
            {
                return false;
            }

            for (int i = 0; i < b1.Length; i++)
            {
                if (b1[i] != b2[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
