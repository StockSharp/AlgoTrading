//+------------------------------------------------------------------+
//|                                                 wsinterfaces.mqh |
//|                             Copyright 2020-2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
//+------------------------------------------------------------------+
//| Defines                                                          |
//+------------------------------------------------------------------+
#define WS_APP_TYPE_CLIENT 1
#define WS_APP_TYPE_SERVER 0 // not supported in MQL5, can be used for unmasking only (frame debugging)

#ifndef WS_APP_TYPE
#define WS_APP_TYPE WS_APP_TYPE_CLIENT // masking is enabled in client, disabled in server
#endif

#define WS_BUFFSIZE 1024
#define CRLFCRLF "\r\n\r\n"

//+------------------------------------------------------------------+
//| Communication level abstraction: a wrapper for built-in          |
//| Socket-functions or other DLL-based implemenations               |
//+------------------------------------------------------------------+
interface IWebSocketTransport
{
   int write(const uchar &data[]);
   int read(uchar &buffer[]);
   bool isConnected(void) const;
   bool isReadable(void) const;
   bool isWritable(void) const;
   int getHandle(void) const;
   void close(void);
};

//+------------------------------------------------------------------+
//| Enum with all frame codes defined in WebSocket protocol          |
//+------------------------------------------------------------------+
enum WS_FRAME_OPCODE
{
   WS_DEFAULT = 0,

   WS_CONTINUATION_FRAME = 0x00,
   WS_TEXT_FRAME = 0x01,
   WS_BINARY_FRAME = 0x02,

   WS_CLOSE_FRAME = 0x08,

   WS_PING_FRAME = 0x09,
   WS_PONG_FRAME = 0x0A
};

//+------------------------------------------------------------------+
//| Interface for WebSocket frames, which compose a message          |
//| (defined as a class because of default/nested implementation)    |
//+------------------------------------------------------------------+
class IWebSocketFrame
{
public:

   //+------------------------------------------------------------------+
   //| Interface for static methods in IWebSocketFrame                  |
   //+------------------------------------------------------------------+
   class StaticCreator
   {
   public:
      virtual IWebSocketFrame *decode(uchar &data[], IWebSocketFrame *head = NULL) = 0;
      virtual IWebSocketFrame *create(WS_FRAME_OPCODE type, const string data = NULL, const bool deflate = false) = 0;
      virtual IWebSocketFrame *create(WS_FRAME_OPCODE type, const uchar &data[], const bool deflate = false) = 0;
   };

protected:
   //+------------------------------------------------------------------+
   //| Require all the static methods to exist in P                     |
   //+------------------------------------------------------------------+
   template<typename P>
   class Creator: public StaticCreator
   {
   public:
      // decode received binary data into a IWebSocketFrame (use head for continuation frames)
      virtual IWebSocketFrame *decode(uchar &data[], IWebSocketFrame *head = NULL) override
      {
         return P::decode(data, head);
      }
      // create a text/close/other frame with optional payload
      virtual IWebSocketFrame *create(WS_FRAME_OPCODE type, const string data = NULL, const bool deflate = false) override
      {
         return P::create(type, data, deflate);
      };
      // create a binary/text/close/other frame with payload
      virtual IWebSocketFrame *create(WS_FRAME_OPCODE type, const uchar &data[], const bool deflate = false) override
      {
         return P::create(type, data, deflate);
      };
   };

public:
   // require creator itself to exist, hence all static methods must be implemented
   virtual IWebSocketFrame::StaticCreator *getCreator() = 0;
   
   // using frame content, prepare and return its raw data to be sent over network
   virtual int encode(uchar &encoded[]) = 0;

   // read payload as text
   virtual string getData() = 0;
   
   // read payload as raw data, return size
   virtual int getData(uchar &buf[]) = 0;

   // return frame type (opcode)
   virtual WS_FRAME_OPCODE getType() = 0;
  
   // check if the frame is a control frame:
   // control frames should be handled internally by websocket classes
   virtual bool isControlFrame()
   {
      return (getType() >= WS_CLOSE_FRAME);
   }

   virtual bool isReady() { return true; }
   virtual bool isFinal() { return true; }
   virtual bool isMasked() { return false; }
   virtual bool isCompressed() { return false; }
};

//+------------------------------------------------------------------+
//| Interface for WebSocket messages                                 |
//+------------------------------------------------------------------+
class IWebSocketMessage
{
public:
   // retreive an array of frames of which this message is composed
   virtual void getFrames(IWebSocketFrame *&frames[]) = 0;
  
   // set text as message content
   virtual bool setString(const string &data) = 0;
  
   // return message content as text
   virtual string getString() = 0;
  
   // set binary data as message content
   virtual bool setData(const uchar &data[]) = 0;
   
   // return message content raw
   virtual bool getData(uchar &data[]) = 0;
  
   // return if all frames of the message are received
   virtual bool isFinalised() = 0;
  
   // add a frame into message
   virtual bool takeFrame(IWebSocketFrame *frame) = 0;
};

//+------------------------------------------------------------------+
//| Interface for WebSocket connection functionality                 |
//+------------------------------------------------------------------+
interface IWebSocketConnection
{
   // open a connection
   bool handshake(const string url, const string host, const string origin, const string custom = NULL);
   
   // low-level get incoming frame(s) from a server
   int readFrame(IWebSocketFrame *&frames[]);
   
   int checkMessages();
   
   // low-level send raw frame (for example, close or ping)
   bool sendFrame(IWebSocketFrame *frame);
   
   // low-level send raw message
   bool sendMessage(IWebSocketMessage *msg);
   
   // user-level send text
   bool sendString(const string msg);
   
   // user-level send binary data
   bool sendData(const uchar &data[]);
   
   // close connection
   bool disconnect(void);
};

//+------------------------------------------------------------------+
//| Interface for WebSocket events callbacks                         |
//+------------------------------------------------------------------+
interface IWebSocketObserver
{
  void onDisconnect();
  void onConnected();
  void onMessage(IWebSocketMessage *msg);
};
//+------------------------------------------------------------------+
