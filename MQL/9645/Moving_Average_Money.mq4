//+------------------------------------------------------------------+
//|                                               Moving Average.mq4 |
//|                      Copyright © 2005, MetaQuotes Software Corp. |
//|                                       http://100k2.blogspot.com  |
//+------------------------------------------------------------------+
#define MAGICMA  20050610

extern double Lots               = 0.01;
extern double MaximumRisk        = 10;
extern double ProfitLossFactor   = 3;
extern double MovingPeriod       = 12;
extern double MovingShift        = 6;



//+------------------------------------------------------------------+
//| Stop Loss pips from amount of money given                        |
//+------------------------------------------------------------------+

double AccountPercentStopPips(string symbol, double percent, double lots)
{
    double StopLevel = MarketInfo(Symbol(), MODE_STOPLEVEL) + MarketInfo(Symbol(), MODE_SPREAD);
    
    double freemargin     = AccountFreeMargin();
    double moneyrisk    = freemargin * percent / 100;
    double spread       = MarketInfo(symbol, MODE_SPREAD);
    double point        = MarketInfo(symbol, MODE_POINT);
    double ticksize     = MarketInfo(symbol, MODE_TICKSIZE);
    double tickvalue    = MarketInfo(symbol, MODE_TICKVALUE);
    double tickvaluefix = tickvalue * point / ticksize; // A fix for an extremely rare occasion when a change in ticksize leads to a change in tickvalue
    
    double stoploss = moneyrisk / (lots * tickvaluefix ) - spread;
    
    if (stoploss < StopLevel)
        stoploss = StopLevel; // This may rise the risk over the requested
        
    stoploss = NormalizeDouble(stoploss, 0);
    
    return (stoploss);
}

//+------------------------------------------------------------------+
//| Calculate open positions                                         |
//+------------------------------------------------------------------+
int CalculateCurrentOrders(string symbol)
  {
   int buys=0,sells=0;
//----
   for(int i=0;i<OrdersTotal();i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false) break;
      if(OrderSymbol()==Symbol() && OrderMagicNumber()==MAGICMA)
        {
         if(OrderType()==OP_BUY)  buys++;
         if(OrderType()==OP_SELL) sells++;
        }
     }
//---- return orders volume
   if(buys>0) return(buys);
   else       return(-sells);
  }

//+------------------------------------------------------------------+
//| Check for open order conditions                                  |
//+------------------------------------------------------------------+
void CheckForOpen()
  {
   double ma;
   int    res;
   
   double StopLoss,TakeProfit;
   StopLoss   = AccountPercentStopPips(Symbol(),MaximumRisk,Lots);
   TakeProfit = NormalizeDouble(ProfitLossFactor * StopLoss,0); 
   
   
//---- go trading only for first tiks of new bar
   if(Volume[0]>1) return;
//---- get Moving Average 
   ma=iMA(NULL,0,MovingPeriod,MovingShift,MODE_SMA,PRICE_CLOSE,0);
//---- sell conditions
   if(Open[1]>ma && Close[1]<ma)  
     {
      res=OrderSend(Symbol(),OP_SELL,Lots,Bid,3,Bid+Point*StopLoss,Bid-TakeProfit*Point,"",MAGICMA,0,Red);
      return;
     }
//---- buy conditions
   if(Open[1]<ma && Close[1]>ma)  
     {
      res=OrderSend(Symbol(),OP_BUY,Lots,Ask,3,Ask-Point*StopLoss,Ask+TakeProfit*Point,"",MAGICMA,0,Blue);
      return;
     }
//----
  }
//+------------------------------------------------------------------+
//| Check for close order conditions                                 |
//+------------------------------------------------------------------+
void CheckForClose()
  {
   double ma;
//---- go trading only for first tiks of new bar
   if(Volume[0]>1) return;
//---- get Moving Average 
   ma=iMA(NULL,0,MovingPeriod,MovingShift,MODE_SMA,PRICE_CLOSE,0);
//----
   for(int i=0;i<OrdersTotal();i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false)        break;
      if(OrderMagicNumber()!=MAGICMA || OrderSymbol()!=Symbol()) continue;
      //---- check order type 
      if(OrderType()==OP_BUY)
        {
         if(Open[1]>ma && Close[1]<ma) OrderClose(OrderTicket(),OrderLots(),Bid,3,White);
         break;
        }
      if(OrderType()==OP_SELL)
        {
         if(Open[1]<ma && Close[1]>ma) OrderClose(OrderTicket(),OrderLots(),Ask,3,White);
         break;
        }
     }
//----
  }
//+------------------------------------------------------------------+
//| Start function                                                   |
//+------------------------------------------------------------------+
void start()
  {
//---- check for history and trading
   if(Bars<100 || IsTradeAllowed()==false) return;
//---- calculate open orders by current symbol
   if(CalculateCurrentOrders(Symbol())<=3) CheckForOpen();
   //else;                                    CheckForClose();
//----
  }
//+------------------------------------------------------------------+