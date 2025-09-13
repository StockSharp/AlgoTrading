//+------------------------------------------------------------------+
//|                                                     EMAAngle.mq5 |
//|                                         Copyright � 2008, jpkfox |
//|                                                                  |
//| You can use this indicator to measure when the EMA angle is      |
//| "near zero". AngleTreshold determines when the angle for the     |
//| EMA is "about zero": This is when the value is between           |
//| [-AngleTreshold, AngleTreshold] (or when the histogram is red).  |
//|   EMAPeriod: EMA period                                          |
//|   AngleTreshold: The angle value is "about zero" when it is      |
//|     between the values [-AngleTreshold, AngleTreshold].          |      
//|   StartEMAShift: The starting point to calculate the             |   
//|     angle. This is a shift value to the left from the            |
//|     observation point. Should be StartEMAShift > EndEMAShift.    | 
//|   StartEMAShift: The ending point to calculate the               |
//|     angle. This is a shift value to the left from the            | 
//|     observation point. Should be StartEMAShift > EndEMAShift.    |
//|                                                                  |
//|   Modified by MrPip                                              |
//|       Red for down                                               |
//|       Yellow for near zero                                       |
//|       Green for up                                               |
//|  10/15/05  MrPip                                                 |
//|            Corrected problem with USDJPY and optimized code      |   
//|                                                                  |
//+------------------------------------------------------------------+
#property  copyright "Copyright � 2008, jpkfox"
#property  link      "http://www.strategybuilderfx.com/forums/showthread.php?t=15274&page=1&pp=8"
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � ��������� ����
#property indicator_separate_window 
//---- ���������� ������������ ������� 2
#property indicator_buffers 2 
//---- ������������ ���� ����������� ����������
#property indicator_plots   1
//+-----------------------------------+
//|  ��������� ��������� ����������   |
//+-----------------------------------+
//---- ��������� ���������� � ���� ������������� �����������
#property indicator_type1 DRAW_COLOR_HISTOGRAM
//---- � �������� ������ ����������� ����������� ������������
#property indicator_color1 clrMagenta,clrPurple,clrGray,clrTeal,clrChartreuse
//---- ����� ���������� - ��������
#property indicator_style1 STYLE_SOLID
//---- ������� ����� ���������� ����� 2
#property indicator_width1 2
//---- ����������� ����� ����������
#property indicator_label1  "EMAAngle"
//+-----------------------------------+
//|  ���������� ��������              |
//+-----------------------------------+
#define RESET  0       // ��������� ��� �������� ��������� ������� �� �������� ����������
#define PI     3.14159 // �������� ����� ��
//+-----------------------------------+
//|  ������� ��������� ����������     |
//+-----------------------------------+
input uint EMAPeriod=34;
input  ENUM_MA_METHOD   MAType=MODE_EMA;
input ENUM_APPLIED_PRICE   MAPrice=PRICE_CLOSE;
input double AngleTreshold=3.0;
input uint StartEMAShift=6;
input uint EndEMAShift=0;
//+-----------------------------------+

//---- ���������� ����� ���������� ������ ������� ������
int  min_rates_total;
//---- ���������� ������������ ��������, ������� ����� � 
// ���������� ������������ � �������� ������������ �������
double IndBuffer[],ColorIndBuffer[];
//---- ���������� ����� ���������� ��� ������� �����������
int MA_Handle;
double dFactor,mFactor;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   if(StartEMAShift<=EndEMAShift)
     {
      Print("�������� �������� �������� ���������� StartEMAShift � EndEMAShift!!!");
      return;
     }
   min_rates_total=int(EMAPeriod +MathMax(StartEMAShift,EndEMAShift));
   
//---- ��������� ������ ���������� iMA
   MA_Handle=iMA(NULL,0,EMAPeriod,0,MAType,MAPrice);
   if(MA_Handle==INVALID_HANDLE) Print(" �� ������� �������� ����� ���������� iMA");
//----  
   dFactor=3.14159/180.0;
   mFactor=10000.0;
   if (Symbol()=="USDJPY") mFactor=100.0;
   double ShiftDif=StartEMAShift-EndEMAShift;
   mFactor/=ShiftDif;
   
//---- ����������� ������������� ������� IndBuffer � ������������ �����
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(IndBuffer,true);

//---- ����������� ������������� ������� � ��������, ��������� �����   
   SetIndexBuffer(1,ColorIndBuffer,INDICATOR_COLOR_INDEX);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(ColorIndBuffer,true);

//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,"EMAAngle("+string(EMAPeriod)+")");
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
    
//---- ����������  �������������� ������� ���������� 2   
   IndicatorSetInteger(INDICATOR_LEVELS,2);
//---- �������� �������������� ������� ����������   
   IndicatorSetDouble(INDICATOR_LEVELVALUE,0,+AngleTreshold);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,1,-AngleTreshold);
//---- � �������� ������ ����� �������������� ������� ����������� ������� � ����� �����  
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,0,clrMagenta);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,1,clrBlue);
//---- � ����� ��������������� ������ ����������� �������� �����-�������  
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,0,STYLE_DASHDOTDOT);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,1,STYLE_DASHDOTDOT);
//---- ���������� �������������
  }
//+------------------------------------------------------------------+  
//| Custom indicator iteration function                              | 
//+------------------------------------------------------------------+  
int OnCalculate(
                const int rates_total,    // ���������� ������� � ����� �� ������� ����
                const int prev_calculated,// ���������� ������� � ����� �� ���������� ����
                const datetime &Time[],
                const double &Open[],
                const double &High[],
                const double &Low[],
                const double &Close[],
                const long &Tick_Volume[],
                const long &Volume[],
                const int &Spread[]
                )
  {
//---- �������� ���������� ����� �� ������������� ��� �������
   if(BarsCalculated(MA_Handle)<rates_total
      || rates_total<min_rates_total)
      return(RESET);
      
//---- ���������� ��������� ���������� 
   int to_copy,limit,bar,clr;
   double fEndMA,fStartMA,fAngle,MA[];

//---- ������ ���������� ������ limit ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
        limit=rates_total-min_rates_total-1; // ��������� ����� ��� ������� ���� �����
   else limit=rates_total-prev_calculated;  // ��������� ����� ��� ������� ������ ����� �����

//---- ���������� ��������� � �������� ��� � ����������  
   ArraySetAsSeries(High,true);
   ArraySetAsSeries(Low,true);  
   ArraySetAsSeries(MA,true); 
   
   to_copy=limit+min_rates_total+1;
   
//---- �������� ����� ����������� ������ � �������
   if(CopyBuffer(MA_Handle,0,0,to_copy,MA)<=0) return(RESET);
   
//---- �������� ���� ������� ����������
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      fEndMA=MA[bar+EndEMAShift];
      fStartMA=MA[bar+StartEMAShift];
      //---- 10000.0 : Multiply by 10000 so that the fAngle is not too small
      //---- for the indicator Window.
      fAngle=mFactor*(fEndMA-fStartMA);
      //---- fAngle = MathArctan(fAngle)/dFactor;
      IndBuffer[bar]=fAngle;
//----
      clr=2;

      if(fAngle>0)
        {
         if(fAngle>+AngleTreshold) clr=4;
         else clr=3;
        }
        
      if(fAngle<0)
        {
         if(fAngle<-AngleTreshold) clr=0;
         else clr=1;
        }
        
      ColorIndBuffer[bar]=clr;
     }
//----    
   return(rates_total);
  }
//+------------------------------------------------------------------+
