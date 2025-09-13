//+------------------------------------------------------------------+
//|                                               Robot_MACD_12.26.9 |
//|                                                     Tokman Yuriy |
//|                                            yuriytokman@gmail.com |
//+------------------------------------------------------------------+
             //������� ����������
extern double TakeProfit = 300;
extern double Lots = 0.1;

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int start()
  {
   double MacdCurrent, MacdPrevious, SignalCurrent,SignalPrevious;
   int cnt, ticket, total;

   MacdCurrent=iMACD(NULL,0,12,26,9,PRICE_CLOSE,MODE_MAIN,0);
   MacdPrevious=iMACD(NULL,0,12,26,9,PRICE_CLOSE,MODE_MAIN,1);
   SignalCurrent=iMACD(NULL,0,12,26,9,PRICE_CLOSE,MODE_SIGNAL,0);
   SignalPrevious=iMACD(NULL,0,12,26,9,PRICE_CLOSE,MODE_SIGNAL,1);

   total=OrdersTotal();
   if(total<1)//�������� ���������� ������� 
     {
      // �������� ��������� �������
      if(AccountFreeMargin()<(1000*Lots))//���������� ��������� �������
        {
         Print("������������ ������� = ", AccountFreeMargin());
         return(0);  
        }
      // �������� ������� �������
      if(MacdCurrent>SignalCurrent && MacdPrevious<SignalPrevious
         && MacdCurrent<0 && SignalCurrent<0 )
        {
         ticket=OrderSend(Symbol(),OP_BUY,Lots,Ask,3,0,Ask+TakeProfit*Point,"-",0,0,Green);
         if(ticket>0)
           {
            if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) Print("������� ������� BUY : ",OrderOpenPrice());
           }
         else Print("������ ��� �������� BUY ������� : ",GetLastError()); 
         return(0);
        }
      // �������� �������� �������
      if(MacdCurrent<SignalCurrent && MacdPrevious>SignalPrevious
         && MacdCurrent>0 && SignalCurrent>0)
        {
         ticket=OrderSend(Symbol(),OP_SELL,Lots,Bid,3,0,Bid-TakeProfit*Point,"-",0,0,Red);
         if(ticket>0)
           {
            if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) Print("������� ������� SELL : ",OrderOpenPrice());
           }
         else Print("������ ��� �������� SELL ������� : ",GetLastError()); 
         return(0); 
        }
      return(0);
     }
   // ������� �������� �������   
   for(cnt=0;cnt<total;cnt++)
     {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
      if(OrderType()<=OP_SELL &&   // ������� �������� ������� 
         OrderSymbol()==Symbol())  // ��������� �� ���������� �����������
        {
         if(OrderType()==OP_BUY)   // ������� ������� �������
           {
            // ������� ��������
            if(MacdCurrent<SignalCurrent && MacdPrevious>SignalPrevious)
                {
                 OrderClose(OrderTicket(),OrderLots(),Bid,3,Violet);
                 return(0);
                }
           }
         else // ������� �������� �������
           {
            // ������� ��������
            if( MacdCurrent>SignalCurrent && MacdPrevious<SignalPrevious)
              {
               OrderClose(OrderTicket(),OrderLots(),Ask,3,Violet);
               return(0);
              }
           }
        }
     }
   return(0);
  }