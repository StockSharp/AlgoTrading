//+------------------------------------------------------------------+
//|                                                    Parabolic.mq4 |
//|                                                          ������� |
//|                                                   wwwita@mail.ru |
//+------------------------------------------------------------------+
extern double     Lots=0.1;
extern double      Pip=5;
extern double MoveBack=3;
extern int         chk=0;
extern double       SL=20;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int start()
  {
   double hi=High[1];
   double lo=Low[1];
   double EMA, EMA1, EMA2, EMA3;
   int cnt=0, ticket, total;
   EMA=iMA(0,0,5,0,MODE_EMA,PRICE_MEDIAN,1);
   EMA1=iMA(0,0,10,0,MODE_EMA,PRICE_MEDIAN,1);
   EMA2=iMA(0,0,5,0,MODE_EMA,PRICE_MEDIAN,0);
   EMA3=iMA(0,0,10,0,MODE_EMA,PRICE_MEDIAN,0);
   total=OrdersTotal();
   if(total<1)
     {
      if(AccountFreeMargin()<(1000*Lots))
        {
         Print("� ��� ��� �����. ��������� �������� = ", AccountFreeMargin());
         return(0);
        }
      if(((EMA>EMA1) && (EMA2<EMA3)) || ((EMA<EMA1) && (EMA2>EMA3)))
        {
         chk=1;
         Print("������� ��������!");
        }
      if(chk==1)
        {
         if(EMA3-EMA2>2*Point && Bid>=(lo+MoveBack*Point))
           {
            ticket=OrderSend(Symbol(),OP_SELL,Lots,Bid,3,0,0,
            "EMA position:",16385,0,Red);
            if(ticket>0)
              {
               if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES))
                  Print("������ ����� SELL : ",OrderOpenPrice());
               chk=0;
              }
            else
              {
               Print("������ �������� SELL ������ : ",GetLastError());
               return(0);
              }
           }
         if(EMA2-EMA3>2*Point && Ask<=(hi-MoveBack*Point))
           {
            ticket=OrderSend(Symbol(),OP_BUY,Lots,Ask,3,0,0,
            "EMA position:",16385,0,Green);
            if(ticket>0)
              {
               if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES))
                  Print("������ ����� BUY : ",OrderOpenPrice());
               chk=0;
              }
            else
              {
               Print("������ �������� BUY ������ : ",GetLastError());
               return(0);
              }
           }
        }
      return(0);
     }
   for(cnt=0;cnt<total;cnt++)
     {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
      if(OrderType()<=OP_SELL &&   // ��� �������� �������? OP_BUY ��� OP_SELL 
         OrderSymbol()==Symbol())  // ���������� ���������?
        {
         if(OrderType()==OP_BUY)   // ������� ������� �������
           {
            // ��������, ����� ��� ���� �����������?
            if(Bid>=(OrderOpenPrice()+Pip*Point))
              {
               chk=0;
               OrderClose(OrderTicket(),OrderLots(),Bid,3,Violet); // ��������� �������
               return(0); // �������
              }
            if(Bid<=(OrderOpenPrice()-SL*Point))
              {
               chk=0;
               OrderClose(OrderTicket(),OrderLots(),Bid,3,Violet); // ��������� �������
               return(0); // �������
              }
           }
         else // ����� ��� �������� �������
           {
            // ��������, ����� ��� ���� �����������?
            if(Ask<=(OrderOpenPrice()-Pip*Point))
              {
               chk=0;
               OrderClose(OrderTicket(),OrderLots(),Ask,3,Violet); // ��������� �������
               return(0); // �������
              }
            if(Ask>=(OrderOpenPrice()+SL*Point))
              {
               chk=0;
               OrderClose(OrderTicket(),OrderLots(),Ask,3,Violet); // ��������� �������
               return(0); // �������
              }
           }
        }
     }
   return(0);
  }
//+------------------------------------------------------------------+