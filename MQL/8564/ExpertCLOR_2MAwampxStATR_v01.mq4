//+----------------------------------------------------------------------------+
//|                                              ExpertCLOR_2MA&StATR_v01.mq4  |
//|                                                     ������ ������ as Vuki  |
//|                                                           f_kombi@mail.ru  |
//+----------------------------------------------------------------------------+
//|  �������� �������:                                                         |
//|   1. ��������� ����� �� ����������� 2-� �� (5 � 7 �� ���������);           |
//|   2. ������������� ������� ���� �� ���������� StopATR_auto;                |
//|   3. ��������� �������� ������� � ��������� ��� ���������� ��������� ������|
//|                                                                            | 
//|   ��������!!! �������� ������ ��������� �������� ������!!!                 |
//+----------------------------------------------------------------------------+
// ������� ���������:
// MA_CloseOnOff - ��������� (1) ��� ���������� (0) ������ �������� ������ �� ����������� ��  
// StATR_CloseOnOff - ��������� (1) ��� ���������� (0) ��������� � ��������� ����� �� ���������� StopATR_auto
// MA_Fast_Pe - ������ ������� ��
// MA_Fast_Ty - ��� ������� �� (0-SMA, 1-EMA, 2-SMMA(����������), 3-LWMA(����������))
// MA_Fast_Pr - ���� ������� �� (0-Close, 1-Open, 2-High, 3-Low, 4-HL/2, 5-HLC/3, 6-HLCC/4)
// MA_Slow_Pe - ������ ��������� ��
// MA_Slow_Ty - ��� ��������� �� (0-SMA, 1-EMA, 2-SMMA(����������), 3-LWMA(����������))
// MA_Slow_Pr - ���� ��������� �� (0-Close, 1-Open, 2-High, 3-Low, 4-HL/2, 5-HLC/3, 6-HLCC/4)
// TimeFrame  - ������� ��������� (1-�1, 5-�5, 15-�15� 60-�1, 240-�4)
// BezUb - ������� ������� � �������, ��� ������� ����� ����������� � ���������
// CountBarsForShift - �������� ��-�� StopATR_auto - ���������� � ����� ��� ������ ������ ����� �� �����  
// CountBarsForAverage - �������� ��-�� StopATR_auto - ���������� ����� ���������� ��� �������� �����
// Target - �������� ��-�� StopATR_auto - ���������� ���������� �������� �������� ���� ��� �������� �����
//-----
//  ��������� StopATR_auto ����������� ������ ���� � ����� Indicators ������ ��4
//  ��������� StopATR_auto �� ������ ����� �� ������, ��� �������� ��� �������
//-----
#property copyright "������ ������ as Vuki"
#property link      "f_kombi@mail.ru"
//-----
  extern int      MA_CloseOnOff      =1;
  extern int      StATR_CloseOnOff   =1;
  extern int      MA_Fast_Pe         =5;
  extern int      MA_Fast_Ty         =MODE_EMA;
  extern int      MA_Fast_Pr         =PRICE_CLOSE;
  extern int      MA_Slow_Pe         =7;
  extern int      MA_Slow_Ty         =MODE_EMA;
  extern int      MA_Slow_Pr         =PRICE_OPEN;
  extern int      TimeFrame          =PERIOD_M5;
  extern int      BezUb              =15;
  extern int      CountBarsForShift  =7;
  extern int      CountBarsForAverage=12;
  extern double   Target             =2.0;
//----
  int Opened=0;
  int OpenedBuy=0;
  int   OpenedSell=0;
  int OpenedBuyTicket=0;
  int   OpenedSellTicket=0;
  int CloseBuy=0;
  int CloseSell=0;
  int Closed=0;
  double StopL;
//expert initialization function
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  int init() 
  {
   return(0);
  }
//expert deinitialization function
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  int deinit() 
  {
   return(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  int start() 
  {
   int cnt;
   bool errbool=true;
   int errint=0;
//----
   CloseBuy=0; CloseSell=0;
   // ��������, ���� �� �������� �������� ��� ���������� ������
   OpenedBuy=0;
   OpenedSell=0;
     for(cnt=0; cnt < OrdersTotal(); cnt++) 
     {
      errbool=OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
      if(errbool!=true) Comment("������ ������ ������ �� �������!","\n","\n");
      if(OrderType()==OP_BUY && OrderSymbol()==Symbol())       {OpenedBuy=1; OpenedBuyTicket=OrderTicket();}
      if(OrderType()==OP_SELL && OrderSymbol()==Symbol())      {OpenedSell=1; OpenedSellTicket=OrderTicket();}
     }
   //���� ���� �������� ���, �� �������� �������� � �� � �� �������
     if(OpenedBuy==1) 
     {
      StopL=iCustom(NULL,TimeFrame,"StopATR_auto",Blue,Brown,false,CountBarsForShift,CountBarsForAverage,Target,2,0);
      StopL=NormalizeDouble(StopL,Digits);
      errbool=OrderSelect(OpenedBuyTicket, SELECT_BY_TICKET, MODE_TRADES);
      if(errbool!=true) Comment("������ ������ ������ �� �������!","\n","\n");
      // ������� � ��������� ������ ���      
        if(BezUb!=0 && (Bid - OrderOpenPrice())>=(BezUb*Point) && OrderStopLoss() < OrderOpenPrice()) 
        {
         errbool=OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()+Point,OrderTakeProfit(),0,White);
         errint=GetLastError();
         if(errbool!=true&&errint>1) Comment(errint," ������ ����������� �������� ������ ��� �� ���������!","\n","\n");
         else Comment("�������� ������� �������� ������ ��� � ���������!\n");
         return(0);
        }
      // ����������� ����� �� ������� ������ ���
        if(OrderStopLoss() < StopL && StATR_CloseOnOff==1) 
        {
         errbool=OrderModify(OrderTicket(),OrderOpenPrice(),StopL,OrderTakeProfit(),0,White);
         errint=GetLastError();
         if(errbool!=true&&errint>1) Comment(errint," ������ ����������� �������� ������ ��� �� �������!","\n","\n");
         return(0);
        }
     }
   //���� ���� �������� ����, �� �������� �������� � �� � �� �������
     if(OpenedSell==1) 
     {
      StopL=iCustom(NULL,TimeFrame,"StopATR_auto",Blue,Brown,false,CountBarsForShift,CountBarsForAverage,Target,1,0);
      StopL=NormalizeDouble(StopL,Digits);
      errbool=OrderSelect(OpenedSellTicket, SELECT_BY_TICKET, MODE_TRADES);
      if(errbool!=true) Comment("������ ������ ������ �� �������!","\n","\n");
      // ������� � ��������� ������ ����      
        if(BezUb!=0 && (OrderOpenPrice() - Ask)>=(BezUb*Point) && OrderStopLoss() > OrderOpenPrice()) 
        {
         errbool=OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()-Point,OrderTakeProfit(),0,White);
         errint=GetLastError();
         if(errbool!=true&&errint>1) Comment(errint," ������ ����������� �������� ������ ���� �� ���������!","\n","\n");
         else Comment("�������� ������� �������� ������ ���� � ���������!\n");
         return(0);
        }
      // ����������� ����� �� ������� ������ ����
        if(StATR_CloseOnOff==1) 
        {
         //        Comment("����� ��������! StopL = ",StopL,"\n");
           if(OrderStopLoss()==0 || OrderStopLoss() > StopL) {
            errbool=OrderModify(OrderTicket(),OrderOpenPrice(),StopL,OrderTakeProfit(),0,White);
            errint=GetLastError();
            if(errbool!=true&&errint>1) Comment(errint," ������ ����������� �������� ������ ���� �� �������!","\n","\n");
            return(0);
           }
        }
     }
   if(MA_CloseOnOff==1) FunctionSignalClose();
   // ���� ���� ������ �� ��������, ���������
     if((OpenedBuy==1&&OpenedSell==0&&CloseBuy==1&&CloseSell==0)||(OpenedBuy==0&&OpenedSell==1&&CloseBuy==0&&CloseSell==1)) 
     {
        if(OpenedBuy==1) 
        {
         errbool=OrderSelect(OpenedBuyTicket, SELECT_BY_TICKET, MODE_TRADES);
         if(errbool!=true) Comment("������ ������ ������ �� �������!","\n","\n");
        }
        if(OpenedSell==1) 
        {
         errbool=OrderSelect(OpenedSellTicket, SELECT_BY_TICKET, MODE_TRADES);
         if(errbool!=true) Comment("������ ������ ������ �� �������!","\n","\n");
        }
      Closed=FunctionCloseTrade(OrderTicket(),OrderType(),OrderLots());
      if(Closed==1) OpenedBuy=0;
      if(Closed==2) OpenedSell=0;
      return(0);
     }
   return(0);
  }
// ������� �������� �������� ������� ���, ������� ����
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  void FunctionSignalClose() 
  {
   double maFast1,maSlow1,maFast2,maSlow2;
   maFast1=iMA(NULL,TimeFrame,MA_Fast_Pe,0,MA_Fast_Ty,MA_Fast_Pr,1);
   maSlow1=iMA(NULL,TimeFrame,MA_Slow_Pe,0,MA_Slow_Ty,MA_Slow_Pr,1);
   maFast2=iMA(NULL,TimeFrame,MA_Fast_Pe,0,MA_Fast_Ty,MA_Fast_Pr,2);
   maSlow2=iMA(NULL,TimeFrame,MA_Slow_Pe,0,MA_Slow_Ty,MA_Slow_Pr,2);
     if(OpenedBuy==1) 
     {
      if(maFast1<=maSlow1 && maFast2 > maSlow2) {CloseBuy=1; CloseSell=0;}
     }
     if(OpenedSell==1) 
     {
      if(maFast1>=maSlow1 && maFast2 < maSlow2) {CloseSell=1; CloseBuy=0;}
     }
   return;
  }
// ������� �������� ������� ��� ��� ����. ���������� 1 ���� ������ ���, 2 ���� ������ ����, 0 ���� ������
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  int FunctionCloseTrade(int Tic, int Type, double Lot) 
  {
   int Ret;
   double PrCl;
   color ArCo;
   bool errbool;
//----
     if(Type==OP_BUY) 
     {
      PrCl=Bid;
      ArCo=Aqua;
      Ret=1;
     }
     if(Type==OP_SELL) 
     {
      PrCl=Ask;
      ArCo=Aqua;
      Ret=2;
     }
   errbool=OrderClose(Tic,Lot,PrCl,10,ArCo);
     if(errbool!=true) 
     {
      Comment("������ ",GetLastError()," �������� ��������� ������!","\n","\n");
      return(0);
     }
   return(Ret);
  }
//+------------------------------------------------------------------+