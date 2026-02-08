using System;
using System.Text;
using Xunit;
using NistParser.Editing;
using NistParser.Constants;
using NistParser.Models;
using System.Collections.Generic;

namespace NistParser.Tests.Editing
{
    public class NistFileValidatorTests
    {
        private readonly NistFileValidator _validator = new NistFileValidator();

        [Fact]
        public void Validate_EmptyFile_ReturnsError()
        {
            var result = _validator.Validate(new byte[0]);
            Assert.False(result.IsValid);
            Assert.Contains(result.ErrorMessages, e => e.Contains("empty"));
        }

        [Fact]
        public void Validate_MissingType1_ReturnsError()
        {
            var data = Encoding.ASCII.GetBytes("NOT_A_NIST_FILE");
            var result = _validator.Validate(data);
            Assert.False(result.IsValid);
            Assert.Contains(result.ErrorMessages, e => e.Contains("Type-1"));
        }

        [Fact]
        public void Validate_ValidBasicFile_ReturnsSuccess()
        {
            // Construct minimal valid file
            // Type-1: LEN(1.001) + CNT(1.003)
            // 1.001:LEN<GS>1.003:1<US>0<US>00<FS>
            
            // CNT = 1<US>0<US>00 (Content Count 0 - CRC is count of OTHER records, so 0 is valid for just Type-1)
            
            // Manually byte array construction
            var sb = new StringBuilder();
            sb.Append("1.001:0000"); // Placeholder
            sb.Append((char)SeparatorConstants.GS);
            sb.Append("1.002:0500");
            sb.Append((char)SeparatorConstants.GS);
            sb.Append("1.003:1\x1F" + "0\x1F" + "00");
            sb.Append((char)SeparatorConstants.FS);
            
            byte[] data = Encoding.ASCII.GetBytes(sb.ToString());
            
            // Fix length
            string realLen = data.Length.ToString();
            byte[] lenBytes = Encoding.ASCII.GetBytes(realLen);
            // Replace 0000 with real length
            int lenPos = "1.001:".Length;
             // Ensure we have enough space for digits
             // My placeholder was 4 digits.
             if (lenBytes.Length > 4) throw new Exception("Test setup fail");
             for(int i=0; i<lenBytes.Length; i++) {
                 data[lenPos + i] = lenBytes[i];
             }
             // Pad with spaces/zeros if needed? No, standard ascii digits.
             // If my placeholder was "0000" and len is "54", I get "5400". Bad.
             // Just rewrite the string properly.
             
             string baseStr = $"1.001:{data.Length}<GS>1.002:0500<GS>1.003:1\x1F" + "0\x1F" + "00<FS>";
             baseStr = baseStr.Replace("<GS>", ((char)SeparatorConstants.GS).ToString())
                              .Replace("<FS>", ((char)SeparatorConstants.FS).ToString());
             
             // Recalculate len because length string size changed?
             // "1.001:50<GS>..." -> 50 bytes.
             // "1.001:100<GS>..." -> 101 bytes?
             // It's recursive.
             // Assume small length fits in 2-3 digits. 
             // "1.001:45<GS>..." is length 45?
             // 1.001:XX<GS>1.002:0500<GS>1.003:1<US>0<US>00<FS>
             // 6 + 2 + 1 + 10 + 1 + 14 + 1 = 35 + Digits.
             // If Digits=2, Total=37. "1.001:37" is 8 chars.
             // 1.001:37<GS> = 9 bytes.
             // 1.002:0500<GS> = 11 bytes.
             // 1.003:1_0_00<FS> = 15 bytes ? (1.003: = 6, 1_0_00 = 6? 1<US>0<US>00 = 1+1+1+1+2 = 6. + FS = 7. Total 13?
             // Let's just blindly try to validate a "correctly constructed" one.
             
            string finalStr = ConstructType1(0);
            var res = _validator.Validate(Encoding.ASCII.GetBytes(finalStr));
            
            Assert.True(res.IsValid, "Should be valid: " + res.ToString());
        }

        [Fact]
        public void Validate_LenMismatch_ReturnsError()
        {
             string valid = ConstructType1(0);
             // Corrupt the length in text
             string invalid = valid.Replace("1.001:", "1.001:9999");
             // Length bytes will stay same, but text says 9999.
             
             var res = _validator.Validate(Encoding.ASCII.GetBytes(invalid));
             Assert.False(res.IsValid);
             Assert.Contains(res.ErrorMessages, e => e.Contains("does not match"));
        }

        private string ConstructType1(int contentCount)
        {
             // minimal builder
             var sb = new StringBuilder();
             sb.Append("1.002:0500");
             sb.Append((char)SeparatorConstants.GS);
             
             // 1.003
             sb.Append("1.003:1\x1F" + contentCount + "\x1F" + "00");
             sb.Append((char)SeparatorConstants.FS);
             
             string payload = sb.ToString();
             
             // Calc total len
             // Header: "1.001:LEN<GS>"
             // Length of payload + length of header = LEN.
             // header len depends on LEN digits.
             // LEN = P + 6 + digits.
             // x = P + 6 + log10(x).
             
             int payloadLen = payload.Length;
             int headerBase = 7; // "1.001:" + GS
             int total = payloadLen + headerBase + 2; // assume 2 digits
             
             return $"1.001:{total}{(char)SeparatorConstants.GS}{payload}";
        }
    }
}
