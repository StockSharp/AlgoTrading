//+------------------------------------------------------------------+
//|                                                SocketConnect.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property description "Connects to a server via sockets."
#property description "NB: Default 'Server' requires to allow 'www.mql5.com' in terminal settings - to use other servers, change the settings accordingly."
#property script_show_inputs

#include <MQL5Book/PRTF.mqh>

input string Server = "www.mql5.com";
input uint Port = 443;

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   PRTF(Server);
   PRTF(Port);
   const int socket = PRTF(SocketCreate());
   if(PRTF(SocketConnect(socket, Server, Port, 5000)))
   {
      PRTF(SocketClose(socket));
   }
}

//+------------------------------------------------------------------+
/*
   Example:
   
   Server=www.mql5.com / ok
   Port=443 / ok
   SocketCreate()=1 / ok
   SocketConnect(socket,Server,Port,5000)=true / ok
   SocketClose(socket)=true / ok
*/
//+------------------------------------------------------------------+
