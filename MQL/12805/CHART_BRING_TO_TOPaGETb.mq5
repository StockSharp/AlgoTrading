//+------------------------------------------------------------------+
//|                                      CHART_BRING_TO_TOP(GET).mq5 |
//|                              Copyright � 2015, Vladimir Karputov |
//|                                           http://wmua.ru/slesar/ |
//+------------------------------------------------------------------+
#property copyright "Copyright � 2015, Vladimir Karputov"
#property link      "http://wmua.ru/slesar/"
#property version   "1.01"
#property description "Determine the active chart"
//+------------------------------------------------------------------+
//| Determine the active chart                                       |
//| ���������� �������� ������                                       |
//+------------------------------------------------------------------+
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
   EventSetTimer(3);
//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OnTimer()
  {
//Print(__FUNCTION__);
//--- variables for chart ID
//--- ���������� ��� ��������������� ��������
   long currChart,prevChart=ChartFirst();
   bool var=false;
   int i=0,limit=100;
   while(i<limit)
      // We have certainly not more than 100 open charts
      // � ��� ��������� �� ������ 100 �������� ��������
     {
      var=ChartGetInteger(prevChart,CHART_BRING_TO_TOP,0); // Get property CHART_BRING_TO_TOP
                                                           // �������� �������� CHART_BRING_TO_TOP
      if(var) // This vhart active? // ���� ������ ��������?
        {
         string name=ChartSymbol(prevChart);
         string text="Chart "+name+" is active!";
         Print(text);
        }
      currChart=ChartNext(prevChart);  // Get the new chart ID by using the previous chart ID
                                       // �� ��������� ����������� ������� ����� ������
      if(currChart<0) break;           // Have reached the end of the chart list
                                       // �������� ����� ������ ��������
      prevChart=currChart;             // let's save the current chart ID for the ChartNext()
                                       // �������� ������������� �������� ������� ��� ChartNext()
      i++;                             // Do not forget to increase the counter
                                       // �� ������� ��������� �������
     }
  }
//+------------------------------------------------------------------+
