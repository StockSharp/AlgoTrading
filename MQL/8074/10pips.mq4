//+------------------------------------------------------------------+
//|                                                       10pips.mq4 |
//|                                                        fortrader |
//|                                                 www.fortrader.ru |
//+------------------------------------------------------------------+
#property copyright "fortrader"
#property link      "www.fortrader.ru"

extern int       TakeProfit_Buy = 10;
extern int       StopLoss_Buy = 50;
extern int       TrailingStop_Buy = 50;
extern int       TakeProfit_Sell = 10;
extern int       StopLoss_Sell = 50;
extern int       TrailingStop_Sell = 50;
extern double     Lots = 0.1;

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
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {
//----
   
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
//----
  if (Volume[0] > 1) return(0);
// ��������� ����������
int total, cnt;

  total=OrdersTotal();

  // �������� �������
  if(AccountFreeMargin()<(1000*Lots))
     {
       Print("We have no money. Free Margin = ", AccountFreeMargin());   
       return(0);  
     }
  if(total<1)
    {  
     // �������� �������
       OrderSend(Symbol(),OP_BUY,Lots,Ask,3,Bid-StopLoss_Buy*Point,Ask+TakeProfit_Buy*Point,"��������",16384,0,Green);
       Sleep(10000);//10 ������
       RefreshRates();
       OrderSend(Symbol(),OP_SELL,Lots,Bid,3,Ask+StopLoss_Sell*Point,Bid-TakeProfit_Sell*Point,"�������",16385,0,Red);
    }
  if(total==1)
    {
       OrderSelect(0, SELECT_BY_POS, MODE_TRADES);
       if(OrderType()==OP_BUY)
         {
           OrderSend(Symbol(),OP_BUY,Lots,Ask,3,Bid-StopLoss_Buy*Point,Ask+TakeProfit_Buy*Point,"��������",16384,0,Green);
         }
       if(OrderType()==OP_SELL)
         {
           OrderSend(Symbol(),OP_SELL,Lots,Bid,3,Ask+StopLoss_Sell*Point,Bid-TakeProfit_Sell*Point,"�������",16385,0,Red);
         }
    }   
  for(cnt=total-1;cnt>=0;cnt--)
     {
       OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
       if(OrderType()==OP_BUY)
         {
           if(TrailingStop_Buy>0)  
             {                 
               if(Bid-OrderOpenPrice()>Point*TrailingStop_Buy) // Bid - ���� �������
                 {
                   if(OrderStopLoss()<Bid-Point*TrailingStop_Buy)
                     {
                       OrderModify(OrderTicket(),OrderOpenPrice(),Bid-Point*TrailingStop_Buy,OrderTakeProfit(),0,Green);
                       return(0);
                     }
                 }
             }
         }
       if(OrderType()==OP_SELL)
         {
           if(TrailingStop_Sell>0)  
             {                 
               if((OrderOpenPrice()-Ask)>(Point*TrailingStop_Sell))  // Ask - ���� �������
                 {
                   if((OrderStopLoss()>(Ask+Point*TrailingStop_Sell)) || (OrderStopLoss()==0))
                     {
                       OrderModify(OrderTicket(),OrderOpenPrice(),Ask+Point*TrailingStop_Sell,OrderTakeProfit(),0,Red);
                       return(0);
                     }
                 }
             }
         }
  
     }
   
//----
   return(0);
  }
//+------------------------------------------------------------------+