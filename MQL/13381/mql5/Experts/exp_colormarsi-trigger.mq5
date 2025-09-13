//+------------------------------------------------------------------+
//|                                       Exp_ColorMaRsi-Trigger.mq5 |
//|                               Copyright � 2015, Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright � 2015, Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "1.00"
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
//| ������� ��������� ���������� ColorMaRsi-Trigger |
//+-------------------------------------------------+
input ENUM_TIMEFRAMES InpInd_Timeframe=PERIOD_H4; // ��������� ����������
input uint nPeriodRsi=3;
input ENUM_APPLIED_PRICE nRSIPrice=PRICE_WEIGHTED;
input uint nPeriodRsiLong=13;
input ENUM_APPLIED_PRICE nRSIPriceLong=PRICE_MEDIAN;
input uint nPeriodMa=5;
input  ENUM_MA_METHOD nMAType=MODE_EMA;
input ENUM_APPLIED_PRICE nMAPrice=PRICE_CLOSE;
input uint nPeriodMaLong=10;
input  ENUM_MA_METHOD nMATypeLong=MODE_EMA;
input ENUM_APPLIED_PRICE nMAPriceLong=PRICE_CLOSE;
input uint SignalBar=1; // ����� ���� ��� ��������� ������� �����
//+-------------------------------------------------+
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
//---- ��������� ������ ���������� ColorMaRsi-Trigger
   InpInd_Handle=iCustom(Symbol(),InpInd_Timeframe,"ColorMaRsi-Trigger",nPeriodRsi,nRSIPrice,nPeriodRsiLong,nRSIPriceLong,
                         nPeriodMa,nMAType,nMAPrice,nPeriodMaLong,nMAPriceLong,0);
   if(InpInd_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� ColorMaRsi-Trigger");
      return(INIT_FAILED);
     }
//---- ������������� ���������� ��� �������� ������� ������� � ��������  
   TimeShiftSec=PeriodSeconds(InpInd_Timeframe);
//---- ������������� ���������� ������ ������� ������
   min_rates_total=int(MathMax(MathMax(MathMax(nPeriodRsi,nPeriodRsiLong),nPeriodMa),nPeriodMaLong));
   min_rates_total+=int(2+SignalBar);
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
   double Value[2];
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
      if(CopyBuffer(InpInd_Handle,1,SignalBar,2,Value)<=0) {Recount=true; return;}
      //---- ������� ������� ��� �������
      if(Value[1]>0)
        {
         if(BuyPosOpen && Value[0]<=0) BUY_Open=true;
         if(SellPosClose) SELL_Close=true;
         UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
        }
      //---- ������� ������� ��� �������
      if(Value[1]<0)
        {
         if(SellPosOpen && Value[0]>=0) SELL_Open=true;
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
