//+------------------------------------------------------------------+
//|                                          SocketReadWriteHTTP.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property description "Requests a header (meta-data) of a web-page (insecure HTTP)."
#property description "NB: Default 'Server' requires to allow 'www.mql5.com' in terminal settings - to use other servers, change the settings accordingly."
#property script_show_inputs

#include <MQL5Book/PRTF.mqh>

input string Server = "www.mql5.com";
input uint Port = 80;

//+------------------------------------------------------------------+
//| Send HTTP-request via socket (insecure connection)               |
//+------------------------------------------------------------------+
bool HTTPSend(int socket, const string request)
{ 
   char req[];
   int len = StringToCharArray(request, req, 0, WHOLE_ARRAY, CP_UTF8) - 1;
   if(len < 0) return false;
   return SocketSend(socket, req, len) == len;
} 

//+------------------------------------------------------------------+
//| Receive HTTP-response via socket (insecure connection)           |
//+------------------------------------------------------------------+
bool HTTPRecv(int socket, string &result, const uint timeout)
{ 
   uchar response[];
   int len;          // must be signed int to keep -1 in case of error
   uint start = GetTickCount();
   result = "";

   do 
   {
      ResetLastError();
      if(!(len = (int)SocketIsReadable(socket)))
      {
         Sleep(10); // wait for data or timeout
      }
      else
      if((len = SocketRead(socket, response, len, 10)) > 0)
      {
         result += CharArrayToString(response, 0, len); // NB: no CP_UTF8 because of 'HEAD'er
         const int p = StringFind(result, "\r\n\r\n");
         if(p > 0)
         {
            // HTTP header ends with double "new line", so using this parsing
            // we make sure entire header received
            Print("HTTP-header found");
            StringSetLength(result, p); // cut off document body
            return true;
         }
         start = GetTickCount();
      }
   } 
   while(GetTickCount() - start < timeout && !IsStopped() && !_LastError);
   
   if(_LastError) PRTF(_LastError);
      
   return StringLen(result) > 0;
}

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   PRTF(Server);
   PRTF(Port);
   const int socket = PRTF(SocketCreate());
   if(socket == INVALID_HANDLE) return;
   if(PRTF(SocketConnect(socket, Server, Port, 5000)))
   {
      if(PRTF(HTTPSend(socket, StringFormat(
         "HEAD / HTTP/1.1\r\nHost: %s\r\nUser-Agent: MetaTrader 5\r\n\r\n",
         Server))))
      {
         string response;
         if(PRTF(HTTPRecv(socket, response, 5000)))
         {
            // may be usefull to track 'Content-Length:',
            // 'Content-Language:', 'Last-Modified:'
            // and other attributes in the header
            Print(response);
         }
      }
   }
   SocketClose(socket);
}

//+------------------------------------------------------------------+
/*
   Example:
   
   Server=www.mql5.com / ok
   Port=80 / ok
   SocketCreate()=1 / ok
   SocketConnect(socket,Server,Port,5000)=true / ok
   HTTPSend(socket,StringFormat(HEAD / HTTP/1.1
   Host: %s
   
   ,Server))=true / ok
   HTTP-header found
   HTTPRecv(socket,response,5000)=true / ok
   HTTP/1.1 301 Moved Permanently
   Server: nginx
   Date: Sun, 31 Jul 2022 10:24:00 GMT
   Content-Type: text/html
   Content-Length: 162
   Connection: keep-alive
   Location: https://www.mql5.com/
   Strict-Transport-Security: max-age=31536000; includeSubDomains; preload
   X-Frame-Options: SAMEORIGIN
   
*/
//+------------------------------------------------------------------+
