//+------------------------------------------------------------------+
//|                                            Password_Generate.mq4 |
//|                               Copyright 2016, Claude G. Beaudoin |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2016, Claude G. Beaudoin"
#property link      "https://www.mql5.com"
#property version   "1.00"
#property strict

//--- input parameters
extern string     Client;

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
{
   string   Password = NULL;

   // Make sure the Cilent input is not empty
   if(StringLen(Client) != 0)
   {   
      // Generate a client password
      Password = Password_Generate(Client);
   
      // Print the generated password (makes it easy to cut and paste)
      Print("Client: '"+Client+"'  Password: "+Password);
      
      // Display the password generated for client
      MessageBox("Password generated for client / account\n\n'"+Client+"' is:\n"+Password, "Password Generator", MB_OK | MB_ICONINFORMATION);
   }
   else
      MessageBox("You must specify a client / account number!", "Password Generator", MB_OK | MB_ICONSTOP);
         
   // All good.  Remove expert.
   ExpertRemove();
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
//| Encrypt client information and return the password
//+------------------------------------------------------------------+
string Password_Generate(string client)
{
   string   MasterKey;
   uchar dst[], src[], key[];

   // Define your encryption key here.  Must be 7 characters for DES/ECB encryption
   // IT MUST BE THE SAME PASSWORD AS DEFINE IN THE "Password_Check()" function!
   // Make your password difficult to figure out.  You last name is not a good idea!
   // Something like "wLdU&$z" would be good.  For now, we'll use a simple one...
   MasterKey = "NotDemo";  
   
   // Convert MasterKey to character array
   StringToCharArray(MasterKey, key);
   
   // Encrypt the client using DES key
   StringToCharArray(client, src);
   CryptEncode(CRYPT_DES, src, key, dst);

   // Clear key and encode to BASE64
   ArrayInitialize(key, 0x00);
   CryptEncode(CRYPT_BASE64, dst, key, src);

   // Return encypted password
   return(CharArrayToString(src));   
}

//+------------------------------------------------------------------+
//| END OF CODE
//+------------------------------------------------------------------+
