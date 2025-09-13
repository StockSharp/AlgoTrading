//+------------------------------------------------------------------+
//|                                                  Timer Trade.mq5 |
//|                                                        Oschenker |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Oschenker"
#property link      "https://www.mql5.com"
#property version   "1.00"
//--- ������� ���������
input int      TimerTime=30; //�������� ������� (���).
input int      TradeValue = 1; // ������� ���
input int      StopLossLevel = 10; // Stop Loss (points)
input int      TakeProfitLevel = 50; //�������� Take Profit (points)

//--- ��������� ����������� �������� ����������
#include <Trade\Trade.mqh>;

CTrade  trade;

int Trigger = 0;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//--- create timer
   if(EventSetTimer(TimerTime * 100)) Print("Timer Setup to ", TimerTime, " sec.");
   else Print("Timer Error ", GetLastError());   
//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//--- destroy timer
   EventKillTimer();

  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//--- ������� �� ������������
   
  }
//+------------------------------------------------------------------+
//| Timer function                                                   |
//+------------------------------------------------------------------+
void OnTimer()
  {
//--- �������� ������ �� ������� - ������� � ����
  Print("Time to Deal");
  
//--- �������� ������ ������� ��� ������� � ����������� �� ��������� ��������
   if(Trigger == 0)
   {
      if(!trade.Buy( TradeValue, NULL, 0, SymbolInfoDouble(Symbol(), SYMBOL_BID) - Point() * StopLossLevel, SymbolInfoDouble(Symbol(), SYMBOL_ASK) + Point() * TakeProfitLevel)) 
         {
               
//--- ������� ���������               
         Print("����� Buy() �������� �������. ��� ��������=",trade.ResultRetcode(),". �������� ����: ",trade.ResultRetcodeDescription());
         }
      else
         {
///--- � ������ ������� ������� ���������� �������
         Trigger = 1;
         }
     }
    if(Trigger == 1)
         {
         if(!trade.Sell( TradeValue, NULL, 0, SymbolInfoDouble(Symbol(), SYMBOL_ASK) + Point() * StopLossLevel, SymbolInfoDouble(Symbol(), SYMBOL_BID) - Point() * TakeProfitLevel)) 
               {
//--- ������� ���������
               Print("����� Sell() �������� �������. ��� ��������=",trade.ResultRetcode(),". �������� ����: ",trade.ResultRetcodeDescription());
               }
             else
               {
///--- � ������ �������� ������� ���������� �������
         Trigger = 0;
               }
         }
    }
//+------------------------------------------------------------------+
