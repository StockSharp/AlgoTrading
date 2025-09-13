//+------------------------------------------------------------------+
//|                                                 EnvSignature.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property script_show_inputs

#include <MQL5Book/PRTF.mqh>

//+------------------------------------------------------------------+
//| Required secret stuff                                            |
//+------------------------------------------------------------------+
// WARNING: change this macro for your own hard to break set of bytes
#define PROGRAM_SPECIFIC_SECRET "<PROGRAM-SPECIFIC-SECRET>"
// WARNING: choose your own string as a glue in the name'='value pairs
#define INSTANCE_SPECIFIC_PEPPER "=" // it's obvious just for the demo
// WARNING: disable the next macro in actual product,
//          leave it in your own subscription utility only
#define I_AM_DEVELOPER

//+------------------------------------------------------------------+
//| Public inputs for user                                           |
//+------------------------------------------------------------------+
input string Validation = "";

#ifdef I_AM_DEVELOPER
#define INPUT input
#else
#define INPUT const
#endif

//+------------------------------------------------------------------+
//| Private inputs for developer                                     |
//+------------------------------------------------------------------+
INPUT string Signature = "";
INPUT string Secret = PROGRAM_SPECIFIC_SECRET;
INPUT string Pepper = INSTANCE_SPECIFIC_PEPPER;

//+------------------------------------------------------------------+
//| Collect environment properties and build a hash array for them   |
//+------------------------------------------------------------------+
class EnvSignature
{
private:
   string data;

protected:
   virtual string secret() const = 0;
   virtual string pepper() const = 0;
   
public:
   bool append(const ENUM_TERMINAL_INFO_STRING e)
   {
      return append(EnumToString(e) + pepper() + TerminalInfoString(e));
   }

   bool append(const ENUM_MQL_INFO_STRING e)
   {
      return append(EnumToString(e) + pepper() + MQLInfoString(e));
   }

   bool append(const ENUM_TERMINAL_INFO_INTEGER e)
   {
      return append(EnumToString(e) + pepper() + StringFormat("%d", TerminalInfoInteger(e)));
   }

   bool append(const ENUM_MQL_INFO_INTEGER e)
   {
      return append(EnumToString(e) + pepper() + StringFormat("%d", MQLInfoInteger(e)));
   }
   
   bool append(const string s)
   {
      data += s;
      return true;
   }
   
   string emit() const
   {
      uchar pack[];
      if(StringToCharArray(data + secret(), pack, 0, StringLen(data) + StringLen(secret()), CP_UTF8) <= 0) return NULL;

      uchar key[], result[];
      if(CryptEncode(CRYPT_HASH_SHA256, pack, key, result) <= 0) return NULL;
      
      Print("Hash bytes:");
      ArrayPrint(result);

      uchar text[];
      CryptEncode(CRYPT_BASE64, result, key, text);
      return CharArrayToString(text);
   }

   bool check(const string sig, string &validation)
   {
      uchar bytes[];
      const int n = StringToCharArray(sig + secret(), bytes, 0, StringLen(sig) + StringLen(secret()), CP_UTF8);
      if(n <= 0) return false;
      
      uchar key[], result1[], result2[];
      if(CryptEncode(CRYPT_HASH_SHA256, bytes, key, result1) <= 0) return false;
      
      /*
         WARNING
         The next code should be available only in an utility running by developer.
         The program delivered to a user should be compiled without this branch.
      */
      #ifdef I_AM_DEVELOPER
      if(StringLen(validation) == 0)
      {
         if(CryptEncode(CRYPT_BASE64, result1, key, result2) <= 0) return false;
         validation = CharArrayToString(result2);
         return true;
      }
      #endif
      uchar values[];
      // the exact length is needed to not append terminating '0'
      if(StringToCharArray(validation, values, 0, StringLen(validation)) <= 0) return false;
      if(CryptDecode(CRYPT_BASE64, values, key, result2) <= 0) return false;
      
      return ArrayCompare(result1, result2) == 0;
   }
};

//+------------------------------------------------------------------+
//| Provide custom secrets for specific product and/or user          |
//+------------------------------------------------------------------+
class MyEnvSignature : public EnvSignature
{
protected:
   virtual string secret() const override
   {
      return Secret;
   }
   virtual string pepper() const override
   {
      return Pepper;
   }
};

//+------------------------------------------------------------------+
//| Collect all required properties                                  |
//+------------------------------------------------------------------+
void FillEnvironment(EnvSignature &env)
{
   // the order is unimportant, we can shuffle it
   env.append(TERMINAL_LANGUAGE);
   env.append(TERMINAL_COMMONDATA_PATH);
   env.append(TERMINAL_CPU_CORES);
   env.append(TERMINAL_MEMORY_PHYSICAL);
   env.append(TERMINAL_SCREEN_DPI);
   env.append(TERMINAL_SCREEN_WIDTH);
   env.append(TERMINAL_SCREEN_HEIGHT);
   env.append(TERMINAL_VPS);
   env.append(MQL_PROGRAM_TYPE);
}

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   MyEnvSignature env;
   string signature;

   if(StringLen(Signature) > 0)
   {
      #ifdef I_AM_DEVELOPER
      if(StringLen(Validation) == 0)
      {
         string validation;
         if(env.check(Signature, validation))
            Print("Validation:", validation);
         // Validation:mT3a6Uuzopjcs2QkbC1zW10sYI8KVLgchq/i7gBY4Bg=
         return;
      }
      signature = Signature;
      #endif
   }
   else
   {
      // check actual environment to detect possible changes against the signature
      FillEnvironment(env);
      // fake change in environment could be a timezone
      // env.append("Dummy" + (string)(TimeGMTOffset() - TimeDaylightSavings()));
      signature = env.emit();
   }
   
   if(StringLen(Validation) == 0)
   {
      Print("Validation string from developer is required to run this script");
      Print("Environment Signature is generated for current state...");
      Print("Signature:", signature);
      // Signature:+96RUrYFVpT9NAhLZTXjkmW9dWeqwo5FjXc2PoQqfl7LdfzJWYjJ+Unf6YU=
      return;
   }
   else
   {
      string validation = Validation; // non-const variable is required as argument
      const bool accessGranted = env.check(signature, validation);
      if(!accessGranted)
      {
         Print("Wrong validation string, terminating");
         return;
      }
   }
   
   Print("The script is validated and running normally");
   // ... actual work code
}
//+------------------------------------------------------------------+
/*

Hash bytes:
 93  92   5 229  65 218 193  49 193  99 184 152 124  64  51 247 178  51 248 124  86 169 216 198  39 146 166  64  81  18 147 174
Validation string from developer is required to run this script
Environment Signature is generated for current state...
Signature:XVwF5UHawTHBY7iYfEAz97Iz+HxWqdjGJ5KmQFESk64=

Validation:ean4ZuRLZespy9D/oPtnNrq3oaokKeXdlQGy47Sv5zo=

*/
//+------------------------------------------------------------------+
