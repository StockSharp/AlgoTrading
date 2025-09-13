//+------------------------------------------------------------------+
//|                                                  IsConnected.mq5 |
//|                                       Copyright 2021, Dark Ryd3r |
//|                                    https://twitter.com/DarkRyd3r |
//+------------------------------------------------------------------+
#property copyright "Copyright 2021, Dark Ryd3r"
#property link      "https://twitter.com/DarkRyd3r"
#property version   "1.00"
#include <Trade/TerminalInfo.mqh>

bool     first             = true;
bool     Now_IsConnected   = false;
bool     Pre_IsConnected   = true;
datetime Connect_Start = 0, Connect_Stop = 0;

CTerminalInfo terminalInfo;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int start()
{  

   ResetLastError(); 
   
	Print("Expert initialized");

	while ( !IsStopped() )
	{
		Pre_IsConnected = Now_IsConnected;
		Now_IsConnected = terminalInfo.IsConnected();
		
		if ( first ) { Pre_IsConnected = !Now_IsConnected; }
		
		if ( Now_IsConnected != Pre_IsConnected )
		{
			if ( Now_IsConnected )
			{
				Connect_Start = TimeLocal();
				if ( !first )
				{
					Print("Offline");
				}
				if ( IsStopped() ) { break; }
				Print("Online");
				}
			else
			{
				Connect_Stop = TimeLocal();
				if ( !first )
				{  Print("Online");
			   }
				if ( IsStopped() ) { break; }
				Print("Offline");
				}		
		}

		first = false;
		Sleep(1000);
	}

	if ( Now_IsConnected )
	{  Print("Online");
	}
	else
	{  Print("Offline");
		}		
	Print("IsConnected Expert Removed");
	return(0);
}

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit() {
//---
   start();

//---
   return(INIT_SUCCEEDED);
}
//+------------------------------------------------------------------+
