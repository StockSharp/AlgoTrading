//+------------------------------------------------------------------+
//|                                                     MaSarADX.mq4 |
//|                                                           MauBra |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "MauBra"
#property link      "https://login.mql5.com/en/users/almaro"
#define MAGICMA  20050610

extern double Lots               = 0.1;
extern double MaximumRisk        = 0.002;
extern double DecreaseFactor     = 0;
extern double MovingPeriod       = 100;
extern double MovingShift        = 0;
extern int    SL                 = 0;

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
//| Calculate optimal lot size                                       |
//+------------------------------------------------------------------+
double LotsOptimized()
  {
   double lot=Lots;
   int    orders=HistoryTotal();     // history orders total
   int    losses=0;                  // number of losses orders without a break
//---- select lot size
   lot=NormalizeDouble(AccountFreeMargin()*MaximumRisk/1000.0,1);
//---- calcuulate number of losses orders without a break
   if(DecreaseFactor>0)
     {
      for(int i=orders-1;i>=0;i--)
        {
         if(OrderSelect(i,SELECT_BY_POS,MODE_HISTORY)==false) { Print("Error in history!"); break; }
         if(OrderSymbol()!=Symbol() || OrderType()>OP_SELL) continue;
         //----
         if(OrderProfit()>0) break;
         if(OrderProfit()<0) losses++;
        }
      if(losses>1) lot=NormalizeDouble(lot-lot*losses/DecreaseFactor,1);
     }
//---- return lot size
   if(lot<0.1) lot=0.1;
   return(lot);
  }
//+------------------------------------------------------------------+
//| Check for open order conditions                                  |
//+------------------------------------------------------------------+
void CheckForOpen()
  {
   double ma,ADXmain,ADXplus,ADXminus,SAR;
   int    res;
//---- go trading only for first tiks of new bar
   //if(Volume[0]>1) return;
//---- get Moving Average etc.
   ma=iMA(NULL,0,MovingPeriod,MovingShift,MODE_SMA,PRICE_CLOSE,0);
   ADXmain=iADX(0,0,14,PRICE_CLOSE,MODE_MAIN,0);
   ADXplus=iADX(0,0,14,PRICE_CLOSE,MODE_PLUSDI,0);
   ADXminus=iADX(0,0,14,PRICE_CLOSE,MODE_MINUSDI,0);
   SAR=iSAR(0,0,0.02,0.1,0);
   
//---- sell conditions
   if( (Close[0]<ma)&&(ADXplus<=ADXminus)&& (Close[0]<SAR) )
     {
      res=OrderSend(Symbol(),OP_SELL,LotsOptimized(),Bid,3,0,0,"",MAGICMA,0,Red);
      return;
     }
//---- buy conditions
   if( (Close[0]>ma)&&(ADXplus>=ADXminus)&& (Close[0]>SAR) )  
     {
      res=OrderSend(Symbol(),OP_BUY,LotsOptimized(),Ask,3,0,0,"",MAGICMA,0,Blue);
      return;
     }
//----
  }
//+------------------------------------------------------------------+
//| Check for close order conditions                                 |
//+------------------------------------------------------------------+
void CheckForClose()
  {
   double SAR;
//---- go trading only for first tiks of new bar
   //if(Volume[0]>1) return;
//---- get Moving Average 
   SAR=iSAR(0,0,0.02,0.1,0);
//----
   for(int i=0;i<OrdersTotal();i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false)        break;
      if(OrderMagicNumber()!=MAGICMA || OrderSymbol()!=Symbol()) continue;
      //---- check order type 
      if(OrderType()==OP_BUY)
        {
         if(Close[0]<SAR) OrderClose(OrderTicket(),OrderLots(),Bid,3,White);
         break;
        }
      if(OrderType()==OP_SELL)
        {
         if(Close[0]>SAR) OrderClose(OrderTicket(),OrderLots(),Ask,3,White);
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
   if(CalculateCurrentOrders(Symbol())==0) CheckForOpen();
   else                                    CheckForClose();
//----
  }
//+------------------------------------------------------------------+