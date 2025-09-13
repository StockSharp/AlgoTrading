//+------------------------------------------------------------------+
//|                                                   for_max_v2.mq4 |
//|                               Copyright � 2009, ������� �������� |
//|                                           http://kontrik.ucoz.ru |
//+------------------------------------------------------------------+
#property copyright "Copyright � 2009, ������� ��������"
#property link      "http://kontrik.ucoz.ru"

extern int bTp  =  100; //������ ������ � ���
extern int sTp  =  100; //������ ������ � ����
extern int zazor= 1; // ������ �� ��� ��� ��� ��������� ����� ��� ����������� �������
extern int Max_Search = 100; // ���������� ����� ��� ������ ����������� ����
extern int OrderExp = 8; // ����� ����� ����������� ������ � �����
extern int MagicType1   =  11111; // ����� ������� ���� ������
extern int MagicType2   =  22222; // ���� ������� ���� ������
extern int MagicType3   =  33333; // ����� �������� ���� ������
extern double StartLot  =  0.1; // ����� ������


#include <Kimiv.mqh>


int start()
  {
 
 MovingInWL(Symbol(), -1, -1);
 
 //------------ �������� �� ������� ������� � �������
  bool exist_pos1= ExistPositions(Symbol(),-1, MagicType1, 0);
  bool exist_pos2= ExistPositions(Symbol(),-1, MagicType2, 0);
  bool exist_pos3= ExistPositions(Symbol(),-1, MagicType3, 0);
  bool exist_ord1= ExistOrders(Symbol(),-1, MagicType1, 0);
  bool exist_ord2= ExistOrders(Symbol(),-1, MagicType2, 0);
  bool exist_ord3= ExistOrders(Symbol(),-1, MagicType3, 0);
 //------------ ����������� �������
  if (!exist_pos1 && !exist_ord1 && !vhod_1(0)==0)  
    { 
      SetOrder(Symbol(), OP_BUYSTOP, StartLot, iHigh(Symbol(),0,1)+zazor*Point, iLow(Symbol(),0,2)-zazor*Point, iHigh(Symbol(),0,1)+(zazor+bTp)*Point, MagicType1, TimeCurrent()+60*Period()*OrderExp); 
      SetArrow(241, Yellow, "������� ������� ����", 0, iLow(Symbol(),0,1)-15*Point, 2); 
      SetOrder(Symbol(), OP_SELLSTOP, StartLot, iLow(Symbol(),0,1)-zazor*Point, iHigh(Symbol(),0,2)+zazor*Point, iLow(Symbol(),0,1)-(zazor+sTp)*Point, MagicType1, TimeCurrent()+60*Period()*OrderExp); 
      SetArrow(242, Yellow, "������� ������� ����", 0, iHigh(Symbol(),0,1)+15*Point, 2);
    }
  //if (!exist_pos1 && !exist_ord1 && vhod_1(0)==-1) { SetOrder(Symbol(), OP_SELLSTOP, StartLot, iLow(Symbol(),0,1)-zazor*Point, iHigh(Symbol(),0,2)+zazor*Point, iLow(Symbol(),0,1)-(zazor+sTp)*Point, MagicType1, TimeCurrent()+60*Period()*OrderExp); SetArrow(242, Yellow, "������� ������� ����", 0, iHigh(Symbol(),0,1)+15*Point, 2); }
  if (!exist_pos2 && !exist_ord2 && !vhod_2(0)==0)  
    { 
      SetOrder(Symbol(), OP_BUYSTOP, StartLot, iHigh(Symbol(),0,1)+zazor*Point, iLow(Symbol(),0,1)-zazor*Point, iHigh(Symbol(),0,1)+(zazor+bTp)*Point, MagicType2, TimeCurrent()+60*Period()*OrderExp); 
      SetArrow(241, SteelBlue, "������� ������� ����", 0, iLow(Symbol(),0,1)-15*Point, 2); 
      SetOrder(Symbol(), OP_SELLSTOP, StartLot, iLow(Symbol(),0,1)-zazor*Point, iHigh(Symbol(),0,1)+zazor*Point, iLow(Symbol(),0,1)-(zazor+sTp)*Point, MagicType2, TimeCurrent()+60*Period()*OrderExp); 
      SetArrow(242, SteelBlue, "������� ������� ����", 0, iHigh(Symbol(),0,1)+15*Point, 2);
    }
  //if (!exist_pos2 && !exist_ord2 && vhod_2(0)==-1) { SetOrder(Symbol(), OP_SELLSTOP, StartLot, iLow(Symbol(),0,1)-zazor*Point, iHigh(Symbol(),0,1)+zazor*Point, iLow(Symbol(),0,1)-(zazor+sTp)*Point, MagicType2, TimeCurrent()+60*Period()*OrderExp); SetArrow(242, SteelBlue, "������� ������� ����", 0, iHigh(Symbol(),0,1)+15*Point, 2); }
 //if (!exist_pos3 && !exist_ord3 && vhod_3(0)==1)  OpenPosition(Symbol(), OP_BUY, StartLot, Ask-bSl*Point, Bid+bTp*Point, MagicType3);
 // if (!exist_pos3 && !exist_ord3 && vhod_3(0)==-1) OpenPosition(Symbol(), OP_SELL, StartLot, Bid+sSl*Point, Ask-sTp*Point, MagicType3);


 //------------ �������� ������� ���� �������� �������
 if (ExistOrders(Symbol(),OP_BUYSTOP, -1, 0) && ExistPositions(Symbol(),OP_SELL, -1, 0)) DeleteOrders(Symbol(), OP_BUYSTOP, -1); 
 if (ExistOrders(Symbol(),OP_SELLSTOP, -1, 0) && ExistPositions(Symbol(),OP_BUY, -1, 0)) DeleteOrders(Symbol(), OP_SELLSTOP, -1); 


  SimpleTrailing(Symbol(),-1,-1);
  return(0);
  }
  

int vhod_1(int i=0)
  {
    {
      if (iLowest(Symbol(), 0, MODE_LOW, Max_Search, 1) == 2 && iHigh(Symbol(), 0, i+2) > iHigh(Symbol(), 0, i+1) && iLow(Symbol(), 0, i+2) < iLow(Symbol(), 0, i+1)) return (1);
      else
      if (iHighest(Symbol(), 0, MODE_HIGH, Max_Search, 2) == 2 && iHigh(Symbol(), 0, i+2) > iHigh(Symbol(), 0, i+1) && iLow(Symbol(), 0, i+2) < iLow(Symbol(), 0, i+1)) return (-1);
      else return(0);
    }
  }
  
int vhod_2(int i=0)
  {
  if (iLowest(Symbol(), 0, MODE_LOW, Max_Search, 1) == 1 && iHigh(Symbol(), 0, i+1) > iHigh(Symbol(), 0, i+2) && iLow(Symbol(), 0, i+1) < iLow(Symbol(), 0, i+2)) return (1);
  else
  if (iHighest(Symbol(), 0, MODE_HIGH, Max_Search, 1) == 1 && iHigh(Symbol(), 0, i+2) <= iHigh(Symbol(), 0, i+1) && iLow(Symbol(), 0, i+2) > iLow(Symbol(), 0, i+1)) return (-1);
  else return(0);
  
  }
  
int vhod_3(int i = 0, int koeff = 5)
  {
  if ((iHigh(Symbol(), 0, i+1)-iLow(Symbol(), 0, i+1))/(iHigh(Symbol(), 0, i+1)-iClose(Symbol(), 0, i+1)) >= koeff  &&
      (iHigh(Symbol(), 0, i+1)-iLow(Symbol(), 0, i+1))/(iHigh(Symbol(), 0, i+1)-iOpen(Symbol(), 0, i+1))  >= koeff  &&
      (iHigh(Symbol(), 0, i+1)-iLow(Symbol(), 0, i+1))/(iOpen(Symbol(), 0, i+1)-iClose(Symbol(), 0, i+1)) >= koeff) return(1);
  else
  if ((iHigh(Symbol(), 0, i+1)-iLow(Symbol(), 0, i+1))/(iClose(Symbol(), 0, i+1)-iLow(Symbol(), 0, i+1))  >= koeff  &&
      (iHigh(Symbol(), 0, i+1)-iLow(Symbol(), 0, i+1))/(iOpen(Symbol(), 0, i+1) -iLow(Symbol(), 0, i+1))  >= koeff  &&
      (iHigh(Symbol(), 0, i+1)-iLow(Symbol(), 0, i+1))/(iOpen(Symbol(), 0, i+1) -iClose(Symbol(), 0, i+1))>= koeff) return(-1);
  else return(0);
  }

