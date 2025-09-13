//+------------------------------------------------------------------+
//|                                                         Prev.mq4 |
//|                              Copyright � 2006, Yury V. Reshetov. |
//|                                       http://betaexpert.narod.ru |
//+------------------------------------------------------------------+
#property copyright "Copyright � 2006, Yury V. Reshetov. ICQ: 282715499"
#property link      "http://betaexpert.narod.ru"
//----
#include <stdlib.mqh>
//---- ������� ���������
// �� �������� �� ����� ������� � ��� ������ �� ����
extern double     TakeProfit=180.0;
// ���� �� ������������, �� ����� ������ ��� ������ �������
extern double     StopLoss=50.0;
// ����������� ���� ����� �������, ������� �� ����� ������� 
// ��� ����� �������� ��� �������
extern double     MaximumRisk=0.03;
// ����� �� ����� ���������� ����� ���������� ����������������� ����
static int        prevtime=0;
//+------------------------------------------------------------------+
//|              �������!                                            |
//+------------------------------------------------------------------+
  int start() 
  {
   // ��� ������ �� ������ ���������, ��� ������������� ����� ���
   // ����� ������ ����� ������� ������� � ��� �����,
   // ����� ���� ��������, ��� �� ��� ������.
   // � ���� ��, ���������� ��� ������������� �������� ����� 
   // ������ ��������� ��� �� ��������� - ���� ������ �� ������.
   if (Time[0]==prevtime) return(0);
   // �������� ������� ���
   prevtime=Time[0];
   // ������� ������� � ��� �������?
   int total=OrdersTotal();
   // ����� ������ 
   int ticket=-1;
     for(int cnt=0; cnt < total; cnt++) 
     {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
      // ���� ��� �������� ������� � ���������� ���������
        if(OrderType()<=OP_SELL && OrderSymbol()==Symbol()) 
        {
         ticket=OrderTicket();
        }
     }
   // ���� ���� ��� �������� �������, �� ����� ��������� �� �����
     if (ticket < 0) 
     {
      int cmd=OP_BUY;
      // ������� � ��� �������� �������?
      int htotal=HistoryTotal();
      // ���������� � ������� �����
        for(int i=0; i < htotal; i++) 
        {
           if (OrderSelect(i, SELECT_BY_POS, MODE_HISTORY)==false) 
           {
            Print("������ � ������� �����! �������� � �������! ����� �� ����� ���� ������� � ������ �����?");
            break;
           }
         // ���� ����� ������ ������� � �� �� �������� 
         // �� ��� ��������
         if ((OrderSymbol()!= Symbol()) || (OrderType() > OP_SELL))
            continue;
         // ���� ����� ��� ������ � ��������, �� ���� �� TakeProfit
           if (OrderProfit() > 0) 
           {
            // ������ ��������� ������� ����� � ��� �� �����������
            cmd=OrderType();
            } else { // � ��������� ������, 
            // ����� ��� �����������
            // � ��� ���� ��
              if (OrderType()==OP_SELL) 
              {
               cmd=OP_BUY;
               }
                else 
               {
               cmd=OP_SELL;
              }
           }
        }
      // ��� ��� ������ ����������?
      double sar=iSAR(NULL, 0, 0.02, 0.2, 0);
      double adx=iADX(NULL, 0, 14, PRICE_CLOSE, MODE_MAIN, 0);
        if (cmd==OP_BUY) { // ���� ��������� ������� �������
         // ���� ����� ����� ������� ������������
         // � �� ����� ��� ��������
           if ((sar < Close[0]) && (adx < 20)) 
           {
            // ��������
            ticket=OrderSend(Symbol(),OP_BUY, LotsOptimized(), Ask, 3, Bid - StopLoss * Point, Ask + TakeProfit * Point, "TrendCapture", 16384, 0, Blue);
            // ������� � ������, ��� ������������ ������
              if(ticket > 0) 
              {
               if (OrderSelect(ticket, SELECT_BY_TICKET, MODE_TRADES))
                  Print("������� ������� ������� �� ���� : ",OrderOpenPrice());
              } else
               Print(ErrorDescription(GetLastError()));
            return(0);
           }
         }
          else 
         { // ���� ��������� �������� �������
         // ���� ����� ����� ������� �������� 
         // � �� ����� ��� ��������
           if ((sar > Close[0]) && (adx < 20)) 
           {
            // �������
            ticket=OrderSend(Symbol(),OP_SELL,LotsOptimized(),Bid,3,Ask + StopLoss * Point,Bid-TakeProfit*Point,"TrendCapture",16384,0,Red);
            // ������� � ������, ��� ������������ ������
              if(ticket > 0) 
              {
               if(OrderSelect(ticket, SELECT_BY_TICKET, MODE_TRADES))
                  Print("�������� ������� ������� �� ���� : ",OrderOpenPrice());
              }
               else
               Print(ErrorDescription(GetLastError()));
            return(0);
           }
        }
     }
   // ��������������
   double    Guard=5.0;
   OrderSelect(ticket, SELECT_BY_TICKET, MODE_TRADES);
     if(OrderType()==OP_BUY) 
     {  // ������� �������� �������
        // ���� ������� ���� ��������� ��������� ��������������             
        if(Bid-OrderOpenPrice()>Point*Guard) 
        {
         // ���� ������ ����� � ������
           if(OrderStopLoss() < OrderOpenPrice()) 
           {
            // �� ��� �����, �� �� � ������
            OrderModify(OrderTicket(), OrderOpenPrice(), OrderOpenPrice(), OrderTakeProfit(), 0, Blue);
            // �������
            return(0);
           }
        }
     }
   else // ���� ������� ��������
     {
      // ���� ������� ���� ��������� ��������� ��������������             
        if((OrderOpenPrice() - Ask) > (Point*Guard)) 
        {
         // ���� ������ ����� � ������
           if(OrderStopLoss() > OrderOpenPrice()) 
           {
            // �� ��� �����, �� �� � ������
            OrderModify(OrderTicket(), OrderOpenPrice(), OrderOpenPrice(), OrderTakeProfit(), 0, Red);
            // �������
            return(0);
           }
        }
     }
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| Money Management                                                 |
//+------------------------------------------------------------------+
double LotsOptimized()
  {
   double lot=0.1;
   int    losses=0;                  // number of losses orders without a break
//---- select lot size
   lot=NormalizeDouble(AccountFreeMargin()*MaximumRisk/1000.0,1);
//---- return lot size
   if (lot > 100) lot=100;
   if(lot < 0.1) lot=0.1;
   return(lot);
  }
//+----------------- ��� � ������ ������� ----------------+

