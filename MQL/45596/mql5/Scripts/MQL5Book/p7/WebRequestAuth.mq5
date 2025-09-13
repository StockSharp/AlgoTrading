//+------------------------------------------------------------------+
//|                                               WebRequestAuth.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property description "Request Digest authorization on a protected web-page."
#property description "NB: Default 'Address' requires to allow 'httpbin.org' in terminal settings - to use other addresses, change settings accordingly."
#property script_show_inputs

#include <MQL5Book/PRTF.mqh>
#include <MQL5Book/StringUtils.mqh>
#include <MQL5Book/URL.mqh>
#include <MQL5Book/HTTPHeader.mqh>

const string Method = "GET";
input string Address = "https://httpbin.org/digest-auth/auth/test/pass";
input string Headers = "User-Agent: noname";
input int Timeout = 5000;
input string User = "test";
input string Password = "pass";
input bool DumpDataToFiles = true;

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   string parts[];
   URL::parse(Address, parts);

   uchar data[], result[];
   string response;
   int code = PRTF(WebRequest(Method, Address, Headers, Timeout, data, result, response));
   Print(response);
   if(code == 401)
   {
      if(StringLen(User) == 0 || StringLen(Password) == 0)
      {
         Print("Credentials required");
         return;
      }
      
      code = -1;
      HttpHeader header(response, '\n', ':');
      const string auth = header["WWW-Authenticate"];
      if(StringFind(auth, "Basic ") == 0)
      {
         string Header = Headers;
         if(StringLen(Header) > 0) Header += "\r\n";
         Header += "Authorization: Basic ";
         Header += HttpHeader::hash(User + ":" + Password, CRYPT_BASE64);
         PRTF(Header);
         code = PRTF(WebRequest(Method, Address, Header, Timeout, data, result, response));
         Print(response);
      }
      else if(StringFind(auth, "Digest ") == 0)
      {
         HttpHeader params(StringSubstr(auth, 7), ',', '=');
         string realm = HttpHeader::unquote(params["realm"]);
         if(realm != NULL)
         {
            string qop = HttpHeader::unquote(params["qop"]);
            if(qop == "auth")
            {
               string h1 = HttpHeader::hash(User + ":" + realm + ":" + Password);
               string h2 = HttpHeader::hash(Method + ":" + parts[URL_PATH]);
               string nonce = HttpHeader::unquote(params["nonce"]);
               string counter = StringFormat("%08x", 1);
               string cnonce = StringFormat("%08x", MathRand());
               string h3 = HttpHeader::hash(h1 + ":" + nonce + ":" + counter + ":" + cnonce + ":" + qop + ":" + h2);
               
               string Header = Headers;
               if(StringLen(Header) > 0) Header += "\r\n";
               Header += "Authorization: Digest ";
               Header += "username=\"" + User + "\",";
               Header += "realm=\"" + realm + "\",";
               Header += "nonce=\"" + nonce + "\",";
               Header += "uri=\"" + parts[URL_PATH] + "\",";
               Header += "qop=" + qop + ",";
               Header += "nc=" + counter + ",";
               Header += "cnonce=\"" + cnonce + "\",";
               Header += "response=\"" + h3 + "\",";
               Header += "opaque=" + params["opaque"] + "";
               PRTF(Header);
               code = PRTF(WebRequest(Method, Address, Header, Timeout, data, result, response));
               Print(response);
            }
         }
      }
   }
   
   if(code > -1)
   {
      if(ArraySize(result) > 0)
      {
         PrintFormat("Got data: %d bytes", ArraySize(result));
         if(DumpDataToFiles)
         {
            const string filename = parts[URL_HOST] +
               (StringLen(parts[URL_PATH]) > 1 ? parts[URL_PATH] : "/_index_.htm");
            Print("Saving ", filename);
            PRTF(FileSave(filename, result));
         }
         else
         {
            Print(CharArrayToString(result, 0, 80, CP_UTF8));
         }
      }
   }
}
//+------------------------------------------------------------------+
