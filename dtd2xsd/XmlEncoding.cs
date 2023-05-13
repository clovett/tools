using System;
using System.Text;
using System.IO;

namespace dtd2xsd {

  internal class XmlStream : TextReader {
    Stream stm;
    byte[] rawBuffer;
    int rawPos;
    int rawUsed;
    Decoder decoder;
    char[] buffer;
    int used;
    int pos;

    private const int BUFSIZE   = 16384;

    public XmlStream(Stream stm) {
      this.stm = stm;    
      rawBuffer = new Byte[BUFSIZE];
      rawUsed = stm.Read(rawBuffer, 0, rawBuffer.Length);
      decoder = AutoDetectEncoding(rawBuffer, ref rawPos, rawUsed);
      int i = rawPos;
      buffer = new char[BUFSIZE];
      DecodeBlock();
      Decoder d = SniffEncoding();
      if (d != null) {
        // re-decode the block in the new encoding.
        decoder = d;
        rawPos = i;
        pos = 0;
        DecodeBlock();
        SniffEncoding(); // and consume the xml declaration again
      }
    }

    internal void DecodeBlock() {
      // shift current chars to beginning.
      if (pos>0) {
        if (pos<used) {
          System.Array.Copy(buffer, pos, buffer, 0, used-pos);
        }
        used -= pos;
        pos = 0;
      }
      int len = decoder.GetCharCount(rawBuffer, rawPos, rawUsed-rawPos);
      int available = buffer.Length - used;
      if (available < len) {
        char[] newbuf = new char[buffer.Length+len];
        System.Array.Copy(buffer, pos, newbuf, 0, used-pos);
        buffer = newbuf;
      }
      used = pos + decoder.GetChars(rawBuffer, rawPos, rawUsed-rawPos, buffer, pos);
      rawPos = rawUsed; // consumed the whole buffer!
    }

    internal static Decoder AutoDetectEncoding(byte[] buffer, ref int index, int length) {
      if (4 <= (length - index)) {
        uint w = (uint)buffer[index + 0] << 24 | (uint)buffer[index + 1] << 16 | (uint)buffer[index + 2] << 8 | (uint)buffer[index + 3];
        // see if it's a 4-byte encoding
        switch(w) {
          case 0xfefffeff:  index += 4; return new Ucs4DecoderBigEngian();
          case 0xfffefffe:  index += 4; return new Ucs4DecoderLittleEndian();
          case 0x3c000000:  goto case 0xfefffeff;
          case 0x0000003c:  goto case 0xfffefffe;
        }
        w >>= 8;
        if (w == 0xefbbbf) {
          index += 3;
          return Encoding.UTF8.GetDecoder();
        }
        w >>= 8;
        switch(w) {
          case 0xfeff:  index += 2; return UnicodeEncoding.BigEndianUnicode.GetDecoder();
          case 0xfffe:  index += 2; return new UnicodeEncoding(false,false).GetDecoder();
          case 0x3c00:  goto case 0xfeff;
          case 0x003c:  goto case 0xfffe;        
        }
      }
      return Encoding.UTF8.GetDecoder(); // default is UTF8
    }

    internal Decoder SniffEncoding() {
      if (used < 20)  // smallest possible <?xml declaration.
        return null;

      Decoder result = null;

      if (buffer[pos] == '<' && buffer[pos+1] == '?' &&
        buffer[pos+2] == 'x' && buffer[pos+3] == 'm' &&
        buffer[pos+4] == 'l') {
        pos += 5;
        SkipWhitespace();
        if (pos<used && buffer[pos] == 'v') { // version attribute
          ParseAttribute();
          SkipWhitespace();
        }
        if (pos<used && buffer[pos] == 'e') { // encoding attribute
          string value = ParseAttribute();
          if (value != null) {
            result= Encoding.GetEncoding(value).GetDecoder();
          }
        }
        SkipTo('>');
        if (pos < used && buffer[pos] == '>') {
          pos++;
        }
      }
      return result;
    }

    internal void SkipWhitespace() {
      char ch = buffer[pos];
      while (pos < used && (ch == ' ' || ch == '\r' || ch == '\n')) {
        ch = buffer[++pos];
      }        
    }

    internal void SkipTo(char what) {
      char ch = buffer[pos];
      while (pos < used && (ch != what)) {
        ch = buffer[++pos];
      }        
    }

    internal string ParseAttribute() {
      SkipTo('=');
      if (pos<used) {
        pos++;
        SkipWhitespace();
        if (pos < used) {
          char quote = buffer[pos];
          pos++;
          int start = pos;
          SkipTo(quote);
          if (pos < used) {
            string result = new string(buffer, start, pos-start);
            pos++;
            return result;
          }
        }
      }
      return null;
    }
   
    public override int Peek() {
      if (pos<used) return buffer[pos];
      return -1;
    }

    public override int Read() {
      if (pos==used) {
        rawUsed = stm.Read(rawBuffer, 0, rawBuffer.Length);
        rawPos = 0;
        if (rawUsed == 0) return -1;
        DecodeBlock();
      }
      if (pos<used) return buffer[pos++];
      return -1;
    }

    public override int Read(char[] buffer, int start, int length) {
      if (pos==used) {
        rawUsed = stm.Read(rawBuffer, 0, rawBuffer.Length);
        rawPos = 0;
        if (rawUsed == 0) return -1;
        DecodeBlock();
      }
      if (pos<used) {
        length = Math.Min(used-pos, length);
        Array.Copy(this.buffer, pos, buffer, start, length);
        pos += length;
        return length;
      }
      return 0;
    }

    public override int ReadBlock(char[] buffer, int index, int count) {
      return Read(buffer,index,count);
    }

    public override void Close() {
      stm.Close();
    }

  }

  internal abstract class Ucs4Decoder : Decoder {

    internal byte [] temp = new byte[4];
    internal int tempBytes=0;

    public override int GetCharCount(byte[] bytes,int index,int count) {
      return (count + tempBytes)/4;
    }

    internal abstract int GetFullChars(byte[] bytes,int byteIndex,int byteCount,char[] chars,int charIndex);

    public override int GetChars(byte[] bytes,int byteIndex,int byteCount,char[] chars,int charIndex) {
      int i = tempBytes;

      if( tempBytes > 0) {
        for(; i < 4; i++) {
          temp[i] = bytes[byteIndex];
          byteIndex++;
          byteCount--;
        }
        i = 1;
        GetFullChars(temp, 0 , 4, chars, charIndex);
        charIndex++;
      }
      else i = 0;
      i = GetFullChars(bytes, byteIndex , byteCount, chars, charIndex) + i;

      int j = ( tempBytes + byteCount ) % 4;
      byteCount += byteIndex;
      byteIndex =  byteCount - j;
      tempBytes = 0;

      if(byteIndex >= 0)
        for(; byteIndex < byteCount; byteIndex++){
          temp[tempBytes] = bytes[byteIndex];
          tempBytes++;
        }
      return i;
    }

    internal char UnicodeToUTF16( UInt32 code) {
      byte lowerByte, higherByte;
      lowerByte = (byte) (0xD7C0 + (code >> 10));
      higherByte = (byte) (0xDC00 | code & 0x3ff);
      return ((char) ((higherByte << 8) | lowerByte));
    }
  }


  internal class Ucs4DecoderBigEngian : Ucs4Decoder  {

    internal override int GetFullChars(byte[] bytes,int byteIndex,int byteCount,char[] chars,int charIndex) {
      UInt32 code;
      int i, j;
      byteCount += byteIndex;
      for (i = byteIndex, j = charIndex; i+3 < byteCount;) {
        code =  (UInt32) (((bytes[i+3])<<24) | (bytes[i+2]<<16) | (bytes[i+1]<<8) | (bytes[i]));
        if (code > 0x10FFFF) {
          throw new Exception("Invalid character 0x" + code.ToString("x") + " in encoding");
        }
        else if (code > 0xFFFF) {
          chars[j] = UnicodeToUTF16(code);
          j++;
        }
        else {
          if (code >= 0xD800 && code <= 0xDFFF) {
            throw new Exception("Invalid character 0x" + code.ToString("x") + " in encoding");
          }
          else {
            chars[j] = (char) code;
          }
        }
        j++;
        i += 4;
      }
      return j - charIndex;
    }
  };

  internal class Ucs4DecoderLittleEndian : Ucs4Decoder  {

    internal override int GetFullChars(byte[] bytes,int byteIndex,int byteCount,char[] chars,int charIndex) {
      UInt32 code;
      int i,j;
      byteCount += byteIndex;
      for (i = byteIndex, j = charIndex; i+3 < byteCount;) {
        code = (UInt32) (((bytes[i])<<24) | (bytes[i+1]<<16) | (bytes[i+2]<<8) | (bytes[i+3]));
        if (code > 0x10FFFF) {
          throw new Exception("Invalid character 0x" + code.ToString("x") + " in encoding");
        }
        else if (code > 0xFFFF) {
          chars[j] = UnicodeToUTF16(code);
          j++;
        }
        else {
          if (code >= 0xD800 && code <= 0xDFFF) {
            throw new Exception("Invalid character 0x" + code.ToString("x") + " in encoding");
          }
          else {
            chars[j] = (char) code;
          }
        }
        j++;
        i += 4;
      }
      return j - charIndex;
    }
  }

}
