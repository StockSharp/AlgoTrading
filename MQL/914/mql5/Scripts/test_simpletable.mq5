//+------------------------------------------------------------------+
//|                                             Test_SimpleTable.mq5 |
//|                        Copyright 2012, MetaQuotes Software Corp. |
//|                                              http://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2012, MetaQuotes Software Corp."
#property link      "http://www.mql5.com"
#property version   "1.00"
#include <SimpleTable.mqh>
#include <ColorProgressBar.mqh>
//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
  {
//--- testing the column
   CColumn column;
   column.Create(600,12,80,20);
   column.BackColor(clrIvory);
   column.BorderColor(clrGray);
   column.Color(clrBlack);
   column.TextAlign(ALIGN_RIGHT);
   column.AddItem("Net Profit:","first");
   column.AddItem("Profit Factor:","second");
   column.AddItem("Factor Recovery:","three");
   column.AddItem("Trades:","four");
   column.AddItem("Deals:","five");
   column.AddItem("Equity DD:","six");
   column.AddItem("OnTester():","seven");
   ChartRedraw();
//--- testing the progress bar
   CColorProgressBar cpb;
   cpb.Create("color progress bar",100,100,500,20,COLOR_FORMAT_XRGB_NOALPHA);
//--- setting the border colors and width
   cpb.BackColor(clrIvory);
   cpb.BorderColor(clrGray);
   cpb.BorderWidth(1);
   cpb.Update();
//--- testing the table
   CSimpleTable table;
   table.Create("table",200,130,120,80,20);
   table.BackColor(clrIvory);
   table.BorderColor(clrGray);
   table.TextColor(clrRed);
   table.AddRow("one","1");
   table.AddRow("two","2");
   table.AddRow("three","3");
//--- the pause for viewing the results on the chart
   Sleep(1000);
//--- starting filling the progress bar, changing the column and the table values
   for(int i=0;i<600;i++)
     {
      cpb.AddResult(MathRand()>32660/2);
      //--- setting the value in the column
      int index=i%7;
      column.SetValue(index,(string)i);
      //--- setting the value in the table
      int table_index=i%3;
      table.SetValue(1,table_index,(string)i);
      //--- a small pause giving us some time to see 
      Sleep(10);
     }
//--- now comes a long pause before the end of the script operation
   Sleep(15000);
  }

//+------------------------------------------------------------------+
