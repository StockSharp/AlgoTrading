#property copyright "Trevor Schuil"
#property link "EA Template"
#define MAGIC 20130719
extern string TOOLS = ".............................................................................................................";
extern bool CloseAll = false;
extern bool ContinueTrading = true;
extern bool BackupSystem = true; 
extern double DynamicSlippage = 1;   
extern double BaseLotSize = 0.01;
extern string REWARD = ".............................................................................................................";
extern int QueryHistory = 12; 
extern double BasketProfit = 3;
extern double OpenProfit = 3.0; 
extern double MinProfit = 0.5;    
extern string RISK = ".............................................................................................................";
extern bool AccumulateMiles = true;
extern double AccountLimit = 50;
extern double AccountReserve = 25;
extern int MaxStartTrades = 2;
extern int MaxBackupTrades = 3;
extern int MaxBasketTrades = 3;
extern double MarginUsage = 0.05;
extern double TradeSpace = 2;
extern string INDICATOR_ATR = ".............................................................................................................";
extern int ATRTimeFrame = 0;
extern int ATRPeriod = 14;
extern int ATRShift = 0; 
extern int ATRShiftCheck = 2; 
extern string INDICATOR_ADX = ".............................................................................................................";
extern double ADXMain = 30; 
extern int ADXTimeFrame = 0;
extern int ADXPeriod = 14;
extern int ADXShift = 0; 
extern int ADXShiftCheck = 2; 
extern string INDICATOR_MA = ".............................................................................................................";
extern int MATimeFrame = 0; 
extern int MAPeriod = 22;
extern int MMAShift = 0;
extern int MAShift = 0;
extern int MAShiftCheck = 2; 
extern string TIME = ".............................................................................................................";
extern int StartCycle = 99; 


double slippage, marginRequirement, lotSize, totalHistoryProfit, totalProfit, totalLoss, symbolHistory,
eATR, eATRPrev, eADXMain, eADXPlusDi, eADXMinusDi, eADXMainPrev, eADXPlusDiPrev, eADXMinusDiPrev, MA3Cur, MA3Prev;

int digits, totalTrades, totalBackupTrades;
double buyLots = 0;
double sellLots = 0;
int totalHistory = 100;
double pipPoints = 0.00010;  
bool nearLongPosition = false;
bool nearShortPosition = false; 
double drawdown = 0; 
bool rangingMarket = false;
bool bullish = false;
bool bearish = false;
string display = "\n"; 
int basno = 0;
int bsnotype = -1;
int bscount = -1;
int startDay = 0;
 
double fractalUpPrice = 0;
double fractalDownPrice = 0;

double milestoneCount = 0;
double milestoneEquity = 0;
double milestoneBalance = 0;
double nextMilestone = 0;
double prevMilestone = 0;
double accumulatedEquity = 0;
double milestoneLoss = 0;

double mileEquity = 0;

int init(){ 
   startDay = DayOfYear();
   
   nextMilestone = AccountEquity() + AccountLimit;
   prevMilestone = AccountEquity();
   accumulatedEquity = AccountEquity() - AccountReserve;
   
   prepare(); 
   
   /*
   
   int handle=FileOpen("filename.csv", FILE_CSV|FILE_WRITE, ';');
  if(handle>0)
    {
     FileWrite(handle, nextMilestone, prevMilestone, accumulatedEquity);
     FileClose(handle);
    } 
  string str1, str2, str3;
  double val1, val2, val3;
  handle=FileOpen("filename.csv", FILE_CSV|FILE_READ);
  if(handle>0)
    { 
     str1=FileReadString(handle);
     val1 = StrToDouble(str1);
     str2=FileReadString(handle);
     val2 = StrToDouble(str2);
     str3=FileReadString(handle);
     val3 = StrToDouble(str3);
     FileClose(handle);
    }
   Print(str1, str2,str3);
   Print(val1+val2+val3);
*/
   return( 0 );
}

double marginCalculate( string symbol, double volume ){ 
   return ( MarketInfo( symbol, MODE_MARGINREQUIRED ) * volume ) ; 
} 

void lotSize(){ 
   slippage = NormalizeDouble( ( eATR / pipPoints ) * DynamicSlippage, 1 );
   marginRequirement = marginCalculate( Symbol(), BaseLotSize ); 
   lotSize = NormalizeDouble( ( ( AccountFreeMargin() - accumulatedEquity ) * MarginUsage / marginRequirement ) * BaseLotSize, 2 ) ; 
   if( lotSize < 0.01 ) lotSize = 0.01; 
} 

void setPipPoint(){
   digits = MarketInfo( Symbol(), MODE_DIGITS );
   if( digits == 3 ) pipPoints = 0.010;
   else if( digits == 5 ) pipPoints = 0.00010;
} 

void closeAll( string type = "none" ){
   for( int i = 0; i < OrdersTotal(); i++ ) {
   if( OrderSelect( i, SELECT_BY_POS, MODE_TRADES ) == false ) break;
      if( OrderSymbol() == Symbol() ){ 
         RefreshRates();
         if( ( OrderStopLoss() == 0 && OrderProfit() > 0 && type == "profits" ) || type == "none" ){
            if( OrderType() == OP_BUY ) OrderClose( OrderTicket(), OrderLots(), Bid, slippage );
            if( OrderType() == OP_SELL ) OrderClose( OrderTicket(), OrderLots(), Ask, slippage );
         }
      }
   }
} 

void update(){
   display = "";
   display = display + " Trade Space: " + DoubleToStr( TradeSpace * eATR / pipPoints, 1 ) + "pips";  
   display = display + " Lot Size: " + DoubleToStr( lotSize, 2 );  
   display = display + " Draw Down: " + DoubleToStr( drawdown, 2 );
   display = display + " Open Trades: " + DoubleToStr( totalTrades, 0 ) + " (" + DoubleToStr( MaxStartTrades, 0 ) + ")";  
   display = display + " Profit: " + DoubleToStr( totalProfit, 2 );
   display = display + " Loss: " + DoubleToStr( totalLoss, 2 );
   display = display + " History: " + DoubleToStr( totalHistoryProfit, 2 ); 
   display = display + " Ranging: " + DoubleToStr( rangingMarket, 0 ); 
   display = display + " Bullish: " + DoubleToStr( bullish, 0 ) ;
   display = display + " Bearish: " + DoubleToStr( bearish, 0 ); 
   display = display + " Diff: " + DoubleToStr( ( MathAbs( High[0] - Low[0] ) - MathAbs( Open[0] - Close[0] ) ) / pipPoints, 2 ); 
   display = display + " Basno: " + DoubleToStr( basno, 0 ); 
   display = display + " bscount: " + DoubleToStr( bscount, 0 );
   display = display + "\n prevMilestone: " + DoubleToStr( prevMilestone, 0 );
   display = display + " nextMilestone: " + DoubleToStr( nextMilestone, 0 );
   display = display + " AccountEquity: " + DoubleToStr( AccountEquity(), 2 );
   display = display + " accumulatedEquity: " + DoubleToStr( accumulatedEquity, 2 );
   display = display + " mileEquity: " + DoubleToStr( mileEquity, 2 );
   display = display + " useMargin: " + DoubleToStr( AccountFreeMargin() - accumulatedEquity, 2 );
   display = display + " milestoneCount: " + DoubleToStr( milestoneCount, 0 ); 
   display = display + " milestoneLoss: " + DoubleToStr( milestoneLoss, 0 ); 
   Comment( display );
}

void prepareHistory(){
   symbolHistory = 0;
   totalHistoryProfit = 0;
   for( int iPos = OrdersHistoryTotal() - 1 ; iPos > ( OrdersHistoryTotal() - 1 ) - totalHistory; iPos-- ){
      OrderSelect( iPos, SELECT_BY_POS, MODE_HISTORY ) ;
      double QueryHistoryDouble = ( double ) QueryHistory;
      if( symbolHistory >= QueryHistoryDouble ) break;
      if( OrderSymbol() == Symbol() ){
         totalHistoryProfit = totalHistoryProfit + OrderProfit() ;
         symbolHistory = symbolHistory + 1 ;
      }
   }
}

void prepareTrend(){
   if( eADXMain < ADXMain ) {
      rangingMarket = true;
      bullish = false;
      bearish = false;
   } else {
      rangingMarket = false;
      if( eADXPlusDi > eADXMinusDi ){
         bullish = true;
         bearish = false; 
      } else if( eADXMinusDi > eADXPlusDi ){
         bullish = false;
         bearish = true; 
      }
   }
}

void preparePositions() {
   nearLongPosition = false;
   nearShortPosition = false;
   totalTrades = 0;
   totalBackupTrades = 0;
   totalProfit = 0;
   totalLoss = 0;
   buyLots = 0;
   sellLots = 0;
   for( int i = 0 ; i < OrdersTotal() ; i++ ) {
      if( OrderSelect( i, SELECT_BY_POS, MODE_TRADES ) == false ) break; 
      if( OrderSymbol() == Symbol() ) totalTrades = totalTrades + 1;
      if( OrderSymbol() == Symbol() && OrderComment() == "backup" ) totalBackupTrades = totalBackupTrades + 1;
      if( OrderSymbol() == Symbol() && OrderStopLoss() == 0 ) {
         if( OrderType() == OP_BUY && MathAbs( OrderOpenPrice() - Ask ) < eATR * TradeSpace ) nearLongPosition = true ;
         else if( OrderType() == OP_SELL && MathAbs( OrderOpenPrice() - Bid ) < eATR * TradeSpace ) nearShortPosition = true ;
         if( OrderType()==OP_BUY ) buyLots = buyLots + OrderLots(); 
         else if( OrderType()== OP_SELL ) sellLots = sellLots + OrderLots(); 
         if( OrderProfit() > 0 ) totalProfit = totalProfit + OrderProfit();
         else totalLoss = totalLoss + OrderProfit(); 
      }
   }
   
   
} 

void prepareIndicators(){
   eATR = iATR( NULL, ATRTimeFrame, ATRPeriod, ATRShift ); 
   eATRPrev = iATR( NULL, ATRTimeFrame, ATRPeriod, ATRShift + ATRShiftCheck ); 
   eADXMain = iADX( NULL, ADXTimeFrame, ADXPeriod, PRICE_MEDIAN, MODE_MAIN, ADXShift ); 
   eADXPlusDi = iADX( NULL, ADXTimeFrame, ADXPeriod, PRICE_MEDIAN, MODE_PLUSDI, ADXShift );  
   eADXMinusDi = iADX( NULL, ADXTimeFrame, ADXPeriod, PRICE_MEDIAN, MODE_MINUSDI, ADXShift );    
   eADXMainPrev = iADX( NULL, ADXTimeFrame, ADXPeriod, PRICE_MEDIAN, MODE_MAIN, ADXShift + ADXShiftCheck ); 
   eADXPlusDiPrev = iADX( NULL, ADXTimeFrame, ADXPeriod, PRICE_MEDIAN, MODE_PLUSDI, ADXShift + ADXShiftCheck );  
   eADXMinusDiPrev = iADX( NULL, ADXTimeFrame, ADXPeriod, PRICE_MEDIAN, MODE_MINUSDI, ADXShift + ADXShiftCheck ); 
   MA3Cur = iMA( NULL, MATimeFrame, MAPeriod, MMAShift, MODE_SMMA, PRICE_MEDIAN, MAShift );  
   MA3Prev = iMA( NULL, MATimeFrame, MAPeriod, MMAShift, MODE_SMMA, PRICE_MEDIAN, MAShift + MAShiftCheck ); 
} 

void prepareFractals(){ 
   fractalUpPrice = 0 ;
   fractalDownPrice = 0;  
   bool iup = false; 
   bool idn = false; 
   for( int i = 0; i < totalHistory; i++ ){ 
      double ifractalUp = iFractals( NULL, 0, MODE_UPPER, i );
      double ifractalDown = iFractals( NULL, 0, MODE_LOWER, i );
      if( ifractalUp > 0 && Open[i] > Open[0] ){
         iup = true;
         if( Open[i] > Close[i] ) fractalUpPrice = Open[i]; 
         else fractalUpPrice = Close[i];
      }
      if( ifractalDown > 0 && Open[i] < Open[0] ){
         idn = true;
         if( Open[i] < Close[i] ) fractalDownPrice = Open[i]; 
         else fractalDownPrice = Close[i];  
      }
      if( iup && idn ) break;
   } 
}

void prepare(){ 
   prepareIndicators();
   prepareFractals();
   prepareTrend();
   setPipPoint(); 
   prepareHistory();
   preparePositions(); 
   lotSize();   
   update() ; 
   
   mileEquity = AccountEquity() - accumulatedEquity;
   if( DayOfYear() < StartCycle || ( DayOfYear() < StartCycle && StartCycle == 0 ) ) ContinueTrading = false;
   else if( AccountEquity() > nextMilestone && totalTrades == 0 ) {
      if( AccumulateMiles ){
         prevMilestone = nextMilestone;
         nextMilestone = AccountEquity() + AccountLimit - AccountReserve;
         accumulatedEquity = prevMilestone - AccountReserve;
         milestoneCount = milestoneCount + 1;
      } else ContinueTrading = false; 
   } else ContinueTrading = true; 
} 

void openPosition(){ 
   if( AccountFreeMargin() - accumulatedEquity > 0 ){
      if( eATR > eATRPrev ){ 
         double tlots = NormalizeDouble( lotSize * ( ( MaxBasketTrades - bscount ) / MaxBasketTrades ), 2 ); 
         if( !nearLongPosition && bullish && sellLots == 0 ) {
            if( bsnotype != OP_BUY ) bscount = 0;
            if( bscount < MaxBasketTrades ){
               OrderSend( Symbol(), OP_BUY , lotSize, Ask, slippage, 0, 0, "basketNo" + DoubleToStr( basno, 0 ), MAGIC ) ; 
               bscount = bscount + 1;
               if( bsnotype != OP_BUY ) basno = basno + 1; 
               bsnotype = OP_BUY;
            } 
         } else if( !nearShortPosition && bearish && buyLots == 0 ) {
            if( bsnotype != OP_SELL ) bscount = 0;
            if( bscount < MaxBasketTrades ){
               OrderSend( Symbol(), OP_SELL, lotSize, Bid, slippage, 0, 0, "basketNo" + DoubleToStr( basno, 0 ), MAGIC ) ; 
               bscount = bscount + 1; 
               if( bsnotype != OP_SELL ) basno = basno + 1; 
               bsnotype = OP_SELL; 
            }
         }
      } 
   }
} 

void backupSystem(){
   if( AccountFreeMargin() - accumulatedEquity > 0 ){
      if( ( ContinueTrading || ( !ContinueTrading && totalBackupTrades > 0 ) ) && ( totalBackupTrades < MaxBackupTrades ) ) {
         int type = -1;
         if( rangingMarket ){
            if( Close[0] >= fractalUpPrice && Close[0] >= MA3Cur ) type = OP_BUY;
            else if( Close[0] <= fractalDownPrice && Close[0] <= MA3Cur ) type = OP_SELL; 
         } else {
            if( Close[0] >= fractalUpPrice && Close[0] >= MA3Cur ) type = OP_SELL;
            else if( Close[0] <= fractalDownPrice && Close[0] <= MA3Cur ) type = OP_BUY; 
         }
         if( !nearLongPosition && type == OP_BUY && sellLots == 0 ) OrderSend( Symbol(), type, lotSize, Ask, slippage, 0, 0, "backup", MAGIC ) ;
         else if( !nearShortPosition && type == OP_SELL && buyLots == 0) OrderSend( Symbol(), type , lotSize, Bid, slippage, 0, 0, "backup", MAGIC ) ;
       }
    }
}

void managePositions(){
   if( totalHistoryProfit < 0 && totalProfit > MathAbs( totalHistoryProfit ) * BasketProfit ) closeAll( "profits" );
   else if( totalTrades > 1 && totalProfit > MathAbs( totalLoss ) * OpenProfit ) closeAll();
   else { 
      for( int i = 0 ; i < OrdersTotal() ; i++ ) {
         if( OrderSelect( i, SELECT_BY_POS, MODE_TRADES ) == false ) break;  
         if( OrderSymbol() == Symbol()  ) {  
            if( totalTrades == 1 ){
               if( OrderType() == OP_BUY && Bid > OrderOpenPrice() &&  MathAbs( Bid - OrderOpenPrice() ) > MinProfit * eATR ) 
                  OrderClose( OrderTicket(), OrderLots(), Bid, slippage ); 
               else if( OrderType() == OP_SELL && Ask < OrderOpenPrice() && MathAbs( OrderOpenPrice() - Ask ) > MinProfit * eATR )
                  OrderClose( OrderTicket(), OrderLots(), Ask, slippage );   
            }
         }  
      }
   }
} 

int start() { 
   prepare() ;  
   if( CloseAll ) closeAll() ;
   else {
      if( AccountEquity() < accumulatedEquity && milestoneCount > 0 && totalTrades > 0 ) {
         closeAll();
         nextMilestone = prevMilestone;
         prevMilestone = nextMilestone - AccountLimit ;
         accumulatedEquity = AccountEquity();
         milestoneLoss = milestoneLoss + 1;
      } else if( BackupSystem && totalTrades >= MaxStartTrades ) backupSystem();
      else if( ( ContinueTrading || ( !ContinueTrading && totalTrades > 0 ) ) && ( totalTrades < MaxStartTrades || MaxStartTrades == 0 ) ) openPosition() ; 
      managePositions() ;
   }
   return( 0 ) ;
}