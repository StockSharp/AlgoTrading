//+------------------------------------------------------------------+
//|                                         Exp_TrendlessAG_Hist.mq5 |
//|                               Copyright � 2015, Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright � 2015, Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "1.00"
//+----------------------------------------------+
//| �������� ������� ����������                  |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+
//--- ���������� ���������� ������ CXMA �� ����� SmoothAlgorithms.mqh
CXMA XMA1;
//+----------------------------------------------+
//| �������� ���������                           | 
//+----------------------------------------------+
#include <TradeAlgorithms.mqh>
//+----------------------------------------------+
//| ���������� ������������                      |
//+----------------------------------------------+
enum Applied_price_ //��� ���������
  {
   PRICE_CLOSE_ = 1,     //Close
   PRICE_OPEN_,          //Open
   PRICE_HIGH_,          //High
   PRICE_LOW_,           //Low
   PRICE_MEDIAN_,        //Median Price (HL/2)
   PRICE_TYPICAL_,       //Typical Price (HLC/3)
   PRICE_WEIGHTED_,      //Weighted Close (HLCC/4)
   PRICE_SIMPL_,         //Simpl Price (OC/2)
   PRICE_QUARTER_,       //Quarted Price (HLOC/4) 
   PRICE_TRENDFOLLOW0_,  //TrendFollow_1 Price 
   PRICE_TRENDFOLLOW1_,  //TrendFollow_2 Price
   PRICE_DEMARK_         //Demark Price
  };
//+----------------------------------------------+
//| ������� ��������� ���������� ��������        |
//+----------------------------------------------+
input double MM=0.1;              // ���� ���������� �������� �� �������� � ������
input MarginMode MMMode=LOT;      // ������ ����������� ������� ����
input int    StopLoss_=1000;      // Stop Loss � �������
input int    TakeProfit_=2000;    // Take Profit � �������
input int    Deviation_=10;       // ����. ���������� ���� � �������
input bool   BuyPosOpen=true;     // ���������� ��� ����� � ����
input bool   SellPosOpen=true;    // ���������� ��� ����� � ����
input bool   BuyPosClose=true;    // ���������� ��� ������ �� ������
input bool   SellPosClose=true;   // ���������� ��� ������ �� ������
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input ENUM_TIMEFRAMES InpInd_Timeframe=PERIOD_H12; // ��������� ����������
input Smooth_Method XMA_Method1=MODE_EMA;          // ����� ����������  � ������� �����������
input int XLength1=7;                              // ������ ���������� ������� � ������� �����������                 
input int XPhase1=15;                              // �������� ���������� ������� � ������� �����������
//--- XPhase1: ��� JJMA ���������� � �������� -100 ... +100, ������ �� �������� ����������� �������� � ������� �����������
//--- XPhase1: ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input Applied_price_ IPC=PRICE_CLOSE;              // ������� ���� ���������� ������� � ������� �����������
input uint PointsCount=600;                        // ���������� ����� ��� ������� ����������. ������-���� ����������� ����� ��� ���������
input uint In100=90;                               // ������� % ����� ���������� ������ ������� � �������� +-100%
input Smooth_Method XMA_Method2=MODE_JJMA;         // ����� ����������� ����������
input int XLength2=5;                              // ������� �����������
input int XPhase2=100;                             // �������� �����������
//--- XPhase2: ��� JJMA ���������� � �������� -100 ... +100, ������ �� �������� ����������� ��������;
//--- XPhase2: ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input uint SignalBar=1;                            // ����� ���� ��� ��������� ������� �����
//+----------------------------------------------+
//--- ���������� ������������� ���������� ��� �������� ������� ������� � �������� 
int TimeShiftSec;
//--- ���������� ������������� ���������� ��� ������� �����������
int InpInd_Handle;
//--- ���������� ������������� ���������� ������ ������� ������
int min_rates_total;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//--- ��������� ������ ���������� TrendlessAG_Hist
   InpInd_Handle=iCustom(Symbol(),InpInd_Timeframe,"TrendlessAG_Hist",0);
   if(InpInd_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� TrendlessAG_Hist");
      return(INIT_FAILED);
     }
//--- ������������� ���������� ��� �������� ������� ������� � ��������  
   TimeShiftSec=PeriodSeconds(InpInd_Timeframe);
//--- ������������� ���������� ������ ������� ������
   int min_rates_1=XMA1.GetStartBars(XMA_Method1,XLength1,XPhase1);
   int min_rates_2=int(PointsCount+min_rates_1);
   min_rates_total=min_rates_2+XMA1.GetStartBars(XMA_Method2,XLength2,XPhase2)+2;
   min_rates_total+=int(3+SignalBar);
//---
   return(INIT_FAILED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---
   GlobalVariableDel_(Symbol());
//---
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//--- �������� ���������� ����� �� ������������� ��� �������
   if(BarsCalculated(InpInd_Handle)<min_rates_total) return;
//--- ��������� ������� ��� ���������� ������ ������� IsNewBar() � SeriesInfoInteger()  
   LoadHistory(TimeCurrent()-PeriodSeconds(InpInd_Timeframe)-1,Symbol(),InpInd_Timeframe);
//--- ���������� ��������� ����������
   double Value[3];
//--- ���������� ����������� ����������
   static bool Recount=true;
   static bool BUY_Open=false,BUY_Close=false;
   static bool SELL_Open=false,SELL_Close=false;
   static datetime UpSignalTime,DnSignalTime;
   static CIsNewBar NB;
//--- ����������� �������� ��� ������
   if(!SignalBar || NB.IsNewBar(Symbol(),InpInd_Timeframe) || Recount) // �������� �� ��������� ������ ����
     {
      //--- ������� �������� �������
      BUY_Open=false;
      SELL_Open=false;
      BUY_Close=false;
      SELL_Close=false;
      Recount=false;
      //--- �������� ����� ����������� ������ � �������
      if(CopyBuffer(InpInd_Handle,0,SignalBar,3,Value)<=0) {Recount=true; return;}
      //--- ������� ������� ��� �������
      if(Value[1]<Value[2])
        {
         if(BuyPosOpen && Value[0]>Value[1]) BUY_Open=true;
         if(SellPosClose) SELL_Close=true;
         UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
        }
      //--- ������� ������� ��� �������
      if(Value[1]>Value[2])
        {
         if(SellPosOpen && Value[0]<Value[1]) SELL_Open=true;
         if(BuyPosClose) BUY_Close=true;
         DnSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
        }
     }
//--- ���������� ������
//--- ��������� ����
   BuyPositionClose(BUY_Close,Symbol(),Deviation_);
//--- ��������� ����   
   SellPositionClose(SELL_Close,Symbol(),Deviation_);
//--- ��������� ����
   BuyPositionOpen(BUY_Open,Symbol(),UpSignalTime,MM,MMMode,Deviation_,StopLoss_,TakeProfit_);
//--- ��������� ����
   SellPositionOpen(SELL_Open,Symbol(),DnSignalTime,MM,MMMode,Deviation_,StopLoss_,TakeProfit_);
//---
  }
//+------------------------------------------------------------------+