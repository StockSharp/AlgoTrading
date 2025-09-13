//+------------------------------------------------------------------+
//|                                                  CryptDecode.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright   "Copyright 2022, MetaQuotes Ltd."
#property link        "https://www.mql5.com"
#property description "Use CryptDecode to restore protected data from given cipher, using different methods."
#property script_show_inputs

#include <MQL5Book/PRTF.mqh>
#include <MQL5Book/EnumToArray.mqh>
#include <MQL5Book/ArrayUtils.mqh>

#define KEY_REQUIRED(C) ((C) == CRYPT_DES || (C) == CRYPT_AES128 || (C) == CRYPT_AES256)
#define IS_HASH(C) ((C) == CRYPT_HASH_MD5 || (C) == CRYPT_HASH_SHA1 || (C) == CRYPT_HASH_SHA256)

enum ENUM_CRYPT_METHOD_EXT
{
   _CRYPT_DES = CRYPT_DES,                 // DES
   _CRYPT_AES128 = CRYPT_AES128,           // AES128
   _CRYPT_AES256 = CRYPT_AES256,           // AES256
   _CRYPT_HASH_MD5 = CRYPT_HASH_MD5,       // MD5    (irreversible)
   _CRYPT_HASH_SHA1 = CRYPT_HASH_SHA1,     // SHA1   (irreversible)
   _CRYPT_HASH_SHA256 = CRYPT_HASH_SHA256, // SHA256 (irreversible)
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

input string Text; // Text (base64, or empty to process File)
input string File = "MQL5Book/clock10.htm.BASE64";
input ENUM_CRYPT_METHOD_EXT Method = _CRYPT_BASE64;
input DUMMY_KEY_LENGTH GenerateKey = DUMMY_KEY_CUSTOM; // GenerateKey (length, or take from CustomKey)
input string CustomKey = "My top secret key is very strong"; // CustomKey
input bool DisableCRCinZIP = false;

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   ENUM_CRYPT_METHOD method = 0;
   int methods[];
   uchar key[] = {};        // key is empty by default: ok for zip, base64
   uchar data[], result[];
   uchar zip[], opt[] = {1, 0, 0, 0};

   if(!StringLen(Text) && !StringLen(File))
   {
      Alert("Please specify either Text or File to decode");
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

   method = (ENUM_CRYPT_METHOD)Method;
   Print("- ", EnumToString(method), ", key required: ", KEY_REQUIRED(method));

   if(StringLen(Text))
   {
      if(method != CRYPT_BASE64)
      {
         // Since all methods except for Base64 produce binary results,
         // it was additionally converted to Base64 in CryptEncode.mq5,
         // hence we need to restore binary data from text input
         // before deciphering it
         uchar base64[];
         const uchar dummy[] = {};
         PRTF(Text);
         PRTF(StringToCharArray(Text, base64, 0, -1, CP_UTF8));
         ArrayResize(base64, ArraySize(base64) - 1);
         Print("Text (bytes):");
         ByteArrayPrint(base64);
         if(!PRTF(CryptDecode(CRYPT_BASE64, base64, dummy, data)))
         {
            return; // error
         }
         
         Print("Raw data to decipher (after de-base64):");
         ByteArrayPrint(data);
      }
      else
      {
         PRTF(StringToCharArray(Text, data, 0, -1, CP_UTF8));
         ArrayResize(data, ArraySize(data) - 1);
      }
   }
   else if(StringLen(File))
   {
      PRTF(File);
      if(PRTF(FileLoad(File, data)) <= 0)
      {
         return; // error
      }
   }
   
   if(IS_HASH(method))
   {
      Print("WARNING: hashes can not be used to restore data! CryptDecode will fail.");
   }
   
   if(method == CRYPT_ARCH_ZIP)
   {
      // a special key is supported for CRYPT_ARCH_ZIP:
      // at least 4 bytes with 1-st '1' is used to disable specific to MQL5
      // Adler32 checksum embedding into the compressed data,
      // needed for compatibility with some standards such as ZIP archives
      if(DisableCRCinZIP)
      {
         ArrayCopy(zip, opt); // make dynamic copy of array (dynamic needed for ArraySwap)
      }
      ArraySwap(key, zip); // substitute the key with empty or optional
   }
   
   ResetLastError();
   if(PRTF(CryptDecode(method, data, key, result)))
   {
      if(StringLen(Text))
      {
         Print("Text restored:");
         Print(CharArrayToString(result, 0, WHOLE_ARRAY, CP_UTF8));
      }
      else // File
      {
         const string filename = File + ".dec";
         if(PRTF(FileSave(filename, result)))
         {
            Print("File saved: ", filename);
         }
      }
   }
}

//+------------------------------------------------------------------+
/*

- CRYPT_BASE64, key required: false
File=MQL5Book/clock10.htm.BASE64 / ok
FileLoad(File,data)=1320 / ok
CryptDecode(method,data,key,result)=988 / ok
FileSave(filename,result)=true / ok
File saved: MQL5Book/clock10.htm.BASE64.dec

...

CustomKey=My top secret key is very strong / ok
Key (bytes):
[00] 4D | 79 | 20 | 74 | 6F | 70 | 20 | 73 | 65 | 63 | 72 | 65 | 74 | 20 | 6B | 65 | 
[16] 79 | 20 | 69 | 73 | 20 | 76 | 65 | 72 | 79 | 20 | 73 | 74 | 72 | 6F | 6E | 67 | 
- CRYPT_AES128, key required: true
Text=AQuvVCoSy1szaN8Owy8tQxl9rIrRj9hOqK7KgYYGh9E= / ok
StringToCharArray(Text,base64,0,-1,CP_UTF8)=44 / ok
Text (bytes):
[00] 41 | 51 | 75 | 76 | 56 | 43 | 6F | 53 | 79 | 31 | 73 | 7A | 61 | 4E | 38 | 4F | 
[16] 77 | 79 | 38 | 74 | 51 | 78 | 6C | 39 | 72 | 49 | 72 | 52 | 6A | 39 | 68 | 4F | 
[32] 71 | 4B | 37 | 4B | 67 | 59 | 59 | 47 | 68 | 39 | 45 | 3D | 
CryptDecode(CRYPT_BASE64,base64,dummy,data)=32 / ok
Raw data to decipher (after de-base64):
[00] 01 | 0B | AF | 54 | 2A | 12 | CB | 5B | 33 | 68 | DF | 0E | C3 | 2F | 2D | 43 | 
[16] 19 | 7D | AC | 8A | D1 | 8F | D8 | 4E | A8 | AE | CA | 81 | 86 | 06 | 87 | D1 | 
CryptDecode(method,data,key,result)=32 / ok
Text restored:
Let's encrypt this message

...

- CRYPT_HASH_MD5, key required: false
File=MQL5Book/clock10.htm.MD5 / ok
FileLoad(File,data)=16 / ok
WARNING: hashes can not be used to restore data! CryptDecode will fail.
CryptDecode(method,data,key,result)=0 / INVALID_PARAMETER(4003)

*/
//+------------------------------------------------------------------+
