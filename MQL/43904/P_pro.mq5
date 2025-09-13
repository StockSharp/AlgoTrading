//+------------------------------------------------------------------+
//|                                                        P_pro.mq5 |
//|                                  Copyright 2023,Obunadike Chioma |
//|                                      https://wa.me/2349124641304 |
//+------------------------------------------------------------------+
#property copyright "Copyright 2023,Obunadike Chioma"
#property link      "https://wa.me/2349124641304"
#property version   "1.00"
#include <Trade\Trade.mqh>
#include <Trade\SymbolInfo.mqh>
#include <Trade\PositionInfo.mqh>
#include <Trade\OrderInfo.mqh>
#include <Trade\DealInfo.mqh>
CTrade trade;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+



enum opt
  {
   Enable,
   Disable
  };
input double lot = 0.1; //Lotsize
input opt ProfitDeal = Enable;
input opt LossDeal = Enable;
input double PD = 5;  //ProfitDeal%
input double  LD = 5;  //LossDeal%
input double LotComission = 7; //Lot Comission (in $)


void OnTick()
  {
  
  if (PositionsTotal()==0)
  {
  double Bid1 = SymbolInfoDouble(_Symbol,SYMBOL_BID);
    trade.Sell(0.10,NULL,Bid1,0,0,NULL);
 }
 
  if(ProfitDeal==Enable)
     {ClosePosProfit();}
     
     if(LossDeal==Enable)
     {ClosePosLoss();}
  }

void ClosePosProfit()

{
 
  double Ask = SymbolInfoDouble(_Symbol,SYMBOL_ASK);
  double Bid = SymbolInfoDouble(_Symbol,SYMBOL_BID);
  double Sp = Ask - Bid; 
  double Spread =  NormalizeDouble(Sp,5);
  double Acct_Bal = AccountInfoDouble(ACCOUNT_BALANCE); 
 
 for(int i = PositionsTotal()-1; i>=0; i--)
     
     {
     string symbols = PositionGetSymbol(i);   // get position symbol
    
     if (_Symbol == symbols)
   
     { //--Get position ticket--  
       ulong PositionTicket=PositionGetInteger(POSITION_TICKET);  
       
       double Profit =PositionGetDouble(POSITION_PROFIT);
       
       if (Profit >= ((PD/100)*Acct_Bal)+(LotComission*lot)+(Spread*lot)) 
    
       trade.PositionClose(PositionTicket,-1);
   
      }
         }  }
           
            
         
         void ClosePosLoss()

{
 
  double Ask = SymbolInfoDouble(_Symbol,SYMBOL_ASK);
  double Bid = SymbolInfoDouble(_Symbol,SYMBOL_BID);
  double Sp = Bid - Ask; 
  double Spread =  NormalizeDouble(Sp,5);
   double Acct_Bal = AccountInfoDouble(ACCOUNT_BALANCE);

 for(int i = PositionsTotal()-1; i>=0; i--)
     {
   {
     string symbols = PositionGetSymbol(i);   // get position symbol
    
     if (_Symbol == symbols)
   
     {
      //--Get position ticket--  
       ulong PositionTicket=PositionGetInteger(POSITION_TICKET);  
     
       double Loss=PositionGetDouble(POSITION_PROFIT);
    
        Print (Loss);
        
    if (Loss <= (-(LD/100)*Acct_Bal)-(LotComission*lot)-(Spread*lot)) 
 trade.PositionClose(PositionTicket,-1);
   }
      }
          }   }
           