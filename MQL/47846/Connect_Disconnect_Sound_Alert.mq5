//+------------------------------------------------------------------+
//|                               Connect_Disconnect_Sound_Alert.mq5 |
//|                                Copyright 2024, Rajesh Kumar Nait |
//|                  https://www.mql5.com/en/users/rajeshnait/seller |
//+------------------------------------------------------------------+
#property copyright "Copyright 2024, Rajesh Kumar Nait"
#property link      "https://www.mql5.com/en/users/rajeshnait/seller"
#property version   "1.00"
#include <Trade/TerminalInfo.mqh>

bool     first             = true;
bool     Now_IsConnected   = false;
bool     Pre_IsConnected   = true;
datetime Connect_Start = 0, Connect_Stop = 0;

CTerminalInfo terminalInfo;
//--- Sound files
//#resource "\\Files\\Sounds\\CONNECTED.wav"
//#resource "\\Files\\Sounds\\DISCONNECTED.wav"
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
      ResetLastError();
      while ( !IsStopped() ) {
         Pre_IsConnected = Now_IsConnected;
         Now_IsConnected = terminalInfo.IsConnected();

         if ( first ) {
            Pre_IsConnected = !Now_IsConnected;
         }

         if ( Now_IsConnected != Pre_IsConnected ) {
            if ( Now_IsConnected ) {
               Connect_Start = TimeLocal();
               if ( !first ) {
                 // if(!PlaySound("::Files\\Sounds\\DISCONNECTED.wav"))
                 //    Print("Error: ",GetLastError());
               }
               if ( IsStopped() ) {
                  break;
               }
              // if(!PlaySound("::Files\\Sounds\\CONNECTED.wav"))
               //   Print("Error: ",GetLastError());
            } else {
               Connect_Stop = TimeLocal();
               if ( !first ) {
               //   if(!PlaySound("::Files\\Sounds\\CONNECTED.wav"))
               //      Print("Error: ",GetLastError());
               }
               if ( IsStopped() ) {
                  break;
               }
              // if(!PlaySound("::Files\\Sounds\\DISCONNECTED.wav"))
               //   Print("Error: ",GetLastError());
            }
         }

         first = false;
         Sleep(1000);
      }
//---
   return(INIT_SUCCEEDED);
  }

//+------------------------------------------------------------------+
