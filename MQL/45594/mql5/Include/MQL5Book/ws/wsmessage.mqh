//+------------------------------------------------------------------+
//|                                                    wsmessage.mqh |
//|                             Copyright 2020-2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include "wstools.mqh"
#include "wsinterfaces.mqh"
#include "wsframe.mqh"

//+------------------------------------------------------------------+
//| WebSocket messaging according to Hixie RFC (back compatibility)  |
//+------------------------------------------------------------------+
class WebSocketMessageHixie: public IWebSocketMessage
{
protected:
   IWebSocketFrame *frame;
  
public:
   WebSocketMessageHixie(IWebSocketFrame *f = NULL): frame(f) { }
   WebSocketMessageHixie(const string &text): frame(NULL)
   {
      setString(text);
   }
    
   ~WebSocketMessageHixie()
   {
      if(frame) delete frame;
   }
    
   void getFrames(IWebSocketFrame *&frames[]) override
   {
      WsTools::push(frames, frame);
   }
  
   bool setString(const string &d) override
   {
      if(frame != NULL) delete frame;
      frame = WebSocketFrameHixie::create(WS_FRAME_OPCODE::WS_TEXT_FRAME, d);
      return frame != NULL;
   }

   string getString() override
   {
      return isFinalised() ? frame.getData() : NULL;
   }

   bool setData(const uchar &[]) override
   {
      return false;
   }
    
   bool getData(uchar &[]) override
   {
      return false;
   }
  
   bool isFinalised() override
   {
      return frame != NULL;
   }
  
   virtual bool takeFrame(IWebSocketFrame *f) override
   {
      if(!frame)
      {
         frame = f;
         return true;
      }
      return false;
   }
};

//+------------------------------------------------------------------+
//| WebSocket messaging according to Hybi RFC (current standard)     |
//+------------------------------------------------------------------+
class WebSocketMessage: public IWebSocketMessage
{
protected:
   IWebSocketFrame *frames[];
   uchar _data[];
   bool compressed;
   bool binary;
    
   bool compose()
   {
      if(compressed)
      {
         uchar key[] = {1, 0, 0, 0};
         uchar load[], result[];
         const int n = CryptEncode(CRYPT_ARCH_ZIP, _data, key, result);
         if(n == 0)
         {
            Print("Can't compress: ", _LastError);
            compressed = false; // fallback (need to clear deflate flag)
         }
         else
         {
            ArraySwap(_data, result);
         }
      }
      WsTools::push(frames, WebSocketFrame::create(/*compressed || */binary ?
         WS_FRAME_OPCODE::WS_BINARY_FRAME : WS_FRAME_OPCODE::WS_TEXT_FRAME, _data, compressed));
      return true;
   }
    
   bool extract()
   {
      if(!isFinalised())
      {
         Print("Can't get data from NotFinalised message");
         return false;
      }
      
      ArrayResize(_data, 0);
      
      for(int i = 0; i < ArraySize(frames); i++)
      {
         uchar temp[];
         frames[i].getData(temp);
         ArrayCopy(_data, temp, ArraySize(_data));
      }
      
      if(compressed)
      {
         uchar key[] = {1, 0, 0, 0};
         uchar result[];
         if(CryptDecode(CRYPT_ARCH_ZIP, _data, key, result) == 0)
         {
            Print("Can't decompress: ", _LastError);
         }
         else
         {
            ArraySwap(_data, result);
         }
      }
      return true;
    }

public:
   WebSocketMessage(const bool deflate = false): compressed(deflate), binary(false) { }
    
   WebSocketMessage(const uchar &bytes[], bool deflate = false): compressed(deflate), binary(true)
   {
      setData(bytes);
   }

   WebSocketMessage(const string &text, bool deflate = false): compressed(deflate), binary(false)
   {
      setString(text);
   }

   ~WebSocketMessage()
   {
      for(int i = 0; i < ArraySize(frames); i++)
      {
         if(CheckPointer(frames[i]) == POINTER_DYNAMIC) delete frames[i];
      }
      ArrayResize(frames, 0);
   }
    
   bool setData(const uchar &data[]) override
   {
      ArrayCopy(_data, data);
      return compose();
   }
    
   bool setString(const string &d) override
   {
      StringToCharArray(d, _data, 0, WHOLE_ARRAY, CP_UTF8);
      ArrayResize(_data, ArraySize(_data) - 1); // cut trailing '\0'
      return compose();
   }
  
   bool getData(uchar &data[]) override
   {
      return extract() && ArrayCopy(data, _data);
   }
  
   string getString() override
   {
      if(extract())
      {
         // NB. return string even if it's binary data, just for logging,
         // calling code should check applicability
         return CharArrayToString(_data, 0, WHOLE_ARRAY, CP_UTF8);
      }
      return NULL;
   }
  
   void getFrames(IWebSocketFrame *&_frames[]) override
   {
      ArrayCopy(_frames, this.frames);
   }
  
   bool isFinalised() override
   {
      if(ArraySize(frames) == 0)
        return false;
  
      return frames[ArraySize(frames) - 1].isFinal();
   }

   // append frame to the message
   virtual bool takeFrame(IWebSocketFrame *frame) override
   {
      if(CheckPointer(frame) != POINTER_INVALID)
      {
         WsTools::push(frames, frame);
         return true;
      }
      return false;
   }
};
//+------------------------------------------------------------------+
