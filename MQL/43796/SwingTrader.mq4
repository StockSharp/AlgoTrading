//+------------------------------------------------------------------+
//|                                                  SwingTrader.mq4 |
//|                        Copyright 2021, MetaQuotes Software Corp. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2021, MetaQuotes Software Corp."
#property link      "https://www.mql5.com"
#property version   "1.00"
#property strict
input double Tp = 0.05;
input double Multiplier = 1.5;
input int MaPeriod = 20;
double Resistance, Support, MiddleBand, Atr, Diff, Lot, Resistance2, Support2, MiddleBand2;
double SavedPrice, TakeProfit, StopLoss;
bool BuySignal, SellSignal, Uptouch, Downtouch, BuySignal2, SellSignal2;
int TimeFrame, i, Ticket;
int OnInit()
  {
//---
      TimeFrame = PERIOD_CURRENT;
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
  if (IsNewCandle())
  {
      Resistance = iBands(Symbol(),TimeFrame,MaPeriod,2,0,PRICE_CLOSE,1,1);
      MiddleBand = iBands(Symbol(),TimeFrame,MaPeriod,2,0,PRICE_CLOSE,0,1);
      Support = iBands(Symbol(),TimeFrame,MaPeriod,2,0,PRICE_CLOSE,2,1);
      Resistance2 = iBands(Symbol(),TimeFrame,MaPeriod,2,0,PRICE_CLOSE,1,2);
      MiddleBand2 = iBands(Symbol(),TimeFrame,MaPeriod,2,0,PRICE_CLOSE,0,2);
      Support2 = iBands(Symbol(),TimeFrame,MaPeriod,2,0,PRICE_CLOSE,2,2);
      if (!Uptouch && !Downtouch){
         Uptouch = (High[1] > Resistance);
         Downtouch = (Low[1] < Support);
         }
         if (Uptouch) {
         Downtouch = ((Low[1] < Support));
         Uptouch = !Downtouch;
         }
         if (Downtouch) {
         Uptouch = (High[1] > Resistance);
         Downtouch = !Uptouch;
         }
      BuySignal = Downtouch &&  (Close[0] > MiddleBand) && (Open[1] < MiddleBand);
      SellSignal = Uptouch &&  (Close[0] < MiddleBand) && (Open[1] > MiddleBand);
      BuySignal2 = Close[1] > Resistance && Close[2] > Resistance2;
      SellSignal2 = Close[1] < Support && Close[2] < Support2;
       if (BuySignal && OrdersTotal() < 1)
         {  Diff = Resistance - Support;
            SavedPrice = Ask;
            Lot = Round(CalcMaxLot(Symbol(),Bid)/12,2);
            if(CheckVolumeValue(Lot))
            {  Ticket = OrderSend(Symbol(),OP_BUY,Lot,Ask,500,NULL,NULL,"This is a buy", 4200, 0, clrGreen);
               i = 1;
            }
            
         }
         if (SellSignal && OrdersTotal() < 1)
         {  Diff = Resistance - Support;
            SavedPrice = Bid;
            Lot = Round(CalcMaxLot(Symbol(),Bid)/12,2);
            if(CheckVolumeValue(Lot))
            {
               Ticket = OrderSend(Symbol(),OP_SELL,Lot,Bid,500,NULL,NULL,"This is a sell", 4200, 0, clrRed);
               i = 1;
            }
         }
      
  
  }
   if (OrdersTotal() >=1)
      {     
            if (OrderSelect(Ticket,SELECT_BY_TICKET))
            
            {
               if (OrderType() == OP_BUY)
                  if (Ask <= SavedPrice - i*Diff && CheckVolumeValue(Round(Lot*MathPow(Multiplier,i),2)))
                  {OrderSend(Symbol(),OP_BUY,Lot*MathPow(Multiplier,i),Ask,500,NULL,NULL,"This is a buy", 4200, 0, clrGreen);
                  i++;
                  }
               if (OrderType() == OP_SELL)
                  if (Bid >= SavedPrice + i*Diff && CheckVolumeValue(Round(Lot*MathPow(Multiplier,i),2)))
                  {OrderSend(Symbol(),OP_SELL,Lot*MathPow(Multiplier,i),Bid,500,NULL,NULL,"This is a sell", 4200, 0, clrGreen);
                  i++;
                  }
            
            }
            
            
            if (OpenOrderProfits() > TotalInvested() * Tp)
            {
            
            Ticket = 0;
            CloseAllOrders();
            
            }
            if (OpenOrderProfits() < -TotalInvested() * 10*Tp)   
            {
           
            Ticket = 0;
            CloseAllOrders();
            
            }
      }
  }
  
  
double CalcMaxLot(string symbol, double Price)
{
double Maxlot;
Maxlot = AccountFreeMargin()*30*MarketInfo(symbol,MODE_TICKSIZE)/MarketInfo(symbol,MODE_TICKVALUE)/Price;
if (MarketInfo(Symbol(),MODE_MINLOT) == 0.1)
   {  
   double Maxlot2 = Round(Maxlot,1);
   if (Maxlot2 >= Maxlot) return Maxlot2-0.1;
   else return Maxlot2;
   }                                
else 
   {  
   double Maxlot2 = Round(Maxlot,2);
   if (Maxlot2 >= Maxlot) return Maxlot2-0.01;
   else return Maxlot2;
   }
}


double OpenOrderProfits()
{
   double cnt = 0;
   for (int i = OrdersTotal(); i >= 0; i--)
   {
      if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES))
      if (OrderType() < 3)
      cnt += OrderProfit();

   }
   return cnt;
}


double TotalInvested()
{
   double Lots = TotalOrderLots();
   return Lots*OrderOpenPrice()/MarketInfo(OrderSymbol(),MODE_TICKSIZE)*MarketInfo(OrderSymbol(),MODE_TICKVALUE)/30;
}

double TotalOrderLots()
{  double cnt = 0;
   for (int i = OrdersTotal()-1 ; i >=0 ; i --)
   {
      if (OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
      {
         cnt = cnt+ OrderLots();
      }
        
   
   }
   return cnt;
}

double Round(double value, int decimals)
{     
   double timesten, truevalue;
   if (decimals < 0) 
      {  
         Print("Wrong decimals input parameter, paramater cant be below 0");
         return 0;
      }
   timesten = value * MathPow(10,decimals);
   timesten = MathRound(timesten);
   truevalue = timesten/MathPow(10,decimals);
   return truevalue;
}

bool IsNewCandle()
{
   static datetime saved_candle_time;
   if(Time[0] == saved_candle_time)
      return false;
   else
      saved_candle_time = Time[0];
   return true;
}

bool CloseAllOrders()
{

   int i = OrdersTotal()-1;
   for(i; i >= 0;i--)
   {
      if (OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
      {
         if (OrderType() <= 1)
         { 
            if(!OrderClose(OrderTicket(),OrderLots(),MarketInfo(OrderSymbol(),MODE_ASK), 5000, clrAqua)) Print("Can't close all orders" + IntegerToString(GetLastError()));
         }
         else 
         {
            if (!OrderDelete(OrderTicket())) Print("Can't close all orders" + IntegerToString(GetLastError()));
         }
      }
   }
   if (OrdersTotal() == 0) return true;
   return false;
}

bool CheckVolumeValue(double volume)
  {
//--- minimal allowed volume for trade operations
   double min_volume=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MIN);
   if(volume<min_volume)
     {
      return(false);
     }

//--- maximal allowed volume of trade operations
   double max_volume=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MAX);
   if(volume>max_volume)
     {
      return(false);
     }

//--- get minimal step of volume changing
   double volume_step=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_STEP);

   int ratio=(int)MathRound(volume/volume_step);
   if(MathAbs(ratio*volume_step-volume)>0.0000001)
     {
      return(false);
     }

   return(true);
  }
