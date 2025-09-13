//+------------------------------------------------------------------+
//|                                                  CryptEncode.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright   "Copyright 2022, MetaQuotes Ltd."
#property link        "https://www.mql5.com"
#property description "Use CryptEncode to protect/convert given data with different methods."
#property script_show_inputs

#include <MQL5Book/PRTF.mqh>
#include <MQL5Book/EnumToArray.mqh>
#include <MQL5Book/ArrayUtils.mqh>

#define KEY_REQUIRED(C) ((C) == CRYPT_DES || (C) == CRYPT_AES128 || (C) == CRYPT_AES256)
#define IS_HASH(C) ((C) == CRYPT_HASH_MD5 || (C) == CRYPT_HASH_SHA1 || (C) == CRYPT_HASH_SHA256)

enum ENUM_CRYPT_METHOD_EXT
{
   _CRYPT_ALL = 0xFF,                      // Try All in a Loop
   _CRYPT_DES = CRYPT_DES,                 // DES    (key required, 7 bytes)
   _CRYPT_AES128 = CRYPT_AES128,           // AES128 (key required, 16 bytes)
   _CRYPT_AES256 = CRYPT_AES256,           // AES256 (key required, 32 bytes)
   _CRYPT_HASH_MD5 = CRYPT_HASH_MD5,       // MD5
   _CRYPT_HASH_SHA1 = CRYPT_HASH_SHA1,     // SHA1
   _CRYPT_HASH_SHA256 = CRYPT_HASH_SHA256, // SHA256
   _CRYPT_ARCH_ZIP = CRYPT_ARCH_ZIP,       // ZIP
   _CRYPT_BASE64 = CRYPT_BASE64,           // BASE64
};

enum DUMMY_KEY_LENGTH
{
   DUMMY_KEY_0 = 0,   // 0 bytes (no key)
   DUMMY_KEY_7 = 7,   // 7 bytes (sufficient for DES)
   DUMMY_KEY_16 = 16, // 16 bytes (sufficient for AES128)
   DUMMY_KEY_32 = 32, // 32 bytes (sufficient for AES256)
   DUMMY_KEY_CUSTOM,  // use CustomKey
};

input string Text = "Let's encrypt this message"; // Text (empty to process File)
input string File = "MQL5Book/clock10.htm"; // File (used only if Text is empty)
input ENUM_CRYPT_METHOD_EXT Method = _CRYPT_ALL;
input DUMMY_KEY_LENGTH GenerateKey = DUMMY_KEY_CUSTOM; // GenerateKey (length, or take from CustomKey)
input string CustomKey = "My top secret key is very strong"; // CustomKey (can be a non-printable binary, but not supported in the demo)
input bool DisableCRCinZIP = false;

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   ENUM_CRYPT_METHOD method = 0;
   int methods[];
   uchar key[] = {};        // key is empty by default: ok for hashing, zip, base64
   uchar zip[], opt[] = {1, 0, 0, 0};
   uchar data[], result[];

   if(!StringLen(Text) && !StringLen(File))
   {
      Alert("Please specify either Text or File to encode");
      return;
   }

   if(GenerateKey == DUMMY_KEY_CUSTOM)
   {
      if(StringLen(CustomKey))
      {
         PRTF(CustomKey);
         StringToCharArray(CustomKey, key, 0, -1, CP_UTF8);
         ArrayResize(key, ArraySize(key) - 1);
      }
   }
   else if(GenerateKey != DUMMY_KEY_0)
   {
      // normally the key should be a secret, hard to guess, random selected token,
      // here we generate it in straightforward manner for your easy testing
      ArrayResize(key, GenerateKey);
      for(int i = 0; i < GenerateKey; ++i) key[i] = (uchar)i;
   }

   if(ArraySize(key))
   {   
      Print("Key (bytes):");
      ByteArrayPrint(key);
   }
   else
   {
      Print("Key is not provided");
   }

   if(StringLen(Text))
   {
      PRTF(Text);
      PRTF(StringToCharArray(Text, data, 0, -1, CP_UTF8));
      ArrayResize(data, ArraySize(data) - 1);
   }
   else if(StringLen(File))
   {
      PRTF(File);
      if(PRTF(FileLoad(File, data)) <= 0)
      {
         return; // error
      }
   }
   
   // loop through all supported methods or run single time for specific 'Method'
   const int n = (Method == _CRYPT_ALL) ? EnumToArray(method, methods, 0, UCHAR_MAX) : 1;
   ResetLastError();
   for(int i = 0; i < n; ++i)
   {
      method = (ENUM_CRYPT_METHOD)((Method == _CRYPT_ALL) ? methods[i] : Method);
      Print("- ", i, " ", EnumToString(method), ", key required: ", KEY_REQUIRED(method));
      
      if(method == CRYPT_ARCH_ZIP)
      {
         // a special key is supported for CRYPT_ARCH_ZIP:
         // at least 4 bytes with 1-st nonzero is used to disable specific to MQL5
         // Adler32 checksum embedding into the compressed data,
         // needed for compatibility with standard ZIP archives,
         // storing CRCs in their own headers, not in the data
         if(DisableCRCinZIP)
         {
            ArrayCopy(zip, opt); // make dynamic copy of array (dynamic needed for ArraySwap)
         }
         ArraySwap(key, zip); // substitute the key with empty or optional
      }
      
      if(PRTF(CryptEncode(method, data, key, result)))
      {
         if(StringLen(Text))
         {
            // use Latin (Western) codepage just for consistency between users
            // with different locales on their PCs, anyway this is a binary data,
            // which should not be normally printed in the log, we do it just for the demo
            Print(CharArrayToString(result, 0, WHOLE_ARRAY, 1252));
            ByteArrayPrint(result);
            if(method != CRYPT_BASE64)
            {
               // All methods except for Base64 produce binary results,
               // so if one of these methods is exclusively selected,
               // make result printable by additional convertion into Base64
               const uchar dummy[] = {};
               uchar readable[];
               if(PRTF(CryptEncode(CRYPT_BASE64, result, dummy, readable)))
               {
                  PrintFormat("Try to decode this with CryptDecode.mq5 (%s):", EnumToString(method));
                  // we can pass only plain text into string input's,
                  // so Base64 is the only option here to accept encoded data back
                  Print("base64:'" + CharArrayToString(readable, 0, WHOLE_ARRAY, 1252) + "'");
               }
            }
         }
         else
         {
            string parts[];
            const string filename = File + "." + parts[StringSplit(EnumToString(method), '_', parts) - 1];
            if(PRTF(FileSave(filename, result)))
            {
               Print("File saved: ", filename);
               if(IS_HASH(method))
               {
                  ByteArrayPrint(result, 1000, "");
               }
            }
         }
      }
   }
}

//+------------------------------------------------------------------+
/*

CustomKey=My top secret key is very strong / ok
Key (bytes):
[00] 4D | 79 | 20 | 74 | 6F | 70 | 20 | 73 | 65 | 63 | 72 | 65 | 74 | 20 | 6B | 65 | 
[16] 79 | 20 | 69 | 73 | 20 | 76 | 65 | 72 | 79 | 20 | 73 | 74 | 72 | 6F | 6E | 67 | 
Text=Let's encrypt this message / ok
StringToCharArray(Text,data,0,-1,CP_UTF8)=26 / ok
- 0 CRYPT_BASE64, key required: false
CryptEncode(method,data,key,result)=36 / ok
TGV0J3MgZW5jcnlwdCB0aGlzIG1lc3NhZ2U=
[00] 54 | 47 | 56 | 30 | 4A | 33 | 4D | 67 | 5A | 57 | 35 | 6A | 63 | 6E | 6C | 77 | 
[16] 64 | 43 | 42 | 30 | 61 | 47 | 6C | 7A | 49 | 47 | 31 | 6C | 63 | 33 | 4E | 68 | 
[32] 5A | 32 | 55 | 3D | 
- 1 CRYPT_AES128, key required: true
CryptEncode(method,data,key,result)=32 / ok
¯T* Ë[3hß Ã/-C }¬ŠÑØN¨®Ê† ‡Ñ
[00] 01 | 0B | AF | 54 | 2A | 12 | CB | 5B | 33 | 68 | DF | 0E | C3 | 2F | 2D | 43 | 
[16] 19 | 7D | AC | 8A | D1 | 8F | D8 | 4E | A8 | AE | CA | 81 | 86 | 06 | 87 | D1 | 
CryptEncode(CRYPT_BASE64,result,dummy,readable)=44 / ok
Try to decode this with CryptDecode.mq5 (CRYPT_AES128):
base64:'AQuvVCoSy1szaN8Owy8tQxl9rIrRj9hOqK7KgYYGh9E='
- 2 CRYPT_AES256, key required: true
CryptEncode(method,data,key,result)=32 / ok
ø‘UL»ÉsëDC‰ô  ¬.K)ŒýÁ Lá¸ +< !Dï
[00] F8 | 91 | 55 | 4C | BB | C9 | 73 | EB | 44 | 43 | 89 | F4 | 06 | 13 | AC | 2E | 
[16] 4B | 29 | 8C | FD | C1 | 11 | 4C | E1 | B8 | 05 | 2B | 3C | 14 | 21 | 44 | EF | 
CryptEncode(CRYPT_BASE64,result,dummy,readable)=44 / ok
Try to decode this with CryptDecode.mq5 (CRYPT_AES256):
base64:'+JFVTLvJc+tEQ4n0BhOsLkspjP3BEUzhuAUrPBQhRO8='
- 3 CRYPT_DES, key required: true
CryptEncode(method,data,key,result)=32 / ok
µ b &“#ÇÅ+ýº'¥ B8f¡rØ-Pè<6âì‚Ë£
[00] B5 | 06 | 9D | 62 | 11 | 26 | 93 | 23 | C7 | C5 | 2B | FD | BA | 27 | A5 | 10 | 
[16] 42 | 38 | 66 | A1 | 72 | D8 | 2D | 50 | E8 | 3C | 36 | E2 | EC | 82 | CB | A3 | 
CryptEncode(CRYPT_BASE64,result,dummy,readable)=44 / ok
Try to decode this with CryptDecode.mq5 (CRYPT_DES):
base64:'tQadYhEmkyPHxSv9uielEEI4ZqFy2C1Q6Dw24uyCy6M='
- 4 CRYPT_HASH_SHA1, key required: false
CryptEncode(method,data,key,result)=20 / ok
§ßö*©ºø
€|)bËbzÇÍ Û€
[00] A7 | DF | F6 | 2A | A9 | BA | F8 | 0A | 80 | 7C | 29 | 62 | CB | 62 | 7A | C7 | 
[16] CD | 0E | DB | 80 | 
CryptEncode(CRYPT_BASE64,result,dummy,readable)=28 / ok
Try to decode this with CryptDecode.mq5 (CRYPT_HASH_SHA1):
base64:'p9/2Kqm6+AqAfCliy2J6x80O24A='
- 5 CRYPT_HASH_SHA256, key required: false
CryptEncode(method,data,key,result)=32 / ok
ÚZ2š€»”¾7 €… ñ–ÄÁ´˜¦“ome2r@¾ô®³”
[00] DA | 5A | 32 | 9A | 80 | BB | 94 | BE | 37 | 0C | 80 | 85 | 07 | F1 | 96 | C4 | 
[16] C1 | B4 | 98 | A6 | 93 | 6F | 6D | 65 | 32 | 72 | 40 | BE | F4 | AE | B3 | 94 | 
CryptEncode(CRYPT_BASE64,result,dummy,readable)=44 / ok
Try to decode this with CryptDecode.mq5 (CRYPT_HASH_SHA256):
base64:'2loymoC7lL43DICFB/GWxMG0mKaTb21lMnJAvvSus5Q='
- 6 CRYPT_HASH_MD5, key required: false
CryptEncode(method,data,key,result)=16 / ok
zIGT…  Fû;—3þèå
[00] 7A | 49 | 47 | 54 | 85 | 1B | 7F | 11 | 46 | FB | 3B | 97 | 33 | FE | E8 | E5 | 
CryptEncode(CRYPT_BASE64,result,dummy,readable)=24 / ok
Try to decode this with CryptDecode.mq5 (CRYPT_HASH_MD5):
base64:'eklHVIUbfxFG+zuXM/7o5Q=='
- 7 CRYPT_ARCH_ZIP, key required: false
CryptEncode(method,data,key,result)=34 / ok
x^óI-Q/VHÍK.ª,(Q(ÉÈ,VÈM-.NLO 
[00] 78 | 5E | F3 | 49 | 2D | 51 | 2F | 56 | 48 | CD | 4B | 2E | AA | 2C | 28 | 51 | 
[16] 28 | C9 | C8 | 2C | 56 | C8 | 4D | 2D | 2E | 4E | 4C | 4F | 05 | 00 | 80 | 07 | 
[32] 09 | C2 | 
CryptEncode(CRYPT_BASE64,result,dummy,readable)=48 / ok
Try to decode this with CryptDecode.mq5 (CRYPT_ARCH_ZIP):
base64:'eF7zSS1RL1ZIzUsuqiwoUSjJyCxWyE0tLk5MTwUAgAcJwg=='

...

- 7 CRYPT_ARCH_ZIP, key required: false
CryptEncode(method,data,key,result)=28 / ok
óI-Q/VHÍK.ª,(Q(ÉÈ,VÈM-.NLO 
[00] F3 | 49 | 2D | 51 | 2F | 56 | 48 | CD | 4B | 2E | AA | 2C | 28 | 51 | 28 | C9 | 
[16] C8 | 2C | 56 | C8 | 4D | 2D | 2E | 4E | 4C | 4F | 05 | 00 | 
CryptEncode(CRYPT_BASE64,result,dummy,readable)=40 / ok
Try to decode this with CryptDecode.mq5 (CRYPT_ARCH_ZIP):
base64:'80ktUS9WSM1LLqosKFEoycgsVshNLS5OTE8FAA=='

*/
//+------------------------------------------------------------------+
