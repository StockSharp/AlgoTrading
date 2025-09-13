//< 1. Property  7 >==========================================================================================//<   1>
                                                                                                              //<   2>
#property      copyright                "Copyright (C) 2010, MetaQuotes Software Corp."                       //<   3>
#property      link                     "http://www.metaquotes.net"                                           //<   4>
                                                                                                              //<   5>
#define        A.Property.Series        "AIS"                                                                 //<   6>
#define        A.Property.Release       "50003"                                                               //<   7>
#define        A.Property.Date          "2010.03.31"                                                          //<   8>
#define        A.Property.Program       "Trade Machine"                                                       //<   9>
#define        A.Property.Programmer    "Airat Safin                           http://www.MQL4.com/users/AIS" //<  10>
                                                                                                              //<  11>
//</1. Property  7 >==========================================================================================//<  12>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////<  13>
//< 2. Structure 7 >==========================================================================================//<  14>
                                                                                                              //<  15>
//< 2.1. Code  7 >--------------------------------------------------------------------------------------------//<  16>
                                                                                                              //<  17>
int init   () { A.Main.Initialization   () ; }                                                                //<  18>
                                                                                                              //<  19>
int start  () { A.Control.Run           () ;                                                                  //<  20>
                A.Process.Run           () ;                                                                  //<  21>
                A.Trading.Run           () ;                                                                  //<  22>
                A.Monitor.Run           () ;                                                                  //<  23>
              }                                                                                               //<  24>
                                                                                                              //<  25>
int deinit () { A.Main.Deinitialization () ; }                                                                //<  26>
                                                                                                              //<  27>
//</2.1. Code  7 >--------------------------------------------------------------------------------------------//<  28>
                                                                                                              //<  29>
//< 2.2. Content >--------------------------------------------------------------------------------------------//<  30>
                                                                                                              //<  31>
//< 3. Data    >             33 pages     contain     295 elements // int    160 / double    48 / string   87 //<  32>
//  3100 Constants            2 pages     contain       8 elements // int      4 / double     1 / string    3 //<  33>
//  3200 Externals            5 pages     contain      20 elements // int      9 / double    10 / string    1 //<  34>
//  3300 Registers            9 pages     contain     117 elements // int    113 / double     2 / string    2 //<  35>
//  3400 Variables           17 pages     contain     150 elements // int     34 / double    35 / string   81 //<  36>
//</3. Data    >                                                                                              //<  37>
                                                                                                              //<  38>
//< 4. Modules >                functions contain         elements // input      / code         / output      //<  39>
//  4100 Main                   functions contain         elements // input      / code         / output      //<  40>
//  4200 Control                functions contain         elements // input      / code         / output      //<  41>
//  4300 Process                functions contain         elements // input      / code         / output      //<  42>
//  4400 Trading                functions contain         elements // input      / code         / output      //<  43>
//  4500 Monitor                functions contain         elements // input      / code         / output      //<  44>
//</4. Modules >                                                                                              //<  45>
                                                                                                              //<  46>
//</2.2. Content >--------------------------------------------------------------------------------------------//<  47>
                                                                                                              //<  48>
//</2. Structure 7 >==========================================================================================//<  49>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////<  50>
//< 3. Data        >==========================================================================================//<  51>
                                                                                                              //<  52>
//< 3.1. Constants >------------------------------------------------------------------------------------------//<  53>
                                                                                                              //<  54>
//  3100 Constants            2 pages     contain       8 elements // int      4 / double     1 / string    3 //<  55>
//  3101 System Contstants                              4 elements // int      2 / double     1 / string    1 //<  56>
//  3102 Monitoring Panels Constants                    4 elements // int      2 / double     - / string    2 //<  57>
                                                                                                              //<  58>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//<  59>
//< 3101 System Contstants >                            4 elements // int      2 / double     1 / string    1 //<  60>
                                                                                                              //<  61>
#define        aci.ReleaseNumber         50003                                                                //<  62>
                                                                                                              //<  63>
#define        acd.TrailStepping         1.0                                                                  //<  64>
                                                                                                              //<  65>
#define        aci.TradingPause          5                                                                    //<  66>
                                                                                                              //<  67>
string         acs.Operation     []  = { "Buy" , "Sell"                                                   } ; //<  68>
                                                                                                              //<  69>
//</3101 System Contstants >                                                                                  //<  70>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//<  71>
                                                                                                              //<  72>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//<  73>
//< 3102 Monitoring Panels Constants >                  4 elements // int      2 / double     - / string    2 //<  74>
                                                                                                              //<  75>
#define        acs.FontName             "Courier New"                                                         //<  76>
#define        aci.TextLines            48                                                                    //<  77>
#define        aci.TextColumns          64                                                                    //<  78>
#define        acs.Blank                "                                                                "    //<  79>
                                                                                                              //<  80>
//</3102 Monitoring Panels Constants >                                                                        //<  81>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//<  82>
                                                                                                              //<  83>
//</3.1. Constants >------------------------------------------------------------------------------------------//<  84>
                                                                                                              //<  85>
//< 3.2. Externals >------------------------------------------------------------------------------------------//<  86>
                                                                                                              //<  87>
//  3200 Externals            5 pages     contain      20 elements // int      9 / double    10 / string    1 //<  88>
//  3201 Order ID                                       1 elements // int      1 / double     - / string    - //<  89>
//  3202 Master Symbol                                  1 elements // int      - / double     - / string    1 //<  90>
//  3203 Risk Management Preset                         4 elements // int      - / double     4 / string    - //<  91>
//  3204 Automatic Trading Preset                      10 elements // int      4 / double     6 / string    - //<  92>
//  3205 Zone Preset                                    4 elements // int      4 / double     - / string    - //<  93>
                                                                                                              //<  94>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//<  95>
//< 3201 Order ID >                                     1 elements // int      1 / double     - / string    - //<  96>
                                                                                                              //<  97>
extern int     aei.OrderID            = aci.ReleaseNumber                                                   ; //<  98>
                                                                                                              //<  99>
//</3201 Order ID >                                                                                           //< 100>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 101>
                                                                                                              //< 102>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 103>
//< 3202 Master Symbol >                                1 elements // int      - / double     - / string    1 //< 104>
                                                                                                              //< 105>
extern string  aes.Symbol             = "EURUSD"                                                            ; //< 106>
                                                                                                              //< 107>
//</3202 Master Symbol >                                                                                      //< 108>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 109>
                                                                                                              //< 110>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 111>
//< 3203 Risk Management Preset >                       4 elements // int      - / double     4 / string    - //< 112>
                                                                                                              //< 113>
extern double  aed.AccountReserve     = 0.20            ; double avd.AccountReserve                         ; //< 114>
extern double  aed.OrderReserve       = 0.04            ; double avd.OrderReserve                           ; //< 115>
                                                                                                              //< 116>
//</3203 Risk Management Preset >                                                                             //< 117>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 118>
                                                                                                              //< 119>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 120>
//< 3204 Automatic Trading Preset >                    10 elements // int      4 / double     6 / string    - //< 121>
                                                                                                              //< 122>
extern int     aei.Timeframe.1        = 15              ; int    avi.Timeframe.1                            ; //< 123>
extern int     aei.Timeframe.2        = 1               ; int    avi.Timeframe.2                            ; //< 124>
                                                                                                              //< 125>
extern double  aed.Parameter.1        = 1.0             ; double avd.Parameter.1                            ; //< 126>
extern double  aed.Parameter.2        = 2.0             ; double avd.Parameter.2                            ; //< 127>
extern double  aed.Parameter.3        = 3.0             ; double avd.Parameter.3                            ; //< 128>
                                                                                                              //< 129>
//</3204 Automatic Trading Preset >                                                                           //< 130>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 131>
                                                                                                              //< 132>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 133>
//< 3205 Zone Preset >                                  4 elements // int      4 / double     - / string    - //< 134>
                                                                                                              //< 135>
extern int     aei.ZoneBasePoints     = 50              ; int    avi.ZoneBasePoints                         ; //< 136>
extern int     aei.ZoneStepPoints     = 100             ; int    avi.ZoneStepPoints                         ; //< 137>
                                                                                                              //< 138>
//</3205 Zone Preset >                                                                                        //< 139>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 140>
                                                                                                              //< 141>
//</3.2. Externals >------------------------------------------------------------------------------------------//< 142>
                                                                                                              //< 143>
//< 3.3. Registers >------------------------------------------------------------------------------------------//< 144>
                                                                                                              //< 145>
//  3300 Registers            9 pages     contain     117 elements // int    113 / double     2 / string    2 //< 146>
//  3301 Profile Registers                             17 elements // int     17 / double     - / string    - //< 147>
//  3302 Zone Register                                 31 elements // int     31 / double     - / string    - //< 148>
//  3303 M1 Register                                   23 elements // int     23 / double     - / string    - //< 149>
//  3304 Stream Registers                               8 elements // int      8 / double     - / string    - //< 150>
//  3305 Volume Registers                               6 elements // int      6 / double     - / string    - //< 151>
//  3306 Fluctuation Register                           5 elements // int      5 / double     - / string    - //< 152>
//  3307 Chart Metrics Register                        14 elements // int     14 / double     - / string    - //< 153>
//  3308 Panel Registers                                8 elements // int      5 / double     2 / string    1 //< 154>
//  3309 Currency Register                              5 elements // int      4 / double     - / string    1 //< 155>
                                                                                                              //< 156>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 157>
//< 3301 Profile Registers >                           17 elements // int     17 / double     - / string    - //< 158>
                                                                                                              //< 159>
#define        ari.P.TimeBegin          1                                                                     //< 160>
#define        ari.P.TimeEnd            2                                                                     //< 161>
#define        ari.P.Frames             3                                                                     //< 162>
#define        ari.P.Volume             4                                                                     //< 163>
#define        ari.P.PricePoints        5                                                                     //< 164>
#define        ari.P.PriceRange         6                                                                     //< 165>
#define        ari.P.PriceLowest        7                                                                     //< 166>
#define        ari.P.PriceHighest       8                                                                     //< 167>
#define        ari.P.MaxRange           9                                                                     //< 168>
#define        ari.P.MaxVolume          10                                                                    //< 169>
#define        ari.P.DateMaxRange       11                                                                    //< 170>
#define        ari.P.DateMaxVolume      12                                                                    //< 171>
#define        ari.P.Reserved           13                                                                    //< 172>
#define        ari.P.RegisterDimension  14                                                                    //< 173>
                                                                                                              //< 174>
int            arv.P.1                [ ari.P.RegisterDimension ]                                           ; //< 175>
int            arv.P.2                [ ari.P.RegisterDimension ]                                           ; //< 176>
int            arv.P.3                [ ari.P.RegisterDimension ]                                           ; //< 177>
                                                                                                              //< 178>
//</3301 Profile Registers >                                                                                  //< 179>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 180>
                                                                                                              //< 181>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 182>
//< 3302 Zone Register >                               31 elements // int     31 / double     - / string    - //< 183>
                                                                                                              //< 184>
#define        ari.Z.BasePoints         1                                                                     //< 185>
#define        ari.Z.StepPoints         2                                                                     //< 186>
#define        ari.Z.PriceHighest       3                                                                     //< 187>
#define        ari.Z.PriceLowest        4                                                                     //< 188>
#define        ari.Z.IndexShift         5                                                                     //< 189>
#define        ari.Z.IndexRange         6                                                                     //< 190>
#define        ari.Z.IndexCurrent       7                                                                     //< 191>
#define        ari.Z.IndexChartHighest  8                                                                     //< 192>
#define        ari.Z.IndexChartLowest   9                                                                     //< 193>
#define        ari.Z.IndexChartRange    10                                                                    //< 194>
#define        ari.Z.Set                11                                                                    //< 195>
#define        ari.Z.Reset              12                                                                    //< 196>
#define        ari.Z.Reserved           13                                                                    //< 197>
#define        ari.Z.IndexMax.1         14                                                                    //< 198>
#define        ari.Z.IndexMax.0         15                                                                    //< 199>
#define        ari.Z.VolumeMax.1        16                                                                    //< 200>
#define        ari.Z.VolumeMax.0        17                                                                    //< 201>
#define        ari.Z.VolumeMean.1       18                                                                    //< 202>
#define        ari.Z.VolumeMean.0       19                                                                    //< 203>
#define        ari.Z.Volume.1           20                                                                    //< 204>
#define        ari.Z.Volume.0           21                                                                    //< 205>
#define        ari.Z.IndexChartMax.1    22                                                                    //< 206>
#define        ari.Z.IndexChartMax.0    23                                                                    //< 207>
#define        ari.Z.VolumeChartMax.1   24                                                                    //< 208>
#define        ari.Z.VolumeChartMax.0   25                                                                    //< 209>
#define        ari.Z.VolumeChartMean.1  26                                                                    //< 210>
#define        ari.Z.VolumeChartMean.0  27                                                                    //< 211>
#define        ari.Z.VolumeChart.1      28                                                                    //< 212>
#define        ari.Z.VolumeChart.0      29                                                                    //< 213>
#define        ari.Z.RegisterDimension  30                                                                    //< 214>
                                                                                                              //< 215>
int            arv.Z                  [ ari.Z.RegisterDimension ]                                           ; //< 216>
                                                                                                              //< 217>
//</3302 Zone Register >                                                                                      //< 218>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 219>
                                                                                                              //< 220>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 221>
//< 3303 M1 Register >                                 23 elements // int     23 / double     - / string    - //< 222>
                                                                                                              //< 223>
#define        ari.1.Period             1                                                                     //< 224>
#define        ari.1.Length             2                                                                     //< 225>
#define        ari.1.Volume.0           3                                                                     //< 226>
#define        ari.1.Volume.1           4                                                                     //< 227>
#define        ari.1.VolumeLast.0       5                                                                     //< 228>
#define        ari.1.VolumeLast.1       6                                                                     //< 229>
#define        ari.1.VolumePack         7                                                                     //< 230>
#define        ari.1.Time.0             8                                                                     //< 231>
#define        ari.1.Time.1             9                                                                     //< 232>
#define        ari.1.TimeLast.0         10                                                                    //< 233>
#define        ari.1.TimeLast.1         11                                                                    //< 234>
#define        ari.1.NewFrames          12                                                                    //< 235>
#define        ari.1.Reserved           13                                                                    //< 236>
#define        ari.1.Range.0            14                                                                    //< 237>
#define        ari.1.Range.1            15                                                                    //< 238>
#define        ari.1.Length.0           16                                                                    //< 239>
#define        ari.1.Completeness.0     17                                                                    //< 240>
#define        ari.1.TimeStamp          18                                                                    //< 241>
#define        ari.1.TimeLatency        19                                                                    //< 242>
#define        ari.1.Series             20                                                                    //< 243>
#define        ari.1.Reset              21                                                                    //< 244>
#define        ari.1.RegisterDimension  22                                                                    //< 245>
                                                                                                              //< 246>
int            arv.1                  [ ari.1.RegisterDimension ]                                           ; //< 247>
                                                                                                              //< 248>
//</3303 M1 Register >                                                                                        //< 249>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 250>
                                                                                                              //< 251>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 252>
//< 3304 Stream Registers >                             8 elements // int      8 / double     - / string    - //< 253>
                                                                                                              //< 254>
#define        ari.S.RegisterDimension  10                                                                    //< 255>
                                                                                                              //< 256>
int            arv.S.Price            [ ari.S.RegisterDimension ]                                           ; //< 257>
int            arv.S.Volume           [ ari.S.RegisterDimension ]                                           ; //< 258>
int            arv.S.Latency          [ ari.S.RegisterDimension ]                                           ; //< 259>
int            arv.S.Fluctuation      [ ari.S.RegisterDimension ]                                           ; //< 260>
int            arv.S.TotalVolume      [ ari.S.RegisterDimension ]                                           ; //< 261>
int            arv.S.TotalLatency     [ ari.S.RegisterDimension ]                                           ; //< 262>
int            arv.S.TotalFluctuation [ ari.S.RegisterDimension ]                                           ; //< 263>
                                                                                                              //< 264>
//</3304 Stream Registers >                                                                                   //< 265>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 266>
                                                                                                              //< 267>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 268>
//< 3305 Volume Registers >                             6 elements // int      6 / double     - / string    - //< 269>
                                                                                                              //< 270>
#define        ari.V.Total              0                                                                     //< 271>
#define        ari.V.Lost               11                                                                    //< 272>
#define        ari.V.RegisterDimension  12                                                                    //< 273>
                                                                                                              //< 274>
int            arv.V.Volume           [ ari.V.RegisterDimension ]                                           ; //< 275>
int            arv.V.Time             [ ari.V.RegisterDimension ]                                           ; //< 276>
int            arv.V.Latency          [ ari.V.RegisterDimension ]                                           ; //< 277>
                                                                                                              //< 278>
//</3305 Volume Registers >                                                                                   //< 279>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 280>
                                                                                                              //< 281>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 282>
//< 3306 Fluctuation Register >                         5 elements // int      5 / double     - / string    - //< 283>
                                                                                                              //< 284>
#define        ari.F.Up                 1                                                                     //< 286>
#define        ari.F.Down               2                                                                     //< 287>
#define        ari.F.Zero               3                                                                     //< 285>
#define        ari.F.RegisterDimension  4                                                                     //< 288>
                                                                                                              //< 289>
int            arv.F.Volume           [ ari.F.RegisterDimension ]                                           ; //< 290>
                                                                                                              //< 291>
//</3306 Fluctuation Register >                                                                               //< 292>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 293>
                                                                                                              //< 294>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 295>
//< 3307 Chart Metrics Register >                      14 elements // int     14 / double     - / string    - //< 296>
                                                                                                              //< 297>
#define        ari.TimeZero             0                                                                     //< 298>
#define        ari.TimeRight            1                                                                     //< 299>
#define        ari.TimeLeft             2                                                                     //< 300>
#define        ari.BarRight             3                                                                     //< 301>
#define        ari.BarLeft              4                                                                     //< 302>
#define        ari.BarsTotal            5                                                                     //< 303>
#define        ari.BarsShift            6                                                                     //< 304>
#define        ari.Resolution.H         7                                                                     //< 305>
#define        ari.PriceMax             8                                                                     //< 306>
#define        ari.PriceMin             9                                                                     //< 307>
#define        ari.PriceRange           10                                                                    //< 308>
#define        ari.Resolution.V         11                                                                    //< 309>
#define        ari.C.RegisterDimension  12                                                                    //< 310>
                                                                                                              //< 311>
double         arv.Chart              [ ari.C.RegisterDimension ]                                           ; //< 312>
                                                                                                              //< 313>
//</3307 Chart Metrics Register >                                                                             //< 314>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 315>
                                                                                                              //< 316>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 317>
//< 3308 Panel Registers >                              8 elements // int      5 / double     2 / string    1 //< 318>
                                                                                                              //< 319>
#define        ari.FontSize             1                                                                     //< 320>
#define        ari.FontColor            2                                                                     //< 321>
#define        ari.LineSpace            3                                                                     //< 322>
#define        ari.PositionX            4                                                                     //< 323>
#define        ari.PositionY            5                                                                     //< 324>
                                                                                                              //< 325>
string         arn.Panel         [] = { "Panel."                                                            , //< 326>
                                        "FontSize"                                                          , //< 327>
                                        "FontColor"                                                         , //< 328>
                                        "LineSpace"                                                         , //< 329>
                                        "PositionX"                                                         , //< 330>
                                        "PositionY"                                                       } ; //< 331>
                                                                                                              //< 332>
double         arv.Panel.1       [] = { 1                                                                   , //< 333>
                                        9                                                                   , //< 334>
                                        Black                                                               , //< 335>
                                        1.8                                                                 , //< 336>
                                        0                                                                   , //< 337>
                                        0                                                                 } ; //< 338>
                                                                                                              //< 339>
double         arv.Panel.2       [] = { 2                                                                   , //< 340>
                                        9                                                                   , //< 341>
                                        Black                                                               , //< 342>
                                        1.8                                                                 , //< 343>
                                        55                                                                  , //< 344>
                                        0                                                                 } ; //< 345>
                                                                                                              //< 346>
//</3308 Panel Registers >                                                                                    //< 347>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 348>
                                                                                                              //< 349>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 350>
//< 3309 Currency Register >                            5 elements // int      4 / double     - / string    1 //< 351>
                                                                                                              //< 352>
#define        ari.Account              0                                                                     //< 353>
#define        ari.Base                 1                                                                     //< 354>
#define        ari.Quote                2                                                                     //< 355>
#define        ari.Margin               3                                                                     //< 356>
                                                                                                              //< 357>
string         arn.Currency     []  = { "" , "" , "" , ""                                                 } ; //< 358>
                                                                                                              //< 359>
//</3309 Currency Register >                                                                                  //< 360>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 361>
                                                                                                              //< 362>
//</3.3. Registers >------------------------------------------------------------------------------------------//< 363>
                                                                                                              //< 364>
//< 3.4. Variables >------------------------------------------------------------------------------------------//< 365>
                                                                                                              //< 366>
//  3400 Variables           17 pages     contain     150 elements // int     34 / double    35 / string   81 //< 367>
//  3401 Trading Strategy Interface                     4 elements // int      1 / double     3 / string    - //< 368>
//  3402 Market Data                                   15 elements // int      1 / double    14 / string    - //< 369>
//  3403 Risk Management Data                           9 elements // int      2 / double     7 / string    - //< 370>
//  3404 Capital Management Data                        6 elements // int      - / double     6 / string    - //< 371>
//  3405 System Flags                                   5 elements // int      5 / double     - / string    - //< 372>
//  3406 System Time Metrics                            5 elements // int      5 / double     - / string    - //< 373>
//  3407 System Exception Handler                       1 elements // int      1 / double     - / string    - //< 374>
//  3408 System Counters                                9 elements // int      9 / double     - / string    - //< 375>
//  3409 Market Profiles                                3 elements // int      - / double     3 / string    - //< 376>
//  3410 Market Zones                                   2 elements // int      - / double     2 / string    - //< 377>
//  3411 Monitoring Controls                           10 elements // int     10 / double     - / string    - //< 378>
//  3412 Monitoring Buffers                             4 elements // int      - / double     - / string    4 //< 379>
//  3413 Reserved                                       0 elements // int      - / double     - / string    - //< 380>
//  3414 System Messages                                5 elements // int      - / double     - / string    5 //< 381>
//  3415 Stream Lines                                   4 elements // int      - / double     - / string    4 //< 382>
//  3416 Setup Menu Names                              35 elements // int      - / double     - / string   35 //< 383>
//  3417 Plotting Names                                33 elements // int      - / double     - / string   33 //< 384>
                                                                                                              //< 385>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 386>
//< 3401 Trading Strategy Interface >                   4 elements // int      1 / double     3 / string    - //< 387>
                                                                                                              //< 388>
int            avi.Command            = EMPTY                                                               ; //< 389>
double         avd.Price              = EMPTY                                                               ; //< 390>
double         avd.Stop               = EMPTY                                                               ; //< 391>
double         avd.Take               = EMPTY                                                               ; //< 392>
                                                                                                              //< 393>
//</3401 Trading Strategy Interface >                                                                         //< 394>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 395>
                                                                                                              //< 396>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 397>
//< 3402 Market Data >                                 15 elements // int      1 / double    14 / string    - //< 398>
                                                                                                              //< 399>
double         avd.QuoteAsk                                                                                 ; //< 400>
double         avd.QuoteBid                                                                                 ; //< 401>
double         avd.QuotePoint                                                                               ; //< 402>
double         avd.QuoteSpread                                                                              ; //< 403>
double         avd.QuoteFreeze                                                                              ; //< 404>
double         avd.QuoteStops                                                                               ; //< 405>
double         avd.QuoteTick                                                                                ; //< 406>
double         avd.NominalTick                                                                              ; //< 407>
double         avd.NominalMargin                                                                            ; //< 408>
double         avd.NominalLot                                                                               ; //< 409>
double         avd.MaximumLots                                                                              ; //< 410>
double         avd.MinimumLots                                                                              ; //< 411>
double         avd.LotStep                                                                                  ; //< 412>
int            avi.Digits                                                                                   ; //< 413>
double         avd.RealSpread                                                                               ; //< 414>
                                                                                                              //< 415>
//</3402 Market Data >                                                                                        //< 416>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 417>
                                                                                                              //< 418>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 419>
//< 3403 Risk Management Data >                         9 elements // int      2 / double     7 / string    - //< 420>
                                                                                                              //< 421>
double         avd.QuoteTarget                                                                              ; //< 422>
double         avd.QuoteRisk                                                                                ; //< 423>
double         avd.NominalPoint                                                                             ; //< 424>
int            avi.MarginPoints                                                                             ; //< 425>
int            avi.RiskPoints                                                                               ; //< 426>
double         avd.VARLimit                                                                                 ; //< 427>
double         avd.RiskPoint                                                                                ; //< 428>
double         avd.MarginLimit                                                                              ; //< 429>
double         avd.SizeLimit                                                                                ; //< 430>
                                                                                                              //< 431>
//</3403 Risk Management Data >                                                                               //< 432>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 433>
                                                                                                              //< 434>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 435>
//< 3404 Capital Management Data >                      6 elements // int      - / double     6 / string    - //< 436>
                                                                                                              //< 437>
double         avd.Capital                                                                                  ; //< 438>
double         avd.PeakTime                                                                                 ; //< 439>
double         avd.PeakEquity                                                                               ; //< 440>
double         avd.InitialEquity                                                                            ; //< 441>
double         avd.InitialCapital                                                                           ; //< 442>
double         avd.EquityReserve                                                                            ; //< 443>
                                                                                                              //< 444>
//</3404 Capital Management Data >                                                                            //< 445>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 446>
                                                                                                              //< 447>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 448>
//< 3405 System Flags >                                 5 elements // int      5 / double     - / string    - //< 449>
                                                                                                              //< 450>
int            avi.SystemFlag                                                                               ; //< 451>
int            avi.TradingFlag                                                                              ; //< 452>
int            avi.LiveModeFlag                                                                             ; //< 453>
int            avi.MonitorFlag.1                                                                            ; //< 454>
int            avi.MonitorFlag.2                                                                            ; //< 455>
                                                                                                              //< 456>
//</3405 System Flags >                                                                                       //< 457>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 458>
                                                                                                              //< 459>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 460>
//< 3406 System Time Metrics >                          5 elements // int      5 / double     - / string    - //< 461>
                                                                                                              //< 462>
int            avi.TimeStamp                                                                                ; //< 463>
int            avi.TimeStart                                                                                ; //< 464>
int            avi.TimeLastRun                                                                              ; //< 465>
                                                                                                              //< 466>
int            avi.ProcessingRunTime                                                                        ; //< 467>
int            avi.MonitoringRunTime                                                                        ; //< 468>
                                                                                                              //< 469>
//</3406 System Time Metrics >                                                                                //< 470>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 471>
                                                                                                              //< 472>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 473>
//< 3407 System Exception Handler >                     1 elements // int      1 / double     - / string    - //< 474>
                                                                                                              //< 475>
int            avi.Exception                                                                                ; //< 476>
                                                                                                              //< 477>
//</3407 System Exception Handler >                                                                           //< 478>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 479>
                                                                                                              //< 480>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 481>
//< 3408 System Counters >                              9 elements // int      9 / double     - / string    - //< 482>
                                                                                                              //< 483>
int            avi.Runs                                                                                     ; //< 484>
int            avi.BuyTrades                                                                                ; //< 485>
int            avi.SellTrades                                                                               ; //< 486>
int            avi.TotalTrades                                                                              ; //< 487>
int            avi.Trailes                                                                                  ; //< 488>
int            avi.AttemptsTrade                                                                            ; //< 489>
int            avi.AttemptsTrail                                                                            ; //< 490>
int            avi.ExcepionsTrade                                                                           ; //< 491>
int            avi.ExcepionsTrail                                                                           ; //< 492>
                                                                                                              //< 493>
//</3408 System Counters >                                                                                    //< 494>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 495>
                                                                                                              //< 496>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 497>
//< 3409 Market Profiles >                              3 elements // int      - / double     3 / string    - //< 498>
                                                                                                              //< 499>
double         avd.Profile.1          []                                                                    ; //< 500>
double         avd.Profile.2          []                                                                    ; //< 501>
double         avd.Profile.3          []                                                                    ; //< 502>
                                                                                                              //< 503>
//</3409 Market Profiles >                                                                                    //< 504>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 505>
                                                                                                              //< 506>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 507>
//< 3410 Market Zones >                                 2 elements // int      - / double     2 / string    - //< 508>
                                                                                                              //< 509>
double         avd.Zone.1             []                                                                    ; //< 510>
double         avd.Zone.0             []                                                                    ; //< 511>
                                                                                                              //< 512>
//</3410 Market Zones >                                                                                       //< 513>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 514>
                                                                                                              //< 515>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 516>
//< 3411 Monitoring Controls >                         10 elements // int     10 / double     - / string    - //< 517>
                                                                                                              //< 518>
int            avi.OrderSelect        = 0                                                                   ; //< 519>
                                                                                                              //< 520>
int            avi.PlotOrderLevels    = 1                                                                   ; //< 521>
int            avi.PlotTimeBorders    = 1                                                                   ; //< 522>
int            avi.PlotZoneLevels     = 1                                                                   ; //< 523>
int            avi.PlotProfiles       = 1                                                                   ; //< 524>
                                                                                                              //< 525>
int            avi.FlagOrderLevelsExist                                                                     ; //< 526>
int            avi.FlagTimeBordersExist                                                                     ; //< 527>
int            avi.FlagZoneLevelsExist                                                                      ; //< 528>
int            avi.FlagProfilesExist                                                                        ; //< 529>
                                                                                                              //< 530>
int            avi.FlagBackground     = 1                                                                   ; //< 531>
                                                                                                              //< 532>
//< 3411 Monitoring Controls >                                                                                //< 533>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 534>
                                                                                                              //< 535>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 536>
//< 3412 Monitoring Buffers >                           4 elements // int      - / double     - / string    4 //< 537>
                                                                                                              //< 538>
string         avs.TextBuffer.1       [ aci.TextLines ]                                                     ; //< 539>
string         avs.BufferName.1       [ aci.TextLines ]                                                     ; //< 540>
                                                                                                              //< 541>
string         avs.TextBuffer.2       [ aci.TextLines ]                                                     ; //< 542>
string         avs.BufferName.2       [ aci.TextLines ]                                                     ; //< 543>
                                                                                                              //< 544>
//</3412 Monitoring Buffers >                                                                                 //< 545>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 546>
                                                                                                              //< 547>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 548>
//< 3413 Reserved >                                     0 elements // int      - / double     - / string    - //< 549>
//</3413 Reserved >                                                                                           //< 550>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 551>
                                                                                                              //< 552>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 553>
//< 3414 System Messages >                              5 elements // int      - / double     - / string    5 //< 554>
                                                                                                              //< 555>
string         avs.SystemMessage                                                                            ; //< 556>
string         avs.LocalMessage                                                                             ; //< 557>
string         avs.SystemStamp                                                                              ; //< 558>
string         avs.LocalStamp                                                                               ; //< 559>
                                                                                                              //< 560>
string         avs.ProcessingMessage                                                                        ; //< 561>
                                                                                                              //< 562>
//</3414 System Messages >                                                                                    //< 563>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 564>
                                                                                                              //< 565>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 566>
//< 3415 Stream Lines >                                 4 elements // int      - / double     - / string    4 //< 567>
                                                                                                              //< 568>
string         avs.StreamLine.1                                                                             ; //< 569>
string         avs.StreamLine.2                                                                             ; //< 570>
string         avs.StreamLine.3                                                                             ; //< 571>
string         avs.StreamLine.4                                                                             ; //< 572>
                                                                                                              //< 573>
//</3415 Stream Lines >                                                                                       //< 574>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 575>
                                                                                                              //< 576>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 577>
//< 3416 Setup Menu Names >                            35 elements // int      - / double     - / string   35 //< 578>
                                                                                                              //< 579>
string         avs.SetupPrefix                                                                              ; //< 580>
                                                                                                              //< 581>
string         avs.SetupBegin                                                                               ; //< 582>
string         avs.SetupEnd                                                                                 ; //< 583>
                                                                                                              //< 584>
string         avs.SetupAccountReserve                                                                      ; //< 585>
string         avs.SetupOrderReserve                                                                        ; //< 586>
                                                                                                              //< 587>
string         avs.SetupTrading                                                                             ; //< 588>
string         avs.SetupTimeframe.1                                                                         ; //< 589>
string         avs.SetupTimeframe.2                                                                         ; //< 590>
string         avs.SetupParameter.1                                                                         ; //< 591>
string         avs.SetupParameter.2                                                                         ; //< 592>
string         avs.SetupParameter.3                                                                         ; //< 593>
                                                                                                              //< 594>
string         avs.SetupMonitor.1                                                                           ; //< 595>
string         avs.SetupFontSize.1                                                                          ; //< 596>
string         avs.SetupFontColor.1                                                                         ; //< 597>
string         avs.SetupLineSpace.1                                                                         ; //< 598>
string         avs.SetupPositionX.1                                                                         ; //< 599>
string         avs.SetupPositionY.1                                                                         ; //< 600>
                                                                                                              //< 601>
string         avs.SetupMonitor.2                                                                           ; //< 602>
string         avs.SetupFontSize.2                                                                          ; //< 603>
string         avs.SetupFontColor.2                                                                         ; //< 604>
string         avs.SetupLineSpace.2                                                                         ; //< 605>
string         avs.SetupPositionX.2                                                                         ; //< 606>
string         avs.SetupPositionY.2                                                                         ; //< 607>
                                                                                                              //< 608>
string         avs.SetupOrderSelect                                                                         ; //< 609>
string         avs.SetupPlotOrderLevels                                                                     ; //< 610>
string         avs.SetupPlotTimeBorders                                                                     ; //< 611>
string         avs.SetupPlotZoneLevels                                                                      ; //< 612>
string         avs.SetupPlotProfiles                                                                        ; //< 613>
                                                                                                              //< 614>
string         avs.SetupProfileReset                                                                        ; //< 615>
string         avs.SetupZoneReset                                                                           ; //< 616>
                                                                                                              //< 617>
string         avs.SetupZoneBasePoints                                                                      ; //< 618>
string         avs.SetupZoneStepPoints                                                                      ; //< 619>
                                                                                                              //< 620>
string         avs.SetupCommand                                                                             ; //< 621>
string         avs.SetupTake                                                                                ; //< 622>
string         avs.SetupStop                                                                                ; //< 623>
                                                                                                              //< 624>
//</3416 Setup Menu Names >                                                                                   //< 625>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 626>
                                                                                                              //< 627>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 628>
//< 3417 Plotting Names >                              33 elements // int      - / double     - / string   33 //< 629>
                                                                                                              //< 630>
string         avs.PlotPrefix                                                                               ; //< 631>
                                                                                                              //< 632>
#define        acs.OrderPrice           "OrderPrice"                                                          //< 633>
 string        avs.OrderPrice                                                                               ; //< 634>
#define        acs.OrderTake            "OrderTake"                                                           //< 635>
 string        avs.OrderTake                                                                                ; //< 636>
#define        acs.OrderStop            "OrderStop"                                                           //< 637>
 string        avs.OrderStop                                                                                ; //< 638>
#define        acs.OrderPriceID         "OrderPriceID"                                                        //< 639>
 string        avs.OrderPriceID                                                                             ; //< 640>
#define        acs.OrderTakeID          "OrderTakeID"                                                         //< 641>
 string        avs.OrderTakeID                                                                              ; //< 642>
#define        acs.OrderStopID          "OrderStopID"                                                         //< 643>
 string        avs.OrderStopID                                                                              ; //< 644>
                                                                                                              //< 645>
#define        acs.TimeBegin.2          "TimeBegin.2."                                                        //< 646>
 string        avs.TimeBegin.2                                                                              ; //< 647>
#define        acs.TimeBegin.3          "TimeBegin.3."                                                        //< 648>
 string        avs.TimeBegin.3                                                                              ; //< 649>
                                                                                                              //< 650>
#define        acs.ZoneLevels           "ZoneLevels"                                                          //< 651>
 string        avs.ZoneLevels                                                                               ; //< 652>
#define        acs.Profile.1            "Profile.1."                                                          //< 653>
 string        avs.Profile.1                                                                                ; //< 654>
#define        acs.Profile.0            "Profile.0."                                                          //< 655>
 string        avs.Profile.0                                                                                ; //< 656>
#define        acs.Line.1               "Line.1."                                                             //< 657>
 string        avs.Line.1                                                                                   ; //< 658>
#define        acs.Line.0               "Line.0."                                                             //< 659>
 string        avs.Line.0                                                                                   ; //< 660>
#define        acs.Line.Central         "Line.Central"                                                        //< 661>
 string        avs.Line.Central                                                                             ; //< 662>
#define        acs.Line.Average.1       "Line.Average.1"                                                      //< 663>
 string        avs.Line.Average.1                                                                           ; //< 664>
#define        acs.Line.Average.0       "Line.Average.0"                                                      //< 665>
 string        avs.Line.Average.0                                                                           ; //< 666>
                                                                                                              //< 667>
//</3417 Plotting Names >                                                                                     //< 668>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 669>
                                                                                                              //< 670>
//</3.4. Variables >------------------------------------------------------------------------------------------//< 671>
                                                                                                              //< 672>
//</3. Data        >==========================================================================================//< 673>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////< 674>
//< 4. Modules     >==========================================================================================//< 675>
                                                                                                              //< 676>
//< 4.1. Main      >------------------------------------------------------------------------------------------//< 677>
                                                                                                              //< 678>
//  4100 Main                 5 functions contain       - elements // input    6 / code       - / output    - //< 679>
//  4101 int    A.Main.Initialization                   - elements // input    - / code       - / output    - //< 680>
//  4102 int    A.Main.Deinitialization                 7 elements // input    - / code       7 / output    - //< 681>
//  4103 int    afr.CurrencyDetector                   24 elements // input    2 / code      22 / output    - //< 682>
//  4104 string afs.Interval                            9 elements // input    2 / code       7 / output    - //< 683>
//  4105 string afs.Time                                8 elements // input    2 / code       6 / output    - //< 684>
                                                                                                              //< 685>
//< 4101 Main Module >````````````````````````````````````````````````````````````````````````````````````````//< 686>
                                                                                                              //< 687>
int    A.Main.Initialization            ()        //    - elements // input    - / code       - / output    - //< 688>
{                                                                                                             //< 689>
                                                                                                              //< 690>
A.Main.Initialization.Old                                                                                () ; //< 691>
}                                                                                                             //< 692>
//</4101 Main Module >````````````````````````````````````````````````````````````````````````````````````````//< 693>
                                                                                                              //< 694>
//< 4102 Main Module >````````````````````````````````````````````````````````````````````````````````````````//< 695>
                                                                                                              //< 696>
int    A.Main.Deinitialization          ()        //    7 elements // input    - / code       7 / output    - //< 697>
{                                                                                                             //< 698>
//< 1. Modules Deinitialization 4 >                                                                           //< 699>
afr.ControlDeinitialization             ()                                                                  ; //< 700>
afr.ProcessDeinitialization             ()                                                                  ; //< 701>
afr.TradingDeinitialization             ()                                                                  ; //< 702>
afr.MonitorDeinitialization             ()                                                                  ; //< 703>
//</1. Modules Deinitialization 4 >                                                                           //< 704>
                                                                                                              //< 705>
//< 2. Final Alert 3 >                                                                                        //< 706>
Alert                    ( avs.SystemStamp         , ": Stop code="        ,        UninitializeReason ()     //< 707>
                                                                           , "/"  , avi.ExcepionsTrade        //< 708>
                                                                           , "/"  , avi.ExcepionsTrail    ) ; //< 709>
//</2. Final Alert 3 >                                                                                        //< 710>
}                                                                                                             //< 711>
//</4102 Main Module >````````````````````````````````````````````````````````````````````````````````````````//< 712>
                                                                                                              //< 713>
//< 4103 Main Module >````````````````````````````````````````````````````````````````````````````````````````//< 714>
                                                                                                              //< 715>
int    afr.CurrencyDetector             (         //   24 elements // input    2 / code      22 / output    - //< 716>
       string  aas.Symbol                ,                                                                    //< 717>
       string& aas.Currency []           )                                                                    //< 718>
{                                                                                                             //< 719>
aas.Currency        [ ari.Account ] = AccountCurrency                                                    () ; //< 720>
                                                                                                              //< 721>
if ( ( MarketInfo   ( aas.Symbol  , MODE_PROFITCALCMODE    ) ==  0 )                                          //< 722>
  && ( MarketInfo   ( aas.Symbol  , MODE_MARGINCALCMODE    ) ==  0 )                                          //< 723>
  && ( StringLen    ( aas.Symbol                           ) ==  6 )                                          //< 724>
  && ( StringFind   ( aas.Symbol  , "#"                    ) == -1 )                                          //< 725>
  && ( StringFind   ( aas.Symbol  , "@"                    ) == -1 )                                          //< 726>
  && ( StringFind   ( aas.Symbol  , "_"                    ) == -1 )             )                            //< 727>
     { aas.Currency [ ari.Base    ]         = StringSubstr ( aas.Symbol  , 0 , 3 )                          ; //< 728>
       aas.Currency [ ari.Quote   ]         = StringSubstr ( aas.Symbol  , 3 , 3 )                          ; //< 729>
     }                                                                                                        //< 730>
else                                                                                                          //< 731>
     { aas.Currency [ ari.Base    ]         = aas.Symbol                                                    ; //< 732>
       aas.Currency [ ari.Quote   ]         = aas.Currency [ ari.Account ]                                  ; //< 733>
     }                                                                                                        //< 734>
                                                                                                              //< 735>
if   ( avd.NominalMargin > 0 )                                                                                //< 736>
     { if ( AccountLeverage ()             == MathRound    ( avd.NominalLot  / avd.NominalMargin ) )          //< 737>
            aas.Currency   [ ari.Margin  ]  = aas.Currency [ ari.Account ]                                  ; //< 738>
       else aas.Currency   [ ari.Margin  ]  = aas.Currency [ ari.Base    ]                                  ; //< 739>
     }                                                                                                        //< 740>
else                                                                                                          //< 741>
     { aas.Currency        [ ari.Margin  ]  = ""                                                            ; //< 742>
     }                                                                                                        //< 743>
}                                                                                                             //< 744>
//</4103 Main Module >````````````````````````````````````````````````````````````````````````````````````````//< 745>
                                                                                                              //< 746>
//< 4104 Main Module >````````````````````````````````````````````````````````````````````````````````````````//< 747>
                                                                                                              //< 748>
string afs.Interval                     (         //    9 elements // input    2 / code       7 / output    - //< 749>
       int     aai.Interval              ,                                                                    //< 750>
       int     aai.Seconds   = EMPTY     )                                                                    //< 751>
{                                                                                                             //< 752>
static string  als.Result                                                                                   ; //< 753>
                                                                                                              //< 754>
static int     ali.Interval           ; ali.Interval = MathAbs      ( aai.Interval                        ) ; //< 755>
if           ( aai.Seconds  == EMPTY  ) als.Result   = TimeToStr    ( ali.Interval , TIME_MINUTES         ) ; //< 756>
else                                    als.Result   = TimeToStr    ( ali.Interval , TIME_SECONDS         ) ; //< 757>
                                                                                                              //< 758>
if           ( ali.Interval >= 86400  ) als.Result   = ali.Interval / 86400 + " "  + als.Result             ; //< 759>
else if      ( aai.Interval  < 0      ) als.Result   = "-"                         + als.Result             ; //< 760>
                                                                                                              //< 761>
return       ( als.Result                                                                                 ) ; //< 762>
}                                                                                                             //< 763>
//</4104 Main Module >````````````````````````````````````````````````````````````````````````````````````````//< 764>
                                                                                                              //< 765>
//< 4105 Main Module >````````````````````````````````````````````````````````````````````````````````````````//< 766>
                                                                                                              //< 767>
string afs.Time                         (         //    8 elements // input    2 / code       6 / output    - //< 768>
       int     aai.Time                  ,                                                                    //< 769>
       int     aai.Seconds   = EMPTY     )                                                                    //< 770>
{                                                                                                             //< 771>
static string  als.Result                                                                                   ; //< 772>
                                                                                                              //< 773>
if           ( aai.Seconds  == EMPTY   ) int ali.Mode = TIME_DATE | TIME_MINUTES                            ; //< 774>
else if      ( aai.Seconds  == 1       )     ali.Mode = TIME_DATE | TIME_SECONDS                            ; //< 775>
else                                         ali.Mode =             TIME_SECONDS                            ; //< 776>
                                                                                                              //< 777>
               als.Result    = TimeToStr   ( aai.Time , ali.Mode                                          ) ; //< 778>
                                                                                                              //< 779>
return       ( als.Result                                                                                 ) ; //< 780>
}                                                                                                             //< 781>
//</4105 Main Module >````````````````````````````````````````````````````````````````````````````````````````//< 782>
                                                                                                              //< 783>
//</4.1. Main      >------------------------------------------------------------------------------------------//< 784>
                                                                                                              //< 785>
//< 4.2. Control   >------------------------------------------------------------------------------------------//< 786>
                                                                                                              //< 787>
//  4200 Control              - functions contain       - elements // input    - / code       - / output    - //< 788>
//  4201                                                - elements // input    - / code       - / output    - //< 789>
                                                                                                              //< 790>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 791>
//< 4201                   >                            - elements // int      - / double     - / string    - //< 792>
                                                                                                              //< 793>
                                                                                                              //< 794>
//</4201                   >                                                                                  //< 795>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 796>
                                                                                                              //< 797>
//</4.2. Control   >------------------------------------------------------------------------------------------//< 798>
                                                                                                              //< 799>
//< 4.3. Process   >------------------------------------------------------------------------------------------//< 800>
//  4300 Process              - functions contain       - elements // input    - / code       - / output    - //< 801>
//  4301                                                - elements // input    - / code       - / output    - //< 802>
                                                                                                              //< 803>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 804>
//< 4301                   >                            - elements // int      - / double     - / string    - //< 805>
                                                                                                              //< 806>
                                                                                                              //< 807>
//</4301                   >                                                                                  //< 808>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 809>
                                                                                                              //< 810>
//</4.3. Process   >------------------------------------------------------------------------------------------//< 811>
                                                                                                              //< 812>
//< 4.4. Trading   >------------------------------------------------------------------------------------------//< 813>
                                                                                                              //< 814>
//  4400 Trading              - functions contain       - elements // input    - / code       - / output    - //< 815>
//  4401                                                - elements // input    - / code       - / output    - //< 816>
                                                                                                              //< 817>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 818>
//< 4401                   >                            - elements // int      - / double     - / string    - //< 819>
                                                                                                              //< 820>
                                                                                                              //< 821>
//</4401                   >                                                                                  //< 822>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 823>
                                                                                                              //< 824>
//</4.4. Trading   >------------------------------------------------------------------------------------------//< 825>
                                                                                                              //< 826>
//< 4.5. Monitor   >------------------------------------------------------------------------------------------//< 827>
                                                                                                              //< 828>
//  4500 Monitor              - functions contain       - elements // input    - / code       - / output    - //< 829>
//  4501                                                - elements // input    - / code       - / output    - //< 830>
                                                                                                              //< 831>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 832>
//< 4501                   >                            - elements // int      - / double     - / string    - //< 833>
                                                                                                              //< 834>
                                                                                                              //< 835>
//</4501                   >                                                                                  //< 836>
//````````````````````````````````````````````````````````````````````````````````````````````````````````````//< 837>
                                                                                                              //< 838>
//</4.5. Monitor   >------------------------------------------------------------------------------------------//< 839>
                                                                                                              //< 840>
//</4. Modules     >==========================================================================================//< 841>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////< 842>
//< 5. Program Initialization 31 >============================================================================//< 843>
                                                                                                              //< 844>
int    A.Main.Initialization.Old        ()                                                                    //< 845>
{                                                                                                             //< 846>
                                                                                                              //< 847>
//< 5.1. External Variables Load 9 >``````````````````````````````````````````````````````````````````````````//< 848>
                                                                                                              //< 849>
avd.AccountReserve       = aed.AccountReserve                                                               ; //< 850>
avd.OrderReserve         = aed.OrderReserve                                                                 ; //< 851>
                                                                                                              //< 852>
avi.Timeframe.1          = aei.Timeframe.1                                                                  ; //< 853>
avi.Timeframe.2          = aei.Timeframe.2                                                                  ; //< 854>
avd.Parameter.1          = aed.Parameter.1                                                                  ; //< 855>
avd.Parameter.2          = aed.Parameter.2                                                                  ; //< 856>
avd.Parameter.3          = aed.Parameter.3                                                                  ; //< 857>
                                                                                                              //< 858>
avi.ZoneBasePoints       = aei.ZoneBasePoints                                                               ; //< 859>
avi.ZoneStepPoints       = aei.ZoneStepPoints                                                               ; //< 860>
                                                                                                              //< 861>
//</5.1. External Variables Load 9 >``````````````````````````````````````````````````````````````````````````//< 862>
                                                                                                              //< 863>
//< 5.2. System Controls Reset 9 >````````````````````````````````````````````````````````````````````````````//< 864>
                                                                                                              //< 865>
avi.TimeStart            = TimeLocal     ()                                                                 ; //< 866>
avi.TimeStamp            = TimeLocal     ()                                                                 ; //< 867>
                                                                                                              //< 868>
avd.PeakTime             = TimeLocal     ()                                                                 ; //< 869>
avd.PeakEquity           = AccountEquity ()                                                                 ; //< 870>
avd.InitialEquity        = AccountEquity ()                                                                 ; //< 871>
avd.InitialCapital       = avd.PeakEquity   * ( 1 - avd.AccountReserve )                                    ; //< 872>
                                                                                                              //< 873>
avi.TradingFlag          = 1                                                                                ; //< 874>
avi.MonitorFlag.1        = 1                                                                                ; //< 875>
avi.MonitorFlag.2        = 1                                                                                ; //< 876>
                                                                                                              //< 877>
//</5.2. System Controls Reset 9 >````````````````````````````````````````````````````````````````````````````//< 878>
                                                                                                              //< 879>
//< 5.3. System Stamp Reset 3 >```````````````````````````````````````````````````````````````````````````````//< 880>
                                                                                                              //< 881>
avs.SystemStamp          = A.Property.Series                               +                                  //< 882>
                           A.Property.Release                              + " "  +                           //< 883>
                           A.Property.Program                                                               ; //< 884>
                                                                                                              //< 885>
//</5.3. System Stamp Reset 3 >```````````````````````````````````````````````````````````````````````````````//< 886>
                                                                                                              //< 887>
//< 5.4. First Alert 8 >``````````````````````````````````````````````````````````````````````````````````````//< 888>
                                                                                                              //< 889>
Alert                    ( avs.SystemStamp                                 , ": Symbol="                  ,   //< 890>
                           aes.Symbol                                      , ", Preset="                  ,   //< 891>
                           avi.Timeframe.1                                 , "/"                          ,   //< 892>
                           avi.Timeframe.2                                 , "/"                          ,   //< 893>
                           DoubleToStr             ( avd.Parameter.1 , 1 ) , "/"                          ,   //< 894>
                           DoubleToStr             ( avd.Parameter.2 , 1 ) , "/"                          ,   //< 895>
                           DoubleToStr             ( avd.Parameter.3 , 1 ) , " "                              //< 896>
                                                   , ", Start code="       ,        UninitializeReason () ) ; //< 897>
                                                                                                              //< 898>
//</5.4. First Alert 8 >``````````````````````````````````````````````````````````````````````````````````````//< 899>
                                                                                                              //< 900>
//< 5.5. .Extra Code Initialization 2 >```````````````````````````````````````````````````````````````````````//< 901>
                                                                                                              //< 902>
afr.ProcessingInitialization         ()                                                                     ; //< 903>
afr.ControlInitialization            ()                                                                     ; //< 904>
                                                                                                              //< 905>
//</5.5. .Extra Code Initialization 2 >```````````````````````````````````````````````````````````````````````//< 906>
                                                                                                              //< 907>
}                                                                                                             //< 908>
//</5. Program Initialization 31 >============================================================================//< 909>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////< 910>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////< 911>
//< 7. Main Program 207 >=====================================================================================//< 912>
                                                                                                              //< 913>
int    A.Process.Run                 () {}                                                                    //< 914>
int    A.Trading.Run                 () {}                                                                    //< 915>
int    A.Monitor.Run                 () {}                                                                    //< 916>
                                                                                                              //< 917>
int    A.Control.Run                 ()                                                                       //< 918>
{                                                                                                             //< 919>
                                                                                                              //< 920>
//< 7.1. System Controls Reset On Enter 75 >``````````````````````````````````````````````````````````````````//< 921>
                                                                                                              //< 922>
//< 7.1.1. Run Mode Detection 1 >                                                                             //< 923>
avi.LiveModeFlag           = ! IsTesting () || IsVisualMode ()                                              ; //< 924>
//</7.1.1. Run Mode Detection 1 >                                                                             //< 925>
                                                                                                              //< 926>
//< 7.1.2. Live Mode Subroutine 23 >                                                                          //< 927>
if (   avi.LiveModeFlag   == 1        )                                                                       //< 928>
   {                                                                                                          //< 929>
       if ( ! GlobalVariableCheck     ( avs.SetupBegin          ) )                                           //< 930>
              GlobalVariableSet       ( avs.SetupBegin            , aci.ReleaseNumber                     ) ; //< 931>
       if ( ! GlobalVariableCheck     ( avs.SetupAccountReserve ) )                                           //< 932>
              GlobalVariableSet       ( avs.SetupAccountReserve   , avd.AccountReserve                    ) ; //< 933>
       if ( ! GlobalVariableCheck     ( avs.SetupOrderReserve   ) )                                           //< 934>
              GlobalVariableSet       ( avs.SetupOrderReserve     , avd.OrderReserve                      ) ; //< 935>
       if ( ! GlobalVariableCheck     ( avs.SetupTrading        ) )                                           //< 936>
              GlobalVariableSet       ( avs.SetupTrading          , avi.TradingFlag                       ) ; //< 937>
       if ( ! GlobalVariableCheck     ( avs.SetupTimeframe.1    ) )                                           //< 938>
              GlobalVariableSet       ( avs.SetupTimeframe.1      , avi.Timeframe.1                       ) ; //< 939>
       if ( ! GlobalVariableCheck     ( avs.SetupTimeframe.2    ) )                                           //< 940>
              GlobalVariableSet       ( avs.SetupTimeframe.2      , avi.Timeframe.2                       ) ; //< 941>
       if ( ! GlobalVariableCheck     ( avs.SetupParameter.1    ) )                                           //< 942>
              GlobalVariableSet       ( avs.SetupParameter.1      , avd.Parameter.1                       ) ; //< 943>
       if ( ! GlobalVariableCheck     ( avs.SetupParameter.2    ) )                                           //< 944>
              GlobalVariableSet       ( avs.SetupParameter.2      , avd.Parameter.2                       ) ; //< 945>
       if ( ! GlobalVariableCheck     ( avs.SetupParameter.3    ) )                                           //< 946>
              GlobalVariableSet       ( avs.SetupParameter.3      , avd.Parameter.3                       ) ; //< 947>
       if ( ! GlobalVariableCheck     ( avs.SetupEnd            ) )                                           //< 948>
              GlobalVariableSet       ( avs.SetupEnd              , aci.ReleaseNumber                     ) ; //< 949>
   }                                                                                                          //< 950>
//</7.1.2. Live Mode Subroutine 23 >                                                                          //< 951>
                                                                                                              //< 952>
//< 7.1.3. All Modes Subroutine 8 >                                                                           //< 953>
if (   GlobalVariableGet      ( avs.SetupTrading )  == 1 )                                                    //< 954>
   {   avi.SystemFlag         = 1                                                                         ;   //< 955>
       avi.TradingFlag        = 1                                                                         ;   //< 956>
       avs.SystemMessage      = "Trading is enabled"                                                      ; } //< 957>
else                                                                                                          //< 958>
   {   avi.SystemFlag         = 0                                                                         ;   //< 959>
       avi.TradingFlag        = 0                                                                         ;   //< 960>
       avs.SystemMessage      = "Trading is disabled"                                                     ; } //< 961>
//</7.1.3. All Modes Subroutine 8 >                                                                           //< 962>
                                                                                                              //< 963>
//< 7.1.4. Live Mode Subroutine 43 >                                                                          //< 964>
if (   avi.LiveModeFlag      == 1 )                                                                           //< 965>
   {                                                                                                          //< 966>
       avd.AccountReserve     = GlobalVariableGet    ( avs.SetupAccountReserve                            ) ; //< 967>
       avd.OrderReserve       = GlobalVariableGet    ( avs.SetupOrderReserve                              ) ; //< 968>
       avi.Timeframe.1        = GlobalVariableGet    ( avs.SetupTimeframe.1                               ) ; //< 969>
       avi.Timeframe.2        = GlobalVariableGet    ( avs.SetupTimeframe.2                               ) ; //< 970>
       avd.Parameter.1        = GlobalVariableGet    ( avs.SetupParameter.1                               ) ; //< 971>
       avd.Parameter.2        = GlobalVariableGet    ( avs.SetupParameter.2                               ) ; //< 972>
       avd.Parameter.3        = GlobalVariableGet    ( avs.SetupParameter.3                               ) ; //< 973>
                                                                                                              //< 974>
       if ( avi.Timeframe.1  != PERIOD_M1                                                                     //< 975>
         && avi.Timeframe.1  != PERIOD_M5                                                                     //< 976>
         && avi.Timeframe.1  != PERIOD_M15                                                                    //< 977>
         && avi.Timeframe.1  != PERIOD_M30                                                                    //< 978>
         && avi.Timeframe.1  != PERIOD_H1                                                                     //< 979>
         && avi.Timeframe.1  != PERIOD_H4                                                                     //< 980>
         && avi.Timeframe.1  != PERIOD_D1                                                                     //< 981>
         && avi.Timeframe.1  != PERIOD_W1                                                                     //< 982>
         && avi.Timeframe.1  != PERIOD_MN1 )                                                                  //< 983>
          { avi.Timeframe.1   = 0                                                                         ;   //< 984>
            avi.SystemFlag    = 0                                                                         ;   //< 985>
            avi.TradingFlag   = 0                                                                         ;   //< 986>
            GlobalVariableSet ( avs.SetupTimeframe.1 , avi.Timeframe.1                                  ) ;   //< 987>
            GlobalVariableSet ( avs.SetupTrading     , avi.TradingFlag                                  ) ;   //< 988>
            avs.SystemMessage = "Check Timeframe.1"                                                       ;   //< 989>
            Alert             ( avs.SystemStamp                              , ": Symbol="                ,   //< 990>
                                aes.Symbol                                   , " "  , avs.SystemMessage ) ; } //< 991>
                                                                                                              //< 992>
       if ( avi.Timeframe.2  != PERIOD_M1                                                                     //< 993>
         && avi.Timeframe.2  != PERIOD_M5                                                                     //< 994>
         && avi.Timeframe.2  != PERIOD_M15                                                                    //< 995>
         && avi.Timeframe.2  != PERIOD_M30                                                                    //< 996>
         && avi.Timeframe.2  != PERIOD_H1                                                                     //< 997>
         && avi.Timeframe.2  != PERIOD_H4                                                                     //< 998>
         && avi.Timeframe.2  != PERIOD_D1                                                                     //< 999>
         && avi.Timeframe.2  != PERIOD_W1                                                                     //<1000>
         && avi.Timeframe.2  != PERIOD_MN1 )                                                                  //<1001>
          { avi.Timeframe.2   = 0                                                                         ;   //<1002>
            avi.SystemFlag    = 0                                                                         ;   //<1003>
            avi.TradingFlag   = 0                                                                         ;   //<1004>
            GlobalVariableSet ( avs.SetupTimeframe.2 , avi.Timeframe.2                                  ) ;   //<1005>
            GlobalVariableSet ( avs.SetupTrading     , avi.TradingFlag                                  ) ;   //<1006>
            avs.SystemMessage = "Check Timeframe.2"                                                       ;   //<1007>
            Alert              ( avs.SystemStamp                             , ": Symbol="                ,   //<1008>
                                 aes.Symbol                                  , " "  , avs.SystemMessage ) ; } //<1009>
   }                                                                                                          //<1010>
//</7.1.4. Live Mode Subroutine 43 >                                                                          //<1011>
                                                                                                              //<1012>
//</7.1. System Controls Reset On Enter 75 >``````````````````````````````````````````````````````````````````//<1013>
                                                                                                              //<1014>
//</7.2. Trading Pause Control 2 >````````````````````````````````````````````````````````````````````````````//<1015>
                                                                                                              //<1016>
if   ( TimeLocal     ()  - avi.TimeStamp           < aci.TradingPause )                                       //<1017>
     { avs.SystemMessage = "Trading pause "        + aci.TradingPause +   " seconds" ; avi.SystemFlag = 0 ; } //<1018>
                                                                                                              //<1019>
//</7.2. Trading Pause Control 2 >````````````````````````````````````````````````````````````````````````````//<1020>
                                                                                                              //<1021>
//< 7.3. Equity Control 6 >```````````````````````````````````````````````````````````````````````````````````//<1022>
                                                                                                              //<1023>
if   ( AccountEquity ()  - avd.PeakEquity > 0      )                                                          //<1024>
     { avd.PeakEquity    = AccountEquity ()                                                                 ; //<1025>
       avd.PeakTime      = TimeLocal     ()                                                               ; } //<1026>
                                                                                                              //<1027>
       avd.Capital       = avd.PeakEquity          * ( 1 - avd.AccountReserve )                             ; //<1028>
       avd.EquityReserve = AccountEquity ()        - avd.Capital                                            ; //<1029>
       avd.VARLimit      = AccountEquity ()        * avd.OrderReserve                                       ; //<1030>
                                                                                                              //<1031>
if   ( avd.EquityReserve - avd.VARLimit   < 0      )                                                          //<1032>
     { avs.SystemMessage = "System stop"                                             ; avi.SystemFlag = 0 ; } //<1033>
                                                                                                              //<1034>
//</7.3. Equity Control 6 >```````````````````````````````````````````````````````````````````````````````````//<1035>
                                                                                                              //<1036>
//< 7.4. Data Feed 16 >```````````````````````````````````````````````````````````````````````````````````````//<1037>
                                                                                                              //<1038>
//< 7.4.1. Common Data Feed 15 >                                                                              //<1039>
       avd.QuoteAsk      = MarketInfo ( aes.Symbol , MODE_ASK            )                                  ; //<1040>
       avd.QuoteBid      = MarketInfo ( aes.Symbol , MODE_BID            )                                  ; //<1041>
       avd.QuotePoint    = MarketInfo ( aes.Symbol , MODE_POINT          )                                  ; //<1042>
       avd.QuoteSpread   = MarketInfo ( aes.Symbol , MODE_SPREAD         ) * avd.QuotePoint                 ; //<1043>
       avd.QuoteFreeze   = MarketInfo ( aes.Symbol , MODE_FREEZELEVEL    ) * avd.QuotePoint                 ; //<1044>
       avd.QuoteStops    = MarketInfo ( aes.Symbol , MODE_STOPLEVEL      ) * avd.QuotePoint                 ; //<1045>
       avd.QuoteTick     = MarketInfo ( aes.Symbol , MODE_TICKSIZE       )                                  ; //<1046>
       avd.NominalTick   = MarketInfo ( aes.Symbol , MODE_TICKVALUE      )                                  ; //<1047>
       avd.NominalMargin = MarketInfo ( aes.Symbol , MODE_MARGINREQUIRED )                                  ; //<1048>
       avd.NominalLot    = MarketInfo ( aes.Symbol , MODE_LOTSIZE        )                                  ; //<1049>
       avd.MaximumLots   = MarketInfo ( aes.Symbol , MODE_MAXLOT         )                                  ; //<1050>
       avd.MinimumLots   = MarketInfo ( aes.Symbol , MODE_MINLOT         )                                  ; //<1051>
       avd.LotStep       = MarketInfo ( aes.Symbol , MODE_LOTSTEP        )                                  ; //<1052>
       avi.Digits        = MarketInfo ( aes.Symbol , MODE_DIGITS         )                                  ; //<1053>
       avd.RealSpread    = avd.QuoteAsk            - avd.QuoteBid                                           ; //<1054>
//</7.4.1. Common Data Feed 15 >                                                                              //<1055>
                                                                                                              //<1056>
//< 7.4.2. Special Data Processing 1 >                                                                        //<1057>
       afr.Processing    ()                                                                                 ; //<1058>
//</7.4.2. Special Data Processing 1 >                                                                        //<1059>
                                                                                                              //<1060>
//</7.4. Data Feed 16 >```````````````````````````````````````````````````````````````````````````````````````//<1061>
                                                                                                              //<1062>
//< 7.5. Trading Strategy Interface Reset 4 >`````````````````````````````````````````````````````````````````//<1063>
                                                                                                              //<1064>
       avi.Command       = EMPTY                                                                            ; //<1065>
       avd.Price         = EMPTY                                                                            ; //<1066>
       avd.Stop          = EMPTY                                                                            ; //<1067>
       avd.Take          = EMPTY                                                                            ; //<1068>
                                                                                                              //<1069>
//</7.5. Trading Strategy Interface Reset 4 >`````````````````````````````````````````````````````````````````//<1070>
                                                                                                              //<1071>
//< 7.6. Position Management Module 38 >``````````````````````````````````````````````````````````````````````//<1072>
                                                                                                              //<1073>
//< 7.6.1. Position Management Module Entry Point 7 >                                                         //<1074>
if ( avi.SystemFlag  == 1             )                                                                       //<1075>
if ( OrdersTotal ()   > 0             )                                                                       //<1076>
                                                                                                              //<1077>
   { int   i , N ; N  = OrdersTotal  () - 1                                                                 ; //<1078>
     for ( i = N ; i >= 0         ; i --                                   )                                  //<1079>
       {      OrderSelect         ( i , SELECT_BY_POS , MODE_TRADES        )                                ; //<1080>
         if ( OrderMagicNumber    () != aei.OrderID                        )                       continue ; //<1081>
         else                                                                            avi.SystemFlag = 0 ; //<1082>
//</7.6.1. Position Management Module Entry Point 7 >                                                         //<1083>
                                                                                                              //<1084>
//< 7.6.2. Trailing Strategy Module Invocation 1 >                                                            //<1085>
         afr.TrailingStrategy ()                                                                            ; //<1086>
//</7.6.2. Trailing Strategy Module Invocation 1 >                                                            //<1087>
                                                                                                              //<1088>
//< 7.6.3. Order Modify Trading Function 28 >                                                                 //<1089>
         if ( avd.Stop                > 0 )                                                                   //<1090>
            { int  ali.TrailPoints    = MathRound       (    MathAbs       ( OrderStopLoss ()  - avd.Stop )   //<1091>
                                                                           / avd.QuotePoint               ) ; //<1092>
              if ( ali.TrailPoints   >= MarketInfo      (    aes.Symbol    , MODE_FREEZELEVEL           ) )   //<1093>
                 {                                                                                            //<1094>
               //< Trading Function Execution Sequence >                                                      //<1095>
               //< Step 1 >                                                                                   //<1096>
                   avs.LocalStamp     = avs.SystemStamp                           + ": Attempt to trail " +   //<1097>
                                        aes.Symbol                                                 + " "  +   //<1098>
                                        acs.Operation    [   OrderType ()  ]                       + " #" +   //<1099>
                                        OrderMagicNumber ()                                        + "/"  +   //<1100>
                                        OrderTicket      ()                                                 ; //<1101>
               //< Step 2 >                                                                                   //<1102>
                   Alert              ( avs.LocalStamp                                           ,     " +" , //<1103>
                                        ali.TrailPoints                                          , " from " , //<1104>
                                        DoubleToStr     (    OrderStopLoss ()     , avi.Digits ) ,   " to " , //<1105>
                                        DoubleToStr     (    avd.Stop             , avi.Digits )          ) ; //<1106>
               //< Step 3 >                                                                                   //<1107>
                   OrderModify        ( OrderTicket     () ,                                                  //<1108>
                                        OrderOpenPrice  () ,                                                  //<1109>
                                        avd.Stop           ,                                                  //<1110>
                                        OrderTakeProfit () , 0 , 0                                        ) ; //<1111>
               //< Step 4 >                                                                                   //<1112>
                   avi.TimeStamp      = TimeLocal       ()                                                  ; //<1113>
                                                                                                              //<1114>
               //< Step 5 >                                                                                   //<1115>
                   avi.Exception      = GetLastError    ()                                                  ; //<1116>
                                                                                                              //<1117>
               //< Step 6 >                                                                                   //<1118>
                   if ( avi.Exception == 0 ) avs.LocalMessage =            " Success "                      ; //<1119>
                   else                      avs.LocalMessage =            " Failure "      + avi.Exception ; //<1120>
                                                                                                              //<1121>
               //< Step 7 >                                                                                   //<1122>
                   avi.AttemptsTrail                          ++                                            ; //<1123>
                   if ( avi.Exception == 0 ) avi.Trailes      ++                                            ; //<1124>
                   else avi.ExcepionsTrail                    ++                                            ; //<1125>
                                                                                                              //<1126>
               //< Step 8 >                                                                                   //<1127>
                   Alert              ( avs.LocalStamp                     + avs.LocalMessage             ) ; //<1128>
                                                                                                              //<1129>
               //< Step 9 >                                                                                   //<1130>
                   avs.SystemMessage  = "Trailing Stop"                    + avs.LocalMessage               ; //<1131>
                                                                                                              //<1132>
          //</Trading Function Execution Sequence >                                                           //<1133>
                 } // if 7.6.3                                                                                //<1134>
            } // if 7.6.3                                                                                     //<1135>
//</7.6.3. Order Modify Trading Function 28 >                                                                 //<1136>
                                                                                                              //<1137>
//< 7.6.4. Position Management Module Exit Point 2 >                                                          //<1138>
       } // for 7.6.1                                                                                         //<1139>
   } // if 7.6.1                                                                                              //<1140>
//</7.6.4. Position Management Module Exit Point 2 >                                                          //<1141>
                                                                                                              //<1142>
//</7.6. Position Management Module 38 >``````````````````````````````````````````````````````````````````````//<1143>
                                                                                                              //<1144>
//< 7.7. Trading Strategy Logic 4 >```````````````````````````````````````````````````````````````````````````//<1145>
                                                                                                              //<1146>
//< 7.7.1. Trading Strategy Entry Point 2 >                                                                   //<1147>
if ( avi.SystemFlag == avi.SystemFlag )                                                                       //<1148>
   {                                                                                                          //<1149>
//</7.7.1. Trading Strategy Entry Point 2 >                                                                   //<1150>
                                                                                                              //<1151>
//< 7.7.2. Trading Strategy Module Invocation 1 >                                                             //<1152>
     afr.TradingStrategy ()                                                                                 ; //<1153>
//</7.7.2. Trading Strategy Module Invocation 1 >                                                             //<1154>
                                                                                                              //<1155>
//< 7.7.3. Trading Strategy Exit Point 1 >                                                                    //<1156>
   } // if 7.7.1                                                                                              //<1157>
//</7.7.3. Trading Strategy Exit Point 1 >                                                                    //<1158>
                                                                                                              //<1159>
//</7.7. Trading Strategy Logic 4 >```````````````````````````````````````````````````````````````````````````//<1160>
                                                                                                              //<1161>
//< 7.8. Trading Module 59 >``````````````````````````````````````````````````````````````````````````````````//<1162>
                                                                                                              //<1163>
//< 7.8.1. Trading Module Entry Point 3 >                                                                     //<1164>
if   ( avi.Command       > EMPTY      )                                                                       //<1165>
if   ( IsTradeAllowed   ()            )                                                                       //<1166>
     {                                                                                                        //<1167>
//</7.8.1. Trading Module Entry Point 3 >                                                                     //<1168>
                                                                                                              //<1169>
//< 7.8.2. Risk Management 9 >                                                                                //<1170>
       avd.QuoteTarget                = MathAbs            ( avd.Price           - avd.Take               ) ; //<1171>
       avd.QuoteRisk                  = MathAbs            ( avd.Price           - avd.Stop               ) ; //<1172>
       avd.NominalPoint               = avd.NominalTick    * avd.QuotePoint      / avd.QuoteTick            ; //<1173>
       avi.MarginPoints               = MathRound          ( avd.NominalMargin   / avd.NominalPoint       ) ; //<1174>
       avi.RiskPoints                 = MathRound          ( avd.QuoteRisk       / avd.QuotePoint         ) ; //<1175>
       avd.VARLimit                   = AccountEquity ()   * avd.OrderReserve                               ; //<1176>
       avd.RiskPoint                  = avd.VARLimit       / avi.RiskPoints                                 ; //<1177>
       avd.MarginLimit                = avd.RiskPoint      * avi.MarginPoints                               ; //<1178>
       avd.SizeLimit                  = avd.MarginLimit    / avd.NominalMargin                              ; //<1179>
//</7.8.2. Risk Management 9 >                                                                                //<1180>
                                                                                                              //<1181>
//< 7.8.3. Operation Size Control 17 >                                                                        //<1182>
       if   ( avd.SizeLimit          >= avd.MinimumLots )                                                     //<1183>
            { int    ali.Steps        = MathFloor       (  ( avd.SizeLimit       - avd.MinimumLots        )   //<1184>
                                                           / avd.LotStep                                  ) ; //<1185>
              double ald.Size         = avd.MinimumLots    + avd.LotStep         * ali.Steps              ; } //<1186>
       else   ald.Size                = 0                                                                   ; //<1187>
                                                                                                              //<1188>
       if   ( ald.Size                > avd.MaximumLots )                                                     //<1189>
              ald.Size                = avd.MaximumLots                                                     ; //<1190>
                                                                                                              //<1191>
       if   ( ald.Size               >= avd.MinimumLots )                                                     //<1192>
              double ald.MarginCheck  = AccountFreeMarginCheck ( aes.Symbol      , avi.Command , ald.Size ) ; //<1193>
       else   ald.MarginCheck         = EMPTY                                                               ; //<1194>
                                                                                                              //<1195>
       if   ( ald.MarginCheck        <= 0                                                                     //<1196>
           || GetLastError ()        == 134 )                                            avi.SystemFlag = 0 ; //<1197>
       else                                                                                                   //<1198>
            { double ald.Margin       = AccountFreeMargin ()   - ald.MarginCheck                            ; //<1199>
              double ald.Contract     = ald.Size               * avd.NominalPoint      / avd.QuotePoint     ; //<1200>
              double ald.VAR          = avd.QuoteRisk          * ald.Contract                               ; //<1201>
              double ald.Target       = avd.QuoteTarget        * ald.Contract                             ; } //<1202>
//</7.8.3. Operation Size Control 17 >                                                                        //<1203>
                                                                                                              //<1204>
//< 7.8.4. Order Send Trading Function 29 >                                                                   //<1205>
       if   ( avi.SystemFlag         == 1 )                                                                   //<1206>
            {                                                                                                 //<1207>
          //< Trading Function Execution Sequence >                                                           //<1208>
          //< Step 1 >                                                                                        //<1209>
              avs.LocalStamp          = avs.SystemStamp                                 + ": Attempt to " +   //<1210>
                                        acs.Operation      [ avi.Command   ]              + " "           +   //<1211>
                                        DoubleToStr        ( ald.Size      , 2          ) + " "           +   //<1212>
                                        aes.Symbol                                        + " at "        +   //<1213>
                                        DoubleToStr        ( avd.Price     , avi.Digits ) + " sl: "       +   //<1214>
                                        DoubleToStr        ( avd.Stop      , avi.Digits ) + " tp: "       +   //<1215>
                                        DoubleToStr        ( avd.Take      , avi.Digits ) + " //"           ; //<1216>
          //< Step 2 >                                                                                        //<1217>
              Alert                   ( avs.LocalStamp                                    , " Margin: "   ,   //<1218>
                                        DoubleToStr        ( ald.Margin    , 2          ) , " / VAR: -"   ,   //<1219>
                                        DoubleToStr        ( ald.VAR       , 2          ) , " / Target: " ,   //<1220>
                                        DoubleToStr        ( ald.Target    , 2          )                 ) ; //<1221>
          //< Step 3 >                                                                                        //<1222>
              int ali.Ticket          = OrderSend          ( aes.Symbol    , avi.Command  , ald.Size      ,   //<1223>
                                        avd.Price   ,  0   , avd.Stop      , avd.Take     , ""            ,   //<1224>
                                        aei.OrderID ,  0   , 0                                            ) ; //<1225>
          //< Step 4 >                                                                                        //<1226>
              avi.TimeStamp           = TimeLocal    ()                                                     ; //<1227>
                                                                                                              //<1228>
          //< Step 5 >                                                                                        //<1229>
              avi.Exception           = GetLastError ()                                                     ; //<1230>
                                                                                                              //<1231>
          //< Step 6 >                                                                                        //<1232>
              if ( avi.Exception == 0 ) avs.LocalMessage   = " Success "   + ali.Ticket                     ; //<1233>
              else                      avs.LocalMessage   = " Failure "   + avi.Exception                  ; //<1234>
                                                                                                              //<1235>
          //< Step 7 >                                                                                        //<1236>
                                        avi.AttemptsTrade  ++                                               ; //<1237>
              if ( avi.Exception == 0 )                                                                       //<1238>
                 { if ( avi.Command  == OP_BUY             )                                                  //<1239>
                                        avi.BuyTrades      ++                                               ; //<1240>
                   else                 avi.SellTrades     ++                                               ; //<1241>
                                        avi.TotalTrades    ++                                             ; } //<1242>
              else avi.ExcepionsTrade                      ++                                               ; //<1243>
                                                                                                              //<1244>
          //< Step 8 >                                                                                        //<1245>
              Alert                   ( avs.LocalStamp                     + avs.LocalMessage             ) ; //<1246>
                                                                                                              //<1247>
          //< Step 9 >                                                                                        //<1248>
              avs.SystemMessage       = acs.Operation      [ avi.Command ] + avs.LocalMessage               ; //<1249>
                                                                                                              //<1250>
      //</Trading Function Execution Sequence >                                                               //<1251>
            } // if 7.8.4                                                                                     //<1252>
//</7.8.4. Order Send Trading Function 29 >                                                                   //<1253>
                                                                                                              //<1254>
//< 7.8.5. Trading Module Exit Point 1 >                                                                      //<1255>
     } // if 7.8.1                                                                                            //<1256>
//</7.8.5. Trading Module Exit Point 1 >                                                                      //<1257>
                                                                                                              //<1258>
//</7.8. Trading Module 59 >``````````````````````````````````````````````````````````````````````````````````//<1259>
                                                                                                              //<1260>
//< 7.9. Monitoring Module 1 >````````````````````````````````````````````````````````````````````````````````//<1261>
                                                                                                              //<1262>
if   (   avi.LiveModeFlag   == 1 )                                                        afr.Monitoring () ; //<1263>
                                                                                                              //<1264>
//</7.9. Monitoring Module 1 >````````````````````````````````````````````````````````````````````````````````//<1265>
                                                                                                              //<1266>
//< 7.10. System Controls Reset On Exit 2 >```````````````````````````````````````````````````````````````````//<1267>
                                                                                                              //<1268>
avi.TimeLastRun          = TimeLocal    ()                                                                  ; //<1269>
avi.Runs                 ++                                                                                 ; //<1270>
                                                                                                              //<1271>
//</7.10. System Controls Reset On Exit 2 >```````````````````````````````````````````````````````````````````//<1272>
                                                                                                              //<1273>
                                                                                                              //<1274>
avi.Exception = GetLastError () ; if ( avi.Exception != 0 ) Alert ( "avi.Exception: " , avi.Exception ) ;     //<1275>
                                                                                                              //<1276>
}                                                                                                             //<1277>
//</7. Main Program 207 >=====================================================================================//<1278>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////<1279>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////<1280>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////<1281>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////<1282>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////<1283>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////<1284>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////<1285>
//< 8. Extra Code >===========================================================================================//<1286>
                                                                                                              //<1287>
//< A.System.Extra: Structure >-------------------------------------------------------------------------------//<1288>
                                                                                                              //<1289>
//< Processing Module >                                                                                       //<1290>
//  1100 Data                   - pages   contain       - elements // int      - / double     - / string    - //<1291>
//  1200 Functions              - methods contain       - elements // input    - / code       - / output    - //<1292>
//</Processing Module >                                                                                       //<1293>
                                                                                                              //<1294>
//< Control Module >                                                                                          //<1295>
//  2100 Data                   5 pages   contain      34 elements // int      8 / double     2 / string   24 //<1296>
//  2200 Functions             24 methods contain     449 elements // input   18 / code     431 / output    - //<1297>
//</Control Module >                                                                                          //<1298>
                                                                                                              //<1299>
//</A.System.Extra: Structure >-------------------------------------------------------------------------------//<1300>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////<1301>
//< A.System.Extra: Processing Module Structure >-------------------------------------------------------------//<1302>
                                                                                                              //<1303>
//< 1100 Data >                 - pages   contain       - elements // int      - / double     - / string    - //<1304>
//< 1100 Market Profile                                 - elements // int      - / double     - / string    - //<1305>
//< 1100 Profile Registers                              - elements // int      - / double     - / string    - //<1306>
//< 1100 Timeframe Register                             - elements // int      - / double     - / string    - //<1307>
//< 1100 Processing Messages                            - elements // int      - / double     - / string    - //<1308>
//< 1100 Processing Control                             - elements // int      - / double     - / string    - //<1309>
//</1100 Data >                                                                                               //<1310>
                                                                                                              //<1311>
//< 1200 Functions >            - methods contain       - elements // input    - / code       - / output    - //<1312>
//  1201 int    afr.ProcessingInitialization            - elements // input    - / code       - / output    - //<1313>
//  1202 int    afr.ProcessingDeinitialization          - elements // input    - / code       - / output    - //<1314>
//  1202 int    afr.ResetMarketProfiles                 - elements // input    - / code       - / output    - //<1315>
//  1202 int    afr.ResetMarketProfile.3                - elements // input    - / code       - / output    - //<1316>
//  1202 int    afr.ControlDataIntegrity                - elements // input    - / code       - / output    - //<1317>
//  1203 int    afr.ResetTimeframeRegister              - elements // input    - / code       - / output    - //<1318>
//  1204 int    afr.SetTimeframeRegister                - elements // input    - / code       - / output    - //<1319>
//  1205 int    afr.Processing                          - elements // input    - / code       - / output    - //<1320>
//  1206 int    afr.TrailingStrategy                    1 elements // input    - / code       1 / output    - //<1321>
//  1207 int    afr.TradingStrategy                     4 elements // input    - / code       4 / output    - //<1322>
//</1200 Functions >                                                                                          //<1323>
                                                                                                              //<1324>
//</A.System.Extra: Processing Module Structure >-------------------------------------------------------------//<1325>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////<1326>
//< A.System.Extra: Control Module Structure >----------------------------------------------------------------//<1327>
                                                                                                              //<1328>
//< 2100 Data >                 5 pages   contain      35 elements // int     10 / double     2 / string   23 //<1329>
//< 2101 Monitoring Constants                           6 elements // int      2 / double     - / string    4 //<1330>
//< 2102 Monitoring Buffers                             4 elements // int      - / double     - / string    4 //<1331>
//< 2103 Monitoring Preset                             10 elements // int      7 / double     2 / string    1 //<1332>
//< 2104 Monitoring Setup Names                        14 elements // int      - / double     - / string   14 //<1333>
//< 2105 Monitoring Setup Data                          1 elements // int      1 / double     - / string    - //<1334>
//</2100 Data >                                                                                               //<1335>
                                                                                                              //<1336>
//< 2200 Functions >           24 methods contain     450 elements // input   18 / code     432 / output    - //<1337>
//  2201 int    afr.ControlInitialization               3 elements // input    - / code       3 / output    - //<1338>
//  2202 int    afr.ControlDeinitialization             3 elements // input    - / code       3 / output    - //<1339>
//  2203 int    afr.CreateSetup                        47 elements // input    - / code      47 / output    - //<1340>
//  2204 int    afr.DeleteSetup                         1 elements // input    - / code       1 / output    - //<1341>
//  2205 int    afr.ResetSetup                         33 elements // input    - / code      33 / output    - //<1342>
//  2206 int    afr.Monitoring                         21 elements // input    - / code      21 / output    - //<1343>
//  2207 int    afr.CreatePanel.1                       5 elements // input    - / code       5 / output    - //<1344>
//  2208 int    afr.CreatePanel.2                       5 elements // input    - / code       5 / output    - //<1345>
//  2209 int    afr.DeletePanel.1                       3 elements // input    - / code       3 / output    - //<1346>
//  2210 int    afr.DeletePanel.2                       3 elements // input    - / code       3 / output    - //<1347>
//  2211 int    afr.ResetPanel.1                        7 elements // input    - / code       7 / output    - //<1348>
//  2212 int    afr.ResetPanel.2                        7 elements // input    - / code       7 / output    - //<1349>
//  2213 int    afr.Reserved                            0 elements // input    - / code       - / output    - //<1350>
//  2214 int    afr.ResetTextLine.1                     9 elements // input    1 / code       8 / output    - //<1351>
//  2215 int    afr.ResetTextLine.2                     9 elements // input    1 / code       8 / output    - //<1352>
//  2216 int    afr.SetTextLine.1                       4 elements // input    1 / code       3 / output    - //<1353>
//  2217 int    afr.SetTextLine.2                       4 elements // input    1 / code       3 / output    - //<1354>
//  2218 int    afr.SetText.1                          14 elements // input    4 / code      10 / output    - //<1355>
//  2219 int    afr.SetText.2                          14 elements // input    4 / code      10 / output    - //<1356>
//  2220 int    afr.Report.1                          198 elements // input    - / code     198 / output    - //<1357>
//  2221 int    afr.Report.2                           25 elements // input    - / code      25 / output    - //<1358>
//  2222 int    afs.Time                                8 elements // input    2 / code       6 / output    - //<1359>
//  2223 int    afs.Interval                            9 elements // input    2 / code       7 / output    - //<1360>
//  2224 int    afr.CurrencyDetector                   18 elements // input    2 / code      16 / output    - //<1361>
//</2220 Functions >                                                                                          //<1362>
                                                                                                              //<1363>
//</A.System.Extra: Control Module Structure >----------------------------------------------------------------//<1364>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////<1365>
//</A.System.Extra: Processing Module >-----------------------------------------------------------------------//<1366>
                                                                                                              //<1367>
//< A.System.Extra: Control Module Function 1202 >````````````````````````````````````````````````````````````//<1368>
int    afr.TradingDeinitialization    ()          //    0 elements // input    - / code       - / output    - //<1369>
{                                                                                                             //<1370>
}                                                                                                             //<1371>
//</A.System.Extra: Control Module Function 1202 >````````````````````````````````````````````````````````````//<1372>
                                                                                                              //<1373>
//< A.System.Extra: Control Module Function 1201 >````````````````````````````````````````````````````````````//<1374>
int    afr.ProcessingInitialization   ()          //    4 elements // input    - / code       4 / output    - //<1375>
{                                                                                                             //<1376>
afr.ResetMarketProfile.1              ()                                                                    ; //<1377>
afr.ResetMarketProfile.2              ()                                                                    ; //<1378>
afr.ResetMarketProfile.3              ()                                                                    ; //<1379>
afr.ResetMarketZones                  ()                                                                    ; //<1380>
afr.ResetRegisterM1                   ()                                                                    ; //<1381>
afr.ResetRegisterStream               ()                                                                    ; //<1382>
afr.ResetRegisterVolume               ()                                                                    ; //<1383>
afr.ResetRegisterFluctuation          ()                                                                    ; //<1384>
}                                                                                                             //<1385>
//</A.System.Extra: Control Module Function 1201 >````````````````````````````````````````````````````````````//<1386>
                                                                                                              //<1387>
//< A.System.Extra: Control Module Function 1202 >````````````````````````````````````````````````````````````//<1388>
int    afr.ProcessDeinitialization    ()          //    0 elements // input    - / code       - / output    - //<1389>
{                                                                                                             //<1390>
}                                                                                                             //<1391>
//</A.System.Extra: Control Module Function 1202 >````````````````````````````````````````````````````````````//<1392>
                                                                                                              //<1393>
//< A.System.Extra: Processing Module Function 1203 >`````````````````````````````````````````````````````````//<1394>
int    afr.ResetMarketProfile.1       ()          //    - elements // input    - / code       - / output    - //<1395>
{                                                                                                             //<1396>
int       ali.Period            = PERIOD_M1                                                                 ; //<1397>
double    ald.QuotePoint        = MarketInfo     ( aes.Symbol     , MODE_POINT                            ) ; //<1398>
                                                                                                              //<1399>
int       ali.FileHandle        = FileOpen       ( aes.Symbol     + ".CSV" , FILE_CSV | FILE_READ , ";"   ) ; //<1400>
arv.P.1 [ ari.P.TimeBegin     ] =                  FileReadNumber ( ali.FileHandle    )                     ; //<1401>
arv.P.1 [ ari.P.TimeEnd       ] =                  FileReadNumber ( ali.FileHandle    )                     ; //<1402>
arv.P.1 [ ari.P.Frames        ] =                  FileReadNumber ( ali.FileHandle    )                     ; //<1403>
arv.P.1 [ ari.P.PricePoints   ] =                  FileReadNumber ( ali.FileHandle    )                     ; //<1404>
arv.P.1 [ ari.P.MaxRange      ] =                  FileReadNumber ( ali.FileHandle    )                     ; //<1405>
arv.P.1 [ ari.P.MaxVolume     ] =                  FileReadNumber ( ali.FileHandle    )                     ; //<1406>
arv.P.1 [ ari.P.DateMaxRange  ] =                  FileReadNumber ( ali.FileHandle    )                     ; //<1407>
arv.P.1 [ ari.P.DateMaxVolume ] =                  FileReadNumber ( ali.FileHandle    )                     ; //<1408>
arv.P.1 [ ari.P.PriceLowest   ] = MathRound      ( FileReadNumber ( ali.FileHandle    )  / ald.QuotePoint ) ; //<1409>
double    ald.Volume            =                  FileReadNumber ( ali.FileHandle    )                     ; //<1410>
                                                                                                              //<1411>
ArrayResize     ( avd.Profile.1 , arv.P.1        [ ari.P.PricePoints  ]                                   ) ; //<1412>
ArrayInitialize ( avd.Profile.1 , 0                                                                       ) ; //<1413>
                                                                                                              //<1414>
avd.Profile.1             [ 0 ] = ald.Volume                                                                ; //<1415>
                                                                                                              //<1416>
int     i , j           ,     N = arv.P.1        [ ari.P.PricePoints  ]                                     ; //<1417>
for (   i = 1           ; i < N ; i ++                                )                                       //<1418>
    {                                                                                                         //<1419>
                                  FileReadNumber ( ali.FileHandle     )                                     ; //<1420>
        avd.Profile.1  [ i ]    = FileReadNumber ( ali.FileHandle     )                                     ; //<1421>
        ald.Volume              = ald.Volume     + avd.Profile.1  [ i ]                                     ; //<1422>
    }                                                                                                         //<1423>
                                                                                                              //<1424>
FileClose                       ( ali.FileHandle                                                          ) ; //<1425>
                                                                                                              //<1426>
Alert ( avs.SystemStamp , ": External History Maximal M1 Range = " ,    arv.P.1 [ ari.P.MaxRange      ]   ,   //<1427>
                          " / " ,                            afs.Time ( arv.P.1 [ ari.P.DateMaxRange  ] ) ) ; //<1428>
Alert ( avs.SystemStamp , ": External History Maximal M1 Volume = " ,   arv.P.1 [ ari.P.MaxVolume     ]   ,   //<1429>
                          " / " ,                            afs.Time ( arv.P.1 [ ari.P.DateMaxVolume ] ) ) ; //<1430>
                                                                                                              //<1431>
arv.P.1 [ ari.P.Volume        ] = MathRound  ( ald.Volume         )                                         ; //<1432>
arv.P.1 [ ari.P.PriceRange    ] = arv.P.1    [ ari.P.PricePoints  ]   - 1                                   ; //<1433>
arv.P.1 [ ari.P.PriceHighest  ] = arv.P.1    [ ari.P.PriceLowest  ]   + arv.P.1 [ ari.P.PriceRange    ]     ; //<1434>
}                                                                                                             //<1435>
//</A.System.Extra: Processing Module Function 1203 >`````````````````````````````````````````````````````````//<1436>
                                                                                                              //<1437>
//< A.System.Extra: Processing Module Function 1203 >`````````````````````````````````````````````````````````//<1438>
int    afr.ResetMarketProfile.2       ()          //    - elements // input    - / code       - / output    - //<1439>
{                                                                                                             //<1440>
int       ali.Period            = PERIOD_M1                                                                 ; //<1441>
double    ald.QuotePoint        = MarketInfo     ( aes.Symbol          , MODE_POINT                       ) ; //<1442>
                                                                                                              //<1443>
ArrayResize     ( avd.Profile.2 , arv.P.1    [ ari.P.PricePoints  ]                                       ) ; //<1444>
ArrayInitialize ( avd.Profile.2 , 0                                                                       ) ; //<1445>
                                                                                                              //<1446>
arv.P.2 [ ari.P.PricePoints   ] = arv.P.1    [ ari.P.PricePoints  ]                                         ; //<1447>
arv.P.2 [ ari.P.PriceRange    ] = arv.P.1    [ ari.P.PriceRange   ]                                         ; //<1448>
arv.P.2 [ ari.P.PriceLowest   ] = arv.P.1    [ ari.P.PriceLowest  ]                                         ; //<1449>
arv.P.2 [ ari.P.PriceHighest  ] = arv.P.1    [ ari.P.PriceHighest ]                                         ; //<1450>
arv.P.2 [ ari.P.MaxRange      ] = 1                                                                         ; //<1451>
arv.P.2 [ ari.P.MaxVolume     ] = 1                                                                         ; //<1452>
                                                                                                              //<1453>
int       ali.IndexBegin        = iBarShift  ( aes.Symbol , ali.Period , arv.P.1 [ ari.P.TimeEnd ] ) - 1    ; //<1454>
int       ali.IndexEnd          = 1                                                                         ; //<1455>
                                                                                                              //<1456>
if      ( iTime ( aes.Symbol    , ali.Period , ali.IndexBegin + 2 ) == 0 )                                    //<1457>
          ali.IndexEnd          = ali.IndexBegin                                                            ; //<1458>
                                                                                                              //<1459>
if      ( ali.IndexBegin        < ali.IndexEnd                           )                                    //<1460>
          ali.IndexBegin        = ali.IndexEnd                                                              ; //<1461>
                                                                                                              //<1462>
arv.P.2 [ ari.P.TimeBegin     ] = iTime      ( aes.Symbol , ali.Period , ali.IndexBegin                   ) ; //<1463>
arv.P.2 [ ari.P.TimeEnd       ] = iTime      ( aes.Symbol , ali.Period , ali.IndexEnd                     ) ; //<1464>
arv.P.2 [ ari.P.Frames        ] = ali.IndexBegin          - ali.IndexEnd                                    ; //<1465>
arv.P.2 [ ari.P.Volume        ] = 0                                                                         ; //<1466>
                                                                                                              //<1467>
int   i , j                                                                                                 ; //<1468>
for ( i = 1 ; i < arv.P.2 [ ari.P.Frames ] ; i ++ )                                                           //<1469>
    {                                                                                                         //<1470>
      int    ali.IndexHigh      = MathRound ( iHigh   ( aes.Symbol , ali.Period , i ) / ald.QuotePoint )      //<1471>
                                            - arv.P.2 [ ari.P.PriceLowest ]                                 ; //<1472>
                                                                                                              //<1473>
      int    ali.IndexLow       = MathRound ( iLow    ( aes.Symbol , ali.Period , i ) / ald.QuotePoint )      //<1474>
                                            - arv.P.2 [ ari.P.PriceLowest ]                                 ; //<1475>
                                                                                                              //<1476>
      double ald.VolumeFrame    = iVolume             ( aes.Symbol , ali.Period , i )                       ; //<1477>
                                                                                                              //<1478>
      arv.P.2 [ ari.P.Volume  ] = arv.P.2   [ ari.P.Volume ] + ald.VolumeFrame                              ; //<1479>
                                                                                                              //<1480>
      double ald.VolumePoint    = ald.VolumeFrame          / ( ali.IndexHigh - ali.IndexLow + 1 )           ; //<1481>
                                                                                                              //<1482>
      for  ( j = ali.IndexLow   ; j  <= ali.IndexHigh  ; j ++ )                                               //<1483>
                 avd.Profile.2  [ j ] = avd.Profile.2  [ j ] + ald.VolumePoint                              ; //<1484>
                                                                                                              //<1485>
      int    ali.Range          = MathRound ( ( iHigh  ( aes.Symbol , ali.Period , i )                        //<1486>
                                              - iLow   ( aes.Symbol , ali.Period , i ) ) / ald.QuotePoint ) ; //<1487>
                                                                                                              //<1488>
      if   ( arv.P.2 [ ari.P.MaxRange      ] - ali.Range        < 0 )                                         //<1489>
           { arv.P.2 [ ari.P.MaxRange      ] = ali.Range                                                    ; //<1490>
             arv.P.2 [ ari.P.DateMaxRange  ] = iTime   ( aes.Symbol , ali.Period , i )                      ; //<1491>
           }                                                                                                  //<1492>
                                                                                                              //<1493>
      if   ( arv.P.2 [ ari.P.MaxVolume     ] - ald.VolumeFrame  < 0 )                                         //<1494>
           { arv.P.2 [ ari.P.MaxVolume     ] = ald.VolumeFrame                                              ; //<1495>
             arv.P.2 [ ari.P.DateMaxVolume ] = iTime   ( aes.Symbol , ali.Period , i )                      ; //<1496>
           }                                                                                                  //<1497>
    }                                                                                                         //<1498>
                                                                                                              //<1499>
Alert ( avs.SystemStamp , ": Local History Maximal M1 Range = " ,       arv.P.2 [ ari.P.MaxRange      ]   ,   //<1500>
                          " / " ,                            afs.Time ( arv.P.2 [ ari.P.DateMaxRange  ] ) ) ; //<1501>
Alert ( avs.SystemStamp , ": Local History Maximal M1 Volume = " ,      arv.P.2 [ ari.P.MaxVolume     ]   ,   //<1502>
                          " / " ,                            afs.Time ( arv.P.2 [ ari.P.DateMaxVolume ] ) ) ; //<1503>
}                                                                                                             //<1504>
//</A.System.Extra: Processing Module Function 1203 >`````````````````````````````````````````````````````````//<1505>
                                                                                                              //<1506>
//< A.System.Extra: Processing Module Function 1203 >`````````````````````````````````````````````````````````//<1507>
int    afr.ResetMarketProfile.3       ()          //    - elements // input    - / code       - / output    - //<1508>
{                                                                                                             //<1509>
int       ali.Period            = PERIOD_M1                                                                 ; //<1510>
double    ald.QuotePoint        = MarketInfo ( aes.Symbol , MODE_POINT                                    ) ; //<1511>
                                                                                                              //<1512>
ArrayResize     ( avd.Profile.3 , arv.P.2    [ ari.P.PricePoints  ]                                       ) ; //<1513>
ArrayInitialize ( avd.Profile.3 , 0                                                                       ) ; //<1514>
                                                                                                              //<1515>
arv.P.3 [ ari.P.PricePoints   ] = arv.P.2    [ ari.P.PricePoints  ]                                         ; //<1516>
arv.P.3 [ ari.P.PriceRange    ] = arv.P.2    [ ari.P.PriceRange   ]                                         ; //<1517>
arv.P.3 [ ari.P.PriceLowest   ] = arv.P.2    [ ari.P.PriceLowest  ]                                         ; //<1518>
arv.P.3 [ ari.P.PriceHighest  ] = arv.P.2    [ ari.P.PriceHighest ]                                         ; //<1519>
arv.P.3 [ ari.P.MaxRange      ] = 1                                                                         ; //<1520>
arv.P.3 [ ari.P.MaxVolume     ] = 1                                                                         ; //<1521>
                                                                                                              //<1522>
arv.P.3 [ ari.P.TimeBegin     ] = iTime      ( aes.Symbol , ali.Period , 0                                ) ; //<1523>
arv.P.3 [ ari.P.TimeEnd       ] = iTime      ( aes.Symbol , ali.Period , 0                                ) ; //<1524>
arv.P.3 [ ari.P.Frames        ] = 0                                                                         ; //<1525>
arv.P.3 [ ari.P.Volume        ] = 0                                                                         ; //<1526>
                                                                                                              //<1527>
static int ali.Resets         ; ali.Resets ++                                                               ; //<1528>
                                                                                                              //<1529>
avs.SystemMessage             = ": Local History Reset "  + ali.Resets                                      ; //<1530>
Alert   ( avs.SystemStamp     , avs.SystemMessage                                                         ) ; //<1531>
}                                                                                                             //<1532>
//</A.System.Extra: Processing Module Function 1203 >`````````````````````````````````````````````````````````//<1533>
                                                                                                              //<1534>
//< A.System.Extra: Processing Module Function 1203 >`````````````````````````````````````````````````````````//<1535>
int    afr.ResetRegisterM1            ()          //    - elements // input    - / code       - / output    - //<1536>
{                                                                                                             //<1537>
int     ali.Period              = PERIOD_M1                                                                 ; //<1538>
                                                                                                              //<1539>
GlobalVariableSet               ( avs.SetupProfileReset   , EMPTY                                         ) ; //<1540>
arv.1 [ ari.1.Reset           ] =                           EMPTY                                           ; //<1541>
                                                                                                              //<1542>
arv.1 [ ari.1.Period          ] =                           ali.Period                                      ; //<1543>
arv.1 [ ari.1.Length          ] =                           ali.Period * 60                                 ; //<1544>
                                                                                                              //<1545>
arv.1 [ ari.1.Volume.0        ] = iVolume    ( aes.Symbol , ali.Period , 0                                ) ; //<1546>
arv.1 [ ari.1.Volume.1        ] = iVolume    ( aes.Symbol , ali.Period , 1                                ) ; //<1547>
arv.1 [ ari.1.VolumeLast.0    ] = iVolume    ( aes.Symbol , ali.Period , 0                                ) ; //<1548>
arv.1 [ ari.1.VolumeLast.1    ] = iVolume    ( aes.Symbol , ali.Period , 1                                ) ; //<1549>
arv.1 [ ari.1.Time.0          ] = iTime      ( aes.Symbol , ali.Period , 0                                ) ; //<1550>
arv.1 [ ari.1.Time.1          ] = iTime      ( aes.Symbol , ali.Period , 1                                ) ; //<1551>
arv.1 [ ari.1.TimeLast.0      ] = iTime      ( aes.Symbol , ali.Period , 0                                ) ; //<1552>
arv.1 [ ari.1.TimeLast.1      ] = iTime      ( aes.Symbol , ali.Period , 1                                ) ; //<1553>
arv.1 [ ari.1.Range.0         ] = 0                                                                         ; //<1554>
arv.1 [ ari.1.Range.1         ] = 0                                                                         ; //<1555>
                                                                                                              //<1556>
arv.1 [ ari.1.TimeStamp       ] = GetTickCount                                                           () ; //<1557>
}                                                                                                             //<1558>
//</A.System.Extra: Processing Module Function 1203 >`````````````````````````````````````````````````````````//<1559>
                                                                                                              //<1560>
//< A.System.Extra: Processing Module Function 1203 >`````````````````````````````````````````````````````````//<1561>
int    afr.ResetRegisterStream        ()          //    - elements // input    - / code       - / output    - //<1562>
{                                                                                                             //<1563>
double ald.QuoteBid             = MarketInfo ( aes.Symbol   , MODE_BID                                    ) ; //<1564>
double ald.QuotePoint           = MarketInfo ( aes.Symbol   , MODE_POINT                                  ) ; //<1565>
                                                                                                              //<1566>
arv.S.Price  [ 1              ] = MathRound  ( ald.QuoteBid / ald.QuotePoint                              ) ; //<1567>
}                                                                                                             //<1568>
//</A.System.Extra: Processing Module Function 1203 >`````````````````````````````````````````````````````````//<1569>
                                                                                                              //<1570>
//< A.System.Extra: Processing Module Function 1203 >`````````````````````````````````````````````````````````//<1571>
int    afr.ResetRegisterVolume        ()          //    - elements // input    - / code       - / output    - //<1572>
{                                                                                                             //<1573>
for ( int i = 1 ; i < ari.V.RegisterDimension ; i ++ )                                                        //<1574>
      arv.V.Volume        [ i ] = 0                                                                         ; //<1575>
                                                                                                              //<1576>
arv.V.Volume [ ari.V.Total    ] = arv.1 [ ari.1.Volume.0 ]                                                  ; //<1577>
arv.V.Volume [ ari.V.Lost     ] = arv.1 [ ari.1.Volume.0 ]                                                  ; //<1578>
arv.V.Time   [ ari.V.Total    ] = TimeCurrent           ()                                                  ; //<1579>
arv.V.Time   [ ari.V.Lost     ] = TimeCurrent           ()                                                  ; //<1580>
}                                                                                                             //<1581>
//</A.System.Extra: Processing Module Function 1203 >`````````````````````````````````````````````````````````//<1582>
                                                                                                              //<1583>
//< A.System.Extra: Processing Module Function 1203 >`````````````````````````````````````````````````````````//<1584>
int    afr.ResetRegisterFluctuation   ()          //    - elements // input    - / code       - / output    - //<1585>
{                                                                                                             //<1586>
for ( int i = 1 ; i < ari.F.RegisterDimension ; i ++ )                                                        //<1587>
      arv.F.Volume        [ i ] = 0                                                                         ; //<1588>
}                                                                                                             //<1589>
//</A.System.Extra: Processing Module Function 1203 >`````````````````````````````````````````````````````````//<1590>
                                                                                                              //<1591>
//< A.System.Extra: Processing Module Function 1204 >`````````````````````````````````````````````````````````//<1592>
int    afr.SetRegisterM1              ()          //    - elements // input    - / code       - / output    - //<1593>
{                                                                                                             //<1594>
int      ali.Period             = arv.1      [ ari.1.Period            ]                                    ; //<1595>
                                                                                                              //<1596>
//< Begin Of Timeframe Processing >                                                                           //<1597>
arv.1 [ ari.1.Volume.0        ] = iVolume    ( aes.Symbol , ali.Period , 0                                ) ; //<1598>
arv.1 [ ari.1.Volume.1        ] = iVolume    ( aes.Symbol , ali.Period , 1                                ) ; //<1599>
arv.1 [ ari.1.Time.0          ] = iTime      ( aes.Symbol , ali.Period , 0                                ) ; //<1600>
arv.1 [ ari.1.Time.1          ] = iTime      ( aes.Symbol , ali.Period , 1                                ) ; //<1601>
                                                                                                              //<1602>
arv.1 [ ari.1.Range.0         ] = MathRound  ( ( iHigh ( aes.Symbol , ali.Period , 0 )                        //<1603>
                                               - iLow  ( aes.Symbol , ali.Period , 0 ) ) / avd.QuotePoint ) ; //<1604>
arv.1 [ ari.1.Range.1         ] = MathRound  ( ( iHigh ( aes.Symbol , ali.Period , 1 )                        //<1605>
                                               - iLow  ( aes.Symbol , ali.Period , 1 ) ) / avd.QuotePoint ) ; //<1606>
//</Begin Of Timeframe Processing >                                                                           //<1607>
                                                                                                              //<1608>
//< New Ticks And Frames Detection And Computing >                                                            //<1609>
int     ali.FrameDelay             = ( arv.1 [ ari.1.Time.0 ]  - arv.1 [ ari.1.TimeLast.0 ] ) / 60          ; //<1610>
if    ( ali.FrameDelay             > 0 )                                                                      //<1611>
      {                                                                                                       //<1612>
        //< New Frames Computing >                                                                            //<1613>
        int     ali.IndexLast.1    = iBarShift ( aes.Symbol , ali.Period , arv.1 [ ari.1.TimeLast.1 ] , 1 ) ; //<1614>
        arv.1 [ ari.1.NewFrames  ] = ali.IndexLast.1 - 1                                                    ; //<1615>
                                                                                                              //<1616>
        int     ali.VolumePack     = 0                                                                      ; //<1617>
        for   ( int i = 1  ; i    <= arv.1 [ ari.1.NewFrames ] ; i ++   )                                     //<1618>
                ali.VolumePack     = ali.VolumePack            + iVolume ( aes.Symbol , ali.Period , i )    ; //<1619>
                                                                                                              //<1620>
        arv.1 [ ari.1.VolumePack ] = ali.VolumePack            - arv.1   [ ari.1.VolumeLast.0 ]               //<1621>
                                                               + arv.1   [ ari.1.Volume.0     ]             ; //<1622>
        //</New Frames Computing >                                                                            //<1623>
      }                                                                                                       //<1624>
else  {                                                                                                       //<1625>
        //< New Ticks Computing >                                                                             //<1626>
        arv.1 [ ari.1.NewFrames  ] = 0                                                                      ; //<1627>
        arv.1 [ ari.1.VolumePack ] = arv.1 [ ari.1.Volume.0 ]  - arv.1   [ ari.1.VolumeLast.0 ]             ; //<1628>
        //</New Ticks Computing >                                                                             //<1629>
      }                                                                                                       //<1630>
//</New Ticks And Frames Detection And Computing >                                                            //<1631>
                                                                                                              //<1632>
//< M1 Series Computing >                                                                                     //<1633>
arv.1 [ ari.1.Series          ] = 0                                                                         ; //<1634>
                                                                                                              //<1635>
for   ( i = 1 ; arv.1 [ ari.1.Series ] == 0 ; i  ++    )                                                      //<1636>
        if      ( NormalizeDouble ( iHigh ( aes.Symbol , ali.Period , i )                                     //<1637>
                                  + avd.RealSpread     - avd.QuoteBid   , avi.Digits ) < 0 )                  //<1638>
                  arv.1 [ ari.1.Series   ] =  i                                                             ; //<1639>
        else if ( NormalizeDouble ( iLow  ( aes.Symbol , ali.Period , i )                                     //<1640>
                                                       - avd.QuoteAsk   , avi.Digits ) > 0 )                  //<1641>
                  arv.1 [ ari.1.Series   ] = -i                                                             ; //<1642>
        else      continue                                                                                  ; //<1643>
//</M1 Series Computing >                                                                                     //<1644>
                                                                                                              //<1645>
//< End Of Timeframe Processing >                                                                             //<1646>
arv.1 [ ari.1.VolumeLast.0    ] = arv.1      [ ari.1.Volume.0           ]                                   ; //<1647>
arv.1 [ ari.1.VolumeLast.1    ] = arv.1      [ ari.1.Volume.1           ]                                   ; //<1648>
arv.1 [ ari.1.TimeLast.0      ] = iTime      ( aes.Symbol , ali.Period  , 0                               ) ; //<1649>
arv.1 [ ari.1.TimeLast.1      ] = iTime      ( aes.Symbol , ali.Period  , 1                               ) ; //<1650>
                                                                                                              //<1651>
arv.1 [ ari.1.Length.0        ] = TimeCurrent  ()   - arv.1 [ ari.1.Time.0    ]                             ; //<1652>
arv.1 [ ari.1.Completeness.0  ] = MathRound ( 100.0 * arv.1 [ ari.1.Length.0  ] / arv.1 [ ari.1.Length ]  ) ; //<1653>
arv.1 [ ari.1.TimeLatency     ] = GetTickCount ()   - arv.1 [ ari.1.TimeStamp ]                             ; //<1654>
arv.1 [ ari.1.TimeStamp       ] = GetTickCount ()                                                           ; //<1655>
//</End Of Timeframe Processing >                                                                             //<1656>
}                                                                                                             //<1657>
//</A.System.Extra: Processing Module Function 1204 >`````````````````````````````````````````````````````````//<1658>
                                                                                                              //<1659>
//< A.System.Extra: Processing Module Function 1204 >`````````````````````````````````````````````````````````//<1660>
int    afr.SetRegisterStream          ()          //    - elements // input    - / code       - / output    - //<1661>
{                                                                                                             //<1662>
        arv.S.Price                 [ 0 ] = MathRound ( avd.QuoteBid  / avd.QuotePoint                    ) ; //<1663>
        arv.S.Volume                [ 0 ] = arv.1 [ ari.1.VolumePack  ]                                     ; //<1664>
        arv.S.Latency               [ 0 ] = arv.1 [ ari.1.TimeLatency ]                                     ; //<1665>
        arv.S.Fluctuation           [ 0 ] = arv.S.Price [ 0 ]         - arv.S.Price [ 1 ]                   ; //<1666>
        arv.S.TotalVolume           [ 0 ] = 0                                                               ; //<1667>
        arv.S.TotalLatency          [ 0 ] = 0                                                               ; //<1668>
        arv.S.TotalFluctuation      [ 0 ] = 0                                                               ; //<1669>
                                                                                                              //<1670>
int i , N = ari.S.RegisterDimension   - 1                                                                   ; //<1671>
for   ( i = N - 1 ; i >= 0          ; i  -- )                                                                 //<1672>
      {                                                                                                       //<1673>
        arv.S.TotalVolume           [ 0 ] = arv.S.TotalVolume           [ 0     ] + arv.S.Volume      [ i ] ; //<1674>
        arv.S.TotalLatency          [ 0 ] = arv.S.TotalLatency          [ 0     ] + arv.S.Latency     [ i ] ; //<1675>
        arv.S.TotalFluctuation      [ 0 ] = arv.S.TotalFluctuation      [ 0     ] + arv.S.Fluctuation [ i ] ; //<1676>
      }                                                                                                       //<1677>
                                                                                                              //<1678>
for   ( i = N ; i > 0               ; i  -- )                                                                 //<1679>
      {                                                                                                       //<1680>
        arv.S.Price                 [ i ] = arv.S.Price                 [ i - 1 ]                           ; //<1681>
        arv.S.Volume                [ i ] = arv.S.Volume                [ i - 1 ]                           ; //<1682>
        arv.S.Latency               [ i ] = arv.S.Latency               [ i - 1 ]                           ; //<1683>
        arv.S.Fluctuation           [ i ] = arv.S.Fluctuation           [ i - 1 ]                           ; //<1684>
        arv.S.TotalVolume           [ i ] = arv.S.TotalVolume           [ i - 1 ]                           ; //<1685>
        arv.S.TotalLatency          [ i ] = arv.S.TotalLatency          [ i - 1 ]                           ; //<1686>
        arv.S.TotalFluctuation      [ i ] = arv.S.TotalFluctuation      [ i - 1 ]                           ; //<1687>
      }                                                                                                       //<1688>
                                                                                                              //<1689>
//< Fluctuation Sequence Length Computing >                                                                   //<1690>
arv.S.Fluctuation                   [ 0 ] = 0                                                               ; //<1691>
arv.S.TotalFluctuation              [ 0 ] = 0                                                               ; //<1692>
                                                                                                              //<1693>
                i                                 = 1                                                       ; //<1694>
if    ( arv.S.Fluctuation                   [ 1 ] > 0          )                                              //<1695>
        while ( arv.S.Fluctuation           [ i ] > 0 && i < N )                                              //<1696>
              { arv.S.Fluctuation           [ 0 ] ++                                                        ; //<1697>
                i                                 ++                                                        ; //<1698>
              }                                                                                               //<1699>
else    while ( arv.S.Fluctuation           [ i ] < 0 && i < N )                                              //<1700>
              { arv.S.Fluctuation           [ 0 ] --                                                        ; //<1701>
                i                                 ++                                                        ; //<1702>
              }                                                                                               //<1703>
                                                                                                              //<1704>
                i                                 = 1                                                       ; //<1705>
if    ( arv.S.TotalFluctuation              [ 1 ] > 0          )                                              //<1706>
        while ( arv.S.TotalFluctuation      [ i ] > 0 && i < N )                                              //<1707>
              { arv.S.TotalFluctuation      [ 0 ] ++                                                        ; //<1708>
                i                                 ++                                                        ; //<1709>
              }                                                                                               //<1710>
else    while ( arv.S.TotalFluctuation      [ i ] < 0 && i < N )                                              //<1711>
              { arv.S.TotalFluctuation      [ 0 ] --                                                        ; //<1712>
                i                                 ++                                                        ; //<1713>
              }                                                                                               //<1714>
//</Fluctuation Sequence Length Computing >                                                                   //<1715>
}                                                                                                             //<1716>
//</A.System.Extra: Processing Module Function 1204 >`````````````````````````````````````````````````````````//<1717>
                                                                                                              //<1718>
//< A.System.Extra: Processing Module Function 1204 >`````````````````````````````````````````````````````````//<1719>
int    afr.SetRegisterVolume          ()          //    - elements // input    - / code       - / output    - //<1720>
{                                                                                                             //<1721>
arv.V.Volume       [ ari.V.Total ]  = arv.V.Volume [ ari.V.Total ] + arv.1  [ ari.1.VolumePack  ]           ; //<1722>
arv.V.Time         [ ari.V.Total ]  = TimeCurrent ()                                                        ; //<1723>
                                                                                                              //<1724>
if ( arv.1 [ ari.1.VolumePack    ] >= ari.V.RegisterDimension    )                                            //<1725>
   { arv.V.Volume  [ ari.V.Lost  ]  = arv.V.Volume [ ari.V.Lost  ] + arv.1  [ ari.1.VolumePack  ]           ; //<1726>
     arv.V.Time    [ ari.V.Lost  ]  = TimeCurrent ()                                                        ; //<1727>
     arv.V.Latency [ ari.V.Lost  ]  =                                arv.1  [ ari.1.TimeLatency ]           ; //<1728>
   }                                                                                                          //<1729>
else for ( int i = 1 ; i < ari.V.RegisterDimension ; i ++ )                                                   //<1730>
      if ( arv.1   [ ari.1.VolumePack ] == i )                                                                //<1731>
         { arv.V.Volume    [ i ]  ++                                                                        ; //<1732>
           arv.V.Time      [ i ]  = TimeCurrent ()                                                          ; //<1733>
           arv.V.Latency   [ i ]  = arv.1 [ ari.1.TimeLatency ]                                             ; //<1734>
           break                                                                                            ; //<1735>
         }                                                                                                    //<1736>
}                                                                                                             //<1737>
//</A.System.Extra: Processing Module Function 1204 >`````````````````````````````````````````````````````````//<1738>
                                                                                                              //<1739>
//< A.System.Extra: Processing Module Function 1204 >`````````````````````````````````````````````````````````//<1740>
int    afr.SetRegisterFluctuation     ()          //    - elements // input    - / code       - / output    - //<1741>
{                                                                                                             //<1742>
if ( arv.1   [ ari.1.VolumePack ]          < ari.V.RegisterDimension   )                                      //<1743>
   { if      ( arv.S.Fluctuation     [ 1 ] > 0 )                                                              //<1744>
               arv.F.Volume [ ari.F.Up   ] = arv.F.Volume [ ari.F.Up   ] + arv.1 [ ari.1.VolumePack  ]      ; //<1745>
     else if ( arv.S.Fluctuation     [ 1 ] < 0 )                                                              //<1746>
               arv.F.Volume [ ari.F.Down ] = arv.F.Volume [ ari.F.Down ] + arv.1 [ ari.1.VolumePack  ]      ; //<1747>
     else                                                                                                     //<1748>
               arv.F.Volume [ ari.F.Zero ] = arv.F.Volume [ ari.F.Zero ] + arv.1 [ ari.1.VolumePack  ]      ; //<1749>
   } // else                                                                                                  //<1750>
}                                                                                                             //<1751>
//</A.System.Extra: Processing Module Function 1204 >`````````````````````````````````````````````````````````//<1752>
                                                                                                              //<1753>
//< A.System.Extra: Processing Module Function 1203 >`````````````````````````````````````````````````````````//<1754>
int    afr.SetMarketProfiles          ()          //    - elements // input    - / code       - / output    - //<1755>
{                                                                                                             //<1756>
//< Local Variables Declaration >                                                                             //<1757>
int       ali.Period            = PERIOD_M1                                                                 ; //<1758>
//</Local Variables Declaration >                                                                             //<1759>
                                                                                                              //<1760>
//< Set Profile 3 >                                                                                           //<1761>
int   i , j , N  = arv.1 [ ari.1.NewFrames ]                                                                ; //<1762>
for ( i = 1 ; i <= N ; i ++                )                                                                  //<1763>
    {                                                                                                         //<1764>
      int    ali.IndexHigh      = MathRound   ( iHigh   ( aes.Symbol , ali.Period , i ) / avd.QuotePoint )    //<1765>
                                              - arv.P.3 [ ari.P.PriceLowest ]                               ; //<1766>
      int    ali.IndexLow       = MathRound   ( iLow    ( aes.Symbol , ali.Period , i ) / avd.QuotePoint )    //<1767>
                                              - arv.P.3 [ ari.P.PriceLowest ]                               ; //<1768>
      double ald.VolumeFrame    = iVolume               ( aes.Symbol , ali.Period , i )                     ; //<1769>
                                                                                                              //<1770>
      arv.P.3 [ ari.P.Volume  ] = ald.VolumeFrame       + arv.P.3          [ ari.P.Volume     ]             ; //<1771>
                                                                                                              //<1772>
      double ald.VolumePoint    = ald.VolumeFrame     / ( ali.IndexHigh    - ali.IndexLow + 1 )             ; //<1773>
                                                                                                              //<1774>
      for  ( j = ali.IndexLow   ; j  <= ali.IndexHigh   ; j ++ )                                              //<1775>
                 avd.Profile.3  [ j ] = avd.Profile.3   [ j ]  + ald.VolumePoint                            ; //<1776>
                                                                                                              //<1777>
      int    ali.Range          = MathRound ( ( iHigh  ( aes.Symbol , ali.Period , i )                        //<1778>
                                              - iLow   ( aes.Symbol , ali.Period , i ) ) / avd.QuotePoint ) ; //<1779>
                                                                                                              //<1780>
      if   ( arv.P.3 [ ari.P.MaxRange      ] - ali.Range       < 0 )                                          //<1781>
           { arv.P.3 [ ari.P.MaxRange      ] = ali.Range                                                    ; //<1782>
             arv.P.3 [ ari.P.DateMaxRange  ] = iTime  ( aes.Symbol , ali.Period , i )                       ; //<1783>
             static    int ali.MaxRanges     ; ali.MaxRanges  ++                                            ; //<1784>
             Print   ( avs.SystemStamp       , ": Maximal Range "  , ali.MaxRanges                , " = " ,   //<1785>
                                                          arv.P.3  [ ari.P.MaxRange      ]        , " / " ,   //<1786>
                                               afs.Time ( arv.P.3  [ ari.P.DateMaxRange  ] )              ) ; //<1787>
           }                                                                                                  //<1788>
                                                                                                              //<1789>
      if   ( arv.P.3 [ ari.P.MaxVolume     ] - ald.VolumeFrame < 0 )                                          //<1790>
           { arv.P.3 [ ari.P.MaxVolume     ] = ald.VolumeFrame                                              ; //<1791>
             arv.P.3 [ ari.P.DateMaxVolume ] = iTime ( aes.Symbol  , ali.Period , i )                       ; //<1792>
             static    int ali.MaxVolumes    ; ali.MaxVolumes ++                                            ; //<1793>
             Print   ( avs.SystemStamp       , ": Maximal Volume " , ali.MaxVolumes               , " = " ,   //<1794>
                                                          arv.P.3  [ ari.P.MaxVolume     ]        , " / " ,   //<1795>
                                               afs.Time ( arv.P.3  [ ari.P.DateMaxVolume ] )              ) ; //<1796>
           }                                                                                                  //<1797>
    }                                                                                                         //<1798>
                                                                                                              //<1799>
arv.P.3 [ ari.P.TimeEnd       ] = iTime      ( aes.Symbol     , ali.Period , 1                            ) ; //<1800>
arv.P.3 [ ari.P.Frames        ] = arv.P.3    [ ari.P.Frames ] + arv.1 [ ari.1.NewFrames ]                   ; //<1801>
//</Set Profile 3 >                                                                                           //<1802>
}                                                                                                             //<1803>
//</A.System.Extra: Processing Module Function 1203 >`````````````````````````````````````````````````````````//<1804>
                                                                                                              //<1805>
//< A.System.Extra: Processing Module Function 1203 >`````````````````````````````````````````````````````````//<1806>
int    afr.ResetMarketZones           ()          //    - elements // input    - / code       - / output    - //<1807>
{                                                                                                             //<1808>
//< Zones Preparation >                                                                                       //<1809>
double  ald.QuotePoint          = MarketInfo ( aes.Symbol , MODE_POINT                                    ) ; //<1810>
                                                                                                              //<1811>
if    ( avi.ZoneStepPoints < 1  )                                                                             //<1812>
        avi.ZoneStepPoints      = MathRound   ( 0.001 / ald.QuotePoint                                    ) ; //<1813>
                                                                                                              //<1814>
GlobalVariableSet               ( avs.SetupZoneReset      , EMPTY                                         ) ; //<1815>
                                                                                                              //<1816>
arv.Z [ ari.Z.Reset           ] = EMPTY                                                                     ; //<1817>
arv.Z [ ari.Z.Set             ] = 1                                                                         ; //<1818>
                                                                                                              //<1819>
arv.Z [ ari.Z.BasePoints      ] = avi.ZoneBasePoints                                                        ; //<1820>
arv.Z [ ari.Z.StepPoints      ] = avi.ZoneStepPoints                                                        ; //<1821>
arv.Z [ ari.Z.PriceHighest    ] = arv.P.3 [ ari.P.PriceHighest ]                                            ; //<1822>
arv.Z [ ari.Z.PriceLowest     ] = arv.P.3 [ ari.P.PriceLowest  ]                                            ; //<1823>
                                                                                                              //<1824>
int     ali.IndexHighest        = MathFloor   ( 1.0                                                           //<1825>
                                  * ( arv.Z   [ ari.Z.PriceHighest ] - arv.Z [ ari.Z.BasePoints ] )           //<1826>
                                  /   arv.Z   [ ari.Z.StepPoints   ]                                      ) ; //<1827>
                                                                                                              //<1828>
int     ali.IndexLowest         = MathFloor   ( 1.0                                                           //<1829>
                                  * ( arv.Z   [ ari.Z.PriceLowest  ] - arv.Z [ ari.Z.BasePoints ] )           //<1830>
                                  /   arv.Z   [ ari.Z.StepPoints   ]                                      ) ; //<1831>
                                                                                                              //<1832>
arv.Z [ ari.Z.IndexShift      ] = ali.IndexLowest                                                           ; //<1833>
arv.Z [ ari.Z.IndexRange      ] = ali.IndexHighest - ali.IndexLowest + 1                                    ; //<1834>
                                                                                                              //<1835>
ArrayResize     ( avd.Zone.0    , arv.Z [ ari.Z.IndexRange ]                                              ) ; //<1836>
ArrayResize     ( avd.Zone.1    , arv.Z [ ari.Z.IndexRange ]                                              ) ; //<1837>
ArrayInitialize ( avd.Zone.0    , 0                                                                       ) ; //<1838>
ArrayInitialize ( avd.Zone.1    , 0                                                                       ) ; //<1839>
//</Zones Preparation >                                                                                       //<1840>
                                                                                                              //<1841>
//< Main Processing >                                                                                         //<1842>
int    ali.PriceShift           , ali.PriceRange                                                            ; //<1843>
                                                                                                              //<1844>
double ald.Volume.0             = 0                                                                         ; //<1845>
double ald.Volume.1             = 0                                                                         ; //<1846>
int   i , j ,      N            = arv.Z   [ ari.Z.IndexRange  ] - 1                                         ; //<1847>
for ( i = 0 ; i <= N ; i ++ )                                                                                 //<1848>
    {                                                                                                         //<1849>
      if      ( i == 0 )                                                                                      //<1850>
              { ali.PriceShift  = 0                                                                         ; //<1851>
                                                                                                              //<1852>
                ali.PriceRange  = arv.Z   [ ari.Z.BasePoints   ]                                              //<1853>
                                + arv.Z   [ ari.Z.StepPoints   ] * ( arv.Z [ ari.Z.IndexShift ] + 1 )         //<1854>
                                - arv.Z   [ ari.Z.PriceLowest  ]                                            ; //<1855>
              }                                                                                               //<1856>
      else if ( i == N )                                                                                      //<1857>
              { ali.PriceShift  = arv.Z   [ ari.Z.BasePoints   ]                                              //<1858>
                                + arv.Z   [ ari.Z.StepPoints   ] * ( arv.Z [ ari.Z.IndexShift ] + i )         //<1859>
                                - arv.Z   [ ari.Z.PriceLowest  ]                                            ; //<1860>
                                                                                                              //<1861>
                ali.PriceRange  = arv.Z   [ ari.Z.PriceHighest ]                                              //<1862>
                                - arv.Z   [ ari.Z.BasePoints   ]                                              //<1863>
                                - arv.Z   [ ari.Z.StepPoints   ] * ( arv.Z [ ari.Z.IndexShift ] + i )       ; //<1864>
              }                                                                                               //<1865>
      else                                                                                                    //<1866>
              { ali.PriceShift  = arv.Z   [ ari.Z.BasePoints   ]                                              //<1867>
                                + arv.Z   [ ari.Z.StepPoints   ] * ( arv.Z [ ari.Z.IndexShift ] + i )         //<1868>
                                - arv.Z   [ ari.Z.PriceLowest  ]                                            ; //<1869>
                                                                                                              //<1870>
                ali.PriceRange  = arv.Z   [ ari.Z.StepPoints   ]                                            ; //<1871>
              }                                                                                               //<1872>
                                                                                                              //<1873>
      for ( j = 0 ;         j   < ali.PriceRange ; j ++ )                                                     //<1874>
          {                                                                                                   //<1875>
            avd.Zone.0    [ i ] = avd.Zone.0 [ i ]   + avd.Profile.2 [ ali.PriceShift + j ]                 ; //<1876>
            avd.Zone.1    [ i ] = avd.Zone.1 [ i ]   + avd.Profile.2 [ ali.PriceShift + j ]                   //<1877>
                                                     + avd.Profile.1 [ ali.PriceShift + j ]                 ; //<1878>
          }                                                                                                   //<1879>
                                                                                                              //<1880>
       ald.Volume.0             = ald.Volume.0       + avd.Zone.0    [ i ]                                  ; //<1881>
       ald.Volume.1             = ald.Volume.1       + avd.Zone.1    [ i ]                                  ; //<1882>
     }                                                                                                        //<1883>
//</Main Processing >                                                                                         //<1884>
                                                                                                              //<1885>
//< Check Processing >                                                                                        //<1886>
double ald.VolumeCheck.0        = 0                                                                         ; //<1887>
double ald.VolumeCheck.1        = 0                                                                         ; //<1888>
for  ( j = 0 ;              j   < arv.P.2 [ ari.P.PriceRange ]       ; j ++ )                                 //<1889>
     { ald.VolumeCheck.0        = ald.VolumeCheck.0 + avd.Profile.2  [ j ]                                  ; //<1890>
       ald.VolumeCheck.1        = ald.VolumeCheck.1 + avd.Profile.2  [ j ]                                    //<1891>
                                                    + avd.Profile.1  [ j ]                                  ; //<1892>
     }                                                                                                        //<1893>
//</Check Processing >                                                                                        //<1894>
                                                                                                              //<1895>
//< Reset Peport >                                                                                            //<1896>
static int ali.Resets           ; ali.Resets ++                                                             ; //<1897>
                                                                                                              //<1898>
avs.SystemMessage               = ": Market Zones Reset " + ali.Resets  + " " +                               //<1899>
                                  arv.Z       [ ari.Z.BasePoints ]      + "/" +                               //<1900>
                                  arv.Z       [ ari.Z.StepPoints ]      + " " +                               //<1901>
                                  arv.Z       [ ari.Z.IndexShift ]      + "/" +                               //<1902>
                                  arv.Z       [ ari.Z.IndexRange ]      + " " +                               //<1903>
                                  DoubleToStr ( ald.Volume.1      , 0 ) + "/" +                               //<1904>
                                  DoubleToStr ( ald.VolumeCheck.1 , 0 ) + " " +                               //<1905>
                                  DoubleToStr ( ald.Volume.0      , 0 ) + "/" +                               //<1906>
                                  DoubleToStr ( ald.VolumeCheck.0 , 0 )                                     ; //<1907>
                                                                                                              //<1908>
Alert   ( avs.SystemStamp       , avs.SystemMessage                                                       ) ; //<1909>
//</Reset Peport >                                                                                            //<1910>
}                                                                                                             //<1911>
//</A.System.Extra: Processing Module Function 1203 >`````````````````````````````````````````````````````````//<1912>
                                                                                                              //<1913>
//< A.System.Extra: Processing Module Function 1203 >`````````````````````````````````````````````````````````//<1914>
int    afr.SetMarketZones             ()          //    - elements // input    - / code       - / output    - //<1915>
{                                                                                                             //<1916>
                                                                                                              //<1917>
//< Every Tick Processing >                                                                                   //<1918>
arv.Z [ ari.Z.IndexCurrent    ] = MathFloor ( ( avd.QuoteBid / avd.QuotePoint - arv.Z [ ari.Z.BasePoints ] )  //<1919>
                                              / arv.Z        [ ari.Z.StepPoints ]                             //<1920>
                                              - arv.Z        [ ari.Z.IndexShift ]                         ) ; //<1921>
//</Every Tick Processing >                                                                                   //<1922>
                                                                                                              //<1923>
//< New M1 Frame / Post Reset Processing >                                                                    //<1924>
if ( arv.1 [ ari.1.NewFrames  ]  > 0                                                                          //<1925>
  || arv.Z [ ari.Z.Set        ] == 1 )                                                                        //<1926>
   { arv.Z [ ari.Z.Set        ]  = 0                                                                        ; //<1927>
                                                                                                              //<1928>
     //< Clear Profile Plotter >                                                                              //<1929>
     afr.DeleteZoneLevels                                                                                () ; //<1930>
     afr.DeleteProfiles                                                                                  () ; //<1931>
     //</Clear Profile Plotter >                                                                              //<1932>
                                                                                                              //<1933>
     //< Chart Price Metrics >                                                                                //<1934>
     static int        ali.PriceChartMax                                                                    ; //<1935>
     static int        ali.PriceChartMin                                                                    ; //<1936>
                                                                                                              //<1937>
     static int        ali.PriceHighest                                                                     ; //<1938>
     static int        ali.PriceLowest                                                                      ; //<1939>
                                                                                                              //<1940>
     ali.PriceChartMax                = MathRound ( WindowPriceMax  () / avd.QuotePoint                   ) ; //<1941>
     ali.PriceChartMin                = MathRound ( WindowPriceMin  () / avd.QuotePoint                   ) ; //<1942>
                                                                                                              //<1943>
     if ( arv.Z [ ari.Z.PriceHighest  ] > ali.PriceChartMax          )                                        //<1944>
          ali.PriceHighest            =   ali.PriceChartMax                                                 ; //<1945>
     else ali.PriceHighest            =   arv.Z [ ari.Z.PriceHighest ]                                      ; //<1946>
                                                                                                              //<1947>
     if ( arv.Z [ ari.Z.PriceLowest   ] < ali.PriceChartMin          )                                        //<1948>
          ali.PriceLowest             =   ali.PriceChartMin                                                 ; //<1949>
     else ali.PriceLowest             =   arv.Z [ ari.Z.PriceLowest  ]                                      ; //<1950>
                                                                                                              //<1951>
     arv.Z [ ari.Z.IndexChartHighest ] = MathFloor ( ( ali.PriceHighest - arv.Z   [ ari.Z.BasePoints ] )      //<1952>
                                                     / arv.Z [ ari.Z.StepPoints ] )                           //<1953>
                                                     - arv.Z [ ari.Z.IndexShift ]                           ; //<1954>
                                                                                                              //<1955>
     arv.Z [ ari.Z.IndexChartLowest  ] = MathFloor ( ( ali.PriceLowest  - arv.Z   [ ari.Z.BasePoints ] )      //<1956>
                                                     / arv.Z [ ari.Z.StepPoints ] )                           //<1957>
                                                     - arv.Z [ ari.Z.IndexShift ]                           ; //<1958>
                                                                                                              //<1959>
     arv.Z [ ari.Z.IndexChartRange  ] = arv.Z [ ari.Z.IndexChartHighest ]                                     //<1960>
                                      - arv.Z [ ari.Z.IndexChartLowest  ] + 1                               ; //<1961>
     //</Chart Price Metrics >                                                                                //<1962>
                                                                                                              //<1963>
     //< Volume Metrics >                                                                                     //<1964>
     double  ald.Volume.1             = 0                                                                   ; //<1965>
     double  ald.Volume.0             = 0                                                                   ; //<1966>
     for   ( int i = 0 ; i < arv.Z [ ari.Z.IndexRange ] ; i ++ )                                              //<1967>
           {                                                                                                  //<1968>
             ald.Volume.1             = ald.Volume.1 + avd.Zone.1 [ i ]                                     ; //<1969>
             ald.Volume.0             = ald.Volume.0 + avd.Zone.0 [ i ]                                     ; //<1970>
           }                                                                                                  //<1971>
     arv.Z [ ari.Z.Volume.1         ] = MathRound    ( ald.Volume.1                                       ) ; //<1972>
     arv.Z [ ari.Z.Volume.0         ] = MathRound    ( ald.Volume.0                                       ) ; //<1973>
     arv.Z [ ari.Z.VolumeMean.1     ] = MathRound    ( ald.Volume.1 / arv.Z [ ari.Z.IndexRange  ]         ) ; //<1974>
     arv.Z [ ari.Z.VolumeMean.0     ] = MathRound    ( ald.Volume.0 / arv.Z [ ari.Z.IndexRange  ]         ) ; //<1975>
                                                                                                              //<1976>
             ald.Volume.1             = 0                                                                   ; //<1977>
             ald.Volume.0             = 0                                                                   ; //<1978>
     for   ( i = arv.Z [ ari.Z.IndexChartLowest ]     ; i <= arv.Z [ ari.Z.IndexChartHighest ] ; i ++ )       //<1979>
           {                                                                                                  //<1980>
             ald.Volume.1             = ald.Volume.1  + avd.Zone.1 [ i ]                                    ; //<1981>
             ald.Volume.0             = ald.Volume.0  + avd.Zone.0 [ i ]                                    ; //<1982>
           }                                                                                                  //<1983>
     arv.Z [ ari.Z.VolumeChart.1     ] = MathRound ( ald.Volume.1                                         ) ; //<1984>
     arv.Z [ ari.Z.VolumeChart.0     ] = MathRound ( ald.Volume.0                                         ) ; //<1985>
     arv.Z [ ari.Z.VolumeChartMean.1 ] = MathRound ( ald.Volume.1 / arv.Z [ ari.Z.IndexChartRange ]       ) ; //<1986>
     arv.Z [ ari.Z.VolumeChartMean.0 ] = MathRound ( ald.Volume.0 / arv.Z [ ari.Z.IndexChartRange ]       ) ; //<1987>
                                                                                                              //<1988>
     arv.Z [ ari.Z.IndexMax.1  ]      = ArrayMaximum ( avd.Zone.1                                         ) ; //<1989>
     arv.Z [ ari.Z.IndexMax.0  ]      = ArrayMaximum ( avd.Zone.0                                         ) ; //<1990>
     arv.Z [ ari.Z.VolumeMax.1 ]      = MathRound    ( avd.Zone.1 [ arv.Z [ ari.Z.IndexMax.1      ] ]     ) ; //<1991>
     arv.Z [ ari.Z.VolumeMax.0 ]      = MathRound    ( avd.Zone.0 [ arv.Z [ ari.Z.IndexMax.0      ] ]     ) ; //<1992>
                                                                                                              //<1993>
     arv.Z [ ari.Z.IndexChartMax.1  ] = ArrayMaximum ( avd.Zone.1 , arv.Z [ ari.Z.IndexChartRange  ] ,        //<1994>
                                                                    arv.Z [ ari.Z.IndexChartLowest ]      ) ; //<1995>
     arv.Z [ ari.Z.IndexChartMax.0  ] = ArrayMaximum ( avd.Zone.0 , arv.Z [ ari.Z.IndexChartRange  ] ,        //<1996>
                                                                    arv.Z [ ari.Z.IndexChartLowest ]      ) ; //<1997>
     arv.Z [ ari.Z.VolumeChartMax.1 ] = MathRound    ( avd.Zone.1 [ arv.Z [ ari.Z.IndexChartMax.1  ] ]    ) ; //<1998>
     arv.Z [ ari.Z.VolumeChartMax.0 ] = MathRound    ( avd.Zone.0 [ arv.Z [ ari.Z.IndexChartMax.0  ] ]    ) ; //<1999>
     //</Volume Metrics >                                                                                     //<2000>
                                                                                                              //<2001>
     //< Run Profile Plotter >                                                                                //<2002>
     if ( avi.PlotZoneLevels > 0 )                                                                            //<2003>
          afr.PlotZoneLevels                                                                             () ; //<2004>
                                                                                                              //<2005>
     if ( avi.PlotProfiles   > 0 )                                                                            //<2006>
          afr.PlotProfiles                                                                               () ; //<2007>
     //</Run Profile Plotter >                                                                                //<2008>
   }                                                                                                          //<2009>
//</New M1 Frame / Post Reset Processing >                                                                    //<2010>
}                                                                                                             //<2011>
//</A.System.Extra: Processing Module Function 1203 >`````````````````````````````````````````````````````````//<2012>
                                                                                                              //<2013>
//< A.System.Extra: Processing Module Function 1206 >`````````````````````````````````````````````````````````//<2014>
int    afr.Processing                 ()          //    - elements // input    - / code       - / output    - //<2015>
{                                                                                                             //<2016>
avi.ProcessingRunTime                 = GetTickCount ()                                                     ; //<2017>
avs.ProcessingMessage                 = ""                                                                  ; //<2018>
                                                                                                              //<2019>
afr.ResetSetup                        ()                                                                    ; //<2020>
                                                                                                              //<2021>
if ( arv.Z [ ari.Z.Reset   ]          > EMPTY )                                                               //<2022>
     afr.ResetMarketZones             ()                                                                    ; //<2023>
                                                                                                              //<2024>
if ( arv.1 [ ari.1.Reset   ]          > EMPTY )                                                               //<2025>
     afr.ProcessingInitialization     ()                                                                    ; //<2026>
else                                                                                                          //<2027>
   { afr.SetRegisterM1                ()                                                                    ; //<2028>
     afr.SetRegisterStream            ()                                                                    ; //<2029>
     afr.SetRegisterVolume            ()                                                                    ; //<2030>
     afr.SetRegisterFluctuation       ()                                                                    ; //<2031>
     afr.SetMarketProfiles            ()                                                                    ; //<2032>
     afr.SetMarketZones               ()                                                                    ; //<2033>
   }                                                                                                          //<2034>
                                                                                                              //<2035>
avi.ProcessingRunTime                 = GetTickCount ()       - avi.ProcessingRunTime                       ; //<2036>
}                                                                                                             //<2037>
//</A.System.Extra: Processing Module Function 1206 >`````````````````````````````````````````````````````````//<2038>
                                                                                                              //<2039>
//< A.System.Extra: Processing Module Function 1207 >`````````````````````````````````````````````````````````//<2040>
int    afr.TrailingStrategy           ()          //    1 elements // input    - / code       1 / output    - //<2041>
{                                                                                                             //<2042>
avd.Stop     = EMPTY                                                                                        ; //<2043>
}                                                                                                             //<2044>
//</A.System.Extra: Processing Module Function 1207 >`````````````````````````````````````````````````````````//<2045>
                                                                                                              //<2046>
//< A.System.Extra: Processing Module Function 1208 >`````````````````````````````````````````````````````````//<2047>
int    afr.TradingStrategy            ()          //    4 elements // input    - / code     422 / output    - //<2048>
{                                                                                                             //<2049>
//< 17.1. First Run Subroutine 27 >```````````````````````````````````````````````````````````````````````````//<2050>
                                                                                                              //<2051>
//< 17.7.1. First Run Subroutine Entry Point 1 >                                                              //<2052>
static int ali.Trigger ; if ( ali.Trigger == 0 ) { ali.Trigger = 1                                          ; //<2053>
//</17.7.1. First Run Subroutine Entry Point 1 >                                                              //<2054>
                                                                                                              //<2055>
//< 17.1.2. Constants Declaration 4 >                                                                         //<2056>
       static int    aci.Buy          = 0                                                                   ; //<2057>
       static int    aci.Sell         = 1                                                                   ; //<2058>
       static int    aci.Modify       = 6                                                                   ; //<2059>
       static int    aci.Close        = 7                                                                   ; //<2060>
//</17.1.2. Constants Declaration 4 >                                                                         //<2061>
                                                                                                              //<2062>
//< 17.1.3. Variables Declaration 12 >                                                                        //<2063>
       //< 17.1.3.1. Control Interface 4 >                                                                    //<2064>
       int           ali.Command                                                                            ; //<2065>
       double        ald.Price                                                                              ; //<2066>
       double        ald.Take                                                                               ; //<2067>
       double        ald.Stop                                                                               ; //<2068>
       //</17.1.3.1. Control Interface 4 >                                                                    //<2069>
                                                                                                              //<2070>
       //< 17.1.3.2. Open Position Data 4 >                                                                   //<2071>
       int           ali.OrdersTotal                                                                        ; //<2072>
       int           ali.OrderType                                                                          ; //<2073>
       double        ald.OrderOpenPrice                                                                     ; //<2074>
       double        ald.OrderStop                                                                          ; //<2075>
       //</17.1.3.2. Open Position Data 4 >                                                                   //<2076>
                                                                                                              //<2077>
       //< 17.1.3.3. Position Management Data 6 >                                                             //<2078>
       int           ali.EmptyTake                                                                          ; //<2079>
       int           ali.EmptyStop                                                                          ; //<2080>
                                                                                                              //<2081>
       double        ald.DistanceTake                                                                       ; //<2082>
       double        ald.DistanceStop                                                                       ; //<2083>
                                                                                                              //<2084>
       double        ald.NewTake                                                                            ; //<2085>
       double        ald.NewStop                                                                            ; //<2086>
       //</17.1.3.3. Position Management Data 6 >                                                             //<2087>
//</17.1.3. Variables Declaration 12 >                                                                        //<2088>
                                                                                                              //<2089>
//< 17.1.4. Control Interface Creation 9 >                                                                    //<2090>
                                                                                                              //<2091>
       avs.SetupCommand               = avs.SetupPrefix           + "8.1." + "Command"                      ; //<2092>
       avs.SetupTake                  = avs.SetupPrefix           + "8.2." + "Take"                         ; //<2093>
       avs.SetupStop                  = avs.SetupPrefix           + "8.3." + "Stop"                         ; //<2094>
                                                                                                              //<2095>
       GlobalVariableSet              ( avs.SetupCommand          , EMPTY                                 ) ; //<2096>
       GlobalVariableSet              ( avs.SetupTake             , EMPTY                                 ) ; //<2097>
       GlobalVariableSet              ( avs.SetupStop             , EMPTY                                 ) ; //<2098>
//</17.1.4. Control Interface Creation 9 >                                                                    //<2099>
                                                                                                              //<2100>
//< 17.7.5. First Run Subroutine Exit Point 1 >                                                               //<2101>
}                                                                                                             //<2102>
//</17.7.5. First Run Subroutine Exit Point 1 >                                                               //<2103>
                                                                                                              //<2104>
//</17.1. First Run Subroutine 27 >```````````````````````````````````````````````````````````````````````````//<2105>
                                                                                                              //<2106>
//< 17.2. Control Interface Read 15 >`````````````````````````````````````````````````````````````````````````//<2107>
if   ( GlobalVariableCheck            ( avs.SetupCommand        ) )                                           //<2108>
       ali.Command                    = GlobalVariableGet ( avs.SetupCommand                              ) ; //<2109>
else { ali.Command                    = EMPTY                                                               ; //<2110>
       GlobalVariableSet              ( avs.SetupCommand          , EMPTY                                 ) ; //<2111>
     }                                                                                                        //<2112>
                                                                                                              //<2113>
if   ( GlobalVariableCheck            ( avs.SetupTake           ) )                                           //<2114>
       ald.Take                       = GlobalVariableGet ( avs.SetupTake                                 ) ; //<2115>
else { ald.Take                       = EMPTY                                                               ; //<2116>
       GlobalVariableSet              ( avs.SetupTake             , EMPTY                                 ) ; //<2117>
     }                                                                                                        //<2118>
                                                                                                              //<2119>
if   ( GlobalVariableCheck            ( avs.SetupStop           ) )                                           //<2120>
       ald.Stop                       = GlobalVariableGet ( avs.SetupStop                                 ) ; //<2121>
else { ald.Stop                       = EMPTY                                                               ; //<2122>
       GlobalVariableSet              ( avs.SetupStop             , EMPTY                                 ) ; //<2123>
     }                                                                                                        //<2124>
//</17.2. Control Interface Read 15 >`````````````````````````````````````````````````````````````````````````//<2125>
                                                                                                              //<2126>
//< 17.3. Open Position Analysis 14 >`````````````````````````````````````````````````````````````````````````//<2127>
ali.OrderType                         = EMPTY                                                               ; //<2128>
ald.OrderOpenPrice                    = EMPTY                                                               ; //<2129>
ald.OrderStop                         = EMPTY                                                               ; //<2130>
ali.OrdersTotal                       = OrdersTotal        ()                                               ; //<2131>
                                                                                                              //<2132>
if ( ali.OrdersTotal  > 0         )                                                                           //<2133>
   {                                                                                                          //<2134>
     int   i , N ; N  = ali.OrdersTotal - 1                                                                 ; //<2135>
     for ( i = N ; i >= 0 ; i --  )                                                                           //<2136>
         { OrderSelect    ( i , SELECT_BY_POS , MODE_TRADES )                                               ; //<2137>
           if ( OrderMagicNumber ()  != aei.OrderID         )                                      continue ; //<2138>
           else                                                                                               //<2139>
              {                                                                                               //<2140>
                ali.OrderType         = OrderType          ()                                               ; //<2141>
                ald.OrderOpenPrice    = OrderOpenPrice     ()                                               ; //<2142>
                ald.OrderStop         = OrderStopLoss      ()                                               ; //<2143>
              } // else                                                                                       //<2144>
         } // for                                                                                             //<2145>
   } // if                                                                                                    //<2146>
//</17.3. Open Position Analysis 14 >`````````````````````````````````````````````````````````````````````````//<2147>
                                                                                                              //<2148>
//< 17.4. Control Interface Validation 265 >``````````````````````````````````````````````````````````````````//<2149>
                                                                                                              //<2150>
//< 17.4.1. Validation Flag Declaration 1 > ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` //<2151>
int       ali.Validation                                                                                    ; //<2152>
//</17.4.1. Validation Flag Declaration 1 > ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` //<2153>
                                                                                                              //<2154>
//< 17.4.2. Buy Command Validation 52 > ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` //<2155>
                                                                                                              //<2156>
          //< 17.4.2.1. Buy Command Validation Entry Point 5 >                                                //<2157>
if      ( ali.Command == aci.Buy          )                                                                   //<2158>
        {                                                                                                     //<2159>
          Alert        ( avs.SystemStamp ,                                                                    //<2160>
                         ": Buy command is detected at Ask price = "                                      ,   //<2161>
                         DoubleToStr  ( avd.QuoteAsk                                       , avi.Digits ) ) ; //<2162>
          //</17.4.2.1. Buy Command Validation Entry Point 5 >                                                //<2163>
                                                                                                              //<2164>
          //< 17.4.2.2. Command Validation 14 >                                                               //<2165>
          if   ( ali.OrdersTotal     == 0 )                                                                   //<2166>
               {                                                                                              //<2167>
                 ali.Validation       = 1                                                                   ; //<2168>
                 ald.Price            = NormalizeDouble ( avd.QuoteAsk                     , avi.Digits   ) ; //<2169>
                 ald.Take             = NormalizeDouble ( ald.Take                         , avi.Digits   ) ; //<2170>
                 ald.Stop             = NormalizeDouble ( ald.Stop                         , avi.Digits   ) ; //<2171>
                 ald.DistanceTake     = ald.Take                          - ald.Price                       ; //<2172>
                 ald.DistanceStop     = ald.Price       - avd.QuoteSpread - ald.Stop                        ; //<2173>
               }                                                                                              //<2174>
          else { ali.Validation       = 0                                                                   ; //<2175>
                 Alert ( avs.SystemStamp ,                                                                    //<2176>
                         ": Buy command is disabled due to open orders detected = "                       ,   //<2177>
                         ali.OrdersTotal                                                                  ) ; //<2178>
               }                                                                                              //<2179>
          //</17.4.2.2. Command Validation 14 >                                                               //<2180>
                                                                                                              //<2181>
          //< 17.4.2.3. Take Price Validation 15 >                                                            //<2182>
          if   ( ali.Validation      == 1 )                                                                   //<2183>
          if   ( avd.QuoteStops       - ald.DistanceTake > 0 )                                                //<2184>
               { ali.Validation       = 0                                                                   ; //<2185>
                 Alert ( avs.SystemStamp ,                                                                    //<2186>
                         ": Buy command is disabled due to invalid Take distance = "                      ,   //<2187>
                         DoubleToStr  ( ald.DistanceTake                                   , avi.Digits ) ,   //<2188>
                         " / "                                                                            ,   //<2189>
                         DoubleToStr  ( avd.QuoteStops                                     , avi.Digits ) ) ; //<2190>
                 Alert ( avs.SystemStamp ,                                                                    //<2191>
                         ": Buy command current Take price = "                                            ,   //<2192>
                         DoubleToStr  ( ald.Take                                           , avi.Digits ) ) ; //<2193>
                 Alert ( avs.SystemStamp ,                                                                    //<2194>
                         ": Buy command minimal Take price = "                                            ,   //<2195>
                         DoubleToStr  ( ald.Price + avd.QuoteStops                         , avi.Digits ) ) ; //<2196>
               }                                                                                              //<2197>
          //</17.4.2.3. Take Price Validation 15 >                                                            //<2198>
                                                                                                              //<2199>
          //< 17.4.2.4. Stop Price Validation 15 >                                                            //<2200>
          if   ( ali.Validation      == 1 )                                                                   //<2201>
          if   ( avd.QuoteStops       - ald.DistanceStop > 0 )                                                //<2202>
               { ali.Validation       = 0                                                                   ; //<2203>
                 Alert ( avs.SystemStamp ,                                                                    //<2204>
                         ": Buy command is disabled due to invalid Stop distance = "                      ,   //<2205>
                         DoubleToStr  ( ald.DistanceStop                                   , avi.Digits ) ,   //<2206>
                         " / "                                                                            ,   //<2207>
                         DoubleToStr  ( avd.QuoteStops                                     , avi.Digits ) ) ; //<2208>
                 Alert ( avs.SystemStamp ,                                                                    //<2209>
                         ": Buy command current Stop price = "                                            ,   //<2210>
                         DoubleToStr  ( ald.Stop                                           , avi.Digits ) ) ; //<2211>
                 Alert ( avs.SystemStamp ,                                                                    //<2212>
                         ": Buy command maximal Stop price = "                                            ,   //<2213>
                         DoubleToStr  ( ald.Price - avd.QuoteSpread - avd.QuoteStops       , avi.Digits ) ) ; //<2214>
               }                                                                                              //<2215>
          //</17.4.2.4. Stop Price Validation 15 >                                                            //<2216>
                                                                                                              //<2217>
          //< 17.4.2.5. Buy Command Validation Exit Point 3 >                                                 //<2218>
          if   ( ali.Validation      == 0 )                                                                   //<2219>
                 ali.Command          = EMPTY                                                               ; //<2220>
        }                                                                                                     //<2221>
          //</17.4.2.5. Buy Command Validation Exit Point 3 >                                                 //<2222>
                                                                                                              //<2223>
//</17.4.2. Buy Command Validation 52 > ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` //<2224>
                                                                                                              //<2225>
//< 17.4.3. Sell Command Validation 52 > ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` `//<2226>
                                                                                                              //<2227>
          //< 17.4.3.1. Sell Command Validation Entry Point 5 >                                               //<2228>
else if ( ali.Command == aci.Sell         )                                                                   //<2229>
        {                                                                                                     //<2230>
          Alert        ( avs.SystemStamp ,                                                                    //<2231>
                         ": Sell command is detected at Bid price = "                                     ,   //<2232>
                         DoubleToStr  ( avd.QuoteBid                                       , avi.Digits ) ) ; //<2233>
          //</17.4.3.1. Sell Command Validation Entry Point 5 >                                               //<2234>
                                                                                                              //<2235>
          //< 17.4.3.2. Command Validation 14 >                                                               //<2236>
          if   ( ali.OrdersTotal     == 0 )                                                                   //<2237>
               {                                                                                              //<2238>
                 ali.Validation       = 1                                                                   ; //<2239>
                 ald.Price            = NormalizeDouble ( avd.QuoteBid                     , avi.Digits   ) ; //<2240>
                 ald.Take             = NormalizeDouble ( ald.Take                         , avi.Digits   ) ; //<2241>
                 ald.Stop             = NormalizeDouble ( ald.Stop                         , avi.Digits   ) ; //<2242>
                 ald.DistanceTake     = ald.Price                         - ald.Take                        ; //<2243>
                 ald.DistanceStop     = ald.Stop        - ald.Price       - avd.QuoteSpread                 ; //<2244>
               }                                                                                              //<2245>
          else { ali.Validation       = 0                                                                   ; //<2246>
                 Alert ( avs.SystemStamp ,                                                                    //<2247>
                         ": Sell command is disabled due to open orders detected = "                      ,   //<2248>
                         ali.OrdersTotal                                                                  ) ; //<2249>
               }                                                                                              //<2250>
          //</17.4.3.2. Command Validation 14 >                                                               //<2251>
                                                                                                              //<2252>
          //< 17.4.3.3. Take Price Validation 15 >                                                            //<2253>
          if   ( ali.Validation      == 1 )                                                                   //<2254>
          if   ( avd.QuoteStops       - ald.DistanceTake > 0 )                                                //<2255>
               { ali.Validation       = 0                                                                   ; //<2256>
                 Alert ( avs.SystemStamp ,                                                                    //<2257>
                         ": Sell command is disabled due to invalid Take distance = "                     ,   //<2258>
                         DoubleToStr  ( ald.DistanceTake                                   , avi.Digits ) ,   //<2259>
                         " / "                                                                            ,   //<2260>
                         DoubleToStr  ( avd.QuoteStops                                     , avi.Digits ) ) ; //<2261>
                 Alert ( avs.SystemStamp ,                                                                    //<2262>
                         ": Sell command current Take price = "                                           ,   //<2263>
                         DoubleToStr  ( ald.Take                                           , avi.Digits ) ) ; //<2264>
                 Alert ( avs.SystemStamp ,                                                                    //<2265>
                         ": Sell command maximal Take price = "                                           ,   //<2266>
                         DoubleToStr  ( ald.Price - avd.QuoteStops                         , avi.Digits ) ) ; //<2267>
               }                                                                                              //<2268>
          //</17.4.3.3. Take Price Validation 15 >                                                            //<2269>
                                                                                                              //<2270>
          //< 17.4.3.4. Stop Price Validation 15 >                                                            //<2271>
          if   ( ali.Validation      == 1 )                                                                   //<2272>
          if   ( avd.QuoteStops       - ald.DistanceStop > 0 )                                                //<2273>
               { ali.Validation       = 0                                                                   ; //<2274>
                 Alert ( avs.SystemStamp ,                                                                    //<2275>
                         ": Sell command is disabled due to invalid Stop distance = "                     ,   //<2276>
                         DoubleToStr  ( ald.DistanceStop                                   , avi.Digits ) ,   //<2277>
                         " / "                                                                            ,   //<2278>
                         DoubleToStr  ( avd.QuoteStops                                     , avi.Digits ) ) ; //<2279>
                 Alert ( avs.SystemStamp ,                                                                    //<2280>
                         ": Sell command current Stop price = "                                           ,   //<2281>
                         DoubleToStr  ( ald.Stop                                           , avi.Digits ) ) ; //<2282>
                 Alert ( avs.SystemStamp ,                                                                    //<2283>
                         ": Sell command minimal Stop price = "                                           ,   //<2284>
                         DoubleToStr  ( ald.Price + avd.QuoteSpread + avd.QuoteStops       , avi.Digits ) ) ; //<2285>
               }                                                                                              //<2286>
          //</17.4.3.4. Stop Price Validation 15 >                                                            //<2287>
                                                                                                              //<2288>
          //< 17.4.3.5. Sell Command Validation Exit Point 3 >                                                //<2289>
          if   ( ali.Validation      == 0 )                                                                   //<2290>
                 ali.Command          = EMPTY                                                               ; //<2291>
        }                                                                                                     //<2292>
          //</17.4.3.5. Sell Command Validation Exit Point 3 >                                                //<2293>
                                                                                                              //<2294>
//</17.4.3. Sell Command Validation 52 > ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` `//<2295>
                                                                                                              //<2296>
//< 17.4.4. Modify Command Validation 137 > ` `` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` `//<2297>
                                                                                                              //<2298>
          //< 17.4.4.1. Modify Command Validation Entry Point 6 >                                             //<2299>
else if ( ali.Command == aci.Modify )                                                                         //<2300>
        {                                                                                                     //<2301>
          if   ( ali.OrdersTotal      > 0 )                                                                   //<2302>
                 avs.LocalStamp       = " for " + acs.Operation [ OrderType () ] + " order"                 ; //<2303>
          else   avs.LocalStamp       = ""                                                                  ; //<2304>
                                                                                                              //<2305>
          Alert        ( avs.SystemStamp        , ": Modify command is detected" , avs.LocalStamp         ) ; //<2306>
          //</17.4.4.1. Modify Command Validation Entry Point 6 >                                             //<2307>
                                                                                                              //<2308>
          //< 17.4.4.2. Modify Command Validation 35 >                                                        //<2309>
          if   ( ali.OrdersTotal      > 0 )                                                                   //<2310>
               { ali.Validation       = 1                                                                   ; //<2311>
                                                                                                              //<2312>
                 if ( avd.QuoteStops - avd.QuoteFreeze > 0 ) double ald.DistanceModify = avd.QuoteStops     ; //<2313>
                 else                                               ald.DistanceModify = avd.QuoteFreeze    ; //<2314>
                                                                                                              //<2315>
                 if ( ald.Take        < 0 )                                                                   //<2316>
                    { ald.Take        = NormalizeDouble ( OrderTakeProfit ()               , avi.Digits   ) ; //<2317>
                      ali.EmptyTake   = 1                                                                   ; //<2318>
                    }                                                                                         //<2319>
                 else                                                                                         //<2320>
                    { ali.EmptyTake   = 0                                                                   ; //<2321>
                      ald.Take        = NormalizeDouble ( ald.Take                         , avi.Digits   ) ; //<2322>
                    }                                                                                         //<2323>
                                                                                                              //<2324>
                 if ( ald.Stop        < 0 )                                                                   //<2325>
                    { ald.Stop        = NormalizeDouble ( OrderStopLoss   ()               , avi.Digits   ) ; //<2326>
                      ali.EmptyStop   = 1                                                                   ; //<2327>
                    }                                                                                         //<2328>
                 else                                                                                         //<2329>
                    { ali.EmptyStop   = 0                                                                   ; //<2330>
                      ald.Stop        = NormalizeDouble ( ald.Stop                         , avi.Digits   ) ; //<2331>
                    }                                                                                         //<2332>
                                                                                                              //<2333>
                 if ( ali.OrderType  == aci.Buy            )                                                  //<2334>
                    { ald.Price       = NormalizeDouble ( avd.QuoteBid                     , avi.Digits   ) ; //<2335>
                      ald.DistanceTake  = ald.Take      - ald.Price                                         ; //<2336>
                      ald.DistanceStop  = ald.Price     - ald.Stop                                          ; //<2337>
                    }                                                                                         //<2338>
                                                                                                              //<2339>
                 if ( ali.OrderType  == aci.Sell           )                                                  //<2340>
                    { ald.Price       = NormalizeDouble ( avd.QuoteAsk                     , avi.Digits   ) ; //<2341>
                      ald.DistanceTake  = ald.Price     - ald.Take                                          ; //<2342>
                      ald.DistanceStop  = ald.Stop      - ald.Price                                         ; //<2343>
                    }                                                                                         //<2344>
               } // if                                                                                        //<2345>
          else { ali.Validation       = 0                                                                   ; //<2346>
                 Alert ( avs.SystemStamp ,                                                                    //<2347>
                         ": Modify command is ignored due to empty order list"                            ) ; //<2348>
               } // else                                                                                      //<2349>
          //</17.4.4.2. Modify Command Validation 35 >                                                        //<2350>
                                                                                                              //<2351>
          //< 17.4.4.3. Empty Levels Validation 15 >                                                          //<2352>
          if   ( ali.Validation      == 1                       )                                             //<2353>
          if   ( ali.EmptyTake       == 1 && ali.EmptyStop == 1 )                                             //<2354>
               {                                                                                              //<2355>
                 ali.Validation       = 0                                                                   ; //<2356>
                 Alert ( avs.SystemStamp ,                                                                    //<2357>
                         ": Modify command is ignored due to empty price levels"                          ) ; //<2358>
               }                                                                                              //<2359>
                                                                                                              //<2360>
          if   ( ali.Validation      == 1                                                )                    //<2361>
          if   ( MathRound ( ( ald.Take - OrderTakeProfit () ) / avd.QuotePoint )   == 0                      //<2362>
              && MathRound ( ( ald.Stop - OrderStopLoss   () ) / avd.QuotePoint )   == 0 )                    //<2363>
               {                                                                                              //<2364>
                 ali.Validation       = 0                                                                   ; //<2365>
                 Alert ( avs.SystemStamp ,                                                                    //<2366>
                         ": Modify command is ignored due to unchanged price levels"                      ) ; //<2367>
               }                                                                                              //<2368>
          //</17.4.4.3. Empty Levels Validation 15 >                                                          //<2369>
                                                                                                              //<2370>
          //< 17.4.4.4. Buy Order Take Price Validation 18 >                                                  //<2371>
          if   ( ali.Validation      == 1                                          )                          //<2372>
          if   ( ali.OrderType       == aci.Buy                                    )                          //<2373>
          if   ( ald.DistanceModify   - ald.DistanceTake > 0 && ali.EmptyTake == 0 )                          //<2374>
               { ali.Validation       = 0                                                                   ; //<2375>
                 Alert ( avs.SystemStamp ,                                                                    //<2376>
                         ": Modify command is disabled due to invalid Take distance = "                   ,   //<2377>
                         DoubleToStr  ( ald.DistanceTake                                   , avi.Digits ) ,   //<2378>
                         " / "                                                                            ,   //<2379>
                         DoubleToStr  ( ald.DistanceModify                                 , avi.Digits ) ) ; //<2380>
                 Alert ( avs.SystemStamp ,                                                                    //<2381>
                         ": Modify command current Take price = "                                         ,   //<2382>
                         DoubleToStr  ( ald.Take                                           , avi.Digits ) ,   //<2383>
                         " / Bid price = "                                                                ,   //<2384>
                         DoubleToStr  ( ald.Price                                          , avi.Digits ) ) ; //<2385>
                 Alert ( avs.SystemStamp ,                                                                    //<2386>
                         ": Buy order minimal Take price = "                                              ,   //<2387>
                         DoubleToStr  ( ald.Price + ald.DistanceModify                     , avi.Digits ) ) ; //<2388>
               }                                                                                              //<2389>
          //< 17.4.4.4. Buy Order Take Price Validation 18 >                                                  //<2390>
                                                                                                              //<2391>
          //< 17.4.4.5. Buy Order Stop Price Validation 25 >                                                  //<2392>
          if   ( ali.Validation      == 1                                          )                          //<2393>
          if   ( ali.OrderType       == aci.Buy                                    )                          //<2394>
          if   ( ald.DistanceModify   - ald.DistanceStop > 0 && ali.EmptyStop == 0 )                          //<2395>
               { ali.Validation       = 0                                                                   ; //<2396>
                 Alert ( avs.SystemStamp ,                                                                    //<2397>
                         ": Modify command is disabled due to invalid Stop distance = "                   ,   //<2398>
                         DoubleToStr  ( ald.DistanceStop                                   , avi.Digits ) ,   //<2399>
                         " / "                                                                            ,   //<2400>
                         DoubleToStr  ( ald.DistanceModify                                 , avi.Digits ) ) ; //<2401>
                 Alert ( avs.SystemStamp ,                                                                    //<2402>
                         ": Modify command current Stop price = "                                         ,   //<2403>
                         DoubleToStr  ( ald.Stop                                           , avi.Digits ) ,   //<2404>
                         " / Bid price = "                                                                ,   //<2405>
                         DoubleToStr  ( ald.Price                                          , avi.Digits ) ) ; //<2406>
                 Alert ( avs.SystemStamp ,                                                                    //<2407>
                         ": Buy order maximal Stop price = "                                              ,   //<2408>
                         DoubleToStr  ( ald.Price - ald.DistanceModify                     , avi.Digits ) ) ; //<2409>
               }                                                                                              //<2410>
                                                                                                              //<2411>
          if   ( ali.Validation      == 1                                          )                          //<2412>
          if   ( ali.OrderType       == aci.Buy                                    )                          //<2413>
          if   ( ald.Stop             - OrderStopLoss () < 0 && ali.EmptyStop == 0 )                          //<2414>
               { ali.Validation       = 0                                                                   ; //<2415>
                 Alert ( avs.SystemStamp ,                                                                    //<2416>
                         ": Modify command is disabled due to Buy order Stop lowering"                    ) ; //<2417>
               }                                                                                              //<2418>
          //</17.4.4.5. Buy Order Stop Price Validation 25 >                                                  //<2419>
                                                                                                              //<2420>
          //< 17.4.4.6. Sell Order Take Price Validation 18 >                                                 //<2421>
          if   ( ali.Validation      == 1                                          )                          //<2422>
          if   ( ali.OrderType       == aci.Sell                                   )                          //<2423>
          if   ( ald.DistanceModify   - ald.DistanceTake > 0 && ali.EmptyTake == 0 )                          //<2424>
               { ali.Validation       = 0                                                                   ; //<2425>
                 Alert ( avs.SystemStamp ,                                                                    //<2426>
                         ": Modify command is disabled due to invalid Take distance = "                   ,   //<2427>
                         DoubleToStr  ( ald.DistanceTake                                   , avi.Digits ) ,   //<2428>
                         " / "                                                                            ,   //<2429>
                         DoubleToStr  ( ald.DistanceModify                                 , avi.Digits ) ) ; //<2430>
                 Alert ( avs.SystemStamp ,                                                                    //<2431>
                         ": Modify command current Take price = "                                         ,   //<2432>
                         DoubleToStr  ( ald.Take                                           , avi.Digits ) ,   //<2433>
                         " / Ask price = "                                                                ,   //<2434>
                         DoubleToStr  ( ald.Price                                          , avi.Digits ) ) ; //<2435>
                 Alert ( avs.SystemStamp ,                                                                    //<2436>
                         ": Sell order maximal Take price = "                                             ,   //<2437>
                         DoubleToStr  ( ald.Price - ald.DistanceModify                     , avi.Digits ) ) ; //<2438>
               }                                                                                              //<2439>
          //< 17.4.4.6. Sell Order Take Price Validation 18 >                                                 //<2440>
                                                                                                              //<2441>
          //< 17.4.4.7. Sell Order Stop Price Validation 25 >                                                 //<2442>
          if   ( ali.Validation      == 1                                          )                          //<2443>
          if   ( ali.OrderType       == aci.Sell                                   )                          //<2444>
          if   ( ald.DistanceModify   - ald.DistanceStop > 0 && ali.EmptyStop == 0 )                          //<2445>
               { ali.Validation       = 0                                                                   ; //<2446>
                 Alert ( avs.SystemStamp ,                                                                    //<2447>
                         ": Modify command is disabled due to invalid Stop distance = "                   ,   //<2448>
                         DoubleToStr  ( ald.DistanceStop                                   , avi.Digits ) ,   //<2449>
                         " / "                                                                            ,   //<2450>
                         DoubleToStr  ( ald.DistanceModify                                 , avi.Digits ) ) ; //<2451>
                 Alert ( avs.SystemStamp ,                                                                    //<2452>
                         ": Modify command current Stop price = "                                         ,   //<2453>
                         DoubleToStr  ( ald.Stop                                           , avi.Digits ) ,   //<2454>
                         " / Ask price = "                                                                ,   //<2455>
                         DoubleToStr  ( ald.Price                                          , avi.Digits ) ) ; //<2456>
                 Alert ( avs.SystemStamp ,                                                                    //<2457>
                         ": Sell order minimal Stop price = "                                             ,   //<2458>
                         DoubleToStr  ( ald.Price + ald.DistanceModify                     , avi.Digits ) ) ; //<2459>
               }                                                                                              //<2460>
                                                                                                              //<2461>
          if   ( ali.Validation      == 1                                          )                          //<2462>
          if   ( ali.OrderType       == aci.Sell                                   )                          //<2463>
          if   ( ald.Stop             - OrderStopLoss () > 0 && ali.EmptyStop == 0 )                          //<2464>
               { ali.Validation       = 0                                                                   ; //<2465>
                 Alert ( avs.SystemStamp ,                                                                    //<2466>
                         ": Modify command is disabled due to Sell order Stop heightening"                ) ; //<2467>
               }                                                                                              //<2468>
          //</17.4.4.7. Sell Order Stop Price Validation 25 >                                                 //<2469>
                                                                                                              //<2470>
          //< 17.4.4.8. Modify Command Validation Exit Point 3 >                                              //<2471>
          if           ( ali.Validation == 0 )                                                                //<2472>
                         ali.Command     = EMPTY                                                            ; //<2473>
        }                                                                                                     //<2474>
          //</17.4.4.8. Modify Command Validation Exit Point 3 >                                              //<2475>
//</17.4.4. Modify Command Validation 137 > ` `  ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` `//<2476>
                                                                                                              //<2477>
//< 17.4.5. Close Command Validation 11 > ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` //<2478>
else if ( ali.Command == aci.Close  )                                                                         //<2479>
        {                                                                                                     //<2480>
          Alert        ( avs.SystemStamp , ": Close command is detected"                                  ) ; //<2481>
                                                                                                              //<2482>
          if   ( ali.OrdersTotal      > 0 )                                                                   //<2483>
                 ali.Validation       = 1                                                                   ; //<2484>
          else { ali.Validation       = 0                                                                   ; //<2485>
                 Alert ( avs.SystemStamp , ": Close command is ignored due to empty order list"           ) ; //<2486>
               }                                                                                              //<2487>
                                                                                                              //<2488>
          if   ( ali.Validation      == 0 )                                                                   //<2489>
                 ali.Command          = EMPTY                                                               ; //<2490>
        }                                                                                                     //<2491>
//</17.4.5. Close Command Validation 11 > ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` //<2492>
                                                                                                              //<2493>
//< 17.4.6. Empty Command Detection 3 > ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` //<2494>
else if ( ali.Command == EMPTY      )                                                                         //<2495>
        {                                                                                                     //<2496>
        }                                                                                                     //<2497>
//</17.4.6. Empty Command Detection 3 > ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` //<2498>
                                                                                                              //<2499>
//< 17.4.7. Invalid Command Detection 1 > ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` //<2500>
else      Alert        ( avs.SystemStamp , ": Invalid Command "     , ali.Command                         ) ; //<2501>
//</17.4.7. Invalid Command Detection 1 > ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` ` //<2502>
                                                                                                              //<2503>
//</17.4. Control Interface Validation 265 >``````````````````````````````````````````````````````````````````//<2504>
                                                                                                              //<2505>
//< 17.5. Attempt To Modify 34 >``````````````````````````````````````````````````````````````````````````````//<2506>
if   ( ali.Command == aci.Modify )                                                                            //<2507>
     { ali.Command  = EMPTY                                                                                 ; //<2508>
                                                                                                              //<2509>
               //< Trading Function Execution Sequence >                                                      //<2510>
               //< Step 1 >                                                                                   //<2511>
                   avs.LocalStamp     = avs.SystemStamp                          + ": Attempt to Modify " +   //<2512>
                                        aes.Symbol                                                 + " "  +   //<2513>
                                        acs.Operation    [   OrderType ()  ]                       + " #" +   //<2514>
                                        OrderMagicNumber ()                                        + "/"  +   //<2515>
                                        OrderTicket      ()                                                 ; //<2516>
               //< Step 2 >                                                                                   //<2517>
                   if ( OrderType () == aci.Buy           )  string          als.Price       = " Bid = "    ; //<2518>
                   else                                                      als.Price       = " Ask = "    ; //<2519>
                                                                                                              //<2520>
                   Alert              ( avs.LocalStamp                                       , als.Price    , //<2521>
                                        DoubleToStr     (    ald.Price     , avi.Digits )    , " / Take = " , //<2522>
                                        DoubleToStr     (    ald.Take      , avi.Digits )    , " / Stop = " , //<2523>
                                        DoubleToStr     (    ald.Stop      , avi.Digits )                 ) ; //<2524>
               //< Step 3 >                                                                                   //<2525>
                   OrderModify        ( OrderTicket     () ,                                                  //<2526>
                                        OrderOpenPrice  () ,                                                  //<2527>
                                        ald.Stop           ,                                                  //<2528>
                                        ald.Take           , 0 , 0                                        ) ; //<2529>
               //< Step 4 >                                                                                   //<2530>
                   avi.TimeStamp      = TimeLocal       ()                                                  ; //<2531>
                                                                                                              //<2532>
               //< Step 5 >                                                                                   //<2533>
                   avi.Exception      = GetLastError    ()                                                  ; //<2534>
                                                                                                              //<2535>
               //< Step 6 >                                                                                   //<2536>
                   if ( avi.Exception == 0 ) avs.LocalMessage =            " Success "                      ; //<2537>
                   else                      avs.LocalMessage =            " Failure "      + avi.Exception ; //<2538>
                                                                                                              //<2539>
               //< Step 7 >                                                                                   //<2540>
                   static int ali.ModifyAttempts , ali.ModifySuccesses     , ali.ModifyExceptions           ; //<2541>
                   ali.ModifyAttempts                            ++                                         ; //<2542>
                   if ( avi.Exception == 0 ) ali.ModifySuccesses ++                                         ; //<2543>
                   else ali.ModifyExceptions                     ++                                         ; //<2544>
                                                                                                              //<2545>
               //< Step 8 >                                                                                   //<2546>
                   Alert              ( avs.LocalStamp                     , avs.LocalMessage       , " " ,   //<2547>
                                        ali.ModifyAttempts                                          , "/" ,   //<2548>
                                        ali.ModifySuccesses                                         , "/" ,   //<2549>
                                        ali.ModifyExceptions                                              ) ; //<2550>
                                                                                                              //<2551>
               //< Step 9 >                                                                                   //<2552>
                   avs.SystemMessage  = "Modification"                     + avs.LocalMessage       + " " +   //<2553>
                                        ali.ModifyAttempts                                          + "/" +   //<2554>
                                        ali.ModifySuccesses                                         + "/" +   //<2555>
                                        ali.ModifyExceptions                                                ; //<2556>
                                                                                                              //<2557>
          //</Trading Function Execution Sequence >                                                           //<2558>
     }                                                                                                        //<2559>
//</17.5. Attempt To Modify 34 >``````````````````````````````````````````````````````````````````````````````//<2560>
                                                                                                              //<2561>
//< 17.6. Attempt To Close 27 >```````````````````````````````````````````````````````````````````````````````//<2562>
if   ( ali.Command == aci.Close  )                                                                            //<2563>
     { ali.Command  = EMPTY                                                                                 ; //<2564>
                                                                                                              //<2565>
               //< Trading Function Execution Sequence >                                                      //<2566>
               //< Step 1 >                                                                                   //<2567>
                   avs.LocalStamp     = avs.SystemStamp                           + ": Attempt to Close " +   //<2568>
                                        aes.Symbol                                                 + " "  +   //<2569>
                                        acs.Operation    [   OrderType ()  ]                       + " #" +   //<2570>
                                        OrderMagicNumber ()                                        + "/"  +   //<2571>
                                        OrderTicket      ()                                                 ; //<2572>
               //< Step 2 >                                                                                   //<2573>
                   Alert              ( avs.LocalStamp                                           ,   " at " , //<2574>
                                        DoubleToStr     (    OrderClosePrice ()   , avi.Digits )          ) ; //<2575>
               //< Step 3 >                                                                                   //<2576>
                   OrderClose         ( OrderTicket     () , OrderLots () ,   OrderClosePrice () , 0 , 0  ) ; //<2577>
                                                                                                              //<2578>
               //< Step 4 >                                                                                   //<2579>
                   avi.TimeStamp      = TimeLocal       ()                                                  ; //<2580>
                                                                                                              //<2581>
               //< Step 5 >                                                                                   //<2582>
                   avi.Exception      = GetLastError    ()                                                  ; //<2583>
                                                                                                              //<2584>
               //< Step 6 >                                                                                   //<2585>
                   if ( avi.Exception == 0 ) avs.LocalMessage =            " Success "                      ; //<2586>
                   else                      avs.LocalMessage =            " Failure "      + avi.Exception ; //<2587>
                                                                                                              //<2588>
               //< Step 7 >                                                                                   //<2589>
                   static int ali.CloseAttempts , ali.CloseSuccesses       , ali.CloseExceptions            ; //<2590>
                   ali.CloseAttempts                             ++                                         ; //<2591>
                   if ( avi.Exception == 0 ) ali.CloseSuccesses  ++                                         ; //<2592>
                   else ali.CloseExceptions                      ++                                         ; //<2593>
                                                                                                              //<2594>
               //< Step 8 >                                                                                   //<2595>
                   Alert              ( avs.LocalStamp                     , avs.LocalMessage       , " " ,   //<2596>
                                        ali.CloseAttempts                                           , "/" ,   //<2597>
                                        ali.CloseSuccesses                                          , "/" ,   //<2598>
                                        ali.CloseExceptions                                               ) ; //<2599>
               //< Step 9 >                                                                                   //<2600>
                   avs.SystemMessage  = "Close command"                    + avs.LocalMessage       + " " +   //<2601>
                                        ali.CloseAttempts                                           + "/" +   //<2602>
                                        ali.CloseSuccesses                                          + "/" +   //<2603>
                                        ali.CloseExceptions                                                 ; //<2604>
          //</Trading Function Execution Sequence >                                                           //<2605>
     }                                                                                                        //<2606>
//</17.6. Attempt To Close 27 >```````````````````````````````````````````````````````````````````````````````//<2607>
                                                                                                              //<2608>
//< 17.7. Automatic Risk Management 39 >``````````````````````````````````````````````````````````````````````//<2609>
                                                                                                              //<2610>
       //< 17.7.1. Automatic Risk Management Entry Point 7 >                                                  //<2611>
if   ( ali.Command == aci.Buy                                                                                 //<2612>
    || ali.Command == aci.Sell )                                                                              //<2613>
     {                                                                                                        //<2614>
       avi.Command = ali.Command                                                                            ; //<2615>
       avd.Price   = ald.Price                                                                              ; //<2616>
       avd.Stop    = ald.Stop                                                                               ; //<2617>
       avd.Take    = ald.Take                                                                               ; //<2618>
       //</17.7.1. Automatic Risk Management Entry Point 7 >                                                  //<2619>
                                                                                                              //<2620>
       //< 17.7.2. Operation Size Limit Computing 9 >                                                         //<2621>
       avd.QuoteTarget                = MathAbs            ( avd.Price           - avd.Take               ) ; //<2622>
       avd.QuoteRisk                  = MathAbs            ( avd.Price           - avd.Stop               ) ; //<2623>
       avd.NominalPoint               = avd.NominalTick    * avd.QuotePoint      / avd.QuoteTick            ; //<2624>
       avi.MarginPoints               = MathRound          ( avd.NominalMargin   / avd.NominalPoint       ) ; //<2625>
       avi.RiskPoints                 = MathRound          ( avd.QuoteRisk       / avd.QuotePoint         ) ; //<2626>
       avd.VARLimit                   = AccountEquity ()   * avd.OrderReserve                               ; //<2627>
       avd.RiskPoint                  = avd.VARLimit       / avi.RiskPoints                                 ; //<2628>
       avd.MarginLimit                = avd.RiskPoint      * avi.MarginPoints                               ; //<2629>
       avd.SizeLimit                  = avd.MarginLimit    / avd.NominalMargin                              ; //<2630>
       //</17.7.2. Operation Size Limit Computing 9 >                                                         //<2631>
                                                                                                              //<2632>
       //< 17.7.3. Operation Size Control 22 >                                                                //<2633>
       if   ( avd.SizeLimit           - avd.MinimumLots >= 0 )                                                //<2634>
            { int    ali.Steps        = MathFloor        ( ( avd.SizeLimit       - avd.MinimumLots        )   //<2635>
                                                           / avd.LotStep                                  ) ; //<2636>
              double ald.Size         = avd.MinimumLots    + avd.LotStep         * ali.Steps              ; } //<2637>
       else   ald.Size                = 0                                                                   ; //<2638>
                                                                                                              //<2639>
       if   ( ald.Size                - avd.MaximumLots >  0 )                                                //<2640>
              ald.Size                = avd.MaximumLots                                                     ; //<2641>
                                                                                                              //<2642>
       if   ( ald.Size                - avd.MinimumLots >= 0 )                                                //<2643>
              double ald.MarginCheck  = AccountFreeMarginCheck ( aes.Symbol      , avi.Command , ald.Size ) ; //<2644>
       else {                                                                                                 //<2645>
              avi.Command             = EMPTY                                                               ; //<2646>
              Alert ( avs.SystemStamp , ": " , acs.Operation [ ali.Command ]                              ,   //<2647>
                      " command is disabled due to low order size limit = "                               ,   //<2648>
                      DoubleToStr     ( avd.SizeLimit , 2 ) , " / " , DoubleToStr ( avd.MinimumLots , 2 ) ) ; //<2649>
            }                                                                                                 //<2650>
                                                                                                              //<2651>
       if   ( ald.MarginCheck        <= 0                                                                     //<2652>
           || GetLastError ()        == 134 )                                                                 //<2653>
            {                                                                                                 //<2654>
              avi.Command             = EMPTY                                                               ; //<2655>
              Alert ( avs.SystemStamp , ": " , acs.Operation [ ali.Command ]                              ,   //<2656>
                      " command is disabled due to insufficient margin"                                   ) ; //<2657>
            }                                                                                                 //<2658>
       //</17.7.3. Operation Size Control 22 >                                                                //<2659>
                                                                                                              //<2660>
       //< 17.7.4. Automatic Risk Management Exit Point 1 >                                                   //<2661>
     } //  if 17.7.1                                                                                          //<2662>
       //</17.7.4. Automatic Risk Management Exit Point 1 >                                                   //<2663>
                                                                                                              //<2664>
//< 17.7. Automatic Risk Management 39 >``````````````````````````````````````````````````````````````````````//<2665>
                                                                                                              //<2666>
//< 17.8. Control Interface Reset On Exit 1 >`````````````````````````````````````````````````````````````````//<2667>
GlobalVariableSet                     ( avs.SetupCommand          , EMPTY                                 ) ; //<2668>
//</17.8. Control Interface Reset On Exit 1 >`````````````````````````````````````````````````````````````````//<2669>
}                                                                                                             //<2670>
//</A.System.Extra: Processing Module Function 1208 >`````````````````````````````````````````````````````````//<2671>
                                                                                                              //<2672>
//</A.System.Extra: Processing Module >-----------------------------------------------------------------------//<2673>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////<2674>
//< A.System.Extra: Control Module >--------------------------------------------------------------------------//<2675>
                                                                                                              //<2676>
//< A.System.Extra: Control Module Function 2201 >````````````````````````````````````````````````````````````//<2677>
int    afr.ControlInitialization      ()          //    5 elements // input    - / code       5 / output    - //<2678>
{                                                                                                             //<2679>
afr.CreateSetup                       ()                                                                    ; //<2680>
                                                                                                              //<2681>
afr.CreatePanel.1                     ()                                                                    ; //<2682>
afr.CreatePanel.2                     ()                                                                    ; //<2683>
                                                                                                              //<2684>
afr.PreparePlotObjects                ()                                                                    ; //<2685>
                                                                                                              //<2686>
afr.ResetChartMetrics                 ()                                                                    ; //<2687>
}                                                                                                             //<2688>
//</A.System.Extra: Control Module Function 2201 >````````````````````````````````````````````````````````````//<2689>
                                                                                                              //<2690>
//< A.System.Extra: Control Module Function 2202 >````````````````````````````````````````````````````````````//<2691>
int    afr.ControlDeinitialization    ()          //    3 elements // input    - / code       3 / output    - //<2692>
{                                                                                                             //<2693>
afr.DeleteSetup                       ()                                                                    ; //<2694>
}                                                                                                             //<2695>
//</A.System.Extra: Control Module Function 2202 >````````````````````````````````````````````````````````````//<2696>
                                                                                                              //<2697>
//< A.System.Extra: Control Module Function 2202 >````````````````````````````````````````````````````````````//<2698>
int    afr.MonitorDeinitialization    ()          //    3 elements // input    - / code       3 / output    - //<2699>
{                                                                                                             //<2700>
afr.DeletePanel.1                     ()                                                                    ; //<2701>
afr.DeletePanel.2                     ()                                                                    ; //<2702>
                                                                                                              //<2703>
afr.DeletePlotObjects                 ()                                                                    ; //<2704>
}                                                                                                             //<2705>
//</A.System.Extra: Control Module Function 2202 >````````````````````````````````````````````````````````````//<2706>
                                                                                                              //<2707>
//< A.System.Extra: Control Module Function 2203 >````````````````````````````````````````````````````````````//<2708>
int    afr.PreparePlotObjects           ()        //    - elements // input    - / code       - / output    - //<2709>
{                                                                                                             //<2710>
       avs.PlotPrefix                 = A.Property.Series + A.Property.Release + ".Plot."                   ; //<2711>
                                                                                                              //<2712>
       avs.OrderPrice                 = avs.PlotPrefix    + acs.OrderPrice                                  ; //<2713>
       avs.OrderTake                  = avs.PlotPrefix    + acs.OrderTake                                   ; //<2714>
       avs.OrderStop                  = avs.PlotPrefix    + acs.OrderStop                                   ; //<2715>
       avs.OrderPriceID               = avs.PlotPrefix    + acs.OrderPriceID                                ; //<2716>
       avs.OrderTakeID                = avs.PlotPrefix    + acs.OrderTakeID                                 ; //<2717>
       avs.OrderStopID                = avs.PlotPrefix    + acs.OrderStopID                                 ; //<2718>
                                                                                                              //<2719>
       avs.TimeBegin.2                = avs.PlotPrefix    + acs.TimeBegin.2                                 ; //<2720>
       avs.TimeBegin.3                = avs.PlotPrefix    + acs.TimeBegin.3                                 ; //<2721>
                                                                                                              //<2722>
       avs.ZoneLevels                 = avs.PlotPrefix    + acs.ZoneLevels                                  ; //<2723>
       avs.Profile.1                  = avs.PlotPrefix    + acs.Profile.1                                   ; //<2724>
       avs.Profile.0                  = avs.PlotPrefix    + acs.Profile.0                                   ; //<2725>
       avs.Line.1                     = avs.PlotPrefix    + acs.Line.1                                      ; //<2726>
       avs.Line.0                     = avs.PlotPrefix    + acs.Line.0                                      ; //<2727>
       avs.Line.Central               = avs.PlotPrefix    + acs.Line.Central                                ; //<2728>
       avs.Line.Average.1             = avs.PlotPrefix    + acs.Line.Average.1                              ; //<2729>
       avs.Line.Average.0             = avs.PlotPrefix    + acs.Line.Average.0                              ; //<2730>
                                                                                                              //<2731>
       avi.FlagOrderLevelsExist       = 0                                                                   ; //<2732>
       avi.FlagTimeBordersExist       = 0                                                                   ; //<2733>
       avi.FlagZoneLevelsExist        = 0                                                                   ; //<2734>
       avi.FlagProfilesExist          = 0                                                                   ; //<2735>
                                                                                                              //<2736>
       afr.DeletePlotObjects          ()                                                                    ; //<2737>
}                                                                                                             //<2738>
//</A.System.Extra: Control Module Function 2203 >````````````````````````````````````````````````````````````//<2739>
                                                                                                              //<2740>
//< A.System.Extra: Control Module Function 2203 >````````````````````````````````````````````````````````````//<2741>
int    afr.DeletePlotObjects          ()          //    - elements // input    - / code       - / output    - //<2742>
{                                                                                                             //<2743>
int    ali.PrefixDigits = StringLen                                   ( avs.PlotPrefix                    ) ; //<2744>
                                                                                                              //<2745>
int   i , N ; N         = ObjectsTotal ()       - 1                                                         ; //<2746>
for ( i = N ; i >= 0    ; i --                                     )                                          //<2747>
    { if ( StringLen    ( ObjectName  ( i ) )  >= ali.PrefixDigits )                                          //<2748>
      if ( StringSubstr ( ObjectName  ( i ) , 0 , ali.PrefixDigits ) == avs.PlotPrefix )                      //<2749>
           ObjectDelete ( ObjectName  ( i )                        )                                        ; //<2750>
    }                                                                                                         //<2751>
}                                                                                                             //<2752>
//</A.System.Extra: Control Module Function 2203 >````````````````````````````````````````````````````````````//<2753>
                                                                                                              //<2754>
//< A.System.Extra: Control Module Function 2203 >````````````````````````````````````````````````````````````//<2755>
int    afr.CreateSetup                ()          //   47 elements // input    - / code      47 / output    - //<2756>
{                                                                                                             //<2757>
       avs.SetupPrefix                = A.Property.Series         + A.Property.Release + ".Setup."          ; //<2758>
       avs.SetupBegin                 = avs.SetupPrefix           + "0.Begin.=============================" ; //<2759>
       avs.SetupAccountReserve        = avs.SetupPrefix           + "1.1." + "AccountReserve"               ; //<2760>
       avs.SetupOrderReserve          = avs.SetupPrefix           + "1.2." + "OrderReserve"                 ; //<2761>
       avs.SetupTrading               = avs.SetupPrefix           + "2.1." + "Trading"                      ; //<2762>
       avs.SetupTimeframe.1           = avs.SetupPrefix           + "2.2." + "Timeframe.1"                  ; //<2763>
       avs.SetupTimeframe.2           = avs.SetupPrefix           + "2.3." + "Timeframe.2"                  ; //<2764>
       avs.SetupParameter.1           = avs.SetupPrefix           + "2.4." + "Parameter.1"                  ; //<2765>
       avs.SetupParameter.2           = avs.SetupPrefix           + "2.5." + "Parameter.2"                  ; //<2766>
       avs.SetupParameter.3           = avs.SetupPrefix           + "2.6." + "Parameter.3"                  ; //<2767>
       avs.SetupMonitor.1             = avs.SetupPrefix           + "3.1." + arn.Panel    [ 0 ] + "1"       ; //<2768>
       avs.SetupFontSize.1            = avs.SetupPrefix           + "3.2." + arn.Panel    [ ari.FontSize  ] ; //<2769>
       avs.SetupFontColor.1           = avs.SetupPrefix           + "3.3." + arn.Panel    [ ari.FontColor ] ; //<2770>
       avs.SetupLineSpace.1           = avs.SetupPrefix           + "3.4." + arn.Panel    [ ari.LineSpace ] ; //<2771>
       avs.SetupPositionX.1           = avs.SetupPrefix           + "3.5." + arn.Panel    [ ari.PositionX ] ; //<2772>
       avs.SetupPositionY.1           = avs.SetupPrefix           + "3.6." + arn.Panel    [ ari.PositionY ] ; //<2773>
       avs.SetupMonitor.2             = avs.SetupPrefix           + "4.1." + arn.Panel    [ 0 ] + "2"       ; //<2774>
       avs.SetupFontSize.2            = avs.SetupPrefix           + "4.2." + arn.Panel    [ ari.FontSize  ] ; //<2775>
       avs.SetupFontColor.2           = avs.SetupPrefix           + "4.3." + arn.Panel    [ ari.FontColor ] ; //<2776>
       avs.SetupLineSpace.2           = avs.SetupPrefix           + "4.4." + arn.Panel    [ ari.LineSpace ] ; //<2777>
       avs.SetupPositionX.2           = avs.SetupPrefix           + "4.5." + arn.Panel    [ ari.PositionX ] ; //<2778>
       avs.SetupPositionY.2           = avs.SetupPrefix           + "4.6." + arn.Panel    [ ari.PositionY ] ; //<2779>
       avs.SetupOrderSelect           = avs.SetupPrefix           + "5.1." + "OrderSelect"                  ; //<2780>
       avs.SetupPlotOrderLevels       = avs.SetupPrefix           + "5.2." + "PlotOrderLevels"              ; //<2781>
       avs.SetupPlotTimeBorders       = avs.SetupPrefix           + "5.3." + "PlotTimeBorders"              ; //<2782>
       avs.SetupPlotZoneLevels        = avs.SetupPrefix           + "5.4." + "PlotZoneLevels"               ; //<2783>
       avs.SetupPlotProfiles          = avs.SetupPrefix           + "5.5." + "PlotProfiles"                 ; //<2784>
       avs.SetupProfileReset          = avs.SetupPrefix           + "6.1." + "ProfileReset"                 ; //<2785>
       avs.SetupZoneReset             = avs.SetupPrefix           + "6.2." + "ZoneReset"                    ; //<2786>
       avs.SetupZoneBasePoints        = avs.SetupPrefix           + "7.1." + "ZoneBasePoints"               ; //<2787>
       avs.SetupZoneStepPoints        = avs.SetupPrefix           + "7.2." + "ZoneStepPoints"               ; //<2788>
       avs.SetupEnd                   = avs.SetupPrefix           + "9.End.===============================" ; //<2789>
                                                                                                              //<2790>
       GlobalVariableSet              ( avs.SetupBegin            , aci.ReleaseNumber                     ) ; //<2791>
       GlobalVariableSet              ( avs.SetupAccountReserve   , avd.AccountReserve                    ) ; //<2792>
       GlobalVariableSet              ( avs.SetupOrderReserve     , avd.OrderReserve                      ) ; //<2793>
       GlobalVariableSet              ( avs.SetupTrading          , avi.TradingFlag                       ) ; //<2794>
       GlobalVariableSet              ( avs.SetupTimeframe.1      , avi.Timeframe.1                       ) ; //<2795>
       GlobalVariableSet              ( avs.SetupTimeframe.2      , avi.Timeframe.2                       ) ; //<2796>
       GlobalVariableSet              ( avs.SetupParameter.1      , avd.Parameter.1                       ) ; //<2797>
       GlobalVariableSet              ( avs.SetupParameter.2      , avd.Parameter.2                       ) ; //<2798>
       GlobalVariableSet              ( avs.SetupParameter.3      , avd.Parameter.3                       ) ; //<2799>
       GlobalVariableSet              ( avs.SetupMonitor.1        , avi.MonitorFlag.1                     ) ; //<2800>
       GlobalVariableSet              ( avs.SetupFontSize.1       , arv.Panel.1 [ ari.FontSize          ] ) ; //<2801>
       GlobalVariableSet              ( avs.SetupFontColor.1      , arv.Panel.1 [ ari.FontColor         ] ) ; //<2802>
       GlobalVariableSet              ( avs.SetupLineSpace.1      , arv.Panel.1 [ ari.LineSpace         ] ) ; //<2803>
       GlobalVariableSet              ( avs.SetupPositionX.1      , arv.Panel.1 [ ari.PositionX         ] ) ; //<2804>
       GlobalVariableSet              ( avs.SetupPositionY.1      , arv.Panel.1 [ ari.PositionY         ] ) ; //<2805>
       GlobalVariableSet              ( avs.SetupMonitor.2        , avi.MonitorFlag.2                     ) ; //<2806>
       GlobalVariableSet              ( avs.SetupFontSize.2       , arv.Panel.2 [ ari.FontSize          ] ) ; //<2807>
       GlobalVariableSet              ( avs.SetupFontColor.2      , arv.Panel.2 [ ari.FontColor         ] ) ; //<2808>
       GlobalVariableSet              ( avs.SetupLineSpace.2      , arv.Panel.2 [ ari.LineSpace         ] ) ; //<2809>
       GlobalVariableSet              ( avs.SetupPositionX.2      , arv.Panel.2 [ ari.PositionX         ] ) ; //<2810>
       GlobalVariableSet              ( avs.SetupPositionY.2      , arv.Panel.2 [ ari.PositionY         ] ) ; //<2811>
       GlobalVariableSet              ( avs.SetupOrderSelect      , avi.OrderSelect                       ) ; //<2812>
       GlobalVariableSet              ( avs.SetupPlotOrderLevels  , avi.PlotOrderLevels                   ) ; //<2813>
       GlobalVariableSet              ( avs.SetupPlotTimeBorders  , avi.PlotTimeBorders                   ) ; //<2814>
       GlobalVariableSet              ( avs.SetupPlotZoneLevels   , avi.PlotZoneLevels                    ) ; //<2815>
       GlobalVariableSet              ( avs.SetupPlotProfiles     , avi.PlotProfiles                      ) ; //<2816>
       GlobalVariableSet              ( avs.SetupProfileReset     , arv.1       [ ari.1.Reset           ] ) ; //<2817>
       GlobalVariableSet              ( avs.SetupZoneReset        , arv.Z       [ ari.Z.Reset           ] ) ; //<2818>
       GlobalVariableSet              ( avs.SetupZoneBasePoints   , avi.ZoneBasePoints                    ) ; //<2819>
       GlobalVariableSet              ( avs.SetupZoneStepPoints   , avi.ZoneStepPoints                    ) ; //<2820>
       GlobalVariableSet              ( avs.SetupEnd              , aci.ReleaseNumber                     ) ; //<2821>
}                                                                                                             //<2822>
//</A.System.Extra: Control Module Function 2203 >````````````````````````````````````````````````````````````//<2823>
                                                                                                              //<2824>
//< A.System.Extra: Control Module Function 2204 >````````````````````````````````````````````````````````````//<2825>
int    afr.DeleteSetup                ()          //    1 elements // input    - / code       1 / output    - //<2826>
{                                                                                                             //<2827>
GlobalVariablesDeleteAll              ( avs.SetupPrefix                                                   ) ; //<2828>
}                                                                                                             //<2829>
//</A.System.Extra: Control Module Function 2204 >````````````````````````````````````````````````````````````//<2830>
                                                                                                              //<2831>
//< A.System.Extra: Control Module Function 2205 >````````````````````````````````````````````````````````````//<2832>
int    afr.ResetSetup                 ()          //   33 elements // input    - / code      33 / output    - //<2833>
{                                                                                                             //<2834>
if ( ! GlobalVariableCheck ( avs.SetupFontSize.1         ) )                                                  //<2835>
       GlobalVariableSet   ( avs.SetupFontSize.1           , arv.Panel.1      [ ari.FontSize            ] ) ; //<2836>
else   arv.Panel.1         [ ari.FontSize                ] = GlobalVariableGet( avs.SetupFontSize.1       ) ; //<2837>
                                                                                                              //<2838>
if ( ! GlobalVariableCheck ( avs.SetupFontColor.1        ) )                                                  //<2839>
       GlobalVariableSet   ( avs.SetupFontColor.1          , arv.Panel.1      [ ari.FontColor           ] ) ; //<2840>
else   arv.Panel.1         [ ari.FontColor               ] = GlobalVariableGet( avs.SetupFontColor.1      ) ; //<2841>
                                                                                                              //<2842>
if ( ! GlobalVariableCheck ( avs.SetupLineSpace.1        ) )                                                  //<2843>
       GlobalVariableSet   ( avs.SetupLineSpace.1          , arv.Panel.1      [ ari.LineSpace           ] ) ; //<2844>
else   arv.Panel.1         [ ari.LineSpace               ] = GlobalVariableGet( avs.SetupLineSpace.1      ) ; //<2845>
                                                                                                              //<2846>
if ( ! GlobalVariableCheck ( avs.SetupPositionX.1        ) )                                                  //<2847>
       GlobalVariableSet   ( avs.SetupPositionX.1          , arv.Panel.1      [ ari.PositionX           ] ) ; //<2848>
else   arv.Panel.1         [ ari.PositionX               ] = GlobalVariableGet( avs.SetupPositionX.1      ) ; //<2849>
                                                                                                              //<2850>
if ( ! GlobalVariableCheck ( avs.SetupPositionY.1        ) )                                                  //<2851>
       GlobalVariableSet   ( avs.SetupPositionY.1          , arv.Panel.1      [ ari.PositionY           ] ) ; //<2852>
else   arv.Panel.1         [ ari.PositionY               ] = GlobalVariableGet( avs.SetupPositionY.1      ) ; //<2853>
                                                                                                              //<2854>
if ( ! GlobalVariableCheck ( avs.SetupFontSize.2         ) )                                                  //<2855>
       GlobalVariableSet   ( avs.SetupFontSize.2           , arv.Panel.2      [ ari.FontSize            ] ) ; //<2856>
else   arv.Panel.2         [ ari.FontSize                ] = GlobalVariableGet( avs.SetupFontSize.2       ) ; //<2857>
                                                                                                              //<2858>
if ( ! GlobalVariableCheck ( avs.SetupFontColor.2        ) )                                                  //<2859>
       GlobalVariableSet   ( avs.SetupFontColor.2          , arv.Panel.2      [ ari.FontColor           ] ) ; //<2860>
else   arv.Panel.2         [ ari.FontColor               ] = GlobalVariableGet( avs.SetupFontColor.2      ) ; //<2861>
                                                                                                              //<2862>
if ( ! GlobalVariableCheck ( avs.SetupLineSpace.2        ) )                                                  //<2863>
       GlobalVariableSet   ( avs.SetupLineSpace.2          , arv.Panel.2      [ ari.LineSpace           ] ) ; //<2864>
else   arv.Panel.2         [ ari.LineSpace               ] = GlobalVariableGet( avs.SetupLineSpace.2      ) ; //<2865>
                                                                                                              //<2866>
if ( ! GlobalVariableCheck ( avs.SetupPositionX.2        ) )                                                  //<2867>
       GlobalVariableSet   ( avs.SetupPositionX.2          , arv.Panel.2      [ ari.PositionX           ] ) ; //<2868>
else   arv.Panel.2         [ ari.PositionX               ] = GlobalVariableGet( avs.SetupPositionX.2      ) ; //<2869>
                                                                                                              //<2870>
if ( ! GlobalVariableCheck ( avs.SetupPositionY.2        ) )                                                  //<2871>
       GlobalVariableSet   ( avs.SetupPositionY.2          , arv.Panel.2      [ ari.PositionY           ] ) ; //<2872>
else   arv.Panel.2         [ ari.PositionY               ] = GlobalVariableGet( avs.SetupPositionY.2      ) ; //<2873>
                                                                                                              //<2874>
if ( ! GlobalVariableCheck ( avs.SetupOrderSelect        ) )                                                  //<2875>
       GlobalVariableSet   ( avs.SetupOrderSelect          , avi.OrderSelect                              ) ; //<2876>
else   avi.OrderSelect                                     = GlobalVariableGet( avs.SetupOrderSelect      ) ; //<2877>
                                                                                                              //<2878>
if ( ! GlobalVariableCheck ( avs.SetupPlotOrderLevels    ) )                                                  //<2879>
       GlobalVariableSet   ( avs.SetupPlotOrderLevels      , avi.PlotOrderLevels                          ) ; //<2880>
else   avi.PlotOrderLevels                                 = GlobalVariableGet( avs.SetupPlotOrderLevels  ) ; //<2881>
                                                                                                              //<2882>
if ( ! GlobalVariableCheck ( avs.SetupPlotTimeBorders    ) )                                                  //<2883>
       GlobalVariableSet   ( avs.SetupPlotTimeBorders      , avi.PlotTimeBorders                          ) ; //<2884>
else   avi.PlotTimeBorders                                 = GlobalVariableGet( avs.SetupPlotTimeBorders  ) ; //<2885>
                                                                                                              //<2886>
if ( ! GlobalVariableCheck ( avs.SetupPlotZoneLevels     ) )                                                  //<2887>
       GlobalVariableSet   ( avs.SetupPlotZoneLevels       , avi.PlotZoneLevels                           ) ; //<2888>
else   avi.PlotZoneLevels                                  = GlobalVariableGet( avs.SetupPlotZoneLevels   ) ; //<2889>
                                                                                                              //<2890>
if ( ! GlobalVariableCheck ( avs.SetupPlotProfiles       ) )                                                  //<2891>
       GlobalVariableSet   ( avs.SetupPlotProfiles         , avi.PlotProfiles                             ) ; //<2892>
else   avi.PlotProfiles                                    = GlobalVariableGet( avs.SetupPlotProfiles     ) ; //<2893>
                                                                                                              //<2894>
if ( ! GlobalVariableCheck ( avs.SetupProfileReset       ) )                                                  //<2895>
       GlobalVariableSet   ( avs.SetupProfileReset         , arv.1            [ ari.1.Reset             ] ) ; //<2896>
else   arv.1               [ ari.1.Reset                 ] = GlobalVariableGet( avs.SetupProfileReset     ) ; //<2897>
                                                                                                              //<2898>
if ( ! GlobalVariableCheck ( avs.SetupZoneReset          ) )                                                  //<2899>
       GlobalVariableSet   ( avs.SetupZoneReset            , arv.Z            [ ari.Z.Reset             ] ) ; //<2900>
else   arv.Z               [ ari.Z.Reset                 ] = GlobalVariableGet( avs.SetupZoneReset        ) ; //<2901>
                                                                                                              //<2902>
if ( ! GlobalVariableCheck ( avs.SetupZoneBasePoints     ) )                                                  //<2903>
       GlobalVariableSet   ( avs.SetupZoneBasePoints       , avi.ZoneBasePoints                           ) ; //<2904>
else   avi.ZoneBasePoints                                  = GlobalVariableGet( avs.SetupZoneBasePoints   ) ; //<2905>
                                                                                                              //<2906>
if ( ! GlobalVariableCheck ( avs.SetupZoneStepPoints     ) )                                                  //<2907>
       GlobalVariableSet   ( avs.SetupZoneStepPoints       , avi.ZoneBasePoints                           ) ; //<2908>
else   avi.ZoneStepPoints                                  = GlobalVariableGet( avs.SetupZoneStepPoints   ) ; //<2909>
}                                                                                                             //<2910>
//</A.System.Extra: Control Module Function 2205 >````````````````````````````````````````````````````````````//<2911>
                                                                                                              //<2912>
//< A.System.Extra: Control Module Function 2203 >````````````````````````````````````````````````````````````//<2913>
int    afr.ResetChartMetrics          ()          //    - elements // input    - / code       - / output    - //<2914>
{                                                                                                             //<2915>
arv.Chart   [ ari.TimeZero         ]  = EMPTY                                                               ; //<2916>
arv.Chart   [ ari.TimeRight        ]  = EMPTY                                                               ; //<2917>
arv.Chart   [ ari.TimeLeft         ]  = EMPTY                                                               ; //<2918>
arv.Chart   [ ari.BarRight         ]  = EMPTY                                                               ; //<2919>
arv.Chart   [ ari.BarLeft          ]  = EMPTY                                                               ; //<2920>
arv.Chart   [ ari.BarsShift        ]  = EMPTY                                                               ; //<2921>
arv.Chart   [ ari.BarsTotal        ]  = EMPTY                                                               ; //<2922>
arv.Chart   [ ari.Resolution.H     ]  = EMPTY                                                               ; //<2923>
arv.Chart   [ ari.PriceMax         ]  = EMPTY                                                               ; //<2924>
arv.Chart   [ ari.PriceMin         ]  = EMPTY                                                               ; //<2925>
arv.Chart   [ ari.PriceRange       ]  = EMPTY                                                               ; //<2926>
arv.Chart   [ ari.Resolution.V     ]  = EMPTY                                                               ; //<2927>
}                                                                                                             //<2928>
//</A.System.Extra: Control Module Function 2203 >````````````````````````````````````````````````````````````//<2929>
                                                                                                              //<2930>
//< A.System.Extra: Control Module Function 2206 >````````````````````````````````````````````````````````````//<2931>
int    afr.MeasureChart               ()          //    - elements // input    - / code       - / output    - //<2932>
{                                                                                                             //<2933>
arv.Chart      [ ari.Resolution.H   ] = WindowBarsPerChart                                               () ; //<2934>
arv.Chart      [ ari.BarLeft        ] = WindowFirstVisibleBar                                            () ; //<2935>
                                                                                                              //<2936>
if ( arv.Chart [ ari.BarLeft        ] < arv.Chart [ ari.Resolution.H ] )                                      //<2937>
     arv.Chart [ ari.BarRight       ] = 0                                                                   ; //<2938>
else arv.Chart [ ari.BarRight       ] = arv.Chart [ ari.BarLeft      ] - arv.Chart [ ari.Resolution.H ]     ; //<2939>
                                                                                                              //<2940>
arv.Chart      [ ari.BarsTotal      ] = arv.Chart [ ari.BarLeft      ] - arv.Chart [ ari.BarRight     ] + 1 ; //<2941>
arv.Chart      [ ari.BarsShift      ] = arv.Chart [ ari.Resolution.H ] - arv.Chart [ ari.BarsTotal    ]     ; //<2942>
                                                                                                              //<2943>
arv.Chart      [ ari.TimeZero       ] = iTime      ( aes.Symbol , PERIOD_M1 , 0                           ) ; //<2944>
int              ali.IndexRight       = arv.Chart [ ari.BarRight     ]                                      ; //<2945>
int              ali.IndexLeft        = arv.Chart [ ari.BarLeft      ]                                      ; //<2946>
arv.Chart      [ ari.TimeRight      ] = Time      [ ali.IndexRight   ]                                      ; //<2947>
arv.Chart      [ ari.TimeLeft       ] = Time      [ ali.IndexLeft    ]                                      ; //<2948>
                                                                                                              //<2949>
arv.Chart      [ ari.PriceMax       ] = WindowPriceMax                                                   () ; //<2950>
arv.Chart      [ ari.PriceMin       ] = WindowPriceMin                                                   () ; //<2951>
arv.Chart      [ ari.PriceRange     ] = arv.Chart [ ari.PriceMax     ] - arv.Chart [ ari.PriceMin     ]     ; //<2952>
if ( Point > 0 )                                                                                              //<2953>
     arv.Chart [ ari.Resolution.V   ] = arv.Chart [ ari.PriceRange   ] / Point                              ; //<2954>
else arv.Chart [ ari.Resolution.V   ] = 2                                                                   ; //<2955>
}                                                                                                             //<2956>
//</A.System.Extra: Control Module Function 2206 >````````````````````````````````````````````````````````````//<2957>
                                                                                                              //<2958>
//< A.System.Extra: Control Module Function 2206 >````````````````````````````````````````````````````````````//<2959>
int    afr.Monitoring                 ()          //   22 elements // input    - / code      22 / output    - //<2960>
{                                                                                                             //<2961>
avi.MonitoringRunTime                 = GetTickCount ()                                                     ; //<2962>
//< 2206.2. Monitoring Panel.1 Control 10 >                                                                   //<2963>
if ( ! GlobalVariableCheck ( avs.SetupMonitor.1      ) )                                                      //<2964>
       GlobalVariableSet   ( avs.SetupMonitor.1        , avi.MonitorFlag.1                                ) ; //<2965>
                                                                                                              //<2966>
if (   GlobalVariableGet   ( avs.SetupMonitor.1 ) == 1 )                                                      //<2967>
   {   if ( avi.MonitorFlag.1                     == 0 )                                                      //<2968>
          { avi.MonitorFlag.1                      = 1                                                    ;   //<2969>
            afr.CreatePanel.1                                                                          () ; } //<2970>
       afr.ResetPanel.1                                                                                () ; } //<2971>
                                                                                                              //<2972>
else   if ( avi.MonitorFlag.1                     == 1 )                                                      //<2973>
          { avi.MonitorFlag.1                      = 0                                                    ;   //<2974>
            afr.DeletePanel.1                                                                          () ; } //<2975>
//</2206.2. Monitoring Panel.1 Control 10 >                                                                   //<2976>
                                                                                                              //<2977>
//< 2206.3. Monitoring Panel.2 Control 10 >                                                                   //<2978>
if ( ! GlobalVariableCheck ( avs.SetupMonitor.2      ) )                                                      //<2979>
       GlobalVariableSet   ( avs.SetupMonitor.2        , avi.MonitorFlag.2                                ) ; //<2980>
                                                                                                              //<2981>
if (   GlobalVariableGet   ( avs.SetupMonitor.2 ) == 1 )                                                      //<2982>
   {   if ( avi.MonitorFlag.2                     == 0 )                                                      //<2983>
          { avi.MonitorFlag.2                      = 1                                                    ;   //<2984>
            afr.CreatePanel.2                                                                          () ; } //<2985>
       afr.ResetPanel.2                                                                                () ; } //<2986>
                                                                                                              //<2987>
else   if ( avi.MonitorFlag.2                     == 1 )                                                      //<2988>
          { avi.MonitorFlag.2                      = 0                                                    ;   //<2989>
            afr.DeletePanel.2                                                                          () ; } //<2990>
//</2206.3. Monitoring Panel.2 Control 10 >                                                                   //<2991>
                                                                                                              //<2992>
//< 2206.4. Reset Plotter >                                                                                   //<2993>
afr.ResetPlotter ()                                                                                         ; //<2994>
//< 2206.4. Reset Plotter >                                                                                   //<2995>
}                                                                                                             //<2996>
//</A.System.Extra: Control Module Function 2206 >````````````````````````````````````````````````````````````//<2997>
                                                                                                              //<2998>
//< A.System.Extra: Control Module Function 2207 >````````````````````````````````````````````````````````````//<2999>
int    afr.CreatePanel.1              ()          //    5 elements // input    - / code       5 / output    - //<3000>
{                                                                                                             //<3001>
static int     i , N =                  aci.TextLines                                                       ; //<3002>
for  ( i = 0 ; i < N ;       i ++ )                                                                           //<3003>
     { avs.TextBuffer.1    [ i ]      = ""                                                                  ; //<3004>
       avs.BufferName.1    [ i ]      = A.Property.Series + A.Property.Release + ".TextBuffer.1." + i       ; //<3005>
       afr.ResetTextLine.1 ( i )                                                                          ; } //<3006>
}                                                                                                             //<3007>
//</A.System.Extra: Control Module Function 2207 >````````````````````````````````````````````````````````````//<3008>
                                                                                                              //<3009>
//< A.System.Extra: Control Module Function 2208 >````````````````````````````````````````````````````````````//<3010>
int    afr.CreatePanel.2              ()          //    5 elements // input    - / code       5 / output    - //<3011>
{                                                                                                             //<3012>
static int     i , N =                  aci.TextLines                                                       ; //<3013>
for  ( i = 0 ; i < N ;       i ++ )                                                                           //<3014>
     { avs.TextBuffer.2    [ i ]      = ""                                                                  ; //<3015>
       avs.BufferName.2    [ i ]      = A.Property.Series + A.Property.Release + ".TextBuffer.2." + i       ; //<3016>
       afr.ResetTextLine.2 ( i )                                                                          ; } //<3017>
}                                                                                                             //<3018>
//</A.System.Extra: Control Module Function 2208 >````````````````````````````````````````````````````````````//<3019>
                                                                                                              //<3020>
//< A.System.Extra: Control Module Function 2209 >````````````````````````````````````````````````````````````//<3021>
int    afr.DeletePanel.1              ()          //    3 elements // input    - / code       3 / output    - //<3022>
{                                                                                                             //<3023>
static int     i , N =                  aci.TextLines                                                       ; //<3024>
for  ( i = 0 ; i < N ; i ++ )                                                                                 //<3025>
     { ObjectDelete                   ( avs.BufferName.1 [ i ] )                                          ; } //<3026>
}                                                                                                             //<3027>
//</A.System.Extra: Control Module Function 2209 >````````````````````````````````````````````````````````````//<3028>
                                                                                                              //<3029>
//< A.System.Extra: Control Module Function 2210 >````````````````````````````````````````````````````````````//<3030>
int    afr.DeletePanel.2              ()          //    3 elements // input    - / code       3 / output    - //<3031>
{                                                                                                             //<3032>
static int     i , N =                  aci.TextLines                                                       ; //<3033>
for  ( i = 0 ; i < N ; i ++ )                                                                                 //<3034>
     { ObjectDelete                   ( avs.BufferName.2 [ i ] )                                          ; } //<3035>
}                                                                                                             //<3036>
//</A.System.Extra: Control Module Function 2210 >````````````````````````````````````````````````````````````//<3037>
                                                                                                              //<3038>
//< A.System.Extra: Control Module Function 2211 >````````````````````````````````````````````````````````````//<3039>
int    afr.ResetPanel.1               ()          //    7 elements // input    - / code       7 / output    - //<3040>
{                                                                                                             //<3041>
static int     i , N =                  aci.TextLines                                                       ; //<3042>
                                                                                                              //<3043>
for  ( i = 0 ; i < N ;       i ++ )                                                                           //<3044>
     { avs.TextBuffer.1    [ i ]      = ""                                                                  ; //<3045>
       afr.SetTextLine.1   ( i )                                                                          ; } //<3046>
                                                                                                              //<3047>
afr.Report.1                          ()                                                                    ; //<3048>
                                                                                                              //<3049>
for  ( i = 0 ; i < N ;       i ++ )                                                                           //<3050>
       afr.SetTextLine.1   ( i )                                                                            ; //<3051>
}                                                                                                             //<3052>
//</A.System.Extra: Control Module Function 2211 >````````````````````````````````````````````````````````````//<3053>
                                                                                                              //<3054>
//< A.System.Extra: Control Module Function 2212 >````````````````````````````````````````````````````````````//<3055>
int    afr.ResetPanel.2               ()          //    7 elements // input    - / code       7 / output    - //<3056>
{                                                                                                             //<3057>
static int     i , N =                  aci.TextLines                                                       ; //<3058>
                                                                                                              //<3059>
for  ( i = 0 ; i < N ;       i ++ )                                                                           //<3060>
     { avs.TextBuffer.2    [ i ]      = ""                                                                  ; //<3061>
       afr.SetTextLine.2   ( i )                                                                          ; } //<3062>
                                                                                                              //<3063>
afr.Report.2                          ()                                                                    ; //<3064>
                                                                                                              //<3065>
for  ( i = 0 ; i < N ;       i ++ )                                                                           //<3066>
       afr.SetTextLine.2   ( i )                                                                            ; //<3067>
}                                                                                                             //<3068>
//</A.System.Extra: Control Module Function 2212 >````````````````````````````````````````````````````````````//<3069>
                                                                                                              //<3070>
//< A.System.Extra: 4.2. Monitoring Module Function 4213 >````````````````````````````````````````````````````//<3071>
//int  afr.Reserved                  ()           //    0 elements // input    - / code       - / output    - //<3072>
//</A.System.Extra: 4.2. Monitoring Module Function 4213 >````````````````````````````````````````````````````//<3073>
                                                                                                              //<3074>
//< A.System.Extra: Control Module Function 2214 >````````````````````````````````````````````````````````````//<3075>
int    afr.ResetTextLine.1            (           //    9 elements // input    1 / code       8 / output    - //<3076>
       int     aai.Line                )                                                                      //<3077>
{                                                                                                             //<3078>
static string   als.Name    ; als.Name = avs.BufferName.1 [ aai.Line ]                                      ; //<3079>
                                                                                                              //<3080>
ObjectCreate  ( als.Name    , OBJ_LABEL             , 0 , 0 , 0                                           ) ; //<3081>
ObjectSet     ( als.Name    , OBJPROP_XDISTANCE     , arv.Panel.1  [ ari.FontSize  ]                          //<3082>
                                                    * arv.Panel.1  [ ari.PositionX ]                      ) ; //<3083>
ObjectSet     ( als.Name    , OBJPROP_YDISTANCE     , arv.Panel.1  [ ari.FontSize  ] *                        //<3084>
              ( arv.Panel.1 [ ari.PositionY ]       + aai.Line     * arv.Panel.1   [ ari.LineSpace ]    ) ) ; //<3085>
                                                                                                              //<3086>
ObjectSetText ( als.Name    , avs.TextBuffer.1      [ aai.Line   ] ,                                          //<3087>
                arv.Panel.1 [ ari.FontSize  ]       , acs.FontName , arv.Panel.1   [ ari.FontColor ]      ) ; //<3088>
}                                                                                                             //<3089>
//</A.System.Extra: Control Module Function 2214 >````````````````````````````````````````````````````````````//<3090>
                                                                                                              //<3091>
//< A.System.Extra: Control Module Function 2215 >````````````````````````````````````````````````````````````//<3092>
int    afr.ResetTextLine.2            (           //    9 elements // input    1 / code       8 / output    - //<3093>
       int     aai.Line                )                                                                      //<3094>
{                                                                                                             //<3095>
static string   als.Name    ; als.Name = avs.BufferName.2 [ aai.Line ]                                      ; //<3096>
                                                                                                              //<3097>
ObjectCreate  ( als.Name    , OBJ_LABEL             , 0 , 0 , 0                                           ) ; //<3098>
ObjectSet     ( als.Name    , OBJPROP_XDISTANCE     , arv.Panel.2  [ ari.FontSize  ]                          //<3099>
                                                    * arv.Panel.2  [ ari.PositionX ]                      ) ; //<3100>
ObjectSet     ( als.Name    , OBJPROP_YDISTANCE     , arv.Panel.2  [ ari.FontSize  ] *                        //<3101>
              ( arv.Panel.2 [ ari.PositionY ]       + aai.Line     * arv.Panel.2   [ ari.LineSpace ]    ) ) ; //<3102>
                                                                                                              //<3103>
ObjectSetText ( als.Name    , avs.TextBuffer.2      [ aai.Line   ] ,                                          //<3104>
                arv.Panel.2 [ ari.FontSize  ]       , acs.FontName , arv.Panel.2   [ ari.FontColor ]      ) ; //<3105>
}                                                                                                             //<3106>
//</A.System.Extra: Control Module Function 2215 >````````````````````````````````````````````````````````````//<3107>
                                                                                                              //<3108>
//< A.System.Extra: Control Module Function 2216 >````````````````````````````````````````````````````````````//<3109>
int    afr.SetTextLine.1              (           //    4 elements // input    1 / code       3 / output    - //<3110>
       int     aai.Line                )                                                                      //<3111>
{                                                                                                             //<3112>
static string   als.Name    ; als.Name = avs.BufferName.1 [ aai.Line ]                                      ; //<3113>
                                                                                                              //<3114>
ObjectSetText ( als.Name    , avs.TextBuffer.1      [ aai.Line   ] ,                                          //<3115>
                arv.Panel.1 [ ari.FontSize  ]       , acs.FontName , arv.Panel.1   [ ari.FontColor ]      ) ; //<3116>
}                                                                                                             //<3117>
//</A.System.Extra: Control Module Function 2216 >````````````````````````````````````````````````````````````//<3118>
                                                                                                              //<3119>
//< A.System.Extra: Control Module Function 2217 >````````````````````````````````````````````````````````````//<3120>
int    afr.SetTextLine.2              (           //    4 elements // input    1 / code       3 / output    - //<3121>
       int     aai.Line                )                                                                      //<3122>
{                                                                                                             //<3123>
static string   als.Name    ; als.Name = avs.BufferName.2 [ aai.Line ]                                      ; //<3124>
                                                                                                              //<3125>
ObjectSetText ( als.Name    , avs.TextBuffer.2      [ aai.Line   ] ,                                          //<3126>
                arv.Panel.2 [ ari.FontSize  ]       , acs.FontName , arv.Panel.2   [ ari.FontColor ]      ) ; //<3127>
}                                                                                                             //<3128>
//</A.System.Extra: Control Module Function 2217 >````````````````````````````````````````````````````````````//<3129>
                                                                                                              //<3130>
//< A.System.Extra: Control Module Function 2218 >````````````````````````````````````````````````````````````//<3131>
int    afr.SetText.1                  (           //   14 elements // input    4 / code      10 / output    - //<3132>
       int     aai.Line                ,                                                                      //<3133>
       int     aai.Position            ,                                                                      //<3134>
       int     aai.Indent              ,                                                                      //<3135>
       string  aas.Text                )                                                                      //<3136>
{                                                                                                             //<3137>
static int     ali.Begin        ; ali.Begin        = aai.Position     - StringLen ( aas.Text ) * aai.Indent ; //<3138>
                                                                                                              //<3139>
if           ( aai.Indent  == 0 ) ali.Begin        --                                                       ; //<3140>
                                                                                                              //<3141>
if           ( ali.Begin   <= 0 ) avs.TextBuffer.1 [ aai.Line ]       = aas.Text                            ; //<3142>
                                                                                                              //<3143>
else { int     ali.BufferLength = StringLen        ( avs.TextBuffer.1 [ aai.Line         ]     )            ; //<3144>
       if    ( ali.Begin        > ali.BufferLength )                                                          //<3145>
               avs.TextBuffer.1 [ aai.Line       ] = avs.TextBuffer.1 [ aai.Line         ]     +              //<3146>
                   StringSubstr ( acs.Blank  , 0 ,   ali.Begin        - ali.BufferLength )     + aas.Text   ; //<3147>
       else    avs.TextBuffer.1 [ aai.Line       ] =                                                          //<3148>
                   StringSubstr ( avs.TextBuffer.1 [ aai.Line ]   , 0 , ali.Begin        )     + aas.Text   ; //<3149>
     }                                                                                                        //<3150>
}                                                                                                             //<3151>
//</A.System.Extra: Control Module Function 2218 >````````````````````````````````````````````````````````````//<3152>
                                                                                                              //<3153>
//< A.System.Extra: Control Module Function 2219 >````````````````````````````````````````````````````````````//<3154>
int    afr.SetText.2                  (           //   14 elements // input    4 / code      10 / output    - //<3155>
       int     aai.Line                ,                                                                      //<3156>
       int     aai.Position            ,                                                                      //<3157>
       int     aai.Indent              ,                                                                      //<3158>
       string  aas.Text                )                                                                      //<3159>
{                                                                                                             //<3160>
static int     ali.Begin        ; ali.Begin        = aai.Position     - StringLen ( aas.Text ) * aai.Indent ; //<3161>
                                                                                                              //<3162>
if           ( aai.Indent  == 0 ) ali.Begin        --                                                       ; //<3163>
                                                                                                              //<3164>
if           ( ali.Begin   <= 0 ) avs.TextBuffer.2 [ aai.Line ]       = aas.Text                            ; //<3165>
                                                                                                              //<3166>
else { int     ali.BufferLength = StringLen        ( avs.TextBuffer.2 [ aai.Line         ]     )            ; //<3167>
       if    ( ali.Begin        > ali.BufferLength )                                                          //<3168>
               avs.TextBuffer.2 [ aai.Line       ] = avs.TextBuffer.2 [ aai.Line         ]     +              //<3169>
                   StringSubstr ( acs.Blank  , 0 ,   ali.Begin        - ali.BufferLength )     + aas.Text   ; //<3170>
       else    avs.TextBuffer.2 [ aai.Line       ] =                                                          //<3171>
                   StringSubstr ( avs.TextBuffer.2 [ aai.Line ]   , 0 , ali.Begin        )     + aas.Text   ; //<3172>
     }                                                                                                        //<3173>
}                                                                                                             //<3174>
//</A.System.Extra: Control Module Function 2219 >````````````````````````````````````````````````````````````//<3175>
                                                                                                              //<3176>
//< A.System.Extra: Control Module Function 2220 >````````````````````````````````````````````````````````````//<3177>
int    afr.Report.1                   ()          //  198 elements // input    - / code     198 / output    - //<3178>
{                                                                                                             //<3179>
//< 4216.1. Header 3 >                                                                                        //<3180>
static int ali.Trigger ; if ( ! ali.Trigger )    { ali.Trigger = 1 ;             static string als.Header ;   //<3181>
       als.Header            = A.Property.Series + A.Property.Release + " " + A.Property.Program          ; } //<3182>
                                                                                                              //<3183>
afr.SetText.1 (   0 ,  1 , 0 , als.Header + ": " + avs.SystemMessage                                      ) ; //<3184>
//</4216.1. Header 3 >                                                                                        //<3185>
                                                                                                              //<3186>
//< 4216.2. Currency Set Initialization 1 >                                                                   //<3187>
afr.CurrencyDetector         ( aes.Symbol        , arn.Currency )                                           ; //<3188>
//</4216.2. Currency Set Initialization 1 >                                                                   //<3189>
                                                                                                              //<3190>
//< 4216.3. First Cluster: System Report 11 >                                                                 //<3191>
afr.SetText.1 (   2 ,  1 , 0 , "Client Time: "   + afs.Time ( TimeLocal   ()               , 1 )          ) ; //<3192>
afr.SetText.1 (   3 ,  1 , 0 , "Client Name: "   + AccountName                                         () ) ; //<3193>
afr.SetText.1 (   4 ,  1 , 0 , "Server Name: "   + AccountServer                                       () ) ; //<3194>
afr.SetText.1 (   5 ,  1 , 0 , "Server Time: "   + afs.Time ( TimeCurrent ()               , 1 )          ) ; //<3195>
                                                                                                              //<3196>
afr.SetText.1 (   2 , 48 , 1 , afs.Interval      ( TimeLocal ()    - avi.TimeStart         , 1 ) + " / "  ) ; //<3197>
afr.SetText.1 (   5 , 48 , 1 , afs.Interval      ( TimeLocal ()    - avi.TimeLastRun       , 1 ) + " / "  ) ; //<3198>
                                                                                                              //<3199>
afr.SetText.1 (   2 , 49 , 0 , avi.BuyTrades     + "+"             +                                          //<3200>
                               avi.SellTrades    + "="             +                                          //<3201>
                               avi.TotalTrades   + "/"             + avi.Trailes                          ) ; //<3202>
                                                                                                              //<3203>
afr.SetText.1 (   5 , 49 , 0 , avi.Runs          + "/"             +                                          //<3204>
                               avi.AttemptsTrade + "/"             + avi.AttemptsTrail                    ) ; //<3205>
//</4216.3. First Cluster: System Report 11 >                                                                 //<3206>
                                                                                                              //<3207>
//< 4216.4. Second Cluster: Capital Management Report 51 >                                                    //<3208>
double ald.DrawdownAbs       = AccountEquity     ()   - avd.PeakEquity                                      ; //<3209>
double ald.DrawdownRel       = ald.DrawdownAbs        / avd.PeakEquity                                      ; //<3210>
double ald.CapitalAbs        = avd.PeakEquity         * ( 1 - avd.AccountReserve )                          ; //<3211>
double ald.CapitalRel        = 1 - avd.AccountReserve                                                       ; //<3212>
double ald.CapitalGainAbs    = avd.Capital            - avd.InitialCapital                                  ; //<3213>
double ald.CapitalGainRel    = ald.CapitalGainAbs     / avd.InitialCapital * 100                            ; //<3214>
double ald.EquityGainAbs     = AccountEquity     ()   - avd.InitialEquity                                   ; //<3215>
double ald.EquityGainRel     = ald.EquityGainAbs      / avd.InitialEquity  * 100                            ; //<3216>
double ald.EquityReserveAbs  = AccountEquity     ()   - ald.CapitalAbs                                      ; //<3217>
double ald.EquityReserveRel  = ald.EquityReserveAbs   / avd.PeakEquity                                      ; //<3218>
double ald.AccountEquityAbs  = AccountEquity     ()                                                         ; //<3219>
double ald.AccountEquityRel  = AccountEquity     ()   / avd.PeakEquity                                      ; //<3220>
double ald.AccountFreeMargin = AccountFreeMargin ()                                                         ; //<3221>
double ald.MarginLevel                                                                                      ; //<3222>
string als.StopoutLevelAbs                                                                                  ; //<3223>
string als.StopoutLevelRel                                                                                  ; //<3224>
                                                                                                              //<3225>
if   ( AccountMargin () > 0 )  ald.MarginLevel    = AccountEquity () / AccountMargin ()                     ; //<3226>
else                           ald.MarginLevel    = 0                                                       ; //<3227>
                                                                                                              //<3228>
if   ( AccountStopoutMode () == 0 )                                                                           //<3229>
     { als.StopoutLevelAbs   = DoubleToStr  ( AccountStopoutLevel () * AccountEquity () / 100 , 2 )         ; //<3230>
       als.StopoutLevelRel   =              + AccountStopoutLevel () + ".00%"                             ; } //<3231>
else                                                                                                          //<3232>
     { als.StopoutLevelAbs   =                AccountStopoutLevel () + ".00"                                ; //<3233>
       als.StopoutLevelRel   = DoubleToStr  ( AccountStopoutLevel () / AccountEquity () * 100 , 2 ) + "%" ; } //<3234>
                                                                                                              //<3235>
if   ( ald.EquityGainAbs     > 0 )     string als.GainSign = "+"                                            ; //<3236>
else                                          als.GainSign = ""                                             ; //<3237>
                                                                                                              //<3238>
afr.SetText.1 (   7 ,  1 , 0 , "Capital "   + arn.Currency           [ ari.Account    ]             + ":" ) ; //<3239>
afr.SetText.1 (   8 ,  1 , 0 , "Reserve:"                                                                 ) ; //<3240>
afr.SetText.1 (   9 ,  1 , 0 , "Peak Equity:"                                                             ) ; //<3241>
afr.SetText.1 (  10 ,  1 , 0 , "Drawdown:"                                                                ) ; //<3242>
afr.SetText.1 (  11 ,  1 , 0 , "Acc. Equity:"                                                             ) ; //<3243>
afr.SetText.1 (  12 ,  1 , 0 , "Free Margin:"                                                             ) ; //<3244>
                                                                                                              //<3245>
afr.SetText.1 (   7 , 23 , 1 , DoubleToStr  ( ald.CapitalAbs                      , 2          )          ) ; //<3246>
afr.SetText.1 (   8 , 23 , 1 , DoubleToStr  ( ald.EquityReserveAbs                , 2          )          ) ; //<3247>
afr.SetText.1 (   9 , 23 , 1 , DoubleToStr  ( avd.PeakEquity                      , 2          )          ) ; //<3248>
afr.SetText.1 (  10 , 23 , 1 , DoubleToStr  ( ald.DrawdownAbs                     , 2          )          ) ; //<3249>
afr.SetText.1 (  11 , 23 , 1 , DoubleToStr  ( ald.AccountEquityAbs                , 2          )          ) ; //<3250>
afr.SetText.1 (  12 , 23 , 1 , DoubleToStr  ( ald.AccountFreeMargin               , 2          )          ) ; //<3251>
                                                                                                              //<3252>
afr.SetText.1 (   7 , 32 , 1 , DoubleToStr  ( ald.CapitalRel       * 100          , 2          ) + "%"    ) ; //<3253>
afr.SetText.1 (   7 , 45 , 1 , "+"          + DoubleToStr ( ald.CapitalGainAbs    , 2          )          ) ; //<3254>
afr.SetText.1 (   7 , 55 , 1 , "+"          + DoubleToStr ( ald.CapitalGainRel    , 2          ) + "%"    ) ; //<3255>
afr.SetText.1 (   8 , 32 , 1 , DoubleToStr  ( ald.EquityReserveRel * 100          , 2          ) + "%"    ) ; //<3256>
afr.SetText.1 (   9 , 32 , 1 , DoubleToStr  (                        100          , 2          ) + "%"    ) ; //<3257>
afr.SetText.1 (   9 , 45 , 1 , afs.Interval ( TimeLocal ()         - avd.PeakTime , 1          )          ) ; //<3258>
afr.SetText.1 (  10 , 32 , 1 , DoubleToStr  ( ald.DrawdownRel      * 100          , 2          ) + "%"    ) ; //<3259>
afr.SetText.1 (  11 , 32 , 1 , DoubleToStr  ( ald.AccountEquityRel * 100          , 2          ) + "%"    ) ; //<3260>
afr.SetText.1 (  11 , 45 , 1 , als.GainSign + DoubleToStr ( ald.EquityGainAbs , 2 )                       ) ; //<3261>
afr.SetText.1 (  11 , 55 , 1 , als.GainSign + DoubleToStr ( ald.EquityGainRel , 2 ) + "%"                 ) ; //<3262>
afr.SetText.1 (  12 , 32 , 1 , DoubleToStr  ( ald.MarginLevel      * 100          , 2          ) + "%"    ) ; //<3263>
afr.SetText.1 (  12 , 45 , 1 , als.StopoutLevelAbs                                                        ) ; //<3264>
afr.SetText.1 (  12 , 55 , 1 , als.StopoutLevelRel                                                        ) ; //<3265>
//</4216.4. Second Cluster: Capital Management Report 51 >                                                    //<3266>
                                                                                                              //<3267>
//< 4216.5. Third Cluster: Position Management Report 93 >                                                    //<3268>
double ald.VARLimit      = AccountEquity ()  * avd.OrderReserve                                             ; //<3269>
int    ali.LotSize       = MarketInfo        ( aes.Symbol          , MODE_LOTSIZE                         ) ; //<3270>
double ald.NominalPoint  = avd.NominalTick   * avd.QuotePoint      / avd.QuoteTick                          ; //<3271>
int    ali.MarginPoints                                                                                     ; //<3272>
if   ( ald.NominalPoint  > 0 )                                                                                //<3273>
       ali.MarginPoints  = MathRound         ( avd.NominalMargin   / ald.NominalPoint                     ) ; //<3274>
else   ali.MarginPoints  = 0                                                                                ; //<3275>
                                                                                                              //<3276>
string als.OrderCurrency [] = { "" , "" , "" , ""                                                         } ; //<3277>
                                                                                                              //<3278>
string als.OrderData     = ""                                                                               ; //<3279>
double ald.ContractSize  = 0                                                                                ; //<3280>
double ald.ContractValue = 0                                                                                ; //<3281>
double ald.OrderPoint    = 0                                                                                ; //<3282>
int    ali.OrderLifetime = 0                                                                                ; //<3283>
                                                                                                              //<3284>
double ald.QuotePrice    = 0                                                                                ; //<3285>
double ald.QuoteTake     = 0                                                                                ; //<3286>
double ald.QuoteStop     = 0                                                                                ; //<3287>
double ald.QuoteTarget   = 0                                                                                ; //<3288>
double ald.QuoteVAR      = 0                                                                                ; //<3289>
                                                                                                              //<3290>
int    ali.OrderProfit   = 0                                                                                ; //<3291>
int    ali.OrderTarget   = 0                                                                                ; //<3292>
int    ali.OrderVAR      = 0                                                                                ; //<3293>
int    ali.OrderLimit    = 0                                                                                ; //<3294>
                                                                                                              //<3295>
double ald.OrderProfit   = 0                                                                                ; //<3296>
double ald.OrderTarget   = 0                                                                                ; //<3297>
double ald.OrderVAR      = 0                                                                                ; //<3298>
double ald.OrderLimit    = 0                                                                                ; //<3299>
                                                                                                              //<3300>
int    ali.OrderTotal    = OrdersTotal ()                                                                   ; //<3301>
                                                                                                              //<3302>
if   ( avi.OrderSelect   > ali.OrderTotal )                                                                   //<3303>
     { avi.OrderSelect   = 0                                                                                ; //<3304>
       GlobalVariableSet ( avs.SetupOrderSelect , avi.OrderSelect                                       ) ; } //<3305>
                                                                                                              //<3306>
if   ( ali.OrderTotal    > 0        )                                                                         //<3307>
     { int   i , N ; N   = ali.OrderTotal - 1                                                               ; //<3308>
       for ( i = N ; i  >= 0 ; i -- )                                                                         //<3309>
           { OrderSelect ( i , SELECT_BY_POS , MODE_TRADES )                                                ; //<3310>
                                                                                                              //<3311>
             if (      avi.OrderSelect      == 0           )                                                  //<3312>
                { if ( OrderMagicNumber ()  != aei.OrderID )                                     continue ; } //<3313>
             else                                                                                             //<3314>
                { if ( avi.OrderSelect      != i + 1       )                                     continue ; } //<3315>
                                                                                                              //<3316>
             afr.PlotOrderLevels        ()                                                                  ; //<3317>
                                                                                                              //<3318>
             afr.CurrencyDetector     ( OrderSymbol     () , als.OrderCurrency                            ) ; //<3319>
                                                                                                              //<3320>
             ald.ContractSize         = OrderLots       ()                                                  ; //<3321>
             ald.ContractValue        = ald.ContractSize   / avd.QuotePoint  * ald.NominalPoint             ; //<3322>
             ald.OrderPoint           = ald.NominalPoint   * ald.ContractSize                               ; //<3323>
             ali.OrderLifetime        = TimeCurrent     () - OrderOpenTime   ()                             ; //<3324>
                                                                                                              //<3325>
             ald.QuotePrice           = OrderOpenPrice  ()                                                  ; //<3326>
             ald.QuoteTake            = OrderTakeProfit ()                                                  ; //<3327>
             ald.QuoteStop            = OrderStopLoss   ()                                                  ; //<3328>
                                                                                                              //<3329>
             ald.QuoteTarget          = MathAbs            ( ald.QuotePrice  - ald.QuoteTake              ) ; //<3330>
                                                                                                              //<3331>
             if ( OrderType ()       == OP_BUY               )                                                //<3332>
                  ald.QuoteVAR        = ald.QuoteStop      - ald.QuotePrice                                 ; //<3333>
             else ald.QuoteVAR        = ald.QuotePrice     - ald.QuoteStop                                  ; //<3334>
                                                                                                              //<3335>
             ald.OrderProfit          = OrderProfit     ()                                                  ; //<3336>
             ald.OrderTarget          = ald.QuoteTarget    * ald.ContractValue                              ; //<3337>
             ald.OrderVAR             = ald.QuoteVAR       * ald.ContractValue                              ; //<3338>
             ald.OrderLimit           = ald.VARLimit                                                        ; //<3339>
                                                                                                              //<3340>
             ali.OrderProfit          = MathRound          ( ald.OrderProfit / ald.OrderPoint )             ; //<3341>
             ali.OrderTarget          = MathRound          ( ald.QuoteTarget / avd.QuotePoint )             ; //<3342>
             ali.OrderVAR             = MathRound          ( ald.QuoteVAR    / avd.QuotePoint )             ; //<3343>
             ali.OrderLimit           = MathRound          ( ald.OrderLimit  / ald.OrderPoint )             ; //<3344>
                                                                                                              //<3345>
             als.OrderData            = "#"                + OrderTicket ()                      + " "   +    //<3346>
                                        acs.Operation      [ OrderType   ()  ]                   + " "   +    //<3347>
                                        DoubleToStr        ( ald.ContractSize             , 2 )  + " x " +    //<3348>
                                        DoubleToStr        ( ali.LotSize                  , 0 )  + " "   +    //<3349>
                                        als.OrderCurrency  [ ari.Margin      ]                   + " / " +    //<3350>
                                        ali.MarginPoints                                         + " x " +    //<3351>
                                        DoubleToStr        ( ald.OrderPoint               , 2 )  + " / " +    //<3352>
                                        afs.Interval       ( ali.OrderLifetime            , 1 )             ; //<3353>
       } // for                                                                                               //<3354>
   } // if                                                                                                    //<3355>
else   afr.DeleteOrderLevels                                                                             () ; //<3356>
                                                                                                              //<3357>
if   ( ald.OrderProfit > 0 )   string   als.OPSign         = "+"                                            ; //<3358>
else                                    als.OPSign         = ""                                             ; //<3359>
                                                                                                              //<3360>
if   ( ald.OrderTarget > 0 )   string   als.OTSign         = "+"                                            ; //<3361>
else                                    als.OTSign         = ""                                             ; //<3362>
                                                                                                              //<3363>
if   ( ald.OrderVAR    > 0 )   string   als.OVSign         = "+"                                            ; //<3364>
else                                    als.OVSign         = ""                                             ; //<3365>
                                                                                                              //<3366>
if   ( ald.QuotePrice  > 0 )   string   als.OrderPrice     = DoubleToStr ( ald.QuotePrice    , avi.Digits ) ; //<3367>
else                                    als.OrderPrice     = ""                                             ; //<3368>
                                                                                                              //<3369>
if   ( ald.QuoteTake   > 0 )   string   als.OrderTake      = DoubleToStr ( ald.QuoteTake     , avi.Digits ) ; //<3370>
else                                    als.OrderTake      = ""                                             ; //<3371>
                                                                                                              //<3372>
if   ( ald.QuoteStop   > 0 )   string   als.OrderStop      = DoubleToStr ( ald.QuoteStop     , avi.Digits ) ; //<3373>
else                                    als.OrderStop      = ""                                             ; //<3374>
                                                                                                              //<3375>
afr.SetText.1 (  14 ,  1 , 0 , als.OrderData                                                              ) ; //<3376>
afr.SetText.1 (  15 ,  1 , 0 , "Profit:"                                                                  ) ; //<3377>
afr.SetText.1 (  15 , 23 , 1 , als.OPSign   + DoubleToStr  ( ald.OrderProfit                        , 2 ) ) ; //<3378>
afr.SetText.1 (  15 , 32 , 1 , als.OPSign                  + ali.OrderProfit                              ) ; //<3379>
afr.SetText.1 (  15 , 45 , 1 , als.OPSign   + DoubleToStr  ( ald.OrderProfit / AccountEquity () * 100 , 2 )   //<3380>
                                                                                                    + "%" ) ; //<3381>
afr.SetText.1 (  15 , 55 , 1 , als.OrderPrice                                                             ) ; //<3382>
afr.SetText.1 (  16 ,  1 , 0 , "Target:"                                                                  ) ; //<3383>
afr.SetText.1 (  16 , 23 , 1 , als.OTSign   + DoubleToStr  ( ald.OrderTarget                        , 2 ) ) ; //<3384>
afr.SetText.1 (  16 , 32 , 1 , als.OTSign                  + ali.OrderTarget                              ) ; //<3385>
afr.SetText.1 (  16 , 45 , 1 , als.OTSign   + DoubleToStr  ( ald.OrderTarget / AccountEquity () * 100 , 2 )   //<3386>
                                                                                                    + "%" ) ; //<3387>
afr.SetText.1 (  16 , 55 , 1 , als.OrderTake                                                              ) ; //<3388>
afr.SetText.1 (  17 ,  1 , 0 , "VAR:"                                                                     ) ; //<3389>
afr.SetText.1 (  17 , 23 , 1 , als.OVSign   + DoubleToStr  ( ald.OrderVAR                           , 2 ) ) ; //<3390>
afr.SetText.1 (  17 , 32 , 1 , als.OVSign                  + ali.OrderVAR                                 ) ; //<3391>
afr.SetText.1 (  17 , 45 , 1 , als.OVSign   + DoubleToStr  ( ald.OrderVAR    / AccountEquity () * 100 , 2 )   //<3392>
                                                                                                    + "%" ) ; //<3393>
afr.SetText.1 (  17 , 55 , 1 , als.OrderStop                                                              ) ; //<3394>
afr.SetText.1 (  18 ,  1 , 0 , "Limit:"                                                                   ) ; //<3395>
afr.SetText.1 (  18 , 23 , 1 , DoubleToStr  ( - ald.VARLimit                                        , 2 ) ) ; //<3396>
afr.SetText.1 (  18 , 32 , 1 , "-"                         + ali.OrderLimit                               ) ; //<3397>
afr.SetText.1 (  18 , 45 , 1 , "-"          + DoubleToStr  ( avd.OrderReserve * 100 , 2           ) + "%" ) ; //<3398>
//</4216.5. Third Cluster: Position Management Report 93 >                                                    //<3399>
                                                                                                              //<3400>
//< 4216.6. Leverage/Contract Specification Indicator 14 >                                                    //<3401>
string als.Leverage          = "1:"                                                                      +    //<3402>
                               AccountLeverage ()                                                + " / " +    //<3403>
                               DoubleToStr  ( avd.NominalMargin                   , 2          ) + " "   +    //<3404>
                               arn.Currency [ ari.Account  ]                                     + " = " +    //<3405>
                               ali.MarginPoints                                           + " points x " +    //<3406>
                               DoubleToStr  ( ald.NominalPoint                    , 2          ) + " "   +    //<3407>
                               arn.Currency [ ari.Account  ]                                                ; //<3408>
                                                                                                              //<3409>
string als.Contract          = DoubleToStr  ( MarketInfo   ( aes.Symbol , MODE_LOTSIZE ) , 2 )   + " "   +    //<3410>
                               arn.Currency [ ari.Margin   ]                                     + " / " +    //<3411>
                               DoubleToStr  ( MarketInfo   ( aes.Symbol , MODE_MINLOT  ) , 2 )   + " / " +    //<3412>
                               DoubleToStr  ( MarketInfo   ( aes.Symbol , MODE_LOTSTEP ) , 2 )   + " / " +    //<3413>
                               DoubleToStr  ( MarketInfo   ( aes.Symbol , MODE_MAXLOT  ) , 2 )   + " / " +    //<3414>
                               DoubleToStr  ( avd.RealSpread            / avd.QuotePoint , 0 )   + "/"   +    //<3415>
                               DoubleToStr  ( avd.RealSpread            / avd.QuotePoint , 0 )              ; //<3416>
                                                                                                              //<3417>
afr.SetText.1 (  20 ,  1 , 0 , "Leverage:    " + als.Leverage                                             ) ; //<3418>
afr.SetText.1 (  21 ,  1 , 0 , "Contract:    " + als.Contract                                             ) ; //<3419>
//</4216.6. Leverage/Contract Specification Indicator 14 >                                                    //<3420>
}                                                                                                             //<3449>
//</A.System.Extra: Control Module Function 2220 >````````````````````````````````````````````````````````````//<3450>
                                                                                                              //<3451>
//< A.System.Extra: Control Module Function 2221 >````````````````````````````````````````````````````````````//<3452>
int    afr.Report.2                   ()          //    - elements // input    - / code       - / output    - //<3453>
{                                                                                                             //<3454>
//< 4217.1. Preparation >                                                                                     //<3455>
static int ali.Trigger    ; if ( ! ali.Trigger )    { ali.Trigger = 1                                       ; //<3456>
  static string avs.StreamLine.1 ; avs.StreamLine.1 = StringSubstr ( acs.Blank , 1 )                        ; //<3457>
  static string avs.StreamLine.2 ; avs.StreamLine.2 = StringSubstr ( acs.Blank , 1 )                        ; //<3458>
  static string avs.StreamLine.3 ; avs.StreamLine.3 = StringSubstr ( acs.Blank , 1 )                        ; //<3459>
  static string avs.StreamLine.4 ; avs.StreamLine.4 = StringSubstr ( acs.Blank , 1 )                        ; //<3460>
  static string avs.StreamLine.5 ; avs.StreamLine.5 = StringSubstr ( acs.Blank , 1 )                        ; //<3461>
  static string avs.StreamLine.6 ; avs.StreamLine.6 = StringSubstr ( acs.Blank , 1 )                        ; //<3462>
}                                                                                                             //<3463>
//</4217.1. Preparation >                                                                                     //<3464>
                                                                                                              //<3465>
//< 4217.4. Processing Log Report >                                                                           //<3466>
if ( arv.1 [ ari.1.NewFrames ]  > 0 )                                                                         //<3467>
   {                                                                                                          //<3468>
     static  int ali.Reports          ; ali.Reports ++                                                      ; //<3469>
                                                                                                              //<3470>
     int        ali.VolumeProfile     = arv.P.1 [ ari.P.Volume   ]                                            //<3471>
                                      + arv.P.2 [ ari.P.Volume   ]                                            //<3472>
                                      + arv.P.3 [ ari.P.Volume   ]                                          ; //<3473>
                                                                                                              //<3474>
     int        ali.VolumeZones       = arv.Z   [ ari.Z.Volume.1 ]                                          ; //<3475>
                                                                                                              //<3476>
     Print ( avs.SystemStamp          , ": Processing Report " , ali.Reports                        , " " ,   //<3477>
                                        " Profile Volume = "   , ali.VolumeProfile                        ,   //<3478>
                                        " Zones Volume = "     , ali.VolumeZones                          ,   //<3479>
                                        " Miscount = "         , ali.VolumeProfile - ali.VolumeZones      ) ; //<3480>
                                                                                                              //<3481>
                                                                                                              //<3482>
     Print ( avs.SystemStamp          , ": Total Zone Report " , ali.Reports , " " ,                          //<3483>
                                        arv.Z [ ari.Z.IndexShift           ] , "/" ,                          //<3484>
                                        arv.Z [ ari.Z.IndexRange           ] , "/" ,                          //<3485>
                                        arv.Z [ ari.Z.IndexCurrent         ] , " " ,                          //<3486>
                                        arv.Z [ ari.Z.Volume.1             ] , "/" ,                          //<3487>
                                        arv.Z [ ari.Z.Volume.0             ] , " " ,                          //<3488>
                                        arv.Z [ ari.Z.VolumeMean.1         ] , "/" ,                          //<3489>
                                        arv.Z [ ari.Z.VolumeMean.0         ] , " " ,                          //<3490>
                                        arv.Z [ ari.Z.IndexMax.1           ] , "/" ,                          //<3491>
                                        arv.Z [ ari.Z.IndexMax.0           ] , " " ,                          //<3492>
                                        arv.Z [ ari.Z.VolumeMax.1          ] , "/" ,                          //<3493>
                                        arv.Z [ ari.Z.VolumeMax.0          ]                              ) ; //<3494>
                                                                                                              //<3495>
     Print ( avs.SystemStamp          , ": Chart Zone Report " , ali.Reports , " " ,                          //<3496>
                                        arv.Z [ ari.Z.IndexChartHighest    ] , "/" ,                          //<3497>
                                        arv.Z [ ari.Z.IndexChartLowest     ] , "/" ,                          //<3498>
                                        arv.Z [ ari.Z.IndexChartRange      ] , " " ,                          //<3499>
                                        arv.Z [ ari.Z.VolumeChart.1        ] , "/" ,                          //<3500>
                                        arv.Z [ ari.Z.VolumeChart.0        ] , " " ,                          //<3501>
                                        arv.Z [ ari.Z.VolumeChartMean.1    ] , "/" ,                          //<3502>
                                        arv.Z [ ari.Z.VolumeChartMean.0    ] , " " ,                          //<3503>
                                        arv.Z [ ari.Z.IndexChartMax.1      ] , "/" ,                          //<3504>
                                        arv.Z [ ari.Z.IndexChartMax.0      ] , " " ,                          //<3505>
                                        arv.Z [ ari.Z.VolumeChartMax.1     ] , "/" ,                          //<3506>
                                        arv.Z [ ari.Z.VolumeChartMax.0     ]                              ) ; //<3507>
   }                                                                                                          //<3508>
//</4217.4. Processing Log Report >                                                                           //<3509>
                                                                                                              //<3510>
//< 4217.3. Market Profiles >                                                                                 //<3511>
afr.SetText.2 (  2 , 16 , 1 ,               arv.P.1 [ ari.P.Frames         ]                              ) ; //<3512>
afr.SetText.2 (  3 , 16 , 1 ,               arv.P.1 [ ari.P.Volume         ]                              ) ; //<3513>
afr.SetText.2 (  4 , 16 , 1 , afs.Time    ( arv.P.1 [ ari.P.TimeBegin      ]              )               ) ; //<3514>
afr.SetText.2 (  5 , 16 , 1 , afs.Time    ( arv.P.1 [ ari.P.TimeEnd        ]              )               ) ; //<3515>
                                                                                                              //<3516>
afr.SetText.2 (  2 , 34 , 1 ,               arv.P.2 [ ari.P.Frames         ]                              ) ; //<3517>
afr.SetText.2 (  3 , 34 , 1 ,               arv.P.2 [ ari.P.Volume         ]                              ) ; //<3518>
afr.SetText.2 (  4 , 34 , 1 , afs.Time    ( arv.P.2 [ ari.P.TimeBegin      ]              )               ) ; //<3519>
afr.SetText.2 (  5 , 34 , 1 , afs.Time    ( arv.P.2 [ ari.P.TimeEnd        ]              )               ) ; //<3520>
                                                                                                              //<3521>
afr.SetText.2 (  2 , 37 , 0 ,               arv.P.3 [ ari.P.Frames         ]                              ) ; //<3522>
afr.SetText.2 (  3 , 37 , 0 ,               arv.P.3 [ ari.P.Volume         ]                              ) ; //<3523>
afr.SetText.2 (  4 , 52 , 1 , afs.Time    ( arv.P.3 [ ari.P.TimeBegin      ]              )               ) ; //<3524>
afr.SetText.2 (  5 , 52 , 1 , afs.Time    ( arv.P.3 [ ari.P.TimeEnd        ]              )               ) ; //<3525>
//</4217.3. Market Profiles >                                                                                 //<3526>
                                                                                                              //<3527>
//< 4217.4. M1 Series >                                                                                       //<3528>
if ( arv.1 [ ari.1.Series ] > 0 )       int ali.Sign   = '+'                                                ; //<3529>
else                                        ali.Sign   = ' '                                                ; //<3530>
                                                                                                              //<3531>
afr.SetText.2 (  2 , 45 , 1 , CharToStr   ( ali.Sign ) + arv.1 [ ari.1.Series ]                           ) ; //<3532>
//</4217.4. M1 Series >                                                                                       //<3533>
                                                                                                              //<3534>
//< 4217.4. Data Integrity Check >                                                                            //<3535>
if ( arv.1 [ ari.1.NewFrames ]  > 0 )                                                                         //<3536>
   {                                                                                                          //<3537>
     int        ali.IndexBegin  = iBarShift       ( aes.Symbol , PERIOD_M1  , arv.P.3 [ ari.P.TimeBegin ] ) ; //<3538>
     static int ali.CheckFrames ; ali.CheckFrames = ali.IndexBegin                                          ; //<3539>
     static int ali.CheckVolume ; ali.CheckVolume = 0                                                       ; //<3540>
     for  ( int i = 1 ;      i <= ali.CheckFrames ; i ++ )                                                    //<3541>
                ali.CheckVolume = ali.CheckVolume + iVolume    ( aes.Symbol , PERIOD_M1 , i )               ; //<3542>
   }                                                                                                          //<3543>
                                                                                                              //<3544>
afr.SetText.2 (  2 , 52 , 1 , ali.CheckFrames                                                             ) ; //<3545>
afr.SetText.2 (  3 , 52 , 1 , ali.CheckVolume                                                             ) ; //<3546>
//</4217.4. Data Integrity Check >                                                                            //<3547>
                                                                                                              //<3548>
//< 4217.5. Market Profile Range >                                                                            //<3549>
afr.SetText.2 (  2 , 63 , 1 , arv.P.3 [ ari.P.PriceHighest   ]                                            ) ; //<3550>
afr.SetText.2 (  3 , 63 , 1 , arv.P.3 [ ari.P.PriceRange     ]                                            ) ; //<3551>
afr.SetText.2 (  4 , 63 , 1 , arv.P.3 [ ari.P.PriceLowest    ]                                            ) ; //<3552>
                                                                                                              //<3553>
afr.SetText.2 (  5 , 63 , 1 , arv.1   [ ari.1.Length.0       ] + "/" +                                        //<3554>
                              arv.1   [ ari.1.Length         ] + "=" +                                        //<3555>
                              arv.1   [ ari.1.Completeness.0 ] + "%"                                      ) ; //<3556>
//</4217.5. Market Profile Range >                                                                            //<3557>
                                                                                                              //<3558>
//< 4217.6. Timeframe Register >                                                                              //<3559>
if ( arv.1 [ ari.1.Volume.1 ] > 0 )                                                                           //<3560>
 int ali.VolumesRatio.0      = MathRound  ( 100.0  * arv.1 [ ari.1.Volume.0 ] / arv.1  [ ari.1.Volume.1 ] ) ; //<3561>
else ali.VolumesRatio.0      = 0                                                                            ; //<3562>
                                                                                                              //<3563>
int  ali.VolumesRatio.1      = MathRound  ( arv.1  [ ari.1.Volume.0 ] / arv.P.1 [ ari.P.MaxVolume ] * 100 ) ; //<3564>
int  ali.VolumesRatio.2      = MathRound  ( arv.1  [ ari.1.Volume.0 ] / arv.P.2 [ ari.P.MaxVolume ] * 100 ) ; //<3565>
int  ali.VolumesRatio.3      = MathRound  ( arv.1  [ ari.1.Volume.0 ] / arv.P.3 [ ari.P.MaxVolume ] * 100 ) ; //<3566>
                                                                                                              //<3567>
if ( arv.1 [ ari.1.Range.1  ]  > 0 )                                                                          //<3568>
 int ali.RangeRatio.0        = MathRound  ( 100.0 * arv.1 [ ari.1.Range.0 ] / arv.1   [ ari.1.Range.1   ] ) ; //<3569>
else ali.RangeRatio.0        = 0                                                                            ; //<3570>
                                                                                                              //<3571>
int  ali.RangeRatio.1        = MathRound  ( arv.1   [ ari.1.Range.0 ] / arv.P.1 [ ari.P.MaxRange  ] * 100 ) ; //<3572>
int  ali.RangeRatio.2        = MathRound  ( arv.1   [ ari.1.Range.0 ] / arv.P.2 [ ari.P.MaxRange  ] * 100 ) ; //<3573>
int  ali.RangeRatio.3        = MathRound  ( arv.1   [ ari.1.Range.0 ] / arv.P.3 [ ari.P.MaxRange  ] * 100 ) ; //<3574>
                                                                                                              //<3575>
afr.SetText.2 (  7 , 16 , 1 , arv.1   [ ari.1.Range.0   ] + "/" +                                             //<3576>
                              arv.P.1 [ ari.P.MaxRange  ] + "=" +                                             //<3577>
                              ali.RangeRatio.1            + "%"                                           ) ; //<3578>
afr.SetText.2 (  8 , 16 , 1 , arv.1   [ ari.1.Volume.0  ] + "/" +                                             //<3579>
                              arv.P.1 [ ari.P.MaxVolume ] + "=" +                                             //<3580>
                              ali.VolumesRatio.1          + "%"                                           ) ; //<3581>
                                                                                                              //<3582>
afr.SetText.2 (  7 , 34 , 1 , arv.1   [ ari.1.Range.0   ] + "/" +                                             //<3583>
                              arv.P.2 [ ari.P.MaxRange  ] + "=" +                                             //<3584>
                              ali.RangeRatio.2            + "%"                                           ) ; //<3585>
afr.SetText.2 (  8 , 34 , 1 , arv.1   [ ari.1.Volume.0  ] + "/" +                                             //<3586>
                              arv.P.2 [ ari.P.MaxVolume ] + "=" +                                             //<3587>
                              ali.VolumesRatio.2          + "%"                                           ) ; //<3588>
                                                                                                              //<3589>
afr.SetText.2 (  7 , 52 , 1 , arv.1   [ ari.1.Range.0   ] + "/" +                                             //<3590>
                              arv.P.3 [ ari.P.MaxRange  ] + "=" +                                             //<3591>
                              ali.RangeRatio.3            + "%"                                           ) ; //<3592>
afr.SetText.2 (  8 , 52 , 1 , arv.1   [ ari.1.Volume.0  ] + "/" +                                             //<3593>
                              arv.P.3 [ ari.P.MaxVolume ] + "=" +                                             //<3594>
                              ali.VolumesRatio.3          + "%"                                           ) ; //<3595>
                                                                                                              //<3596>
afr.SetText.2 (  7 , 63 , 1 , arv.1   [ ari.1.Range.0   ] + "/" +                                             //<3597>
                              arv.1   [ ari.1.Range.1   ] + "=" +                                             //<3598>
                              ali.RangeRatio.0            + "%"                                           ) ; //<3599>
afr.SetText.2 (  8 , 63 , 1 , arv.1   [ ari.1.Volume.0  ] + "/" +                                             //<3600>
                              arv.1   [ ari.1.Volume.1  ] + "=" +                                             //<3601>
                              ali.VolumesRatio.0          + "%"                                           ) ; //<3602>
//</4217.6. Timeframe Register >                                                                              //<3603>
                                                                                                              //<3604>
//< 4217.7. Stream Lines >                                                                                    //<3605>
string  als.BufferBlank     = "       "                                                                     ; //<3606>
string  als.BufferSign                                                                                      ; //<3607>
string  als.Buffer                                                                                          ; //<3608>
                                                                                                              //<3609>
        als.Buffer          =                arv.S.Volume                [ 1 ]                              ; //<3610>
        als.Buffer          = StringSubstr ( als.BufferBlank      + als.Buffer , StringLen ( als.Buffer ) ) ; //<3611>
        avs.StreamLine.1    = StringSubstr ( avs.StreamLine.1     + als.Buffer ,                        7 ) ; //<3612>
                                                                                                              //<3613>
        als.Buffer          = DoubleToStr  ( arv.S.Latency               [ 1 ] / 1000.0               , 3 ) ; //<3614>
        als.Buffer          = StringSubstr ( als.BufferBlank      + als.Buffer , StringLen ( als.Buffer ) ) ; //<3615>
        avs.StreamLine.2    = StringSubstr ( avs.StreamLine.2     + als.Buffer ,                        7 ) ; //<3616>
                                                                                                              //<3617>
        if ( arv.S.Fluctuation             [ 1 ] > 0 )              als.BufferSign         = "+"            ; //<3618>
        else                                                        als.BufferSign         = ""             ; //<3619>
        als.Buffer          = als.BufferSign                      + arv.S.Fluctuation           [ 1 ]       ; //<3620>
        als.Buffer          = StringSubstr ( als.BufferBlank      + als.Buffer , StringLen ( als.Buffer ) ) ; //<3621>
        avs.StreamLine.3    = StringSubstr ( avs.StreamLine.3     + als.Buffer ,                        7 ) ; //<3622>
        als.Buffer          = als.BufferSign                      + arv.S.Fluctuation           [ 0 ]       ; //<3623>
        avs.StreamLine.3    = als.Buffer   + StringSubstr   ( avs.StreamLine.3 , StringLen ( als.Buffer ) ) ; //<3624>
                                                                                                              //<3625>
        if ( arv.S.TotalFluctuation        [ 1 ] > 0 )              als.BufferSign         = "+"            ; //<3626>
        else                                                        als.BufferSign         = ""             ; //<3627>
        als.Buffer          = als.BufferSign                      + arv.S.TotalFluctuation      [ 1 ]       ; //<3628>
        als.Buffer          = StringSubstr ( als.BufferBlank      + als.Buffer , StringLen ( als.Buffer ) ) ; //<3629>
        avs.StreamLine.4    = StringSubstr ( avs.StreamLine.4     + als.Buffer ,                        7 ) ; //<3630>
        als.Buffer          = als.BufferSign                      + arv.S.TotalFluctuation      [ 0 ]       ; //<3631>
        avs.StreamLine.4    = als.Buffer   + StringSubstr   ( avs.StreamLine.4 , StringLen ( als.Buffer ) ) ; //<3632>
                                                                                                              //<3633>
        als.Buffer          = DoubleToStr  ( arv.S.TotalLatency          [ 1 ] / 1000.0               , 3 ) ; //<3634>
        als.Buffer          = StringSubstr ( als.BufferBlank      + als.Buffer , StringLen ( als.Buffer ) ) ; //<3635>
        avs.StreamLine.5    = StringSubstr ( avs.StreamLine.5     + als.Buffer ,                        7 ) ; //<3636>
                                                                                                              //<3637>
        als.Buffer          =                arv.S.TotalVolume           [ 1 ]                              ; //<3638>
        als.Buffer          = StringSubstr ( als.BufferBlank      + als.Buffer , StringLen ( als.Buffer ) ) ; //<3639>
        avs.StreamLine.6    = StringSubstr ( avs.StreamLine.6     + als.Buffer ,                        7 ) ; //<3640>
                                                                                                              //<3641>
afr.SetText.2 (  9 , 63 , 1 , avs.StreamLine.1                                                            ) ; //<3642>
afr.SetText.2 ( 10 , 63 , 1 , avs.StreamLine.2                                                            ) ; //<3643>
afr.SetText.2 ( 11 , 63 , 1 , avs.StreamLine.3                                                            ) ; //<3644>
afr.SetText.2 ( 12 , 63 , 1 , avs.StreamLine.4                                                            ) ; //<3645>
afr.SetText.2 ( 13 , 63 , 1 , avs.StreamLine.5                                                            ) ; //<3646>
afr.SetText.2 ( 14 , 63 , 1 , avs.StreamLine.6                                                            ) ; //<3647>
//</4217.7. Stream Lines >                                                                                    //<3648>
                                                                                                              //<3649>
//< 4217.8. Volume Monitoring >                                                                               //<3650>
afr.SetText.2 ( 16 , 28 , 1 , "Total:"                                                                    ) ; //<3651>
afr.SetText.2 ( 17 , 28 , 1 ,    "1 x"                                                                    ) ; //<3652>
afr.SetText.2 ( 18 , 28 , 1 ,    "2 x"                                                                    ) ; //<3653>
afr.SetText.2 ( 19 , 28 , 1 ,    "3 x"                                                                    ) ; //<3654>
afr.SetText.2 ( 20 , 28 , 1 ,    "4 x"                                                                    ) ; //<3655>
afr.SetText.2 ( 21 , 28 , 1 ,    "5 x"                                                                    ) ; //<3656>
afr.SetText.2 ( 22 , 28 , 1 ,    "6 x"                                                                    ) ; //<3657>
afr.SetText.2 ( 23 , 28 , 1 ,    "7 x"                                                                    ) ; //<3658>
afr.SetText.2 ( 24 , 28 , 1 ,    "8 x"                                                                    ) ; //<3659>
afr.SetText.2 ( 25 , 28 , 1 ,    "9 x"                                                                    ) ; //<3660>
afr.SetText.2 ( 26 , 28 , 1 ,   "10 x"                                                                    ) ; //<3661>
afr.SetText.2 ( 27 , 28 , 1 ,  "Lost:"                                                                    ) ; //<3662>
                                                                                                              //<3663>
afr.SetText.2 ( 16 , 37 , 1 ,           arv.V.Volume [ ari.V.Total ]                                      ) ; //<3664>
afr.SetText.2 ( 16 , 63 , 1 ,     "+" + arv.F.Volume [ ari.F.Up    ]                                          //<3665>
                               + "/-" + arv.F.Volume [ ari.F.Down  ]                                          //<3666>
                               + "/"  + arv.F.Volume [ ari.F.Zero  ]                                          //<3667>
                               + "/"  + arv.V.Volume [ ari.V.Lost  ]                                      ) ; //<3668>
                                                                                                              //<3669>
for ( i = 1   ; i < ari.V.RegisterDimension ; i ++ )                                                          //<3670>
      if ( arv.V.Volume  [ i ] == 0 )                                                                         //<3671>
           afr.SetText.2 ( 16 + i , 63 , 1 , ""                                                           ) ; //<3672>
      else                                                                                                    //<3673>
         { afr.SetText.2 ( 16 + i , 37 , 1 ,                arv.V.Volume  [ i ]                           ) ; //<3674>
           afr.SetText.2 ( 16 + i , 63 , 1 , DoubleToStr  ( arv.V.Latency [ i ] / 1000.0      , 3 ) + " " +   //<3675>
                                             afs.Interval ( TimeCurrent () - arv.V.Time [ i ] , 1 ) + " " +   //<3676>
                                             afs.Time     (                  arv.V.Time [ i ] , 2 )       ) ; //<3677>
         }                                                                                                    //<3678>
//</4217.8. Volume Monitoring >                                                                               //<3679>
                                                                                                              //<3680>
//< 4217.9. Zone Monitoring >                                                                                 //<3681>
//< Computing Optimization >                                                                                  //<3682>
static int   ali.FlagResetZones                                                                             ; //<3683>
static int   ali.IndexCurrent                                                                               ; //<3684>
                                                                                                              //<3685>
if ( arv.1 [ ari.1.NewFrames    ]  > 0                                                                        //<3686>
  || arv.Z [ ari.Z.IndexCurrent ] !=  ali.IndexCurrent )                                                      //<3687>
   {                                                                                                          //<3688>
     ali.IndexCurrent     = arv.Z [ ari.Z.IndexCurrent ]                                                    ; //<3689>
     ali.FlagResetZones   = 1                                                                               ; //<3690>
   }                                                                                                          //<3691>
else ali.FlagResetZones   = 0                                                                               ; //<3692>
//< Computing Optimization >                                                                                  //<3693>
                                                                                                              //<3694>
//< Zone Levels Indication >                                                                                  //<3695>
if ( ali.FlagResetZones  == 1 )                                                                               //<3696>
   {                                                                                                          //<3697>
     static int ali.Level.Down.1 ; ali.Level.Down.1 =   arv.Z [ ari.Z.BasePoints   ]                          //<3698>
                                                    +   arv.Z [ ari.Z.StepPoints   ]                          //<3699>
                                                    * ( arv.Z [ ari.Z.IndexCurrent ]                          //<3700>
                                                    +   arv.Z [ ari.Z.IndexShift   ]                      ) ; //<3701>
                                                                                                              //<3702>
     static int ali.Level.Down.2 ; ali.Level.Down.2 =   ali.Level.Down.1 - arv.Z [ ari.Z.StepPoints ]       ; //<3703>
     static int ali.Level.Down.3 ; ali.Level.Down.3 =   ali.Level.Down.2 - arv.Z [ ari.Z.StepPoints ]       ; //<3704>
     static int ali.Level.Down.4 ; ali.Level.Down.4 =   ali.Level.Down.3 - arv.Z [ ari.Z.StepPoints ]       ; //<3705>
     static int ali.Level.Up.1   ; ali.Level.Up.1   =   ali.Level.Down.1 + arv.Z [ ari.Z.StepPoints ]       ; //<3706>
     static int ali.Level.Up.2   ; ali.Level.Up.2   =   ali.Level.Up.1   + arv.Z [ ari.Z.StepPoints ]       ; //<3707>
     static int ali.Level.Up.3   ; ali.Level.Up.3   =   ali.Level.Up.2   + arv.Z [ ari.Z.StepPoints ]       ; //<3708>
     static int ali.Level.Up.4   ; ali.Level.Up.4   =   ali.Level.Up.3   + arv.Z [ ari.Z.StepPoints ]       ; //<3709>
   }                                                                                                          //<3710>
                                                                                                              //<3711>
afr.SetText.2 ( 31 ,  1 , 0 , "+4: " + DoubleToStr ( ali.Level.Up.4   * avd.QuotePoint     , avi.Digits ) ) ; //<3712>
afr.SetText.2 ( 32 ,  1 , 0 , "+3: " + DoubleToStr ( ali.Level.Up.3   * avd.QuotePoint     , avi.Digits ) ) ; //<3713>
afr.SetText.2 ( 33 ,  1 , 0 , "+2: " + DoubleToStr ( ali.Level.Up.2   * avd.QuotePoint     , avi.Digits ) ) ; //<3714>
afr.SetText.2 ( 34 ,  1 , 0 , "+1: " + DoubleToStr ( ali.Level.Up.1   * avd.QuotePoint     , avi.Digits ) ) ; //<3715>
afr.SetText.2 ( 35 ,  1 , 0 , " 0: " + DoubleToStr ( avd.QuoteBid                          , avi.Digits ) ) ; //<3716>
afr.SetText.2 ( 36 ,  1 , 0 , "-1: " + DoubleToStr ( ali.Level.Down.1 * avd.QuotePoint     , avi.Digits ) ) ; //<3717>
afr.SetText.2 ( 37 ,  1 , 0 , "-2: " + DoubleToStr ( ali.Level.Down.2 * avd.QuotePoint     , avi.Digits ) ) ; //<3718>
afr.SetText.2 ( 38 ,  1 , 0 , "-3: " + DoubleToStr ( ali.Level.Down.3 * avd.QuotePoint     , avi.Digits ) ) ; //<3719>
afr.SetText.2 ( 39 ,  1 , 0 , "-4: " + DoubleToStr ( ali.Level.Down.4 * avd.QuotePoint     , avi.Digits ) ) ; //<3720>
//</Zone Levels Indication >                                                                                  //<3721>
                                                                                                              //<3722>
//</Zone.1 >                                                                                                  //<3723>
if ( ali.FlagResetZones == 1 )                                                                                //<3724>
{                                                                                                             //<3725>
     static int ali.Volume.1.Up.4                                                                           ; //<3726>
     static int ali.Volume.1.Up.3                                                                           ; //<3727>
     static int ali.Volume.1.Up.2                                                                           ; //<3728>
     static int ali.Volume.1.Up.1                                                                           ; //<3729>
     static int ali.Volume.1.Current                                                                        ; //<3730>
     static int ali.Volume.1.Down.1                                                                         ; //<3731>
     static int ali.Volume.1.Down.2                                                                         ; //<3732>
     static int ali.Volume.1.Down.3                                                                         ; //<3733>
     static int ali.Volume.1.Down.4                                                                         ; //<3734>
                                                                                                              //<3735>
     ali.Volume.1.Up.4       = MathRound ( avd.Zone.1 [ arv.Z [ ari.Z.IndexCurrent ] + 4 ]                ) ; //<3736>
     ali.Volume.1.Up.3       = MathRound ( avd.Zone.1 [ arv.Z [ ari.Z.IndexCurrent ] + 3 ]                ) ; //<3737>
     ali.Volume.1.Up.2       = MathRound ( avd.Zone.1 [ arv.Z [ ari.Z.IndexCurrent ] + 2 ]                ) ; //<3738>
     ali.Volume.1.Up.1       = MathRound ( avd.Zone.1 [ arv.Z [ ari.Z.IndexCurrent ] + 1 ]                ) ; //<3739>
     ali.Volume.1.Current    = MathRound ( avd.Zone.1 [ arv.Z [ ari.Z.IndexCurrent ]     ]                ) ; //<3740>
     ali.Volume.1.Down.1     = MathRound ( avd.Zone.1 [ arv.Z [ ari.Z.IndexCurrent ] - 1 ]                ) ; //<3741>
     ali.Volume.1.Down.2     = MathRound ( avd.Zone.1 [ arv.Z [ ari.Z.IndexCurrent ] - 2 ]                ) ; //<3742>
     ali.Volume.1.Down.3     = MathRound ( avd.Zone.1 [ arv.Z [ ari.Z.IndexCurrent ] - 3 ]                ) ; //<3743>
     ali.Volume.1.Down.4     = MathRound ( avd.Zone.1 [ arv.Z [ ari.Z.IndexCurrent ] - 4 ]                ) ; //<3744>
                                                                                                              //<3745>
     static int ali.RelToMax.1.Up.4                                                                         ; //<3746>
     static int ali.RelToMax.1.Up.3                                                                         ; //<3747>
     static int ali.RelToMax.1.Up.2                                                                         ; //<3748>
     static int ali.RelToMax.1.Up.1                                                                         ; //<3749>
     static int ali.RelToMax.1.Current                                                                      ; //<3750>
     static int ali.RelToMax.1.Down.1                                                                       ; //<3751>
     static int ali.RelToMax.1.Down.2                                                                       ; //<3752>
     static int ali.RelToMax.1.Down.3                                                                       ; //<3753>
     static int ali.RelToMax.1.Down.4                                                                       ; //<3754>
                                                                                                              //<3755>
     double     ald.Max      =                          arv.Z [ ari.Z.VolumeMax.1  ]       / 100.0          ; //<3756>
                                                                                                              //<3757>
     ali.RelToMax.1.Up.4     = MathRound ( avd.Zone.1 [ arv.Z [ ari.Z.IndexCurrent ] + 4 ] / ald.Max      ) ; //<3758>
     ali.RelToMax.1.Up.3     = MathRound ( avd.Zone.1 [ arv.Z [ ari.Z.IndexCurrent ] + 3 ] / ald.Max      ) ; //<3759>
     ali.RelToMax.1.Up.2     = MathRound ( avd.Zone.1 [ arv.Z [ ari.Z.IndexCurrent ] + 2 ] / ald.Max      ) ; //<3760>
     ali.RelToMax.1.Up.1     = MathRound ( avd.Zone.1 [ arv.Z [ ari.Z.IndexCurrent ] + 1 ] / ald.Max      ) ; //<3761>
     ali.RelToMax.1.Current  = MathRound ( avd.Zone.1 [ arv.Z [ ari.Z.IndexCurrent ]     ] / ald.Max      ) ; //<3762>
     ali.RelToMax.1.Down.1   = MathRound ( avd.Zone.1 [ arv.Z [ ari.Z.IndexCurrent ] - 1 ] / ald.Max      ) ; //<3763>
     ali.RelToMax.1.Down.2   = MathRound ( avd.Zone.1 [ arv.Z [ ari.Z.IndexCurrent ] - 2 ] / ald.Max      ) ; //<3764>
     ali.RelToMax.1.Down.3   = MathRound ( avd.Zone.1 [ arv.Z [ ari.Z.IndexCurrent ] - 3 ] / ald.Max      ) ; //<3765>
     ali.RelToMax.1.Down.4   = MathRound ( avd.Zone.1 [ arv.Z [ ari.Z.IndexCurrent ] - 4 ] / ald.Max      ) ; //<3766>
                                                                                                              //<3767>
     static double ald.RelToAver.1.Up.4                                                                     ; //<3768>
     static double ald.RelToAver.1.Up.3                                                                     ; //<3769>
     static double ald.RelToAver.1.Up.2                                                                     ; //<3770>
     static double ald.RelToAver.1.Up.1                                                                     ; //<3771>
     static double ald.RelToAver.1.Current                                                                  ; //<3772>
     static double ald.RelToAver.1.Down.1                                                                   ; //<3773>
     static double ald.RelToAver.1.Down.2                                                                   ; //<3774>
     static double ald.RelToAver.1.Down.3                                                                   ; //<3775>
     static double ald.RelToAver.1.Down.4                                                                   ; //<3776>
                                                                                                              //<3777>
     double     ald.Mean     =                          arv.Z [ ari.Z.VolumeMean.1 ]                        ; //<3778>
                                                                                                              //<3779>
     ald.RelToAver.1.Up.4    =             avd.Zone.1 [ arv.Z [ ari.Z.IndexCurrent ] + 4 ] / ald.Mean       ; //<3780>
     ald.RelToAver.1.Up.3    =             avd.Zone.1 [ arv.Z [ ari.Z.IndexCurrent ] + 3 ] / ald.Mean       ; //<3781>
     ald.RelToAver.1.Up.2    =             avd.Zone.1 [ arv.Z [ ari.Z.IndexCurrent ] + 2 ] / ald.Mean       ; //<3782>
     ald.RelToAver.1.Up.1    =             avd.Zone.1 [ arv.Z [ ari.Z.IndexCurrent ] + 1 ] / ald.Mean       ; //<3783>
     ald.RelToAver.1.Current =             avd.Zone.1 [ arv.Z [ ari.Z.IndexCurrent ]     ] / ald.Mean       ; //<3784>
     ald.RelToAver.1.Down.1  =             avd.Zone.1 [ arv.Z [ ari.Z.IndexCurrent ] - 1 ] / ald.Mean       ; //<3785>
     ald.RelToAver.1.Down.2  =             avd.Zone.1 [ arv.Z [ ari.Z.IndexCurrent ] - 2 ] / ald.Mean       ; //<3786>
     ald.RelToAver.1.Down.3  =             avd.Zone.1 [ arv.Z [ ari.Z.IndexCurrent ] - 3 ] / ald.Mean       ; //<3787>
     ald.RelToAver.1.Down.4  =             avd.Zone.1 [ arv.Z [ ari.Z.IndexCurrent ] - 4 ] / ald.Mean       ; //<3788>
                                                                                                              //<3789>
     static int ali.Volume.1.Up.04                                                                          ; //<3790>
     static int ali.Volume.1.Up.03                                                                          ; //<3791>
     static int ali.Volume.1.Up.02                                                                          ; //<3792>
     static int ali.Volume.1.Difference                                                                     ; //<3793>
     static int ali.Volume.1.Down.02                                                                        ; //<3794>
     static int ali.Volume.1.Down.03                                                                        ; //<3795>
     static int ali.Volume.1.Down.04                                                                        ; //<3796>
                                                                                                              //<3797>
     ali.Volume.1.Up.02      = ali.Volume.1.Up.1      + ali.Volume.1.Up.2                                   ; //<3798>
     ali.Volume.1.Up.03      = ali.Volume.1.Up.02     + ali.Volume.1.Up.3                                   ; //<3799>
     ali.Volume.1.Up.04      = ali.Volume.1.Up.03     + ali.Volume.1.Up.4                                   ; //<3800>
                                                                                                              //<3801>
     ali.Volume.1.Down.02    = ali.Volume.1.Down.1    + ali.Volume.1.Down.2                                 ; //<3802>
     ali.Volume.1.Down.03    = ali.Volume.1.Down.02   + ali.Volume.1.Down.3                                 ; //<3803>
     ali.Volume.1.Down.04    = ali.Volume.1.Down.03   + ali.Volume.1.Down.4                                 ; //<3804>
                                                                                                              //<3805>
     ali.Volume.1.Difference = ali.Volume.1.Up.04     - ali.Volume.1.Down.04                                ; //<3806>
                                                                                                              //<3807>
     static string                                      als.Sign.1.Difference                               ; //<3808>
     if   ( ali.Volume.1.Difference > 0 )               als.Sign.1.Difference = "+"                         ; //<3809>
     else                                               als.Sign.1.Difference = ""                          ; //<3810>
} // if                                                                                                       //<3811>
                                                                                                              //<3812>
afr.SetText.2 ( 29 , 18 , 1 , arv.Z [ ari.Z.VolumeMax.1  ]                                                ) ; //<3813>
afr.SetText.2 ( 29 , 34 , 1 , arv.Z [ ari.Z.VolumeMean.1 ]                                                ) ; //<3814>
afr.SetText.2 ( 29 , 43 , 1 , arv.Z [ ari.Z.Volume.1     ]                                                ) ; //<3815>
                                                                                                              //<3816>
afr.SetText.2 ( 31 , 18 , 1 , ali.RelToMax.1.Up.4                                                         ) ; //<3817>
afr.SetText.2 ( 32 , 18 , 1 , ali.RelToMax.1.Up.3                                                         ) ; //<3818>
afr.SetText.2 ( 33 , 18 , 1 , ali.RelToMax.1.Up.2                                                         ) ; //<3819>
afr.SetText.2 ( 34 , 18 , 1 , ali.RelToMax.1.Up.1                                                         ) ; //<3820>
afr.SetText.2 ( 35 , 18 , 1 , ali.RelToMax.1.Current                                                      ) ; //<3821>
afr.SetText.2 ( 36 , 18 , 1 , ali.RelToMax.1.Down.1                                                       ) ; //<3822>
afr.SetText.2 ( 37 , 18 , 1 , ali.RelToMax.1.Down.2                                                       ) ; //<3823>
afr.SetText.2 ( 38 , 18 , 1 , ali.RelToMax.1.Down.3                                                       ) ; //<3824>
afr.SetText.2 ( 39 , 18 , 1 , ali.RelToMax.1.Down.4                                                       ) ; //<3825>
                                                                                                              //<3826>
afr.SetText.2 ( 31 , 34 , 1 , ali.Volume.1.Up.4                                                           ) ; //<3827>
afr.SetText.2 ( 32 , 34 , 1 , ali.Volume.1.Up.3                                                           ) ; //<3828>
afr.SetText.2 ( 33 , 34 , 1 , ali.Volume.1.Up.2                                                           ) ; //<3829>
afr.SetText.2 ( 34 , 34 , 1 , ali.Volume.1.Up.1                                                           ) ; //<3830>
afr.SetText.2 ( 35 , 34 , 1 , ali.Volume.1.Current                                                        ) ; //<3831>
afr.SetText.2 ( 36 , 34 , 1 , ali.Volume.1.Down.1                                                         ) ; //<3832>
afr.SetText.2 ( 37 , 34 , 1 , ali.Volume.1.Down.2                                                         ) ; //<3833>
afr.SetText.2 ( 38 , 34 , 1 , ali.Volume.1.Down.3                                                         ) ; //<3834>
afr.SetText.2 ( 39 , 34 , 1 , ali.Volume.1.Down.4                                                         ) ; //<3835>
                                                                                                              //<3836>
afr.SetText.2 ( 31 , 43 , 1 , ali.Volume.1.Up.04                                                          ) ; //<3837>
afr.SetText.2 ( 32 , 43 , 1 , ali.Volume.1.Up.03                                                          ) ; //<3838>
afr.SetText.2 ( 33 , 43 , 1 , ali.Volume.1.Up.02                                                          ) ; //<3839>
afr.SetText.2 ( 34 , 43 , 1 , ali.Volume.1.Up.1                                                           ) ; //<3840>
afr.SetText.2 ( 35 , 43 , 1 , als.Sign.1.Difference + ali.Volume.1.Difference                             ) ; //<3841>
afr.SetText.2 ( 36 , 43 , 1 , ali.Volume.1.Down.1                                                         ) ; //<3842>
afr.SetText.2 ( 37 , 43 , 1 , ali.Volume.1.Down.02                                                        ) ; //<3843>
afr.SetText.2 ( 38 , 43 , 1 , ali.Volume.1.Down.03                                                        ) ; //<3844>
afr.SetText.2 ( 39 , 43 , 1 , ali.Volume.1.Down.04                                                        ) ; //<3845>
//</Zone.1 >                                                                                                  //<3846>
                                                                                                              //<3847>
//</Zone.0 >                                                                                                  //<3848>
if ( ali.FlagResetZones == 1 )                                                                                //<3849>
{                                                                                                             //<3850>
     static int ali.Volume.0.Up.4                                                                           ; //<3851>
     static int ali.Volume.0.Up.3                                                                           ; //<3852>
     static int ali.Volume.0.Up.2                                                                           ; //<3853>
     static int ali.Volume.0.Up.1                                                                           ; //<3854>
     static int ali.Volume.0.Current                                                                        ; //<3855>
     static int ali.Volume.0.Down.1                                                                         ; //<3856>
     static int ali.Volume.0.Down.2                                                                         ; //<3857>
     static int ali.Volume.0.Down.3                                                                         ; //<3858>
     static int ali.Volume.0.Down.4                                                                         ; //<3859>
                                                                                                              //<3860>
     ali.Volume.0.Up.4       = MathRound ( avd.Zone.0 [ arv.Z [ ari.Z.IndexCurrent ] + 4 ]                ) ; //<3861>
     ali.Volume.0.Up.3       = MathRound ( avd.Zone.0 [ arv.Z [ ari.Z.IndexCurrent ] + 3 ]                ) ; //<3862>
     ali.Volume.0.Up.2       = MathRound ( avd.Zone.0 [ arv.Z [ ari.Z.IndexCurrent ] + 2 ]                ) ; //<3863>
     ali.Volume.0.Up.1       = MathRound ( avd.Zone.0 [ arv.Z [ ari.Z.IndexCurrent ] + 1 ]                ) ; //<3864>
     ali.Volume.0.Current    = MathRound ( avd.Zone.0 [ arv.Z [ ari.Z.IndexCurrent ]     ]                ) ; //<3865>
     ali.Volume.0.Down.1     = MathRound ( avd.Zone.0 [ arv.Z [ ari.Z.IndexCurrent ] - 1 ]                ) ; //<3866>
     ali.Volume.0.Down.2     = MathRound ( avd.Zone.0 [ arv.Z [ ari.Z.IndexCurrent ] - 2 ]                ) ; //<3867>
     ali.Volume.0.Down.3     = MathRound ( avd.Zone.0 [ arv.Z [ ari.Z.IndexCurrent ] - 3 ]                ) ; //<3868>
     ali.Volume.0.Down.4     = MathRound ( avd.Zone.0 [ arv.Z [ ari.Z.IndexCurrent ] - 4 ]                ) ; //<3869>
                                                                                                              //<3870>
     static int ali.Volume.0.Up.04                                                                          ; //<3871>
     static int ali.Volume.0.Up.03                                                                          ; //<3872>
     static int ali.Volume.0.Up.02                                                                          ; //<3873>
     static int ali.Volume.0.Difference                                                                     ; //<3874>
     static int ali.Volume.0.Down.02                                                                        ; //<3875>
     static int ali.Volume.0.Down.03                                                                        ; //<3876>
     static int ali.Volume.0.Down.04                                                                        ; //<3877>
                                                                                                              //<3878>
     ali.Volume.0.Up.02      = ali.Volume.0.Up.1      + ali.Volume.0.Up.2                                   ; //<3879>
     ali.Volume.0.Up.03      = ali.Volume.0.Up.02     + ali.Volume.0.Up.3                                   ; //<3880>
     ali.Volume.0.Up.04      = ali.Volume.0.Up.03     + ali.Volume.0.Up.4                                   ; //<3881>
                                                                                                              //<3882>
     ali.Volume.0.Down.02    = ali.Volume.0.Down.1    + ali.Volume.0.Down.2                                 ; //<3883>
     ali.Volume.0.Down.03    = ali.Volume.0.Down.02   + ali.Volume.0.Down.3                                 ; //<3884>
     ali.Volume.0.Down.04    = ali.Volume.0.Down.03   + ali.Volume.0.Down.4                                 ; //<3885>
                                                                                                              //<3886>
     ali.Volume.0.Difference = ali.Volume.0.Up.04     - ali.Volume.0.Down.04                                ; //<3887>
                                                                                                              //<3888>
     static string                                      als.Sign.0.Difference                               ; //<3889>
     if   ( ali.Volume.0.Difference > 0 )               als.Sign.0.Difference = "+"                         ; //<3890>
     else                                               als.Sign.0.Difference = ""                          ; //<3891>
} // if                                                                                                       //<3892>
                                                                                                              //<3893>
afr.SetText.2 ( 29 , 54 , 1 , arv.Z [ ari.Z.VolumeChartMean.0 ]                                           ) ; //<3894>
afr.SetText.2 ( 29 , 63 , 1 , arv.Z [ ari.Z.Volume.0          ]                                           ) ; //<3895>
                                                                                                              //<3896>
afr.SetText.2 ( 31 , 54 , 1 , ali.Volume.0.Up.4                                                           ) ; //<3897>
afr.SetText.2 ( 32 , 54 , 1 , ali.Volume.0.Up.3                                                           ) ; //<3898>
afr.SetText.2 ( 33 , 54 , 1 , ali.Volume.0.Up.2                                                           ) ; //<3899>
afr.SetText.2 ( 34 , 54 , 1 , ali.Volume.0.Up.1                                                           ) ; //<3900>
afr.SetText.2 ( 35 , 54 , 1 , ali.Volume.0.Current                                                        ) ; //<3901>
afr.SetText.2 ( 36 , 54 , 1 , ali.Volume.0.Down.1                                                         ) ; //<3902>
afr.SetText.2 ( 37 , 54 , 1 , ali.Volume.0.Down.2                                                         ) ; //<3903>
afr.SetText.2 ( 38 , 54 , 1 , ali.Volume.0.Down.3                                                         ) ; //<3904>
afr.SetText.2 ( 39 , 54 , 1 , ali.Volume.0.Down.4                                                         ) ; //<3905>
                                                                                                              //<3906>
afr.SetText.2 ( 31 , 63 , 1 , ali.Volume.0.Up.04                                                          ) ; //<3907>
afr.SetText.2 ( 32 , 63 , 1 , ali.Volume.0.Up.03                                                          ) ; //<3908>
afr.SetText.2 ( 33 , 63 , 1 , ali.Volume.0.Up.02                                                          ) ; //<3909>
afr.SetText.2 ( 34 , 63 , 1 , ali.Volume.0.Up.1                                                           ) ; //<3910>
afr.SetText.2 ( 35 , 63 , 1 , als.Sign.0.Difference + ali.Volume.0.Difference                             ) ; //<3911>
afr.SetText.2 ( 36 , 63 , 1 , ali.Volume.0.Down.1                                                         ) ; //<3912>
afr.SetText.2 ( 37 , 63 , 1 , ali.Volume.0.Down.02                                                        ) ; //<3913>
afr.SetText.2 ( 38 , 63 , 1 , ali.Volume.0.Down.03                                                        ) ; //<3914>
afr.SetText.2 ( 39 , 63 , 1 , ali.Volume.0.Down.04                                                        ) ; //<3915>
//</Zone.0 >                                                                                                  //<3916>
//</4217.9. Zone Monitoring >                                                                                 //<3917>
                                                                                                              //<3918>
//< 4217.2. Header 1 >                                                                                        //<3919>
avi.MonitoringRunTime       = GetTickCount () - avi.MonitoringRunTime                                       ; //<3920>
                                                                                                              //<3921>
afr.SetText.2 (  0 ,  1 , 0 , "Run Time "     + avi.ProcessingRunTime + "/" + avi.MonitoringRunTime + " ms: " //<3922>
                                                                            + avs.ProcessingMessage       ) ; //<3923>
//</4217.2. Header 1 >                                                                                        //<3924>
}                                                                                                             //<3925>
//</A.System.Extra: Control Module Function 2221 >````````````````````````````````````````````````````````````//<3926>
                                                                                                              //<3927>
//< A.System.Indicator: Function  5 >`````````````````````````````````````````````````````````````````````````//<3928>
int    afr.CreateText                 (           //    - elements // input    - / code       - / output    - //<3929>
       string  aas.Name                ,                                                                      //<3930>
       int     aai.Color               ,                                                                      //<3931>
       int     aai.Size                ,                                                                      //<3932>
       int     aai.Time                ,                                                                      //<3933>
       double  aad.Price               ,                                                                      //<3934>
       string  aas.Text                )                                                                      //<3935>
{                                                                                                             //<3936>
ObjectCreate ( aas.Name  , OBJ_TEXT   , 0 , aai.Time  , aad.Price                                         ) ; //<3937>
ObjectSetText( aas.Name  , aas.Text       , aai.Size  , "Courier New" , aai.Color                         ) ; //<3938>
ObjectSet    ( aas.Name  , OBJPROP_BACK   , avi.FlagBackground                                            ) ; //<3939>
}                                                                                                             //<3940>
//</A.System.Indicator: Function  5 >`````````````````````````````````````````````````````````````````````````//<3941>
                                                                                                              //<3942>
//< A.System.Indicator: Function  6 >`````````````````````````````````````````````````````````````````````````//<3943>
int    afr.ResetText                  (           //    - elements // input    - / code       - / output    - //<3944>
       string  aas.Name                ,                                                                      //<3945>
       int     aai.Color               ,                                                                      //<3946>
       int     aai.Size                ,                                                                      //<3947>
       int     aai.Time                ,                                                                      //<3948>
       double  aad.Price               ,                                                                      //<3949>
       string  aas.Text                )                                                                      //<3950>
{                                                                                                             //<3951>
ObjectSet    ( aas.Name  , OBJPROP_TIME1  , aai.Time                                                      ) ; //<3952>
ObjectSet    ( aas.Name  , OBJPROP_PRICE1 , aad.Price                                                     ) ; //<3953>
ObjectSetText( aas.Name  , aas.Text       , aai.Size  , "Courier New" , aai.Color                         ) ; //<3954>
}                                                                                                             //<3955>
//</A.System.Indicator: Function  6 >`````````````````````````````````````````````````````````````````````````//<3956>
                                                                                                              //<3957>
//< A.System.Indicator: Function 11 >`````````````````````````````````````````````````````````````````````````//<3958>
int    afr.CreateLineH                (           //    - elements // input    - / code       - / output    - //<3959>
       string  aas.Name                ,                                                                      //<3960>
       int     aai.Color               ,                                                                      //<3961>
       int     aai.Width               ,                                                                      //<3962>
       int     aai.Style               ,                                                                      //<3963>
       double  aad.Price               )                                                                      //<3964>
{                                                                                                             //<3965>
ObjectCreate ( aas.Name  , OBJ_HLINE  , 0 , 0 , aad.Price                                                 ) ; //<3966>
ObjectSet    ( aas.Name  , OBJPROP_COLOR  , aai.Color                                                     ) ; //<3967>
ObjectSet    ( aas.Name  , OBJPROP_WIDTH  , aai.Width                                                     ) ; //<3968>
ObjectSet    ( aas.Name  , OBJPROP_STYLE  , aai.Style                                                     ) ; //<3969>
ObjectSet    ( aas.Name  , OBJPROP_BACK   , avi.FlagBackground                                            ) ; //<3970>
}                                                                                                             //<3971>
//</A.System.Indicator: Function 11 >`````````````````````````````````````````````````````````````````````````//<3972>
                                                                                                              //<3973>
//< A.System.Indicator: Function 12 >`````````````````````````````````````````````````````````````````````````//<3974>
int    afr.ResetLineH                 (           //    - elements // input    - / code       - / output    - //<3975>
       string  aas.Name                ,                                                                      //<3976>
       double  aad.Price               )                                                                      //<3977>
{                                                                                                             //<3978>
ObjectSet    ( aas.Name  , OBJPROP_PRICE1 , aad.Price                                                     ) ; //<3979>
}                                                                                                             //<3980>
//</A.System.Indicator: Function 12 >`````````````````````````````````````````````````````````````````````````//<3981>
                                                                                                              //<3982>
//< A.System.Indicator: Function  9 >`````````````````````````````````````````````````````````````````````````//<3983>
int    afr.CreateLineV                (           //    - elements // input    - / code       - / output    - //<3984>
       string  aas.Name                ,                                                                      //<3985>
       int     aai.Color               ,                                                                      //<3986>
       int     aai.Width               ,                                                                      //<3987>
       int     aai.Style               ,                                                                      //<3988>
       int     aai.Time                )                                                                      //<3989>
{                                                                                                             //<3990>
ObjectCreate ( aas.Name  , OBJ_VLINE  , 0 , aai.Time  , 0                                                 ) ; //<3991>
ObjectSet    ( aas.Name  , OBJPROP_COLOR  , aai.Color                                                     ) ; //<3992>
ObjectSet    ( aas.Name  , OBJPROP_WIDTH  , aai.Width                                                     ) ; //<3993>
ObjectSet    ( aas.Name  , OBJPROP_STYLE  , aai.Style                                                     ) ; //<3994>
ObjectSet    ( aas.Name  , OBJPROP_BACK   , avi.FlagBackground                                            ) ; //<3995>
}                                                                                                             //<3996>
//</A.System.Indicator: Function  9 >`````````````````````````````````````````````````````````````````````````//<3997>
                                                                                                              //<3998>
//< A.System.Indicator: Function 10 >`````````````````````````````````````````````````````````````````````````//<3999>
 int    afr.ResetLineV                    (                                                            //   1 //<4000>
       string  aas.Name                   ,                                                                   //<4001>
       int     aai.Time                   )                                                                   //<4002>
{                                                                                                             //<4003>
ObjectSet    ( aas.Name  , OBJPROP_TIME1  , aai.Time                                                      ) ; //<4004>
}                                                                                                             //<4005>
//</A.System.Indicator: Function 10 >`````````````````````````````````````````````````````````````````````````//<4006>
                                                                                                              //<4007>
//< A.System.Indicator: Function 14 >`````````````````````````````````````````````````````````````````````````//<4008>
int    afr.CreateFrame                    (                                                           //    3 //<4009>
       string  aas.Name                   ,                                                                   //<4010>
       int     aai.Color                  ,                                                                   //<4011>
       int     aai.BackGround             ,                                                                   //<4012>
       int     aai.Time1                  ,                                                                   //<4013>
       double  aad.Price1                 ,                                                                   //<4014>
       int     aai.Time2                  ,                                                                   //<4015>
       double  aad.Price2                 )                                                                   //<4016>
{                                                                                                             //<4017>
ObjectCreate ( aas.Name  , OBJ_RECTANGLE  , 0 , aai.Time1 , aad.Price1 , aai.Time2 , aad.Price2           ) ; //<4018>
ObjectSet    ( aas.Name  , OBJPROP_COLOR  , aai.Color                                                     ) ; //<4019>
ObjectSet    ( aas.Name  , OBJPROP_BACK   , aai.BackGround                                                ) ; //<4020>
}                                                                                                             //<4021>
//</A.System.Indicator: Function 14 >`````````````````````````````````````````````````````````````````````````//<4022>
                                                                                                              //<4023>
//< A.System.Indicator: Function 20 >`````````````````````````````````````````````````````````````````````````//<4024>
int    afr.PlotOrderLevels            ()          //    - elements // input    - / code       - / output    - //<4025>
{                                                                                                             //<4026>
if      ( avi.PlotOrderLevels > 0 )                                                                           //<4027>
        {                                                                                                     //<4028>
          static string als.OrderID ; als.OrderID = OrderMagicNumber () + "/" + OrderTicket   ()            ; //<4029>
                                                                                                              //<4030>
          int ali.PlotTime = iTime  ( aes.Symbol  , Period ()  , WindowFirstVisibleBar        () / 2      ) ; //<4031>
                                                                                                              //<4032>
          if ( avi.FlagOrderLevelsExist == 0 )                                                                //<4033>
             { avi.FlagOrderLevelsExist  = 1                                                                ; //<4034>
                                                                                                              //<4035>
               afr.CreateLineH ( avs.OrderPrice   , White  , 1 , STYLE_DASH , OrderOpenPrice  ()          ) ; //<4036>
               afr.CreateLineH ( avs.OrderTake    , Green  , 1 , STYLE_DASH , OrderTakeProfit ()          ) ; //<4037>
               afr.CreateLineH ( avs.OrderStop    , Red    , 1 , STYLE_DASH , OrderStopLoss   ()          ) ; //<4038>
                                                                                                              //<4039>
               afr.CreateText  ( avs.OrderPriceID , White              , 7  ,                                 //<4040>
                                 ali.PlotTime     , OrderOpenPrice  () , als.OrderID                      ) ; //<4041>
               afr.CreateText  ( avs.OrderTakeID  , Green              , 7  ,                                 //<4042>
                                 ali.PlotTime     , OrderTakeProfit () , als.OrderID                      ) ; //<4043>
               afr.CreateText  ( avs.OrderStopID  , Red                , 7  ,                                 //<4044>
                                 ali.PlotTime     , OrderStopLoss   () , als.OrderID                      ) ; //<4045>
             } // if                                                                                          //<4046>
          else                                                                                                //<4047>
             {                                                                                                //<4048>
               afr.ResetLineH  ( avs.OrderPrice   ,                           OrderOpenPrice  ()          ) ; //<4049>
               afr.ResetLineH  ( avs.OrderTake    ,                           OrderTakeProfit ()          ) ; //<4050>
               afr.ResetLineH  ( avs.OrderStop    ,                           OrderStopLoss   ()          ) ; //<4051>
                                                                                                              //<4052>
               afr.ResetText   ( avs.OrderPriceID , White              , 7  ,                                 //<4053>
                                 ali.PlotTime     , OrderOpenPrice  () , als.OrderID                      ) ; //<4054>
               afr.ResetText   ( avs.OrderTakeID  , Green              , 7  ,                                 //<4055>
                                 ali.PlotTime     , OrderTakeProfit () , als.OrderID                      ) ; //<4056>
               afr.ResetText   ( avs.OrderStopID  , Red                , 7  ,                                 //<4057>
                                 ali.PlotTime     , OrderStopLoss   () , als.OrderID                      ) ; //<4058>
             } // else                                                                                        //<4059>
        } // if                                                                                               //<4060>
else if ( avi.FlagOrderLevelsExist == 1 )                                                                     //<4061>
        { avi.FlagOrderLevelsExist  = 0                                                                     ; //<4062>
          afr.DeleteOrderLevels       ()                                                                    ; //<4063>
        }                                                                                                     //<4064>
}                                                                                                             //<4065>
//</A.System.Indicator: Function 20 >`````````````````````````````````````````````````````````````````````````//<4066>
                                                                                                              //<4067>
//< A.System.Indicator: Function 19 >`````````````````````````````````````````````````````````````````````````//<4068>
int    afr.DeleteOrderLevels          ()          //    - elements // input    - / code       - / output    - //<4069>
{                                                                                                             //<4070>
if ( avi.FlagOrderLevelsExist   == 1 )                                                                        //<4071>
   { avi.FlagOrderLevelsExist    = 0                                                                        ; //<4072>
     afr.DeleteOrderLevels          ()                                                                      ; //<4073>
     ObjectDelete ( avs.OrderPrice   )                                                                      ; //<4074>
     ObjectDelete ( avs.OrderTake    )                                                                      ; //<4075>
     ObjectDelete ( avs.OrderStop    )                                                                      ; //<4076>
     ObjectDelete ( avs.OrderPriceID )                                                                      ; //<4077>
     ObjectDelete ( avs.OrderTakeID  )                                                                      ; //<4078>
     ObjectDelete ( avs.OrderStopID  )                                                                      ; //<4079>
   }                                                                                                          //<4080>
}                                                                                                             //<4081>
//</A.System.Indicator: Function 19 >`````````````````````````````````````````````````````````````````````````//<4082>
                                                                                                              //<4083>
//< A.System.Indicator: Function 20 >`````````````````````````````````````````````````````````````````````````//<4084>
int    afr.PlotTimeBorders            ()          //    - elements // input    - / code       - / output    - //<4085>
{                                                                                                             //<4086>
if ( avi.FlagTimeBordersExist        == 0 )                                                                   //<4087>
   { avi.FlagTimeBordersExist         = 1                                                                   ; //<4088>
                                                                                                              //<4089>
     afr.CreateLineV ( avs.TimeBegin.2 , Green              , 1 , STYLE_DOT , arv.P.2 [ ari.P.TimeBegin ] ) ; //<4090>
     afr.CreateLineV ( avs.TimeBegin.3 , Green              , 1 , STYLE_DOT , arv.P.3 [ ari.P.TimeBegin ] ) ; //<4091>
   }                                                                                                          //<4092>
else                                                                                                          //<4093>
   { afr.ResetLineV  ( avs.TimeBegin.2                                      , arv.P.2 [ ari.P.TimeBegin ] ) ; //<4094>
     afr.ResetLineV  ( avs.TimeBegin.3                                      , arv.P.3 [ ari.P.TimeBegin ] ) ; //<4095>
   }                                                                                                          //<4096>
}                                                                                                             //<4097>
//</A.System.Indicator: Function 20 >`````````````````````````````````````````````````````````````````````````//<4098>
                                                                                                              //<4099>
//< A.System.Indicator: Function 19 >`````````````````````````````````````````````````````````````````````````//<4100>
int    afr.DeleteTimeBorders          ()          //    - elements // input    - / code       - / output    - //<4101>
{                                                                                                             //<4102>
if ( avi.FlagTimeBordersExist        == 1 )                                                                   //<4103>
   { avi.FlagTimeBordersExist         = 0                                                                   ; //<4104>
                                                                                                              //<4105>
     ObjectDelete ( avs.TimeBegin.2 )                                                                       ; //<4106>
     ObjectDelete ( avs.TimeBegin.3 )                                                                       ; //<4107>
   }                                                                                                          //<4108>
}                                                                                                             //<4109>
//</A.System.Indicator: Function 19 >`````````````````````````````````````````````````````````````````````````//<4110>
                                                                                                              //<4111>
//< A.System.Indicator: Function 20 >`````````````````````````````````````````````````````````````````````````//<4112>
int    afr.PlotProfiles               ()          //    - elements // input    - / code       - / output    - //<4113>
{                                                                                                             //<4114>
//< Entry Point >                                                                                             //<4115>
if ( avi.FlagProfilesExist           == 0 )                                                                   //<4116>
   { avi.FlagProfilesExist            = 1                                                                   ; //<4117>
//</Entry Point >                                                                                             //<4118>
                                                                                                              //<4119>
     //< Vertical Markers >                                                                                   //<4120>
     static int ali.Time.1                                                                                  ; //<4121>
     static int ali.Time.0                                                                                  ; //<4122>
                                                                                                              //<4123>
     static int ali.Time.Central                                                                            ; //<4124>
                                                                                                              //<4125>
     static int ali.Time.Average.1                                                                          ; //<4126>
     static int ali.Time.Average.0                                                                          ; //<4127>
                                                                                                              //<4128>
     if ( arv.Chart [ ari.BarsShift ] > 0 )                                                                   //<4129>
      int ali.WidthMax   = arv.Chart        [ ari.BarsShift ] * Period () * 60 /      4                     ; //<4130>
     else ali.WidthMax   = arv.Chart        [ ari.BarsTotal ] * Period () * 60 / ( - 20 )                   ; //<4131>
                                                                                                              //<4132>
     ali.Time.1          = iTime            ( aes.Symbol      , Period () , arv.Chart [ ari.BarRight ] )      //<4133>
                                            + ali.WidthMax                                                  ; //<4134>
     ali.Time.Central    = ali.Time.1       + ali.WidthMax                                                  ; //<4135>
     ali.Time.0          = ali.Time.Central + ali.WidthMax                                                  ; //<4136>
                                                                                                              //<4137>
     ali.Time.Average.1  = ali.Time.Central                                                                   //<4138>
                         - MathRound ( 1.0 * ali.WidthMax * arv.Z [ ari.Z.VolumeMean.1      ]                 //<4139>
                                                          / arv.Z [ ari.Z.VolumeMax.1       ]             ) ; //<4140>
     ali.Time.Average.0  = ali.Time.Central                                                                   //<4141>
                         + MathRound ( 1.0 * ali.WidthMax * arv.Z [ ari.Z.VolumeChartMean.0 ]                 //<4142>
                                                          / arv.Z [ ari.Z.VolumeChartMax.0  ]             ) ; //<4143>
                                                                                                              //<4144>
     afr.CreateLineV ( avs.Line.1         , DarkGray              , 1 , STYLE_DOT    , ali.Time.1         ) ; //<4145>
     afr.CreateLineV ( avs.Line.0         , DarkGray              , 1 , STYLE_DOT    , ali.Time.0         ) ; //<4146>
     afr.CreateLineV ( avs.Line.Central   , Gray                  , 1 , STYLE_SOLID  , ali.Time.Central   ) ; //<4147>
     afr.CreateLineV ( avs.Line.Average.1 , DarkGray              , 1 , STYLE_DOT    , ali.Time.Average.1 ) ; //<4148>
     afr.CreateLineV ( avs.Line.Average.0 , DarkGray              , 1 , STYLE_DOT    , ali.Time.Average.0 ) ; //<4149>
     //</Vertical Markers >                                                                                   //<4150>
                                                                                                              //<4151>
     //< Zones >                                                                                              //<4152>
     static double ald.PriceStep    ; ald.PriceStep    = avd.QuotePoint * arv.Z [ ari.Z.StepPoints ]        ; //<4153>
                                                                                                              //<4154>
     for ( int i = 0 ; i < arv.Z [ ari.Z.IndexChartRange ] ; i ++ )                                           //<4155>
         {                                                                                                    //<4156>
           int    ali.Index         = i + arv.Z [ ari.Z.IndexChartLowest ]                                  ; //<4157>
                                                                                                              //<4158>
           double ald.Width.1       = avd.Zone.1 [ ali.Index ] / arv.Z [ ari.Z.VolumeMax.1 ]                ; //<4159>
           double ald.Width.0       = avd.Zone.0 [ ali.Index ] / arv.Z [ ari.Z.VolumeMax.0 ]                ; //<4160>
                                                                                                              //<4161>
           double ald.PriceBottom   = avd.QuotePoint * ( arv.Z [ ari.Z.BasePoints ]                           //<4162>
                                                     +   arv.Z [ ari.Z.StepPoints ]                           //<4163>
                                                     * ( arv.Z [ ari.Z.IndexShift ]  + ali.Index        ) ) ; //<4164>
                                                                                                              //<4165>
           double ald.PriceTop      = ald.PriceBottom  + ald.PriceStep                                      ; //<4166>
                                                                                                              //<4167>
           afr.CreateFrame ( avs.Profile.1    + i           , Silver                 ,                  1 ,   //<4168>
                             ali.Time.Central - ald.Width.1 * ali.WidthMax           ,       ald.PriceTop ,   //<4169>
                             ali.Time.Central                                        ,    ald.PriceBottom ) ; //<4170>
                                                                                                              //<4171>
           afr.CreateFrame ( avs.Profile.0    + i           , Silver                 ,                  1 ,   //<4172>
                             ali.Time.Central + ald.Width.0 * ali.WidthMax           ,       ald.PriceTop ,   //<4173>
                             ali.Time.Central                                        ,    ald.PriceBottom ) ; //<4174>
         } // for                                                                                             //<4175>
     //</Zones >                                                                                              //<4176>
                                                                                                              //<4177>
//< Exit Point >                                                                                              //<4178>
   } // if                                                                                                    //<4179>
//</Exit Point >                                                                                              //<4180>
}                                                                                                             //<4181>
//</A.System.Indicator: Function 20 >`````````````````````````````````````````````````````````````````````````//<4182>
                                                                                                              //<4183>
//< A.System.Indicator: Function 19 >`````````````````````````````````````````````````````````````````````````//<4184>
int    afr.DeleteProfiles             ()          //    - elements // input    - / code       - / output    - //<4185>
{                                                                                                             //<4186>
if ( avi.FlagProfilesExist           == 1 )                                                                   //<4187>
   { avi.FlagProfilesExist            = 0                                                                   ; //<4188>
                                                                                                              //<4189>
     for ( int i = 0 ; i < arv.Z [ ari.Z.IndexChartRange ] ; i ++ )                                           //<4190>
         { ObjectDelete  ( avs.Profile.1 + i )                                                              ; //<4191>
           ObjectDelete  ( avs.Profile.0 + i )                                                              ; //<4192>
         }                                                                                                    //<4193>
                                                                                                              //<4194>
     ObjectDelete        ( avs.Line.1         )                                                             ; //<4195>
     ObjectDelete        ( avs.Line.0         )                                                             ; //<4196>
     ObjectDelete        ( avs.Line.Central   )                                                             ; //<4197>
     ObjectDelete        ( avs.Line.Average.1 )                                                             ; //<4198>
     ObjectDelete        ( avs.Line.Average.0 )                                                             ; //<4199>
   }                                                                                                          //<4200>
}                                                                                                             //<4201>
//</A.System.Indicator: Function 19 >`````````````````````````````````````````````````````````````````````````//<4202>
                                                                                                              //<4203>
//< A.System.Indicator: Function 20 >`````````````````````````````````````````````````````````````````````````//<4204>
int    afr.PlotZoneLevels             ()          //    - elements // input    - / code       - / output    - //<4205>
{                                                                                                             //<4206>
if ( avi.FlagZoneLevelsExist         == 0 )                                                                   //<4207>
   { avi.FlagZoneLevelsExist          = 1                                                                   ; //<4208>
                                                                                                              //<4209>
     static double ald.PriceStep    ; ald.PriceStep    = avd.QuotePoint * arv.Z [ ari.Z.StepPoints ]        ; //<4210>
                                                                                                              //<4211>
     for ( int i = 0 ; i < arv.Z [ ari.Z.IndexChartRange ] ; i ++ )                                           //<4212>
         {                                                                                                    //<4213>
           int    ali.Index         = i + arv.Z [ ari.Z.IndexChartLowest ]                                  ; //<4214>
                                                                                                              //<4215>
           double ald.PriceBottom   = avd.QuotePoint * ( arv.Z [ ari.Z.BasePoints ]                           //<4216>
                                                     +   arv.Z [ ari.Z.StepPoints ]                           //<4217>
                                                     * ( arv.Z [ ari.Z.IndexShift ]  + ali.Index        ) ) ; //<4218>
                                                                                                              //<4219>
                  afr.CreateLineH   ( avs.ZoneLevels + i , LightGray , 1 , STYLE_DOT , ald.PriceBottom    ) ; //<4220>
                                                                                                              //<4221>
         } // for                                                                                             //<4222>
   } // if                                                                                                    //<4223>
}                                                                                                             //<4224>
//</A.System.Indicator: Function 20 >`````````````````````````````````````````````````````````````````````````//<4225>
                                                                                                              //<4226>
//< A.System.Indicator: Function 19 >`````````````````````````````````````````````````````````````````````````//<4227>
int    afr.DeleteZoneLevels           ()          //    - elements // input    - / code       - / output    - //<4228>
{                                                                                                             //<4229>
if ( avi.FlagZoneLevelsExist         == 1 )                                                                   //<4230>
   { avi.FlagZoneLevelsExist          = 0                                                                   ; //<4231>
                                                                                                              //<4232>
     for ( int i = 0 ; i < arv.Z [ ari.Z.IndexChartRange ] ; i ++ )                                           //<4233>
           ObjectDelete  ( avs.ZoneLevels + i )                                                             ; //<4234>
   }                                                                                                          //<4235>
}                                                                                                             //<4236>
//</A.System.Indicator: Function 19 >`````````````````````````````````````````````````````````````````````````//<4237>
                                                                                                              //<4238>
//< A.System.Extra: Control Module Function 2222 >````````````````````````````````````````````````````````````//<4239>
string afr.ResetPlotter               ()          //    - elements // input    - / code       - / output    - //<4240>
{                                                                                                             //<4241>
if ( arv.Chart [ ari.BarLeft       ] != WindowFirstVisibleBar                                            ()   //<4242>
  || arv.Chart [ ari.Resolution.H  ] != WindowBarsPerChart                                               ()   //<4243>
  || arv.Chart [ ari.PriceMax      ] != WindowPriceMax                                                   ()   //<4244>
  || arv.Chart [ ari.PriceMin      ] != WindowPriceMin                                                   () ) //<4245>
   {                                                                                                          //<4246>
     afr.MeasureChart                                                                                    () ; //<4247>
                                                                                                              //<4248>
     afr.DeleteTimeBorders                                                                               () ; //<4249>
     if ( avi.PlotTimeBorders         > 0 )                                                                   //<4250>
          afr.PlotTimeBorders                                                                            () ; //<4251>
                                                                                                              //<4252>
     arv.Z [ ari.Z.Set ]              = 1                                                                   ; //<4253>
     afr.SetMarketZones                                                                                  () ; //<4254>
   }                                                                                                          //<4255>
else if ( arv.1 [ ari.1.NewFrames  ]  > 0                                                                     //<4256>
       || arv.Z [ ari.Z.Set        ]  > 0 )                                                                   //<4257>
   {                                                                                                          //<4258>
     if ( avi.PlotTimeBorders         > 0 )                                                                   //<4259>
          afr.PlotTimeBorders                                                                            () ; //<4260>
     else afr.DeleteTimeBorders                                                                          () ; //<4261>
   }                                                                                                          //<4262>
}                                                                                                             //<4263>
//</A.System.Extra: Control Module Function 2222 >````````````````````````````````````````````````````````````//<4264>
                                                                                                              //<4265>
//</A.System.Extra: Control Module >--------------------------------------------------------------------------//<4266>
                                                                                                              //<4267>
//</8. Extra Code >===========================================================================================//<4268>