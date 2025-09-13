//+------------------------------------------------------------------+
//|                                                   FirminMACD.mq4 |
//|                                                          Mikhail |
//|                                        http://www.landofcash.net |
//+------------------------------------------------------------------+
#property copyright "Mikhail"
#property link      "http://www.landofcash.net"
string _ver="LCS v2.0 MACD-Trader ";
bool _doTrade=true;
extern string _orderComment="LCS v1.3 MACD-Trader";
extern string PIPSMultiplyerComment="SET 10 on 5 digit account OR 1 on 4 digit account.";
extern int _pipsMultiplyer=10;
extern int _fastEMAPeriod=12;
extern int _slowEMAPeriod=26;
extern int _signalPeriod=9;
extern bool _useStoch=true;
extern int _barsToCheckStoch=5;
extern string StochPropertiesComment="Stochastic Properties (Default:5,3,3,SMA,Close/Close)";
extern int StochKPeriod=5;
extern int StochDPeriod=3;
extern int StochSlowing=3;
extern string StochStochModeComment="SMA=0; EMA=1; SMMA=2; LWMA=3";
extern int StochMode=MODE_SMA;
extern string StochPriceFieldComment="HIGH/LOW=0; CLOSE/CLOSE=1;";
extern int StochPriceField=1;


extern double _lotSize=0.01;
extern int _takeProfitPips=100;
extern int _stopLossPips=100;
extern int _maxOrders=5;

extern bool _enableTrailing=false;
extern int _stopLossTrailInitialStartPips=50;
extern int _stopLossTrailPips=25;

extern int _profitWhenToSetNoLossStopPips=25;
extern int _profitOfNoLossStopPips=1;

extern string _startPeriod1="08:15";
extern string _endPeriod1="8:35";
extern string _startPeriod2="13:45";
extern string _endPeriod2="14:42";
extern string _startPeriod3="22:15";
extern string _endPeriod3="22:45";
int _initialMagicNumber=8889;
datetime _lastRun=0;
datetime _lastOrder=0;
bool _isFirstTick=false;

string _objPref="LCSMACDTrader";
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----  
   _stopLossTrailInitialStartPips=_stopLossTrailInitialStartPips*_pipsMultiplyer;
   _stopLossTrailPips=_stopLossTrailPips*_pipsMultiplyer;
   _profitWhenToSetNoLossStopPips=_profitWhenToSetNoLossStopPips*_pipsMultiplyer;
   _profitOfNoLossStopPips=_profitOfNoLossStopPips*_pipsMultiplyer;
   _takeProfitPips=_takeProfitPips*_pipsMultiplyer;
   _stopLossPips=_stopLossPips*_pipsMultiplyer;
   TradeIsNotBusy();
   Print(_ver+" Started at "+TimeCurrent());
   Comment(_ver);
   return(0);
  }
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {
//----
   DeleteLabels();
   Comment("");
//----
   return(0);
  }
bool CheckStoch(int orderType){   
   double main,mainCurrent;
   double signal,signalCurrent;
   int i;
   mainCurrent=iStochastic(Symbol(),Period(),StochKPeriod,StochDPeriod,StochSlowing,StochMode,StochPriceField,MODE_MAIN,0); 
   signalCurrent=iStochastic(Symbol(),Period(),StochKPeriod,StochDPeriod,StochSlowing,StochMode,StochPriceField,MODE_SIGNAL,0); 
   if(signalCurrent<mainCurrent && orderType==OP_BUY){
      for(i=1;i<_barsToCheckStoch;i++){
         main=iStochastic(Symbol(),Period(),StochKPeriod,StochDPeriod,StochSlowing,StochMode,StochPriceField,MODE_MAIN,i); 
         signal=iStochastic(Symbol(),Period(),StochKPeriod,StochDPeriod,StochSlowing,StochMode,StochPriceField,MODE_SIGNAL,i); 
      }
      if(signal>main){
         return (true);
      }
   }
   if(signalCurrent>mainCurrent && orderType==OP_SELL){
      for(i=1;i<_barsToCheckStoch;i++){
         main=iStochastic(Symbol(),Period(),StochKPeriod,StochDPeriod,StochSlowing,StochMode,StochPriceField,MODE_MAIN,i); 
         signal=iStochastic(Symbol(),Period(),StochKPeriod,StochDPeriod,StochSlowing,StochMode,StochPriceField,MODE_SIGNAL,i); 
      }
      if(signal<main){
         return (true);
      }
   }
   return (false);
}

int NeedOpenType(){
   double macdMain = iMACD(Symbol(),Period(),_fastEMAPeriod,_slowEMAPeriod,_signalPeriod,PRICE_CLOSE,MODE_MAIN,0);
   double macdSignal = iMACD(Symbol(),Period(),_fastEMAPeriod,_slowEMAPeriod,_signalPeriod,PRICE_CLOSE,MODE_SIGNAL,0);
   double macdMainPrev = iMACD(Symbol(),Period(),_fastEMAPeriod,_slowEMAPeriod,_signalPeriod,PRICE_CLOSE,MODE_MAIN,1);
   double macdSignalPrev = iMACD(Symbol(),Period(),_fastEMAPeriod,_slowEMAPeriod,_signalPeriod,PRICE_CLOSE,MODE_SIGNAL,1);
   if(macdMain>macdSignal && macdMainPrev<=macdSignalPrev && macdMain<0 && macdMainPrev<0){     
      if(!_useStoch || CheckStoch(OP_BUY)){
         return (OP_BUY);
      }
   }
   if(macdMain<macdSignal && macdMainPrev>=macdSignalPrev && macdMain>0 && macdMainPrev>0){     
      if(!_useStoch || CheckStoch(OP_SELL)){
         return (OP_SELL);
      }
   }
   return (-1);
}
bool NeedToClose(){
   return (false);
} 
bool OrdersNumber(){
   int number =0;
   for(int i=0;i<OrdersTotal();i++)
   {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false) break;      
      if(OrderSymbol()==Symbol() && OrderCloseTime()==0)
      {  
         if(OrderMagicNumber()==GenerateMagicNumber(_initialMagicNumber,Symbol(),Period())){            
            number++;            
         }         
      }
   }
   return (number);
}
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
//----
   if(TimeCurrent()-_lastRun>Period()*60){          
      _lastRun =  Time[0];     
      Print("BarOpen:"+TimeToStr(_lastRun,TIME_DATE|TIME_SECONDS)+" Current time:"+TimeToStr(TimeCurrent(),TIME_DATE|TIME_SECONDS));
      _isFirstTick=true;
   }else{
      _isFirstTick=false;
   }  
   CreateTextLabel(_objPref+"caption",1,0,_ver,DodgerBlue,16, "Tahoma");
   bool needOpenType=-1;
   int ordersNumber = OrdersNumber();
   if(ordersNumber>0){
      for(int i=0;i<OrdersTotal();i++)
      {
         if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false) break;      
         if(OrderSymbol()==Symbol() && OrderCloseTime()==0)
         {
            if(OrderMagicNumber()==GenerateMagicNumber(_initialMagicNumber,Symbol(),Period())){ 
               if(NeedToClose()){
                  if(TradeIsBusy() < 0) {
                         return(-1); 
                  }  
                  if(OrderType()==OP_BUY){
                     OrderClose(OrderTicket(),OrderLots(),NormalizeDouble(Bid,Digits),4, Lime);
                  }else{
                     OrderClose(OrderTicket(),OrderLots(),NormalizeDouble(Ask,Digits),4, Lime);
                  }
                  TradeIsNotBusy(); 
               }else{
                  if(_enableTrailing){
                     TralingStop();
                  }
               }
            }
         }
      }
   }
   
   if(ordersNumber<_maxOrders && _lastOrder<Time[0]){
      needOpenType=NeedOpenType();      
      if(needOpenType==OP_SELL){
         if(isGoodTime(_startPeriod1,_endPeriod1) || isGoodTime(_startPeriod2,_endPeriod2) || isGoodTime(_startPeriod3,_endPeriod3)){
            orderOpen(needOpenType, Bid, _stopLossPips,_takeProfitPips, _initialMagicNumber);
            _lastOrder=Time[0];
         }
      }
      if(needOpenType==OP_BUY){
         if(isGoodTime(_startPeriod1,_endPeriod1) || isGoodTime(_startPeriod2,_endPeriod2) || isGoodTime(_startPeriod3,_endPeriod3)){
            orderOpen(needOpenType, Ask, _stopLossPips,_takeProfitPips, _initialMagicNumber);      
            _lastOrder=Time[0];
         }
      }
   }
   return(0);
  }
  

bool isGoodTime(string startAllowedDayTime, string endAllowedDayTime){
   bool goodDay = true;
   bool goodTime = false;  
   bool goodMinute=true;
   if(goodDay){
      //if hour greater or less STRICT!!!
      if(TimeHour(TimeCurrent())>TimeHour(StrToTime(startAllowedDayTime)) && TimeHour(TimeCurrent())<TimeHour(StrToTime(endAllowedDayTime))){
         goodTime = true;
      } 
      //if Hour is equal to Start hour then compare start minute
      if(TimeHour(TimeCurrent())==TimeHour(StrToTime(startAllowedDayTime))){
         goodTime = true;
         if(TimeMinute(TimeCurrent())>=TimeMinute(StrToTime(startAllowedDayTime))){
            goodMinute = goodMinute&&true;
         }else{
            goodMinute = goodMinute&&false;
         }
      } 
      //if Hour is equal to End hour then compare end minute
      if(TimeHour(TimeCurrent())==TimeHour(StrToTime(endAllowedDayTime))){
         goodTime = true;
         if(TimeMinute(TimeCurrent())<=TimeMinute(StrToTime(endAllowedDayTime))){
            goodMinute = goodMinute && true;
         }else{
            goodMinute = goodMinute && false;
         }           
      }      
   }
   if(!(goodDay&&goodTime&&goodMinute) && _isFirstTick){
      Print("GoodTime false. TimeCurrent:"+TimeToStr(TimeCurrent())
      +" _startAllowedDayTime:"+TimeToStr(StrToTime(startAllowedDayTime))
      +" _endAllowedDayTime"+TimeToStr(StrToTime(endAllowedDayTime))
      +" goodDay:"+goodDay+" goodTime:"+goodTime);
   }
   return (goodDay&&goodTime&&goodMinute);
}

//+------------------------------------------------------------------+
double OrderLotSize(){
   double lots;
   //lots= (AccountLeverage() * (_lotPercent/100) * AccountEquity())/MarketInfo(Symbol(), MODE_LOTSIZE);
   lots= _lotSize;
   //Print("Lots:"+lots);
   if(lots<MarketInfo(Symbol(), MODE_MINLOT)){
      lots=MarketInfo(Symbol(), MODE_MINLOT);
   }
   //lots=lots-MathMod(lots, MarketInfo(Symbol(), MODE_LOTSTEP));
   return (lots);
} 

//+------------------------------------------------------------------+
//+------------------------------------------------------------------+
int  TralingStop(){ 
   int magicNumber=OrderMagicNumber();   
   int ticket=OrderTicket();
   double sl=0,tp=0;
   for(int i=0;i<OrdersTotal();i++)
   {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==true){
         if(OrderSymbol()==Symbol() && OrderCloseTime()==0)
         {
            if(OrderMagicNumber()==magicNumber){
               break;
            }
         }
      }else{
         Print("Error Selecting Order.");
      }         
   }

//move SL in Profit
   if(OrderType()==OP_BUY)
   {
     if(Bid-OrderOpenPrice()>Point*_profitWhenToSetNoLossStopPips)
     {
      sl=NormalizeDouble(OrderOpenPrice()+Point*_profitOfNoLossStopPips,Digits);   
      if(OrderStopLoss()+Point*5*_pipsMultiplyer<sl && OrderOpenPrice()<Bid)
        {
         if(TradeIsBusy() < 0) {
                return(-1); 
         }                     
         tp=OrderTakeProfit();
         if(!OrderModify(OrderTicket(),OrderOpenPrice(),sl,tp,0,Green)>0){
            PrintOrderError("Error in UseNoLossStop (OP_BUY)", GetLastError(),sl,tp);
         }
          TradeIsNotBusy();
        }
     }
   }
   if(OrderType()==OP_SELL)
   {
    if((OrderOpenPrice()-Ask)>(Point*_profitWhenToSetNoLossStopPips))
      {
       sl=NormalizeDouble(OrderOpenPrice()-Point*_profitOfNoLossStopPips,Digits);   
       if(OrderStopLoss()-Point*5*_pipsMultiplyer>sl && OrderOpenPrice()>Ask)
         {
          if(TradeIsBusy() < 0) {
                return(-1); 
            }                     
          tp=OrderTakeProfit();
          if(!OrderModify(OrderTicket(),OrderOpenPrice(),sl,tp,0,Red)){
             PrintOrderError("Error in UseNoLossStop (OP_SELL)", GetLastError(), sl, tp);
          }
           TradeIsNotBusy();
         }
      }
   }
 
   //stop loss
   if(OrderType()==OP_BUY)
   {
     if(Bid-OrderOpenPrice()>Point*_stopLossTrailInitialStartPips)
     {
      sl=NormalizeDouble(Bid-Point*MathMax(_stopLossTrailPips,MarketInfo(Symbol(),MODE_STOPLEVEL)),Digits); 
      if(OrderStopLoss()+Point*5*_pipsMultiplyer<sl && OrderOpenPrice()<Bid)
        {
         if(TradeIsBusy() < 0) {
                return(-1); 
         }                      
         tp=OrderTakeProfit();
         if(!OrderModify(OrderTicket(),OrderOpenPrice(),sl,tp,0,Green)){
            PrintOrderError("Error in StopLossTrailAlgorythm==1 (OP_BUY)", GetLastError(),sl,tp);
         }
          TradeIsNotBusy();
        }
     }
   }
   if(OrderType()==OP_SELL)
   {
    if((OrderOpenPrice()-Ask)>(Point*_stopLossTrailInitialStartPips))
      {
       sl=NormalizeDouble(Ask+Point*MathMax(_stopLossTrailPips,MarketInfo(Symbol(),MODE_STOPLEVEL)),Digits);       
       if(OrderStopLoss()-Point*5*_pipsMultiplyer>sl && OrderOpenPrice()>Ask)
         {
          if(TradeIsBusy() < 0) {
                return(-1); 
            }              
           tp=OrderTakeProfit();
           if(!OrderModify(OrderTicket(),OrderOpenPrice(),sl,tp,0,Red)){
            PrintOrderError("Error in StopLossTrailAlgorythm==1 (OP_SELL)", GetLastError(),sl,tp);
           }           
           TradeIsNotBusy();
         }
      }
   }

   return (OrderTicket());      
} 
   
void PrintOrderError(string name, int error, double sl, double tp){
   Print(name+"Error:"+error+" Ticket:"+OrderTicket()+" Order Price:"+OrderOpenPrice()+" Order SL:"+OrderStopLoss()+" New SL:"+ sl +
                " Order TP:"+OrderTakeProfit()+" New TP:"+tp+" min SL:"+MarketInfo(Symbol(),MODE_STOPLEVEL));
} 

int orderOpen(int cmd, double open, int stopLossPips, int takeProfitPips, int magicNumber){       
      double sl=0, tp=0;
      if(stopLossPips>0){
         stopLossPips = MathMax(stopLossPips,MarketInfo(Symbol(),MODE_STOPLEVEL)+3*_pipsMultiplyer);
      }
      if(takeProfitPips>0){
         takeProfitPips = MathMax(takeProfitPips,MarketInfo(Symbol(),MODE_STOPLEVEL)+3*_pipsMultiplyer);
      }
      int ticket=-1;
      if(_doTrade){        
         double lots  = OrderLotSize();
         if(lots>=MarketInfo(Symbol(), MODE_MINLOT)){          
            //wait context
            if(TradeIsBusy() < 0) {
                 return(-1); 
            }
            if(cmd==OP_BUYSTOP || cmd==OP_BUY || cmd==OP_SELLLIMIT){
               RefreshRates();
               if(cmd==OP_BUY){
                  open=Ask;
               }
               if(stopLossPips>0){
                  sl=NormalizeDouble(open-stopLossPips*Point,Digits);
               }
               if(takeProfitPips>0){
                  tp=NormalizeDouble(open+takeProfitPips*Point,Digits);
               }
               ticket=OrderSend(Symbol(),cmd,lots,NormalizeDouble(open,Digits),5*_pipsMultiplyer,sl,tp,_orderComment,GenerateMagicNumber(magicNumber,Symbol(),Period()),0,SpringGreen);              
            }else{
               RefreshRates();
               if(cmd==OP_SELL){
                  open=Bid;
               }
               if(stopLossPips>0){
                  sl=NormalizeDouble(open+stopLossPips*Point,Digits);
               }
               if(takeProfitPips>0){
                  tp=NormalizeDouble(open-takeProfitPips*Point,Digits);
               }
               ticket=OrderSend(Symbol(),cmd,lots,NormalizeDouble(open,Digits),5*_pipsMultiplyer,sl,tp,_orderComment,GenerateMagicNumber(magicNumber,Symbol(),Period()),0,Red);
            }
            if(ticket<0)
            {
             if(cmd==OP_BUYSTOP || cmd==OP_BUY || cmd==OP_SELLLIMIT){
               Print("OrderSend failed with error #",GetLastError()," bid:"+NormalizeDouble(Bid,Digits)," ask:"+NormalizeDouble(Ask,Digits)," Open:"+NormalizeDouble(open,Digits),
                  " SL:",NormalizeDouble(open-stopLossPips*Point,Digits)," TP:",NormalizeDouble(open+takeProfitPips*Point,Digits)
                  ," MODE_STOPLEVEL:"+MarketInfo(Symbol(),MODE_STOPLEVEL)," LOT:"+lots);  
             }else{
               Print("OrderSend failed with error #",GetLastError()," bid:"+NormalizeDouble(Bid,Digits)," ask:"+NormalizeDouble(Ask,Digits)," Open:"+NormalizeDouble(open,Digits),
                  " SL:",NormalizeDouble(open+stopLossPips*Point,Digits)," TP:",NormalizeDouble(open-takeProfitPips*Point,Digits),
                  " MODE_STOPLEVEL:"+MarketInfo(Symbol(),MODE_STOPLEVEL)," LOT:"+lots);  
             }      
            }
            TradeIsNotBusy();
         }else{
            Print("Order Failed. Lot is too small lot:"+lots);
         }
      } else{
         if(cmd==OP_BUY){
            CreateLabel("order",Time[0],Low[0]-0.0000,5);            
         }else{
            CreateLabel("order",Time[0],High[0]+0.0000,5);             
         }
      } 
      return (ticket);
  }

  
//+------------------------------------------------------------------+
string PeriodToStr(){
   if(Period()==1){
      return (" 1M");
   }
   if(Period()==5){
      return (" 5M");
   }
   if(Period()==15){
      return ("15M");
   }
   if(Period()==30){
      return ("30M");
   }
   if(Period()==60){
      return (" 1H");
   }
   if(Period()==240){
      return (" 4H");
   }
   if(Period()==1440){
      return (" 1D");
   }
   if(Period()==10080){
      return (" 1W");
   }
   if(Period()==43200){
      return (" 1M");
   }
}
void DeleteLabels(){
   int    obj_total=ObjectsTotal();
   string name;
   for(int i=0;i<obj_total;i++)
   {
    name=ObjectName(i);    
    if(StringFind(name, _objPref,0)>-1){      
      ObjectDelete(name);
      i--;
    }
   }
}
void UpdateTextLabel(string name, color boxcolor, string text){
   ObjectSetText(name, text, 12, "Arial", boxcolor); 
}
void CreateTextLabel(string name, int x, int y,  string text, color boxcolor=LightGreen, int fontSize=12, string fontFace="Verdana"){
   if(ObjectFind(name)<0){
      ObjectDelete(name);
      if(!ObjectCreate(name, OBJ_LABEL,0, 0, 0))
      {
       Print("error: cant create OBJ_LABEL! code #",GetLastError());
       return(0);
      }
   }
   ObjectSet(name, OBJPROP_CORNER, 1);
   ObjectSet(name, OBJPROP_XDISTANCE, x);
   ObjectSet(name, OBJPROP_YDISTANCE, y);  
   ObjectSetText(name, text, fontSize, fontFace, boxcolor);      
}
void CreateText(string name, datetime time1, double price,color boxcolor, string text){
   ObjectDelete(name);
   if(!ObjectCreate(name, OBJ_TEXT,0, time1, price))
   {
    Print("error: cant create OBJ_TEXT! code #",GetLastError());
    return(0);
   }
   ObjectSetText(name, text, 7, "Verdana", boxcolor);
}
void CreateLabel(string name, datetime time1, double price,int code, color lcolor = Coral){
   ObjectDelete(name);
   if(!ObjectCreate(name, OBJ_ARROW,0, time1, price))
   {
    Print("error: cant create OBJ_TEXT! code #",GetLastError());
    return(0);
   }
   ObjectSet(name, OBJPROP_ARROWCODE, code); 
   ObjectSet(name, OBJPROP_COLOR, lcolor);      
}
void CreateHLine(string name, double price, color linecolor){
      ObjectDelete(name);
      if(!ObjectCreate(name, OBJ_HLINE,0, 0, price))
      {
       Print("error: cant create OBJ_HLINE! code #",GetLastError());
       return(0);
      }
      ObjectSet(name, OBJPROP_COLOR, linecolor);
  }
void CreateVLine(string name, datetime time1, color linecolor){
      ObjectDelete(name);
      if(!ObjectCreate(name, OBJ_VLINE,0, time1, 0))
      {
       Print("error: cant create OBJ_VLINE! code #",GetLastError());
       return(0);
      }
      ObjectSet(name, OBJPROP_COLOR, linecolor);
  }
void CreateTLine(string name, datetime time1, double price1, datetime time2, double price2, color linecolor, int linethikness=1){
   ObjectDelete(name);
   if(!ObjectCreate(name, OBJ_TREND,0, time2,price2,time1,price1))
   {
    Print("error: cant create OBJ_VLINE! code #",GetLastError());
    return(0);
   }
   
   ObjectSet(name, OBJPROP_WIDTH, linethikness);
   ObjectSet(name, OBJPROP_RAY, true);
   ObjectSet(name, OBJPROP_STYLE, STYLE_DASH);
   ObjectSet(name, OBJPROP_COLOR, linecolor);
}

//----------------------- GENERATE MAGIC NUMBER BASE ON SYMBOL AND TIME FRAME FUNCTION
int GenerateMagicNumber(int MagicNumber, string symbol, int timeFrame)
{
   int isymbol = 0;
   if (symbol == "EURUSD")       isymbol = 1;
   else if (symbol == "GBPUSD")  isymbol = 2;
   else if (symbol == "USDJPY")  isymbol = 3;
   else if (symbol == "USDCHF")  isymbol = 4;
   else if (symbol == "AUDUSD")  isymbol = 5;
   else if (symbol == "USDCAD")  isymbol = 6;
   else if (symbol == "EURGBP")  isymbol = 7;
   else if (symbol == "EURJPY")  isymbol = 8;
   else if (symbol == "EURCHF")  isymbol = 9;
   else if (symbol == "EURAUD")  isymbol = 10;
   else if (symbol == "EURCAD")  isymbol = 11;
   else if (symbol == "GBPUSD")  isymbol = 12;
   else if (symbol == "GBPJPY")  isymbol = 13;
   else if (symbol == "GBPCHF")  isymbol = 14;
   else if (symbol == "GBPAUD")  isymbol = 15;
   else if (symbol == "GBPCAD")  isymbol = 16;
   else if (symbol == "AUDCAD")  isymbol = 17;
   else if (symbol == "AUDCHF")  isymbol = 18;
   else if (symbol == "AUDJPY")  isymbol = 19;
   else if (symbol == "AUDNZD")  isymbol = 20;
   else if (symbol == "CHFJPY")  isymbol = 21;
   else if (symbol == "EURNZD")  isymbol = 22;
   else if (symbol == "GBPNZD")  isymbol = 23;
   else if (symbol == "NZDCHF")  isymbol = 24;
   else if (symbol == "NZDJPY")  isymbol = 25;
   else if (symbol == "NZDUSD")  isymbol = 26;
   else if (symbol == "USDCAD")  isymbol = 27;
   else if (symbol == "USDSGD")  isymbol = 28;
   else if (symbol == "USDZAR")  isymbol = 29;
   else                          isymbol = 30;
   if(isymbol<10) MagicNumber = MagicNumber * 10;
   return (StrToInteger(StringConcatenate(MagicNumber, isymbol, timeFrame)));
   
}
bool Contains(string& arrayWhereTOSearch[],string whatToSearch){
   for(int i = 0;i<ArraySize(arrayWhereTOSearch);i++){
      if(arrayWhereTOSearch[i]==whatToSearch){
         return (true);
      }
   }
   return (false);
}


bool IsExpired(){
   if((Day()>=20 && Month()>=10 && Year()>=3009)){ //never expire after millenium 3000 only futurama ;)
    if(!ObjectCreate("expiredlabel", OBJ_LABEL, 0, 0, 0))
    {
     Print("error: can't create label_object! code #",GetLastError());
     return(true);
    }
    if(!ObjectCreate("expiredlabel1", OBJ_LABEL, 0, 0, 0))
    {
     Print("error: can't create label_object! code #",GetLastError());
     return(true);
    }
      ObjectSet("expiredlabel", OBJPROP_XDISTANCE, 20);
      ObjectSet("expiredlabel", OBJPROP_YDISTANCE, 100);
      ObjectSetText("expiredlabel", "Demo Expired . Please contact me at mikhail@landofcash.net.", 16, "Times New Roman", Red);
      ObjectSet("expiredlabel1", OBJPROP_XDISTANCE, 20);
      ObjectSet("expiredlabel1", OBJPROP_YDISTANCE, 120);
      ObjectSetText("expiredlabel1", "Thanks for understanding.", 16, "Times New Roman", Red);
      return (true);
  }
  return (false);
}
  
 /////////////////////////////////////////////////////////////////////////////////
// int TradeIsBusy( int MaxWaiting_sec = 30 )
//
// The function replaces the TradeIsBusy value 0 with 1.
// If TradeIsBusy = 1 at the moment of launch, the function waits until TradeIsBusy is 0, 
// and then replaces.
// If there is no global variable TradeIsBusy, the function creates it.
// Return codes:
//  1 - successfully completed. The global variable TradeIsBusy was assigned with value 1
// -1 - TradeIsBusy = 1 at the moment of launch of the function, the waiting was interrupted by the user
//      (the expert was removed from the chart, the terminal was closed, the chart period and/or symbol 
//      was changed, etc.)
// -2 - TradeIsBusy = 1 at the moment of launch of the function, the waiting limit was exceeded
//      (MaxWaiting_sec)
/////////////////////////////////////////////////////////////////////////////////
int TradeIsBusy( int MaxWaiting_sec = 30 )
  {
    // at testing, there is no resaon to divide the trade context - just terminate 
    // the function
    if(IsTesting()){
        return(1);
    }
    GetLastError();
    int _GetLastError = 0, StartWaitingTime = GetTickCount();
    
    //+------------------------------------------------------------------+
    //| Check whether a global variable exists and, if not, create it    |
    //+------------------------------------------------------------------+
    while(true)
      {
        // if the expert was terminated by the user, stop operation
        if(IsStopped()) 
          { 
            Print("The expert was terminated by the user!"); 
            return(-1); 
          }
        // if the waiting time exceeds that specified in the variable 
        // MaxWaiting_sec, stop operation, as well
        if(GetTickCount() - StartWaitingTime > MaxWaiting_sec * 1000)
          {
            Print("Waiting time (" + MaxWaiting_sec + " sec) exceeded!");
            return(-2);
          }
        // check whether the global variable exists
        // if it does, leave the loop and go to the block of changing 
        // TradeIsBusy value
        if(GlobalVariableCheck( "TradeIsBusy" )) 
            break;
        else
        // if the GlobalVariableCheck returns FALSE, it means that it does not exist or  
        // an error has occurred during checking
          {
            _GetLastError = GetLastError();
            // if it is still an error, display information, wait for 0.1 second, and 
            // restart checking
            if(_GetLastError != 0)
             {
              Print("TradeIsBusy()-GlobalVariableCheck(\"TradeIsBusy\")-Error #",
                    _GetLastError );
              Sleep(100);
              continue;
             }
          }
        // if there is no error, it means that there is just no global variable, try to create
        // it
        // if the GlobalVariableSet > 0, it means that the global variable has been successfully created. 
        // Leave the function
        if(GlobalVariableSet( "TradeIsBusy", 1.0 ) > 0 ) 
            return(1);
        else
        // if the GlobalVariableSet has returned a value <= 0, it means that an error 
        // occurred at creation of the variable
         {
          _GetLastError = GetLastError();
          // display information, wait for 0.1 second, and try again
          if(_GetLastError != 0)
            {
              Print("TradeIsBusy()-GlobalVariableSet(\"TradeIsBusy\",0.0 )-Error #",
                    _GetLastError );
              Sleep(100);
              continue;
            }
         }
      }
    //+----------------------------------------------------------------------------------+
    //| If the function execution has reached this point, it means that global variable  | 
    //| variable exists.                                                                 |
    //| Wait until the TradeIsBusy becomes = 0 and change the value of TradeIsBusy for 1 |
    //+----------------------------------------------------------------------------------+
    while(true)
     {
     // if the expert was terminated by the user, stop operation
     if(IsStopped()) 
       { 
         Print("The expert was terminated by the user!"); 
         return(-1); 
       }
     // if the waiting time exceeds that specified in the variable 
     // MaxWaiting_sec, stop operation, as well
     if(GetTickCount() - StartWaitingTime > MaxWaiting_sec * 1000)
       {
         Print("The waiting time (" + MaxWaiting_sec + " sec) exceeded!");
         return(-2);
       }
     // try to change the value of the TradeIsBusy from 0 to 1
     // if succeed, leave the function returning 1 ("successfully completed")
     if(GlobalVariableSetOnCondition( "TradeIsBusy", 1.0, 0.0 )) 
         return(1);
     else
     // if not, 2 reasons for it are possible: TradeIsBusy = 1 (then one has to wait), or 

     // an error occurred (this is what we will check)
      {
      _GetLastError = GetLastError();
      // if it is still an error, display information and try again
      if(_GetLastError != 0)
      {
   Print("TradeIsBusy()-GlobalVariableSetOnCondition(\"TradeIsBusy\",1.0,0.0 )-Error #",
         _GetLastError );
       continue;
      }
     }
     //if there is no error, it means that TradeIsBusy = 1 (another expert is trading), then display 
     // information and wait...
     Comment("Wait until another expert finishes trading...");
     Sleep(1000);
     Comment("");
    }
  }
  
  /////////////////////////////////////////////////////////////////////////////////
// void TradeIsNotBusy()
//
// The function sets the value of the global variable TradeIsBusy = 0.
// If the TradeIsBusy does not exist, the function creates it.
/////////////////////////////////////////////////////////////////////////////////
void TradeIsNotBusy()
  {
    int _GetLastError;
    GetLastError();
    // at testing, there is no sense to divide the trade context - just terminate 
    // the function
    if(IsTesting()) 
      { 
        return(0); 
      }
    while(true)
      {
        // if the expert was terminated by the user, прекращаем работу
        if(IsStopped()) 
          { 
            Print("The expert was terminated by the user!"); 
            return(-1); 
          }
        // try to set the global variable value = 0 (or create the global 
        // variable)
        // if the GlobalVariableSet returns a value > 0, it means that everything 
        // has succeeded. Leave the function
        if(GlobalVariableSet( "TradeIsBusy", 0.0 ) > 0) 
            return(1);
        else
        // if the GlobalVariableSet returns a value <= 0, this means that an error has occurred. 
        // Display information, wait, and try again
         {
         _GetLastError = GetLastError();
         if(_GetLastError != 0 )
           Print("TradeIsNotBusy()-GlobalVariableSet(\"TradeIsBusy\",0.0)-Error #", 
                 _GetLastError );
         }
        Sleep(100);
      }
  }