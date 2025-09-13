/*

This expert implements the method described on page
http://www.tradejuice.com/forex/forex-1-2-3-methode-mm.htm.
This is not a canonical imlementation. This is just a try.
I run the expert on the H1-graph of EURJPY with positive but not
excellent results. The expert has one "disatvantage", I mean rare trades.

*/

//+------------------------------------------------------------------+
//|                                 Author of implementation: Martes |
//|               http://championship.mql4.com/2007/ru/users/Martes/ |
//+------------------------------------------------------------------+

#property copyright "Martes"
#property link      "http://championship.mql4.com/2007/ru/users/Martes/"

extern double TakeProfit = 60;
extern double Lots = 0.5;
extern double TrailingStop = 30;

//How much must the current trend be shorter than the previous trend.
extern double TrendRatio = 4;

double dStopLoss=0, dTakeProfit=0;
int MagicNumber=12301;


bool BuyCondition()
{
   bool Condition=false;
   int Point1Index, Point2Index, Point3Index;
   double CurrentLevel=Ask, Point1Level, Point2Level, Point3Level;
   double DownTrLen,UpTrLen;
   
   double MacdCurrent, MacdPrevious;
   double SignalCurrent, SignalPrevious;
   bool MacdSignalsToBuy;
   
   Point3Index=FindFirstValley(CurrentLevel, -10000,1,100);
   if(Point3Index==-1)
      return(false);
   else
      Point3Level=MathMin(Open[Point3Index],Close[Point3Index]);
   
   Point2Index=FindFirstPeak(CurrentLevel, Point3Level, Point3Index+1, 100);
   if(Point2Index==-1)
      return(false);
   else
      Point2Level=MathMax(Open[Point2Index],Close[Point2Index]);
   
   Point1Index=FindFirstValley(Point2Level, -10000,Point2Index+1,100);
   if(Point1Index==-1)
      return(false);
   else
      Point1Level=MathMin(Open[Point1Index],Close[Point1Index]);
   
   if(CurrentLevel-Point2Level>5*Point)
      return(false);//Too far from the potential entry level.
   
   MacdCurrent=iMACD(NULL,0,12,26,9,PRICE_CLOSE,MODE_MAIN,0);
   MacdPrevious=iMACD(NULL,0,12,26,9,PRICE_CLOSE,MODE_MAIN,1);
   SignalCurrent=iMACD(NULL,0,12,26,9,PRICE_CLOSE,MODE_SIGNAL,0);
   SignalPrevious=iMACD(NULL,0,12,26,9,PRICE_CLOSE,MODE_SIGNAL,1);
   MacdSignalsToBuy=((MacdCurrent>SignalCurrent && MacdPrevious<SignalPrevious) ||
                    (MacdCurrent>0 && MacdPrevious<0)) &&
                    iMACD(NULL,0,12,26,9,PRICE_CLOSE,MODE_MAIN,Point3Index)>0;
   if(!MacdSignalsToBuy)
      return(false);
   
   DownTrLen=iCustom(NULL, 0, "RelDownTrLen_forCodeBase_v01", 100, Point1Index, true, 0, 0);
   UpTrLen=iCustom(NULL, 0, "RelUpTrLen_forCodeBase_v01", Point1Index, 1, true, 0, 0);
   
   Condition=DownTrLen/(UpTrLen+0.001)>TrendRatio && //The down trend was longer enough.
             (Point2Level-Point3Level)/Point>13;//Potential stop loss far enough.
   
   if(Condition)
   {
      dStopLoss=Point3Level-Point;
      dTakeProfit=CurrentLevel+(Point2Level-Point3Level);
   }
   
   return(Condition);
}

bool SellCondition()
{
   bool Condition=false;
   int Point1Index, Point2Index, Point3Index;
   double CurrentLevel=Bid, Point1Level, Point2Level, Point3Level;
   double DownTrLen,UpTrLen;
   
   double MacdCurrent, MacdPrevious;
   double SignalCurrent, SignalPrevious;
   bool MacdSignalsToSell;
   
   Point3Index=FindFirstPeak(10000, CurrentLevel,1,100);
   if(Point3Index==-1)
      return(false);
   else
      Point3Level=MathMax(Open[Point3Index],Close[Point3Index]);
      
  
   Point2Index=FindFirstValley(Point3Level, CurrentLevel, Point3Index+1, 100);
   if(Point2Index==-1)
      return(false);
   else
      Point2Level=MathMin(Open[Point2Index],Close[Point2Index]);
   
   Point1Index=FindFirstPeak(10000, Point2Level,Point2Index+1,100);
   if(Point1Index==-1)
      return(false);
   else
      Point1Level=MathMax(Open[Point1Index],Close[Point1Index]);
   
   if(MathAbs(CurrentLevel-Point2Level)>5*Point)
      return(false);////Too far from the potential entry level.
   
   MacdCurrent=iMACD(NULL,0,12,26,9,PRICE_CLOSE,MODE_MAIN,0);
   MacdPrevious=iMACD(NULL,0,12,26,9,PRICE_CLOSE,MODE_MAIN,1);
   SignalCurrent=iMACD(NULL,0,12,26,9,PRICE_CLOSE,MODE_SIGNAL,0);
   SignalPrevious=iMACD(NULL,0,12,26,9,PRICE_CLOSE,MODE_SIGNAL,1);
   MacdSignalsToSell=((MacdCurrent<SignalCurrent && MacdPrevious>SignalPrevious) ||
                    (MacdCurrent<0 && MacdPrevious>0)) &&
                    iMACD(NULL,0,12,26,9,PRICE_CLOSE,MODE_MAIN,Point3Index)<0;
   if(!MacdSignalsToSell)
      return(false);
   
   DownTrLen=iCustom(NULL, 0, "RelDownTrLen_forCodeBase_v01", 100, Point1Index, true, 0, 0);
   UpTrLen=iCustom(NULL, 0, "RelUpTrLen_forCodeBase_v01", Point1Index, 1, true, 0, 0);
   
   Condition=UpTrLen/(DownTrLen+0.001)>TrendRatio && //The up trend was longer enough.
             MathAbs(Point2Level-Point3Level)/Point>13;////Potential stop loss far enough.
   
   if(Condition)
   {
      dStopLoss=Point3Level+Point;
      dTakeProfit=CurrentLevel-MathAbs(Point2Level-Point3Level);
   }
   return(Condition);
}

bool CloseLongCondition()
{
   bool Condition=false;
   return(Condition);
}

bool CloseShortCondition()
{
   bool Condition=false;
   return(Condition);
}

int FindFirstValley(double MaxLevel, double MinLevel,
                    int StartIndex=1, int barsToProcess=100)
{
   double ValueToCompare;
   for(int i=StartIndex; i<StartIndex+barsToProcess; i++)
   {
      ValueToCompare=MathMin(Open[i],Close[i]);
      //The search fails if the price crosses the bound levels.
      if(MathMax(Open[i],Close[i])>MaxLevel ||
         ValueToCompare<MinLevel)
         return(-1);
      if(i>0 &&
         ValueToCompare<MathMin(Open[i+1],Close[i+1]) &&
         ValueToCompare<MathMin(Open[i-1],Close[i-1]))
            return(i);
   }
   return(-1);
}


int FindFirstPeak(double MaxLevel, double MinLevel, 
             int StartIndex=1, int barsToProcess=100)
{
   double ValueToCompare;
   for(int i=StartIndex; i<StartIndex+barsToProcess; i++)
   {
      ValueToCompare=MathMax(Open[i],Close[i]);
      //The search fails if the price crosses the bound levels.
      if(ValueToCompare>MaxLevel ||
         MathMin(Open[i],Close[i])<MinLevel)
         return(-1);
      if(i>0 &&
         ValueToCompare>MathMax(Open[i+1],Close[i+1]) &&
         ValueToCompare>MathMax(Open[i-1],Close[i-1]))
            return(i);
   }
   return(-1);
}

//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----
     
//----
   return(0);
  }


//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int start()
  {

   int cnt, ticket, total;
   double BetterStopLoss;

   if(Bars<100)
     {
      Print("bars less than 100");
      return(0);  
     }
   if(TakeProfit<10)
     {
      Print("TakeProfit less than 10");
      return(0); 
     }


   total=OrdersTotal();
   if(total<1)
     {
      if(AccountFreeMargin()<(1000*Lots))
        {
         Print("We have no money. Free Margin = ", AccountFreeMargin());
         return(0);  
        }
      
      if(BuyCondition())
        {
         ticket=OrderSend(Symbol(),OP_BUY,Lots,Ask,3,
                          dStopLoss,dTakeProfit,
                          "1-2-3_forCodeBase_v01.mq4",MagicNumber,0,Green);
         if(ticket>0)
           {
            if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) 
               Print("BUY order opened : ",OrderOpenPrice());
           }
         else Print("Error opening BUY order : ",GetLastError());
         return(0); 
        }
      
      if(SellCondition())
        {
         ticket=OrderSend(Symbol(),OP_SELL,Lots,Bid,3,
                          dStopLoss,dTakeProfit,
                          "1-2-3_forCodeBase_v01.mq4",MagicNumber,0,Red);
         if(ticket>0)
           {
            if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES))
               Print("SELL order opened : ",OrderOpenPrice());
           }
         else Print("Error opening SELL order : ",GetLastError());
         return(0); 
        }
      return(0);
     }// end of block if(total<1)
     
 
   for(cnt=0;cnt<total;cnt++)
     {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
      if(OrderType()<=OP_SELL &&   
         OrderSymbol()==Symbol() &&
         OrderMagicNumber()==MagicNumber)     {
         if(OrderType()==OP_BUY)              {
            
            if(CloseLongCondition())
                {
                 OrderClose(OrderTicket(),OrderLots(),Bid,3,Violet); 
                 return(0); 
                }

            
            if(TrailingStop>0)  
              {                 
               if(Bid-OrderOpenPrice()>Point*TrailingStop)
                 {
                  BetterStopLoss=Bid-Point*TrailingStop;
                  if(OrderStopLoss()<BetterStopLoss)
                    {
                     OrderModify(OrderTicket(),OrderOpenPrice(),BetterStopLoss,OrderTakeProfit(),0,Green);
                     return(0);
                    }//end of block if(OrderStopLoss()<BetterStopLoss)
                 }//end of block if(Bid-OrderOpenPrice()>Point*TrailingStop)
              }//end of block if(TrailingStop>0)  
           }//end of block if(OrderType()==OP_BUY)
         else 
           {
            
            
            if(CloseShortCondition())
              {
               OrderClose(OrderTicket(),OrderLots(),Ask,3,Violet); 
               return(0); // exit
              }
            
            
            if(TrailingStop>0)  
              {                 
               if((OrderOpenPrice()-Ask)>(Point*TrailingStop))
                 {
                  BetterStopLoss=Ask+Point*TrailingStop;
                  if((OrderStopLoss()>BetterStopLoss) || (OrderStopLoss()==0))
                    {
                     OrderModify(OrderTicket(),OrderOpenPrice(),BetterStopLoss,OrderTakeProfit(),0,Red);
                     return(0);
                    }//end of block if((OrderStopLoss()>BetterStopLoss)...)
                 }//end of block if((OrderOpenPrice()-Ask)>(Point*TrailingStop))
              }//end of block if(TrailingStop>0)
           }//end of block else
        }//end of block if(OrderType()<=OP_SELL...)  
     }//end of block for(cnt=0;cnt<total;cnt++)
   return(0);
  }

