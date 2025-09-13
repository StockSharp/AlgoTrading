//< 1. Property 7 >===========================================================================================//<   1>
                                                                                                              //<   2>
#property     copyright                 "Copyright (C) 2009, MetaQuotes Software Corp."                       //<   3>
#property     link                      "http://www.metaquotes.net"                                           //<   4>
                                                                                                              //<   5>
#define       A.System.Series           "AIS"                                                                 //<   6>
#define       A.System.Modification     "30001"                                                               //<   7>
#define       A.System.ReleaseDate      "2009.03.26"                                                          //<   8>
#define       A.System.Program          "Trading Robot Template"                                              //<   9>
#define       A.System.Programmer       "Airat Safin                           http://www.mql4.com/users/Ais" //<  10>
                                                                                                              //<  11>
//</1. Property 7 >===========================================================================================//<  12>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////<  13>
//< 2. Constants 5 >==========================================================================================//<  14>
                                                                                                              //<  15>
#define       aci.OrderID               30001                                                                 //<  16>
                                                                                                              //<  17>
#define       acd.TrailStepping         1.0                                                                   //<  18>
                                                                                                              //<  19>
#define       aci.TradingPause          5                                                                     //<  20>
                                                                                                              //<  21>
string        acs.Operation     []  = { "Buy" , "Sell"                                                    } ; //<  22>
                                                                                                              //<  23>
#define       aci.SetupSeparator        1000000000                                                            //<  24>
                                                                                                              //<  25>
//</2. Constants 5 >==========================================================================================//<  26>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////<  27>
//< 3. Presets 8 >============================================================================================//<  28>
                                                                                                              //<  29>
//< 3.1. Risk Management Preset 2 >```````````````````````````````````````````````````````````````````````````//<  30>
                                                                                                              //<  31>
extern double aed.AccountReserve      = 0.20                                                                ; //<  32>
extern double aed.OrderReserve        = 0.04                                                                ; //<  33>
                                                                                                              //<  34>
//</3.1. Risk Management Preset 2 >```````````````````````````````````````````````````````````````````````````//<  35>
                                                                                                              //<  36>
//< 3.2. Trading Strategy Preset 6 >``````````````````````````````````````````````````````````````````````````//<  37>
                                                                                                              //<  38>
extern string aes.Symbol              = "EURUSD"                                                            ; //<  39>
                                                                                                              //<  40>
extern int    aei.Timeframe.1         = 15                                                                  ; //<  41>
extern int    aei.Timeframe.2         = 1                                                                   ; //<  42>
                                                                                                              //<  43>
extern double aed.Parameter.1         = 1.0                                                                 ; //<  44>
extern double aed.Parameter.2         = 2.0                                                                 ; //<  45>
extern double aed.Parameter.3         = 3.0                                                                 ; //<  46>
                                                                                                              //<  47>
//</3.2. Trading Strategy Preset 6 >``````````````````````````````````````````````````````````````````````````//<  48>
                                                                                                              //<  49>
//</3. Presets 8 >============================================================================================//<  50>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////<  51>
//< 4. Global Variables 69 >==================================================================================//<  52>
                                                                                                              //<  53>
//< 4.1. Trading Strategy Interface 4 >```````````````````````````````````````````````````````````````````````//<  54>
                                                                                                              //<  55>
int           avi.Command             = EMPTY                                                               ; //<  56>
double        avd.Price               = EMPTY                                                               ; //<  57>
double        avd.Stop                = EMPTY                                                               ; //<  58>
double        avd.Take                = EMPTY                                                               ; //<  59>
                                                                                                              //<  60>
//</4.1. Trading Strategy Interface 4 >```````````````````````````````````````````````````````````````````````//<  61>
                                                                                                              //<  62>
//< 4.2. System Controls 28 >`````````````````````````````````````````````````````````````````````````````````//<  63>
                                                                                                              //<  64>
int           avi.SystemFlag                                                                                ; //<  65>
int           avi.TradingFlag                                                                               ; //<  66>
int           avi.MonitorFlag                                                                               ; //<  67>
int           avi.LiveModeFlag                                                                              ; //<  68>
                                                                                                              //<  69>
int           avi.TimeStamp                                                                                 ; //<  70>
int           avi.Exception                                                                                 ; //<  71>
int           avi.ExcepionsTrade                                                                            ; //<  72>
int           avi.ExcepionsTrail                                                                            ; //<  73>
                                                                                                              //<  74>
int           avi.TimeStart                                                                                 ; //<  75>
int           avi.TimeLastRun                                                                               ; //<  76>
int           avi.Runs                                                                                      ; //<  77>
int           avi.BuyTrades                                                                                 ; //<  78>
int           avi.SellTrades                                                                                ; //<  79>
int           avi.TotalTrades                                                                               ; //<  80>
int           avi.Trailes                                                                                   ; //<  81>
int           avi.AttemptsTrade                                                                             ; //<  82>
int           avi.AttemptsTrail                                                                             ; //<  83>
                                                                                                              //<  84>
double        avd.Capital                                                                                   ; //<  85>
double        avd.PeakTime                                                                                  ; //<  86>
double        avd.PeakEquity                                                                                ; //<  87>
double        avd.InitialEquity                                                                             ; //<  88>
double        avd.InitialCapital                                                                            ; //<  89>
double        avd.EquityReserve                                                                             ; //<  90>
                                                                                                              //<  91>
string        avs.Currency      []  = { "" , "" , "" , ""                                                 } ; //<  92>
#define       ari.Account               0                                                                     //<  93>
#define       ari.Base                  1                                                                     //<  94>
#define       ari.Quote                 2                                                                     //<  95>
#define       ari.Margin                3                                                                     //<  96>
                                                                                                              //<  97>
//</4.2. System Controls 28 >`````````````````````````````````````````````````````````````````````````````````//<  98>
                                                                                                              //<  99>
//< 4.3. Preset Control 10 >``````````````````````````````````````````````````````````````````````````````````//< 100>
                                                                                                              //< 101>
//< 4.3.1. Setup Separators 2 >                                                                               //< 102>
string        avs.SetupBegin                                                                                ; //< 103>
string        avs.SetupEnd                                                                                  ; //< 104>
//</4.3.1. Setup Separators 2 >                                                                               //< 105>
                                                                                                              //< 106>
//< 4.3.2. Risk Management Preset Setup 2 >                                                                   //< 107>
string        avs.SetupAccountReserve                                                                       ; //< 108>
string        avs.SetupOrderReserve                                                                         ; //< 109>
//</4.3.2. Risk Management Preset Setup 2 >                                                                   //< 110>
                                                                                                              //< 111>
//< 4.3.3. Trading Preset Setup 6 >                                                                           //< 112>
string        avs.SetupTrading                                                                              ; //< 113>
string        avs.SetupTimeframe.1                                                                          ; //< 114>
string        avs.SetupTimeframe.2                                                                          ; //< 115>
string        avs.SetupParameter.1                                                                          ; //< 116>
string        avs.SetupParameter.2                                                                          ; //< 117>
string        avs.SetupParameter.3                                                                          ; //< 118>
//</4.3.3. Trading Preset Setup 6 >                                                                           //< 119>
                                                                                                              //< 120>
//</4.3. Preset Control 10 >``````````````````````````````````````````````````````````````````````````````````//< 121>
                                                                                                              //< 122>
//< 4.5. System Messages 4 >``````````````````````````````````````````````````````````````````````````````````//< 123>
                                                                                                              //< 124>
string        avs.SystemMessage                                                                             ; //< 125>
string        avs.LocalMessage                                                                              ; //< 126>
string        avs.SystemStamp                                                                               ; //< 127>
string        avs.LocalStamp                                                                                ; //< 128>
                                                                                                              //< 129>
//</4.4. System Messages 4 >``````````````````````````````````````````````````````````````````````````````````//< 130>
                                                                                                              //< 131>
//< 4.5. Common Data 14 >`````````````````````````````````````````````````````````````````````````````````````//< 132>
                                                                                                              //< 133>
double        avd.QuoteAsk                                                                                  ; //< 134>
double        avd.QuoteBid                                                                                  ; //< 135>
double        avd.QuotePoint                                                                                ; //< 136>
double        avd.QuoteSpread                                                                               ; //< 137>
double        avd.QuoteFreeze                                                                               ; //< 138>
double        avd.QuoteStops                                                                                ; //< 139>
double        avd.QuoteTick                                                                                 ; //< 140>
double        avd.NominalTick                                                                               ; //< 141>
double        avd.NominalMargin                                                                             ; //< 142>
double        avd.NominalLot                                                                                ; //< 143>
double        avd.MaximumLots                                                                               ; //< 144>
double        avd.MinimumLots                                                                               ; //< 145>
double        avd.LotStep                                                                                   ; //< 146>
int           avi.Digits                                                                                    ; //< 147>
                                                                                                              //< 148>
//</4.5. Common Data 14 >`````````````````````````````````````````````````````````````````````````````````````//< 149>
                                                                                                              //< 150>
//< 4.6. Risk Management Data 9 >`````````````````````````````````````````````````````````````````````````````//< 151>
                                                                                                              //< 152>
double        avd.QuoteTarget                                                                               ; //< 153>
double        avd.QuoteRisk                                                                                 ; //< 154>
double        avd.NominalPoint                                                                              ; //< 155>
int           avi.MarginPoints                                                                              ; //< 156>
int           avi.RiskPoints                                                                                ; //< 157>
double        avd.VARLimit                                                                                  ; //< 158>
double        avd.RiskPoint                                                                                 ; //< 159>
double        avd.MarginLimit                                                                               ; //< 160>
double        avd.SizeLimit                                                                                 ; //< 161>
                                                                                                              //< 162>
//</4.6. Risk Management Data 9 >`````````````````````````````````````````````````````````````````````````````//< 163>
                                                                                                              //< 164>
//< 4.7. Trading Strategy Data 0 >````````````````````````````````````````````````````````````````````````````//< 165>
//</4.7. Trading Strategy Data 0 >````````````````````````````````````````````````````````````````````````````//< 166>
                                                                                                              //< 167>
//</4. Global Variables 69 >==================================================================================//< 168>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////< 169>
//< 5. Program Initialization 21 >============================================================================//< 170>
                                                                                                              //< 171>
int    init                          ()                                                                       //< 172>
{                                                                                                             //< 173>
                                                                                                              //< 174>
//< 5.1. System Controls Reset 8 >````````````````````````````````````````````````````````````````````````````//< 175>
                                                                                                              //< 176>
avi.TimeStart            = TimeLocal     ()                                                                 ; //< 177>
avi.TimeStamp            = TimeLocal     ()                                                                 ; //< 178>
                                                                                                              //< 179>
avd.PeakTime             = TimeLocal     ()                                                                 ; //< 180>
avd.PeakEquity           = AccountEquity ()                                                                 ; //< 181>
avd.InitialEquity        = AccountEquity ()                                                                 ; //< 182>
avd.InitialCapital       = avd.PeakEquity   * ( 1 - aed.AccountReserve )                                    ; //< 183>
                                                                                                              //< 184>
avi.TradingFlag          = 1                                                                                ; //< 185>
avi.MonitorFlag          = 1                                                                                ; //< 186>
                                                                                                              //< 187>
//</5.1. System Controls Reset 8 >````````````````````````````````````````````````````````````````````````````//< 188>
                                                                                                              //< 189>
//< 5.2. System Stamp Reset 3 >```````````````````````````````````````````````````````````````````````````````//< 190>
                                                                                                              //< 191>
avs.SystemStamp          = A.System.Series                                 +                                  //< 192>
                           A.System.Modification                           + " "  +                           //< 193>
                           A.System.Program                                                                 ; //< 194>
                                                                                                              //< 195>
//</5.2. System Stamp Reset 3 >```````````````````````````````````````````````````````````````````````````````//< 196>
                                                                                                              //< 197>
//< 5.3. First Alert 8 >``````````````````````````````````````````````````````````````````````````````````````//< 198>
                                                                                                              //< 199>
Alert                    ( avs.SystemStamp                                 , ": Symbol="                  ,   //< 200>
                           aes.Symbol                                      , ", Preset="                  ,   //< 201>
                           aei.Timeframe.1                                 , "/"                          ,   //< 202>
                           aei.Timeframe.2                                 , "/"                          ,   //< 203>
                           DoubleToStr             ( aed.Parameter.1 , 1 ) , "/"                          ,   //< 204>
                           DoubleToStr             ( aed.Parameter.2 , 1 ) , "/"                          ,   //< 205>
                           DoubleToStr             ( aed.Parameter.3 , 1 ) , " "                              //< 206>
                                                   , ", Reload code="      ,        UninitializeReason () ) ; //< 207>
                                                                                                              //< 208>
//</5.3. First Alert 8 >``````````````````````````````````````````````````````````````````````````````````````//< 209>
                                                                                                              //< 210>
//< 5.4. Setup Reset 1 >``````````````````````````````````````````````````````````````````````````````````````//< 211>
                                                                                                              //< 212>
afr.CreateSetup                      ()                                                                     ; //< 213>
                                                                                                              //< 214>
//</5.4. Setup Reset 1 >``````````````````````````````````````````````````````````````````````````````````````//< 215>
                                                                                                              //< 216>
//< 5.5. Monitoring Panel Reset 1 >```````````````````````````````````````````````````````````````````````````//< 217>
                                                                                                              //< 218>
afr.CreatePanel.1                    ()                                                                     ; //< 219>
                                                                                                              //< 220>
//</5.5. Monitoring Panel Reset 1 >```````````````````````````````````````````````````````````````````````````//< 221>
                                                                                                              //< 222>
}                                                                                                             //< 223>
//</5. Program Initialization 21 >============================================================================//< 224>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////< 225>
//< 6. Program Deinitialization 5 >===========================================================================//< 226>
                                                                                                              //< 227>
int    deinit                        ()                                                                       //< 228>
{                                                                                                             //< 229>
                                                                                                              //< 230>
//< 6.1. Setup Deletion 1 >```````````````````````````````````````````````````````````````````````````````````//< 231>
                                                                                                              //< 232>
afr.DeleteSetup                      ()                                                                     ; //< 233>
                                                                                                              //< 234>
//</6.1. Setup Deletion 1 >```````````````````````````````````````````````````````````````````````````````````//< 235>
                                                                                                              //< 236>
//< 6.2. Monitoring Panel Deletion 1 >````````````````````````````````````````````````````````````````````````//< 237>
                                                                                                              //< 238>
afr.DeletePanel.1                    ()                                                                     ; //< 239>
                                                                                                              //< 240>
//</6.2. Monitoring Panel Deletion 1 >````````````````````````````````````````````````````````````````````````//< 241>
                                                                                                              //< 242>
//< 6.3. Final Alert 3 >``````````````````````````````````````````````````````````````````````````````````````//< 243>
                                                                                                              //< 244>
Alert                    ( avs.SystemStamp         , ": Stop code="        ,        UninitializeReason ()     //< 245>
                                                                           , "/"  , avi.ExcepionsTrade        //< 246>
                                                                           , "/"  , avi.ExcepionsTrail    ) ; //< 247>
                                                                                                              //< 248>
//</6.3. Final Alert 3 >``````````````````````````````````````````````````````````````````````````````````````//< 249>
                                                                                                              //< 250>
}                                                                                                             //< 251>
//</6. Program Deinitialization 5 >===========================================================================//< 252>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////< 253>
//< 7. Main Program 205 >=====================================================================================//< 254>
                                                                                                              //< 255>
int    start                         ()                                                                       //< 256>
{                                                                                                             //< 257>
                                                                                                              //< 258>
//< 7.1. System Controls Reset On Enter 75 >``````````````````````````````````````````````````````````````````//< 259>
                                                                                                              //< 260>
//< 7.1.1. Run Mode Detection 1 >                                                                             //< 261>
avi.LiveModeFlag           = ! IsTesting () || IsVisualMode ()                                              ; //< 262>
//</7.1.1. Run Mode Detection 1 >                                                                             //< 263>
                                                                                                              //< 264>
//< 7.1.2. Live Mode Subroutine 23 >                                                                          //< 265>
if (   avi.LiveModeFlag   == 1        )                                                                       //< 266>
   {                                                                                                          //< 267>
       if ( ! GlobalVariableCheck     ( avs.SetupBegin          ) )                                           //< 268>
              GlobalVariableSet       ( avs.SetupBegin            , aci.SetupSeparator                    ) ; //< 269>
       if ( ! GlobalVariableCheck     ( avs.SetupAccountReserve ) )                                           //< 270>
              GlobalVariableSet       ( avs.SetupAccountReserve   , aed.AccountReserve                    ) ; //< 271>
       if ( ! GlobalVariableCheck     ( avs.SetupOrderReserve   ) )                                           //< 272>
              GlobalVariableSet       ( avs.SetupOrderReserve     , aed.OrderReserve                      ) ; //< 273>
       if ( ! GlobalVariableCheck     ( avs.SetupTrading        ) )                                           //< 274>
              GlobalVariableSet       ( avs.SetupTrading          , avi.TradingFlag                       ) ; //< 275>
       if ( ! GlobalVariableCheck     ( avs.SetupTimeframe.1    ) )                                           //< 276>
              GlobalVariableSet       ( avs.SetupTimeframe.1      , aei.Timeframe.1                       ) ; //< 277>
       if ( ! GlobalVariableCheck     ( avs.SetupTimeframe.2    ) )                                           //< 278>
              GlobalVariableSet       ( avs.SetupTimeframe.2      , aei.Timeframe.2                       ) ; //< 279>
       if ( ! GlobalVariableCheck     ( avs.SetupParameter.1    ) )                                           //< 280>
              GlobalVariableSet       ( avs.SetupParameter.1      , aed.Parameter.1                       ) ; //< 281>
       if ( ! GlobalVariableCheck     ( avs.SetupParameter.2    ) )                                           //< 282>
              GlobalVariableSet       ( avs.SetupParameter.2      , aed.Parameter.2                       ) ; //< 283>
       if ( ! GlobalVariableCheck     ( avs.SetupParameter.3    ) )                                           //< 284>
              GlobalVariableSet       ( avs.SetupParameter.3      , aed.Parameter.3                       ) ; //< 285>
       if ( ! GlobalVariableCheck     ( avs.SetupEnd            ) )                                           //< 286>
              GlobalVariableSet       ( avs.SetupEnd              , aci.SetupSeparator                    ) ; //< 287>
   }                                                                                                          //< 288>
//</7.1.2. Live Mode Subroutine 23 >                                                                          //< 289>
                                                                                                              //< 290>
//< 7.1.3. All Modes Subroutine 8 >                                                                           //< 291>
if (   GlobalVariableGet      ( avs.SetupTrading )  == 1 )                                                    //< 292>
   {   avi.SystemFlag         = 1                                                                         ;   //< 293>
       avi.TradingFlag        = 1                                                                         ;   //< 294>
       avs.SystemMessage      = "Trading is enabled"                                                      ; } //< 295>
else                                                                                                          //< 296>
   {   avi.SystemFlag         = 0                                                                         ;   //< 297>
       avi.TradingFlag        = 0                                                                         ;   //< 298>
       avs.SystemMessage      = "Trading is disabled"                                                     ; } //< 299>
//</7.1.3. All Modes Subroutine 8 >                                                                           //< 300>
                                                                                                              //< 301>
//< 7.1.4. Live Mode Subroutine 43 >                                                                          //< 302>
if (   avi.LiveModeFlag      == 1 )                                                                           //< 303>
   {                                                                                                          //< 304>
       aed.AccountReserve     = GlobalVariableGet    ( avs.SetupAccountReserve                            ) ; //< 305>
       aed.OrderReserve       = GlobalVariableGet    ( avs.SetupOrderReserve                              ) ; //< 306>
       aei.Timeframe.1        = GlobalVariableGet    ( avs.SetupTimeframe.1                               ) ; //< 307>
       aei.Timeframe.2        = GlobalVariableGet    ( avs.SetupTimeframe.2                               ) ; //< 308>
       aed.Parameter.1        = GlobalVariableGet    ( avs.SetupParameter.1                               ) ; //< 309>
       aed.Parameter.2        = GlobalVariableGet    ( avs.SetupParameter.2                               ) ; //< 310>
       aed.Parameter.3        = GlobalVariableGet    ( avs.SetupParameter.3                               ) ; //< 311>
                                                                                                              //< 312>
       if ( aei.Timeframe.1  != PERIOD_M1                                                                     //< 313>
         && aei.Timeframe.1  != PERIOD_M5                                                                     //< 314>
         && aei.Timeframe.1  != PERIOD_M15                                                                    //< 315>
         && aei.Timeframe.1  != PERIOD_M30                                                                    //< 316>
         && aei.Timeframe.1  != PERIOD_H1                                                                     //< 317>
         && aei.Timeframe.1  != PERIOD_H4                                                                     //< 318>
         && aei.Timeframe.1  != PERIOD_D1                                                                     //< 319>
         && aei.Timeframe.1  != PERIOD_W1                                                                     //< 320>
         && aei.Timeframe.1  != PERIOD_MN1 )                                                                  //< 321>
          { aei.Timeframe.1   = 0                                                                         ;   //< 322>
            avi.SystemFlag    = 0                                                                         ;   //< 323>
            avi.TradingFlag   = 0                                                                         ;   //< 324>
            GlobalVariableSet ( avs.SetupTimeframe.1 , aei.Timeframe.1                                  ) ;   //< 325>
            GlobalVariableSet ( avs.SetupTrading     , avi.TradingFlag                                  ) ;   //< 326>
            avs.SystemMessage = "Check Timeframe.1"                                                       ;   //< 327>
            Alert             ( avs.SystemStamp                              , ": Symbol="                ,   //< 328>
                                aes.Symbol                                   , " "  , avs.SystemMessage ) ; } //< 329>
                                                                                                              //< 330>
       if ( aei.Timeframe.2  != PERIOD_M1                                                                     //< 331>
         && aei.Timeframe.2  != PERIOD_M5                                                                     //< 332>
         && aei.Timeframe.2  != PERIOD_M15                                                                    //< 333>
         && aei.Timeframe.2  != PERIOD_M30                                                                    //< 334>
         && aei.Timeframe.2  != PERIOD_H1                                                                     //< 335>
         && aei.Timeframe.2  != PERIOD_H4                                                                     //< 336>
         && aei.Timeframe.2  != PERIOD_D1                                                                     //< 337>
         && aei.Timeframe.2  != PERIOD_W1                                                                     //< 338>
         && aei.Timeframe.2  != PERIOD_MN1 )                                                                  //< 339>
          { aei.Timeframe.2   = 0                                                                         ;   //< 340>
            avi.SystemFlag    = 0                                                                         ;   //< 341>
            avi.TradingFlag   = 0                                                                         ;   //< 342>
            GlobalVariableSet ( avs.SetupTimeframe.2 , aei.Timeframe.2                                  ) ;   //< 343>
            GlobalVariableSet ( avs.SetupTrading     , avi.TradingFlag                                  ) ;   //< 344>
            avs.SystemMessage = "Check Timeframe.2"                                                       ;   //< 345>
            Alert              ( avs.SystemStamp                             , ": Symbol="                ,   //< 346>
                                 aes.Symbol                                  , " "  , avs.SystemMessage ) ; } //< 347>
   }                                                                                                          //< 348>
//</7.1.4. Live Mode Subroutine 43 >                                                                          //< 349>
                                                                                                              //< 350>
//</7.1. System Controls Reset On Enter 75 >``````````````````````````````````````````````````````````````````//< 351>
                                                                                                              //< 352>
//</7.2. Trading Pause Control 2 >````````````````````````````````````````````````````````````````````````````//< 353>
                                                                                                              //< 354>
if   ( TimeLocal     ()  - avi.TimeStamp           < aci.TradingPause )                                       //< 355>
     { avs.SystemMessage = "Trading pause "        + aci.TradingPause +   " seconds" ; avi.SystemFlag = 0 ; } //< 356>
                                                                                                              //< 357>
//</7.2. Trading Pause Control 2 >````````````````````````````````````````````````````````````````````````````//< 358>
                                                                                                              //< 359>
//< 7.3. Equity Control 6 >```````````````````````````````````````````````````````````````````````````````````//< 360>
                                                                                                              //< 361>
if   ( AccountEquity ()  - avd.PeakEquity > 0      )                                                          //< 362>
     { avd.PeakEquity    = AccountEquity ()                                                                 ; //< 363>
       avd.PeakTime      = TimeLocal     ()                                                               ; } //< 364>
                                                                                                              //< 365>
       avd.Capital       = avd.PeakEquity          * ( 1 - aed.AccountReserve )                             ; //< 366>
       avd.EquityReserve = AccountEquity ()        - avd.Capital                                            ; //< 367>
       avd.VARLimit      = AccountEquity ()        * aed.OrderReserve                                       ; //< 368>
                                                                                                              //< 369>
if   ( avd.EquityReserve - avd.VARLimit   < 0      )                                                          //< 370>
     { avs.SystemMessage = "System stop"                                             ; avi.SystemFlag = 0 ; } //< 371>
                                                                                                              //< 372>
//</7.3. Equity Control 6 >```````````````````````````````````````````````````````````````````````````````````//< 373>
                                                                                                              //< 374>
//< 7.4. Data Feed 14 >```````````````````````````````````````````````````````````````````````````````````````//< 375>
                                                                                                              //< 376>
//< 7.4.1. Common Data 14 >                                                                                   //< 377>
       avd.QuoteAsk      = MarketInfo ( aes.Symbol , MODE_ASK            )                                  ; //< 378>
       avd.QuoteBid      = MarketInfo ( aes.Symbol , MODE_BID            )                                  ; //< 379>
       avd.QuotePoint    = MarketInfo ( aes.Symbol , MODE_POINT          )                                  ; //< 380>
       avd.QuoteSpread   = MarketInfo ( aes.Symbol , MODE_SPREAD         ) * avd.QuotePoint                 ; //< 381>
       avd.QuoteFreeze   = MarketInfo ( aes.Symbol , MODE_FREEZELEVEL    ) * avd.QuotePoint                 ; //< 382>
       avd.QuoteStops    = MarketInfo ( aes.Symbol , MODE_STOPLEVEL      ) * avd.QuotePoint                 ; //< 383>
       avd.QuoteTick     = MarketInfo ( aes.Symbol , MODE_TICKSIZE       )                                  ; //< 384>
       avd.NominalTick   = MarketInfo ( aes.Symbol , MODE_TICKVALUE      )                                  ; //< 385>
       avd.NominalMargin = MarketInfo ( aes.Symbol , MODE_MARGINREQUIRED )                                  ; //< 386>
       avd.NominalLot    = MarketInfo ( aes.Symbol , MODE_LOTSIZE        )                                  ; //< 387>
       avd.MaximumLots   = MarketInfo ( aes.Symbol , MODE_MAXLOT         )                                  ; //< 388>
       avd.MinimumLots   = MarketInfo ( aes.Symbol , MODE_MINLOT         )                                  ; //< 389>
       avd.LotStep       = MarketInfo ( aes.Symbol , MODE_LOTSTEP        )                                  ; //< 390>
       avi.Digits        = MarketInfo ( aes.Symbol , MODE_DIGITS         )                                  ; //< 391>
//</7.4.1. Common Data 14 >                                                                                   //< 392>
                                                                                                              //< 393>
//< 7.4.2. Trading Strategy Data 0 >                                                                          //< 394>
//</7.4.2. Trading Strategy Data 0 >                                                                          //< 395>
                                                                                                              //< 396>
//</7.4. Data Feed 14 >```````````````````````````````````````````````````````````````````````````````````````//< 397>
                                                                                                              //< 398>
//< 7.5. Trading Strategy Interface Reset 4 >`````````````````````````````````````````````````````````````````//< 399>
                                                                                                              //< 400>
       avi.Command       = EMPTY                                                                            ; //< 401>
       avd.Price         = EMPTY                                                                            ; //< 402>
       avd.Stop          = EMPTY                                                                            ; //< 403>
       avd.Take          = EMPTY                                                                            ; //< 404>
                                                                                                              //< 405>
//</7.5. Trading Strategy Interface Reset 4 >`````````````````````````````````````````````````````````````````//< 406>
                                                                                                              //< 407>
//< 7.6. Position Management Module 38 >``````````````````````````````````````````````````````````````````````//< 408>
                                                                                                              //< 409>
//< 7.6.1. Position Management Module Entry Point 7 >                                                         //< 410>
if ( avi.SystemFlag  == 1             )                                                                       //< 411>
if ( OrdersTotal ()   > 0             )                                                                       //< 412>
                                                                                                              //< 413>
   { int   i , N ; N  = OrdersTotal  () - 1                                                                 ; //< 414>
     for ( i = N ; i >= 0         ; i --                                   )                                  //< 415>
       {      OrderSelect         ( i , SELECT_BY_POS , MODE_TRADES        )                                ; //< 416>
         if ( OrderMagicNumber    () != aci.OrderID                        )                       continue ; //< 417>
         else                                                                            avi.SystemFlag = 0 ; //< 418>
//</7.6.1. Position Management Module Entry Point 7 >                                                         //< 419>
                                                                                                              //< 420>
//< 7.6.2. Trailing Strategy Module Invocation 1 >                                                            //< 421>
         afr.TrailingStrategy ()                                                                            ; //< 422>
//</7.6.2. Trailing Strategy Module Invocation 1 >                                                            //< 423>
                                                                                                              //< 424>
//< 7.6.3. Order Modify Trading Function 28 >                                                                 //< 425>
         if ( avd.Stop                > 0 )                                                                   //< 426>
            { int  ali.TrailPoints    = MathRound       (    MathAbs       ( OrderStopLoss ()  - avd.Stop )   //< 427>
                                                                           / avd.QuotePoint               ) ; //< 428>
              if ( ali.TrailPoints   >= MarketInfo      (    aes.Symbol    , MODE_FREEZELEVEL           ) )   //< 429>
                 {                                                                                            //< 430>
               //< Trading Function Execution Sequence >                                                      //< 431>
               //< Step 1 >                                                                                   //< 432>
                   avs.LocalStamp     = avs.SystemStamp                           + ": Attempt to trail " +   //< 433>
                                        aes.Symbol                                                 + " "  +   //< 434>
                                        acs.Operation    [   OrderType ()  ]                       + " #" +   //< 435>
                                        OrderMagicNumber ()                                        + "/"  +   //< 436>
                                        OrderTicket      ()                                                 ; //< 437>
               //< Step 2 >                                                                                   //< 438>
                   Alert              ( avs.LocalStamp                                           ,     " +" , //< 439>
                                        ali.TrailPoints                                          , " from " , //< 440>
                                        DoubleToStr     (    OrderStopLoss ()     , avi.Digits ) ,   " to " , //< 441>
                                        DoubleToStr     (    avd.Stop             , avi.Digits )          ) ; //< 442>
               //< Step 3 >                                                                                   //< 443>
                   OrderModify        ( OrderTicket     () ,                                                  //< 444>
                                        OrderOpenPrice  () ,                                                  //< 445>
                                        avd.Stop           ,                                                  //< 446>
                                        OrderTakeProfit () , 0 , 0                                        ) ; //< 447>
               //< Step 4 >                                                                                   //< 448>
                   avi.TimeStamp      = TimeLocal       ()                                                  ; //< 449>
                                                                                                              //< 450>
               //< Step 5 >                                                                                   //< 451>
                   avi.Exception      = GetLastError    ()                                                  ; //< 452>
                                                                                                              //< 453>
               //< Step 6 >                                                                                   //< 454>
                   if ( avi.Exception == 0 ) avs.LocalMessage =            " Success "                      ; //< 455>
                   else                      avs.LocalMessage =            " Failure "      + avi.Exception ; //< 456>
                                                                                                              //< 457>
               //< Step 7 >                                                                                   //< 458>
                   avi.AttemptsTrail                          ++                                            ; //< 459>
                   if ( avi.Exception == 0 ) avi.Trailes      ++                                            ; //< 460>
                   else avi.ExcepionsTrail                    ++                                            ; //< 461>
                                                                                                              //< 462>
               //< Step 8 >                                                                                   //< 463>
                   Alert              ( avs.LocalStamp                     + avs.LocalMessage             ) ; //< 464>
                                                                                                              //< 465>
               //< Step 9 >                                                                                   //< 466>
                   avs.SystemMessage  = "Trailing Stop"                    + avs.LocalMessage               ; //< 467>
                                                                                                              //< 468>
          //</Trading Function Execution Sequence >                                                           //< 469>
                 } // if 7.6.3                                                                                //< 470>
            } // if 7.6.3                                                                                     //< 471>
//</7.6.3. Order Modify Trading Function 28 >                                                                 //< 472>
                                                                                                              //< 473>
//< 7.6.4. Position Management Module Exit Point 2 >                                                          //< 474>
       } // for 7.6.1                                                                                         //< 475>
   } // if 7.6.1                                                                                              //< 476>
//</7.6.4. Position Management Module Exit Point 2 >                                                          //< 477>
                                                                                                              //< 478>
//</7.6. Position Management Module 38 >``````````````````````````````````````````````````````````````````````//< 479>
                                                                                                              //< 480>
//< 7.7. Trading Strategy Logic 4 >```````````````````````````````````````````````````````````````````````````//< 481>
                                                                                                              //< 482>
//< 7.7.1. Trading Strategy Entry Point 2 >                                                                   //< 483>
if ( avi.SystemFlag  == 1             )                                                                       //< 484>
   {                                                                                                          //< 485>
//</7.7.1. Trading Strategy Entry Point 2 >                                                                   //< 486>
                                                                                                              //< 487>
//< 7.7.2. Trading Strategy Module Invocation 1 >                                                             //< 488>
     afr.TradingStrategy ()                                                                                 ; //< 489>
//</7.7.2. Trading Strategy Module Invocation 1 >                                                             //< 490>
                                                                                                              //< 491>
//< 7.7.3. Trading Strategy Exit Point 1 >                                                                    //< 492>
   } // if 7.7.1                                                                                              //< 493>
//</7.7.3. Trading Strategy Exit Point 1 >                                                                    //< 494>
                                                                                                              //< 495>
//</7.7. Trading Strategy Logic 4 >```````````````````````````````````````````````````````````````````````````//< 496>
                                                                                                              //< 497>
//< 7.8. Trading Module 59 >``````````````````````````````````````````````````````````````````````````````````//< 498>
                                                                                                              //< 499>
//< 7.8.1. Trading Module Entry Point 3 >                                                                     //< 500>
if   ( avi.Command       > EMPTY      )                                                                       //< 501>
if   ( IsTradeAllowed   ()            )                                                                       //< 502>
     {                                                                                                        //< 503>
//</7.8.1. Trading Module Entry Point 3 >                                                                     //< 504>
                                                                                                              //< 505>
//< 7.8.2. Risk Management 9 >                                                                                //< 506>
       avd.QuoteTarget                = MathAbs            ( avd.Price           - avd.Take               ) ; //< 507>
       avd.QuoteRisk                  = MathAbs            ( avd.Price           - avd.Stop               ) ; //< 508>
       avd.NominalPoint               = avd.NominalTick    * avd.QuotePoint      / avd.QuoteTick            ; //< 509>
       avi.MarginPoints               = MathRound          ( avd.NominalMargin   / avd.NominalPoint       ) ; //< 510>
       avi.RiskPoints                 = MathRound          ( avd.QuoteRisk       / avd.QuotePoint         ) ; //< 511>
       avd.VARLimit                   = AccountEquity ()   * aed.OrderReserve                               ; //< 512>
       avd.RiskPoint                  = avd.VARLimit       / avi.RiskPoints                                 ; //< 513>
       avd.MarginLimit                = avd.RiskPoint      * avi.MarginPoints                               ; //< 514>
       avd.SizeLimit                  = avd.MarginLimit    / avd.NominalMargin                              ; //< 515>
//</7.8.2. Risk Management 9 >                                                                                //< 516>
                                                                                                              //< 517>
//< 7.8.3. Operation Size Control 17 >                                                                        //< 518>
       if   ( avd.SizeLimit          >= avd.MinimumLots )                                                     //< 519>
            { int    ali.Steps        = MathFloor       (  ( avd.SizeLimit       - avd.MinimumLots        )   //< 520>
                                                           / avd.LotStep                                  ) ; //< 521>
              double ald.Size         = avd.MinimumLots    + avd.LotStep         * ali.Steps              ; } //< 522>
       else   ald.Size                = 0                                                                   ; //< 523>
                                                                                                              //< 524>
       if   ( ald.Size                > avd.MaximumLots )                                                     //< 525>
              ald.Size                = avd.MaximumLots                                                     ; //< 526>
                                                                                                              //< 527>
       if   ( ald.Size               >= avd.MinimumLots )                                                     //< 528>
              double ald.MarginCheck  = AccountFreeMarginCheck ( aes.Symbol      , avi.Command , ald.Size ) ; //< 529>
       else   ald.MarginCheck         = EMPTY                                                               ; //< 530>
                                                                                                              //< 531>
       if   ( ald.MarginCheck        <= 0                                                                     //< 532>
           || GetLastError ()        == 134 )                                            avi.SystemFlag = 0 ; //< 533>
       else                                                                                                   //< 534>
            { double ald.Margin       = AccountFreeMargin ()   - ald.MarginCheck                            ; //< 535>
              double ald.Contract     = ald.Size               * avd.NominalPoint      / avd.QuotePoint     ; //< 536>
              double ald.VAR          = avd.QuoteRisk          * ald.Contract                               ; //< 537>
              double ald.Target       = avd.QuoteTarget        * ald.Contract                             ; } //< 538>
//</7.8.3. Operation Size Control 17 >                                                                        //< 539>
                                                                                                              //< 540>
//< 7.8.4. Order Send Trading Function 29 >                                                                   //< 541>
       if   ( avi.SystemFlag         == 1 )                                                                   //< 542>
            {                                                                                                 //< 543>
          //< Trading Function Execution Sequence >                                                           //< 544>
          //< Step 1 >                                                                                        //< 545>
              avs.LocalStamp          = avs.SystemStamp                                 + ": Attempt to " +   //< 546>
                                        acs.Operation      [ avi.Command   ]              + " "           +   //< 547>
                                        DoubleToStr        ( ald.Size      , 2          ) + " "           +   //< 548>
                                        aes.Symbol                                        + " at "        +   //< 549>
                                        DoubleToStr        ( avd.Price     , avi.Digits ) + " sl: "       +   //< 550>
                                        DoubleToStr        ( avd.Stop      , avi.Digits ) + " tp: "       +   //< 551>
                                        DoubleToStr        ( avd.Take      , avi.Digits ) + " //"           ; //< 552>
          //< Step 2 >                                                                                        //< 553>
              Alert                   ( avs.LocalStamp                                    , " Margin: "   ,   //< 554>
                                        DoubleToStr        ( ald.Margin    , 2          ) , " / VAR: -"   ,   //< 555>
                                        DoubleToStr        ( ald.VAR       , 2          ) , " / Target: " ,   //< 556>
                                        DoubleToStr        ( ald.Target    , 2          )                 ) ; //< 557>
          //< Step 3 >                                                                                        //< 558>
              int ali.Ticket          = OrderSend          ( aes.Symbol    , avi.Command  , ald.Size      ,   //< 559>
                                        avd.Price   ,  0   , avd.Stop      , avd.Take     , ""            ,   //< 560>
                                        aci.OrderID ,  0   , 0                                            ) ; //< 561>
          //< Step 4 >                                                                                        //< 562>
              avi.TimeStamp           = TimeLocal    ()                                                     ; //< 563>
                                                                                                              //< 564>
          //< Step 5 >                                                                                        //< 565>
              avi.Exception           = GetLastError ()                                                     ; //< 566>
                                                                                                              //< 567>
          //< Step 6 >                                                                                        //< 568>
              if ( avi.Exception == 0 ) avs.LocalMessage   = " Success "   + ali.Ticket                     ; //< 569>
              else                      avs.LocalMessage   = " Failure "   + avi.Exception                  ; //< 570>
                                                                                                              //< 571>
          //< Step 7 >                                                                                        //< 572>
                                        avi.AttemptsTrade  ++                                               ; //< 573>
              if ( avi.Exception == 0 )                                                                       //< 574>
                 { if ( avi.Command  == OP_BUY             )                                                  //< 575>
                                        avi.BuyTrades      ++                                               ; //< 576>
                   else                 avi.SellTrades     ++                                               ; //< 577>
                                        avi.TotalTrades    ++                                             ; } //< 578>
              else avi.ExcepionsTrade                      ++                                               ; //< 579>
                                                                                                              //< 580>
          //< Step 8 >                                                                                        //< 581>
              Alert                   ( avs.LocalStamp                     + avs.LocalMessage             ) ; //< 582>
                                                                                                              //< 583>
          //< Step 9 >                                                                                        //< 584>
              avs.SystemMessage       = acs.Operation      [ avi.Command ] + avs.LocalMessage               ; //< 585>
                                                                                                              //< 586>
      //</Trading Function Execution Sequence >                                                               //< 587>
            } // if 7.8.4                                                                                     //< 588>
//</7.8.4. Order Send Trading Function 29 >                                                                   //< 589>
                                                                                                              //< 590>
//< 7.8.5. Trading Module Exit Point 1 >                                                                      //< 591>
     } // if 7.8.1                                                                                            //< 592>
//</7.8.5. Trading Module Exit Point 1 >                                                                      //< 593>
                                                                                                              //< 594>
//</7.8. Trading Module 59 >``````````````````````````````````````````````````````````````````````````````````//< 595>
                                                                                                              //< 596>
//< 7.9. Monitoring Module 1 >````````````````````````````````````````````````````````````````````````````````//< 597>
                                                                                                              //< 598>
if   (   avi.LiveModeFlag   == 1 )                                                        afr.Monitoring () ; //< 599>
                                                                                                              //< 600>
//</7.9. Monitoring Module 1 >````````````````````````````````````````````````````````````````````````````````//< 601>
                                                                                                              //< 602>
//< 7.10. System Controls Reset On Exit 2 >```````````````````````````````````````````````````````````````````//< 603>
                                                                                                              //< 604>
avi.TimeLastRun          = TimeLocal    ()                                                                  ; //< 605>
avi.Runs                 ++                                                                                 ; //< 606>
                                                                                                              //< 607>
//</7.10. System Controls Reset On Exit 2 >```````````````````````````````````````````````````````````````````//< 608>
                                                                                                              //< 609>
}                                                                                                             //< 610>
//</7. Main Program 205 >=====================================================================================//< 611>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////< 612>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////< 613>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////< 614>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////< 615>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////< 616>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////< 617>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////< 618>
//< 8. Extra Code >===========================================================================================//< 619>
                                                                                                              //< 620>
//< A.System.Extra: Data 25 >`````````````````````````````````````````````````````````````````````````````````//< 621>
                                                                                                              //< 622>
#define acs.FontName                    "Courier New"                                                         //< 623>
#define aci.TextLines                   48                                                                    //< 624>
#define aci.TextColumns                 64                                                                    //< 625>
                                                                                                              //< 626>
#define acs.Blank                       "                                                                "    //< 627>
#define aci.Right                       1                                                                     //< 628>
#define aci.Left                        0                                                                     //< 629>
                                                                                                              //< 630>
string  abs.TextBuffer.1              [ aci.TextLines ]                                                     ; //< 631>
string  avs.BufferName.1              [ aci.TextLines ]                                                     ; //< 632>
                                                                                                              //< 633>
#define ari.Panel.1                     0                                                                     //< 634>
#define ari.FontSize                    1                                                                     //< 635>
#define ari.FontColor                   2                                                                     //< 636>
#define ari.LineSpace                   3                                                                     //< 637>
#define ari.PositionX                   4                                                                     //< 638>
#define ari.PositionY                   5                                                                     //< 639>
                                                                                                              //< 640>
double  arv.Panel.1              [] = { ari.Panel.1                                                         , //< 641>
                                        8                                                                   , //< 642>
                                        White                                                               , //< 643>
                                        1.8                                                                 , //< 644>
                                        0                                                                   , //< 645>
                                        0                                                                 } ; //< 646>
                                                                                                              //< 647>
string  arn.Panel.1              [] = { "Panel.1"                                                           , //< 648>
                                        "FontSize"                                                          , //< 649>
                                        "FontColor"                                                         , //< 650>
                                        "LineSpace"                                                         , //< 651>
                                        "PositionX"                                                         , //< 652>
                                        "PositionY"                                                       } ; //< 653>
                                                                                                              //< 654>
string  avs.SetupPrefix                                                                                     ; //< 655>
string  avs.SetupMonitor                                                                                    ; //< 656>
string  avs.SetupFontSize                                                                                   ; //< 657>
string  avs.SetupFontColor                                                                                  ; //< 658>
string  avs.SetupLineSpace                                                                                  ; //< 659>
string  avs.SetupPositionX                                                                                  ; //< 660>
string  avs.SetupPositionY                                                                                  ; //< 661>
                                                                                                              //< 662>
string  avs.SetupOrderSelect                                                                                ; //< 663>
                                                                                                              //< 664>
int     avi.OrderSelect                                                                                     ; //< 665>
                                                                                                              //< 666>
//< A.System.Extra: Date 25 >`````````````````````````````````````````````````````````````````````````````````//< 667>
                                                                                                              //< 668>
//< A.System.Extra: Function List 342 >```````````````````````````````````````````````````````````````````````//< 669>
                                                                                                              //< 670>
// 01 int    afr.Monitoring                                                                           //   12 //< 671>
// 02 int    afr.CreateSetup                                                                          //   31 //< 672>
// 03 int    afr.DeleteSetup                                                                          //    1 //< 673>
// 04 int    afr.ResetSetup                                                                           //   22 //< 674>
// 05 int    afr.CreatePanel.1                                                                        //    5 //< 675>
// 06 int    afr.DeletePanel.1                                                                        //    3 //< 676>
// 07 int    afr.ResetPanel.1                                                                         //    7 //< 677>
// 08 int    afr.ResetTextLine.1                                                                      //    8 //< 678>
// 09 int    afr.SetTextLine.1                                                                        //    3 //< 679>
// 10 int    afr.SetText.1                                                                            //    9 //< 680>
// 11 int    afr.Report.1                                                                             //  208 //< 681>
// 12 int    afs.Interval                                                                             //    7 //< 682>
// 13 int    afr.Reserved                                                                             //    0 //< 683>
// 14 int    afs.Time                                                                                 //    5 //< 684>
// 15 int    afr.CurrencyDetector                                                                     //   16 //< 685>
// 16 int    afr.TrailingStrategy                                                                     //    1 //< 686>
// 17 int    afr.TradingStrategy                                                                      //    4 //< 687>
                                                                                                              //< 688>
//</A.System.Extra: Function List 342 >```````````````````````````````````````````````````````````````````````//< 689>
                                                                                                              //< 690>
//< A.System.Extra: Function 01 >`````````````````````````````````````````````````````````````````````````````//< 691>
                                                                                                              //< 692>
int    afr.Monitoring                ()                                                               //   12 //< 693>
{                                                                                                             //< 694>
//< 1.1. Monitoring Control 11 >                                                                              //< 695>
if ( ! GlobalVariableCheck ( avs.SetupMonitor            ) )                                                  //< 696>
       GlobalVariableSet   ( avs.SetupMonitor              , avi.MonitorFlag                              ) ; //< 697>
                                                                                                              //< 698>
if (   GlobalVariableGet   ( avs.SetupMonitor ) == 1       )                                                  //< 699>
   {   if ( avi.MonitorFlag                     == 0       )                                                  //< 700>
          { avi.MonitorFlag                      = 1                                                      ;   //< 701>
            afr.CreatePanel.1                                                                          () ; } //< 702>
       afr.ResetPanel.1                                                                                () ; } //< 703>
                                                                                                              //< 704>
else { if ( avi.MonitorFlag                     == 1       )                                                  //< 705>
          { avi.MonitorFlag                      = 0                                                      ;   //< 706>
            afr.DeleteSetup                                                                            () ;   //< 707>
            afr.DeletePanel.1                                                                          () ; } //< 708>
       return                                                                                             ; } //< 709>
//</1.1. Monitoring Control 11 >                                                                              //< 710>
                                                                                                              //< 711>
//< 1.2. Setup Reset 1 >                                                                                      //< 712>
       afr.ResetSetup                                                                                    () ; //< 713>
//</1.2. Setup Reset 1 >                                                                                      //< 714>
}                                                                                                             //< 715>
//</A.System.Extra: Function 01 >`````````````````````````````````````````````````````````````````````````````//< 716>
                                                                                                              //< 717>
//< A.System.Extra: Function 02 >`````````````````````````````````````````````````````````````````````````````//< 718>
int    afr.CreateSetup               ()                                                               //   31 //< 719>
{                                                                                                             //< 720>
       avs.SetupPrefix                = A.System.Series           + A.System.Modification + ".Setup."       ; //< 721>
                                                                                                              //< 722>
       avs.SetupBegin                 = avs.SetupPrefix           + "0.Begin.=============================" ; //< 723>
       avs.SetupAccountReserve        = avs.SetupPrefix           + "1.1." + "AccountReserve"               ; //< 724>
       avs.SetupOrderReserve          = avs.SetupPrefix           + "1.2." + "OrderReserve"                 ; //< 725>
       avs.SetupTrading               = avs.SetupPrefix           + "2.1." + "Trading"                      ; //< 726>
       avs.SetupTimeframe.1           = avs.SetupPrefix           + "2.2." + "Timeframe.1"                  ; //< 727>
       avs.SetupTimeframe.2           = avs.SetupPrefix           + "2.3." + "Timeframe.2"                  ; //< 728>
       avs.SetupParameter.1           = avs.SetupPrefix           + "2.4." + "Parameter.1"                  ; //< 729>
       avs.SetupParameter.2           = avs.SetupPrefix           + "2.5." + "Parameter.2"                  ; //< 730>
       avs.SetupParameter.3           = avs.SetupPrefix           + "2.6." + "Parameter.3"                  ; //< 731>
       avs.SetupMonitor               = avs.SetupPrefix           + "3.1." + "Monitor"                      ; //< 732>
       avs.SetupFontSize              = avs.SetupPrefix           + "3.2." + arn.Panel.1  [ ari.FontSize  ] ; //< 733>
       avs.SetupFontColor             = avs.SetupPrefix           + "3.3." + arn.Panel.1  [ ari.FontColor ] ; //< 734>
       avs.SetupLineSpace             = avs.SetupPrefix           + "3.4." + arn.Panel.1  [ ari.LineSpace ] ; //< 735>
       avs.SetupPositionX             = avs.SetupPrefix           + "3.5." + arn.Panel.1  [ ari.PositionX ] ; //< 736>
       avs.SetupPositionY             = avs.SetupPrefix           + "3.6." + arn.Panel.1  [ ari.PositionY ] ; //< 737>
       avs.SetupOrderSelect           = avs.SetupPrefix           + "4.1." + "OrderSelect"                  ; //< 738>
       avs.SetupEnd                   = avs.SetupPrefix           + "9.End.===============================" ; //< 739>
                                                                                                              //< 740>
       GlobalVariableSet              ( avs.SetupBegin            , aci.SetupSeparator                    ) ; //< 741>
       GlobalVariableSet              ( avs.SetupAccountReserve   , aed.AccountReserve                    ) ; //< 742>
       GlobalVariableSet              ( avs.SetupOrderReserve     , aed.OrderReserve                      ) ; //< 743>
       GlobalVariableSet              ( avs.SetupTrading          , avi.TradingFlag                       ) ; //< 744>
       GlobalVariableSet              ( avs.SetupTimeframe.1      , aei.Timeframe.1                       ) ; //< 745>
       GlobalVariableSet              ( avs.SetupTimeframe.2      , aei.Timeframe.2                       ) ; //< 746>
       GlobalVariableSet              ( avs.SetupParameter.1      , aed.Parameter.1                       ) ; //< 747>
       GlobalVariableSet              ( avs.SetupParameter.2      , aed.Parameter.2                       ) ; //< 748>
       GlobalVariableSet              ( avs.SetupParameter.3      , aed.Parameter.3                       ) ; //< 749>
       GlobalVariableSet              ( avs.SetupMonitor          , avi.MonitorFlag                       ) ; //< 750>
       GlobalVariableSet              ( avs.SetupFontSize         , arv.Panel.1  [ ari.FontSize  ]        ) ; //< 751>
       GlobalVariableSet              ( avs.SetupFontColor        , arv.Panel.1  [ ari.FontColor ]        ) ; //< 752>
       GlobalVariableSet              ( avs.SetupLineSpace        , arv.Panel.1  [ ari.LineSpace ]        ) ; //< 753>
       GlobalVariableSet              ( avs.SetupPositionX        , arv.Panel.1  [ ari.PositionX ]        ) ; //< 754>
       GlobalVariableSet              ( avs.SetupPositionY        , arv.Panel.1  [ ari.PositionY ]        ) ; //< 755>
       GlobalVariableSet              ( avs.SetupOrderSelect      , avi.OrderSelect                       ) ; //< 756>
       GlobalVariableSet              ( avs.SetupEnd              , aci.SetupSeparator                    ) ; //< 757>
}                                                                                                             //< 758>
//</A.System.Extra: Function 02 >`````````````````````````````````````````````````````````````````````````````//< 759>
                                                                                                              //< 760>
//< A.System.Extra: Function 03 >`````````````````````````````````````````````````````````````````````````````//< 761>
int    afr.DeleteSetup               ()                                                               //    1 //< 762>
{                                                                                                             //< 763>
GlobalVariablesDeleteAll              ( avs.SetupPrefix                                                   ) ; //< 764>
}                                                                                                             //< 765>
//</A.System.Extra: Function 03 >`````````````````````````````````````````````````````````````````````````````//< 766>
                                                                                                              //< 767>
//< A.System.Extra: Function 04 >`````````````````````````````````````````````````````````````````````````````//< 768>
                                                                                                              //< 769>
int    afr.ResetSetup                ()                                                               //   22 //< 770>
{                                                                                                             //< 771>
if ( ! GlobalVariableCheck ( avs.SetupBegin              ) )                                                  //< 772>
       GlobalVariableSet   ( avs.SetupBegin                , aci.SetupSeparator                           ) ; //< 773>
                                                                                                              //< 774>
if ( ! GlobalVariableCheck ( avs.SetupFontSize           ) )                                                  //< 775>
       GlobalVariableSet   ( avs.SetupFontSize             , arv.Panel.1      [ ari.FontSize            ] ) ; //< 776>
else   arv.Panel.1         [ ari.FontSize                ] = GlobalVariableGet( avs.SetupFontSize         ) ; //< 777>
                                                                                                              //< 778>
if ( ! GlobalVariableCheck ( avs.SetupFontColor          ) )                                                  //< 779>
       GlobalVariableSet   ( avs.SetupFontColor            , arv.Panel.1      [ ari.FontColor           ] ) ; //< 780>
else   arv.Panel.1         [ ari.FontColor               ] = GlobalVariableGet( avs.SetupFontColor        ) ; //< 781>
                                                                                                              //< 782>
if ( ! GlobalVariableCheck ( avs.SetupLineSpace          ) )                                                  //< 783>
       GlobalVariableSet   ( avs.SetupLineSpace            , arv.Panel.1      [ ari.LineSpace           ] ) ; //< 784>
else   arv.Panel.1         [ ari.LineSpace               ] = GlobalVariableGet( avs.SetupLineSpace        ) ; //< 785>
                                                                                                              //< 786>
if ( ! GlobalVariableCheck ( avs.SetupPositionX          ) )                                                  //< 787>
       GlobalVariableSet   ( avs.SetupPositionX            , arv.Panel.1      [ ari.PositionX           ] ) ; //< 788>
else   arv.Panel.1         [ ari.PositionX               ] = GlobalVariableGet( avs.SetupPositionX        ) ; //< 789>
                                                                                                              //< 790>
if ( ! GlobalVariableCheck ( avs.SetupPositionY          ) )                                                  //< 791>
       GlobalVariableSet   ( avs.SetupPositionY            , arv.Panel.1      [ ari.PositionY           ] ) ; //< 792>
else   arv.Panel.1         [ ari.PositionY               ] = GlobalVariableGet( avs.SetupPositionY        ) ; //< 793>
                                                                                                              //< 794>
if ( ! GlobalVariableCheck ( avs.SetupOrderSelect        ) )                                                  //< 795>
       GlobalVariableSet   ( avs.SetupOrderSelect          , avi.OrderSelect                              ) ; //< 796>
else   avi.OrderSelect                                     = GlobalVariableGet( avs.SetupOrderSelect      ) ; //< 797>
                                                                                                              //< 798>
if ( ! GlobalVariableCheck ( avs.SetupEnd                ) )                                                  //< 799>
       GlobalVariableSet   ( avs.SetupEnd                  , aci.SetupSeparator                           ) ; //< 800>
}                                                                                                             //< 801>
//</A.System.Extra: Function 04 >`````````````````````````````````````````````````````````````````````````````//< 802>
                                                                                                              //< 803>
//< A.System.Extra: Function 05 >`````````````````````````````````````````````````````````````````````````````//< 804>
int    afr.CreatePanel.1             ()                                                               //    5 //< 805>
{                                                                                                             //< 806>
static int     i , N =                  aci.TextLines                                                       ; //< 807>
for  ( i = 0 ; i < N ;       i ++ )                                                                           //< 808>
     { abs.TextBuffer.1    [ i ]      = ""                                                                  ; //< 809>
       avs.BufferName.1    [ i ]      = A.System.Series    + A.System.Modification + ".TextBuffer." + i     ; //< 810>
       afr.ResetTextLine.1 ( i )                                                                          ; } //< 811>
}                                                                                                             //< 812>
//</A.System.Extra: Function 05 >`````````````````````````````````````````````````````````````````````````````//< 813>
                                                                                                              //< 814>
//< A.System.Extra: Function 06 >`````````````````````````````````````````````````````````````````````````````//< 815>
int    afr.DeletePanel.1             ()                                                               //    3 //< 816>
{                                                                                                             //< 817>
static int     i , N =                  aci.TextLines                                                       ; //< 818>
for  ( i = 0 ; i < N ; i ++ )                                                                                 //< 819>
     { ObjectDelete                   ( avs.BufferName.1 [ i ] )                                          ; } //< 820>
}                                                                                                             //< 821>
//</A.System.Extra: Function 06 >`````````````````````````````````````````````````````````````````````````````//< 822>
                                                                                                              //< 823>
//< A.System.Extra: Function 07 >`````````````````````````````````````````````````````````````````````````````//< 824>
int    afr.ResetPanel.1              ()                                                               //    7 //< 825>
{                                                                                                             //< 826>
static int     i , N =                  aci.TextLines                                                       ; //< 827>
                                                                                                              //< 828>
for  ( i = 0 ; i < N ;       i ++ )                                                                           //< 829>
     { abs.TextBuffer.1    [ i ]      = ""                                                                  ; //< 830>
       afr.SetTextLine.1   ( i )                                                                          ; } //< 831>
                                                                                                              //< 832>
afr.Report.1                         ()                                                                     ; //< 833>
                                                                                                              //< 834>
for  ( i = 0 ; i < N ;       i ++ )                                                                           //< 835>
       afr.SetTextLine.1   ( i )                                                                            ; //< 836>
}                                                                                                             //< 837>
//</A.System.Extra: Function 07 >`````````````````````````````````````````````````````````````````````````````//< 838>
                                                                                                              //< 839>
//< A.System.Extra: Function 08 >`````````````````````````````````````````````````````````````````````````````//< 840>
int    afr.ResetTextLine.1           (                                                                //    8 //< 841>
       int     aai.Line               )                                                                       //< 842>
{                                                                                                             //< 843>
static string   als.Name    ; als.Name = avs.BufferName.1 [ aai.Line ]                                      ; //< 844>
                                                                                                              //< 845>
ObjectCreate  ( als.Name    , OBJ_LABEL             , 0 , 0 , 0                                           ) ; //< 846>
ObjectSet     ( als.Name    , OBJPROP_XDISTANCE     , arv.Panel.1  [ ari.FontSize  ]                          //< 847>
                                                    * arv.Panel.1  [ ari.PositionX ]                      ) ; //< 848>
ObjectSet     ( als.Name    , OBJPROP_YDISTANCE     , arv.Panel.1  [ ari.FontSize  ] *                        //< 849>
              ( arv.Panel.1 [ ari.PositionY ]       + aai.Line     * arv.Panel.1   [ ari.LineSpace ]    ) ) ; //< 850>
                                                                                                              //< 851>
ObjectSetText ( als.Name    , abs.TextBuffer.1      [ aai.Line   ] ,                                          //< 852>
                arv.Panel.1 [ ari.FontSize  ]       , acs.FontName , arv.Panel.1   [ ari.FontColor ]      ) ; //< 853>
}                                                                                                             //< 854>
//</A.System.Extra: Function 08 >`````````````````````````````````````````````````````````````````````````````//< 855>
                                                                                                              //< 856>
//< A.System.Extra: Function 09 >`````````````````````````````````````````````````````````````````````````````//< 857>
int    afr.SetTextLine.1             (                                                                //    3 //< 858>
       int     aai.Line               )                                                                       //< 859>
{                                                                                                             //< 860>
static string   als.Name    ; als.Name = avs.BufferName.1 [ aai.Line ]                                      ; //< 861>
                                                                                                              //< 862>
ObjectSetText ( als.Name    , abs.TextBuffer.1      [ aai.Line   ] ,                                          //< 863>
                arv.Panel.1 [ ari.FontSize  ]       , acs.FontName , arv.Panel.1   [ ari.FontColor ]      ) ; //< 864>
}                                                                                                             //< 865>
//</A.System.Extra: Function 09 >`````````````````````````````````````````````````````````````````````````````//< 866>
                                                                                                              //< 867>
//< A.System.Extra: Function 10 >`````````````````````````````````````````````````````````````````````````````//< 868>
int    afr.SetText.1                 (                                                                //    9 //< 869>
       int     aai.Line               ,                                                                       //< 870>
       int     aai.Position           ,                                                                       //< 871>
       int     aai.Indent             ,                                                                       //< 872>
       string  aas.Text               )                                                                       //< 873>
{                                                                                                             //< 874>
static int     ali.Begin        ; ali.Begin        = aai.Position     - StringLen ( aas.Text ) * aai.Indent ; //< 875>
                                                                                                              //< 876>
if           ( aai.Indent  == 0 ) ali.Begin        --                                                       ; //< 877>
                                                                                                              //< 878>
if           ( ali.Begin   <= 0 ) abs.TextBuffer.1 [ aai.Line ]       = aas.Text                            ; //< 879>
                                                                                                              //< 880>
else { int     ali.BufferLength = StringLen        ( abs.TextBuffer.1 [ aai.Line         ]     )            ; //< 881>
       if    ( ali.Begin        > ali.BufferLength )                                                          //< 882>
               abs.TextBuffer.1 [ aai.Line       ] = abs.TextBuffer.1 [ aai.Line         ]     +              //< 883>
                   StringSubstr ( acs.Blank  , 0 ,   ali.Begin        - ali.BufferLength )     + aas.Text   ; //< 884>
       else    abs.TextBuffer.1 [ aai.Line       ] =                                                          //< 885>
                   StringSubstr ( abs.TextBuffer.1 [ aai.Line ]   , 0 , ali.Begin        )     + aas.Text ; } //< 886>
}                                                                                                             //< 887>
//</A.System.Extra: Function 10 >`````````````````````````````````````````````````````````````````````````````//< 888>
                                                                                                              //< 889>
//< A.System.Extra: Function 11 >`````````````````````````````````````````````````````````````````````````````//< 890>
int    afr.Report.1                  ()                                                               //  208 //< 891>
{                                                                                                             //< 892>
//< 11.1. Header 3 >                                                                                          //< 893>
static int ali.Trigger ; if ( ! ali.Trigger )    { ali.Trigger = 1 ;             static string als.Header ;   //< 894>
       als.Header            = A.System.Series   + A.System.Modification         + " " + A.System.Program ; } //< 895>
                                                                                                              //< 896>
afr.SetText.1 (   0 ,  1 , 0 , als.Header + ": " + avs.SystemMessage                                      ) ; //< 897>
//</11.1. Header 3 >                                                                                          //< 898>
                                                                                                              //< 899>
//< 11.2. Currency Set Initialization 1 >`````````````````````````````````````````````````````````````````````//< 900>
afr.CurrencyDetector         ( aes.Symbol        , avs.Currency )                                           ; //< 901>
//</11.2. Currency Set Initialization 1 >`````````````````````````````````````````````````````````````````````//< 902>
                                                                                                              //< 903>
//< 11.3. First Cluster: System Report 11 >                                                                   //< 904>
afr.SetText.1 (   2 ,  1 , 0 , "Client Time: "   + afs.Time ( TimeLocal   ()               , 1 )          ) ; //< 905>
afr.SetText.1 (   3 ,  1 , 0 , "Client Name: "   + AccountName                                         () ) ; //< 906>
afr.SetText.1 (   4 ,  1 , 0 , "Server Name: "   + AccountServer                                       () ) ; //< 907>
afr.SetText.1 (   5 ,  1 , 0 , "Server Time: "   + afs.Time ( TimeCurrent ()               , 1 )          ) ; //< 908>
                                                                                                              //< 909>
afr.SetText.1 (   2 , 48 , 1 , afs.Interval      ( TimeLocal ()    - avi.TimeStart         , 1 ) + " / "  ) ; //< 910>
afr.SetText.1 (   5 , 48 , 1 , afs.Interval      ( TimeLocal ()    - avi.TimeLastRun       , 1 ) + " / "  ) ; //< 911>
                                                                                                              //< 912>
afr.SetText.1 (   2 , 49 , 0 , avi.BuyTrades     + "+"             +                                          //< 913>
                               avi.SellTrades    + "="             +                                          //< 914>
                               avi.TotalTrades   + "/"             + avi.Trailes                          ) ; //< 915>
                                                                                                              //< 916>
afr.SetText.1 (   5 , 49 , 0 , avi.Runs          + "/"             +                                          //< 917>
                               avi.AttemptsTrade + "/"             + avi.AttemptsTrail                    ) ; //< 918>
//</11.3. First Cluster: System Report 11 >                                                                   //< 919>
                                                                                                              //< 920>
//< 11.4. Second Cluster: Capital Management Report 57 >                                                      //< 921>
double ald.DrawdownAbs       = AccountEquity     ()   - avd.PeakEquity                                      ; //< 922>
double ald.DrawdownRel       = ald.DrawdownAbs        / avd.PeakEquity                                      ; //< 923>
double ald.CapitalAbs        = avd.PeakEquity         * ( 1 - aed.AccountReserve )                          ; //< 924>
double ald.CapitalRel        = 1 - aed.AccountReserve                                                       ; //< 925>
double ald.CapitalGainAbs    = avd.Capital            - avd.InitialCapital                                  ; //< 926>
double ald.CapitalGainRel    = ald.CapitalGainAbs     / avd.InitialCapital * 100                            ; //< 927>
double ald.EquityGainAbs     = AccountEquity     ()   - avd.InitialEquity                                   ; //< 928>
double ald.EquityGainRel     = ald.EquityGainAbs      / avd.InitialEquity  * 100                            ; //< 929>
double ald.EquityReserveAbs  = AccountEquity     ()   - ald.CapitalAbs                                      ; //< 930>
double ald.EquityReserveRel  = ald.EquityReserveAbs   / avd.PeakEquity                                      ; //< 931>
double ald.AccountEquityAbs  = AccountEquity     ()                                                         ; //< 932>
double ald.AccountEquityRel  = AccountEquity     ()   / avd.PeakEquity                                      ; //< 933>
double ald.AccountFreeMargin = AccountFreeMargin ()                                                         ; //< 934>
double ald.MarginLevel                                                                                      ; //< 935>
string als.StopoutLevelAbs                                                                                  ; //< 936>
string als.StopoutLevelRel                                                                                  ; //< 937>
                                                                                                              //< 938>
if   ( AccountMargin () > 0 )  ald.MarginLevel    = AccountEquity () / AccountMargin ()                     ; //< 939>
else                           ald.MarginLevel    = 0                                                       ; //< 940>
                                                                                                              //< 941>
if   ( AccountStopoutMode () == 0 )                                                                           //< 942>
     { als.StopoutLevelAbs   = DoubleToStr  ( AccountStopoutLevel () * AccountEquity () / 100 , 2 )         ; //< 943>
       als.StopoutLevelRel   =              + AccountStopoutLevel () + ".00%"                             ; } //< 944>
else                                                                                                          //< 945>
     { als.StopoutLevelAbs   =                AccountStopoutLevel () + ".00"                                ; //< 946>
       als.StopoutLevelRel   = DoubleToStr  ( AccountStopoutLevel () / AccountEquity () * 100 , 2 ) + "%" ; } //< 947>
                                                                                                              //< 948>
if   ( ald.EquityGainAbs     > 0 )     string als.GainSign = "+"                                            ; //< 949>
else                                          als.GainSign = ""                                             ; //< 950>
                                                                                                              //< 951>
afr.SetText.1 (   7 ,  1 , 0 , "Capital "   + avs.Currency           [ ari.Account    ]             + ":" ) ; //< 952>
afr.SetText.1 (   8 ,  1 , 0 , "Reserve:"                                                                 ) ; //< 953>
afr.SetText.1 (   9 ,  1 , 0 , "Peak Equity:"                                                             ) ; //< 954>
afr.SetText.1 (  10 ,  1 , 0 , "Drawdown:"                                                                ) ; //< 955>
afr.SetText.1 (  11 ,  1 , 0 , "Acc. Equity:"                                                             ) ; //< 956>
afr.SetText.1 (  12 ,  1 , 0 , "Free Margin:"                                                             ) ; //< 957>
                                                                                                              //< 958>
afr.SetText.1 (   7 , 23 , 1 , DoubleToStr  ( ald.CapitalAbs                      , 2          )          ) ; //< 959>
afr.SetText.1 (   8 , 23 , 1 , DoubleToStr  ( ald.EquityReserveAbs                , 2          )          ) ; //< 960>
afr.SetText.1 (   9 , 23 , 1 , DoubleToStr  ( avd.PeakEquity                      , 2          )          ) ; //< 961>
afr.SetText.1 (  10 , 23 , 1 , DoubleToStr  ( ald.DrawdownAbs                     , 2          )          ) ; //< 962>
afr.SetText.1 (  11 , 23 , 1 , DoubleToStr  ( ald.AccountEquityAbs                , 2          )          ) ; //< 963>
afr.SetText.1 (  12 , 23 , 1 , DoubleToStr  ( ald.AccountFreeMargin               , 2          )          ) ; //< 964>
                                                                                                              //< 965>
afr.SetText.1 (   7 , 32 , 1 , DoubleToStr  ( ald.CapitalRel       * 100          , 2          ) + "%"    ) ; //< 966>
afr.SetText.1 (   7 , 45 , 1 , "+"          + DoubleToStr ( ald.CapitalGainAbs    , 2          )          ) ; //< 967>
afr.SetText.1 (   7 , 55 , 1 , "+"          + DoubleToStr ( ald.CapitalGainRel    , 2          ) + "%"    ) ; //< 968>
afr.SetText.1 (   8 , 32 , 1 , DoubleToStr  ( ald.EquityReserveRel * 100          , 2          ) + "%"    ) ; //< 969>
afr.SetText.1 (   9 , 32 , 1 , DoubleToStr  (                        100          , 2          ) + "%"    ) ; //< 970>
afr.SetText.1 (   9 , 45 , 1 , afs.Interval ( TimeLocal ()         - avd.PeakTime , 1          )          ) ; //< 971>
afr.SetText.1 (  10 , 32 , 1 , DoubleToStr  ( ald.DrawdownRel      * 100          , 2          ) + "%"    ) ; //< 972>
afr.SetText.1 (  11 , 32 , 1 , DoubleToStr  ( ald.AccountEquityRel * 100          , 2          ) + "%"    ) ; //< 973>
afr.SetText.1 (  11 , 45 , 1 , als.GainSign + DoubleToStr ( ald.EquityGainAbs , 2 )                       ) ; //< 974>
afr.SetText.1 (  11 , 55 , 1 , als.GainSign + DoubleToStr ( ald.EquityGainRel , 2 ) + "%"                 ) ; //< 975>
afr.SetText.1 (  12 , 32 , 1 , DoubleToStr  ( ald.MarginLevel      * 100          , 2          ) + "%"    ) ; //< 976>
afr.SetText.1 (  12 , 45 , 1 , als.StopoutLevelAbs                                                        ) ; //< 977>
afr.SetText.1 (  12 , 55 , 1 , als.StopoutLevelRel                                                        ) ; //< 978>
//</11.4. Second Cluster: Capital Management Report 57 >                                                      //< 979>
                                                                                                              //< 980>
//< 11.5. Third Cluster: Position Management Report 97 >                                                      //< 981>
double ald.VARLimit      = AccountEquity ()  * aed.OrderReserve                                             ; //< 982>
int    ali.LotSize       = MarketInfo        ( aes.Symbol          , MODE_LOTSIZE                         ) ; //< 983>
double ald.NominalPoint  = avd.NominalTick   * avd.QuotePoint      / avd.QuoteTick                          ; //< 984>
int    ali.MarginPoints                                                                                     ; //< 985>
if   ( ald.NominalPoint  > 0 )                                                                                //< 986>
       ali.MarginPoints  = MathRound         ( avd.NominalMargin   / ald.NominalPoint                     ) ; //< 987>
else   ali.MarginPoints  = 0                                                                                ; //< 988>
                                                                                                              //< 989>
string als.OrderCurrency [] = { "" , "" , "" , ""                                                         } ; //< 990>
                                                                                                              //< 991>
string als.OrderType     = ""                                                                               ; //< 992>
double ald.ContractSize  = 0                                                                                ; //< 993>
double ald.ContractValue = 0                                                                                ; //< 994>
double ald.OrderPoint    = 0                                                                                ; //< 995>
int    ali.OrderLifetime = 0                                                                                ; //< 996>
                                                                                                              //< 997>
double ald.QuotePrice    = 0                                                                                ; //< 998>
double ald.QuoteTake     = 0                                                                                ; //< 999>
double ald.QuoteStop     = 0                                                                                ; //<1000>
double ald.QuoteTarget   = 0                                                                                ; //<1001>
double ald.QuoteVAR      = 0                                                                                ; //<1002>
                                                                                                              //<1003>
int    ali.OrderProfit   = 0                                                                                ; //<1004>
int    ali.OrderTarget   = 0                                                                                ; //<1005>
int    ali.OrderVAR      = 0                                                                                ; //<1006>
                                                                                                              //<1007>
double ald.OrderProfit   = 0                                                                                ; //<1008>
double ald.OrderTarget   = 0                                                                                ; //<1009>
double ald.OrderVAR      = 0                                                                                ; //<1010>
                                                                                                              //<1011>
int    ali.OrderTotal    = OrdersTotal ()                                                                   ; //<1012>
                                                                                                              //<1013>
if   ( avi.OrderSelect   > ali.OrderTotal )                                                                   //<1014>
     { avi.OrderSelect   = 0                                                                                ; //<1015>
       GlobalVariableSet ( avs.SetupOrderSelect , avi.OrderSelect                                       ) ; } //<1016>
                                                                                                              //<1017>
if   ( ali.OrderTotal    > 0        )                                                                         //<1018>
     { int   i , N ; N   = ali.OrderTotal - 1                                                               ; //<1019>
       for ( i = N ; i  >= 0 ; i -- )                                                                         //<1020>
           { OrderSelect ( i , SELECT_BY_POS , MODE_TRADES )                                                ; //<1021>
                                                                                                              //<1022>
             if (      avi.OrderSelect      == 0           )                                                  //<1023>
                { if ( OrderMagicNumber ()  != aci.OrderID )                                     continue ; } //<1024>
             else                                                                                             //<1025>
                { if ( avi.OrderSelect      != i + 1       )                                     continue ; } //<1026>
                                                                                                              //<1027>
             afr.CurrencyDetector     ( OrderSymbol     () , als.OrderCurrency                            ) ; //<1028>
                                                                                                              //<1029>
             ald.ContractSize         = OrderLots       ()                                                  ; //<1030>
             ald.ContractValue        = ald.ContractSize   / avd.QuotePoint  * ald.NominalPoint             ; //<1031>
             ald.OrderPoint           = ald.NominalPoint   * ald.ContractSize                               ; //<1032>
             ali.OrderLifetime        = TimeCurrent     () - OrderOpenTime   ()                             ; //<1033>
                                                                                                              //<1034>
             ald.QuotePrice           = OrderOpenPrice  ()                                                  ; //<1035>
             ald.QuoteTake            = OrderTakeProfit ()                                                  ; //<1036>
             ald.QuoteStop            = OrderStopLoss   ()                                                  ; //<1037>
                                                                                                              //<1038>
             ald.QuoteTarget          = MathAbs            ( ald.QuotePrice  - ald.QuoteTake              ) ; //<1039>
                                                                                                              //<1040>
             if ( OrderType ()       == OP_BUY               )                                                //<1041>
                  ald.QuoteVAR        = ald.QuoteStop      - ald.QuotePrice                                 ; //<1042>
             else ald.QuoteVAR        = ald.QuotePrice     - ald.QuoteStop                                  ; //<1043>
                                                                                                              //<1044>
             ald.OrderProfit          = OrderProfit     ()                                                  ; //<1045>
             ald.OrderTarget          = ald.QuoteTarget    * ald.ContractValue                              ; //<1046>
             ald.OrderVAR             = ald.QuoteVAR       * ald.ContractValue                              ; //<1047>
                                                                                                              //<1048>
             ali.OrderProfit          = MathRound          ( ald.OrderProfit / ald.OrderPoint )             ; //<1049>
             ali.OrderTarget          = MathRound          ( ald.QuoteTarget / avd.QuotePoint )             ; //<1050>
             ali.OrderVAR             = MathRound          ( ald.QuoteVAR    / avd.QuotePoint )             ; //<1051>
                                                                                                              //<1052>
             als.OrderType            = "#"                + OrderTicket ()                      + " "   +    //<1053>
                                        acs.Operation      [ OrderType   ()  ]                   + " "   +    //<1054>
                                        DoubleToStr        ( ald.ContractSize             , 2 )  + " x " +    //<1055>
                                        DoubleToStr        ( ali.LotSize                  , 0 )  + " "   +    //<1056>
                                        als.OrderCurrency  [ ari.Margin      ]                   + " / " +    //<1057>
                                        ali.MarginPoints                                         + " x " +    //<1058>
                                        DoubleToStr        ( ald.OrderPoint               , 2 )             ; //<1059>
       } // for                                                                                               //<1060>
   } // if                                                                                                    //<1061>
                                                                                                              //<1062>
if   ( ald.OrderProfit > 0 )   string   als.OPSign         = "+"                                            ; //<1063>
else                                    als.OPSign         = ""                                             ; //<1064>
                                                                                                              //<1065>
if   ( ald.OrderTarget > 0 )   string   als.OTSign         = "+"                                            ; //<1066>
else                                    als.OTSign         = ""                                             ; //<1067>
                                                                                                              //<1068>
if   ( ald.OrderVAR    > 0 )   string   als.OVSign         = "+"                                            ; //<1069>
else                                    als.OVSign         = ""                                             ; //<1070>
                                                                                                              //<1071>
if   ( ald.QuotePrice  > 0 )   string   als.OrderPrice     = DoubleToStr ( ald.QuotePrice    , avi.Digits ) ; //<1072>
else                                    als.OrderPrice     = ""                                             ; //<1073>
                                                                                                              //<1074>
if   ( ald.QuoteTake   > 0 )   string   als.OrderTake      = DoubleToStr ( ald.QuoteTake     , avi.Digits ) ; //<1075>
else                                    als.OrderTake      = ""                                             ; //<1076>
                                                                                                              //<1077>
if   ( ald.QuoteStop   > 0 )   string   als.OrderStop      = DoubleToStr ( ald.QuoteStop     , avi.Digits ) ; //<1078>
else                                    als.OrderStop      = ""                                             ; //<1079>
                                                                                                              //<1080>
afr.SetText.1 (  14 ,  1 , 0 , "Order:"                                                                   ) ; //<1081>
afr.SetText.1 (  15 ,  1 , 0 , "Profit:"                                                                  ) ; //<1082>
afr.SetText.1 (  16 ,  1 , 0 , "Target:"                                                                  ) ; //<1083>
afr.SetText.1 (  17 ,  1 , 0 , "VAR:"                                                                     ) ; //<1084>
afr.SetText.1 (  18 ,  1 , 0 , "Limit:"                                                                   ) ; //<1085>
                                                                                                              //<1086>
afr.SetText.1 (  14 , 14 , 0 , als.OrderType                                                              ) ; //<1087>
afr.SetText.1 (  15 , 23 , 1 , als.OPSign   + DoubleToStr  ( ald.OrderProfit      , 2          )          ) ; //<1088>
afr.SetText.1 (  15 , 32 , 1 , als.OPSign                  + ali.OrderProfit                              ) ; //<1089>
afr.SetText.1 (  15 , 45 , 1 , afs.Interval ( ali.OrderLifetime                   , 1          )          ) ; //<1090>
afr.SetText.1 (  15 , 55 , 1 , als.OrderPrice                                                             ) ; //<1091>
afr.SetText.1 (  16 , 23 , 1 , als.OTSign   + DoubleToStr  ( ald.OrderTarget      , 2          )          ) ; //<1092>
afr.SetText.1 (  16 , 32 , 1 , als.OTSign                  + ali.OrderTarget                              ) ; //<1093>
afr.SetText.1 (  16 , 55 , 1 , als.OrderTake                                                              ) ; //<1094>
afr.SetText.1 (  17 , 23 , 1 , als.OVSign   + DoubleToStr  ( ald.OrderVAR         , 2          )          ) ; //<1095>
afr.SetText.1 (  17 , 32 , 1 , als.OVSign                  + ali.OrderVAR                                 ) ; //<1096>
afr.SetText.1 (  17 , 55 , 1 , als.OrderStop                                                              ) ; //<1097>
afr.SetText.1 (  18 , 23 , 1 , DoubleToStr  ( - ald.VARLimit                      , 2          )          ) ; //<1098>
afr.SetText.1 (  18 , 32 , 1 , DoubleToStr  ( aed.OrderReserve * 100              , 2          ) + "%"    ) ; //<1099>
//</11.5. Third Cluster: Position Management Report 97 >                                                      //<1100>
                                                                                                              //<1101>
//< 11.6. Leverage/Contract Specification Indicator 14 >                                                      //<1102>
string als.Leverage          = "1:"                                                                      +    //<1103>
                               AccountLeverage ()                                                + " / " +    //<1104>
                               DoubleToStr  ( avd.NominalMargin                   , 2          ) + " "   +    //<1105>
                               avs.Currency [ ari.Account  ]                                     + " = " +    //<1106>
                               ali.MarginPoints                                           + " points x " +    //<1107>
                               DoubleToStr  ( ald.NominalPoint                    , 2          ) + " "   +    //<1108>
                               avs.Currency [ ari.Account  ]                                                ; //<1109>
                                                                                                              //<1110>
string als.Contract          = DoubleToStr  ( MarketInfo   ( aes.Symbol , MODE_LOTSIZE ) , 2 )   + " "   +    //<1111>
                               avs.Currency [ ari.Margin   ]                                     + " / " +    //<1112>
                               DoubleToStr  ( MarketInfo   ( aes.Symbol , MODE_MINLOT  ) , 2 )   + " / " +    //<1113>
                               DoubleToStr  ( MarketInfo   ( aes.Symbol , MODE_LOTSTEP ) , 2 )   + " / " +    //<1114>
                               DoubleToStr  ( MarketInfo   ( aes.Symbol , MODE_MAXLOT  ) , 2 )              ; //<1115>
                                                                                                              //<1116>
afr.SetText.1 (  20 ,  1 , 0 , "Leverage:    " + als.Leverage                                             ) ; //<1117>
afr.SetText.1 (  21 ,  1 , 0 , "Contract:    " + als.Contract                                             ) ; //<1118>
//</11.6. Leverage/Contract Specification Indicator 14 >                                                      //<1119>
                                                                                                              //<1120>
//< 11.7. Reserved 25 >                                                                                       //<1121>
afr.SetText.1 (  23 ,  1 , 0 , 23                                                                         ) ; //<1122>
afr.SetText.1 (  24 ,  1 , 0 , 24                                                                         ) ; //<1123>
afr.SetText.1 (  25 ,  1 , 0 , 25                                                                         ) ; //<1124>
afr.SetText.1 (  26 ,  1 , 0 , 26                                                                         ) ; //<1125>
afr.SetText.1 (  27 ,  1 , 0 , 27                                                                         ) ; //<1126>
afr.SetText.1 (  28 ,  1 , 0 , 28                                                                         ) ; //<1127>
afr.SetText.1 (  29 ,  1 , 0 , 29                                                                         ) ; //<1128>
afr.SetText.1 (  30 ,  1 , 0 , 30                                                                         ) ; //<1129>
afr.SetText.1 (  31 ,  1 , 0 , 31                                                                         ) ; //<1130>
afr.SetText.1 (  32 ,  1 , 0 , 32                                                                         ) ; //<1131>
afr.SetText.1 (  33 ,  1 , 0 , 33                                                                         ) ; //<1132>
afr.SetText.1 (  34 ,  1 , 0 , 34                                                                         ) ; //<1133>
afr.SetText.1 (  35 ,  1 , 0 , 35                                                                         ) ; //<1134>
afr.SetText.1 (  36 ,  1 , 0 , 36                                                                         ) ; //<1135>
afr.SetText.1 (  37 ,  1 , 0 , 37                                                                         ) ; //<1136>
afr.SetText.1 (  38 ,  1 , 0 , 38                                                                         ) ; //<1137>
afr.SetText.1 (  39 ,  1 , 0 , 39                                                                         ) ; //<1138>
afr.SetText.1 (  40 ,  1 , 0 , 40                                                                         ) ; //<1139>
afr.SetText.1 (  41 ,  1 , 0 , 41                                                                         ) ; //<1140>
afr.SetText.1 (  42 ,  1 , 0 , 42                                                                         ) ; //<1141>
afr.SetText.1 (  43 ,  1 , 0 , 43                                                                         ) ; //<1142>
afr.SetText.1 (  44 ,  1 , 0 , 44                                                                         ) ; //<1143>
afr.SetText.1 (  45 ,  1 , 0 , 45                                                                         ) ; //<1144>
afr.SetText.1 (  46 ,  1 , 0 , 46                                                                         ) ; //<1145>
afr.SetText.1 (  47 ,  1 , 0 , 47                                                                         ) ; //<1146>
//</11.7. Reserved 25 >                                                                                       //<1147>
}                                                                                                             //<1148>
//</A.System.Extra: Function 11 >`````````````````````````````````````````````````````````````````````````````//<1149>
                                                                                                              //<1150>
//< A.System.Extra: Function 12 >`````````````````````````````````````````````````````````````````````````````//<1151>
string afs.Interval                  (                                                                //    7 //<1152>
       int     aai.Interval           ,                                                                       //<1153>
       int     aai.Seconds   = EMPTY  )                                                                       //<1154>
{                                                                                                             //<1155>
static string  als.Result                                                                                   ; //<1156>
                                                                                                              //<1157>
static int     ali.Interval          ;  ali.Interval       = MathAbs       ( aai.Interval                 ) ; //<1158>
if           ( aai.Seconds  == EMPTY )  als.Result         = TimeToStr     ( ali.Interval , TIME_MINUTES  ) ; //<1159>
else                                    als.Result         = TimeToStr     ( ali.Interval , TIME_SECONDS  ) ; //<1160>
                                                                                                              //<1161>
if           ( ali.Interval >= 86400 )  als.Result         = ali.Interval  / 86400 + " "  + als.Result      ; //<1162>
else if      ( aai.Interval  < 0     )  als.Result         = "-"                          + als.Result      ; //<1163>
                                                                                                              //<1164>
return       ( als.Result                                                                                 ) ; //<1165>
}                                                                                                             //<1166>
//</A.System.Extra: Function 12 >`````````````````````````````````````````````````````````````````````````````//<1167>
                                                                                                              //<1168>
//< A.System.Extra: Function 13 >`````````````````````````````````````````````````````````````````````````````//<1169>
//</A.System.Extra: Function 13 >`````````````````````````````````````````````````````````````````````````````//<1170>
                                                                                                              //<1171>
//< A.System.Extra: Function 14 >`````````````````````````````````````````````````````````````````````````````//<1172>
string afs.Time                      (                                                                //    5 //<1173>
       int     aai.Time               ,                                                                       //<1174>
       int     aai.Seconds   = EMPTY  )                                                                       //<1175>
{                                                                                                             //<1176>
static string  als.Result                                                                                   ; //<1177>
                                                                                                              //<1178>
if           ( aai.Seconds  == EMPTY  ) int ali.Mode       = TIME_DATE     | TIME_MINUTES                   ; //<1179>
else                                        ali.Mode       = TIME_DATE     | TIME_SECONDS                   ; //<1180>
                                                                                                              //<1181>
               als.Result    = TimeToStr  ( aai.Time       , ali.Mode                                     ) ; //<1182>
                                                                                                              //<1183>
return       ( als.Result                                                                                 ) ; //<1184>
}                                                                                                             //<1185>
//</A.System.Extra: Function 14 >`````````````````````````````````````````````````````````````````````````````//<1186>
                                                                                                              //<1187>
//< A.System.Extra: Function 15 >`````````````````````````````````````````````````````````````````````````````//<1188>
int    afr.CurrencyDetector          (                                                                //   16 //<1189>
       string  aas.Symbol             ,                                                                       //<1190>
       string& aas.Currency []        )                                                                       //<1191>
{                                                                                                             //<1192>
aas.Currency        [ ari.Account ] = AccountCurrency ()                                                    ; //<1193>
                                                                                                              //<1194>
if ( ( MarketInfo   ( aas.Symbol  , MODE_PROFITCALCMODE    ) ==  0 )                                          //<1195>
  && ( MarketInfo   ( aas.Symbol  , MODE_MARGINCALCMODE    ) ==  0 )                                          //<1196>
  && ( StringLen    ( aas.Symbol                           ) ==  6 )                                          //<1197>
  && ( StringFind   ( aas.Symbol  , "#"                    ) == -1 )                                          //<1198>
  && ( StringFind   ( aas.Symbol  , "@"                    ) == -1 )                                          //<1199>
  && ( StringFind   ( aas.Symbol  , "_"                    ) == -1 )             )                            //<1200>
     { aas.Currency [ ari.Base    ]         = StringSubstr ( aas.Symbol  , 0 , 3 )                          ; //<1201>
       aas.Currency [ ari.Quote   ]         = StringSubstr ( aas.Symbol  , 3 , 3 )                        ; } //<1202>
else { aas.Currency [ ari.Base    ]         = aas.Symbol                                                    ; //<1203>
       aas.Currency [ ari.Quote   ]         = aas.Currency [ ari.Account ]                                ; } //<1204>
                                                                                                              //<1205>
if   ( avd.NominalMargin > 0 )                                                                                //<1206>
     { if ( AccountLeverage ()             == MathRound    ( avd.NominalLot  / avd.NominalMargin ) )          //<1207>
            aas.Currency   [ ari.Margin  ]  = aas.Currency [ ari.Account ]                                  ; //<1208>
       else aas.Currency   [ ari.Margin  ]  = aas.Currency [ ari.Base    ]                                ; } //<1209>
else { aas.Currency        [ ari.Margin  ]  = ""                                                          ; } //<1210>
}                                                                                                             //<1211>
//</A.System.Extra: Function 15 >`````````````````````````````````````````````````````````````````````````````//<1212>
                                                                                                              //<1213>
//< A.System.Extra: Function 16 >`````````````````````````````````````````````````````````````````````````````//<1214>
int    afr.TrailingStrategy          ()                                                               //    1 //<1215>
{                                                                                                             //<1216>
avd.Stop     = EMPTY                                                                                        ; //<1217>
}                                                                                                             //<1218>
//</A.System.Extra: Function 16 >`````````````````````````````````````````````````````````````````````````````//<1219>
                                                                                                              //<1220>
//< A.System.Extra: Function 17 >`````````````````````````````````````````````````````````````````````````````//<1221>
int    afr.TradingStrategy           ()                                                               //    4 //<1222>
{                                                                                                             //<1223>
avi.Command  = EMPTY                                                                                        ; //<1224>
avd.Price    = EMPTY                                                                                        ; //<1225>
avd.Stop     = EMPTY                                                                                        ; //<1226>
avd.Take     = EMPTY                                                                                        ; //<1227>
}                                                                                                             //<1228>
//</A.System.Extra: Function 17 >`````````````````````````````````````````````````````````````````````````````//<1229>
                                                                                                              //<1230>
//</8. Extra Code >===========================================================================================//<1231>