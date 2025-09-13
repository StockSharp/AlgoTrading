//+------------------------------------------------------------------+
//|                                                   LSMA_Angle.mq5 |
//|                                                           MrPip  |
//|                                                                  |
//| You can use this indicator to measure when the LSMA angle is     |
//| "near zero". AngleTreshold determines when the angle for the     |
//| LSMA is "about zero": This is when the value is between          |
//| [-AngleTreshold, AngleTreshold] (or when the histogram is red).  |
//|   LSMAPeriod: LSMA period                                        |
//|   AngleTreshold: The angle value is "about zero" when it is      |
//|     between the values [-AngleTreshold, AngleTreshold].          |      
//|   StartLSMAShift: The starting point to calculate the            |   
//|     angle. This is a shift value to the left from the            |
//|     observation point. Should be StartEMAShift > EndEMAShift.    | 
//|   EndLSMAShift: The ending point to calculate the                |
//|     angle. This is a shift value to the left from the            | 
//|     observation point. Should be StartEMAShift > EndEMAShift.    |
//|                                                                  |
//|   Modified by MrPip from EMAAngle by jpkfox                      |
//|       Red for down                                               |
//|       Yellow for near zero                                       |
//|       Green for up                                               |   
//|                                                                  |
//+------------------------------------------------------------------+
#property copyright "Copyright � 2005, Robert L. Hill aka MrPip"
#property link "" 
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � ��������� ����
#property indicator_separate_window 
//---- ���������� ������������ ������� 2
#property indicator_buffers 2 
//---- ������������ ����� ���� ����������� ����������
#property indicator_plots   1
//+-----------------------------------+
//|  ��������� ��������� ����������   |
//+-----------------------------------+
//---- ��������� ���������� � ���� ������������� �����������
#property indicator_type1 DRAW_COLOR_HISTOGRAM
//---- � �������� ������ ������������� ����������� ������������
#property indicator_color1 clrMagenta,clrViolet,clrGray,clrDodgerBlue,clrBlue
//---- ����� ���������� - ��������
#property indicator_style1 STYLE_SOLID
//---- ������� ����� ���������� ����� 2
#property indicator_width1 2
//---- ����������� ����� ����������
#property indicator_label1 "LSMA_Angle"

//+-----------------------------------+
//|  ������� ��������� ����������     |
//+-----------------------------------+
input uint LSMAPeriod=25;
input int  AngleTreshold=15; // ����� ������������
input uint StartLSMAShift=4;
input uint EndLSMAShift=0;
//+-----------------------------------+
double mFactor,dFactor,ShiftDif;
//---- ���������� ����� ���������� ������ ������� ������
int min_rates_total;
//---- ���������� ������������ ��������, ������� ����� � 
// ���������� ������������ � �������� ������������ �������
double IndBuffer[],ColorIndBuffer[];
//+------------------------------------------------------------------+
//|  LSMA - Least Squares Moving Average function calculation        |
//+------------------------------------------------------------------+
double LSMA(int Rperiod,const double &Close[],int shift)
  {
//----
   int iii;
   double sum;
   int length;
   double lengthvar;
   double tmp;
   double wt;
//----
   length=Rperiod;
//---- 
   sum=0;
   for(iii=length; iii>=1; iii--)
     {
      lengthvar = length+1;
      lengthvar/= 3;
      tmp=(iii-lengthvar)*Close[length-iii+shift];
      sum+=tmp;
     }
   wt=sum*6/(length*(length+1));
//----
   return(wt);
  }
//+------------------------------------------------------------------+    
//| LSMA_Angle indicator initialization function                     | 
//+------------------------------------------------------------------+  
int OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   min_rates_total=int(MathMax(LSMAPeriod+StartLSMAShift,LSMAPeriod+EndLSMAShift));
   dFactor = 2*3.14159/180.0;
   mFactor = 100000.0;
   string Sym = StringSubstr(Symbol(),3,3);
   if (Sym == "JPY") mFactor = 1000.0;
   ShiftDif = StartLSMAShift-EndLSMAShift;
   mFactor /= ShiftDif; 

   if(EndLSMAShift>=StartLSMAShift)
     {
      Print("Error: EndLSMAShift >= StartLSMAShift");
      return(INIT_FAILED);
     }

//---- ����������� ������������� ������� IndBuffer � ������������ �����
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(IndBuffer,true);

//---- ����������� ������������� ������� � ��������, ��������� �����   
   SetIndexBuffer(1,ColorIndBuffer,INDICATOR_COLOR_INDEX);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(ColorIndBuffer,true);
   
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0.0);

//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,"LSMA_Angle");
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,0);
   
//---- ����������  �������������� ������� ���������� 9   
   IndicatorSetInteger(INDICATOR_LEVELS,2);
//---- �������� �������������� ������� ����������   
   IndicatorSetDouble(INDICATOR_LEVELVALUE,0,+AngleTreshold);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,1,-AngleTreshold);
//---- � �������� ������ ����� �������������� ������� ������������ ����� � ������� �����  
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,0,clrTeal);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,1,clrTeal);
//---- � ����� ��������������� ������ ����������� �������� �����-�������  
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,0,STYLE_DASHDOTDOT);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,1,STYLE_DASHDOTDOT);

//---- ���������� �������������
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+  
//| LSMA_Angle iteration function                                    | 
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

//---- ���������� ��������� ���������� 
   int limit,bar,clr;
   double fEndMA,fStartMA,fAngle;

//---- ������ ���������� ������ limit ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
     {
      limit=rates_total-1-min_rates_total; // ��������� ����� ��� ������� ���� �����
     }
   else
     {
      limit=rates_total-prev_calculated; // ��������� ����� ��� ������� ����� �����
     }
    
//---- ���������� ��������� � �������� ��� � ����������  
   ArraySetAsSeries(close,true);

//---- �������� ���� ������� ����������
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      fEndMA=LSMA(LSMAPeriod,close,bar+EndLSMAShift);
      fStartMA=LSMA(LSMAPeriod,close,bar+StartLSMAShift);
      fAngle=mFactor*(fEndMA-fStartMA)/2;
      IndBuffer[bar]=fAngle;
     }

   if(prev_calculated>rates_total || prev_calculated<=0) limit--;
//---- �������� ���� ��������� ����������
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      clr=2;

      if(IndBuffer[bar]>AngleTreshold)
        {
         if(IndBuffer[bar]>IndBuffer[bar+1]) clr=4;
         if(IndBuffer[bar]<IndBuffer[bar+1]) clr=3;
        }

      if(IndBuffer[bar]<-AngleTreshold)
        {
         if(IndBuffer[bar]<IndBuffer[bar+1]) clr=0;
         if(IndBuffer[bar]>IndBuffer[bar+1]) clr=1;
        }

      ColorIndBuffer[bar]=clr;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
