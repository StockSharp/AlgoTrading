//+------------------------------------------------------------------+
//|                                         SocketReadWriteHTTPS.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property description "Executes complete HTTP-request with given method, address, and port. Secure connections on port 443 are supported."
#property description "NB: Default 'Server' requires to allow 'www.google.com' in terminal settings - to use other servers, change the settings accordingly."
#property script_show_inputs

#include <MQL5Book/PRTF.mqh>

input string Method = "GET"; // Method (HEAD,GET)
input string Server = "www.google.com";
input uint Port = 443;
input uint Timeout = 5000;

//+------------------------------------------------------------------+
//| Send HTTP-request via socket (secure or insecure connection)     |
//+------------------------------------------------------------------+
bool HTTPSend(int socket, const string request, const bool TLS)
{ 
   char req[];
   int len = StringToCharArray(request, req, 0, WHOLE_ARRAY, CP_UTF8) - 1;
   if(len < 0) return false;
   return (TLS ? SocketTlsSend(socket, req, len) : SocketSend(socket, req, len)) == len;
}

//+------------------------------------------------------------------+
//| Helper converter hex string to number                            |
//+------------------------------------------------------------------+
int HexStringToInteger(string s, const int offset = 0)
{
   int result = 0;
   StringToUpper(s);
   for(int i = offset; i < StringLen(s); ++i)
   {
      int code = s[i] - '0';
      if(code > 9) code -= 'A' - ':';
      if(code < 0 || code > 15) break;     // unsupported char
      result = result * 16 + code;
   }
   return result;
}

//+------------------------------------------------------------------+
//| Pseudo-nonblocking probe and read of some data from socket       |
//+------------------------------------------------------------------+
int SocketReadAvailable(int socket, uchar &block[], const uint maxlen = INT_MAX)
{
   ArrayResize(block, 0);
   const uint len = SocketIsReadable(socket);
   if(len > 0)
      return SocketRead(socket, block, fmin(len, maxlen), 10);
   return 0;
}

//+------------------------------------------------------------------+
//| Receive web-page via socket (secure or insecure connection)      |
//+------------------------------------------------------------------+
bool HTTPRecv(int socket, string &result, const uint timeout, const bool TLS)
{
   uchar response[]; // entire data (headers + document body)
   uchar block[];    // single read block
   int len;          // current read block size (signed int to keep -1 in case of error)
   int lastLF = -1;  // position of LF(Line-Feed) symbol latest found
   int body = 0;     // offset where document body starts
   int size = 0;     // document size according to header
   int chunk_size = 0, chunk_start = 0, chunk_n = 1;
   const static string content_length = "Content-Length:";
   const static string crlf = "\r\n";
   const static int crlf_length = 2;
   
   uint start = GetTickCount();
   result = "";

   do 
   {
      ResetLastError();
      if((len = (TLS ? SocketTlsReadAvailable(socket, block, 1024) :
         SocketReadAvailable(socket, block, 1024))) > 0)
      {
         const int n = ArraySize(response);
         ArrayCopy(response, block, n); // combine all received blocks
         
         if(body == 0) // seach for header termination sequence until found
         {
            for(int i = n; i < ArraySize(response); ++i)
            {
               if(response[i] == '\n')
               {
                  if(lastLF == i - crlf_length) // found "\r\n\r\n" sequence
                  {
                     body = i + 1;
                     string headers = CharArrayToString(response, 0, i);
                     Print("* HTTP-header found, header size: ", body);
                     Print(headers);
                     const int p = StringFind(headers, content_length); // TODO: should be case-insensitive!
                     if(p > -1)
                     {
                        size = (int)StringToInteger(StringSubstr(headers, p + StringLen(content_length)));
                        Print("* ", content_length, size);
                     }
                     else
                     {
                        size = -1; // server didn't report document length
                        // try to find chunk size in front of document
                        if(StringFind(headers, "Transfer-Encoding: chunked") > 0) // TODO: case-insensitive
                        {
                           // chunks syntax:
                           // hex-size\r\ncontent\r\n...
                           const string preview = CharArrayToString(response, body, 20);
                           chunk_size = HexStringToInteger(preview);
                           if(chunk_size > 0)
                           {
                              const int d = StringFind(preview, crlf) + crlf_length;
                              chunk_start = body;
                              Print("Chunk: ", chunk_size, " start at ", chunk_start, " -", d);
                              ArrayRemove(response, body, d);
                           }
                        }
                     }
                     break; // header/body boundary found
                  }
                  lastLF = i;
               }
            }
         }
         
         if(size == ArraySize(response) - body) // complete document
         {
            Print("* Complete document");
            break;
         }
         else if(chunk_size > 0 && ArraySize(response) - chunk_start >= chunk_size)
         {
            Print("* ", chunk_n, " chunk done: ", chunk_size, " total: ", ArraySize(response));
            const int p = chunk_start + chunk_size;
            const string preview = CharArrayToString(response, p, 20);
            if(StringLen(preview) > crlf_length
               && StringFind(preview, crlf, crlf_length) > crlf_length)
            {
               chunk_size = HexStringToInteger(preview, crlf_length);
               if(chunk_size > 0)
               {
                  int d = StringFind(preview, crlf, crlf_length) + crlf_length; // twice '\r\n'
                  chunk_start = p;
                  Print("Chunk: ", chunk_size, " start at ", chunk_start, " -", d);
                  ArrayRemove(response, chunk_start, d);
                  ++chunk_n;
               }
               else
               {
                  Print("* Final chunk");
                  ArrayRemove(response, p, 5); // "\r\n0\r\n"
                  break;
               }
            } // else wait for more data
         }
         start = GetTickCount();
      }
      else
      {
         if(len == 0) Sleep(10); // give more time for data to arrive
      }
   } 
   while(GetTickCount() - start < timeout && !IsStopped() && !_LastError);
      
   if(_LastError) PRTF(_LastError);
   
   if(ArraySize(response) > 0)
   {
      if(body != 0)
      {
         // TODO: we should check 'Content-Type:' for 'charset=UTF-8'
         result = CharArrayToString(response, body, WHOLE_ARRAY, CP_UTF8);
      }
      else
      {
         // provide incomplete header for troubleshooting
         result = CharArrayToString(response);
      }
   }

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
   SocketTimeouts(socket, Timeout, Timeout);
   if(PRTF(SocketConnect(socket, Server, Port, Timeout)))
   {
      string subject, issuer, serial, thumbprint; 
      datetime expiration;
      bool TLS = false;
      if(PRTF(SocketTlsCertificate(socket, subject, issuer, serial, thumbprint, expiration)))
      {
         PRTF(subject);
         PRTF(issuer);
         PRTF(serial);
         PRTF(thumbprint);
         PRTF(expiration);
         TLS = true;
      }
      else
      {
         ResetLastError(); // clear NETSOCKET_NO_CERTIFICATE(5275), continue in insecure mode
      }
      
      if(PRTF(HTTPSend(socket, StringFormat(
         "%s / HTTP/1.1\r\nHost: %s\r\nUser-Agent: MetaTrader 5\r\n\r\n",
         Method, Server), TLS)))
      {
         string response;
         if(PRTF(HTTPRecv(socket, response, Timeout, TLS)))
         {
            Print("Got ", StringLen(response), " bytes");
            // can be a big content, consider to save it into a file
            if(StringLen(response) > 1000)
            {
               int h = FileOpen(Server + ".htm", FILE_WRITE | FILE_TXT | FILE_ANSI, 0, CP_UTF8);
               FileWriteString(h, response);
               FileClose(h);
            }
            else
            {
               Print(response);
            }
         }
      }
   }
   SocketClose(socket);
}

//+------------------------------------------------------------------+
/*
   Examples:
   
   Server=www.google.com / ok
   Port=443 / ok
   SocketCreate()=1 / ok
   SocketConnect(socket,Server,Port,Timeout)=true / ok
   SocketTlsCertificate(socket,subject,issuer,serial,thumbprint,expiration)=true / ok
   subject=CN=www.google.com / ok
   issuer=C=US, O=Google Trust Services LLC, CN=GTS CA 1C3 / ok                                                                               
   serial=00c9c57583d70aa05d12161cde9ee32578 / ok
   thumbprint=1EEE9A574CC92773EF948B50E79703F1B55556BF / ok
   expiration=2022.10.03 08:25:10 / ok
   HTTPSend(socket,StringFormat(%s / HTTP/1.1
   Host: %s
   
   ,Method,Server),TLS)=true / ok
   * HTTP-header found, header size: 1080
   HTTP/1.1 200 OK
   Date: Mon, 01 Aug 2022 20:48:35 GMT
   Expires: -1
   Cache-Control: private, max-age=0
   Content-Type: text/html; charset=ISO-8859-1
   P3P: CP="This is not a P3P policy! See g.co/p3phelp for more info."
   Server: gws
   X-XSS-Protection: 0
   X-Frame-Options: SAMEORIGIN
   Set-Cookie: 1P_JAR=2022-08-01-20; expires=Wed, 31-Aug-2022 20:48:35 GMT; path=/; domain=.google.com; Secure
   ...
   Accept-Ranges: none
   Vary: Accept-Encoding
   Transfer-Encoding: chunked
   
   Chunk: 22172 start at 1080 -6
   * 1 chunk done: 22172 total: 24081
   Chunk: 30824 start at 23252 -8
   * 2 chunk done: 30824 total: 54083
   * Final chunk
   HTTPRecv(socket,response,Timeout,TLS)=true / ok
   Got 52998 bytes
   
   
   
   Server=www.mql5.com / ok
   Port=80 / ok
   SocketCreate()=1 / ok
   SocketConnect(socket,Server,Port,Timeout)=true / ok
   SocketTlsCertificate(socket,subject,issuer,serial,thumbprint,expiration)=false / NETSOCKET_NO_CERTIFICATE(5275)
   HTTPSend(socket,StringFormat(%s / HTTP/1.1
   Host: %s
   
   ,Method,Server),TLS)=true / ok
   * HTTP-header found, header size: 291
   HTTP/1.1 301 Moved Permanently
   Server: nginx
   Date: Sun, 31 Jul 2022 19:28:57 GMT
   Content-Type: text/html
   Content-Length: 162
   Connection: keep-alive
   Location: https://www.mql5.com/
   Strict-Transport-Security: max-age=31536000; includeSubDomains; preload
   X-Frame-Options: SAMEORIGIN
   
   * Content-Length:162
   * Complete document
   HTTPRecv(socket,response,Timeout,TLS)=true / ok
   <html>
   <head><title>301 Moved Permanently</title></head>
   <body>
   <center><h1>301 Moved Permanently</h1></center>
   <hr><center>nginx</center>
   </body>
   </html>
   
*/
//+------------------------------------------------------------------+
