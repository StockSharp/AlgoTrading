//+------------------------------------------------------------------+
//|                                       VisualTrader-BUYScript.mq4 |
//|                                       when-money-makes-money.com |
//|                                       when-money-makes-money.com |
//+------------------------------------------------------------------+
#property copyright "when-money-makes-money.com"
#property link      "when-money-makes-money.com"

//+------------------------------------------------------------------+
//| script program start function                                    |
//+------------------------------------------------------------------+
extern double lot=0.1;
extern int SL=20;
extern int TP=40;
int start()
  {
//----
   string prefix="";
   if(IsTesting()) prefix="";
   
   if(!GlobalVariableCheck(prefix+"NewTrade")){
      GlobalVariableSet(prefix+"NewTrade","1");
      GlobalVariableSet(prefix+"NewTrade_mode",OP_SELL);
      GlobalVariableSet(prefix+"NewTrade_lot",lot);
      GlobalVariableSet(prefix+"NewTrade_tp",TP);
      GlobalVariableSet(prefix+"NewTrade_sl",SL);   
   }
//----
   return(0);
  }
//+------------------------------------------------------------------+