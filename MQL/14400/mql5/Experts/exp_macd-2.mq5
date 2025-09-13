//+------------------------------------------------------------------+
//|                                                   Exp_MACD-2.mq5 |
//|                               Copyright � 2015, Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright � 2015, Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "1.00"
//+----------------------------------------------+
//| ���������� ������������                      |
//+----------------------------------------------+
enum TREND_MODE //��� ���������
  {
   HISTOGRAM = 1,     //��������� ����������� �������� �����������
   CLOUD,             //��������� ����� ������
   ZERO               //������ ������������ MACD ����
  };
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
//+----------------------------------------------+
//| ������� ��������� ���������� MACD-2          |
//+----------------------------------------------+
input ENUM_TIMEFRAMES InpInd_Timeframe=PERIOD_H4; // ��������� ����������
input TREND_MODE TrendMode=CLOUD; // ������� ����������� ������
input uint FastMACD     = 12;
input uint SlowMACD     = 26;
input uint SignalMACD   = 9;
input ENUM_APPLIED_PRICE   PriceMACD=PRICE_CLOSE;
input uint SignalBar=1; // ����� ���� ��� ��������� ������� �����
//+----------------------------------------------+
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
//---- ��������� ������ ���������� MACD-2
   InpInd_Handle=iCustom(Symbol(),InpInd_Timeframe,"MACD-2",FastMACD,SlowMACD,SignalMACD,PriceMACD);
   if(InpInd_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� MACD-2");
      return(INIT_FAILED);
     }
//---- ������������� ���������� ��� �������� ������� ������� � ��������  
   TimeShiftSec=PeriodSeconds(InpInd_Timeframe);
//---- ������������� ���������� ������ ������� ������
   min_rates_total=int(SignalMACD+MathMax(FastMACD,SlowMACD));
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
      if(TrendMode==HISTOGRAM)
        {
         //---- ���������� ��������� ����������
         double Value[3];
         //---- �������� ����� ����������� ������ � �������
         if(CopyBuffer(InpInd_Handle,2,SignalBar,3,Value)<=0) {Recount=true; return;}
         //---- ������� ������� ��� �������
         if(Value[1]<Value[2])
           {
            if(BuyPosOpen && Value[0]>Value[1])
              {
               BUY_Open=true;
               UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
              }
            if(SellPosClose) SELL_Close=true;
           }
         //---- ������� ������� ��� �������
         if(Value[1]>Value[2])
           {
            if(SellPosOpen && Value[0]<Value[1])
              {
               SELL_Open=true;
               DnSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
              }
            if(BuyPosClose) BUY_Close=true;
           }
        }
       if(TrendMode==CLOUD)
        {
         //---- ���������� ��������� ����������
         double Up[2],Dn[2];
         //---- �������� ����� ����������� ������ � �������
         if(CopyBuffer(InpInd_Handle,0,SignalBar,2,Up)<=0) {Recount=true; return;}
         if(CopyBuffer(InpInd_Handle,1,SignalBar,2,Dn)<=0) {Recount=true; return;}
         //---- ������� ������� ��� �������
         if(Up[1]>Dn[1])
           {
            if(BuyPosOpen && Up[0]<Dn[0])
              {
               BUY_Open=true;
               UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
              }
            if(SellPosClose) SELL_Close=true;
           }
         //---- ������� ������� ��� �������
         if(Up[1]<Dn[1])
           {
            if(SellPosOpen && Up[0]>Dn[0])
              {
               SELL_Open=true;
               DnSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
              }
            if(BuyPosClose) BUY_Close=true;
           }
        }
      if(TrendMode==ZERO)
        {
         //---- ���������� ��������� ����������
         double Value[2];
         //---- �������� ����� ����������� ������ � �������
         if(CopyBuffer(InpInd_Handle,2,SignalBar,2,Value)<=0) {Recount=true; return;}
         //---- ������� ������� ��� �������
         if(Value[1]>0)
           {
            if(BuyPosOpen && Value[0]<=0)
              {
               BUY_Open=true;
               UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
              }
            if(SellPosClose) SELL_Close=true;
           }
         //---- ������� ������� ��� �������
         if(Value[1]<0)
           {
            if(SellPosOpen && Value[0]>=0)
              {
               SELL_Open=true;
               DnSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
              }
            if(BuyPosClose) BUY_Close=true;
           }
        }
     }
//+----------------------------------------------+
//| ���������� ������                            |
//+----------------------------------------------+
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
