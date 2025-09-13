//+------------------------------------------------------------------+
//|                                                  wstransport.mqh |
//|                             Copyright 2020-2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include "wstools.mqh"
#include "wsinterfaces.mqh"

//+------------------------------------------------------------------+
//| Network processing based on built-in MQL5 Socket-functions       |
//+------------------------------------------------------------------+
class MqlWebSocketTransport: public IWebSocketTransport
{
protected:
   int handle;
   uint timeout;
   bool TLS;
   bool connected;

   MqlWebSocketTransport(const string scheme, const string host, const uint port, const uint t = 1)
   {
      handle = SocketCreate();
      timeout = t;
      TLS = scheme == "wss";
      if(handle != INVALID_HANDLE)
      {
         Print("Connecting to ", host, ":", port);
         if(SocketConnect(handle, host, port, timeout))
         {
            SocketTimeouts(handle, timeout, timeout);
          
            string subject, issuer, serial, thumbprint;
            datetime expiration;
            if(TLS)
            {
               if(!SocketTlsHandshake(handle, host))
               {
                  Print("TLS handshake error: ", _LastError);
                  TLS = false;
               }
               else
               {
                  Print("TLS enabled");
               }
               if(SocketTlsCertificate(handle, subject, issuer, serial, thumbprint, expiration))
               {
                  Print("TLS:");
                  Print("  Owner:      ", subject);
                  Print("  Issuer:     ", issuer);
                  Print("  Number:     ", serial);
                  Print("  Signature:  ", thumbprint);
                  Print("  Expiration: ", expiration);
               }
            }
         }
         else
         {
            Print("Can't connect: ", GetLastError());
         }
      }
      else
      {
         Print("Can't create socket: ", GetLastError());
      }
   }

   ~MqlWebSocketTransport()
   {
      SocketClose(handle);
   }

public:
   static MqlWebSocketTransport *create(const string scheme, const string host, const uint port, const uint timeout)
   {
      MqlWebSocketTransport *result = new MqlWebSocketTransport(scheme, host, port, timeout);
      if(result.getHandle() == INVALID_HANDLE)
      {
         Print("Socket create failed: ", _LastError);
         delete result;
         result = NULL;
      }
      
      return result;
   }

   bool isConnected(void) const override
   {
      return SocketIsConnected(handle);
   }

   int getHandle() const override
   {
      return handle;
   }
    
   int write(const uchar &buffer[]) override
   {
      if(!SocketIsConnected(handle))
      {
         Print("Can't write to non-connected socket");
         return -1;
      }
      
      const uint len = ArraySize(buffer);
      
      int written = 0;
      ResetLastError();

      if(TLS)
      {
         written = SocketTlsSend(handle, buffer, len);
      }
      else
      {
         written = SocketSend(handle, buffer, len);
      }
      return written;
   }
    
   int read(uchar &buffer[]) override
   {  // can be disconnected already but still having data in internal buffer
      if(!SocketIsConnected(handle) && SocketIsReadable(handle) == 0)
      {
         Print("Can't read from non-connected socket");
         return -1;
      }
      
      ResetLastError();
      ArrayResize(buffer, 0); // ensure buffer is of zero size

      uint available = SocketIsReadable(handle);
      int received = 0;
      
      #ifdef NETWORK_PURGE
      ResetLastError();
      // depending from network conditions it may require to call
      // SocketIsReadable twice in a row!
      // yes, this is ridiculous but may help here
      available = SocketIsReadable(handle);
      #endif

      if(!available) return 0;
      else if(available == !_LastError && TLS) return 0;

      if(TLS)
      {
         // SocketIsReadable is not compatible with SocketTlsRead!
         // first returns raw data count, second tries to read clean deciphered bytes count
         received = SocketTlsReadAvailable(handle, buffer, available); // waiting for data (block)
      }
      else
      {
         received = SocketRead(handle, buffer, available, timeout);
      }

      if(received == -1)
      {
         // NB: even if we got -1 result and _LastError = 5273,
         // buffer may contain valid data received before connection was lost/closed
         Print("SocketRead failed: ", _LastError, " Available: ", available);
         if(_LastError == 5273 && available == 1 && !TLS) return -1;
      }
      
      return ArraySize(buffer);
   }

   bool isReadable(void) const override
   {
      return SocketIsReadable(handle) > 0;
   }
    
   bool isWritable(void) const override
   {
      return SocketIsWritable(handle);
   }
    
   void close(void) override
   {
      SocketClose(handle);
      handle = INVALID_HANDLE;
   }
};
//+------------------------------------------------------------------+
