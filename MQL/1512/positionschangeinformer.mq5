//+------------------------------------------------------------------+
//|                                                      ForMQL5.mq5 |
//|                                            Copyright 2013, Rone. |
//|                                            rone.sergey@gmail.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2013, Rone."
#property link      "rone.sergey@gmail.com"
#property version   "1.00"
//+------------------------------------------------------------------+
//| Includes                                                         |
//+------------------------------------------------------------------+
#include <Translator.mqh>
//+------------------------------------------------------------------+
//| Enums                                                            |
//+------------------------------------------------------------------+
enum ENUM_ALERT_TYPE {
   ALERT,         // Alert
   SOUND,         // Sound
   EMAIL          // e-mail
};
//+------------------------------------------------------------------+
//| Input parameters                                                 |
//+------------------------------------------------------------------+
input ENUM_ALERT_TYPE   InpAlertType = ALERT;         // Alert type
input string            InpSoundName = "alert.wav";   // Sound filename
input ENUM_LANGUAGES    InpToLanguage = RU;           // Language
//+------------------------------------------------------------------+
//| Global variables                                                 |
//+------------------------------------------------------------------+
string            fileName;
ENUM_ALERT_TYPE   alertType;
CTranslator       toLang;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit() {
//---
   toLang.init(MQL5InfoString(MQL5_PROGRAM_NAME), InpToLanguage);
//---
   if ( InpAlertType == EMAIL ) {
      if ( !(bool)TerminalInfoInteger(TERMINAL_EMAIL_ENABLED) ) {
         alertType = ALERT;
         Print(toLang.tr("Email is not enabled. EA will use alerts"));
      }
   } else {
      alertType = InpAlertType;
   }
//---
   if ( alertType == SOUND && FileIsExist(InpSoundName, 0) == false ) {
      fileName = "alert.wav";
      Print(InpSoundName, ": ", toLang.tr("file does not exist"), ". ", toLang.tr("EA will use"), " ", fileName);
   } else {
      fileName = "\\Files\\" + InpSoundName;
   }   
//---
   return(0);
}
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason) {
//---

//---   
}
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick() {
//---

//---   
}
//+------------------------------------------------------------------+
//| TradeTransaction function                                        |
//+------------------------------------------------------------------+
void OnTradeTransaction(const MqlTradeTransaction& trans,
                        const MqlTradeRequest& request,
                        const MqlTradeResult& result)
{
//---
   CheckPositionsChange(trans);   
//---   
}
//+------------------------------------------------------------------+
//| Check positions changs function                                  |
//+------------------------------------------------------------------+
void CheckPositionsChange(const MqlTradeTransaction &trans) {
//---
   if ( (ENUM_TRADE_TRANSACTION_TYPE)trans.type == TRADE_TRANSACTION_DEAL_ADD ) {
      datetime end = TimeCurrent();
      datetime start = end - end % PeriodSeconds(PERIOD_D1);
      
      ResetLastError();
      if ( !HistorySelect(start, end) ) {
         Print(toLang.tr("Getting deals history failed"), ". ", toLang.tr("Error"), " #", GetLastError());
         return;
      }
      
      ulong ticket = HistoryDealGetTicket(HistoryDealsTotal()-1);
      
      if ( ticket == trans.deal ) {
         string type, comm;
         string msg = trans.symbol + ": ";
         double result;
         
         switch ( trans.deal_type ) {
            case DEAL_TYPE_BUY:
               type = toLang.tr("buy");
               break;
            case DEAL_TYPE_SELL:
               type = toLang.tr("sell");
               break;
            default:
               Print(msg, toLang.tr("Other operation"));
               return;
         }
         
         switch ( (int)HistoryDealGetInteger(ticket, DEAL_ENTRY) ) {
            case DEAL_ENTRY_IN:
                  msg += type + " " + toLang.tr("Opened at") + " " + getDealPrices(trans);
               break;
               
            case DEAL_ENTRY_OUT:
                  result = HistoryDealGetDouble(ticket, DEAL_PROFIT);
                  comm = HistoryDealGetString(ticket, DEAL_COMMENT);
                  if ( StringFind(comm, "sl") != -1 ) {
                     msg += toLang.tr("closed by Stop Loss at");
                  } else if ( StringFind(comm, "tp") != -1 ) {
                     msg += toLang.tr("closed by Take Profit at");
                  } else {
                     msg += toLang.tr("closed at");
                  }
                  msg += " " + DoubleToString(trans.price, _Digits) + ". " + 
                     toLang.tr("Result") + ": " + DoubleToString(result, 2) +
                     " " + AccountInfoString(ACCOUNT_CURRENCY);
               break;
               
            case DEAL_ENTRY_INOUT:
                  msg += " " + toLang.tr("reverse") + "-" + type + " " + getDealPrices(trans);
               break;
               
            default:
               Print(msg, "Other operation");
               return;
         }
         RaiseAlert(msg);
      }
   }
//---
}
//+------------------------------------------------------------------+
//| Get deal prices function                                         |
//+------------------------------------------------------------------+
string getDealPrices(const MqlTradeTransaction &trans) {
//---
   string str = DoubleToString(trans.price, _Digits) +
      ". " + toLang.tr("Stop Loss") + ": " + DoubleToString(trans.price_sl, _Digits) +
      ". " + toLang.tr("Take Profit") + ": " + DoubleToString(trans.price_tp, _Digits) +
      ". " + toLang.tr("Volume") + ": " + DoubleToString(trans.volume, 2);
//---
   return(str);
}
//+------------------------------------------------------------------+
//| Raise alert function                                             |
//+------------------------------------------------------------------+
void RaiseAlert(string msg) {
//---
   switch ( alertType ) {
      case SOUND:
         PlaySound(fileName);
         Print(msg);
         break;
      case EMAIL:
         if ( !SendMail(MQL5InfoString(MQL5_PROGRAM_NAME), msg) ) {
            Print(toLang.tr("Sending email failed"), ". ",  toLang.tr("Error"), " #", GetLastError());
         }
         break;
      case ALERT:
      default:
         Alert(msg);
   }
//---
}
//+------------------------------------------------------------------+
