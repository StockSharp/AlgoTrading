//+------------------------------------------------------------------+
//|                                                 jMasterRSXv1.mq4 |
//|                                               Jiri Balcar © 2009 |
//|                                                jirimac@yahoo.com |
//|                                                           EURUSD |
//+------------------------------------------------------------------+
#define MAGICMA  07202009

double Lots               = 0.1;
double MaximumRisk        = 0.01;
double DecreaseFactor     = 0;

bool cBuy, cSell, cExitBuy, cExitSell, Buy, Sell;

//+------------------------------------------------------------------+
//| Calculate open orders                                            |
//+------------------------------------------------------------------+
int OpenOrders(string symbol)
{
  int co=0;
  int total  = OrdersTotal();

  for (int i=total-1; i >=0; i--)
  {
    OrderSelect(i,SELECT_BY_POS,MODE_TRADES);     
    if (OrderMagicNumber()==MAGICMA && OrderSymbol()==Symbol() && (OrderType()==OP_BUY || OrderType()==OP_SELL))
    {
      co++;
    }
  }
  return(co);
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
   lot=NormalizeDouble(AccountBalance()*MaximumRisk/1000.0,1);
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
  
int hstTotal=HistoryTotal();
Buy=false;
Sell=false;
 
if(hstTotal==0 || OrderType()==OP_SELL) Buy=true;
if(hstTotal==0 || OrderType()==OP_BUY) Sell=true;


   int    res;
//---- go trading only for first tiks of new bar
   if(Volume[0]>1) return;   
//---- buy conditions
   if(cBuy==true && Buy==true)
     {
      res=OrderSend(Symbol(),OP_BUY,LotsOptimized(),Ask,3,0,0,"",MAGICMA,0,Blue);
      return;
     }
//---- sell conditions
   if(cSell==true && Sell==true)  
     {
      res=OrderSend(Symbol(),OP_SELL,LotsOptimized(),Bid,3,0,0,"",MAGICMA,0,Red);
      return;
     }
//----
  }
  
//+------------------------------------------------------------------+
//| Check for close order conditions                                 |
//+------------------------------------------------------------------+

void CheckForClose()
  {
//----
   for(int i=0;i<OrdersTotal();i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false)        break;
      if(OrderMagicNumber()!=MAGICMA || OrderSymbol()!=Symbol()) continue;
      //---- check order type 
      if(OrderType()==OP_BUY)
        {
         if(cExitBuy==true) OrderClose(OrderTicket(),OrderLots(),Bid,3,White);
         break;
        }
      if(OrderType()==OP_SELL)
        {
         if(cExitSell==true) OrderClose(OrderTicket(),OrderLots(),Ask,3,White);
         break;
        }
     }
}


//+------------------------------------------------------------------+
//| Start function                                                   |
//+------------------------------------------------------------------+
void start()
  {

//+------------------------------------------------------------------+
//| Trading conditions                                               |
//+------------------------------------------------------------------+

double cRSX_0 = iCustom(NULL, PERIOD_M5, "rsx", 0, 1);
double cRSX_1 = iCustom(NULL, PERIOD_M30, "rsx", 0, 1);

cSell  = cRSX_1 < 50 && cRSX_0 > 75;
cBuy   = cRSX_1 > 50 && cRSX_0 < 25;

cExitBuy  = cSell;
cExitSell = cBuy;



//---- calculate open orders by current symbol
   if(OpenOrders(Symbol())==0) CheckForOpen();
   if(OpenOrders(Symbol())!=0) CheckForClose();
//----
  }
//+------------------------------------------------------------------+






