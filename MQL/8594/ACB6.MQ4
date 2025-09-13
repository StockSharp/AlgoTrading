//+------------------------------------------------------------------+
//|                                                                  |
//|                 Copyright © 1999-2008, MetaQuotes Software Corp. |
//|                                         http://www.metaquotes.ru |
//+------------------------------------------------------------------+

                         #define A.System.Code  "A System: Championship Strategy Final Edit"
                         #define A.System.Data  "27.11.2008"

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
                         int init  () { Alert("A System: Reloaded")                 ;}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
                         int start () { if   (aib.System.IsOK ()  )air.System.Run ();}

//< A System: Structure >

//       A.Memory:    102 elements
//< 01 > constants     10
//< 02 > parameters.1   5
//< 03 > parameters.2  14
//< 04 > variables.1   14
//< 05 > variables.2   16
//< 06 > variables.3   18
//< 07 > variables.4   25

//       A.Interface:   2 elements         // Input   0 / Code   5 / Output  1
//< 01 > aib.System.IsOK                   // input   - / code   1 / output  1
//< 02 > air.System.Run                    // input   - / code   4 / output  -

//       A.Function:   24 elements         // Input  11 / Code 322 / Output  6
//< 01 > afb.System.TradePauseIsOK         // input   - / code   1 / output  1
//< 02 > afb.System.EquityIsOK             // input   - / code  10 / output  1
//< 03 > afr.System.Reset                  // input   - / code  10 / output  -
//< 04 > afr.System.LoadOptimization       // input   - / code  20 / output  -
//< 05 > afr.System.LoadSymbol             // input   1 / code  16 / output  -
//< 06 > afr.System.LoadMarketData         // input   - / code  16 / output  -
//< 07 > afr.System.SelectParameters       // input   1 / code  13 / output  -
//< 08 > afr.System.ComputeFactors         // input   - / code  21 / output  -
//< 09 > afr.System.ManagePosition         // input   - / code  17 / output  -
//< 10 > afr.System.AttemptToClose         // input   1 / code   6 / output  -
//< 11 > afi.System.SignalToClose          // input   1 / code  18 / output  1
//< 12 > afd.System.MaximalProfit          // input   1 / code   6 / output  1
//< 13 > afr.System.Reserved               // input   - / code   0 / output  -
//< 14 > afr.System.AttemptToTrail         // input   1 / code  22 / output  -
//< 15 > afd.System.TrailAdjustment        // input   1 / code  16 / output  -
//< 16 > afr.System.OpenPosition           // input   - / code   8 / output  -
//< 17 > afr.System.AttemptToTrade         // input   1 / code   9 / output  -
//< 18 > afr.System.SendOrder              // input   1 / code  10 / output  -
//< 19 > afr.System.ComputeStrategy        // input   - / code  15 / output  -
//< 20 > afi.System.SignalToOpen           // input   - / code  14 / output  1
//< 21 > afr.System.ComputeOrderSize       // input   1 / code  12 / output  -
//< 22 > afd.System.RiskAdjustment         // input   1 / code  23 / output  1
//< 23 > afr.System.LockPosition           // input   - / code  15 / output  -
//< 24 > afr.System.AttemptToLock          // input   - / code  24 / output  -

//</A System: Structure >

//< A System: Constants >

#define ac.Stamp         "[ A System ] : "

#define ac.TradePause       5

#define ac.Symbols          4

#define ac.Parameters       14

#define ac.Threads          4

#define ac.ThreadsDimension 5

#define ac.LockShift        100

#define ac.OrderIDDimension 401

#define ac.Timeframes       10

int     ac.Timeframe []={ 0 , 1 , 5 , 15 , 30 , 60 , 240 , 1440 , 10080 , 43200 }                            ;

//</A System: Constants >

//< A System: Parameters 1 >

bool   avb.IsOptimizing =0                                                                                   ;

double avd.SystemStop   =0.80                                                                                ;

string  ac.Symbol     []={ "" , "EURUSD" , "GBPUSD" , "" }                                                   ;

double  ac.RiskFactor []={ 0  , 1.00     , 1.00     , 0  }                                                   ;

double  ac.Parameter  [ ac.Symbols , ac.Threads , ac.Parameters ]={

        // Reserved
        0 , 7 , 7 , 1.0 , 1.0 , 0.00 , 1.00 , 0.00 , 0.00 ,  0.00 ,  0.00 , 0.000 ,   1 ,   1 ,
        0 , 7 , 7 , 1.0 , 1.0 , 0.00 , 1.00 , 0.00 , 0.00 ,  0.00 ,  0.00 , 0.000 ,   1 ,   1 ,
        0 , 7 , 7 , 1.0 , 1.0 , 0.00 , 1.00 , 0.00 , 0.00 ,  0.00 ,  0.00 , 0.000 ,   1 ,   1 ,
        0 , 7 , 7 , 1.0 , 1.0 , 0.00 , 1.00 , 0.00 , 0.00 ,  0.00 ,  0.00 , 0.000 ,   1 ,   1 ,

        // EURUSD
        1 , 7 , 6 , 0.8 ,10.0 , 0.50 , 0.40 , 0.00 , 0.00 ,  0.00 , 16.40 , 0.014 ,  20 ,  85 ,
        0 , 7 , 7 , 1.0 , 1.0 , 0.00 , 1.00 , 0.00 , 0.00 ,  0.00 ,  0.00 , 0.000 ,   1 ,   1 ,
        0 , 7 , 7 , 1.0 , 1.0 , 0.00 , 1.00 , 0.00 , 0.00 ,  0.00 ,  0.00 , 0.000 ,   1 ,   1 ,
        0 , 7 , 7 , 1.0 , 1.0 , 0.00 , 1.00 , 0.00 , 0.00 ,  0.00 ,  0.00 , 0.000 ,   1 ,   1 ,

        // GBPUSD
        0 , 7 , 7 , 1.0 , 1.0 , 0.00 , 1.00 , 0.00 , 0.00 ,  0.00 ,  0.00 , 0.000 ,   1 ,   1 ,
        0 , 7 , 7 , 1.0 , 1.0 , 0.00 , 1.00 , 0.00 , 0.00 ,  0.00 ,  0.00 , 0.000 ,   1 ,   1 ,
        0 , 7 , 7 , 1.0 , 1.0 , 0.00 , 1.00 , 0.00 , 0.00 ,  0.00 ,  0.00 , 0.000 ,   1 ,   1 ,
        0 , 7 , 7 , 1.0 , 1.0 , 0.00 , 1.00 , 0.00 , 0.00 ,  0.00 ,  0.00 , 0.000 ,   1 ,   1 ,

        // Reserved
        0 , 7 , 7 , 1.0 , 1.0 , 0.00 , 1.00 , 0.00 , 0.00 ,  0.00 ,  0.00 , 0.000 ,   1 ,   1 ,
        0 , 7 , 7 , 1.0 , 1.0 , 0.00 , 1.00 , 0.00 , 0.00 ,  0.00 ,  0.00 , 0.000 ,   1 ,   1 ,
        0 , 7 , 7 , 1.0 , 1.0 , 0.00 , 1.00 , 0.00 , 0.00 ,  0.00 ,  0.00 , 0.000 ,   1 ,   1 ,
        0 , 7 , 7 , 1.0 , 1.0 , 0.00 , 1.00 , 0.00 , 0.00 ,  0.00 ,  0.00 , 0.000 ,   1 ,   1 ,

   } ;

//</A System: Parameters 1 >

//< A System: Parameters 2 >

//< extern >
int    avi.InputIndex.1     =7                                                                               ;
int    avi.InputIndex.2     =7                                                                               ;
double avd.InputParameter.1 =1.0                                                                             ;
double avd.InputParameter.2 =1.0                                                                             ;
double avd.InputFallLimit   =0.00                                                                            ;
double avd.InputFallFactor  =1.00                                                                            ;
double avd.InputLockSize    =0.00                                                                            ;
double avd.InputLockFactor  =1.00                                                                            ;
double avd.InputTimeFactor.1=0.00                                                                            ;
double avd.InputTimeFactor.2=0.00                                                                            ;
double avd.InputRiskLimit   =0.025                                                                           ;
//</extern >

//< adjustment >
int    avi.InputLE          =1                                                                               ;
int    avi.InputTE          =1                                                                               ;

double avd.InputRiskFactor  =1.00                                                                            ;
//</adjustment >

//</A System: Parameters 2 >

//< A System: Variables 1 >

int    avi.Index.1                                                                                             ;
int    avi.Index.2                                                                                             ;
double avd.TakeFactor                                                                                          ;
double avd.TrailFactor                                                                                         ;
double avd.FallLimit                                                                                           ;
double avd.FallFactor                                                                                          ;
double avd.LockSize                                                                                            ;
double avd.LockFactor                                                                                          ;
double avd.TimeFactor.1                                                                                        ;
double avd.TimeFactor.2                                                                                        ;
double avd.RiskLimit                                                                                           ;

int    avi.LossesExpected , avi.LossesObtained                                                                 ;
int    avi.TradesExpected , avi.TradesObtained                                                                 ;

double avd.RiskFactor                                                                                          ;

//</A System: Variables 1 >

//< A System: Variables 2 >

int    ai.Index.1        [ ac.ThreadsDimension ]                                                               ;
int    ai.Index.2        [ ac.ThreadsDimension ]                                                               ;
double ad.TakeFactor     [ ac.ThreadsDimension ]                                                               ;
double ad.TrailFactor    [ ac.ThreadsDimension ]                                                               ;
double ad.FallLimit      [ ac.ThreadsDimension ]                                                               ;
double ad.FallFactor     [ ac.ThreadsDimension ]                                                               ;
double ad.LockSize       [ ac.ThreadsDimension ]                                                               ;
double ad.LockFactor     [ ac.ThreadsDimension ]                                                               ;
double ad.TimeFactor.1   [ ac.ThreadsDimension ]                                                               ;
double ad.TimeFactor.2   [ ac.ThreadsDimension ]                                                               ;
double ad.RiskLimit      [ ac.ThreadsDimension ]                                                               ;

int    ai.LossesExpected [ ac.ThreadsDimension ]                                                               ;
int    ai.TradesExpected [ ac.ThreadsDimension ]                                                               ;

int    avi.Thread        [ ac.ThreadsDimension ]                                                               ;
int    avi.MainFlag      [ ac.ThreadsDimension ]                                                               ;
int    avi.LockFlag      [ ac.ThreadsDimension ]                                                               ;

//</A System: Variables 2 >

//< A System: Variables 3 >

double avi.TimeStamp                                                                                           ;
bool   avb.FlagOfReturn                                                                                        ;

double avd.MaximalProfit [ ac.OrderIDDimension ]                                                               ;
int    avi.MaximalTime   [ ac.OrderIDDimension ]                                                               ;

string avs.Symbol                                                                                              ;
int    avd.Digits                                                                                              ;
int    avi.Spread                                                                                              ;

double avd.QuoteAsk                                                                                            ;
double avd.QuoteBid                                                                                            ;

double avd.QuotePoint                                                                                          ;
double avd.QuoteSpread                                                                                         ;
double avd.QuoteStops                                                                                          ;
double avd.QuoteTick                                                                                           ;

double avd.NominalTick                                                                                         ;
double avd.NominalMargin                                                                                       ;
double avd.NominalLot                                                                                          ;
double avd.MinimumLots                                                                                         ;
double avd.LotStep                                                                                             ;

//</A System: Variables 3 >

//< A System: Variables 4 >

int    avi.Period.1                                                                                            ;
int    avi.Period.2                                                                                            ;

double avd.Low.1                                                                                               ;
double avd.High.1                                                                                              ;
double avd.Open.1                                                                                              ;
double avd.Close.1                                                                                             ;

double avd.Low.2                                                                                               ;
double avd.High.2                                                                                              ;
double avd.Open.2                                                                                              ;
double avd.Close.2                                                                                             ;

double avd.QuoteTake                                                                                           ;
double avd.QuoteTrail                                                                                          ;

int    avi.Command                                                                                             ;
double avd.Price                                                                                               ;
double avd.Stop                                                                                                ;
double avd.Take                                                                                                ;
double avd.Risk                                                                                                ;

int    avi.Position                                                                                            ;
int    avi.RiskPoints                                                                                          ;
double avd.VARLimit                                                                                            ;
double avd.RiskPoint                                                                                           ;
double avd.PositionLimit                                                                                       ;
double avd.VolumeLimit                                                                                         ;

int    avi.Steps                                                                                               ;
double avd.Volume                                                                                              ;

//</A System: Variables 4 >

//< A System: Interface 1 >

//< head>

bool   aib.System.IsOK                (// input - / code  1 / output 1
                                         //
                                         //
                                         //
                                         //
                                      )//
//</head>

  {//<body>

   return(afb.System.TradePauseIsOK () && afb.System.EquityIsOK ())                                          ;//  1

  }//</body>

//</A System: Interface 1 >

//< A System: Interface 2 >

//< head>

int    air.System.Run                 (// input - / code  4 / output -
                                         //
                                         //
                                         //
                                         //
                                      )//
//</head>

  {//<body>

   afr.System.Reset          ()                                                                                  ;//  1
   afr.System.ManagePosition ()                                                                                  ;//  2
   afr.System.OpenPosition   ()                                                                                  ;//  3
   afr.System.LockPosition   ()                                                                                  ;//  4

  }//</body>

//</A System: Interface 2 >

//< A System: Function 1 >

//< head>

bool   afb.System.TradePauseIsOK      (// input - / code  1 / output 1
                                         //
                                         //
                                         //
                                         //
                                      )//
//</head>

  {//<body>

   return(TimeLocal () > avi.TimeStamp + ac.TradePause)                                                       ;

  }//</body>

//</A System: Function 1 >

//< A System: Function 2 >

//< head>

bool   afb.System.EquityIsOK          (// input - / code 10 / output 1
                                         //
                                         //
                                         //
                                         //
                                      )//
//</head>

  {//<body>

   static double               ald.MaximalEquity                                                                  ;
   if(AccountEquity ()     > ald.MaximalEquity)ald.MaximalEquity=AccountEquity ()                           ;

   static bool alb.Result   ;alb.Result=  (AccountEquity ()  > ald.MaximalEquity * avd.SystemStop)      ;

   if(alb.Result !=True  )
      if(OrdersTotal ()==0  )
      { static bool alb.Flag;if(! alb.Flag){ alb.Flag=True;Alert(ac.Stamp , "Stop")            ;} }

   return(alb.Result)                                                                                      ;

  }//</body>

//</A System: Function 2 >

//< A System: Function 3 >

//< head>

int    afr.System.Reset               (// input - / code 10 / output -
                                         //
                                         //
                                         //
                                         //
                                      )//
//</head>

  {//<body>

     static bool alb.Flag;if(! alb.Flag){ alb.Flag=True ;
      Alert(ac.Stamp , "Start")                     ;
      avs.Symbol= Symbol ()                                                                                      ;

      if(avb.IsOptimizing)                           afr.System.LoadOptimization ()                            ;
      else for( int i=1;   i  < ac.Symbols;i ++ )
           if (avs.Symbol==ac.Symbol  [ i ] )afr.System.LoadSymbol   (i)                        ;}

   afr.System.LoadMarketData ()                                                                                   ;
   avb.FlagOfReturn=! True                                                                                      ;

  }//</body>

//</A System: Function 3 >

//< A System: Function 4 >

//< head>

int    afr.System.LoadOptimization    (// input - / code 20 / output -
                                         //
                                         //
                                         //
                                         //
                                      )//
//</head>

  {//<body>

   avi.Thread        [ 1 ]=True                                                                                 ;
   avi.Thread        [ 2 ]=EMPTY                                                                                ;
   avi.Thread        [ 3 ]=EMPTY                                                                                ;
   avi.Thread        [ 4 ]=EMPTY                                                                                ;

   ai.Index.1        [ 1 ]=avi.InputIndex.1                                                                     ;
   ai.Index.2        [ 1 ]=avi.InputIndex.2                                                                     ;
   ad.TakeFactor     [ 1 ]=avd.InputParameter.1                                                                 ;
   ad.TrailFactor    [ 1 ]=avd.InputParameter.2                                                                 ;
   ad.FallLimit      [ 1 ]=avd.InputFallLimit                                                                   ;
   ad.FallFactor     [ 1 ]=avd.InputFallFactor                                                                  ;
   ad.LockSize       [ 1 ]=avd.InputLockSize                                                                    ;
   ad.LockFactor     [ 1 ]=avd.InputLockFactor                                                                  ;
   ad.TimeFactor.1   [ 1 ]=avd.InputTimeFactor.1                                                                ;
   ad.TimeFactor.2   [ 1 ]=avd.InputTimeFactor.2                                                                ;
   ad.RiskLimit      [ 1 ]=avd.InputRiskLimit                                                                   ;
   ai.LossesExpected [ 1 ]=avi.InputLE                                                                          ;
   ai.TradesExpected [ 1 ]=avi.InputTE                                                                          ;

   avd.RiskFactor         =1.0                                                                                  ;

  }//</body>

//</A System: Function 4 >

//< A System: Function 5 >

//< head>

int    afr.System.LoadSymbol          (// input 1 / code 16 / output -
                                         //
       int    aai.SymbolID               //
                                         //
                                         //
                                      )//
//</head>

  {//<body>

   avd.RiskFactor=ac.RiskFactor [ aai.SymbolID ];//avd.RiskFactor = avd.InputRiskFactor                       ;
   for( static int i     ;i=0;i < ac.Threads;i ++ )
     { avi.Thread        [ i + 1 ]=ac.Parameter [ aai.SymbolID , i ,  0 ]                                     ;
      ai.Index.1        [ i + 1 ]=ac.Parameter [ aai.SymbolID , i ,  1 ]                                     ;
      ai.Index.2        [ i + 1 ]=ac.Parameter [ aai.SymbolID , i ,  2 ]                                     ;
      ad.TakeFactor     [ i + 1 ]=ac.Parameter [ aai.SymbolID , i ,  3 ]                                     ;
      ad.TrailFactor    [ i + 1 ]=ac.Parameter [ aai.SymbolID , i ,  4 ]                                     ;
      ad.FallLimit      [ i + 1 ]=ac.Parameter [ aai.SymbolID , i ,  5 ]                                     ;
      ad.FallFactor     [ i + 1 ]=ac.Parameter [ aai.SymbolID , i ,  6 ]                                     ;
      ad.LockSize       [ i + 1 ]=ac.Parameter [ aai.SymbolID , i ,  7 ]                                     ;
      ad.LockFactor     [ i + 1 ]=ac.Parameter [ aai.SymbolID , i ,  8 ]                                     ;
      ad.TimeFactor.1   [ i + 1 ]=ac.Parameter [ aai.SymbolID , i ,  9 ]                                     ;
      ad.TimeFactor.2   [ i + 1 ]=ac.Parameter [ aai.SymbolID , i , 10 ]                                     ;
      ad.RiskLimit      [ i + 1 ]=ac.Parameter [ aai.SymbolID , i , 11 ]                                     ;
      ai.LossesExpected [ i + 1 ]=ac.Parameter [ aai.SymbolID , i , 12 ]                                     ;
     ai.TradesExpected [ i + 1 ]=ac.Parameter [ aai.SymbolID , i , 13 ]                                  ;}

  }//</body>

//</A System: Function 5 >

//< A System: Function 6 >

//< head>

int    afr.System.LoadMarketData      (// input - / code 16 / output -
                                         //
                                         //
                                         //
                                         //
                                      )//
//</head>

  {//<body>

   avd.NominalLot   =MarketInfo(avs.Symbol , MODE_LOTSIZE     )                                         ;
   avd.MinimumLots  =MarketInfo(avs.Symbol , MODE_MINLOT      )                                         ;
   avd.LotStep      =MarketInfo(avs.Symbol , MODE_LOTSTEP     )                                         ;

   avi.Spread       =MarketInfo(avs.Symbol , MODE_SPREAD      )                                         ;

   avd.QuoteAsk     =MarketInfo(avs.Symbol , MODE_ASK         )                                         ;
   avd.QuoteBid     =MarketInfo(avs.Symbol , MODE_BID         )                                         ;
   avd.QuotePoint   =MarketInfo(avs.Symbol , MODE_POINT       )                                         ;
   avd.QuoteSpread  =MarketInfo(avs.Symbol , MODE_SPREAD        )* avd.QuotePoint                           ;
   avd.QuoteStops   =MarketInfo(avs.Symbol , MODE_STOPLEVEL     )* avd.QuotePoint                           ;
   avd.QuoteTick    =MarketInfo(avs.Symbol , MODE_TICKSIZE    )                                         ;
   avd.NominalTick  =MarketInfo(avs.Symbol , MODE_TICKVALUE   )                                         ;
   avd.NominalMargin=MarketInfo(avs.Symbol , MODE_MARGINREQUIRED)                                           ;

   avd.Digits       =MarketInfo(avs.Symbol , MODE_DIGITS      )                                         ;

  }//</body>

//</A System: Function 6 >

//< A System: Function 7 >

//< head>

int    afr.System.SelectParameters    (// input 1 / code 13 / output -
                                         //
       int    aai.Thread                 //
                                         //
                                         //
                                      )//
//</head>

  {//<body>

   avi.Index.1       =ai.Index.1        [ aai.Thread ]                                                          ;
   avi.Index.2       =ai.Index.2        [ aai.Thread ]                                                          ;
   avd.TakeFactor    =ad.TakeFactor     [ aai.Thread ]                                                          ;
   avd.TrailFactor   =ad.TrailFactor    [ aai.Thread ]                                                          ;
   avd.FallLimit     =ad.FallLimit      [ aai.Thread ]                                                          ;
   avd.FallFactor    =ad.FallFactor     [ aai.Thread ]                                                          ;
   avd.LockSize      =ad.LockSize       [ aai.Thread ]                                                          ;
   avd.LockFactor    =ad.LockFactor     [ aai.Thread ]                                                          ;
   avd.TimeFactor.1  =ad.TimeFactor.1   [ aai.Thread ]                                                          ;
   avd.TimeFactor.2  =ad.TimeFactor.2   [ aai.Thread ]                                                          ;
   avd.RiskLimit     =ad.RiskLimit      [ aai.Thread ] * avd.RiskFactor                                         ;
   avi.LossesExpected=ai.LossesExpected [ aai.Thread ]                                                          ;
   avi.TradesExpected=ai.TradesExpected [ aai.Thread ]                                                          ;

  }//</body>

//</A System: Function 7 >

//< A System: Function 8 >

//< head>

int    afr.System.ComputeFactors      (// input - / code 21 / output -
                                         //
                                         //
                                         //
                                         //
                                      )//
//</head>

  {//<body>

   avi.Command   =EMPTY                                                                                         ;
   avd.Price     =EMPTY                                                                                         ;
   avd.Stop      =EMPTY                                                                                         ;
   avd.Take      =EMPTY                                                                                         ;
   avd.Risk      =EMPTY                                                                                         ;

   avi.Period.1  =ac.Timeframe [ avi.Index.1 ]                                                                  ;
   avi.Period.2  =ac.Timeframe [ avi.Index.2 ]                                                                  ;

   avd.Low.1     =iLow  (avs.Symbol , avi.Period.1 , 1)                                                     ;
   avd.High.1    =iHigh (avs.Symbol , avi.Period.1 , 1)                                                     ;
   avd.Open.1    =iOpen (avs.Symbol , avi.Period.1 , 1)                                                     ;
   avd.Close.1   =iClose(avs.Symbol , avi.Period.1 , 1)                                                     ;

   avd.Low.2     =iLow  (avs.Symbol , avi.Period.2 , 1)                                                     ;
   avd.High.2    =iHigh (avs.Symbol , avi.Period.2 , 1)                                                     ;
   avd.Open.2    =iOpen (avs.Symbol , avi.Period.2 , 1)                                                     ;
   avd.Close.2   =iClose(avs.Symbol , avi.Period.2 , 1)                                                     ;

   avd.QuoteTake =NormalizeDouble(( avd.High.1 - avd.Low.1)* avd.TakeFactor  , avd.Digits)                ;
   avd.QuoteTrail=NormalizeDouble(( avd.High.2 - avd.Low.2)* avd.TrailFactor , avd.Digits)                ;

  }//</body>

//</A System: Function 8 >

//< A System: Function 9 >

//< head>

int    afr.System.ManagePosition      (// input - / code 17 / output -
                                         //
                                         //
                                         //
                                         //
                                      )//
//</head>

  {//<body>

   for( static int i;i=1;i<=ac.Threads;i ++)    avi.MainFlag [ i ]=EMPTY                           ;

   if(OrdersTotal ()      > 0 )
      for( static int j;j=0;j  < OrdersTotal ();j ++   )
        { OrderSelect (j , SELECT_BY_POS      , MODE_TRADES)                                                 ;
         static int ali.Thread                                                                                  ;
         static int ali.OrderID;ali.OrderID =OrderMagicNumber ()                                            ;
         if      (ali.OrderID < ac.LockShift )
         { ali.Thread =ali.OrderID ;avi.MainFlag [ ali.OrderID ]=True                         ;}
         else       ali.Thread =ali.OrderID /ac.LockShift                                                   ;

         afr.System.SelectParameters(ali.Thread)                                                          ;
         afr.System.ComputeFactors   ()                                                                         ;
         afr.System.AttemptToClose  (ali.OrderID)                                                           ;
        afr.System.AttemptToTrail  (ali.OrderID)                                                        ;}

  }//</body>

//</A System: Function 9 >

//< A System: Function 10 >

//< head>

int    afr.System.AttemptToClose      (// input 1 / code  6 / output -
                                         //
       int    aai.OrderID                //
                                         //
                                         //
                                      )//
//</head>

  {//<body>

   if(avb.FlagOfReturn)return                                                                                 ;

   if(OrderType ()   ==afi.System.SignalToClose      (aai.OrderID)      )
     { OrderClose      (OrderTicket ()  , OrderLots () , avd.Price   , 0 , 0)                                ;
      avi.TimeStamp   =TimeLocal   ()                                                                         ;
     avb.FlagOfReturn=True                                                                                ;}

  }//</body>

//</A System: Function 10 >

//< A System: Function 11 >

//< head>

int    afi.System.SignalToClose       (// input 1 / code 18 / output 1
                                         //
       int    aai.OrderID                //
                                         //
                                         //
                                      )//
//</head>

  {//<body>

   static int ali.SignalToClose;ali.SignalToClose=EMPTY                                                       ;

   if(OrderType   ()  ==OP_BUY                                                                        )
      if(OrderProfit ()                     >   0                                                          )
         if(OrderProfit ()                     <   afd.System.MaximalProfit(aai.OrderID )* avd.FallLimit  )
            if(avd.QuoteBid - OrderOpenPrice   () >(OrderTakeProfit () - OrderOpenPrice () )* avd.FallFactor )
               ali.SignalToClose=OP_BUY                                                                                ;

   if(OrderType   ()  ==OP_SELL                                                                       )
      if(OrderProfit ()                     >   0                                                          )
         if(OrderProfit ()                     <   afd.System.MaximalProfit(aai.OrderID )* avd.FallLimit  )
            if(OrderOpenPrice () - avd.QuoteAsk   >(OrderOpenPrice  () - OrderTakeProfit ())* avd.FallFactor )
               ali.SignalToClose=OP_SELL                                                                               ;

   if(ali.SignalToClose==OP_BUY)avd.Price=avd.QuoteBid                                                    ;
   else                               avd.Price=avd.QuoteAsk                                                    ;

   return(ali.SignalToClose)                                                                                  ;

  }//</body>

//</A System: Function 11 >

//< A System: Function 12 >

//< head>

double afd.System.MaximalProfit       (// input 1 / code  6 / output 1
                                         //
       int    aai.OrderID                //
                                         //
                                         //
                                      )//
//</head>

  {//<body>

   if(OrderProfit () > 0                                 )
      if(OrderProfit () > avd.MaximalProfit [ aai.OrderID ] )
        { avd.MaximalProfit [ aai.OrderID ] =OrderProfit  ()                                                      ;
        avi.MaximalTime   [ aai.OrderID ] =TimeLocal    ()                                                   ;}

   return(avd.MaximalProfit [ aai.OrderID ])                                                     ;

  }//</body>

//</A System: Function 12 >

//< A System: Function 13 >

//< head>

//int  afr.System.Reserved             ( // input - / code  0 / output -
                                         //
                                         //
                                         //
                                         //
//                                     ) //
//</head>

///<body>

///</body>

//</A System: Function 13 >

//< A System: Function 14 >

//< head>

int    afr.System.AttemptToTrail      (// input 1 / code 22 / output -
                                         //
       int    aai.OrderID                //
                                         //
                                         //
                                      )//
//</head>

  {//<body>

   if(avb.FlagOfReturn               )return                                                                  ;
   if(avd.QuoteTrail < avd.QuoteStops)return                                                                  ;

   if(avd.TimeFactor.1!=0)avd.QuoteTrail=afd.System.TrailAdjustment(aai.OrderID)                    ;

   if(OrderType   ()==OP_BUY                                                           )
      if(OrderProfit () > 0                                                                 )
         if(avd.QuoteBid   < OrderTakeProfit ()                  - avd.QuoteStops              )
            if(avd.QuoteBid   > OrderStopLoss   ()                  + avd.QuoteTrail              )
               avd.Stop      =NormalizeDouble(avd.QuoteBid      - avd.QuoteTrail , avd.Digits)                     ;

   if(OrderType   ()==OP_SELL                                                          )
      if(OrderProfit () > 0                                                                 )
         if(avd.QuoteAsk   > OrderTakeProfit ()                  + avd.QuoteStops              )
            if(avd.QuoteAsk   < OrderStopLoss   ()                  - avd.QuoteTrail              )
               avd.Stop      =NormalizeDouble(avd.QuoteAsk      + avd.QuoteTrail , avd.Digits)                     ;

   if(avd.Stop       > 0 )
      if(avd.Stop     !=NormalizeDouble(OrderStopLoss  () , avd.Digits))
        { OrderModify   (OrderTicket ()  , OrderOpenPrice () , avd.Stop , OrderTakeProfit () , 0 , 0)           ;
         avi.TimeStamp   =TimeLocal ()                                                                           ;
        avb.FlagOfReturn=True                                                                                ;}

  }//</body>

//</A System: Function 14 >

//< A System: Function 15 >

//< head>

double afd.System.TrailAdjustment     (// input 1 / code 16 / output -
                                         //
       int    aai.OrderID                //
                                         //
                                         //
                                      )//
//</head>

  {//<body>

   static double ald.NewQuoteTrail                                                                                ;
   static double ald.TrailAdjustment                                                                              ;
   static int    ali.OpenTime                                                                                     ;
   static int    ali.ComparisonTime                                                                               ;

   ali.OpenTime    =TimeCurrent () - OrderOpenTime ()                                                       ;
   ali.ComparisonTime =(avi.Period.1   + avi.Period.2  )* 60 * avd.TimeFactor.1                               ;

   if(( avd.QuoteTrail==avd.QuoteStops     )
     ||(ali.OpenTime  >=ali.ComparisonTime)) ald.NewQuoteTrail=avd.QuoteStops                             ;

     else { ald.TrailAdjustment=1.0 *(ali.ComparisonTime - ali.OpenTime )/ali.ComparisonTime                  ;
      ald.NewQuoteTrail=      avd.QuoteTrail     * ald.TrailAdjustment                                  ;
     if(ald.NewQuoteTrail<=avd.QuoteStops)ald.NewQuoteTrail=avd.QuoteStops                        ;}

   return(ald.NewQuoteTrail)                                                                                  ;

  }//</body>

//</A System: Function 15 >

//< A System: Function 16 >

//< head>

int    afr.System.OpenPosition        (// input - / code  8 / output -
                                         //
                                         //
                                         //
                                         //
                                      )//
//</head>

  {//<body>

   if(avb.FlagOfReturn)return                                                                                 ;

   for( static int i;i=1;i<=ac.Threads;i ++ )
     { if(avi.Thread      [ i ]==True )
         if(avi.MainFlag    [ i ]!=True )
            if(avb.FlagOfReturn     !=True )
              { afr.System.SelectParameters(i)                                                                  ;
              afr.System.AttemptToTrade  (i)                                                             ;} }

  }//</body>

//</A System: Function 16 >

//< A System: Function 17 >

//< head>

int    afr.System.AttemptToTrade      (// input 1 / code  9 / output -
                                         //
       int    aai.OrderID                //
                                         //
                                         //
                                      )//
//</head>

  {//<body>

   if(avb.FlagOfReturn)return                                                                                 ;

   afr.System.ComputeFactors  ()                                                                                  ;
   afr.System.ComputeStrategy ()                                                                                  ;

   if(IsTradeAllowed ()   )
      if(avi.Command > EMPTY )
        { afr.System.ComputeOrderSize(aai.OrderID)                                                              ;
        afr.System.SendOrder       (aai.OrderID)                                                           ;}

  }//</body>

//</A System: Function 17 >

//< A System: Function 18 >

//< head>

int    afr.System.SendOrder           (// input 1 / code 10 / output -
                                         //
       int    aai.OrderID                //
                                         //
                                         //
                                      )//
//</head>

  {//<body>

   if(avd.Volume > 0                                                        )
      if(AccountFreeMarginCheck (avs.Symbol       , avi.Command , avd.Volume)> 0 )
        { OrderSend(avs.Symbol  , avi.Command      , avd.Volume  , avd.Price  ,   0 , avd.Stop ,       avd.Take ,
                         ac.Stamp    , aai.OrderID  , 0 , 0)                                                         ;
         Alert    (ac.Stamp    , aai.OrderID      , "/" , GetLastError ()  , "/"   ,
                     avi.Command , "/" , avd.Volume , "/" , avd.Price        , "/"   , avd.Stop , "/" , avd.Take);
         avd.MaximalProfit [ aai.OrderID ]=EMPTY                                                                 ;
         avi.MaximalTime   [ aai.OrderID ]=EMPTY                                                                 ;
         avi.TimeStamp    =TimeLocal ()                                                                          ;
        avb.FlagOfReturn =True                                                                               ;}

  }//</body>

//</A System: Function 18 >

//< A System: Function 19 >

//< head>

int    afr.System.ComputeStrategy     (// input - / code 15 / output -
                                         //
                                         //
                                         //
                                         //
                                      )//
//</head>

  {//<body>

   if(avd.QuoteTake < avd.QuoteStops)return                                                                   ;

   if(afi.System.SignalToOpen ()==OP_BUY )
     { avi.Command=OP_BUY                                                                                      ;
      avd.Price  =avd.QuoteAsk                                                                                ;
      avd.Stop   =NormalizeDouble(avd.Low.1                     , avd.Digits)                             ;
      avd.Take   =NormalizeDouble(avd.QuoteAsk + avd.QuoteTake  , avd.Digits)                             ;
     avd.Risk   =avd.Price       - avd.Stop                                                               ;}

   if(afi.System.SignalToOpen ()==OP_SELL )
     { avi.Command=OP_SELL                                                                                     ;
      avd.Price  =avd.QuoteBid                                                                                ;
      avd.Stop   =NormalizeDouble(avd.High.1                   , avd.Digits)                              ;
      avd.Take   =NormalizeDouble(avd.QuoteBid - avd.QuoteTake , avd.Digits)                              ;
     avd.Risk   =avd.Stop - avd.Price                                                                     ;}

  }//</body>

//</A System: Function 19 >

//< A System: Function 20 >

//< head>

int    afi.System.SignalToOpen        (// input - / code 14 / output 1
                                         //
                                         //
                                         //
                                         //
                                      )//
//</head>

  {//<body>

   static int ali.SignalToOpen                                                                                    ;
   ali.SignalToOpen=  EMPTY                                                                                ;

   if(avd.Close.1      >(avd.High.1   + avd.Low.1 )/2 )
      if(avd.QuoteAsk     >   avd.High.1                     )
         if(avd.QuoteStops   <   avd.QuoteBid - avd.Low.1       )
            ali.SignalToOpen=  OP_BUY                                                                               ;

   if(avd.Close.1      <(avd.High.1   + avd.Low.1 )/2 )
      if(avd.QuoteBid     <   avd.Low.1                      )
         if(avd.QuoteStops   <   avd.High.1   - avd.QuoteAsk    )
            ali.SignalToOpen=  OP_SELL                                                                              ;

   return(ali.SignalToOpen)                                                                                   ;

  }//</body>

//</A System: Function 20 >

//< A System: Function 21 >

//< head>

int    afr.System.ComputeOrderSize    (// input 1 / code 12 / output -
                                         //
       int    aai.OrderID                //
                                         //
                                         //
                                      )//
//</head>

  {//<body>

   avi.Position     =MathRound(avd.NominalMargin/avd.NominalTick * avd.QuoteTick/avd.QuotePoint)        ;
   avi.RiskPoints   =avd.Risk         /avd.QuotePoint                                                         ;

   avd.VARLimit     =AccountEquity ()  * avd.RiskLimit   * afd.System.RiskAdjustment(aai.OrderID )      ;
   avd.RiskPoint    =avd.VARLimit     /avi.RiskPoints                                                         ;
   avd.PositionLimit=avi.Position      * avd.RiskPoint                                                          ;
   avd.VolumeLimit  =avd.PositionLimit/avd.NominalMargin                                                      ;

   if  (avd.VolumeLimit>=avd.MinimumLots )
     { avi.Steps =MathFloor      (( avd.VolumeLimit - avd.MinimumLots )/avd.LotStep)                   ;
     avd.Volume=avd.MinimumLots   + avd.LotStep     * avi.Steps                                         ;}
   else   avd.Volume=0.0                                                                                        ;

  }//</body>

//</A System: Function 21 >

//< A System: Function 22 >

//< head>

double afd.System.RiskAdjustment      (// input 1 / code 23 / output 1
                                         //
       int    aai.OrderID                //
                                         //
                                         //
                                      )//
//</head>

  {//<body>

   if(avd.TimeFactor.2==0)return(1.0)                                                                   ;

   static double ald.RiskAdjustment                                                                               ;
   static double ald.LossProbability                                                                              ;
   static double ald.LossRatio                                                                                    ;
   avi.LossesObtained =0                                                                                        ;
   avi.TradesObtained =1                                                                                        ;

   if(OrdersHistoryTotal () > 0 )
      for( static int j;j=0;j < OrdersHistoryTotal ();j ++ )
        { OrderSelect (j , SELECT_BY_POS , MODE_HISTORY)                                                  ;
         if(TimeCurrent () - OrderCloseTime () <(avi.Period.1 + avi.Period.2)* 60 * avd.TimeFactor.2 )
            if(OrderMagicNumber ()==aai.OrderID  || OrderMagicNumber ()==aai.OrderID * ac.LockShift     )
              { avi.TradesObtained ++                                                                       ;
              if(OrderProfit () < 0)avi.LossesObtained ++                                        ;} }

   if(avi.TradesObtained  > 0 )
     { ald.LossRatio      =1.0 * avi.LossesObtained /avi.TradesObtained                                      ;
      ald.LossProbability=1.0 * avi.LossesExpected /avi.TradesExpected                                      ;
     ald.RiskAdjustment =1.0 + ald.LossProbability - ald.LossRatio                                        ;}
   else ald.RiskAdjustment =1.0                                                                                 ;

   return(ald.RiskAdjustment)                                                                    ;

  }//</body>

//</A System: Function 22 >

//< A System: Function 23 >

//< head>

int    afr.System.LockPosition        (// input - / code 15 / output -
                                         //
                                         //
                                         //
                                         //
                                      )//
//</head>

  {//<body>

   if(avb.FlagOfReturn)return                                                                                 ;

   for( static int i;i=1;i<=ac.Threads;i ++)avi.LockFlag [ i ]=EMPTY                               ;

   if(OrdersTotal () > 0 )
      for( static int j;j=0;j < OrdersTotal ();j ++ )
        { OrderSelect (j , SELECT_BY_POS , MODE_TRADES)                                               ;
         for( i=1;i<=ac.Threads;i ++ )
           if(OrderMagicNumber ()==i * ac.LockShift)avi.LockFlag [ i ]=True                       ;}

   for(                j=0;j < OrdersTotal ();j ++ )
     { OrderSelect (j , SELECT_BY_POS , MODE_TRADES)                                               ;
      if(OrderProfit () < 0 )
         for( i=1;i<=ac.Threads;i ++ )
           if(OrderMagicNumber ()==i && avi.LockFlag [ i ]!=True)    afr.System.AttemptToLock ();}

  }//</body>

//</A System: Function 23 >

//< A System: Function 24 >

//< head>

int    afr.System.AttemptToLock       (// input - / code 24 / output -
                                         //
                                         //
                                         //
                                         //
                                      )//
//</head>

  {//<body>

   if(avb.FlagOfReturn)return                                                                                 ;

   avi.Command=EMPTY                                                                                            ;

   if(OrderType  ()==OP_BUY  && afi.System.SignalToOpen ()==OP_SELL )
      if(avd.QuoteBid <=OrderOpenPrice ()   -(OrderOpenPrice () - OrderStopLoss  ())* avd.LockFactor )
         if(avd.QuoteBid >=OrderStopLoss  ()   + avd.QuoteStops
           && avd.QuoteAsk <=OrderOpenPrice ()   - avd.QuoteStops )
         { avi.Command   =OP_SELL;avd.Price=avd.QuoteBid                                                    ;}

   if(OrderType  ()==OP_SELL && afi.System.SignalToOpen ()==OP_BUY  )
      if(avd.QuoteAsk >=OrderOpenPrice ()   +(OrderStopLoss  () - OrderOpenPrice ())* avd.LockFactor )
         if(avd.QuoteAsk <=OrderStopLoss  ()   - avd.QuoteStops
           && avd.QuoteBid >=OrderOpenPrice ()   + avd.QuoteStops )
         { avi.Command   =OP_BUY ;avd.Price=avd.QuoteAsk                                                    ;}

   if(IsTradeAllowed ()    )
      if(avi.Command  > EMPTY )
        { avi.Steps   =MathFloor     (( OrderLots ()      * avd.LockSize - avd.MinimumLots )/avd.LotStep)   ;
         avd.Volume  =avd.MinimumLots  + avd.LotStep       * avi.Steps                                           ;
         avd.Stop    =NormalizeDouble (OrderOpenPrice () , avd.Digits)                                       ;
         avd.Take    =NormalizeDouble (OrderStopLoss  () , avd.Digits)                                       ;
         static int ali.OrderID;ali.OrderID=OrderMagicNumber () * ac.LockShift                                 ;
        afr.System.SendOrder  (ali.OrderID)                                                                ;}

  }//</body>

//</A System: Function 24>

//< A System: End of code >

//</A System: End of code >

//+------------------------------------------------------------------+
                                  ;
     avd.Take     = NormalizeDouble  ( OrderStopLoss  () , avd.Digits )                                        ;
     static int ali.OrderID ; ali.OrderID = OrderMagicNumber () * ac.LockShift                                 ;
     afr.System.SendOrder   ( ali.OrderID )                                                                  ; }


}//</body>

//</A System: Function 24>

//< A System: End of code >









































//</A System: End of code >

