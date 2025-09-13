#property copyright   "Copyright 2019, Pelanjie Mafuta"
#property link        "https://www.mql5.com/en/users/lpelanjie"
#property description "This utility allows you to automatically enable or disable the automated trading button ."
#property description "For assistance Email : info@forexlimerence.com Telegram : https://t.me/forexlimerence"
#property version     "1.00"
#property strict

#include <WinUser32.mqh>
//#import "user32.dll"  // Uncomment This
 
int GetAncestor(int, int);
#define MT4_WMCMD_EXPERTS  33020 

#import
//extern bool Run=true;

input int            Start_Time     = 1;    //Start AutoTrade Time      
input int            Finish_Time    = 8;    //Stop AutoTrade Time 

int            start_Time;          
int            finish_Time;



//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
   start_Time     = Start_Time;          
   finish_Time    = Finish_Time;

//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---
   
  } 
   

//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
{
//---
  //int main = GetAncestor(WindowHandle(Symbol(), Period()), 2/*GA_ROOT*/);   // Uncomment This

  int Current_Time = TimeHour(TimeCurrent());
     
   if (start_Time == Current_Time)
   {
   
     if(!TerminalInfoInteger(TERMINAL_TRADE_ALLOWED))
     {
       //PostMessageA(main, WM_COMMAND,  MT4_WMCMD_EXPERTS, 0 ) ;  // Uncomment This        
     }
   }
   
   
   if (finish_Time == Current_Time)
   {
      if(TerminalInfoInteger(TERMINAL_TRADE_ALLOWED))
      {
        //PostMessageA(main, WM_COMMAND,  MT4_WMCMD_EXPERTS, 0 ) ;   // Uncomment This         
      }
        
   } 
   
   if(OrdersTotal()<1)                                                  // Remove this lines
   OrderSend(Symbol(),OP_SELL,1,Bid, 3,0,0,"Test ",111111,0,White);  // Remove this lines
        
}
//+------------------------------------------------------------------+


   
 

 