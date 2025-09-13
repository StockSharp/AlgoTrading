//+------------------------------------------------------------------+
//|                                           AddOn_TrailingStop.mq4 |
//|                                     Copyright 2021, Signal Forex |
//|                                           https://signalforex.id |
//+------------------------------------------------------------------+
#property copyright "Copyright 2021, Signal Forex"
#property link      "https://signalforex.id"
#property version   "1.00"
#property strict
#property description   "Group sharing t.me/codeMQL"

input    bool     isTrailingStop = true;  //Trailing Stop
input    int      trailingStart  = 15;    //Trailing Start (pips)
input    int      trailingStep   = 5;     //Trailing Step (pips)

input    int      MagicNumber = 0;        //Magic Number

//Variabel Global
double   myPoint    = 0.0;


//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
  
   if (isTrailingStop && trailingStart <= 0){
      Alert ("Parameters incorrect");
      return(INIT_PARAMETERS_INCORRECT);
   }
   
   myPoint     = GetPipPoint(Symbol());
   
   return(INIT_SUCCEEDED);
  }
  
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---
   Print ("Thank you for using this EA");
   
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//---
   setTrailingStop(MagicNumber);
   
  }
//+------------------------------------------------------------------+


void setTrailingStop(int magicNumber=0){
   if (isTrailingStop==false) return;
   
   int      tOrder = 0;
   string   pair = "";
   double   sl = 0.0, tp = 0.0;
   
   pair = Symbol();
   
   tOrder = OrdersTotal();
   for (int i=tOrder-1; i>=0; i--){
      bool hrsSelect = OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
      if (OrderMagicNumber() == magicNumber && StringFind(OrderSymbol(), pair, 0) == 0 ){
         if (OrderType() == OP_BUY){
            if ( (Bid - (trailingStart * myPoint)) >= OrderOpenPrice()
                  && (Bid - ((trailingStart+trailingStep) * myPoint) >= OrderStopLoss() )
                )
            {
               sl = NormalizeDouble(Bid - (trailingStart * myPoint), Digits());
               if (!OrderModify(OrderTicket(), OrderOpenPrice(), sl, OrderTakeProfit(), 0, clrBlue)){
                  Print ("#", OrderTicket(), " gagal update sl");
               }
            }
         }
         
         if (OrderType() == OP_SELL){
            if ( (Ask + (trailingStart * myPoint)) <= OrderOpenPrice()
                  && ( (Ask + ((trailingStart+trailingStep) * myPoint) <= OrderStopLoss() ) || OrderStopLoss() == 0.0)
               )
            {
               sl = NormalizeDouble(Ask + (trailingStart * myPoint), Digits() );
               if (!OrderModify(OrderTicket(), OrderOpenPrice(), sl, OrderTakeProfit(), 0, clrBlue)){
                  Print ("#", OrderTicket(), " gagal update sl");
               }
            }
         }
      } //end if magicNumber
   }//end for
}



// Fungsi GetPipPoint
double GetPipPoint(string pair)
{
   double point= 0.0;
   int digits = (int) MarketInfo(pair, MODE_DIGITS);
   if(digits == 2 || digits== 3) point= 0.01;
   else if(digits== 4 || digits== 5) point= 0.0001;
   return(point);
}

