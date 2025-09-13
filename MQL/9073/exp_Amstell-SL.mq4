//+------------------------------------------------------------------+
//|                                                  exp_Amstell.mq4 |
//|                                   Copyright � 2009, Yuriy Tokman |
//|                                            yuriytokman@gmail.com |
//+------------------------------------------------------------------+
#property copyright "Copyright � 2009, Yuriy Tokman"
#property link      "yuriytokman@gmail.com"

//�������� ��� ���� �����,������ day,�������� ,���� ���� �������� ���������� 
//��������� ������ ���� ������� ����,� ��������� ����� �� ���������� 1000 �������,
//�� �� � � ��������.��������� ����� sell,���� ��������� �������� sell ���� ������� 
//����. ��������� ������� �� ����� ��������

extern int    TakeProfit       = 30;            // ������ ����� � �������
extern int    StopLoss         = 30;           // ������ ����� � �������
extern double Lots             = 0.01;          // ������ ����

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
  int Magic=0;
  
for(int cnt=0;cnt<OrdersTotal();cnt++)// ���������� ��� ������
     {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);//����� ���������� ����� �������� � ���������� �������
      if( OrderSymbol()==Symbol() && OrderMagicNumber()==Magic)// ��������� �� ������ ������( ����� �� ���������� ��� ����� ����� ���������)
        {
         if(OrderType()==OP_BUY)//�������� ������� ���
           {
            if(Bid-OrderOpenPrice()>TakeProfit*Point || OrderOpenPrice()-Ask>StopLoss*Point)//
              {
               OrderClose(OrderTicket(),OrderLots(),Bid,3,Violet); //��������� �����
               return(0); // �������
              }//StopLoss
           }
          if(OrderType()==OP_SELL)//�������� ������� ����
           {
            if(OrderOpenPrice()-Ask>TakeProfit*Point || Bid-OrderOpenPrice()>StopLoss*Point)//
              {
               OrderClose(OrderTicket(),OrderLots(),Ask,3,Violet); //��������� �����
               return(0); // �������
              }
           }
         }
      }
//----
 int buy = 0, sell = 0;
//----
   if(!ExistPositions(NULL,OP_BUY))buy=1;
   else if(PriceOpenLastPos(NULL,OP_BUY)-Ask>10*Point)buy=1; 
   
   if(!ExistPositions(NULL,OP_SELL))sell=1;
   else if(Bid-PriceOpenLastPos(NULL,OP_SELL)>10*Point)sell=1;   
//----
    if(buy==1)
    OrderSend(Symbol(),OP_BUY,Lots,Ask,3,0,0,"",0,0,Green);
    
    if(sell==1)
    OrderSend(Symbol(),OP_SELL,Lots,Bid,3,0,0,"",0,0,Red);
//----
   return(0);
  }
//+------------------------------------------------------------------+
//|  �������� : ���������� ���� ������������� �������                          |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   (""   - ����� ������,                   |
//|                                     NULL - ������� ������)                 |
//|    op - ��������                   (-1   - ����� �������)                  |
//|    mn - MagicNumber                (-1   - ����� �����)                    |
//|    ot - ����� ��������             ( 0   - ����� ����� ��������)           |
//+----------------------------------------------------------------------------+
bool ExistPositions(string sy="", int op=-1, int mn=-1, datetime ot=0) {
  int i, k=OrdersTotal();

  if (sy=="0") sy=Symbol();
  for (i=0; i<k; i++) {
    if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) {
      if (OrderSymbol()==sy || sy=="") {
        if (OrderType()==OP_BUY || OrderType()==OP_SELL) {
          if (op<0 || OrderType()==op) {
            if (mn<0 || OrderMagicNumber()==mn) {
              if (ot<=OrderOpenTime()) return(True);
            }
          }
        }
      }
    }
  }
  return(False);
}
//+----------------------------------------------------------------------------+
//|  ������   : 19.02.2008                                                     |
//|  �������� : ���������� ���� �������� ��������� �������� �������.           |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   (""   - ����� ������,                   |
//|                                     NULL - ������� ������)                 |
//|    op - ��������                   (-1   - ����� �������)                  |
//|    mn - MagicNumber                (-1   - ����� �����)                    |
//+----------------------------------------------------------------------------+
double PriceOpenLastPos(string sy="", int op=-1, int mn=-1) {
  datetime t;
  double   r=0;
  int      i, k=OrdersTotal();

  if (sy=="0") sy=Symbol();
  for (i=0; i<k; i++) {
    if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) {
      if (OrderSymbol()==sy || sy=="") {
        if (OrderType()==OP_BUY || OrderType()==OP_SELL) {
          if (op<0 || OrderType()==op) {
            if (mn<0 || OrderMagicNumber()==mn) {
              if (t<OrderOpenTime()) {
                t=OrderOpenTime();
                r=OrderOpenPrice();
              }
            }
          }
        }
      }
    }
  }
  return(r);
}