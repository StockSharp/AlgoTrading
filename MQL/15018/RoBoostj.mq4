// RSI based trading robot
// logic bar>bar2 + rsi 50 level
// ILAN based half-scalping robot with modifications, especially regarding max. lots

#property copyright "HeinBollo"
#property link      "www.mql5.com"
//

extern double TakeProfit        = 10.0;
extern double PipStep           = 40.0;  //the higher, the less often robot opens additional trades 
extern double Lots              = 0.01;  //initial lot size 
extern bool   AutomaticLotCalc  = TRUE;
extern double LotExponent=1.2;   //for fast recovery it should be higher, e.g. 3.84
extern double slip              =3.0;
extern int    MaxTrades=10;     //some prefer more trades, e.g. 10
extern int    MagicNumber       = 11111; // every trade pair a different magic number
extern bool   UseEquityStop     = FALSE; //only if true, the TotalEquityRisk will be considered
extern double TotalEquityRisk   = 80;    //100 means full deposit at risk
extern double Stoploss          = 200.0;
extern bool   UseTrailingStop   = FALSE;
extern double TrailStart        = 1.0;
extern double TrailStop         = 20.0;
extern bool   UseTimeOut=FALSE; //if true, the clock will be enabled
extern double MaxSpread         =3.4;
extern double MaxTradeOpenHours=24;  //defines how long (max) in hours a trade is open until closed automatically
extern double MaxLots=0.50;   //absolute upper cap in case automatic lot calculation would result in undesirable order volumes
double        MinLots= 0.01;
//extern double BDistance=   3;      // plus how much
//extern int    BPeriod= 4;          // Bollinger period
//extern int    Deviation=   2;      // Bollinger deviation
extern int    RSIup             = 50;
extern int    RSIdown           = 50;
extern int    StartHour         = 22;
extern int    EndHour           = 2;
int           lotdecimal=2;     //2-lotsize rounded 2 digits, e.g. 0.01
double        sLot=1;   // has been 10

                        //

//

double PriceTarget,StartEquity,BuyTarget,SellTarget;
double AveragePrice,SellLimit,BuyLimit;
double LastBuyPrice,LastSellPrice,Spread;
bool flag;
string EAName= "RoBoost";
int timeprev = 0,expiration;
int NumOfTrades=0;
double iLots;
int cnt=0;
int total=0;
double Stopper= 0.0;
bool TradeNow = FALSE,LongTrade = FALSE,ShortTrade = FALSE;
int ticket;
bool NewOrdersPlaced=FALSE;
double AccountEquityHighAmt,PrevEquity;
//
int init() 
  {
   Spread=MarketInfo(Symbol(),MODE_SPREAD)*Point;
   return (0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int deinit() 
  {
   return (0);
  }
//
int start() 
  {
   double PrevCl;
   double CurrCl;
   if(AutomaticLotCalc) 
     {
      Lots=NormalizeDouble((AccountFreeMargin()/20000),2);
      MaxLots=NormalizeDouble((AccountEquity()/222),2);
     }
   if(UseTrailingStop) TrailingAlls(TrailStart,TrailStop,AveragePrice);
   if(UseTimeOut) 
     {
      if(TimeCurrent()>=expiration) 
        {
         CloseThisSymbolAll();
         Print("Closed All due to TimeOut");
        }
     }
   if(timeprev == Time[0]) return (0);
   timeprev=Time[0];
   double CurrentPairProfit=CalculateProfit();
   if(UseEquityStop) 
     {
      if(CurrentPairProfit<0.0 && MathAbs(CurrentPairProfit)>TotalEquityRisk/100.0*AccountEquityHigh()) 
        {
         CloseThisSymbolAll();
         Print("Closed All due to Stop Out");
         NewOrdersPlaced=FALSE;
        }
     }
   total=CountTrades();
   if(total==0) flag =FALSE;
   for(cnt=OrdersTotal()-1; cnt>=0; cnt--) 
     {
      if(OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES)) continue; else Print("Cannot select Order: ",GetLastError());
      if(OrderSymbol()!=Symbol() || OrderMagicNumber()!=MagicNumber) continue;
      if(OrderSymbol()==Symbol() && OrderMagicNumber()==MagicNumber) 
        {
         if(OrderType()==OP_BUY) 
           {
            LongTrade=TRUE;
            ShortTrade=FALSE;
            break;
           }
        }
      if(OrderSymbol()==Symbol() && OrderMagicNumber()==MagicNumber) 
        {
         if(OrderType()==OP_SELL) 
           {
            LongTrade=FALSE;
            ShortTrade=TRUE;
            break;
           }
        }
     }
   if(total>0 && total<MaxTrades) 
     {
      RefreshRates();
      LastBuyPrice=FindLastBuyPrice();
      LastSellPrice=FindLastSellPrice();
      if(LongTrade  &&  LastBuyPrice-Ask>=PipStep*Point && Spread<=MaxSpread &&(Hour()>=StartHour || Hour()<=EndHour)) TradeNow=TRUE;
      if(ShortTrade && Bid-LastSellPrice>=PipStep*Point && Spread<=MaxSpread &&(Hour()>=StartHour || Hour()<=EndHour)) TradeNow=TRUE;
      //////////////////////////////////////////////////////////////////////////////
      //	Insert your individual confirmation on different indicators here
      //	Idea was to use BollingerBands and then confirm RSI trade - if confirmed by second indicator, keep TradeNow = TRUE
      //	If not confirmed, you may set TradeNow = FALSE here.
      //////////////////////////////////////////////////////////////////////////////
     }
   if(total<1) 
     {
      ShortTrade= FALSE;
      LongTrade = FALSE;
      if(Spread<= MaxSpread  &&(Hour()>= StartHour || Hour() <= EndHour)) TradeNow = TRUE;
      StartEquity=AccountEquity();
     }
   if(TradeNow) 
     {
      LastBuyPrice=FindLastBuyPrice();
      LastSellPrice=FindLastSellPrice();
      if(ShortTrade) 
        {
         NumOfTrades=total;
         iLots=NormalizeDouble(Lots*MathPow(LotExponent,NumOfTrades),lotdecimal);
         if(iLots>MaxLots) 
           {
            iLots=MaxLots;
           }
         if(iLots<MinLots) 
           {
            iLots=MinLots;
           }
         RefreshRates();
         ticket=OpenPendingOrder(1,iLots,Bid,slip,Ask,Stoploss,TakeProfit,EAName+"-"+NumOfTrades,MagicNumber,0,HotPink);
         if(ticket<0) 
           {
            Print("Error: ",GetLastError());
            return (0);
           }
         LastSellPrice=FindLastSellPrice();
         TradeNow=FALSE;
         NewOrdersPlaced=TRUE;
        }
      else 
        {
         if(LongTrade) 
           {
            NumOfTrades=total;
            iLots=NormalizeDouble(Lots*MathPow(LotExponent,NumOfTrades),lotdecimal);
            if(iLots>MaxLots) 
              {
               iLots=MaxLots;
              }
            if(iLots<MinLots) 
              {
               iLots=MinLots;
              }
            RefreshRates();
            ticket=OpenPendingOrder(0,iLots,Ask,slip,Bid,Stoploss,TakeProfit,EAName+"-"+NumOfTrades,MagicNumber,0,Lime);
            if(ticket<0) 
              {
               Print("Error: ",GetLastError());
               return (0);
              }
            LastBuyPrice=FindLastBuyPrice();
            TradeNow=FALSE;
            NewOrdersPlaced=TRUE;
           }
        }
     }
   if(TradeNow && total<1) 
     {
      PrevCl = iClose(Symbol(), 0, 2);
      CurrCl = iClose(Symbol(), 0, 1);
      SellLimit= Bid;
      BuyLimit = Ask;
      if(!ShortTrade && !LongTrade) 
        {
         NumOfTrades=total;
         iLots=NormalizeDouble(Lots *(NumOfTrades+1),lotdecimal);
         //iLots = NormalizeDouble(Lots * MathPow(LotExponent, NumOfTrades), lotdecimal);
         if(iLots>MaxLots) 
           {
            iLots=MaxLots;
           }
         if(iLots<MinLots) 
           {
            iLots=MinLots;
           }
         if(PrevCl>CurrCl) 
           {
            if(iRSI(NULL,PERIOD_H1,7,PRICE_CLOSE,1)<RSIdown) 
              { //sell  >
               ticket=OpenPendingOrder(1,iLots,SellLimit,slip,SellLimit,Stoploss,TakeProfit,EAName+"-"+NumOfTrades,MagicNumber,0,HotPink);
               if(ticket<0) 
                 {
                  Print("Error: ",GetLastError());
                  return (0);
                 }
               LastBuyPrice=FindLastBuyPrice();
               NewOrdersPlaced=TRUE;
              }
              } else {
            if(iRSI(NULL,PERIOD_H1,7,PRICE_CLOSE,1)>=RSIup) 
              { //buy  <
               ticket=OpenPendingOrder(0,iLots,BuyLimit,slip,BuyLimit,Stoploss,TakeProfit,EAName+"-"+NumOfTrades,MagicNumber,0,Lime);
               if(ticket<0) 
                 {
                  Print("Error: ",GetLastError());
                  return (0);
                 }
               LastSellPrice=FindLastSellPrice();
               NewOrdersPlaced=TRUE;
              }
           }
         if(ticket>0) expiration=TimeCurrent()+60.0 *(60.0*MaxTradeOpenHours);
         TradeNow=FALSE;
        }
     }
   total=CountTrades();
   AveragePrice = 0;
   double Count = 0;
   for(cnt=OrdersTotal()-1; cnt>=0; cnt--) 
     {
      if(OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES)) continue; else Print("Cannot select Order: ",GetLastError());
      if(OrderSymbol()!=Symbol() || OrderMagicNumber()!=MagicNumber) continue;
      if(OrderSymbol()==Symbol() && OrderMagicNumber()==MagicNumber) 
        {
         if(OrderType()==OP_BUY || OrderType()==OP_SELL) 
           {
            AveragePrice+=OrderOpenPrice()*OrderLots();
            Count+=OrderLots();
           }
        }
     }
   if(total>0) AveragePrice=NormalizeDouble(AveragePrice/Count,Digits);
   if(NewOrdersPlaced) 
     {
      for(cnt=OrdersTotal()-1; cnt>=0; cnt--) 
        {
         if(OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES)) continue; else Print("Cannot select Order: ",GetLastError());
         if(OrderSymbol()!=Symbol() || OrderMagicNumber()!=MagicNumber) continue;
         if(OrderSymbol()==Symbol() && OrderMagicNumber()==MagicNumber) 
           {
            if(OrderType()==OP_BUY) 
              {
               PriceTarget= AveragePrice + TakeProfit * Point;
               BuyTarget=PriceTarget;
               Stopper=AveragePrice-Stoploss*Point;
               flag=TRUE;
              }
           }
         if(OrderSymbol()==Symbol() && OrderMagicNumber()==MagicNumber) 
           {
            if(OrderType()==OP_SELL) 
              {
               PriceTarget= AveragePrice - TakeProfit * Point;
               SellTarget = PriceTarget;
               Stopper=AveragePrice+Stoploss*Point;
               flag=TRUE;
              }
           }
        }
     }
   if(NewOrdersPlaced) 
     {
      if(flag == TRUE) 
        {
         for(cnt=OrdersTotal()-1; cnt>=0; cnt--) 
           {
            if(OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES)) continue; else Print("Cannot select Order: ",GetLastError());
            if(OrderSymbol()!=Symbol() || OrderMagicNumber()!=MagicNumber) continue;
            if(OrderSymbol()==Symbol() && OrderMagicNumber()==MagicNumber) if(OrderModify(OrderTicket(),AveragePrice,OrderStopLoss(),PriceTarget,0,Yellow));
            else Print("Cannot modify Order: ",GetLastError());
            NewOrdersPlaced=FALSE;
           }
        }
     }
   return (0);
  }
//Counting Trades
int CountTrades() 
  {
   int count=0;
   for(int trade=OrdersTotal()-1; trade>=0; trade--) 
     {
      if(OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES)) continue; else Print("Cannot select Order: ",GetLastError());
      if(OrderSymbol()!=Symbol() || OrderMagicNumber()!=MagicNumber) continue;
      if(OrderSymbol()==Symbol() && OrderMagicNumber()==MagicNumber)
         if(OrderType()==OP_SELL || OrderType()==OP_BUY) count++;
     }
   return (count);
  }
//

void CloseThisSymbolAll() 
  {
   for(int trade=OrdersTotal()-1; trade>=0; trade--) 
     {
      if(OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES)) continue; else Print("Cannot select Order: ",GetLastError());
      if(OrderSymbol()==Symbol()) 
        {
         if(OrderSymbol()==Symbol() && OrderMagicNumber()==MagicNumber) 
           {
            if(OrderType() == OP_BUY) if(OrderClose(OrderTicket(), OrderLots(), Bid, slip, Blue)); else Print("Cannot Close Order: ", GetLastError());
            if(OrderType() == OP_SELL) if(OrderClose(OrderTicket(), OrderLots(), Ask, slip, Red)); else Print("Cannot Close Order: ", GetLastError());
           }
         Sleep(1000);
        }
     }
  }
//Open Order section
int OpenPendingOrder(int pType,double pLots,double pPrice,int pSlippage,double ad_24,int ai_32,int ai_36,string a_comment_40,int a_magic_48,int a_datetime_52,color a_color_56) 
  {
   int l_ticket_60= 0;
   int l_error_64 = 0;
   int l_count_68 = 0;
   int li_72=100;
   switch(pType) 
     {
      case 2:
         for(l_count_68=0; l_count_68<li_72; l_count_68++) 
           {

            if(AccountBalance()-AccountEquity()>0)
              {
               sLot = MarketInfo(Symbol(),MODE_TICKVALUE);
               pLots=(AccountBalance()-AccountEquity())/(sLot)/TakeProfit;
               if(pLots>MaxLots) 
                 {
                  pLots=MaxLots;
                 }
               if(pLots<MinLots) 
                 {
                  pLots=MinLots;
                 }
               Print("pLots: ",pLots);
              }

            l_ticket_60= OrderSend(Symbol(),OP_BUYLIMIT,pLots,pPrice,pSlippage,StopLong(ad_24,ai_32),TakeLong(pPrice,ai_36),a_comment_40,a_magic_48,a_datetime_52,a_color_56);
            l_error_64 = GetLastError();
            if(l_error_64==0/* NO_ERROR */) break;
            if(!(l_error_64==4/* SERVER_BUSY */ || l_error_64==137/* BROKER_BUSY */ || l_error_64==146/* TRADE_CONTEXT_BUSY */ || l_error_64==136/* OFF_QUOTES */)) break;
            Sleep(1000);
           }
         break;
      case 4:
         for(l_count_68=0; l_count_68<li_72; l_count_68++) 
           {

            if(AccountBalance()-AccountEquity()>0)
              {
               sLot = MarketInfo(Symbol(),MODE_TICKVALUE);
               pLots=(AccountBalance()-AccountEquity())/(sLot)/TakeProfit;
               if(pLots>MaxLots) 
                 {
                  pLots=MaxLots;
                 }
               if(pLots<MinLots) 
                 {
                  pLots=MinLots;
                 }
               Print("pLots: ",pLots);
              }

            l_ticket_60= OrderSend(Symbol(),OP_BUYSTOP,pLots,pPrice,pSlippage,StopLong(ad_24,ai_32),TakeLong(pPrice,ai_36),a_comment_40,a_magic_48,a_datetime_52,a_color_56);
            l_error_64 = GetLastError();
            if(l_error_64==0/* NO_ERROR */) break;
            if(!(l_error_64==4/* SERVER_BUSY */ || l_error_64==137/* BROKER_BUSY */ || l_error_64==146/* TRADE_CONTEXT_BUSY */ || l_error_64==136/* OFF_QUOTES */)) break;
            Sleep(5000);
           }
         break;
      case 0:
         for(l_count_68=0; l_count_68<li_72; l_count_68++) 
           {
            RefreshRates();

            if(AccountBalance()-AccountEquity()>0)
              {
               sLot = MarketInfo(Symbol(),MODE_TICKVALUE);
               pLots=(AccountBalance()-AccountEquity())/(sLot)/TakeProfit;
               if(pLots>MaxLots) 
                 {
                  pLots=MaxLots;
                 }
               if(pLots<MinLots) 
                 {
                  pLots=MinLots;
                 }
               Print("pLots: ",pLots);
              }

            l_ticket_60= OrderSend(Symbol(),OP_BUY,pLots,Ask,pSlippage,StopLong(Bid,ai_32),TakeLong(Ask,ai_36),a_comment_40,a_magic_48,a_datetime_52,a_color_56);
            l_error_64 = GetLastError();
            if(l_error_64==0/* NO_ERROR */) break;
            if(!(l_error_64==4/* SERVER_BUSY */ || l_error_64==137/* BROKER_BUSY */ || l_error_64==146/* TRADE_CONTEXT_BUSY */ || l_error_64==136/* OFF_QUOTES */)) break;
            Sleep(5000);
           }
         break;
      case 3:
         for(l_count_68=0; l_count_68<li_72; l_count_68++) 
           {

            if(AccountBalance()-AccountEquity()>0)
              {
               sLot = MarketInfo(Symbol(),MODE_TICKVALUE);
               pLots=(AccountBalance()-AccountEquity())/(sLot)/TakeProfit;
               if(pLots>MaxLots) 
                 {
                  pLots=MaxLots;
                 }
               if(pLots<MinLots) 
                 {
                  pLots=MinLots;
                 }
               Print("pLots: ",pLots);
              }

            l_ticket_60= OrderSend(Symbol(),OP_SELLLIMIT,pLots,pPrice,pSlippage,StopShort(ad_24,ai_32),TakeShort(pPrice,ai_36),a_comment_40,a_magic_48,a_datetime_52,a_color_56);
            l_error_64 = GetLastError();
            if(l_error_64==0/* NO_ERROR */) break;
            if(!(l_error_64==4/* SERVER_BUSY */ || l_error_64==137/* BROKER_BUSY */ || l_error_64==146/* TRADE_CONTEXT_BUSY */ || l_error_64==136/* OFF_QUOTES */)) break;
            Sleep(5000);
           }
         break;
      case 5:
         for(l_count_68=0; l_count_68<li_72; l_count_68++) 
           {

            if(AccountBalance()-AccountEquity()>0)
              {
               sLot = MarketInfo(Symbol(),MODE_TICKVALUE);
               pLots=(AccountBalance()-AccountEquity())/(sLot)/TakeProfit;
               if(pLots>MaxLots) 
                 {
                  pLots=MaxLots;
                 }
               if(pLots<MinLots) 
                 {
                  pLots=MinLots;
                 }
               Print("pLots: ",pLots);
              }

            l_ticket_60= OrderSend(Symbol(),OP_SELLSTOP,pLots,pPrice,pSlippage,StopShort(ad_24,ai_32),TakeShort(pPrice,ai_36),a_comment_40,a_magic_48,a_datetime_52,a_color_56);
            l_error_64 = GetLastError();
            if(l_error_64==0/* NO_ERROR */) break;
            if(!(l_error_64==4/* SERVER_BUSY */ || l_error_64==137/* BROKER_BUSY */ || l_error_64==146/* TRADE_CONTEXT_BUSY */ || l_error_64==136/* OFF_QUOTES */)) break;
            Sleep(5000);
           }
         break;
      case 1:
         for(l_count_68=0; l_count_68<li_72; l_count_68++) 
           {

            if(AccountBalance()-AccountEquity()>0)
              {
               sLot = MarketInfo(Symbol(),MODE_TICKVALUE);
               pLots=(AccountBalance()-AccountEquity())/(sLot)/TakeProfit;
               if(pLots>MaxLots) 
                 {
                  pLots=MaxLots;
                 }
               if(pLots<MinLots) 
                 {
                  pLots=MinLots;
                 }
               Print("pLots: ",pLots);
              }

            l_ticket_60= OrderSend(Symbol(),OP_SELL,pLots,Bid,pSlippage,StopShort(Ask,ai_32),TakeShort(Bid,ai_36),a_comment_40,a_magic_48,a_datetime_52,a_color_56);
            l_error_64 = GetLastError();
            if(l_error_64==0/* NO_ERROR */) break;
            if(!(l_error_64==4/* SERVER_BUSY */ || l_error_64==137/* BROKER_BUSY */ || l_error_64==146/* TRADE_CONTEXT_BUSY */ || l_error_64==136/* OFF_QUOTES */)) break;
            Sleep(5000);
           }
     }
   return (l_ticket_60);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double StopLong(double ad_0,int ai_8) 
  {
   if(ai_8 == 0) return (0);
   else return (ad_0 - ai_8 * Point);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double StopShort(double ad_0,int ai_8) 
  {
   if(ai_8 == 0) return (0);
   else return (ad_0 + ai_8 * Point);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double TakeLong(double ad_0,int ai_8) 
  {
   if(ai_8 == 0) return (0);
   else return (ad_0 + ai_8 * Point);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double TakeShort(double ad_0,int ai_8) 
  {
   if(ai_8 == 0) return (0);
   else return (ad_0 - ai_8 * Point);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double CalculateProfit() 
  {
   double ld_ret_0=0;
   for(cnt=OrdersTotal()-1; cnt>=0; cnt--) 
     {
      if(OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES)) continue; else Print("Cannot select Order: ",GetLastError());
      if(OrderSymbol()!=Symbol() || OrderMagicNumber()!=MagicNumber) continue;
      if(OrderSymbol()==Symbol() && OrderMagicNumber()==MagicNumber)
         if(OrderType()==OP_BUY || OrderType()==OP_SELL) ld_ret_0+=OrderProfit();
     }
   return (ld_ret_0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void TrailingAlls(int pType,int ai_4,double a_price_8) 
  {
   int l_ticket_16;
   double l_ord_stoploss_20;
   double l_price_28;
   if(ai_4!=0) 
     {
      for(int l_pos_36=OrdersTotal()-1; l_pos_36>=0; l_pos_36--) 
        {
         if(OrderSelect(l_pos_36,SELECT_BY_POS,MODE_TRADES)) 
           {
            if(OrderSymbol()!=Symbol() || OrderMagicNumber()!=MagicNumber) continue;
            if(OrderSymbol()==Symbol() || OrderMagicNumber()==MagicNumber) 
              {
               if(OrderType()==OP_BUY) 
                 {
                  l_ticket_16= NormalizeDouble((Bid - a_price_8) / Point, 0);
                  if(l_ticket_16<pType) continue;
                  l_ord_stoploss_20=OrderStopLoss();
                  l_price_28=Bid-ai_4*Point;
                  if(l_ord_stoploss_20==0.0 || (l_ord_stoploss_20!=0.0 && l_price_28>l_ord_stoploss_20)) if(OrderModify(OrderTicket(),a_price_8,l_price_28,OrderTakeProfit(),0,Aqua));
                  else Print("Cannot modify Order: ",GetLastError());
                 }
               if(OrderType()==OP_SELL) 
                 {
                  l_ticket_16= NormalizeDouble((a_price_8 - Ask) / Point, 0);
                  if(l_ticket_16<pType) continue;
                  l_ord_stoploss_20=OrderStopLoss();
                  l_price_28=Ask+ai_4*Point;
                  if(l_ord_stoploss_20==0.0 || (l_ord_stoploss_20!=0.0 && l_price_28<l_ord_stoploss_20))
                     if(OrderModify(OrderTicket(),a_price_8,l_price_28,OrderTakeProfit(),0,Red));
                  else Print("Cannot modify Order: ",GetLastError());
                 }
              }
            Sleep(1000);
           }
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double AccountEquityHigh() 
  {
   if(CountTrades()==0) AccountEquityHighAmt=AccountEquity();
   if(AccountEquityHighAmt<PrevEquity) AccountEquityHighAmt=PrevEquity;
   else AccountEquityHighAmt=AccountEquity();
   PrevEquity=AccountEquity();
   return (AccountEquityHighAmt);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double FindLastBuyPrice() 
  {
   double l_ord_open_price_8;
   int l_ticket_24;
   double ld_unused_0=0;
   int l_ticket_20=0;
   for(int l_pos_16=OrdersTotal()-1; l_pos_16>=0; l_pos_16--) 
     {
      if(OrderSelect(l_pos_16,SELECT_BY_POS,MODE_TRADES)) continue; else Print("Cannot select Order: ",GetLastError());
      if(OrderSymbol()!=Symbol() || OrderMagicNumber()!=MagicNumber) continue;
      if(OrderSymbol()==Symbol() && OrderMagicNumber()==MagicNumber && OrderType()==OP_BUY) 
        {
         l_ticket_24=OrderTicket();
         if(l_ticket_24>l_ticket_20) 
           {
            l_ord_open_price_8=OrderOpenPrice();
            ld_unused_0 = l_ord_open_price_8;
            l_ticket_20 = l_ticket_24;
           }
        }
     }
   return (l_ord_open_price_8);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double FindLastSellPrice() 
  {
   double l_ord_open_price_8;
   int l_ticket_24;
   double ld_unused_0=0;
   int l_ticket_20=0;
   for(int l_pos_16=OrdersTotal()-1; l_pos_16>=0; l_pos_16--) 
     {
      if(OrderSelect(l_pos_16,SELECT_BY_POS,MODE_TRADES)) continue; else Print("Cannot select Order: ",GetLastError());
      if(OrderSymbol()!=Symbol() || OrderMagicNumber()!=MagicNumber) continue;
      if(OrderSymbol()==Symbol() && OrderMagicNumber()==MagicNumber && OrderType()==OP_SELL) 
        {
         l_ticket_24=OrderTicket();
         if(l_ticket_24>l_ticket_20) 
           {
            l_ord_open_price_8=OrderOpenPrice();
            ld_unused_0 = l_ord_open_price_8;
            l_ticket_20 = l_ticket_24;
           }
        }
     }
   return (l_ord_open_price_8);
  }
//+------------------------------------------------------------------+
