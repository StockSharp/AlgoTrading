//+------------------------------------------------------------------+
//|                                           Exp_ColorHMA_StDev.mq5 |
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
input uint HMA_Period=13;                         //������ �������
input double dK1=1.5;                             //����������� 1 ��� ������������� �������
input double dK2=2.5;                             //����������� 2 ��� ������������� �������
input uint std_period=9;                          //������ ������������� �������
input ENUM_APPLIED_PRICE IPC=PRICE_CLOSE;         //������� ���������
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
//---- ��������� ������ ���������� ColorHMA_StDev
   InpInd_Handle=iCustom(Symbol(),InpInd_Timeframe,"ColorHMA_StDev",HMA_Period,dK1,dK2,std_period,0,0,IPC);
   if(InpInd_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� ColorHMA_StDev");
      return(INIT_FAILED);
     }
//---- ������������� ���������� ��� �������� ������� ������� � ��������  
   TimeShiftSec=PeriodSeconds(InpInd_Timeframe);
//---- ������������� ���������� ������ ������� ������
   min_rates_total=int(MathFloor(MathSqrt(HMA_Period)))+int(HMA_Period)+int(std_period);
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
