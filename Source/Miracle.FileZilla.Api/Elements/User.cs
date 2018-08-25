using System;
using System.IO;
using System.Text;

namespace Miracle.FileZilla.Api.Elements
{
    /// <summary>
    /// Class representing a FileZilla user
    /// </summary>
    public class User : Group
    {
        /// <summary>
        /// Name of user
        /// </summary>
        public string UserName { get; set; }
        /// <summary>
        /// Hashed password of user. Use User.HashPassword to set value from cleartext
        /// </summary>
        public string Password {get; private set; }
        /// <summary>
        /// Salt used to hash password.
        /// </summary>
        public string Salt { get; private set; }

        /// <summary>
        /// Default constructor (sets defaults as in FileZilla server interface)
        /// </summary>
        public User()
        {
            Enabled = TriState.Default;
        }

        /// <summary>
        /// Deserialise FileZilla binary data into object
        /// </summary>
        /// <param name="reader">Binary reader to read data from</param>
        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            UserName = reader.ReadText();
            Password = reader.ReadText();
            if (protocolVersion >= ProtocolVersions.Sha512)
            {
                Salt = reader.ReadText();
            }
        }

        /// <summary>
        /// Serialise object into FileZilla binary data
        /// </summary>
        /// <param name="writer">Binary writer to write data to</param>
        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.WriteText(UserName);
            writer.WriteText(Password);
            if (protocolVersion >= ProtocolVersions.Sha512)
            {
                writer.WriteText(Salt);
            }
        }

        /// <summary>
        /// Assign a new password to the user. If protocolVersion requires it, also generate a salt.
        /// </summary>
        /// <param name="password">New passwordinary writer to write data to</param>
        /// <param name="protocolVersion">Current FileZilla protocol version</param>
        public void AssignPassword(string password, int protocolVersion)
        {
            if (protocolVersion < ProtocolVersions.Sha512)
            {
                Password = HashPasswordMd5(password);
            }
            else
            {
                Salt = GenerateSalt();
                Password = HashPasswordSha512(password,Salt);
            }
        }

        /// <summary>
        /// Compute a correctly formatted hashed password for use in User.Password.
        /// </summary>
        /// <param name="password"></param>
        /// <param name="salt"></param>
        /// <returns></returns>
        public static string HashPasswordSha512(string password, string salt)
        {
            var x = new System.Security.Cryptography.SHA512CryptoServiceProvider();
            byte[] data = Encoding.UTF8.GetBytes(password+salt);
            data = x.ComputeHash(data);
            var ret = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
                ret.AppendFormat("{0:X2}", data[i]);
            return ret.ToString();
        }

        /// <summary>
        /// Compute a correctly formatted hashed password for use in User.Password.
        /// </summary>
        /// <param name="password"></param>
        /// <returns></returns>
        public static string HashPasswordMd5(string password)
        {
            var x = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] data = Encoding.UTF8.GetBytes(password);
            data = x.ComputeHash(data);
            var ret = new StringBuilder(32);
            for (int i = 0; i < data.Length; i++)
                ret.AppendFormat("{0:x2}", data[i]);
            return ret.ToString();
        }

        private const string ValidSaltChars = "!\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";
        private const int SaltSize = 64;

        private static string GenerateSalt() 
        {
            var random = new Random();
            var salt = new StringBuilder(SaltSize); 

            for (var i = 0; i < SaltSize; i++)
            {
                salt.Append(ValidSaltChars[random.Next(ValidSaltChars.Length)]);
            }
            return salt.ToString();
        }
    }
}