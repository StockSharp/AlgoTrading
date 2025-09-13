//+------------------------------------------------------------------+
//|                                                    ���������.mq4 |
//|                      Copyright � 2006, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "Copyright � 2006, �������� :-)"
#property link      "scrivimi@mail.ru"
extern int �������_�������=20;
extern int �������_�������=-2;
extern int ���������_�����=20;
extern int �����������_��������=30;
extern int ������������_�������=25;
extern double �����=0.1;
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
   double pip=MarketInfo(Symbol(),MODE_TICKSIZE);
//----
   if (OrdersTotal()==0)
     {
      double buy= Ask+�������_�������*pip;
      double sell= Bid-�������_�������*pip;
      int ticket1=OrderSend(Symbol(),OP_BUYSTOP,�����,buy,3,0,0,"buy",16384,CurTime()+���������_�����*60,Green);
      int ticket2=OrderSend(Symbol(),OP_SELLSTOP,�����,sell,3,0,0,"buy",16384,CurTime()+���������_�����*60,Green);
     }
   if (OrdersTotal()>0)
     {
      for(int i=0;i<=OrdersTotal();i++)
        {
         OrderSelect(i,SELECT_BY_POS,MODE_TRADES);
//----
         ticket1=OrderTicket();
         double profit1=OrderProfit();
         double price1=OrderOpenPrice();
         if(OrderType()==OP_BUY)
           {
            OrderSelect(i+1,SELECT_BY_POS,MODE_TRADES);
            ticket2=OrderTicket();
            if(OrderType()==OP_SELLSTOP)//---�������� �������� �� ������� �1:
              {
               OrderModify(OrderTicket(),OrderOpenPrice(),0,0,0,CLR_NONE);
               if(profit1>�������_�������&&MathAbs(Close[1]-Open[1])<=������������_�������*pip)
                 {
                  OrderClose(ticket1,�����,Bid,3,CLR_NONE);
                  OrderDelete(ticket2);
                 }
               if(MathAbs(Close[1]-Open[1])<=������������_�������*pip&&MathAbs(Close[2]-Open[2])<=������������_�������*pip)
                 {
                  OrderClose(ticket1,�����,Bid,3,CLR_NONE);
                  OrderDelete(ticket2);
                 }
               if(profit1>=�����������_��������)
                 {
                  OrderClose(ticket1,�����,Bid,3,CLR_NONE);
                  OrderDelete(ticket2);
                 }
              }
            if(OrderType()==OP_SELL)
              {//---����� �������� �����:
               OrderClose(ticket1,�����,Bid,3,CLR_NONE);
               OrderClose(ticket2,�����,Ask,3,CLR_NONE);
              }
           }
         OrderSelect(i,SELECT_BY_POS,MODE_TRADES);
         if(OrderType()==OP_BUYSTOP)
           {
            OrderSelect(i+1,SELECT_BY_POS,MODE_TRADES);
            ticket2=OrderTicket();
            if(OrderType()==OP_SELL)//---�������� �������� �� ������� �2:
              {
               OrderModify(ticket1,price1,0,0,0,CLR_NONE);
               double profit2=OrderProfit();
               if(profit2>�������_�������&&MathAbs(Open[1]-Close[1])<=������������_�������*pip)
                 {
                  OrderClose(ticket2,�����,Bid,3,CLR_NONE);
                  OrderDelete(ticket1);
                 }
               if(MathAbs(Open[1]-Close[1])<=������������_�������*pip&&MathAbs(Open[2]-Close[2])<=������������_�������*pip)
                 {
                  OrderClose(ticket2,�����,Bid,3,CLR_NONE);
                  OrderDelete(ticket1);
                 }
               if(profit2>=�����������_��������)
                 {
                  OrderClose(ticket2,�����,Bid,3,CLR_NONE);
                  OrderDelete(ticket1);
                 }
              }
           }
        }
     }
//----
   return(0);
  }
//+------------------------------------------------------------------+