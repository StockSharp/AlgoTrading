//+------------------------------------------------------------------+
//|                                                 trend_arrows.mq5 |
//|                               Copyright � 2012, Vladimir Mametov | 
//|                                                                  | 
//+------------------------------------------------------------------+ 
#property copyright "Copyright � 2012, Vladimir Mametov" 
#property link      "" 
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � �������� ����
#property indicator_chart_window
//---- ���������� ������������ ������� 4
#property indicator_buffers 4 
//---- ������������ ����� ������ ����������� ����������
#property indicator_plots   4
//+----------------------------------------------+
//|  ��������� ��������� ����������              |
//+----------------------------------------------+
//---- ��������� ���������� � ���� ������
#property indicator_type1 DRAW_ARROW
//---- � �������� ������� ���������� �����������
#property indicator_color1 clrBlue
//---- ����� ���������� - ��������
#property indicator_style1 STYLE_SOLID
//---- ������� ����� ���������� ����� 2
#property indicator_width1 2
//---- ����������� ����� ���������� �����
#property indicator_label1  "trend_arrows Up"
//+----------------------------------------------+
//|  ��������� ��������� ����������              |
//+----------------------------------------------+
//---- ��������� ���������� � ���� ������
#property indicator_type2 DRAW_ARROW
//---- � �������� ������� ���������� �����������
#property indicator_color2 clrRed
//---- ����� ���������� - ��������
#property indicator_style2 STYLE_SOLID
//---- ������� ����� ���������� ����� 2
#property indicator_width2 2
//---- ����������� ����� ���������� �����
#property indicator_label2  "trend_arrows Down"
//+----------------------------------------------+
//|  ��������� ��������� ������� ����������      |
//+----------------------------------------------+
//---- ��������� ���������� 3 � ���� ������
#property indicator_type3   DRAW_ARROW
//---- � �������� ����� ����� ����� ���������� ����������� ���� DodgerBlue
#property indicator_color3  clrDodgerBlue
//---- ����� ���������� 3 - ����������� ������
#property indicator_style3  STYLE_SOLID
//---- ������� ����� ���������� 3 ����� 2
#property indicator_width3  2
//---- ����������� ������ ����� ����������
#property indicator_label3  "Buy trend_arrows signal"
//+----------------------------------------------+
//|  ��������� ��������� ���������� ����������   |
//+----------------------------------------------+
//---- ��������� ���������� 4 � ���� ������
#property indicator_type4   DRAW_ARROW
//---- � �������� ����� ��������� ����� ���������� ����������� ���� DarkOrange
#property indicator_color4  clrDarkOrange
//---- ����� ���������� 2 - ����������� ������
#property indicator_style4  STYLE_SOLID
//---- ������� ����� ���������� 2 ����� 2
#property indicator_width4  2
//---- ����������� ��������� ����� ����������
#property indicator_label4  "Sell trend_arrows signal"
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input uint iPeriod=15;  // ������ ����������
input uint iFullPeriods=1;
input int Shift=0;      // ����� ���������� �� ����������� � ����� 
//+----------------------------------------------+
//---- ���������� ������������ ��������, ������� ����� � 
//---- ���������� ������������ � �������� ������������ �������
double TrendUp[],TrendDown[];
double SignUp[],SignDown[];
//---- ���������� ������������� ���������� ������ ������� ������
int min_rates_total;
int arr[];
bool boolp1;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   min_rates_total=int(iPeriod+iFullPeriods);
//---- ������������� ������ ��� ������� ����������   
   ArrayResize(arr,min_rates_total);

//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   StringConcatenate(shortname,"trend_arrows(",string(iPeriod),", ",string(iFullPeriods),", ",string(Shift),")");
//---- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//---- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);

//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,TrendUp,INDICATOR_DATA);
//---- ������������� ������ ���������� �� ����������� �� Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ������ �� ��������� ����������� ������ ��������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0.0);
//---- ���������� ��������� � �������, ��� � ����������   
   ArraySetAsSeries(TrendUp,true);

//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(1,TrendDown,INDICATOR_DATA);
//---- ������������� ������ ���������� �� ����������� �� Shift
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- ������ �� ��������� ����������� ������ ��������
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,0.0);
//---- ���������� ��������� � �������, ��� � ����������   
   ArraySetAsSeries(TrendDown,true);

//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(2,SignUp,INDICATOR_DATA);
//---- ������������� ������ ���������� 1 �� ����������� �� Shift
   PlotIndexSetInteger(2,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ���������� 1
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,min_rates_total);
//---- ���������� ��������� � �������, ��� � ����������   
   ArraySetAsSeries(SignUp,true);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,0.0);
//---- ������ ��� ����������
   PlotIndexSetInteger(2,PLOT_ARROW,108);

//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(3,SignDown,INDICATOR_DATA);
//---- ������������� ������ ���������� 2 �� ����������� �� Shift
   PlotIndexSetInteger(3,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ���������� 2
   PlotIndexSetInteger(3,PLOT_DRAW_BEGIN,min_rates_total);
//---- ���������� ��������� � �������, ��� � ����������   
   ArraySetAsSeries(SignDown,true);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(3,PLOT_EMPTY_VALUE,0.0);
//---- ������ ��� ����������
   PlotIndexSetInteger(3,PLOT_ARROW,108);
//----
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,    // ���������� ������� � ����� �� ������� ����
                const int prev_calculated,// ���������� ������� � ����� �� ���������� ����
                const datetime &time[],
                const double &open[],
                const double& high[],     // ������� ������ ���������� ���� ��� ������� ����������
                const double& low[],      // ������� ������ ��������� ���� ��� ������� ����������
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
  {
//---- �������� ���������� ����� �� ������������� ��� �������
   if(rates_total<min_rates_total) return(0);

   int limit,bar;

//---- ���������� ��������� � ��������, ��� � ����������  
   ArraySetAsSeries(low,true);
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(close,true);
   ArraySetAsSeries(time,true);

//---- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
     {
      limit=rates_total-min_rates_total-1;               // ��������� ����� ��� ������� ���� �����

      int tms1=rates_total-2-min_rates_total;
      while(!isDelimeter(Period(),time,tms1)) tms1--;
      boolp1=tms1;
      tms1--;
      for(int rrr=0; rrr<int(iPeriod); rrr++)
        {
         while(!isDelimeter(Period(),time,tms1)) tms1--;
         tms1--;
        }
      tms1++;
      limit=tms1;
     }
   else
     {
      limit=rates_total-prev_calculated;                 // ��������� ����� ��� ������� ����� �����
     }

//---- �������� ���� ������� ����������
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      TrendUp[bar]=0.0;
      TrendDown[bar]=0.0;
      SignUp[bar]=0.0;
      SignDown[bar]=0.0;
      double HH=AverageHigh(high,time,bar);
      double LL=AverageLow(low,time,bar);

      if(close[bar]>HH) TrendUp[bar]=LL;
      else
        {
         if(close[bar]<LL) TrendDown[bar]=HH;
         else
           {
            if(TrendDown[bar+1]) TrendDown[bar]=HH;
            if(TrendUp[bar+1]) TrendUp[bar]=LL;
           }
        }

      if(!TrendUp[bar+1] && TrendUp[bar]) SignUp[bar]=TrendUp[bar];
      if(!TrendDown[bar+1] && TrendDown[bar]) SignDown[bar]=TrendDown[bar];
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
//| AverageHigh                                                      |
//+------------------------------------------------------------------+
double AverageHigh(const double &High[],const datetime &Time[],int index)
  {
//----
   double hhv;
   double ret= 0.0;
   int nbars = index;
   int max=int(iPeriod+iFullPeriods);
   boolp1=false;
   for(int iii=0; iii<max; iii++)
     {
      while(!isDelimeter(Period(),Time,nbars)) nbars++;
      if(!boolp1) boolp1=nbars;
      arr[iii]=nbars;
      nbars++;
     }

   for(int count=int(iPeriod-1); count>0; count--)
     {
      hhv=High[ArrayMaximum(High,arr[count-1]+1,arr[count]-arr[count-1])];
      ret+=hhv;
     }
   if(iFullPeriods==1)
     {
      hhv=High[ArrayMaximum(High,arr[iPeriod-1]+1,arr[iPeriod]-arr[iPeriod-1])];
      ret += hhv;
      ret /= NormalizeDouble(iPeriod, 0);
     }
   else
     {
      hhv=High[ArrayMaximum(High,index,arr[0]-index)];
      ret += hhv;
      ret /= NormalizeDouble(iPeriod, 0);
     }
//----
   return (ret);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double AverageLow(const double &Low[],const datetime &Time[],int index)
  {
//----
   double llv;
   double ret= 0.0;
   int nbars = index;
   int max=int(iPeriod+iFullPeriods);
   boolp1=false;
   for(int iii=0; iii<max; iii++)
     {
      while(!isDelimeter(Period(),Time,nbars)) nbars++;
      if(!boolp1) boolp1=nbars;
      arr[iii]=nbars;
      nbars++;
     }
   for(int count=int(iPeriod-1); count>0; count--)
     {
      llv=Low[ArrayMinimum(Low,arr[count-1]+1,arr[count]-arr[count-1])];
      ret+=llv;
     }
   if(iFullPeriods==1)
     {
      llv=Low[ArrayMinimum(Low,arr[iPeriod-1]+1,arr[iPeriod]-arr[iPeriod-1])];
      ret += llv;
      ret /= NormalizeDouble(iPeriod, 0);
     }
   else
     {
      llv=Low[ArrayMinimum(Low,index,arr[0]-index)];
      ret += llv;
      ret /= NormalizeDouble(iPeriod, 0);
     }
//----
   return (ret);
  }
//+------------------------------------------------------------------+
//| isDelimeter()                                                    |
//+------------------------------------------------------------------+
bool isDelimeter(ENUM_TIMEFRAMES TimFrame,const datetime &Time[],int index)
  {
//----
   MqlDateTime tm;
   TimeToStruct(Time[index],tm);
   bool blper=false;
   switch(TimFrame)
     {
      case PERIOD_M1: blper=tm.min==0; break;
      case PERIOD_M2: blper=tm.min==0; break;
      case PERIOD_M3: blper=tm.min==0; break;
      case PERIOD_M4: blper=tm.min==0; break;
      case PERIOD_M5: blper=tm.min==0; break;
      case PERIOD_M6: blper=tm.min==0; break;
      case PERIOD_M10: blper=tm.min==0; break;
      case PERIOD_M12: blper=tm.min==0; break;
      case PERIOD_M15: blper=tm.min==0; break;
      case PERIOD_M20: blper=tm.min==0; break;
      case PERIOD_M30: blper=tm.min==0 && MathMod(tm.hour,4.0)==0.0; break;
      case PERIOD_H1: blper=tm.min==0 && MathMod(tm.hour,4.0)==0.0; break;
      case PERIOD_H2: blper=tm.min==0 && MathMod(tm.hour,4.0)==0.0; break;
      case PERIOD_H3: blper=tm.min==0 && MathMod(tm.hour,4.0)==0.0; break;
      case PERIOD_H4: blper=tm.min==0 && tm.hour==0; break;
      case PERIOD_H6: blper=tm.min==0 && tm.hour==0; break;
      case PERIOD_H8: blper=tm.min==0 && tm.hour==0; break;
      case PERIOD_H12: blper=tm.min==0 && tm.hour==0; break;
      case PERIOD_D1: blper=tm.day_of_week==1 && tm.hour==0; break;
      case PERIOD_W1:
        {
         MqlDateTime tm2;
         TimeToStruct(Time[index+1],tm2);
         blper=tm.day==1 || (tm.day==2 && tm2.day!=1) || (tm.day==3 && tm2.day!=2);
         break;
        }
      case PERIOD_MN1:
        {
         MqlDateTime tm2;
         TimeToStruct(Time[index+1],tm2);
         blper=tm.day==1 || (tm.day==2 && tm2.day!=1) || (tm.day==3 && tm2.day!=2);
         break;
        }
     }
//----
   return (blper);
  }
//+------------------------------------------------------------------+
