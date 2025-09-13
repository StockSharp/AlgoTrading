////////////////////////////////////////////////////////////////////
//
// MCB ScalpWiz 9001
// Default config is for EURUSD M1
//
//
#property copyright "Copyright © 2015, MCB Shenannegans."
#property link      "marchaxcpp@gmail.com"

extern int PercentRisk         = 2;
extern int TakeProfit          = 500;
extern int TrailingStop        = 15;
extern int OpenStopLoss        = 500;
extern int Slippage            = 3;
extern int ExpirationInMinutes = 10;
extern bool UseStops           = true;
extern string comment          = "ScalpWiz9001";

extern double Level1Pips = 100.0;
extern double Level2Pips = 120.0;
extern double Level3Pips = 150.0;
extern double Level4Pips = 200.0;

extern int StengthLevel1Multiplier = 1;
extern int StengthLevel2Multiplier = 2;
extern int StengthLevel3Multiplier = 3;
extern int StengthLevel4Multiplier = 4;
extern string msg="WARNING: optimized for EURUSD.";

int BandsShift=0;
double BarsLevel1Long[];
double BarsLevel2Long[];
double BarsLevel3Long[];
double BarsLevel4Long[];
double BarsLevel1Short[];
double BarsLevel2Short[];
double BarsLevel3Short[];
double BarsLevel4Short[];

int MagicNumber =         91827364;
int BandsPeriod = 30;
int BandsDeviations=2;

double RSI=0;
int StopLevel=0;
datetime ExpDate;
//////////////////////////////////////////////////////////////////////
//
// gets lot size for percentage of account you want to risk
//
double PercentPurchase(double price,double balance,int istrength)
  {
   double dRetVal;
   double dLotPrice=price*1000;
   double percentage=balance *((istrength*PercentRisk)*0.01);
   percentage=NormalizeDouble(percentage,2);
   dRetVal=NormalizeDouble((percentage/dLotPrice),2);
   if(dRetVal < .01)
      dRetVal=.01;

   return dRetVal;
  }
///////////////////////////////////////////////////////////////////////
//
// Opens buy/buy stop and multiplies lot size based on signal strength
//
bool OpenSingleBuy(int strength)
  {

   int ticket=-1;

   RefreshRates();
   double dLots=PercentPurchase(Ask,AccountBalance(),strength);

   if(UseStops)
      ticket=OrderSend(Symbol(),OP_BUYSTOP,dLots,Ask+StopLevel*Point,Slippage,Ask-OpenStopLoss*Point,Ask+TakeProfit*Point,comment+" buy @ RSI:"+StringFormat("%.2f",RSI),MagicNumber,ExpDate,Lime);
   else
      ticket=OrderSend(Symbol(),OP_BUY,dLots,Ask,Slippage,Ask-OpenStopLoss*Point,Ask+TakeProfit*Point,comment+" buy @ RSI:"+StringFormat("%.2f",RSI),MagicNumber,ExpDate,Lime);

   if(ticket<0)
     {
      Print("Failed to OpenSingleBuy, error # ",GetLastError());
      return (false);
     }
   else
     {
      PlaySound("alert2.wav");
      Print("Successfully placed order with OpenSingleBuy");
      return (true);
     }
   return (false);
  }
///////////////////////////////////////////////////////////////////////
//
// Opens sell/sell stop and multiplies lot size based on signal strength
//
bool OpenSingleSell(int strength)
  {
   int ticket=-1;
   while(!IsTradeAllowed()) Sleep(MathRand()/10);
   RefreshRates();
   double dLots=PercentPurchase(Bid,AccountBalance(),strength);
   if(UseStops)
      ticket=OrderSend(Symbol(),OP_SELLSTOP,dLots,Bid-StopLevel*Point,Slippage,Bid+OpenStopLoss*Point,Bid-TakeProfit*Point,comment+" sell @ RSI:"+StringFormat("%.2f",RSI),MagicNumber,ExpDate,Lime);
   else
      ticket=OrderSend(Symbol(),OP_SELL,dLots,Bid,Slippage,Bid+OpenStopLoss*Point,Bid-TakeProfit*Point,comment+" sell @ RSI:"+StringFormat("%.2f",RSI),MagicNumber,ExpDate,Lime);

   if(ticket<0) 
     {
      Print("Failed to OpenSingleSell, error # ",GetLastError());
      return (false);
        } else {
      PlaySound("alert2.wav");
      Print("Successfully placed order with OpenSingleSell");
      return (true);
     }
   return (false);
  }
///////////////////////////////////////////////////////////////////////////////
//
//
int init() 
  {
   SetIndexStyle(0,DRAW_ARROW,EMPTY,1);
   SetIndexBuffer(0,BarsLevel1Long);
   SetIndexArrow(0,140);
   SetIndexStyle(1,DRAW_ARROW,EMPTY,1);
   SetIndexBuffer(1,BarsLevel2Long);
   SetIndexArrow(1,141);
   SetIndexStyle(2,DRAW_ARROW,EMPTY,1);
   SetIndexBuffer(2,BarsLevel3Long);
   SetIndexArrow(2,142);
   SetIndexStyle(3,DRAW_ARROW,EMPTY,1);
   SetIndexBuffer(3,BarsLevel4Long);
   SetIndexArrow(3,143);
   SetIndexStyle(4,DRAW_ARROW,EMPTY,1);
   SetIndexBuffer(4,BarsLevel1Short);
   SetIndexArrow(4,140);
   SetIndexStyle(5,DRAW_ARROW,EMPTY,1);
   SetIndexBuffer(5,BarsLevel2Short);
   SetIndexArrow(5,141);
   SetIndexStyle(6,DRAW_ARROW,EMPTY,1);
   SetIndexBuffer(6,BarsLevel3Short);
   SetIndexArrow(6,142);
   SetIndexStyle(7,DRAW_ARROW,EMPTY,1);
   SetIndexBuffer(7,BarsLevel4Short);
   SetIndexArrow(7,143);
   SetIndexDrawBegin(0,BandsPeriod+BandsShift);
   SetIndexDrawBegin(1,BandsPeriod+BandsShift);
   SetIndexDrawBegin(2,BandsPeriod+BandsShift);
   SetIndexDrawBegin(3,BandsPeriod+BandsShift);
   SetIndexDrawBegin(4,BandsPeriod+BandsShift);
   SetIndexDrawBegin(5,BandsPeriod+BandsShift);
   SetIndexDrawBegin(6,BandsPeriod+BandsShift);
   SetIndexDrawBegin(7,BandsPeriod+BandsShift);
   ObjectCreate("lblTop",OBJ_LABEL,0,0,0,0,0);
   ObjectCreate("lblBottom",OBJ_LABEL,0,0,0,0,0);
   return (0);
  }
///////////////////////////////////////////////////////////////////////////////
//
//
int deinit()
  {
   ObjectDelete("lblTop");
   ObjectDelete("lblBottom");
   return (0);
  }
///////////////////////////////////////////////////////////////////////////////
//
//
int start()
  {
   ExpDate = TimeCurrent();
   ExpDate+= TimeMinute(ExpDate) + ExpirationInMinutes * 60;
   StopLevel=MarketInfo(Symbol(),MODE_STOPLEVEL);
   double BandsUpper;
   double BandsLower;
   ObjectSet("lblTop",OBJPROP_COLOR,Black);
   ObjectSet("lblTop",OBJPROP_CORNER,2);
   ObjectSet("lblTop",OBJPROP_XDISTANCE,3);
   ObjectSet("lblTop",OBJPROP_YDISTANCE,3);
   ObjectSet("lblBottom",OBJPROP_COLOR,Black);
   ObjectSet("lblBottom",OBJPROP_CORNER,2);
   ObjectSet("lblBottom",OBJPROP_XDISTANCE,3);
   ObjectSet("lblBottom",OBJPROP_YDISTANCE,23);
   int total=OrdersTotal();
   int i = 0;
   for(i = 0; i < total; i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
        {
         if(Bid-OrderOpenPrice()>Point*TrailingStop)
           {
            if(OrderStopLoss()<Bid-Point*TrailingStop)
              {
               if(!OrderModify(OrderTicket(),
                  OrderOpenPrice(),
                  Bid-Point*TrailingStop,
                  Ask+OrderTakeProfit()*Point,
                  0,Green))
                  Print("Error modifying ticket "+OrderTicket());
              }
           }
         if((OrderOpenPrice()-Ask)>(Point*TrailingStop))
           {
            if((OrderStopLoss()>(Ask+Point*TrailingStop)) || (OrderStopLoss()==0))
              {

               if(!OrderModify(OrderTicket(),
                  OrderOpenPrice(),
                  Ask+Point*TrailingStop,
                  Bid-OrderTakeProfit()*Point,
                  0,
                  Red))
                  Print("Error modifying ticket "+OrderTicket());
              }
           }
        }
     }

   int ind_counted_0=IndicatorCounted();
   if(Period()!=PERIOD_M1)
      ObjectSetText("lblBottom","Attach to M1 chart",12,"Arial",Black);
   else
     {
      ObjectSetText("lblBottom","Reverse Scalping Signal : None",12,"Arial",Black);
      for(int BarsShift=Bars-ind_counted_0-1; BarsShift>=0; BarsShift--)
        {
         BandsUpper = iBands(NULL, 0, BandsPeriod, BandsDeviations, BandsShift, PRICE_CLOSE, MODE_UPPER, BarsShift);
         BandsLower = iBands(NULL, 0, BandsPeriod, BandsDeviations, BandsShift, PRICE_CLOSE, MODE_LOWER, BarsShift);
         if(Close[BarsShift]-BandsUpper>Level4Pips*Point)
           {
            BarsLevel4Short[BarsShift]=BandsUpper+Level4Pips*Point;
            if(BarsShift==0)
              {
               ObjectSetText("lblBottom","Signal: Short     Strength: 4",12,"Arial",Black);
               OpenSingleSell(StengthLevel4Multiplier);
              }
           }
         else
           {
            if(Close[BarsShift]-BandsUpper>Level3Pips*Point)
              {
               BarsLevel3Short[BarsShift]=BandsUpper+Level3Pips*Point;
               if(BarsShift==0)
                 {
                  ObjectSetText("lblBottom","Signal: Short     Strength: 3",12,"Arial",Black);
                  OpenSingleSell(StengthLevel3Multiplier);
                 }
              }
            else
              {
               if(Close[BarsShift]-BandsUpper>Level2Pips*Point)
                 {
                  BarsLevel2Short[BarsShift]=BandsUpper+Level2Pips*Point;
                  if(BarsShift==0)
                    {
                     ObjectSetText("lblBottom","Signal: Short     Strength: 2 (of 4)",12,"Arial",Black);
                     OpenSingleSell(StengthLevel2Multiplier);
                    }
                 }
               else
                 {
                  if(Close[BarsShift]-BandsUpper>Level1Pips*Point)
                    {
                     BarsLevel1Short[BarsShift]=BandsUpper+Level1Pips*Point;
                     if(BarsShift==0)
                       {
                        ObjectSetText("lblBottom","Reverse Scalping Signal: Short     Strength: 1",12,"Arial",Black);
                        OpenSingleSell(StengthLevel1Multiplier);
                       }
                    }
                  else
                    {
                     if(BandsLower-Close[BarsShift]>Level4Pips*Point)
                       {
                        BarsLevel4Long[BarsShift]=BandsLower-Level4Pips*Point;
                        if(BarsShift==0)
                          {
                           ObjectSetText("lblBottom","Signal: Long      Strength: 4",12,"Arial",Black);
                           OpenSingleBuy(StengthLevel4Multiplier);
                          }
                       }
                     else
                       {
                        if(BandsLower-Close[BarsShift]>Level3Pips*Point)
                          {
                           BarsLevel3Long[BarsShift]=BandsLower-Level3Pips*Point;
                           if(BarsShift==0)
                             {
                              ObjectSetText("lblBottom","Signal: Long      Strength: 3",12,"Arial",Black);
                              OpenSingleBuy(StengthLevel3Multiplier);
                             }
                          }
                        else
                          {
                           if(BandsLower-Close[BarsShift]>Level2Pips*Point)
                             {
                              BarsLevel2Long[BarsShift]=BandsLower-Level2Pips*Point;
                              if(BarsShift==0)
                                {
                                 ObjectSetText("lblBottom","Signal: Long      Strength: 2",12,"Arial",Black);
                                 OpenSingleBuy(StengthLevel2Multiplier);
                                }
                             }
                           else
                             {
                              if(BandsLower-Close[BarsShift]>Level1Pips*Point)
                                {
                                 BarsLevel1Long[BarsShift]=BandsLower-Level1Pips*Point;
                                 if(BarsShift==0)
                                   {
                                    ObjectSetText("lblBottom","Signal: Long      Strength: 1",12,"Arial",Black);
                                    OpenSingleBuy(StengthLevel1Multiplier);
                                   }
                                }
                             }
                          }
                       }
                    }
                 }
              }
           }
        }
     }
   if(StringFind(Symbol(),"EURUSD")==-1)
      ObjectSetText("lblTop",msg,10,"Arial",Black);
   else
      ObjectSetText("lblTop","",10,"Arial",White);
   return (0);
  }
//+------------------------------------------------------------------+
