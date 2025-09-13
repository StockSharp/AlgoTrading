//+---------------------------------------------------------------------+ 
//|                                              SlopeDirectionLine.mq5 | 
//|                                        Copyright � 2006, WizardSerg | 
//|                                                  wizardserg@mail.ru | 
//+---------------------------------------------------------------------+ 
#property copyright "Copyright � 2006, WizardSerg"
#property link "wizardserg@mail.ru"
//--- ����� ������ ����������
#property version   "1.01"
//--- ��������� ���������� � ������� ����
#property indicator_chart_window 
//--- ���������� ������������ �������
#property indicator_buffers 2 
//--- ������������ ����� ���� ����������� ����������
#property indicator_plots   1
//+-----------------------------------+
//| ��������� ��������� ����������    |
//+-----------------------------------+
//--- ��������� ���������� � ���� ������������ �����
#property indicator_type1   DRAW_COLOR_LINE
//--- � �������� ������ ���������� ����� ������������
#property indicator_color1  clrDeepPink,clrGray,clrDarkViolet
//--- ����� ���������� - ����������� ������
#property indicator_style1  STYLE_SOLID
//--- ������� ����� ���������� ����� 2
#property indicator_width1  2
//--- ����������� ����� ����������
#property indicator_label1  "SlopeDirectionLine"
//+-----------------------------------+
//| �������� ������ CXMA              |
//+-----------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------+
//--- ���������� ���������� ������ CXMA �� ����� SmoothAlgorithms.mqh
CXMA XMA1,XMA2,XMA3;
//+-----------------------------------+
//| ���������� ������������           |
//+-----------------------------------+
enum Applied_price_ //��� ���������
  {
   PRICE_CLOSE_ = 1,     //Close
   PRICE_OPEN_,          //Open
   PRICE_HIGH_,          //High
   PRICE_LOW_,           //Low
   PRICE_MEDIAN_,        //Median Price (HL/2)
   PRICE_TYPICAL_,       //Typical Price (HLC/3)
   PRICE_WEIGHTED_,      //Weighted Close (HLCC/4)
   PRICE_SIMPL_,         //Simple Price (OC/2)
   PRICE_QUARTER_,       //Quarted Price (HLOC/4) 
   PRICE_TRENDFOLLOW0_,  //TrendFollow_1 Price 
   PRICE_TRENDFOLLOW1_,  //TrendFollow_2 Price
   PRICE_DEMARK_         //Demark Price
  };
//+-----------------------------------+
//| ���������� ������������           |
//+-----------------------------------+
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
//+-----------------------------------+
//| ������� ��������� ����������      |
//+-----------------------------------+
input Smooth_Method MA_Method1=MODE_LWMA;             // ����� ������� ����������
input uint Length1=12;                                // ������� ������� ����������
input int Phase1=15;                                  // �������� ������� ����������
input Smooth_Method MA_Method2=MODE_SMA;              // ����� ���������� ������� ����������� 
input int Phase2=15;                                  // �������� ������� �����������
input Applied_price_ IPC=PRICE_CLOSE;                 // ������� ���������
input int Shift=0;                                    // ����� ���������� �� ����������� � �����
input int PriceShift=0;                               // ����� ���������� �� ��������� � �������
input bool On_Push = false;                           // ���������� �� �������� push-���������
input bool On_Email = false;                          // ���������� �� �������� �����
input bool On_Alert = true;                           // ���������� �� ������ ������
input bool On_Play_Sound = false;                     // ���������� �� ������ ��������� �������
input string NameFileSound = "expert.wav";            // ��� ��� ����� ��������� �������
input string  CommentSirName="SlopeDirectionLine: ";  // ������ ����� �����-��������
input uint SignalBar=1;                               // ����� ���� ��� �������
//+-----------------------------------+
//--- ���������� ������������ ��������, ������� � ����������
//--- ����� ������������ � �������� ������������ �������
double IndBuffer[];
double ColorIndBuffer[];
//--- ���������� ���������� �������� �������� ����������
int LengthX,LengthR;
//--- ���������� ���������� �������� ������������� ������ �������
double dPriceShift;
//--- ���������� ������������� ���������� ������ ������� ������
int min_rates_total,min_rates_;
//+------------------------------------------------------------------+
//| ��������� ���������� � ���� ������                               |
//+------------------------------------------------------------------+
string GetStringTimeframe(ENUM_TIMEFRAMES timeframe)
  {return(StringSubstr(EnumToString(timeframe),7,-1));}
//+------------------------------------------------------------------+   
//| SlopeDirectionLine indicator initialization function             | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//--- ������������� ���������� ������ ������� ������
   LengthX=int(Length1/2);
   LengthR=int(MathMax(MathSqrt(Length1),1));
   min_rates_=+XMA1.GetStartBars(MA_Method1,Length1,Phase1);
   min_rates_total=min_rates_+XMA1.GetStartBars(MA_Method2,LengthR,Phase2);
//--- ������������� ������ �� ���������
   dPriceShift=_Point*PriceShift;
//--- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//--- ����������� ������������� ������� � ��������, ��������� �����   
   SetIndexBuffer(1,ColorIndBuffer,INDICATOR_COLOR_INDEX);
//--- ������������� ������ ���������� 1 �� �����������
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//--- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0.0);
//--- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   string Smooth2=XMA1.GetString_MA_Method(MA_Method2);
   StringConcatenate(shortname,"SlopeDirectionLine(",Length1,", ",LengthR,", ",Smooth2,")");
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits+1);
//--- ���������� �������������
  }
//+------------------------------------------------------------------+ 
//| SlopeDirectionLine iteration function                            | 
//+------------------------------------------------------------------+ 
int OnCalculate(const int rates_total,    // ���������� ������� � ����� �� ������� ����
                const int prev_calculated,// ���������� ������� � ����� �� ���������� ����
                const datetime &time[],
                const double &open[],
                const double &high[],
                const double &low[],
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
  {
//--- �������� ���������� ����� �� ������������� ��� �������
   if(rates_total<min_rates_total) return(0);
//--- ���������� ���������� � ��������� ������  
   double price,line,xline;
//--- ���������� ������������� ���������� � ��������� ��� ������������ �����
   int first,bar,clr;
//--- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
      first=0; // ��������� ����� ��� ������� ���� �����
   else first=prev_calculated-1; // ��������� ����� ��� ������� ����� �����
//--- �������� ���� ������� ����������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      price=PriceSeries(IPC,bar,open,low,high,close);

      line=XMA1.XMASeries(0,prev_calculated,rates_total,MA_Method1,Phase1,Length1,price,bar,false);
      line=2*XMA2.XMASeries(0,prev_calculated,rates_total,MA_Method1,Phase1,LengthX,price,bar,false)-line;
      xline=XMA3.XMASeries(min_rates_,prev_calculated,rates_total,MA_Method2,Phase2,LengthR,line,bar,false);
      //---       
      IndBuffer[bar]=xline+dPriceShift;
     }
//--- ������������� �������� ���������� first
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
      first=min_rates_total-1; // ��������� ����� ��� ������� ���� �����
//--- �������� ���� ��������� ���������� �����
   for(bar=first; bar<rates_total; bar++)
     {
      clr=1;
      ColorIndBuffer[bar]=1;
      if(IndBuffer[bar-1]<IndBuffer[bar]) clr=2;
      if(IndBuffer[bar-1]>IndBuffer[bar]) clr=0;
      ColorIndBuffer[bar]=clr;

      if(bar==rates_total-1-SignalBar)
        {
         if(ColorIndBuffer[bar-1]!=2 && clr==2)
           {
            datetime SignalTime=TimeCurrent();
            if(On_Play_Sound) PlaySound(NameFileSound);
            string period=GetStringTimeframe(Period());
            string comment,sTime=" CurrTime="+TimeToString(SignalTime,TIME_MINUTES);
            StringConcatenate(comment,CommentSirName,Symbol(),period," ",sTime," ������ �� �������!");
            if(On_Alert) Alert(comment);
            if(On_Push) SendNotification(comment);
            if(On_Email) SendMail(CommentSirName+Symbol()+period,comment);
           }
           
         if(ColorIndBuffer[bar-1]!=0 && clr==0)
           {
            datetime SignalTime=TimeCurrent();
            if(On_Play_Sound) PlaySound(NameFileSound);
            string period=GetStringTimeframe(Period());
            string comment,sTime=" CurrTime="+TimeToString(SignalTime,TIME_MINUTES);
            StringConcatenate(comment,CommentSirName,Symbol(),period," ",sTime," ������ �� �������!");
            if(On_Alert) Alert(comment);
            if(On_Push) SendNotification(comment);
            if(On_Email) SendMail(CommentSirName+Symbol()+period,comment);
           }
        }
     }
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+
