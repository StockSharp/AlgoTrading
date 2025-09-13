//+------------------------------------------------------------------+
//|                                                   wlxBW5Zone.mq5 |
//|                                          Copyright � 2005, Wellx |
//|                                       http://www.metaquotes.net/ |
//+------------------------------------------------------------------+
//---- ��������� ����������
#property copyright "Copyright � 2005, Wellx"
//---- ������ �� ���� ������
#property link      "http://www.metaquotes.net/"
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
//---- � �������� ����� ��������� ����� ���������� ����������� ������� ����
#property indicator_color1  clrMagenta
//---- ������� ����� ���������� 1 ����� 4
#property indicator_width1  4
//---- ����������� ����� ����� ����������
#property indicator_label1  "wlxBW5Zone Sell"
//+----------------------------------------------+
//|  ��������� ��������� ������ ����������       |
//+----------------------------------------------+
//---- ��������� ���������� 2 � ���� �������
#property indicator_type2   DRAW_ARROW
//---- � �������� ����� ����� ����� ���������� ����������� ������ ����
#property indicator_color2  clrLime
//---- ������� ����� ���������� 2 ����� 4
#property indicator_width2  4
//---- ����������� ��������� ����� ����������
#property indicator_label2 "wlxBW5Zone Buy"
//+----------------------------------------------+
//|  ���������� ������������                     |
//+----------------------------------------------+
enum Direct //��� ���������
  {
   ON = 0,     // �� ������
   OFF         // ������ ������
  };
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input Direct Dir=ON; // ����������� ��������
//+----------------------------------------------+

//---- ���������� ������������ ��������, ������� ����� � 
// ���������� ������������ � �������� ������������ �������
double SellBuffer[];
double BuyBuffer[];
//---- ���������� ����� ���������� ��� ������� �����������
int AC_Handle,AO_Handle,ATR_Handle;
//---- ���������� ����� ���������� ������ ������� ������
int min_rates_total;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
  {
//---- ������������� ���������� ���������� 
   int ATR_Period=12;
   int AC_Period=37;
   int AO_Period=33;
   min_rates_total=int(MathMax(MathMax(AC_Period,AO_Period)+4,ATR_Period));
   
//---- ��������� ������ ���������� ATR
   ATR_Handle=iATR(NULL,0,ATR_Period);
   if(ATR_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� ATR");
      return(INIT_FAILED);
     }

//---- ��������� ������ ����������  Accelerator Oscillator 
   AC_Handle=iAC(Symbol(),PERIOD_CURRENT);
   if(AC_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ����������  Accelerator Oscillator");
      return(INIT_FAILED);
     }

//---- ��������� ������ ����������  Awesome Oscillator 
   AO_Handle=iAO(Symbol(),PERIOD_CURRENT);
   if(AO_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ����������  Awesome Oscillator");
      return(INIT_FAILED);
     }

//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,SellBuffer,INDICATOR_DATA);
//---- ������������� ������ ������ ������� ��������� ���������� 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ������ ��� ����������
   PlotIndexSetInteger(0,PLOT_ARROW,119);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(SellBuffer,true);

//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(1,BuyBuffer,INDICATOR_DATA);
//---- ������������� ������ ������ ������� ��������� ���������� 2
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- ������ ��� ����������
   PlotIndexSetInteger(1,PLOT_ARROW,119);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(BuyBuffer,true);

//---- ��������� ������� �������� ����������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- ��� ��� ���� ������ � ����� ��� �������� 
   string short_name="wlxBW5ZoneSig";
   IndicatorSetString(INDICATOR_SHORTNAME,short_name);
//---- ���������� �������������
   return(INIT_SUCCEEDED);
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
   if(BarsCalculated(AC_Handle)<rates_total
      || BarsCalculated(AO_Handle)<rates_total
      || BarsCalculated(ATR_Handle)<rates_total
      || rates_total<min_rates_total)
      return(RESET);

//---- ���������� ��������� ���������� 
   int to_copy,limit,bar;
   double AC[],AO[],ATR[],range;
   bool flagUP,flagDown;
   static bool flagUP_,flagDown_;

//---- ������� ������������ ���������� ���������� ������ �
//���������� ������ limit ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
     {
      limit=rates_total-min_rates_total; // ��������� ����� ��� ������� ���� �����
      flagUP_=false;
      flagDown_=false;
     }
   else
     {
      limit=rates_total-prev_calculated; // ��������� ����� ��� ������� ����� �����
     }

   to_copy=limit+1;
//---- �������� ����� ����������� ������ � ������� AO[],AC[] � ATR[]
   if(CopyBuffer(ATR_Handle,0,0,to_copy,ATR)<=0) return(RESET);
   to_copy+=4;
   if(CopyBuffer(AC_Handle,0,0,to_copy,AC)<=0) return(RESET);
   if(CopyBuffer(AO_Handle,0,0,to_copy,AO)<=0) return(RESET);

//---- ���������� ��������� � �������� ��� � ����������  
   ArraySetAsSeries(AC,true);
   ArraySetAsSeries(AO,true);
   ArraySetAsSeries(ATR,true);
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);

//---- ��������������� �������� ����������
   flagUP=flagUP_;
   flagDown=flagDown_;

//---- �������� ���� ������� ����������
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      BuyBuffer[bar]=0.0;
      SellBuffer[bar]=0.0;

      if(!flagUP)
         if((AO[bar]>AO[bar+1] && AO[bar+1]>AO[bar+2] && AO[bar+2]>AO[bar+3] && AO[bar+3]>AO[bar+4])
         && (AC[bar]>AC[bar+1] && AC[bar+1]>AC[bar+2] && AC[bar+2]>AC[bar+3] && AC[bar+3]>AC[bar+4]))
           {
            range=ATR[bar]*3/8;
            if(Dir==ON) BuyBuffer[bar]=low[bar]-range;
            else SellBuffer[bar]=high[bar]+range;
            flagUP=true;
           }

      if(!flagDown)
         if((AO[bar]<AO[bar+1] && AO[bar+1]<AO[bar+2] && AO[bar+2]<AO[bar+3] && AO[bar+3]<AO[bar+4])
         && (AC[bar]<AC[bar+1] && AC[bar+1]<AC[bar+2] && AC[bar+2]<AC[bar+3] && AC[bar+3]<AC[bar+4]))
           {
            range=ATR[bar]*3/8;
            if(Dir==ON) SellBuffer[bar]=high[bar]+range;
            else BuyBuffer[bar]=low[bar]-range;
            flagDown=true;
           }

      if(AO[bar+0]<AO[bar+1] || AC[bar+0]<AC[bar+1]) flagUP=false;
      if(AO[bar+0]>AO[bar+1] || AC[bar+0]>AC[bar+1]) flagDown=false;

      //---- ��������� �������� ����������
      if(bar)
        {
         flagUP_=flagUP;
         flagDown_=flagDown;
        }
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
