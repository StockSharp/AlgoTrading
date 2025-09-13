//+------------------------------------------------------------------+
//|                                              BackToTheFuture.mq5 |
//|                        Copyright 2015, MetaQuotes Software Corp. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "jimbulux"
#property link      "https://www.mql5.com"
#property version   "2.00"
//+------------------------------------------------------------------+
//| Defines                                                          |
//+------------------------------------------------------------------+
#define CHART_TEXT_OBJECT_NAME   "chart_google_quote"
#define CHART_TEXT_COUNTDOWN_NAME   "chart_google_countDown"
//+------------------------------------------------------------------+
//| Includes                                                         |
//+------------------------------------------------------------------+
#include <trade/trade.mqh>
//+------------------------------------------------------------------+
//| Inputs                                                           |
//+------------------------------------------------------------------+
input string Commentation1="";//Параметры регулятора:
input string GoogleSymbolName="NYSE:C"; //Символ в Google
input string GoogleFinanceLink="http://www.google.com/finance?q=c&ei=SK5dVaGeKouvsAHzioDIDA"; //Ссылка на символ
input double BarSize=0.25; //Бар для открытия ордера
input int GoogleFinanceHistoryMins=60;  //Опережение MetaTrader-а (мин.)
input string Commentation2="";//Параметры MetaTrader:
input double Lots=0.1;   //Лот
input int TpPips=10;   //Take Profit
input int SlPips=5000;   //Stop Loss
input int Count=5; //Сколько открывать позиций (-1 бесконечно)
input ENUM_ORDER_TYPE_FILLING Filling=ORDER_FILLING_RETURN;  //Режим заполнения ордера
input string Commentation3="";//Работа по расписанию:
input bool   Monday=true;// Понедельник (True - работает, False - отключен)
input bool   Tuesday=true;// Вторник (True - работает, False - отключен)
input bool   Wednesday=true;// Среда (True - работает, False - отключен)
input bool   Thursday=true;// Четверг (True - работает, False - отключен)
input bool   Friday=true;// Пятница (True - работает, False - отключен)
int  Monday1=0,Tuesday2=0,Wednesday3=0,Thursday4=0,Friday5=0;
double ArrayQuote[],DiffPrice=0,DeelayPosition=0,HistoryPrice=-1,ClosePrice=0;
int j=0;
datetime StartTime=0;
bool tradeResult=false;
int CountPos=0;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int FIFO() //first - price, two - time
  {
   j++;
   j++;
   ArrayResize(ArrayQuote,j,j);
//ArrayFill(ArrayQuote,j-1,1,0);
//Print(__FUNCTION__," ",j," ",gGoogleFinance.GetCurrentQuote()," ",ClosePrice);
   ArrayQuote[j-2]=gGoogleFinance.GetCurrentQuote();
   if(ClosePrice==ArrayQuote[j-2]) ArrayQuote[j-2]=0; //Delete Close Price
   else ClosePrice=0; //Unvalible Close Price
   ArrayQuote[j-1]=double(TimeCurrent());
   return 1;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double FindHistoryPrice() //Get price 75 min to past
  {
   int b=ArraySize(ArrayQuote);
   do
     {
      if(b <=0 ) return -1;
      b--;
      b--;
//Print(__FUNCTION__," ",b," ",TimeToString(ArrayQuote[b+1],TIME_DATE|TIME_MINUTES)," > ",TimeCurrent()-GoogleFinanceHistoryMins*60);
     }
   while(ArrayQuote[b+1]>(TimeCurrent()-GoogleFinanceHistoryMins*60));
   int b75=b;//Price 75 min to past
   b++;
   while(b+2<=ArraySize(ArrayQuote))//Find trend
     {
      b++;
      b++;
      //if(ArrayQuote[b-1]>(ArrayQuote[b75]+BarSize)) break;
      //if(ArrayQuote[b-1]<(ArrayQuote[b75]-BarSize)) break;
     };
   DiffPrice=ArrayQuote[b-1]; //Price for diffrent
   DeelayPosition=ArrayQuote[b]-ArrayQuote[b75+1]; //deelay time
   if(ArrayQuote[b75]==0 || DiffPrice==0) //error get quote
     {
      DiffPrice=0;
      return 0;
     }
   else
      return ArrayQuote[b75]; //price 75 min to past
  }
//+------------------------------------------------------------------+
//| Classes                                                          |
//+------------------------------------------------------------------+
class GoogleFinanceSymbol
  {
private:
   datetime          symbolQuoteStartTime;
   double            symbolQuoteStartPrice;
   string            symbolName;
   string            googleFinanceWebLink;
   double            getQuoteFromGoogleFinance();
   bool              sessionOpened;
public:
   void              GoogleFinanceSymbol(string symbolName,string webLink);
   datetime          GetQuoteStartTime();
   double            GetCurrentQuote();
   double            GetStartTimeQuoteValue();
   bool              GetGoogleFinanceSessionOpened();
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
GoogleFinanceSymbol::GoogleFinanceSymbol(string pSymbolName,string pWebLink) {
//Print(__FUNCTION__);
   symbolName=pSymbolName;
   googleFinanceWebLink=pWebLink;
   symbolQuoteStartPrice= getQuoteFromGoogleFinance();
   symbolQuoteStartTime = TimeCurrent();
   sessionOpened=sessionOpened;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
datetime GoogleFinanceSymbol::GetQuoteStartTime(void)
  {
   return symbolQuoteStartTime;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double GoogleFinanceSymbol::GetStartTimeQuoteValue(void)
  {
   return symbolQuoteStartPrice;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double GoogleFinanceSymbol::GetCurrentQuote(void)
  {
   return getQuoteFromGoogleFinance();
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool GoogleFinanceSymbol::GetGoogleFinanceSessionOpened(void)
  {
   return sessionOpened;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double GoogleFinanceSymbol::getQuoteFromGoogleFinance(void) {
//Print(__FUNCTION__);
   double quote=0;
   string headers;
   char post[],result[];
//---
   int res=WebRequest("GET",googleFinanceWebLink,"",NULL,10000,post,ArraySize(post),result,headers);
//---
   string webRes=CharArrayToString(result,0,WHOLE_ARRAY,CP_OEMCP);
   int sessionStatusCandidateIndex=StringFind(webRes,"nwp\">");
   int quoteCandidateIndex=StringFind(webRes,"<span class=\"pr\">");
//---
   if(quoteCandidateIndex!=-1)
     {
      quoteCandidateIndex+=StringLen("<span class=\"pr\"><span id=");
      string quoteCandidate=StringSubstr(webRes,quoteCandidateIndex,50);
      int openDelimiterIndex=StringFind(quoteCandidate,">")+1;
      int closeDelimiterIndex=StringFind(quoteCandidate,"</");
      quoteCandidate=StringSubstr(quoteCandidate,openDelimiterIndex,closeDelimiterIndex-openDelimiterIndex);
      StringReplace(quoteCandidate,",","");
      quote=StringToDouble(quoteCandidate);
     }
   if(sessionStatusCandidateIndex!=-1)
     {
      sessionOpened=((StringFind(webRes,"Real-time:")!=-1) || (StringFind(webRes,"Delayed:")!=-1) ? true : false);
     }
//---
   return quote;
  }
//+------------------------------------------------------------------+
//| Global variables                                                 |
//+------------------------------------------------------------------+
GoogleFinanceSymbol *gGoogleFinance;
double gTickSize;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit() {
//--- Work Plan
   if(Monday==false)
      Monday1=1;
   if(Tuesday==false)
      Tuesday2=2;
   if(Wednesday==false)
      Wednesday3=3;
   if(Thursday==false)
      Thursday4=4;
   if(Friday==false)
      Friday5=5;
//--- create timer
   EventSetTimer(10);
//---
   gTickSize=SymbolInfoDouble(_Symbol,SYMBOL_TRADE_TICK_SIZE);
   delete gGoogleFinance;
   gGoogleFinance=new GoogleFinanceSymbol(GoogleSymbolName,GoogleFinanceLink);
   string infoText="Google: "+GoogleSymbolName+". Time:"+TimeToString(gGoogleFinance.GetQuoteStartTime())
                   +". Value:"+DoubleToString(NormalizeDouble(gGoogleFinance.GetStartTimeQuoteValue(),2),2)+".";
   DisplayTextOnChart(CHART_TEXT_OBJECT_NAME,infoText);
//---
//Print(__FUNCTION__," ------------ ");
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
//---
  }
//+------------------------------------------------------------------+
//| Timer function                                                   |
//+------------------------------------------------------------------+
void OnTimer() {
//--- Server Time:
   datetime Time1=TimeCurrent();
   MqlDateTime strTime;
   TimeToStruct(Time1,strTime);
//---
   if(!gGoogleFinance.GetGoogleFinanceSessionOpened() || (Monday1==strTime.day_of_week || Tuesday2==strTime.day_of_week || Wednesday3==strTime.day_of_week || Thursday4==strTime.day_of_week || Friday5==strTime.day_of_week))
     {
//Print(__FUNCTION__," ",tradeResult," ",strTime.sec," ",StartTime," ",DeelayPosition);
      if((tradeResult) && ((TimeCurrent()-StartTime)>=(DeelayPosition)))
        {
         CTrade trade;
         trade.SetTypeFilling(Filling);
//Print(__FUNCTION__," 1 ");
         trade.PositionClose(_Symbol);
         tradeResult=false;
        }
      if(!gGoogleFinance.GetGoogleFinanceSessionOpened())
        {
         CountPos=0;//reset counter position
         ClosePrice=gGoogleFinance.GetCurrentQuote();//Save Close Price
//Print(__FUNCTION__," 2 ",gGoogleFinance.GetCurrentQuote());
        }
      j=0;
      EventKillTimer();
      OnInit();
//Print(__FUNCTION__," 3 ");
     }
   FIFO(); //Load history for ArrayQuote
   if(!tradeResult)
      HistoryPrice=FindHistoryPrice(); //Price 75 min to past + DiffPrice + DeelayPosition      
   double timeDiff=(double(TimeCurrent())-gGoogleFinance.GetQuoteStartTime());
   DisplayTextOnChart(CHART_TEXT_COUNTDOWN_NAME,"Since quote start: "+DoubleToString(timeDiff/60,2)+"min",420,20,clrLightSalmon);
   if(HistoryPrice!=-1)
     {
//Print(__FUNCTION__," 4 ",HistoryPrice);
      if((tradeResult) && ((TimeCurrent()-StartTime)>=(DeelayPosition)))
        {
         CTrade trade;
         trade.SetTypeFilling(Filling);
//Print(__FUNCTION__," 5 ");
         trade.PositionClose(_Symbol);
         tradeResult=false;
        }
      else
        {
         if(!tradeResult && CountPos!=Count)
           {
//Print(__FUNCTION__," 6 ",DiffPrice," > ",(HistoryPrice+BarSize)," < ",(HistoryPrice-BarSize));
            tradeResult=false;
            bool tradeOpened=false;
            MqlTradeRequest request={0};
            MqlTradeResult result={0};
            //double currQuote = gGoogleFinance.GetCurrentQuote();
            if(DiffPrice>(HistoryPrice+BarSize))
              {
//Print(__FUNCTION__," 7 ",DiffPrice);
               tradeOpened=true;
               double ask=NormalizeDouble(SymbolInfoDouble(_Symbol,SYMBOL_ASK),_Digits);
               double bid=NormalizeDouble(SymbolInfoDouble(_Symbol,SYMBOL_BID),_Digits);
               double spread = MathAbs(ask-bid);
               request.action=TRADE_ACTION_DEAL;         // setting a pending order
               request.magic=68975;                      // ORDER_MAGIC
               request.symbol=_Symbol;                   // symbol
               request.volume=Lots;                      // volume in 0.1 lots
               request.sl=NormalizeDouble(ask - SlPips * gTickSize, _Digits);        // Stop Loss is not specified
               request.tp=NormalizeDouble(ask + TpPips * gTickSize, _Digits);        // Take Profit is not specified     
               request.type=ORDER_TYPE_BUY;              // order type
               request.price=ask;                        // open price
               request.type_filling=Filling;
               //--- send a trade request
               tradeResult=OrderSend(request,result);
              }
            else if(DiffPrice<(HistoryPrice-BarSize))
              {
//Print(__FUNCTION__," 8 ");
               tradeOpened=true;
               double bid=NormalizeDouble(SymbolInfoDouble(_Symbol,SYMBOL_BID),_Digits);
               request.action=TRADE_ACTION_DEAL;         // setting a pending order
               request.magic=68975;                      // ORDER_MAGIC
               request.symbol=_Symbol;                   // symbol
               request.volume=Lots;                      // volume in 0.1 lots
               request.sl=NormalizeDouble(bid + SlPips * gTickSize, _Digits);        // Stop Loss is not specified
               request.tp=NormalizeDouble(bid - TpPips * gTickSize, _Digits);        // Take Profit is not specified     
               request.type=ORDER_TYPE_SELL;             // order type
               request.price=bid;                        // open price
               request.type_filling=Filling;
               //--- send a trade request
               tradeResult=OrderSend(request,result);
              }
            if((!tradeResult) && (tradeOpened))
              {
               Alert("Error opening new trade: "+result.comment);
              }
            if((tradeResult) && (tradeOpened))
              {
//Print(__FUNCTION__," 9 ",TimeCurrent());
               StartTime=TimeCurrent();
               CountPos++;
              }
            delete gGoogleFinance;
            gGoogleFinance=new GoogleFinanceSymbol(GoogleSymbolName,GoogleFinanceLink);
            string infoText="Google: "+GoogleSymbolName+". Time:"+TimeToString(gGoogleFinance.GetQuoteStartTime())
                            +". Value:"+DoubleToString(NormalizeDouble(gGoogleFinance.GetStartTimeQuoteValue(),2),2)+".";
            DisplayTextOnChart(CHART_TEXT_OBJECT_NAME,infoText);
           }
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void DisplayTextOnChart(string objetName,string textToDisplay,int xPos=10,int yPos=20,int textColor=clrGoldenrod)
  {
   if(ObjectFind(0,objetName)<0)
     {
      ObjectCreate(0,objetName,OBJ_LABEL,0,0,0);
     }
   ObjectSetInteger(0,objetName,OBJPROP_XDISTANCE,xPos);
   ObjectSetInteger(0,objetName,OBJPROP_YDISTANCE,yPos);
   ObjectSetString(0,objetName,OBJPROP_TEXT,textToDisplay);
   ObjectSetString(0,objetName,OBJPROP_FONT,"Verdana");
   ObjectSetInteger(0,objetName,OBJPROP_COLOR,textColor);
   ObjectSetInteger(0,objetName,OBJPROP_FONTSIZE,10);
   ObjectSetInteger(0,objetName,OBJPROP_SELECTABLE,false);
   ChartRedraw(0);
  }
//+------------------------------------------------------------------+
