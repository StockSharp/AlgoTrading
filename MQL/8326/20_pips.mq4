extern int Period_PCh = 20; // ������ Price Channel
extern int TP = 20; // �������� TakeProfit � ������
extern int ��������� = 20; // �� ������� ��� ���������� �������� �������� ���� ����� ��������� ������
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int start()
  {
   double Price_Channel_UP,Price_Channel_DOUN;
   double MA_Fast,MA_Low;
   int cnt, ticket, total;
   double Lots = 0.01;
   
// ���������� ���������� ��������

      Price_Channel_UP=iCustom(NULL,0,"Price Channel",Period_PCh,0,2);
      Price_Channel_DOUN=iCustom(NULL,0,"Price Channel",Period_PCh,1,2);
      MA_Fast=iMA( NULL, 0, 1, 0, MODE_SMA, PRICE_TYPICAL, 1); // ������� ��
      MA_Low=iMA( NULL, 0, 20, 0, MODE_SMA, PRICE_CLOSE, 1); // ��������� �� 
            
   total=OrdersTotal();
   if(total<1) // ���� ��� �������
     {
     OrderSelect(OrdersHistoryTotal( )-1, SELECT_BY_POS, MODE_HISTORY);
     // �������� ������� ������� �� �����������
      if(MA_Fast>MA_Low && Open[0]<Open[1] && OrderProfit()<0)
        {
        OrderSend(Symbol(),OP_BUY,Lots*���������,Open[0],5,0,Open[0]+TP*Point,"������� ����������",16384,0,Green);
        }
      
      // �������� �������� ������� �� �����������
      if(MA_Fast<MA_Low && Open[0]>Open[1] && OrderProfit()<0)
        {
         OrderSend(Symbol(),OP_SELL,Lots*���������,Open[0],5,0,Open[0]-TP*Point,"������� ����������",16384,0,Red);
        }
   // �������� ������� �������
      if(MA_Fast>MA_Low && Open[0]<Open[1] && OrderProfit()>=0)
        {
        OrderSend(Symbol(),OP_BUY,Lots*1,Open[0],5,0,Open[0]+TP*Point,"�������",16384,0,Green);
        }
      
      // �������� �������� �������
      if(MA_Fast<MA_Low && Open[0]>Open[1] && OrderProfit()>=0)
        {
         OrderSend(Symbol(),OP_SELL,Lots*1,Open[0],5,0,Open[0]-TP*Point,"�������",16384,0,Red);
        }
     }
       
    total=OrdersTotal();
    for(cnt=0;cnt<total;cnt++)
    {//1
         OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
         if(OrderType()<=OP_SELL &&    
         OrderSymbol()==Symbol())  
         {
         if(OrderType()==OP_BUY)
            {
               if(Low[1]<Price_Channel_DOUN)
                {
                     if(Open[0]<Low[1])
                     {
                     OrderClose(OrderTicket(),OrderLots(),Open[0],5,Red);
                     return(0);
                     }
                  OrderModify(OrderTicket(),OrderOpenPrice(),Low[1]-10*Point ,0,0,Red);
                  return(0);
                }
                
            }
            if(OrderType()==OP_SELL)
            {
               if(High[1]>Price_Channel_UP)
                  {
                  if(Open[0]>High[1])
                     {
                     OrderClose(OrderTicket(),OrderLots(),Open[0],5,Red);
                     return(0);
                     }    
                  OrderModify(OrderTicket(),OrderOpenPrice(),High[1]+10*Point ,0,0,Red);
                  return(0);
                  }
            }
         }

    }
     
return(0);
}