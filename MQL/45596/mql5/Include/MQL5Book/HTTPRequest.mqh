//+------------------------------------------------------------------+
//|                                                  HTTPRequest.mqh |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#ifndef PRTF
#define PRTF
#endif

//+------------------------------------------------------------------+
//| Basic HTTP-requests with cookies                                 |
//+------------------------------------------------------------------+
class HTTPRequest
{
protected:
   string common_headers;
   int timeout;
   
public:
   HTTPRequest(const string h = NULL, const int t = 5000):
      common_headers(h), timeout(t)
   {
      if(h != NULL) StringReplace(common_headers, "|", "\r\n"); // provide this because headers can come from 'input'
   }

   int HEAD(const string address, uchar &result[], string &response,
      const string custom_headers = NULL)
   {
      uchar nodata[];
      return request("HEAD", address, custom_headers, nodata, result, response);
   }
   
   int GET(const string address, uchar &result[], string &response,
      const string custom_headers = NULL)
   {
      uchar nodata[];
      return request("GET", address, custom_headers, nodata, result, response);
   }
   
   int POST(const string address, const string payload,
      uchar &result[], string &response, const string custom_headers = NULL)
   {
      uchar bytes[];
      const int n = StringToCharArray(payload, bytes, 0, -1, CP_UTF8);
      ArrayResize(bytes, n - 1); // remove terminal zero
      return request("POST", address, custom_headers, bytes, result, response);
   }

   int POST(const string address, const uchar &payload[],
      uchar &result[], string &response, const string custom_headers = NULL)
   {
      return request("POST", address, custom_headers, payload, result, response);
   }
   
   int request(const string method, const string address,
      string headers, const uchar &data[], uchar &result[], string &response)
   {
      if(headers == NULL) headers = common_headers;

      ArrayResize(result, 0);
      response = NULL;
      Print(">>> Request:\n", method + " " + address + "\n" + headers);
      
      const int code = PRTF(WebRequest(method, address, headers, timeout, data, result, response));
      Print("<<< Response:\n", response);
      return code;
   }
};
//+------------------------------------------------------------------+
