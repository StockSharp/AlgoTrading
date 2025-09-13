//+------------------------------------------------------------------+
//|                                                     Pipsover.mq4 |
//|                              Copyright � 2006, Yury V. Reshetov. |
//|                                       http://betaexpert.narod.ru |
//+------------------------------------------------------------------+
#property copyright "Copyright � 2006, Yury V. Reshetov. ICQ: 282715499"
#property link      "http://betaexpert.narod.ru"

//---- input parameters
// ������
extern double lots = 0.1;
// ������
extern double stoploss = 70;
// �������
extern double takeprofit = 140;
// ���������� ������� �������� ���������� ������� ��� �������� �������
extern double openlevel = 55;
// ���������� ������� �������� ���������� ������� ��� ����������� �������
extern double closelevel = 90;
// ����� ���������� ����
static int prevtime = 0;

//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
   // ����, ����� ������������ ����� ���
   if (Time[0] == prevtime) return(0);
   prevtime = Time[0];
//---- 
   // 20 ��������� ������
   double ma = iMA(NULL, 0, 20, 0, MODE_SMA, PRICE_CLOSE, 0);
   // ���������� �������� ���������� �������
   double ch = iCustom(NULL, 0, "Chaikin", 0, 0, 1);
   
   // ��� �������� �������
   if (OrdersTotal() < 1) {
      int res = 0;
      // ���� �������� ���������� ������� ��������� � ������� ������������� ��������
      // ������ ���� ���������������
      // ��������
      if (Close[1] > Open[1] && Low[1] < ma && ch < -openlevel) {
         res=OrderSend(Symbol(), OP_BUY, lots ,Ask, 3, Ask - stoploss * Point, Bid + takeprofit * Point, "Pipsover", 888, 0, Blue);
         return(0);
      }
      // ���� �������� ���������� ������� ��������� � ������� ������������� ��������
      // ������ ���� ���������������
      // �������
      if (Close[1] < Open[1] && High[1] > ma &&  ch > openlevel) {
         res=OrderSend(Symbol(), OP_SELL, lots ,Bid, 3, Bid + stoploss * Point, Ask - takeprofit * Point, "Pipsover", 888, 0, Red);
         return(0);
      }
   } else { // ���� �������� �������. ����� ���� ���� ���������������?
   
      // ���� ������� ����� ���� �������
      if (OrdersTotal() > 1) return(0);
   
      OrderSelect(0, SELECT_BY_POS, MODE_TRADES);
   
      // ������ �� �����, ���������� ������� �������
      if (OrderType() == OP_BUY  && Close[1] < Open[1] && High[1] > ma &&  ch > closelevel) {
         res=OrderSend(Symbol(), OP_SELL, lots ,Bid, 3, Bid + stoploss * Point, Ask - takeprofit * Point, "Pipsover", 888, 0, Red);
         return(0);
      }
      
      // ������ �� �����, ���������� ������� �������
      if (OrderType() == OP_SELL && Close[1] > Open[1] && Low[1] < ma && ch < -closelevel) {
         res=OrderSend(Symbol(), OP_BUY, lots ,Ask, 3, Ask - stoploss * Point, Bid + takeprofit * Point, "Pipsover", 888, 0, Blue);
         return(0);
      }
   }
   
//---- ��� � ������ �������
   return(0);
  }
//+------------------------------------------------------------------+