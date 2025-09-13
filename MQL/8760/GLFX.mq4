//GLFX basic information:
//Optimalized for Metatrader 4, build 229
//+------------------------------------------------------------------+
#property copyright "Copyright © 2008 Globus"

// Constants used in the Signals section

#define Buy                +1
#define Sell               -1
#define None                0

// Constant used in T2_SendTrade for ordersend.

#define Failed             -1

//************* User Variables ******************//

extern string     COM_TMT              = "#-# Trade Settings";
extern int        TMT_MaxCntTrades_1account        = 5; //allow this amount of open trade at the same account and within all EAs
extern int        TMT_MaxCntTrades_1EA             = 1; //allow this amount of open trade at the same time and in the same EA
extern int        TMT_MaxInDirection              = 1; //allow this amount of open trade at the same direction and in the same EA
int CntBuyDirection; //system holds a count of long trades
int CntSellDirection;//system holds a count of short trades
int               TMT_LastOpenTime;                   //holds a open time of a last trade
extern int        TMT_TimeShiftOrder             =0; //amount of seconds must pass to allow opening a new trade after a last open trade, active when TMT_MaxCntTrades_1EA>1
extern int        TMT_SignalsRepeat            = 1; //to open new trade, wait for confirmation repeated TMT_SignalsRepeat *times
extern bool       TMT_SignalsReset       = true; //if true, open only in case of a explicit consequence signals without interrupt
extern string     TMT_Currency           = "EURUSD"; //Currency pair to be traded
extern string     TMT_Period                = "M15";  //TimeFrame, use the labels from the MT4 buttons
extern int        TMT_Slippage        =  10; // acceptable slippage
extern int        TMT_TP                 = 308;  //Take Profit in ticks, it can move higher if EMT_ExitWithTS_On =true;
extern int        TMT_SL                 = 290;  //Stop Loss in ticks
extern bool       TMT_TPfromSL_1On        =false; //count TP from SL as TP=SL+TMT_ADDtoSLforTP
extern int        TMT_ADDtoSLforTP       =18; // when f.e.=100 then TP=SL+100
double TMT_LastOpenDirection[3]; //holds type of the last still open trade(Buy or Sell) and its open price[1] and order ticket[2]
double TMT_LastLostDirection[3];//holds type of the last trade (Buy or Sell) closed with lost and its open price[1] and order ticket[2]

extern string     COM_MMT1               = "***********************************************************************";
extern string     COM_MMT2               = "#-# Money Management";
extern double     MMT_Lots                     = 0.1;      //use this LOT size, money management will override setting
extern double     MMT_MinLot                   = 0.01;	 //Set to be micro safe by default
extern double     MMT_MaxLot                   = 2.0;	 //Set to previous max value by default
extern bool       MMT_UseMManagement_On      = false; //Money management for lot sizing
extern double     MMT_MaxRisk                  = 0.05;     //Percentage of FreeMargin for trade
extern bool       MMT_DecreaseLots_1On       = false; //use decrease factor , when false then is automaticly set on 100% value
extern string     MMT_DecrLotsPerc           = "080050010";    //9 digit number, 3 digits per subsequent loss 080050010= 080% 050% 010%
int MMT_DecrFactor[3]={80,50,10}; //9 digit number, 3 digits per subsequent loss 080050010= 080% 050% 010%

extern string     COM_MMC1               = "***********************************************************************";
extern string     COM_MMC2               = "#-# Money Management- LOT confirmation signals";
int MMC_signalsRequired;     // calculated on init (a count of signals)
//if all these signals would be confirmed then LOT size will be multiplied by MMC_IncreseFactor
extern double     MMC_IncreseFactor       =2; //allows to increase lot size if signals are confirmed, increse calculated LOT size multiplied with this coeficient
extern bool       MMC_MA_HF_On            = false; //moving average on a higher frame, follows trend, uses SGE_MA_HF_On settings
extern bool       MMC_RSI_HF_On           = false; //RSI on a higher frame, uses SGE_RSI_HF_On settings

extern string     COM_SMT               = "#-# Set Management";
/*
Set management is used for the 2nd system optimalization, i.e. you have done the 1st optimalization for time period 2006-2010 and 
now you can prove all results taken from MT Optimalization Results within period i.e. 2002-2005 and you can find the best set
During the 2nd optimalization only SMT_NextSet will be changed and function SMT_ProveSets will read data from file Sets-Symbol-Period.csv saved in tester/files
*/

extern bool       SMT_ProveSets         = false; //you will use this function when you are looking for the second set from results of the first optimalization
extern int        SMT_NextSet           =0; //number of set readed from the file Sets-Symbol-Period.csv
extern int        SMT_SetCount=0;     //how many sets are within Optim file, if you leave =0 then PC finds alone but it takes more time


extern string     COM_EMS1              = "***********************************************************************";
extern string     COM_EMS2              = "#-# Exit Management stategies";
//these functions need to have at least one additional filter or signal
extern bool       EMT_DecreaseTP_On            =false; // allow to decrease Take Profit value if a counter trend is recognized
extern bool       EMT_ExitWithTS_On            =false; // enable static trailing stops
extern bool       EMT_RSI_HF_On                =false; // close order if a counter trend is recognized, used SGE_MultiCurr_1MA_On
extern bool       EMT_MA_HF_On                 =false; // close order if a counter trend is recognized, used SGE_RSI_HF_On
extern bool       EMT_CheckMainTrend           =false; // if a trade is going agains trend set trailing stop, using FTE_FolMainTrend_On
extern bool       EMT_RecrossClose             =false; // close a profit ticket in case of crossing open price x times
extern bool       EMT_TrendExtendTP_On         =false; // when there is a trend don´t allow to close order supported by this trend (then in case of closing a new order will be open in the same direction)
extern bool       EMT_ForceClose_On            =false; // force to close an order after certain amount of hours 
extern bool       EMT_OpenBarCorrelation_On    =false; // check currencies if they have the same movement when they open a new bar, follows trend
extern bool       EMT_CloseOnFriday_On         =false; // to close all open trades when it is Friday at certain time if there is no trend
extern bool       EMT_QM1_On                   =false; // check Quantum 
extern bool       EMT_TSI_On                   =false; // check Twitter sentiment indicator


//these functions don't need additional filters or signals
extern bool       EMT_move_SL           = false; //move SL with profit in steps and save profit
extern bool       EMT_move_TP           = false; //move TP when an order is in lost and save small profit
extern bool       EMT_CheckTempTrend    = false; //check temporary trend, if it appears, close contra trade orders if SL is close, using FTE_LocMainTrend_On


extern string     COM_EMS3              = "***********************************************************************";
extern string     COM_EMS4              = "#-# Exit Management filters settings ";
/*
function EXT_Confirm_trend(ticket,settings) explanation:
used in a connection with additional filters
if the function returns true, it means that the ticket doesn´t follow trend and it admits to continue closing or changing the ticket
if the function returns false, it means that the ticket follows trend and it is not adviced to close it, it doesn't continue and jump on another function
1st parameter - switch on (1) or switch off (0) using additional filters for the function (if 2nd parameter=0 then switch on (0) or switch off (1)) 
2nd parameter - if (1) and if all conditions are true then it returns true
              - if (1) and if all conditions are not true then it returns false
              - if (0) and if all conditions are true then it returns false
              - if (0) and if all conditions are not true then it returns true              
3rd parameter - sets an expectation that kind of market the ticket should follow - (1) follow a trend, (0) follow a counter trend
4th parameter - filters no trend markets, (1) filter on, (0) filter off
i.e. "1010" = filter is on, it returns 0 when all conditions are confirmed, function doesn't filter when there isn't a trend, it doesn't filter flat markets
*/ 
extern string     EMF_DecreaseTP_On             ="1111";
extern string     EMF_ExitWithTS_On             ="1111";
extern string     EMF_RSI_HF_On                 ="1111";
extern string     EMF_MA_HF_On                  ="1111";
extern string     EMF_CheckMainTrend            ="1111";
extern string     EMF_RecrossClose              ="1100";
extern string     EMF_TrendExtendTP_On          ="1010";
extern string     EMF_ForceClose_On             ="1110";
extern string     EMF_OpenBarCorrelation_On     ="0111";
extern string     EMF_CloseOnFriday_On          ="1111";
extern string     EMF_QM1_On                    ="1111";
extern string     EMF_TSI_On                    ="1110";


extern string     COM_EXF1                  = "**************************************************************************************";
extern string     COM_EXF2                  = "#-# Additional filters enabled EXIT STRATEGIES for closing order or for changing SL or TP";



/*
EMT_DecreaseTP_On           filters when trend or no trend, allows close or change an order when counter trend
EMT_ExitWithTS_On           filters when trend or no trend, allows close or change an order when counter trend
EMT_CheckMainTrend          filters when trend or no trend, allows close or change an order when counter trend   
EMT_TrendExtendTP_On        when trend then extend TP
*/



int EXS_signalsRequired;     // calculated on init (a count of signals)

            //These settings turn on/off different signals.Editing these settings is not necessary or recommended
            //  when GLFX is being optimized. New strategies require changes to the signals that are executed.
            //SIGNALS  
extern bool       EXS_FolMainTrend_On       = false; // follow main trend 
extern bool       EXS_LocMainTrend_On       = false; // locate main trend and allow to trade only in its  direction
extern bool       EXS_TemporaryTrend_On     = false; // find a temporary trend and block to trade agains it,uses ticks scalping
extern bool       EXS_RSI_HF_On             = false; // RSI on a higher timeframe, choose step forward, follows trend 
extern bool       EXS_MA_HF_On              = false; // MA on a higher timeframe, choose step forward, follows trend 
extern bool       EXS_WaitForPeaks_On       = false;// wait for a next peak and do not enter trades immediately
extern bool       EXS_OpenBarCorrelation_On = false; // check currencies if they have the same movement when they open a new bar, follows trend
extern bool       EXS_QM1_On                = false; // check Quantum
extern bool       EXS_TSI_On                = false; // check Twitter sentiment indicator

            //FILTERS
              

extern string     COM_EMT1              = "************************************************************************";
extern string     COM_EMT2              = "#-# Exit Management parameters";
extern int        EMT_DecTPShiftBar               =19; //count this function within this amount last bars
extern int        EMT_DecTPShiftPeriod            =0; //0=current period, 2=period about 2 degree higher (f.e. from M15 to H1)
extern int        EMT_TrendExtendTP_DISTtoTP      =400; //keep minimal this distance between actual price and TP when there is a trend in the same direction as the open order, uses also EMT_MoveTPonTS
extern int        EMT_TS_pipsDISTANCE           = 70; //If EMT_TS_pipsDISTANCE is enabled, use this pip value for TS
extern int        EMT_MoveTPonTS                =10; //increase Trailing stop by this value in pips each time TS is adjusted. Allows good trades to rise higher
extern bool       EMT_Count_TSOn               = false; //modify EMT_MoveTPonTS according to distance between TP and SL
extern int        EMT_Start_DelayTS             = 0; //pips to wait before activating trailing stop
extern int        EMT_RecrossMax               =15;  //0 disables setting. Close a profit ticket in case of crossing open price x times
extern double     EMT_RecrossCoefGood          =0.1; //close order only if it reached again  max reached price decreased about EMT_RecrossCoefGood %
extern double     EMT_RecrossCoefBad           =0.9; //close order only if it reached again  max reached price decreased about EMT_RecrossCoefBad %
extern double     EXF_ForceClose_Hours         = 0;  //force to close an order after certain amount of hours, 48 = 48 hours -force to exit
extern double     EXF_FridayTimeClose         = 19.55;  //force to close on Friday at 19h 55min

double EMT_bcRecross[1][7]; //holds a couple of last trade with parameters ticket Nr.,recross count,last recrossed bar,max reached price
int EMT_RecrossBars; // saves CPU
int EMT_minStop;   //Pulled from broker in init(), minimal TS value

int EMT_MSL_PF[4]                       ={60,70,80,90}; //move SL about 10% when 40% TP is reached
extern string     EMT_MSL_ProfitPerc = "060070080090";// as EMT_MSL_PF, but available in extern variables
int EMT_MSL_SL[4]                   ={30,40,50,60}; // when 40_ of TP  is gained then move SL about 10%
extern string     EMT_MSL_MoveSL = "030040050060";// as EMT_MSL_SL, but available in extern variables
int EMT_MTP_LF[4]                       ={60,70,80,90}; //move TP about 10% when 40% SL is reached
extern string     EMT_MTP_LostPerc = "060070080090";// as EMT_MTP_LF, but available in extern variables
int EMT_MTP_TP[4]                   ={30,40,50,60}; // when 40_ of SL  is gained then move TP about 10%
extern string     EMT_MTP_MoveTP = "030040050060";// as EMT_MTP_TP but available in extern variables


extern string     COM_SGE1                  = "***********************************************************************";
extern string     COM_SGE2                  = "#-# Signals and Filters Enabled for entry trade";
int SGE_signalsRequired;     // calculated on init (a count of signals)
            //These settings turn on/off different signals.Editing these settings is not necessary or recommended
            //  when GLFX is being optimized. New strategies require changes to the signals that are executed.
extern bool       SGE_RSI_HF_On             = false; // RSI on a higher timeframe, choose step forward, follows trend
extern bool       SGE_MA_HF_On              = false; // MA on a higher timeframe, choose step forward, follows trend
extern bool       SGE_OpenBarCorrelation_On = false; // check currencies if they have the same movement when they open a new bar, follows trend
extern bool       SGE_QM1_On                = false; // check Quantum
extern bool       SGE_TSI_On                = false; // check Twitter sentiment indicator


                           //FILTERS : Enabling these settings reduces the number of trades.
extern bool       FTE_Time_On                   = false;  //only trade during permitted hours
extern bool       FTE_FolMainTrend_On           = false; //follow main trend 
extern bool       FTE_LocMainTrend_On           = false; //locate main trend and allow to trade only in its  direction
extern bool       FTE_TemporaryTrend_On         = false; //find a temporary trend and block to trade agains it
extern bool       FTE_WaitForPeaks_On           = false;//wait for a next peak and do not enter trades immediately

extern string     COM_SGS0                  = "***********************************************************************";


extern string     COM_SGS12                  = "SGE_RSI_HF_On - settings";
extern int        SGS_RSI_HF_High                 = 65; //max value to confirm buy signal,but in case of condition RSI1>RSI2>RSI3
extern int        SGS_RSI_HF_Low                  = 25; //min value to confirm sell signal,but in case of condition RSI1<RSI2<RSI3
extern int        SGS_RSI_HF_Per                  = 57; //Number of periods for calculation
extern int        SGS_RSI_HF_TimeFrameShift       = 0; //Time Frame:0=current perion,1=1 step higher period
double SGS_RSI_HF_1, SGS_RSI_HF_2;   
int    SGS_RSI_HF_Bars;
int SGS_RSI_HF_Signal; 

extern string     COM_SGSMA_HF                  = "SGE_MA_HF_On - settings";
extern int        SGS_MA_HF_Period             = 60; //Averaging period for calculation, used for SGS_MC1MA_P1
extern int        SGS_MA_HF_TimeFrameShift     = 4; //Time Frame:0=current perion,1=1 step higher period, used for SGS_MC1MA_P1
extern int        SGS_MA_HF_Shift     = 0;       //MA shift. Indicators line offset relate to the chart by timeframe
double SGS_MA_HF_1, SGS_MA_HF_2;   
int    SGS_MA_HF_Bars;
int SGS_MA_HF_Signal; 

 
extern string     COM_SGSOBR1                 = "#-# SGE_OpenBarCorrelation_On"; // check currencies if they have the same movement when they open a new bar, follows trend
extern string     SGS_OBC1_P1                = "EURJPY"; //the 1st currency pair
extern string     SGS_OBC1_P2                = "USDCHF"; //the 2nd currency pair
extern string     SGS_OBC1_P3                = "GBPUSD"; //the 3rd currency pair
extern string     SGS_OBC1_P4                = "NZDUSD"; //the 4th currency pair
extern bool       SGS_OBC1_Confirm1          = true; //possitive or negative confirmation, used for SGS_OBC1_P1
extern bool       SGS_OBC1_Confirm2          = false; //possitive or negative confirmation, used for SGS_OBC1_P2
extern bool       SGS_OBC1_Confirm3          = true; //possitive or negative confirmation, used for SGS_OBC1_P3
extern bool       SGS_OBC1_Confirm4          = true; //possitive or negative confirmation, used for SGS_OBC1_P4
extern int        SGS_OBC1MaxSumConfirm      =4; //if 3 of 4 currencies correlate with the leading currency then SGS_OBC1MaxSumConfirm will be 1+1+1-1=2
extern int        SGS_OBC1MinSumConfirm      =2; //if 3 of 4 currencies correlate with the leading currency then SGS_OBC1MinSumConfirm will be 1+1+1-1=2
extern int        SGS_OBC1Period  = 7; //how deep it should control actual_move in history
extern int        SGS_OBC1History  =96; //how deep it should control sum_move in history
extern int        SGS_OBC1MinEntryDifference  =6; //minimal difference to entry a new trade
extern int        SGS_OBC1CloseStepsBefore  =0; //close an order x steps before it reaches its maximum
extern int        SGS_OBC1CloseStepsOpposite =2; //close an order x steps in opposite direction
extern int        SGS_OBC1TrendHistory =288; //check trend in this count of last bars

int SGS_OBC1Bars;
int SGS_OBC1Signal; //+1=buy, -1=sell, 0=none
//section only for optimalization
//  order                   0        1       2       3         4       5         6        7       8         9       10       11       12       13       14         15     16       17         18     19         20      21
string SGS_OBC1_Pairs[]={"USDCHF","USDJPY","GBPUSD","AUDUSD","USDCAD","EURGBP","EURCHF","EURJPY","GBPJPY","GBPCHF","EURCAD","EURAUD","CHFJPY","EURNZD","AUDJPY","AUDNZD","AUDCAD","AUDCHF","CADCHF","CADJPY","NZDUSD","NZDJPY"}; //all possible money pair, usable till 05/2007
extern bool       SGS_OBC1_OPTIMChoosePairs   = false; //run optimalization for all currencies pairs
extern int        SGS_OBC1_OP1                = 21; //the 1st currency pair
extern int        SGS_OBC1_OP2                = 20; //the 2nd currency pair
extern int        SGS_OBC1_OP3                = 10; //the 3rd currency pair
extern int        SGS_OBC1_OP4                = 0; //the 4th currency pair
extern int        SGS_OBC1_OConfirm1          = 0; //possitive or negative confirmation, used for SGS_OBC1_OP1
extern int        SGS_OBC1_OConfirm2          = 1; //possitive or negative confirmation, used for SGS_OBC1_OP2
extern int        SGS_OBC1_OConfirm3          = 0; //possitive or negative confirmation, used for SGS_OBC1_OP3
extern int        SGS_OBC1_OConfirm4          = 1; //possitive or negative confirmation, used for SGS_OBC1_OP4



extern string     COM_SGSQM1                 = "#-# SGE_OQM1_On"; // check Quantum
extern bool       SGS_QM1_ChangeOrientation = false; //if true then draw the curve with opposite orientation then price moving
extern int        SGS_QM1_CalculatingMethod = 0; //
extern int        SGS_QM1History  =20; //how deep it should control threeway in history
extern bool       SGS_QM1_ChangeWhenLost     =false; //if 3 consequence closed order finished in lost then change SGS_QM1_ChangeOrientation
int SGS_QM1_Bars;
int SGS_QM1_Signal; //+1=buy, -1=sell, 0=none


extern string     COM_SGSTSI          = "SGE_TSI_On - settings";
extern double     SGS_TSI_Distance   =   0.03; //distance from zero to allow open orders,values form sentiment
extern double     SGS_TSI_mood_max    =   5; //sent_mood<= -0.02, value among (-0.062,0.006)
extern int        SGS_TSI_MA_Period   =   12; //period for moving avarage
//sent_direction>=0.03 to open sell order, value among (-0.252,0.10133)
//sent_direction<= -0.03 to open buy order, value among (-0.252,0.10133)
// then average value is 0.17667 and difference between max and min value is 0.353333
int SGS_TSI_Bars; 
int SGS_TSI_Signal; //+1=buy, -1=sell, 0=none


extern string     C0M_FTS0                  = "***********************************************************************";

extern string        C0M_FTS2               = "#-# FTE_Time_On - settings";
/*
if FTE_Time_On is true then there is no possibility to open a new trade even if current time is within
defined time periods described lower. These conditions are sort in order how they are executed. For example, if current time is
within Sunday´s period then opening new trades are not prohibited and other periods will not be checked
Checking if a new trade is prohibited stops when a new is allowed.
*/
extern double        FTS_SunAt                     = 24;    //Sunday trading is disabled
extern double        FTS_SunTo                     = 24;
extern double        FTS_FriAt                     = 0;    //Friday trading ends early
extern double        FTS_FriTo                     = 13.50; // at Friday trade to 13:50 hour/minutes
extern double        FTS_MonAt                     = 13;    //Monday without Japanese Open
extern double        FTS_MonTo                     = 24;
extern double        FTS_WeekAt1                   = 0;    //Defaults will trader 24 hours Mon-Thu
extern double        FTS_WeekTo1                   = 24;   
extern double        FTS_WeekAt2                   = 24;
extern double        FTS_WeekTo2                   = 24;
//use FTS_NotTradeFrom if you find errors in history data and you want to skip its influence
extern datetime      FTS_NotTradeFrom1             =D'2010.07.23 00:00'; //do not open new order within selected time zone ; does not depend on setting FTE_Time_2On
extern datetime      FTS_NotTradeTo1               =D'2010.07.23 00:00'; //it is very useful during the second optimalization; does not depend on setting FTE_Time_2On
extern datetime      FTS_NotTradeFrom2             =D'2034.02.11 00:00'; //the second time filter, also useful within optimalization; does not depend on setting FTE_Time_2On
extern datetime      FTS_NotTradeTo2               =D'2034.02.11 00:00'; // does not depend on setting FTE_Time_2On


extern string     C0M_FTS4                = "#-# FTE_FolMainTrend_On - settings";
extern int        FTS_HorizDist              = 270; // amount bars checked in history to locate a trend 
double            FTS_SR_MaxPoint0,FTS_SR_MinPoint0,FTS_SR_MaxPoint1,FTS_SR_MinPoint1,FTS_SR_MaxPoint2,FTS_SR_MinPoint2;
int               FTS_SR_MaxPos0,FTS_SR_MinPos0,FTS_SR_MaxPos1,FTS_SR_MinPos1,FTS_SR_MaxPos2,FTS_SR_MinPos2,FTS_SR_barsCount;

extern string     C0M_FTS5                = "#-# FTE_LocMainTrend_On - settings";
extern int        FTS_Distance = 8; // specify how many bars in history this filter calculates
extern int        FTS_TimePerShift = 2; // counts within period times higher (if current period is M15 and FTS_TimePerShift=2, then H1)
extern double     FTS_MaxDiff = 1400; // difference in pips in total sum of bars
extern double     FTS_MinSlope=0.3; // slope between the first and the last calculated bar
double FTS_Slope,FTS_Difference,FTS_Bar1,FTS_Bar3;
int FTS_LastBarCnt;


extern string     COM_FTS6                  = "#-# FTE_TemporaryTrend_On - settings"; 
extern int        EXT_TC_SetSize   = 14; //remember last amount of ticks, amount = EXT_TC_SetSize
extern int        EXT_TC_Relevant  = 11; //if this number from amount of last ticks is in one direction than temporary trend is located
extern int        EXT_TC_PipsToClose  = 300; //if between current price and SL is diference <=EXT_TC_PipsToClose then sooner closing is allowed 
double EXT_TC_LastTick; //remember a last tick
int EXT_TC_Direction; //shows a direction of a discovered temporary trend
double EXT_TC_Store[]; //holds last ticks


extern string     C0M_FTS7                = "#-# FTE_WaitForPeaks_On - settings";
extern int        FTS_BarsBack=3; //count of bars where to find max and min price
extern int        FTS_BarsForward=2; //count of bars where the peak is valid
datetime FTS_TimeLimitLong; //system variable, controls a time limitation of the peak
datetime FTS_TimeLimitShort;//system variable, controls a time limitation of the peak
extern double FTS_PeakPerc=0.4; //wait for a peak in a certain distance from actual price given with difference between max and min price 
double FTS_PeakLong; //system variable,holds price of the peak for long trades
double FTS_PeakShort; //system variable,holds price of the peak for short trades
int FTS_PeaksBars; //holds count of bars in the current chart to save CPU



extern string     COM_RCS1             = "***********************************************************************";
extern string     COM_RCS2             = "#-# Record Settings";
extern bool       RCS_WriteLog                 = false; //display and write log event
extern bool       RCS_WriteDebug               = false; //display and write system informations
extern bool       RCS_SaveInFile               =false; //archive all logs into a file
int RCS_LogFilter[9]={1,1,1,1,1,1,1,1,1}; //filter messages what all should be written on printscreen or into a log file,1=true,0=false
//0.signals,1.filters,2.money management,3.open trade,4.all about sets,5.closing ticket,6.changing strategy,7.filters and signals for exit,8.common settings 
extern string     RCS_FilterLog = "111111111"; //as RCS_LogFilter, but available in extern variables
int RCS_DebugFilter[9]={0,0,1,1,1,1,1,1,1}; //filter messages what all should be written on printscreen or into a log file,1=true,0=false
extern string     RCS_FilterDebug = "001111111"; // //as RCS_DebugFilter, but available in extern variables
extern string     RCS_EAComment                = "101127"; //attached as trade comment 
extern string     RCS_NameEA     = "GLFX X";        // EA's name, under this name a tester looks for this EA
double RCS_LogFileSize=1000000; //The Log file will be devided into files with a given size


extern string     COM_GBV            = "#-# Global Variables";
extern int        GBV_MagicNumSet =    71; //must be unique when using more EA under one account
bool   GBV_GreenLight     =  true; //allow or forbid trading, controled by system
int    GBV_LogHandle        = -1; //number of log file
int    GBV_BuySignalCount; //system counts how many buy signals were fulfilled
int    GBV_SellSignalCount;//system counts how many sell signals were fulfilled
int    GBV_Ordercount_1EA;        //sum of open trades within this EA
int    GBV_Ordercount_1account;     //sum of open trades within one account, counts also trades open in different EAs
double GBV_currentTime; //One global variable saves thousands of CPU cycles.
extern bool       GBV_HideTestIndicators=false; //sets a flag hiding indicators called by the EA
int GBV_Bars; //holds count of bars to save CPU cycles


//----------------------  INIT  --------------------

void OnInit()
  { //Startup Functions, only called once.
    string filename,message;
    int handle;
    L1_OpenLogFile(RCS_NameEA);
    
    if (SMT_ProveSets)
      { 
        filename=StringConcatenate("Sets-",TMT_Currency,"-",TMT_Period,".csv");
        handle=FileOpen(filename,FILE_SHARE_READ,';'); 
        if(handle==-1)
         {
            message=StringConcatenate("SMT_ProveSets: File ",filename,"does not exist! Looking for the set can not start!");
            Alert(message);  
            if(RCS_WriteLog) L2_WriteLog(8,message);
            GBV_GreenLight=false;
         }   
        else   
         {
            FileClose(handle);  
            MM_VarFromFile();            
         }                                                 
       }
      
    
    if((Symbol() != TMT_Currency) && (Symbol() != TMT_Currency+"m"))
      {
        message=StringConcatenate("TMT_Currency: These settings are designed for ",TMT_Currency," only! Change chart or update TMT_Currency");
        Alert(message);  
        if(RCS_WriteLog) L2_WriteLog(8,message);
        GBV_GreenLight=false;
      }
      
    if(SGS_OBC1MaxSumConfirm<SGS_OBC1MinSumConfirm)  
      {
               message="SGS_OBC1MaxSumConfirm: The value must be greater then SGS_OBC1MinSumConfirm. System will set SGS_OBC1MinSumConfirm=SGS_OBC1MaxSumConfirm";
               Alert(message);
               SGS_OBC1MinSumConfirm=SGS_OBC1MaxSumConfirm;  
               if(RCS_WriteLog) L2_WriteLog(0,message);
      }
    

    //this function is only for the optimalization
    if(SGS_OBC1_OPTIMChoosePairs)
      {
         message=StringConcatenate("SGS_OBC1_OPTIMChoosePairs: List of previous money pairs: SGS_OBC1_P1:",SGS_OBC1_P1," ,SGS_OBC1_P2:",SGS_OBC1_P2," ,SGS_OBC1_P3:",SGS_OBC1_P3," ,SGS_OBC1_P4:",SGS_OBC1_P4);
         Alert(message);
         if(RCS_WriteLog) L2_WriteLog(6,message);
         message=StringConcatenate("SGS_OBC1_OPTIMChoosePairs: List of previous confirmations: SGS_OBC1_Confirm1:",SGS_OBC1_Confirm1," ,SGS_OBC1_Confirm2:",SGS_OBC1_Confirm2," ,SGS_OBC1_Confirm3:",SGS_OBC1_Confirm3," ,SGS_OBC1_Confirm4:",SGS_OBC1_Confirm4);
         Alert(message);
         if(RCS_WriteLog) L2_WriteLog(6,message);
        
         SGS_OBC1_P1=SGS_OBC1_Pairs[SGS_OBC1_OP1]; 
         SGS_OBC1_P2=SGS_OBC1_Pairs[SGS_OBC1_OP2]; 
         SGS_OBC1_P3=SGS_OBC1_Pairs[SGS_OBC1_OP3]; 
         SGS_OBC1_P4=SGS_OBC1_Pairs[SGS_OBC1_OP4]; 
         SGS_OBC1_Confirm1=SGS_OBC1_OConfirm1;
         SGS_OBC1_Confirm2=SGS_OBC1_OConfirm2; 
         SGS_OBC1_Confirm3=SGS_OBC1_OConfirm3; 
         SGS_OBC1_Confirm4=SGS_OBC1_OConfirm4; 
         
         message=StringConcatenate("SGS_OBC1_OPTIMChoosePairs: List of current money pairs: SGS_OBC1_P1:",SGS_OBC1_P1," ,SGS_OBC1_P2:",SGS_OBC1_P2," ,SGS_OBC1_P3:",SGS_OBC1_P3," ,SGS_OBC1_P4:",SGS_OBC1_P4);
         if(RCS_WriteLog) L2_WriteLog(6,message);
         message=StringConcatenate("SGS_OBC1_OPTIMChoosePairs: List of current confirmations: SGS_OBC1_Confirm1:",SGS_OBC1_Confirm1," ,SGS_OBC1_Confirm2:",SGS_OBC1_Confirm2," ,SGS_OBC1_Confirm3:",SGS_OBC1_Confirm3," ,SGS_OBC1_Confirm4:",SGS_OBC1_Confirm4);
         if(RCS_WriteLog) L2_WriteLog(6,message);
      }


    if(MMT_MaxLot >MarketInfo(Symbol(),MODE_MAXLOT)) 
      {
         MMT_MaxLot=MarketInfo(Symbol(),MODE_MAXLOT);
         if(RCS_WriteLog) L2_WriteLog(2,"MMT_MaxLot size was changed on "+MMT_MaxLot);
      }   
    if(MMT_MinLot <MarketInfo(Symbol(),MODE_MINLOT))
      {
         MMT_MinLot=MarketInfo(Symbol(),MODE_MINLOT);
         if(RCS_WriteLog) L2_WriteLog(2,"MMT_MinLot size was changed on "+MMT_MinLot);
      }   

    int PerArrayNr[]={1,5,15,30,60,240,1440,10080,43200};
    string PerArrayStr[]={"M1","M5","M15","M30","H1","H4","D1","W1","MN"};
    int PosPer=ArrayBsearch(PerArrayNr,Period(),WHOLE_ARRAY,0,MODE_ASCEND);
    if(PerArrayStr[PosPer]!=TMT_Period) 
      {
        message=StringConcatenate("TMT_Period: These settings are designed for ",TMT_Period," only! Change chart or update TMT_Period");
        Alert(message);  
        if(RCS_WriteLog) L2_WriteLog(8,message);
        GBV_GreenLight=false;
      }   


    //Initial Settings:
    
    SGE_signalsRequired=SGE_RSI_HF_On+SGE_MA_HF_On+SGE_OpenBarCorrelation_On+SGE_QM1_On+SGE_TSI_On; //all signals must be together

    EXS_signalsRequired=EXS_FolMainTrend_On+EXS_LocMainTrend_On+EXS_TemporaryTrend_On+EXS_RSI_HF_On+EXS_MA_HF_On+EXS_WaitForPeaks_On+EXS_OpenBarCorrelation_On+EXS_QM1_On+EXS_TSI_On; 
   
    if(RCS_WriteLog) L2_WriteLog(7,"EXS_signalsRequired: "+EXS_signalsRequired+" signals and filters are active.");
    if(EXS_signalsRequired==0) if(RCS_WriteLog) L2_WriteLog(7,"EXS_signalsRequired: Additional filters are inactive!");

    MMC_signalsRequired=MMC_MA_HF_On+MMC_RSI_HF_On;
    if(RCS_WriteLog) L2_WriteLog(2,"MMC_signalsRequired: "+MMC_signalsRequired+" signals and filters are active to increase LOT size.");
    if(MMC_signalsRequired==0) if(RCS_WriteLog) L2_WriteLog(2,"MMC_signalsRequired: Additional filters are inactive!");

  
    EMT_minStop=MarketInfo(Symbol(),MODE_STOPLEVEL);
      
    if(FTS_WeekAt1>FTS_WeekTo1) { Alert("FTS_WeekAt1>FTS_WeekTo1"); GBV_GreenLight = false; }
    if(FTS_WeekAt2>FTS_WeekTo2) { Alert("FTS_WeekAt2>FTS_WeekTo2"); GBV_GreenLight = false; }
    if(FTS_WeekTo1>FTS_WeekAt2) { Alert("FTS_WeekTo1>=FTS_WeekAt2"); GBV_GreenLight = false; }
    if(FTS_SunAt>FTS_SunTo) { Alert("FTS_SunAt>FTS_SunTo"); GBV_GreenLight = false; }
    if(FTS_FriAt>FTS_FriTo) { Alert("FTS_FriAt>FTS_FriTo"); GBV_GreenLight = false; }
    if(FTS_MonAt>FTS_MonTo) { Alert("FTS_MonAt>FTS_MonTo"); GBV_GreenLight = false; }
    
    if(GBV_HideTestIndicators) HideTestIndicators(true);
    else HideTestIndicators(false);
    
    ArrayResize(EXT_TC_Store,EXT_TC_SetSize);
    
    // gain arrays from strings obtained from extern variables  
    TextIntoArray (RCS_FilterLog,"RCS_FilterLog",RCS_LogFilter,1);
    TextIntoArray (RCS_FilterDebug,"RCS_FilterDebug",RCS_DebugFilter,1);   
    TextIntoArray (EMT_MSL_ProfitPerc,"EMT_MSL_ProfitPerc",EMT_MSL_PF,3);
    TextIntoArray (EMT_MSL_MoveSL,"EMT_MSL_MoveSL",EMT_MSL_SL,3);    
    TextIntoArray (MMT_DecrLotsPerc,"MMT_DecrLotsPerc",MMT_DecrFactor,3);


    if(MMT_DecreaseLots_1On)
      {
         if(MMT_DecrFactor[0]<MMT_DecrFactor[1] || MMT_DecrFactor[1]<MMT_DecrFactor[2])
            {
               message="MMT_DecrLotsPerc : Usually values should be sorted from max to min !";
               Alert(message);  
               if(RCS_WriteLog) L2_WriteLog(2,message);
            }
      }
      
 
    
    //if(!MMT_DecreaseLots_1On) for(cnt=0;cnt<3;cnt++) MMT_DecrFactor[cnt]=100; //same chance for all trade, use it specially with auto-optimalization
   
    if(TMT_TPfromSL_1On) TMT_TP=TMT_SL+TMT_ADDtoSLforTP; //count with different TP then in startup
    
// system settings and account info   
   if(RCS_WriteLog)
    { 
      L2_WriteLog(8,"Account credit "+AccountCredit());    
      L2_WriteLog(8,"Account #"+AccountNumber()+" leverage is "+AccountLeverage());    
      L2_WriteLog(8,"Account currency is "+AccountCurrency());    
      L2_WriteLog(8,"Point size in the quote currency:"+MarketInfo(Symbol(),MODE_POINT));    
      L2_WriteLog(8,"Count of digits after decimal point in the symbol prices:"+MarketInfo(Symbol(),MODE_DIGITS));    
      L2_WriteLog(8,"Spread value in points:"+MarketInfo(Symbol(),MODE_SPREAD));    
      L2_WriteLog(8,"Stop level in points:"+MarketInfo(Symbol(),MODE_STOPLEVEL));    
      L2_WriteLog(8,"Lot size in the base currency:"+MarketInfo(Symbol(),MODE_LOTSIZE));    
      L2_WriteLog(8,"Tick value in the deposit currency:"+MarketInfo(Symbol(),MODE_TICKVALUE));    
      L2_WriteLog(8,"Tick size in points:"+MarketInfo(Symbol(),MODE_TICKSIZE));    
      L2_WriteLog(8,"Swap of the long position:"+MarketInfo(Symbol(),MODE_SWAPLONG));    
      L2_WriteLog(8,"Swap of the short position:"+MarketInfo(Symbol(),MODE_SWAPSHORT));    
      L2_WriteLog(8,"Minimum permitted amount of a lot:"+MarketInfo(Symbol(),MODE_MINLOT));    
      L2_WriteLog(8,"Step for changing lots:"+MarketInfo(Symbol(),MODE_LOTSTEP));    
      L2_WriteLog(8,"Maximum permitted amount of a lot:"+MarketInfo(Symbol(),MODE_MAXLOT));    
      L2_WriteLog(8,"Free margin required to open 1 lot for buying:"+MarketInfo(Symbol(),MODE_MARGINREQUIRED));    
      L2_WriteLog(8,"Order freeze level in points. If the execution price lies within the range defined by the freeze level, the order cannot be modified, cancelled or closed:"+MarketInfo(Symbol(),MODE_FREEZELEVEL));    
    }
  if(RCS_SaveInFile) FileFlush(GBV_LogHandle);
 
  
}

//---- DEINITIACION
int deinit()
  {
   HideTestIndicators(false);
   return(0);
  }  

//----------------------  START  --------------------

void start()
  {
    if(GBV_GreenLight == false) return;
    
    GBV_currentTime=TimeHour(TimeCurrent())+TimeMinute(TimeCurrent())*0.01;    //one global variable saves thousands of CPU cycles
    
    L1_OpenLogFile(RCS_NameEA);
    GBV_Ordercount_1EA=0;
    GBV_Ordercount_1account=0;
    CntBuyDirection=0;
    CntSellDirection=0;

    if(EMT_CheckTempTrend || FTE_TemporaryTrend_On) TickContainer(); //check a temporary trend
    
    if( ArrayRange(EMT_bcRecross,0) < TMT_MaxCntTrades_1EA) ArrayResize(EMT_bcRecross,TMT_MaxCntTrades_1EA); //must be checked because TMT_MaxCntTrades_1EA should change by a new set

    if(GBV_Bars<Bars) //This should be based on bars, reduces check to once every bar.
      {  
         GBV_Bars=Bars;
         EMT_minStop=MarketInfo(Symbol(),MODE_STOPLEVEL);       
      }


    //*************** Detect open trades *********************//
    for (int cticket = 0; cticket < OrdersTotal(); cticket++) 
      {
       if (OrderSelect(cticket, SELECT_BY_POS) == false)
         continue;
       GBV_Ordercount_1account++;  

       if (OrderSymbol() != Symbol())
         continue;

       if (OrderMagicNumber() == GBV_MagicNumSet)
         {
           if(OrderOpenTime()>TMT_LastOpenDirection[1]) //holds type of the last open trade 
            {
               TMT_LastOpenDirection[1]=OrderOpenTime();
               TMT_LastOpenDirection[2]=OrderTicket();
               if(OrderType()==OP_BUY) TMT_LastOpenDirection[0]=Buy;
               if(OrderType()==OP_SELL) TMT_LastOpenDirection[0]=Sell; 
            }                 
               
           X1_ManageExit(OrderTicket());
           GBV_Ordercount_1EA++;
           if(OrderType()==OP_BUY) CntBuyDirection++; //count how many long trades are opened
           if(OrderType()==OP_SELL) CntSellDirection++;  //count how many short trades are opened     
         }
      }
      
    if (TimeCurrent()>FTS_NotTradeFrom1 && TimeCurrent()<FTS_NotTradeTo1) return; //do not open new order within selected time zone
    if (TimeCurrent()>FTS_NotTradeFrom2 && TimeCurrent()<FTS_NotTradeTo2) return; //the second time filter

    if(GBV_Ordercount_1account>TMT_MaxCntTrades_1account)
      {
          if(RCS_WriteDebug) L3_WriteDebug(0,"TMT_MaxCntTrades_1account: Max sum of all open orders is reached! It is not possible to open a new order.");
      }
    else if ( GBV_Ordercount_1EA < TMT_MaxCntTrades_1EA) //check if it is allowed to open new trades
           {
             if(GBV_Ordercount_1EA==0) A1_OpenTrade_If_Signal();
             else if(TMT_LastOpenTime+TMT_TimeShiftOrder<TimeCurrent() && GBV_Ordercount_1EA>0) A1_OpenTrade_If_Signal();
           } 
    if(RCS_SaveInFile) FileFlush(GBV_LogHandle);
    
  } //End Start() 



//********************* Check for New Trade *******************//

void A1_OpenTrade_If_Signal()
  { 
    bool enableBuy= true;
    bool enableSell=true;
    double stoplevel=MarketInfo(Symbol(), MODE_STOPLEVEL); 
    double spread=MarketInfo(Symbol(), MODE_SPREAD); 
    
    if(stoplevel>(spread+TMT_TP))
      {
         if(RCS_WriteLog) L2_WriteLog(3,"A1_OpenTrade_If_Signal: Stoplevel ["+stoplevel+"] or Spread ["+spread+"] are very high or Take profit ["+TMT_TP+"] is very low ");
         return;
      }   
    if((stoplevel+spread)>TMT_SL)
      {
         if(RCS_WriteLog) L2_WriteLog(3,"A1_OpenTrade_If_Signal: Stoplevel ["+stoplevel+"] or Spread ["+spread+"] are very high or Stoploss ["+TMT_SL+"] is very low ");
         return;
      }  

    int TradeDirect=A2_Check_If_Signal(TMT_SignalsRepeat,GBV_BuySignalCount,GBV_SellSignalCount);   
     // If the GBV_BuySignalCount or GBV_SellSignalCount exceeds the TMT_SignalsRepeat required
     // then the Buy order or Sell order will be submitted.
     
    if(TradeDirect==0) return; //no signals to open a trade
    if (FTE_WaitForPeaks_On) 
      {
         FTS_ControlPeaks(TradeDirect);
         Z_F8_BlockTradingFilter8(enableBuy,enableSell);
      }   
      
       
    
    if(TradeDirect==1 && CntBuyDirection<TMT_MaxInDirection)
      {
        if(!enableBuy) return;
        if( A1_1_OrderNewBuy(MM_OptimizeLotSize(Buy)) != Failed)
         {
          GBV_BuySignalCount=0;
         } 
      }
    else if(TradeDirect==-1 && CntSellDirection<TMT_MaxInDirection)
      {
        if(!enableSell) return;
        if( A1_2_OrderNewSell(MM_OptimizeLotSize(Sell)) != Failed)
         {
          GBV_SellSignalCount=0;
         } 
      }
  }




int A2_Check_If_Signal(int ConsSignal,int& BuySigCnt,int& SellSigCnt)
  { //Check singals and conditions to enter new trade
    int  signalCount;
    bool enableBuy= true;
    bool enableSell=true;

//EntryFilters enable or disable trading, while EntrySignals generate "buy" or "sell"
//EntryFilter should return "false" if trading is still enabled.    

    if (FTE_Time_On) 
      if (Filter_Time()) return(0);

    if (FTE_FolMainTrend_On) 
      if (Z_F5_BlockTradingFilter5(enableBuy,enableSell)) return(0);
 
    if (FTE_LocMainTrend_On)
      if (Z_F6_BlockTradingFilter6(enableBuy,enableSell)) return(0);      

    if (FTE_TemporaryTrend_On) 
      if (Z_F7_BlockTradingFilter7(enableBuy,enableSell)) return(0);

    if (FTE_WaitForPeaks_On) Z_F8_BlockTradingFilter8(enableBuy,enableSell);      


    //Check all the EntrySignals for Buy=1 or Sell=-1 values. See constants at top of file.

    if (SGE_RSI_HF_On)
      {
        signalCount += EntrySignal_RSIHF();    
        if(RCS_WriteDebug) L3_WriteDebug(0,"SGE_RSI_HF_On: Signal sum: "+signalCount);
      }     
    if (SGE_MA_HF_On)
      {
        signalCount += EntrySignal_MAHF();    
        if(RCS_WriteDebug) L3_WriteDebug(0,"SGE_MA_HF_On: Signal sum: "+signalCount);
      }     
      
    if (SGE_OpenBarCorrelation_On)
      {
        signalCount += EntrySignal_OBC1();    
        if(RCS_WriteDebug) L3_WriteDebug(0,"SGE_OpenBarCorrelation_On: Signal sum: "+signalCount);
      } 
       
    if (SGE_QM1_On)
      {
        signalCount += EntrySignal_QM1();    
        if(RCS_WriteDebug) L3_WriteDebug(0,"SGE_QM1_On: Signal sum: "+signalCount);
      } 
       
    if (SGE_TSI_On)
      {
        signalCount += EntrySignal_TSI();    
        if(RCS_WriteDebug) L3_WriteDebug(0,"SGE_TSI_On: Signal sum: "+signalCount);
      }        
          
       
    // Counting up the number of buy or sell signals that happen consecutively.  
    if(enableBuy)
      if((signalCount >= SGE_signalsRequired)&& SGE_signalsRequired>0) 
        { //Check for Buy
          BuySigCnt++;
          SellSigCnt=0; 
        }   
    if(enableSell)
      if((signalCount <= (-1)*SGE_signalsRequired)&& SGE_signalsRequired>0) 
        {
          BuySigCnt=0;
          SellSigCnt++;
        }
    if(TMT_SignalsReset)
       
       if((signalCount<0 && signalCount>(-1)*SGE_signalsRequired) || (signalCount>=0 && signalCount < SGE_signalsRequired))
         {//If neither buy nor sell signal is received
           BuySigCnt =0;
           SellSigCnt=0;
         }
    if(RCS_WriteDebug) L3_WriteDebug(0,"TMT_SignalsReset: signal#:"+signalCount+" ConsSignal:" +ConsSignal+" BuySigCnt:" +BuySigCnt+" SellSigCnt:"+ SellSigCnt);
     
     // If the BuySigCnt or SellSigCnt exceeds the ConsSignal required
     // then the Buy order or Sell order shoul be entered
    if(ConsSignal <= BuySigCnt)return(1); // possibility to open buy order
    else if(ConsSignal <= SellSigCnt)return(-1);// possibility to open sell order
    return(0); //no possibility
  }

 
int A1_1_OrderNewBuy(double lots)  //Trade TP+SL Signals
  { 
    if(!EXS_CheckExit_stategy(OP_BUY)) return(0);
    int ticket=T2_SendTrade(TMT_TP, TMT_SL, lots, OP_BUY);
    return(ticket);
  }

int A1_2_OrderNewSell(double lots)   //Trade TP+SL Signals
  { 
    if(!EXS_CheckExit_stategy(OP_SELL)) return(0);
    int ticket=T2_SendTrade(TMT_TP, TMT_SL, lots, OP_SELL);
    return(ticket);    
  }


bool A1_4_IsTradePossible()
  {
    if(IsTradeAllowed() || !IsTradeContextBusy()) return(true);
    else return(false);
  }  

// check if a new order will not be closed by exit stategies and filters

bool EXS_CheckExit_stategy(int order)
   {
      bool enableBuy=true;
      bool enableSell=true;
      int signal;
        
          
      if(EMT_DecreaseTP_On) 
            if(Z_F6_BlockTradingFilter6(enableBuy,enableSell))
               if(RCS_WriteDebug) L3_WriteDebug(7,"EXS_CheckExit_stategy/Z_F6_BlockTradingFilter6: blocking recognized!");
     
      if(EMT_RSI_HF_On)
            {
               signal=EntrySignal_RSIHF(); 
               if(signal==Buy && order==OP_SELL)
                  {
                     if(RCS_WriteDebug) L3_WriteDebug(7,"EXS_CheckExit_stategy/EntrySignal_RSIHF: block sell order!");
                     return(false);                  
                  }
               if(signal==Sell && order==OP_BUY)
                  {
                     if(RCS_WriteDebug) L3_WriteDebug(7,"EXS_CheckExit_stategy/EntrySignal_RSIHF: block buy order!");
                     return(false);                  
                  }
             }   
               
      if(EMT_MA_HF_On)
            {
               signal=EntrySignal_MAHF(); 
               if(signal==Buy && order==OP_SELL)
                  {
                     if(RCS_WriteDebug) L3_WriteDebug(7,"EXS_CheckExit_stategy/EntrySignal_MAHF: block sell order!");
                     return(false);                  
                  }
               if(signal==Sell && order==OP_BUY)
                  {
                     if(RCS_WriteDebug) L3_WriteDebug(7,"EXS_CheckExit_stategy/EntrySignal_MAHF: block buy order!");
                     return(false);                  
                  }
             }     
      
  
      if(EMT_OpenBarCorrelation_On)
            {
               signal=EntrySignal_OBC1(); 
               if(signal==Buy && order==OP_SELL)
                  {
                     if(RCS_WriteDebug) L3_WriteDebug(7,"EXS_CheckExit_stategy/EntrySignal_OBC1: block sell order!");
                     return(false);                  
                  }
               if(signal==Sell && order==OP_BUY)
                  {
                     if(RCS_WriteDebug) L3_WriteDebug(7,"EXS_CheckExit_stategy/EntrySignal_OBC1: block buy order!");
                     return(false);                  
                  }
             }   

      if(EMT_QM1_On)
            {
               signal=EntrySignal_QM1(); 
               if(signal==Buy && order==OP_SELL)
                  {
                     if(RCS_WriteDebug) L3_WriteDebug(7,"EXS_CheckExit_stategy/EntrySignal_QM1: block sell order!");
                     return(false);                  
                  }
               if(signal==Sell && order==OP_BUY)
                  {
                     if(RCS_WriteDebug) L3_WriteDebug(7,"EXS_CheckExit_stategy/EntrySignal_QM1: block buy order!");
                     return(false);                  
                  }
             }       
             
      if(EMT_TSI_On)
            {
               signal=EntrySignal_TSI(); 
               if(signal==Buy && order==OP_SELL)
                  {
                     if(RCS_WriteDebug) L3_WriteDebug(7,"EXS_CheckExit_stategy/EntrySignal_TSI: block sell order!");
                     return(false);                  
                  }
               if(signal==Sell && order==OP_BUY)
                  {
                     if(RCS_WriteDebug) L3_WriteDebug(7,"EXS_CheckExit_stategy/EntrySignal_TSI: block buy order!");
                     return(false);                  
                  }
             }                    
                          
          
       if(!enableBuy && order==OP_BUY)
         {
            if(RCS_WriteDebug) L3_WriteDebug(7,"EXS_CheckExit_stategy: block buy order!");
            return(false);                  
         }
       if(!enableSell && order==OP_SELL)
         {
            if(RCS_WriteDebug) L3_WriteDebug(7,"EXS_CheckExit_stategy: block sell order!");
            return(false);                  
         }   
       return(true);
   }





//************* exit management
int EXT_Check_If_Signal(int ConsSignal,bool& enableBuy, bool& enableSell)
  { //Check signals and conditions to enter new trade
    int  signalCount;
    enableBuy= true;
    enableSell=true;
    bool callfilter;
    double direction;


//EntryFilters enable or disable trading, while Exit signals generate "buy" or "sell"
//Exit Filter should return "false" if trading is still enabled.    

//FILTERS 


//SIGNALS
    if (EXS_FolMainTrend_On)
      {
         callfilter=Z_F5_BlockTradingFilter5(enableBuy,enableSell);
         if(enableBuy) signalCount +=1;
         if(enableSell) signalCount +=-1;
         enableBuy=true;
         enableSell=true;
      }   
 
    if (EXS_LocMainTrend_On)
      {
         callfilter=Z_F6_BlockTradingFilter6(enableBuy,enableSell);
         if(enableBuy) signalCount +=1;
         if(enableSell) signalCount +=-1;
         enableBuy=true;
         enableSell=true;
      }   
    
    if (EXS_TemporaryTrend_On)
      {
         callfilter=Z_F7_BlockTradingFilter7(enableBuy,enableSell);
         if(enableBuy) signalCount +=1;
         if(enableSell) signalCount +=-1;
         enableBuy=true;
         enableSell=true;
      }   
      
    if (EXS_WaitForPeaks_On)
      {
         FTS_ControlPeaks(Buy);
         FTS_ControlPeaks(Sell);         
         callfilter=Z_F8_BlockTradingFilter8(enableBuy,enableSell);
         if(enableBuy) signalCount +=1;
         if(enableSell) signalCount +=-1;
         enableBuy=true;
         enableSell=true;
      }   

    
    //Check all the EntrySignals for Buy=1 or Sell=-1 values. See constants at top of file.

    if (EXS_RSI_HF_On)
      {
        signalCount += EntrySignal_RSIHF();    
        if(RCS_WriteDebug) L3_WriteDebug(0,"EXS_RSI_HF_On: Signal sum: "+signalCount);
      }  
    if (EXS_MA_HF_On)
      {
        signalCount += EntrySignal_MAHF();    
        if(RCS_WriteDebug) L3_WriteDebug(0,"EXS_MA_HF_On: Signal sum: "+signalCount);
      }  
     
     if (EXS_OpenBarCorrelation_On)
      {
        signalCount += EntrySignal_OBC1();    
        if(RCS_WriteDebug) L3_WriteDebug(0,"EXS_OpenBarCorrelation_On: Signal sum: "+signalCount);
      }  
      
     if (EXS_QM1_On)
      {
        signalCount += EntrySignal_QM1();    
        if(RCS_WriteDebug) L3_WriteDebug(0,"EXS_QM1_On: Signal sum: "+signalCount);
      }  
      
     if (EXS_TSI_On) //if Twitter shows an opposite trend, stop open trades
      {
        signalCount += EntrySignal_TSI();  
        direction = iCustom(TMT_Currency,0,"TSI",SGS_TSI_Distance,SGS_TSI_mood_max,SGS_TSI_MA_Period,4,0);
        if(direction < SGS_TSI_Distance) enableSell=false;
        if(direction > SGS_TSI_Distance*(-1))  enableBuy=false;      
        if(RCS_WriteDebug) L3_WriteDebug(0,"EXS_TSI_On: Signal sum: "+signalCount);
      }      
       
    // Counting up the number of buy or sell signals that happen consecutively.  
    if(RCS_WriteDebug) L3_WriteDebug(7,"EXT_Check_If_Signal: signal#:"+signalCount+" ConsSignal:" +ConsSignal);
   
      // If the BuySigCnt or SellSigCnt exceeds the ConsSignal required
     // then the Buy order or Sell order should be closed   
    if((signalCount >= EXS_signalsRequired) && enableBuy) return(1); // possibility to keep buy order
    if((signalCount <= (-1)*EXS_signalsRequired) && enableSell) return(-1);// possibility to keep sell order

    return(0); //no possibility
  }

//************* LOT confirmation signals
int MMC_Check_If_Signal(int ConsSignal)
  { //Check signals and conditions to increse LOT size
    int  signalCount;
    bool enableBuy= true;
    bool enableSell=true;


//EntryFilters enable or disable LOT incresing, while Exit signals generate "buy" or "sell"
//Exit Filter should return "false" if LOT incresing is still enabled.    

    if (MMC_RSI_HF_On)
      {
        signalCount += EntrySignal_RSIHF();    
        if(RCS_WriteDebug) L3_WriteDebug(2,"MMC_RSI_HF_On: Signal sum: "+signalCount);
      }     
    if (MMC_MA_HF_On)
      {
        signalCount += EntrySignal_MAHF();    
        if(RCS_WriteDebug) L3_WriteDebug(2,"MMC_MA_HF_On: Signal sum: "+signalCount);
      }     
    
       
    // Counting up the number of buy or sell signals that happen consecutively.  
    if(RCS_WriteDebug) L3_WriteDebug(7,"MMC_Check_If_Signal: signal#:"+signalCount+" ConsSignal:" +ConsSignal);
   
      // If the BuySigCnt or SellSigCnt exceeds the ConsSignal required
     // then the Buy order or Sell order should be closed  
    if(MMC_signalsRequired==0) return(0);
    if(signalCount >= MMC_signalsRequired) return(1); // possibility to increase LOT size by BUY order
    if(signalCount <= (-1)*MMC_signalsRequired) return(-1);// possibility to increase LOT size by SELL order

    return(0); //no possibility
  }

// check if it is possible to keep an order opened

bool EXT_Confirm_trend(int ticket,string settings ) 
   {
/*
function EXT_Confirm_trend(ticket,settings) explanation:
used in a connection with additional filters
if the function returns true, it means that the ticket doesn´t follow trend and it admits to continue closing or changing the ticket
if the function returns false, it means that the ticket follows trend and it is not adviced to close it, it doesn't continue and jump on another function
1st parameter - switch on (1) or switch off (0) using additional filters for the function (if 2nd parameter=0 then switch on (0) or switch off (1)) 
2nd parameter - if (1) and if all conditions are true then it returns true
              - if (1) and if all conditions are not true then it returns false
              - if (0) and if all conditions are true then it returns false
              - if (0) and if all conditions are not true then it returns true              
3rd parameter - sets an expectation that kind of market the ticket should follow - (1) follow a trend, (0) follow a counter trend
4th parameter - filters no trend markets, (1) filter on, (0) filter off
i.e. "1010" = filter is on, it returns 0 when all conditions are confirmed, function doesn't filter when there isn't a trend, it doesn't filter flat markets
*/ 
  
    int paramfunc[4]; 
    bool confirmtrue,confirmfalse,FollowTrendOn,Keep_NO_trend_On;  
    settings=TextIntoArray (settings,"settings",paramfunc,1); 
    bool enableBuy,enableSell;
    
    confirmtrue=paramfunc[1];
    if(confirmtrue==0) confirmfalse=1;
    else confirmfalse=0;
    if(paramfunc[0]==0) return(confirmtrue); //filters isn't active and return without filtering
    if(EXS_signalsRequired==0) return(confirmtrue); // filters were not applied so it has to return without filtering
    FollowTrendOn=paramfunc[2];
    Keep_NO_trend_On=paramfunc[3];
        
    int TradeDirect=EXT_Check_If_Signal(EXS_signalsRequired,enableBuy,enableSell);  
     // If the GBV_BuySignalCount or GBV_SellSignalCount exceeds the TMT_SignalsRepeat required
     // then the Buy order or Sell order will be submitted.


    if(RCS_WriteDebug) L3_WriteDebug(7,"EXT_Confirm_trend: Ticket#:"+ticket+", FollowTrendOn:"+FollowTrendOn+", Keep_NO_trend_On :"+Keep_NO_trend_On +", TradeDirect:"+TradeDirect+", EXS_signalsRequired:"+EXS_signalsRequired+", enableBuy:"+enableBuy+", enableSell:"+enableSell);
     
    if(!enableSell && OrderType()==OP_BUY) 
      {
            if(RCS_WriteDebug) L3_WriteDebug(7,"EXT_Confirm_trend: Ticket#:"+ticket+" - filter does not allow to change buy orders");
            return(confirmfalse); //buy order is not possible to close 
      }   
    if(!enableBuy && OrderType()==OP_SELL) 
      {
            if(RCS_WriteDebug) L3_WriteDebug(7,"EXT_Confirm_trend: Ticket#:"+ticket+" - filter does not allow to change sell orders");
            return(confirmfalse); //sell order is not possible to close 
      }   
    
    if(TradeDirect==0)
       if(Keep_NO_trend_On) return(confirmfalse); //no signals to change or close the order
       else 
         {
            if(RCS_WriteDebug) L3_WriteDebug(7,"EXT_Confirm_trend: Ticket#:"+ticket+" - no trend - no filtering, possible to change or close");
            return(confirmtrue); //no trend, possible to change or close
         }   
    
    if(TradeDirect==1 && OrderType()==OP_BUY)
      {
         if(FollowTrendOn) 
            {
               if(RCS_WriteDebug) L3_WriteDebug(7,"EXT_Confirm_trend: Ticket#:"+ticket+" - long trend is confirmed - no change");
               return(confirmfalse); //long trend is confirmed
            }   
         else 
            {
               if(RCS_WriteDebug) L3_WriteDebug(7,"EXT_Confirm_trend: Ticket#:"+ticket+" - long trend was found but the order will be changed or closed");
               return(confirmtrue); //long trend was found but the order will be changed or closed
            }
      }   
  
    else if(TradeDirect==-1 && OrderType()==OP_SELL)
      {
         if(FollowTrendOn) 
            {
               if(RCS_WriteDebug) L3_WriteDebug(7,"EXT_Confirm_trend: Ticket#:"+ticket+" - short trend is confirmed - no change");
               return(confirmfalse); //short trend is confirmed
            }   
         else 
            {
               if(RCS_WriteDebug) L3_WriteDebug(7,"EXT_Confirm_trend: Ticket#:"+ticket+" - short trend was found but the order will be changed or closed");
               return(confirmtrue); //short trend was found but the order will be changed or closed
            }
      }   
 
    if(RCS_WriteDebug) L3_WriteDebug(7,"EXT_Confirm_trend: Ticket#:"+ticket+" can be changed or closed.");
    return(confirmtrue); //the order will be changed or closed
   }



//********************* Money Management *****************//

double MM_OptimizeLotSize(int direction)
  {
    double lots         =MMT_Lots;
    int    orders       =OrdersHistoryTotal();
    int    i            =orders-1;
    int    trades       =0;
    double    wins         =0;
    double    losses       =0;
    double lotStep=MarketInfo(Symbol(),MODE_LOTSTEP);   //Step in volume changing 
    int   MMC_signal;
    bool FirstLost=false;
    double FreeMargin =AccountFreeMargin();         // Free margin
    double One_Lot=MarketInfo(Symbol(),MODE_MARGINREQUIRED);//!-lot cost
    double tickvalue = MarketInfo( Symbol(), MODE_TICKVALUE );    
    double Lots1, Lots2;
    
    if(MMT_UseMManagement_On) 
      {
         // limit according to stop-loss risk
         Lots1 = (FreeMargin*MMT_MaxRisk)/(TMT_SL*tickvalue);

         // limit according to margin requirement
         Lots2 = FreeMargin/One_Lot;  // given margin / margin cost of one lot
   
         // the lower of the two limits
         if(RCS_WriteDebug) L3_WriteDebug(2,"MMT_UseMManagement_On: LOT size limits - limit according to stop-loss risk:"+Lots1+" limit according to margin requirement:"+Lots2) ;                     
         lots = MathMin(Lots1,Lots2);
   
       // if too small 
         if( lots < MMT_MinLot )
            if( Lots1 < Lots2 ) if(RCS_WriteLog) L2_WriteLog(2,"MMT_UseMManagement_On: Lot size was decreased due to stop distance");
            else if(RCS_WriteLog) L2_WriteLog(2,"MMT_UseMManagement_On: Lot size was decreased due to margin");
      }   
    else
      {
         lots=MMT_Lots;
         if(lots*One_Lot>FreeMargin)      // If free margin is not enough..
           lots=MathFloor(FreeMargin/One_Lot/lotStep)*lotStep;// Calculate lots
      }   
    
    if(RCS_WriteLog) L2_WriteLog(2,"MM_OptimizeLotSize: 1st calculation for LOT size is "+lots);
  
    //lot size increasing 
    if(MMC_signalsRequired>0)
      {
         MMC_signal=MMC_Check_If_Signal(MMC_signalsRequired);
         if(direction==Buy && MMC_signal==1) lots*=MMC_IncreseFactor;
         if(direction==Sell && MMC_signal==-1) lots*=MMC_IncreseFactor;
         if(RCS_WriteLog) L2_WriteLog(2,"MM_OptimizeLotSize: After LOT increasing: "+lots);
      }   
        
    while (i > 0 || (!FirstLost && trades<3))
      {
            if(OrderSelect(i,SELECT_BY_POS,MODE_HISTORY)==false)
              {
                if(RCS_WriteLog) L2_WriteLog(2,"MMT_DecrLotsPerc: Decrease factor noticed error in history!");
                break;
              }
            if(OrderSymbol()==Symbol() && OrderType()<=OP_SELL && OrderMagicNumber()==GBV_MagicNumSet)
              {
                if(OrderProfit()<0 && trades<3) losses++;
                if(OrderProfit()>0 && trades<3) wins++;
                trades++;   
                if(OrderProfit()<0 && TMT_LastLostDirection[1]<=OrderCloseTime())
                  {
                     TMT_LastLostDirection[1]=OrderCloseTime(); //holds the last lossy trade
                     TMT_LastLostDirection[2]=OrderTicket();
                     if(OrderType()==OP_BUY) TMT_LastLostDirection[0]=Buy;
                     else TMT_LastLostDirection[0]=Sell;
                     FirstLost=true;
                     if(RCS_WriteDebug) L3_WriteDebug(2,"TMT_LastLostDirection: Last lossy trade : ticket Nr."+OrderTicket()) ;                     
                  }   
              }
            i--;
      }


    if(MMT_DecreaseLots_1On) 
      {
         if(RCS_WriteDebug) L3_WriteDebug(2,"MMT_DecrLotsPerc: Decrease Factors "+MMT_DecrFactor[0]+" "+MMT_DecrFactor[1]+" "+MMT_DecrFactor[2]) ;
         if (losses==1)lots=lots*MMT_DecrFactor[0]*0.01;   
         if (losses==2)lots=lots*MMT_DecrFactor[1]*0.01;   
         if (losses>=3)lots=lots*MMT_DecrFactor[2]*0.01;   
         if(RCS_WriteDebug) L3_WriteDebug(2,"MMT_DecrLotsPerc: wins: "+wins+" losses: "+losses+"  lots: "+lots);
      }
      
    
    if(lots<MMT_MinLot) { lots=MMT_MinLot; if(RCS_WriteLog) L2_WriteLog(2,"MM_OptimizeLotSize: lots switched to min "+lots); }
    if(lots>MMT_MaxLot) { lots=MMT_MaxLot; if(RCS_WriteLog) L2_WriteLog(2,"MM_OptimizeLotSize: lots switched to max "+lots); }

    lots /=lotStep;
    lots  = MathFloor(lots);
    lots *= lotStep;
    
    if(RCS_WriteLog) L2_WriteLog(2,"MM_OptimizeLotSize: "+lots+" LOTS prepared for a next order.");
    return(lots);
  }

int T2_SendTrade(int TP, int SL, double lot, int order)  //Execute Trades
  { 
    double price;
    color  arrow;
    int    ticket;

    string tradecomment= RCS_EAComment;
    RefreshRates();

    if (order %2==OP_BUY)
      { //if number is even
        price =  Ask;
        arrow =  Navy;
      }

    if (order %2==OP_SELL)
      { //if number is odd
        price =  Bid;
        SL    = -SL;
        TP    = -TP;
        arrow =  Magenta;
      }
      
    if(!A1_4_IsTradePossible()) return(Failed);
    
    if(AccountFreeMarginCheck(Symbol(),order,lot)<0)
      {
        if(RCS_WriteLog) L2_WriteLog(3,"AccountFreeMarginCheck - not enough money to open a new trade");
        GBV_GreenLight=false;
      }    
      
    else ticket = OrderSend( Symbol(),
                        order,
                        lot,
                        price,
                        TMT_Slippage,
                        price-SL*Point,
                        price+TP*Point,
                        tradecomment,
                        GBV_MagicNumSet,
                        0, 
                        arrow );

    if (ticket != Failed)
      {
        if(RCS_WriteLog) L2_WriteLog(3,"New trade-Ticket Nr."+ticket+" "+Symbol()+" order:"+order+" lot:"+lot+" price:"+price+" SL:"+(price-SL*Point)+" TP:"+(price+TP*Point));
        TMT_LastOpenTime=TimeCurrent();
        return (ticket); 
      } 
    else
      {
        int error=GetLastError();
        if(RCS_WriteLog) L2_WriteLog(3,"New trade-Error:"+error+" "+Symbol()+" order:"+order+" lot:"+lot+" price:"+price+" slippage:"+TMT_Slippage+" SL:"+(price-SL*Point)+" TP:"+(price+TP*Point));
        if(RCS_WriteLog) L2_WriteLog(3,"New trade-Error(addition):"+" Ask:"+Ask+" Bid:"+Bid+" Spread:"+MarketInfo(Symbol(),MODE_SPREAD)+" Stoplevel: "+MarketInfo(Symbol(),MODE_STOPLEVEL)+" Freezelevel: "+MarketInfo(Symbol(),MODE_FREEZELEVEL));
        return (Failed); 
      }
  }

 
  
//**********  Exit Strategies & Trailing Stops **********************************//
void X1_ManageExit(int ticket)
{ //Contains all of the exit strategies and trade management routines. Listed in priority.

    RefreshRates();

    bool enableBuy=true,enableSell=true;
    
    if(EMT_CheckTempTrend && EXT_TC_SetSize>=EXT_TC_Relevant && EXT_TC_Direction!=0) //close sooner if a temporary counter trend is recognized
     {  
       if(EXT_TC_Direction==Buy && OrderType()==OP_SELL && OrderStopLoss()-Ask<=EXT_TC_PipsToClose*Point) 
           if(A1_4_IsTradePossible()) 
            {
              if(OrderClose(ticket,OrderLots(),Ask,TMT_Slippage,Red)) if(RCS_WriteLog) L2_WriteLog(5,StringConcatenate("EMT_CheckTempTrend: Ticket Nr.",ticket," was closed because of a temporary counter trend and close SL."));
            }  
       if(EXT_TC_Direction==Sell && OrderType()==OP_BUY && Bid-OrderStopLoss()<=EXT_TC_PipsToClose*Point)
           if(A1_4_IsTradePossible()) 
            {
              if(OrderClose(ticket,OrderLots(),Bid,TMT_Slippage,Red)) if(RCS_WriteLog) L2_WriteLog(5,StringConcatenate("Ticket Nr.",ticket," was closed because of a temporary counter trend and close SL."));
            }  
     }
     
     if(EMT_TrendExtendTP_On) //extend TP when there is a trend
      if(EXT_Confirm_trend(ticket,EMF_TrendExtendTP_On)) //filter when no trend
         {
            if(OrderType()==OP_BUY && (OrderTakeProfit()<(EMT_TrendExtendTP_DISTtoTP*Point+Ask)))
               {
                 if(RCS_WriteLog) L2_WriteLog(5,StringConcatenate("EMT_TrendExtendTP_On: BUY Ticket Nr.",ticket," will have extended TP because it is on trend."));
                 ChangeTP(ticket,OrderTakeProfit()+EMT_MoveTPonTS*Point); 
               }    
            if(OrderType()==OP_SELL && (OrderTakeProfit()>(Bid-EMT_TrendExtendTP_DISTtoTP*Point)))
               {
                 if(RCS_WriteLog) L2_WriteLog(5,StringConcatenate("EMT_TrendExtendTP_On: SELL Ticket Nr.",ticket," will have extended TP because it is on trend."));
                 ChangeTP(ticket,OrderTakeProfit()-EMT_MoveTPonTS*Point); 
               }    
         }      

     
         //**********  Exit with multi currency indicator - using MA **********************************// 
         
/*
function EXT_Confirm_trend(ticket,settings) explanation:
used in a connection with additional filters
if the function returns true, it means that the ticket doesn´t follow trend and it admits to continue closing or changing the ticket
if the function returns false, it means that the ticket follows trend and it is not adviced to close it, it doesn't continue and jump on another function
1st parameter - switch on (1) or switch off (0) using additional filters for the function (if 2nd parameter=0 then switch on (0) or switch off (1)) 
2nd parameter - if (1) and if all conditions are true then it returns true
              - if (1) and if all conditions are not true then it returns false
              - if (0) and if all conditions are true then it returns false
              - if (0) and if all conditions are not true then it returns true              
3rd parameter - sets an expectation that kind of market the ticket should follow - (1) follow a trend, (0) follow a counter trend
4th parameter - filters no trend markets, (1) filter on, (0) filter off
i.e. "1010" = filter is on, it returns 0 when all conditions are confirmed, function doesn't filter when there isn't a trend, it doesn't filter flat markets
*/  
       
    if(EMT_RSI_HF_On) // exit immediately when counter trend recognized 
      {
         if(EXT_Confirm_trend(ticket,EMF_RSI_HF_On))
           if(EX_CheckTREND_RSIHF(ticket)) 
             {
               if(OrderType()==OP_BUY)
                   if(A1_4_IsTradePossible()) 
                      {
                        if(OrderClose(ticket,OrderLots(),Bid,TMT_Slippage,Red)) if(RCS_WriteLog) L2_WriteLog(5,StringConcatenate("EMT_RSI_HF_On: Ticket Nr.",ticket," was closed because of a recognized counter trend."));
                       }  
               if(OrderType()==OP_SELL)
                   if(A1_4_IsTradePossible()) 
                      {
                        if(OrderClose(ticket,OrderLots(),Ask,TMT_Slippage,Red)) if(RCS_WriteLog) L2_WriteLog(5,StringConcatenate("EMT_RSI_HF_On: Ticket Nr.",ticket," was closed because of a recognized counter trend."));
                       }
              }
       }
       
     if(EMT_MA_HF_On) // exit immediately when counter trend recognized 
      {
         if(EXT_Confirm_trend(ticket,EMF_MA_HF_On))
           if(EX_CheckTREND_MAHF(ticket)) 
             {
               if(OrderType()==OP_BUY)
                   if(A1_4_IsTradePossible()) 
                      {
                       if(OrderClose(ticket,OrderLots(),Bid,TMT_Slippage,Red)) if(RCS_WriteLog) L2_WriteLog(5,StringConcatenate("EMT_MA_HF_On: Ticket Nr.",ticket," was closed because of a recognized counter trend."));
                      }  
               if(OrderType()==OP_SELL)
                   if(A1_4_IsTradePossible()) 
                      {
                       if(OrderClose(ticket,OrderLots(),Ask,TMT_Slippage,Red)) if(RCS_WriteLog) L2_WriteLog(5,StringConcatenate("EMT_MA_HF_On: Ticket Nr.",ticket," was closed because of a recognized counter trend."));
                      }
              }
       }
      
              
          //**********  Exit with multi currency indicator - using RSI **********************************//   
            

          
    if(EMT_OpenBarCorrelation_On) // exit when a certain conditions are present
      {
         if(EXT_Confirm_trend(ticket,EMF_OpenBarCorrelation_On))
           if(EX_CheckTREND_OBC1(ticket)) 
             {
               if(OrderType()==OP_BUY)
                   if(A1_4_IsTradePossible()) 
                      {
                        if(OrderClose(ticket,OrderLots(),Bid,TMT_Slippage,Red)) if(RCS_WriteLog) L2_WriteLog(5,StringConcatenate("EMT_OpenBarCorrelation_On: Ticket Nr.",ticket," was closed."));
                      }  
               if(OrderType()==OP_SELL)
                   if(A1_4_IsTradePossible()) 
                      {
                        if(OrderClose(ticket,OrderLots(),Ask,TMT_Slippage,Red)) if(RCS_WriteLog) L2_WriteLog(5,StringConcatenate("EMT_OpenBarCorrelation_On: Ticket Nr.",ticket," was closed."));
                      }
              }
       }

    if(EMT_QM1_On) // exit when a certain conditions are present
      {
         if(EXT_Confirm_trend(ticket,EMF_QM1_On))
           if(EX_CheckTREND_QM1(ticket)) 
             {
               if(OrderType()==OP_BUY)
                   if(A1_4_IsTradePossible()) 
                      {
                        if(OrderClose(ticket,OrderLots(),Bid,TMT_Slippage,Red)) if(RCS_WriteLog) L2_WriteLog(5,StringConcatenate("EMT_QM1_On: Ticket Nr.",ticket," was closed."));
                      }  
               if(OrderType()==OP_SELL)
                   if(A1_4_IsTradePossible()) 
                      {
                        if(OrderClose(ticket,OrderLots(),Ask,TMT_Slippage,Red)) if(RCS_WriteLog) L2_WriteLog(5,StringConcatenate("EMT_QM1_On: Ticket Nr.",ticket," was closed."));
                      }
              }
       }


    if(EMT_TSI_On) // exit when a certain conditions are present
      {
         if(EXT_Confirm_trend(ticket,EMF_TSI_On))
           if(EX_CheckTREND_TSI(ticket)) 
             {
               if(OrderType()==OP_BUY)
                   if(A1_4_IsTradePossible()) 
                      {
                       if(OrderClose(ticket,OrderLots(),Bid,TMT_Slippage,Red)) if(RCS_WriteLog) L2_WriteLog(5,StringConcatenate("EMT_MA_HF_On: Ticket Nr.",ticket," was closed because of a recognized counter trend."));
                      }  
               if(OrderType()==OP_SELL)
                   if(A1_4_IsTradePossible()) 
                      {
                       if(OrderClose(ticket,OrderLots(),Ask,TMT_Slippage,Red)) if(RCS_WriteLog) L2_WriteLog(5,StringConcatenate("EMT_MA_HF_On: Ticket Nr.",ticket," was closed because of a recognized counter trend."));
                      }
              }
       }


         //end of **********  Exit with multi currency indicator **********************************//     
   
   
    EMT_minStop=MarketInfo(Symbol(),MODE_STOPLEVEL); //updated every tick in case of news SL movement
    if(EMT_ForceClose_On) //close after certain amount hours
      { 
         if(EXT_Confirm_trend(ticket,EMF_ForceClose_On)) 
            if(X1_ForceClose(ticket)) return;
      }   

    if(EMT_CloseOnFriday_On) //close on Friday at EXF_FridayTimeClose time
      { 
         if(EXT_Confirm_trend(ticket,EMF_CloseOnFriday_On)) 
            if(X2_CloseFriday(ticket)) return;
      }   

   
    if(EMT_RecrossClose) //check recross functions
      {
         int poolPos=EX_RecrossCheck(ticket);
         if(poolPos!=-1) 
            {
                if(EXT_Confirm_trend(ticket,EMF_RecrossClose)) 
                  if(EX_RecrossExit(ticket,poolPos)) 
                    {
                       if(RCS_WriteLog) L2_WriteLog(5,"EMT_RecrossClose: Ticket : "+ticket+" was closed because of a function Recross Exit.") ;
                       return;
                    }
  
                  else if(RCS_WriteDebug) L3_WriteDebug(5,"EMT_RecrossClose: Nothing to close.");
                else if(RCS_WriteDebug) L3_WriteDebug(5,"EMT_RecrossClose: Stop! There is no flat market!");  
      
            }                
      }
  
  if(EMT_move_SL && OrderProfit()>0) EX_Decrease_SL(ticket);//move SL with profit in steps and save profit
  if(EMT_move_TP && OrderProfit()<0) EX_Decrease_TP(ticket);//move TP in lost in steps and save profit


  if(EMT_CheckMainTrend) // if a trade is going agains trend set trailing stop
    {
      if(EXT_Confirm_trend(ticket,EMF_CheckMainTrend)) //check additional exit filters, do not allow to exit when there are not good conditions
         {
            enableBuy=true;
            enableSell=true;
            Z_F5_BlockTradingFilter5(enableBuy,enableSell);   //locate main trend and trade in its direction only 
            if (OrderType()==OP_BUY && enableBuy==false) X9_ModifyTrailingStop(ticket,EMT_TS_pipsDISTANCE,0);
            if (OrderType()==OP_SELL && enableSell==false) X9_ModifyTrailingStop(ticket,EMT_TS_pipsDISTANCE,0);   
            if(RCS_WriteLog && (enableBuy==false || enableSell==false)) L2_WriteLog(6,"EMT_CheckMainTrend - Open ticket : "+ticket+" goes again main trend ! Trailing stop was setted. ") ;
         }   
    }       


/*
function EXT_Confirm_trend(ticket,settings) explanation:
used in a connection with additional filters
if the function returns true, it means that the ticket doesn´t follow trend and it admits to continue closing or changing the ticket
if the function returns false, it means that the ticket follows trend and it is not adviced to close it, it doesn't continue and jump on another function
1st parameter - switch on (1) or switch off (0) using additional filters for the function (if 2nd parameter=0 then switch on (0) or switch off (1)) 
2nd parameter - if (1) and if all conditions are true then it returns true
              - if (1) and if all conditions are not true then it returns false
              - if (0) and if all conditions are true then it returns false
              - if (0) and if all conditions are not true then it returns true              
3rd parameter - sets an expectation that kind of market the ticket should follow - (1) follow a trend, (0) follow a counter trend
4th parameter - filters no trend markets, (1) filter on, (0) filter off
i.e. "1010" = filter is on, it returns 0 when all conditions are confirmed, function doesn't filter when there isn't a trend, it doesn't filter flat markets
*/        
  if(EMT_ExitWithTS_On)
   if(EXT_Confirm_trend(ticket,EMF_ExitWithTS_On)) //check additional exit filters, do not allow to exit when there are not good conditions
      {
        X9_ModifyTrailingStop(ticket,EMT_TS_pipsDISTANCE,EMT_Start_DelayTS);
      }
}


bool X1_ForceClose(int ticket)   //force to exit after a certain amount of time 
  {  
    int onlyhours=MathFloor(EXF_ForceClose_Hours);
    int    PerForce = onlyhours*3600+(EXF_ForceClose_Hours-onlyhours)*60*100;
    double newSL       =0;
    if (PerForce!=0)
      {
      if((OrderOpenTime() + PerForce) < Time[0])
         {
         if(OrderType() == OP_BUY)  
            {
               if(OrderClose(OrderTicket(),OrderLots(),Bid,TMT_Slippage,Red)) if(RCS_WriteLog) L2_WriteLog(5,StringConcatenate("X1_ForceClose: Ticket Nr.",ticket," was closed.")); 
            }  
         if(OrderType() == OP_SELL) 
            {
               if(OrderClose(OrderTicket(),OrderLots(),Ask,TMT_Slippage,Red)) if(RCS_WriteLog) L2_WriteLog(5,StringConcatenate("X1_ForceClose: Ticket Nr.",ticket," was closed.")); 
            }   
         if(RCS_WriteLog) L2_WriteLog(5,"Force to close - ticket:"+ticket+" - too long is opened this trade!");
         return(true);
         }
      }
    return(false);  
   }
   
bool X2_CloseFriday(int ticket) 
   {
       int onlyhours=MathFloor(EXF_FridayTimeClose);
       int  closetime = onlyhours*3600+(EXF_FridayTimeClose-onlyhours)*60*100;
       if(GBV_currentTime>closetime && DayOfWeek()==5) 
         {
         if(OrderType() == OP_BUY)  
            {
               if(OrderClose(OrderTicket(),OrderLots(),Bid,TMT_Slippage,Red)) if(RCS_WriteLog) L2_WriteLog(5,StringConcatenate("X2_CloseFriday: Ticket Nr.",ticket," was closed.")); 
            }  
         if(OrderType() == OP_SELL) 
            {
               if(OrderClose(OrderTicket(),OrderLots(),Ask,TMT_Slippage,Red)) if(RCS_WriteLog) L2_WriteLog(5,StringConcatenate("X2_CloseFriday: Ticket Nr.",ticket," was closed.")); 
            }   
         if(RCS_WriteLog) L2_WriteLog(5,"Force to close - ticket:"+ticket+" - on Friday before weekend!");
         return(true);
         }
    return(false);  
   }  

void X9_ModifyTrailingStop(int ticket, int tsvalue, int delayts)
  {  //          
    //check additional exit filters, do not allow to exit when there are not good conditions
    if(EMT_DecreaseTP_On)
      if(EXT_Confirm_trend(ticket,EMF_DecreaseTP_On)) EX_DecrTPifCountTrend(ticket); 

    double slvalue;
    if(tsvalue>=EMT_minStop) //check minimal value allowed by broker
      {        
        if(EMT_Count_TSOn) 
        { 
          if(OrderProfit()>0)tsvalue=MathAbs(MathFloor(((OrderTakeProfit()-OrderStopLoss())/Point)*0.5));
          if(tsvalue<EMT_minStop)tsvalue=EMT_minStop;
        }
      } 
    else 
      { //Set safe value
        tsvalue=EMT_minStop;
      }
      
    if(RCS_WriteDebug) L3_WriteDebug(5,"X9_ModifyTrailingStop: TS ticket:"+ticket+" TS value:"+tsvalue+" delay TS:"+delayts);

    if      (OrderType() == OP_BUY)
      {
        slvalue=NormalizeDouble(Bid-(Point*tsvalue),Digits);
        if (OrderStopLoss()<slvalue && OrderOpenPrice()+Point*delayts<Bid)//&& OrderOpenPrice()+Point*delayts<Bid-tsvalue*Point
        {
          if(!X9_ModifySL(slvalue,ticket))
          {
            L4_WriteError();
            L2_WriteLog(5,"Buy order-ERROR OrderSL:" +OrderStopLoss()+ " slvalue:"+ slvalue);
          }
        }
      } 
    else if (OrderType() == OP_SELL)
      {
        slvalue=NormalizeDouble(Ask+(Point*tsvalue),Digits);
        if (OrderStopLoss()>slvalue && OrderOpenPrice()-Point*delayts>Ask)// && OrderOpenPrice()-Point*delayts>Ask+tsvalue*Point
        {
          if(!X9_ModifySL(slvalue,ticket))
          {
            L4_WriteError();
            L2_WriteLog(5,"EXIT function: Sell order-ERROR OrderSL:" +OrderStopLoss()+ " slvalue:"+ slvalue);
          }
        }
      }
  }

bool X9_ModifySL(double sl, int ticket)
  {
    double MoveTP;
    if(EMT_MoveTPonTS !=0)
    {
      if (OrderType() == OP_SELL) 
      {
        MoveTP=EMT_MoveTPonTS*(-1)*Point;
        if(NormalizeDouble(sl-Ask,Digits)<EMT_minStop*Point)return(true); 
        if(Ask-OrderTakeProfit()+MoveTP<EMT_minStop*Point) 
          MoveTP=-(EMT_minStop*Point+Ask-OrderTakeProfit()); //Set the new distance to minimum safe point, -OTP() cancels OTP() // repared error 4051
      }
      else 
      {
        MoveTP=EMT_MoveTPonTS*Point;
        if(NormalizeDouble(Bid-sl,Digits)<EMT_minStop*Point) return(true); 
        if(OrderTakeProfit()-Bid+MoveTP<EMT_minStop*Point) 
          MoveTP=EMT_minStop*Point-Bid+OrderTakeProfit();  //Set the new distance to minimum safe point, -OTP() cancels OTP()
      }
    }
    
    sl=NormalizeDouble(sl,Digits);
    MoveTP=NormalizeDouble(MoveTP,Digits); 
    if(sl==NormalizeDouble(OrderStopLoss(),Digits)) return(true); 
    if(RCS_WriteDebug) L3_WriteDebug(6,"MODIFY Ticket:"+ticket+" OpenPrice:"+OrderOpenPrice()+" old SL:"+OrderStopLoss()+" new SL:"+sl+" TP:"+(OrderTakeProfit()+MoveTP*Point));

    if(!A1_4_IsTradePossible()) return(false); //check if trade is allowed 
    if(OrderModify ( ticket, OrderOpenPrice(), sl,
                      OrderTakeProfit()+MoveTP,   
                      0, DarkOrchid)) 
        return(true);
    else return(false);
  }



//********************** SIGNALS *********************//


int EntrySignal_OBC1() //check currencies if they have the same movement when they open a new bar, follows trend
  { 
    if(SGS_OBC1Bars<Bars) SGS_OBC1Bars=Bars;//This should be based on bars, reduces check to once every bar.
    else return(SGS_OBC1Signal);
 
    SGS_OBC1Signal=iCustom(TMT_Currency,0,"OBCE",SGS_OBC1_P1,SGS_OBC1_P2,SGS_OBC1_P3,SGS_OBC1_P4,SGS_OBC1_Confirm1,SGS_OBC1_Confirm2,SGS_OBC1_Confirm3,SGS_OBC1_Confirm4,SGS_OBC1MaxSumConfirm,SGS_OBC1MinSumConfirm,SGS_OBC1Period,SGS_OBC1History,SGS_OBC1MinEntryDifference,SGS_OBC1CloseStepsBefore,SGS_OBC1CloseStepsOpposite,SGS_OBC1TrendHistory,0,0);
    if(RCS_WriteDebug) L3_WriteDebug(0,"SGE_OpenBarCorrelation_On: SGS_OBC1Signal:"+SGS_OBC1Signal);
    return (SGS_OBC1Signal);
  }

int EntrySignal_QM1() //check Quantum indicator
  { 
    
    if(SGS_QM1_Bars<Bars) SGS_QM1_Bars=Bars;//This should be based on bars, reduces check to once every bar.
    else return(SGS_QM1_Signal);
    
    
    if(SGS_QM1_ChangeWhenLost)
      {
         int    orders       =OrdersHistoryTotal();
         int    i            =orders-1;
         int    trades       =0;
         double    losses       =0;
        
         while (i > 0 || trades<3)
          {
            if(OrderSelect(i,SELECT_BY_POS,MODE_HISTORY)==false) break;
            if(OrderSymbol()==Symbol() && OrderType()<=OP_SELL && OrderMagicNumber()==GBV_MagicNumSet)
              {
                if(OrderProfit()<0 && trades<3) losses++;
                trades++;   
              }
            i--;
          }
         if(losses>2 && SGS_QM1_CalculatingMethod==0) SGS_QM1_CalculatingMethod=1; 
         if(losses>2 && SGS_QM1_CalculatingMethod==1) SGS_QM1_CalculatingMethod=0;
         if(losses>2) if(RCS_WriteDebug) L3_WriteDebug(0,"SGS_QM1_ChangeWhenLos: Because of losses SGS_QM1_CalculatingMethod now ="+SGS_QM1_CalculatingMethod);
      }
    
    // ... need to finish!  
    
    if(SGS_QM1_CalculatingMethod==0) SGS_QM1_Signal=iCustom(TMT_Currency,0,"Quantum",SGS_QM1_ChangeOrientation,SGS_QM1History,0,0);
    if(SGS_QM1_CalculatingMethod==1) SGS_QM1_Signal=-iCustom(TMT_Currency,0,"Quantum",SGS_QM1_ChangeOrientation,SGS_QM1History,2,0);
    
    if(RCS_WriteDebug) L3_WriteDebug(0,"SGE_QM1_On: SGS_QM1_Signal:"+SGS_QM1_Signal);
    return (SGS_QM1_Signal);
  }
  
       
   
int EntrySignal_RSIHF()
  { //RSI Difference on a higher timeframe about 2 degree
  

    if(SGS_RSI_HF_Bars<Bars) //This should be based on bars, reduces check to once every bar.
      {  
         SGS_RSI_HF_Bars=Bars;
      }
    else return(SGS_RSI_HF_Signal);
  
    SGS_RSI_HF_Signal=None;

    SGS_RSI_HF_1=iRSI(NULL,MovePeriod(SGS_RSI_HF_TimeFrameShift),SGS_RSI_HF_Per,PRICE_CLOSE,1);
    SGS_RSI_HF_2=iRSI(NULL,MovePeriod(SGS_RSI_HF_TimeFrameShift),SGS_RSI_HF_Per,PRICE_CLOSE,2);

    if ((SGS_RSI_HF_1>SGS_RSI_HF_2) && (SGS_RSI_HF_High>SGS_RSI_HF_1)) SGS_RSI_HF_Signal=Buy;
    else if ((SGS_RSI_HF_1<SGS_RSI_HF_2)&& (SGS_RSI_HF_Low<SGS_RSI_HF_1)) SGS_RSI_HF_Signal=Sell;

    if(RCS_WriteDebug) L3_WriteDebug(0,"SGE_RSI_HF_On: Signal "+SGS_RSI_HF_Signal+" SGS_RSI_HF_1:"+SGS_RSI_HF_1+" SGS_RSI_HF_2:"+SGS_RSI_HF_2);

    return (SGS_RSI_HF_Signal);
  }

int EntrySignal_MAHF()

  { //MA on a higher timeframe about x degrees, follows trend
  

  /*  if(SGS_MA_HF_Bars<Bars) //This should be based on bars, reduces check to once every bar.
      {  
         SGS_MA_HF_Bars=Bars;
      }
    else return(SGS_MA_HF_Signal);
  */
    SGS_MA_HF_Signal=None;
    
    SGS_MA_HF_1=iMA(NULL,MovePeriod(SGS_MA_HF_TimeFrameShift),SGS_MA_HF_Period,SGS_MA_HF_Shift,0,PRICE_CLOSE,1);
    SGS_MA_HF_2=iMA(NULL,MovePeriod(SGS_MA_HF_TimeFrameShift),SGS_MA_HF_Period,SGS_MA_HF_Shift,0,PRICE_CLOSE,2);

    if (SGS_MA_HF_1>SGS_MA_HF_2 && SGS_MA_HF_1>iClose(NULL,PERIOD_CURRENT,0)) SGS_MA_HF_Signal=Buy;
    else if (SGS_MA_HF_1<SGS_MA_HF_2 && SGS_MA_HF_1<iClose(NULL,PERIOD_CURRENT,0)) SGS_MA_HF_Signal=Sell;

    if(RCS_WriteDebug) L3_WriteDebug(0,"SGE_MA_HF_On: Signal "+SGS_MA_HF_Signal+" SGS_MA_HF_1:"+SGS_MA_HF_1+" SGS_MA_HF_2:"+SGS_MA_HF_2+", iClose(0): "+iClose(NULL,PERIOD_CURRENT,0));

    return (SGS_MA_HF_Signal);
  }    
  
int EntrySignal_TSI()
  { //Twitter sentiment indicator
  
    double TSI_buy,TSI_sell;
    if(SGS_TSI_Bars<Bars) //This should be based on bars, reduces check to once every bar.
      {  
         SGS_TSI_Bars=Bars;
      }
    else return(SGS_TSI_Signal);
  
    SGS_TSI_Signal=None;
    
    TSI_buy= iCustom(TMT_Currency,0,"TSI",SGS_TSI_Distance,SGS_TSI_mood_max,SGS_TSI_MA_Period,2,0);
    TSI_sell=iCustom(TMT_Currency,0,"TSI",SGS_TSI_Distance,SGS_TSI_mood_max,SGS_TSI_MA_Period,3,0);
    
    if(TSI_buy!=0) SGS_TSI_Signal=Buy;
    if(TSI_sell!=0) SGS_TSI_Signal=Sell;
    if(RCS_WriteDebug) L3_WriteDebug(0,"SGE_TSI_On: Signal: "+SGS_TSI_Signal);

    return (SGS_TSI_Signal);
  }    
    

bool Filter_Time()   
  { //Time Expiry
    bool BlockTrade=true;  //only trade during permitted hours
    
      if(TimeDayOfWeek(Time[0])==0)  //Sunday
        {
          if(GBV_currentTime>=FTS_SunAt && GBV_currentTime<FTS_SunTo) 
            {
               BlockTrade=false;
               if(RCS_WriteDebug) L3_WriteDebug(1,"FTE_Time_On: Trading within Sunday is allowed:"+GBV_currentTime);
            }   
        }
      else if(TimeDayOfWeek(Time[0])==5)  //Friday
        {
          if(GBV_currentTime>=FTS_FriAt && GBV_currentTime<FTS_FriTo) 
            {
               BlockTrade=false;
               if(RCS_WriteDebug) L3_WriteDebug(1,"FTE_Time_On: Trading within Friday is allowed:"+GBV_currentTime);
            }   
        }
      else if(TimeDayOfWeek(Time[0])==1)  //Monday
        {
          if(GBV_currentTime>=FTS_MonAt && GBV_currentTime<FTS_MonTo) 
            {
               BlockTrade=false;
               if(RCS_WriteDebug) L3_WriteDebug(1,"FTE_Time_On: Trading within Monday is allowed:"+GBV_currentTime);
            }   
        }
      else if((GBV_currentTime>=FTS_WeekAt1 && GBV_currentTime<FTS_WeekTo1) ||
      (GBV_currentTime>=FTS_WeekAt2 && GBV_currentTime<FTS_WeekTo2)) BlockTrade=false;

    if(RCS_WriteDebug && BlockTrade) L3_WriteDebug(1,"FTE_Time_On: Trading is blocked:"+GBV_currentTime);
    return (BlockTrade);
  }

   
bool Z_F5_BlockTradingFilter5(bool& enableBuy,bool& enableSell)   //locate main trend and trade in its direction only
  { 
    double lengh;
    if (FTS_SR_barsCount<iBars(NULL,0) || FTS_SR_barsCount==0) //count only when a new bar appears
      {
        double FTS_SR_high[];
        double FTS_SR_low[];
        ArrayCopySeries(FTS_SR_high,MODE_HIGH,NULL,0);
        ArrayCopySeries(FTS_SR_low,MODE_LOW,NULL,0);        
        FTS_SR_MaxPos0=ArrayMaximum(FTS_SR_high,FTS_HorizDist,0);
        FTS_SR_MinPos0=ArrayMinimum(FTS_SR_low,FTS_HorizDist,0);
        FTS_SR_MaxPoint0=FTS_SR_high[FTS_SR_MaxPos0];
        FTS_SR_MinPoint0=FTS_SR_low[FTS_SR_MinPos0];
        FTS_SR_MaxPos1=ArrayMaximum(FTS_SR_high,MathRound(FTS_HorizDist/2),0);
        FTS_SR_MinPos1=ArrayMinimum(FTS_SR_low,MathRound(FTS_HorizDist/2),0);        
        FTS_SR_MaxPos2=ArrayMaximum(FTS_SR_high,FTS_HorizDist,MathRound(FTS_HorizDist/2)+1);
        FTS_SR_MinPos2=ArrayMinimum(FTS_SR_low,FTS_HorizDist,MathRound(FTS_HorizDist/2)+1);
        FTS_SR_MaxPoint1=FTS_SR_high[FTS_SR_MaxPos1];
        FTS_SR_MinPoint1=FTS_SR_low[FTS_SR_MinPos1];
        FTS_SR_MaxPoint2=FTS_SR_high[FTS_SR_MaxPos2];
        FTS_SR_MinPoint2=FTS_SR_low[FTS_SR_MinPos2];
        
        FTS_SR_barsCount=iBars(NULL,0);                        
        
      }

    if (FTS_SR_MaxPos0<FTS_SR_MinPos0) // check buy condition
      {
         lengh=Bid-FTS_SR_MinPoint0;
         if (TMT_SL*Point<lengh) 
           {
            if(!(FTS_SR_MaxPoint1>FTS_SR_MaxPoint2 && FTS_SR_MinPoint1>FTS_SR_MinPoint2)) 
               {
                if(RCS_WriteDebug) L3_WriteDebug(1,"FTE_FolMainTrend_On: main trend : Block buy trade.");
                enableBuy=false;
               }
            else if(RCS_WriteDebug) L3_WriteDebug(1,"FTE_FolMainTrend_On: main trend : Follow long trend.");              
           } 
               
      } 
    if (FTS_SR_MaxPos0>FTS_SR_MinPos0) // check sell condition
      {
         lengh=FTS_SR_MaxPoint0-Bid;
         if(TMT_SL*Point<lengh) 
           {
            if(!(FTS_SR_MaxPoint1<FTS_SR_MaxPoint2 && FTS_SR_MinPoint1<FTS_SR_MinPoint2))
               {
                if(RCS_WriteDebug) L3_WriteDebug(1,"FTE_FolMainTrend_On: main trend : Block sell trade.");
                enableSell=false;
               } 
            else if(RCS_WriteDebug) L3_WriteDebug(1,"FTE_FolMainTrend_On: main trend : Follow short trend."); 
           } 
         
      } 

    return (false);  //do not block trading
  }


bool Z_F6_BlockTradingFilter6(bool& enableBuy,bool& enableSell)//locate main trend and allow to trade only in its  direction  
  { 

 if (FTS_LastBarCnt<iBars(NULL,MovePeriod(FTS_TimePerShift))) //count only when a new bar appears
  {
    FTS_Bar1=iClose(NULL,MovePeriod(FTS_TimePerShift),1);
    FTS_Bar3=iClose(NULL,MovePeriod(FTS_TimePerShift),FTS_Distance);
    double averPrice,bar2;
   
    FTS_Difference=0;
    for(int cnt=FTS_Distance;cnt>0;cnt--)
      {
         averPrice=cnt*MathAbs(FTS_Bar3-FTS_Bar1)/FTS_Distance;
         if(FTS_Bar3-FTS_Bar1>=0) bar2=FTS_Bar1+averPrice;
         else bar2=FTS_Bar1-averPrice;
         FTS_Difference+=MathAbs( iClose(NULL,MovePeriod(FTS_TimePerShift),cnt)-bar2);  
      }
     FTS_Difference/=Point;
     FTS_Slope=MathAbs( (FTS_Bar1-FTS_Bar3)/(Point*FTS_Distance) );
     FTS_LastBarCnt=iBars(NULL,0);
   }     
     if(FTS_Slope<FTS_MinSlope || FTS_MaxDiff<FTS_Difference) return(false);//do not block trading
     if(FTS_Bar3>FTS_Bar1) 
      {
         enableBuy=false;
         if(RCS_WriteDebug) L3_WriteDebug(1,"FTE_LocMainTrend_On-main trend : Block long trades.");
      }   

     else 
      {
         enableSell=false;
         if(RCS_WriteDebug) L3_WriteDebug(1,"FTE_LocMainTrend_On-main trend : Block short trades.");
      }   
         
     return (false);//do not block trading
  }

bool Z_F7_BlockTradingFilter7(bool& enableBuy,bool& enableSell)  //find a temporary trend and block to trade agains it  
  { 
    if(EXT_TC_Direction==Buy) 
      {
         enableSell=false;
         if(RCS_WriteDebug) L3_WriteDebug(1,"FTE_TemporaryTrend_On-temporary trend : Block short trades.");
      }   
         
    if(EXT_TC_Direction==Sell) 
      {
         enableBuy=false;
         if(RCS_WriteDebug) L3_WriteDebug(1,"FTE_TemporaryTrend_On-temporary trend : Block long trades.");
      }   
         
  return(true);
  }  
  
bool FTS_ControlPeaks(int TradeDirect)  //wait for peaks

  { 
     if(FTS_PeaksBars==Bars) return(false);
     else FTS_PeaksBars=Bars;
     double maxhigh,minlow,scale;
          
     if(FTS_PeakLong==0 || FTS_PeakShort==0) 
      {
         maxhigh=High[iHighest(NULL,0,MODE_HIGH,FTS_BarsBack,0)];
         minlow=Low[iLowest(NULL,0,MODE_LOW,FTS_BarsBack,0)]; 
         scale=NormalizeDouble((maxhigh-minlow)*FTS_PeakPerc,Digits);
      } 
     else return(false);     

     if(FTS_PeakLong==0 && TradeDirect==1) 
      {
         FTS_PeakLong=Bid-scale;
         FTS_TimeLimitLong=TimeCurrent()+Period()*FTS_BarsForward*60;
      }           
     if(FTS_PeakShort==0 && TradeDirect==-1) 
      {
         FTS_PeakShort=Bid+scale;
         FTS_TimeLimitShort=TimeCurrent()+Period()*FTS_BarsForward*60;
      }           
     return(true);    
             
  }    

bool Z_F8_BlockTradingFilter8(bool& enableBuy,bool& enableSell)  //wait for peaks FTE_WaitForPeaks_On
  { 
     if(FTS_PeakLong<Bid && FTS_PeakLong!=0) 
      {
         enableBuy=false;
         if(RCS_WriteDebug) L3_WriteDebug(1,StringConcatenate("FTE_WaitForPeaks-still waiting for the peak, shift  ",Bid-FTS_PeakLong," needed : Block long trades."));
      }   
         
     if(FTS_PeakShort>Bid && FTS_PeakShort!=0) 
      {
         enableSell=false;
         if(RCS_WriteDebug) L3_WriteDebug(1,StringConcatenate("FTE_WaitForPeaks-still waiting for the peak, shift  ",FTS_PeakShort-Bid," needed : Block short trades."));
      }  
     if(FTS_TimeLimitLong<TimeCurrent()) FTS_PeakLong=0;
     if(FTS_TimeLimitShort<TimeCurrent()) FTS_PeakShort=0;
      
  return(true);
  }   


//*********************** LOGGING **************************//
void L1_OpenLogFile(string strName)
  {
    if ((!RCS_WriteLog && !RCS_WriteDebug) || !RCS_SaveInFile) return;
    bool newfile=false;
    int cnt=0;

    if(GBV_LogHandle <= 0) newfile=true;
    if(GBV_LogHandle > 0)
      {
         if(FileSize(GBV_LogHandle)>RCS_LogFileSize)newfile=true; //a file is divided into parts 
      }   
    if(!newfile)return;  //no need to open a new file, than return     
    if(GBV_LogHandle!=-1) FileClose(GBV_LogHandle);          
    string strMonthPad = "" ;
    if (Month() < 10) strMonthPad = "0";
    string strDayPad   = "" ;
    if (Day() < 10) strDayPad   = "0";
    string strFilename = StringConcatenate(strName, "_", Year(),
                                           strMonthPad, Month(),
                                           strDayPad,     Day(),"_",cnt, "_log.txt");
    while(FileIsExist(strFilename))
      {
         cnt++;
         strFilename = StringConcatenate(strName, "_", Year(),
                                           strMonthPad, Month(),
                                           strDayPad,     Day(),"_",cnt,"_log.txt");
      }                                     
                                                                                               

    GBV_LogHandle =FileOpen(strFilename, FILE_CSV | FILE_READ | FILE_WRITE); //Pick a new file name and open it. 
  }



void L2_WriteLog(int rank,string msg)
  {
    if (!RCS_WriteLog) return;
    if(RCS_LogFilter[rank]==0) return;    
    Print(msg); 
    if(!RCS_SaveInFile) return;       
    if (GBV_LogHandle <= 0)   return;
    msg = TimeToStr(TimeCurrent(), TIME_DATE | TIME_MINUTES | TIME_SECONDS) + " " + msg;
    FileWrite(GBV_LogHandle, msg);
  }

void L3_WriteDebug(int rank,string msg)  //Signal Debugging, rarely called
  {
    if (!RCS_WriteDebug) return;
    if(RCS_DebugFilter[rank]==0) return;   
    Print(msg);     
    if(!RCS_SaveInFile) return;    
    if (GBV_LogHandle <= 0) return;
    msg = TimeToStr(TimeCurrent(), TIME_DATE | TIME_MINUTES | TIME_SECONDS) + " " + msg;
    FileWrite(GBV_LogHandle, msg);
  }

void L4_WriteError()
  {
    if (!RCS_WriteLog) return;
    string msg="Error:"+ GetLastError()+" OrderType:"+OrderType()+" Ticket:"+OrderTicket();    
    Print(msg);   
    if(!RCS_SaveInFile) return;          
    if (GBV_LogHandle <= 0)   return;
    msg = TimeToStr(TimeCurrent(), TIME_DATE | TIME_MINUTES | TIME_SECONDS) + " " + msg;
    FileWrite(GBV_LogHandle, msg);
  }


int MovePeriod(int shift)
   {
      int CurrPer=Period();
      int PerArray[]={1,5,15,30,60,240,1440,10080,43200}; //array with periods in minutes
      int PosPer=ArrayBsearch(PerArray,CurrPer,WHOLE_ARRAY,0,MODE_ASCEND);
      if(PosPer+shift>7) return(43200);
      if(PosPer+shift<1)return(1);      
      else return(PerArray[PosPer+shift]);            
   }   
   
   
bool DecreaseTP(int ticket,double orderTP)
   {
      if(!A1_4_IsTradePossible()) return(false); //check if trade is allowed 
      if(orderTP==OrderTakeProfit()) return(false);
      if(OrderType()==OP_SELL)
         {
            if(MathAbs(Ask-orderTP)<EMT_minStop*Point || orderTP<OrderTakeProfit()) return(false);
         }   
      if(OrderType()==OP_BUY)
         {
            if(MathAbs(Bid-orderTP)<EMT_minStop*Point || orderTP>OrderTakeProfit()) return(false);
         }   
      if(RCS_WriteLog) L2_WriteLog(6,StringConcatenate("Decrease TP by Ticket Nr.",ticket," ,Current TP : ",OrderTakeProfit()," ,Next TP : ",orderTP)) ;
      if(OrderModify ( ticket, OrderOpenPrice(), OrderStopLoss(),
                      orderTP, 0, Purple)) return(true);
                     else return(false);

   }
   
bool ChangeTP(int ticket,double orderTP)
   {
      if(!A1_4_IsTradePossible()) return(false); //check if trade is allowed 
      orderTP=NormalizeDouble(orderTP,Digits);
      if(orderTP==OrderTakeProfit()) return(false);
      if(OrderType()==OP_SELL)
         {
            if(MathAbs(Ask-orderTP)<EMT_minStop*Point) return(false);
         }   
      if(OrderType()==OP_BUY)
         {
            if(MathAbs(Bid-orderTP)<EMT_minStop*Point) return(false);
         }   
      if(RCS_WriteLog) L2_WriteLog(6,StringConcatenate("ChangeTP by Ticket Nr.",ticket," ,Current TP : ",OrderTakeProfit()," ,Next TP : ",orderTP)) ;
      if(OrderModify ( ticket, OrderOpenPrice(), OrderStopLoss(),
                      orderTP, 0, Purple)) return(true);
                     else return(false);

   }


int TickContainer()
   {
      if(EXT_TC_Relevant<=EXT_TC_SetSize/2)return(0); //no result
      int cntRelevant;
      EXT_TC_Direction=0;
      for (int cnt=EXT_TC_SetSize-2;cnt>-1;cnt--)
         {
           EXT_TC_Store[cnt+1]=EXT_TC_Store[cnt];
           if(EXT_TC_Store[cnt+1]==1) cntRelevant++;
         }
      if(Bid>EXT_TC_LastTick)
         {
            cntRelevant++;
            EXT_TC_Store[0]=1;
         }
      else  EXT_TC_Store[0]=0;     
      EXT_TC_LastTick=Bid;    
      if(cntRelevant>=EXT_TC_Relevant) EXT_TC_Direction=1;   //a temporary trend is going up
      else if(cntRelevant<=EXT_TC_SetSize-EXT_TC_Relevant) EXT_TC_Direction=-1;  //a temporary trend is going down 
      return(EXT_TC_Direction); //no results   
   }

 
bool EX_Decrease_SL(int ticket) // in case of profit move SL


   {
       for(int cnt=ArraySize(EMT_MSL_PF)-1;cnt>=0;cnt--)
          {
             double countTP=NormalizeDouble(TMT_TP*Point*EMT_MSL_PF[cnt]*0.01,Digits); 
             double countSL=NormalizeDouble(TMT_SL*Point*EMT_MSL_SL[cnt]*0.01,Digits);              
             if (OrderType()==OP_BUY && Ask-OrderOpenPrice()>=countTP)
                {
                   if ((OrderOpenPrice()-TMT_SL*Point-Point)>(OrderStopLoss()-countSL)) //move stoploss
                      {
                          if(RCS_WriteLog) L2_WriteLog(6,"EX_Decrease_SL: BUY ORDER: EMT_MSL_ProfitPerc:"+EMT_MSL_PF[cnt]+" countTP:"+countTP+" EMT_MSL_MoveSL:"+EMT_MSL_SL[cnt]+" countSL:"+countSL) ;
                          X9_ModifySL(OrderOpenPrice()-TMT_SL*Point+countSL,ticket);
                          break;
                      }     
                 } 
             if (OrderType()==OP_SELL && OrderOpenPrice()-Bid>=countTP)
                 {
                   if ((OrderOpenPrice()+TMT_SL*Point+Point)<(OrderStopLoss()+countSL)) //move stoploss
                      {
                          if(RCS_WriteLog) L2_WriteLog(6,"EX_Decrease_SL: SELL ORDER: EMT_MSL_ProfitPerc:"+EMT_MSL_PF[cnt]+" countTP:"+countTP+" EMT_MSL_MoveSL:"+EMT_MSL_SL[cnt]+" countSL:"+countSL) ;
                          X9_ModifySL(OrderOpenPrice()+TMT_SL*Point-countSL,ticket);
                          break;
                      }     
                  } 
                        
            } 
   return(true);
   }// ******** end of a function 

bool EX_Decrease_TP(int ticket) // in case of profit move TP

   {
       for(int cnt=ArraySize(EMT_MTP_LF)-1;cnt>=0;cnt--)
          {
             double countSL=NormalizeDouble(TMT_SL*Point*EMT_MTP_LF[cnt]*0.01,Digits); 
             double countTP=NormalizeDouble(TMT_TP*Point*EMT_MTP_TP[cnt]*0.01,Digits);              
             if (OrderType()==OP_BUY && OrderOpenPrice()-Ask>=countSL)
                {
                   if ((OrderOpenPrice()+TMT_TP*Point+Point)<(OrderTakeProfit()+countTP)) //move takeprofit
                      {
                          if(RCS_WriteLog) L2_WriteLog(6,"EX_Decrease_TP: BUY ORDER-ticket:"+ticket+" EMT_MTP_LostPerc:"+EMT_MTP_LF[cnt]+" countSL:"+countSL+" EMT_MTP_MoveTP:"+EMT_MTP_TP[cnt]+" countTP:"+countTP) ;
                          DecreaseTP(ticket,OrderOpenPrice()+TMT_TP*Point-countTP);
                          break;
                      }     
                 } 
             if (OrderType()==OP_SELL && Bid-OrderOpenPrice()>=countTP)
                 {
                   if ((OrderOpenPrice()-TMT_TP*Point-Point)>(OrderTakeProfit()-countTP)) //move takeprofit
                      {
                          if(RCS_WriteLog) L2_WriteLog(6,"EX_Decrease_TP: SELL ORDER-ticket:"+ticket+" EMT_MTP_LostPerc:"+EMT_MTP_LF[cnt]+" countSL:"+countSL+" EMT_MTP_MoveTP:"+EMT_MTP_TP[cnt]+" countTP:"+countTP) ;
                          DecreaseTP(ticket,OrderOpenPrice()-TMT_TP*Point+countTP);
                          break;
                      }     
                  } 
                        
            } 
          return(true);  
   }// ******** end of a function 
   

bool EX_DecrTPifCountTrend(int ticket)
   {
          bool enableBuy=true,enableSell=true;
          Z_F6_BlockTradingFilter6(enableBuy,enableSell); 
          
          if(OrderType()==OP_BUY && enableBuy==false)//&& FTS_Difference>FTS_MaxDiff
            {
               double FTS_SR_high[],FTS_SR_Max;
               ArrayCopySeries(FTS_SR_high,MODE_HIGH,NULL,MovePeriod(EMT_DecTPShiftPeriod));
               FTS_SR_Max=FTS_SR_high[ArrayMaximum(FTS_SR_high,EMT_DecTPShiftBar,0)];  
               if(FTS_SR_Max-Bid>TMT_SL*Point && OrderTakeProfit()>FTS_SR_Max)// && OrderOpenPrice()+TMT_TP*Point<FTS_SR_Max 
                  {
                     if(DecreaseTP(ticket,FTS_SR_Max)) 
                        {
                           if(RCS_WriteLog) L2_WriteLog(4,StringConcatenate("EMT_DecreaseTP_On: Ticket Nr. ",ticket," has new TP=",FTS_SR_Max," because of a counter trend danger."));
                           return(true);
                        }  
                  }
             }
             
          if(OrderType()==OP_SELL && enableSell==false)
            {
               double FTS_SR_low[],FTS_SR_Min;
               ArrayCopySeries(FTS_SR_low,MODE_LOW,NULL,MovePeriod(EMT_DecTPShiftPeriod));
               FTS_SR_Min=FTS_SR_low[ArrayMinimum(FTS_SR_low,EMT_DecTPShiftBar,0)];  
               if(Bid-FTS_SR_Min>TMT_SL*Point && OrderTakeProfit()<FTS_SR_Min+MarketInfo(Symbol(),MODE_SPREAD)*Point)// && OrderOpenPrice()-TMT_TP*Point>FTS_SR_Min
                  {
                     if(DecreaseTP(ticket,FTS_SR_Min-MarketInfo(Symbol(),MODE_SPREAD)*Point))
                        {
                           if(RCS_WriteLog) L2_WriteLog(4,StringConcatenate("EMT_DecreaseTP_On: Ticket Nr. ",ticket," has new TP=",FTS_SR_Min-MarketInfo(Symbol(),MODE_SPREAD)*Point," because of a counter trend danger."));
                           return(true);
                        }  

                  }
             }
           return(true);   
   }
   
bool EX_CheckTREND_RSIHF(int ticket)
   {
          int Signal=EntrySignal_RSIHF(); 
          
          if(OrderType()==OP_BUY && Signal==Sell) // looks for short trend
            {
               if(RCS_WriteLog) L2_WriteLog(4,StringConcatenate("EX_CheckTREND_RSI_HF: Ticket Nr. ",ticket," is on counter trend because there is a short trend on multi currency indicator."));
               return(true);
            }
             
          if(OrderType()==OP_SELL && Signal==Buy) // looks for long trend
            {
               if(RCS_WriteLog) L2_WriteLog(4,StringConcatenate("EX_CheckTREND_RSI_HF: Ticket Nr. ",ticket," is on counter trend because there is a long trend on multi currency indicator."));
               return(true);
            } 
          return(false);   
   }
   
   
bool EX_CheckTREND_OBC1(int ticket)
   {
          int sum_move0=iCustom(TMT_Currency,0,"OBC",SGS_OBC1_P1,SGS_OBC1_P2,SGS_OBC1_P3,SGS_OBC1_P4,SGS_OBC1_Confirm1,SGS_OBC1_Confirm2,SGS_OBC1_Confirm3,SGS_OBC1_Confirm4,SGS_OBC1MaxSumConfirm,SGS_OBC1MinSumConfirm,true,SGS_OBC1Period,SGS_OBC1History,SGS_OBC1MinEntryDifference,SGS_OBC1CloseStepsBefore,SGS_OBC1CloseStepsOpposite,0,0);
          int exit=iCustom(TMT_Currency,0,"OBCE",SGS_OBC1_P1,SGS_OBC1_P2,SGS_OBC1_P3,SGS_OBC1_P4,SGS_OBC1_Confirm1,SGS_OBC1_Confirm2,SGS_OBC1_Confirm3,SGS_OBC1_Confirm4,SGS_OBC1MaxSumConfirm,SGS_OBC1MinSumConfirm,SGS_OBC1Period,SGS_OBC1History,SGS_OBC1MinEntryDifference,SGS_OBC1CloseStepsBefore,SGS_OBC1CloseStepsOpposite,SGS_OBC1TrendHistory,1,0);
          
          if(OrderType()==OP_BUY && exit==sum_move0+1) // close buy ticket
            {
               if(RCS_WriteLog) L2_WriteLog(4,StringConcatenate("EX_CheckTREND_OBC1: Ticket Nr. ",ticket," will  be closed."));
               return(true);
            }
          if(OrderType()==OP_SELL && exit==sum_move0-1) // close sell ticket
            {
               if(RCS_WriteLog) L2_WriteLog(4,StringConcatenate("EX_CheckTREND_OBC1: Ticket Nr. ",ticket,"  will  be closed."));
               return(true);
            }

          return(false);  //nothing to close 
   }

bool EX_CheckTREND_QM1(int ticket)
   {
          int signal;
          if(SGS_QM1_CalculatingMethod==0)signal=iCustom(TMT_Currency,0,"Quantum",SGS_QM1_ChangeOrientation,SGS_QM1History,0,0);
          if(SGS_QM1_CalculatingMethod==1)signal=-iCustom(TMT_Currency,0,"Quantum",SGS_QM1_ChangeOrientation,SGS_QM1History,2,0);
          
          if(OrderType()==OP_BUY && signal==Sell) // close buy ticket
            {
               if(RCS_WriteLog) L2_WriteLog(4,StringConcatenate("EX_CheckTREND_QM1: Ticket Nr. ",ticket," will  be closed."));
               return(true);
            }
          if(OrderType()==OP_SELL && signal==Buy) // close sell ticket
            {
               if(RCS_WriteLog) L2_WriteLog(4,StringConcatenate("EX_CheckTREND_QM1: Ticket Nr. ",ticket,"  will  be closed."));
               return(true);
            }

          if(RCS_WriteDebug) L3_WriteDebug(4,"EX_CheckTREND_QM1: signal:"+signal);
          return(false);  //nothing to close 
   }   
   
   
bool EX_CheckTREND_MAHF(int ticket)
   {
          int Signal=EntrySignal_MAHF(); 
          
          if(OrderType()==OP_BUY && Signal==Sell) // looks for short trend
            {
               if(RCS_WriteLog) L2_WriteLog(4,StringConcatenate("EX_CheckTREND_MAHF: Ticket Nr. ",ticket," is on counter trend because there is a short trend on multi currency indicator."));
               return(true);
            }
             
          if(OrderType()==OP_SELL && Signal==Buy) // looks for long trend
            {
               if(RCS_WriteLog) L2_WriteLog(4,StringConcatenate("EX_CheckTREND_MAHF: Ticket Nr. ",ticket," is on counter trend because there is a long trend on multi currency indicator."));
               return(true);
            } 
          return(false);   
   }
   
   
bool EX_CheckTREND_TSI(int ticket)
   {
          int Signal=EntrySignal_TSI(); 
          
          if(OrderType()==OP_BUY && Signal==Sell) // looks for short trend
            {
               if(RCS_WriteLog) L2_WriteLog(4,StringConcatenate("EX_CheckTREND_TSI: Ticket Nr. ",ticket," is on counter trend because there is a short trend on multi currency indicator."));
               return(true);
            }
             
          if(OrderType()==OP_SELL && Signal==Buy) // looks for long trend
            {
               if(RCS_WriteLog) L2_WriteLog(4,StringConcatenate("EX_CheckTREND_TSI: Ticket Nr. ",ticket," is on counter trend because there is a long trend on multi currency indicator."));
               return(true);
            } 
          return(false);   
   }  
   
   
int EX_RecrossCheck(int ticket)
   {
     if(EMT_RecrossBars==Bars) return(-1); //only once throught this function withing one Bar
     EMT_RecrossBars=Bars;
     if(RCS_WriteDebug) L3_WriteDebug(5,"EX_RecrossCheck: Starting EX_RecrossCheck function.");
     
     int index,cnt,cnt2;
     ArraySort(EMT_bcRecross,WHOLE_ARRAY,0,MODE_DESCEND);
     for(cnt=0;cnt<ArrayRange(EMT_bcRecross,0);cnt++)
      {
         if(EMT_bcRecross[cnt][0] == 0) continue;
         if(ticket!=EMT_bcRecross[cnt][0] && OrderSelect(EMT_bcRecross[cnt][0], SELECT_BY_TICKET,MODE_TRADES)==false) continue;
         if(OrderCloseTime() != 0) 
            {
              if(RCS_WriteDebug) L3_WriteDebug(5,"EX_RecrossCheck: Erase ticket : "+EMT_bcRecross[cnt][0]+" from recrosses pool.");              
              for(cnt2=0;cnt2<ArrayDimension(EMT_bcRecross);cnt2++) EMT_bcRecross[cnt][cnt2]=0;
            }   

         if(EMT_bcRecross[cnt][0] ==ticket)
            {
               if(OrderSelect(ticket, SELECT_BY_TICKET, MODE_TRADES)) if(RCS_WriteDebug) L3_WriteDebug(5,"EX_RecrossCheck: Start checking open order."); //it needs to be after Lasttrade function   
               if(OrderType()==OP_BUY &&  EMT_bcRecross[cnt][2]<Bid) EMT_bcRecross[cnt][2]=Bid;    
               if(OrderType()==OP_SELL && EMT_bcRecross[cnt][2]>Ask) EMT_bcRecross[cnt][2]=Ask;
               if(OrderType()==OP_BUY &&  EMT_bcRecross[cnt][5]>Bid) 
                  {
                     EMT_bcRecross[cnt][5]=Bid;
                     EMT_bcRecross[cnt][3]=EX_CountRecross(ticket,(EMT_bcRecross[index][5]-OrderOpenPrice())/ 2+OrderOpenPrice());  
                     if(RCS_WriteDebug) L3_WriteDebug(5,"EX_RecrossCheck : Ticket : "+ticket+", EMT_bcRecross[index][5]"+EMT_bcRecross[cnt][5]);
                  }     
               if(OrderType()==OP_SELL && EMT_bcRecross[cnt][5]<Ask) 
                  {
                     EMT_bcRecross[cnt][5]=Ask;
                     EMT_bcRecross[cnt][3]=EX_CountRecross(ticket,OrderOpenPrice()-(OrderOpenPrice()-EMT_bcRecross[index][5])/ 2);
                     if(RCS_WriteDebug) L3_WriteDebug(5,"EX_RecrossCheck : Ticket : "+ticket+", EMT_bcRecross[index][5]"+EMT_bcRecross[cnt][5]);
                  }
               return(cnt);      
            }   
       }      
      if(OrderSelect(ticket, SELECT_BY_TICKET, MODE_TRADES)) if(RCS_WriteLog) L2_WriteLog(5,"EX_RecrossCheck : Ticket : "+ticket+" was not founded recrosses pool."); //it needs to be after previous orderselect  
      for(index=0;index<ArrayRange(EMT_bcRecross,0);index++) if(EMT_bcRecross[index][0]==0) break;
      if(EMT_bcRecross[index][0] !=0)
         {
            if(RCS_WriteLog) L2_WriteLog(5,"EX_RecrossCheck : Error -no place in array");
            return(-1);
         }  
          
      EMT_bcRecross[index][0]=ticket; //holds Nr.of a ticket
      EMT_bcRecross[index][1]=1;       //holds count of recross throught an upper S/R price
      EMT_bcRecross[index][2]=OrderOpenPrice();    //holds max reached price of the ticket
      EMT_bcRecross[index][3]=Bars;       //holds last sum of bars when upper S/R was recrossed 
      EMT_bcRecross[index][4]=1;       //holds count of recross throught a lower S/R price      
      EMT_bcRecross[index][5]=OrderOpenPrice();    //holds min reached price of the ticket
      EMT_bcRecross[index][6]=Bars;       //holds last sum of bars when lower S/R was recrossed 

      return(index);
    }  


bool EX_RecrossExit(int ticket,int poolPos)
   {
      double price,requiredPrice,comparePrice,replacedOP;
      bool crossingCheck=false;
      int cntRecross;
      if (OrderType() == OP_BUY)   
         {
            price=Bid;
            comparePrice=(EMT_bcRecross[poolPos][2]-EMT_bcRecross[poolPos][5])/2+EMT_bcRecross[poolPos][5];
            if(comparePrice<OrderOpenPrice()) //
               {
                  cntRecross=EMT_bcRecross[poolPos][4];
                  requiredPrice=( EMT_bcRecross[poolPos][2]-comparePrice)*EMT_RecrossCoefBad + comparePrice;
                  if(RCS_WriteDebug) L3_WriteDebug(5,"EX_RecrossExit: Ticket : "+ticket+", lower S/R was reached "+cntRecross+"x");
               }   
            else 
               {
                  cntRecross=EMT_bcRecross[poolPos][1];
                  requiredPrice=( EMT_bcRecross[poolPos][2]-comparePrice)*EMT_RecrossCoefGood + comparePrice;
                  if(RCS_WriteDebug) L3_WriteDebug(5,"EX_RecrossExit: Ticket : "+ticket+", upper S/R was reached "+cntRecross+"x");
               }   
            if(requiredPrice>=price && EMT_bcRecross[poolPos][4]>EMT_bcRecross[poolPos][1]) crossingCheck=true;
         }   
      if (OrderType() == OP_SELL) 
         {
            price=Ask;
            comparePrice=EMT_bcRecross[poolPos][5]-(EMT_bcRecross[poolPos][2]-EMT_bcRecross[poolPos][5])/2;
            if(comparePrice>OrderOpenPrice()) 
               {
                  cntRecross=EMT_bcRecross[poolPos][4];
                  requiredPrice=comparePrice-( comparePrice-EMT_bcRecross[poolPos][2] )*EMT_RecrossCoefBad;
                  if(RCS_WriteDebug) L3_WriteDebug(5,"EX_RecrossExit: Ticket : "+ticket+", lower S/R was reached "+cntRecross+"x");
               }                    
            else 
               {  cntRecross=EMT_bcRecross[poolPos][1];
                  requiredPrice=comparePrice-( comparePrice-EMT_bcRecross[poolPos][2] )*EMT_RecrossCoefGood;
                  if(RCS_WriteDebug) L3_WriteDebug(5,"EX_RecrossExit: Ticket : "+ticket+", upper S/R was reached "+cntRecross+"x");
               }   
            if(requiredPrice<=price  && EMT_bcRecross[poolPos][4]>EMT_bcRecross[poolPos][1]) crossingCheck=true;
         }

     if ( cntRecross>=EMT_RecrossMax && crossingCheck)
          {
           if(A1_4_IsTradePossible())
               {
                  for(int cnt2=0;cnt2<7;cnt2++) if(RCS_WriteDebug) L3_WriteDebug(6,StringConcatenate("EMT_RecrossMax-ticket : ",ticket," position : ",cnt2," value : ",EMT_bcRecross[poolPos][cnt2]));
                  if(RCS_WriteDebug) L3_WriteDebug(5,"EX_RecrossExit: Ticket :"+ticket+" lots:"+OrderLots()+" price: "+price);
                  if(OrderClose(ticket,OrderLots(),price,TMT_Slippage,Red)) return(true);
                  else L4_WriteError();
               }   
         }   
         
        if(Bars>EMT_bcRecross[poolPos][3]) //Bar hasn't been counted yet
         { 
            
            if(OrderType()==OP_BUY) replacedOP=NormalizeDouble((EMT_bcRecross[poolPos][2]-OrderOpenPrice())/ 2+OrderOpenPrice(),Digits );    
            else                    replacedOP=NormalizeDouble(OrderOpenPrice()-(OrderOpenPrice()-EMT_bcRecross[poolPos][2])/ 2,Digits );
            if( replacedOP == NormalizeDouble(price,Digits))
                {
                  EMT_bcRecross[poolPos][1]++;
                  EMT_bcRecross[poolPos][3]=Bars;
                }  
          }  
          
        if(Bars>EMT_bcRecross[poolPos][6]) //Bar hasn't been counted yet
          { 
            
            if(OrderType()==OP_BUY) replacedOP=NormalizeDouble((OrderOpenPrice()-EMT_bcRecross[poolPos][5])/2 +EMT_bcRecross[poolPos][5],Digits );    
            else                    replacedOP=NormalizeDouble(EMT_bcRecross[poolPos][5]-(EMT_bcRecross[poolPos][5]-OrderOpenPrice())/ 2,Digits );
            if( replacedOP == NormalizeDouble(price,Digits))
                {
                  EMT_bcRecross[poolPos][4]++;
                  EMT_bcRecross[poolPos][6]=Bars;
                }  
           }  
        
      return(false);
    }

int EX_CountRecross(int ticket,double price)
   {
       double FTS_SR_high[],FTS_SR_low[];
       double spread=MarketInfo(Symbol(),MODE_SPREAD)*Point;
       int recross;
       ArrayCopySeries(FTS_SR_high,MODE_HIGH,NULL,0);
       ArrayCopySeries(FTS_SR_low,MODE_LOW,NULL,0); 
       int timeBack=Bars-iBarShift(NULL,0,OrderOpenTime(),false);
       
       for(int cnt=0;cnt<=timeBack;cnt++)
         {
           if(OrderType()==OP_BUY && price>=FTS_SR_low[cnt] && price<=FTS_SR_high[cnt]) recross++;
           if(OrderType()==OP_SELL && price>=FTS_SR_low[cnt]+spread && price<=FTS_SR_high[cnt]+spread) recross++;           
         }
       return(recross);    
    }   


bool TextIntoArray (string textVar, string nameVar,int& variable[],int lenght)
   {
      string parametr,message;
      double val;
      if(StringLen(textVar)!=lenght*ArraySize(variable))
         {
            message=nameVar+" has wrong initial settings given by user! It uses predefined values.";
            Alert(message);
            if(RCS_WriteLog) L2_WriteLog(8,message);  
            return(false);
         } 
      for (int cnt=0;cnt<ArraySize(variable);cnt++)
         {
            parametr=StringSubstr(textVar,0,lenght);
            val=StrToDouble(parametr);
            variable[cnt]=val;
            textVar=StringSubstr(textVar,lenght,StringLen(textVar));
         }   
      return(true);
    }   

    
          
bool MM_VarFromFile()  
{

   int pass,newpass=-1;
   string variable,file,message;
   int futureset,fsize;
   double pointer;
   
   file=StringConcatenate("Sets-",TMT_Currency,"-",TMT_Period,".csv");
   int handle=FileOpen(file,FILE_CSV|FILE_SHARE_READ,';');

   if (handle<1) 
      {
        int error=GetLastError();
        if(RCS_WriteLog) L2_WriteLog(4,"MM_VarFromFile:Error:"+error+" File "+file+" does not exist !");
        return(false);
      } 
   
   futureset=SMT_NextSet;
   if (SMT_SetCount>0)
      {
         fsize=FileSize(handle);
         pointer=((fsize*SMT_NextSet)/SMT_SetCount)-5000; //for quicker running set a starting point near to SMT_CurrentPass
         if(pointer>0) 
          {
             FileSeek(handle,pointer,SEEK_SET);
             while(FileIsLineEnding(handle)==false) variable=FileReadString(handle);
          }
      }  

         
   while(FileIsEnding(handle)==false && pass<=futureset && newpass==-1)
      {
        pass=StrToInteger(FileReadString(handle));
        if (futureset==0) futureset=pass;
        
        while(FileIsLineEnding(handle)==false && FileIsEnding(handle)==false) 
         {
           variable=FileReadString(handle);
           if (pass==futureset) 
            {
              MM_FindVar(variable);
            }  
         }
        if (pass==futureset) 
         {
           variable=FileReadString(handle);
           if (FileIsEnding(handle)==false) newpass=StrToInteger(variable);
           else 
            {
                   FileSeek(handle,0,SEEK_SET);
                   newpass=StrToInteger(FileReadString(handle));
            }  
         }
      }
   SMT_NextSet=newpass; 

   message=StringConcatenate(message,"SMT_CheckSetBeforeLoading: The next set will be ",SMT_NextSet);
   if(RCS_WriteLog) L2_WriteLog(4,message);
   FileClose(handle);
 
   if(TMT_TPfromSL_1On) TMT_TP=TMT_SL+TMT_ADDtoSLforTP; //count with different TP then in startup
 
   return(true);
}

bool MM_FindVar(string variable)
{
  int pos=StringFind(variable,"=",0);
  if (pos==-1) return(false);
  string var=StringSubstr(variable,0,pos);
  string value=StringSubstr(variable,pos+1,StringLen(variable)-1);
  double val=StrToDouble(value);
  
 // Print (variable," var : ",var," val : ",val);
 /* if(SMT_NextSet==101) 
   {
      int handle=FileOpen("zk.csv",FILE_CSV|FILE_READ|FILE_WRITE,';');
      FileSeek(handle,0,SEEK_END);
      FileWrite(handle,variable," var : ",var," val : ",val);
      FileClose(handle);
   }   
 */  
    
          if(var=="TMT_MaxCntTrades_1account") TMT_MaxCntTrades_1account=val;
          if(var=="TMT_MaxCntTrades_1EA") TMT_MaxCntTrades_1EA=val;
          if(var=="TMT_MaxInDirection") TMT_MaxInDirection=val;
          if(var=="TMT_TimeShiftOrder") TMT_TimeShiftOrder=val;
          if(var=="TMT_SignalsRepeat") TMT_SignalsRepeat=val;
          if(var=="TMT_Slippage") TMT_Slippage=val;
          if(var=="TMT_TP") TMT_TP=val;
          if(var=="TMT_SL") TMT_SL=val;
          if(var=="TMT_ADDtoSLforTP") TMT_ADDtoSLforTP=val;
          if(var=="MMT_Lots") MMT_Lots=val;
          if(var=="MMT_MinLot") MMT_MinLot=val;
          if(var=="MMT_MaxLot") MMT_MaxLot=val;
          if(var=="MMT_MaxRisk") MMT_MaxRisk=val;
          if(var=="MMC_IncreseFactor") MMC_IncreseFactor=val;
          if(var=="SMT_NextSet") SMT_NextSet=val;
          if(var=="SMT_SetCount") SMT_SetCount=val;
          if(var=="EMT_DecTPShiftBar") EMT_DecTPShiftBar=val;
          if(var=="EMT_DecTPShiftPeriod") EMT_DecTPShiftPeriod=val;
          if(var=="EMT_TrendExtendTP_DISTtoTP") EMT_TrendExtendTP_DISTtoTP=val;
          if(var=="EMT_TS_pipsDISTANCE") EMT_TS_pipsDISTANCE=val;
          if(var=="EMT_MoveTPonTS") EMT_MoveTPonTS=val;
          if(var=="EMT_Start_DelayTS") EMT_Start_DelayTS=val;
          if(var=="EMT_RecrossMax") EMT_RecrossMax=val;
          if(var=="EMT_RecrossCoefGood") EMT_RecrossCoefGood=val;
          if(var=="EMT_RecrossCoefBad") EMT_RecrossCoefBad=val;
          if(var=="EXF_ForceClose_Hours") EXF_ForceClose_Hours=val;
          if(var=="EXF_FridayTimeClose") EXF_FridayTimeClose=val;
          if(var=="SGS_RSI_HF_High") SGS_RSI_HF_High=val;
          if(var=="SGS_RSI_HF_Low") SGS_RSI_HF_Low=val;
          if(var=="SGS_RSI_HF_Per") SGS_RSI_HF_Per=val;
          if(var=="SGS_RSI_HF_TimeFrameShift") SGS_RSI_HF_TimeFrameShift=val;
          if(var=="SGS_MA_HF_Period") SGS_MA_HF_Period=val;
          if(var=="SGS_MA_HF_TimeFrameShift") SGS_MA_HF_TimeFrameShift=val;
          if(var=="SGS_MA_HF_Shift") SGS_MA_HF_Shift=val;
          if(var=="SGS_OBC1MaxSumConfirm") SGS_OBC1MaxSumConfirm=val;
          if(var=="SGS_OBC1MinSumConfirm") SGS_OBC1MinSumConfirm=val;
          if(var=="SGS_OBC1Period") SGS_OBC1Period=val;
          if(var=="SGS_OBC1History") SGS_OBC1History=val;
          if(var=="SGS_OBC1MinEntryDifference") SGS_OBC1MinEntryDifference=val;
          if(var=="SGS_OBC1CloseStepsBefore") SGS_OBC1CloseStepsBefore=val;
          if(var=="SGS_OBC1CloseStepsOpposite") SGS_OBC1CloseStepsOpposite=val;
          if(var=="SGS_OBC1TrendHistory") SGS_OBC1TrendHistory=val;
          if(var=="SGS_OBC1_OP1") SGS_OBC1_OP1=val;
          if(var=="SGS_OBC1_OP2") SGS_OBC1_OP2=val;
          if(var=="SGS_OBC1_OP3") SGS_OBC1_OP3=val;
          if(var=="SGS_OBC1_OP4") SGS_OBC1_OP4=val;
          if(var=="SGS_OBC1_OConfirm1") SGS_OBC1_OConfirm1=val;
          if(var=="SGS_OBC1_OConfirm2") SGS_OBC1_OConfirm2=val;
          if(var=="SGS_OBC1_OConfirm3") SGS_OBC1_OConfirm3=val;
          if(var=="SGS_OBC1_OConfirm4") SGS_OBC1_OConfirm4=val;
          if(var=="FTS_SunAt") FTS_SunAt=val;
          if(var=="FTS_SunTo") FTS_SunTo=val;
          if(var=="FTS_FriAt") FTS_FriAt=val;
          if(var=="FTS_FriTo") FTS_FriTo=val;
          if(var=="FTS_MonAt") FTS_MonAt=val;
          if(var=="FTS_MonTo") FTS_MonTo=val;
          if(var=="FTS_WeekAt1") FTS_WeekAt1=val;
          if(var=="FTS_WeekTo1") FTS_WeekTo1=val;
          if(var=="FTS_WeekAt2") FTS_WeekAt2=val;
          if(var=="FTS_WeekTo2") FTS_WeekTo2=val;
          if(var=="FTS_HorizDist") FTS_HorizDist=val;
          if(var=="FTS_Distance") FTS_Distance=val;
          if(var=="FTS_TimePerShift") FTS_TimePerShift=val;
          if(var=="FTS_MaxDiff") FTS_MaxDiff=val;
          if(var=="FTS_MinSlope") FTS_MinSlope=val;
          if(var=="EXT_TC_SetSize") EXT_TC_SetSize=val;
          if(var=="EXT_TC_Relevant") EXT_TC_Relevant=val;
          if(var=="EXT_TC_PipsToClose") EXT_TC_PipsToClose=val;
          if(var=="FTS_BarsBack") FTS_BarsBack=val;
          if(var=="FTS_BarsForward") FTS_BarsForward=val;
          if(var=="FTS_PeakPerc") FTS_PeakPerc=val;
          if(var=="GBV_MagicNumSet") GBV_MagicNumSet=val;
    
   

  return(true);
}

//End Of File

