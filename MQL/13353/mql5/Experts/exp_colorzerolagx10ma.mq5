//+------------------------------------------------------------------+
//|                                        Exp_ColorZerolagX10MA.mq5 |
//|                               Copyright � 2015, Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright � 2015, Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "1.01"
//+----------------------------------------------+
//| �������� ������ CXMA                         |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
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
//---
/*enum Smooth_Method - ������������ ��������� � ����� SmoothAlgorithms.mqh
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
   MODE_AMA,   //AMA
  }; */
//+----------------------------------------------+
//| �������� ���������                           |
//+----------------------------------------------+
#include <TradeAlgorithms.mqh>
//+----------------------------------------------+
//| ������������ ��� ��������� ������� ����      |
//+----------------------------------------------+
/*enum MarginMode  - ������������ ��������� � ����� TradeAlgorithms.mqh
  {
   FREEMARGIN=0,     //MM �� ��������� ������� �� �����
   BALANCE,          //MM �� ������� ������� �� �����
   LOSSFREEMARGIN,   //MM �� ������� �� ��������� ������� �� �����
   LOSSBALANCE,      //MM �� ������� �� ������� ������� �� �����
   LOT               //��� ��� ���������
  }; */
//+----------------------------------------------+
//| ������� ��������� ��������                   |
//+----------------------------------------------+
input double MM=0.1;              // ���� ���������� �������� �� �������� � ������
input MarginMode MMMode=LOT;      // ������ ����������� ������� ����
input int    StopLoss_=1000;      // �������� � �������
input int    TakeProfit_=2000;    // ���������� � �������
input int    Deviation_=10;       // ����. ���������� ���� � �������
input bool   BuyPosOpen=true;     // ���������� ��� ����� � ������� �������
input bool   SellPosOpen=true;    // ���������� ��� ����� � �������� �������
input bool   BuyPosClose=true;    // ���������� ��� ������ �� ������� �������
input bool   SellPosClose=true;   // ���������� ��� ������ �� �������� �������
//+------------------------------------------------+
//| ������� ��������� ���������� ColorZerolagX10MA |
//+------------------------------------------------+
input ENUM_TIMEFRAMES InpInd_Timeframe=PERIOD_H4; // ��������� ����������
input Smooth_Method XMA_Method=MODE_JJMA; // ����� ���������� 
input uint Length1=3;   // ������� ���������� 1
input double Factor1=0.1;
input uint Length2=5;   // ������� ���������� 2
input double Factor2=0.1;
input uint Length3=7;   // ������� ���������� 3
input double Factor3=0.1;
input uint Length4=9;   // ������� ���������� 4 
input double Factor4=0.1;
input uint Length5=11;  // ������� ���������� 5 
input double Factor5=0.1;
input uint Length6=13;  // ������� ���������� 6
input double Factor6=0.1;
input uint Length7=15;  // ������� ���������� 7 
input double Factor7=0.1;
input uint Length8=17;  // ������� ���������� 8
input double Factor8=0.1;
input uint Length9=21;  // ������� ���������� 9
input double Factor9=0.1;
input uint Length10=23; // ������� ���������� 10 
input double Factor10=0.1;
input int XPhase=15; // �������� ����������
                     // ��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������
                     // ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input Smooth_Method SmoothMethod=MODE_SMA; //����� ����������� 
input uint Smooth=3;      // ������� �����������
input int SmoothPhase=15; // �������� �����������
                          // ��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������
                          // ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input Applied_price_ IPC=PRICE_CLOSE; // ������� ���������
input uint SignalBar=1; // ����� ���� ��� ��������� ������� �����
//+------------------------------------------------+
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
//---- ��������� ������ ���������� ColorZerolagX10MA
   InpInd_Handle=iCustom(Symbol(),InpInd_Timeframe,"ColorZerolagX10MA",
                         XMA_Method,Length1,Factor1,Length2,Factor2,Length3,Factor3,Length4,Factor4,
                         Length5,Factor5,Length6,Factor6,Length7,Factor7,Length8,Factor8,Length9,Factor9,
                         Length10,Factor10,XPhase,SmoothMethod,Smooth,SmoothPhase,IPC,0,0);
   if(InpInd_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� ColorZerolagX10MA");
      return(INIT_FAILED);
     }
//---- ������������� ���������� ��� �������� ������� ������� � ��������  
   TimeShiftSec=PeriodSeconds(InpInd_Timeframe);
//---- ������������� ���������� ������ ������� ������
   int min_rates[10];
   min_rates[0]=GetStartBars(XMA_Method,Length1,XPhase);
   min_rates[1]=GetStartBars(XMA_Method,Length2,XPhase);
   min_rates[2]=GetStartBars(XMA_Method,Length3,XPhase);
   min_rates[3]=GetStartBars(XMA_Method,Length4,XPhase);
   min_rates[4]=GetStartBars(XMA_Method,Length5,XPhase);
   min_rates[5]=GetStartBars(XMA_Method,Length6,XPhase);
   min_rates[6]=GetStartBars(XMA_Method,Length7,XPhase);
   min_rates[7]=GetStartBars(XMA_Method,Length8,XPhase);
   min_rates[8]=GetStartBars(XMA_Method,Length9,XPhase);
   min_rates[9]=GetStartBars(XMA_Method,Length10,XPhase);
   min_rates_total=min_rates[ArrayMaximum(min_rates)];
   min_rates_total+=GetStartBars(SmoothMethod,Smooth,SmoothPhase);
   min_rates_total+=int(3+SignalBar);
//---- ���������� �������������
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
   double Value[3];
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
      if(CopyBuffer(InpInd_Handle,0,SignalBar,3,Value)<=0) {Recount=true; return;}
      //---- ������� ������� ��� �������
      if(Value[1]<Value[2])
        {
         if(BuyPosOpen && Value[0]>Value[1]) BUY_Open=true;
         if(SellPosClose) SELL_Close=true;
         UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
        }
      //---- ������� ������� ��� �������
      if(Value[1]>Value[2])
        {
         if(SellPosOpen && Value[0]<Value[1]) SELL_Open=true;
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
