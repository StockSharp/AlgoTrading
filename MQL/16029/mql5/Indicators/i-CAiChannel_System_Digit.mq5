//+---------------------------------------------------------------------+
//|                                       i-CAiChannel_System_Digit.mq5 | 
//|                         Copyright � RickD 2006, Alexander Piechotta | 
//|                                        http://onix-trade.net/forum/ | 
//+---------------------------------------------------------------------+ 
//| ��� ������  ����������  �������  �������� ���� SmoothAlgorithms.mqh |
//| � ����� (����������): �������_������_���������\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property copyright "Copyright � RickD 2006, Alexander Piechotta"
#property link      "http://onix-trade.net/forum/"
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � ������� ����
#property indicator_chart_window 
//---- ���������� ������������ ������� 11
#property indicator_buffers 11 
//---- ������������ ����� ������ ����������� ����������
#property indicator_plots   4
//+----------------------------------------------+
//|  ��������� ��������� ������                  |
//+----------------------------------------------+
//---- ��������� ���������� � ���� �������� ������
#property indicator_type1   DRAW_FILLING
//---- � �������� ����� ������ ����������� Lavender
#property indicator_color1  clrLavender
//---- ����������� ����� ����������
#property indicator_label1  "i-CAiChannel Cloud"
//+----------------------------------------------+
//|  ��������� ��������� ������� �������         |
//+----------------------------------------------+
//---- ��������� ���������� 2 � ���� �����
#property indicator_type2   DRAW_LINE
//---- � �������� ����� ����� ����� ���������� ����������� DodgerBlue
#property indicator_color2  clrDodgerBlue
//---- ����� ���������� 2 - ����������� ������
#property indicator_style2  STYLE_SOLID
//---- ������� ����� ���������� 2 ����� 2
#property indicator_width2  2
//---- ����������� ����� ����� ����������
#property indicator_label2  "Upper i-CAiChannel"
//+----------------------------------------------+
//|  ��������� ��������� ������ �����            |
//+----------------------------------------------+
//---- ��������� ���������� 3 � ���� �����
#property indicator_type3   DRAW_LINE
//---- � �������� ����� ��������� ����� ���������� ����������� Orange
#property indicator_color3  clrOrange
//---- ����� ���������� 3 - ����������� ������
#property indicator_style3  STYLE_SOLID
//---- ������� ����� ���������� 3 ����� 2
#property indicator_width3  2
//---- ����������� ��������� ����� ����������
#property indicator_label3  "Lower i-CAiChannel"
//+----------------------------------------------+
//|  ��������� ��������� ���������� 4            |
//+----------------------------------------------+
//---- ��������� ���������� � ���� ������� ����
#property indicator_type4 DRAW_COLOR_CANDLES
//---- � �������� ������ ���������� ������������
#property indicator_color4 clrMagenta,clrPurple,clrGray,clrTeal,clrMediumSpringGreen
//---- ����� ���������� - ��������
#property indicator_style4 STYLE_SOLID
//---- ������� ����� ���������� ����� 2
#property indicator_width4 2
//---- ����������� ����� ����������
#property indicator_label4 "PChannel_CANDLES"
//+----------------------------------------------+
//|  �������� ������ CXMA                        |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+

//---- ���������� ���������� ������� CXMA � CStdDeviation �� ����� SmoothAlgorithms.mqh
CXMA XMA1;
CStdDeviation STD;
//+----------------------------------------------+
//|  ���������� ������������                     |
//+----------------------------------------------+
enum Applied_price_      //��� ���������
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
//|  ���������� ������������                     |
//+----------------------------------------------+
/*enum SmoothMethod - ������������ ��������� � ����� SmoothAlgorithms.mqh
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
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input string SirName="i-CAiChannel_System_Digit";//������ ����� ����� ����������� ��������
input Smooth_Method XMA_Method=MODE_SMA_; //����� ����������
input uint XLength=12;                    //������� �����������                    
input int XPhase=15;                      //�������� �����������,
//---- ��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������;
//---- ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input Applied_price_ IPC=PRICE_CLOSE_;    //������� ���������
input uint Dev=1000;                      //�������� ������ ������ � ������� 
input uint Digit=2;                       //���������� �������� ����������                  
input int Shift=2;                        //����� ������ �� ����������� � �����
input bool ShowPrice=true;                //���������� ������� ����� 
input color Upper_color=clrBlue;
input color Lower_color=clrMagenta;
//+----------------------------------------------+

//---- ���������� ������������ ��������, ������� ����� � ���������� ������������ � �������� ������������ �������
double ExtUp1Buffer[],ExtUp2Buffer[],ExtDn1Buffer[],ExtDn2Buffer[];
double ExtOpenBuffer[],ExtHighBuffer[],ExtLowBuffer[],ExtCloseBuffer[],ExtColorBuffer[];

//---- ���������� ���������� �������� ������������� ������ �������
double dDev;
//---- ���������� ����� ���������� ������ ������� ������
int min_rates_total;
//---- ���������� �������� ��� ��������� �����
string upper_name,lower_name;
double PointPow10;
//+------------------------------------------------------------------+   
//| i-CAi indicator initialization function                          | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   min_rates_total=GetStartBars(XMA_Method,XLength,XPhase)+Shift;
//---- ������������� ��������
   upper_name=SirName+" upper text lable";
   lower_name=SirName+" lower text lable";
//---- ������������� ����������         
   PointPow10=_Point*MathPow(10,Digit);
   dDev=Dev*_Point;

//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,ExtUp1Buffer,INDICATOR_DATA);
   SetIndexBuffer(1,ExtDn1Buffer,INDICATOR_DATA);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- ������������� ������ ���������� �� �����������
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
   
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(2,ExtUp2Buffer,INDICATOR_DATA);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- ������������� ������ ���������� �� �����������
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);

//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(3,ExtDn2Buffer,INDICATOR_DATA);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- ������������� ������ ���������� �� �����������
   PlotIndexSetInteger(2,PLOT_SHIFT,Shift);
   
//---- ����������� ������������� ������� IndBuffer � ������������ �����
   SetIndexBuffer(4,ExtOpenBuffer,INDICATOR_DATA);
   SetIndexBuffer(5,ExtHighBuffer,INDICATOR_DATA);
   SetIndexBuffer(6,ExtLowBuffer,INDICATOR_DATA);
   SetIndexBuffer(7,ExtCloseBuffer,INDICATOR_DATA);

//---- ����������� ������������� ������� � ��������, ��������� �����   
   SetIndexBuffer(8,ExtColorBuffer,INDICATOR_COLOR_INDEX);
   
//---- ������������� ������ ���������� 3 �� ����������� �� Shift
   PlotIndexSetInteger(3,PLOT_SHIFT,0);
//---- ������������� ������ ������ ������� ��������� ���������� 4 �� min_rates_total
   PlotIndexSetInteger(3,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(3,PLOT_EMPTY_VALUE,0);

//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   string Smooth1=XMA1.GetString_MA_Method(XMA_Method);
   StringConcatenate(shortname,"i-CAiChannel_System_Digit(",XLength,", ",Smooth1,", ",Dev,")");
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);

//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits+1);
//---- ���������� �������������
  }
//+------------------------------------------------------------------+
//| Custom indicator deinitialization function                       |
//+------------------------------------------------------------------+    
void OnDeinit(const int reason)
  {
//----
   ObjectDelete(0,upper_name);
   ObjectDelete(0,lower_name);
//----
   ChartRedraw(0);
  }
//+------------------------------------------------------------------+ 
//| i-CAi iteration function                                         | 
//+------------------------------------------------------------------+ 
int OnCalculate(
                const int rates_total,    // ���������� ������� � ����� �� ������� ����
                const int prev_calculated,// ���������� ������� � ����� �� ���������� ����
                const datetime &time[],
                const double &open[],
                const double &high[],
                const double &low[],
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[]
                )
  {
//---- �������� ���������� ����� �� ������������� ��� �������
   if(rates_total<min_rates_total) return(0);

//---- ���������� ���������� � ��������� ������  
   double price,xma,stdev,powstdev,powdxma,koeff,line;
   static double line_prev;
//---- ���������� ����� ���������� � ��������� ��� ����������� �����
   int first,bar;

//---- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
     {
      first=1; // ��������� ����� ��� ������� ���� �����
     }
   else first=prev_calculated-1; // ��������� ����� ��� ������� ����� �����

//---- �������� ���� ������� ����������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      price=PriceSeries(IPC,bar,open,low,high,close);
      xma=XMA1.XMASeries(1,prev_calculated,rates_total,XMA_Method,XPhase,XLength,price,bar,false);
      stdev=STD.StdDevSeries(1,prev_calculated,rates_total,XLength,1,price,xma,bar,false);
      powstdev=MathPow(stdev,2);     
      if(bar<=min_rates_total) line_prev=xma;     
      powdxma=MathPow(line_prev-xma,2);
      if(powdxma<powstdev || !powdxma) koeff=NULL;
      else koeff=1.0-powstdev/powdxma;      
      line=line_prev+koeff*(xma-line_prev);     
      ExtUp1Buffer[bar]=line+dDev;
      ExtDn1Buffer[bar]=line-dDev;     
      ExtUp1Buffer[bar]=ExtUp2Buffer[bar]=PointPow10*MathCeil(ExtUp1Buffer[bar]/PointPow10);
      ExtDn1Buffer[bar]=ExtDn2Buffer[bar]=PointPow10*MathFloor(ExtDn1Buffer[bar]/PointPow10);     
      if(bar<rates_total-1) line_prev=line;
     }

   if(prev_calculated>rates_total || prev_calculated<=0) first=min_rates_total;   
//---- �������� ���� ��������� ����� ����������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      int clr=2;
      ExtOpenBuffer[bar]=NULL;
      ExtCloseBuffer[bar]=NULL;
      ExtHighBuffer[bar]=NULL;
      ExtLowBuffer[bar]=NULL;

      if(close[bar]>ExtUp1Buffer[bar-Shift])
        {
         if(open[bar]<=close[bar]) clr=4;
         else clr=3;
         ExtOpenBuffer[bar]=open[bar];
         ExtCloseBuffer[bar]=close[bar];
         ExtHighBuffer[bar]=high[bar];
         ExtLowBuffer[bar]=low[bar];
        }

      if(close[bar]<ExtDn1Buffer[bar-Shift])
        {
         if(open[bar]>close[bar]) clr=0;
         else clr=1;
         ExtOpenBuffer[bar]=open[bar];
         ExtCloseBuffer[bar]=close[bar];
         ExtHighBuffer[bar]=high[bar];
         ExtLowBuffer[bar]=low[bar];
        }
        
      ExtColorBuffer[bar]=clr;
     }
   if(ShowPrice)
     {
      int bar0=int(rates_total-1-Shift);
      datetime time0=time[rates_total-1];
      SetRightPrice(0,upper_name,0,time0,ExtUp1Buffer[bar0],Upper_color);
      SetRightPrice(0,lower_name,0,time0,ExtDn1Buffer[bar0],Lower_color);
     }
//----     
   ChartRedraw(0);
   return(rates_total);
  }
//+------------------------------------------------------------------+
//|  RightPrice creation                                             |
//+------------------------------------------------------------------+
void CreateRightPrice(long chart_id,// chart ID
                      string   name,              // object name
                      int      nwin,              // window index
                      datetime time,              // price level time
                      double   price,             // price level
                      color    Color              // Text color
                      )
//---- 
  {
//----
   ObjectCreate(chart_id,name,OBJ_ARROW_RIGHT_PRICE,nwin,time,price);
   ObjectSetInteger(chart_id,name,OBJPROP_COLOR,Color);
   ObjectSetInteger(chart_id,name,OBJPROP_BACK,true);
   ObjectSetInteger(chart_id,name,OBJPROP_WIDTH,2);
//----
  }
//+------------------------------------------------------------------+
//|  RightPrice reinstallation                                       |
//+------------------------------------------------------------------+
void SetRightPrice(long chart_id,// chart ID
                   string   name,              // object name
                   int      nwin,              // window index
                   datetime time,              // price level time
                   double   price,             // price level
                   color    Color              // Text color
                   )
//---- 
  {
//----
   if(ObjectFind(chart_id,name)==-1) CreateRightPrice(chart_id,name,nwin,time,price,Color);
   else ObjectMove(chart_id,name,0,time,price);
//----
  }
//+------------------------------------------------------------------+
