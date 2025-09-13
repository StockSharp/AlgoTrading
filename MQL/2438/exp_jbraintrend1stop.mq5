//+------------------------------------------------------------------+
//|                                         Exp_JBrainTrend1Stop.mq5 |
//|                               Copyright � 2014, Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright � 2014, Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "1.00"
//+------------------------------------------------+
//| �������� ���������                             |
//+------------------------------------------------+
#include <TradeAlgorithms.mqh>
//+------------------------------------------------+
//| ������������ ��� ��������� ������� ����        |
//+------------------------------------------------+
/*enum MarginMode  - ������������ ��������� � ����� TradeAlgorithms.mqh
  {
   FREEMARGIN=0,     //MM �� ��������� ������� �� �����
   BALANCE,          //MM �� ������� ������� �� �����
   LOSSFREEMARGIN,   //MM �� ������� �� ��������� ������� �� �����
   LOSSBALANCE,      //MM �� ������� �� ������� ������� �� �����
   LOT               //��� ��� ���������
  }; */
//+------------------------------------------------+
//| ������� ��������� ���������� ��������          |
//+------------------------------------------------+
input double MM=0.1;              // ���� ���������� �������� �� �������� � ������
input MarginMode MMMode=LOT;      // ������ ����������� ������� ����
input int    StopLoss_=1000;      // Stop Loss � �������
input int    TakeProfit_=2000;    // Take Profit � �������
input int    Deviation_=10;       // ����. ���������� ���� � �������
input bool   BuyPosOpen=true;     // ���������� ��� ����� � ����
input bool   SellPosOpen=true;    // ���������� ��� ����� � ����
input bool   BuyPosClose=true;    // ���������� ��� ������ �� ������
input bool   SellPosClose=true;   // ���������� ��� ������ �� ������
//+------------------------------------------------+
//| ������� ��������� ���������� JBrainTrend1Stop  |
//+------------------------------------------------+
input ENUM_TIMEFRAMES InpInd_Timeframe=PERIOD_H4; // ��������� ����������
input int ATR_Period=7;                           // ������ ATR 
input int STO_Period=9;                           // ������ ����������
input ENUM_MA_METHOD MA_Method=MODE_SMA;          // ����� ����������
input ENUM_STO_PRICE STO_Price=STO_LOWHIGH;       // ����� ������� ��� 
input int Stop_dPeriod=3;                         // ���������� ������� ��� �����
input int Length_=7;                              // ������� JMA �����������                   
input int Phase_=100;                             // �������� JMA �����������
input uint SignalBar=1;                           // ����� ���� ��� ��������� ������� �����
//+------------------------------------------------+
int TimeShiftSec;
//--- ���������� ������������� ���������� ��� ������� �����������
int InpInd_Handle;
//--- ���������� ������������� ���������� ������ ������� ������
int min_rates_total;
//+------------------------------------------------------------------+
//| �������� ���������                                               |
//+------------------------------------------------------------------+
#include <TradeAlgorithms.mqh>
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//--- ��������� ������ ���������� JBrainTrend1Stop
   InpInd_Handle=iCustom(Symbol(),InpInd_Timeframe,"JBrainTrend1Stop",ATR_Period,STO_Period,MA_Method,STO_Price,Stop_dPeriod,Length_,Phase_);
   if(InpInd_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� JBrainTrend1Stop");
      return(INIT_FAILED);
     }
//--- ������������� ���������� ��� �������� ������� ������� � ��������  
   TimeShiftSec=PeriodSeconds(InpInd_Timeframe);
//--- ������������� ���������� ������ ������� ������
   min_rates_total=int(MathMax(MathMax(MathMax(ATR_Period,STO_Period),ATR_Period+Stop_dPeriod),30)+2);
   min_rates_total+=int(3+SignalBar);
//---
   return(INIT_SUCCEEDED);
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
//--- ���������� ����������� ����������
   int LastTrend;
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
      LastTrend=0;
      Recount=false;
      //--- ����� ���������� ����������� ��������
      int Bars_=Bars(Symbol(),InpInd_Timeframe);
      if(Bars_<min_rates_total) {Recount=true; return;}
      Bars_-=min_rates_total+3;
      //--- ���������� ��������� ����������
      double DnTrend[2],UpTrend[2];
      //--- �������� ����� ����������� ������ � �������
      if(CopyBuffer(InpInd_Handle,1,SignalBar,2,UpTrend)<=0) {Recount=true; return;}
      if(CopyBuffer(InpInd_Handle,0,SignalBar,2,DnTrend)<=0) {Recount=true; return;}
      //--- ������� ������� ��� �������
      if(UpTrend[1])
        {
         if(BuyPosOpen && DnTrend[0]) BUY_Open=true;
         if(SellPosClose) SELL_Close=true;
         UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
        }
      //--- ������� ������� ��� �������
      if(DnTrend[1])
        {
         if(SellPosOpen && UpTrend[0]) SELL_Open=true;
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
