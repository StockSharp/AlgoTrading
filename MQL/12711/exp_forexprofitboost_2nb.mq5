//+------------------------------------------------------------------+
//|                                     Exp_ForexProfitBoost_2nb.mq5 |
//|                               Copyright � 2015, Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright � 2015, Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "1.00"
//+-----------------------------------------------+
//| �������� ���������                            |
//+-----------------------------------------------+
#include <TradeAlgorithms.mqh>
//+-----------------------------------------------+
//| ������� ��������� ���������� ��������         |
//+-----------------------------------------------+
input double MM=0.1;              // ���� ���������� �������� �� �������� � ������
input MarginMode MMMode=LOT;      // ������ ����������� ������� ����
input int    StopLoss_=1000;      // Stop Loss � �������
input int    TakeProfit_=2000;    // Take Profit � �������
input int    Deviation_=10;       // ����. ���������� ���� � �������
input bool   BuyPosOpen=true;     // ���������� ��� ����� � ����
input bool   SellPosOpen=true;    // ���������� ��� ����� � ����
input bool   BuyPosClose=true;    // ���������� ��� ������ �� ������
input bool   SellPosClose=true;   // ���������� ��� ������ �� ������
//+-----------------------------------------------+
//| ������� ��������� ��� ����������              |
//+-----------------------------------------------+
input ENUM_TIMEFRAMES InpInd_Timeframe=PERIOD_H6; // ��������� ����������
input uint SignalBar=1;                           // ����� ���� ��� ��������� ������� �����
//--- ��������� ����������� �������� 1
input uint   MAPeriod1=7;
input  ENUM_MA_METHOD   MAType1=MODE_EMA;
input ENUM_APPLIED_PRICE   MAPrice1=PRICE_CLOSE;
//--- ��������� ����������� �������� 2
input uint   MAPeriod2=21;
input  ENUM_MA_METHOD   MAType2=MODE_SMA;
input ENUM_APPLIED_PRICE   MAPrice2=PRICE_CLOSE;
input uint BBPeriod=15;
input double BBDeviation=1;
input uint BBShift=1;

input int Shift=0;                                // ����� ���������� �� ����������� � �����
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
//--- ��������� ������ ���������� ForexProfitBoost_2nb
   InpInd_Handle=iCustom(Symbol(),InpInd_Timeframe,"ForexProfitBoost_2nb",MAPeriod1,MAType1,MAPrice1,MAPeriod2,MAType2,MAPrice2,BBPeriod,BBDeviation,BBShift);
   if(InpInd_Handle==INVALID_HANDLE)
     {
      Print("�� ������� �������� ����� ���������� ForexProfitBoost_2nb");
      return(INIT_FAILED);
     }
//--- ������������� ���������� ��� �������� ������� ������� � ��������  
   TimeShiftSec=PeriodSeconds(InpInd_Timeframe);
//--- ������������� ���������� ������ ������� ������
   min_rates_total=int(MathMax(BBPeriod,MathMax(MAPeriod1,MAPeriod2)));
   min_rates_total+=int(1+2+SignalBar);
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
      //--- ���������� ��������� ����������
      double Up[2],Dn[2];
      //--- �������� ����� ����������� ������ � �������
      if(CopyBuffer(InpInd_Handle,2,SignalBar,2,Dn)<=0) {Recount=true; return;}
      if(CopyBuffer(InpInd_Handle,4,SignalBar,2,Up)<=0) {Recount=true; return;}
      //--- ������� ������� ��� �������
      if(Up[1]!=EMPTY_VALUE)
        {
         if(BuyPosOpen && Dn[0]!=EMPTY_VALUE) BUY_Open=true;
         if(SellPosClose) SELL_Close=true;
         UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
        }
      //--- ������� ������� ��� �������
      if(Dn[1]!=EMPTY_VALUE)
        {
         if(SellPosOpen && Up[0]!=EMPTY_VALUE) SELL_Open=true;
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
