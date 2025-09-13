//+------------------------------------------------------------------+
//|                                            Exp_ToCloseProfit.mq5 |
//|                             Copyright © 2011,   Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright © 2011, Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "1.00"
//+------------------------------------------------+
//| Expert Advisor input parameters                |
//+------------------------------------------------+
input double MaxProfit=1000.00;  //Maximum profit 
//+------------------------------------------------+

//+------------------------------------------------------------------+
//  Trading algorithms                                               | 
//+------------------------------------------------------------------+
#include <TradeAlgorithms.mqh>
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//----

//----
   return(0);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//----
   Comment("");
   GlobalVariableDel(GetMaxProfitLevelName());
//----
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//----
   double profit=AccountInfoDouble(ACCOUNT_PROFIT);
   MaxProfitLevelSet(profit,MaxProfit);
   MaxProfitLevelDel();
   string info;
   StringConcatenate(info,"Profit = ",DoubleToString(profit,2),"; MaxProfit = ",DoubleToString(MaxProfit,2),";");
   Comment(info);

//+----------------------------------------------+
//| Performing deals                             |
//+----------------------------------------------+
   if(MaxProfitLevelCheck(profit))
      for(int pos=PositionsTotal()-1; pos>=0; pos--)
        {
         string symbol=PositionGetSymbol(pos);
         //---- Closing a long position
         bool BUY_Close=true;
         BuyPositionClose(BUY_Close,symbol,10);

         //---- Closing a short position 
         bool SELL_Close=true;
         SellPositionClose(SELL_Close,symbol,10);
        }
  }
//+------------------------------------------------------------------+
//| GetMaxProfitLevelName() function                                 |
//+------------------------------------------------------------------+
string GetMaxProfitLevelName()
  {
//----
   string G_Name_;
   StringConcatenate(G_Name_,"MaxProfit_",AccountInfoInteger(ACCOUNT_LOGIN));
//----
   return(G_Name_);
  }
//+------------------------------------------------------------------+
//| MaxProfitLevelCheck() function                                   |
//+------------------------------------------------------------------+
bool MaxProfitLevelCheck(double Profit)
  {
//---- Getting the name of a global variable
   string G_Name_=GetMaxProfitLevelName();

//---- Checking for the profit level triggering 
   if(GlobalVariableCheck(G_Name_) && GlobalVariableGet(G_Name_)==1) return(true);
//----
   return(false);
  }
//+------------------------------------------------------------------+
//| MaxProfitLevelSet() function                                     |
//+------------------------------------------------------------------+
void MaxProfitLevelSet(double Profit,double Max_Profit)
  {
//----
   string G_Name_=GetMaxProfitLevelName();
   if(Profit>=Max_Profit) GlobalVariableSet(G_Name_,1);
//----
  }
//+------------------------------------------------------------------+
//| MaxProfitLevelDel() function                                     |
//+------------------------------------------------------------------+
void MaxProfitLevelDel()
  {
//---- Getting the name of a global variable
   string G_Name_=GetMaxProfitLevelName();
   if(GlobalVariableCheck(G_Name_) && !PositionsTotal()) GlobalVariableDel(G_Name_);
//----
  }
//+------------------------------------------------------------------+
