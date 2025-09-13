//+------------------------------------------------------------------+
//|                                      Example1_LibCustomChart.mq5 |
//|                                            Copyright 2012, Lizar |
//|                            https://login.mql5.com/en/users/Lizar |
//+------------------------------------------------------------------+
#property copyright     "Copyright 2012, Lizar"
#property link          "https://login.mql5.com/en/users/Lizar"
#property version       "1.00"
#property description   "The Expert Advisor shows the application of LibCustomChart.ex5 library."
#property description   "The presence of iCustomChart custom chart is checked when attaching the Expert Advisor to a chart window."
#property description   "In case a custom chart has been uploaded, the library functions use the data of that chart. Otherwise, the data of a standard chart is used."
#property description   "Therefore, the Expert Advisor can work both with custom and standard charts without the need for any changes."

/*------------------------------------------------------------------+/
Instructions:
1. Download LibCustomChart.ex5 library file and place it to the
   terminal_data_folder\MQL5\Libraries
2. Download the file containing the library functions description LibCustomChart.mqh and 
   place it to the terminal_data_folder\MQL5\Include
3. Download Example1_LibCustomChart.mq5 file and place it to the 
   terminal_data_folder\MQL5\Experts
4. Download Example1_LibCustomChart.mq5 in MetaEditor and compile 
   (F7)
5. Example1_LibCustomChart.ex5 Expert Advisor can be launched both on a standard 
   and custom chart created with the use of 
   iCustomChart.
6. The following steps must be executed to upload the Expert Advisor on a custom chart:
   - download demo or full version of iCustomChart and 
     attach it to any chart window;
   - then attach Example1_LibCustomChart to the same window, 
     will automatically detect the presence of iCustomChart and 
     get calculation data from it. 
Links:
1. LibCustomChart library:
   http://www.mql5.com/en/market/product/196
2. Included file containing the library functions description (LibCustomChart.mqh)
   http://www.mql5.com/en/code/870
3. iCustomChart demo version:
   http://www.mql5.com/en/market/product/203
4. iCustomChart full version:
   http://www.mql5.com/en/market/product/186
/+------------------------------------------------------------------*/
//--- the file containing the LibCustomChart library functions description:
#include <LibCustomChart.mqh>
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//--- attaching the Expert Advisor to a custom chart, in case it has been uploaded:
   return(!CustomChartInit());
  }

//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//--- updating custom chart data: 
   if(!CustomChartRefresh()) return;
//--- getting Close price of the last unfinished bar of a custom chart:
   double custom_price[1]; // массив с ценами Close
   if(CopyClose(0,1,custom_price)!=1) return;
//--- printing the result:
   PrintFormat("Close=%8.5f",custom_price[0]);
//--- new bar alert:
   if(CustomChartNewBar()) Print("New bar");
  }

//+------------------------------------------------------------------+
