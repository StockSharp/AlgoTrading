//+------------------------------------------------------------------+
//|                                              Exp_ToCloseLoss.mq5 |
//|                             Copyright © 2011,   Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright © 2011, Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "1.00"
//+------------------------------------------------+
//| Expert Advisor input parameters                |
//+------------------------------------------------+
input double MaxLoss=1000.00;  //Maximum profit 
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
   GlobalVariableDel(GetMaxLossLevelName());
//----
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//----
   double profit=AccountInfoDouble(ACCOUNT_PROFIT);
   MaxLossLevelSet(profit,MaxLoss);
   MaxLossLevelDel();
   string info;
   StringConcatenate(info,"Loss = ",DoubleToString(profit,2),"; MaxLoss = ",DoubleToString(MaxLoss,2),";");
   Comment(info);

//+----------------------------------------------+
//| Performing deals                             |
//+----------------------------------------------+
   if(MaxLossLevelCheck(profit))
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
//| GetMaxLossLevelName() function                                   |
//+------------------------------------------------------------------+
string GetMaxLossLevelName()
  {
//----
   string G_Name_;
   StringConcatenate(G_Name_,"MaxLoss_",AccountInfoInteger(ACCOUNT_LOGIN));
//----
   return(G_Name_);
  }
//+------------------------------------------------------------------+
//| MaxLossLevelCheck() function                                     |
//+------------------------------------------------------------------+
bool MaxLossLevelCheck(double Loss)
  {
//---- Getting the name of a global variable
   string G_Name_=GetMaxLossLevelName();

//---- Checking for the profit level triggering 
   if(GlobalVariableCheck(G_Name_) && GlobalVariableGet(G_Name_)==1) return(true);
//----
   return(false);
  }
//+------------------------------------------------------------------+
//| MaxLossLevelSet() function                                       |
//+------------------------------------------------------------------+
void MaxLossLevelSet(double Loss,double Max_Loss)
  {
//----
   string G_Name_=GetMaxLossLevelName();
   if(Loss<=-Max_Loss) GlobalVariableSet(G_Name_,1);
//----
  }
//+------------------------------------------------------------------+
//| MaxLossLevelDel() function                                       |
//+------------------------------------------------------------------+
void MaxLossLevelDel()
  {
//---- Getting the name of a global variable
   string G_Name_=GetMaxLossLevelName();
   if(GlobalVariableCheck(G_Name_) && !PositionsTotal()) GlobalVariableDel(G_Name_);
//----
  }
//+------------------------------------------------------------------+
