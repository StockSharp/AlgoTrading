//+------------------------------------------------------------------+
//|                                         Exp_ColorXMACDCandle.mq5 |
//|                               Copyright � 2016, Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright � 2016, Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "1.10"
//+-------------------------------------------------+
//| �������� ���������                              |
//+-------------------------------------------------+
#include <TradeAlgorithms.mqh>
//+-------------------------------------------------+
//| ������������ ��� ��������� ������� ����         |
//+-------------------------------------------------+
/*enum MarginMode  - ������������ ��������� � ����� TradeAlgorithms.mqh
  {
   FREEMARGIN=0,     //MM �� ��������� ������� �� �����
   BALANCE,          //MM �� ������� ������� �� �����
   LOSSFREEMARGIN,   //MM �� ������� �� ��������� ������� �� �����
   LOSSBALANCE,      //MM �� ������� �� ������� ������� �� �����
   LOT               //��� ��� ���������
  }; */
//+-------------------------------------------------+
//| �������� ������ CXMA                            |
//+-------------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-------------------------------------------------+
//| ���������� ������������                         |
//+-------------------------------------------------+
/*enum Smooth_Method - ��������� � ����� SmoothAlgorithms.mqh
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
//+-------------------------------------------------+
//| ���������� ������������                         |
//+-------------------------------------------------+
enum Applied_price_ //��� ���������
  {
   PRICE_CLOSE_ = 1,     //Close
   PRICE_OPEN_,          //Open
   PRICE_HIGH_,          //High
   PRICE_LOW_            //Low
  };
//+-------------------------------------------------+
//| ���������� ������������                         |
//+-------------------------------------------------+
enum Mode //��� ���������
  {
   Histogram = 1,     //��������� ����������� �����������
   SignalLine         //����������� ����������� � ���������� ������
  };
//+-------------------------------------------------+
//| ������� ��������� ��������                      |
//+-------------------------------------------------+
input double MM=0.1;              // ���� ���������� �������� �� �������� � ������
input MarginMode MMMode=LOT;      // ������ ����������� ������� ����
input int    StopLoss_=1000;      // �������� � �������
input int    TakeProfit_=2000;    // ���������� � �������
input int    Deviation_=10;       // ����. ���������� ���� � �������
input bool   BuyPosOpen=true;     // ���������� ��� ����� � ������� �������
input bool   SellPosOpen=true;    // ���������� ��� ����� � �������� �������
input bool   BuyPosClose=true;    // ���������� ��� ������ �� ������� �������
input bool   SellPosClose=true;   // ���������� ��� ������ �� �������� �������
//+-------------------------------------------------+
//| ������� ��������� ���������� ColorXMACDCandle   |
//+-------------------------------------------------+
input ENUM_TIMEFRAMES InpInd_Timeframe=PERIOD_H4; // ��������� ���������� ColorXMACDCandle
input Mode SignalMode=Histogram; // �������� ��������� �������
input Smooth_Method XMA_Method=MODE_T3; // ����� ���������� �����������
input int Fast_XMA = 12; // ������ �������� �������
input int Slow_XMA = 26; // ������ ���������� �������
input int XPhase = 100;  // �������� ���������� ��������
//--- ��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������;
//--- ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input Smooth_Method Signal_Method=MODE_JJMA; // ����� ���������� ���������� �����
input int Signal_XMA=9; // ������ ���������� ����� 
input int Signal_Phase=100; // �������� ���������� �����
//--- ������������ � �������� -100 ... +100,
//--- ������ �� �������� ����������� ��������;
input Applied_price_ AppliedPrice=PRICE_CLOSE_;// ������� ��������� ���������� �����
input uint SignalBar=1;                            // ����� ���� ��� ��������� ������� �����
//+-------------------------------------------------+
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
//---- ��������� ������ ���������� ColorXMACDCandle
   InpInd_Handle=iCustom(Symbol(),InpInd_Timeframe,"ColorXMACDCandle",XMA_Method,Fast_XMA,Slow_XMA,XPhase,Signal_Method,Signal_XMA,Signal_Phase,AppliedPrice);
   if(InpInd_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� ColorXMACDCandle");
      return(INIT_FAILED);
     }
//---- ������������� ���������� ��� �������� ������� ������� � ��������  
   TimeShiftSec=PeriodSeconds(InpInd_Timeframe);
//---- ������������� ���������� ������ ������� ������
   min_rates_total=MathMax(GetStartBars(XMA_Method,Fast_XMA,XPhase),GetStartBars(XMA_Method,Slow_XMA,XPhase));
   min_rates_total+=GetStartBars(Signal_Method,Signal_XMA,Signal_Phase)+2;
   min_rates_total+=int(3+SignalBar);
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
   double Clr[2];
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
      //----
      if(SignalMode==Histogram)
        {
         //---- �������� ����� ����������� ������ � �������
         if(CopyBuffer(InpInd_Handle,4,SignalBar,2,Clr)<=0) {Recount=true; return;}
         //---- ������� ������� ��� �������
         if(Clr[1]==2)
           {
            if(BuyPosOpen && Clr[0]<2) BUY_Open=true;
            if(SellPosClose)SELL_Close=true;
            UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
           }
         //---- ������� ������� ��� �������
         if(Clr[1]==0)
           {
            if(SellPosOpen && Clr[0]>0) SELL_Open=true;
            if(BuyPosClose) BUY_Close=true;
            DnSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
           }
        }
      //----
      if(SignalMode==SignalLine)
        {
         //---- �������� ����� ����������� ������ � �������
         if(CopyBuffer(InpInd_Handle,7,SignalBar,2,Clr)<=0) {Recount=true; return;}
         //---- ������� ������� ��� �������
         if(Clr[1]==1)
           {
            if(BuyPosOpen  &&  Clr[0]!=1) BUY_Open=true;
            if(SellPosClose)SELL_Close=true;
            UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
           }
         //---- ������� ������� ��� �������
         if(Clr[1]==2)
           {
            if(SellPosOpen && Clr[0]!=2) SELL_Open=true;
            if(BuyPosClose) BUY_Close=true;
            DnSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
           }
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
//----
  }
//+------------------------------------------------------------------+
