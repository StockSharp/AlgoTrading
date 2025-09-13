//+------------------------------------------------------------------+
//|                                                       Expert.mq4 |
//|                                     Copyright 2020,alipoormomen. |
//|                       https://www.mql5.com/en/users/alipoormomen |
//+------------------------------------------------------------------+
#property copyright "Copyright 2020,alipoormomen"
#property link      "https://www.mql5.com/en/users/alipoormomen"
#property strict
//+------------------------------------------------------------------+
//|OnInit                                                            |
//+------------------------------------------------------------------+
int  OnInit()
  {
   if(!AFS_func_checkup())
     {
      Print(GetLastError());
      ExpertRemove();
     }

   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//|AFS_func_checkup                         |
//+------------------------------------------------------------------+
bool AFS_func_checkup(void)
  {
   if(SymbolsTotal(false)<1)
     {
      Alert("You have no symbol in MarketWatch");
      return(false);
     }
   if(!MQLInfoInteger(MQL_TRADE_ALLOWED))
     {
      Alert("Automated trading is forbidden in the program settings for ",__FILE__);
      return(false);
     }

   if(!TerminalInfoInteger(TERMINAL_TRADE_ALLOWED))
     {
      Alert("Check if automated trading is allowed in the terminal settings!");
      return(false);
     }

   if(!AccountInfoInteger(ACCOUNT_TRADE_EXPERT))
     {
      Alert("Automated trading is forbidden for the account ",AccountInfoInteger(ACCOUNT_LOGIN),
            " at the trade server side");
      return(false);
     }


   if(!AccountInfoInteger(ACCOUNT_TRADE_ALLOWED))
     {
      Comment("Trading is forbidden for the account ",AccountInfoInteger(ACCOUNT_LOGIN),
              ".\n Perhaps an investor password has been used to connect to the trading account.",
              "\n Check the terminal journal for the following entry:",
              "\n\'",AccountInfoInteger(ACCOUNT_LOGIN),"\': trading has been disabled - investor mode.");
      return(false);
     }

   if(!IsConnected())
     {
      Print("No connection!");
      return(false);
     }
//////**********************************************************
//--- Demo, contest or real account
   ENUM_ACCOUNT_TRADE_MODE account_type=(ENUM_ACCOUNT_TRADE_MODE)AccountInfoInteger(ACCOUNT_TRADE_MODE);
   if(account_type!=ACCOUNT_TRADE_MODE_DEMO)
     {
      Print("This is a real account! for trade on real account connect to author. https://www.mql5.com/en/users/alipoormomen");
      ExpertRemove();
      return(false);
     }
   Print("checkup pass :");
   return(true);
  }
//+------------------------------------------------------------------+
//| OnTick function                                                  |
//+------------------------------------------------------------------+
void OnTick()
  {

  }
//+------------------------------------------------------------------+
//|OnDeinit                                                          |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//--- The first way to get the uninitialization reason code
   Print(__FUNCTION__,"_Uninitalization reason code = ",reason);
//--- The second way to get the uninitialization reason code
   Print(__FUNCTION__,"_UninitReason = ",AFS_getUninitReasonText(_UninitReason));
  }
//+------------------------------------------------------------------+
//|AFS_getUninitReasonText                                           |
//+------------------------------------------------------------------+
string AFS_getUninitReasonText(int reasonCode)
  {
   string text="";
//---
   switch(reasonCode)
     {
      case REASON_ACCOUNT:
         text="Account was changed";
         break;
      case REASON_CHARTCHANGE:
         text="Symbol or timeframe was changed";
         break;
      case REASON_CHARTCLOSE:
         text="Chart was closed";
         break;
      case REASON_PARAMETERS:
         text="Input-parameter was changed";
         break;
      case REASON_RECOMPILE:
         text="Program "+__FILE__+" was recompiled";
         break;
      case REASON_REMOVE:
         text="Program "+__FILE__+" was removed from chart";
         break;
      case REASON_TEMPLATE:
         text="New template was applied to chart";
         break;
      default:
         text="Another reason";
     }
//---
   return text;
  } 
//+------------------------------------------------------------------+
