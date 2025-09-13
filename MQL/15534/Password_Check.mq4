//+------------------------------------------------------------------+
//|                                               Password_Check.mq4 |
//|                               Copyright 2016, Claude G. Beaudoin |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2016, Claude G. Beaudoin"
#property link      "https://www.mql5.com"
#property version   "1.00"
#property strict

//--- input parameters
extern string     Password;

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
{
   string   client = NULL;
   
   // Make sure the client is online in order to get his client name and account number
   if(IsConnected()) client = AccountInfoString(ACCOUNT_NAME) + " / " + DoubleToStr(AccountInfoInteger(ACCOUNT_LOGIN), 0);

   // Check client's password
   if(!Password_Check(client))
   {
      if(StringLen(Password) != 0)
         MessageBox("Unable to verify client and account number!" + 
            (IsConnected() ? "\nPlease verify you have the correct password." : "\n\nYou need to be online for verification."), 
            (IsConnected() ? "Invalid Password!" : "Offline!"), MB_OK | MB_ICONSTOP);
      else
         MessageBox("Unregistered software.\n\nPlease contact software vendor to obtain\nyour personal activation password." +
            (StringLen(client) == 0 ? "" : "\n\nYour registration information is:\n\n'"+client+"'"), "Unregistered", MB_OK | MB_ICONSTOP);
      
      // Invalid password or user is offline.  Remove expert and exit with error
      ExpertRemove();
      return(INIT_FAILED);
   }

   // All good...
   return(INIT_SUCCEEDED);
}


//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
{
   
}


//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
{
   
}

//+------------------------------------------------------------------+
//| Validate client's password
//+------------------------------------------------------------------+
bool Password_Check(string client)
{
   string   MasterKey;
   uchar dst[], src[], key[];

   // Define your encryption key here.  Must be 7 characters for DES/ECB encryption
   // Make your password difficult to figure out.  You last name is not a good idea!
   // Something like "wLdU&$z" would be good.  For now, we'll use a simple one...
   MasterKey = "NotDemo";  
   
   // Convert MasterKey to character array
   StringToCharArray(MasterKey, key);
   
   // Make sure client string is not null
   if(StringLen(client) == 0) return(false);
   
   // Encrypt the client using DES key
   StringToCharArray(client, src);
   CryptEncode(CRYPT_DES, src, key, dst);

   // Clear key and encode to BASE64
   ArrayInitialize(key, 0x00);
   CryptEncode(CRYPT_BASE64, dst, key, src);

   // Compare password and return result
   return(CharArrayToString(src) == Password);
}

//+------------------------------------------------------------------+
//| END OF CODE
//+------------------------------------------------------------------+
