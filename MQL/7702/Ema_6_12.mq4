//+------------------------------------------------------------------+
//|                                                     EMA_6_12.mq4 |
//|                                                      Coders Guru |
//|                                         http://www.forex-tsd.com |
//+------------------------------------------------------------------+
#property copyright "Coders Guru"
#property link      "http://www.forex-tsd.com"
//---- Includes
#include <stdlib.mqh>
//---- Trades limits
extern double    TrailingStop=40;
extern double    TakeProfit=1000; //any big number
extern double    Lots=1;
extern int       Slippage=5;
//--- External options
extern int       CurrentBar=1;
extern bool      UseClose=true;
//--- Indicators settings
extern int       MaMode=1; /* MODE_SMA 0   MODE_EMA 1  MODE_SMMA 2 MODE_LWMA 3 */
extern int       ShortEma=6;
extern int       LongEma=12;
//--- Global variables
int      MagicNumber=123430;
string   ExpertComment="EMA_6_12";
bool     LimitPairs=false;
bool     LimitFrame=false;
int      TimeFrame=60;
string   LP[]={"EURUSD","USDJPY","AUDUSD","USDCAD"}; // add/remove the paris you want to limit.
bool     Optimize=false;
int      NumberOfTries=5;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int init()
  {
   return(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int deinit()
  {
   return(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool isNewSymbol(string current_symbol)
  {
   //loop through all the opened order and compare the symbols
   int total =OrdersTotal();
   for(int cnt=0;cnt < total;cnt++)
     {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
      string selected_symbol=OrderSymbol();
      if (current_symbol==selected_symbol && OrderMagicNumber()==MagicNumber)
         return(False);
     }
   return(True);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int Crossed()
  {
   double EmaLongPrevious=iMA(NULL,0,LongEma,0,MaMode, PRICE_CLOSE, CurrentBar+1);
   double EmaLongCurrent=iMA(NULL,0,LongEma,0,MaMode, PRICE_CLOSE, CurrentBar);
   double EmaShortPrevious=iMA(NULL,0,ShortEma,0,MaMode, PRICE_CLOSE, CurrentBar+1);
   double EmaShortCurrent=iMA(NULL,0,ShortEma,0,MaMode, PRICE_CLOSE, CurrentBar);
//----
   if (EmaShortPrevious<EmaLongPrevious && EmaShortCurrent>EmaLongCurrent)    return(1); //up trend
   if (EmaShortPrevious>EmaLongPrevious && EmaShortCurrent<EmaLongCurrent)    return(2); //down trend
//----
   return(0); //elsewhere
  }
//+------------------------------------------------------------------+
int start()
  {
   int cnt, ticket, total,n;
   double trend ;
   if(Bars<100) {Print("bars less than 100"); return(0);}
   if(LimitFrame)
     {
      if(Period()!=TimeFrame) {Print("This EA is not working with this TimeFrame!"); return(0);}
     }
   if(LimitPairs)
     {
      if(AllowedPair(Symbol())==false) {Print("This EA is not working with this Currency!"); return(0);}
     }
//--- Trading conditions
   bool BuyCondition=false , SellCondition=false , CloseBuyCondition=false , CloseSellCondition=false;

   if (Crossed()==1 )
      BuyCondition=true;
   if (Crossed ()== 2 )
      SellCondition=true;
   if (Crossed ()== 2)
      CloseBuyCondition=true;
   if (Crossed ()== 1)
      CloseSellCondition=true;
   total =OrdersTotal();
   if(total < 1 || isNewSymbol(Symbol()))
     {
      if(BuyCondition) //<-- BUY condition
        {
         ticket=OpenOrder(OP_BUY); //<-- Open BUY order
         CheckError(ticket,"BUY");
         return(0);
        }
      if(SellCondition) //<-- SELL condition
        {
         ticket=OpenOrder(OP_SELL); //<-- Open SELL order
         CheckError(ticket,"SELL");
         return(0);
        }
      return(0);
     }
   for(cnt=0;cnt<total;cnt++)
     {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
      if(OrderType()<=OP_SELL && OrderSymbol()==Symbol())
        {
         if(OrderType()==OP_BUY)   //<-- Long position is opened
           {
            if(UseClose)
              {
               if(CloseBuyCondition) //<-- Close the order and exit! 
                 {
                  CloseOrder(OrderType()); return(0);
                 }
              }
            TrailOrder(OrderType()); return(0); //<-- Trailling the order
           }
         if(OrderType()==OP_SELL) //<-- Go to short position
           {
            if(UseClose)
              {
               if(CloseSellCondition) //<-- Close the order and exit! 
                 {
                  CloseOrder(OP_SELL); return(0);
                 }
              }
            TrailOrder(OrderType()); return(0); //<-- Trailling the order
           }
        }
     }
//----
   return(0);
  }
//+------------------------------------------------------------------+
int OpenOrder(int type)
  {
   int ticket=0;
   int err=0;
   int c=0;
   if(type==OP_BUY)
     {
      for(c=0;c < NumberOfTries;c++)
        {
         ticket=OrderSend(Symbol(),OP_BUY,Lots,Ask,Slippage,0,Ask+TakeProfit*Point,ExpertComment,MagicNumber,0,Green);
         err=GetLastError();
         if(err==0)
           {
            break;
           }
         else
           {
            if(err==4 || err==137 ||err==146 || err==136) //Busy errors
              {
               Sleep(5000);
               continue;
              }
            else //normal error
              {
               break;
              }
           }
        }
     }
   if(type==OP_SELL)
     {
      for(c=0;c < NumberOfTries;c++)
        {
         ticket=OrderSend(Symbol(),OP_SELL,Lots,Bid,Slippage,0,Bid-TakeProfit*Point,ExpertComment,MagicNumber,0,Red);
         err=GetLastError();
         if(err==0)
           {
            break;
           }
         else
           {
            if(err==4 || err==137 ||err==146 || err==136) //Busy errors
              {
               Sleep(5000);
               continue;
              }
            else //normal error
              {
               break;
              }
           }
        }
     }
   return(ticket);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CloseOrder(int type)
  {
   if(OrderMagicNumber()==MagicNumber)
     {
      if(type==OP_BUY)
         return(OrderClose(OrderTicket(),OrderLots(),Bid,Slippage,Violet));
      if(type==OP_SELL)
         return(OrderClose(OrderTicket(),OrderLots(),Ask,Slippage,Violet));
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void TrailOrder(int type)
  {
   if(TrailingStop>0)
     {
      if(OrderMagicNumber()==MagicNumber)
        {
         if(type==OP_BUY)
           {
            if(Bid-OrderOpenPrice()>Point*TrailingStop)
              {
               if(OrderStopLoss()<Bid-Point*TrailingStop)
                 {
                  OrderModify(OrderTicket(),OrderOpenPrice(),Bid-Point*TrailingStop,OrderTakeProfit(),0,Green);
                 }
              }
           }
         if(type==OP_SELL)
           {
            if((OrderOpenPrice()-Ask)>(Point*TrailingStop))
              {
               if((OrderStopLoss()>(Ask+Point*TrailingStop)) || (OrderStopLoss()==0))
                 {
                  OrderModify(OrderTicket(),OrderOpenPrice(),Ask+Point*TrailingStop,OrderTakeProfit(),0,Red);
                 }
              }
           }
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CheckError(int ticket, string Type)
  {
   if(ticket>0)
     {
      if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) Print(Type + " order opened : ",OrderOpenPrice());
     }
   else Print("Error opening " + Type + " order : ", ErrorDescription(GetLastError()));
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool AllowedPair(string pair)
  {
   bool result=false;
   for(int n=0;n < ArraySize(LP); n++)
     {
      if(Symbol()==LP[n])
        {
         result=true;
        }
     }
   return(result);
  }
//+------------------------------------------------------------------+