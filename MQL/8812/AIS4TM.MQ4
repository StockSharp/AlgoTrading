//< 1. Property 7 >===========================================================================================//<   1>
                                                                                                              //<   2>
#property     copyright                 "Copyright (C) 2009, MetaQuotes Software Corp."                       //<   3>
#property     link                      "http://www.metaquotes.net"                                           //<   4>
                                                                                                              //<   5>
#define       A.System.Series           "AIS"                                                                 //<   6>
#define       A.System.Modification     "40000"                                                               //<   7>
#define       A.System.ReleaseDate      "2009.04.07"                                                          //<   8>
#define       A.System.Program          "Trade Machine"                                                       //<   9>
#define       A.System.Programmer       "Airat Safin                           http://www.mql4.com/users/Ais" //<  10>
                                                                                                              //<  11>
//</1. Property 7 >===========================================================================================//<  12>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////<  13>
//< 2. Constants 4 >==========================================================================================//<  14>
                                                                                                              //<  15>
#define       acd.TrailStepping         1.0                                                                   //<  16>
                                                                                                              //<  17>
#define       aci.TradingPause          5                                                                     //<  18>
                                                                                                              //<  19>
string        acs.Operation     []  = { "Buy" , "Sell"                                                    } ; //<  20>
                                                                                                              //<  21>
#define       aci.SetupSeparator        1000000000                                                            //<  22>
                                                                                                              //<  23>
//</2. Constants 4 >==========================================================================================//<  24>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////<  25>
//< 3. Presets 9 >============================================================================================//<  26>
                                                                                                              //<  27>
//< 3.1. Order Identification Number Preset 1 >```````````````````````````````````````````````````````````````//<  28>
                                                                                                              //<  29>
extern int    aei.OrderID             = 40000                                                               ; //<  30>
                                                                                                              //<  31>
//</3.1. Order Identification Number Preset 1 >```````````````````````````````````````````````````````````````//<  32>
                                                                                                              //<  33>
//< 3.2. Risk Management Preset 2 >```````````````````````````````````````````````````````````````````````````//<  34>
                                                                                                              //<  35>
extern double aed.AccountReserve      = 0.20                                                                ; //<  36>
extern double aed.OrderReserve        = 0.04                                                                ; //<  37>
                                                                                                              //<  38>
//</3.2. Risk Management Preset 2 >```````````````````````````````````````````````````````````````````````````//<  39>
                                                                                                              //<  40>
//< 3.3. Trading Strategy Preset 6 >``````````````````````````````````````````````````````````````````````````//<  41>
                                                                                                              //<  42>
extern string aes.Symbol              = "EURUSD"                                                            ; //<  43>
                                                                                                              //<  44>
extern int    aei.Timeframe.1         = 15                                                                  ; //<  45>
extern int    aei.Timeframe.2         = 1                                                                   ; //<  46>
                                                                                                              //<  47>
extern double aed.Parameter.1         = 1.0                                                                 ; //<  48>
extern double aed.Parameter.2         = 2.0                                                                 ; //<  49>
extern double aed.Parameter.3         = 3.0                                                                 ; //<  50>
                                                                                                              //<  51>
//</3.3. Trading Strategy Preset 9 >``````````````````````````````````````````````````````````````````````````//<  52>
                                                                                                              //<  53>
//</3. Presets 8 >============================================================================================//<  54>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////<  55>
//< 4. Global Variables 69 >==================================================================================//<  56>
                                                                                                              //<  57>
//< 4.1. Trading Strategy Interface 4 >```````````````````````````````````````````````````````````````````````//<  58>
                                                                                                              //<  59>
int           avi.Command             = EMPTY                                                               ; //<  60>
double        avd.Price               = EMPTY                                                               ; //<  61>
double        avd.Stop                = EMPTY                                                               ; //<  62>
double        avd.Take                = EMPTY                                                               ; //<  63>
                                                                                                              //<  64>
//</4.1. Trading Strategy Interface 4 >```````````````````````````````````````````````````````````````````````//<  65>
                                                                                                              //<  66>
//< 4.2. System Controls 28 >`````````````````````````````````````````````````````````````````````````````````//<  67>
                                                                                                              //<  68>
int           avi.SystemFlag                                                                                ; //<  69>
int           avi.TradingFlag                                                                               ; //<  70>
int           avi.MonitorFlag                                                                               ; //<  71>
int           avi.LiveModeFlag                                                                              ; //<  72>
                                                                                                              //<  73>
int           avi.TimeStamp                                                                                 ; //<  74>
int           avi.Exception                                                                                 ; //<  75>
int           avi.ExcepionsTrade                                                                            ; //<  76>
int           avi.ExcepionsTrail                                                                            ; //<  77>
                                                                                                              //<  78>
int           avi.TimeStart                                                                                 ; //<  79>
int           avi.TimeLastRun                                                                               ; //<  80>
int           avi.Runs                                                                                      ; //<  81>
int           avi.BuyTrades                                                                                 ; //<  82>
int           avi.SellTrades                                                                                ; //<  83>
int           avi.TotalTrades                                                                               ; //<  84>
int           avi.Trailes                                                                                   ; //<  85>
int           avi.AttemptsTrade                                                                             ; //<  86>
int           avi.AttemptsTrail                                                                             ; //<  87>
                                                                                                              //<  88>
double        avd.Capital                                                                                   ; //<  89>
double        avd.PeakTime                                                                                  ; //<  90>
double        avd.PeakEquity                                                                                ; //<  91>
double        avd.InitialEquity                                                                             ; //<  92>
double        avd.InitialCapital                                                                            ; //<  93>
double        avd.EquityReserve                                                                             ; //<  94>
                                                                                                              //<  95>
string        avs.Currency      []  = { "" , "" , "" , ""                                                 } ; //<  96>
#define       ari.Account               0                                                                     //<  97>
#define       ari.Base                  1                                                                     //<  98>
#define       ari.Quote                 2                                                                     //<  99>
#define       ari.Margin                3                                                                     //< 100>
                                                                                                              //< 101>
//</4.2. System Controls 28 >`````````````````````````````````````````````````````````````````````````````````//< 102>
                                                                                                              //< 103>
//< 4.3. Preset Control 10 >``````````````````````````````````````````````````````````````````````````````````//< 104>
                                                                                                              //< 105>
//< 4.3.1. Setup Separators 2 >                                                                               //< 106>
string        avs.SetupBegin                                                                                ; //< 107>
string        avs.SetupEnd                                                                                  ; //< 108>
//</4.3.1. Setup Separators 2 >                                                                               //< 109>
                                                                                                              //< 110>
//< 4.3.2. Risk Management Preset Setup 2 >                                                                   //< 111>
string        avs.SetupAccountReserve                                                                       ; //< 112>
string        avs.SetupOrderReserve                                                                         ; //< 113>
//</4.3.2. Risk Management Preset Setup 2 >                                                                   //< 114>
                                                                                                              //< 115>
//< 4.3.3. Trading Preset Setup 6 >                                                                           //< 116>
string        avs.SetupTrading                                                                              ; //< 117>
string        avs.SetupTimeframe.1                                                                          ; //< 118>
string        avs.SetupTimeframe.2                                                                          ; //< 119>
string        avs.SetupParameter.1                                                                          ; //< 120>
string        avs.SetupParameter.2                                                                          ; //< 121>
string        avs.SetupParameter.3                                                                          ; //< 122>
//</4.3.3. Trading Preset Setup 6 >                                                                           //< 123>
                                                                                                              //< 124>
//</4.3. Preset Control 10 >``````````````````````````````````````````````````````````````````````````````````//< 125>
                                                                                                              //< 126>
//< 4.5. System Messages 4 >``````````````````````````````````````````````````````````````````````````````````//< 127>
                                                                                                              //< 128>
string        avs.SystemMessage                                                                             ; //< 129>
string        avs.LocalMessage                                                                              ; //< 130>
string        avs.SystemStamp                                                                               ; //< 131>
string        avs.LocalStamp                                                                                ; //< 132>
                                                                                                              //< 133>
//</4.4. System Messages 4 >``````````````````````````````````````````````````````````````````````````````````//< 134>
                                                                                                              //< 135>
//< 4.5. Common Data 14 >`````````````````````````````````````````````````````````````````````````````````````//< 136>
                                                                                                              //< 137>
double        avd.QuoteAsk                                                                                  ; //< 138>
double        avd.QuoteBid                                                                                  ; //< 139>
double        avd.QuotePoint                                                                                ; //< 140>
double        avd.QuoteSpread                                                                               ; //< 141>
double        avd.QuoteFreeze                                                                               ; //< 142>
double        avd.QuoteStops                                                                                ; //< 143>
double        avd.QuoteTick                                                                                 ; //< 144>
double        avd.NominalTick                                                                               ; //< 145>
double        avd.NominalMargin                                                                             ; //< 146>
double        avd.NominalLot                                                                                ; //< 147>
double        avd.MaximumLots                                                                               ; //< 148>
double        avd.MinimumLots                                                                               ; //< 149>
double        avd.LotStep                                                                                   ; //< 150>
int           avi.Digits                                                                                    ; //< 151>
                                                                                                              //< 152>
//</4.5. Common Data 14 >`````````````````````````````````````````````````````````````````````````````````````//< 153>
                                                                                                              //< 154>
//< 4.6. Risk Management Data 9 >`````````````````````````````````````````````````````````````````````````````//< 155>
                                                                                                              //< 156>
double        avd.QuoteTarget                                                                               ; //< 157>
double        avd.QuoteRisk                                                                                 ; //< 158>
double        avd.NominalPoint                                                                              ; //< 159>
int           avi.MarginPoints                                                                              ; //< 160>
int           avi.RiskPoints                                                                                ; //< 161>
double        avd.VARLimit                                                                                  ; //< 162>
double        avd.RiskPoint                                                                                 ; //< 163>
double        avd.MarginLimit                                                                               ; //< 164>
double        avd.SizeLimit                                                                                 ; //< 165>
                                                                                                              //< 166>
//</4.6. Risk Management Data 9 >`````````````````````````````````````````````````````````````````````````````//< 167>
                                                                                                              //< 168>
//< 4.7. Trading Strategy Data 0 >````````````````````````````````````````````````````````````````````````````//< 169>
//</4.7. Trading Strategy Data 0 >````````````````````````````````````````````````````````````````````````````//< 170>
                                                                                                              //< 171>
//</4. Global Variables 69 >==================================================================================//< 172>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////< 173>
//< 5. Program Initialization 21 >============================================================================//< 174>
                                                                                                              //< 175>
int    init                          ()                                                                       //< 176>
{                                                                                                             //< 177>
                                                                                                              //< 178>
//< 5.1. System Controls Reset 8 >````````````````````````````````````````````````````````````````````````````//< 179>
                                                                                                              //< 180>
avi.TimeStart            = TimeLocal     ()                                                                 ; //< 181>
avi.TimeStamp            = TimeLocal     ()                                                                 ; //< 182>
                                                                                                              //< 183>
avd.PeakTime             = TimeLocal     ()                                                                 ; //< 184>
avd.PeakEquity           = AccountEquity ()                                                                 ; //< 185>
avd.InitialEquity        = AccountEquity ()                                                                 ; //< 186>
avd.InitialCapital       = avd.PeakEquity   * ( 1 - aed.AccountReserve )                                    ; //< 187>
                                                                                                              //< 188>
avi.TradingFlag          = 1                                                                                ; //< 189>
avi.MonitorFlag          = 1                                                                                ; //< 190>
                                                                                                              //< 191>
//</5.1. System Controls Reset 8 >````````````````````````````````````````````````````````````````````````````//< 192>
                                                                                                              //< 193>
//< 5.2. System Stamp Reset 3 >```````````````````````````````````````````````````````````````````````````````//< 194>
                                                                                                              //< 195>
avs.SystemStamp          = A.System.Series                                 +                                  //< 196>
                           A.System.Modification                           + " "  +                           //< 197>
                           A.System.Program                                                                 ; //< 198>
                                                                                                              //< 199>
//</5.2. System Stamp Reset 3 >```````````````````````````````````````````````````````````````````````````````//< 200>
                                                                                                              //< 201>
//< 5.3. First Alert 8 >``````````````````````````````````````````````````````````````````````````````````````//< 202>
                                                                                                              //< 203>
Alert                    ( avs.SystemStamp                                 , ": Symbol="                  ,   //< 204>
                           aes.Symbol                                      , ", Preset="                  ,   //< 205>
                           aei.Timeframe.1                                 , "/"                          ,   //< 206>
                           aei.Timeframe.2                                 , "/"                          ,   //< 207>
                           DoubleToStr             ( aed.Parameter.1 , 1 ) , "/"                          ,   //< 208>
                           DoubleToStr             ( aed.Parameter.2 , 1 ) , "/"                          ,   //< 209>
                           DoubleToStr             ( aed.Parameter.3 , 1 ) , " "                              //< 210>
                                                   , ", Reload code="      ,        UninitializeReason () ) ; //< 211>
                                                                                                              //< 212>
//</5.3. First Alert 8 >``````````````````````````````````````````````````````````````````````````````````````//< 213>
                                                                                                              //< 214>
//< 5.4. Setup Reset 1 >``````````````````````````````````````````````````````````````````````````````````````//< 215>
                                                                                                              //< 216>
afr.CreateSetup                      ()                                                                     ; //< 217>
                                                                                                              //< 218>
//</5.4. Setup Reset 1 >``````````````````````````````````````````````````````````````````````````````````````//< 219>
                                                                                                              //< 220>
//< 5.5. Monitoring Panel Reset 1 >```````````````````````````````````````````````````````````````````````````//< 221>
                                                                                                              //< 222>
afr.CreatePanel.1                    ()                                                                     ; //< 223>
                                                                                                              //< 224>
//</5.5. Monitoring Panel Reset 1 >```````````````````````````````````````````````````````````````````````````//< 225>
                                                                                                              //< 226>
}                                                                                                             //< 227>
//</5. Program Initialization 21 >============================================================================//< 228>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////< 229>
//< 6. Program Deinitialization 5 >===========================================================================//< 230>
                                                                                                              //< 231>
int    deinit                        ()                                                                       //< 232>
{                                                                                                             //< 233>
                                                                                                              //< 234>
//< 6.1. Setup Deletion 1 >```````````````````````````````````````````````````````````````````````````````````//< 235>
                                                                                                              //< 236>
afr.DeleteSetup                      ()                                                                     ; //< 237>
                                                                                                              //< 238>
//</6.1. Setup Deletion 1 >```````````````````````````````````````````````````````````````````````````````````//< 239>
                                                                                                              //< 240>
//< 6.2. Monitoring Panel Deletion 1 >````````````````````````````````````````````````````````````````````````//< 241>
                                                                                                              //< 242>
afr.DeletePanel.1                    ()                                                                     ; //< 243>
                                                                                                              //< 244>
//</6.2. Monitoring Panel Deletion 1 >````````````````````````````````````````````````````````````````````````//< 245>
                                                                                                              //< 246>
//< 6.3. Final Alert 3 >``````````````````````````````````````````````````````````````````````````````````````//< 247>
                                                                                                              //< 248>
Alert                    ( avs.SystemStamp         , ": Stop code="        ,        UninitializeReason ()     //< 249>
                                                                           , "/"  , avi.ExcepionsTrade        //< 250>
                                                                           , "/"  , avi.ExcepionsTrail    ) ; //< 251>
                                                                                                              //< 252>
//</6.3. Final Alert 3 >``````````````````````````````````````````````````````````````````````````````````````//< 253>
                                                                                                              //< 254>
}                                                                                                             //< 255>
//</6. Program Deinitialization 5 >===========================================================================//< 256>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////< 257>
//< 7. Main Program 205 >=====================================================================================//< 258>
                                                                                                              //< 259>
int    start                         ()                                                                       //< 260>
{                                                                                                             //< 261>
                                                                                                              //< 262>
//< 7.1. System Controls Reset On Enter 75 >``````````````````````````````````````````````````````````````````//< 263>
                                                                                                              //< 264>
//< 7.1.1. Run Mode Detection 1 >                                                                             //< 265>
avi.LiveModeFlag           = ! IsTesting () || IsVisualMode ()                                              ; //< 266>
//</7.1.1. Run Mode Detection 1 >                                                                             //< 267>
                                                                                                              //< 268>
//< 7.1.2. Live Mode Subroutine 23 >                                                                          //< 269>
if (   avi.LiveModeFlag   == 1        )                                                                       //< 270>
   {                                                                                                          //< 271>
       if ( ! GlobalVariableCheck     ( avs.SetupBegin          ) )                                           //< 272>
              GlobalVariableSet       ( avs.SetupBegin            , aci.SetupSeparator                    ) ; //< 273>
       if ( ! GlobalVariableCheck     ( avs.SetupAccountReserve ) )                                           //< 274>
              GlobalVariableSet       ( avs.SetupAccountReserve   , aed.AccountReserve                    ) ; //< 275>
       if ( ! GlobalVariableCheck     ( avs.SetupOrderReserve   ) )                                           //< 276>
              GlobalVariableSet       ( avs.SetupOrderReserve     , aed.OrderReserve                      ) ; //< 277>
       if ( ! GlobalVariableCheck     ( avs.SetupTrading        ) )                                           //< 278>
              GlobalVariableSet       ( avs.SetupTrading          , avi.TradingFlag                       ) ; //< 279>
       if ( ! GlobalVariableCheck     ( avs.SetupTimeframe.1    ) )                                           //< 280>
              GlobalVariableSet       ( avs.SetupTimeframe.1      , aei.Timeframe.1                       ) ; //< 281>
       if ( ! GlobalVariableCheck     ( avs.SetupTimeframe.2    ) )                                           //< 282>
              GlobalVariableSet       ( avs.SetupTimeframe.2      , aei.Timeframe.2                       ) ; //< 283>
       if ( ! GlobalVariableCheck     ( avs.SetupParameter.1    ) )                                           //< 284>
              GlobalVariableSet       ( avs.SetupParameter.1      , aed.Parameter.1                       ) ; //< 285>
       if ( ! GlobalVariableCheck     ( avs.SetupParameter.2    ) )                                           //< 286>
              GlobalVariableSet       ( avs.SetupParameter.2      , aed.Parameter.2                       ) ; //< 287>
       if ( ! GlobalVariableCheck     ( avs.SetupParameter.3    ) )                                           //< 288>
              GlobalVariableSet       ( avs.SetupParameter.3      , aed.Parameter.3                       ) ; //< 289>
       if ( ! GlobalVariableCheck     ( avs.SetupEnd            ) )                                           //< 290>
              GlobalVariableSet       ( avs.SetupEnd              , aci.SetupSeparator                    ) ; //< 291>
   }                                                                                                          //< 292>
//</7.1.2. Live Mode Subroutine 23 >                                                                          //< 293>
                                                                                                              //< 294>
//< 7.1.3. All Modes Subroutine 8 >                                                                           //< 295>
if (   GlobalVariableGet      ( avs.SetupTrading )  == 1 )                                                    //< 296>
   {   avi.SystemFlag         = 1                                                                         ;   //< 297>
       avi.TradingFlag        = 1                                                                         ;   //< 298>
       avs.SystemMessage      = "Trading is enabled"                                                      ; } //< 299>
else                                                                                                          //< 300>
   {   avi.SystemFlag         = 0                                                                         ;   //< 301>
       avi.TradingFlag        = 0                                                                         ;   //< 302>
       avs.SystemMessage      = "Trading is disabled"                                                     ; } //< 303>
//</7.1.3. All Modes Subroutine 8 >                                                                           //< 304>
                                                                                                              //< 305>
//< 7.1.4. Live Mode Subroutine 43 >                                                                          //< 306>
if (   avi.LiveModeFlag      == 1 )                                                                           //< 307>
   {                                                                                                          //< 308>
       aed.AccountReserve     = GlobalVariableGet    ( avs.SetupAccountReserve                            ) ; //< 309>
       aed.OrderReserve       = GlobalVariableGet    ( avs.SetupOrderReserve                              ) ; //< 310>
       aei.Timeframe.1        = GlobalVariableGet    ( avs.SetupTimeframe.1                               ) ; //< 311>
       aei.Timeframe.2        = GlobalVariableGet    ( avs.SetupTimeframe.2                               ) ; //< 312>
       aed.Parameter.1        = GlobalVariableGet    ( avs.SetupParameter.1                               ) ; //< 313>
       aed.Parameter.2        = GlobalVariableGet    ( avs.SetupParameter.2                               ) ; //< 314>
       aed.Parameter.3        = GlobalVariableGet    ( avs.SetupParameter.3                               ) ; //< 315>
                                                                                                              //< 316>
       if ( aei.Timeframe.1  != PERIOD_M1                                                                     //< 317>
         && aei.Timeframe.1  != PERIOD_M5                                                                     //< 318>
         && aei.Timeframe.1  != PERIOD_M15                                                                    //< 319>
         && aei.Timeframe.1  != PERIOD_M30                                                                    //< 320>
         && aei.Timeframe.1  != PERIOD_H1                                                                     //< 321>
         && aei.Timeframe.1  != PERIOD_H4                                                                     //< 322>
         && aei.Timeframe.1  != PERIOD_D1                                                                     //< 323>
         && aei.Timeframe.1  != PERIOD_W1                                                                     //< 324>
         && aei.Timeframe.1  != PERIOD_MN1 )                                                                  //< 325>
          { aei.Timeframe.1   = 0                                                                         ;   //< 326>
            avi.SystemFlag    = 0                                                                         ;   //< 327>
            avi.TradingFlag   = 0                                                                         ;   //< 328>
            GlobalVariableSet ( avs.SetupTimeframe.1 , aei.Timeframe.1                                  ) ;   //< 329>
            GlobalVariableSet ( avs.SetupTrading     , avi.TradingFlag                                  ) ;   //< 330>
            avs.SystemMessage = "Check Timeframe.1"                                                       ;   //< 331>
            Alert             ( avs.SystemStamp                              , ": Symbol="                ,   //< 332>
                                aes.Symbol                                   , " "  , avs.SystemMessage ) ; } //< 333>
                                                                                                              //< 334>
       if ( aei.Timeframe.2  != PERIOD_M1                                                                     //< 335>
         && aei.Timeframe.2  != PERIOD_M5                                                                     //< 336>
         && aei.Timeframe.2  != PERIOD_M15                                                                    //< 337>
         && aei.Timeframe.2  != PERIOD_M30                                                                    //< 338>
         && aei.Timeframe.2  != PERIOD_H1                                                                     //< 339>
         && aei.Timeframe.2  != PERIOD_H4                                                                     //< 340>
         && aei.Timeframe.2  != PERIOD_D1                                                                     //< 341>
         && aei.Timeframe.2  != PERIOD_W1                                                                     //< 342>
         && aei.Timeframe.2  != PERIOD_MN1 )                                                                  //< 343>
          { aei.Timeframe.2   = 0                                                                         ;   //< 344>
            avi.SystemFlag    = 0                                                                         ;   //< 345>
            avi.TradingFlag   = 0                                                                         ;   //< 346>
            GlobalVariableSet ( avs.SetupTimeframe.2 , aei.Timeframe.2                                  ) ;   //< 347>
            GlobalVariableSet ( avs.SetupTrading     , avi.TradingFlag                                  ) ;   //< 348>
            avs.SystemMessage = "Check Timeframe.2"                                                       ;   //< 349>
            Alert              ( avs.SystemStamp                             , ": Symbol="                ,   //< 350>
                                 aes.Symbol                                  , " "  , avs.SystemMessage ) ; } //< 351>
   }                                                                                                          //< 352>
//</7.1.4. Live Mode Subroutine 43 >                                                                          //< 353>
                                                                                                              //< 354>
//</7.1. System Controls Reset On Enter 75 >``````````````````````````````````````````````````````````````````//< 355>
                                                                                                              //< 356>
//</7.2. Trading Pause Control 2 >````````````````````````````````````````````````````````````````````````````//< 357>
                                                                                                              //< 358>
if   ( TimeLocal     ()  - avi.TimeStamp           < aci.TradingPause )                                       //< 359>
     { avs.SystemMessage = "Trading pause "        + aci.TradingPause +   " seconds" ; avi.SystemFlag = 0 ; } //< 360>
                                                                                                              //< 361>
//</7.2. Trading Pause Control 2 >````````````````````````````````````````````````````````````````````````````//< 362>
                                                                                                              //< 363>
//< 7.3. Equity Control 6 >```````````````````````````````````````````````````````````````````````````````````//< 364>
                                                                                                              //< 365>
if   ( AccountEquity ()  - avd.PeakEquity > 0      )                                                          //< 366>
     { avd.PeakEquity    = AccountEquity ()                                                                 ; //< 367>
       avd.PeakTime      = TimeLocal     ()                                                               ; } //< 368>
                                                                                                              //< 369>
       avd.Capital       = avd.PeakEquity          * ( 1 - aed.AccountReserve )                             ; //< 370>
       avd.EquityReserve = AccountEquity ()        - avd.Capital                                            ; //< 371>
       avd.VARLimit      = AccountEquity ()        * aed.OrderReserve                                       ; //< 372>
                                                                                                              //< 373>
if   ( avd.EquityReserve - avd.VARLimit   < 0      )                                                          //< 374>
     { avs.SystemMessage = "System stop"                                             ; avi.SystemFlag = 0 ; } //< 375>
                                                                                                              //< 376>
//</7.3. Equity Control 6 >```````````````````````````````````````````````````````````````````````````````````//< 377>
                                                                                                              //< 378>
//< 7.4. Data Feed 14 >```````````````````````````````````````````````````````````````````````````````````````//< 379>
                                                                                                              //< 380>
//< 7.4.1. Common Data 14 >                                                                                   //< 381>
       avd.QuoteAsk      = MarketInfo ( aes.Symbol , MODE_ASK            )                                  ; //< 382>
       avd.QuoteBid      = MarketInfo ( aes.Symbol , MODE_BID            )                                  ; //< 383>
       avd.QuotePoint    = MarketInfo ( aes.Symbol , MODE_POINT          )                                  ; //< 384>
       avd.QuoteSpread   = MarketInfo ( aes.Symbol , MODE_SPREAD         ) * avd.QuotePoint                 ; //< 385>
       avd.QuoteFreeze   = MarketInfo ( aes.Symbol , MODE_FREEZELEVEL    ) * avd.QuotePoint                 ; //< 386>
       avd.QuoteStops    = MarketInfo ( aes.Symbol , MODE_STOPLEVEL      ) * avd.QuotePoint                 ; //< 387>
       avd.QuoteTick     = MarketInfo ( aes.Symbol , MODE_TICKSIZE       )                                  ; //< 388>
       avd.NominalTick   = MarketInfo ( aes.Symbol , MODE_TICKVALUE      )                                  ; //< 389>
       avd.NominalMargin = MarketInfo ( aes.Symbol , MODE_MARGINREQUIRED )                                  ; //< 390>
       avd.NominalLot    = MarketInfo ( aes.Symbol , MODE_LOTSIZE        )                                  ; //< 391>
       avd.MaximumLots   = MarketInfo ( aes.Symbol , MODE_MAXLOT         )                                  ; //< 392>
       avd.MinimumLots   = MarketInfo ( aes.Symbol , MODE_MINLOT         )                                  ; //< 393>
       avd.LotStep       = MarketInfo ( aes.Symbol , MODE_LOTSTEP        )                                  ; //< 394>
       avi.Digits        = MarketInfo ( aes.Symbol , MODE_DIGITS         )                                  ; //< 395>
//</7.4.1. Common Data 14 >                                                                                   //< 396>
                                                                                                              //< 397>
//< 7.4.2. Trading Strategy Data 0 >                                                                          //< 398>
//</7.4.2. Trading Strategy Data 0 >                                                                          //< 399>
                                                                                                              //< 400>
//</7.4. Data Feed 14 >```````````````````````````````````````````````````````````````````````````````````````//< 401>
                                                                                                              //< 402>
//< 7.5. Trading Strategy Interface Reset 4 >`````````````````````````````````````````````````````````````````//< 403>
                                                                                                              //< 404>
       avi.Command       = EMPTY                                                                            ; //< 405>
       avd.Price         = EMPTY                                                                            ; //< 406>
       avd.Stop          = EMPTY                                                                            ; //< 407>
       avd.Take          = EMPTY                                                                            ; //< 408>
                                                                                                              //< 409>
//</7.5. Trading Strategy Interface Reset 4 >`````````````````````````````````````````````````````````````````//< 410>
                                                                                                              //< 411>
//< 7.6. Position Management Module 38 >``````````````````````````````````````````````````````````````````````//< 412>
                                                                                                              //< 413>
//< 7.6.1. Position Management Module Entry Point 7 >                                                         //< 414>
if ( avi.SystemFlag  == 1             )                                                                       //< 415>
if ( OrdersTotal ()   > 0             )                                                                       //< 416>
                                                                                                              //< 417>
   { int   i , N ; N  = OrdersTotal  () - 1                                                                 ; //< 418>
     for ( i = N ; i >= 0         ; i --                                   )                                  //< 419>
       {      OrderSelect         ( i , SELECT_BY_POS , MODE_TRADES        )                                ; //< 420>
         if ( OrderMagicNumber    () != aei.OrderID                        )                       continue ; //< 421>
         else                                                                            avi.SystemFlag = 0 ; //< 422>
//</7.6.1. Position Management Module Entry Point 7 >                                                         //< 423>
                                                                                                              //< 424>
//< 7.6.2. Trailing Strategy Module Invocation 1 >                                                            //< 425>
         afr.TrailingStrategy ()                                                                            ; //< 426>
//</7.6.2. Trailing Strategy Module Invocation 1 >                                                            //< 427>
                                                                                                              //< 428>
//< 7.6.3. Order Modify Trading Function 28 >                                                                 //< 429>
         if ( avd.Stop                > 0 )                                                                   //< 430>
            { int  ali.TrailPoints    = MathRound       (    MathAbs       ( OrderStopLoss ()  - avd.Stop )   //< 431>
                                                                           / avd.QuotePoint               ) ; //< 432>
              if ( ali.TrailPoints   >= MarketInfo      (    aes.Symbol    , MODE_FREEZELEVEL           ) )   //< 433>
                 {                                                                                            //< 434>
               //< Trading Function Execution Sequence >                                                      //< 435>
               //< Step 1 >                                                                                   //< 436>
                   avs.LocalStamp     = avs.SystemStamp                           + ": Attempt to trail " +   //< 437>
                                        aes.Symbol                                                 + " "  +   //< 438>
                                        acs.Operation    [   OrderType ()  ]                       + " #" +   //< 439>
                                        OrderMagicNumber ()                                        + "/"  +   //< 440>
                                        OrderTicket      ()                                                 ; //< 441>
               //< Step 2 >                                                                                   //< 442>
                   Alert              ( avs.LocalStamp                                           ,     " +" , //< 443>
                                        ali.TrailPoints                                          , " from " , //< 444>
                                        DoubleToStr     (    OrderStopLoss ()     , avi.Digits ) ,   " to " , //< 445>
                                        DoubleToStr     (    avd.Stop             , avi.Digits )          ) ; //< 446>
               //< Step 3 >                                                                                   //< 447>
                   OrderModify        ( OrderTicket     () ,                                                  //< 448>
                                        OrderOpenPrice  () ,                                                  //< 449>
                                        avd.Stop           ,                                                  //< 450>
                                        OrderTakeProfit () , 0 , 0                                        ) ; //< 451>
               //< Step 4 >                                                                                   //< 452>
                   avi.TimeStamp      = TimeLocal       ()                                                  ; //< 453>
                                                                                                              //< 454>
               //< Step 5 >                                                                                   //< 455>
                   avi.Exception      = GetLastError    ()                                                  ; //< 456>
                                                                                                              //< 457>
               //< Step 6 >                                                                                   //< 458>
                   if ( avi.Exception == 0 ) avs.LocalMessage =            " Success "                      ; //< 459>
                   else                      avs.LocalMessage =            " Failure "      + avi.Exception ; //< 460>
                                                                                                              //< 461>
               //< Step 7 >                                                                                   //< 462>
                   avi.AttemptsTrail                          ++                                            ; //< 463>
                   if ( avi.Exception == 0 ) avi.Trailes      ++                                            ; //< 464>
                   else avi.ExcepionsTrail                    ++                                            ; //< 465>
                                                                                                              //< 466>
               //< Step 8 >                                                                                   //< 467>
                   Alert              ( avs.LocalStamp                     + avs.LocalMessage             ) ; //< 468>
                                                                                                              //< 469>
               //< Step 9 >                                                                                   //< 470>
                   avs.SystemMessage  = "Trailing Stop"                    + avs.LocalMessage               ; //< 471>
                                                                                                              //< 472>
          //</Trading Function Execution Sequence >                                                           //< 473>
                 } // if 7.6.3                                                                                //< 474>
            } // if 7.6.3                                                                                     //< 475>
//</7.6.3. Order Modify Trading Function 28 >                                                                 //< 476>
                                                                                                              //< 477>
//< 7.6.4. Position Management Module Exit Point 2 >                                                          //< 478>
       } // for 7.6.1                                                                                         //< 479>
   } // if 7.6.1                                                                                              //< 480>
//</7.6.4. Position Management Module Exit Point 2 >                                                          //< 481>
                                                                                                              //< 482>
//</7.6. Position Management Module 38 >``````````````````````````````````````````````````````````````````````//< 483>
                                                                                                              //< 484>
//< 7.7. Trading Strategy Logic 4 >```````````````````````````````````````````````````````````````````````````//< 485>
                                                                                                              //< 486>
//< 7.7.1. Trading Strategy Entry Point 2 >                                                                   //< 487>
if ( avi.SystemFlag == avi.SystemFlag )                                                                       //< 488>
   {                                                                                                          //< 489>
//</7.7.1. Trading Strategy Entry Point 2 >                                                                   //< 490>
                                                                                                              //< 491>
//< 7.7.2. Trading Strategy Module Invocation 1 >                                                             //< 492>
     afr.TradingStrategy ()                                                                                 ; //< 493>
//</7.7.2. Trading Strategy Module Invocation 1 >                                                             //< 494>
                                                                                                              //< 495>
//< 7.7.3. Trading Strategy Exit Point 1 >                                                                    //< 496>
   } // if 7.7.1                                                                                              //< 497>
//</7.7.3. Trading Strategy Exit Point 1 >                                                                    //< 498>
                                                                                                              //< 499>
//</7.7. Trading Strategy Logic 4 >```````````````````````````````````````````````````````````````````````````//< 500>
                                                                                                              //< 501>
//< 7.8. Trading Module 59 >``````````````````````````````````````````````````````````````````````````````````//< 502>
                                                                                                              //< 503>
//< 7.8.1. Trading Module Entry Point 3 >                                                                     //< 504>
if   ( avi.Command       > EMPTY      )                                                                       //< 505>
if   ( IsTradeAllowed   ()            )                                                                       //< 506>
     {                                                                                                        //< 507>
//</7.8.1. Trading Module Entry Point 3 >                                                                     //< 508>
                                                                                                              //< 509>
//< 7.8.2. Risk Management 9 >                                                                                //< 510>
       avd.QuoteTarget                = MathAbs            ( avd.Price           - avd.Take               ) ; //< 511>
       avd.QuoteRisk                  = MathAbs            ( avd.Price           - avd.Stop               ) ; //< 512>
       avd.NominalPoint               = avd.NominalTick    * avd.QuotePoint      / avd.QuoteTick            ; //< 513>
       avi.MarginPoints               = MathRound          ( avd.NominalMargin   / avd.NominalPoint       ) ; //< 514>
       avi.RiskPoints                 = MathRound          ( avd.QuoteRisk       / avd.QuotePoint         ) ; //< 515>
       avd.VARLimit                   = AccountEquity ()   * aed.OrderReserve                               ; //< 516>
       avd.RiskPoint                  = avd.VARLimit       / avi.RiskPoints                                 ; //< 517>
       avd.MarginLimit                = avd.RiskPoint      * avi.MarginPoints                               ; //< 518>
       avd.SizeLimit                  = avd.MarginLimit    / avd.NominalMargin                              ; //< 519>
//</7.8.2. Risk Management 9 >                                                                                //< 520>
                                                                                                              //< 521>
//< 7.8.3. Operation Size Control 17 >                                                                        //< 522>
       if   ( avd.SizeLimit          >= avd.MinimumLots )                                                     //< 523>
            { int    ali.Steps        = MathFloor       (  ( avd.SizeLimit       - avd.MinimumLots        )   //< 524>
                                                           / avd.LotStep                                  ) ; //< 525>
              double ald.Size         = avd.MinimumLots    + avd.LotStep         * ali.Steps              ; } //< 526>
       else   ald.Size                = 0                                                                   ; //< 527>
                                                                                                              //< 528>
       if   ( ald.Size                > avd.MaximumLots )                                                     //< 529>
              ald.Size                = avd.MaximumLots                                                     ; //< 530>
                                                                                                              //< 531>
       if   ( ald.Size               >= avd.MinimumLots )                                                     //< 532>
              double ald.MarginCheck  = AccountFreeMarginCheck ( aes.Symbol      , avi.Command , ald.Size ) ; //< 533>
       else   ald.MarginCheck         = EMPTY                                                               ; //< 534>
                                                                                                              //< 535>
       if   ( ald.MarginCheck        <= 0                                                                     //< 536>
           || GetLastError ()        == 134 )                                            avi.SystemFlag = 0 ; //< 537>
       else                                                                                                   //< 538>
            { double ald.Margin       = AccountFreeMargin ()   - ald.MarginCheck                            ; //< 539>
              double ald.Contract     = ald.Size               * avd.NominalPoint      / avd.QuotePoint     ; //< 540>
              double ald.VAR          = avd.QuoteRisk          * ald.Contract                               ; //< 541>
              double ald.Target       = avd.QuoteTarget        * ald.Contract                             ; } //< 542>
//</7.8.3. Operation Size Control 17 >                                                                        //< 543>
                                                                                                              //< 544>
//< 7.8.4. Order Send Trading Function 29 >                                                                   //< 545>
       if   ( avi.SystemFlag         == 1 )                                                                   //< 546>
            {                                                                                                 //< 547>
          //< Trading Function Execution Sequence >                                                           //< 548>
          //< Step 1 >                                                                                        //< 549>
              avs.LocalStamp          = avs.SystemStamp                                 + ": Attempt to " +   //< 550>
                                        acs.Operation      [ avi.Command   ]              + " "           +   //< 551>
                                        DoubleToStr        ( ald.Size      , 2          ) + " "           +   //< 552>
                                        aes.Symbol                                        + " at "        +   //< 553>
                                        DoubleToStr        ( avd.Price     , avi.Digits ) + " sl: "       +   //< 554>
                                        DoubleToStr        ( avd.Stop      , avi.Digits ) + " tp: "       +   //< 555>
                                        DoubleToStr        ( avd.Take      , avi.Digits ) + " //"           ; //< 556>
          //< Step 2 >                                                                                        //< 557>
              Alert                   ( avs.LocalStamp                                    , " Margin: "   ,   //< 558>
                                        DoubleToStr        ( ald.Margin    , 2          ) , " / VAR: -"   ,   //< 559>
                                        DoubleToStr        ( ald.VAR       , 2          ) , " / Target: " ,   //< 560>
                                        DoubleToStr        ( ald.Target    , 2          )                 ) ; //< 561>
          //< Step 3 >                                                                                        //< 562>
              int ali.Ticket          = OrderSend          ( aes.Symbol    , avi.Command  , ald.Size      ,   //< 563>
                                        avd.Price   ,  0   , avd.Stop      , avd.Take     , ""            ,   //< 564>
                                        aei.OrderID ,  0   , 0                                            ) ; //< 565>
          //< Step 4 >                                                                                        //< 566>
              avi.TimeStamp           = TimeLocal    ()                                                     ; //< 567>
                                                                                                              //< 568>
          //< Step 5 >                                                                                        //< 569>
              avi.Exception           = GetLastError ()                                                     ; //< 570>
                                                                                                              //< 571>
          //< Step 6 >                                                                                        //< 572>
              if ( avi.Exception == 0 ) avs.LocalMessage   = " Success "   + ali.Ticket                     ; //< 573>
              else                      avs.LocalMessage   = " Failure "   + avi.Exception                  ; //< 574>
                                                                                                              //< 575>
          //< Step 7 >                                                                                        //< 576>
                                        avi.AttemptsTrade  ++                                               ; //< 577>
              if ( avi.Exception == 0 )                                                                       //< 578>
                 { if ( avi.Command  == OP_BUY             )                                                  //< 579>
                                        avi.BuyTrades      ++                                               ; //< 580>
                   else                 avi.SellTrades     ++                                               ; //< 581>
                                        avi.TotalTrades    ++                                             ; } //< 582>
              else avi.ExcepionsTrade                      ++                                               ; //< 583>
                                                                                                              //< 584>
          //< Step 8 >                                                                                        //< 585>
              Alert                   ( avs.LocalStamp                     + avs.LocalMessage             ) ; //< 586>
                                                                                                              //< 587>
          //< Step 9 >                                                                                        //< 588>
              avs.SystemMessage       = acs.Operation      [ avi.Command ] + avs.LocalMessage               ; //< 589>
                                                                                                              //< 590>
      //</Trading Function Execution Sequence >                                                               //< 591>
            } // if 7.8.4                                                                                     //< 592>
//</7.8.4. Order Send Trading Function 29 >                                                                   //< 593>
                                                                                                              //< 594>
//< 7.8.5. Trading Module Exit Point 1 >                                                                      //< 595>
     } // if 7.8.1                                                                                            //< 596>
//</7.8.5. Trading Module Exit Point 1 >                                                                      //< 597>
                                                                                                              //< 598>
//</7.8. Trading Module 59 >``````````````````````````````````````````````````````````````````````````````````//< 599>
                                                                                                              //< 600>
//< 7.9. Monitoring Module 1 >````````````````````````````````````````````````````````````````````````````````//< 601>
                                                                                                              //< 602>
if   (   avi.LiveModeFlag   == 1 )                                                        afr.Monitoring () ; //< 603>
                                                                                                              //< 604>
//</7.9. Monitoring Module 1 >````````````````````````````````````````````````````````````````````````````````//< 605>
                                                                                                              //< 606>
//< 7.10. System Controls Reset On Exit 2 >```````````````````````````````````````````````````````````````````//< 607>
                                                                                                              //< 608>
avi.TimeLastRun          = TimeLocal    ()                                                                  ; //< 609>
avi.Runs                 ++                                                                                 ; //< 610>
                                                                                                              //< 611>
//</7.10. System Controls Reset On Exit 2 >```````````````````````````````````````````````````````````````````//< 612>
                                                                                                              //< 613>
}                                                                                                             //< 614>
//</7. Main Program 205 >=====================================================================================//< 615>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////< 616>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////< 617>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////< 618>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////< 619>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////< 620>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////< 621>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////< 622>
//< 8. Extra Code >===========================================================================================//< 623>
                                                                                                              //< 624>
//< A.System.Extra: Data 25 >`````````````````````````````````````````````````````````````````````````````````//< 625>
                                                                                                              //< 626>
#define acs.FontName                    "Courier New"                                                         //< 627>
#define aci.TextLines                   48                                                                    //< 628>
#define aci.TextColumns                 64                                                                    //< 629>
                                                                                                              //< 630>
#define acs.Blank                       "                                                                "    //< 631>
#define aci.Right                       1                                                                     //< 632>
#define aci.Left                        0                                                                     //< 633>
                                                                                                              //< 634>
string  abs.TextBuffer.1              [ aci.TextLines ]                                                     ; //< 635>
string  avs.BufferName.1              [ aci.TextLines ]                                                     ; //< 636>
                                                                                                              //< 637>
#define ari.Panel.1                     0                                                                     //< 638>
#define ari.FontSize                    1                                                                     //< 639>
#define ari.FontColor                   2                                                                     //< 640>
#define ari.LineSpace                   3                                                                     //< 641>
#define ari.PositionX                   4                                                                     //< 642>
#define ari.PositionY                   5                                                                     //< 643>
                                                                                                              //< 644>
double  arv.Panel.1              [] = { ari.Panel.1                                                         , //< 645>
                                        10                                                                  , //< 646>
                                        White                                                               , //< 647>
                                        2.0                                                                 , //< 648>
                                        0                                                                   , //< 649>
                                        0                                                                 } ; //< 650>
                                                                                                              //< 651>
string  arn.Panel.1              [] = { "Panel.1"                                                           , //< 652>
                                        "FontSize"                                                          , //< 653>
                                        "FontColor"                                                         , //< 654>
                                        "LineSpace"                                                         , //< 655>
                                        "PositionX"                                                         , //< 656>
                                        "PositionY"                                                       } ; //< 657>
                                                                                                              //< 658>
string  avs.SetupPrefix                                                                                     ; //< 659>
string  avs.SetupMonitor                                                                                    ; //< 660>
string  avs.SetupFontSize                                                                                   ; //< 661>
string  avs.SetupFontColor                                                                                  ; //< 662>
string  avs.SetupLineSpace                                                                                  ; //< 663>
string  avs.SetupPositionX                                                                                  ; //< 664>
string  avs.SetupPositionY                                                                                  ; //< 665>
                                                                                                              //< 666>
string  avs.SetupOrderSelect                                                                                ; //< 667>
                                                                                                              //< 668>
int     avi.OrderSelect                                                                                     ; //< 669>
                                                                                                              //< 670>
//< A.System.Extra: Date 25 >`````````````````````````````````````````````````````````````````````````````````//< 671>
                                                                                                              //< 672>
//< A.System.Extra: Function List 342 >```````````````````````````````````````````````````````````````````````//< 673>
                                                                                                              //< 674>
// 01 int    afr.Monitoring                                                                           //   12 //< 675>
// 02 int    afr.CreateSetup                                                                          //   31 //< 676>
// 03 int    afr.DeleteSetup                                                                          //    1 //< 677>
// 04 int    afr.ResetSetup                                                                           //   22 //< 678>
// 05 int    afr.CreatePanel.1                                                                        //    5 //< 679>
// 06 int    afr.DeletePanel.1                                                                        //    3 //< 680>
// 07 int    afr.ResetPanel.1                                                                         //    7 //< 681>
// 08 int    afr.ResetTextLine.1                                                                      //    8 //< 682>
// 09 int    afr.SetTextLine.1                                                                        //    3 //< 683>
// 10 int    afr.SetText.1                                                                            //    9 //< 684>
// 11 int    afr.Report.1                                                                             //  211 //< 685>
// 12 int    afs.Interval                                                                             //    7 //< 686>
// 13 int    afr.Reserved                                                                             //    0 //< 687>
// 14 int    afs.Time                                                                                 //    5 //< 688>
// 15 int    afr.CurrencyDetector                                                                     //   16 //< 689>
// 16 int    afr.TrailingStrategy                                                                     //    1 //< 690>
// 17 int    afr.TradingStrategy                                                                      //  422 //< 691>
                                                                                                              //< 692>
//</A.System.Extra: Function List 342 >```````````````````````````````````````````````````````````````````````//< 693>
                                                                                                              //< 694>
//< A.System.Extra: Function 01 >`````````````````````````````````````````````````````````````````````````````//< 695>
                                                                                                              //< 696>
int    afr.Monitoring                ()                                                               //   12 //< 697>
{                                                                                                             //< 698>
//< 1.1. Monitoring Control 11 >                                                                              //< 699>
if ( ! GlobalVariableCheck ( avs.SetupMonitor            ) )                                                  //< 700>
       GlobalVariableSet   ( avs.SetupMonitor              , avi.MonitorFlag                              ) ; //< 701>
                                                                                                              //< 702>
if (   GlobalVariableGet   ( avs.SetupMonitor ) == 1       )                                                  //< 703>
   {   if ( avi.MonitorFlag                     == 0       )                                                  //< 704>
          { avi.MonitorFlag                      = 1                                                      ;   //< 705>
            afr.CreatePanel.1                                                                          () ; } //< 706>
       afr.ResetPanel.1                                                                                () ; } //< 707>
                                                                                                              //< 708>
else { if ( avi.MonitorFlag                     == 1       )                                                  //< 709>
          { avi.MonitorFlag                      = 0                                                      ;   //< 710>
            afr.DeleteSetup                                                                            () ;   //< 711>
            afr.DeletePanel.1                                                                          () ; } //< 712>
       return                                                                                             ; } //< 713>
//</1.1. Monitoring Control 11 >                                                                              //< 714>
                                                                                                              //< 715>
//< 1.2. Setup Reset 1 >                                                                                      //< 716>
       afr.ResetSetup                                                                                    () ; //< 717>
//</1.2. Setup Reset 1 >                                                                                      //< 718>
}                                                                                                             //< 719>
//</A.System.Extra: Function 01 >`````````````````````````````````````````````````````````````````````````````//< 720>
                                                                                                              //< 721>
//< A.System.Extra: Function 02 >`````````````````````````````````````````````````````````````````````````````//< 722>
int    afr.CreateSetup               ()                                                               //   31 //< 723>
{                                                                                                             //< 724>
       avs.SetupPrefix                = A.System.Series           + A.System.Modification + ".Setup."       ; //< 725>
                                                                                                              //< 726>
       avs.SetupBegin                 = avs.SetupPrefix           + "0.Begin.=============================" ; //< 727>
       avs.SetupAccountReserve        = avs.SetupPrefix           + "1.1." + "AccountReserve"               ; //< 728>
       avs.SetupOrderReserve          = avs.SetupPrefix           + "1.2." + "OrderReserve"                 ; //< 729>
       avs.SetupTrading               = avs.SetupPrefix           + "2.1." + "Trading"                      ; //< 730>
       avs.SetupTimeframe.1           = avs.SetupPrefix           + "2.2." + "Timeframe.1"                  ; //< 731>
       avs.SetupTimeframe.2           = avs.SetupPrefix           + "2.3." + "Timeframe.2"                  ; //< 732>
       avs.SetupParameter.1           = avs.SetupPrefix           + "2.4." + "Parameter.1"                  ; //< 733>
       avs.SetupParameter.2           = avs.SetupPrefix           + "2.5." + "Parameter.2"                  ; //< 734>
       avs.SetupParameter.3           = avs.SetupPrefix           + "2.6." + "Parameter.3"                  ; //< 735>
       avs.SetupMonitor               = avs.SetupPrefix           + "3.1." + "Monitor"                      ; //< 736>
       avs.SetupFontSize              = avs.SetupPrefix           + "3.2." + arn.Panel.1  [ ari.FontSize  ] ; //< 737>
       avs.SetupFontColor             = avs.SetupPrefix           + "3.3." + arn.Panel.1  [ ari.FontColor ] ; //< 738>
       avs.SetupLineSpace             = avs.SetupPrefix           + "3.4." + arn.Panel.1  [ ari.LineSpace ] ; //< 739>
       avs.SetupPositionX             = avs.SetupPrefix           + "3.5." + arn.Panel.1  [ ari.PositionX ] ; //< 740>
       avs.SetupPositionY             = avs.SetupPrefix           + "3.6." + arn.Panel.1  [ ari.PositionY ] ; //< 741>
       avs.SetupOrderSelect           = avs.SetupPrefix           + "4.1." + "OrderSelect"                  ; //< 742>
       avs.SetupEnd                   = avs.SetupPrefix           + "9.End.===============================" ; //< 743>
                                                                                                              //< 744>
       GlobalVariableSet              ( avs.SetupBegin            , aci.SetupSeparator                    ) ; //< 745>
       GlobalVariableSet              ( avs.SetupAccountReserve   , aed.AccountReserve                    ) ; //< 746>
       GlobalVariableSet              ( avs.SetupOrderReserve     , aed.OrderReserve                      ) ; //< 747>
       GlobalVariableSet              ( avs.SetupTrading          , avi.TradingFlag                       ) ; //< 748>
       GlobalVariableSet              ( avs.SetupTimeframe.1      , aei.Timeframe.1                       ) ; //< 749>
       GlobalVariableSet              ( avs.SetupTimeframe.2      , aei.Timeframe.2                       ) ; //< 750>
       GlobalVariableSet              ( avs.SetupParameter.1      , aed.Parameter.1                       ) ; //< 751>
       GlobalVariableSet              ( avs.SetupParameter.2      , aed.Parameter.2                       ) ; //< 752>
       GlobalVariableSet              ( avs.SetupParameter.3      , aed.Parameter.3                       ) ; //< 753>
       GlobalVariableSet              ( avs.SetupMonitor          , avi.MonitorFlag                       ) ; //< 754>
       GlobalVariableSet              ( avs.SetupFontSize         , arv.Panel.1  [ ari.FontSize  ]        ) ; //< 755>
       GlobalVariableSet              ( avs.SetupFontColor        , arv.Panel.1  [ ari.FontColor ]        ) ; //< 756>
       GlobalVariableSet              ( avs.SetupLineSpace        , arv.Panel.1  [ ari.LineSpace ]        ) ; //< 757>
       GlobalVariableSet              ( avs.SetupPositionX        , arv.Panel.1  [ ari.PositionX ]        ) ; //< 758>
       GlobalVariableSet              ( avs.SetupPositionY        , arv.Panel.1  [ ari.PositionY ]        ) ; //< 759>
       GlobalVariableSet              ( avs.SetupOrderSelect      , avi.OrderSelect                       ) ; //< 760>
       GlobalVariableSet              ( avs.SetupEnd              , aci.SetupSeparator                    ) ; //< 761>
}                                                                                                             //< 762>
//</A.System.Extra: Function 02 >`````````````````````````````````````````````````````````````````````````````//< 763>
                                                                                                              //< 764>
//< A.System.Extra: Function 03 >`````````````````````````````````````````````````````````````````````````````//< 765>
int    afr.DeleteSetup               ()                                                               //    1 //< 766>
{                                                                                                             //< 767>
GlobalVariablesDeleteAll              ( avs.SetupPrefix                                                   ) ; //< 768>
}                                                                                                             //< 769>
//</A.System.Extra: Function 03 >`````````````````````````````````````````````````````````````````````````````//< 770>
                                                                                                              //< 771>
//< A.System.Extra: Function 04 >`````````````````````````````````````````````````````````````````````````````//< 772>
                                                                                                              //< 773>
int    afr.ResetSetup                ()                                                               //   22 //< 774>
{                                                                                                             //< 775>
if ( ! GlobalVariableCheck ( avs.SetupBegin              ) )                                                  //< 776>
       GlobalVariableSet   ( avs.SetupBegin                , aci.SetupSeparator                           ) ; //< 777>
                                                                                                              //< 778>
if ( ! GlobalVariableCheck ( avs.SetupFontSize           ) )                                                  //< 779>
       GlobalVariableSet   ( avs.SetupFontSize             , arv.Panel.1      [ ari.FontSize            ] ) ; //< 780>
else   arv.Panel.1         [ ari.FontSize                ] = GlobalVariableGet( avs.SetupFontSize         ) ; //< 781>
                                                                                                              //< 782>
if ( ! GlobalVariableCheck ( avs.SetupFontColor          ) )                                                  //< 783>
       GlobalVariableSet   ( avs.SetupFontColor            , arv.Panel.1      [ ari.FontColor           ] ) ; //< 784>
else   arv.Panel.1         [ ari.FontColor               ] = GlobalVariableGet( avs.SetupFontColor        ) ; //< 785>
                                                                                                              //< 786>
if ( ! GlobalVariableCheck ( avs.SetupLineSpace          ) )                                                  //< 787>
       GlobalVariableSet   ( avs.SetupLineSpace            , arv.Panel.1      [ ari.LineSpace           ] ) ; //< 788>
else   arv.Panel.1         [ ari.LineSpace               ] = GlobalVariableGet( avs.SetupLineSpace        ) ; //< 789>
                                                                                                              //< 790>
if ( ! GlobalVariableCheck ( avs.SetupPositionX          ) )                                                  //< 791>
       GlobalVariableSet   ( avs.SetupPositionX            , arv.Panel.1      [ ari.PositionX           ] ) ; //< 792>
else   arv.Panel.1         [ ari.PositionX               ] = GlobalVariableGet( avs.SetupPositionX        ) ; //< 793>
                                                                                                              //< 794>
if ( ! GlobalVariableCheck ( avs.SetupPositionY          ) )                                                  //< 795>
       GlobalVariableSet   ( avs.SetupPositionY            , arv.Panel.1      [ ari.PositionY           ] ) ; //< 796>
else   arv.Panel.1         [ ari.PositionY               ] = GlobalVariableGet( avs.SetupPositionY        ) ; //< 797>
                                                                                                              //< 798>
if ( ! GlobalVariableCheck ( avs.SetupOrderSelect        ) )                                                  //< 799>
       GlobalVariableSet   ( avs.SetupOrderSelect          , avi.OrderSelect                              ) ; //< 800>
else   avi.OrderSelect                                     = GlobalVariableGet( avs.SetupOrderSelect      ) ; //< 801>
                                                                                                              //< 802>
if ( ! GlobalVariableCheck ( avs.SetupEnd                ) )                                                  //< 803>
       GlobalVariableSet   ( avs.SetupEnd                  , aci.SetupSeparator                           ) ; //< 804>
}                                                                                                             //< 805>
//</A.System.Extra: Function 04 >`````````````````````````````````````````````````````````````````````````````//< 806>
                                                                                                              //< 807>
//< A.System.Extra: Function 05 >`````````````````````````````````````````````````````````````````````````````//< 808>
int    afr.CreatePanel.1             ()                                                               //    5 //< 809>
{                                                                                                             //< 810>
static int     i , N =                  aci.TextLines                                                       ; //< 811>
for  ( i = 0 ; i < N ;       i ++ )                                                                           //< 812>
     { abs.TextBuffer.1    [ i ]      = ""                                                                  ; //< 813>
       avs.BufferName.1    [ i ]      = A.System.Series    + A.System.Modification + ".TextBuffer." + i     ; //< 814>
       afr.ResetTextLine.1 ( i )                                                                          ; } //< 815>
}                                                                                                             //< 816>
//</A.System.Extra: Function 05 >`````````````````````````````````````````````````````````````````````````````//< 817>
                                                                                                              //< 818>
//< A.System.Extra: Function 06 >`````````````````````````````````````````````````````````````````````````````//< 819>
int    afr.DeletePanel.1             ()                                                               //    3 //< 820>
{                                                                                                             //< 821>
static int     i , N =                  aci.TextLines                                                       ; //< 822>
for  ( i = 0 ; i < N ; i ++ )                                                                                 //< 823>
     { ObjectDelete                   ( avs.BufferName.1 [ i ] )                                          ; } //< 824>
}                                                                                                             //< 825>
//</A.System.Extra: Function 06 >`````````````````````````````````````````````````````````````````````````````//< 826>
                                                                                                              //< 827>
//< A.System.Extra: Function 07 >`````````````````````````````````````````````````````````````````````````````//< 828>
int    afr.ResetPanel.1              ()                                                               //    7 //< 829>
{                                                                                                             //< 830>
static int     i , N =                  aci.TextLines                                                       ; //< 831>
                                                                                                              //< 832>
for  ( i = 0 ; i < N ;       i ++ )                                                                           //< 833>
     { abs.TextBuffer.1    [ i ]      = ""                                                                  ; //< 834>
       afr.SetTextLine.1   ( i )                                                                          ; } //< 835>
                                                                                                              //< 836>
afr.Report.1                         ()                                                                     ; //< 837>
                                                                                                              //< 838>
for  ( i = 0 ; i < N ;       i ++ )                                                                           //< 839>
       afr.SetTextLine.1   ( i )                                                                            ; //< 840>
}                                                                                                             //< 841>
//</A.System.Extra: Function 07 >`````````````````````````````````````````````````````````````````````````````//< 842>
                                                                                                              //< 843>
//< A.System.Extra: Function 08 >`````````````````````````````````````````````````````````````````````````````//< 844>
int    afr.ResetTextLine.1           (                                                                //    8 //< 845>
       int     aai.Line               )                                                                       //< 846>
{                                                                                                             //< 847>
static string   als.Name    ; als.Name = avs.BufferName.1 [ aai.Line ]                                      ; //< 848>
                                                                                                              //< 849>
ObjectCreate  ( als.Name    , OBJ_LABEL             , 0 , 0 , 0                                           ) ; //< 850>
ObjectSet     ( als.Name    , OBJPROP_XDISTANCE     , arv.Panel.1  [ ari.FontSize  ]                          //< 851>
                                                    * arv.Panel.1  [ ari.PositionX ]                      ) ; //< 852>
ObjectSet     ( als.Name    , OBJPROP_YDISTANCE     , arv.Panel.1  [ ari.FontSize  ] *                        //< 853>
              ( arv.Panel.1 [ ari.PositionY ]       + aai.Line     * arv.Panel.1   [ ari.LineSpace ]    ) ) ; //< 854>
                                                                                                              //< 855>
ObjectSetText ( als.Name    , abs.TextBuffer.1      [ aai.Line   ] ,                                          //< 856>
                arv.Panel.1 [ ari.FontSize  ]       , acs.FontName , arv.Panel.1   [ ari.FontColor ]      ) ; //< 857>
}                                                                                                             //< 858>
//</A.System.Extra: Function 08 >`````````````````````````````````````````````````````````````````````````````//< 859>
                                                                                                              //< 860>
//< A.System.Extra: Function 09 >`````````````````````````````````````````````````````````````````````````````//< 861>
int    afr.SetTextLine.1             (                                                                //    3 //< 862>
       int     aai.Line               )                                                                       //< 863>
{                                                                                                             //< 864>
static string   als.Name    ; als.Name = avs.BufferName.1 [ aai.Line ]                                      ; //< 865>
                                                                                                              //< 866>
ObjectSetText ( als.Name    , abs.TextBuffer.1      [ aai.Line   ] ,                                          //< 867>
                arv.Panel.1 [ ari.FontSize  ]       , acs.FontName , arv.Panel.1   [ ari.FontColor ]      ) ; //< 868>
}                                                                                                             //< 869>
//</A.System.Extra: Function 09 >`````````````````````````````````````````````````````````````````````````````//< 870>
                                                                                                              //< 871>
//< A.System.Extra: Function 10 >`````````````````````````````````````````````````````````````````````````````//< 872>
int    afr.SetText.1                 (                                                                //    9 //< 873>
       int     aai.Line               ,                                                                       //< 874>
       int     aai.Position           ,                                                                       //< 875>
       int     aai.Indent             ,                                                                       //< 876>
       string  aas.Text               )                                                                       //< 877>
{                                                                                                             //< 878>
static int     ali.Begin        ; ali.Begin        = aai.Position     - StringLen ( aas.Text ) * aai.Indent ; //< 879>
                                                                                                              //< 880>
if           ( aai.Indent  == 0 ) ali.Begin        --                                                       ; //< 881>
                                                                                                              //< 882>
if           ( ali.Begin   <= 0 ) abs.TextBuffer.1 [ aai.Line ]       = aas.Text                            ; //< 883>
                                                                                                              //< 884>
else { int     ali.BufferLength = StringLen        ( abs.TextBuffer.1 [ aai.Line         ]     )            ; //< 885>
       if    ( ali.Begin        > ali.BufferLength )                                                          //< 886>
               abs.TextBuffer.1 [ aai.Line       ] = abs.TextBuffer.1 [ aai.Line         ]     +              //< 887>
                   StringSubstr ( acs.Blank  , 0 ,   ali.Begin        - ali.BufferLength )     + aas.Text   ; //< 888>
       else    abs.TextBuffer.1 [ aai.Line       ] =                                                          //< 889>
                   StringSubstr ( abs.TextBuffer.1 [ aai.Line ]   , 0 , ali.Begin        )     + aas.Text ; } //< 890>
}                                                                                                             //< 891>
//</A.System.Extra: Function 10 >`````````````````````````````````````````````````````````````````````````````//< 892>
                                                                                                              //< 893>
//< A.System.Extra: Function 11 >`````````````````````````````````````````````````````````````````````````````//< 894>
int    afr.Report.1                  ()                                                               //  211 //< 895>
{                                                                                                             //< 896>
//< 11.1. Header 3 >                                                                                          //< 897>
static int ali.Trigger ; if ( ! ali.Trigger )    { ali.Trigger = 1 ;             static string als.Header ;   //< 898>
       als.Header            = A.System.Series   + A.System.Modification         + " " + A.System.Program ; } //< 899>
                                                                                                              //< 900>
afr.SetText.1 (   0 ,  1 , 0 , als.Header + ": " + avs.SystemMessage                                      ) ; //< 901>
//</11.1. Header 3 >                                                                                          //< 902>
                                                                                                              //< 903>
//< 11.2. Currency Set Initialization 1 >`````````````````````````````````````````````````````````````````````//< 904>
afr.CurrencyDetector         ( aes.Symbol        , avs.Currency )                                           ; //< 905>
//</11.2. Currency Set Initialization 1 >`````````````````````````````````````````````````````````````````````//< 906>
                                                                                                              //< 907>
//< 11.3. First Cluster: System Report 11 >                                                                   //< 908>
afr.SetText.1 (   2 ,  1 , 0 , "Client Time: "   + afs.Time ( TimeLocal   ()               , 1 )          ) ; //< 909>
afr.SetText.1 (   3 ,  1 , 0 , "Client Name: "   + AccountName                                         () ) ; //< 910>
afr.SetText.1 (   4 ,  1 , 0 , "Server Name: "   + AccountServer                                       () ) ; //< 911>
afr.SetText.1 (   5 ,  1 , 0 , "Server Time: "   + afs.Time ( TimeCurrent ()               , 1 )          ) ; //< 912>
                                                                                                              //< 913>
afr.SetText.1 (   2 , 48 , 1 , afs.Interval      ( TimeLocal ()    - avi.TimeStart         , 1 ) + " / "  ) ; //< 914>
afr.SetText.1 (   5 , 48 , 1 , afs.Interval      ( TimeLocal ()    - avi.TimeLastRun       , 1 ) + " / "  ) ; //< 915>
                                                                                                              //< 916>
afr.SetText.1 (   2 , 49 , 0 , avi.BuyTrades     + "+"             +                                          //< 917>
                               avi.SellTrades    + "="             +                                          //< 918>
                               avi.TotalTrades   + "/"             + avi.Trailes                          ) ; //< 919>
                                                                                                              //< 920>
afr.SetText.1 (   5 , 49 , 0 , avi.Runs          + "/"             +                                          //< 921>
                               avi.AttemptsTrade + "/"             + avi.AttemptsTrail                    ) ; //< 922>
//</11.3. First Cluster: System Report 11 >                                                                   //< 923>
                                                                                                              //< 924>
//< 11.4. Second Cluster: Capital Management Report 57 >                                                      //< 925>
double ald.DrawdownAbs       = AccountEquity     ()   - avd.PeakEquity                                      ; //< 926>
double ald.DrawdownRel       = ald.DrawdownAbs        / avd.PeakEquity                                      ; //< 927>
double ald.CapitalAbs        = avd.PeakEquity         * ( 1 - aed.AccountReserve )                          ; //< 928>
double ald.CapitalRel        = 1 - aed.AccountReserve                                                       ; //< 929>
double ald.CapitalGainAbs    = avd.Capital            - avd.InitialCapital                                  ; //< 930>
double ald.CapitalGainRel    = ald.CapitalGainAbs     / avd.InitialCapital * 100                            ; //< 931>
double ald.EquityGainAbs     = AccountEquity     ()   - avd.InitialEquity                                   ; //< 932>
double ald.EquityGainRel     = ald.EquityGainAbs      / avd.InitialEquity  * 100                            ; //< 933>
double ald.EquityReserveAbs  = AccountEquity     ()   - ald.CapitalAbs                                      ; //< 934>
double ald.EquityReserveRel  = ald.EquityReserveAbs   / avd.PeakEquity                                      ; //< 935>
double ald.AccountEquityAbs  = AccountEquity     ()                                                         ; //< 936>
double ald.AccountEquityRel  = AccountEquity     ()   / avd.PeakEquity                                      ; //< 937>
double ald.AccountFreeMargin = AccountFreeMargin ()                                                         ; //< 938>
double ald.MarginLevel                                                                                      ; //< 939>
string als.StopoutLevelAbs                                                                                  ; //< 940>
string als.StopoutLevelRel                                                                                  ; //< 941>
                                                                                                              //< 942>
if   ( AccountMargin () > 0 )  ald.MarginLevel    = AccountEquity () / AccountMargin ()                     ; //< 943>
else                           ald.MarginLevel    = 0                                                       ; //< 944>
                                                                                                              //< 945>
if   ( AccountStopoutMode () == 0 )                                                                           //< 946>
     { als.StopoutLevelAbs   = DoubleToStr  ( AccountStopoutLevel () * AccountEquity () / 100 , 2 )         ; //< 947>
       als.StopoutLevelRel   =              + AccountStopoutLevel () + ".00%"                             ; } //< 948>
else                                                                                                          //< 949>
     { als.StopoutLevelAbs   =                AccountStopoutLevel () + ".00"                                ; //< 950>
       als.StopoutLevelRel   = DoubleToStr  ( AccountStopoutLevel () / AccountEquity () * 100 , 2 ) + "%" ; } //< 951>
                                                                                                              //< 952>
if   ( ald.EquityGainAbs     > 0 )     string als.GainSign = "+"                                            ; //< 953>
else                                          als.GainSign = ""                                             ; //< 954>
                                                                                                              //< 955>
afr.SetText.1 (   7 ,  1 , 0 , "Capital "   + avs.Currency           [ ari.Account    ]             + ":" ) ; //< 956>
afr.SetText.1 (   8 ,  1 , 0 , "Reserve:"                                                                 ) ; //< 957>
afr.SetText.1 (   9 ,  1 , 0 , "Peak Equity:"                                                             ) ; //< 958>
afr.SetText.1 (  10 ,  1 , 0 , "Drawdown:"                                                                ) ; //< 959>
afr.SetText.1 (  11 ,  1 , 0 , "Acc. Equity:"                                                             ) ; //< 960>
afr.SetText.1 (  12 ,  1 , 0 , "Free Margin:"                                                             ) ; //< 961>
                                                                                                              //< 962>
afr.SetText.1 (   7 , 23 , 1 , DoubleToStr  ( ald.CapitalAbs                      , 2          )          ) ; //< 963>
afr.SetText.1 (   8 , 23 , 1 , DoubleToStr  ( ald.EquityReserveAbs                , 2          )          ) ; //< 964>
afr.SetText.1 (   9 , 23 , 1 , DoubleToStr  ( avd.PeakEquity                      , 2          )          ) ; //< 965>
afr.SetText.1 (  10 , 23 , 1 , DoubleToStr  ( ald.DrawdownAbs                     , 2          )          ) ; //< 966>
afr.SetText.1 (  11 , 23 , 1 , DoubleToStr  ( ald.AccountEquityAbs                , 2          )          ) ; //< 967>
afr.SetText.1 (  12 , 23 , 1 , DoubleToStr  ( ald.AccountFreeMargin               , 2          )          ) ; //< 968>
                                                                                                              //< 969>
afr.SetText.1 (   7 , 32 , 1 , DoubleToStr  ( ald.CapitalRel       * 100          , 2          ) + "%"    ) ; //< 970>
afr.SetText.1 (   7 , 45 , 1 , "+"          + DoubleToStr ( ald.CapitalGainAbs    , 2          )          ) ; //< 971>
afr.SetText.1 (   7 , 55 , 1 , "+"          + DoubleToStr ( ald.CapitalGainRel    , 2          ) + "%"    ) ; //< 972>
afr.SetText.1 (   8 , 32 , 1 , DoubleToStr  ( ald.EquityReserveRel * 100          , 2          ) + "%"    ) ; //< 973>
afr.SetText.1 (   9 , 32 , 1 , DoubleToStr  (                        100          , 2          ) + "%"    ) ; //< 974>
afr.SetText.1 (   9 , 45 , 1 , afs.Interval ( TimeLocal ()         - avd.PeakTime , 1          )          ) ; //< 975>
afr.SetText.1 (  10 , 32 , 1 , DoubleToStr  ( ald.DrawdownRel      * 100          , 2          ) + "%"    ) ; //< 976>
afr.SetText.1 (  11 , 32 , 1 , DoubleToStr  ( ald.AccountEquityRel * 100          , 2          ) + "%"    ) ; //< 977>
afr.SetText.1 (  11 , 45 , 1 , als.GainSign + DoubleToStr ( ald.EquityGainAbs , 2 )                       ) ; //< 978>
afr.SetText.1 (  11 , 55 , 1 , als.GainSign + DoubleToStr ( ald.EquityGainRel , 2 ) + "%"                 ) ; //< 979>
afr.SetText.1 (  12 , 32 , 1 , DoubleToStr  ( ald.MarginLevel      * 100          , 2          ) + "%"    ) ; //< 980>
afr.SetText.1 (  12 , 45 , 1 , als.StopoutLevelAbs                                                        ) ; //< 981>
afr.SetText.1 (  12 , 55 , 1 , als.StopoutLevelRel                                                        ) ; //< 982>
//</11.4. Second Cluster: Capital Management Report 57 >                                                      //< 983>
                                                                                                              //< 984>
//< 11.5. Third Cluster: Position Management Report 100 >                                                     //< 985>
double ald.VARLimit      = AccountEquity ()  * aed.OrderReserve                                             ; //< 986>
int    ali.LotSize       = MarketInfo        ( aes.Symbol          , MODE_LOTSIZE                         ) ; //< 987>
double ald.NominalPoint  = avd.NominalTick   * avd.QuotePoint      / avd.QuoteTick                          ; //< 988>
int    ali.MarginPoints                                                                                     ; //< 989>
if   ( ald.NominalPoint  > 0 )                                                                                //< 990>
       ali.MarginPoints  = MathRound         ( avd.NominalMargin   / ald.NominalPoint                     ) ; //< 991>
else   ali.MarginPoints  = 0                                                                                ; //< 992>
                                                                                                              //< 993>
string als.OrderCurrency [] = { "" , "" , "" , ""                                                         } ; //< 994>
                                                                                                              //< 995>
string als.OrderData     = ""                                                                               ; //< 996>
double ald.ContractSize  = 0                                                                                ; //< 997>
double ald.ContractValue = 0                                                                                ; //< 998>
double ald.OrderPoint    = 0                                                                                ; //< 999>
int    ali.OrderLifetime = 0                                                                                ; //<1000>
                                                                                                              //<1001>
double ald.QuotePrice    = 0                                                                                ; //<1002>
double ald.QuoteTake     = 0                                                                                ; //<1003>
double ald.QuoteStop     = 0                                                                                ; //<1004>
double ald.QuoteTarget   = 0                                                                                ; //<1005>
double ald.QuoteVAR      = 0                                                                                ; //<1006>
                                                                                                              //<1007>
int    ali.OrderProfit   = 0                                                                                ; //<1008>
int    ali.OrderTarget   = 0                                                                                ; //<1009>
int    ali.OrderVAR      = 0                                                                                ; //<1010>
int    ali.OrderLimit    = 0                                                                                ; //<1011>
                                                                                                              //<1012>
double ald.OrderProfit   = 0                                                                                ; //<1013>
double ald.OrderTarget   = 0                                                                                ; //<1014>
double ald.OrderVAR      = 0                                                                                ; //<1015>
double ald.OrderLimit    = 0                                                                                ; //<1016>
                                                                                                              //<1017>
int    ali.OrderTotal    = OrdersTotal ()                                                                   ; //<1018>
                                                                                                              //<1019>
if   ( avi.OrderSelect   > ali.OrderTotal )                                                                   //<1020>
     { avi.OrderSelect   = 0                                                                                ; //<1021>
       GlobalVariableSet ( avs.SetupOrderSelect , avi.OrderSelect                                       ) ; } //<1022>
                                                                                                              //<1023>
if   ( ali.OrderTotal    > 0        )                                                                         //<1024>
     { int   i , N ; N   = ali.OrderTotal - 1                                                               ; //<1025>
       for ( i = N ; i  >= 0 ; i -- )                                                                         //<1026>
           { OrderSelect ( i , SELECT_BY_POS , MODE_TRADES )                                                ; //<1027>
                                                                                                              //<1028>
             if (      avi.OrderSelect      == 0           )                                                  //<1029>
                { if ( OrderMagicNumber ()  != aei.OrderID )                                     continue ; } //<1030>
             else                                                                                             //<1031>
                { if ( avi.OrderSelect      != i + 1       )                                     continue ; } //<1032>
                                                                                                              //<1033>
             afr.CurrencyDetector     ( OrderSymbol     () , als.OrderCurrency                            ) ; //<1034>
                                                                                                              //<1035>
             ald.ContractSize         = OrderLots       ()                                                  ; //<1036>
             ald.ContractValue        = ald.ContractSize   / avd.QuotePoint  * ald.NominalPoint             ; //<1037>
             ald.OrderPoint           = ald.NominalPoint   * ald.ContractSize                               ; //<1038>
             ali.OrderLifetime        = TimeCurrent     () - OrderOpenTime   ()                             ; //<1039>
                                                                                                              //<1040>
             ald.QuotePrice           = OrderOpenPrice  ()                                                  ; //<1041>
             ald.QuoteTake            = OrderTakeProfit ()                                                  ; //<1042>
             ald.QuoteStop            = OrderStopLoss   ()                                                  ; //<1043>
                                                                                                              //<1044>
             ald.QuoteTarget          = MathAbs            ( ald.QuotePrice  - ald.QuoteTake              ) ; //<1045>
                                                                                                              //<1046>
             if ( OrderType ()       == OP_BUY               )                                                //<1047>
                  ald.QuoteVAR        = ald.QuoteStop      - ald.QuotePrice                                 ; //<1048>
             else ald.QuoteVAR        = ald.QuotePrice     - ald.QuoteStop                                  ; //<1049>
                                                                                                              //<1050>
             ald.OrderProfit          = OrderProfit     ()                                                  ; //<1051>
             ald.OrderTarget          = ald.QuoteTarget    * ald.ContractValue                              ; //<1052>
             ald.OrderVAR             = ald.QuoteVAR       * ald.ContractValue                              ; //<1053>
             ald.OrderLimit           = ald.VARLimit                                                        ; //<1054>
                                                                                                              //<1055>
             ali.OrderProfit          = MathRound          ( ald.OrderProfit / ald.OrderPoint )             ; //<1056>
             ali.OrderTarget          = MathRound          ( ald.QuoteTarget / avd.QuotePoint )             ; //<1057>
             ali.OrderVAR             = MathRound          ( ald.QuoteVAR    / avd.QuotePoint )             ; //<1058>
             ali.OrderLimit           = MathRound          ( ald.OrderLimit  / ald.OrderPoint )             ; //<1059>
                                                                                                              //<1060>
             als.OrderData            = "#"                + OrderTicket ()                      + " "   +    //<1061>
                                        acs.Operation      [ OrderType   ()  ]                   + " "   +    //<1062>
                                        DoubleToStr        ( ald.ContractSize             , 2 )  + " x " +    //<1063>
                                        DoubleToStr        ( ali.LotSize                  , 0 )  + " "   +    //<1064>
                                        als.OrderCurrency  [ ari.Margin      ]                   + " / " +    //<1065>
                                        ali.MarginPoints                                         + " x " +    //<1066>
                                        DoubleToStr        ( ald.OrderPoint               , 2 )  + " / " +    //<1067>
                                        afs.Interval       ( ali.OrderLifetime            , 1 )             ; //<1068>
       } // for                                                                                               //<1069>
   } // if                                                                                                    //<1070>
                                                                                                              //<1071>
if   ( ald.OrderProfit > 0 )   string   als.OPSign         = "+"                                            ; //<1072>
else                                    als.OPSign         = ""                                             ; //<1073>
                                                                                                              //<1074>
if   ( ald.OrderTarget > 0 )   string   als.OTSign         = "+"                                            ; //<1075>
else                                    als.OTSign         = ""                                             ; //<1076>
                                                                                                              //<1077>
if   ( ald.OrderVAR    > 0 )   string   als.OVSign         = "+"                                            ; //<1078>
else                                    als.OVSign         = ""                                             ; //<1079>
                                                                                                              //<1080>
if   ( ald.QuotePrice  > 0 )   string   als.OrderPrice     = DoubleToStr ( ald.QuotePrice    , avi.Digits ) ; //<1081>
else                                    als.OrderPrice     = ""                                             ; //<1082>
                                                                                                              //<1083>
if   ( ald.QuoteTake   > 0 )   string   als.OrderTake      = DoubleToStr ( ald.QuoteTake     , avi.Digits ) ; //<1084>
else                                    als.OrderTake      = ""                                             ; //<1085>
                                                                                                              //<1086>
if   ( ald.QuoteStop   > 0 )   string   als.OrderStop      = DoubleToStr ( ald.QuoteStop     , avi.Digits ) ; //<1087>
else                                    als.OrderStop      = ""                                             ; //<1088>
                                                                                                              //<1089>
afr.SetText.1 (  14 ,  1 , 0 , als.OrderData                                                              ) ; //<1090>
afr.SetText.1 (  15 ,  1 , 0 , "Profit:"                                                                  ) ; //<1091>
afr.SetText.1 (  15 , 23 , 1 , als.OPSign   + DoubleToStr  ( ald.OrderProfit                        , 2 ) ) ; //<1092>
afr.SetText.1 (  15 , 32 , 1 , als.OPSign                  + ali.OrderProfit                              ) ; //<1093>
afr.SetText.1 (  15 , 45 , 1 , als.OPSign   + DoubleToStr  ( ald.OrderProfit / AccountEquity () * 100 , 2 )   //<1094>
                                                                                                    + "%" ) ; //<1095>
afr.SetText.1 (  15 , 55 , 1 , als.OrderPrice                                                             ) ; //<1096>
afr.SetText.1 (  16 ,  1 , 0 , "Target:"                                                                  ) ; //<1097>
afr.SetText.1 (  16 , 23 , 1 , als.OTSign   + DoubleToStr  ( ald.OrderTarget                        , 2 ) ) ; //<1098>
afr.SetText.1 (  16 , 32 , 1 , als.OTSign                  + ali.OrderTarget                              ) ; //<1099>
afr.SetText.1 (  16 , 45 , 1 , als.OTSign   + DoubleToStr  ( ald.OrderTarget / AccountEquity () * 100 , 2 )   //<1100>
                                                                                                    + "%" ) ; //<1101>
afr.SetText.1 (  16 , 55 , 1 , als.OrderTake                                                              ) ; //<1102>
afr.SetText.1 (  17 ,  1 , 0 , "VAR:"                                                                     ) ; //<1103>
afr.SetText.1 (  17 , 23 , 1 , als.OVSign   + DoubleToStr  ( ald.OrderVAR                           , 2 ) ) ; //<1104>
afr.SetText.1 (  17 , 32 , 1 , als.OVSign                  + ali.OrderVAR                                 ) ; //<1105>
afr.SetText.1 (  17 , 45 , 1 , als.OVSign   + DoubleToStr  ( ald.OrderVAR    / AccountEquity () * 100 , 2 )   //<1106>
                                                                                                    + "%" ) ; //<1107>
afr.SetText.1 (  17 , 55 , 1 , als.OrderStop                                                              ) ; //<1108>
afr.SetText.1 (  18 ,  1 , 0 , "Limit:"                                                                   ) ; //<1109>
afr.SetText.1 (  18 , 23 , 1 , DoubleToStr  ( - ald.VARLimit                                        , 2 ) ) ; //<1110>
afr.SetText.1 (  18 , 32 , 1 , "-"                         + ali.OrderLimit                               ) ; //<1111>
afr.SetText.1 (  18 , 45 , 1 , "-"          + DoubleToStr  ( aed.OrderReserve * 100 , 2           ) + "%" ) ; //<1112>
//</11.5. Third Cluster: Position Management Report 100 >                                                     //<1113>
                                                                                                              //<1114>
//< 11.6. Leverage/Contract Specification Indicator 14 >                                                      //<1115>
string als.Leverage          = "1:"                                                                      +    //<1116>
                               AccountLeverage ()                                                + " / " +    //<1117>
                               DoubleToStr  ( avd.NominalMargin                   , 2          ) + " "   +    //<1118>
                               avs.Currency [ ari.Account  ]                                     + " = " +    //<1119>
                               ali.MarginPoints                                           + " points x " +    //<1120>
                               DoubleToStr  ( ald.NominalPoint                    , 2          ) + " "   +    //<1121>
                               avs.Currency [ ari.Account  ]                                                ; //<1122>
                                                                                                              //<1123>
string als.Contract          = DoubleToStr  ( MarketInfo   ( aes.Symbol , MODE_LOTSIZE ) , 2 )   + " "   +    //<1124>
                               avs.Currency [ ari.Margin   ]                                     + " / " +    //<1125>
                               DoubleToStr  ( MarketInfo   ( aes.Symbol , MODE_MINLOT  ) , 2 )   + " / " +    //<1126>
                               DoubleToStr  ( MarketInfo   ( aes.Symbol , MODE_LOTSTEP ) , 2 )   + " / " +    //<1127>
                               DoubleToStr  ( MarketInfo   ( aes.Symbol , MODE_MAXLOT  ) , 2 )              ; //<1128>
                                                                                                              //<1129>
afr.SetText.1 (  20 ,  1 , 0 , "Leverage:    " + als.Leverage                                             ) ; //<1130>
afr.SetText.1 (  21 ,  1 , 0 , "Contract:    " + als.Contract                                             ) ; //<1131>
//</11.6. Leverage/Contract Specification Indicator 14 >                                                      //<1132>
}                                                                                                             //<1133>
//</A.System.Extra: Function 11 >`````````````````````````````````````````````````````````````````````````````//<1134>
                                                                                                              //<1135>
//< A.System.Extra: Function 12 >`````````````````````````````````````````````````````````````````````````````//<1136>
string afs.Interval                  (                                                                //    7 //<1137>
       int     aai.Interval           ,                                                                       //<1138>
       int     aai.Seconds   = EMPTY  )                                                                       //<1139>
{                                                                                                             //<1140>
static string  als.Result                                                                                   ; //<1141>
                                                                                                              //<1142>
static int     ali.Interval          ;  ali.Interval       = MathAbs       ( aai.Interval                 ) ; //<1143>
if           ( aai.Seconds  == EMPTY )  als.Result         = TimeToStr     ( ali.Interval , TIME_MINUTES  ) ; //<1144>
else                                    als.Result         = TimeToStr     ( ali.Interval , TIME_SECONDS  ) ; //<1145>
                                                                                                              //<1146>
if           ( ali.Interval >= 86400 )  als.Result         = ali.Interval  / 86400 + " "  + als.Result      ; //<1147>
else if      ( aai.Interval  < 0     )  als.Result         = "-"                          + als.Result      ; //<1148>
                                                                                                              //<1149>
return       ( als.Result                                                                                 ) ; //<1150>
}                                                                                                             //<1151>
//</A.System.Extra: Function 12 >`````````````````````````````````````````````````````````````````````````````//<1152>
                                                                                                              //<1153>
//< A.System.Extra: Function 13 >`````````````````````````````````````````````````````````````````````````````//<1154>
//</A.System.Extra: Function 13 >`````````````````````````````````````````````````````````````````````````````//<1155>
                                                                                                              //<1156>
//< A.System.Extra: Function 14 >`````````````````````````````````````````````````````````````````````````````//<1157>
string afs.Time                      (                                                                //    5 //<1158>
       int     aai.Time               ,                                                                       //<1159>
       int     aai.Seconds   = EMPTY  )                                                                       //<1160>
{                                                                                                             //<1161>
static string  als.Result                                                                                   ; //<1162>
                                                                                                              //<1163>
if           ( aai.Seconds  == EMPTY  ) int ali.Mode       = TIME_DATE     | TIME_MINUTES                   ; //<1164>
else                                        ali.Mode       = TIME_DATE     | TIME_SECONDS                   ; //<1165>
                                                                                                              //<1166>
               als.Result    = TimeToStr  ( aai.Time       , ali.Mode                                     ) ; //<1167>
                                                                                                              //<1168>
return       ( als.Result                                                                                 ) ; //<1169>
}                                                                                                             //<1170>
//</A.System.Extra: Function 14 >`````````````````````````````````````````````````````````````````````````````//<1171>
                                                                                                              //<1172>
//< A.System.Extra: Function 15 >`````````````````````````````````````````````````````````````````````````````//<1173>
int    afr.CurrencyDetector          (                                                                //   16 //<1174>
       string  aas.Symbol             ,                                                                       //<1175>
       string& aas.Currency []        )                                                                       //<1176>
{                                                                                                             //<1177>
aas.Currency        [ ari.Account ] = AccountCurrency ()                                                    ; //<1178>
                                                                                                              //<1179>
if ( ( MarketInfo   ( aas.Symbol  , MODE_PROFITCALCMODE    ) ==  0 )                                          //<1180>
  && ( MarketInfo   ( aas.Symbol  , MODE_MARGINCALCMODE    ) ==  0 )                                          //<1181>
  && ( StringLen    ( aas.Symbol                           ) ==  6 )                                          //<1182>
  && ( StringFind   ( aas.Symbol  , "#"                    ) == -1 )                                          //<1183>
  && ( StringFind   ( aas.Symbol  , "@"                    ) == -1 )                                          //<1184>
  && ( StringFind   ( aas.Symbol  , "_"                    ) == -1 )             )                            //<1185>
     { aas.Currency [ ari.Base    ]         = StringSubstr ( aas.Symbol  , 0 , 3 )                          ; //<1186>
       aas.Currency [ ari.Quote   ]         = StringSubstr ( aas.Symbol  , 3 , 3 )                        ; } //<1187>
else { aas.Currency [ ari.Base    ]         = aas.Symbol                                                    ; //<1188>
       aas.Currency [ ari.Quote   ]         = aas.Currency [ ari.Account ]                                ; } //<1189>
                                                                                                              //<1190>
if   ( avd.NominalMargin > 0 )                                                                                //<1191>
     { if ( AccountLeverage ()             == MathRound    ( avd.NominalLot  / avd.NominalMargin ) )          //<1192>
            aas.Currency   [ ari.Margin  ]  = aas.Currency [ ari.Account ]                                  ; //<1193>
       else aas.Currency   [ ari.Margin  ]  = aas.Currency [ ari.Base    ]                                ; } //<1194>
else { aas.Currency        [ ari.Margin  ]  = ""                                                          ; } //<1195>
}                                                                                                             //<1196>
//</A.System.Extra: Function 15 >`````````````````````````````````````````````````````````````````````````````//<1197>
                                                                                                              //<1198>
//< A.System.Extra: Function 16 >`````````````````````````````````````````````````````````````````````````````//<1199>
int    afr.TrailingStrategy          ()                                                               //    1 //<1200>
{                                                                                                             //<1201>
avd.Stop     = EMPTY                                                                                        ; //<1202>
}                                                                                                             //<1203>
//</A.System.Extra: Function 16 >`````````````````````````````````````````````````````````````````````````````//<1204>
                                                                                                              //<1205>
//< A.System.Extra: Function 17 >-----------------------------------------------------------------------------//<1206>
int    afr.TradingStrategy           ()                                                               //  422 //<1207>
{                                                                                                             //<1208>
//< 17.1. First Run Subroutine 27 >```````````````````````````````````````````````````````````````````````````//<1209>
                                                                                                              //<1210>
//< 17.7.1. First Run Subroutine Entry Point 1 >                                                              //<1211>
static int ali.Trigger ; if ( ali.Trigger == 0 ) { ali.Trigger = 1                                          ; //<1212>
//</17.7.1. First Run Subroutine Entry Point 1 >                                                              //<1213>
                                                                                                              //<1214>
//< 17.1.2. Constants Declaration 4 >                                                                         //<1215>
       static int    aci.Buy          = 0                                                                   ; //<1216>
       static int    aci.Sell         = 1                                                                   ; //<1217>
       static int    aci.Modify       = 6                                                                   ; //<1218>
       static int    aci.Close        = 7                                                                   ; //<1219>
//</17.1.2. Constants Declaration 4 >                                                                         //<1220>
                                                                                                              //<1221>
//< 17.1.3. Variables Declaration 12 >                                                                        //<1222>
       //< 17.1.3.1. Control Interface 4 >                                                                    //<1223>
       int           ali.Command                                                                            ; //<1224>
       double        ald.Price                                                                              ; //<1225>
       double        ald.Take                                                                               ; //<1226>
       double        ald.Stop                                                                               ; //<1227>
       //</17.1.3.1. Control Interface 4 >                                                                    //<1228>
                                                                                                              //<1229>
       //< 17.1.3.2. Open Position Data 4 >                                                                   //<1230>
       int           ali.OrdersTotal                                                                        ; //<1231>
       int           ali.OrderType                                                                          ; //<1232>
       double        ald.OrderOpenPrice                                                                     ; //<1233>
       double        ald.OrderStop                                                                          ; //<1234>
       //</17.1.3.2. Open Position Data 4 >                                                                   //<1235>
                                                                                                              //<1236>
       //< 17.1.3.3. Position Management Data 6 >                                                             //<1237>
       int           ali.EmptyTake                                                                          ; //<1238>
       int           ali.EmptyStop                                                                          ; //<1239>
                                                                                                              //<1240>
       double        ald.DistanceTake                                                                       ; //<1241>
       double        ald.DistanceStop                                                                       ; //<1242>
                                                                                                              //<1243>
       double        ald.NewTake                                                                            ; //<1244>
       double        ald.NewStop                                                                            ; //<1245>
       //</17.1.3.3. Position Management Data 6 >                                                             //<1246>
//</17.1.3. Variables Declaration 12 >                                                                        //<1247>
                                                                                                              //<1248>
//< 17.1.4. Control Interface Creation 9 >                                                                    //<1249>
       static string avs.SetupCommand                                                                       ; //<1250>
       static string avs.SetupTake                                                                          ; //<1251>
       static string avs.SetupStop                                                                          ; //<1252>
                                                                                                              //<1253>
       avs.SetupCommand               = avs.SetupPrefix           + "5.1." + "Command"                      ; //<1254>
       avs.SetupTake                  = avs.SetupPrefix           + "5.2." + "Take"                         ; //<1255>
       avs.SetupStop                  = avs.SetupPrefix           + "5.3." + "Stop"                         ; //<1256>
                                                                                                              //<1257>
       GlobalVariableSet              ( avs.SetupCommand          , EMPTY                                 ) ; //<1258>
       GlobalVariableSet              ( avs.SetupTake             , EMPTY                                 ) ; //<1259>
       GlobalVariableSet              ( avs.SetupStop             , EMPTY                                 ) ; //<1260>
//</17.1.4. Control Interface Creation 9 >                                                                    //<1261>
                                                                                                              //<1262>
//< 17.7.5. First Run Subroutine Exit Point 1 >                                                               //<1263>
}                                                                                                             //<1264>
//</17.7.5. First Run Subroutine Exit Point 1 >                                                               //<1265>
                                                                                                              //<1266>
//</17.1. First Run Subroutine 27 >```````````````````````````````````````````````````````````````````````````//<1267>
                                                                                                              //<1268>
//< 17.2. Control Interface Read 15 >`````````````````````````````````````````````````````````````````````````//<1269>
if   ( GlobalVariableCheck            ( avs.SetupCommand        ) )                                           //<1270>
       ali.Command                    = GlobalVariableGet ( avs.SetupCommand                              ) ; //<1271>
else { ali.Command                    = EMPTY                                                               ; //<1272>
       GlobalVariableSet              ( avs.SetupCommand          , EMPTY                                 ) ; //<1273>
     }                                                                                                        //<1274>
                                                                                                              //<1275>
if   ( GlobalVariableCheck            ( avs.SetupTake           ) )                                           //<1276>
       ald.Take                       = GlobalVariableGet ( avs.SetupTake                                 ) ; //<1277>
else { ald.Take                       = EMPTY                                                               ; //<1278>
       GlobalVariableSet              ( avs.SetupTake             , EMPTY                                 ) ; //<1279>
     }                                                                                                        //<1280>
                                                                                                              //<1281>
if   ( GlobalVariableCheck            ( avs.SetupStop           ) )                                           //<1282>
       ald.Stop                       = GlobalVariableGet ( avs.SetupStop                                 ) ; //<1283>
else { ald.Stop                       = EMPTY                                                               ; //<1284>
       GlobalVariableSet              ( avs.SetupStop             , EMPTY                                 ) ; //<1285>
     }                                                                                                        //<1286>
//</17.2. Control Interface Read 15 >`````````````````````````````````````````````````````````````````````````//<1287>
                                                                                                              //<1288>
//< 17.3. Open Position Analysis 14 >`````````````````````````````````````````````````````````````````````````//<1289>
ali.OrderType                         = EMPTY                                                               ; //<1290>
ald.OrderOpenPrice                    = EMPTY                                                               ; //<1291>
ald.OrderStop                         = EMPTY                                                               ; //<1292>
ali.OrdersTotal                       = OrdersTotal        ()                                               ; //<1293>
                                                                                                              //<1294>
if ( ali.OrdersTotal  > 0         )                                                                           //<1295>
   {                                                                                                          //<1296>
     int   i , N ; N  = ali.OrdersTotal - 1                                                                 ; //<1297>
     for ( i = N ; i >= 0 ; i --  )                                                                           //<1298>
         { OrderSelect    ( i , SELECT_BY_POS , MODE_TRADES )                                               ; //<1299>
           if ( OrderMagicNumber ()  != aei.OrderID         )                                      continue ; //<1300>
           else                                                                                               //<1301>
              {                                                                                               //<1302>
                ali.OrderType         = OrderType          ()                                               ; //<1303>
                ald.OrderOpenPrice    = OrderOpenPrice     ()                                               ; //<1304>
                ald.OrderStop         = OrderStopLoss      ()                                               ; //<1305>
              } // else                                                                                       //<1306>
         } // for                                                                                             //<1307>
   } // if                                                                                                    //<1308>
//</17.3. Open Position Analysis 14 >`````````````````````````````````````````````````````````````````````````//<1309>
                                                                                                              //<1310>
//< 17.4. Control Interface Validation 265 >``````````````````````````````````````````````````````````````````//<1311>
                                                                                                              //<1312>
//< 17.4.1. Validation Flag Declaration 1 > ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` //<1313>
int       ali.Validation                                                                                    ; //<1314>
//</17.4.1. Validation Flag Declaration 1 > ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` //<1315>
                                                                                                              //<1316>
//< 17.4.2. Buy Command Validation 52 > ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` //<1317>
                                                                                                              //<1318>
          //< 17.4.2.1. Buy Command Validation Entry Point 5 >                                                //<1319>
if      ( ali.Command == aci.Buy          )                                                                   //<1320>
        {                                                                                                     //<1321>
          Alert        ( avs.SystemStamp ,                                                                    //<1322>
                         ": Buy command is detected at Ask price = "                                      ,   //<1323>
                         DoubleToStr  ( avd.QuoteAsk                                       , avi.Digits ) ) ; //<1324>
          //</17.4.2.1. Buy Command Validation Entry Point 5 >                                                //<1325>
                                                                                                              //<1326>
          //< 17.4.2.2. Command Validation 14 >                                                               //<1327>
          if   ( ali.OrdersTotal     == 0 )                                                                   //<1328>
               {                                                                                              //<1329>
                 ali.Validation       = 1                                                                   ; //<1330>
                 ald.Price            = NormalizeDouble ( avd.QuoteAsk                     , avi.Digits   ) ; //<1331>
                 ald.Take             = NormalizeDouble ( ald.Take                         , avi.Digits   ) ; //<1332>
                 ald.Stop             = NormalizeDouble ( ald.Stop                         , avi.Digits   ) ; //<1333>
                 ald.DistanceTake     = ald.Take                          - ald.Price                       ; //<1334>
                 ald.DistanceStop     = ald.Price       - avd.QuoteSpread - ald.Stop                        ; //<1335>
               }                                                                                              //<1336>
          else { ali.Validation       = 0                                                                   ; //<1337>
                 Alert ( avs.SystemStamp ,                                                                    //<1338>
                         ": Buy command is disabled due to open orders detected = "                       ,   //<1339>
                         ali.OrdersTotal                                                                  ) ; //<1340>
               }                                                                                              //<1341>
          //</17.4.2.2. Command Validation 14 >                                                               //<1342>
                                                                                                              //<1343>
          //< 17.4.2.3. Take Price Validation 15 >                                                            //<1344>
          if   ( ali.Validation      == 1 )                                                                   //<1345>
          if   ( avd.QuoteStops       - ald.DistanceTake > 0 )                                                //<1346>
               { ali.Validation       = 0                                                                   ; //<1347>
                 Alert ( avs.SystemStamp ,                                                                    //<1348>
                         ": Buy command is disabled due to invalid Take distance = "                      ,   //<1349>
                         DoubleToStr  ( ald.DistanceTake                                   , avi.Digits ) ,   //<1350>
                         " / "                                                                            ,   //<1351>
                         DoubleToStr  ( avd.QuoteStops                                     , avi.Digits ) ) ; //<1352>
                 Alert ( avs.SystemStamp ,                                                                    //<1353>
                         ": Buy command current Take price = "                                            ,   //<1354>
                         DoubleToStr  ( ald.Take                                           , avi.Digits ) ) ; //<1355>
                 Alert ( avs.SystemStamp ,                                                                    //<1356>
                         ": Buy command minimal Take price = "                                            ,   //<1357>
                         DoubleToStr  ( ald.Price + avd.QuoteStops                         , avi.Digits ) ) ; //<1358>
               }                                                                                              //<1359>
          //</17.4.2.3. Take Price Validation 15 >                                                            //<1360>
                                                                                                              //<1361>
          //< 17.4.2.4. Stop Price Validation 15 >                                                            //<1362>
          if   ( ali.Validation      == 1 )                                                                   //<1363>
          if   ( avd.QuoteStops       - ald.DistanceStop > 0 )                                                //<1364>
               { ali.Validation       = 0                                                                   ; //<1365>
                 Alert ( avs.SystemStamp ,                                                                    //<1366>
                         ": Buy command is disabled due to invalid Stop distance = "                      ,   //<1367>
                         DoubleToStr  ( ald.DistanceStop                                   , avi.Digits ) ,   //<1368>
                         " / "                                                                            ,   //<1369>
                         DoubleToStr  ( avd.QuoteStops                                     , avi.Digits ) ) ; //<1370>
                 Alert ( avs.SystemStamp ,                                                                    //<1371>
                         ": Buy command current Stop price = "                                            ,   //<1372>
                         DoubleToStr  ( ald.Stop                                           , avi.Digits ) ) ; //<1373>
                 Alert ( avs.SystemStamp ,                                                                    //<1374>
                         ": Buy command maximal Stop price = "                                            ,   //<1375>
                         DoubleToStr  ( ald.Price - avd.QuoteSpread - avd.QuoteStops       , avi.Digits ) ) ; //<1376>
               }                                                                                              //<1377>
          //</17.4.2.4. Stop Price Validation 15 >                                                            //<1378>
                                                                                                              //<1379>
          //< 17.4.2.5. Buy Command Validation Exit Point 3 >                                                 //<1380>
          if   ( ali.Validation      == 0 )                                                                   //<1381>
                 ali.Command          = EMPTY                                                               ; //<1382>
        }                                                                                                     //<1383>
          //</17.4.2.5. Buy Command Validation Exit Point 3 >                                                 //<1384>
                                                                                                              //<1385>
//</17.4.2. Buy Command Validation 52 > ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` //<1386>
                                                                                                              //<1387>
//< 17.4.3. Sell Command Validation 52 > ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` `//<1388>
                                                                                                              //<1389>
          //< 17.4.3.1. Sell Command Validation Entry Point 5 >                                               //<1390>
else if ( ali.Command == aci.Sell         )                                                                   //<1391>
        {                                                                                                     //<1392>
          Alert        ( avs.SystemStamp ,                                                                    //<1393>
                         ": Sell command is detected at Bid price = "                                     ,   //<1394>
                         DoubleToStr  ( avd.QuoteBid                                       , avi.Digits ) ) ; //<1395>
          //</17.4.3.1. Sell Command Validation Entry Point 5 >                                               //<1396>
                                                                                                              //<1397>
          //< 17.4.3.2. Command Validation 14 >                                                               //<1398>
          if   ( ali.OrdersTotal     == 0 )                                                                   //<1399>
               {                                                                                              //<1400>
                 ali.Validation       = 1                                                                   ; //<1401>
                 ald.Price            = NormalizeDouble ( avd.QuoteBid                     , avi.Digits   ) ; //<1402>
                 ald.Take             = NormalizeDouble ( ald.Take                         , avi.Digits   ) ; //<1403>
                 ald.Stop             = NormalizeDouble ( ald.Stop                         , avi.Digits   ) ; //<1404>
                 ald.DistanceTake     = ald.Price                         - ald.Take                        ; //<1405>
                 ald.DistanceStop     = ald.Stop        - ald.Price       - avd.QuoteSpread                 ; //<1406>
               }                                                                                              //<1407>
          else { ali.Validation       = 0                                                                   ; //<1408>
                 Alert ( avs.SystemStamp ,                                                                    //<1409>
                         ": Sell command is disabled due to open orders detected = "                      ,   //<1410>
                         ali.OrdersTotal                                                                  ) ; //<1411>
               }                                                                                              //<1412>
          //</17.4.3.2. Command Validation 14 >                                                               //<1413>
                                                                                                              //<1414>
          //< 17.4.3.3. Take Price Validation 15 >                                                            //<1415>
          if   ( ali.Validation      == 1 )                                                                   //<1416>
          if   ( avd.QuoteStops       - ald.DistanceTake > 0 )                                                //<1417>
               { ali.Validation       = 0                                                                   ; //<1418>
                 Alert ( avs.SystemStamp ,                                                                    //<1419>
                         ": Sell command is disabled due to invalid Take distance = "                     ,   //<1420>
                         DoubleToStr  ( ald.DistanceTake                                   , avi.Digits ) ,   //<1421>
                         " / "                                                                            ,   //<1422>
                         DoubleToStr  ( avd.QuoteStops                                     , avi.Digits ) ) ; //<1423>
                 Alert ( avs.SystemStamp ,                                                                    //<1424>
                         ": Sell command current Take price = "                                           ,   //<1425>
                         DoubleToStr  ( ald.Take                                           , avi.Digits ) ) ; //<1426>
                 Alert ( avs.SystemStamp ,                                                                    //<1427>
                         ": Sell command maximal Take price = "                                           ,   //<1428>
                         DoubleToStr  ( ald.Price - avd.QuoteStops                         , avi.Digits ) ) ; //<1429>
               }                                                                                              //<1430>
          //</17.4.3.3. Take Price Validation 15 >                                                            //<1431>
                                                                                                              //<1432>
          //< 17.4.3.4. Stop Price Validation 15 >                                                            //<1433>
          if   ( ali.Validation      == 1 )                                                                   //<1434>
          if   ( avd.QuoteStops       - ald.DistanceStop > 0 )                                                //<1435>
               { ali.Validation       = 0                                                                   ; //<1436>
                 Alert ( avs.SystemStamp ,                                                                    //<1437>
                         ": Sell command is disabled due to invalid Stop distance = "                     ,   //<1438>
                         DoubleToStr  ( ald.DistanceStop                                   , avi.Digits ) ,   //<1439>
                         " / "                                                                            ,   //<1440>
                         DoubleToStr  ( avd.QuoteStops                                     , avi.Digits ) ) ; //<1441>
                 Alert ( avs.SystemStamp ,                                                                    //<1442>
                         ": Sell command current Stop price = "                                           ,   //<1443>
                         DoubleToStr  ( ald.Stop                                           , avi.Digits ) ) ; //<1444>
                 Alert ( avs.SystemStamp ,                                                                    //<1445>
                         ": Sell command minimal Stop price = "                                           ,   //<1446>
                         DoubleToStr  ( ald.Price + avd.QuoteSpread + avd.QuoteStops       , avi.Digits ) ) ; //<1447>
               }                                                                                              //<1448>
          //</17.4.3.4. Stop Price Validation 15 >                                                            //<1449>
                                                                                                              //<1450>
          //< 17.4.3.5. Sell Command Validation Exit Point 3 >                                                //<1451>
          if   ( ali.Validation      == 0 )                                                                   //<1452>
                 ali.Command          = EMPTY                                                               ; //<1453>
        }                                                                                                     //<1454>
          //</17.4.3.5. Sell Command Validation Exit Point 3 >                                                //<1455>
                                                                                                              //<1456>
//</17.4.3. Sell Command Validation 52 > ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` `//<1457>
                                                                                                              //<1458>
//< 17.4.4. Modify Command Validation 137 > ` `` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` `//<1459>
                                                                                                              //<1460>
          //< 17.4.4.1. Modify Command Validation Entry Point 6 >                                             //<1461>
else if ( ali.Command == aci.Modify )                                                                         //<1462>
        {                                                                                                     //<1463>
          if   ( ali.OrdersTotal      > 0 )                                                                   //<1464>
                 avs.LocalStamp       = " for " + acs.Operation [ OrderType () ] + " order"                 ; //<1465>
          else   avs.LocalStamp       = ""                                                                  ; //<1466>
                                                                                                              //<1467>
          Alert        ( avs.SystemStamp        , ": Modify command is detected" , avs.LocalStamp         ) ; //<1468>
          //</17.4.4.1. Modify Command Validation Entry Point 6 >                                             //<1469>
                                                                                                              //<1470>
          //< 17.4.4.2. Modify Command Validation 35 >                                                        //<1471>
          if   ( ali.OrdersTotal      > 0 )                                                                   //<1472>
               { ali.Validation       = 1                                                                   ; //<1473>
                                                                                                              //<1474>
                 if ( avd.QuoteStops - avd.QuoteFreeze > 0 ) double ald.DistanceModify = avd.QuoteStops     ; //<1475>
                 else                                               ald.DistanceModify = avd.QuoteFreeze    ; //<1476>
                                                                                                              //<1477>
                 if ( ald.Take        < 0 )                                                                   //<1478>
                    { ald.Take        = NormalizeDouble ( OrderTakeProfit ()               , avi.Digits   ) ; //<1479>
                      ali.EmptyTake   = 1                                                                   ; //<1480>
                    }                                                                                         //<1481>
                 else                                                                                         //<1482>
                    { ali.EmptyTake   = 0                                                                   ; //<1483>
                      ald.Take        = NormalizeDouble ( ald.Take                         , avi.Digits   ) ; //<1484>
                    }                                                                                         //<1485>
                                                                                                              //<1486>
                 if ( ald.Stop        < 0 )                                                                   //<1487>
                    { ald.Stop        = NormalizeDouble ( OrderStopLoss   ()               , avi.Digits   ) ; //<1488>
                      ali.EmptyStop   = 1                                                                   ; //<1489>
                    }                                                                                         //<1490>
                 else                                                                                         //<1491>
                    { ali.EmptyStop   = 0                                                                   ; //<1492>
                      ald.Stop        = NormalizeDouble ( ald.Stop                         , avi.Digits   ) ; //<1493>
                    }                                                                                         //<1494>
                                                                                                              //<1495>
                 if ( ali.OrderType  == aci.Buy            )                                                  //<1496>
                    { ald.Price       = NormalizeDouble ( avd.QuoteBid                     , avi.Digits   ) ; //<1497>
                      ald.DistanceTake  = ald.Take      - ald.Price                                         ; //<1498>
                      ald.DistanceStop  = ald.Price     - ald.Stop                                          ; //<1499>
                    }                                                                                         //<1500>
                                                                                                              //<1501>
                 if ( ali.OrderType  == aci.Sell           )                                                  //<1502>
                    { ald.Price       = NormalizeDouble ( avd.QuoteAsk                     , avi.Digits   ) ; //<1503>
                      ald.DistanceTake  = ald.Price     - ald.Take                                          ; //<1504>
                      ald.DistanceStop  = ald.Stop      - ald.Price                                         ; //<1505>
                    }                                                                                         //<1506>
               } // if                                                                                        //<1507>
          else { ali.Validation       = 0                                                                   ; //<1508>
                 Alert ( avs.SystemStamp ,                                                                    //<1509>
                         ": Modify command is ignored due to empty order list"                            ) ; //<1510>
               } // else                                                                                      //<1511>
          //</17.4.4.2. Modify Command Validation 35 >                                                        //<1512>
                                                                                                              //<1513>
          //< 17.4.4.3. Empty Levels Validation 15 >                                                          //<1514>
          if   ( ali.Validation      == 1                       )                                             //<1515>
          if   ( ali.EmptyTake       == 1 && ali.EmptyStop == 1 )                                             //<1516>
               {                                                                                              //<1517>
                 ali.Validation       = 0                                                                   ; //<1518>
                 Alert ( avs.SystemStamp ,                                                                    //<1519>
                         ": Modify command is ignored due to empty price levels"                          ) ; //<1520>
               }                                                                                              //<1521>
                                                                                                              //<1522>
          if   ( ali.Validation      == 1                                                )                    //<1523>
          if   ( MathRound ( ( ald.Take - OrderTakeProfit () ) / avd.QuotePoint )   == 0                      //<1524>
              && MathRound ( ( ald.Stop - OrderStopLoss   () ) / avd.QuotePoint )   == 0 )                    //<1525>
               {                                                                                              //<1526>
                 ali.Validation       = 0                                                                   ; //<1527>
                 Alert ( avs.SystemStamp ,                                                                    //<1528>
                         ": Modify command is ignored due to unchanged price levels"                      ) ; //<1529>
               }                                                                                              //<1530>
          //</17.4.4.3. Empty Levels Validation 15 >                                                          //<1531>
                                                                                                              //<1532>
          //< 17.4.4.4. Buy Order Take Price Validation 18 >                                                  //<1533>
          if   ( ali.Validation      == 1                                          )                          //<1534>
          if   ( ali.OrderType       == aci.Buy                                    )                          //<1535>
          if   ( ald.DistanceModify   - ald.DistanceTake > 0 && ali.EmptyTake == 0 )                          //<1536>
               { ali.Validation       = 0                                                                   ; //<1537>
                 Alert ( avs.SystemStamp ,                                                                    //<1538>
                         ": Modify command is disabled due to invalid Take distance = "                   ,   //<1539>
                         DoubleToStr  ( ald.DistanceTake                                   , avi.Digits ) ,   //<1540>
                         " / "                                                                            ,   //<1541>
                         DoubleToStr  ( ald.DistanceModify                                 , avi.Digits ) ) ; //<1542>
                 Alert ( avs.SystemStamp ,                                                                    //<1543>
                         ": Modify command current Take price = "                                         ,   //<1544>
                         DoubleToStr  ( ald.Take                                           , avi.Digits ) ,   //<1545>
                         " / Bid price = "                                                                ,   //<1546>
                         DoubleToStr  ( ald.Price                                          , avi.Digits ) ) ; //<1547>
                 Alert ( avs.SystemStamp ,                                                                    //<1548>
                         ": Buy order minimal Take price = "                                              ,   //<1549>
                         DoubleToStr  ( ald.Price + ald.DistanceModify                     , avi.Digits ) ) ; //<1550>
               }                                                                                              //<1551>
          //< 17.4.4.4. Buy Order Take Price Validation 18 >                                                  //<1552>
                                                                                                              //<1553>
          //< 17.4.4.5. Buy Order Stop Price Validation 25 >                                                  //<1554>
          if   ( ali.Validation      == 1                                          )                          //<1555>
          if   ( ali.OrderType       == aci.Buy                                    )                          //<1556>
          if   ( ald.DistanceModify   - ald.DistanceStop > 0 && ali.EmptyStop == 0 )                          //<1557>
               { ali.Validation       = 0                                                                   ; //<1558>
                 Alert ( avs.SystemStamp ,                                                                    //<1559>
                         ": Modify command is disabled due to invalid Stop distance = "                   ,   //<1560>
                         DoubleToStr  ( ald.DistanceStop                                   , avi.Digits ) ,   //<1561>
                         " / "                                                                            ,   //<1562>
                         DoubleToStr  ( ald.DistanceModify                                 , avi.Digits ) ) ; //<1563>
                 Alert ( avs.SystemStamp ,                                                                    //<1564>
                         ": Modify command current Stop price = "                                         ,   //<1565>
                         DoubleToStr  ( ald.Stop                                           , avi.Digits ) ,   //<1566>
                         " / Bid price = "                                                                ,   //<1567>
                         DoubleToStr  ( ald.Price                                          , avi.Digits ) ) ; //<1568>
                 Alert ( avs.SystemStamp ,                                                                    //<1569>
                         ": Buy order maximal Stop price = "                                              ,   //<1570>
                         DoubleToStr  ( ald.Price - ald.DistanceModify                     , avi.Digits ) ) ; //<1571>
               }                                                                                              //<1572>
                                                                                                              //<1573>
          if   ( ali.Validation      == 1                                          )                          //<1574>
          if   ( ali.OrderType       == aci.Buy                                    )                          //<1575>
          if   ( ald.Stop             - OrderStopLoss () < 0 && ali.EmptyStop == 0 )                          //<1576>
               { ali.Validation       = 0                                                                   ; //<1577>
                 Alert ( avs.SystemStamp ,                                                                    //<1578>
                         ": Modify command is disabled due to Buy order Stop lowering"                    ) ; //<1579>
               }                                                                                              //<1580>
          //</17.4.4.5. Buy Order Stop Price Validation 25 >                                                  //<1581>
                                                                                                              //<1582>
          //< 17.4.4.6. Sell Order Take Price Validation 18 >                                                 //<1583>
          if   ( ali.Validation      == 1                                          )                          //<1584>
          if   ( ali.OrderType       == aci.Sell                                   )                          //<1585>
          if   ( ald.DistanceModify   - ald.DistanceTake > 0 && ali.EmptyTake == 0 )                          //<1586>
               { ali.Validation       = 0                                                                   ; //<1587>
                 Alert ( avs.SystemStamp ,                                                                    //<1588>
                         ": Modify command is disabled due to invalid Take distance = "                   ,   //<1589>
                         DoubleToStr  ( ald.DistanceTake                                   , avi.Digits ) ,   //<1590>
                         " / "                                                                            ,   //<1591>
                         DoubleToStr  ( ald.DistanceModify                                 , avi.Digits ) ) ; //<1592>
                 Alert ( avs.SystemStamp ,                                                                    //<1593>
                         ": Modify command current Take price = "                                         ,   //<1594>
                         DoubleToStr  ( ald.Take                                           , avi.Digits ) ,   //<1595>
                         " / Ask price = "                                                                ,   //<1596>
                         DoubleToStr  ( ald.Price                                          , avi.Digits ) ) ; //<1597>
                 Alert ( avs.SystemStamp ,                                                                    //<1598>
                         ": Sell order maximal Take price = "                                             ,   //<1599>
                         DoubleToStr  ( ald.Price - ald.DistanceModify                     , avi.Digits ) ) ; //<1600>
               }                                                                                              //<1601>
          //< 17.4.4.6. Sell Order Take Price Validation 18 >                                                 //<1602>
                                                                                                              //<1603>
          //< 17.4.4.7. Sell Order Stop Price Validation 25 >                                                 //<1604>
          if   ( ali.Validation      == 1                                          )                          //<1605>
          if   ( ali.OrderType       == aci.Sell                                   )                          //<1606>
          if   ( ald.DistanceModify   - ald.DistanceStop > 0 && ali.EmptyStop == 0 )                          //<1607>
               { ali.Validation       = 0                                                                   ; //<1608>
                 Alert ( avs.SystemStamp ,                                                                    //<1609>
                         ": Modify command is disabled due to invalid Stop distance = "                   ,   //<1610>
                         DoubleToStr  ( ald.DistanceStop                                   , avi.Digits ) ,   //<1611>
                         " / "                                                                            ,   //<1612>
                         DoubleToStr  ( ald.DistanceModify                                 , avi.Digits ) ) ; //<1613>
                 Alert ( avs.SystemStamp ,                                                                    //<1614>
                         ": Modify command current Stop price = "                                         ,   //<1615>
                         DoubleToStr  ( ald.Stop                                           , avi.Digits ) ,   //<1616>
                         " / Ask price = "                                                                ,   //<1617>
                         DoubleToStr  ( ald.Price                                          , avi.Digits ) ) ; //<1618>
                 Alert ( avs.SystemStamp ,                                                                    //<1619>
                         ": Sell order minimal Stop price = "                                             ,   //<1620>
                         DoubleToStr  ( ald.Price + ald.DistanceModify                     , avi.Digits ) ) ; //<1621>
               }                                                                                              //<1622>
                                                                                                              //<1623>
          if   ( ali.Validation      == 1                                          )                          //<1624>
          if   ( ali.OrderType       == aci.Sell                                   )                          //<1625>
          if   ( ald.Stop             - OrderStopLoss () > 0 && ali.EmptyStop == 0 )                          //<1626>
               { ali.Validation       = 0                                                                   ; //<1627>
                 Alert ( avs.SystemStamp ,                                                                    //<1628>
                         ": Modify command is disabled due to Sell order Stop heightening"                ) ; //<1629>
               }                                                                                              //<1630>
          //</17.4.4.7. Sell Order Stop Price Validation 25 >                                                 //<1631>
                                                                                                              //<1632>
          //< 17.4.4.8. Modify Command Validation Exit Point 3 >                                              //<1633>
          if           ( ali.Validation == 0 )                                                                //<1634>
                         ali.Command     = EMPTY                                                            ; //<1635>
        }                                                                                                     //<1636>
          //</17.4.4.8. Modify Command Validation Exit Point 3 >                                              //<1637>
//</17.4.4. Modify Command Validation 137 > ` `  ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` `//<1638>
                                                                                                              //<1639>
//< 17.4.5. Close Command Validation 11 > ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` //<1640>
else if ( ali.Command == aci.Close  )                                                                         //<1641>
        {                                                                                                     //<1642>
          Alert        ( avs.SystemStamp , ": Close command is detected"                                  ) ; //<1643>
                                                                                                              //<1644>
          if   ( ali.OrdersTotal      > 0 )                                                                   //<1645>
                 ali.Validation       = 1                                                                   ; //<1646>
          else { ali.Validation       = 0                                                                   ; //<1647>
                 Alert ( avs.SystemStamp , ": Close command is ignored due to empty order list"           ) ; //<1648>
               }                                                                                              //<1649>
                                                                                                              //<1650>
          if   ( ali.Validation      == 0 )                                                                   //<1651>
                 ali.Command          = EMPTY                                                               ; //<1652>
        }                                                                                                     //<1653>
//</17.4.5. Close Command Validation 11 > ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` //<1654>
                                                                                                              //<1655>
//< 17.4.6. Empty Command Detection 3 > ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` //<1656>
else if ( ali.Command == EMPTY      )                                                                         //<1657>
        {                                                                                                     //<1658>
        }                                                                                                     //<1659>
//</17.4.6. Empty Command Detection 3 > ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` //<1660>
                                                                                                              //<1661>
//< 17.4.7. Invalid Command Detection 1 > ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` //<1662>
else      Alert        ( avs.SystemStamp , ": Invalid Command "     , ali.Command                         ) ; //<1663>
//</17.4.7. Invalid Command Detection 1 > ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` //<1664>
                                                                                                              //<1665>
//</17.4. Control Interface Validation 265 >``````````````````````````````````````````````````````````````````//<1666>
                                                                                                              //<1667>
//< 17.5. Attempt To Modify 34 >``````````````````````````````````````````````````````````````````````````````//<1668>
if   ( ali.Command == aci.Modify )                                                                            //<1669>
     { ali.Command  = EMPTY                                                                                 ; //<1670>
                                                                                                              //<1671>
               //< Trading Function Execution Sequence >                                                      //<1672>
               //< Step 1 >                                                                                   //<1673>
                   avs.LocalStamp     = avs.SystemStamp                          + ": Attempt to Modify " +   //<1674>
                                        aes.Symbol                                                 + " "  +   //<1675>
                                        acs.Operation    [   OrderType ()  ]                       + " #" +   //<1676>
                                        OrderMagicNumber ()                                        + "/"  +   //<1677>
                                        OrderTicket      ()                                                 ; //<1678>
               //< Step 2 >                                                                                   //<1679>
                   if ( OrderType () == aci.Buy           )  string          als.Price       = " Bid = "    ; //<1680>
                   else                                                      als.Price       = " Ask = "    ; //<1681>
                                                                                                              //<1682>
                   Alert              ( avs.LocalStamp                                       , als.Price    , //<1683>
                                        DoubleToStr     (    ald.Price     , avi.Digits )    , " / Take = " , //<1684>
                                        DoubleToStr     (    ald.Take      , avi.Digits )    , " / Stop = " , //<1685>
                                        DoubleToStr     (    ald.Stop      , avi.Digits )                 ) ; //<1686>
               //< Step 3 >                                                                                   //<1687>
                   OrderModify        ( OrderTicket     () ,                                                  //<1688>
                                        OrderOpenPrice  () ,                                                  //<1689>
                                        ald.Stop           ,                                                  //<1690>
                                        ald.Take           , 0 , 0                                        ) ; //<1691>
               //< Step 4 >                                                                                   //<1692>
                   avi.TimeStamp      = TimeLocal       ()                                                  ; //<1693>
                                                                                                              //<1694>
               //< Step 5 >                                                                                   //<1695>
                   avi.Exception      = GetLastError    ()                                                  ; //<1696>
                                                                                                              //<1697>
               //< Step 6 >                                                                                   //<1698>
                   if ( avi.Exception == 0 ) avs.LocalMessage =            " Success "                      ; //<1699>
                   else                      avs.LocalMessage =            " Failure "      + avi.Exception ; //<1700>
                                                                                                              //<1701>
               //< Step 7 >                                                                                   //<1702>
                   static int ali.ModifyAttempts , ali.ModifySuccesses     , ali.ModifyExceptions           ; //<1703>
                   ali.ModifyAttempts                            ++                                         ; //<1704>
                   if ( avi.Exception == 0 ) ali.ModifySuccesses ++                                         ; //<1705>
                   else ali.ModifyExceptions                     ++                                         ; //<1706>
                                                                                                              //<1707>
               //< Step 8 >                                                                                   //<1708>
                   Alert              ( avs.LocalStamp                     , avs.LocalMessage       , " " ,   //<1709>
                                        ali.ModifyAttempts                                          , "/" ,   //<1710>
                                        ali.ModifySuccesses                                         , "/" ,   //<1711>
                                        ali.ModifyExceptions                                              ) ; //<1712>
                                                                                                              //<1713>
               //< Step 9 >                                                                                   //<1714>
                   avs.SystemMessage  = "Modification"                     + avs.LocalMessage       + " " +   //<1715>
                                        ali.ModifyAttempts                                          + "/" +   //<1716>
                                        ali.ModifySuccesses                                         + "/" +   //<1717>
                                        ali.ModifyExceptions                                                ; //<1718>
                                                                                                              //<1719>
          //</Trading Function Execution Sequence >                                                           //<1720>
     }                                                                                                        //<1721>
//</17.5. Attempt To Modify 34 >``````````````````````````````````````````````````````````````````````````````//<1722>
                                                                                                              //<1723>
//< 17.6. Attempt To Close 27 >```````````````````````````````````````````````````````````````````````````````//<1724>
if   ( ali.Command == aci.Close  )                                                                            //<1725>
     { ali.Command  = EMPTY                                                                                 ; //<1726>
                                                                                                              //<1727>
               //< Trading Function Execution Sequence >                                                      //<1728>
               //< Step 1 >                                                                                   //<1729>
                   avs.LocalStamp     = avs.SystemStamp                           + ": Attempt to Close " +   //<1730>
                                        aes.Symbol                                                 + " "  +   //<1731>
                                        acs.Operation    [   OrderType ()  ]                       + " #" +   //<1732>
                                        OrderMagicNumber ()                                        + "/"  +   //<1733>
                                        OrderTicket      ()                                                 ; //<1734>
               //< Step 2 >                                                                                   //<1735>
                   Alert              ( avs.LocalStamp                                           ,   " at " , //<1736>
                                        DoubleToStr     (    OrderClosePrice ()   , avi.Digits )          ) ; //<1737>
               //< Step 3 >                                                                                   //<1738>
                   OrderClose         ( OrderTicket     () , OrderLots () ,   OrderClosePrice () , 0 , 0  ) ; //<1739>
                                                                                                              //<1740>
               //< Step 4 >                                                                                   //<1741>
                   avi.TimeStamp      = TimeLocal       ()                                                  ; //<1742>
                                                                                                              //<1743>
               //< Step 5 >                                                                                   //<1744>
                   avi.Exception      = GetLastError    ()                                                  ; //<1745>
                                                                                                              //<1746>
               //< Step 6 >                                                                                   //<1747>
                   if ( avi.Exception == 0 ) avs.LocalMessage =            " Success "                      ; //<1748>
                   else                      avs.LocalMessage =            " Failure "      + avi.Exception ; //<1749>
                                                                                                              //<1750>
               //< Step 7 >                                                                                   //<1751>
                   static int ali.CloseAttempts , ali.CloseSuccesses       , ali.CloseExceptions            ; //<1752>
                   ali.CloseAttempts                             ++                                         ; //<1753>
                   if ( avi.Exception == 0 ) ali.CloseSuccesses  ++                                         ; //<1754>
                   else ali.CloseExceptions                      ++                                         ; //<1755>
                                                                                                              //<1756>
               //< Step 8 >                                                                                   //<1757>
                   Alert              ( avs.LocalStamp                     , avs.LocalMessage       , " " ,   //<1758>
                                        ali.CloseAttempts                                           , "/" ,   //<1759>
                                        ali.CloseSuccesses                                          , "/" ,   //<1760>
                                        ali.CloseExceptions                                               ) ; //<1761>
               //< Step 9 >                                                                                   //<1762>
                   avs.SystemMessage  = "Close command"                    + avs.LocalMessage       + " " +   //<1763>
                                        ali.CloseAttempts                                           + "/" +   //<1764>
                                        ali.CloseSuccesses                                          + "/" +   //<1765>
                                        ali.CloseExceptions                                                 ; //<1766>
          //</Trading Function Execution Sequence >                                                           //<1767>
     }                                                                                                        //<1768>
//</17.6. Attempt To Close 27 >```````````````````````````````````````````````````````````````````````````````//<1769>
                                                                                                              //<1770>
//< 17.7. Automatic Risk Management 39 >``````````````````````````````````````````````````````````````````````//<1771>
                                                                                                              //<1772>
       //< 17.7.1. Automatic Risk Management Entry Point 7 >                                                  //<1773>
if   ( ali.Command == aci.Buy                                                                                 //<1774>
    || ali.Command == aci.Sell )                                                                              //<1775>
     {                                                                                                        //<1776>
       avi.Command = ali.Command                                                                            ; //<1777>
       avd.Price   = ald.Price                                                                              ; //<1778>
       avd.Stop    = ald.Stop                                                                               ; //<1779>
       avd.Take    = ald.Take                                                                               ; //<1780>
       //</17.7.1. Automatic Risk Management Entry Point 7 >                                                  //<1781>
                                                                                                              //<1782>
       //< 17.7.2. Operation Size Limit Computing 9 >                                                         //<1783>
       avd.QuoteTarget                = MathAbs            ( avd.Price           - avd.Take               ) ; //<1784>
       avd.QuoteRisk                  = MathAbs            ( avd.Price           - avd.Stop               ) ; //<1785>
       avd.NominalPoint               = avd.NominalTick    * avd.QuotePoint      / avd.QuoteTick            ; //<1786>
       avi.MarginPoints               = MathRound          ( avd.NominalMargin   / avd.NominalPoint       ) ; //<1787>
       avi.RiskPoints                 = MathRound          ( avd.QuoteRisk       / avd.QuotePoint         ) ; //<1788>
       avd.VARLimit                   = AccountEquity ()   * aed.OrderReserve                               ; //<1789>
       avd.RiskPoint                  = avd.VARLimit       / avi.RiskPoints                                 ; //<1790>
       avd.MarginLimit                = avd.RiskPoint      * avi.MarginPoints                               ; //<1791>
       avd.SizeLimit                  = avd.MarginLimit    / avd.NominalMargin                              ; //<1792>
       //</17.7.2. Operation Size Limit Computing 9 >                                                         //<1793>
                                                                                                              //<1794>
       //< 17.7.3. Operation Size Control 22 >                                                                //<1795>
       if   ( avd.SizeLimit           - avd.MinimumLots >= 0 )                                                //<1796>
            { int    ali.Steps        = MathFloor        ( ( avd.SizeLimit       - avd.MinimumLots        )   //<1797>
                                                           / avd.LotStep                                  ) ; //<1798>
              double ald.Size         = avd.MinimumLots    + avd.LotStep         * ali.Steps              ; } //<1799>
       else   ald.Size                = 0                                                                   ; //<1800>
                                                                                                              //<1801>
       if   ( ald.Size                - avd.MaximumLots >  0 )                                                //<1802>
              ald.Size                = avd.MaximumLots                                                     ; //<1803>
                                                                                                              //<1804>
       if   ( ald.Size                - avd.MinimumLots >= 0 )                                                //<1805>
              double ald.MarginCheck  = AccountFreeMarginCheck ( aes.Symbol      , avi.Command , ald.Size ) ; //<1806>
       else {                                                                                                 //<1807>
              avi.Command             = EMPTY                                                               ; //<1808>
              Alert ( avs.SystemStamp , ": " , acs.Operation [ ali.Command ]                              ,   //<1809>
                      " command is disabled due to low order size limit = "                               ,   //<1810>
                      DoubleToStr     ( avd.SizeLimit , 2 ) , " / " , DoubleToStr ( avd.MinimumLots , 2 ) ) ; //<1811>
            }                                                                                                 //<1812>
                                                                                                              //<1813>
       if   ( ald.MarginCheck        <= 0                                                                     //<1814>
           || GetLastError ()        == 134 )                                                                 //<1815>
            {                                                                                                 //<1816>
              avi.Command             = EMPTY                                                               ; //<1817>
              Alert ( avs.SystemStamp , ": " , acs.Operation [ ali.Command ]                              ,   //<1818>
                      " command is disabled due to insufficient margin"                                   ) ; //<1819>
            }                                                                                                 //<1820>
       //</17.7.3. Operation Size Control 22 >                                                                //<1821>
                                                                                                              //<1822>
       //< 17.7.4. Automatic Risk Management Exit Point 1 >                                                   //<1823>
     } //  if 17.7.1                                                                                          //<1824>
       //</17.7.4. Automatic Risk Management Exit Point 1 >                                                   //<1825>
                                                                                                              //<1826>
//< 17.7. Automatic Risk Management 39 >``````````````````````````````````````````````````````````````````````//<1827>
                                                                                                              //<1828>
//< 17.8. Control Interface Reset On Exit 1 >`````````````````````````````````````````````````````````````````//<1829>
GlobalVariableSet                     ( avs.SetupCommand          , EMPTY                                 ) ; //<1830>
//</17.8. Control Interface Reset On Exit 1 >`````````````````````````````````````````````````````````````````//<1831>
}                                                                                                             //<1832>
//</A.System.Extra: Function 17 >-----------------------------------------------------------------------------//<1833>
                                                                                                              //<1834>
//</8. Extra Code >===========================================================================================//<1835>