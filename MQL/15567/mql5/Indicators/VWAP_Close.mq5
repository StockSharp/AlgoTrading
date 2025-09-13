//+------------------------------------------------------------------+
//|                                                   VWAP_Close.mq5 |
//|                                            Copyright � 2016, STS | 
//|                                                                  | 
//+------------------------------------------------------------------+ 
#property copyright "Copyright � 2016, STS"
#property link ""
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � ������� ����
#property indicator_chart_window 
//---- ��� ������� � ��������� ���������� ����������� ���� �����
#property indicator_buffers 1
//---- ������������ ���� ����������� ����������
#property indicator_plots   1
//+----------------------------------------------+
//|  ��������� ��������� ����������              |
//+----------------------------------------------+
//---- ��������� ���������� 1 � ���� �����
#property indicator_type1   DRAW_LINE
//---- � �������� ����� ����� ����� ���������� ����������� DarkOrchid ����
#property indicator_color1  clrDarkOrchid
//---- ����� ���������� 1 - ����������� ������
#property indicator_style1  STYLE_SOLID
//---- ������� ����� ���������� 1 ����� 3
#property indicator_width1  3
//---- ����������� ����� ����������
#property indicator_label1  "VWAP_Close"
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input uint n=2;
input ENUM_APPLIED_VOLUME VolumeType=VOLUME_TICK;  // �����
input int Shift=0; //����� ���������� �� ����������� � �����
//+----------------------------------------------+
//---- ���������� ������������ ��������, ������� ����� � 
// ���������� ������������ � �������� ������������ �������
double IndBuffer[];
//---- ���������� ����� ���������� ������ ������� ������
int min_rates_total,size;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   min_rates_total=int(n)+1;
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//---- ������������� ������ ���������� 1 �� ����������� �� Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ���������� 1 �� min_rates_total
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(IndBuffer,true);
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,"VWAP_Close");
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
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
   if(rates_total<min_rates_total) return(0);

//---- ���������� ��������� ���������� 
   long vol,sum2;
   int limit,bar;
   double sum1;

//---- ������ ���������� ������ limit ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
      limit=rates_total-min_rates_total-1; // ��������� ����� ��� ������� ���� �����
   else limit=rates_total-prev_calculated; // ��������� ����� ��� ������� ����� �����

//---- ���������� ��������� � �������� ��� � ����������  
   if(VolumeType==VOLUME_TICK) ArraySetAsSeries(tick_volume,true);
   else ArraySetAsSeries(volume,true);
   ArraySetAsSeries(close,true);

//---- �������� ���� ������� ����������
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      sum1=0;
      sum2=0;
      for(int ntmp=0; ntmp<int(n); ntmp++)
        {
         if(VolumeType==VOLUME_TICK) vol=long(tick_volume[bar+ntmp]);
         else vol=long(volume[bar+ntmp]);
         sum1+=close[bar+ntmp]*vol;
         sum2+=vol;
        }

      if(sum2) IndBuffer[bar]=sum1/sum2;
      else IndBuffer[bar]=IndBuffer[bar+1];
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
