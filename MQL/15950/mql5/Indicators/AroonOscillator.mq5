//+------------------------------------------------------------------+
//|                                              AroonOscillator.mq5 |
//|                             Copyright � 2011,   Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
//---- ��������� ����������
#property copyright "Copyright � 2011, Nikolay Kositsin"
//---- ������ �� ���� ������
#property link "farria@mail.redcom.ru"
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � ��������� ����
#property indicator_separate_window
//---- ���������� ������������ ������� 1
#property indicator_buffers 1 
//---- ������������ ����� ���� ����������� ����������
#property indicator_plots   1
//+-----------------------------------+
//|  ��������� ��������� ����������   |
//+-----------------------------------+
//---- ��������� ���������� � ���� ����������� �����
#property indicator_type1 DRAW_LINE
//---- � �������� ����� ����� ����������� ������� ����
#property indicator_color1 clrRed
//---- ����� ���������� - ��������
#property indicator_style1 STYLE_SOLID
//---- ������� ����� ���������� ����� 2
#property indicator_width1 2
//---- ����������� ����� ���������� �����
#property indicator_label1  "AroonOscillator"
//+----------------------------------------------+
//| ��������� ����������� �������������� ������� |
//+----------------------------------------------+
#property indicator_level1 +50
#property indicator_level2   0
#property indicator_level3 -50
#property indicator_levelcolor clrGray
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input int AroonPeriod= 9; // ������ ���������� 
input int AroonShift = 0; // ����� ���������� �� ����������� � ����� 
//+----------------------------------------------+
//---- ���������� ������������ ��������, ������� ����� � 
// ���������� ������������ � �������� ������������ �������
double ExtLineBuffer[];
//+------------------------------------------------------------------+
//|  searching index of the highest bar                              |
//+------------------------------------------------------------------+
int iHighest(
             const double &array[],// ������ ��� ������ ������� ������������� ��������
             int count,// ����� ��������� ������� (� ����������� �� �������� ���� � ������� �������� �������), 
             // ����� ������� ������ ���� ���������� �����.
             int startPos //������ (�������� ������������ �������� ����) ���������� ����, 
             // � �������� ���������� ����� ����������� ��������
             )
  {
//----
   int index=startPos;

//---- �������� ���������� ������� �� ������������
   if(startPos<0)
     {
      Print("�������� �������� � ������� iHighest, startPos = ",startPos);
      return(0);
     }

//---- �������� �������� startPos �� ������������
   if(startPos-count<0)
      count=startPos;

   double max=array[startPos];

//---- ����� �������
   for(int i=startPos; i>startPos-count; i--)
     {
      if(array[i]>max)
        {
         index=i;
         max=array[i];
        }
     }
//---- ������� ������� ����������� ����
   return(index);
  }
//+------------------------------------------------------------------+
//|  searching index of the lowest bar                               |
//+------------------------------------------------------------------+
int iLowest(
            const double &array[],// ������ ��� ������ ������� ������������ ��������
            int count,// ����� ��������� ������� (� ����������� �� �������� ���� � ������� �������� �������), 
            // ����� ������� ������ ���� ���������� �����.
            int startPos //������ (�������� ������������ �������� ����) ���������� ����, 
            // � �������� ���������� ����� ����������� ��������
            )
  {
//----
   int index=startPos;

//---- �������� ���������� ������� �� ������������
   if(startPos<0)
     {
      Print("�������� �������� � ������� iLowest, startPos = ",startPos);
      return(0);
     }

//---- �������� �������� startPos �� ������������
   if(startPos-count<0)
      count=startPos;

   double min=array[startPos];

//---- ����� �������
   for(int i=startPos; i>startPos-count; i--)
     {
      if(array[i]<min)
        {
         index=i;
         min=array[i];
        }
     }
//---- ������� ������� ����������� ����
   return(index);
  }
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- ����������� ������������� ������� ExtLineBuffer � ������������ �����
   SetIndexBuffer(0,ExtLineBuffer,INDICATOR_DATA);
//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   StringConcatenate(shortname,"AroonOscillator(",AroonPeriod,")");
//---- ������������� ������ ���������� 1 �� ����������� �� AroonShift
   PlotIndexSetInteger(0,PLOT_SHIFT,AroonShift);
//---- �������� ����� ��� ����������� � ���� ������
   PlotIndexSetString(0,PLOT_LABEL,shortname);
//---- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//---- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//---- ������ �� ��������� ����������� ������ ��������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//----
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
   if(rates_total<AroonPeriod) return(0);

//---- ���������� ��������� ���������� 
   int first,bar,highest,lowest;

//---- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
      first=AroonPeriod-1; // ��������� ����� ��� ������� ���� �����
   else first=prev_calculated-1; // ��������� ����� ��� ������� ����� �����

//---- �������� ���� ������� ����������
   for(bar=first; bar<rates_total; bar++)
     {
      highest=iHighest(high,AroonPeriod,bar);
      lowest=iLowest(low,AroonPeriod,bar);
      //----
      ExtLineBuffer[bar]=100*(highest-lowest)/AroonPeriod;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
