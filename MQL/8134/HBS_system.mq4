//+------------------------------------------------------------------+
//|                                                   HBS system.mq4 |
//|                                                        fortrader |
//|                                                 www.fortrader.ru |
//+------------------------------------------------------------------+
#property copyright "fortrader"
#property link      "www.fortrader.ru"

//---- input parameters
extern int       per_MA = 200;
extern int       StopLoss_Buy = 50;
extern int       TrailingStop_Buy = 10;
extern int       StopLoss_Sell = 50;
extern int       TrailingStop_Sell = 10;
extern double    Lots = 0.1;
double    pc_B = 0, pc_S = 0, pc_open = 0;

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
  
  if (Volume[0] > 1) return(0);

//������� ���������� �������� ��������, �������������� ���������� ����� �����, ������� ������ ��� ����� x.  
//  Print(" ������� ���� ",(iClose(NULL,0,1)));
  pc_B = (MathCeil((iClose(NULL,0,1)*100))/100);
//  Print("���������� ����� �����, ������� ������ ������� ���� ���� ",pc_B);
//������� ���������� �������� ��������, �������������� ���������� ����� �����, ������� ������ ��� ����� x. 
  pc_S = (MathFloor((iClose(NULL,0,1)*100))/100);
//  Print("���������� ����� �����, ������� ������ ������� ���� ���� ",pc_S);
 

//----
// ��������� ���������� tmp_pc
int total=0, cnt=0;
double MA;
int err;

// ��������� ��������� ��������� ����������� ��� ������ ������� �����

  MA = iMA(NULL,0,per_MA,0,MODE_EMA,PRICE_MEDIAN,1);

  total=OrdersTotal();
 if(total<1)
  {
  // �������� �������
  if(AccountFreeMargin()<(1000*Lots))
     {
       Print("We have no money. Free Margin = ", AccountFreeMargin());   
       return(0);  
     }
  
  // �������� ������� ��� ���������� ������
  if(iOpen(NULL,0,1)>MA && iClose(NULL,0,1)>MA && pc_open<pc_B)// 
     { 
       OrderSend(Symbol(),OP_BUYSTOP,Lots,(pc_B-Point*15),3,pc_B-(15+StopLoss_Buy)*Point,pc_B,"��������",16384,10,Green);
       OrderSend(Symbol(),OP_BUYSTOP,Lots,(pc_B-Point*15),3,pc_B-(15+StopLoss_Buy)*Point,pc_B+15*Point,"��������",16385,10,Green);
       pc_open = pc_B;
     }

  if(iOpen(NULL,0,1)<MA && iClose(NULL,0,1)<MA && pc_open>pc_B) 
     { 
       OrderSend(Symbol(),OP_SELLSTOP,Lots,(pc_S+Point*15),3,pc_S+(15+StopLoss_Sell)*Point,pc_S,"�������",16386,10,Red);
       OrderSend(Symbol(),OP_SELLSTOP,Lots,(pc_S+Point*15),3,pc_S+(15+StopLoss_Sell)*Point,pc_S-15*Point,"�������",16387,10,Red);
       pc_open = pc_S;
     }
  }
  for(cnt=total-1;cnt>=0;cnt--)
     {
       OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
       if(OrderType()==OP_BUYSTOP && OrderOpenPrice()!=(pc_B-Point*15))
         {
           OrderDelete(OrderTicket()); 
         }
       if(OrderType()==OP_SELLSTOP && OrderOpenPrice()!=(pc_S+Point*15))
         {
           OrderDelete(OrderTicket()); 
         }
       if(OrderType()==OP_BUY)
         {
           if(TrailingStop_Buy>0)  
             {                 
               if(Bid-OrderOpenPrice()>Point*TrailingStop_Buy) //
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
               if((OrderOpenPrice()-Ask)>(Point*TrailingStop_Sell))  // 
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

   
//----   return(0);
  }
//+------------------------------------------------------------------+