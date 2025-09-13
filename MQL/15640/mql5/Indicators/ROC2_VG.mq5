//+------------------------------------------------------------------+
//|                                                      ROC2_VG.mq5 |
//|                         Copyright � 2006, Vladislav Goshkov (VG) |
//|                                                      4vg@mail.ru |
//+------------------------------------------------------------------+
#property copyright "Copyright � 2006, Vladislav Goshkov (VG)"
#property link      "4vg@mail.ru"
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � ��������� ����
#property indicator_separate_window
//---- ���������� ������������ ������� 2
#property indicator_buffers 2 
//---- ������������ ����� ���� ����������� ����������
#property indicator_plots   1
//+----------------------------------------------+
//|  ��������� ��������� ����������              |
//+----------------------------------------------+
//---- ��������� ���������� 1 � ���� ������
#property indicator_type1   DRAW_FILLING
//---- � �������� ������ ������
#property indicator_color1  clrForestGreen,clrOrangeRed
//---- ����������� ����� ����������
//+----------------------------------------------+
//|  ���������� ��������                         |
//+----------------------------------------------+
#define RESET  0 // ��������� ��� �������� ��������� ������� �� �������� ����������
//+----------------------------------------------+
//|  ���������� ������������                     |
//+----------------------------------------------+
enum ENUM_TYPE
  {
   MOM=1,  //MOM
   ROC,    //ROC
   ROCP,   //ROCP
   ROCR,   //ROC
   ROCR100 //ROCR100
  };
//+----------------------------------------------+
//|  ������� ��������� ����������                |
//+----------------------------------------------+
input uint ROCPeriod1=8;
input ENUM_TYPE ROCType1=MOM;
input uint ROCPeriod2=14;
input ENUM_TYPE ROCType2=MOM;
input int Shift=0;                               // ����� ���������� �� ����������� � �����
input double Livel1=+0.005;
input double Livel2=+0.002;
input double Livel3=0.00;
input double Livel4=-0.002;
input double Livel5=-0.005;
//+----------------------------------------------+
//---- ���������� ������������ ��������, ������� ����� � 
// ���������� ������������ � �������� ������������ �������
double IndBuffer1[];
double IndBuffer2[];
//---- ���������� ����� ���������� ������ ������� ������
int min_rates_total,min_rates_1,min_rates_2;
//---- ���������� ����� ���������� ��� ������� �����������
int RSI_Handle;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
int OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   min_rates_total=int(MathMax(ROCPeriod1,ROCPeriod2));
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,IndBuffer1,INDICATOR_DATA);
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(1,IndBuffer2,INDICATOR_DATA);
//---- ������������� ������ ���������� 1 �� ����������� �� Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ���������� 1 �� min_rates_total
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);

//---- ������������� ���������� ��� ��������� ����� ����������
   string short_name="ROC2_VG( "+EnumToString(ROCType1)+" = "+string(ROCPeriod1)+" ,"+EnumToString(ROCType2)+" = "+string(ROCPeriod2)+")";
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,short_name);
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- ����������  �������������� ������� ���������� 3   
   IndicatorSetInteger(INDICATOR_LEVELS,5);
//---- �������� �������������� ������� ����������   
   IndicatorSetDouble(INDICATOR_LEVELVALUE,0,Livel1);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,1,Livel2);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,2,Livel3);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,3,Livel4);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,4,Livel5);
//---- � �������� ������ ����� �������������� ������� ������������ �����  
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,0,clrDodgerBlue);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,1,clrBlue);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,2,clrGray);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,3,clrRed);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,4,clrMagenta);
//---- � ����� ��������������� ������ ����������� �������� �����-�������  
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,0,STYLE_DASHDOTDOT);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,1,STYLE_DASHDOTDOT);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,2,STYLE_DASH);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,3,STYLE_DASHDOTDOT);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,4,STYLE_DASHDOTDOT);
//---- ���������� �������������
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(
                const int rates_total,    // ���������� ������� � ����� �� ������� ����
                const int prev_calculated,// ���������� ������� � ����� �� ���������� ����
                const datetime &time[],
                const double &open[],
                const double& high[],     // ������� ������ ���������� ���� ��� ������� ����������
                const double& low[],      // ������� ������ ��������� ����  ��� ������� ����������
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[]
                )
  {
//---- �������� ���������� ����� �� ������������� ��� �������
   if(rates_total<min_rates_total) return(RESET);

//---- ���������� ���������� � ��������� ������  
   double price,prevPrice;
//---- ���������� ����� ����������
   int first,bar;

//---- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
      first=min_rates_total; // ��������� ����� ��� ������� ���� �����
   else first=prev_calculated-1; // ��������� ����� ��� ������� ����� �����

//---- �������� ���� ������� ����������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      price     = close[bar];
      prevPrice = close[bar-ROCPeriod1];
      switch(ROCType1)
        {
         case MOM : IndBuffer1[bar]= (price - prevPrice);             break; //"MOM"
         case ROC : IndBuffer1[bar]= ((price/prevPrice)-1)*100;       break; //"ROC"
         case ROCP : IndBuffer1[bar]= (price-prevPrice)/prevPrice;    break; //"ROCP"
         case ROCR : IndBuffer1[bar]= (price/prevPrice);              break; //"ROCR"
         case ROCR100 : IndBuffer1[bar]= (price/prevPrice)*100;       break; //"ROCR100"
         default: IndBuffer1[bar]=(price-prevPrice)/prevPrice;       break;
        }

      prevPrice=close[bar-ROCPeriod2];
      switch(ROCType2)
        {
         case MOM : IndBuffer2[bar]= (price - prevPrice);             break; //"MOM"
         case ROC : IndBuffer2[bar]= ((price/prevPrice)-1)*100;       break; //"ROC"
         case ROCP : IndBuffer2[bar]= (price-prevPrice)/prevPrice;    break; //"ROCP"
         case ROCR : IndBuffer2[bar]= (price/prevPrice);              break; //"ROCR"
         case ROCR100 : IndBuffer2[bar]= (price/prevPrice)*100;       break; //"ROCR100"
         default: IndBuffer2[bar]=(price-prevPrice)/prevPrice;       break;
        }
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
