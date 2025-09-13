//+------------------------------------------------------------------+
//|                                                          BUY.mq4 |
//|                      Copyright © 2008, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+

#include <WinUser32.mqh>



#import "c:\\ChartPlusChart\\SharedVarsDLLv2.dll"

 double   GetFloat@4 (int N);
 int   GetInt@4 (int N);
 //   Init@0 
 void   SetFloat@12 (int N, double Val);
 void    SetInt@8 (int N, int Val);

#import




int start()
  {
//D2 reading example:
D2=GetFloat@4 (71);

//control D2 and do smth. 

  }

