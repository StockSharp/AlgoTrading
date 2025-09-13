//+------------------------------------------------------------------+
//|                                         Exp_Candles_Smoothed.mq5 |
//|                             Copyright � 2011,   Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright � 2011, Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "1.00"
//+------------------------------------------------+
//|  ���������� ������������                       |
//+------------------------------------------------+
enum Smooth_Method
  {
   MODE_SMA_,  // SMA
   MODE_EMA_,  // EMA
   MODE_SMMA_, // SMMA
   MODE_LWMA_, // LWMA
   MODE_JJMA,  // JJMA
   MODE_JurX,  // JurX
   MODE_ParMA, // ParMA
   MODE_T3,    // T3
   MODE_VIDYA, // VIDYA
   MODE_AMA,   // AMA
  };
//+------------------------------------------------+
//| ������� ��������� ��������                     |
//+------------------------------------------------+
input double MM=0.1;              //���� ���������� �������� �� �������� � ������, ������������� �������� - ������ ����
input int    StopLoss_=1000;      //�������� � �������
input int    TakeProfit_=2000;    //���������� � �������
input int    Deviation_=10;       //����. ���������� ���� � �������
input bool   BuyPosOpen=true;     //���������� ��� ����� � ����
input bool   SellPosOpen=true;    //���������� ��� ����� � ����
input bool   BuyPosClose=true;     //���������� ��� ������ �� ������
input bool   SellPosClose=true;    //���������� ��� ������ �� ������
//+------------------------------------------------+
//| ������� ��������� ���������� Candles_Smoothed  |
//+------------------------------------------------+
input ENUM_TIMEFRAMES InpInd_Timeframe=PERIOD_H1; //��������� ���������� ASCtrend
input Smooth_Method MA_SMethod=MODE_LWMA;         // ����� ����������
input int MA_Length=30;                           // ������� ����������                    
input int MA_Phase=100;                           // �������� ����������
                                                  // ��� JJMA ������������ � �������� -100 ... +100,
// ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input uint SignalBar=1;                           //����� ���� ��� ��������� ������� �����
//+------------------------------------------------+

int TimeShiftSec;
//---- ���������� ����� ���������� ��� ������� �����������
int InpInd_Handle;
//---- ���������� ����� ���������� ������ ������� ������
int min_rates_total;
//+------------------------------------------------------------------+
//  �������� ���������                                               | 
//+------------------------------------------------------------------+
#include <TradeAlgorithms.mqh>
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---- ��������� ������ ���������� Candles_Smoothed
   InpInd_Handle=iCustom(Symbol(),InpInd_Timeframe,"Candles_Smoothed",MA_SMethod,MA_Length,MA_Phase);
   if(InpInd_Handle==INVALID_HANDLE) Print(" �� ������� �������� ����� ���������� Candles_Smoothed");

//---- ������������� ���������� ��� �������� ������� ������� � ��������  
   TimeShiftSec=PeriodSeconds(InpInd_Timeframe);

//---- ������������� ���������� ������ ������� ������
   min_rates_total=int(MA_Length+SignalBar+1);
//----
   return(0);
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
   double TrendValue[2];
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
      if(CopyBuffer(InpInd_Handle,4,SignalBar,2,TrendValue)<=0) {Recount=true; return;}

      //---- ������� ������� ��� �������
      if(TrendValue[0]==1 && TrendValue[1]==0)
        {
         if(BuyPosOpen) BUY_Open=true;
         if(SellPosClose)SELL_Close=true;
         UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
        }

      //---- ������� ������� ��� �������
      if(TrendValue[0]==0 && TrendValue[1]==1)
        {
         if(SellPosOpen) SELL_Open=true;
         if(BuyPosClose) BUY_Close=true;
         DnSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
        }

      //---- ����� ���������� ����������� �������� ��� ��������� �������� ��� ���������� �������
      //if(!MQL5InfoInteger(MQL5_TESTING) && !MQL5InfoInteger(MQL5_OPTIMIZATION)) //���� ����� �������� � ������� "������������ ��������" 
        {
         if(SellPosOpen && SellPosClose && TrendValue[1]==0) SELL_Close=true;
         if(BuyPosOpen && BuyPosClose && TrendValue[1]==1) BUY_Close=true;
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
   BuyPositionOpen(BUY_Open,Symbol(),UpSignalTime,MM,0,Deviation_,StopLoss_,TakeProfit_);

//---- ��������� ����
   SellPositionOpen(SELL_Open,Symbol(),DnSignalTime,MM,0,Deviation_,StopLoss_,TakeProfit_);
//----
  }
//+------------------------------------------------------------------+
