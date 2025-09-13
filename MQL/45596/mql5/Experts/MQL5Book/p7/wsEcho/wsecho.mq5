//+------------------------------------------------------------------+
//|                                                       wsecho.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property script_show_inputs

//+------------------------------------------------------------------+
//| I N P U T S                                                      |
//| Choose 'wss:' protocol for secured TLS connection,               |
//| if the server is setup for it                                    |
//+------------------------------------------------------------------+
input string Server = "ws://localhost:9000/";
input string Message = "My outbound message";

#include <MQL5Book/AutoPtr.mqh>
#include <MQL5Book/ws/wsclient.mqh>

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   Print("\n");
   WebSocketClient<Hybi> wss(Server);
   Print("Opening...");
   if(wss.open())
   {
      Print("Waiting for welcome message (if any)");
      AutoPtr<IWebSocketMessage> welcome(wss.readMessage());

      Print("Sending message...");
      wss.send(Message);
      Print("Receiving echo...");
      // waiting for new messages in 'blocking' mode (default timeout 5 seconds)
      AutoPtr<IWebSocketMessage> echo(wss.readMessage());
   }

   if(wss.isConnected())
   {
      Print("Closing...");
      wss.close();
   }
}
//+------------------------------------------------------------------+
