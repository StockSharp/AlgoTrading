//+------------------------------------------------------------------+
//|                                         Exp_BinaryWave_StDev.mq5 |
//|                               Copyright � 2016, Nikolay Kositsin | 
//|                                Khabarovsk, farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright � 2016, Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "1.00"
//+----------------------------------------------+
//| �������� ���������                           |
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
//+----------------------------------------------+
//|  ������������ ��� ��������� ������ � �����   |
//+----------------------------------------------+
enum SignalMode
  {
   POINT=0,          //��� ��������� �������� �������� (����� ����� - ������)
   DIRECT,           //��� ��������� ����������� �������� ����������
   WITHOUT           //��� ����������
  };
//+-----------------------------------------------+
//|  �������� ������ CXMA                         |
//+-----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------------------+
//|  ���������� ������������                      |
//+-----------------------------------------------+
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
//| ������� ��������� ��������                   |
//+----------------------------------------------+
input double MM=0.1;                  // ���� ���������� �������� �� �������� � ������
input MarginMode MMMode=LOT;          // ������ ����������� ������� ����
input int    StopLoss_=1000;          // �������� � �������
input int    TakeProfit_=2000;        // ���������� � �������
input int    Deviation_=10;           // ����. ���������� ���� � �������
input SignalMode BuyPosOpen=POINT;    // ���������� ��� ����� � ������� �������
input SignalMode SellPosOpen=POINT;   // ���������� ��� ����� � �������� �������
input SignalMode BuyPosClose=DIRECT;  // ���������� ��� ������ �� ������� �������
input SignalMode SellPosClose=DIRECT; // ���������� ��� ������ �� �������� �������
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input ENUM_TIMEFRAMES InpInd_Timeframe=PERIOD_H4; //��������� ����������
//--- ��� �����������. ���� ����, ��������� �� ��������� � ������� �����
input double WeightMA    = 1.0;
input double WeightMACD  = 1.0;
input double WeightOsMA  = 1.0;
input double WeightCCI   = 1.0;
input double WeightMOM   = 1.0;
input double WeightRSI   = 1.0;
input double WeightADX   = 1.0;
//---- ��������� ����������� ��������
input int   MAPeriod=13;
input  ENUM_MA_METHOD   MAType=MODE_EMA;
input ENUM_APPLIED_PRICE   MAPrice=PRICE_CLOSE;
//---- ��������� MACD
input int   FastMACD     = 12;
input int   SlowMACD     = 26;
input int   SignalMACD   = 9;
input ENUM_APPLIED_PRICE   PriceMACD=PRICE_CLOSE;
//---- ��������� OsMA
input int   FastPeriod   = 12;
input int   SlowPeriod   = 26;
input int   SignalPeriod = 9;
input ENUM_APPLIED_PRICE   OsMAPrice=PRICE_CLOSE;
//---- ��������� CCI
input int   CCIPeriod=14;
input ENUM_APPLIED_PRICE   CCIPrice=PRICE_MEDIAN;
//---- ��������� �������
input int   MOMPeriod=14;
input ENUM_APPLIED_PRICE   MOMPrice=PRICE_CLOSE;
//---- ��������� RSI
input int   RSIPeriod=14;
input ENUM_APPLIED_PRICE   RSIPrice=PRICE_CLOSE;
//---- ��������� ADX
input int   ADXPeriod=14;
//---- ��������� ����������� �����
input Smooth_Method bMA_Method=MODE_JJMA; //����� ����������
input int bLength=5; //������� �����������                    
input int bPhase=100; //�������� �����������,
                      //��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������;
// ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input double dK1=1.5;  //����������� 1 ��� ������������� �������
input double dK2=2.5;  //����������� 2 ��� ������������� �������
input uint std_period=9; //������ ������������� �������
input uint SignalBar=1;                           //����� ���� ��� ��������� ������� �����
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
//---- ��������� ������ ���������� BinaryWave_StDev
   InpInd_Handle=iCustom(Symbol(),InpInd_Timeframe,"BinaryWave_StDev",
                         WeightMA,WeightMACD,WeightOsMA,WeightCCI,WeightMOM,WeightRSI,WeightADX,
                         MAPeriod,MAType,MAPrice,FastMACD,SlowMACD,SignalMACD,PriceMACD,FastPeriod,SlowPeriod,SignalPeriod,
                         OsMAPrice,CCIPeriod,CCIPrice,MOMPeriod,MOMPrice,RSIPeriod,RSIPrice,ADXPeriod,bMA_Method,bLength,bPhase,dK1,dK2,std_period,0);
   if(InpInd_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� BinaryWave_StDev");
      return(INIT_FAILED);
     }
//---- ������������� ���������� ��� �������� ������� ������� � ��������  
   TimeShiftSec=PeriodSeconds(InpInd_Timeframe);
//---- ������������� ���������� ������ ������� ������
   min_rates_total=MathMax(MAPeriod,MathMax(SlowPeriod,MathMax(CCIPeriod,MathMax(SlowMACD,MOMPeriod))))+1;
   min_rates_total+=GetStartBars(bMA_Method,bLength,bPhase);
   min_rates_total+=int(std_period);
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
      //----
      switch(BuyPosOpen)
        {
         case POINT:
           {
            //---- ���������� ��������� ����������
            double Sign1[1],Sign2[1];
            //---- �������� ����� ����������� ������ � �������
            if(CopyBuffer(InpInd_Handle,3,SignalBar,1,Sign1)<=0) {Recount=true; return;}
            if(CopyBuffer(InpInd_Handle,5,SignalBar,1,Sign2)<=0) {Recount=true; return;}
            if((Sign1[0]!=EMPTY_VALUE) || (Sign2[0]!=EMPTY_VALUE)) BUY_Open=true;
            break;
           }
         case DIRECT:
           {
            //---- ���������� ��������� ����������
            double Line[3];
            if(CopyBuffer(InpInd_Handle,0,SignalBar,3,Line)<=0) {Recount=true; return;}
            if(Line[0]>Line[1] &&  Line[1]<Line[2]) BUY_Open=true;
            break;
           }
         case WITHOUT:
           {
            break;
           }
        }
      //----
      switch(SellPosOpen)
        {
         case POINT:
           {
            //---- ���������� ��������� ����������
            double Sign1[1],Sign2[1];
            //---- �������� ����� ����������� ������ � �������
            if(CopyBuffer(InpInd_Handle,2,SignalBar,1,Sign1)<=0) {Recount=true; return;}
            if(CopyBuffer(InpInd_Handle,4,SignalBar,1,Sign2)<=0) {Recount=true; return;}
            if((Sign1[0]!=EMPTY_VALUE) || (Sign2[0]!=EMPTY_VALUE)) SELL_Open=true;
            break;
           }
         case DIRECT:
           {
            //---- ���������� ��������� ����������
            double Line[3];
            if(CopyBuffer(InpInd_Handle,0,SignalBar,3,Line)<=0) {Recount=true; return;}
            if(Line[0]<Line[1] && Line[1]>Line[2]) SELL_Open=true;
            break;
           }
         case WITHOUT:
           {
            break;
           }
        }
      //----
      switch(BuyPosClose)
        {
         case POINT:
           {
            //---- ���������� ��������� ����������
            double Sign1[1],Sign2[1];
            //---- �������� ����� ����������� ������ � �������
            if(CopyBuffer(InpInd_Handle,2,SignalBar,1,Sign1)<=0) {Recount=true; return;}
            if(CopyBuffer(InpInd_Handle,4,SignalBar,1,Sign2)<=0) {Recount=true; return;}
            if((Sign1[0]!=EMPTY_VALUE) || (Sign2[0]!=EMPTY_VALUE)) BUY_Close=true;
            break;
           }
         case DIRECT:
           {
            //---- ���������� ��������� ����������
            double Line[2];
            if(CopyBuffer(InpInd_Handle,0,SignalBar,2,Line)<=0) {Recount=true; return;}
            if(Line[0]>Line[1]) BUY_Close=true;
            break;
           }
         case WITHOUT:
           {
            break;
           }
        }
      //----
      switch(SellPosClose)
        {
         case POINT:
           {
            //---- ���������� ��������� ����������
            double Sign1[1],Sign2[1];
            //---- �������� ����� ����������� ������ � �������
            if(CopyBuffer(InpInd_Handle,3,SignalBar,1,Sign1)<=0) {Recount=true; return;}
            if(CopyBuffer(InpInd_Handle,5,SignalBar,1,Sign2)<=0) {Recount=true; return;}
            if((Sign1[0]!=EMPTY_VALUE) || (Sign2[0]!=EMPTY_VALUE)) SELL_Close=true;
            break;
           }
         case DIRECT:
           {
            //---- ���������� ��������� ����������
            double Line[2];
            if(CopyBuffer(InpInd_Handle,0,SignalBar,2,Line)<=0) {Recount=true; return;}
            if(Line[0]<Line[1]) SELL_Close=true;
            break;
           }
         case WITHOUT:
           {
            break;
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
//----
  }
//+------------------------------------------------------------------+
