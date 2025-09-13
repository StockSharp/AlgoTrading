//+------------------------------------------------------------------+
//|                                              Exp_CHOWithFlat.mq5 |
//|                               Copyright � 2015, Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright � 2015, Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "1.00"
//+---------------------------------------------------+
//| �������� ���������                                |
//+---------------------------------------------------+
#include <TradeAlgorithms.mqh>
//+---------------------------------------------------+
//| ���������� ������������                           |
//+---------------------------------------------------+
enum Smooth_Method
  {
   MODE_SMA_,  //SMA
   MODE_EMA_,  //EMA
   MODE_SMMA_, //SMMA
   MODE_LWMA_, //LWMA
   MODE_JJMA,  //JJMA
   MODE_JurX,  //JurX
   MODE_ParMA, //ParMA
   MODE_T3,    //T3
   MODE_VIDYA, //VIDYA
   MODE_AMA    //AMA
  };
//+---------------------------------------------------+
//| ������� ��������� ��������                        |
//+---------------------------------------------------+
input double MM=0.1;              // ���� ���������� �������� �� �������� � ������
input MarginMode MMMode=LOT;      // ������ ����������� ������� ����
input int    StopLoss_=1000;      // �������� � �������
input int    TakeProfit_=2000;    // ���������� � �������
input int    Deviation_=10;       // ����. ���������� ���� � �������
input bool   BuyPosOpen=true;     // ���������� ��� ����� � ������� �������
input bool   SellPosOpen=true;    // ���������� ��� ����� � �������� �������
input bool   BuyPosClose=true;    // ���������� ��� ������ �� ������� �������
input bool   SellPosClose=true;   // ���������� ��� ������ �� �������� �������
//+--------------------------------------------------+
//| ������� ��������� ��� ���������� CHOWithFlat     |
//+--------------------------------------------------+
input ENUM_TIMEFRAMES     InpInd_Timeframe=PERIOD_H4;  // ��������� ����������
input uint                SignalBar=1;                 // ����� ���� ��� ��������� ������� �����
input uint                BBPeriod=20;                 // ������ ��� ������� �����������
input double              StdDeviation=2.0;            // �������� �����������
input ENUM_APPLIED_PRICE  applied_price=PRICE_CLOSE;   // ��� ���� �����������
input Smooth_Method XMA_Method=MODE_SMA;               // ����� ����������
input uint FastPeriod=3;                               // ������ �������� ����������
input uint SlowPeriod=10;                              // ����� ���������� ����������
input int XPhase=15;                                   // �������� �����������
//---- ��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������;
input ENUM_APPLIED_VOLUME VolumeType=VOLUME_TICK;      // �����
input uint                MAPeriod=13;                 // ������ ���������� ���������� �����
input  ENUM_MA_METHOD     MAType=MODE_SMA;             // ��� ���������� ���������� �����
//+--------------------------------------------------+
//---- ���������� ������������� ���������� ��� �������� ������� ������� � �������� 
int TimeShiftSec;
//---- ���������� ������������� ���������� ��� ������� �����������
int InpInd_Handle;
//---- ���������� ������������� ���������� ������ ������� ������
int min_rates_total;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---- ��������� ������ ���������� CHOWithFlat
   InpInd_Handle=iCustom(Symbol(),InpInd_Timeframe,"CHOWithFlat",
                         BBPeriod,StdDeviation,applied_price,XMA_Method,FastPeriod,SlowPeriod,XPhase,VolumeType,MAPeriod,MAType,0,0);
   if(InpInd_Handle==INVALID_HANDLE)
     {
      Print("�� ������� �������� ����� ���������� CHOWithFlat");
      return(INIT_FAILED);
     }
//---- ������������� ���������� ��� �������� ������� ������� � ��������  
   TimeShiftSec=PeriodSeconds(InpInd_Timeframe);
//---- ������������� ���������� ������ ������� ������
   min_rates_total=int(MathMax(MathMax(BBPeriod,FastPeriod),SlowPeriod));
   min_rates_total+=int(1+2+SignalBar);
//--- ���������� �������������
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//----
   GlobalVariableDel_(Symbol());
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//---- �������� ���������� ����� �� ������������� ��� �������
   if(BarsCalculated(InpInd_Handle)<min_rates_total) return;
//---- ��������� ������� ��� ���������� ������ ������� IsNewBar() � SeriesInfoInteger()  
   LoadHistory(TimeCurrent()-PeriodSeconds(InpInd_Timeframe)-1,Symbol(),InpInd_Timeframe);
//---- ���������� ��������� ����������
   double Up[2],Dn[2];
//---- ���������� ����������� ����������
   static bool Recount=true;
   static bool BUY_Open=false,BUY_Close=false;
   static bool SELL_Open=false,SELL_Close=false;
   static datetime UpSignalTime,DnSignalTime;
   static CIsNewBar NB;
//---- ����������� �������� ��� ������
   if(!SignalBar || NB.IsNewBar(Symbol(),InpInd_Timeframe) || Recount) // �������� �� ��������� ������ ����
     {
      //---- ������� �������� �������
      BUY_Open=false;
      SELL_Open=false;
      BUY_Close=false;
      SELL_Close=false;
      Recount=false;
      //---- �������� ����� ����������� ������ � �������
      if(CopyBuffer(InpInd_Handle,0,SignalBar,2,Up)<=0) {Recount=true; return;}
      if(CopyBuffer(InpInd_Handle,1,SignalBar,2,Dn)<=0) {Recount=true; return;}
      //---- ������� ������� ��� �������
      if(Up[1]>Dn[1])
        {
         if(BuyPosOpen && Up[0]<Dn[0]) BUY_Open=true;
         if(SellPosClose) SELL_Close=true;
         UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
        }
      //---- ������� ������� ��� �������
      if(Up[1]<Dn[1])
        {
         if(SellPosOpen && Up[0]>Dn[0]) SELL_Open=true;
         if(BuyPosClose) BUY_Close=true;
         DnSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
        }
     }
//---- ���������� ������
//---- ��������� ������� �������
   BuyPositionClose(BUY_Close,Symbol(),Deviation_);
//---- ��������� �������� �������
   SellPositionClose(SELL_Close,Symbol(),Deviation_);
//---- ��������� ������� �������
   BuyPositionOpen(BUY_Open,Symbol(),UpSignalTime,MM,MMMode,Deviation_,StopLoss_,TakeProfit_);
//---- ��������� �������� �������
   SellPositionOpen(SELL_Open,Symbol(),DnSignalTime,MM,MMMode,Deviation_,StopLoss_,TakeProfit_);
  }
//+------------------------------------------------------------------+
