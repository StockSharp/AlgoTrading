//+---------------------------------------------------------------------------+
//|                                                           ChaosTrader.mq4 |
//|                                                                 Jay Davis |
//|                                                  https://www.tidyneat.com |
//+---------------------------------------------------------------------------+
#property copyright "Jay Davis"
#property link      "https://www.tidyneat.com"
#property version   "1.00"
#property strict

// Awesome Indicator
#define PERIOD_FAST  5
#define PERIOD_SLOW 34
input int magnitude=10; // Pips of distance from mouth needed
input bool UseFirstWiseMan=true;
input bool UseSecondWiseMan= true;
input bool UseThirdWiseMan = true;
input double lotSize=0.01; // What size for your orders
input int magic=1010101; // magic number
string volumeString="";
double stoploss=0;
int InpJawsPeriod=13; // Jaws Period
int InpJawsShift=8;   // Jaws Shift
int InpTeethPeriod=8; // Teeth Period
int InpTeethShift=5;  // Teeth Shift
int InpLipsPeriod=5;  // Lips Period
int InpLipsShift=3;   // Lips Shift
datetime expiry = TimeCurrent();
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
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
   static datetime candletime=0;
   if(candletime!=Time[0])
     {
      expiry=TimeCurrent()+PeriodSeconds(PERIOD_CURRENT);
      if(UseFirstWiseMan) FirstWiseMan();
      if(UseSecondWiseMan)SecondWiseMan();
      if(UseThirdWiseMan)ThirdWiseMan();
      candletime=Time[0];
     }

  }

//+------------------------------------------------------------------+
//| Check if another order can be placed                             |
//+------------------------------------------------------------------+
bool IsNewOrderAllowed()
  {
//--- get the number of pending orders allowed on the account
   int max_allowed_orders=(int)AccountInfoInteger(ACCOUNT_LIMIT_ORDERS);

//--- if there is no limitation, return true; you can send an order
   if(max_allowed_orders==0) return(true);

//--- if we passed to this line, then there is a limitation; find out how many orders are already placed
   int orders=OrdersTotal();

//--- return the result of comparing
   return(orders<max_allowed_orders);
  }
//+------------------------------------------------------------------+
//| Check money for trade                                            |
//+------------------------------------------------------------------+
bool CheckMoneyForTrade(string symb,double lots,int type)
  {
   double free_margin=AccountFreeMarginCheck(symb,type,lots);
//-- if there is not enough money
   if(free_margin<0)
     {
      string oper=(type==OP_BUY)? "Buy":"Sell";
      Print("Not enough money for ",oper," ",lots," ",symb," Error code=",GetLastError());
      return(false);
     }
//--- checking successful
   return(true);
  }
//+------------------------------------------------------------------+
//| First Wise Man calculations                                      |
//+------------------------------------------------------------------+
bool FirstWiseMan()
  {

   if(BullishDivergent(1)==true && 10*Magnitude(1)/Point>magnitude*10)
     {
      if(AccountFreeMarginCheck(Symbol(),OP_BUY,0.01)>0)
        {
         if(CheckVolumeValue(lotSize,volumeString) && CheckMoneyForTrade(NULL,lotSize,OP_BUY)
            && IsNewOrderAllowed())
           {
            PlaceBuystop();
           }
         else
           {
            Print(volumeString);
           }
        }
     }
   if(BearishDivergent(1)==true && 10*Magnitude(1)>10*magnitude*Point)
     {
      if(AccountFreeMarginCheck(NULL,OP_SELL,0.01)>0)
        {
         if(CheckVolumeValue(0.01,volumeString) && CheckMoneyForTrade(NULL,lotSize,OP_SELL)
            && IsNewOrderAllowed())
           {
            PlaceSellstop();
           }
         else
           {
            Print(volumeString);
           }
        }
     }
   return false;
  }
//+------------------------------------------------------------------+
//| Place BUYSTOP                                                    |
//+------------------------------------------------------------------+
bool PlaceBuystop()
  {
   expiry=TimeCurrent()+PeriodSeconds(PERIOD_CURRENT);
//---
   double
/*
      At placing of a pending order, the open price cannot be too close to the market. 
      The minimal distance of the pending price from the current market one in points 
      can be obtained using the MarketInfo() function with the MODE_STOPLEVEL parameter. 
      In case of false open price of a pending order, the error 130 (ERR_INVALID_STOPS) 
      will be generated.
      */
   high=iHigh(NULL,PERIOD_CURRENT,1),
   low=iLow(NULL,PERIOD_CURRENT,1),
   stopLevel = MarketInfo(NULL,MODE_STOPLEVEL)*Point,
   openLevel = 0;
   openLevel=high+Point;
   if(openLevel-Ask<stopLevel) openLevel=Ask+stopLevel+Point;
   stoploss=low-Point;
   CloseAll(OP_SELL);
   SetStoplosses(OP_BUY,stoploss);

   if(CheckVolumeValue(lotSize,volumeString) && CheckMoneyForTrade(NULL,lotSize,OP_BUY)
      && IsNewOrderAllowed())
     {
      if(OrderSend(NULL,OP_BUYSTOP,lotSize,openLevel,10,stoploss,0,"first wise man",magic,expiry,clrGreen))
        {
         //Print("Successfully sent BUYSTOP order, orders total = ",OrdersTotal());
         return true;
        }
     }
   else
     {
      Print(volumeString);
     }
   return false;
  }
//+------------------------------------------------------------------+
//| Place Sellstop                                                   |
//+------------------------------------------------------------------+
bool PlaceSellstop()
  {
   expiry=TimeCurrent()+PeriodSeconds(PERIOD_CURRENT);
//---
   double
//jaw=iMA(NULL,0,InpJawsPeriod,InpJawsShift,MODE_SMMA,PRICE_MEDIAN,1),
//teeth=iMA(NULL,0,InpTeethPeriod,InpTeethShift,MODE_SMMA,PRICE_MEDIAN,1),
//lips=iMA(NULL,0,InpLipsPeriod,InpLipsShift,MODE_SMMA,PRICE_MEDIAN,1),
/*
      At placing of a pending order, the open price cannot be too close to the market. 
      The minimal distance of the pending price from the current market one in points 
      can be obtained using the MarketInfo() function with the MODE_STOPLEVEL parameter. 
      In case of false open price of a pending order, the error 130 (ERR_INVALID_STOPS) 
      will be generated.
      */
   high=iHigh(NULL,PERIOD_CURRENT,1),
   low=iLow(NULL,PERIOD_CURRENT,1),
   stopLevel = MarketInfo(NULL,MODE_STOPLEVEL)*Point,
   openLevel = 0;
   openLevel=low-Point;
   if(Bid-openLevel<stopLevel) openLevel=Bid-stopLevel-Point;
   stoploss=high+Point;
   CloseAll(OP_BUY);
   SetStoplosses(OP_SELL,stoploss);
   if(AccountFreeMarginCheck(NULL,OP_SELL,0.01)>0)
     {
      if(CheckVolumeValue(0.01,volumeString) && CheckMoneyForTrade(NULL,lotSize,OP_SELL)
         && IsNewOrderAllowed())
        {
         if(OrderSend(NULL,OP_SELLSTOP,lotSize,openLevel,10,stoploss,0,"first wise man",magic,expiry,clrRosyBrown))
           {
            //Print("Successfully sent SELLSTOP order, orders total = ",OrdersTotal());
            return true;
           }
        }
      else
        {
         Print(volumeString);
        }
     }
   return false;
  }
//+------------------------------------------------------------------+
//| Second Wise Man calculations                                     |
//+------------------------------------------------------------------+
bool SecondWiseMan()
  {
// Add on only if first wise man has occured
   double
   high=iHigh(NULL,PERIOD_CURRENT,1),
   low=iLow(NULL,PERIOD_CURRENT,1),
   stopLevel = MarketInfo(NULL,MODE_STOPLEVEL)*Point,
   openLevel = 0,
   bar2=0.0,
   bar3=0.0,
   bar4=0.0,
   bar5=0.0,
   current;

   current=iMA(NULL,0,PERIOD_FAST,0,MODE_SMA,PRICE_MEDIAN,1)-
           iMA(NULL,0,PERIOD_SLOW,0,MODE_SMA,PRICE_MEDIAN,1);
   bar2=iMA(NULL,0,PERIOD_FAST,0,MODE_SMA,PRICE_MEDIAN,2)-
        iMA(NULL,0,PERIOD_SLOW,0,MODE_SMA,PRICE_MEDIAN,2);
   bar3=iMA(NULL,0,PERIOD_FAST,0,MODE_SMA,PRICE_MEDIAN,3)-
        iMA(NULL,0,PERIOD_SLOW,0,MODE_SMA,PRICE_MEDIAN,3);
   bar4=iMA(NULL,0,PERIOD_FAST,0,MODE_SMA,PRICE_MEDIAN,4)-
        iMA(NULL,0,PERIOD_SLOW,0,MODE_SMA,PRICE_MEDIAN,4);
   bar5=iMA(NULL,0,PERIOD_FAST,0,MODE_SMA,PRICE_MEDIAN,5)-
        iMA(NULL,0,PERIOD_SLOW,0,MODE_SMA,PRICE_MEDIAN,5);
   if(current>bar2 && bar2>bar3 && bar3>bar4 && bar4<bar5)
     {
      if(AccountFreeMarginCheck(Symbol(),OP_BUY,0.01)>0)
        {

         if(CheckVolumeValue(lotSize,volumeString) && CheckMoneyForTrade(NULL,lotSize,OP_BUY)
            && IsNewOrderAllowed())
           {
            PlaceBuystop();
           }
         else
           {
            Print(volumeString);
           }
        }
     }
   if(current<bar2 && bar2<bar3 && bar3<bar4 && bar4>bar5)
     {
      if(AccountFreeMarginCheck(NULL,OP_SELL,0.01)>0)
        {
         if(CheckVolumeValue(0.01,volumeString) && CheckMoneyForTrade(NULL,lotSize,OP_SELL)
            && IsNewOrderAllowed())
           {
            PlaceSellstop();
           }
         else
           {
            Print(volumeString);
           }
        }
     }
   return false;
  }
//+------------------------------------------------------------------+
//| Calculations for the thrid and final wise man                    |
//+------------------------------------------------------------------+
bool ThirdWiseMan()
  {
   double
   teeth=iMA(NULL,0,InpTeethPeriod,InpTeethShift,MODE_SMMA,PRICE_MEDIAN,1),
   high=iHigh(NULL,PERIOD_CURRENT,1),
   low=iLow(NULL,PERIOD_CURRENT,1),
   stopLevel = MarketInfo(NULL,MODE_STOPLEVEL)*Point,
   openLevel = 0,
   upper=iFractals(NULL,PERIOD_CURRENT,MODE_UPPER,2),
   lower=iFractals(NULL,PERIOD_CURRENT,MODE_LOWER,2);
   if(upper!=NULL && Ask>teeth+magnitude*10*Point)
     {
      if(AccountFreeMarginCheck(Symbol(),OP_BUY,0.01)>0)
        {

         if(CheckVolumeValue(lotSize,volumeString) && CheckMoneyForTrade(NULL,lotSize,OP_BUY)
            && IsNewOrderAllowed())
           {
            PlaceBuystop();
           }
         else
           {
            Print(volumeString);
           }
        }
     }

   if(lower!=NULL && Bid<teeth-magnitude*10*Point)
     {
      if(AccountFreeMarginCheck(NULL,OP_SELL,0.01)>0)
        {
         if(CheckVolumeValue(0.01,volumeString) && CheckMoneyForTrade(NULL,lotSize,OP_SELL)
            && IsNewOrderAllowed())
           {
            PlaceSellstop();
           }
         else
           {
            Print(volumeString);
           }
        }
     }

   return false;
  }
//+------------------------------------------------------------------+
//| Close all of a particular type of order                          |
//+------------------------------------------------------------------+
void CloseAll(int type)
  {
   for(int i=OrdersTotal()-1; i>=0; i--)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
        {
         if(OrderMagicNumber()==magic)
           {
            if(OrderType()==type)
              {
               double closingPrice=NULL;
               if(type==OP_BUY)closingPrice=Bid;
               if(type==OP_SELL)closingPrice=Ask;
               if(OrderClose(OrderTicket(),OrderLots(),closingPrice,10,clrAzure))
                 {
                  Print("Order Closed");
                 }
              }
           }
        }
     }
  }
//+------------------------------------------------------------------+
//| A bullish divergent bar is a bar that has a lower low and closes |
//| in the top half of the bar.                                      |
//| Integer --> Boolean                                              |
//+------------------------------------------------------------------+
bool BullishDivergent(int bar)
  {
   double low=iLow(NULL,PERIOD_CURRENT,bar),
   median=(iHigh(NULL,PERIOD_CURRENT,bar)+low)/2;
   if(low<iLow(NULL,PERIOD_CURRENT,bar+1)
      && iClose(NULL,PERIOD_CURRENT,bar)>median)
     {
      return true;
     }
   return false;
  }
//+------------------------------------------------------------------+
//| A bearish divergent bar is a bar that has a higher high and      |
//| closes in the bottom half of the bar.                            |
//| Integer --> Boolean                                              |
//+------------------------------------------------------------------+
bool BearishDivergent(int bar)
  {
   double high=iHigh(NULL,PERIOD_CURRENT,bar),
   median=(iLow(NULL,PERIOD_CURRENT,bar)+high)/2;
   if(high>iHigh(NULL,PERIOD_CURRENT,bar+1)
      && iClose(NULL,PERIOD_CURRENT,bar)<median)
     {
      return true;
     }
   return false;
  }
//+------------------------------------------------------------------+
//| This function determines the magnitude which is the distance     |
//| between the low of the bearish divergent bar and the lips of the |
//| Alligator, this is vice versa for a bullish divergent bar        |
//| Integer --> Double                                               |
//+------------------------------------------------------------------+
double Magnitude(int bar)
  {
   double mag=0,
   magLips=iMA(NULL,0,InpLipsPeriod,InpLipsShift,MODE_SMMA,PRICE_MEDIAN,bar),
   high= iHigh(NULL,PERIOD_CURRENT,bar),
   low = iLow(NULL,PERIOD_CURRENT,bar);
   if(BullishDivergent(bar)==true)
     {
      mag=magLips-high;
     }
   if(BearishDivergent(bar)==true)
     {
      mag=low-magLips;
     }
   return mag;
  }
//+------------------------------------------------------------------+
/* On a bullish divergent bar, we place the buy stop just above the 
top of the bullish divergent bar (see B, Figure 9.1) and on the 
bearish divergent bar we place our sell stop just below the bottom of
 the bearish divergent bar
Gregory-Williams, Justine. Trading Chaos: Maximize Profits with Proven 
Technical Techniques (A Marketplace Book) (Kindle Locations 2852-2854). 
Wiley. Kindle Edition. 
*/
//+------------------------------------------------------------------+
//| Set stoplosses                                                   |
//| Integer                                                          |
//+------------------------------------------------------------------+
void SetStoplosses(int type,double stopLoss)
  {
//Print(__FUNCTION__);
   double    stopLevel=MarketInfo(NULL,MODE_STOPLEVEL)*Point;
   int total = OrdersTotal();
   for(int i = total; i >= 0; i--)
     {
      // Select order to work with
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
        {
         // Check if order magic number matches
         if(OrderMagicNumber()==magic)
           {
            if(OrderType()==type && type==OP_BUY)
              {
               if(OrderStopLoss()<stopLoss && stopLoss<Bid-stopLevel)
                 {
                  // Set new stoploss
                  if(OrderModify(OrderTicket(),OrderOpenPrice(),stopLoss,0,0,clrAliceBlue))
                    {
                     Print("Stoploss moved on #",OrderTicket()," to ",stopLoss);
                    }
                 }
              }
            if(OrderType()==type && type==OP_SELL)
              {
               if(OrderStopLoss()>stopLoss && stopLoss>Ask+stopLevel)
                 {
                  // Set new stoploss
                  if(OrderModify(OrderTicket(),OrderOpenPrice(),stopLoss,0,0,clrAliceBlue))
                    {
                     Print("Stoploss moved on #",OrderTicket()," to ",stopLoss);
                    }
                 }
              }
           }
        }
     }
  }
//+------------------------------------------------------------------+
//| Check the correctness of the order volume                        |
//+------------------------------------------------------------------+
bool CheckVolumeValue(double volume,string &description)
  {
//--- minimal allowed volume for trade operations
   double min_volume=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MIN);
   if(volume<min_volume)
     {
      description=StringFormat("Volume is less than the minimal allowed SYMBOL_VOLUME_MIN=%.2f",min_volume);
      return(false);
     }

//--- maximal allowed volume of trade operations
   double max_volume=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MAX);
   if(volume>max_volume)
     {
      description=StringFormat("Volume is greater than the maximal allowed SYMBOL_VOLUME_MAX=%.2f",max_volume);
      return(false);
     }

//--- get minimal step of volume changing
   double volume_step=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_STEP);

   int ratio=(int)MathRound(volume/volume_step);
   if(MathAbs(ratio*volume_step-volume)>0.0000001)
     {
      description=StringFormat("Volume is not a multiple of the minimal step SYMBOL_VOLUME_STEP=%.2f, the closest correct volume is %.2f",
                               volume_step,ratio*volume_step);
      return(false);
     }
   description="Correct volume value";
   return(true);
  }
//+------------------------------------------------------------------+
