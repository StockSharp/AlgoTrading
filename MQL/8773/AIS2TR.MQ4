//< 1. Property 7 >===========================================================================================//<   1>
                                                                                                              //<   2>
#property     copyright                 "Copyright (C) 2009, MetaQuotes Software Corp."                       //<   3>
#property     link                      "http://www.metaquotes.net"                                           //<   4>
                                                                                                              //<   5>
#define       A.System.Series           "AIS"                                                                 //<   6>
#define       A.System.Modification     "20005"                                                               //<   7>
#define       A.System.ReleaseDate      "2009.03.25"                                                          //<   8>
#define       A.System.Program          "Trading Robot"                                                       //<   9>
#define       A.System.Programmer       "Airat Safin                           http://www.mql4.com/users/Ais" //<  10>
                                                                                                              //<  11>
//</1. Property 7 >===========================================================================================//<  12>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////<  13>
//< 2. Constants 6 >==========================================================================================//<  14>
                                                                                                              //<  15>
#define       A.System.Name             "A System"                                                            //<  16>
                                                                                                              //<  17>
#define       aci.OrderID               20005                                                                 //<  18>
                                                                                                              //<  19>
#define       acd.TrailStepping         1.0                                                                   //<  20>
                                                                                                              //<  21>
#define       aci.TradingPause          5                                                                     //<  22>
                                                                                                              //<  23>
string        acs.Operation     []  = { "Buy" , "Sell"                                                    } ; //<  24>
                                                                                                              //<  25>
#define       aci.SetupSeparator        1000000000                                                            //<  26>
                                                                                                              //<  27>
//</2. Constants 6 >==========================================================================================//<  28>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////<  29>
//< 3. Presets 8 >============================================================================================//<  30>
                                                                                                              //<  31>
//< 3.1. Risk Management Preset 2 >```````````````````````````````````````````````````````````````````````````//<  32>
                                                                                                              //<  33>
extern double aed.AccountReserve      = 0.20                                                                ; //<  34>
extern double aed.OrderReserve        = 0.04                                                                ; //<  35>
                                                                                                              //<  36>
//</3.1. Risk Management Preset 2 >```````````````````````````````````````````````````````````````````````````//<  37>
                                                                                                              //<  38>
//< 3.2. Trading Strategy Preset 6 >``````````````````````````````````````````````````````````````````````````//<  39>
                                                                                                              //<  40>
extern string aes.Symbol              = "EURUSD"                                                            ; //<  41>
extern int    aei.Timeframe.1         = 15                                                                  ; //<  42>
extern int    aei.Timeframe.2         = 1                                                                   ; //<  43>
                                                                                                              //<  44>
extern double aed.TakeFactor          = 1.7                                                                 ; //<  45>
extern double aed.StopFactor          = 1.7                                                                 ; //<  46>
extern double aed.TrailFactor         = 0.5                                                                 ; //<  47>
                                                                                                              //<  48>
//</3.2. Trading Strategy Preset 6 >``````````````````````````````````````````````````````````````````````````//<  49>
                                                                                                              //<  50>
//</3. Presets 8 >============================================================================================//<  51>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////<  52>
//< 4. Global Variables 84 >==================================================================================//<  53>
                                                                                                              //<  54>
//< 4.1. Trading Strategy Interface 4 >```````````````````````````````````````````````````````````````````````//<  55>
                                                                                                              //<  56>
int           avi.Command             = EMPTY                                                               ; //<  57>
double        avd.Price               = EMPTY                                                               ; //<  58>
double        avd.Stop                = EMPTY                                                               ; //<  59>
double        avd.Take                = EMPTY                                                               ; //<  60>
                                                                                                              //<  61>
//</4.1. Trading Strategy Interface 4 >```````````````````````````````````````````````````````````````````````//<  62>
                                                                                                              //<  63>
//< 4.2. System Controls 28 >`````````````````````````````````````````````````````````````````````````````````//<  64>
                                                                                                              //<  65>
int           avi.SystemFlag                                                                                ; //<  66>
int           avi.TradingFlag                                                                               ; //<  67>
int           avi.MonitorFlag                                                                               ; //<  68>
int           avi.LiveModeFlag                                                                              ; //<  69>
                                                                                                              //<  70>
int           avi.TimeStamp                                                                                 ; //<  71>
int           avi.Exception                                                                                 ; //<  72>
int           avi.ExcepionsTrade                                                                            ; //<  73>
int           avi.ExcepionsTrail                                                                            ; //<  74>
                                                                                                              //<  75>
int           avi.TimeStart                                                                                 ; //<  76>
int           avi.TimeLastRun                                                                               ; //<  77>
int           avi.Runs                                                                                      ; //<  78>
int           avi.BuyTrades                                                                                 ; //<  79>
int           avi.SellTrades                                                                                ; //<  80>
int           avi.TotalTrades                                                                               ; //<  81>
int           avi.Trailes                                                                                   ; //<  82>
int           avi.AttemptsTrade                                                                             ; //<  83>
int           avi.AttemptsTrail                                                                             ; //<  84>
                                                                                                              //<  85>
double        avd.Capital                                                                                   ; //<  86>
double        avd.PeakTime                                                                                  ; //<  87>
double        avd.PeakEquity                                                                                ; //<  88>
double        avd.InitialEquity                                                                             ; //<  89>
double        avd.InitialCapital                                                                            ; //<  90>
double        avd.EquityReserve                                                                             ; //<  91>
                                                                                                              //<  92>
string        avs.Currency      []  = { "" , "" , "" , ""                                                 } ; //<  93>
#define       ari.Account               0                                                                     //<  94>
#define       ari.Base                  1                                                                     //<  95>
#define       ari.Quote                 2                                                                     //<  96>
#define       ari.Margin                3                                                                     //<  97>
                                                                                                              //<  98>
//</4.2. System Controls 28 >`````````````````````````````````````````````````````````````````````````````````//<  99>
                                                                                                              //< 100>
//< 4.3. Preset Control 10 >``````````````````````````````````````````````````````````````````````````````````//< 101>
                                                                                                              //< 102>
//< 4.3.1. Setup Separators 2 >                                                                               //< 103>
string        avs.SetupBegin                                                                                ; //< 104>
string        avs.SetupEnd                                                                                  ; //< 105>
//</4.3.1. Setup Separators 2 >                                                                               //< 106>
                                                                                                              //< 107>
//< 4.3.2. Risk Management Preset Setup 2 >                                                                   //< 108>
string        avs.SetupAccountReserve                                                                       ; //< 109>
string        avs.SetupOrderReserve                                                                         ; //< 110>
//</4.3.2. Risk Management Preset Setup 2 >                                                                   //< 111>
                                                                                                              //< 112>
//< 4.3.3. Trading Preset Setup 6 >                                                                           //< 113>
string        avs.SetupTrading                                                                              ; //< 114>
string        avs.SetupTimeframe.1                                                                          ; //< 115>
string        avs.SetupTimeframe.2                                                                          ; //< 116>
string        avs.SetupTakeFactor                                                                           ; //< 117>
string        avs.SetupStopFactor                                                                           ; //< 118>
string        avs.SetupTrailFactor                                                                          ; //< 119>
//</4.3.3. Trading Preset Setup 6 >                                                                           //< 120>
                                                                                                              //< 121>
//</4.3. Preset Control 10 >``````````````````````````````````````````````````````````````````````````````````//< 122>
                                                                                                              //< 123>
//< 4.5. System Messages 4 >``````````````````````````````````````````````````````````````````````````````````//< 124>
                                                                                                              //< 125>
string        avs.SystemMessage                                                                             ; //< 126>
string        avs.LocalMessage                                                                              ; //< 127>
string        avs.SystemStamp                                                                               ; //< 128>
string        avs.LocalStamp                                                                                ; //< 129>
                                                                                                              //< 130>
//</4.4. System Messages 4 >``````````````````````````````````````````````````````````````````````````````````//< 131>
                                                                                                              //< 132>
//< 4.5. Common Data 14 >`````````````````````````````````````````````````````````````````````````````````````//< 133>
                                                                                                              //< 134>
double        avd.QuoteAsk                                                                                  ; //< 135>
double        avd.QuoteBid                                                                                  ; //< 136>
double        avd.QuotePoint                                                                                ; //< 137>
double        avd.QuoteSpread                                                                               ; //< 138>
double        avd.QuoteFreeze                                                                               ; //< 139>
double        avd.QuoteStops                                                                                ; //< 140>
double        avd.QuoteTick                                                                                 ; //< 141>
double        avd.NominalTick                                                                               ; //< 142>
double        avd.NominalMargin                                                                             ; //< 143>
double        avd.NominalLot                                                                                ; //< 144>
double        avd.MaximumLots                                                                               ; //< 145>
double        avd.MinimumLots                                                                               ; //< 146>
double        avd.LotStep                                                                                   ; //< 147>
int           avi.Digits                                                                                    ; //< 148>
                                                                                                              //< 149>
//</4.5. Common Data 14 >`````````````````````````````````````````````````````````````````````````````````````//< 150>
                                                                                                              //< 151>
//< 4.6. Trading Strategy Data 15 >```````````````````````````````````````````````````````````````````````````//< 152>
                                                                                                              //< 153>
int           avi.Period.1                                                                                  ; //< 154>
int           avi.Period.2                                                                                  ; //< 155>
                                                                                                              //< 156>
double        avd.Low.1                                                                                     ; //< 157>
double        avd.High.1                                                                                    ; //< 158>
double        avd.Close.1                                                                                   ; //< 159>
                                                                                                              //< 160>
double        avd.Low.2                                                                                     ; //< 161>
double        avd.High.2                                                                                    ; //< 162>
double        avd.Close.2                                                                                   ; //< 163>
                                                                                                              //< 164>
double        avd.Average.1                                                                                 ; //< 165>
                                                                                                              //< 166>
double        avd.Range.1                                                                                   ; //< 167>
double        avd.Range.2                                                                                   ; //< 168>
                                                                                                              //< 169>
double        avd.QuoteTake                                                                                 ; //< 170>
double        avd.QuoteStop                                                                                 ; //< 171>
double        avd.QuoteTrail                                                                                ; //< 172>
                                                                                                              //< 173>
double        avd.TrailStep                                                                                 ; //< 174>
                                                                                                              //< 175>
//</4.6. Trading Strategy Data 15 >```````````````````````````````````````````````````````````````````````````//< 176>
                                                                                                              //< 177>
//< 4.7. Risk Management Data 9 >`````````````````````````````````````````````````````````````````````````````//< 178>
                                                                                                              //< 179>
double        avd.QuoteTarget                                                                               ; //< 180>
double        avd.QuoteRisk                                                                                 ; //< 181>
double        avd.NominalPoint                                                                              ; //< 182>
int           avi.MarginPoints                                                                              ; //< 183>
int           avi.RiskPoints                                                                                ; //< 184>
double        avd.VARLimit                                                                                  ; //< 185>
double        avd.RiskPoint                                                                                 ; //< 186>
double        avd.MarginLimit                                                                               ; //< 187>
double        avd.SizeLimit                                                                                 ; //< 188>
                                                                                                              //< 189>
//</4.7. Risk Management Data 9 >`````````````````````````````````````````````````````````````````````````````//< 190>
                                                                                                              //< 191>
//</4. Global Variables 84 >==================================================================================//< 192>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////< 193>
//< 5. Program Initialization 21 >============================================================================//< 194>
                                                                                                              //< 195>
int    init                          ()                                                                       //< 196>
{                                                                                                             //< 197>
                                                                                                              //< 198>
//< 5.1. System Controls Reset 8 >````````````````````````````````````````````````````````````````````````````//< 199>
                                                                                                              //< 200>
avi.TimeStart            = TimeLocal     ()                                                                 ; //< 201>
avi.TimeStamp            = TimeLocal     ()                                                                 ; //< 202>
                                                                                                              //< 203>
avd.PeakTime             = TimeLocal     ()                                                                 ; //< 204>
avd.PeakEquity           = AccountEquity ()                                                                 ; //< 205>
avd.InitialEquity        = AccountEquity ()                                                                 ; //< 206>
avd.InitialCapital       = avd.PeakEquity   * ( 1 - aed.AccountReserve )                                    ; //< 207>
                                                                                                              //< 208>
avi.TradingFlag          = 1                                                                                ; //< 209>
avi.MonitorFlag          = 1                                                                                ; //< 210>
                                                                                                              //< 211>
//</5.1. System Controls Reset 8 >````````````````````````````````````````````````````````````````````````````//< 212>
                                                                                                              //< 213>
//< 5.2. System Stamp Reset 3 >```````````````````````````````````````````````````````````````````````````````//< 214>
                                                                                                              //< 215>
avs.SystemStamp          = A.System.Series                                 +                                  //< 216>
                           A.System.Modification                           + " "  +                           //< 217>
                           A.System.Program                                                                 ; //< 218>
                                                                                                              //< 219>
//</5.2. System Stamp Reset 3 >```````````````````````````````````````````````````````````````````````````````//< 220>
                                                                                                              //< 221>
//< 5.3. First Alert 8 >``````````````````````````````````````````````````````````````````````````````````````//< 222>
                                                                                                              //< 223>
Alert                    ( avs.SystemStamp                                 , ": Symbol="                  ,   //< 224>
                           aes.Symbol                                      , ", Preset="                  ,   //< 225>
                           aei.Timeframe.1                                 , "/"                          ,   //< 226>
                           aei.Timeframe.2                                 , "/"                          ,   //< 227>
                           DoubleToStr             ( aed.TakeFactor  , 1 ) , "/"                          ,   //< 228>
                           DoubleToStr             ( aed.StopFactor  , 1 ) , "/"                          ,   //< 229>
                           DoubleToStr             ( aed.TrailFactor , 1 ) , " "                              //< 230>
                                                   , ", Reload code="      ,        UninitializeReason () ) ; //< 231>
                                                                                                              //< 232>
//</5.3. First Alert 8 >``````````````````````````````````````````````````````````````````````````````````````//< 233>
                                                                                                              //< 234>
//< 5.4. Setup Reset 1 >``````````````````````````````````````````````````````````````````````````````````````//< 235>
                                                                                                              //< 236>
afr.CreateSetup                      ()                                                                     ; //< 237>
                                                                                                              //< 238>
//</5.4. Setup Reset 1 >``````````````````````````````````````````````````````````````````````````````````````//< 239>
                                                                                                              //< 240>
//< 5.5. Monitoring Panel Reset 1 >```````````````````````````````````````````````````````````````````````````//< 241>
                                                                                                              //< 242>
afr.CreatePanel.1                    ()                                                                     ; //< 243>
                                                                                                              //< 244>
//</5.5. Monitoring Panel Reset 1 >```````````````````````````````````````````````````````````````````````````//< 245>
}                                                                                                             //< 246>
//</5. Program Initialization 21 >============================================================================//< 247>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////< 248>
//< 6. Program Deinitialization 5 >===========================================================================//< 249>
                                                                                                              //< 250>
int    deinit                        ()                                                                       //< 251>
{                                                                                                             //< 252>
                                                                                                              //< 253>
//< 6.1. Setup Deletion 1 >```````````````````````````````````````````````````````````````````````````````````//< 254>
                                                                                                              //< 255>
afr.DeleteSetup                      ()                                                                     ; //< 256>
                                                                                                              //< 257>
//</6.1. Setup Deletion 1 >```````````````````````````````````````````````````````````````````````````````````//< 258>
                                                                                                              //< 259>
//< 6.2. Monitoring Panel Deletion 1 >````````````````````````````````````````````````````````````````````````//< 260>
                                                                                                              //< 261>
afr.DeletePanel.1                    ()                                                                     ; //< 262>
                                                                                                              //< 263>
//</6.2. Monitoring Panel Deletion 1 >````````````````````````````````````````````````````````````````````````//< 264>
                                                                                                              //< 265>
//< 6.3. Final Alert 3 >``````````````````````````````````````````````````````````````````````````````````````//< 266>
                                                                                                              //< 267>
Alert                    ( avs.SystemStamp         , ": Stop code="        ,        UninitializeReason ()     //< 268>
                                                                           , "/"  , avi.ExcepionsTrade        //< 269>
                                                                           , "/"  , avi.ExcepionsTrail    ) ; //< 270>
                                                                                                              //< 271>
//</6.3. Final Alert 3 >``````````````````````````````````````````````````````````````````````````````````````//< 272>
}                                                                                                             //< 273>
//</6. Program Deinitialization 5 >===========================================================================//< 274>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////< 275>
//< 7. Main Program 266 >=====================================================================================//< 276>
                                                                                                              //< 277>
int    start                         ()                                                                       //< 278>
{                                                                                                             //< 279>
                                                                                                              //< 280>
//< 7.1. System Controls Reset On Enter 75 >``````````````````````````````````````````````````````````````````//< 281>
                                                                                                              //< 282>
//< 7.1.1. Run Mode Detection 1 >                                                                             //< 283>
avi.LiveModeFlag           = ! IsTesting () || IsVisualMode ()                                              ; //< 284>
//</7.1.1. Run Mode Detection 1 >                                                                             //< 285>
                                                                                                              //< 286>
//< 7.1.2. Live Mode Subroutine 23 >                                                                          //< 287>
if (   avi.LiveModeFlag   == 1        )                                                                       //< 288>
   {                                                                                                          //< 289>
       if ( ! GlobalVariableCheck     ( avs.SetupBegin          ) )                                           //< 290>
              GlobalVariableSet       ( avs.SetupBegin            , aci.SetupSeparator                    ) ; //< 291>
       if ( ! GlobalVariableCheck     ( avs.SetupAccountReserve ) )                                           //< 292>
              GlobalVariableSet       ( avs.SetupAccountReserve   , aed.AccountReserve                    ) ; //< 293>
       if ( ! GlobalVariableCheck     ( avs.SetupOrderReserve   ) )                                           //< 294>
              GlobalVariableSet       ( avs.SetupOrderReserve     , aed.OrderReserve                      ) ; //< 295>
       if ( ! GlobalVariableCheck     ( avs.SetupTrading        ) )                                           //< 296>
              GlobalVariableSet       ( avs.SetupTrading          , avi.TradingFlag                       ) ; //< 297>
       if ( ! GlobalVariableCheck     ( avs.SetupTimeframe.1    ) )                                           //< 298>
              GlobalVariableSet       ( avs.SetupTimeframe.1      , aei.Timeframe.1                       ) ; //< 299>
       if ( ! GlobalVariableCheck     ( avs.SetupTimeframe.2    ) )                                           //< 300>
              GlobalVariableSet       ( avs.SetupTimeframe.2      , aei.Timeframe.2                       ) ; //< 301>
       if ( ! GlobalVariableCheck     ( avs.SetupTakeFactor     ) )                                           //< 302>
              GlobalVariableSet       ( avs.SetupTakeFactor       , aed.TakeFactor                        ) ; //< 303>
       if ( ! GlobalVariableCheck     ( avs.SetupStopFactor     ) )                                           //< 304>
              GlobalVariableSet       ( avs.SetupStopFactor       , aed.StopFactor                        ) ; //< 305>
       if ( ! GlobalVariableCheck     ( avs.SetupTrailFactor    ) )                                           //< 306>
              GlobalVariableSet       ( avs.SetupTrailFactor      , aed.TrailFactor                       ) ; //< 307>
       if ( ! GlobalVariableCheck     ( avs.SetupEnd            ) )                                           //< 308>
              GlobalVariableSet       ( avs.SetupEnd              , aci.SetupSeparator                    ) ; //< 309>
   }                                                                                                          //< 310>
//</7.1.2. Live Mode Subroutine 23 >                                                                          //< 311>
                                                                                                              //< 312>
//< 7.1.3. All Modes Subroutine 8 >                                                                           //< 313>
if (   GlobalVariableGet      ( avs.SetupTrading )  == 1 )                                                    //< 314>
   {   avi.SystemFlag         = 1                                                                         ;   //< 315>
       avi.TradingFlag        = 1                                                                         ;   //< 316>
       avs.SystemMessage      = "Trading is enabled"                                                      ; } //< 317>
else                                                                                                          //< 318>
   {   avi.SystemFlag         = 0                                                                         ;   //< 319>
       avi.TradingFlag        = 0                                                                         ;   //< 320>
       avs.SystemMessage      = "Trading is disabled"                                                     ; } //< 321>
//</7.1.3. All Modes Subroutine 8 >                                                                           //< 322>
                                                                                                              //< 323>
//< 7.1.4. Live Mode Subroutine 43 >                                                                          //< 324>
if (   avi.LiveModeFlag      == 1 )                                                                           //< 325>
   {                                                                                                          //< 326>
       aed.AccountReserve     = GlobalVariableGet    ( avs.SetupAccountReserve                            ) ; //< 327>
       aed.OrderReserve       = GlobalVariableGet    ( avs.SetupOrderReserve                              ) ; //< 328>
       aei.Timeframe.1        = GlobalVariableGet    ( avs.SetupTimeframe.1                               ) ; //< 329>
       aei.Timeframe.2        = GlobalVariableGet    ( avs.SetupTimeframe.2                               ) ; //< 330>
       aed.TakeFactor         = GlobalVariableGet    ( avs.SetupTakeFactor                                ) ; //< 331>
       aed.StopFactor         = GlobalVariableGet    ( avs.SetupStopFactor                                ) ; //< 332>
       aed.TrailFactor        = GlobalVariableGet    ( avs.SetupTrailFactor                               ) ; //< 333>
                                                                                                              //< 334>
       if ( aei.Timeframe.1  != PERIOD_M1                                                                     //< 335>
         && aei.Timeframe.1  != PERIOD_M5                                                                     //< 336>
         && aei.Timeframe.1  != PERIOD_M15                                                                    //< 337>
         && aei.Timeframe.1  != PERIOD_M30                                                                    //< 338>
         && aei.Timeframe.1  != PERIOD_H1                                                                     //< 339>
         && aei.Timeframe.1  != PERIOD_H4                                                                     //< 340>
         && aei.Timeframe.1  != PERIOD_D1                                                                     //< 341>
         && aei.Timeframe.1  != PERIOD_W1                                                                     //< 342>
         && aei.Timeframe.1  != PERIOD_MN1 )                                                                  //< 343>
          { aei.Timeframe.1   = 0                                                                         ;   //< 344>
            avi.SystemFlag    = 0                                                                         ;   //< 345>
            avi.TradingFlag   = 0                                                                         ;   //< 346>
            GlobalVariableSet ( avs.SetupTimeframe.1 , aei.Timeframe.1                                  ) ;   //< 347>
            GlobalVariableSet ( avs.SetupTrading     , avi.TradingFlag                                  ) ;   //< 348>
            avs.SystemMessage = "Check Timeframe.1"                                                       ;   //< 349>
            Alert             ( avs.SystemStamp                              , ": Symbol="                ,   //< 350>
                                aes.Symbol                                   , " "  , avs.SystemMessage ) ; } //< 351>
                                                                                                              //< 352>
       if ( aei.Timeframe.2  != PERIOD_M1                                                                     //< 353>
         && aei.Timeframe.2  != PERIOD_M5                                                                     //< 354>
         && aei.Timeframe.2  != PERIOD_M15                                                                    //< 355>
         && aei.Timeframe.2  != PERIOD_M30                                                                    //< 356>
         && aei.Timeframe.2  != PERIOD_H1                                                                     //< 357>
         && aei.Timeframe.2  != PERIOD_H4                                                                     //< 358>
         && aei.Timeframe.2  != PERIOD_D1                                                                     //< 359>
         && aei.Timeframe.2  != PERIOD_W1                                                                     //< 360>
         && aei.Timeframe.2  != PERIOD_MN1 )                                                                  //< 361>
          { aei.Timeframe.2   = 0                                                                         ;   //< 362>
            avi.SystemFlag    = 0                                                                         ;   //< 363>
            avi.TradingFlag   = 0                                                                         ;   //< 364>
            GlobalVariableSet ( avs.SetupTimeframe.2 , aei.Timeframe.2                                  ) ;   //< 365>
            GlobalVariableSet ( avs.SetupTrading     , avi.TradingFlag                                  ) ;   //< 366>
            avs.SystemMessage = "Check Timeframe.2"                                                       ;   //< 367>
            Alert              ( avs.SystemStamp                             , ": Symbol="                ,   //< 368>
                                 aes.Symbol                                  , " "  , avs.SystemMessage ) ; } //< 369>
   }                                                                                                          //< 370>
//</7.1.4. Live Mode Subroutine 43 >                                                                          //< 371>
                                                                                                              //< 372>
//</7.1. System Controls Reset On Enter 75 >``````````````````````````````````````````````````````````````````//< 373>
                                                                                                              //< 374>
//</7.2. Trading Pause Control 2 >````````````````````````````````````````````````````````````````````````````//< 375>
                                                                                                              //< 376>
if   ( TimeLocal     ()  - avi.TimeStamp           < aci.TradingPause )                                       //< 377>
     { avs.SystemMessage = "Trading pause "        + aci.TradingPause +   " seconds" ; avi.SystemFlag = 0 ; } //< 378>
                                                                                                              //< 379>
//</7.2. Trading Pause Control 2 >````````````````````````````````````````````````````````````````````````````//< 380>
                                                                                                              //< 381>
//< 7.3. Equity Control 6 >```````````````````````````````````````````````````````````````````````````````````//< 382>
                                                                                                              //< 383>
if   ( AccountEquity ()  - avd.PeakEquity > 0      )                                                          //< 384>
     { avd.PeakEquity    = AccountEquity ()                                                                 ; //< 385>
       avd.PeakTime      = TimeLocal     ()                                                               ; } //< 386>
                                                                                                              //< 387>
       avd.Capital       = avd.PeakEquity          * ( 1 - aed.AccountReserve )                             ; //< 388>
       avd.EquityReserve = AccountEquity ()        - avd.Capital                                            ; //< 389>
       avd.VARLimit      = AccountEquity ()        * aed.OrderReserve                                       ; //< 390>
                                                                                                              //< 391>
if   ( avd.EquityReserve - avd.VARLimit   < 0      )                                                          //< 392>
     { avs.SystemMessage = "System stop"                                             ; avi.SystemFlag = 0 ; } //< 393>
                                                                                                              //< 394>
//</7.3. Equity Control 6 >```````````````````````````````````````````````````````````````````````````````````//< 395>
                                                                                                              //< 396>
//< 7.4. Data Feed 29 >```````````````````````````````````````````````````````````````````````````````````````//< 397>
                                                                                                              //< 398>
//< 7.4.1. Common Data 14 >                                                                                   //< 399>
       avd.QuoteAsk      = MarketInfo ( aes.Symbol , MODE_ASK            )                                  ; //< 400>
       avd.QuoteBid      = MarketInfo ( aes.Symbol , MODE_BID            )                                  ; //< 401>
       avd.QuotePoint    = MarketInfo ( aes.Symbol , MODE_POINT          )                                  ; //< 402>
       avd.QuoteSpread   = MarketInfo ( aes.Symbol , MODE_SPREAD         ) * avd.QuotePoint                 ; //< 403>
       avd.QuoteFreeze   = MarketInfo ( aes.Symbol , MODE_FREEZELEVEL    ) * avd.QuotePoint                 ; //< 404>
       avd.QuoteStops    = MarketInfo ( aes.Symbol , MODE_STOPLEVEL      ) * avd.QuotePoint                 ; //< 405>
       avd.QuoteTick     = MarketInfo ( aes.Symbol , MODE_TICKSIZE       )                                  ; //< 406>
       avd.NominalTick   = MarketInfo ( aes.Symbol , MODE_TICKVALUE      )                                  ; //< 407>
       avd.NominalMargin = MarketInfo ( aes.Symbol , MODE_MARGINREQUIRED )                                  ; //< 408>
       avd.NominalLot    = MarketInfo ( aes.Symbol , MODE_LOTSIZE        )                                  ; //< 409>
       avd.MaximumLots   = MarketInfo ( aes.Symbol , MODE_MAXLOT         )                                  ; //< 410>
       avd.MinimumLots   = MarketInfo ( aes.Symbol , MODE_MINLOT         )                                  ; //< 411>
       avd.LotStep       = MarketInfo ( aes.Symbol , MODE_LOTSTEP        )                                  ; //< 412>
       avi.Digits        = MarketInfo ( aes.Symbol , MODE_DIGITS         )                                  ; //< 413>
//</7.4.1. Common Data 14 >                                                                                   //< 414>
                                                                                                              //< 415>
//< 7.4.2. Trading Strategy Data 15 >                                                                         //< 416>
       avi.Period.1      = aei.Timeframe.1                                                                  ; //< 417>
       avi.Period.2      = aei.Timeframe.2                                                                  ; //< 418>
                                                                                                              //< 419>
       avd.Low.1         = iLow       ( aes.Symbol , avi.Period.1 , 1    )                                  ; //< 420>
       avd.High.1        = iHigh      ( aes.Symbol , avi.Period.1 , 1    )                                  ; //< 421>
       avd.Close.1       = iClose     ( aes.Symbol , avi.Period.1 , 1    )                                  ; //< 422>
                                                                                                              //< 423>
       avd.Low.2         = iLow       ( aes.Symbol , avi.Period.2 , 1    )                                  ; //< 424>
       avd.High.2        = iHigh      ( aes.Symbol , avi.Period.2 , 1    )                                  ; //< 425>
       avd.Close.2       = iClose     ( aes.Symbol , avi.Period.2 , 1    )                                  ; //< 426>
                                                                                                              //< 427>
       avd.Average.1     =            ( avd.High.1 + avd.Low.1           ) / 2                              ; //< 428>
                                                                                                              //< 429>
       avd.Range.1       =              avd.High.1 - avd.Low.1                                              ; //< 430>
       avd.Range.2       =              avd.High.2 - avd.Low.2                                              ; //< 431>
                                                                                                              //< 432>
       avd.QuoteTake     = avd.Range.1             * aed.TakeFactor                                         ; //< 433>
       avd.QuoteStop     = avd.Range.1             * aed.StopFactor                                         ; //< 434>
       avd.QuoteTrail    = avd.Range.2             * aed.TrailFactor                                        ; //< 435>
                                                                                                              //< 436>
       avd.TrailStep     = avd.QuoteSpread         * acd.TrailStepping                                      ; //< 437>
//</7.4.2. Trading Strategy Data 15 >                                                                         //< 438>
                                                                                                              //< 439>
//</7.4. Data Feed 29 >```````````````````````````````````````````````````````````````````````````````````````//< 440>
                                                                                                              //< 441>
//< 7.5. Trading Strategy Interface Reset 4 >`````````````````````````````````````````````````````````````````//< 442>
                                                                                                              //< 443>
       avi.Command       = EMPTY                                                                            ; //< 444>
       avd.Price         = EMPTY                                                                            ; //< 445>
       avd.Stop          = EMPTY                                                                            ; //< 446>
       avd.Take          = EMPTY                                                                            ; //< 447>
                                                                                                              //< 448>
//</7.5. Trading Strategy Interface Reset 4 >`````````````````````````````````````````````````````````````````//< 449>
                                                                                                              //< 450>
//< 7.6. Position Management Module 55 >``````````````````````````````````````````````````````````````````````//< 451>
                                                                                                              //< 452>
//< 7.6.1. Position Management Module Entry Point 7 >                                                         //< 453>
if ( avi.SystemFlag  == 1             )                                                                       //< 454>
if ( OrdersTotal ()   > 0             )                                                                       //< 455>
                                                                                                              //< 456>
   { int   i , N ; N  = OrdersTotal  () - 1                                                                 ; //< 457>
     for ( i = N ; i >= 0      ; i -- )                                                                       //< 458>
       {      OrderSelect      ( i , SELECT_BY_POS , MODE_TRADES        )                                   ; //< 459>
         if ( OrderMagicNumber () != aci.OrderID                        )                          continue ; //< 460>
         else                                                                            avi.SystemFlag = 0 ; //< 461>
//</7.6.1. Position Management Module Entry Point 7 >                                                         //< 462>
                                                                                                              //< 463>
//< 7.6.2. Trailing Logic 18 >                                                                                //< 464>
         //<  Buy Orders Trailing Rules >                                                                     //< 465>
         if ( OrderType       () == OP_BUY                                                                  ) //< 466>
         if ( OrderProfit     ()  > 0                                                                       ) //< 467>
         if ( NormalizeDouble ( avd.QuoteTrail     - avd.QuoteStops                      , avi.Digits ) > 0 ) //< 468>
         if ( NormalizeDouble ( avd.QuoteTrail     - avd.QuoteFreeze                     , avi.Digits ) > 0 ) //< 469>
         if ( NormalizeDouble ( OrderTakeProfit () - avd.QuoteBid       - avd.QuoteStops                      //< 470>
                                                                                         , avi.Digits ) > 0 ) //< 471>
         if ( NormalizeDouble ( avd.QuoteBid       - OrderStopLoss ()   - avd.TrailStep  - avd.QuoteTrail     //< 472>
                                                                                         , avi.Digits ) > 0 ) //< 473>
              avd.Stop        = NormalizeDouble (    avd.QuoteBid       - avd.QuoteTrail , avi.Digits     ) ; //< 474>
         //</ Buy Orders Trailing Rules >                                                                     //< 475>
                                                                                                              //< 476>
         //<  Sell Orders Trailing Rules >                                                                    //< 477>
         if ( OrderType       () == OP_SELL                                                                 ) //< 478>
         if ( OrderProfit     ()  > 0                                                                       ) //< 479>
         if ( NormalizeDouble ( avd.QuoteTrail     - avd.QuoteStops                      , avi.Digits ) > 0 ) //< 480>
         if ( NormalizeDouble ( avd.QuoteTrail     - avd.QuoteFreeze                     , avi.Digits ) > 0 ) //< 481>
         if ( NormalizeDouble ( avd.QuoteAsk       - OrderTakeProfit () - avd.QuoteStops                      //< 482>
                                                                                         , avi.Digits ) > 0 ) //< 483>
         if ( NormalizeDouble ( OrderStopLoss   () - avd.QuoteAsk       - avd.TrailStep  - avd.QuoteTrail     //< 484>
                                                                                         , avi.Digits ) > 0 ) //< 485>
              avd.Stop        = NormalizeDouble (    avd.QuoteAsk       + avd.QuoteTrail , avi.Digits     ) ; //< 486>
         //</ Sell Orders Trailing Rules >                                                                    //< 487>
//</7.6.2. Trailing Logic 18 >                                                                                //< 488>
                                                                                                              //< 489>
//< 7.6.3. Order Modify Trading Function 28 >                                                                 //< 490>
         if ( avd.Stop                > 0 )                                                                   //< 491>
            { int  ali.TrailPoints    = MathRound       (    MathAbs       ( OrderStopLoss ()  - avd.Stop )   //< 492>
                                                                           / avd.QuotePoint               ) ; //< 493>
              if ( ali.TrailPoints   >= MarketInfo      (    aes.Symbol    , MODE_FREEZELEVEL           ) )   //< 494>
                 {                                                                                            //< 495>
               //< Trading Function Execution Sequence >                                                      //< 496>
               //< Step 1 >                                                                                   //< 497>
                   avs.LocalStamp     = avs.SystemStamp                           + ": Attempt to trail " +   //< 498>
                                        aes.Symbol                                                 + " "  +   //< 499>
                                        acs.Operation    [   OrderType ()  ]                       + " #" +   //< 500>
                                        OrderMagicNumber ()                                        + "/"  +   //< 501>
                                        OrderTicket      ()                                                 ; //< 502>
               //< Step 2 >                                                                                   //< 503>
                   Alert              ( avs.LocalStamp                                           ,     " +" , //< 504>
                                        ali.TrailPoints                                          , " from " , //< 505>
                                        DoubleToStr     (    OrderStopLoss ()     , avi.Digits ) ,   " to " , //< 506>
                                        DoubleToStr     (    avd.Stop             , avi.Digits )          ) ; //< 507>
               //< Step 3 >                                                                                   //< 508>
                   OrderModify        ( OrderTicket     () ,                                                  //< 509>
                                        OrderOpenPrice  () ,                                                  //< 510>
                                        avd.Stop           ,                                                  //< 511>
                                        OrderTakeProfit () , 0 , 0                                        ) ; //< 512>
               //< Step 4 >                                                                                   //< 513>
                   avi.TimeStamp      = TimeLocal       ()                                                  ; //< 514>
                                                                                                              //< 515>
               //< Step 5 >                                                                                   //< 516>
                   avi.Exception      = GetLastError    ()                                                  ; //< 517>
                                                                                                              //< 518>
               //< Step 6 >                                                                                   //< 519>
                   if ( avi.Exception == 0 ) avs.LocalMessage =            " Success "                      ; //< 520>
                   else                      avs.LocalMessage =            " Failure "      + avi.Exception ; //< 521>
                                                                                                              //< 522>
               //< Step 7 >                                                                                   //< 523>
                   avi.AttemptsTrail                          ++                                            ; //< 524>
                   if ( avi.Exception == 0 ) avi.Trailes      ++                                            ; //< 525>
                   else avi.ExcepionsTrail                    ++                                            ; //< 526>
                                                                                                              //< 527>
               //< Step 8 >                                                                                   //< 528>
                   Alert              ( avs.LocalStamp                     + avs.LocalMessage             ) ; //< 529>
                                                                                                              //< 530>
               //< Step 9 >                                                                                   //< 531>
                   avs.SystemMessage  = "Trailing Stop"                    + avs.LocalMessage               ; //< 532>
                                                                                                              //< 533>
          //</Trading Function Execution Sequence >                                                           //< 534>
                 } // if 7.6.3                                                                                //< 535>
            } // if 7.6.3                                                                                     //< 536>
//</7.6.3. Order Modify Trading Function 28 >                                                                 //< 537>
                                                                                                              //< 538>
//< 7.6.4. Position Management Module Exit Point 2 >                                                          //< 539>
       } // for 7.6.1                                                                                         //< 540>
   } // if 7.6.1                                                                                              //< 541>
//</7.6.4. Position Management Module Exit Point 2 >                                                          //< 542>
                                                                                                              //< 543>
//</7.6. Position Management Module 55 >``````````````````````````````````````````````````````````````````````//< 544>
                                                                                                              //< 545>
//< 7.7. Trading Strategy Logic 33 >``````````````````````````````````````````````````````````````````````````//< 546>
                                                                                                              //< 547>
//< 7.7.1. Trading Strategy Entry Point 2 >                                                                   //< 548>
if ( avi.SystemFlag       == 1 )                                                                              //< 549>
   {                                                                                                          //< 550>
//</7.7.1. Trading Strategy Entry Point 2 >                                                                   //< 551>
                                                                                                              //< 552>
//< 7.7.2. Buy Rules 2 >                                                                                      //< 553>
     if ( NormalizeDouble  ( avd.Close.1   -   avd.Average.1                             , avi.Digits ) > 0 ) //< 554>
     if ( NormalizeDouble  ( avd.QuoteAsk  - ( avd.High.1 + avd.QuoteSpread )            , avi.Digits ) > 0 ) //< 555>
//</7.7.2. Buy Rules 2 >                                                                                      //< 556>
                                                                                                              //< 557>
//< 7.7.3. Trading Strategy Interface Set for Buy 8 >                                                         //< 558>
        { avd.Price        = NormalizeDouble ( avd.QuoteAsk                                  , avi.Digits ) ; //< 559>
          avd.Stop         = NormalizeDouble ( avd.High.1 + avd.QuoteSpread  - avd.QuoteStop , avi.Digits ) ; //< 560>
          avd.Take         = NormalizeDouble ( avd.QuoteAsk                  + avd.QuoteTake , avi.Digits ) ; //< 561>
                                                                                                              //< 562>
          if               ( NormalizeDouble (                                                                //< 563>
                           ( avd.Take  - avd.Price                    ) - avd.QuoteStops , avi.Digits ) > 0 ) //< 564>
          if               ( NormalizeDouble (                                                                //< 565>
                           ( avd.Price - avd.QuoteSpread - avd.Stop   ) - avd.QuoteStops , avi.Digits ) > 0 ) //< 566>
               avi.Command = OP_BUY                                                                       ; } //< 567>
//</7.7.3. Trading Strategy Interface Set for Buy 8 >                                                         //< 568>
                                                                                                              //< 569>
//< 7.7.4. Sell Rules 2 >                                                                                     //< 570>
     if ( NormalizeDouble  ( avd.Close.1     - avd.Average.1                             , avi.Digits ) < 0 ) //< 571>
     if ( NormalizeDouble  ( avd.QuoteBid    - avd.Low.1                                 , avi.Digits ) < 0 ) //< 572>
//</7.7.4. Sell Rules 2 >                                                                                     //< 573>
                                                                                                              //< 574>
//< 7.7.5. Trading Strategy Interface Set for Sell 8 >                                                        //< 575>
        { avd.Price        = NormalizeDouble ( avd.QuoteBid                                  , avi.Digits ) ; //< 576>
          avd.Stop         = NormalizeDouble ( avd.Low.1                + avd.QuoteStop      , avi.Digits ) ; //< 577>
          avd.Take         = NormalizeDouble ( avd.QuoteBid             - avd.QuoteTake      , avi.Digits ) ; //< 578>
                                                                                                              //< 579>
          if               ( NormalizeDouble (                                                                //< 580>
                           ( avd.Price - avd.Take                     ) - avd.QuoteStops , avi.Digits ) > 0 ) //< 581>
          if               ( NormalizeDouble (                                                                //< 582>
                           ( avd.Stop  - avd.Price - avd.QuoteSpread  ) - avd.QuoteStops , avi.Digits ) > 0 ) //< 583>
               avi.Command = OP_SELL                                                                      ; } //< 584>
//</7.7.5. Trading Strategy Interface Set for Sell 8 >                                                        //< 585>
                                                                                                              //< 586>
//< 7.7.6. Trading Strategy Exit Point 1 >                                                                    //< 587>
   } // if 7.7.1                                                                                              //< 588>
//</7.7.6. Trading Strategy Exit Point 1 >                                                                    //< 589>
                                                                                                              //< 590>
//</7.7. Trading Strategy Logic 33 >``````````````````````````````````````````````````````````````````````````//< 591>
                                                                                                              //< 592>
//< 7.8. Trading Module 59 >``````````````````````````````````````````````````````````````````````````````````//< 593>
                                                                                                              //< 594>
//< 7.8.1. Trading Module Entry Point 3 >                                                                     //< 595>
if   ( avi.Command       > EMPTY      )                                                                       //< 596>
if   ( IsTradeAllowed   ()            )                                                                       //< 597>
     {                                                                                                        //< 598>
//</7.8.1. Trading Module Entry Point 3 >                                                                     //< 599>
                                                                                                              //< 600>
//< 7.8.2. Risk Management 9 >                                                                                //< 601>
       avd.QuoteTarget                = MathAbs            ( avd.Price           - avd.Take               ) ; //< 602>
       avd.QuoteRisk                  = MathAbs            ( avd.Price           - avd.Stop               ) ; //< 603>
       avd.NominalPoint               = avd.NominalTick    * avd.QuotePoint      / avd.QuoteTick            ; //< 604>
       avi.MarginPoints               = MathRound          ( avd.NominalMargin   / avd.NominalPoint       ) ; //< 605>
       avi.RiskPoints                 = MathRound          ( avd.QuoteRisk       / avd.QuotePoint         ) ; //< 606>
       avd.VARLimit                   = AccountEquity ()   * aed.OrderReserve                               ; //< 607>
       avd.RiskPoint                  = avd.VARLimit       / avi.RiskPoints                                 ; //< 608>
       avd.MarginLimit                = avd.RiskPoint      * avi.MarginPoints                               ; //< 609>
       avd.SizeLimit                  = avd.MarginLimit    / avd.NominalMargin                              ; //< 610>
//</7.8.2. Risk Management 9 >                                                                                //< 611>
                                                                                                              //< 612>
//< 7.8.3. Operation Size Control 17 >                                                                        //< 613>
       if   ( avd.SizeLimit          >= avd.MinimumLots )                                                     //< 614>
            { int    ali.Steps        = MathFloor       (  ( avd.SizeLimit       - avd.MinimumLots        )   //< 615>
                                                           / avd.LotStep                                  ) ; //< 616>
              double ald.Size         = avd.MinimumLots    + avd.LotStep         * ali.Steps              ; } //< 617>
       else   ald.Size                = 0                                                                   ; //< 618>
                                                                                                              //< 619>
       if   ( ald.Size                > avd.MaximumLots )                                                     //< 620>
              ald.Size                = avd.MaximumLots                                                     ; //< 621>
                                                                                                              //< 622>
       if   ( ald.Size               >= avd.MinimumLots )                                                     //< 623>
              double ald.MarginCheck  = AccountFreeMarginCheck ( aes.Symbol      , avi.Command , ald.Size ) ; //< 624>
       else   ald.MarginCheck         = EMPTY                                                               ; //< 625>
                                                                                                              //< 626>
       if   ( ald.MarginCheck        <= 0                                                                     //< 627>
           || GetLastError ()        == 134 )                                            avi.SystemFlag = 0 ; //< 628>
       else                                                                                                   //< 629>
            { double ald.Margin       = AccountFreeMargin ()   - ald.MarginCheck                            ; //< 630>
              double ald.Contract     = ald.Size               * avd.NominalPoint      / avd.QuotePoint     ; //< 631>
              double ald.VAR          = avd.QuoteRisk          * ald.Contract                               ; //< 632>
              double ald.Target       = avd.QuoteTarget        * ald.Contract                             ; } //< 633>
//</7.8.3. Operation Size Control 17 >                                                                        //< 634>
                                                                                                              //< 635>
//< 7.8.4. Order Send Trading Function 29 >                                                                   //< 636>
       if   ( avi.SystemFlag         == 1 )                                                                   //< 637>
            {                                                                                                 //< 638>
          //< Trading Function Execution Sequence >                                                           //< 639>
          //< Step 1 >                                                                                        //< 640>
              avs.LocalStamp          = avs.SystemStamp                                 + ": Attempt to " +   //< 641>
                                        acs.Operation      [ avi.Command   ]              + " "           +   //< 642>
                                        DoubleToStr        ( ald.Size      , 2          ) + " "           +   //< 643>
                                        aes.Symbol                                        + " at "        +   //< 644>
                                        DoubleToStr        ( avd.Price     , avi.Digits ) + " sl: "       +   //< 645>
                                        DoubleToStr        ( avd.Stop      , avi.Digits ) + " tp: "       +   //< 646>
                                        DoubleToStr        ( avd.Take      , avi.Digits ) + " //"           ; //< 647>
          //< Step 2 >                                                                                        //< 648>
              Alert                   ( avs.LocalStamp                                    , " Margin: "   ,   //< 649>
                                        DoubleToStr        ( ald.Margin    , 2          ) , " / VAR: -"   ,   //< 650>
                                        DoubleToStr        ( ald.VAR       , 2          ) , " / Target: " ,   //< 651>
                                        DoubleToStr        ( ald.Target    , 2          )                 ) ; //< 652>
          //< Step 3 >                                                                                        //< 653>
              int ali.Ticket          = OrderSend          ( aes.Symbol    , avi.Command  , ald.Size      ,   //< 654>
                                        avd.Price   ,  0   , avd.Stop      , avd.Take     , ""            ,   //< 655>
                                        aci.OrderID ,  0   , 0                                            ) ; //< 656>
          //< Step 4 >                                                                                        //< 657>
              avi.TimeStamp           = TimeLocal    ()                                                     ; //< 658>
                                                                                                              //< 659>
          //< Step 5 >                                                                                        //< 660>
              avi.Exception           = GetLastError ()                                                     ; //< 661>
                                                                                                              //< 662>
          //< Step 6 >                                                                                        //< 663>
              if ( avi.Exception == 0 ) avs.LocalMessage   = " Success "   + ali.Ticket                     ; //< 664>
              else                      avs.LocalMessage   = " Failure "   + avi.Exception                  ; //< 665>
                                                                                                              //< 666>
          //< Step 7 >                                                                                        //< 667>
                                        avi.AttemptsTrade  ++                                               ; //< 668>
              if ( avi.Exception == 0 )                                                                       //< 669>
                 { if ( avi.Command  == OP_BUY             )                                                  //< 670>
                                        avi.BuyTrades      ++                                               ; //< 671>
                   else                 avi.SellTrades     ++                                               ; //< 672>
                                        avi.TotalTrades    ++                                             ; } //< 673>
              else avi.ExcepionsTrade                      ++                                               ; //< 674>
                                                                                                              //< 675>
          //< Step 8 >                                                                                        //< 676>
              Alert                   ( avs.LocalStamp                     + avs.LocalMessage             ) ; //< 677>
                                                                                                              //< 678>
          //< Step 9 >                                                                                        //< 679>
              avs.SystemMessage       = acs.Operation      [ avi.Command ] + avs.LocalMessage               ; //< 680>
                                                                                                              //< 681>
      //</Trading Function Execution Sequence >                                                               //< 682>
            } // if 7.8.4                                                                                     //< 683>
//</7.8.4. Order Send Trading Function 29 >                                                                   //< 684>
                                                                                                              //< 685>
//< 7.8.5. Trading Module Exit Point 1 >                                                                      //< 686>
     } // if 7.8.1                                                                                            //< 687>
//</7.8.5. Trading Module Exit Point 1 >                                                                      //< 688>
                                                                                                              //< 689>
//</7.8. Trading Module 59 >``````````````````````````````````````````````````````````````````````````````````//< 690>
                                                                                                              //< 691>
//< 7.9. Monitoring Module 1 >````````````````````````````````````````````````````````````````````````````````//< 692>
                                                                                                              //< 693>
if   (   avi.LiveModeFlag   == 1 )                                                        afr.Monitoring () ; //< 694>
                                                                                                              //< 695>
//</7.9. Monitoring Module 1 >````````````````````````````````````````````````````````````````````````````````//< 696>
                                                                                                              //< 697>
//< 7.10. System Controls Reset On Exit 2 >```````````````````````````````````````````````````````````````````//< 698>
                                                                                                              //< 699>
avi.TimeLastRun          = TimeLocal    ()                                                                  ; //< 700>
avi.Runs                 ++                                                                                 ; //< 701>
                                                                                                              //< 702>
//</7.10. System Controls Reset On Exit 2 >```````````````````````````````````````````````````````````````````//< 703>
                                                                                                              //< 704>
}                                                                                                             //< 705>
//</7. Main Program 266 >=====================================================================================//< 706>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////< 707>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////< 708>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////< 709>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////< 710>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////< 711>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////< 712>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////< 713>
//< 8. Extra Code >===========================================================================================//< 714>
                                                                                                              //< 715>
//< A.System.Extra: Data 23 >`````````````````````````````````````````````````````````````````````````````````//< 716>
                                                                                                              //< 717>
#define acs.FontName                    "Courier New"                                                         //< 718>
#define aci.TextLines                   32                                                                    //< 719>
#define aci.TextColumns                 64                                                                    //< 720>
                                                                                                              //< 721>
#define acs.Blank                       "                                                                "    //< 722>
#define aci.Right                       1                                                                     //< 723>
#define aci.Left                        0                                                                     //< 724>
                                                                                                              //< 725>
string  abs.TextBuffer.1              [ aci.TextLines ]                                                     ; //< 726>
string  avs.BufferName.1              [ aci.TextLines ]                                                     ; //< 727>
                                                                                                              //< 728>
#define ari.Panel.1                     0                                                                     //< 729>
#define ari.FontSize                    1                                                                     //< 730>
#define ari.FontColor                   2                                                                     //< 731>
#define ari.LineSpace                   3                                                                     //< 732>
#define ari.PositionX                   4                                                                     //< 733>
#define ari.PositionY                   5                                                                     //< 734>
                                                                                                              //< 735>
double  arv.Panel.1              [] = { ari.Panel.1                                                         , //< 736>
                                        10                                                                  , //< 737>
                                        White                                                               , //< 738>
                                        2.0                                                                 , //< 739>
                                        0                                                                   , //< 740>
                                        0                                                                 } ; //< 741>
                                                                                                              //< 742>
string  arn.Panel.1              [] = { "Panel.1"                                                           , //< 743>
                                        "FontSize"                                                          , //< 744>
                                        "FontColor"                                                         , //< 745>
                                        "LineSpace"                                                         , //< 746>
                                        "PositionX"                                                         , //< 747>
                                        "PositionY"                                                       } ; //< 748>
                                                                                                              //< 749>
string  avs.SetupPrefix                                                                                     ; //< 750>
string  avs.SetupMonitor                                                                                    ; //< 751>
string  avs.SetupFontSize                                                                                   ; //< 752>
string  avs.SetupFontColor                                                                                  ; //< 753>
string  avs.SetupLineSpace                                                                                  ; //< 754>
string  avs.SetupPositionX                                                                                  ; //< 755>
string  avs.SetupPositionY                                                                                  ; //< 756>
                                                                                                              //< 757>
//< A.System.Extra: Date 23 >`````````````````````````````````````````````````````````````````````````````````//< 758>
                                                                                                              //< 759>
//< A.System.Extra: Function List 258 >```````````````````````````````````````````````````````````````````````//< 760>
                                                                                                              //< 761>
// 01 int    afr.Monitoring                                                                           //   12 //< 762>
// 02 int    afr.CreateSetup                                                                          //   29 //< 763>
// 03 int    afr.DeleteSetup                                                                          //    1 //< 764>
// 04 int    afr.ResetSetup                                                                           //   19 //< 765>
// 05 int    afr.CreatePanel.1                                                                        //    5 //< 766>
// 06 int    afr.DeletePanel.1                                                                        //    3 //< 767>
// 07 int    afr.ResetPanel.1                                                                         //    7 //< 768>
// 08 int    afr.ResetTextLine.1                                                                      //    8 //< 769>
// 09 int    afr.SetTextLine.1                                                                        //    3 //< 770>
// 10 int    afr.SetText.1                                                                            //    9 //< 771>
// 11 int    afr.Report.1                                                                             //  234 //< 772>
// 12 int    afs.Interval                                                                             //    7 //< 773>
// 13 int    afr.Reserved                                                                             //    0 //< 774>
// 14 int    afs.Time                                                                                 //    5 //< 775>
// 15 int    afr.CurrencyDetector                                                                     //   16 //< 776>
                                                                                                              //< 777>
//</A.System.Extra: Function List 258 >```````````````````````````````````````````````````````````````````````//< 778>
                                                                                                              //< 779>
//< A.System.Extra: Function 01 >`````````````````````````````````````````````````````````````````````````````//< 780>
                                                                                                              //< 781>
int    afr.Monitoring                ()                                                               //   12 //< 782>
{                                                                                                             //< 783>
//< 1.1. Monitoring Control 11 >                                                                              //< 784>
if ( ! GlobalVariableCheck ( avs.SetupMonitor            ) )                                                  //< 785>
       GlobalVariableSet   ( avs.SetupMonitor              , avi.MonitorFlag                              ) ; //< 786>
                                                                                                              //< 787>
if (   GlobalVariableGet   ( avs.SetupMonitor ) == 1       )                                                  //< 788>
   {   if ( avi.MonitorFlag                     == 0       )                                                  //< 789>
          { avi.MonitorFlag                      = 1                                                      ;   //< 790>
            afr.CreatePanel.1                                                                          () ; } //< 791>
       afr.ResetPanel.1                                                                                () ; } //< 792>
                                                                                                              //< 793>
else { if ( avi.MonitorFlag                     == 1       )                                                  //< 794>
          { avi.MonitorFlag                      = 0                                                      ;   //< 795>
            afr.DeleteSetup                                                                            () ;   //< 796>
            afr.DeletePanel.1                                                                          () ; } //< 797>
       return                                                                                             ; } //< 798>
//</1.1. Monitoring Control 11 >                                                                              //< 799>
                                                                                                              //< 800>
//< 1.2. Setup Reset 1 >                                                                                      //< 801>
       afr.ResetSetup                                                                                    () ; //< 802>
//</1.2. Setup Reset 1 >                                                                                      //< 803>
}                                                                                                             //< 804>
//</A.System.Extra: Function 01 >`````````````````````````````````````````````````````````````````````````````//< 805>
                                                                                                              //< 806>
//< A.System.Extra: Function 02 >`````````````````````````````````````````````````````````````````````````````//< 807>
int    afr.CreateSetup               ()                                                               //   29 //< 808>
{                                                                                                             //< 809>
       avs.SetupPrefix                = A.System.Series           + A.System.Modification + ".Setup."       ; //< 810>
                                                                                                              //< 811>
       avs.SetupBegin                 = avs.SetupPrefix           + "0.Begin.=============================" ; //< 812>
       avs.SetupAccountReserve        = avs.SetupPrefix           + "1.1." + "AccountReserve"               ; //< 813>
       avs.SetupOrderReserve          = avs.SetupPrefix           + "1.2." + "OrderReserve"                 ; //< 814>
       avs.SetupTrading               = avs.SetupPrefix           + "2.1." + "Trading"                      ; //< 815>
       avs.SetupTimeframe.1           = avs.SetupPrefix           + "2.2." + "Timeframe.1"                  ; //< 816>
       avs.SetupTimeframe.2           = avs.SetupPrefix           + "2.3." + "Timeframe.2"                  ; //< 817>
       avs.SetupTakeFactor            = avs.SetupPrefix           + "2.4." + "TakeFactor"                   ; //< 818>
       avs.SetupStopFactor            = avs.SetupPrefix           + "2.5." + "StopFactor"                   ; //< 819>
       avs.SetupTrailFactor           = avs.SetupPrefix           + "2.6." + "TrailFactor"                  ; //< 820>
       avs.SetupMonitor               = avs.SetupPrefix           + "3.1." + "Monitor"                      ; //< 821>
       avs.SetupFontSize              = avs.SetupPrefix           + "3.2." + arn.Panel.1  [ ari.FontSize  ] ; //< 822>
       avs.SetupFontColor             = avs.SetupPrefix           + "3.3." + arn.Panel.1  [ ari.FontColor ] ; //< 823>
       avs.SetupLineSpace             = avs.SetupPrefix           + "3.4." + arn.Panel.1  [ ari.LineSpace ] ; //< 824>
       avs.SetupPositionX             = avs.SetupPrefix           + "3.5." + arn.Panel.1  [ ari.PositionX ] ; //< 825>
       avs.SetupPositionY             = avs.SetupPrefix           + "3.6." + arn.Panel.1  [ ari.PositionY ] ; //< 826>
       avs.SetupEnd                   = avs.SetupPrefix           + "9.End.===============================" ; //< 827>
                                                                                                              //< 828>
       GlobalVariableSet              ( avs.SetupBegin            , aci.SetupSeparator                    ) ; //< 829>
       GlobalVariableSet              ( avs.SetupAccountReserve   , aed.AccountReserve                    ) ; //< 830>
       GlobalVariableSet              ( avs.SetupOrderReserve     , aed.OrderReserve                      ) ; //< 831>
       GlobalVariableSet              ( avs.SetupTrading          , avi.TradingFlag                       ) ; //< 832>
       GlobalVariableSet              ( avs.SetupTimeframe.1      , aei.Timeframe.1                       ) ; //< 833>
       GlobalVariableSet              ( avs.SetupTimeframe.2      , aei.Timeframe.2                       ) ; //< 834>
       GlobalVariableSet              ( avs.SetupTakeFactor       , aed.TakeFactor                        ) ; //< 835>
       GlobalVariableSet              ( avs.SetupStopFactor       , aed.StopFactor                        ) ; //< 836>
       GlobalVariableSet              ( avs.SetupTrailFactor      , aed.TrailFactor                       ) ; //< 837>
       GlobalVariableSet              ( avs.SetupMonitor          , avi.MonitorFlag                       ) ; //< 838>
       GlobalVariableSet              ( avs.SetupFontSize         , arv.Panel.1  [ ari.FontSize  ]        ) ; //< 839>
       GlobalVariableSet              ( avs.SetupFontColor        , arv.Panel.1  [ ari.FontColor ]        ) ; //< 840>
       GlobalVariableSet              ( avs.SetupLineSpace        , arv.Panel.1  [ ari.LineSpace ]        ) ; //< 841>
       GlobalVariableSet              ( avs.SetupPositionX        , arv.Panel.1  [ ari.PositionX ]        ) ; //< 842>
       GlobalVariableSet              ( avs.SetupPositionY        , arv.Panel.1  [ ari.PositionY ]        ) ; //< 843>
       GlobalVariableSet              ( avs.SetupEnd              , aci.SetupSeparator                    ) ; //< 844>
}                                                                                                             //< 845>
//</A.System.Extra: Function 02 >`````````````````````````````````````````````````````````````````````````````//< 846>
                                                                                                              //< 847>
//< A.System.Extra: Function 03 >`````````````````````````````````````````````````````````````````````````````//< 848>
int    afr.DeleteSetup               ()                                                               //    1 //< 849>
{                                                                                                             //< 850>
GlobalVariablesDeleteAll              ( avs.SetupPrefix                                                   ) ; //< 851>
}                                                                                                             //< 852>
//</A.System.Extra: Function 03 >`````````````````````````````````````````````````````````````````````````````//< 853>
                                                                                                              //< 854>
//< A.System.Extra: Function 04 >`````````````````````````````````````````````````````````````````````````````//< 855>
                                                                                                              //< 856>
int    afr.ResetSetup                ()                                                               //   19 //< 857>
{                                                                                                             //< 858>
if ( ! GlobalVariableCheck ( avs.SetupBegin              ) )                                                  //< 859>
       GlobalVariableSet   ( avs.SetupBegin                , aci.SetupSeparator                           ) ; //< 860>
                                                                                                              //< 861>
if ( ! GlobalVariableCheck ( avs.SetupFontSize           ) )                                                  //< 862>
       GlobalVariableSet   ( avs.SetupFontSize             , arv.Panel.1      [ ari.FontSize            ] ) ; //< 863>
else   arv.Panel.1         [ ari.FontSize                ] = GlobalVariableGet( avs.SetupFontSize         ) ; //< 864>
                                                                                                              //< 865>
if ( ! GlobalVariableCheck ( avs.SetupFontColor          ) )                                                  //< 866>
       GlobalVariableSet   ( avs.SetupFontColor            , arv.Panel.1      [ ari.FontColor           ] ) ; //< 867>
else   arv.Panel.1         [ ari.FontColor               ] = GlobalVariableGet( avs.SetupFontColor        ) ; //< 868>
                                                                                                              //< 869>
if ( ! GlobalVariableCheck ( avs.SetupLineSpace          ) )                                                  //< 870>
       GlobalVariableSet   ( avs.SetupLineSpace            , arv.Panel.1      [ ari.LineSpace           ] ) ; //< 871>
else   arv.Panel.1         [ ari.LineSpace               ] = GlobalVariableGet( avs.SetupLineSpace        ) ; //< 872>
                                                                                                              //< 873>
if ( ! GlobalVariableCheck ( avs.SetupPositionX          ) )                                                  //< 874>
       GlobalVariableSet   ( avs.SetupPositionX            , arv.Panel.1      [ ari.PositionX           ] ) ; //< 875>
else   arv.Panel.1         [ ari.PositionX               ] = GlobalVariableGet( avs.SetupPositionX        ) ; //< 876>
                                                                                                              //< 877>
if ( ! GlobalVariableCheck ( avs.SetupPositionY          ) )                                                  //< 878>
       GlobalVariableSet   ( avs.SetupPositionY            , arv.Panel.1      [ ari.PositionY           ] ) ; //< 879>
else   arv.Panel.1         [ ari.PositionY               ] = GlobalVariableGet( avs.SetupPositionY        ) ; //< 880>
                                                                                                              //< 881>
if ( ! GlobalVariableCheck ( avs.SetupEnd                ) )                                                  //< 882>
       GlobalVariableSet   ( avs.SetupEnd                  , aci.SetupSeparator                           ) ; //< 883>
}                                                                                                             //< 884>
//</A.System.Extra: Function 04 >`````````````````````````````````````````````````````````````````````````````//< 885>
                                                                                                              //< 886>
//< A.System.Extra: Function 05 >`````````````````````````````````````````````````````````````````````````````//< 887>
int    afr.CreatePanel.1             ()                                                               //    5 //< 888>
{                                                                                                             //< 889>
static int     i , N =                  aci.TextLines                                                       ; //< 890>
for  ( i = 0 ; i < N ;       i ++ )                                                                           //< 891>
     { abs.TextBuffer.1    [ i ]      = ""                                                                  ; //< 892>
       avs.BufferName.1    [ i ]      = A.System.Series    + A.System.Modification + ".TextBuffer." + i     ; //< 893>
       afr.ResetTextLine.1 ( i )                                                                          ; } //< 894>
}                                                                                                             //< 895>
//</A.System.Extra: Function 05 >`````````````````````````````````````````````````````````````````````````````//< 896>
                                                                                                              //< 897>
//< A.System.Extra: Function 06 >`````````````````````````````````````````````````````````````````````````````//< 898>
int    afr.DeletePanel.1             ()                                                               //    3 //< 899>
{                                                                                                             //< 900>
static int     i , N =                  aci.TextLines                                                       ; //< 901>
for  ( i = 0 ; i < N ; i ++ )                                                                                 //< 902>
     { ObjectDelete                   ( avs.BufferName.1 [ i ] )                                          ; } //< 903>
}                                                                                                             //< 904>
//</A.System.Extra: Function 06 >`````````````````````````````````````````````````````````````````````````````//< 905>
                                                                                                              //< 906>
//< A.System.Extra: Function 07 >`````````````````````````````````````````````````````````````````````````````//< 907>
int    afr.ResetPanel.1              ()                                                               //    7 //< 908>
{                                                                                                             //< 909>
static int     i , N =                  aci.TextLines                                                       ; //< 910>
                                                                                                              //< 911>
for  ( i = 0 ; i < N ;       i ++ )                                                                           //< 912>
     { abs.TextBuffer.1    [ i ]      = ""                                                                  ; //< 913>
       afr.SetTextLine.1   ( i )                                                                          ; } //< 914>
                                                                                                              //< 915>
afr.Report.1                         ()                                                                     ; //< 916>
                                                                                                              //< 917>
for  ( i = 0 ; i < N ;       i ++ )                                                                           //< 918>
       afr.SetTextLine.1   ( i )                                                                            ; //< 919>
}                                                                                                             //< 920>
//</A.System.Extra: Function 07 >`````````````````````````````````````````````````````````````````````````````//< 921>
                                                                                                              //< 922>
//< A.System.Extra: Function 08 >`````````````````````````````````````````````````````````````````````````````//< 923>
int    afr.ResetTextLine.1           (                                                                //    8 //< 924>
       int     aai.Line               )                                                                       //< 925>
{                                                                                                             //< 926>
static string   als.Name    ; als.Name = avs.BufferName.1 [ aai.Line ]                                      ; //< 927>
                                                                                                              //< 928>
ObjectCreate  ( als.Name    , OBJ_LABEL             , 0 , 0 , 0                                           ) ; //< 929>
ObjectSet     ( als.Name    , OBJPROP_XDISTANCE     , arv.Panel.1  [ ari.FontSize  ]                          //< 930>
                                                    * arv.Panel.1  [ ari.PositionX ]                      ) ; //< 931>
ObjectSet     ( als.Name    , OBJPROP_YDISTANCE     , arv.Panel.1  [ ari.FontSize  ] *                        //< 932>
              ( arv.Panel.1 [ ari.PositionY ]       + aai.Line     * arv.Panel.1   [ ari.LineSpace ]    ) ) ; //< 933>
                                                                                                              //< 934>
ObjectSetText ( als.Name    , abs.TextBuffer.1      [ aai.Line   ] ,                                          //< 935>
                arv.Panel.1 [ ari.FontSize  ]       , acs.FontName , arv.Panel.1   [ ari.FontColor ]      ) ; //< 936>
}                                                                                                             //< 937>
//</A.System.Extra: Function 08 >`````````````````````````````````````````````````````````````````````````````//< 938>
                                                                                                              //< 939>
//< A.System.Extra: Function 09 >`````````````````````````````````````````````````````````````````````````````//< 940>
int    afr.SetTextLine.1             (                                                                //    3 //< 941>
       int     aai.Line               )                                                                       //< 942>
{                                                                                                             //< 943>
static string   als.Name    ; als.Name = avs.BufferName.1 [ aai.Line ]                                      ; //< 944>
                                                                                                              //< 945>
ObjectSetText ( als.Name    , abs.TextBuffer.1      [ aai.Line   ] ,                                          //< 946>
                arv.Panel.1 [ ari.FontSize  ]       , acs.FontName , arv.Panel.1   [ ari.FontColor ]      ) ; //< 947>
}                                                                                                             //< 948>
//</A.System.Extra: Function 09 >`````````````````````````````````````````````````````````````````````````````//< 949>
                                                                                                              //< 950>
//< A.System.Extra: Function 10 >`````````````````````````````````````````````````````````````````````````````//< 951>
int    afr.SetText.1                 (                                                                //    9 //< 952>
       int     aai.Line               ,                                                                       //< 953>
       int     aai.Position           ,                                                                       //< 954>
       int     aai.Indent             ,                                                                       //< 955>
       string  aas.Text               )                                                                       //< 956>
{                                                                                                             //< 957>
static int     ali.Begin        ; ali.Begin        = aai.Position     - StringLen ( aas.Text ) * aai.Indent ; //< 958>
                                                                                                              //< 959>
if           ( aai.Indent  == 0 ) ali.Begin        --                                                       ; //< 960>
                                                                                                              //< 961>
if           ( ali.Begin   <= 0 ) abs.TextBuffer.1 [ aai.Line ]       = aas.Text                            ; //< 962>
                                                                                                              //< 963>
else { int     ali.BufferLength = StringLen        ( abs.TextBuffer.1 [ aai.Line         ]     )            ; //< 964>
       if    ( ali.Begin        > ali.BufferLength )                                                          //< 965>
               abs.TextBuffer.1 [ aai.Line       ] = abs.TextBuffer.1 [ aai.Line         ]     +              //< 966>
                   StringSubstr ( acs.Blank  , 0 ,   ali.Begin        - ali.BufferLength )     + aas.Text   ; //< 967>
       else    abs.TextBuffer.1 [ aai.Line       ] =                                                          //< 968>
                   StringSubstr ( abs.TextBuffer.1 [ aai.Line ]   , 0 , ali.Begin        )     + aas.Text ; } //< 969>
}                                                                                                             //< 970>
//</A.System.Extra: Function 10 >`````````````````````````````````````````````````````````````````````````````//< 971>
                                                                                                              //< 972>
//< A.System.Extra: Function 11 >`````````````````````````````````````````````````````````````````````````````//< 973>
int    afr.Report.1                  ()                                                               //  234 //< 974>
{                                                                                                             //< 975>
//< 11.1. Header 3 >                                                                                          //< 976>
static int ali.Trigger ; if ( ! ali.Trigger )    { ali.Trigger = 1 ;             static string als.Header ;   //< 977>
       als.Header            = A.System.Series   + A.System.Modification         + " " + A.System.Program ; } //< 978>
                                                                                                              //< 979>
afr.SetText.1 (   0 ,  1 , 0 , als.Header + ": " + avs.SystemMessage                                      ) ; //< 980>
//</11.1. Header 3 >                                                                                          //< 981>
                                                                                                              //< 982>
//< 11.2. Currency Set Initialization 1 >`````````````````````````````````````````````````````````````````````//< 983>
afr.CurrencyDetector         ( aes.Symbol        , avs.Currency )                                           ; //< 984>
//</11.2. Currency Set Initialization 1 >`````````````````````````````````````````````````````````````````````//< 985>
                                                                                                              //< 986>
//< 11.3. First Cluster: System Report 11 >                                                                   //< 987>
afr.SetText.1 (   2 ,  1 , 0 , "Client Time: "   + afs.Time ( TimeLocal   ()               , 1 ) + " / "  ) ; //< 988>
afr.SetText.1 (   3 ,  1 , 0 , "Client Name: "   + AccountName                                         () ) ; //< 989>
afr.SetText.1 (   4 ,  1 , 0 , "Server Name: "   + AccountServer                                       () ) ; //< 990>
afr.SetText.1 (   5 ,  1 , 0 , "Server Time: "   + afs.Time ( TimeCurrent ()               , 1 ) + " / "  ) ; //< 991>
                                                                                                              //< 992>
afr.SetText.1 (   2 , 48 , 1 , afs.Interval      ( TimeLocal ()    - avi.TimeStart         , 1 ) + " / "  ) ; //< 993>
afr.SetText.1 (   5 , 48 , 1 , afs.Interval      ( TimeLocal ()    - avi.TimeLastRun       , 1 ) + " / "  ) ; //< 994>
                                                                                                              //< 995>
afr.SetText.1 (   2 , 49 , 0 , avi.BuyTrades     + "+"             +                                          //< 996>
                               avi.SellTrades    + "="             +                                          //< 997>
                               avi.TotalTrades   + "/"             + avi.Trailes                          ) ; //< 998>
                                                                                                              //< 999>
afr.SetText.1 (   5 , 49 , 0 , avi.Runs          + "/"             +                                          //<1000>
                               avi.AttemptsTrade + "/"             + avi.AttemptsTrail                    ) ; //<1001>
//</11.3. First Cluster: System Report 11 >                                                                   //<1002>
                                                                                                              //<1003>
//< 11.4. Second Cluster: Capital Management Report 57 >                                                      //<1004>
double ald.DrawdownAbs       = AccountEquity     ()   - avd.PeakEquity                                      ; //<1005>
double ald.DrawdownRel       = ald.DrawdownAbs        / avd.PeakEquity                                      ; //<1006>
double ald.CapitalAbs        = avd.PeakEquity         * ( 1 - aed.AccountReserve )                          ; //<1007>
double ald.CapitalRel        = 1 - aed.AccountReserve                                                       ; //<1008>
double ald.CapitalGainAbs    = avd.Capital            - avd.InitialCapital                                  ; //<1009>
double ald.CapitalGainRel    = ald.CapitalGainAbs     / avd.InitialCapital * 100                            ; //<1010>
double ald.EquityGainAbs     = AccountEquity     ()   - avd.InitialEquity                                   ; //<1011>
double ald.EquityGainRel     = ald.EquityGainAbs      / avd.InitialEquity  * 100                            ; //<1012>
double ald.EquityReserveAbs  = AccountEquity     ()   - ald.CapitalAbs                                      ; //<1013>
double ald.EquityReserveRel  = ald.EquityReserveAbs   / avd.PeakEquity                                      ; //<1014>
double ald.AccountEquityAbs  = AccountEquity     ()                                                         ; //<1015>
double ald.AccountEquityRel  = AccountEquity     ()   / avd.PeakEquity                                      ; //<1016>
double ald.AccountFreeMargin = AccountFreeMargin ()                                                         ; //<1017>
double ald.MarginLevel                                                                                      ; //<1018>
string als.StopoutLevelAbs                                                                                  ; //<1019>
string als.StopoutLevelRel                                                                                  ; //<1020>
                                                                                                              //<1021>
if   ( AccountMargin () > 0 )  ald.MarginLevel    = AccountEquity () / AccountMargin ()                     ; //<1022>
else                           ald.MarginLevel    = 0                                                       ; //<1023>
                                                                                                              //<1024>
if   ( AccountStopoutMode () == 0 )                                                                           //<1025>
     { als.StopoutLevelAbs   = DoubleToStr  ( AccountStopoutLevel () * AccountEquity () / 100 , 2 )         ; //<1026>
       als.StopoutLevelRel   =              + AccountStopoutLevel () + ".00%"                             ; } //<1027>
else                                                                                                          //<1028>
     { als.StopoutLevelAbs   =                AccountStopoutLevel () + ".00"                                ; //<1029>
       als.StopoutLevelRel   = DoubleToStr  ( AccountStopoutLevel () / AccountEquity () * 100 , 2 ) + "%" ; } //<1030>
                                                                                                              //<1031>
if   ( ald.EquityGainAbs     > 0 )     string als.GainSign = "+"                                            ; //<1032>
else                                          als.GainSign = ""                                             ; //<1033>
                                                                                                              //<1034>
afr.SetText.1 (   7 ,  1 , 0 , "Capital "   + avs.Currency           [ ari.Account    ]             + ":" ) ; //<1035>
afr.SetText.1 (   8 ,  1 , 0 , "Reserve:"                                                                 ) ; //<1036>
afr.SetText.1 (   9 ,  1 , 0 , "Peak Equity:"                                                             ) ; //<1037>
afr.SetText.1 (  10 ,  1 , 0 , "Drawdown:"                                                                ) ; //<1038>
afr.SetText.1 (  11 ,  1 , 0 , "Acc. Equity:"                                                             ) ; //<1039>
afr.SetText.1 (  12 ,  1 , 0 , "Free Margin:"                                                             ) ; //<1040>
                                                                                                              //<1041>
afr.SetText.1 (   7 , 23 , 1 , DoubleToStr  ( ald.CapitalAbs                      , 2          )          ) ; //<1042>
afr.SetText.1 (   8 , 23 , 1 , DoubleToStr  ( ald.EquityReserveAbs                , 2          )          ) ; //<1043>
afr.SetText.1 (   9 , 23 , 1 , DoubleToStr  ( avd.PeakEquity                      , 2          )          ) ; //<1044>
afr.SetText.1 (  10 , 23 , 1 , DoubleToStr  ( ald.DrawdownAbs                     , 2          )          ) ; //<1045>
afr.SetText.1 (  11 , 23 , 1 , DoubleToStr  ( ald.AccountEquityAbs                , 2          )          ) ; //<1046>
afr.SetText.1 (  12 , 23 , 1 , DoubleToStr  ( ald.AccountFreeMargin               , 2          )          ) ; //<1047>
                                                                                                              //<1048>
afr.SetText.1 (   7 , 32 , 1 , DoubleToStr  ( ald.CapitalRel       * 100          , 2          ) + "%"    ) ; //<1049>
afr.SetText.1 (   7 , 45 , 1 , "+"          + DoubleToStr ( ald.CapitalGainAbs    , 2          )          ) ; //<1050>
afr.SetText.1 (   7 , 54 , 1 , "+"          + DoubleToStr ( ald.CapitalGainRel    , 2          ) + "%"    ) ; //<1051>
afr.SetText.1 (   8 , 32 , 1 , DoubleToStr  ( ald.EquityReserveRel * 100          , 2          ) + "%"    ) ; //<1052>
afr.SetText.1 (   9 , 32 , 1 , DoubleToStr  (                        100          , 2          ) + "%"    ) ; //<1053>
afr.SetText.1 (   9 , 45 , 1 , afs.Interval ( TimeLocal ()         - avd.PeakTime , 1          )          ) ; //<1054>
afr.SetText.1 (  10 , 32 , 1 , DoubleToStr  ( ald.DrawdownRel      * 100          , 2          ) + "%"    ) ; //<1055>
afr.SetText.1 (  11 , 32 , 1 , DoubleToStr  ( ald.AccountEquityRel * 100          , 2          ) + "%"    ) ; //<1056>
afr.SetText.1 (  11 , 45 , 1 , als.GainSign + DoubleToStr ( ald.EquityGainAbs , 2 )                       ) ; //<1057>
afr.SetText.1 (  11 , 54 , 1 , als.GainSign + DoubleToStr ( ald.EquityGainRel , 2 ) + "%"                 ) ; //<1058>
afr.SetText.1 (  12 , 32 , 1 , DoubleToStr  ( ald.MarginLevel      * 100          , 2          ) + "%"    ) ; //<1059>
afr.SetText.1 (  12 , 45 , 1 , als.StopoutLevelAbs                                                        ) ; //<1060>
afr.SetText.1 (  12 , 54 , 1 , als.StopoutLevelRel                                                        ) ; //<1061>
//</11.4. Second Cluster: Capital Management Report 57 >                                                      //<1062>
                                                                                                              //<1063>
//< 11.5. Third Cluster: Position Management Report 97 >                                                      //<1064>
double ald.VARLimit      = AccountEquity ()  * aed.OrderReserve                                             ; //<1065>
int    ali.LotSize       = MarketInfo        ( aes.Symbol          , MODE_LOTSIZE                         ) ; //<1066>
double ald.NominalPoint  = avd.NominalTick   * avd.QuotePoint      / avd.QuoteTick                          ; //<1067>
int    ali.MarginPoints                                                                                     ; //<1068>
if   ( ald.NominalPoint  > 0 )                                                                                //<1069>
       ali.MarginPoints  = MathRound         ( avd.NominalMargin   / ald.NominalPoint                     ) ; //<1070>
else   ali.MarginPoints  = 0                                                                                ; //<1071>
                                                                                                              //<1072>
string als.OrderCurrency [] = { "" , "" , "" , ""                                                         } ; //<1073>
                                                                                                              //<1074>
string als.OrderType     = ""                                                                               ; //<1075>
double ald.ContractSize  = 0                                                                                ; //<1076>
double ald.ContractValue = 0                                                                                ; //<1077>
double ald.OrderPoint    = 0                                                                                ; //<1078>
int    ali.OrderLifetime = 0                                                                                ; //<1079>
                                                                                                              //<1080>
double ald.QuotePrice    = 0                                                                                ; //<1081>
double ald.QuoteTake     = 0                                                                                ; //<1082>
double ald.QuoteStop     = 0                                                                                ; //<1083>
double ald.QuoteTarget   = 0                                                                                ; //<1084>
double ald.QuoteVAR      = 0                                                                                ; //<1085>
                                                                                                              //<1086>
int    ali.OrderProfit   = 0                                                                                ; //<1087>
int    ali.OrderTarget   = 0                                                                                ; //<1088>
int    ali.OrderVAR      = 0                                                                                ; //<1089>
                                                                                                              //<1090>
double ald.OrderProfit   = 0                                                                                ; //<1091>
double ald.OrderTarget   = 0                                                                                ; //<1092>
double ald.OrderVAR      = 0                                                                                ; //<1093>
                                                                                                              //<1094>
if ( OrdersTotal ()   > 0             )                                                                       //<1095>
   { int   i , N ; N  = OrdersTotal  () - 1                                                                 ; //<1096>
     for ( i = N ; i >= 0      ; i -- )                                                                       //<1097>
       {      OrderSelect      ( i    , SELECT_BY_POS   , MODE_TRADES  )                                    ; //<1098>
         if ( OrderMagicNumber ()    != aci.OrderID                    )                           continue ; //<1099>
         else                                                                                                 //<1100>
            { afr.CurrencyDetector                     (  OrderSymbol ()  , als.OrderCurrency             ) ; //<1101>
                                                                                                              //<1102>
              ald.ContractSize  = OrderLots       ()                                                        ; //<1103>
              ald.ContractValue = ald.ContractSize      / avd.QuotePoint  * ald.NominalPoint                ; //<1104>
              ald.OrderPoint    = ald.NominalPoint      * ald.ContractSize                                  ; //<1105>
              ali.OrderLifetime = TimeCurrent     ()    - OrderOpenTime   ()                                ; //<1106>
                                                                                                              //<1107>
              ald.QuotePrice    = OrderOpenPrice  ()                                                        ; //<1108>
              ald.QuoteTake     = OrderTakeProfit ()                                                        ; //<1109>
              ald.QuoteStop     = OrderStopLoss   ()                                                        ; //<1110>
                                                                                                              //<1111>
              ald.QuoteTarget   = MathAbs               ( ald.QuotePrice  - ald.QuoteTake                 ) ; //<1112>
                                                                                                              //<1113>
              if ( OrderType () == OP_BUY               )                                                     //<1114>
                   ald.QuoteVAR = ald.QuoteStop         - ald.QuotePrice                                    ; //<1115>
              else ald.QuoteVAR = ald.QuotePrice        - ald.QuoteStop                                     ; //<1116>
                                                                                                              //<1117>
              ald.OrderProfit   = OrderProfit     ()                                                        ; //<1118>
              ald.OrderTarget   = ald.QuoteTarget       * ald.ContractValue                                 ; //<1119>
              ald.OrderVAR      = ald.QuoteVAR          * ald.ContractValue                                 ; //<1120>
                                                                                                              //<1121>
              ali.OrderProfit   = MathRound             ( ald.OrderProfit / ald.OrderPoint )                ; //<1122>
              ali.OrderTarget   = MathRound             ( ald.QuoteTarget / avd.QuotePoint )                ; //<1123>
              ali.OrderVAR      = MathRound             ( ald.QuoteVAR    / avd.QuotePoint )                ; //<1124>
                                                                                                              //<1125>
              als.OrderType     = "#"                   + OrderTicket ()                         + " "   +    //<1126>
                                  acs.Operation         [ OrderType   ()  ]                      + " "   +    //<1127>
                                  DoubleToStr           ( ald.ContractSize        , 2          ) + " x " +    //<1128>
                                  DoubleToStr           ( ali.LotSize             , 0          ) + " "   +    //<1129>
                                  als.OrderCurrency     [ ari.Margin      ]                      + " / " +    //<1130>
                                  ali.MarginPoints                                               + " x " +    //<1131>
                                  DoubleToStr           ( ald.OrderPoint          , 2          )            ; //<1132>
            } // else                                                                                         //<1133>
       } // for                                                                                               //<1134>
   } // if                                                                                                    //<1135>
                                                                                                              //<1136>
if   ( ald.OrderProfit       > 0 )                 string als.OPSign = "+"                                  ; //<1137>
else                                                      als.OPSign = ""                                   ; //<1138>
                                                                                                              //<1139>
if   ( ald.OrderTarget       > 0 )                 string als.OTSign = "+"                                  ; //<1140>
else                                                      als.OTSign = ""                                   ; //<1141>
                                                                                                              //<1142>
if   ( ald.OrderVAR          > 0 )                 string als.OVSign = "+"                                  ; //<1143>
else                                                      als.OVSign = ""                                   ; //<1144>
                                                                                                              //<1145>
afr.SetText.1 (  14 ,  1 , 0 , "Order:"                                                                   ) ; //<1146>
afr.SetText.1 (  15 ,  1 , 0 , "Profit:"                                                                  ) ; //<1147>
afr.SetText.1 (  16 ,  1 , 0 , "Target:"                                                                  ) ; //<1148>
afr.SetText.1 (  17 ,  1 , 0 , "VAR:"                                                                     ) ; //<1149>
afr.SetText.1 (  18 ,  1 , 0 , "Limit:"                                                                   ) ; //<1150>
                                                                                                              //<1151>
afr.SetText.1 (  14 , 14 , 0 , als.OrderType                                                              ) ; //<1152>
afr.SetText.1 (  15 , 23 , 1 , als.OPSign   + DoubleToStr  ( ald.OrderProfit      , 2          )          ) ; //<1153>
afr.SetText.1 (  15 , 32 , 1 , als.OPSign   + ali.OrderProfit                                             ) ; //<1154>
afr.SetText.1 (  15 , 45 , 1 , afs.Interval ( ali.OrderLifetime                   , 1          )          ) ; //<1155>
afr.SetText.1 (  16 , 23 , 1 , als.OTSign   + DoubleToStr  ( ald.OrderTarget      , 2          )          ) ; //<1156>
afr.SetText.1 (  16 , 32 , 1 , als.OTSign   + ali.OrderTarget                                             ) ; //<1157>
afr.SetText.1 (  17 , 23 , 1 , als.OVSign   + DoubleToStr  ( ald.OrderVAR         , 2          )          ) ; //<1158>
afr.SetText.1 (  17 , 32 , 1 , als.OVSign   + ali.OrderVAR                                                ) ; //<1159>
afr.SetText.1 (  18 , 23 , 1 , DoubleToStr  ( - ald.VARLimit                      , 2          )          ) ; //<1160>
afr.SetText.1 (  18 , 32 , 1 , DoubleToStr  ( aed.OrderReserve * 100              , 2          ) + "%"    ) ; //<1161>
//</11.5. Third Cluster: Position Management Report 97 >                                                      //<1162>
                                                                                                              //<1163>
//< 11.6. Leverage/Contract Specification Indicator 14 >                                                      //<1164>
string als.Leverage          = "1:"                                                                      +    //<1165>
                               AccountLeverage ()                                                + " / " +    //<1166>
                               DoubleToStr  ( avd.NominalMargin                   , 2          ) + " "   +    //<1167>
                               avs.Currency [ ari.Account ]                                      + " = " +    //<1168>
                               ali.MarginPoints                                           + " points x " +    //<1169>
                               DoubleToStr  ( ald.NominalPoint                    , 2          ) + " "   +    //<1170>
                               avs.Currency [ ari.Account ]                                                 ; //<1171>
                                                                                                              //<1172>
string als.Contract          = DoubleToStr  ( MarketInfo  ( aes.Symbol , MODE_LOTSIZE ) , 2 )    + " "   +    //<1173>
                               avs.Currency [ ari.Margin  ]                                      + " / " +    //<1174>
                               DoubleToStr  ( MarketInfo  ( aes.Symbol , MODE_MINLOT  ) , 2 )    + " / " +    //<1175>
                               DoubleToStr  ( MarketInfo  ( aes.Symbol , MODE_LOTSTEP ) , 2 )    + " / " +    //<1176>
                               DoubleToStr  ( MarketInfo  ( aes.Symbol , MODE_MAXLOT  ) , 2 )               ; //<1177>
                                                                                                              //<1178>
afr.SetText.1 (  20 ,  1 , 0 , "Leverage:    " + als.Leverage                                             ) ; //<1179>
afr.SetText.1 (  21 ,  1 , 0 , "Contract:    " + als.Contract                                             ) ; //<1180>
//</11.6. Leverage/Contract Specification Indicator 14 >                                                      //<1181>
                                                                                                              //<1182>
//< 11.7. Fourth Cluster: Trading Strategy Rules 12 >                                                         //<1183>
afr.SetText.1 (  23 ,  1 , 0 , "Rules            Ask:     High.1      Close.1  Average.1"                 ) ; //<1184>
afr.SetText.1 (  24 ,  1 , 0 , "Buy: "                                                                    ) ; //<1185>
afr.SetText.1 (  24 , 24 , 1 , DoubleToStr  ( avd.QuoteAsk                        , avi.Digits ) + " > "  ) ; //<1186>
afr.SetText.1 (  24 , 36 , 1 , DoubleToStr  ( avd.High.1    + avd.QuoteSpread     , avi.Digits ) + "  &&" ) ; //<1187>
afr.SetText.1 (  24 , 48 , 1 , DoubleToStr  ( avd.Close.1   + avd.QuoteSpread     , avi.Digits ) + " > "  ) ; //<1188>
afr.SetText.1 (  24 , 56 , 1 , DoubleToStr  ( avd.Average.1 + avd.QuoteSpread     , avi.Digits )          ) ; //<1189>
                                                                                                              //<1190>
afr.SetText.1 (  25 ,  1 , 0 , "Sell: "                                                                   ) ; //<1191>
afr.SetText.1 (  25 , 24 , 1 , DoubleToStr  ( avd.QuoteBid                        , avi.Digits ) + " < "  ) ; //<1192>
afr.SetText.1 (  25 , 36 , 1 , DoubleToStr  ( avd.Low.1                           , avi.Digits ) + "  &&" ) ; //<1193>
afr.SetText.1 (  25 , 48 , 1 , DoubleToStr  ( avd.Close.1                         , avi.Digits ) + " < "  ) ; //<1194>
afr.SetText.1 (  25 , 56 , 1 , DoubleToStr  ( avd.Average.1                       , avi.Digits )          ) ; //<1195>
afr.SetText.1 (  26 ,  1 , 0 , "                 Bid:      Low.1      Close.1  Average.1"                 ) ; //<1196>
//</11.7. Fourth Cluster: Trading Strategy Rules 12 >                                                         //<1197>
                                                                                                              //<1198>
//< 11.8. Fifth Cluster: Trading Strategy Preset 14 >                                                         //<1199>
afr.SetText.1 (  28 ,  1 , 0 , "Preset        Factors      Range         Take       Stop"                 ) ; //<1200>
afr.SetText.1 (  29 ,  1 , 0 , "Trade "     + aei.Timeframe.1                                    + ":"    ) ; //<1201>
afr.SetText.1 (  29 , 21 , 1 , DoubleToStr  ( aed.TakeFactor                      , 1          ) + "/" +      //<1202>
                               DoubleToStr  ( aed.StopFactor                      , 1          )          ) ; //<1203>
afr.SetText.1 (  29 , 32 , 1 , DoubleToStr  ( avd.Range.1                         , avi.Digits )          ) ; //<1204>
afr.SetText.1 (  29 , 45 , 1 , DoubleToStr  ( avd.QuoteTake                       , avi.Digits )          ) ; //<1205>
afr.SetText.1 (  29 , 56 , 1 , DoubleToStr  ( avd.QuoteStop                       , avi.Digits )          ) ; //<1206>
                                                                                                              //<1207>
afr.SetText.1 (  30 ,  1 , 0 , "Trail "     + aei.Timeframe.2                                    + ":"    ) ; //<1208>
afr.SetText.1 (  30 , 21 , 1 , DoubleToStr  ( aed.TrailFactor                     , 1          ) + "/" +      //<1209>
                               DoubleToStr  ( acd.TrailStepping                   , 1          )          ) ; //<1210>
afr.SetText.1 (  30 , 32 , 1 , DoubleToStr  ( avd.Range.2                         , avi.Digits )          ) ; //<1211>
afr.SetText.1 (  30 , 45 , 1 , DoubleToStr  ( avd.QuoteTrail                      , avi.Digits )          ) ; //<1212>
afr.SetText.1 (  30 , 56 , 1 , DoubleToStr  ( avd.TrailStep                       , avi.Digits )          ) ; //<1213>
afr.SetText.1 (  31 ,  1 , 0 , "              Factors      Range        Trail       Step"                 ) ; //<1214>
//</11.8. Fifth Cluster: Trading Strategy Preset 14 >                                                         //<1215>
}                                                                                                             //<1216>
//</A.System.Extra: Function 11 >`````````````````````````````````````````````````````````````````````````````//<1217>
                                                                                                              //<1218>
//< A.System.Extra: Function 12 >`````````````````````````````````````````````````````````````````````````````//<1219>
string afs.Interval                  (                                                                //    7 //<1220>
       int     aai.Interval           ,                                                                       //<1221>
       int     aai.Seconds   = EMPTY  )                                                                       //<1222>
{                                                                                                             //<1223>
static string  als.Result                                                                                   ; //<1224>
                                                                                                              //<1225>
static int     ali.Interval          ;  ali.Interval       = MathAbs       ( aai.Interval                 ) ; //<1226>
if           ( aai.Seconds  == EMPTY )  als.Result         = TimeToStr     ( ali.Interval , TIME_MINUTES  ) ; //<1227>
else                                    als.Result         = TimeToStr     ( ali.Interval , TIME_SECONDS  ) ; //<1228>
                                                                                                              //<1229>
if           ( ali.Interval >= 86400 )  als.Result         = ali.Interval  / 86400 + " "  + als.Result      ; //<1230>
else if      ( aai.Interval  < 0     )  als.Result         = "-"                          + als.Result      ; //<1231>
                                                                                                              //<1232>
return       ( als.Result                                                                                 ) ; //<1233>
}                                                                                                             //<1234>
//</A.System.Extra: Function 12 >`````````````````````````````````````````````````````````````````````````````//<1235>
                                                                                                              //<1236>
//< A.System.Extra: Function 13 >`````````````````````````````````````````````````````````````````````````````//<1237>
//</A.System.Extra: Function 13 >`````````````````````````````````````````````````````````````````````````````//<1238>
                                                                                                              //<1239>
//< A.System.Extra: Function 14 >`````````````````````````````````````````````````````````````````````````````//<1240>
string afs.Time                      (                                                                //    5 //<1241>
       int     aai.Time               ,                                                                       //<1242>
       int     aai.Seconds   = EMPTY  )                                                                       //<1243>
{                                                                                                             //<1244>
static string  als.Result                                                                                   ; //<1245>
                                                                                                              //<1246>
if           ( aai.Seconds  == EMPTY  ) int ali.Mode       = TIME_DATE     | TIME_MINUTES                   ; //<1247>
else                                        ali.Mode       = TIME_DATE     | TIME_SECONDS                   ; //<1248>
                                                                                                              //<1249>
               als.Result    = TimeToStr  ( aai.Time       , ali.Mode                                     ) ; //<1250>
                                                                                                              //<1251>
return       ( als.Result                                                                                 ) ; //<1252>
}                                                                                                             //<1253>
//</A.System.Extra: Function 14 >`````````````````````````````````````````````````````````````````````````````//<1254>
                                                                                                              //<1255>
//< A.System.Extra: Function 15 >`````````````````````````````````````````````````````````````````````````````//<1256>
int    afr.CurrencyDetector          (                                                                //   16 //<1257>
       string  aas.Symbol             ,                                                                       //<1258>
       string& aas.Currency []        )                                                                       //<1259>
{                                                                                                             //<1260>
aas.Currency        [ ari.Account ] = AccountCurrency ()                                                    ; //<1261>
                                                                                                              //<1262>
if ( ( MarketInfo   ( aas.Symbol  , MODE_PROFITCALCMODE    ) ==  0 )                                          //<1263>
  && ( MarketInfo   ( aas.Symbol  , MODE_MARGINCALCMODE    ) ==  0 )                                          //<1264>
  && ( StringLen    ( aas.Symbol                           ) ==  6 )                                          //<1265>
  && ( StringFind   ( aas.Symbol  , "#"                    ) == -1 )                                          //<1266>
  && ( StringFind   ( aas.Symbol  , "@"                    ) == -1 )                                          //<1267>
  && ( StringFind   ( aas.Symbol  , "_"                    ) == -1 )             )                            //<1268>
     { aas.Currency [ ari.Base    ]         = StringSubstr ( aas.Symbol  , 0 , 3 )                          ; //<1269>
       aas.Currency [ ari.Quote   ]         = StringSubstr ( aas.Symbol  , 3 , 3 )                        ; } //<1270>
else { aas.Currency [ ari.Base    ]         = aas.Symbol                                                    ; //<1271>
       aas.Currency [ ari.Quote   ]         = aas.Currency [ ari.Account ]                                ; } //<1272>
                                                                                                              //<1273>
if   ( avd.NominalMargin > 0 )                                                                                //<1274>
     { if ( AccountLeverage ()             == MathRound    ( avd.NominalLot  / avd.NominalMargin ) )          //<1275>
            aas.Currency   [ ari.Margin  ]  = aas.Currency [ ari.Account ]                                  ; //<1276>
       else aas.Currency   [ ari.Margin  ]  = aas.Currency [ ari.Base    ]                                ; } //<1277>
else { aas.Currency        [ ari.Margin  ]  = ""                                                          ; } //<1278>
}                                                                                                             //<1279>
//</A.System.Extra: Function 15 >`````````````````````````````````````````````````````````````````````````````//<1280>
                                                                                                              //<1281>
//</8. Extra Code >===========================================================================================//<1282>