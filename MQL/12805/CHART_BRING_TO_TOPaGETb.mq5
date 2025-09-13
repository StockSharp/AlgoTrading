//+------------------------------------------------------------------+
//|                                      CHART_BRING_TO_TOP(GET).mq5 |
//|                              Copyright © 2015, Vladimir Karputov |
//|                                           http://wmua.ru/slesar/ |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2015, Vladimir Karputov"
#property link      "http://wmua.ru/slesar/"
#property version   "1.01"
#property description "Determine the active chart"
//+------------------------------------------------------------------+
//| Determine the active chart                                       |
//| Определяем активный график                                       |
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
//--- переменные для идентификаторов графиков
   long currChart,prevChart=ChartFirst();
   bool var=false;
   int i=0,limit=100;
   while(i<limit)
      // We have certainly not more than 100 open charts
      // у нас наверняка не больше 100 открытых графиков
     {
      var=ChartGetInteger(prevChart,CHART_BRING_TO_TOP,0); // Get property CHART_BRING_TO_TOP
                                                           // получаем свойство CHART_BRING_TO_TOP
      if(var) // This vhart active? // Этот график активный?
        {
         string name=ChartSymbol(prevChart);
         string text="Chart "+name+" is active!";
         Print(text);
        }
      currChart=ChartNext(prevChart);  // Get the new chart ID by using the previous chart ID
                                       // на основании предыдущего получим новый график
      if(currChart<0) break;           // Have reached the end of the chart list
                                       // достигли конца списка графиков
      prevChart=currChart;             // let's save the current chart ID for the ChartNext()
                                       // запомним идентификатор текущего графика для ChartNext()
      i++;                             // Do not forget to increase the counter
                                       // не забудем увеличить счетчик
     }
  }
//+------------------------------------------------------------------+
