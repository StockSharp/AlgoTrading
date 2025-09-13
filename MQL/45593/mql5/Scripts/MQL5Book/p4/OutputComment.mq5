//+------------------------------------------------------------------+
//|                                                OutputComment.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include <MQL5Book/Comments.mqh>

// You can define custom comments feed
//    Comments c(30/*capacity*/, true/*order*/);
// then use it via methods:
//    c.add("123");
//    c.clear();

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   // simulate text data generation in the lopp
   for(int i = 0; i < 50 && !IsStopped(); ++i)
   {
      // every 10-th item clear the comment
      if((i + 1) % 10 == 0) MultiComment();
      // add a numbered item, every 3-rd comprising 2 lines
      MultiComment("Line " + (string)i + ((i % 3 == 0) ? "\n  (details)" : ""));
      // keep a decent pace to let user see the show
      Sleep(1000);
   }
   // final cleanup
   MultiComment();
}
//+------------------------------------------------------------------+
