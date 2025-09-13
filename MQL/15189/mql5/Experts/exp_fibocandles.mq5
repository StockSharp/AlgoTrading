//+------------------------------------------------------------------+
//|                                              Exp_FiboCandles.mq5 |
//|                               Copyright � 2011, Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright � 2011, Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "1.10"
//+----------------------------------------------+
//  �������� ���������                           | 
//+----------------------------------------------+
#include <TradeAlgorithms.mqh>
//+----------------------------------------------+
//|  ������������ ��� ��������� ������� ����     |
//+----------------------------------------------+
/*enum MarginMode  - ������������ ��������� � ����� TradeAlgorithms.mqh
  {
   FREEMARGIN=0,     //MM �� ��������� ������� �� �����
   BALANCE,          //MM �� ������� ������� �� �����
   LOSSFREEMARGIN,   //MM �� ������� �� ��������� ������� �� �����
   LOSSBALANCE,      //MM �� ������� �� ������� ������� �� �����
   LOT               //��� ��� ���������
  }; */
//+------------------------------------------------+
//|  ���������� ��������                           |
//+------------------------------------------------+
#define RESET  0 // ��������� ��� �������� ��������� ������� �� �������� ����������
//---- ��������� ����-�������
#define LEVEL_1 0.236
#define LEVEL_2 0.382
#define LEVEL_3 0.500
#define LEVEL_4 0.618
#define LEVEL_5 0.762
//+------------------------------------------------+
//|  ������������ ��� ����-�������                 |
//+------------------------------------------------+
enum ENUM_FIBORATIO //��� ���������
  {
   LEVEL_1_ = 1,   //0.236
   LEVEL_2_,       //0.382
   LEVEL_3_,       //0.500
   LEVEL_4_,       //0.618
   LEVEL_5_        //0.762
  };
//+----------------------------------------------+
//| ������� ��������� ���������� ��������        |
//+----------------------------------------------+
input double MM=0.1;              //���� ���������� �������� �� �������� � ������
input MarginMode MMMode=LOT;      //������ ����������� ������� ����
input int    StopLoss_=1000;      //�������� � �������
input int    TakeProfit_=2000;    //���������� � �������
input int    Deviation_=10;       //����. ���������� ���� � �������
input bool   BuyPosOpen=true;     //���������� ��� ����� � ����
input bool   SellPosOpen=true;    //���������� ��� ����� � ����
input bool   BuyPosClose=true;    //���������� ��� ������ �� ������
input bool   SellPosClose=true;   //���������� ��� ������ �� ������
//+----------------------------------------------+
//| ������� ��������� ���������� ASCtrend        |
//+----------------------------------------------+
input ENUM_TIMEFRAMES InpInd_Timeframe=PERIOD_H1; //��������� ���������� ASCtrend
input uint period_=10;                            //������ ����������
input ENUM_FIBORATIO fiboLevel=LEVEL_1_;          //�������� ����������
input uint SignalBar=1;                           //����� ���� ��� ��������� ������� �����
//+----------------------------------------------+

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
//---- ��������� ������ ���������� FiboCandles
   InpInd_Handle=iCustom(Symbol(),InpInd_Timeframe,"FiboCandles",period_,fiboLevel,0,0,0);
   if(InpInd_Handle==INVALID_HANDLE) Print(" �� ������� �������� ����� ���������� FiboCandles");

//---- ������������� ���������� ��� �������� ������� ������� � ��������  
   TimeShiftSec=PeriodSeconds(InpInd_Timeframe);

//---- ������������� ���������� ������ ������� ������
   min_rates_total=int(period_+SignalBar+1);
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
         if(SellPosOpen && SellPosClose  &&  TrendValue[1]==0) SELL_Close=true;
         if(BuyPosOpen  &&  BuyPosClose  &&  TrendValue[1]==1) BUY_Close=true;
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
