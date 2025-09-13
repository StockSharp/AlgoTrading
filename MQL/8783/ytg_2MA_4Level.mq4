//+------------------------------------------------------------------+
//|                                               ytg_2MA_4Level.mq4 |
//|                                                     Yuriy Tokman |
//|                                            yuriytokman@gmail.com |
//+------------------------------------------------------------------+
#property copyright "Yuriy Tokman"
#property link      "yuriytokman@gmail.com"

/*
2 SMA ������ � ����������� 14 ������ � 180
����� ���� ������������ �����:
SMA 180 + 250 ������� �� Y
SMA 180 + 500 ������� �� Y
SMA 180 - 250 ������� �� Y
SMA 180 - 500 ������� �� Y
�������� ��� ���: ��� ����������� MA14 ����� ����� ���������� ���� �������, ���� �������*/

extern string _____1_____ = "�������� ��������� ";
extern int ����_������ =   130; 
extern int ����_���� =    1000;
extern int ���� = 1;
extern string _____2_____ = "��������� ����������� ";
extern int ���������_���          = 1;
extern int ������_�������_��      = 14;
extern int �����_�������_��       = 2;//0-3
extern int �����_�����_�������_�� = 4;//0-6
extern int ������_�����_��        =180;
extern int �����_�����_��         =2;//0-3
extern int �����_�����_�����_��   =4;//0-6
extern string _____3_____ = "��������� ������� ";
extern int �������_1      = 500;
extern int �������_2      = 250;
extern int ������_1       = 500;
extern int ������_2       = 250;

#include <stdlib.mqh>        // ����������� ���������� ��4

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
   if(!IsDemo()) 
   {
    Comment("������ ������ ��� ������");
    return(0);
   }
   else Comment("�������� ������");

   if(!ExistPositions()) 
     {
     
      if( GetSignal()==1)OpenPosition("",OP_BUY,����,Bid-����_����*Point,Ask+����_������*Point);

      if( GetSignal()==-1)OpenPosition("",OP_SELL,����,Ask+����_����*Point,Bid-����_������*Point);
      
     }   
//----
   return(0);
  }
//+------------------------------------------------------------------+

 int GetSignal()
   {
    double MA_1_0=iMA(Symbol(),0,������_�������_��,0,�����_�������_��,�����_�����_�������_��,���������_���);
    double MA_1_1=iMA(Symbol(),0,������_�������_��,0,�����_�������_��,�����_�����_�������_��,���������_���+1);
    double MA_2_0=iMA(Symbol(),0,������_�����_��,0,�����_�����_��,�����_�����_�����_��,���������_���);
    double MA_2_1=iMA(Symbol(),0,������_�����_��,0,�����_�����_��,�����_�����_�����_��,���������_���+1);
   
    
    int vSignal = 0;
    if(MA_1_1<=MA_2_1&&MA_1_0>MA_2_0)vSignal = 1;//up
    else
    if(MA_1_1<=MA_2_1+�������_1*Point&&MA_1_0>MA_2_0+�������_1*Point)vSignal = 1;//up
    else
    if(MA_1_1<=MA_2_1+�������_2*Point&&MA_1_0>MA_2_0+�������_2*Point)vSignal = 1;//up    
    else
    if(MA_1_1<=MA_2_1-������_1*Point&&MA_1_0>MA_2_0-������_1*Point)vSignal = 1;//up
    else
    if(MA_1_1<=MA_2_1-������_2*Point&&MA_1_0>MA_2_0-������_2*Point)vSignal = 1;//up    
    
    
     
    else
    if(MA_1_1>=MA_2_1&&MA_1_0<MA_2_0) vSignal =-1;//down
    else
    if(MA_1_1>=MA_2_1+�������_1*Point&&MA_1_0<MA_2_0+�������_1*Point) vSignal =-1;//down
    else
    if(MA_1_1>=MA_2_1+�������_2*Point&&MA_1_0<MA_2_0+�������_2*Point) vSignal =-1;//down
    else
    if(MA_1_1>=MA_2_1-������_1*Point&&MA_1_0<MA_2_0-������_1*Point) vSignal =-1;//down
    else
    if(MA_1_1>=MA_2_1-������_2*Point&&MA_1_0<MA_2_0-������_2*Point) vSignal =-1;//down    
    
        
    return (vSignal);
   }
   
//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 06.03.2008                                                     |
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
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������  : 13.06.2007                                                      |
//|  �������� : �������� �������. ������ ������� ��� ������ �� �������.        |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   ("" - ������� ������)                   |
//|    op - ��������                                                           |
//|    ll - ���                                                                |
//|    sl - ������� ����                                                       |
//|    tp - ������� ����                                                       |
//|    mn - MagicNumber                                                        |
//+----------------------------------------------------------------------------+
void OpenPosition(string sy, int op, double ll, double sl=0, double tp=0, int mn=0) {
  color  clOpen;
  double pp;
  int    err, ticket;
 
  if (sy=="") sy=Symbol();
  if (op==OP_BUY) {
    pp=MarketInfo(sy, MODE_ASK); clOpen=Green;
  } else {
    pp=MarketInfo(sy, MODE_BID); clOpen=Red;
  }
  ticket=OrderSend(sy, op, ll, pp,5, sl, tp, "", mn, 0, clOpen);
  if (ticket<0) {
    err=GetLastError();
    Print("Error(",err,") open ",GetNameOP(op),": ",ErrorDescription(err));
    Print("Ask=",Ask," Bid=",Bid," sy=",sy," ll=",ll,
          " pp=",pp," sl=",sl," tp=",tp," mn=",mn);
  }
}

//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 01.09.2005                                                     |
//|  �������� : ���������� ������������ �������� ��������                      |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    op - ������������� �������� ��������                                    |
//+----------------------------------------------------------------------------+
string GetNameOP(int op) {
  switch (op) {
    case OP_BUY      : return("Buy");
    case OP_SELL     : return("Sell");
    case OP_BUYLIMIT : return("Buy Limit");
    case OP_SELLLIMIT: return("Sell Limit");
    case OP_BUYSTOP  : return("Buy Stop");
    case OP_SELLSTOP : return("Sell Stop");
    default          : return("Unknown Operation");
  }
} 