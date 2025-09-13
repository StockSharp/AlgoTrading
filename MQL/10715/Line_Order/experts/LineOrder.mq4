 //+------------------------------------------------------------------+
//|                                                    Line Test.mq4 |
//|                                                       heelflip43 |
//|                                                                  |
//+------------------------------------------------------------------+
#property copyright "heelflip43"
#property link      "http://forum.mql.com/users/heelflip43"
#include <LineOrderLibrary 2.mqh>
//+------------------------------------------------------------------+
//| script program start function                                    |
//+------------------------------------------------------------------+
int init()
{
startInit();
}
int start()
  {
//----
if(IsTesting()||IsOptimization()){
processLines();
}else{

Print("Expert starting");
   while(!IsStopped()){
   int tickcount = GetTickCount();
   processLines();
   tickcount = GetTickCount()-tickcount;
   tickcount = 1000-tickcount;
   if(tickcount<10)tickcount = 10;
   //Print(tickcount+" "+GetTickCount());
   Sleep(tickcount);
   }

   Print("Expert ending");
   }
//----
   return(0);
  }
//+------------------------------------------------------------------+

int deinit()
{
   deinitVar();
   if(IsTesting()||IsOptimization()){
   for(int i=1;i<=OrdersHistoryTotal();i++){
   OrderSelect(i,SELECT_BY_POS,MODE_HISTORY);int handle = FileOpen("MLO"+OrderTicket(),FILE_READ);
   if(handle!=-1){
   FileClose(handle);FileDelete("MLO"+OrderTicket()+".txt");
   }}
   cleanUpGlobal();   
   }
}