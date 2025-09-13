//+------------------------------------------------------------------+
//|                                                  MACD_signal.mq4 |
//|                                                           tom112 |
//|                                            tom112@mail.wplus.net |
//+------------------------------------------------------------------+
#property copyright "tom112"
#property link "tom112@mail.wplus.net"
//---- input parameters
extern double TakeProfit = 10;
extern double Lots = 10;
extern double TrailingStop = 25;
extern int Pfast = 9;
extern int Pslow = 15;
extern int Psignal = 8;
extern double LEVEL = 0.004;
double Points;
//+------------------------------------------------------------------+
//| expert initialization function |
//+------------------------------------------------------------------+
int init()
  {
   Points = MarketInfo(Symbol(), MODE_POINT);
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert deinitialization function |
//+------------------------------------------------------------------+
int deinit()
  {
   return(0);
  }
//+------------------------------------------------------------------+
//| expert start function |
//+------------------------------------------------------------------+
int start()
  {
   double MacdCurrent = 0, MacdPrevious = 0, SignalCurrent = 0;
   double SignalPrevious = 0, MaCurrent = 0, MaPrevious = 0;
   int cnt = 0, total;
   int i1, pp, shift;
   double Tv[2][500] ;
   double Range, rr, Delta, Delta1, val3;
// ��������� �������� ������
// ����� �������������� ��� ������� �������� �� ���������� ������� �
// ������������ ��������� �������� ������� ���������� (Lots, StopLoss,
// TakeProfit, TrailingStop)
// � ����� ������ ��������� ������ TakeProfit
   if(Bars < 100)
     {
       Print("bars less than 100");
       return(0); // �� ������� ����� 100 �����
     }
   if(TakeProfit < 10)
     {
       Print("TakeProfit less than 10");
       return(0); // ��������� TakeProfit
     }
   Range = iATR(NULL, 0, 200, 1);
   rr = Range*LEVEL;
   Delta = iMACD(NULL, 0, Pfast, Pslow, Psignal, PRICE_CLOSE, MODE_MAIN, 0)-
           iMACD(NULL, 0, Pfast, Pslow, Psignal, PRICE_CLOSE, MODE_SIGNAL, 0);
   Delta1 = iMACD(NULL, 0, Pfast, Pslow, Psignal, PRICE_CLOSE, MODE_MAIN, 1)-
            iMACD(NULL, 0, Pfast, Pslow, Psignal, PRICE_CLOSE, MODE_SIGNAL, 1);
// ������ ���� ������������ - � ����� ��������� �������� ��������?
// ��������, ���� �� ����� �������� ������� ��� ������?
   if(OrdersTotal() < 1) 
     {
       // ��� �� ������ ��������� ������
       // �� ������ ������ ��������, ���� � ��� ��������� ������ �� �����?
       // �������� 1000 ����� ��� �������, ������ ����� ������� 1 ���
       if(AccountFreeMargin() < (1000*Lots))
         {
           Print("We have no money");
           return(0); // ����� ��� - �������
         }
       // ��������, �� ������� �� ����� �������� ���������?
       // ���� ��������� ��� ��������� ����� ��� 5 �����(5*60=300 ���)
       // �����, �� �������
       // If((CurTime-LastTradeTime)<300) return(0);
       // ��������� �� ����������� ������ � ������� ������� (BUY)
       if(Delta > rr && Delta1 < rr )
         {
           OrderSend(Symbol(), OP_BUY, Lots, Ask, 3, 0, Ask + TakeProfit*Points, 
                     "macd signal", 16384, 0, Red); // ���������
           if(GetLastError() == 0)
               Print("Order opened : ", OrderOpenPrice());
           return(0); // �������, ��� ��� ��� ����� ����� ���������� �������� ��������
           // �������� 10-�� ��������� ������� �� ���������� �������� ��������
         }
       // ��������� �� ����������� ������ � �������� ������� (SELL)
       if(Delta < -rr && Delta1 > -rr )
         {
           OrderSend(Symbol(), OP_SELL, Lots, Bid, 3, 0, Bid - TakeProfit*Points, 
                     "macd sample", 16384, 0, Red); // ���������
           if(GetLastError() == 0)
               Print("Order opened : ", OrderOpenPrice());
           return(0); // �������
         }
       // ����� �� ��������� �������� �� ����������� �������� ����� �������.
       // ����� ������� ������� �� ���� � ������ ������� �� Exit, ��� ���
       // ��� ����� ������������� ������
       return(0);
     }
   // ��������� � ������ ����� �������� - �������� �������� �������
   // '����� ��������� ����� � �����, �� ����� - ��� ������...'
   total = OrdersTotal();
   for(cnt = 0; cnt < total; cnt++)
     {
       OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
       if(OrderType() <= OP_SELL && // ��� �������� �������? OP_BUY ��� OP_SELL 
          OrderSymbol()==Symbol())  // ���������� ���������?
         {
           if(OrderType() == OP_BUY) // ������� ������� �������
             {
               // ��������, ����� ��� ���� �����������?
               if(Delta < 0)
                 {
                   // ��������� �������
                   OrderClose(OrderTicket(), OrderLots(), Bid, 3, Violet); 
                   return(0); // �������
                 }
               // �������� - ����� �����/����� ��� �������� ���� �������?
               if(TrailingStop > 0) // ������������ �������� � ���������� ������������
                 { // ������ �� ���� ��� ���������
                   if(Bid - OrderOpenPrice() > Points*TrailingStop)
                     {
                       if(OrderStopLoss() < Bid - Points*TrailingStop)
                         {
                           OrderModify(OrderTicket(), OrderOpenPrice(), 
                                       Bid - Points*TrailingStop, 
                                       OrderTakeProfit(), 0, Red);
                           return(0);
                         }
                     }
                 }
             }
           else // ����� ��� �������� �������
             {
               // ��������, ����� ��� ���� �����������?
               if(Delta > 0)
                 {
                   // ��������� �������
                   OrderClose(OrderTicket(), OrderLots(), Ask, 3, Violet); 
                   return(0); // �������
                 }
               // �������� - ����� �����/����� ��� �������� ���� �������?
               if(TrailingStop > 0) // ������������ �������� � ���������� ������������
                 { // ������ �� ���� ��� ���������
                   if((OrderOpenPrice() - Ask) > (Points*TrailingStop))
                     {
                       if(OrderStopLoss() == 0.0 || OrderStopLoss() > 
                          (Ask + Points*TrailingStop))
                         {
                           OrderModify(OrderTicket(), OrderOpenPrice(), 
                                       Ask + Points*TrailingStop, 
                                       OrderTakeProfit(), 0, Red);
                           return(0);
                         }
                     }
                 }
             }
         }
     }
   return(0);
  }
// the end.
//+------------------------------------------------------------------+

