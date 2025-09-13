//+------------------------------------------------------------------+
//|                                                       Stalin.mq5 |
//|                   Copyright � 2011, Andrey Vassiliev (MoneyJinn) |
//|                                         http://www.vassiliev.ru/ |
//+------------------------------------------------------------------+
#property copyright "Copyright � 2011, Andrey Vassiliev (MoneyJinn)"
#property link      "http://www.vassiliev.ru/"
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � ������� ����
#property indicator_chart_window 
//---- ��� ������� � ��������� ���������� ������������ ��� ������
#property indicator_buffers 2
//---- ������������ ����� ��� ����������� ����������
#property indicator_plots   2
//+----------------------------------------------+
//|  ���������� ��������                         |
//+----------------------------------------------+
#define RESET 0 // ��������� ��� �������� ��������� ������� �� �������� ����������
//+----------------------------------------------+
//|  ��������� ��������� ���������� ����������   |
//+----------------------------------------------+
//---- ��������� ���������� 1 � ���� �������
#property indicator_type1   DRAW_ARROW
//---- � �������� ����� ��������� ����� ���������� ����������� LightPink ����
#property indicator_color1  LightPink
//---- ������� ����� ���������� 1 ����� 4
#property indicator_width1  4
//---- ����������� ������ ����� ����������
#property indicator_label1  "Silver Sell"
//+----------------------------------------------+
//|  ��������� ��������� ������� ����������      |
//+----------------------------------------------+
//---- ��������� ���������� 2 � ���� �������
#property indicator_type2   DRAW_ARROW
//---- � �������� ����� ������ ����� ���������� ����������� LightSkyBlue ����
#property indicator_color2  LightSkyBlue
//---- ������� ����� ���������� 2 ����� 4
#property indicator_width2  4
//---- ����������� ��������� ����� ����������
#property indicator_label2 "Silver Buy"

//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input ENUM_MA_METHOD MAMethod=MODE_EMA;
input int    MAShift=0;
input int    Fast=14;
input int    Slow=21;
input int    RSI=17;
input int    Confirm=0.0;
input int    Flat=0.0;
input bool   SoundAlert=false;
input bool   EmailAlert=false;
//+----------------------------------------------+

//---- ���������� ������������ ��������, ������� ����� � 
// ���������� ������������ � �������� ������������ �������
double SellBuffer[];
double BuyBuffer[];
//----
double IUP,IDN,E1,E2,Confirm2,Flat2;
//---- ���������� ����� ���������� ������ ������� ������
int StartBars;
//---- ���������� ����� ���������� ��� �������� ������� �����������
int SLMA_Handle,FSMA_Handle,RSI_Handle;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void BU(int i,const double &Low[],const datetime &Time[])
  {
//----
   if(Low[i]>=(E1+Flat2) || Low[i]<=(E1-Flat2))
     {
      BuyBuffer[i]=Low[i];
      E1=BuyBuffer[i];
      Alerts(i,"UP "+Symbol()+" "+TimeToString(Time[i]));
     }
//----
  }
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void BD(int i,const double &High[],const datetime &Time[])
  {
//----
   if(High[i]>=(E2+Flat2) || High[i]<=(E2-Flat2))
     {
      SellBuffer[i]=High[i];
      E2=SellBuffer[i];
      Alerts(i,"DN "+Symbol()+" "+TimeToString(Time[i]));
     }
//---- 
  }
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void Alerts(int pos,string txt)
  {
//----
   if(SoundAlert==true&&pos==1){PlaySound("alert.wav");}
   if(EmailAlert==true&&pos==1){SendMail("Stalin alert signal: "+txt,txt);}
//----
  }
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   StartBars=MathMax(RSI,MathMax(Slow,Fast));

//---- ������������� ����������
   IUP=0;
   IDN=0;
   E1=0;
   E2=0;

   if(_Digits==3 || _Digits==5)
     {
      double Point10=10*_Point;
      Confirm2=Point10;
      Flat2=Flat*Point10;
     }
   else
     {
      Confirm2=Confirm*_Point;
      Flat2=Flat*_Point;
     }

//---- ��������� ������ ���������� iRSI
   if(RSI)
     {
      RSI_Handle=iRSI(NULL,0,RSI,PRICE_CLOSE);
      if(RSI_Handle==INVALID_HANDLE) Print(" �� ������� �������� ����� ���������� iRSI");
      Print("RSI=",RSI);
     }
//---- ��������� ������ ���������� iMA
   SLMA_Handle=iMA(NULL,0,Slow,MAShift,MAMethod,PRICE_CLOSE);
   if(SLMA_Handle==INVALID_HANDLE) Print(" �� ������� �������� ����� ���������� iMA");
//---- ��������� ������ ���������� iMA
   FSMA_Handle=iMA(NULL,0,Fast,MAShift,MAMethod,PRICE_CLOSE);
   if(FSMA_Handle==INVALID_HANDLE) Print(" �� ������� �������� ����� ���������� iMA");

//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,SellBuffer,INDICATOR_DATA);
//---- ������������� ������ ������ ������� ��������� ���������� 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,StartBars);
//--- �������� ����� ��� ����������� � DataWindow
   PlotIndexSetString(0,PLOT_LABEL,"Stalin Sell");
//---- ������ ��� ����������
   PlotIndexSetInteger(0,PLOT_ARROW,234);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0.0);
//---- ���������� ��������� � ������, ��� � ���������
   ArraySetAsSeries(SellBuffer,true);

//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(1,BuyBuffer,INDICATOR_DATA);
//---- ������������� ������ ������ ������� ��������� ���������� 2
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,StartBars);
//---- �������� ����� ��� ����������� � DataWindow
   PlotIndexSetString(1,PLOT_LABEL,"Stalin Buy");
//---- ������ ��� ����������
   PlotIndexSetInteger(1,PLOT_ARROW,233);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,0.0);
//---- ���������� ��������� � ������, ��� � ���������
   ArraySetAsSeries(BuyBuffer,true);

//---- ��������� ������� �������� ����������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- ��� ��� ���� ������ � ����� ��� ������� 
   string short_name="Stalin";
   IndicatorSetString(INDICATOR_SHORTNAME,short_name);
//----   
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,
                const int prev_calculated,
                const datetime &time[],
                const double &open[],
                const double &high[],
                const double &low[],
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
  {
//---- �������� ���������� ����� �� ������������� ��� �������
   if(RSI && BarsCalculated(RSI_Handle)<rates_total
      || BarsCalculated(SLMA_Handle)<rates_total
      || BarsCalculated(FSMA_Handle)<rates_total
      || rates_total<StartBars)
      return(RESET);

//---- ���������� ��������� ���������� 
   int to_copy,limit;
   double RSI_[],SLMA_[],FSMA_[];

//---- ������� ������������ ���������� ���������� ������ �
//���������� ������ limit ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
     {
      limit=rates_total-StartBars; // ��������� ����� ��� ������� ���� �����
      to_copy=rates_total; // ��������� ���������� ���� �����
     }
   else
     {
      limit=rates_total-prev_calculated; // ��������� ����� ��� ������� ����� �����
      to_copy=limit+2; // ��������� ���������� ������ ����� �����
     }

//---- ���������� ��������� � ��������, ��� � ����������
   ArraySetAsSeries(open,true);
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
   ArraySetAsSeries(close,true);
   ArraySetAsSeries(time,true);
   ArraySetAsSeries(SLMA_,true);
   ArraySetAsSeries(FSMA_,true);

//---- �������� ����� ����������� ������ � �������
   if(CopyBuffer(SLMA_Handle,0,0,to_copy,SLMA_)<=0) return(RESET);
   if(CopyBuffer(FSMA_Handle,0,0,to_copy,FSMA_)<=0) return(RESET);

   if(RSI)
     {
      ArraySetAsSeries(RSI_,true);
      if(CopyBuffer(RSI_Handle,0,0,to_copy,RSI_)<=0) return(RESET);
     }

//---- �������� ���� ������� ����������
   for(int bar=limit; bar>=0; bar--)
     {
      BuyBuffer[bar]=0.0;
      SellBuffer[bar]=0.0;

      if(!RSI||FSMA_[bar+1]<SLMA_[bar+1]&&FSMA_[bar]>SLMA_[bar]&&(RSI_[bar]>50)){if(!Confirm2)BU(bar,low, time); else{IUP=low[bar]; IDN=0;}}
      if(!RSI||FSMA_[bar+1]>SLMA_[bar+1]&&FSMA_[bar]<SLMA_[bar]&&(RSI_[bar]<50)){if(!Confirm2)BD(bar,high,time); else{IDN=high[bar];IUP=0;}}
      if(IUP && high[bar]-IUP>=Confirm2 && close[bar]<=high[bar]){BU(bar,low,time); IUP=0;}
      if(IDN && IDN-low[bar]>=Confirm2 && open[bar]>=close[bar]){BD(bar,high,time);IDN=0;}
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
