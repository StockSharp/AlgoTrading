//+------------------------------------------------------------------+
//|                                               WebRequestForm.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property description "Send a form with test pizza order. Form fields are hardcoded - to test other forms, change the set of inputs."
#property description "NB: Default 'Address' requires to allow 'httpbin.org' in terminal settings - to use other addresses, change settings accordingly."
#property script_show_inputs

#include <MQL5Book/PRTF.mqh>
#include <MQL5Book/HTTPRequest.mqh>

input string Address = "https://httpbin.org/post";

input string Customer = "custname=Vincent Silver";
input string Telephone = "custtel=123-123-123";
input string Email = "custemail=email@address.org";
input string PizzaSize = "size=small"; // PizzaSize (small,medium,large)
input string PizzaTopping = "topping=bacon"; // PizzaTopping (bacon,cheese,onion,mushroom)
input string DeliveryTime = "delivery=";
input string Comments = "comments=";

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   uchar result[];
   string response;
   // ; UTF-8 is implied for 'unreserved' chars
   // urlencode is performed by WebRequest behind the scene
   // local chars are supported
   string header = "Content-Type: application/x-www-form-urlencoded";
   string form_fields;
   StringConcatenate(form_fields,
      Customer, "&",
      Telephone, "&",
      Email, "&",
      PizzaSize, "&",
      PizzaTopping, "&",
      DeliveryTime, "&",
      Comments);
   HTTPRequest http(header);
   if(http.POST(Address, form_fields, result, response) > -1)
   {
      if(ArraySize(result) > 0)
      {
         PrintFormat("Got data: %d bytes", ArraySize(result));
         // NB: UTF-8 is implied in many content-types,
         // but some sites may require to analyze response headers
         Print(CharArrayToString(result, 0, WHOLE_ARRAY, CP_UTF8));
      }
   }
}
//+------------------------------------------------------------------+
