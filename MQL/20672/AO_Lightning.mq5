//+------------------------------------------------------------------+
//|                                                 AO_Lightning.mq5 |
//|                                                Evgeny Kravchenko |
//|                           https://www.mql5.com/ru/users/evgen333 |
//+------------------------------------------------------------------+
#property copyright "Evgeny Kravchenko"
#property link      "https://www.mql5.com/ru/users/evgen333"
#property version   "1.00"
#include <Trade\Trade.mqh>

//--- input parameters
input double   LotFixed=0.01;             //Lots
input int      LotFromPercent=0;          //Lot From Percent
input int      Period_sma_slow=5;         //Period sma slow
input int      Period_sma_fast=34;        //Period sma fast
input int      Orders=10;                 //Orders max
input int      Magic=5632;                //Magic number 


//---Create a object for opening positions
CTrade            trade;

//---create variables and arrays
int bars;
int buy_total;
int sell_total;

double buy_profit;
double sell_profit;
double Bid;
double Ask;
double Lot;
double AO[10];
double High[],Low[],Media[];

datetime Time[];
datetime time_bar=0;

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//--- set the magic number
   trade.SetExpertMagicNumber(Magic);

   bars=Period_sma_fast+Period_sma_slow;
   
   ArrayResize(Media,bars);

   Alert("AO_Lightning Load");
//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---

  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//---Let's check how many bars
   if(Bars(_Symbol,PERIOD_CURRENT)<bars) return;


//--- calculate the lot
   Lot=Lots(_Symbol,LotFromPercent,LotFixed);

//--- we copy in the arrays prices of bars and indicators
   high(_Symbol,PERIOD_CURRENT,High);
   low(_Symbol,PERIOD_CURRENT,Low);
   time(_Symbol,PERIOD_CURRENT,Time);

//--- if a new bar does not appear, return   
   if(Time[0]==time_bar) return;
   time_bar=Time[0];

//--- we copy in the arrays prices of bars and indicators   
   Median(High,Low,Media,bars);
   
      
   AO_calculate(Media,Period_sma_slow,Period_sma_fast,AO,5);

//--- check information about open positions
   info_order(_Symbol,Magic);


//--- open long position
  if(AO[0]<AO[1])
    {
     //---Close sell orders
      if(sell_total>0)
        {
         Alert("Close Sell ",sell_profit);
         close_sell(_Symbol);
        }

      if(buy_total<Orders)
        {
         Ask=SymbolInfoDouble(_Symbol,SYMBOL_ASK);
         //--- normalize the price
         normalPrice(Ask);
         //--- Check the money for trade, if there is no return
         if(CheckMoneyForTrade(_Symbol,Lot,ORDER_TYPE_BUY,Ask)) return;

         trade.Buy(Lot,_Symbol,Ask,0,0);
        }
    }

//--- open a short position
   if(AO[0]>AO[1])
     {
      //---Close buy orders
      if(buy_total>0)
        {
         Alert("Close Buy ",buy_profit);
         close_buy(_Symbol);
        }

      if(sell_total<Orders)
        {
         Bid=SymbolInfoDouble(_Symbol,SYMBOL_BID);
         //--- normalize the price
         normalPrice(Bid);
         //--- Check the money for trade, if there is no return
         if(CheckMoneyForTrade(_Symbol,Lot,ORDER_TYPE_BUY,Bid)) return;

         trade.Sell(Lot,_Symbol,Bid,0,0);
        } 
     }
       
   
   
   

  }//EndOnTick
//+------------------------------------------------------------------+



//+------------------------------------------------------------------+
//|                        time                                      |
//+------------------------------------------------------------------+
bool time(string symbol,ENUM_TIMEFRAMES timeframe,datetime &data[])
  {
   ArraySetAsSeries(data,true);
   int copied=CopyTime(symbol,timeframe,0,bars,data);
   if(copied>0) return(true);
   else
      return(false);
  }
//+------------------------------------------------------------------+
//|                        Low                                       |
//+------------------------------------------------------------------+
bool low(string symbol,ENUM_TIMEFRAMES timeframe,double &data[])
  {
   ArraySetAsSeries(data,true);
   int copied=CopyLow(symbol,timeframe,0,bars,data);
   if(copied>0) return(true);
   else
      return(false);
  }
//+------------------------------------------------------------------+
//|                        high                                      |
//+------------------------------------------------------------------+
bool high(string symbol,ENUM_TIMEFRAMES timeframe,double &data[])
  {
   ArraySetAsSeries(data,true);
   int copied=CopyHigh(symbol,timeframe,0,bars,data);
   if(copied>0) return(true);
   else
      return(false);
  }
//+------------------------------------------------------------------+
//|           info_order                                             |
//+------------------------------------------------------------------+
void info_order(string simb,int magic)
  {

   buy_total=0;
   sell_total=0;
   buy_profit=0.0;
   sell_profit=0.0;

   for(int i=0; i<PositionsTotal(); i++)
     {
      if(PositionGetSymbol(i)==simb)
        {
         ulong ticket=PositionGetTicket(i);
         PositionSelectByTicket(ticket);
         ulong position_type=PositionGetInteger(POSITION_TYPE);
         ulong order_magic=PositionGetInteger(POSITION_MAGIC);
         string com=PositionGetString(POSITION_COMMENT);
         string my_symbol=PositionGetString(POSITION_SYMBOL);

         if(position_type==POSITION_TYPE_SELL && simb==my_symbol && magic==order_magic)
           {
            sell_total++;
            sell_profit+=PositionGetDouble(POSITION_PROFIT);
           }

         if(position_type==POSITION_TYPE_BUY && simb==my_symbol && magic==order_magic)
           {
            buy_total++;
            buy_profit+=PositionGetDouble(POSITION_PROFIT);
           }

        }
     }

  }
//+------------------------------------------------------------------+
//|                       Lots                                       |
//+------------------------------------------------------------------+
double Lots(string simb,int proc,double lot)
  {
   double Lot_min=SymbolInfoDouble(simb,SYMBOL_VOLUME_MIN);
   double Lot_max=SymbolInfoDouble(simb,SYMBOL_VOLUME_MAX);
   double Free=AccountInfoDouble(ACCOUNT_FREEMARGIN);
   double Lot_step=SymbolInfoDouble(simb,SYMBOL_VOLUME_STEP);
   double contrakt=SymbolInfoDouble(simb,SYMBOL_TRADE_CONTRACT_SIZE);

   if(proc>0)
     {
      lot=MathFloor(Free*proc/contrakt/Lot_step)*Lot_step;
     }

   if(lot<Lot_min)
     {
      lot=Lot_min;
     }

   if(lot>Lot_max)
     {
      lot=Lot_max;
     }

   return(NormalizeDouble(lot,2));
  }
//+------------------------------------------------------------------+
//|                       CheckMoneyForTrade                         |
//+------------------------------------------------------------------+
bool CheckMoneyForTrade(string symb,double lots,ENUM_ORDER_TYPE type,double price)
  {
   string eror="";

//--- value of necessary and free margin
   double margin,free_margin=AccountInfoDouble(ACCOUNT_MARGIN_FREE);
//--- call the validation function
   if(!OrderCalcMargin(type,symb,lots,price,margin))
     {
      eror="Error in Function CheckMoneyForTrade ";
      Print("Error in ",__FUNCTION__," code=",GetLastError());
      return(true);
     }
//--- if there is not enough money for the operation
   if(margin>free_margin)
     {
      eror="Not enough money for "+EnumToString(type)+" "+DoubleToString(lots,2)+" "+symb+". ";

      //--- report an error and return true
      MessageBox(eror,NULL,0);
      Print(eror);
      return(true);
     }

//--- the check was successful
   return(false);
  }
//+------------------------------------------------------------------+
//|                         normalPrice                              |
//+------------------------------------------------------------------+
bool normalPrice(double &price)
  {
   double tick_size_simb=SymbolInfoDouble(_Symbol,SYMBOL_TRADE_TICK_SIZE);
   if(tick_size_simb>0)
     {
      int ratio=(int)MathRound(price/tick_size_simb);
      if(MathAbs(ratio*tick_size_simb-price)>0.0000001)
        {
         price=NormalizeDouble(ratio*tick_size_simb,_Digits);
         return true;
        }
     }
   return false;
  }
//+------------------------------------------------------------------+
//|                 close_buy                                        |
//+------------------------------------------------------------------+
void close_buy(string simb)
  {
   while(buy_total>0)
     {
      for(int i=0; i<PositionsTotal(); i++)
        {
         if(PositionGetSymbol(i)==simb)
           {
            ulong ticket=PositionGetTicket(i);
            PositionSelectByTicket(ticket);
            ulong position_type=PositionGetInteger(POSITION_TYPE);
            ulong order_magic=PositionGetInteger(POSITION_MAGIC);
            string com=PositionGetString(POSITION_COMMENT);
            string my_symbol=PositionGetString(POSITION_SYMBOL);

            if(position_type==POSITION_TYPE_BUY && simb==my_symbol && Magic==order_magic)
              {
               trade.PositionClose(ticket);
               buy_total--;
              }
           }
        }
     }
  }
//+------------------------------------------------------------------+
//|                             close_sell                           |
//+------------------------------------------------------------------+
void close_sell(string simb)
  {
   while(sell_total>0)
     {
      for(int i=0; i<PositionsTotal(); i++)
        {
         if(PositionGetSymbol(i)==simb)
           {
            ulong ticket=PositionGetTicket(i);
            PositionSelectByTicket(ticket);
            ulong position_type=PositionGetInteger(POSITION_TYPE);
            ulong order_magic=PositionGetInteger(POSITION_MAGIC);
            string com=PositionGetString(POSITION_COMMENT);
            string my_symbol=PositionGetString(POSITION_SYMBOL);

            if(position_type==POSITION_TYPE_SELL && simb==my_symbol && Magic==order_magic)
              {
               trade.PositionClose(ticket);
               sell_total--;
              }
           }
        }
     }
  }
//+------------------------------------------------------------------+
//|                            SMA from AO                           |
//+------------------------------------------------------------------+
double SMA(double &price[],int period,int index)
  {
   double out=0;

   for(int i=0;i<period;i++)
     {
      out+=price[i+index];
     }
   return (NormalizeDouble(out/period,_Digits));
  }
//+------------------------------------------------------------------+
//|                              AO_calculate                        |
//+------------------------------------------------------------------+
void AO_calculate(double &median[],int period_5,int period_34,double &out[],int size)
  {
   for(int i=0;i<size;i++)
     {
      out[i]=SMA(median,period_5,i)-SMA(median,period_34,i);
     }

  }
//+------------------------------------------------------------------+
//|                        Median                                    |
//+------------------------------------------------------------------+
void Median(double &high[],double &low[],double &out[],int size)
  {
   for(int i=0;i<size;i++)
     {
      out[i]=(high[i]+low[i])/2;
     }
  }