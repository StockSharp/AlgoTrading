//+------------------------------------------------------------------+
//|                                                   wsprotocol.mqh |
//|                             Copyright 2020-2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include "wstools.mqh"
#include "wsframe.mqh"
#include "wsmessage.mqh"
#include "wsinterfaces.mqh"

#define Hybi WebSocketConnectionHybi
#define Hixie WebSocketConnectionHixie

//+------------------------------------------------------------------+
//| WebSocket protocol common part                                   |
//+------------------------------------------------------------------+
class WebSocketConnection: public IWebSocketConnection
{
protected:
   IWebSocketObserver *owner;
   IWebSocketTransport *socket;
   bool deflate;
    
   bool disconnecting;  // hard close at socket level
   bool closeRequested; // soft close at protocol level
   uchar writeBuffer[];
   uchar tail[];

   string headers[][2];

   void adjustCompression()
   {
      for(int i = 0; i < ArrayRange(headers, 0); i++)
      {
         if(StringFind(headers[i][0], "sec-websocket-extensions") == 0
         && StringFind(headers[i][1], "permessage-deflate") >= 0)
         {
            return;
         }
      }
      deflate = false;
      Print("Deflate doesn't supported by server");
   }

   bool writeInternalBuffer()
   {
      uchar buff[];
      const int n = ArraySize(writeBuffer);
      if(n > WS_BUFFSIZE)
      {
         ArrayCopy(buff, writeBuffer, 0, 0, WS_BUFFSIZE);
         ArrayCopy(writeBuffer, writeBuffer, 0, WS_BUFFSIZE);
         ArrayResize(writeBuffer, n - WS_BUFFSIZE);
      }
      else
      {
         ArrayCopy(buff, writeBuffer);
         ArrayResize(writeBuffer, 0);
      }
      
      if(!writePacketToNet(buff))
      {
         close();
         return false;
      }
    
      if(ArraySize(writeBuffer) == 0 && disconnecting)
      {
         close();
      }
      return true;
   }

   int writePacketToNet(const uchar &data[])
   {
      const int n = ArraySize(data);
      int fwrite = 0;
      int written = 0;
      uchar buffer[];
      for(; written < n; written += fwrite)
      {
         if(written > 0)
         {
            ArrayResize(buffer, 0); // shrink to 0 if something left from previous iteration
            ArrayCopy(buffer, data, 0, written);
            fwrite = socket.write(buffer);
         }
         else
         {
            fwrite = socket.write(data);
         }
         if(fwrite == -1)
         {
            Print("Write failed: ", _LastError);
            return 0; // can't
         }
      }
      return written;
   }

   int readPacketFromNet(uchar &data[], const bool waitForHeaders = false)
   {
      uchar buffer[];
      int read = 0;
      int cursor = 0;
    
      do
      {
         read = socket.read(buffer);
         if(read > 0)
         {
            ArrayCopy(data, buffer, cursor, 0, read);
            cursor += read;
         }
      
         if(waitForHeaders)
         {
            if(ArraySize(data) > 4 && StringFind(CharArrayToString(data), CRLFCRLF) > 0)
            {
               break;
            }
         }
      }
      while(((socket.isReadable() && read > 0)
         || (waitForHeaders && read != -1 && !_LastError)) && !IsStopped());
    
      if(!socket.isConnected())
      {
         close(); // notify subscribers
      }
    
      return cursor;
   }

   bool write(const uchar &data[])
   {
      ArrayCopy(writeBuffer, data, ArraySize(writeBuffer));
    
      while(ArraySize(writeBuffer) > 0)
      {
         if(!writeInternalBuffer()) return false;
      }
      return true;
   }

   int bufferSwap(uchar &buff1[], uchar &buff2[])
   {
      return ArraySwap(buff1, buff2) ? fmax(ArraySize(buff1), ArraySize(buff2)) : 0;
   }

   void close()
   {
      if(!disconnecting)
      {
         disconnecting = true;
         socket.close();
         owner.onDisconnect();
      }
   }

   string serializeHeaders(const string custom = NULL)
   {
      string str = "";
  
      for(int i = 0; i < ArrayRange(headers, 0); i++)
      {
         str += headers[i][0] + " " + headers[i][1] + "\r\n";
      }
      if(StringLen(custom)) str += custom;

      str += "\r\n";
      
      return str;
   }
    
public:
   WebSocketConnection(IWebSocketObserver *client, IWebSocketTransport *trans, const bool compression = false): deflate(compression)
   {
      owner = client;
      socket = trans;
      disconnecting = false;
      closeRequested = false;
   }

   virtual bool handshake(const string url, const string host, const string origin, const string custom = NULL)
   {
      uchar buffer[];
      WsTools::StringToByteArray(serializeHeaders(custom), buffer);
  
      socket.write(buffer);
      
      ArrayResize(buffer, 0); // shrink
    
      // wait for response with headers
      const int len = readPacketFromNet(buffer, true);
      string response = CharArrayToString(buffer, 0, WHOLE_ARRAY, CP_UTF8);
      PrintFormat("Buffer: '%s'", response);
      if(len == 0) return false;
      const int end = StringFind(response, CRLFCRLF);
      if(ArraySize(buffer) > end + StringLen(CRLFCRLF))
      {
         ArrayCopy(tail, buffer, 0, end + StringLen(CRLFCRLF));
         StringSetLength(response, end);
      }
      WsTools::parseHeaders(response, headers); // re-fill headers from responce
      Print("Headers: ");
      ArrayPrint(headers);
      
      if(deflate) adjustCompression(); // look for "Sec-WebSocket-Extensions: permessage-deflate"
      owner.onConnected();
      return true;
   }

   bool sendFrame(IWebSocketFrame *frame) override
   {
      uchar encoded[];
      frame.encode(encoded);
      const bool sent = writePacketToNet(encoded);
      if(sent && frame.getType() == WS_CLOSE_FRAME)
      {
         Print("Close requested");
         closeRequested = true;
      }
      return sent;
   }

   bool sendMessage(IWebSocketMessage *msg) override
   {
      IWebSocketFrame *frames[];
      msg.getFrames(frames);
      for(int i = 0; i < ArraySize(frames); i++)
      {
         if(!sendFrame(frames[i])) return false;
      }
    
      return true;
   }

   int checkMessages() override
   {
      IWebSocketFrame *dummy[1];
      return readFrame(dummy);
   }
};

//+------------------------------------------------------------------+
//| WebSocket protocol Hybi specific part                            |
//+------------------------------------------------------------------+
class WebSocketConnectionHybi: public WebSocketConnection
{
protected:
   IWebSocketMessage *openMessage;
   IWebSocketFrame *lastFrame;

   // Process single frame: create a new message from it or append to existing message;
   // when message is complete (all frames received), it's sent to onMessage() handler
   void processMessageFrame(IWebSocketFrame *frame)
   {
      if(openMessage && !openMessage.isFinalised())
      {
         openMessage.takeFrame(frame);
      }
      else
      {
         openMessage = new WebSocketMessage(frame.isCompressed());
         openMessage.takeFrame(frame);
      }
  
      if(openMessage && openMessage.isFinalised())
      {
         owner.onMessage(openMessage);
         openMessage = NULL;
      }
   }

   // Handle incoming control frames: sends Pong on Ping and close connection after a Close request
   void processControlFrame(IWebSocketFrame *frame)
   {
      switch(frame.getType())
      {
      case WS_FRAME_OPCODE::WS_CLOSE_FRAME:
         if(closeRequested) // our close was confirmed
         {
            Print("Server close ack");
         }
         else if(!disconnecting) // server initiated close
         {
            if(openMessage) // still not finalized(!)
            {
               owner.onMessage(openMessage);
               openMessage = NULL;
            }
          
            WebSocketFrame temp(WS_FRAME_OPCODE::WS_CLOSE_FRAME); // send our ack
            sendFrame(&temp);
         }
         close();
         break;
      case WS_FRAME_OPCODE::WS_PING_FRAME:
         {
            IWebSocketFrame *temp = WebSocketFrame::create(WS_FRAME_OPCODE::WS_PONG_FRAME, frame.getData());
            sendFrame(temp);
            delete temp;
         }
         break;
      }
   }

   bool send(WebSocketMessage *m)
   {
      return sendMessage(m) || disconnect(); // success status
   }

public:
   WebSocketConnectionHybi(IWebSocketObserver *client, IWebSocketTransport *trans,
      const bool compression = false): WebSocketConnection(client, trans, compression)
   {
      openMessage = NULL;
      lastFrame = NULL;
   }
    
   ~WebSocketConnectionHybi()
   {
      if(CheckPointer(openMessage) == POINTER_DYNAMIC) delete openMessage; // incomplete message
      if(CheckPointer(lastFrame) == POINTER_DYNAMIC) delete lastFrame; // orphan frame
   }

   static string randHybiKey()
   {
      uchar chars[16];
      for(int i = 0; i < 16; i++)
      {
         chars[i] = (uchar)(rand() % 256);
      }
    
      uchar key[];
      uchar encoded[];
      CryptEncode(CRYPT_BASE64, chars, key, encoded);
      return CharArrayToString(encoded);
   }

   bool handshake(const string url, const string host, const string origin, const string custom = NULL) override
   {
      const string handshakeChallenge = randHybiKey();
  
      WsTools::push(headers, "GET", url + " HTTP/1.1");
      WsTools::push(headers, "Connection:", "Upgrade");
      WsTools::push(headers, "Host:", host);
      WsTools::push(headers, "Sec-WebSocket-Key:", handshakeChallenge);
      WsTools::push(headers, "Origin:", origin);
      WsTools::push(headers, "Sec-WebSocket-Version:", (string)13);
      WsTools::push(headers, "Upgrade:", "websocket");
      if(deflate)
      {
         WsTools::push(headers, "Sec-WebSocket-Extensions:", "permessage-deflate; server_no_context_takeover; client_no_context_takeover; client_max_window_bits=15; server_max_window_bits=15");//client_max_window_bits; server_max_window_bits=15; client_max_window_bits=15
      }
      return WebSocketConnection::handshake(url, host, origin, custom);
   }
   
   int readFrame(IWebSocketFrame *&frames[]) override
   {
      uchar data[];
      bool closed = false;
      const int n = (ArraySize(tail) > 0) ? bufferSwap(tail, data) : readPacketFromNet(data);
      while(ArraySize(data) > 0 && !closed)
      {
         IWebSocketFrame *frame = WebSocketFrame::decode(data, lastFrame);
         if(frame)
         {
            if(frame.isReady())
            {
               if(frame.isControlFrame())
               {
                  processControlFrame(frame);
                  closed = (frame.getType() == WS_FRAME_OPCODE::WS_CLOSE_FRAME);
                  delete frame;
                  frame = NULL;
               }
               else
               {
                  processMessageFrame(frame);
                  if(ArrayIsDynamic(frames))
                  {
                     WsTools::push(frames, frame);
                  }
               }
               
               lastFrame = NULL;
            }
            else
            {
               lastFrame = frame;
            }
         }
         else
         {
            Print(__FUNCSIG__, " unexpected content - not a valid frame");
            return 0;
         }
      }
      return ArraySize(frames);
   }
    
   bool sendString(const string msg) override
   {
      WebSocketMessage m(msg, deflate);
      return send(&m);
   }
    
   bool sendData(const uchar &data[]) override
   {
      WebSocketMessage m(data, deflate);
      return send(&m);
   }

   bool disconnect() override
   {
      WebSocketFrame f(WS_FRAME_OPCODE::WS_CLOSE_FRAME);
      sendFrame(&f);
  
      int i = 0;
      do
      {
         i++;
         Print("Waiting...");
         Sleep(100);
         checkMessages();
      }
      while(i < 5 && socket.isConnected());
      
      return false;
   }
};

//+------------------------------------------------------------------+
//| WebSocket protocol Hixie specific part (not tested in MQL5!)     |
//+------------------------------------------------------------------+
class WebSocketConnectionHixie: public WebSocketConnection
{
   // In Hixie protocol, a message contains 1 frame always
public:
   WebSocketConnectionHixie(IWebSocketObserver *client, IWebSocketTransport *trans,
      const bool = false): WebSocketConnection(client, trans)
   {
   }

   struct HixieKey
   {
      long number;
      string key;
   };

   static HixieKey randHixieKey()
   {
      int spaces_n = rand() % 12 + 1;
      int max_n = INT_MAX / spaces_n;
      int number_n = rand() % max_n;
      long product_n = number_n * spaces_n;
      string key_n = IntegerToString(product_n);
      int range = rand() % 12 + 1;
      for(int i = 0; i < range; i++)
      {
         uchar c;
         if(rand() > 32767 / 2)
         {
            c = (uchar)(rand() % (0x2f + 1 - 0x21 + 1) + 0x21);
         }
         else
         {
            c = (uchar)(rand() % (0x7e + 1 - 0x3a + 1) + 0x3a);
         }
         int len = StringLen(key_n);
         int pos = rand() % len;
         string key_n1 = StringSubstr(key_n, 0, pos);
         string key_n2 = StringSubstr(key_n, pos);
         key_n = key_n1 + (string)c + key_n2;
      }
    
      for(int i = 0; i < spaces_n; i++)
      {
         int len = StringLen(key_n);
         int pos = rand() % (len - 1) + 1;
         string key_n1 = StringSubstr(key_n, 0, pos);
         string key_n2 = StringSubstr(key_n, pos);
         key_n = key_n1 + " " + key_n2;
      }
      HixieKey result;
      result.number = number_n;
      result.key = key_n;
      return result;
   }

   bool handshake(const string url, const string host, const string origin, const string custom = NULL) override
   {
      const string hixieKey1 = randHixieKey().key;
      const string hixieKey2 = randHixieKey().key;
      WsTools::push(headers, "GET", url + " HTTP/1.1");
      WsTools::push(headers, "Connection:", "Upgrade");
      WsTools::push(headers, "Host:", host);
      WsTools::push(headers, "Origin:", origin);
      WsTools::push(headers, "Sec-WebSocket-Key1:", hixieKey1);
      WsTools::push(headers, "Sec-WebSocket-Key2:", hixieKey2);
      WsTools::push(headers, "Upgrade:", "websocket");
      return WebSocketConnection::handshake(url, host, origin, custom);
   }

   int readFrame(IWebSocketFrame *&frames[]) override
   {
      uchar data[];
      const int n = (ArraySize(tail) > 0) ? bufferSwap(tail, data) : readPacketFromNet(data);
      IWebSocketFrame *f = WebSocketFrameHixie::decode(data);
      if(f)
      {
         IWebSocketMessage *m = new WebSocketMessageHixie(f);
         if(m)
         {
            WsTools::push(frames, f);
            owner.onMessage(m); // user code is responsible for message object from now on
            return 1;
         }
      }
      return 0;
   }

   bool sendString(const string msg) override
   {
      WebSocketMessageHixie m(msg);
      return sendMessage(&m);
   }
    
   bool sendData(const uchar &data[]) override
   {
      return false; // not supported
   }

   bool disconnect() override
   {
      WebSocketFrameHixie f(WS_FRAME_OPCODE::WS_CLOSE_FRAME);
      sendFrame(&f);
      close();
      return false;
   }
};
//+------------------------------------------------------------------+
