//+------------------------------------------------------------------+
//|                                                 AfterEffects.mq4 |
//|                               Copyright © 2015, Yury V. Reshetov |
//|                                         http://yury-reshetov.com |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2015, Yury V. Reshetov"
#property link      "http://yury-reshetov.com"

#property version   "1.01"
#property description "The robot for exchange trading according to the theorem about presence the aftereffects in random series" 
#property description "See also:" 
#property description "http://yury-reshetov.com/node/36 - Theorem (Russian language only)"         
#property description "http://yury-reshetov.com/node/40 - Support for the robot"       

//+------------------------------------------------------------------+
//|  Input parameters                                                        |
//+------------------------------------------------------------------+
input double   sl=500.0; // The value of StopLoss [in points]
input int      p=3;            // Period of bars [1..30]
input bool     random = false; // Is random price range
input double   lots=0.1;       // The volume of open positions [in lots]
input ulong    dev=2;          // Allowable slippage [in points]
extern int MagicNumber=888;    // Magic number
static int prevtime=0;
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
   prevtime=Time[0];
   return(0);
  }
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
   if(Time[0]==prevtime)
      return(0);
   prevtime=Time[0];
   int spread=3;
//----
   if(IsTradeAllowed())
     {
      RefreshRates();
      spread=MarketInfo(Symbol(),MODE_SPREAD);
     }
   else
     {
      prevtime=Time[1];
      return(0);
     }
   int ticket=-1;
// check for opened position
   int total=OrdersTotal();
//----
   for(int i=0; i<total; i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
        {
         // check for symbol & magic number
         if(OrderSymbol()==Symbol() && OrderMagicNumber()==MagicNumber)
           {
            int prevticket=OrderTicket();
            // long position is opened
            if(OrderType()==OP_BUY)
              {
               // check profit 
               if(Bid>(OrderStopLoss()+(sl*2+spread)*Point))
                 {
                  if(signals()<0)
                    { // reverse
                     ticket=OrderSend(Symbol(),OP_SELL,lots*2,Bid,dev,
                                      Ask+sl*Point,0,"AfterEffects",MagicNumber,0,Red);
                     Sleep(30000);
                     //----
                     if(ticket<0)
                        prevtime=Time[1];
                     else
                     while(!OrderCloseBy(ticket,prevticket,Blue))
                       {
                        Sleep(30000);
                       }
                    }
                  else
                    { // trailing stop
                     if(!OrderModify(OrderTicket(),OrderOpenPrice(),Bid-sl*Point,
                        0,0,Blue))
                       {
                        Sleep(30000);
                        prevtime=Time[1];
                       }
                    }
                 }
               // short position is opened
              }
            else
              {
               // check profit 
               if(Ask<(OrderStopLoss() -(sl*2+spread)*Point))
                 {
                  if(signals()>0)
                    { // reverse
                     ticket=OrderSend(Symbol(),OP_BUY,lots*2,Ask,dev,
                                      Bid-sl*Point,0,"AfterEffects",MagicNumber,0,Blue);
                     Sleep(30000);
                     //----
                     if(ticket<0)
                        prevtime=Time[1];
                     else
                     while(!OrderCloseBy(ticket,prevticket,Blue))
                       {
                        Sleep(30000);
                       }
                    }
                  else
                    { // trailing stop
                     if(!OrderModify(OrderTicket(),OrderOpenPrice(),Ask+sl*Point,
                        0,0,Blue))
                       {
                        Sleep(30000);
                        prevtime=Time[1];
                       }
                    }
                 }
              }
            // exit
            return(0);
           }
        }
     }
// check for long or short position possibility
   if(signals()>0)
     { //long
      ticket=OrderSend(Symbol(),OP_BUY,lots,Ask,dev,Bid-sl*Point,0,"AfterEffects",
                       MagicNumber,0,Blue);
      //----
      if(ticket<0)
        {
         Sleep(30000);
         prevtime=Time[1];
        }
     }
   else
     { // short
      ticket=OrderSend(Symbol(),OP_SELL,lots,Bid,dev,Ask+sl*Point,0,"AfterEffects",
                       MagicNumber,0,Red);
      if(ticket<0)
        {
         Sleep(30000);
         prevtime=Time[1];
        }
     }
//--- exit
   return(0);
  }
//+------------------------------------------------------------------+
//| Trades Signals                                                   |
//+------------------------------------------------------------------+
double signals()
  {
   double result=Close[0]-2.0*Open[p]+Open[2*p];
   if(random)
     {
      result=-result;
     }
   return(result);
  }
//+------------------------------------------------------------------+
