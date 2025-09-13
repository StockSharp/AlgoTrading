//+------------------------------------------------------------------+
//|                                            SocketIsConnected.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property description "Connects to a server via sockets and shows socket state."
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
      int i = 0;
      while(PRTF(SocketIsConnected(socket)) && !IsStopped())
      {
         PRTF(SocketIsReadable(socket));
         PRTF(SocketIsWritable(socket));
         Sleep(1000);
         if(++i >= 2)
         {
            PRTF(SocketClose(socket));
         }
      }
   }
}

//+------------------------------------------------------------------+
/*
   Example:
   
   Server=www.mql5.com / ok
   Port=443 / ok
   SocketCreate()=1 / ok
   SocketConnect(socket,Server,Port,5000)=true / ok
   SocketIsConnected(socket)=true / ok
   SocketIsReadable(socket)=0 / ok
   SocketIsWritable(socket)=true / ok
   SocketIsConnected(socket)=true / ok
   SocketIsReadable(socket)=0 / ok
   SocketIsWritable(socket)=true / ok
   SocketClose(socket)=true / ok
   SocketIsConnected(socket)=false / NETSOCKET_INVALIDHANDLE(5270)
   
*/
//+------------------------------------------------------------------+
