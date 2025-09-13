//+------------------------------------------------------------------+
//|                                         ReadNewsByWebRequest.mq4 |
//|                                                          Tungman |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Tungman"
#property link      "https://www.mql5.com"
#property version   "1.00"
#property strict

#property description "Read news from forex factory"

string               comment1 = "",
                     comment2 = "",
                     comment3 = ""; // Show news data

struct NewsData
  {
   string            title;
   string            country; // Country
   datetime          ReleaseTime; // Release time
   datetime          Expirytime; // Expiry time
   string            impact;
   string            forecast;
   string            previous;
  };

//+------------------------------------------------------------------+
//|Input parameter                                                   |
//+------------------------------------------------------------------+
// Price parameters
input double               PriceVolume             = 0.01; // Lot
input double               PriceSL                 = 300; // Stop loss
input double               PriceTP                 = 900; // Take profit (Point, 0 = not use)
input int                  Slippage                = 10;
input bool                 Showcomment             = false; // Show comment in left corner chart
input int                  Buydistance             = 200; // BUY - Place at point distance
input int                  Selldistance            = 200; // SELL - Place at point distance
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//--- create timer
   EventSetTimer(60);

//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//--- destroy timer
   EventKillTimer();

  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
   TradeNews();
  }
//+------------------------------------------------------------------+
//| Timer function                                                   |
//+------------------------------------------------------------------+
void OnTimer()
  {
   ReadNews();
  }
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//|Check is test mode or not                                         |
//+------------------------------------------------------------------+
bool IsTestMode()
  {
   bool res = true;
   if(!(MQLInfoInteger(MQL_DEBUG) || MQLInfoInteger(MQL_TESTER) || MQLInfoInteger(MQL_VISUAL_MODE) || MQLInfoInteger(MQL_OPTIMIZATION)))
      res = false;

   return res;
  }

//+------------------------------------------------------------------+
//| Read news from forex factory website                             |
//+------------------------------------------------------------------+
void ReadNews()
  {
   string cookie=NULL, headers;
   string reqheaders="User-Agent: Mozilla/4.0\r\n";
   char post[],result[];
   int res;
//  string url="https://www.forexfactory.com/calendar?day=today";
// string url="http://www.forexfactory.com/ffcal_week_this.xml";
   string url = "https://nfs.faireconomy.media/ff_calendar_thisweek.xml";
   ResetLastError();
   int timeout=5000;
   res= WebRequest("GET",url,reqheaders,timeout,post,result,headers);
   if(res==-1)
     {
      Print("Error in WebRequest. Error code  =",GetLastError());
      //--- Perhaps the URL is not listed, display a message about the necessity to add the address
      MessageBox("Add the address '"+url+"' in the list of allowed URLs on tab 'Expert Advisors'","Error",MB_ICONINFORMATION);
     }
   else
     {
      //--- Load successfully
      PrintFormat("The file has been successfully loaded, File size =%d bytes.",ArraySize(result));
      //--- Save the data to a file
      int filehandle=FileOpen("test.xml",FILE_READ);
      string tagtitle,tagcountry,tagdate,tagtime,tagimpact;
      //--- Checking errors
      if(filehandle!=INVALID_HANDLE)
        {
         uchar xx[];
         int i = 0;
         ;
         while(!FileIsEnding(filehandle))
           {
            ulong size  = FileSize(filehandle);
            string s  = FileReadString(filehandle,size);

            if(StringFind(s,"<title>") > -1)
              {
               StringReplace(s,"<title>","");
               StringReplace(s,"</title>","");
               tagtitle = s;
               // Print("News name : " + tagtitle);
              }

            if(StringFind(s,"<country>") > -1)
              {
               StringReplace(s,"<country>","");
               StringReplace(s,"</country>","");
               tagcountry =  s;
               //   Print("Country : " + tagcountry);
              }

            if(StringFind(s,"<date>") > -1)
              {
               StringReplace(s,"<date>","");
               StringReplace(s,"</date>","");
               StringReplace(s,"<![CDATA[","");
               StringReplace(s,"]]>","");
               tagdate = s;
              }

            if(StringFind(s,"<time>") > -1)
              {
               StringReplace(s,"<time>","");
               StringReplace(s,"</time>","");
               StringReplace(s,"<![CDATA[","");
               StringReplace(s,"]]>","");
               tagtime = s;
              }

            if(StringFind(s,"<impact>") > -1)
              {
               StringReplace(s,"<![CDATA[","");
               StringReplace(s,"]]>","");
               StringReplace(s,"<impact>","");
               StringReplace(s,"</impact>","");
               tagimpact =  s;
               // Print("impact : " + tagimpact);
              }

            // Input news data to strcuture
            if(tagimpact == "High" && TimeGMT() < StringToTime(TimeToString(StringToTime(tagdate),TIME_DATE) + " " + TimeToString(StringToTime(tagtime),TIME_MINUTES)))
              {
               data[i].title              = tagtitle;
               data[i].country            = tagcountry;
               data[i].ReleaseTime        = StringToTime(TimeToString(StringToTime(tagdate),TIME_DATE) + " " + TimeToString(StringToTime(tagtime),TIME_MINUTES));
               data[i].Expirytime         = StringToTime(TimeToString(StringToTime(tagdate),TIME_DATE) + " " + TimeToString(StringToTime(tagtime),TIME_MINUTES))
                                            + PeriodSeconds(PERIOD_M15);
              }

            i++;
           }
        }

      else
         Print("Error in FileOpen. Error code=",GetLastError());

      FileClose(filehandle);
     }
  }
//+------------------------------------------------------------------+

NewsData data[];

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void TradeNews()
  {
   double   CurrentBid        = SymbolInfoDouble(_Symbol, SYMBOL_BID);
   double   CurrentAsk        = SymbolInfoDouble(_Symbol, SYMBOL_ASK);
   double   BuyPrice, SellPrice, SL, TP;
   double   OrderVolume       = PriceVolume;
   MqlDateTime eadate;
   datetime curdatetime       = TimeGMT(eadate);
   datetime before            = curdatetime - PeriodSeconds(PERIOD_H4);
   datetime end               = curdatetime + PeriodSeconds(PERIOD_D1);
   datetime expdate           = curdatetime + PeriodSeconds(PERIOD_M10); // 12102021 Modify
   int spred                  = (int)SymbolInfoInteger(_Symbol, SYMBOL_SPREAD);
   int stop                   = (int)SymbolInfoInteger(_Symbol, SYMBOL_TRADE_STOPS_LEVEL);
   string d                   = "";
   double stopprice           = NormalizeDouble((stop + spred) * _Point, _Digits);

   if(ArraySize(data) > 0)
     {
      if(Showcomment == true)
        {
         if(!IsTestMode())
           {
            comment3 = "\n Today have red news is : " + IntegerToString(ArraySize(data));

            Comment(comment1 + comment2 + comment3);
           }
        }
     }

   for(int i = 0; i < ArraySize(data); i++)
     {
      datetime BeforeReleaseTime       = data[i].ReleaseTime - PeriodSeconds(PERIOD_M5);
      // put pending order before released time
      if(TimeGMT() > BeforeReleaseTime && TimeGMT() < data[i].ReleaseTime)
        {
         if(!CheckPendingOrder("B", "N") && CheckVolumeValue(OrderVolume,d)) // BUY NEWS
           {
            BuyPrice = CurrentAsk + (Buydistance * _Point);
            SL       = NormalizeDouble(BuyPrice - (PriceSL * _Point), _Digits);
            TP       = NormalizeDouble(BuyPrice + (PriceTP * _Point), _Digits);
            if(!OrderSend(_Symbol,OP_BUYSTOP,OrderVolume,BuyPrice,Slippage,SL,TP,"B" + _Symbol + EnumToString(PERIOD_CURRENT) + "N",0,expdate,clrNONE))
              {
               Print(__FUNCTION__ + " Cannot buy stop");
              }
           }

         if(!CheckPendingOrder("S", "N") && CheckVolumeValue(OrderVolume,d)) // SELL NEWS
           {
            SellPrice   = CurrentBid - (Selldistance * _Point);
            SL          = NormalizeDouble(SellPrice + (PriceSL * _Point), _Digits);
            TP          = NormalizeDouble(SellPrice - (PriceTP * _Point), _Digits);
            if(!OrderSend(_Symbol,OP_SELLSTOP,OrderVolume,SellPrice,Slippage,SL,TP,"S" + _Symbol + EnumToString(PERIOD_CURRENT) + "N",0,expdate,clrNONE))
              {
               Print(__FUNCTION__ + " Cannot sell stop");
              }
           }
        }
     }
  }
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//|Check exists pending order                                        |
//+------------------------------------------------------------------+
bool CheckPendingOrder(string ordertype,string suffix)
  {
   bool res = false;

   for(int i = OrdersTotal(); i >= 0; i --)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES) == true)
        {
         string PenSymbol        = OrderSymbol();
         string comment          = OrderComment();
         int type                = OrderType();
         if(PenSymbol == _Symbol && comment == ordertype + _Symbol + EnumToString(PERIOD_CURRENT) + suffix && type == OP_SELLSTOP && ordertype == "S")
           {
            res = true;
            break;
           }

         if(PenSymbol == _Symbol && comment == ordertype + _Symbol + EnumToString(PERIOD_CURRENT) + suffix && type == OP_BUYSTOP && ordertype == "B")
           {
            res = true;
            break;
           }
        }
     }

   return res;
  }

//+------------------------------------------------------------------+
//|Check Volume before open pending order                            |
//+------------------------------------------------------------------+
bool CheckVolumeValue(double volume,string &description)
  {
//--- minimal allowed volume for trade operations
   double min_volume=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MIN);
   if(volume<min_volume)
     {
      description=StringFormat("Volume is less than the minimal allowed SYMBOL_VOLUME_MIN=%.2f",min_volume);
      return(false);
     }

//--- maximal allowed volume of trade operations
   double max_volume=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MAX);
   if(volume>max_volume)
     {
      description=StringFormat("Volume is greater than the maximal allowed SYMBOL_VOLUME_MAX=%.2f",max_volume);
      return(false);
     }

//--- get minimal step of volume changing
   double volume_step=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_STEP);

   int ratio=(int)MathRound(volume/volume_step);
   if(MathAbs(ratio*volume_step-volume)>0.0000001)
     {
      description=StringFormat("Volume is not a multiple of the minimal step SYMBOL_VOLUME_STEP=%.2f, the closest correct volume is %.2f",
                               volume_step,ratio*volume_step);
      return(false);
     }
   description="Correct volume value";
   return(true);

  }
//+------------------------------------------------------------------+
