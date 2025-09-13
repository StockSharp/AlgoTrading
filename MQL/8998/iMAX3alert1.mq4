/*+-------------------------------------------------------------------+
  |                                                    iMAX3alert.mq4 |
  | Based upon the iMAX3 indicator.                                   |
  |                                                                   |
  | In this alert version only one of the three modes available can   |
  | selected at a time in order to generate optional display arrows   |
  | and alerts on a phase crossing/fast trend change.                 |
  |                                                                   |
  | The iMAX mode is the most adaptive, but the least responsive in   |
  | terms of speed of detection of a trend change of the three modes. |
  | However, it is a good all around choice, especially in highly     |
  | volatile markets.                                                 |
  |                                                                   |
  | The iMAXhp mode is a faster trend detector, adaptive, but less    |
  | immune to spikes in prices than the iMAX mode (More easily        |
  | whipsawed by price action). It is a good compromise between trend |
  | detection speed/filtering.                                        |
  |                                                                   |
  | The iMAXhpx mode is the fastest trend detector, but because it    |
  | follows price action so closely, it will change trends very       |
  | rapidly at times... even in less volatile markets.  So it is      |
  | probably not a good choice for alerts, in the lower time frames...|
  | under almost any circumstances, but might work out in higher time |
  | frames... so, it is available.                                    |
  |                                                                   |
  | It is up to the user to select an appropriate mode for the        |
  | desired speed of detection and price action filtering, for the    |
  | security and time frame that alerts are to be generated for.      |                                                             |
  |                                                                   |
  | Alerts and arrows are generated on the open bar... that means     |
  | they can change as price changes on that bar.  Once the bar       |
  | closes, the arrow becomes fixed.  Alerts are generated on the     |
  | first alert event on the open bar, giving the earliest possible   |
  | warning of a possible trend change, but thereafter may not be     |
  | triggered again until bar closing... this is done to strike a     |
  | compromise in favor of an early alert.  Price can move back and   |
  | forth over a phase crossing on an open bar generating repititious |
  | alerts, that most traders find irritating.  However, an alert     |
  | generated on a closed bar may come to late to do any good for a   |
  | trader... So, the compromise is to alert the trader to the first  |
  | crossing event on the open bar... and then they know to watch     |
  | what happens after that, on that bar, because until a new bar     |
  | forms, there may not be another alert generated.  This is a       |
  | "heads up" alert system.                                          |
  |                                                                   |
  | Twenty currency pairs have been checked for compantibility with   |
  | the preset amplitude settings for the hp modes.  In that group,   |
  | two currencies, the Mexican Peso, and the Japanese Yen are        |
  | treated as exceptions, with unique settings provided for in       |
  | those cases.  So, this version of the iMAX indicator is a good    |
  | example of how to handle programming the amplitude phase shifts   |
  | for the hp trend detection modes.                                 |
  |                                                                   |
  | iMAX mode 0 will automatically handle any security or commodity   |
  | pair detected that hp modes are not programmed for.               |
  |                                                                   |
  | This version is liberally commented to help in fully understanding|
  | the iMAX indicator in practical usage.                            |
  |                                                                   |
  | "Modified Optimum Elliptic Filter" ref:                           |                                            |
  | Source of calculations;                                           |
  | Stocks & Commodities vol 18:7 p20-29                              |
  | Optimal Detrending by John F. Ehlers                              |
  |                                                                   |
  |                                              Crafted by Wylie     |
  |                                              Copyright © 2009     |
  |                                              dazzle.html@live.com |
  +-------------------------------------------------------------------+*/

#property copyright "Copyright © 2009, Wylie"
#property link      "dazzle.html@live.com"

#property indicator_chart_window
#property indicator_buffers 8
#property indicator_color1  Lime
#property indicator_width1  1
#property indicator_color2  HotPink
#property indicator_width2  1
#property indicator_color3  White
#property indicator_width3  1
#property indicator_color4  Orange
#property indicator_width4  1
#property indicator_color5  Aqua
#property indicator_width5  1
#property indicator_color6  Red
#property indicator_width6  1
#property indicator_color7  Aqua
#property indicator_width7  1
#property indicator_color8  Red
#property indicator_width8  1

/*+-------------------------------------------------------------------+
  | iMAX3alert Parameters                                             |
  +-------------------------------------------------------------------+*/

extern string   _0               = "";
 bool     EnableAlerts     = false;
 bool     EnableArrows     = false;
 int      ArrowUPsize      = 1;
 int      ArrowDNsize      = 1;
 color    ArrowUPcolor     = Aqua;
 color    ArrowDNcolor     = Red;

 string   _1               = "";
 string   _2               = "Select Mode";
 string   _3               = "iMAX mode = 0";
 string   _4               = "hp mode   = 1";
 string   _5               = "hpx mode  = 2";
 int      Mode_user        = 1;
 string   _6               = "";

 string   _7               = "Display phases on chart";
 bool     ChartPhases      = true;
 string   _8               = "";

 string   _9               = "Amplitude shift for hp modes.";
 string   _10              = "0.0 = Use internal presets.";
 double   Ph1stepHPmodes   = 0.0;

 string   _11              = "";
 color    hpxPh1color      = Lime;
 color    hpxPh2color      = HotPink;

 string   _12              = "";
 color    hpPh1color       = White;
 color    hpPh2color       = Orange;

 string   _13              = "";
 color    iMAXph1color     = Aqua;
 color    iMAXph2color     = Red;


bool            ModeFault        = false,   // Used to flag an incorrect mode selection by the user.
                Mode;                       // Used to switch over to mode 0, if hp modes are not programmed to handle a security pair.
                                            // indicator buffers
double          iMAX0[10000],iMAX1[10000],hp0[],hp1[],hpx0[],hpx1[],CrossUp[10000],CrossDn[10000], 
                Clearance,                  // Used to set clearance distance for arrows above or below price on the chart.
                Ph1step,                    // Phase 1 amplitude modulation value for hp modes.
                x,                          // Available for use as a temporary variable.
                xhp0,xhp1,                  // Used to compensate for 180 degree phase difference between iMAX and hp mode.
                b0,b1,r0,r1;                // Used to interface phases to arrow and alert logic.


int             MinBars,limit,countedBars,
                AST,                        // Alert stop time.  Time filter period to prevent repetitious alerts from occurring.
                c,                          // Integer variable used for counting.
                sym,                        // Integer identifying either JPY, MXN, or all other securities (states 1 thru 3).
                NumSym;                     // Number of authorized securities.

string          SymList[20],                // Array containing authorized security pairs. 
                symStr,                     // Truncated symbol string to identify one security from the symbol pair.
                Chart;                      // Used by alert function to identify the chart reporting the alert.


static datetime AlertX[3];                  // Array used ot store alert event time flags.


/*+-------------------------------------------------------------------+
  | iMAX3alert Initialization                                         |
  +-------------------------------------------------------------------+*/

int init()
{
 SymList[0]  = "AUDCAD";  // Symbols this indicator has been checked to function with.
 SymList[1]  = "AUDNZD";  // Other symbols can be added or substituted here if the
 SymList[2]  = "AUDJPY";  // securities pose no problems for the Ph1step amplitude
 SymList[3]  = "AUDUSD";  // settings for the hp modes.  That can be modified too, if necessatry.
 SymList[4]  = "CADJPY";
 SymList[5]  = "CHFJPY";
 SymList[6]  = "EURAUD";
 SymList[7]  = "EURCAD";
 SymList[8]  = "EURCHF";
 SymList[9]  = "EURGBP";
 SymList[10] = "EURJPY";
 SymList[11] = "EURUSD";
 SymList[12] = "GBPCHF";
 SymList[13] = "GBPJPY";
 SymList[14] = "GBPUSD";
 SymList[15] = "NZDUSD";
 SymList[16] = "USDCAD";
 SymList[17] = "USDCHF";
 SymList[18] = "USDMXN";
 SymList[19] = "USDJPY";
 SymList[20] = "USDTRY";NumSym = 20;

 if(ModeFault)
    {Mode_user = 0;Mode = 0;ModeFault = false;}

 if((Mode_user == 1 || Mode_user == 2) && !GoSym())
    {sendAlert(3,("iMAX3alert: HP modes not programmed to function with this Symbol. Switching to mode 0."));
     Mode = 0;}
 else
    {Mode = Mode_user;}

 IndicatorBuffers(8);

 if(Mode == 2)                 // Not initializing these buffers unless hpxMode mode is selected.
    {SetIndexBuffer(0,hpx0);
     SetIndexBuffer(1,hpx1);
     if(ChartPhases)           // Display phases in chart price area
        {SetIndexStyle(0,DRAW_LINE,STYLE_SOLID,1,hpxPh1color);
         SetIndexStyle(1,DRAW_LINE,STYLE_SOLID,1,hpxPh2color);}
     else                      // Do not Display phases in chart price area
        {SetIndexStyle(0,DRAW_NONE);
         SetIndexStyle(1,DRAW_NONE);}}

 if(Mode == 1)                 // Not initializing these buffers unless hpMode mode is selected.
    {SetIndexBuffer(2,hp0);
     SetIndexBuffer(3,hp1);
     if(ChartPhases)           // Display phases in chart price area
        {SetIndexStyle(2,DRAW_LINE,STYLE_SOLID,1,hpPh1color);
         SetIndexStyle(3,DRAW_LINE,STYLE_SOLID,1,hpPh2color);}
     else                      // Do not Display phases in chart price area
        {SetIndexStyle(2,DRAW_NONE);
         SetIndexStyle(3,DRAW_NONE);}}

 SetIndexBuffer(4,iMAX0);
 SetIndexBuffer(5,iMAX1);
 if(Mode == 0 && ChartPhases)  // Display phases in chart price area
    {SetIndexStyle(4,DRAW_LINE,STYLE_SOLID,1,iMAXph1color);
     SetIndexStyle(5,DRAW_LINE,STYLE_SOLID,1,iMAXph2color);}
 else                          // Do not Display phases in chart price area
    {SetIndexStyle(4,DRAW_NONE);
     SetIndexStyle(5,DRAW_NONE);}

 if(EnableArrows)              // Not initializing these buffers unless arrows generation is selected.
    {SetIndexBuffer(6,CrossUp);
     SetIndexStyle(6,DRAW_ARROW,STYLE_SOLID,ArrowUPsize,ArrowUPcolor);
     SetIndexArrow(6,221);

     SetIndexBuffer(7,CrossDn);
     SetIndexStyle(7,DRAW_ARROW,STYLE_SOLID,ArrowDNsize,ArrowDNcolor);
     SetIndexArrow(7,222);}

 symStr = StringSubstr(Symbol(),3,3); // Extract the portion of the symbol string that may contain
                                      // references to JPY or MXN securities.
 sym = 0;                             // Set an integer to identify JPY, MXN, or other securities.
 if(symStr == "MXN"){sym = 1;}
 if(symStr == "JPY"){sym = 2;}

 if(Mode == 1 || Mode == 2)    // Set phase amplitude values if an hp mode is selected.
    {if(Ph1stepHPmodes == 0.0) // If no external amplitude is specified...
        {switch(sym)           // Select values for JPY, MXN, or all other securites.
            {case 0:           // Securities other than JPY or MXN
             switch(Period())  // Phase 1 amplitude values for each time frame for security pairs other than JPY and MXN.
                {case 1:     Ph1step = 0.0001;  Clearance = 0.0003; break;
                 case 5:     Ph1step = 0.00015; Clearance = 0.0005; break;
                 case 15:    Ph1step = 0.0003;  Clearance = 0.0009; break;
                 case 30:    Ph1step = 0.0005;  Clearance = 0.0015; break;
                 case 60:    Ph1step = 0.00075; Clearance = 0.0025; break;
                 case 240:   Ph1step = 0.0015;  Clearance = 0.004;  break;
                 case 1440:  Ph1step = 0.003;   Clearance = 0.007;  break;
                 case 10080: Ph1step = 0.005;   Clearance = 0.014;  break;
                 case 43200: Ph1step = 0.01;    Clearance = 0.026;  break;}break;
             case 1: // MXN securities
             switch(Period())  // Phase 1 amplitude values for each time frame of MXN paired securities.
                {case 1:     Ph1step = 0.001;  Clearance = 0.003; break;
                 case 5:     Ph1step = 0.0015; Clearance = 0.005; break;
                 case 15:    Ph1step = 0.003;  Clearance = 0.009; break;
                 case 30:    Ph1step = 0.005;  Clearance = 0.015; break;
                 case 60:    Ph1step = 0.0075; Clearance = 0.025; break;
                 case 240:   Ph1step = 0.015;  Clearance = 0.04;  break;
                 case 1440:  Ph1step = 0.03;   Clearance = 0.07;  break;
                 case 10080: Ph1step = 0.05;   Clearance = 0.14;  break;
                 case 43200: Ph1step = 0.1;    Clearance = 0.26;  break;}break;
             case 2: // JPY securities
             switch(Period())  // Phase 1 amplitude values for each time frame of JPY paired securities.
                {case 1:     Ph1step = 0.01;  Clearance = 0.03; break;
                 case 5:     Ph1step = 0.015; Clearance = 0.05; break;
                 case 15:    Ph1step = 0.03;  Clearance = 0.09; break;
                 case 30:    Ph1step = 0.05;  Clearance = 0.15; break;
                 case 60:    Ph1step = 0.075; Clearance = 0.25; break;
                 case 240:   Ph1step = 0.15;  Clearance = 0.4;  break;
                 case 1440:  Ph1step = 0.3;   Clearance = 0.7;  break;
                 case 10080: Ph1step = 0.5;   Clearance = 1.4;  break;
                 case 43200: Ph1step = 1.0;   Clearance = 2.6;  break;}break;}}
     else                      // Set amplitude via external parameter.
        {Ph1step = Ph1stepHPmodes;}}

 if(Mode == 0)  // Set chart arrow clearances for mode 0
    {switch(Period())
        {case 1:     Clearance = 0.0003; break;
         case 5:     Clearance = 0.0005; break;
         case 15:    Clearance = 0.0009; break;
         case 30:    Clearance = 0.0015; break;
         case 60:    Clearance = 0.0025; break;
         case 240:   Clearance = 0.004;  break;
         case 1440:  Clearance = 0.007;  break;
         case 10080: Clearance = 0.014;  break;
         case 43200: Clearance = 0.026;  break;}}

 if(Mode == 0 && symStr == "MXN")  // Set chart arrow clearances for mode 0 Mexican Peso
    {switch(Period())
        {case 1:     Clearance = 0.003; break;
         case 5:     Clearance = 0.005; break;
         case 15:    Clearance = 0.009; break;
         case 30:    Clearance = 0.015; break;
         case 60:    Clearance = 0.025; break;
         case 240:   Clearance = 0.04;  break;
         case 1440:  Clearance = 0.07;  break;
         case 10080: Clearance = 0.14;  break;
         case 43200: Clearance = 0.26;  break;}}

 if(Mode == 0 && symStr == "JPY")  // Set chart arrow clearances for mode 0 Japanese Yen
    {switch(Period())
        {case 1:     Clearance = 0.03; break;
         case 5:     Clearance = 0.05; break;
         case 15:    Clearance = 0.09; break;
         case 30:    Clearance = 0.15; break;
         case 60:    Clearance = 0.25; break;
         case 240:   Clearance = 0.4;  break;
         case 1440:  Clearance = 0.7;  break;
         case 10080: Clearance = 1.4;  break;
         case 43200: Clearance = 2.6;  break;}}

 switch(Period())  // Set general time frame related settings. Chart is used in Alert text, and Alert Stop Time varies by bar time.
     {case 1:     Chart = "M1";  AST =      59; break;
      case 5:     Chart = "M5";  AST =     259; break;
      case 15:    Chart = "M15"; AST =     899; break;
      case 30:    Chart = "M30"; AST =    1799; break;
      case 60:    Chart = "H1";  AST =    3599; break;
      case 240:   Chart = "H4";  AST =   14399; break;
      case 1440:  Chart = "D1";  AST =   86399; break;
      case 10080: Chart = "W1";  AST =  604799; break;
      case 43200: Chart = "MN1"; AST = 2591999; break;}

 MinBars = 20;

 IndicatorShortName("iMAX3alert");

 return (0);
} // int init()

/*+-------------------------------------------------------------------+
  | iMAX3alert Main cycle                                             |
  +-------------------------------------------------------------------+*/

int start()
{
 if(Bars <= MinBars)
    {Alert("iMAX3alert: Not enough bars on the chart.");return (0);}

 if(Mode < 0 || Mode > 2)
    {if(!ModeFault)
        {ModeFault = true;
         Alert("iMAX3alert: User selected an invalid mode... switched to iMAX mode 0.");init();}}
 else
    {ModeFault = false;}

 countedBars = IndicatorCounted();

 if(countedBars < 0){return (-1);}
 if(countedBars > 0){countedBars--;}

 limit = Bars - countedBars - 1;
 x     = 0.5;
 c     = limit;

 while(c >= 0)
     {
      // Perform Wylie's modified Ehlers' calculation when hpx mode is selected.
      // Results are loaded into indicator buffers hpx0[] (phase 1) and hpx1[] (phase 2)
      // Result is also phase compensated to agree with iMAX mode.
      if(Mode == 2)
         {hpx1[c] = (0.13785 * (2 * ((High[c]   + Low[c])   * x) - ((High[c+1] + Low[c+1]) * x)))
                  + (0.0007  * (2 * ((High[c+1] + Low[c+1]) * x) - ((High[c+2] + Low[c+2]) * x)))
                  + (0.13785 * (2 * ((High[c+2] + Low[c+2]) * x) - ((High[c+3] + Low[c+3]) * x)))
                  + (1.2103  * hpx0[c + 1] - 0.4867 * hpx0[c + 2]); 
          if(Close[c] > hpx1[c])
             {hpx0[c] = hpx1[c] + Ph1step;}
          if(Close[c] < hpx1[c])
             {hpx0[c] = hpx1[c] - Ph1step;}
          b0 = hpx0[c];b1 = hpx0[c+1];r0 = hpx1[c];r1 = hpx1[c+1];} // Copy phase values to arrow and alert interface variables.

      // Perform Ehlers calculation when iMAXmode or hp mode is selected.
      // Results are loaded into indicator buffers iMAX0[] (phase 1) and iMAX1[] (phase 2)
      if(Mode == 0 || Mode == 1)
         {iMAX0[c] = (0.13785 * (2 * ((High[c]   + Low[c])   * x) - ((High[c+1] + Low[c+1]) * x)))
                   + (0.0007  * (2 * ((High[c+1] + Low[c+1]) * x) - ((High[c+2] + Low[c+2]) * x)))
                   + (0.13785 * (2 * ((High[c+2] + Low[c+2]) * x) - ((High[c+3] + Low[c+3]) * x)))
                   + (1.2103  * iMAX0[c + 1] - 0.4867 * iMAX0[c + 2]); 
          xhp0     = iMAX0[c];    // Set this variable in case hp mode is selected.
                                  // In hp mode the xhp0 variable is used to synthesize hp phases.
          iMAX1[c] = iMAX0[c+1];  // Basic iMAX single bar phase shift.
          b0 = iMAX0[c];b1 = iMAX0[c+1];r0 = iMAX1[c];r1 = iMAX1[c+1];} // Copy phase values to arrow and alert interface variables.


      // Synthesize hp phases from iMAX0 phase 1 signal when hp mode is selected.
      // Results are loaded into indicator buffers hp0[] (phase 1) and hp1[] (phase 2)
      if(Mode == 1)
         {if(Close[c] > iMAX0[c])
             {xhp1 = iMAX0[c] + Ph1step;}
          if(Close[c] < iMAX0[c])
             {xhp1 = iMAX0[c] - Ph1step;}
          hp0[c] = xhp1;          // Phase compensate 180 degrees (So iMAX mode and hp mode agrees about up and down).
          hp1[c] = xhp0;
          b0 = hp0[c];b1 = hp0[c+1];r0 = hp1[c];r1 = hp1[c+1];} // Copy phase values to arrow and alert interface variables.

      if(EnableArrows)
         {if(b0 > r0 && b1 < r1)
             {CrossUp[c] = r0 - Clearance;}else{CrossUp[c] = EMPTY_VALUE;}
          if(b0 < r0 && b1 > r1)
             {CrossDn[c] = r0 + Clearance;}else{CrossDn[c] = EMPTY_VALUE;}}

     c--;
     } // while(c >= 0)

 if(EnableAlerts)
    {if(b0 > r0 && b1 < r1)
        {sendAlert(0,(StringConcatenate(getDateTime()," ",Symbol()," ",Chart," iMAX3alert signals up trend crossing.")));}
     if(b0 < r0 && b1 > r1)
        {sendAlert(1,(StringConcatenate(getDateTime()," ",Symbol()," ",Chart," iMAX3alert signals down trend crossing.")));}}

 return (0);  
} // int start()

/*+-------------------------------------------------------------------+
  | End iMAX3alert Main cycle                                         |
  +-------------------------------------------------------------------+*/


/*+-------------------------------------------------------------------+
  | iMAX3alert support functions                                      |
  +-------------------------------------------------------------------+*/

/*+-------------------------------------------------------------------+
  | *** Alert processing function                                     |
  +-------------------------------------------------------------------+*/

void sendAlert(int AlertNum,string AlertText)
{
 if(TimeCurrent() > AlertX[AlertNum] + AST)
    {AlertX[AlertNum] = TimeCurrent();
                                        /* Add a sound file here if you want another form of alert...
                                           The sound file must be located in the MT4 "sounds" file folder 
                                           and must be a .wav format file. Also remove the // characters before
                                           the PlaySound instruction.  The Alert command and semicolon can 
                                           be removed if the user wishes to remove the alert entirely, if favor 
                                           of a sound file... or it can be "commented out" just add // before 
                                           the alert instruction. */

//     PlaySound("YourFavoriteAlertSound.wav"); 
//     Print(AlertText);                // If using just the PlaySound alert print a copy of the alert message to the journal.
     Alert(AlertText);
    } // if(TimeCurrent() > AlertX[AlertNum] + AST)

 return(0);
} // void sendAlert(int AlertNum,string AlertText)

/*+-------------------------------------------------------------------+
  | *** Date/Time display function                                    |
  +-------------------------------------------------------------------+*/

string getDateTime()
{
 string dsplyDateTime = TimeToStr(TimeCurrent(),TIME_DATE|TIME_SECONDS);

 return(dsplyDateTime);
} // string getDateTime()

/*+-------------------------------------------------------------------+
  | *** Symbol check function                                         |
  +-------------------------------------------------------------------+*/

bool GoSym()
{
 bool Go = false;
 for(c = 0;c <= NumSym;c++)
     {if(Symbol() == SymList[c]){Go = true;}}
 return(Go);
}

/*+-------------------------------------------------------------------+
  | End iMAX3alert support functions                                  |
  +-------------------------------------------------------------------+*/


