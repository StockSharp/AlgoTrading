//+------------------------------------------------------------------+
//|                                                Exp_ForexLine.mq5 |
//|                               Copyright � 2016, Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright � 2016, Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "1.00"
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
//+----------------------------------------------+
//| ���������� ������������                      |
//+----------------------------------------------+
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
//+----------------------------------------------+
//| ������� ��������� ���������� ForexLine       |
//+----------------------------------------------+
input ENUM_TIMEFRAMES InpInd_Timeframe=PERIOD_H8; //��������� ����������
//----
input Smooth_Method MA_Method11=MODE_LWMA; //����� ���������� ������� ����������� ������� 1
input int Length11=5; //�������  ������� �����������  ������� 1
input int Phase11=15; //�������� ������� �����������
//---- ��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������;
//---- ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input Smooth_Method MA_Method12=MODE_LWMA; //����� ���������� ������� ����������� 
input int Length12=10; //�������  ������� �����������  ������� 1
input int Phase12=15;  //�������� ������� �����������  ������� 1
//---- ��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������;
//---- ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input Applied_price_ IPC1=PRICE_CLOSE;//������� ��������� ������� 1
//----
input Smooth_Method MA_Method21=MODE_LWMA; //����� ���������� ������� ����������� ������� 2
input int Length21=20; //�������  ������� ����������� ������� 2                   
input int Phase21=15; //�������� ������� ����������� ������� 2
//---- ��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������;
//---- ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input Smooth_Method MA_Method22=MODE_LWMA; //����� ���������� ������� ����������� ������� 2
input int Length22=20; //�������  ������� ����������� ������� 2
input int Phase22=15;  //�������� ������� �����������
//---- ��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������;
//---- ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input Applied_price_ IPC2=PRICE_CLOSE;//������� ��������� ������� 2
//----
input uint SignalBar=1;//����� ���� ��� ��������� ������� �����
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
//---- ��������� ������ ���������� ForexLine
   InpInd_Handle=iCustom(Symbol(),InpInd_Timeframe,"ForexLine",MA_Method11,Length11,Phase11,MA_Method12,Length12,Phase12,IPC1,
                         MA_Method21,Length21,Phase21,MA_Method22,Length22,Phase22,IPC2,0,0);
   if(InpInd_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� ForexLine");
      return(INIT_FAILED);
     }
//---- ������������� ���������� ��� �������� ������� ������� � ��������  
   TimeShiftSec=PeriodSeconds(InpInd_Timeframe);
//---- ������������� ���������� ������ ������� ������
   int min_rates_1=GetStartBars(MA_Method11,Length11,Phase11);
   int min_rates_2=GetStartBars(MA_Method21,Length21,Phase21);
   int min_rates_12=GetStartBars(MA_Method12,Length12,Phase12);
   int min_rates_22=GetStartBars(MA_Method22,Length22,Phase22);
   min_rates_total=MathMax(min_rates_1+min_rates_12,min_rates_2+min_rates_22);
   min_rates_total+=int(3+SignalBar);
//----
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
      //---- ���������� ��������� ����������
      double Col[2];
      //---- �������� ����� ����������� ������ � �������
      if(CopyBuffer(InpInd_Handle,1,SignalBar,2,Col)<=0) {Recount=true; return;}
      //---- ������� ������� ��� �������
      if(Col[1]==0)
        {
         if(BuyPosOpen && Col[0]>0) BUY_Open=true;
         if(SellPosClose) SELL_Close=true;
         UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
        }
      //---- ������� ������� ��� �������
      if(Col[1]==2)
        {
         if(SellPosOpen && Col[0]<2) SELL_Open=true;
         if(BuyPosClose) BUY_Close=true;
         DnSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
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
