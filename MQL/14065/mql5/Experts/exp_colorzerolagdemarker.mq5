//+------------------------------------------------------------------+
//|                                     Exp_ColorZerolagDeMarker.mq5 |
//|                               Copyright � 2015, Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright � 2015, Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "1.00"
//+---------------------------------------------------+
//|  �������� ���������                               | 
//+---------------------------------------------------+
#include <TradeAlgorithms.mqh>
//+---------------------------------------------------+
//| ������� ��������� ���������� ��������             |
//+---------------------------------------------------+
input double MM=0.1;              //���� ���������� �������� �� �������� � ������
input MarginMode MMMode=LOT;      //������ ����������� ������� ����
input int    StopLoss_=1000;      //�������� � �������
input int    TakeProfit_=2000;    //���������� � �������
input int    Deviation_=10;       //����. ���������� ���� � �������
input bool   BuyPosOpen=true;     //���������� ��� ����� � ����
input bool   SellPosOpen=true;    //���������� ��� ����� � ����
input bool   BuyPosClose=true;     //���������� ��� ������ �� ������
input bool   SellPosClose=true;    //���������� ��� ������ �� ������
//+--------------------------------------------------+
//| ������� ��������� ��� ����������                 |
//+--------------------------------------------------+
input ENUM_TIMEFRAMES InpInd_Timeframe=PERIOD_H3; //��������� ����������
input uint SignalBar=1;//����� ���� ��� ��������� ������� �����
input uint    smoothing=15;
//----
input double Factor1=0.05;
input uint    DeMarker_period1=8;
//----
input double Factor2=0.10;
input uint    DeMarker_period2=21;
//----
input double Factor3=0.16;
input uint    DeMarker_period3=34;
//----
input double Factor4=0.26;
input int    DeMarker_period4=55;
//----
input double Factor5=0.43;
input uint    DeMarker_period5=89;
//+--------------------------------------------------+
//---- ���������� ����� ���������� ��� �������� ������� ������� � �������� 
int TimeShiftSec;
//---- ���������� ����� ���������� ��� ������� �����������
int InpInd_Handle;
//---- ���������� ����� ���������� ������ ������� ������
int min_rates_total;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---- ��������� ������ ���������� ColorZerolagDeMarker
   InpInd_Handle=iCustom(Symbol(),InpInd_Timeframe,"ColorZerolagDeMarker",
                         smoothing,Factor1,DeMarker_period1,Factor2,DeMarker_period2,
                         Factor3,DeMarker_period3,Factor4,DeMarker_period4,Factor5,DeMarker_period5);
   if(InpInd_Handle==INVALID_HANDLE)
     {
      Print("�� ������� �������� ����� ���������� ColorZerolagDeMarker");
      return(INIT_FAILED);
     }

//---- ������������� ���������� ��� �������� ������� ������� � ��������  
   TimeShiftSec=PeriodSeconds(InpInd_Timeframe);
   
//---- ������������� ���������� ������ ������� ������
   uint PeriodBuffer[5];
   PeriodBuffer[0] = DeMarker_period1;
   PeriodBuffer[1] = DeMarker_period2;
   PeriodBuffer[2] = DeMarker_period3;
   PeriodBuffer[3] = DeMarker_period4;
   PeriodBuffer[4] = DeMarker_period5;
   min_rates_total=int(PeriodBuffer[ArrayMaximum(PeriodBuffer,0,WHOLE_ARRAY)])+2;
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
//----
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
   double Osc1[2],Osc2[2];
//---- ���������� ����������� ����������
   static bool Recount=true;
   static bool BUY_Open=false,BUY_Close=false;
   static bool SELL_Open=false,SELL_Close=false;
   static datetime UpSignalTime,DnSignalTime;
   static CIsNewBar NB;

//+----------------------------------------------+
//| ����������� �������� ��� ������              |
//+----------------------------------------------+
   if(!SignalBar || NB.IsNewBar(Symbol(),InpInd_Timeframe) || Recount) // �������� �� ��������� ������ ����
     {
      //---- ������� �������� �������
      BUY_Open=false;
      SELL_Open=false;
      BUY_Close=false;
      SELL_Close=false;
      Recount=false;

      //---- �������� ����� ����������� ������ � �������
      if(CopyBuffer(InpInd_Handle,0,SignalBar,2,Osc1)<=0) {Recount=true; return;}
      if(CopyBuffer(InpInd_Handle,1,SignalBar,2,Osc2)<=0) {Recount=true; return;}

      //---- ������� ������� ��� �������
      if(Osc1[1]>Osc2[1])
        {
         if(BuyPosOpen && Osc1[0]<Osc2[0]) BUY_Open=true;
         if(SellPosClose) SELL_Close=true;
         UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
        }

      //---- ������� ������� ��� �������
      if(Osc1[1]<Osc2[1])
        {
         if(SellPosOpen && Osc1[0]>Osc2[0]) SELL_Open=true;
         if(BuyPosClose) BUY_Close=true;
         DnSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
        }
     }

//+----------------------------------------------+
//| ���������� ������                            |
//+----------------------------------------------+
//---- ��������� ����
   BuyPositionClose(BUY_Close,Symbol(),Deviation_);

//---- ��������� ����   
   SellPositionClose(SELL_Close,Symbol(),Deviation_);

//---- ��������� ����
   BuyPositionOpen(BUY_Open,Symbol(),UpSignalTime,MM,MMMode,Deviation_,StopLoss_,TakeProfit_);

//---- ��������� ����
   SellPositionOpen(SELL_Open,Symbol(),DnSignalTime,MM,MMMode,Deviation_,StopLoss_,TakeProfit_);
//----
  }
//+------------------------------------------------------------------+
