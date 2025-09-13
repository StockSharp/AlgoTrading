#property copyright "trevone"
#property link "http://www.mql4.com/users/trevone"  
extern string MAGIC_Description = "If MAGIC = 0 then basket trade identification is done using the Symbol and not the MAGIC number"; 
extern int MAGIC = 0;
extern string LotPrecision_Description = "If your lot increments are 0.1, 0.2, 0.3 etc. then set to 1 otherwise leave as 2. If the EA does not open trades then set to 1"; 
extern int LotPrecision = 2;
extern string FixedMargin_Description = "Some brokers fix the margin requirement per 0.01 e.g. AAAFX for ZuluTrade is 5USD per 0.01, otherwise leave as 0";
extern int FixedMargin = 0;
extern string StartEndHour_Description = "If StartHour is less than EndHour then trade between those times otherwise trade outside of those times";
extern int SignalAStartHour = 0;
extern int SignalAEndHour = 23; 
extern string RefreshHours_Description = "Resets its profit counter at this interval and limits further trading for that duration if the profit exeeds MilestoneGrowth of the AccountBalance";
extern int RefreshHours = 24; 
extern string TradeTime_Description = "Wait this number of seconds before any more trading";
extern int TradeTime = 1800;  
extern string MarginUsage_Description = "Use this amount of the AccountBalance for the Front System lotsize calculation";
extern double MarginUsage = 0.15;
extern string MinStop_Description = "If the trailing stop price is past this point then it will place the stop. 0 is the safest, 5 means 5pips and garantees a profit from each trade";
extern double MinStop = 0;
extern string MaxStop_Description = "If the prices passes this point, trailing will not continue giving the EA a chance to catch up to the exponential growth";
extern double MaxStop = 15;
extern string TrailingStop_Description = "Well if you dont know what this is Invespedia can tell you this";
extern int TrailingStop = 7;
extern string BasketProfit_Description = "If we are doing well and above the exponent then baskets are closed in profit at this value of the balance";
extern double BasketProfit = 1.05;
extern string BasketBoost_Description = "If we are not doing so well and below the exponent then multiply the BasketProfit by this amount to catch up";
extern double BasketBoost = 1.1;
extern string ExponentialGrowth_Description = "Every RefreshHours we need to make this amount of the balance or else...";
extern double ExponentialGrowth = 0.01;  
extern string MinCandle_Description = "Only signal entries if the candle is bigger than this in pips"; 
extern double MinCandle = 5;
extern string MaxCandle_Description = "Only signal entries if the candle is smaller than this in pips"; 
extern double MaxCandle = 10;
extern string MaxAtr_Description = "Only signal entries if the ATR is smaller than this in pips"; 
extern double MaxAtr = 10; 
extern string TrendBars_Description = "The period to calculate a trend signal from the RSI"; 
extern int TrendBars = 60;
extern string ATRPeriod_Description = "The averaging period for calculating the ATR"; 
extern int ATRPeriod = 14;   
extern string ATRShiftCheck_Description = "Shift-check the ATR to make sure its moving up or down"; 
extern int ATRShiftCheck = 1;   
extern string RSIPeriod_Description = "The averaging period for calculating the RSI"; 
extern int RSIPeriod = 14; 
extern string RSIShiftCheck_Description = "Shift-check the RSI to make sure its moving up or down"; 
extern int RSIShiftCheck = 1;   
extern string RSIUpperLevel_Description = "Only signal entries if the RSI is above this value"; 
extern double RSIUpperLevel = 50; 
extern string RSILowerLevel_Description = "Only signal entries if the RSI is below this value"; 
extern double RSILowerLevel = 50;    

double eRSICur, eRSIPrev, eATRCur, spread, pipPoints, slippage, buyLots, sellLots, marginRequirement, lotSize, digits, exponentGrowth, totalTrades, postionsStopPrice, eATRPrev, accountBalance, totalProfit, totalLoss, accountEquity, exponent, startBalance;

int MaxTrades = 3;  
int MaxUnprotected = 1;
int TotalTrades = 5; 
int ATRTimeFrame = 0;
int RSITimeFrame = 0;  
int ATRShift = 0;   
int RSIShift = 0;  
int lastTradeTime = 0; 
int tradeCount = 0;
int lastExponentTime = 0;
int totalTargets = 0;
int turn = 0;
int totalCycles = 0;  
int profitType = 0;
int openType = -1;
int totalUnprotected = 0;

double ExponentQuality = 1;
double BaseLotSize = 0.01;
double DynamicSlippage = 1; 
double buyLimitPrice = 0;
double sellLimitPrice = 0;
double buyStopPrice = 0;
double sellStopPrice = 0; 
double openStopPrice = 0;
 
bool RSIBull = false;
bool RSIBear = false;

void init(){
   startBalance = AccountBalance();
   accountBalance = AccountBalance();
   accountEquity = AccountEquity();
   startBalance = accountBalance;
   exponent = accountBalance; 
   setPipPoint(); 
   if( Symbol() != "GBPJPY" || Period() != 5 ) Comment( "This piece of magic is curve-fitted for GBPJPY 5M only" );
}

void closeAll( string type = "none" ){   
   for( int i = OrdersTotal() - 1; i >= 0; i-- ) {
      if( OrderSelect( i, SELECT_BY_POS, MODE_TRADES ) == false ) break;
      if( ( MAGIC > 0 && OrderMagicNumber() == MAGIC ) || OrderSymbol() == Symbol() ){  
         if( ( OrderProfit() > 0 && type == "profits" ) || type == "none" ){
            RefreshRates();
            if( OrderType() == OP_BUY ){ 
               OrderClose( OrderTicket(), OrderLots(), Bid, slippage );
               if( OrderSymbol() == Symbol() ) exponentGrowth = exponentGrowth + OrderProfit();  
            }
            if( OrderType() == OP_SELL ) {
               OrderClose( OrderTicket(), OrderLots(), Ask, slippage );
               if( OrderSymbol() == Symbol() ) exponentGrowth = exponentGrowth + OrderProfit();  
            }
         }
      }
   }
}  

void exponent(){ 
   if( MathMod( TimeCurrent(), 3600 * RefreshHours ) <= 10 ){ 
      lastExponentTime = 1;
      if( turn == 0 ) totalCycles = totalCycles + 1;
      turn = 1;  
      if( exponentGrowth / accountBalance > ExponentialGrowth && lastExponentTime == 0 ) {  
         totalTargets = totalTargets + 1;
         lastExponentTime = 1; 
         if( accountBalance / exponent > ExponentQuality ){
            startBalance = AccountBalance();
            exponent = AccountBalance();  
         }
      } 
      exponentGrowth = 0;    
   } else {
      turn = 0;
      lastExponentTime = TimeCurrent(); 
   }    
} 

void setPipPoint(){
   digits = MarketInfo( Symbol(), MODE_DIGITS );
   if( digits == 3 ) pipPoints = 0.01;
   else if( digits == 5 ) pipPoints = 0.0001;
} 

void prepareExponent(){  
   exponent = startBalance * MathPow( 1 + ExponentialGrowth,  totalCycles ); 
} 

double marginCalculate( string symbol, double volume ){ 
   if( FixedMargin > 0 ) return ( FixedMargin );
   else return ( MarketInfo( symbol, MODE_MARGINREQUIRED ) * volume ) ; 
} 

void lotSize(){  
   spread = ( Ask - Bid ) / pipPoints;
   slippage = NormalizeDouble( ( spread / pipPoints ) * DynamicSlippage, 1 );
   marginRequirement = marginCalculate( Symbol(), BaseLotSize ); 
   lotSize = NormalizeDouble( ( AccountFreeMargin() * MarginUsage / marginRequirement ) * BaseLotSize, LotPrecision ); 
   if( LotPrecision == 2 && lotSize < 0.01 ) lotSize = 0.01;
   else if( LotPrecision == 1 && lotSize < 0.1 ) lotSize = 0.1;
}   

void preparePositions() { 
   totalTrades = 0; 
   totalProfit = 0;
   totalLoss = 0;
   buyLots = 0;
   sellLots = 0;
   openType = -1;
   totalUnprotected  = 0;
   for( int i = 0; i < OrdersTotal(); i++ ) {
      if( OrderSelect( i, SELECT_BY_POS, MODE_TRADES ) == false ) break;   
      if( ( MAGIC > 0 && OrderMagicNumber() == MAGIC ) || OrderSymbol() == Symbol() )  { 
         totalTrades = totalTrades + 1; 
         if( OrderType() == OP_BUY ) {
            buyLots = buyLots + OrderLots(); 
            openType = OP_BUY;
         } else if( OrderType() == OP_SELL ) {
            sellLots = sellLots + OrderLots(); 
            openType = OP_SELL;
         }
         if( OrderProfit() > 0 ) totalProfit = totalProfit + OrderProfit();
         else totalLoss = totalLoss + OrderProfit();   
      }
      if( OrderStopLoss() == 0 ) totalUnprotected = totalUnprotected + 1; 
   }  
   accountBalance = AccountBalance();
   accountEquity = accountBalance + totalProfit + totalLoss;    
}  

void manageStops(){ 
   string trailSymbol = Symbol();  
   for( int i = 1; i <= OrdersTotal(); i++ ) {
      if ( OrderSelect( i - 1, SELECT_BY_POS ) == true ) {  
         int trailOrderType = OrderType(); 
         if( OrderSymbol() != trailSymbol || trailOrderType > 1 || OrderProfit() < 0 ) continue; 
         double trailStopLoss = OrderStopLoss();   
         while( true ) {
            double trailStop = TrailingStop * 10; 
            int trailDistance = MarketInfo( trailSymbol, MODE_STOPLEVEL ); 
            if( trailStop < trailDistance ) trailStop = trailDistance;  
            bool modifyTrailingStop = false;  
            switch( trailOrderType ) {
               case 0 : 
                  if( NormalizeDouble( trailStopLoss, Digits ) < NormalizeDouble( Bid - trailStop * Point,Digits ) ) {
                     trailStopLoss = Bid - trailStop * Point;   
                     modifyTrailingStop = true;       
                  }
                  break;    
               case 1 :                      
                  if( NormalizeDouble( trailStopLoss, Digits ) > NormalizeDouble( Ask + trailStop * Point, Digits ) || NormalizeDouble( trailStopLoss, Digits ) == 0 ) {
                     trailStopLoss = Ask + trailStop * Point;             
                     modifyTrailingStop = true;             
                  }
               }                               
            if( modifyTrailingStop == false ) break;                       
            double trailTakeProfit = OrderTakeProfit(); 
            double trailPrice = OrderOpenPrice();  
            int trailTicket = OrderTicket();   
            bool response = false;
            if( ( OrderType() == OP_BUY && trailStopLoss > trailPrice && trailStopLoss < OrderOpenPrice() + ( MaxStop * pipPoints ) ) || ( OrderType() == OP_SELL && trailStopLoss < trailPrice && trailStopLoss > OrderOpenPrice() - ( MaxStop * pipPoints ) ) ) response = OrderModify( trailTicket, trailPrice, trailStopLoss, trailTakeProfit, 0 );           
            break; 
           } 
        }  
     }  
   return;   
} 

void openPosition(){ 
   if( ( ( SignalAStartHour < SignalAEndHour && Hour() >= SignalAStartHour && Hour() < SignalAEndHour ) || ( SignalAStartHour > SignalAEndHour && ( ( Hour() <= SignalAEndHour && Hour() >= 0 ) || ( Hour() <= 23 && Hour() >= SignalAStartHour ) ) ) ) ){
      if( eATRCur < ( MaxAtr * pipPoints ) && eATRCur > eATRPrev ){ 
         if( iVolume( NULL, 0, 1 ) > iVolume( NULL, 0, 2 ) ){
            if( RSIBull && eRSICur > eRSIPrev && eRSICur <  RSILowerLevel && MathAbs( Open[1] - Close[1] ) > ( MinCandle * pipPoints ) && MathAbs( Open[1] - Close[1] ) < ( MaxCandle * pipPoints ) ) {
               if( AccountFreeMarginCheck( Symbol(), OP_BUY, lotSize ) <= 0 || GetLastError() == 134 ) return;
               OrderSend( Symbol(), OP_BUY, lotSize, Ask, slippage, 0, 0, "MadTrader", MAGIC );    
               tradeCount = tradeCount + 1; 
               lastTradeTime = TimeCurrent();  
            } else if( RSIBear && eRSICur < eRSIPrev && eRSICur > RSIUpperLevel && MathAbs( Open[1] - Close[1] ) > ( MinCandle * pipPoints ) && MathAbs( Open[1] - Close[1] ) < ( MaxCandle * pipPoints ) ) {    
               if( AccountFreeMarginCheck( Symbol(), OP_BUY, lotSize ) <= 0 || GetLastError() == 134 ) return;
               OrderSend( Symbol(), OP_SELL, lotSize, Bid, slippage, 0, 0, "MadTrader", MAGIC );   
               tradeCount = tradeCount + 1; 
               lastTradeTime = TimeCurrent(); 
            }    
            if( accountEquity / exponent < ExponentQuality ) profitType = 1;
            else profitType = 2;
         }
      }
   } 
}   

void prepareRSI(){
   RSIBull = false;
   RSIBear = false;
   int rsiCountBull = 0;
   int rsiCountBear = 0;
   for( int i = 0; i < TrendBars; i++ ){
      double eRSICur1 = iRSI( NULL, RSITimeFrame, RSIPeriod, PRICE_CLOSE, i );
      if( eRSICur1 > 50 ) rsiCountBull = rsiCountBull + 1; 
      if( eRSICur1 < 50 ) rsiCountBear = rsiCountBear + 1; 
   }
   if( rsiCountBull > rsiCountBear ) RSIBull = true; 
   if( rsiCountBear > rsiCountBull ) RSIBear = true; 
}  

void prepareIndicators(){ 
   eATRCur = iATR( NULL, ATRTimeFrame, ATRPeriod, ATRShift );   
   eATRPrev = iATR( NULL, ATRTimeFrame, ATRPeriod, ATRShift + ATRShiftCheck );  
   eRSICur = iRSI( NULL, RSITimeFrame, RSIPeriod, PRICE_CLOSE, RSIShift );
   eRSIPrev = iRSI( NULL, RSITimeFrame, RSIPeriod, PRICE_CLOSE, RSIShift + RSIShiftCheck );   
   prepareRSI();
}   

void prepare(){ 
   prepareExponent(); 
   prepareIndicators();
   preparePositions();  
   exponent();
   lotSize(); 
} 

void start(){
   if( Symbol() == "GBPJPY" && Period() == 5 ){
      prepare();  
      if( tradeCount < TotalTrades && totalUnprotected < MaxUnprotected && TimeCurrent() - lastTradeTime > TradeTime ) openPosition();  
      if( profitType == 1 && ( accountEquity / exponent > ExponentQuality || AccountEquity() / AccountBalance() > BasketProfit * BasketBoost ) ) closeAll();  
      else if( profitType == 2 && AccountEquity() / AccountBalance() > BasketProfit ) closeAll();   
      if( tradeCount >= TotalTrades ) tradeCount = 0;
      manageStops(); 
   } else Comment( "This piece of magic is curve-fitted for GBPJPY 5M only" );   
}