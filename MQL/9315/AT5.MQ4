//< 1. Property 7 >===========================================================================================//<   1>
                                                                                                              //<   2>
#property     copyright                 "Copyright (C) 2010, MetaQuotes Software Corp."                       //<   3>
#property     link                      "http://www.metaquotes.net"                                           //<   4>
                                                                                                              //<   5>
#define       A.System.Series           "A Tool"                                                              //<   6>
#define       A.System.Modification     "51003"                                                               //<   7>
#define       A.System.ReleaseDate      "2010.03.31"                                                          //<   8>
#define       A.System.Program          "Market Profile Generator"                                            //<   9>
#define       A.System.Programmer       "Airat Safin                           http://www.MQL4.com/users/AIS" //<  10>
                                                                                                              //<  11>
//</1. Property 7 >===========================================================================================//<  12>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////<  13>
//< 2. Structure 93 >=========================================================================================//<  14>
                                                                                                              //<  15>
//< Data >                      4 pages     contain    21 elements // int     16 / double     4 / string    1 //<  16>
//< 3. Constants >              1 page      contains    1 elements // int      1 / double     - / string    - //<  17>
//< 4. Variables >              3 pages     contain    20 elements // int     15 / double     4 / string    1 //<  18>
//</Data >                                                                                                    //<  19>
                                                                                                              //<  20>
//< Functions >                 3 methods   contain    72 elements // input    - / code      72 / output    - //<  21>
//  5. int    init                                      1 elements // input    - / code       1 / output    - //<  22>
//  6. int    deinit                                    1 elements // input    - / code       1 / output    - //<  23>
//  7. int    start                                     - elements // input    - / code      70 / output    - //<  24>
//</Functions >                                                                                               //<  25>
                                                                                                              //<  26>
//</2. Structure 93 >=========================================================================================//<  27>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////<  28>
//< 3. Constants 1 >==========================================================================================//<  29>
                                                                                                              //<  30>
//< Structure >                                                                                               //<  31>
//< 3. Constants >              1 page      contains    1 elements // int      1 / double     - / string    - //<  32>
//< 3.1. End Point Date 1 >                             1 elements // int      1 / double     - / string    - //<  33>
//</Structure >                                                                                               //<  34>
                                                                                                              //<  35>
//< 3.1. End Point Date 1 >                             1 elements // int      1 / double     - / string    - //<  36>
#define       aci.DateEnd               D'2010.01.01 00:00'                                                   //<  37>
//</3.1. End Point Date 1 >                                                                                   //<  38>
                                                                                                              //<  39>
//</3. Constants 1 >==========================================================================================//<  40>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////<  41>
//< 4. Variables 20 >=========================================================================================//<  42>
                                                                                                              //<  43>
//< Structure >                                                                                               //<  44>
//< 4. Variables >              3 pages     contain    20 elements // int     15 / double     4 / string    1 //<  45>
//< 4.1. Chart Descriptors 2 >                          2 elements // int      1 / double     - / string    1 //<  46>
//< 4.2. Profile Register 15 >                         15 elements // int     14 / double     1 / string    - //<  47>
//< 4.3. Market Profile 3 >                             3 elements // int      - / double     3 / string    - //<  48>
//</Structure >                                                                                               //<  49>
                                                                                                              //<  50>
//< 4.1. Chart Descriptors 2 >                          2 elements // int      1 / double     - / string    1 //<  51>
int           avi.Period                                                                                    ; //<  52>
string        avs.Symbol                                                                                    ; //<  53>
//</4.1. Chart Descriptors 2 >                                                                                //<  54>
                                                                                                              //<  55>
//< 4.2. Profile Register 15 >                         15 elements // int     14 / double     1 / string    - //<  56>
              //< 4.2.1. Index 14 >                                                                           //<  57>
#define       ari.P.TimeBegin           1                                                                     //<  58>
#define       ari.P.TimeEnd             2                                                                     //<  59>
#define       ari.P.Frames              3                                                                     //<  60>
#define       ari.P.Volume              4                                                                     //<  61>
#define       ari.P.PricePoints         5                                                                     //<  62>
#define       ari.P.PriceRange          6                                                                     //<  63>
#define       ari.P.PriceLowest         7                                                                     //<  64>
#define       ari.P.PriceHighest        8                                                                     //<  65>
#define       ari.P.MaxRange            9                                                                     //<  66>
#define       ari.P.MaxVolume           10                                                                    //<  67>
#define       ari.P.DateMaxRange        11                                                                    //<  68>
#define       ari.P.DateMaxVolume       12                                                                    //<  69>
#define       ari.P.Reserved            13                                                                    //<  70>
#define       ari.P.RegisterDimension   14                                                                    //<  71>
              //</4.2.1. Index 14 >                                                                           //<  72>
                                                                                                              //<  73>
              //< 4.2.2. Register 1 >                                                                         //<  74>
double        arv.P                   [ ari.P.RegisterDimension ]                                           ; //<  75>
              //</4.2.2. Register 1 >                                                                         //<  76>
//</4.2. Profile Register 15 >                                                                                //<  77>
                                                                                                              //<  78>
//< 4.3. Market Profile 3 >                             3 elements // int      - / double     3 / string    - //<  79>
double        avd.Profile             []                                                                    ; //<  80>
                                                                                                              //<  81>
double        avd.ProfileFrames       = 0                                                                   ; //<  82>
double        avd.ProfilePoints       = 0                                                                   ; //<  83>
//</4.3. Market Profile 3 >                                                                                   //<  84>
                                                                                                              //<  85>
//</4. Variables 20 >=========================================================================================//<  86>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////<  87>
//< 5. Program Initialization 1 >=============================================================================//<  88>
                                                                                                              //<  89>
int    init                          ()           //    1 elements // input    - / code       1 / output    - //<  90>
{                                                                                                             //<  91>
Alert       ( A.System.Series , ": "  , A.System.Program , " Start "         , TimeToStr ( TimeLocal () ) ) ; //<  92>
}                                                                                                             //<  93>
//</5. Program Initialization 1 >=============================================================================//<  94>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////<  95>
//< 6. Program Deinitialization 1 >===========================================================================//<  96>
                                                                                                              //<  97>
int    deinit                        ()           //    1 elements // input    - / code       1 / output    - //<  98>
{                                                                                                             //<  99>
Alert       ( A.System.Series , ": "  , A.System.Program , " Stop "          , TimeToStr ( TimeLocal () ) ) ; //< 100>
}                                                                                                             //< 101>
//</6. Program Deinitialization 1 >===========================================================================//< 102>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////< 103>
//< 7. Main Program   >=======================================================================================//< 104>
                                                                                                              //< 105>
int    start                         ()           //    - elements // input    - / code      70 / output    - //< 106>
                                                                                                              //< 107>
//< 7.1. Chart Descriptors Definition 2 >                                                                     //< 108>
//< 7.2. Profile Preparation 15 >                                                                             //< 109>
//< 7.3. Profile Processing 25 >                                                                              //< 110>
//< 7.4. File Operation 15 >                                                                                  //< 111>
//< 7.5. Final Report 12 >                                                                                    //< 112>
//< 7.6. Exception Handler 1 >                                                                                //< 113>
{                                                                                                             //< 114>
//< 7.1. Chart Descriptors Definition 2 >                                                                     //< 115>
avi.Period                   = PERIOD_M1                                                                    ; //< 116>
avs.Symbol                   = Symbol ()                                                                    ; //< 117>
//</7.1. Chart Descriptors Definition 2 >                                                                     //< 118>
                                                                                                              //< 119>
//< 7.2. Profile Preparation 15 >                                                                             //< 120>
int     ali.IndexBegin       = iBars     ( avs.Symbol , avi.Period                         ) - 1            ; //< 121>
int     ali.IndexEnd         = iBarShift ( avs.Symbol , avi.Period           , aci.DateEnd )                ; //< 122>
                                                                                                              //< 123>
arv.P [ ari.P.TimeBegin    ] = iTime     ( avs.Symbol , avi.Period           , ali.IndexBegin             ) ; //< 124>
arv.P [ ari.P.TimeEnd      ] = iTime     ( avs.Symbol , avi.Period           , ali.IndexEnd               ) ; //< 125>
                                                                                                              //< 126>
arv.P [ ari.P.Frames       ] =             ali.IndexBegin                    - ali.IndexEnd  + 1            ; //< 127>
                                                                                                              //< 128>
int     ali.IndexLowest      = iLowest   ( avs.Symbol , avi.Period           , MODE_LOW                   ,   //< 129>
                                           arv.P      [ ari.P.Frames       ] , ali.IndexEnd               ) ; //< 130>
int     ali.IndexHighest     = iHighest  ( avs.Symbol , avi.Period           , MODE_HIGH                  ,   //< 131>
                                           arv.P      [ ari.P.Frames       ] , ali.IndexEnd               ) ; //< 132>
                                                                                                              //< 133>
arv.P [ ari.P.PriceLowest  ] = iLow      ( avs.Symbol , avi.Period           , ali.IndexLowest            ) ; //< 134>
arv.P [ ari.P.PriceHighest ] = iHigh     ( avs.Symbol , avi.Period           , ali.IndexHighest           ) ; //< 135>
                                                                                                              //< 136>
arv.P [ ari.P.PriceRange   ] =             arv.P      [ ari.P.PriceHighest ] - arv.P [ ari.P.PriceLowest  ] ; //< 137>
arv.P [ ari.P.PricePoints  ] = MathRound ( arv.P      [ ari.P.PriceRange   ] / Point )       + 1            ; //< 138>
                                                                                                              //< 139>
ArrayResize     ( avd.Profile , arv.P     [ ari.P.PricePoints  ]                                          ) ; //< 140>
ArrayInitialize ( avd.Profile , 0                                                                         ) ; //< 141>
//</7.2. Profile Preparation 15 >                                                                             //< 142>
                                                                                                              //< 143>
//< 7.3. Profile Processing 25 >                                                                              //< 144>
arv.P [ ari.P.MaxRange     ] = 0                                                                            ; //< 145>
arv.P [ ari.P.MaxVolume    ] = 0                                                                            ; //< 146>
                                                                                                              //< 147>
int   i , j                                                                                                 ; //< 148>
for ( i = ali.IndexBegin ; i >=  ali.IndexEnd       ; i --           )                                        //< 149>
    {                                                                                                         //< 150>
      int    ali.Range       = ( iHigh ( avs.Symbol , avi.Period , i )                                        //< 151>
                               - iLow  ( avs.Symbol , avi.Period , i )     ) / Point                        ; //< 152>
                                                                                                              //< 153>
      int    ali.IndexHigh   = ( iHigh ( avs.Symbol , avi.Period , i )                                        //< 154>
                               - arv.P [ ari.P.PriceLowest           ]     ) / Point                        ; //< 155>
                                                                                                              //< 156>
      int    ali.IndexLow    = ( iLow  ( avs.Symbol , avi.Period , i )                                        //< 157>
                               - arv.P [ ari.P.PriceLowest           ]     ) / Point                        ; //< 158>
                                                                                                              //< 159>
      double ald.VolumeFrame = iVolume ( avs.Symbol , avi.Period , i )                                      ; //< 160>
                                                                                                              //< 161>
      avd.ProfileFrames      = avd.ProfileFrames    + ald.VolumeFrame                                       ; //< 162>
                                                                                                              //< 163>
      double ald.VolumePoint = ald.VolumeFrame    / ( ali.IndexHigh    - ali.IndexLow + 1 )                 ; //< 164>
                                                                                                              //< 165>
      for  ( j     = ali.IndexLow ; j     <= ali.IndexHigh      ; j ++ )                                      //< 166>
                     avd.Profile  [ j ]    = avd.Profile        [ j ]  + ald.VolumePoint                    ; //< 167>
                                                                                                              //< 168>
      if   ( arv.P [ ari.P.MaxRange      ] - ali.Range          < 0 )                                         //< 169>
           { arv.P [ ari.P.MaxRange      ] = ali.Range                                                      ; //< 170>
             arv.P [ ari.P.DateMaxRange  ] = iTime ( avs.Symbol , avi.Period , i )                          ; //< 171>
           }                                                                                                  //< 172>
                                                                                                              //< 173>
      if   ( arv.P [ ari.P.MaxVolume     ] - ald.VolumeFrame    < 0 )                                         //< 174>
           { arv.P [ ari.P.MaxVolume     ] = ald.VolumeFrame                                                ; //< 175>
             arv.P [ ari.P.DateMaxVolume ] = iTime ( avs.Symbol , avi.Period , i )                          ; //< 176>
           }                                                                                                  //< 177>
    }                                                                                                         //< 178>
//</7.3. Profile Processing 25 >                                                                              //< 179>
                                                                                                              //< 180>
//< 7.4. File Operation 15 >                                                                                  //< 181>
int          ali.FileHandle           = FileOpen    ( avs.Symbol + ".CSV"  , FILE_CSV | FILE_WRITE ,  ";" ) ; //< 182>
FileWrite  ( ali.FileHandle           , DoubleToStr ( arv.P [ ari.P.TimeBegin     ] , 0      )            ) ; //< 183>
FileWrite  ( ali.FileHandle           , DoubleToStr ( arv.P [ ari.P.TimeEnd       ] , 0      )            ) ; //< 184>
FileWrite  ( ali.FileHandle           , DoubleToStr ( arv.P [ ari.P.Frames        ] , 0      )            ) ; //< 185>
FileWrite  ( ali.FileHandle           , DoubleToStr ( arv.P [ ari.P.PricePoints   ] , 0      )            ) ; //< 186>
FileWrite  ( ali.FileHandle           , DoubleToStr ( arv.P [ ari.P.MaxRange      ] , 0      )            ) ; //< 187>
FileWrite  ( ali.FileHandle           , DoubleToStr ( arv.P [ ari.P.MaxVolume     ] , 0      )            ) ; //< 188>
FileWrite  ( ali.FileHandle           , DoubleToStr ( arv.P [ ari.P.DateMaxRange  ] , 0      )            ) ; //< 189>
FileWrite  ( ali.FileHandle           , DoubleToStr ( arv.P [ ari.P.DateMaxVolume ] , 0      )            ) ; //< 190>
                                                                                                              //< 191>
for ( i = 0 ;                       i < arv.P [ ari.P.PricePoints ]           ; i ++         )                //< 192>
    { FileWrite ( ali.FileHandle      , DoubleToStr ( arv.P [ ari.P.PriceLowest   ] + Point * i , Digits  ) , //< 193>
                                        DoubleToStr (         avd.Profile     [ i ] , 8      )            ) ; //< 194>
      avd.ProfilePoints               = avd.ProfilePoints   + avd.Profile     [ i ]                         ; //< 195>
    }                                                                                                         //< 196>
                                                                                                              //< 197>
FileClose  ( ali.FileHandle                                                                               ) ; //< 198>
//</7.4. File Operation 15 >                                                                                  //< 199>
                                                                                                              //< 200>
//< 7.5. Final Report 12 >                                                                                    //< 201>
Alert      ( "End:           "        , TimeToStr   ( arv.P [ ari.P.TimeEnd       ]          )            ) ; //< 202>
Alert      ( "Begin:         "        , TimeToStr   ( arv.P [ ari.P.TimeBegin     ]          )            ) ; //< 203>
Alert      ( "MaxRange:      "        , DoubleToStr ( arv.P [ ari.P.MaxRange      ] , 0      )   , " / "  ,   //< 204>
                                        TimeToStr   ( arv.P [ ari.P.DateMaxRange  ]          )            ) ; //< 205>
Alert      ( "MaxVolume:     "        , DoubleToStr ( arv.P [ ari.P.MaxVolume     ] , 0      )   , " / "  ,   //< 206>
                                        TimeToStr   ( arv.P [ ari.P.DateMaxVolume ]          )            ) ; //< 207>
Alert      ( "Lowest:        "        , DoubleToStr ( arv.P [ ari.P.PriceLowest   ] , Digits )            ) ; //< 208>
Alert      ( "Highest:       "        , DoubleToStr ( arv.P [ ari.P.PriceHighest  ] , Digits )            ) ; //< 209>
Alert      ( "Points:        "        , DoubleToStr ( arv.P [ ari.P.PricePoints   ] , 0      )            ) ; //< 210>
Alert      ( "Frames:        "        , DoubleToStr ( arv.P [ ari.P.Frames        ] , 0      )            ) ; //< 211>
Alert      ( "ProfileFrames: "        , DoubleToStr ( avd.ProfileFrames             , 0      )            ) ; //< 212>
Alert      ( "ProfilePoints: "        , DoubleToStr ( avd.ProfilePoints             , 0      )            ) ; //< 213>
//</7.5. Final Report 12 >                                                                                    //< 214>
                                                                                                              //< 215>
//< 7.6. Exception Handler 1 >                                                                                //< 216>
Alert      ( "Exception:     "        , GetLastError ()                                                   ) ; //< 217>
//</7.6. Exception Handler 1 >                                                                                //< 218>
}                                                                                                             //< 219>
//</7. Main Program   >=======================================================================================//< 220>