//------------------------------------------------------------------------------
// <copyright file="XmlCharType.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace dtd2xsd {

  internal sealed class XmlCharType {
    // Whitespace chars -- Section 2.3 [3]
    // Letters -- Appendix B [84]
    // Starting name characters -- Section 2.3 [5]
    // Name characters -- Section 2.3 [4]
    // Character data characters -- Section 2.2 [2]
    // PubidChar ::=  #x20 | #xD | #xA | [a-zA-Z0-9] | [-'()+,./:=?;!*#@$_%] Section 2.3 of spec
    public const int FWHITESPACE    = 1;
    public const int FLETTER        = 2;
    public const int FSTARTNAME     = 4;
    public const int FNAME          = 8;
    public const int FCHARDATA      = 16;
    public const int FPUBLICID      = 32;

    public const char MAXWCHAR      = (char)0xffff;
    public const int  MAXCHARDATA   = 0x10ffff;

    internal const string s_Whitespace =
      "\u0009\u000a\u000d\u000d\u0020\u0020";

    const string s_Letter =
      "\u0041\u005a\u0061\u007a\u00c0\u00d6\u00d8\u00f6" +
      "\u00f8\u0131\u0134\u013e\u0141\u0148\u014a\u017e" +
      "\u0180\u01c3\u01cd\u01f0\u01f4\u01f5\u01fa\u0217" +
      "\u0250\u02a8\u02bb\u02c1\u0386\u0386\u0388\u038a" +
      "\u038c\u038c\u038e\u03a1\u03a3\u03ce\u03d0\u03d6" +
      "\u03da\u03da\u03dc\u03dc\u03de\u03de\u03e0\u03e0" +
      "\u03e2\u03f3\u0401\u040c\u040e\u044f\u0451\u045c" +
      "\u045e\u0481\u0490\u04c4\u04c7\u04c8\u04cb\u04cc" +
      "\u04d0\u04eb\u04ee\u04f5\u04f8\u04f9\u0531\u0556" +
      "\u0559\u0559\u0561\u0586\u05d0\u05ea\u05f0\u05f2" +
      "\u0621\u063a\u0641\u064a\u0671\u06b7\u06ba\u06be" +
      "\u06c0\u06ce\u06d0\u06d3\u06d5\u06d5\u06e5\u06e6" +
      "\u0905\u0939\u093d\u093d\u0958\u0961\u0985\u098c" +
      "\u098f\u0990\u0993\u09a8\u09aa\u09b0\u09b2\u09b2" +
      "\u09b6\u09b9\u09dc\u09dd\u09df\u09e1\u09f0\u09f1" +
      "\u0a05\u0a0a\u0a0f\u0a10\u0a13\u0a28\u0a2a\u0a30" +
      "\u0a32\u0a33\u0a35\u0a36\u0a38\u0a39\u0a59\u0a5c" +
      "\u0a5e\u0a5e\u0a72\u0a74\u0a85\u0a8b\u0a8d\u0a8d" +
      "\u0a8f\u0a91\u0a93\u0aa8\u0aaa\u0ab0\u0ab2\u0ab3" +
      "\u0ab5\u0ab9\u0abd\u0abd\u0ae0\u0ae0\u0b05\u0b0c" +
      "\u0b0f\u0b10\u0b13\u0b28\u0b2a\u0b30\u0b32\u0b33" +
      "\u0b36\u0b39\u0b3d\u0b3d\u0b5c\u0b5d\u0b5f\u0b61" +
      "\u0b85\u0b8a\u0b8e\u0b90\u0b92\u0b95\u0b99\u0b9a" +
      "\u0b9c\u0b9c\u0b9e\u0b9f\u0ba3\u0ba4\u0ba8\u0baa" +
      "\u0bae\u0bb5\u0bb7\u0bb9\u0c05\u0c0c\u0c0e\u0c10" +
      "\u0c12\u0c28\u0c2a\u0c33\u0c35\u0c39\u0c60\u0c61" +
      "\u0c85\u0c8c\u0c8e\u0c90\u0c92\u0ca8\u0caa\u0cb3" +
      "\u0cb5\u0cb9\u0cde\u0cde\u0ce0\u0ce1\u0d05\u0d0c" +
      "\u0d0e\u0d10\u0d12\u0d28\u0d2a\u0d39\u0d60\u0d61" +
      "\u0e01\u0e2e\u0e30\u0e30\u0e32\u0e33\u0e40\u0e45" +
      "\u0e81\u0e82\u0e84\u0e84\u0e87\u0e88\u0e8a\u0e8a" +
      "\u0e8d\u0e8d\u0e94\u0e97\u0e99\u0e9f\u0ea1\u0ea3" +
      "\u0ea5\u0ea5\u0ea7\u0ea7\u0eaa\u0eab\u0ead\u0eae" +
      "\u0eb0\u0eb0\u0eb2\u0eb3\u0ebd\u0ebd\u0ec0\u0ec4" +
      "\u0f40\u0f47\u0f49\u0f69\u10a0\u10c5\u10d0\u10f6" +
      "\u1100\u1100\u1102\u1103\u1105\u1107\u1109\u1109" +
      "\u110b\u110c\u110e\u1112\u113c\u113c\u113e\u113e" +
      "\u1140\u1140\u114c\u114c\u114e\u114e\u1150\u1150" +
      "\u1154\u1155\u1159\u1159\u115f\u1161\u1163\u1163" +
      "\u1165\u1165\u1167\u1167\u1169\u1169\u116d\u116e" +
      "\u1172\u1173\u1175\u1175\u119e\u119e\u11a8\u11a8" +
      "\u11ab\u11ab\u11ae\u11af\u11b7\u11b8\u11ba\u11ba" +
      "\u11bc\u11c2\u11eb\u11eb\u11f0\u11f0\u11f9\u11f9" +
      "\u1e00\u1e9b\u1ea0\u1ef9\u1f00\u1f15\u1f18\u1f1d" +
      "\u1f20\u1f45\u1f48\u1f4d\u1f50\u1f57\u1f59\u1f59" +
      "\u1f5b\u1f5b\u1f5d\u1f5d\u1f5f\u1f7d\u1f80\u1fb4" +
      "\u1fb6\u1fbc\u1fbe\u1fbe\u1fc2\u1fc4\u1fc6\u1fcc" +
      "\u1fd0\u1fd3\u1fd6\u1fdb\u1fe0\u1fec\u1ff2\u1ff4" +
      "\u1ff6\u1ffc\u2126\u2126\u212a\u212b\u212e\u212e" +
      "\u2180\u2182\u3007\u3007\u3021\u3029\u3041\u3094" +
      "\u30a1\u30fa\u3105\u312c\u4e00\u9fa5\uac00\ud7a3";

    const string s_StartName =
      "\u003a\u003a\u0041\u005a\u005f\u005f\u0061\u007a" +
      "\u00c0\u00d6\u00d8\u00f6\u00f8\u0131\u0134\u013e" +
      "\u0141\u0148\u014a\u017e\u0180\u01c3\u01cd\u01f0" +
      "\u01f4\u01f5\u01fa\u0217\u0250\u02a8\u02bb\u02c1" +
      "\u0386\u0386\u0388\u038a\u038c\u038c\u038e\u03a1" +
      "\u03a3\u03ce\u03d0\u03d6\u03da\u03da\u03dc\u03dc" +
      "\u03de\u03de\u03e0\u03e0\u03e2\u03f3\u0401\u040c" +
      "\u040e\u044f\u0451\u045c\u045e\u0481\u0490\u04c4" +
      "\u04c7\u04c8\u04cb\u04cc\u04d0\u04eb\u04ee\u04f5" +
      "\u04f8\u04f9\u0531\u0556\u0559\u0559\u0561\u0586" +
      "\u05d0\u05ea\u05f0\u05f2\u0621\u063a\u0641\u064a" +
      "\u0671\u06b7\u06ba\u06be\u06c0\u06ce\u06d0\u06d3" +
      "\u06d5\u06d5\u06e5\u06e6\u0905\u0939\u093d\u093d" +
      "\u0958\u0961\u0985\u098c\u098f\u0990\u0993\u09a8" +
      "\u09aa\u09b0\u09b2\u09b2\u09b6\u09b9\u09dc\u09dd" +
      "\u09df\u09e1\u09f0\u09f1\u0a05\u0a0a\u0a0f\u0a10" +
      "\u0a13\u0a28\u0a2a\u0a30\u0a32\u0a33\u0a35\u0a36" +
      "\u0a38\u0a39\u0a59\u0a5c\u0a5e\u0a5e\u0a72\u0a74" +
      "\u0a85\u0a8b\u0a8d\u0a8d\u0a8f\u0a91\u0a93\u0aa8" +
      "\u0aaa\u0ab0\u0ab2\u0ab3\u0ab5\u0ab9\u0abd\u0abd" +
      "\u0ae0\u0ae0\u0b05\u0b0c\u0b0f\u0b10\u0b13\u0b28" +
      "\u0b2a\u0b30\u0b32\u0b33\u0b36\u0b39\u0b3d\u0b3d" +
      "\u0b5c\u0b5d\u0b5f\u0b61\u0b85\u0b8a\u0b8e\u0b90" +
      "\u0b92\u0b95\u0b99\u0b9a\u0b9c\u0b9c\u0b9e\u0b9f" +
      "\u0ba3\u0ba4\u0ba8\u0baa\u0bae\u0bb5\u0bb7\u0bb9" +
      "\u0c05\u0c0c\u0c0e\u0c10\u0c12\u0c28\u0c2a\u0c33" +
      "\u0c35\u0c39\u0c60\u0c61\u0c85\u0c8c\u0c8e\u0c90" +
      "\u0c92\u0ca8\u0caa\u0cb3\u0cb5\u0cb9\u0cde\u0cde" +
      "\u0ce0\u0ce1\u0d05\u0d0c\u0d0e\u0d10\u0d12\u0d28" +
      "\u0d2a\u0d39\u0d60\u0d61\u0e01\u0e2e\u0e30\u0e30" +
      "\u0e32\u0e33\u0e40\u0e45\u0e81\u0e82\u0e84\u0e84" +
      "\u0e87\u0e88\u0e8a\u0e8a\u0e8d\u0e8d\u0e94\u0e97" +
      "\u0e99\u0e9f\u0ea1\u0ea3\u0ea5\u0ea5\u0ea7\u0ea7" +
      "\u0eaa\u0eab\u0ead\u0eae\u0eb0\u0eb0\u0eb2\u0eb3" +
      "\u0ebd\u0ebd\u0ec0\u0ec4\u0f40\u0f47\u0f49\u0f69" +
      "\u10a0\u10c5\u10d0\u10f6\u1100\u1100\u1102\u1103" +
      "\u1105\u1107\u1109\u1109\u110b\u110c\u110e\u1112" +
      "\u113c\u113c\u113e\u113e\u1140\u1140\u114c\u114c" +
      "\u114e\u114e\u1150\u1150\u1154\u1155\u1159\u1159" +
      "\u115f\u1161\u1163\u1163\u1165\u1165\u1167\u1167" +
      "\u1169\u1169\u116d\u116e\u1172\u1173\u1175\u1175" +
      "\u119e\u119e\u11a8\u11a8\u11ab\u11ab\u11ae\u11af" +
      "\u11b7\u11b8\u11ba\u11ba\u11bc\u11c2\u11eb\u11eb" +
      "\u11f0\u11f0\u11f9\u11f9\u1e00\u1e9b\u1ea0\u1ef9" +
      "\u1f00\u1f15\u1f18\u1f1d\u1f20\u1f45\u1f48\u1f4d" +
      "\u1f50\u1f57\u1f59\u1f59\u1f5b\u1f5b\u1f5d\u1f5d" +
      "\u1f5f\u1f7d\u1f80\u1fb4\u1fb6\u1fbc\u1fbe\u1fbe" +
      "\u1fc2\u1fc4\u1fc6\u1fcc\u1fd0\u1fd3\u1fd6\u1fdb" +
      "\u1fe0\u1fec\u1ff2\u1ff4\u1ff6\u1ffc\u2126\u2126" +
      "\u212a\u212b\u212e\u212e\u2180\u2182\u3007\u3007" +
      "\u3021\u3029\u3041\u3094\u30a1\u30fa\u3105\u312c" +
      "\u4e00\u9fa5\uac00\ud7a3";

    const string s_Name =
      "\u002d\u002e\u0030\u003a\u0041\u005a\u005f\u005f" +
      "\u0061\u007a\u00b7\u00b7\u00c0\u00d6\u00d8\u00f6" +
      "\u00f8\u0131\u0134\u013e\u0141\u0148\u014a\u017e" +
      "\u0180\u01c3\u01cd\u01f0\u01f4\u01f5\u01fa\u0217" +
      "\u0250\u02a8\u02bb\u02c1\u02d0\u02d1\u0300\u0345" +
      "\u0360\u0361\u0386\u038a\u038c\u038c\u038e\u03a1" +
      "\u03a3\u03ce\u03d0\u03d6\u03da\u03da\u03dc\u03dc" +
      "\u03de\u03de\u03e0\u03e0\u03e2\u03f3\u0401\u040c" +
      "\u040e\u044f\u0451\u045c\u045e\u0481\u0483\u0486" +
      "\u0490\u04c4\u04c7\u04c8\u04cb\u04cc\u04d0\u04eb" +
      "\u04ee\u04f5\u04f8\u04f9\u0531\u0556\u0559\u0559" +
      "\u0561\u0586\u0591\u05a1\u05a3\u05b9\u05bb\u05bd" +
      "\u05bf\u05bf\u05c1\u05c2\u05c4\u05c4\u05d0\u05ea" +
      "\u05f0\u05f2\u0621\u063a\u0640\u0652\u0660\u0669" +
      "\u0670\u06b7\u06ba\u06be\u06c0\u06ce\u06d0\u06d3" +
      "\u06d5\u06e8\u06ea\u06ed\u06f0\u06f9\u0901\u0903" +
      "\u0905\u0939\u093c\u094d\u0951\u0954\u0958\u0963" +
      "\u0966\u096f\u0981\u0983\u0985\u098c\u098f\u0990" +
      "\u0993\u09a8\u09aa\u09b0\u09b2\u09b2\u09b6\u09b9" +
      "\u09bc\u09bc\u09be\u09c4\u09c7\u09c8\u09cb\u09cd" +
      "\u09d7\u09d7\u09dc\u09dd\u09df\u09e3\u09e6\u09f1" +
      "\u0a02\u0a02\u0a05\u0a0a\u0a0f\u0a10\u0a13\u0a28" +
      "\u0a2a\u0a30\u0a32\u0a33\u0a35\u0a36\u0a38\u0a39" +
      "\u0a3c\u0a3c\u0a3e\u0a42\u0a47\u0a48\u0a4b\u0a4d" +
      "\u0a59\u0a5c\u0a5e\u0a5e\u0a66\u0a74\u0a81\u0a83" +
      "\u0a85\u0a8b\u0a8d\u0a8d\u0a8f\u0a91\u0a93\u0aa8" +
      "\u0aaa\u0ab0\u0ab2\u0ab3\u0ab5\u0ab9\u0abc\u0ac5" +
      "\u0ac7\u0ac9\u0acb\u0acd\u0ae0\u0ae0\u0ae6\u0aef" +
      "\u0b01\u0b03\u0b05\u0b0c\u0b0f\u0b10\u0b13\u0b28" +
      "\u0b2a\u0b30\u0b32\u0b33\u0b36\u0b39\u0b3c\u0b43" +
      "\u0b47\u0b48\u0b4b\u0b4d\u0b56\u0b57\u0b5c\u0b5d" +
      "\u0b5f\u0b61\u0b66\u0b6f\u0b82\u0b83\u0b85\u0b8a" +
      "\u0b8e\u0b90\u0b92\u0b95\u0b99\u0b9a\u0b9c\u0b9c" +
      "\u0b9e\u0b9f\u0ba3\u0ba4\u0ba8\u0baa\u0bae\u0bb5" +
      "\u0bb7\u0bb9\u0bbe\u0bc2\u0bc6\u0bc8\u0bca\u0bcd" +
      "\u0bd7\u0bd7\u0be7\u0bef\u0c01\u0c03\u0c05\u0c0c" +
      "\u0c0e\u0c10\u0c12\u0c28\u0c2a\u0c33\u0c35\u0c39" +
      "\u0c3e\u0c44\u0c46\u0c48\u0c4a\u0c4d\u0c55\u0c56" +
      "\u0c60\u0c61\u0c66\u0c6f\u0c82\u0c83\u0c85\u0c8c" +
      "\u0c8e\u0c90\u0c92\u0ca8\u0caa\u0cb3\u0cb5\u0cb9" +
      "\u0cbe\u0cc4\u0cc6\u0cc8\u0cca\u0ccd\u0cd5\u0cd6" +
      "\u0cde\u0cde\u0ce0\u0ce1\u0ce6\u0cef\u0d02\u0d03" +
      "\u0d05\u0d0c\u0d0e\u0d10\u0d12\u0d28\u0d2a\u0d39" +
      "\u0d3e\u0d43\u0d46\u0d48\u0d4a\u0d4d\u0d57\u0d57" +
      "\u0d60\u0d61\u0d66\u0d6f\u0e01\u0e2e\u0e30\u0e3a" +
      "\u0e40\u0e4e\u0e50\u0e59\u0e81\u0e82\u0e84\u0e84" +
      "\u0e87\u0e88\u0e8a\u0e8a\u0e8d\u0e8d\u0e94\u0e97" +
      "\u0e99\u0e9f\u0ea1\u0ea3\u0ea5\u0ea5\u0ea7\u0ea7" +
      "\u0eaa\u0eab\u0ead\u0eae\u0eb0\u0eb9\u0ebb\u0ebd" +
      "\u0ec0\u0ec4\u0ec6\u0ec6\u0ec8\u0ecd\u0ed0\u0ed9" +
      "\u0f18\u0f19\u0f20\u0f29\u0f35\u0f35\u0f37\u0f37" +
      "\u0f39\u0f39\u0f3e\u0f47\u0f49\u0f69\u0f71\u0f84" +
      "\u0f86\u0f8b\u0f90\u0f95\u0f97\u0f97\u0f99\u0fad" +
      "\u0fb1\u0fb7\u0fb9\u0fb9\u10a0\u10c5\u10d0\u10f6" +
      "\u1100\u1100\u1102\u1103\u1105\u1107\u1109\u1109" +
      "\u110b\u110c\u110e\u1112\u113c\u113c\u113e\u113e" +
      "\u1140\u1140\u114c\u114c\u114e\u114e\u1150\u1150" +
      "\u1154\u1155\u1159\u1159\u115f\u1161\u1163\u1163" +
      "\u1165\u1165\u1167\u1167\u1169\u1169\u116d\u116e" +
      "\u1172\u1173\u1175\u1175\u119e\u119e\u11a8\u11a8" +
      "\u11ab\u11ab\u11ae\u11af\u11b7\u11b8\u11ba\u11ba" +
      "\u11bc\u11c2\u11eb\u11eb\u11f0\u11f0\u11f9\u11f9" +
      "\u1e00\u1e9b\u1ea0\u1ef9\u1f00\u1f15\u1f18\u1f1d" +
      "\u1f20\u1f45\u1f48\u1f4d\u1f50\u1f57\u1f59\u1f59" +
      "\u1f5b\u1f5b\u1f5d\u1f5d\u1f5f\u1f7d\u1f80\u1fb4" +
      "\u1fb6\u1fbc\u1fbe\u1fbe\u1fc2\u1fc4\u1fc6\u1fcc" +
      "\u1fd0\u1fd3\u1fd6\u1fdb\u1fe0\u1fec\u1ff2\u1ff4" +
      "\u1ff6\u1ffc\u20d0\u20dc\u20e1\u20e1\u2126\u2126" +
      "\u212a\u212b\u212e\u212e\u2180\u2182\u3005\u3005" +
      "\u3007\u3007\u3021\u302f\u3031\u3035\u3041\u3094" +
      "\u3099\u309a\u309d\u309e\u30a1\u30fa\u30fc\u30fe" +
      "\u3105\u312c\u4e00\u9fa5\uac00\ud7a3";

    const string s_CharData =
      "\u0009\u000a\u000d\u000d\u0020\ud7ff\udc00\udeff" +
      "\ue000\ufffd";

    const string s_PublicID =
      "\u000a\u000a\u000d\u000d\u0020\u0021\u0023\u0025" +
      "\u0027\u003b\u003d\u003d\u003f\u005a\u005f\u005f" +
      "\u0061\u007a";

    static byte [] s_CharProperties;

    static XmlCharType() {
      s_CharProperties = new byte[65536];

      SetProperties(s_Whitespace, FWHITESPACE);
      SetProperties(s_Letter,     FLETTER);
      SetProperties(s_StartName,  FSTARTNAME);
      SetProperties(s_Name,       FNAME);
      SetProperties(s_CharData,   FCHARDATA);
      SetProperties(s_PublicID,   FPUBLICID);
    }

    // Don't create any instances of this class
    private XmlCharType() {
    }

    private static void SetProperties(string ranges, byte value) {
      for (int p = 0; p < ranges.Length; p += 2) {
        for (int i = ranges[p], last = ranges[p + 1]; i <= last; i++) {
          byte b = s_CharProperties[i];
          s_CharProperties[i] = (byte)(b | value);
        }
      }
    }

    // methods for efficiently querying the character type.
    public static bool IsWhiteSpace(char ch) {
        int v = s_CharProperties[(int)ch];
        return (v & FWHITESPACE) != 0;
    }

    public static bool IsLetter(char ch) {
        int v = s_CharProperties[(int)ch];
        return (v & FLETTER) != 0;
    }

    public static bool IsDigit(char ch) {
      return (ch >= 0x30 && ch <= 0x39);
    }

    public static bool IsHexDigit(char ch) {
      return (ch >= 0x30 && ch <= 0x39) || (ch >= 'a' && ch <= 'f') || (ch >= 'A' && ch <= 'F');
    }

    public static bool IsCombiningChar(char ch) {
      return false;
    }

    public static bool IsExtender(char ch) {
      return (ch == 0xb7);
    }

    public static bool IsNameChar(char ch) {
      int v = s_CharProperties[(int)ch];
      return (v & FNAME) != 0;
    }

    public static bool IsStartNameChar(char ch) {
      int v = s_CharProperties[(int)ch];
      return (v & FSTARTNAME ) != 0;
    }

    public static bool IsNCNameChar(char ch) {
      return IsNameChar(ch) && ch != ':';
    }

    public static bool IsStartNCNameChar(char ch) {
      return IsStartNameChar(ch) && ch != ':';
    }

    public static bool IsCharData(char ch) {
        int v = s_CharProperties[(int)ch];
        return (v & FCHARDATA) != 0;
    }

    // [13] PubidChar ::=  #x20 | #xD | #xA | [a-zA-Z0-9] | [-'()+,./:=?;!*#@$_%] Section 2.3 of spec
    public static bool IsPubidChar(char ch) {
        int v = s_CharProperties[(int)ch];
        return (v & FPUBLICID) != 0;
    }

    internal static bool IsOnlyWhitespace(string str) {
      if (str != null) {
        for (int index = 0; index < str.Length; index ++) {
          if (! IsWhiteSpace(str[index]))
            return false;
        }
      }
      return true;
    }

    public static bool IsValidName(string name) {
      if (name == null || name.Length==0) return false;
      if (! XmlCharType.IsStartNameChar(name[0])) return false;
      for (int i = 0, n = name.Length; i < n; i++) {
        if (! XmlCharType.IsNameChar(name[i])) return false;
      }
      return true;
    }
  }
}
