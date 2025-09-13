//+------------------------------------------------------------------+
//|                                                   AutoTrader.mq5 |
//|                                              Copyright 2016, AM2 |
//|                                      http://www.forexsystems.biz |
//+------------------------------------------------------------------+
#property copyright "Copyright 2016, AM2"
#property link      "http://www.forexsystems.biz"
#property version   "1.00"

#define VK_CONTROL 0x11 //CTRL key
#define KEY_CODE   'E'

// ���������� ���������� Windows ��� ������������� ������� ������
#import "user32.dll"
void  keybd_event(int bVk,int bScan,int dwFlags,int dwExtraInfo);
#import

// ���������� ����� CTrade
#include<Trade\Trade.mqh>

//������ ������ CTrade
CTrade trade;

// ������ ������ CPositionInfo
CPositionInfo position;

// ������� ����������
input int StartHour   = 9;   // ��� ������ ��������
input int StartMinute = 30;  // ������ ������ ��������
input int StopHour    = 23;  // ��� ��������� ��������
input int StopMinute  = 30;  // ������ ��������� ��������
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
   Comment("");
  }
//+------------------------------------------------------------------+
//|  �������� ������� Ctrl+E                                         |
//+------------------------------------------------------------------+
void Key()
  {
   keybd_event(VK_CONTROL,0,0,0);
   Sleep(10);
   keybd_event(KEY_CODE,0,0,0);
   Sleep(10);
   keybd_event(KEY_CODE,0,2,0);
   Sleep(10);
   keybd_event(VK_CONTROL,0,2,0);
  }
//+------------------------------------------------------------------+
//|   �������� ������� ��������                                      |
//+------------------------------------------------------------------+
bool TimeSession(int aStartHour,int aStartMinute,int aStopHour,int aStopMinute,datetime aTimeCur)
  {
//--- ����� ������ ������
   int StartTime=3600*aStartHour+60*aStartMinute;
//--- ����� ��������� ������
   int StopTime=3600*aStopHour+60*aStopMinute;
//--- ������� ����� � �������� �� ������ ���
   aTimeCur=aTimeCur%86400;
   if(StopTime<StartTime)
     {
      //--- ������� ����� �������
      if(aTimeCur>=StartTime || aTimeCur<StopTime)
        {
         return(true);
        }
     }
   else
     {
      //--- ������ ������ ���
      if(aTimeCur>=StartTime && aTimeCur<StopTime)
        {
         return(true);
        }
     }
   return(false);
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
// ��������� ��� ������� �� �������
   if(!TimeSession(StartHour,StartMinute,StopHour,StopMinute,TimeCurrent()))
     {
      for(int i=0; i<PositionsTotal(); i++)
        {
         if(position.Select(PositionGetSymbol(i)))
           {
            // ������� �������� ������� �� ����� �������
            trade.PositionClose(PositionGetSymbol(i));
           }
        }
     }

// �������� �� �������
   if(TerminalInfoInteger(TERMINAL_TRADE_ALLOWED)==0 && TimeSession(StartHour,StartMinute,StopHour,StopMinute,TimeCurrent())) Key();
// ��������� �� �������
   if(TerminalInfoInteger(TERMINAL_TRADE_ALLOWED)==1 && !TimeSession(StartHour,StartMinute,StopHour,StopMinute,TimeCurrent())) Key();

   Comment("\n Enable: ",TerminalInfoInteger(TERMINAL_TRADE_ALLOWED),
           "\n Time: ",TimeCurrent());
  }
//+------------------------------------------------------------------+
