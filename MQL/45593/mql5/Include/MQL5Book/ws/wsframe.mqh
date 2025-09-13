//+------------------------------------------------------------------+
//|                                                      wsframe.mqh |
//|                             Copyright 2020-2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include "wstools.mqh"
#include "wsinterfaces.mqh"

//+------------------------------------------------------------------+
//| WebSocket frame according to Hybi RFC (current standard)         |
//+------------------------------------------------------------------+
class WebSocketFrame: public IWebSocketFrame
{
protected:
   // first byte
   uchar FIN;
   uchar RSV1;
   uchar RSV2;
   uchar RSV3;
   WS_FRAME_OPCODE opcode;
  
   // second byte
   uchar mask;
   uint payloadLength; // NB: this lib supports 32 bit length only
    
   // if mask is enabled
   uint maskingKey;
  
   uchar payloadData[];
   uint actualLength;
   /*
      0                   1                   2                   3
      0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
     +-+-+-+-+-------+-+-------------+-------------------------------+
     |F|R|R|R| opcode|M| Payload len |    Extended payload length    |
     |I|S|S|S|  (4)  |A|     (7)     |             (16/64)           |
     |N|V|V|V|       |S|             |   (if payload len==126/127)   |
     | |1|2|3|       |K|             |                               |
     +-+-+-+-+-------+-+-------------+ - - - - - - - - - - - - - - - +
     |     Extended payload length continued, if payload len == 127  |
     + - - - - - - - - - - - - - - - +-------------------------------+
     |                               |Masking-key, if MASK set to 1  |
     +-------------------------------+-------------------------------+
     | Masking-key (continued)       |          Payload Data         |
     +-------------------------------- - - - - - - - - - - - - - - - +
     :                     Payload Data continued ...                :
     + - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - +
     |                     Payload Data continued ...                |
     +---------------------------------------------------------------+  
   */

   void setType(WS_FRAME_OPCODE type)
   {
      opcode = type;
      if(type == WS_FRAME_OPCODE::WS_CLOSE_FRAME) // needed to make frame larger than 1 byte
      {
         mask = 1;
      }
   }

   static bool IsBitSet(uchar byte, int pos)
   {
      return (byte & (1 << pos)) > 0;
   }
  
   static void rotMask(uchar &data[], uint key, int offset = 0)
   {
      WsTools::BYTES4 u4b(key);
      for(int i = 0; i < ArraySize(data); i++)
      {
         int j = (i + offset) % 4;
         data[i] = (data[i] ^ u4b[j]);
      }
   }
    
   class Creator1: public IWebSocketFrame::Creator<WebSocketFrame>
   {
   };
    
   virtual IWebSocketFrame::StaticCreator *getCreator() override
   {
      static Creator1 c1;
      return &c1;
   }

   void assign(WS_FRAME_OPCODE type, const bool fin, const bool deflate, const int n)
   {
      setType(type);
      FIN = fin;         // TODO: adjust this for continuation frames
      RSV1 = deflate;    // NB: this can be set in the first frame only
      payloadLength = n;
   }

public:
   WebSocketFrame(WS_FRAME_OPCODE deftype = WS_FRAME_OPCODE::WS_TEXT_FRAME)
   {
      setType(deftype);
      FIN = true;
      RSV1 = 0;
      RSV2 = 0;
      RSV3 = 0;
      mask = WS_APP_TYPE;
      payloadLength = 0;
      maskingKey = 0;
      actualLength = 0;
   }
    
   ~WebSocketFrame()
   {
   }

   static IWebSocketFrame *create(WS_FRAME_OPCODE type, const string data = NULL, const bool deflate = false)
   {
      WebSocketFrame *f = new WebSocketFrame();
  
      const int n = WsTools::StringToByteArray(data, f.payloadData, 0, WHOLE_ARRAY,
         type == WS_FRAME_OPCODE::WS_TEXT_FRAME && !deflate ? CP_UTF8 : CP_ACP);

      f.assign(type, true, deflate, n);
      return f;
   }

   static IWebSocketFrame *create(WS_FRAME_OPCODE type, const uchar &data[], const bool deflate = false)
   {
      WebSocketFrame *f = new WebSocketFrame();
  
      const int n = ArrayCopy(f.payloadData, data, 0, 0);
      
      f.assign(type, true, deflate, n);
      return f;
   }
  
   bool isMasked() override
   {
      return mask == 1;
   }

   bool isCompressed() override
   {
      return RSV1 == 1;
   }

   WS_FRAME_OPCODE getType() override
   {
      return opcode;
   }

   int encode(uchar &encoded[]) override
   {
      payloadLength = ArraySize(payloadData);
  
      uchar firstByte = (uchar)opcode;
      uchar secondByte = 0;
  
      firstByte += FIN * 128 + RSV1 * 64 + RSV2 * 32 + RSV3 * 16;
      
      ArrayResize(encoded, 14); // allocate memory for maximal header
      encoded[0] = firstByte;
      int filled = 0;
  
      if(payloadLength <= 125)
      {
         secondByte = (uchar)payloadLength;
         secondByte += mask * 128;
         encoded[1] = secondByte;
         filled = 2;
      }
      else if(payloadLength <= 255 * 255 - 1)
      {
         secondByte = 126;
         secondByte += mask * 128;
         encoded[1] = secondByte;
         WsTools::pack2((ushort)payloadLength, encoded, 2); // 16 bit, big endian byte order
         filled = 4;
      }
      else
      {
         // TODO: max length is now 32 bits instead of 64
         secondByte = 127;
         secondByte += mask * 128;
         encoded[1] = secondByte;
         WsTools::pack4(0, encoded, 2);
         WsTools::pack4(payloadLength, encoded, 6);
         filled = 10;
      }
  
      uint key = 0;
      if(mask)
      {
         key = ((uint)rand() | ((uint)rand() << 16)) % (4228250625); // rand is 2 bytes wide
         WsTools::pack4(key, encoded, filled);
         filled += 4;
         key = MathSwap(key);
      }
      
      ArrayResize(encoded, filled + ArraySize(payloadData));

      if(ArraySize(payloadData))
      {
         if(mask == 1) rotMask(payloadData, key);
         ArrayCopy(encoded, payloadData, filled);
      }

      return ArraySize(encoded);
   }

   static IWebSocketFrame *decode(uchar &data[], IWebSocketFrame *head = NULL)
   {
      uint remaining = 0;
      uint consumed = 0;

      WebSocketFrame *frame = NULL;
      if(head != NULL)
      {
         frame = head;
         remaining = ArraySize(data);
      }
      else
      {
         if(ArraySize(data) < 2)
         {
            Print("Insufficient data for frame ", ArraySize(data));
            return NULL;
         }
        
         frame = new WebSocketFrame();
        
         // read the first two bytes, then chop them off
         uchar firstByte = (uchar)data[0];
         uchar secondByte = (uchar)data[1];
        
         const uint n = ArraySize(data);

         frame.FIN = IsBitSet(firstByte, 7);
         frame.RSV1 = IsBitSet(firstByte, 6);
         frame.RSV2 = IsBitSet(firstByte, 5);
         frame.RSV3 = IsBitSet(firstByte, 4);
         frame.mask = IsBitSet(secondByte, 7);
         frame.opcode = (WS_FRAME_OPCODE)(firstByte & 0x0F);
        
         if(!(frame.opcode >= WS_CONTINUATION_FRAME && frame.opcode <= WS_PONG_FRAME))
         {
            Print("Bad frame opcode");
         }
  
         int len = secondByte & ~128;
  
         if(len <= 125)
         {
            frame.payloadLength = len;
            consumed = 2;
         }
         else if(len == 126)
         {
            frame.payloadLength = WsTools::unpack2(data, 2);
            consumed = 4;
         }
         else if(len == 127)
         {
            uint h = WsTools::unpack4(data, 2);
            uint l = WsTools::unpack4(data, 6);
            frame.payloadLength = (uint)(l + (h * 0x0100000000));
            consumed = 10;
         }
  
         if(frame.mask)
         {
            WsTools::BYTES4 u4b(data, consumed);
            frame.maskingKey = u4b.num;
            consumed += 4;
         }
         remaining = n - consumed;
      }
  
      uint currentOffset = frame.actualLength;
      uint fullLength = MathMin(frame.payloadLength - frame.actualLength, remaining);
      frame.actualLength += fullLength;

      uchar frameData[];
      if(fullLength < remaining) // data contains current frame and beginning of the next frame
      {
         ArrayCopy(frameData, data, 0, consumed, fullLength);
         ArrayCopy(data, data, 0, consumed + fullLength);
         ArrayResize(data, remaining - fullLength);
      }
      else
      {
         ArrayCopy(frameData, data, 0, consumed);
         ArrayResize(data, 0);
      }
  
      if(frame.mask)
      {
         rotMask(frameData, frame.maskingKey, currentOffset);
      }
      ArrayCopy(frame.payloadData, frameData, ArraySize(frame.payloadData));
  
      return frame;
   }

   bool isReady()
   {
      if(actualLength > payloadLength)
      {
         Print("WebSocket frame size mismatch");
         return true;
      }
      return (actualLength == payloadLength);
   }

   bool isFinal() override
   {
      return FIN == 1;
   }

   string getData() override
   {
      return CharArrayToString(payloadData, 0, WHOLE_ARRAY, CP_UTF8);
   }
    
   int getData(uchar &buf[]) override
   {
      return ArrayCopy(buf, payloadData);
   }
};

//+------------------------------------------------------------------+
//| WebSocket frame according to Hixie RFC (back compatibility)      |
//+------------------------------------------------------------------+
class WebSocketFrameHixie: public IWebSocketFrame
{
protected:
   WS_FRAME_OPCODE opcode;
   string payloadData;

public:
    
   class Creator1: public IWebSocketFrame::Creator<WebSocketFrameHixie>
   {
   };

   virtual IWebSocketFrame::StaticCreator *getCreator() override
   {
      static Creator1 c1;
      return &c1;
   }

   WebSocketFrameHixie(WS_FRAME_OPCODE deftype = WS_FRAME_OPCODE::WS_TEXT_FRAME): opcode(deftype) { }

   static WebSocketFrameHixie *create(WS_FRAME_OPCODE type, const string data = NULL, const bool deflate = false)
   {
      WebSocketFrameHixie *o = new WebSocketFrameHixie();
  
      o.payloadData = data;
  
      return o;
   }

   static WebSocketFrameHixie *create(WS_FRAME_OPCODE type, const uchar &data[], const bool deflate = false)
   {
      return NULL; // binary data not supported by design
   }

   int encode(uchar &encoded[]) override
   {
      if(opcode == WS_FRAME_OPCODE::WS_CLOSE_FRAME)
      {
         static const uchar closing[] = {0xFF, 0x00};
         return ArrayCopy(encoded, closing);
      }
      return WsTools::StringToByteArray(CharToString(0) + payloadData + CharToString(255), encoded);
   }

   string getData() override
   {
      return payloadData;
   }

   int getData(uchar &buf[]) override
   {
      return StringToCharArray(payloadData, buf, 0, WHOLE_ARRAY, CP_UTF8);
   }

   WS_FRAME_OPCODE getType() override
   {
      return opcode;
   }

   static WebSocketFrameHixie *decode(uchar &str[], WebSocketFrameHixie *head = NULL)
   {
      if(ArraySize(str) < 2) return NULL;
      if(str[0] == 0 && str[ArraySize(str) - 1] == 255) // NB: frames with 0x01+ first byte not supported here
      {
         WebSocketFrameHixie *o = new WebSocketFrameHixie();
         o.payloadData = CharArrayToString(str, 1, ArraySize(str) - 2, CP_UTF8);
         return o;
      }
      // NB: frames with length prefix (0x80+) not supported here
      return NULL;
   }
};
//+------------------------------------------------------------------+
