using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MergePhotos
{    
    class FileLengthHash : IEquatable<FileLengthHash>
    {
        long fileLength;
        int hashCode;
        string path;

        public FileLengthHash(string path)
        {
            // start with a weak hash code for speed.
            fileLength = new FileInfo(path).Length;
            hashCode = (int)fileLength;
            this.path = path;
        }

        // get or create the FileBlockHash for the file.
        public FileBlockHash InnerHash { get; set; }

        public long FileLength { get { return this.fileLength; } }

        public FileLengthHash(string path, int hashCode)
        {
            this.path = path;
            this.hashCode = hashCode;
        }

        public string Path { get { return this.path; } }

        public override int GetHashCode()
        {
            return hashCode;
        }

        public override bool Equals(object obj)
        {
            FileLengthHash other = obj as FileLengthHash;
            if (other == null)
            {
                return false;
            }
            return this.HashEquals(other);
        }

        public bool Equals(FileLengthHash other)
        {
            return this.HashEquals(other);
        }

        internal bool HashEquals(FileLengthHash other)
        {
            if (hashCode != other.hashCode)
            {
                return false;
            }
            return true;
        }

    }

    public class BlockHash
    {
        protected string path;
        protected byte[] hash;
        protected int hashCode;
        protected int hashBlockLength;

        public BlockHash(string file)
        {
            this.path = file;
            byte[] buffer = File.ReadAllBytes(file);
            this.hashBlockLength = buffer.Length;
            this.hash = ComputeSha256Hash(buffer);
            this.hashCode = ComputeHashCode();
        }

        public BlockHash(string file, int blockLength)
        {
            this.path = file;
            this.hashBlockLength = blockLength;
            this.hash = ComputeSha256Hash(file, blockLength);
            this.hashCode = ComputeHashCode();
        }

        public string Path { get { return this.path; } }

        public override bool Equals(object obj)
        {
            BlockHash other = obj as BlockHash;
            if (other == null)
            {
                return false;
            }
            return this.Equals(other);
        }

        public bool Equals(BlockHash other)
        {
            return this.hashCode == other.hashCode && StructuralComparisons.StructuralEqualityComparer.Equals(hash, other.hash);
        }

        public override int GetHashCode()
        {
            return this.hashCode;
        }

        private byte[] ComputeSha256Hash(string file, int blockLength)
        {
            byte[] hashBuffer = new byte[blockLength];
            using (Stream fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                int len = fs.Read(hashBuffer, 0, hashBuffer.Length);
                return ComputeSha256Hash(hashBuffer);
            }
        }

        private byte[] ComputeSha256Hash(byte[] buffer)
        {
            HashAlgorithm hasher = HashAlgorithm.Create("SHA256");
            return hasher.ComputeHash(buffer);
        }

        private int ComputeHashCode()
        {
            int hashCode = 0;
            unchecked
            {
                foreach (byte b in hash)
                {
                    hashCode ^= ~b;
                    hashCode <<= 1;
                }
            }
            return hashCode;
        }


    }

    public class FileBlockHash : BlockHash
    {
        public FileBlockHash(string path, int hashBlockLength = 64000) : base(path, hashBlockLength)
        {
        }

        // get or create the entire file hash for this file.
        public EntireFileHash InnerHash { get; set; }

    }

    public class EntireFileHash : BlockHash
    {
        public EntireFileHash(string path) : base(path)
        {
        }

        public bool FileEquals(EntireFileHash other)
        {
            HashAlgorithm hasher = HashAlgorithm.Create("SHA256");

            using (Stream fs = new FileStream(this.path, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                using (Stream fs2 = new FileStream(other.path, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    return StreamEquals(fs, fs2);
                }
            }
        }

        static bool StreamEquals(Stream s1, Stream s2)
        {
            byte[] buffer1 = new byte[65536];
            byte[] buffer2 = new byte[65536];

            while (true)
            {
                int read = s1.Read(buffer1, 0, buffer1.Length);
                int read2 = s2.Read(buffer2, 0, buffer2.Length);
                if (read != read2)
                {
                    return false;
                }
                if (read == 0)
                {
                    break;
                }
                unchecked
                {
                    for (int i = 0; i < read; i++)
                    {
                        if (buffer1[i] != buffer2[i])
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

    }
}
