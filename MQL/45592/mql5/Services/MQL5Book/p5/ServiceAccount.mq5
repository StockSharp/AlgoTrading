//+------------------------------------------------------------------+
//|                                               ServiceAccount.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property service

input long MasterAccount = 123456789;
input string Prefix = "!A_";

//+------------------------------------------------------------------+
//| T type pair of values to byte array                              |
//+------------------------------------------------------------------+
template<typename T>
union ByteOverlay2
{
   T values[2];
   uchar bytes[sizeof(T) * 2];
   ByteOverlay2(const T v1, const T v2) { values[0] = v1; values[1] = v2; }
};

//+------------------------------------------------------------------+
//| Common generator of a signature for given account numbers        |
//+------------------------------------------------------------------+
string Cipher(const long data1, const long data2)
{
   // TODO: replace the secret with your own hard to break byte set
   // TODO: for CRYPT_AES128/CRYPT_AES256 use 16/32 bytes long array
   const static uchar secret[] = {'S', 'E', 'C', 'R', 'E', 'T', '0'};
   ByteOverlay2<long> bo(data1, data2);
   // TODO: embed time limit into the cipher, for example, 1 day long:
   // const long day = 60 * 60 * 24;
   // ByteOverlay3<long> bo(TimeCurrent() / day * day, data1, data2);
   // OR computer session long
   // ByteOverlay3<long> bo((TimeLocal() - GetTickCount() / 1000), data1, data2);
   uchar result[];
   if(CryptEncode(CRYPT_DES, bo.bytes, secret, result) > 0)
   {
      uchar dummy[], text[];
      if(CryptEncode(CRYPT_BASE64, result, dummy, text) > 0)
      {
         return CharArrayToString(text);
      }
   }
   return NULL;
}

//+------------------------------------------------------------------+
//| Lookup list of licensed accounts (single one in this example)    |
//| and optional dependent accounts                                  |
//+------------------------------------------------------------------+
bool CheckAccounts()
{
   const long accounts[] = {MasterAccount}; // populate this array
   for(int i = 0; i < ArraySize(accounts); ++i)
   {
      if(IsCurrentAccountAuthorizedByMaster(accounts[i])) return true;
   }
   return false;
}

//+------------------------------------------------------------------+
//| Example of validation of current account against master account  |
//+------------------------------------------------------------------+
bool IsCurrentAccountAuthorizedByMaster(const long data)
{
   const long a = AccountInfoInteger(ACCOUNT_LOGIN);
   if(a == data) return true;
   const string s = Cipher(data, a);
   if(a != 0 && GlobalVariableGet(Prefix + s) == a)
   {
      Print("Sub-License is active: ", s);
      return true;
   }
   return false;
}

//+------------------------------------------------------------------+
//| Service program start function                                   |
//+------------------------------------------------------------------+
void OnStart()
{
   static long account = 0; // previous account
   
   // example of check-up in an external program
   if(CheckAccounts())
   {
      // act on success as appropriate
   }

   for( ; !IsStopped(); )
   {
      // account must be logged in, connected, and not in investor mode
      const bool c = TerminalInfoInteger(TERMINAL_CONNECTED)
                  && AccountInfoInteger(ACCOUNT_TRADE_ALLOWED);
      const long a = c ? AccountInfoInteger(ACCOUNT_LOGIN) : 0;

      if(account != a)
      {
         if(a != 0)
         {
            if(account != 0) // previous and current accounts are authorized
            {
               const string signature = Cipher(account, a);
               PrintFormat("Account %I64d registered by %I64d: %s", a, account, signature);
               // save info about logged accounts binding
               if(StringLen(signature) > 0)
               {
                  GlobalVariableTemp(Prefix + signature);
                  GlobalVariableSet(Prefix + signature, a);
               }
            }
            else
            {
               PrintFormat("New account %I64d detected", a);
            }
            // remember current account
            account = a;
         }
      }

      Sleep(1000);
   }
}
//+------------------------------------------------------------------+
/*
   example output (adjust master account number in inputs)
   
   [launch service instance]
   New account 123456789 detected
   [change account]
   Account 5555555 registered by 123456789: jdVKxUswBiNlZzDAnV3yxw==
   [re-launch service instance]
   Sub-License is active: jdVKxUswBiNlZzDAnV3yxw==
   Account 123456789 registered by 5555555: ZWcwwJ1d8seN1UrFSzAGIw==
*/
//+------------------------------------------------------------------+
