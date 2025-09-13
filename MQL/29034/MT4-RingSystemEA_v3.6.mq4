//===============================================================================================================================================================================================================================================================//
//Start code
//===============================================================================================================================================================================================================================================================//
#property copyright   "Copyright 2012-2020, Nikolaos Pantzos"
#property link        "https://www.mql5.com/en/users/pannik"
#property version     "3.6"
#property description "It's a multi currency system (use and need at least 3 pairs, which created from 3 currencies, to trade)."
#property description "Expert can to use maximum 8 currencies to make 28 pairs from which it creates 56 rings."
#property description "It is important the order of currencies be from the strongest to the weakest."
#property description "Strongest... EUR/GBP/AUD/NZD/USD/CAD/CHF/JPY ...weakest."
#property description "Attach expert in one chart only (no matter what pair or time frame)."
#property description "To better illustrate of graphics, select screen resolution 1280x1024."
//#property icon        "\\Images\\RS_EA_Logo.ico";
#property strict
//===============================================================================================================================================================================================================================================================//
enum Oper {Stand_by_Mode, Normal_Operation, Close_In_Profit_And_Stop, Close_Immediately_All_Orders};
enum Side {Open_Only_Plus, Open_Only_Minus, Open_Plus_And_Minus};
enum Step {Not_Open_In_Loss, Open_With_Manual_Step, Open_With_Auto_Step};
enum ProgrS {Statical_Step, Geometrical_Step, Exponential_Step};
enum CloseP {Single_Ticket, Basket_Ticket, Pair_By_Pair};
enum CloseL {Whole_Ticket, Partial_Ticket, Not_Close_In_Loss};
enum ProgrL {Statical_Lot, Geometrical_Lot, Exponential_Lot, Decreases_Lot};
//===============================================================================================================================================================================================================================================================//
#define PairsPerGroup 3
#define MagicSet      1230321
//===============================================================================================================================================================================================================================================================//
extern string OperationStr       = "||---------- Operation Set ----------||";//___ Exteral Settings_1 ___
extern Oper   TypeOfOperation    = Normal_Operation;//Type Of Operation Mode
extern int    TimerInMillisecond = 1000;//Timer In Millisecond For Events
extern string ManagePairsUse     = "||---------- Manage Pairs And Side ----------||";//___ Exteral Settings_2 ___
extern string CurrenciesTrade    = "EUR/GBP/AUD/NZD/USD/CAD/CHF/JPY";//Currencies To Make Pairs
extern string NoOfGroupToSkip    = "57,58,59,60";//No Of Groups To Skip
extern Side   SideOpenOrders     = Open_Plus_And_Minus;//Side Open Orders
extern string ManageOpenOrders   = "||---------- Manage Open Orders ----------||";//___ Exteral Settings_4 ___
extern Step   OpenOrdersInLoss   = Open_With_Manual_Step;//Open Orders In Loss Mode
extern double StepOpenNextOrders = 200.0;//Step For Next Orders (Value $/Lot)
extern ProgrS StepOrdersProgress = Statical_Step;//Type Of Progress Step
extern int    MinutesForNextOrder= 60;//Minutes Between Orders
extern int    MaximumGroups      = 0;//Max Opened Groups (0=Not Limit)
extern string ManageCloseProfit  = "||---------- Manage Close Profit Orders ----------||";//___ Exteral Settings_5 ___
extern CloseP TypeCloseInProfit  = Single_Ticket;//Type Of Close In Profit Orders
extern double TargetCloseProfit  = 200.0;//Target Close In Profit (Value $/Lot)
extern int    DelayCloseProfit   = 1;//Delay Before Close In Profit (Value Ticks)
extern string ManageCloseLoss    = "||---------- Manage Close Losses Orders ----------||";//___ Exteral Settings_6 ___
extern CloseL TypeCloseInLoss    = Not_Close_In_Loss;//Type Of Close In Loss Orders
extern double TargetCloseLoss    = 1000.0;//Target Close In Loss (Value $/Lot)
extern int    DelayCloseLoss     = 1;//Delay Before Close In Loss (Value Ticks)
extern string MoneyManagement    = "||---------- Money Management ----------||";//___ Exteral Settings_7 ___
extern bool   AutoLotSize        = true;//Use Auto Lot Size
extern double RiskFactor         = 5.0;//Risk Factor For Auto Lot Size
extern double ManualLotSize      = 0.01;//Manual Lot Size
extern ProgrL LotOrdersProgress  = Statical_Lot;//Type Of Progress Lot
extern bool   UseFairLotSize     = false;//Use Fair Lot Size For Each Pair
extern double MaximumLotSize     = 0.0;//Max Lot Size (0=Not Limit)
extern string ControlSessionSet  = "||---------- Control Session ----------||";//___ Exteral Settings_8 ___
extern bool   ControlSession     = false;//Use Trade Control Session
extern int    WaitAfterOpen      = 60;//Wait After Monday Open
extern int    StopBeforeClose    = 60;//Stop Before Friday Close
extern string InfoOnTheScreen    = "||---------- Info On The Screen ----------||";//___ Exteral Settings_9 ___
extern bool   ShowPairsInfo      = true;//Show Pairs Info On Screen
extern color  ColorOfTitle       = clrKhaki;//Color Of Titles
extern color  ColorOfInfo        = clrBeige;//Color Of Info
extern color  ColorLineTitles    = clrOrange;//Color Of Line Titles
extern color  ColorOfLine1       = clrMidnightBlue;//Color Of Line 1
extern color  ColorOfLine2       = clrDarkSlateGray;//Color Of Line 2
extern int    PositionOrders     = 485;//Position 'Orders' Info
extern int    PositionPnL        = 580;//Position 'PnL' Info
extern int    PositionClose      = 645;//Position 'Close' Info
extern int    PositionNext       = 785;//Position 'Next' Info
extern int    PositionHistory    = 900;//Position 'History' Info
extern int    PositionMaximum    = 1000;//Position 'Maximum' Info
extern int    PositionSpread     = 1125;//Position 'Spread' Info
extern string Limitations        = "||---------- Limitations ----------||";//___ Exteral Settings_10 ___
extern double MaxSpread          = 0.0;//Max Accepted Spread (0=Not Check)
extern long   MaximumOrders      = 0;//Max Total Opened Orders (0=Not Limit)
extern int    MaxSlippage        = 3;//Max Accepted Slippage
extern string Configuration      = "||---------- Configuration ----------||";//___ Exteral Settings_11 ___
extern string SymbolPrefix       = "NONE";//Add Symbol Prefix
extern string SymbolSuffix       = "AUTO";//Add Symbol Suffix
extern int    MagicNumber        = 0;//Orders' ID (0=Generate Automatically)
extern bool   SetChartUses       = true;//Set Automatically Chart To Use
extern bool   CheckOrders        = true;//Check All Orders
extern bool   ShowTaskInfo       = true;//Show On Chart Information
extern bool   PrintLogReport     = false;//Print Log Report
extern string StringOrdersEA     = "RingSystemEA";//Comment For Orders
extern bool   SetChartInterface  = true;//Set Chart Appearance
extern bool   SaveInformations   = false;//Save Groups Informations
//===============================================================================================================================================================================================================================================================//
string SymPrefix;
string SymSuffix;
string CommentsEA;
string WarningPrint="";
string SymbolStatus[99][4];
string SkippedStatus[99];
//---------------------------------------------------------------------
double BidPricePair[99][4];
double SumSpreadGroup[99];
double FirstLotPlus[99][4];
double FirstLotMinus[99][4];
double LastLotPlus[99][4];
double LastLotMinus[99][4];
double CheckMargin[99];
double TotalProfitPlus[99][4];
double TotalProfitMinus[99][4];
double MaxProfit=-99999;
double MinProfit=99999;
double LevelProfitClosePlus[99];
double LevelProfitCloseMinus[99];
double LevelProfitClosePairPlus[99][4];
double LevelProfitClosePairMinus[99][4];
double LevelLossClosePlus[99];
double LevelLossCloseMinus[99];
double LevelOpenNextPlus[99];
double LevelOpenNextMinus[99];
double HistoryTotalProfitLoss;
double HistoryTotalPips;
double TotalLotPlus[99][4];
double TotalLotMinus[99][4];
double MultiplierStepPlus[99];
double MultiplierStepMinus[99];
double MultiplierLotPlus[99];
double MultiplierLotMinus[99];
double SumSpreadValuePlus[99];
double SumSpreadValueMinus[99];
double TotalCommissionPlus[99][4];
double TotalCommissionMinus[99][4];
double FirstProfitPlus[99][4];
double FirstProfitMinus[99][4];
double MaxFloating[99];
double TotalProfitLoss;
double TotalLots;
double FirstTotalLotPlus[99];
double FirstTotalLotMinus[99];
double FirstTotalProfitPlus[99];
double FirstTotalProfitMinus[99];
double SumProfitPlus[99];
double SumProfitMinus[99];
double SumCommissionPlus[99];
double SumCommissionMinus[99];
double SumLotPlus[99];
double SumLotMinus[99];
double SpreadPair[99][4];
double SpreadValuePlus[99][4];
double SpreadValueMinus[99][4];
double HistoryPlusProfit[99];
double HistoryMinusProfit[99];
double TotalGroupsProfit=0;
double TickValuePair[99][4];
double FirstLotPair[99][4];
double TotalGroupsSpread;
double MaxTotalLots=0;
//---------------------------------------------------------------------
int i;
int j;
int k;
int MagicNo;
int CountComma;
int OrdersID[99];
int TicketNo[99];
int DecimalsPair;
int LenPrefix=0;
int MaxTotalOrders=0;
int MultiplierPoint;
int FirstTicketPlus[99][4];
int FirstTicketMinus[99][4];
int LastTicketPlus[99][4];
int LastTicketMinus[99][4];
int TotalOrdersPlus[99][4];
int TotalOrdersMinus[99][4];
int HistoryTotalTrades;
int HistoryPlusOrders[99];
int HistoryMinusOrders[99];
int SuitePlus[99][4];
int SuiteMinus[99][4];
int CntTry;
int CntTick=0;
int CheckTicksOpenMarket;
int DelayTimesForCloseInLossPlus[99];
int DelayTimesForCloseInLossMinus[99];
int DelayTimesForCloseInProfitPlus[99];
int DelayTimesForCloseInProfitMinus[99];
int DelayTimesForCloseBasketProfit[99];
int DelayTimesForCloseBasketLoss[99];
int CountAllOpenedOrders;
int CountAllHistoryOrders;
int LastHistoryOrders=0;
int WarningMessage;
int GetCurrencyPos[99];
int MaxOrders[99];
int TotalOrders;
int NumberCurrenciesTrade;
int SumOrdersPlus[99];
int SumOrdersMinus[99];
int GroupsPlus[99];
int GroupsMinus[99];
int TotalGroupsOrders=0;
int NumberGroupsSkip[99];
int FindComma[200];
int PositionSkipped=0;
int DecimalsGet=0;
int CountSkippedGroups;
int GetGroupUnUse[99];
int SignalsMessageWarning;
int LastHourSaved=0;
//---------------------------------------------------------------------
bool SpreadOK[99];
bool OrdersIsOK[99];
bool CommentWarning;
bool CountHistory=false;
bool StopWorking=false;
bool WrongSet=false;
bool WrongPairs=false;
bool MarketIsOpen=false;
bool CallMain=false;
bool ChangeOperation=false;
bool ExpertCloseBasketInProfit[99];
bool ExpertCloseBasketInLoss[99];
bool ExpertClosePlusInLoss[99];
bool ExpertClosePlusInProfit[99];
bool ExpertClosePairPlusInProfit[99][4];
bool ExpertCloseMinusInLoss[99];
bool ExpertCloseMinusInProfit[99];
bool ExpertClosePairMinusInProfit[99][4];
bool SkipGroup[99];
bool FirsOrdersPlusOK[99];
bool FirsOrdersMinusOK[99];
bool LimitOfOrdersOk;
//---------------------------------------------------------------------
datetime TimeBegin;
datetime TimeEnd;
datetime ChcekLockedDay=0;
datetime DiffTimes;
datetime StartTime;
datetime TimeOpenLastPlus[99];
datetime TimeOpenLastMinus[99];
datetime FirstOpenedOrder;
//---------------------------------------------------------------------
int NumberGroupsTrade=0;
string SymbolPair[99][4];
int Position[9];
//---------------------------------------------------------------------
long ChartColor;
//---------------------------------------------------------------------
bool LockedDate=false;
datetime ExpiryDate=D'31.12.2020';
bool LockedAccount=false;
int AccountNo=123456;
//===============================================================================================================================================================================================================================================================//
//OnInit function
//===============================================================================================================================================================================================================================================================//
int OnInit()
  {
//---------------------------------------------------------------------
//Set chart
   if(SetChartInterface==true)
     {
      ChartSetInteger(0,CHART_COLOR_BACKGROUND,clrBlack);//Set chart color
      ChartSetInteger(0,CHART_SHOW_GRID,false);//Hide grid
      ChartSetInteger(0,CHART_SHOW_VOLUMES,false);//Hide volume
      ChartSetInteger(0,CHART_SHOW_ASK_LINE,false);//Hide ask line
      ChartSetInteger(0,CHART_SHOW_BID_LINE,false);//Hide bid line
      ChartSetInteger(0,CHART_MODE,0);//Set price in bars
      ChartSetInteger(0,CHART_SCALE,1);//Set scale
      ChartSetInteger(0,CHART_SHOW_VOLUMES,CHART_VOLUME_HIDE);//Hide value
      ChartSetInteger(0,CHART_COLOR_CHART_UP,clrNONE);//Hide line up
      ChartSetInteger(0,CHART_COLOR_CHART_DOWN,clrNONE);//Hide line down
      ChartSetInteger(0,CHART_COLOR_CHART_LINE,clrNONE);//Hide chart line
      ChartSetInteger(0,CHART_AUTOSCROLL,true);//Autoscroll
      ChartSetInteger(0,CHART_SHIFT,true);//Set the indent of the right border of the chart
     }
//---------------------------------------------------------------------
//Set background
   ChartColor=ChartGetInteger(0,CHART_COLOR_BACKGROUND,0);
   if(ObjectFind(0,"Background")==-1)
      ChartBackground("Background",(color)ChartColor,BORDER_FLAT,false,0,16,240,274);
//---------------------------------------------------------------------
//Stop run expert on tester
   /*if((IsTesting())||(IsVisualMode())||(IsOptimization()))
     {
      Comment("\n "+StringOrdersEA+
              "\n\n\n      Expert Can't make backtest"+
              "\n\n      use it on real time!!!");
      //---
      Print("<========================= Sorry, expert can't make any backtest!!! =========================>");
      StopWorking=true;
      Sleep(30000);
      return(INIT_FAILED);
     }*/
//---------------------------------------------------------------------
//Confirm ranges and sets
   if(RiskFactor<0.01)
      RiskFactor=0.01;
   if(RiskFactor>100.0)
      RiskFactor=100.0;
   if(DelayCloseProfit<1)
      DelayCloseProfit=1;
   if(DelayCloseProfit>60)
      DelayCloseProfit=60;
   if(DelayCloseLoss<1)
      DelayCloseLoss=1;
   if(DelayCloseLoss>60)
      DelayCloseLoss=60;
   if(MagicNumber<0)
      MagicNumber=0;
   if(MaximumGroups<0)
      MaximumGroups=0;
   if(MaximumOrders<0)
      MaximumOrders=0;
   if(MaxSlippage<1)
      MaxSlippage=1;
   if(MaxSpread<0)
      MaxSpread=0;
   if(WaitAfterOpen<0)
      WaitAfterOpen=0;
   if(StopBeforeClose<0)
      StopBeforeClose=0;
   if(MaximumLotSize<0.0)
      MaximumLotSize=0.0;
   if(MinutesForNextOrder<0)
      MinutesForNextOrder=0;
//---------------------------------------------------------------------
//Reset value
   ArrayInitialize(SkipGroup,false);
   ArrayInitialize(OrdersIsOK,true);
   ArrayInitialize(ExpertCloseBasketInProfit,false);
   ArrayInitialize(ExpertCloseBasketInLoss,false);
   ArrayInitialize(ExpertClosePlusInLoss,false);
   ArrayInitialize(ExpertClosePlusInProfit,false);
   ArrayInitialize(ExpertClosePairPlusInProfit,false);
   ArrayInitialize(ExpertCloseMinusInLoss,false);
   ArrayInitialize(ExpertCloseMinusInProfit,false);
   ArrayInitialize(ExpertClosePairMinusInProfit,false);
   ArrayInitialize(DelayTimesForCloseInLossPlus,0);
   ArrayInitialize(DelayTimesForCloseInLossMinus,0);
   ArrayInitialize(DelayTimesForCloseInProfitPlus,0);
   ArrayInitialize(DelayTimesForCloseInProfitMinus,0);
   ArrayInitialize(DelayTimesForCloseBasketProfit,0);
   ArrayInitialize(DelayTimesForCloseBasketLoss,0);
   ArrayInitialize(OrdersID,0);
   ArrayInitialize(MaxOrders,0);
   ArrayInitialize(Position,0);
   ArrayInitialize(MaxFloating,99999);
   ArrayInitialize(NumberGroupsSkip,-1);
   ArrayInitialize(GetGroupUnUse,-1);
   ArrayInitialize(FindComma,0);
   CountSkippedGroups=0;
   CheckTicksOpenMarket=0;
   NumberCurrenciesTrade=0;
   PositionSkipped=0;
   WrongPairs=false;
   CntTick=0;
   CountComma=0;
//---------------------------------------------------------------------
//Symbol prefix and suffix
   if(SymbolPrefix=="NONE")
      SymPrefix="";
   if(SymbolPrefix!="NONE")
     {
      SymPrefix=SymbolPrefix;
      LenPrefix=StringLen(SymPrefix);
     }
//---
   if(SymbolSuffix=="AUTO")
     {
      if(StringLen(Symbol())>6+LenPrefix)
         SymSuffix=StringSubstr(Symbol(),6+LenPrefix);
     }
   if(SymbolSuffix!="AUTO")
      SymSuffix=SymbolSuffix;
//---------------------------------------------------------------------
//Comments orders
   if(StringOrdersEA=="")
      CommentsEA=WindowExpertName();
   else
      CommentsEA=StringOrdersEA;
//---------------------------------------------------------------------
//Set up pairs
   NumberCurrenciesTrade=((StringLen(CurrenciesTrade)+1)/4);
//---Set numbers of Groups
   if(NumberCurrenciesTrade==3)
      NumberGroupsTrade=1;
   if(NumberCurrenciesTrade==4)
      NumberGroupsTrade=4;
   if(NumberCurrenciesTrade==5)
      NumberGroupsTrade=10;
   if(NumberCurrenciesTrade==6)
      NumberGroupsTrade=20;
   if(NumberCurrenciesTrade==7)
      NumberGroupsTrade=35;
   if(NumberCurrenciesTrade==8)
      NumberGroupsTrade=56;
//---Set Positions
   Position[1]=0;
   Position[2]=4;
   Position[3]=8;
   Position[4]=12;
   Position[5]=16;
   Position[6]=20;
   Position[7]=24;
   Position[8]=28;
//---------------------------------------------------------------------
//Check and info
   if(NumberCurrenciesTrade<3)
     {
      Comment("\n "+StringOrdersEA+
              "\n\n --- W A R N I N G S ---"+
              "\n\nNumber of currencies to add \nis below the threshold of 3 (",NumberCurrenciesTrade,")"+
              "\n\nplease check added currencies!");
      Print("Number of currencies to add is below the threshold of 3 (",NumberCurrenciesTrade,")");
      WrongPairs=true;
      return(0);
     }
//---
   if(NumberCurrenciesTrade>8)
     {
      Comment("\n "+StringOrdersEA+
              "\n\n --- W A R N I N G S ---"+
              "\n\nNumber of currencies to add \nis above the threshold of 8 (",NumberCurrenciesTrade,")"+
              "\n\nplease check added currencies!");
      Print("Number of currencies to add is above the threshold of 8 (",NumberCurrenciesTrade,")");
      WrongPairs=true;
      return(0);
     }
//---------------------------------------------------------------------
//Set up Groups
   if(NumberCurrenciesTrade>=3)
     {
      //---(1/2/3)
      SymbolPair[0][1]=SymPrefix+StringSubstr(CurrenciesTrade,Position[1],3)+StringSubstr(CurrenciesTrade,Position[2],3)+SymSuffix;
      SymbolPair[0][2]=SymPrefix+StringSubstr(CurrenciesTrade,Position[1],3)+StringSubstr(CurrenciesTrade,Position[3],3)+SymSuffix;
      SymbolPair[0][3]=SymPrefix+StringSubstr(CurrenciesTrade,Position[2],3)+StringSubstr(CurrenciesTrade,Position[3],3)+SymSuffix;
     }
//---Set groups of 4 currencies
   if(NumberCurrenciesTrade>=4)
     {
      //---(1/2/4)
      SymbolPair[1][1]=SymPrefix+StringSubstr(CurrenciesTrade,Position[1],3)+StringSubstr(CurrenciesTrade,Position[2],3)+SymSuffix;
      SymbolPair[1][2]=SymPrefix+StringSubstr(CurrenciesTrade,Position[1],3)+StringSubstr(CurrenciesTrade,Position[4],3)+SymSuffix;
      SymbolPair[1][3]=SymPrefix+StringSubstr(CurrenciesTrade,Position[2],3)+StringSubstr(CurrenciesTrade,Position[4],3)+SymSuffix;
      //---(1/3/4)
      SymbolPair[2][1]=SymPrefix+StringSubstr(CurrenciesTrade,Position[1],3)+StringSubstr(CurrenciesTrade,Position[3],3)+SymSuffix;
      SymbolPair[2][2]=SymPrefix+StringSubstr(CurrenciesTrade,Position[1],3)+StringSubstr(CurrenciesTrade,Position[4],3)+SymSuffix;
      SymbolPair[2][3]=SymPrefix+StringSubstr(CurrenciesTrade,Position[3],3)+StringSubstr(CurrenciesTrade,Position[4],3)+SymSuffix;
      //---(2/3/4)
      SymbolPair[3][1]=SymPrefix+StringSubstr(CurrenciesTrade,Position[2],3)+StringSubstr(CurrenciesTrade,Position[3],3)+SymSuffix;
      SymbolPair[3][2]=SymPrefix+StringSubstr(CurrenciesTrade,Position[2],3)+StringSubstr(CurrenciesTrade,Position[4],3)+SymSuffix;
      SymbolPair[3][3]=SymPrefix+StringSubstr(CurrenciesTrade,Position[3],3)+StringSubstr(CurrenciesTrade,Position[4],3)+SymSuffix;
     }
//---Set groups of 5 currencies
   if(NumberCurrenciesTrade>=5)
     {
      //---(1/2/5)
      SymbolPair[4][1]=SymPrefix+StringSubstr(CurrenciesTrade,Position[1],3)+StringSubstr(CurrenciesTrade,Position[2],3)+SymSuffix;
      SymbolPair[4][2]=SymPrefix+StringSubstr(CurrenciesTrade,Position[1],3)+StringSubstr(CurrenciesTrade,Position[5],3)+SymSuffix;
      SymbolPair[4][3]=SymPrefix+StringSubstr(CurrenciesTrade,Position[2],3)+StringSubstr(CurrenciesTrade,Position[5],3)+SymSuffix;
      //---(1/3/5)
      SymbolPair[5][1]=SymPrefix+StringSubstr(CurrenciesTrade,Position[1],3)+StringSubstr(CurrenciesTrade,Position[3],3)+SymSuffix;
      SymbolPair[5][2]=SymPrefix+StringSubstr(CurrenciesTrade,Position[1],3)+StringSubstr(CurrenciesTrade,Position[5],3)+SymSuffix;
      SymbolPair[5][3]=SymPrefix+StringSubstr(CurrenciesTrade,Position[3],3)+StringSubstr(CurrenciesTrade,Position[5],3)+SymSuffix;
      //---(1/4/5)
      SymbolPair[6][1]=SymPrefix+StringSubstr(CurrenciesTrade,Position[1],3)+StringSubstr(CurrenciesTrade,Position[4],3)+SymSuffix;
      SymbolPair[6][2]=SymPrefix+StringSubstr(CurrenciesTrade,Position[1],3)+StringSubstr(CurrenciesTrade,Position[5],3)+SymSuffix;
      SymbolPair[6][3]=SymPrefix+StringSubstr(CurrenciesTrade,Position[4],3)+StringSubstr(CurrenciesTrade,Position[5],3)+SymSuffix;
      //---(2/3/5)
      SymbolPair[7][1]=SymPrefix+StringSubstr(CurrenciesTrade,Position[2],3)+StringSubstr(CurrenciesTrade,Position[3],3)+SymSuffix;
      SymbolPair[7][2]=SymPrefix+StringSubstr(CurrenciesTrade,Position[2],3)+StringSubstr(CurrenciesTrade,Position[5],3)+SymSuffix;
      SymbolPair[7][3]=SymPrefix+StringSubstr(CurrenciesTrade,Position[3],3)+StringSubstr(CurrenciesTrade,Position[5],3)+SymSuffix;
      //---(2/4/5)
      SymbolPair[8][1]=SymPrefix+StringSubstr(CurrenciesTrade,Position[2],3)+StringSubstr(CurrenciesTrade,Position[4],3)+SymSuffix;
      SymbolPair[8][2]=SymPrefix+StringSubstr(CurrenciesTrade,Position[2],3)+StringSubstr(CurrenciesTrade,Position[5],3)+SymSuffix;
      SymbolPair[8][3]=SymPrefix+StringSubstr(CurrenciesTrade,Position[4],3)+StringSubstr(CurrenciesTrade,Position[5],3)+SymSuffix;
      //---(3/4/5)
      SymbolPair[9][1]=SymPrefix+StringSubstr(CurrenciesTrade,Position[3],3)+StringSubstr(CurrenciesTrade,Position[4],3)+SymSuffix;
      SymbolPair[9][2]=SymPrefix+StringSubstr(CurrenciesTrade,Position[3],3)+StringSubstr(CurrenciesTrade,Position[5],3)+SymSuffix;
      SymbolPair[9][3]=SymPrefix+StringSubstr(CurrenciesTrade,Position[4],3)+StringSubstr(CurrenciesTrade,Position[5],3)+SymSuffix;
     }
//---Set groups of 6 currencies
   if(NumberCurrenciesTrade>=6)
     {
      //---(1/2/6)
      SymbolPair[10][1]=SymPrefix+StringSubstr(CurrenciesTrade,Position[1],3)+StringSubstr(CurrenciesTrade,Position[2],3)+SymSuffix;
      SymbolPair[10][2]=SymPrefix+StringSubstr(CurrenciesTrade,Position[1],3)+StringSubstr(CurrenciesTrade,Position[6],3)+SymSuffix;
      SymbolPair[10][3]=SymPrefix+StringSubstr(CurrenciesTrade,Position[2],3)+StringSubstr(CurrenciesTrade,Position[6],3)+SymSuffix;
      //---(1/3/6)
      SymbolPair[11][1]=SymPrefix+StringSubstr(CurrenciesTrade,Position[1],3)+StringSubstr(CurrenciesTrade,Position[3],3)+SymSuffix;
      SymbolPair[11][2]=SymPrefix+StringSubstr(CurrenciesTrade,Position[1],3)+StringSubstr(CurrenciesTrade,Position[6],3)+SymSuffix;
      SymbolPair[11][3]=SymPrefix+StringSubstr(CurrenciesTrade,Position[3],3)+StringSubstr(CurrenciesTrade,Position[6],3)+SymSuffix;
      //---(1/4/6)
      SymbolPair[12][1]=SymPrefix+StringSubstr(CurrenciesTrade,Position[1],3)+StringSubstr(CurrenciesTrade,Position[4],3)+SymSuffix;
      SymbolPair[12][2]=SymPrefix+StringSubstr(CurrenciesTrade,Position[1],3)+StringSubstr(CurrenciesTrade,Position[6],3)+SymSuffix;
      SymbolPair[12][3]=SymPrefix+StringSubstr(CurrenciesTrade,Position[4],3)+StringSubstr(CurrenciesTrade,Position[6],3)+SymSuffix;
      //---(1/5/6)
      SymbolPair[13][1]=SymPrefix+StringSubstr(CurrenciesTrade,Position[1],3)+StringSubstr(CurrenciesTrade,Position[5],3)+SymSuffix;
      SymbolPair[13][2]=SymPrefix+StringSubstr(CurrenciesTrade,Position[1],3)+StringSubstr(CurrenciesTrade,Position[6],3)+SymSuffix;
      SymbolPair[13][3]=SymPrefix+StringSubstr(CurrenciesTrade,Position[5],3)+StringSubstr(CurrenciesTrade,Position[6],3)+SymSuffix;
      //---(2/3/6)
      SymbolPair[14][1]=SymPrefix+StringSubstr(CurrenciesTrade,Position[2],3)+StringSubstr(CurrenciesTrade,Position[3],3)+SymSuffix;
      SymbolPair[14][2]=SymPrefix+StringSubstr(CurrenciesTrade,Position[2],3)+StringSubstr(CurrenciesTrade,Position[6],3)+SymSuffix;
      SymbolPair[14][3]=SymPrefix+StringSubstr(CurrenciesTrade,Position[3],3)+StringSubstr(CurrenciesTrade,Position[6],3)+SymSuffix;
      //---(2/4/6)
      SymbolPair[15][1]=SymPrefix+StringSubstr(CurrenciesTrade,Position[2],3)+StringSubstr(CurrenciesTrade,Position[4],3)+SymSuffix;
      SymbolPair[15][2]=SymPrefix+StringSubstr(CurrenciesTrade,Position[2],3)+StringSubstr(CurrenciesTrade,Position[6],3)+SymSuffix;
      SymbolPair[15][3]=SymPrefix+StringSubstr(CurrenciesTrade,Position[4],3)+StringSubstr(CurrenciesTrade,Position[6],3)+SymSuffix;
      //---(2/5/6)
      SymbolPair[16][1]=SymPrefix+StringSubstr(CurrenciesTrade,Position[2],3)+StringSubstr(CurrenciesTrade,Position[5],3)+SymSuffix;
      SymbolPair[16][2]=SymPrefix+StringSubstr(CurrenciesTrade,Position[2],3)+StringSubstr(CurrenciesTrade,Position[6],3)+SymSuffix;
      SymbolPair[16][3]=SymPrefix+StringSubstr(CurrenciesTrade,Position[5],3)+StringSubstr(CurrenciesTrade,Position[6],3)+SymSuffix;
      //---(3/4/6)
      SymbolPair[17][1]=SymPrefix+StringSubstr(CurrenciesTrade,Position[3],3)+StringSubstr(CurrenciesTrade,Position[4],3)+SymSuffix;
      SymbolPair[17][2]=SymPrefix+StringSubstr(CurrenciesTrade,Position[3],3)+StringSubstr(CurrenciesTrade,Position[6],3)+SymSuffix;
      SymbolPair[17][3]=SymPrefix+StringSubstr(CurrenciesTrade,Position[4],3)+StringSubstr(CurrenciesTrade,Position[6],3)+SymSuffix;
      //---(3/5/6)
      SymbolPair[18][1]=SymPrefix+StringSubstr(CurrenciesTrade,Position[3],3)+StringSubstr(CurrenciesTrade,Position[5],3)+SymSuffix;
      SymbolPair[18][2]=SymPrefix+StringSubstr(CurrenciesTrade,Position[3],3)+StringSubstr(CurrenciesTrade,Position[6],3)+SymSuffix;
      SymbolPair[18][3]=SymPrefix+StringSubstr(CurrenciesTrade,Position[5],3)+StringSubstr(CurrenciesTrade,Position[6],3)+SymSuffix;
      //---(4/5/6)
      SymbolPair[19][1]=SymPrefix+StringSubstr(CurrenciesTrade,Position[4],3)+StringSubstr(CurrenciesTrade,Position[5],3)+SymSuffix;
      SymbolPair[19][2]=SymPrefix+StringSubstr(CurrenciesTrade,Position[4],3)+StringSubstr(CurrenciesTrade,Position[6],3)+SymSuffix;
      SymbolPair[19][3]=SymPrefix+StringSubstr(CurrenciesTrade,Position[5],3)+StringSubstr(CurrenciesTrade,Position[6],3)+SymSuffix;
     }
//---Set groups of 7 currencies
   if(NumberCurrenciesTrade>=7)
     {
      //---(1/2/7)
      SymbolPair[20][1]=SymPrefix+StringSubstr(CurrenciesTrade,Position[1],3)+StringSubstr(CurrenciesTrade,Position[2],3)+SymSuffix;
      SymbolPair[20][2]=SymPrefix+StringSubstr(CurrenciesTrade,Position[1],3)+StringSubstr(CurrenciesTrade,Position[7],3)+SymSuffix;
      SymbolPair[20][3]=SymPrefix+StringSubstr(CurrenciesTrade,Position[2],3)+StringSubstr(CurrenciesTrade,Position[7],3)+SymSuffix;
      //---(1/3/7)
      SymbolPair[21][1]=SymPrefix+StringSubstr(CurrenciesTrade,Position[1],3)+StringSubstr(CurrenciesTrade,Position[3],3)+SymSuffix;
      SymbolPair[21][2]=SymPrefix+StringSubstr(CurrenciesTrade,Position[1],3)+StringSubstr(CurrenciesTrade,Position[7],3)+SymSuffix;
      SymbolPair[21][3]=SymPrefix+StringSubstr(CurrenciesTrade,Position[3],3)+StringSubstr(CurrenciesTrade,Position[7],3)+SymSuffix;
      //---(1/4/7)
      SymbolPair[22][1]=SymPrefix+StringSubstr(CurrenciesTrade,Position[1],3)+StringSubstr(CurrenciesTrade,Position[4],3)+SymSuffix;
      SymbolPair[22][2]=SymPrefix+StringSubstr(CurrenciesTrade,Position[1],3)+StringSubstr(CurrenciesTrade,Position[7],3)+SymSuffix;
      SymbolPair[22][3]=SymPrefix+StringSubstr(CurrenciesTrade,Position[4],3)+StringSubstr(CurrenciesTrade,Position[7],3)+SymSuffix;
      //---(1/5/7)
      SymbolPair[23][1]=SymPrefix+StringSubstr(CurrenciesTrade,Position[1],3)+StringSubstr(CurrenciesTrade,Position[5],3)+SymSuffix;
      SymbolPair[23][2]=SymPrefix+StringSubstr(CurrenciesTrade,Position[1],3)+StringSubstr(CurrenciesTrade,Position[7],3)+SymSuffix;
      SymbolPair[23][3]=SymPrefix+StringSubstr(CurrenciesTrade,Position[5],3)+StringSubstr(CurrenciesTrade,Position[7],3)+SymSuffix;
      //---(1/6/7)
      SymbolPair[24][1]=SymPrefix+StringSubstr(CurrenciesTrade,Position[1],3)+StringSubstr(CurrenciesTrade,Position[6],3)+SymSuffix;
      SymbolPair[24][2]=SymPrefix+StringSubstr(CurrenciesTrade,Position[1],3)+StringSubstr(CurrenciesTrade,Position[7],3)+SymSuffix;
      SymbolPair[24][3]=SymPrefix+StringSubstr(CurrenciesTrade,Position[6],3)+StringSubstr(CurrenciesTrade,Position[7],3)+SymSuffix;
      //---(2/3/7)
      SymbolPair[25][1]=SymPrefix+StringSubstr(CurrenciesTrade,Position[2],3)+StringSubstr(CurrenciesTrade,Position[3],3)+SymSuffix;
      SymbolPair[25][2]=SymPrefix+StringSubstr(CurrenciesTrade,Position[2],3)+StringSubstr(CurrenciesTrade,Position[7],3)+SymSuffix;
      SymbolPair[25][3]=SymPrefix+StringSubstr(CurrenciesTrade,Position[3],3)+StringSubstr(CurrenciesTrade,Position[7],3)+SymSuffix;
      //---(2/4/7)
      SymbolPair[26][1]=SymPrefix+StringSubstr(CurrenciesTrade,Position[2],3)+StringSubstr(CurrenciesTrade,Position[4],3)+SymSuffix;
      SymbolPair[26][2]=SymPrefix+StringSubstr(CurrenciesTrade,Position[2],3)+StringSubstr(CurrenciesTrade,Position[7],3)+SymSuffix;
      SymbolPair[26][3]=SymPrefix+StringSubstr(CurrenciesTrade,Position[4],3)+StringSubstr(CurrenciesTrade,Position[7],3)+SymSuffix;
      //---(2/5/7)
      SymbolPair[27][1]=SymPrefix+StringSubstr(CurrenciesTrade,Position[2],3)+StringSubstr(CurrenciesTrade,Position[5],3)+SymSuffix;
      SymbolPair[27][2]=SymPrefix+StringSubstr(CurrenciesTrade,Position[2],3)+StringSubstr(CurrenciesTrade,Position[7],3)+SymSuffix;
      SymbolPair[27][3]=SymPrefix+StringSubstr(CurrenciesTrade,Position[5],3)+StringSubstr(CurrenciesTrade,Position[7],3)+SymSuffix;
      //---(2/6/7)
      SymbolPair[28][1]=SymPrefix+StringSubstr(CurrenciesTrade,Position[2],3)+StringSubstr(CurrenciesTrade,Position[6],3)+SymSuffix;
      SymbolPair[28][2]=SymPrefix+StringSubstr(CurrenciesTrade,Position[2],3)+StringSubstr(CurrenciesTrade,Position[7],3)+SymSuffix;
      SymbolPair[28][3]=SymPrefix+StringSubstr(CurrenciesTrade,Position[6],3)+StringSubstr(CurrenciesTrade,Position[7],3)+SymSuffix;
      //---(3/4/7)
      SymbolPair[29][1]=SymPrefix+StringSubstr(CurrenciesTrade,Position[3],3)+StringSubstr(CurrenciesTrade,Position[4],3)+SymSuffix;
      SymbolPair[29][2]=SymPrefix+StringSubstr(CurrenciesTrade,Position[3],3)+StringSubstr(CurrenciesTrade,Position[7],3)+SymSuffix;
      SymbolPair[29][3]=SymPrefix+StringSubstr(CurrenciesTrade,Position[4],3)+StringSubstr(CurrenciesTrade,Position[7],3)+SymSuffix;
      //---(3/5/7)
      SymbolPair[30][1]=SymPrefix+StringSubstr(CurrenciesTrade,Position[3],3)+StringSubstr(CurrenciesTrade,Position[5],3)+SymSuffix;
      SymbolPair[30][2]=SymPrefix+StringSubstr(CurrenciesTrade,Position[3],3)+StringSubstr(CurrenciesTrade,Position[7],3)+SymSuffix;
      SymbolPair[30][3]=SymPrefix+StringSubstr(CurrenciesTrade,Position[5],3)+StringSubstr(CurrenciesTrade,Position[7],3)+SymSuffix;
      //---(3/6/7)
      SymbolPair[31][1]=SymPrefix+StringSubstr(CurrenciesTrade,Position[3],3)+StringSubstr(CurrenciesTrade,Position[6],3)+SymSuffix;
      SymbolPair[31][2]=SymPrefix+StringSubstr(CurrenciesTrade,Position[3],3)+StringSubstr(CurrenciesTrade,Position[7],3)+SymSuffix;
      SymbolPair[31][3]=SymPrefix+StringSubstr(CurrenciesTrade,Position[6],3)+StringSubstr(CurrenciesTrade,Position[7],3)+SymSuffix;
      //---(4/5/7)
      SymbolPair[32][1]=SymPrefix+StringSubstr(CurrenciesTrade,Position[4],3)+StringSubstr(CurrenciesTrade,Position[5],3)+SymSuffix;
      SymbolPair[32][2]=SymPrefix+StringSubstr(CurrenciesTrade,Position[4],3)+StringSubstr(CurrenciesTrade,Position[7],3)+SymSuffix;
      SymbolPair[32][3]=SymPrefix+StringSubstr(CurrenciesTrade,Position[5],3)+StringSubstr(CurrenciesTrade,Position[7],3)+SymSuffix;
      //---(4/6/7)
      SymbolPair[33][1]=SymPrefix+StringSubstr(CurrenciesTrade,Position[4],3)+StringSubstr(CurrenciesTrade,Position[6],3)+SymSuffix;
      SymbolPair[33][2]=SymPrefix+StringSubstr(CurrenciesTrade,Position[4],3)+StringSubstr(CurrenciesTrade,Position[7],3)+SymSuffix;
      SymbolPair[33][3]=SymPrefix+StringSubstr(CurrenciesTrade,Position[6],3)+StringSubstr(CurrenciesTrade,Position[7],3)+SymSuffix;
      //---(5/6/7)
      SymbolPair[34][1]=SymPrefix+StringSubstr(CurrenciesTrade,Position[5],3)+StringSubstr(CurrenciesTrade,Position[6],3)+SymSuffix;
      SymbolPair[34][2]=SymPrefix+StringSubstr(CurrenciesTrade,Position[5],3)+StringSubstr(CurrenciesTrade,Position[7],3)+SymSuffix;
      SymbolPair[34][3]=SymPrefix+StringSubstr(CurrenciesTrade,Position[6],3)+StringSubstr(CurrenciesTrade,Position[7],3)+SymSuffix;
     }
//---Set groups of 8 currencies
   if(NumberCurrenciesTrade>=8)
     {
      //---(1/2/8)
      SymbolPair[35][1]=SymPrefix+StringSubstr(CurrenciesTrade,Position[1],3)+StringSubstr(CurrenciesTrade,Position[2],3)+SymSuffix;
      SymbolPair[35][2]=SymPrefix+StringSubstr(CurrenciesTrade,Position[1],3)+StringSubstr(CurrenciesTrade,Position[8],3)+SymSuffix;
      SymbolPair[35][3]=SymPrefix+StringSubstr(CurrenciesTrade,Position[2],3)+StringSubstr(CurrenciesTrade,Position[8],3)+SymSuffix;
      //---(1/3/8)
      SymbolPair[36][1]=SymPrefix+StringSubstr(CurrenciesTrade,Position[1],3)+StringSubstr(CurrenciesTrade,Position[3],3)+SymSuffix;
      SymbolPair[36][2]=SymPrefix+StringSubstr(CurrenciesTrade,Position[1],3)+StringSubstr(CurrenciesTrade,Position[8],3)+SymSuffix;
      SymbolPair[36][3]=SymPrefix+StringSubstr(CurrenciesTrade,Position[3],3)+StringSubstr(CurrenciesTrade,Position[8],3)+SymSuffix;
      //---(1/4/8)
      SymbolPair[37][1]=SymPrefix+StringSubstr(CurrenciesTrade,Position[1],3)+StringSubstr(CurrenciesTrade,Position[4],3)+SymSuffix;
      SymbolPair[37][2]=SymPrefix+StringSubstr(CurrenciesTrade,Position[1],3)+StringSubstr(CurrenciesTrade,Position[8],3)+SymSuffix;
      SymbolPair[37][3]=SymPrefix+StringSubstr(CurrenciesTrade,Position[4],3)+StringSubstr(CurrenciesTrade,Position[8],3)+SymSuffix;
      //---(1/5/8)
      SymbolPair[38][1]=SymPrefix+StringSubstr(CurrenciesTrade,Position[1],3)+StringSubstr(CurrenciesTrade,Position[5],3)+SymSuffix;
      SymbolPair[38][2]=SymPrefix+StringSubstr(CurrenciesTrade,Position[1],3)+StringSubstr(CurrenciesTrade,Position[8],3)+SymSuffix;
      SymbolPair[38][3]=SymPrefix+StringSubstr(CurrenciesTrade,Position[5],3)+StringSubstr(CurrenciesTrade,Position[8],3)+SymSuffix;
      //---(1/6/8)
      SymbolPair[39][1]=SymPrefix+StringSubstr(CurrenciesTrade,Position[1],3)+StringSubstr(CurrenciesTrade,Position[6],3)+SymSuffix;
      SymbolPair[39][2]=SymPrefix+StringSubstr(CurrenciesTrade,Position[1],3)+StringSubstr(CurrenciesTrade,Position[8],3)+SymSuffix;
      SymbolPair[39][3]=SymPrefix+StringSubstr(CurrenciesTrade,Position[6],3)+StringSubstr(CurrenciesTrade,Position[8],3)+SymSuffix;
      //---(1/7/8)
      SymbolPair[40][1]=SymPrefix+StringSubstr(CurrenciesTrade,Position[1],3)+StringSubstr(CurrenciesTrade,Position[7],3)+SymSuffix;
      SymbolPair[40][2]=SymPrefix+StringSubstr(CurrenciesTrade,Position[1],3)+StringSubstr(CurrenciesTrade,Position[8],3)+SymSuffix;
      SymbolPair[40][3]=SymPrefix+StringSubstr(CurrenciesTrade,Position[7],3)+StringSubstr(CurrenciesTrade,Position[8],3)+SymSuffix;
      //---(2/3/8)
      SymbolPair[41][1]=SymPrefix+StringSubstr(CurrenciesTrade,Position[2],3)+StringSubstr(CurrenciesTrade,Position[3],3)+SymSuffix;
      SymbolPair[41][2]=SymPrefix+StringSubstr(CurrenciesTrade,Position[2],3)+StringSubstr(CurrenciesTrade,Position[8],3)+SymSuffix;
      SymbolPair[41][3]=SymPrefix+StringSubstr(CurrenciesTrade,Position[3],3)+StringSubstr(CurrenciesTrade,Position[8],3)+SymSuffix;
      //---(2/4/8)
      SymbolPair[42][1]=SymPrefix+StringSubstr(CurrenciesTrade,Position[2],3)+StringSubstr(CurrenciesTrade,Position[4],3)+SymSuffix;
      SymbolPair[42][2]=SymPrefix+StringSubstr(CurrenciesTrade,Position[2],3)+StringSubstr(CurrenciesTrade,Position[8],3)+SymSuffix;
      SymbolPair[42][3]=SymPrefix+StringSubstr(CurrenciesTrade,Position[4],3)+StringSubstr(CurrenciesTrade,Position[8],3)+SymSuffix;
      //---(2/5/8)
      SymbolPair[43][1]=SymPrefix+StringSubstr(CurrenciesTrade,Position[2],3)+StringSubstr(CurrenciesTrade,Position[5],3)+SymSuffix;
      SymbolPair[43][2]=SymPrefix+StringSubstr(CurrenciesTrade,Position[2],3)+StringSubstr(CurrenciesTrade,Position[8],3)+SymSuffix;
      SymbolPair[43][3]=SymPrefix+StringSubstr(CurrenciesTrade,Position[5],3)+StringSubstr(CurrenciesTrade,Position[8],3)+SymSuffix;
      //---(2/6/8)
      SymbolPair[44][1]=SymPrefix+StringSubstr(CurrenciesTrade,Position[2],3)+StringSubstr(CurrenciesTrade,Position[6],3)+SymSuffix;
      SymbolPair[44][2]=SymPrefix+StringSubstr(CurrenciesTrade,Position[2],3)+StringSubstr(CurrenciesTrade,Position[8],3)+SymSuffix;
      SymbolPair[44][3]=SymPrefix+StringSubstr(CurrenciesTrade,Position[6],3)+StringSubstr(CurrenciesTrade,Position[8],3)+SymSuffix;
      //---(2/7/8)
      SymbolPair[45][1]=SymPrefix+StringSubstr(CurrenciesTrade,Position[2],3)+StringSubstr(CurrenciesTrade,Position[7],3)+SymSuffix;
      SymbolPair[45][2]=SymPrefix+StringSubstr(CurrenciesTrade,Position[2],3)+StringSubstr(CurrenciesTrade,Position[8],3)+SymSuffix;
      SymbolPair[45][3]=SymPrefix+StringSubstr(CurrenciesTrade,Position[7],3)+StringSubstr(CurrenciesTrade,Position[8],3)+SymSuffix;
      //---(3/4/8)
      SymbolPair[46][1]=SymPrefix+StringSubstr(CurrenciesTrade,Position[3],3)+StringSubstr(CurrenciesTrade,Position[4],3)+SymSuffix;
      SymbolPair[46][2]=SymPrefix+StringSubstr(CurrenciesTrade,Position[3],3)+StringSubstr(CurrenciesTrade,Position[8],3)+SymSuffix;
      SymbolPair[46][3]=SymPrefix+StringSubstr(CurrenciesTrade,Position[4],3)+StringSubstr(CurrenciesTrade,Position[8],3)+SymSuffix;
      //---(3/5/8)
      SymbolPair[47][1]=SymPrefix+StringSubstr(CurrenciesTrade,Position[3],3)+StringSubstr(CurrenciesTrade,Position[5],3)+SymSuffix;
      SymbolPair[47][2]=SymPrefix+StringSubstr(CurrenciesTrade,Position[3],3)+StringSubstr(CurrenciesTrade,Position[8],3)+SymSuffix;
      SymbolPair[47][3]=SymPrefix+StringSubstr(CurrenciesTrade,Position[5],3)+StringSubstr(CurrenciesTrade,Position[8],3)+SymSuffix;
      //---(3/6/8)
      SymbolPair[48][1]=SymPrefix+StringSubstr(CurrenciesTrade,Position[3],3)+StringSubstr(CurrenciesTrade,Position[6],3)+SymSuffix;
      SymbolPair[48][2]=SymPrefix+StringSubstr(CurrenciesTrade,Position[3],3)+StringSubstr(CurrenciesTrade,Position[8],3)+SymSuffix;
      SymbolPair[48][3]=SymPrefix+StringSubstr(CurrenciesTrade,Position[6],3)+StringSubstr(CurrenciesTrade,Position[8],3)+SymSuffix;
      //---(3/7/8)
      SymbolPair[49][1]=SymPrefix+StringSubstr(CurrenciesTrade,Position[3],3)+StringSubstr(CurrenciesTrade,Position[7],3)+SymSuffix;
      SymbolPair[49][2]=SymPrefix+StringSubstr(CurrenciesTrade,Position[3],3)+StringSubstr(CurrenciesTrade,Position[8],3)+SymSuffix;
      SymbolPair[49][3]=SymPrefix+StringSubstr(CurrenciesTrade,Position[7],3)+StringSubstr(CurrenciesTrade,Position[8],3)+SymSuffix;
      //---(4/5/8)
      SymbolPair[50][1]=SymPrefix+StringSubstr(CurrenciesTrade,Position[4],3)+StringSubstr(CurrenciesTrade,Position[5],3)+SymSuffix;
      SymbolPair[50][2]=SymPrefix+StringSubstr(CurrenciesTrade,Position[4],3)+StringSubstr(CurrenciesTrade,Position[8],3)+SymSuffix;
      SymbolPair[50][3]=SymPrefix+StringSubstr(CurrenciesTrade,Position[5],3)+StringSubstr(CurrenciesTrade,Position[8],3)+SymSuffix;
      //---(4/6/8)
      SymbolPair[51][1]=SymPrefix+StringSubstr(CurrenciesTrade,Position[4],3)+StringSubstr(CurrenciesTrade,Position[6],3)+SymSuffix;
      SymbolPair[51][2]=SymPrefix+StringSubstr(CurrenciesTrade,Position[4],3)+StringSubstr(CurrenciesTrade,Position[8],3)+SymSuffix;
      SymbolPair[51][3]=SymPrefix+StringSubstr(CurrenciesTrade,Position[6],3)+StringSubstr(CurrenciesTrade,Position[8],3)+SymSuffix;
      //---(4/7/8)
      SymbolPair[52][1]=SymPrefix+StringSubstr(CurrenciesTrade,Position[4],3)+StringSubstr(CurrenciesTrade,Position[7],3)+SymSuffix;
      SymbolPair[52][2]=SymPrefix+StringSubstr(CurrenciesTrade,Position[4],3)+StringSubstr(CurrenciesTrade,Position[8],3)+SymSuffix;
      SymbolPair[52][3]=SymPrefix+StringSubstr(CurrenciesTrade,Position[7],3)+StringSubstr(CurrenciesTrade,Position[8],3)+SymSuffix;
      //---(5/6/8)
      SymbolPair[53][1]=SymPrefix+StringSubstr(CurrenciesTrade,Position[5],3)+StringSubstr(CurrenciesTrade,Position[6],3)+SymSuffix;
      SymbolPair[53][2]=SymPrefix+StringSubstr(CurrenciesTrade,Position[5],3)+StringSubstr(CurrenciesTrade,Position[8],3)+SymSuffix;
      SymbolPair[53][3]=SymPrefix+StringSubstr(CurrenciesTrade,Position[6],3)+StringSubstr(CurrenciesTrade,Position[8],3)+SymSuffix;
      //---(5/7/8)
      SymbolPair[54][1]=SymPrefix+StringSubstr(CurrenciesTrade,Position[5],3)+StringSubstr(CurrenciesTrade,Position[7],3)+SymSuffix;
      SymbolPair[54][2]=SymPrefix+StringSubstr(CurrenciesTrade,Position[5],3)+StringSubstr(CurrenciesTrade,Position[8],3)+SymSuffix;
      SymbolPair[54][3]=SymPrefix+StringSubstr(CurrenciesTrade,Position[7],3)+StringSubstr(CurrenciesTrade,Position[8],3)+SymSuffix;
      //---(6/7/8)
      SymbolPair[55][1]=SymPrefix+StringSubstr(CurrenciesTrade,Position[6],3)+StringSubstr(CurrenciesTrade,Position[7],3)+SymSuffix;
      SymbolPair[55][2]=SymPrefix+StringSubstr(CurrenciesTrade,Position[6],3)+StringSubstr(CurrenciesTrade,Position[8],3)+SymSuffix;
      SymbolPair[55][3]=SymPrefix+StringSubstr(CurrenciesTrade,Position[7],3)+StringSubstr(CurrenciesTrade,Position[8],3)+SymSuffix;
     }
//---------------------------------------------------------------------
//Set Skipped groups
   if(StringLen(NoOfGroupToSkip)>0)
     {
      if(StringLen(NoOfGroupToSkip)>2)
        {
         for(i=0; i<StringLen(NoOfGroupToSkip); i++)
           {
            if((i>0)&&(StringFind(NoOfGroupToSkip,",",i)!=-1)&&(StringFind(NoOfGroupToSkip,",",i)!=StringFind(NoOfGroupToSkip,",",i+1)))
              {
               FindComma[CountComma]=StringFind(NoOfGroupToSkip,",",i);
               CountComma++;
              }
           }
         //---
         for(i=0; i<CountComma+1; i++)
           {
            if(i==0)
               NumberGroupsSkip[i]=StrToInteger(StringSubstr(NoOfGroupToSkip,0,FindComma[i]));
            if((i>0)&&(i<CountComma))
               NumberGroupsSkip[i]=StrToInteger(StringSubstr(NoOfGroupToSkip,FindComma[i-1]+1,(FindComma[i]-FindComma[i-1])-1));
            if(i==CountComma)
               NumberGroupsSkip[i]=StrToInteger(StringSubstr(NoOfGroupToSkip,FindComma[i-1]+1,0));
           }
        }
      //---
      if(StringLen(NoOfGroupToSkip)<=2)
        {
         PositionSkipped=0;
         DecimalsGet=StringLen(NoOfGroupToSkip);
         NumberGroupsSkip[0]=StrToInteger(StringSubstr(NoOfGroupToSkip,PositionSkipped,DecimalsGet));
        }
      //---Set Couples to skip
      for(i=0; i<NumberGroupsTrade; i++)
        {
         if(NumberGroupsSkip[i]!=-1)
           {
            CountSkippedGroups++;
            GetGroupUnUse[CountSkippedGroups]=NumberGroupsSkip[i];
           }
        }
      //---
      for(i=0; i<=CountSkippedGroups; i++)
        {
         for(int cnt10=0; cnt10<=NumberGroupsTrade; cnt10++)
           {
            if(GetGroupUnUse[i]==cnt10)
              {
               SkipGroup[cnt10-1]=true;
              }
           }
        }
     }
//---------------------------------------------------------------------
//Add symbols in data window
   for(i=0; i<NumberGroupsTrade; i++)
     {
      SkippedStatus[i]="";
      //---
      Print(" # "+WindowExpertName()+" # "+"Check group No "+IntegerToString(i+1)+"...("+SymbolPair[i][1]+"/"+SymbolPair[i][2]+"/"+SymbolPair[i][3]+")");
      //---
      if((SymbolSelect(SymbolPair[i][1],true))&&(SymbolSelect(SymbolPair[i][2],true))&&(SymbolSelect(SymbolPair[i][3],true)))
        {
         Print(" # "+WindowExpertName()+" # "+SymbolPair[i][1]+"/"+SymbolPair[i][2]+"/"+SymbolPair[i][3]+" are ok");
         if(SkipGroup[i]==true)
           {
            SkippedStatus[i]="Group Skipped by user settings from external parameters";
            Print(" # ",WindowExpertName()," # Skip group No ",IntegerToString(i+1)," #");
           }
        }
      else
         Print(" # "+WindowExpertName()+" # "+SymbolPair[i][1]+"/"+SymbolPair[i][2]+"/"+SymbolPair[i][3]+" not found");
      //---Get prices of symbols
      BidPricePair[i][1]=MarketInfo(SymbolPair[i][1],MODE_BID);
      BidPricePair[i][2]=MarketInfo(SymbolPair[i][2],MODE_BID);
      BidPricePair[i][3]=MarketInfo(SymbolPair[i][3],MODE_BID);
      //---Check symbols
      if(((BidPricePair[i][1]==0)||(BidPricePair[i][2]==0)||(BidPricePair[i][3]==0))&&(WrongPairs==false))
        {
         SymbolStatus[i][1]="Pair "+SymbolPair[i][1]+" Not Found. No Of Pair: "+IntegerToString(i+1);
         SymbolStatus[i][2]="Pair "+SymbolPair[i][2]+" Not Found. No Of Pair: "+IntegerToString(i+1);
         SymbolStatus[i][3]="Pair "+SymbolPair[i][3]+" Not Found. No Of Pair: "+IntegerToString(i+1);
         //---Warnings message
         Comment("\n "+StringOrdersEA+
                 "\n\n --- W A R N I N G S ---"+
                 "\n\n"+SymbolStatus[i][1]+" or \n"+SymbolStatus[i][2]+" or \n"+SymbolStatus[i][3]+
                 "\n\nplease check added currencies!"+
                 "\n\nCorrect format and series for each currency is \nEUR/GBP/AUD/NZD/USD/CAD/CHF/JPY");
         WrongPairs=true;
         return(0);
        }
     }
//---------------------------------------------------------------------
//Set chart
   if(SetChartUses==true)
     {
      if((ChartSymbol()!=SymbolPair[0][1])||(ChartPeriod()!=PERIOD_M1))
        {
         Comment("\n\nExpert set chart symbol...");
         Print(" # "+WindowExpertName()+" # "+"Set chart symbol: "+SymbolPair[0][1]+" and Period: M1");
         ChartSetSymbolPeriod(0,SymbolPair[0][1],PERIOD_M1);
         Sleep(5000);
        }
     }
//---------------------------------------------------------------------
//Check sets and value
   CheckValue();
   if(WrongSet==true)
      return(0);
   if(WrongPairs==true)
      return(0);
//---------------------------------------------------------------------
//Calculate for 4 or 5 digits broker
   MultiplierPoint=1;
   DecimalsPair=(int)MarketInfo(Symbol(),MODE_DIGITS);
   if((DecimalsPair==3)||(DecimalsPair==5))
      MultiplierPoint=10;
//---------------------------------------------------------------------
//Currency and groups infirmations
   Print("### ",WindowExpertName()," || Number Of currencies use: ",NumberCurrenciesTrade," || Number of groups trade: ",NumberGroupsTrade," ###");
//---------------------------------------------------------------------
//ID orders
   if(MagicNumber==0)
     {
      MagicNo=0;
      for(i=0; i<StringLen(CurrenciesTrade); i++)
         MagicNo+=(StringGetChar(CurrenciesTrade,i)*(i+1));
      MagicNo+=MagicSet+AccountNumber();
     }
   else
     {
      MagicNo=MagicNumber;
     }
//---------------------------------------------------------------------
//Set magic and suite for each group
   for(i=0; i<NumberGroupsTrade; i++)
     {
      OrdersID[i]=MagicNo+i;
      //---
      SuitePlus[i][1]=OP_BUY;
      SuitePlus[i][2]=OP_SELL;
      SuitePlus[i][3]=OP_BUY;
      //---
      SuiteMinus[i][1]=OP_SELL;
      SuiteMinus[i][2]=OP_BUY;
      SuiteMinus[i][3]=OP_SELL;
     }
//---------------------------------------------------------------------
//Set maximum orders
   if(((MaximumOrders==0)&&(AccountInfoInteger(ACCOUNT_LIMIT_ORDERS)!=0))||((MaximumOrders>AccountInfoInteger(ACCOUNT_LIMIT_ORDERS))&&(AccountInfoInteger(ACCOUNT_LIMIT_ORDERS)!=0)))
     {
      MaximumOrders=AccountInfoInteger(ACCOUNT_LIMIT_ORDERS);
     }
//---------------------------------------------------------------------
//Set timer
   EventSetMillisecondTimer(TimerInMillisecond);
   StartTime=TimeCurrent();
//---------------------------------------------------------------------
//Call MainFunction function to show information if market is closed
   MainFunction();
//---------------------------------------------------------------------
   return(INIT_SUCCEEDED);
//---------------------------------------------------------------------
  }
//===============================================================================================================================================================================================================================================================//
//OnDeinit function
//===============================================================================================================================================================================================================================================================//
void OnDeinit(const int reason)
  {
//---------------------------------------------------------------------
//Print reason
   if(MQLInfoInteger(MQL_PROGRAM_TYPE)==PROGRAM_EXPERT)
     {
      switch(UninitializeReason())
        {
         case REASON_PROGRAM     :
            Print("Expert Advisor self terminated");
            break;
         case REASON_REMOVE      :
            Print("Expert Advisor removed from the chart");
            break;
         case REASON_RECOMPILE   :
            Print("Expert Advisorhas been recompiled");
            break;
         case REASON_CHARTCHANGE :
            Print("Symbol or chart period has been changed");
            break;
         case REASON_CHARTCLOSE  :
            Print("Chart has been closed");
            break;
         case REASON_PARAMETERS  :
            Print("Input parameters have been changed by a user");
            break;
         case REASON_ACCOUNT     :
            Print("Another account has been activated or reconnection to the trade server has occurred due to changes in the account settings");
            break;
         case REASON_TEMPLATE    :
            Print("A new template has been applied");
            break;
         case REASON_INITFAILED  :
            Print("OnInit() handler has returned a nonzero value");
            break;
         case REASON_CLOSE       :
            Print("Terminal has been closed");
            break;
        }
     }
//---------------------------------------------------------------------
//Clear chart
   for(i=0; i<99; i++)
     {
      if(ObjectFind(0,"Comm1"+IntegerToString(i))>-1)
         ObjectDelete(0,"Comm1"+IntegerToString(i));
      if(ObjectFind(0,"Comm2"+IntegerToString(i))>-1)
         ObjectDelete(0,"Comm2"+IntegerToString(i));
      if(ObjectFind(0,"Comm3"+IntegerToString(i))>-1)
         ObjectDelete(0,"Comm3"+IntegerToString(i));
      if(ObjectFind(0,"Comm4"+IntegerToString(i))>-1)
         ObjectDelete(0,"Comm4"+IntegerToString(i));
      if(ObjectFind(0,"Comm5"+IntegerToString(i))>-1)
         ObjectDelete(0,"Comm5"+IntegerToString(i));
      if(ObjectFind(0,"Comm6"+IntegerToString(i))>-1)
         ObjectDelete(0,"Comm6"+IntegerToString(i));
      if(ObjectFind(0,"Comm7"+IntegerToString(i))>-1)
         ObjectDelete(0,"Comm7"+IntegerToString(i));
      if(ObjectFind(0,"Comm8"+IntegerToString(i))>-1)
         ObjectDelete(0,"Comm8"+IntegerToString(i));
      if(ObjectFind(0,"BackgroundLine1"+IntegerToString(i))>-1)
         ObjectDelete(0,"BackgroundLine1"+IntegerToString(i));
      if(ObjectFind(0,"BackgroundLine2"+IntegerToString(i))>-1)
         ObjectDelete(0,"BackgroundLine2"+IntegerToString(i));
      if(ObjectFind(0,"Text"+IntegerToString(i))>-1)
         ObjectDelete(0,"Text"+IntegerToString(i));
      if(ObjectFind(0,"Str"+IntegerToString(i))>-1)
         ObjectDelete(0,"Str"+IntegerToString(i));
     }
//---
   if(ObjectFind(0,"BackgroundLine0")>-1)
      ObjectDelete(0,"BackgroundLine0");
   if(ObjectFind(0,"Background")>-1)
      ObjectDelete(0,"Background");
   if(ObjectFind(0,"Optimization")>-1)
      ObjectDelete(0,"Optimization");
   if(ObjectFind(0,"LastTime")>-1)
      ObjectDelete(0,"LastTime");
   if(ObjectFind(0,"Line")>-1)
      ObjectDelete(0,"Line");
//---
   Comment("");
//---------------------------------------------------------------------
//Destroy timer
   EventKillTimer();
//---------------------------------------------------------------------
  }
//===============================================================================================================================================================================================================================================================//
//OnTick function
//===============================================================================================================================================================================================================================================================//
void OnTick()
  {
//---------------------------------------------------------------------
//Pass test to approval it on market place
   int OpenedOrders=0;
   int iSendOrder1=0;
   int iSendOrder2=0;
   bool iCloseOrder1=false;
   bool iCloseOrder2=false;
   double Profit1=0;
   double Profit2=0;
   double OrdersTakeProfit=10;
   double OrdersStopLoss=10;
   double LotsSize=NormalizeLot(0.01);
//---------------------------------------------------------------------
   if((IsTesting())||(IsVisualMode())||(IsOptimization()))
     {
      if(OrdersTotal()>0)
        {
         for(i=OrdersTotal()-1; i>=0; i--)
           {
            if(OrderSelect(i,SELECT_BY_POS)==true)
              {
               if(OrderMagicNumber()==123321)
                 {
                  OpenedOrders++;
                  if(OrderType()==OP_BUY)
                    {
                     Profit1=OrderProfit()+OrderCommission()+OrderSwap();
                     if((Profit1>=(OrderLots()*OrdersTakeProfit)*MarketInfo(Symbol(),MODE_TICKVALUE)*10)||(Profit1<=-((OrderLots()*OrdersStopLoss)*MarketInfo(Symbol(),MODE_TICKVALUE)*10)))
                       {
                        iCloseOrder1=OrderClose(OrderTicket(),OrderLots(),Bid,3,clrNONE);
                       }
                    }
                  if(OrderType()==OP_SELL)
                    {
                     Profit2=OrderProfit()+OrderCommission()+OrderSwap();
                     if((Profit2>=(OrderLots()*OrdersTakeProfit)*MarketInfo(Symbol(),MODE_TICKVALUE)*10)||(Profit2<=-((OrderLots()*OrdersStopLoss)*MarketInfo(Symbol(),MODE_TICKVALUE)*10)))
                       {
                        iCloseOrder2=OrderClose(OrderTicket(),OrderLots(),Ask,3,clrNONE);
                       }
                    }
                 }
              }
           }
        }
      else
         if(Hour()==12)
           {
            if((OpenedOrders==0)&&(AccountFreeMargin()-((AccountFreeMargin()-AccountFreeMarginCheck(Symbol(),OP_BUY,LotsSize))+(AccountFreeMargin()-AccountFreeMarginCheck(Symbol(),OP_SELL,LotsSize)))>0))
              {
               iSendOrder1=OrderSend(Symbol(),OP_BUY,LotsSize,Ask,3,0,0,"",123321,0,clrBlue);
               iSendOrder2=OrderSend(Symbol(),OP_SELL,LotsSize,Bid,3,0,0,"",123321,0,clrRed);
              }
           }
      return;
     }
//---------------------------------------------------------------------
//Reset value
   CallMain=false;
//---------------------------------------------------------------------
//Warning message
   if(!IsTesting())
     {
      if(!IsExpertEnabled())
        {
         Comment("\n      The trading terminal",
                 "\n      of experts do not run",
                 "\n\n\n      Enable auto trading Please .......");
         return;
        }
      else
         if(!IsTradeAllowed())
           {
            Comment("\n      Trade is disabled",
                    "\n      experts can't run",
                    "\n\n\n      Check login credential Please .......");
            return;
           }
         else
           {
            CallMain=true;
           }
     }
//---------------------------------------------------------------------
  }
//===============================================================================================================================================================================================================================================================//
//OnTimer function
//===============================================================================================================================================================================================================================================================//
void OnTimer()
  {
//---------------------------------------------------------------------
//Call main function
   if(CallMain==true)
      MainFunction();
//---------------------------------------------------------------------
  }
//===============================================================================================================================================================================================================================================================//
//Main function
//===============================================================================================================================================================================================================================================================//
void MainFunction()
  {
//---------------------------------------------------------------------
//Reset value
   TotalGroupsProfit=0;
   TotalGroupsOrders=0;
   CommentWarning=false;
   LimitOfOrdersOk=true;
//---------------------------------------------------------------------
//Reset value
   ArrayInitialize(SpreadOK,true);
   ArrayInitialize(TicketNo,-1);
   ArrayInitialize(SumSpreadValuePlus,0);
   ArrayInitialize(SumSpreadValueMinus,0);
   ArrayInitialize(LevelProfitClosePlus,0);
   ArrayInitialize(LevelProfitCloseMinus,0);
   ArrayInitialize(LevelProfitClosePairPlus,0);
   ArrayInitialize(LevelProfitClosePairMinus,0);
   ArrayInitialize(LevelLossClosePlus,0);
   ArrayInitialize(LevelLossCloseMinus,0);
   ArrayInitialize(LevelOpenNextPlus,0);
   ArrayInitialize(LevelOpenNextMinus,0);
   ArrayInitialize(CheckMargin,0);
   ArrayInitialize(MultiplierLotPlus,0);
   ArrayInitialize(MultiplierLotMinus,0);
   ArrayInitialize(MultiplierStepPlus,0);
   ArrayInitialize(MultiplierStepMinus,0);
   ArrayInitialize(SpreadPair,0);
   ArrayInitialize(SumSpreadGroup,0);
   ArrayInitialize(SpreadValuePlus,0);
   ArrayInitialize(SpreadValueMinus,0);
   ArrayInitialize(FirsOrdersPlusOK,false);
   ArrayInitialize(FirsOrdersMinusOK,false);
//---------------------------------------------------------------------
//Check expiry date
   if(ChcekLockedDay!=DayOfYear())
     {
      ChcekLockedDay=DayOfYear();
      ChangeOperation=false;
      LockedCheck();
     }
//---Change operation
   if((ChangeOperation==true)&&(TypeOfOperation==1))
     {
      TypeOfOperation=2;
      CommentWarning=true;
      WarningPrint=StringConcatenate("Expert Has Expired, Working To Close In Profit And Stop");
     }
//---------------------------------------------------------------------
//Stop in locked version or wrong sets or missing bars
   if(StopWorking==true)
      return;
   if(WrongSet==true)
      return;
   if(WrongPairs==true)
      return;
//---------------------------------------------------------------------
//Check open market
   if((!IsTesting())&&(!IsVisualMode())&&(!IsOptimization()))
     {
      if(CheckTicksOpenMarket<3)
         CheckTicksOpenMarket++;
      //---
      if(CheckTicksOpenMarket==1)
        {
         if(IsConnected())
           {
            MarketIsOpen=false;
            CommentWarning=true;
            WarningPrint="Market is closed!!!";
           }
         else
           {
            MarketIsOpen=false;
            CommentWarning=true;
            WarningPrint="Disconnected terminal!!!";
           }
        }
      //---
      if((CheckTicksOpenMarket>=2)&&(MarketIsOpen==false))
         MarketIsOpen=true;
     }
   else
     {
      MarketIsOpen=true;
     }
//---------------------------------------------------------------------
//Check limit of orders
   if(MaximumOrders!=0)
     {
      if(OrdersTotal()+(PairsPerGroup)>MaximumOrders)
        {
         LimitOfOrdersOk=false;
         CommentWarning=true;
         WarningPrint=StringConcatenate("Expert reached the limit of opened orders!!!");
        }
     }
//---------------------------------------------------------------------
//Control market session
   if(ControlSession==true)
     {
      //---Wait on Monday
      if((DayOfWeek()==1)&&(SymbolInfoSessionTrade(Symbol(),MONDAY,0,TimeBegin,TimeEnd)==true))
        {
         if(TimeToString(TimeCurrent(),TIME_MINUTES)<=TimeToString(TimeBegin+(WaitAfterOpen*60),TIME_MINUTES))
           {
            MarketIsOpen=false;
            CommentWarning=true;
            WarningPrint=StringConcatenate("Wait ",WaitAfterOpen," minutes after Monday open market!!!");
           }
        }
      //---Stop on Friday
      if((DayOfWeek()==5)&&(SymbolInfoSessionTrade(Symbol(),FRIDAY,0,TimeBegin,TimeEnd)==true))
        {
         if(TimeToString(TimeCurrent(),TIME_MINUTES)>=TimeToString(TimeEnd-(StopBeforeClose*60),TIME_MINUTES))
           {
            MarketIsOpen=false;
            CommentWarning=true;
            WarningPrint=StringConcatenate("Wait ",StopBeforeClose," minutes before Friday close market!!!");
           }
        }
     }
//---------------------------------------------------------------------
//Start multipair function
   for(int cnt=0; cnt<NumberGroupsTrade; cnt++)
     {
      //---------------------------------------------------------------------
      //Skip groups
      if(SkipGroup[cnt]==true)
         continue;
      //---------------------------------------------------------------------
      //Get orders' informations
      CountCurrentOrders(cnt);
      //---------------------------------------------------------------------
      //Get spreads and tick value
      for(i=1; i<=PairsPerGroup; i++)
        {
         SpreadPair[cnt][i]=NormalizeDouble(MarketInfo(SymbolPair[cnt][i],MODE_SPREAD)/MultiplierPoint,2);
         SumSpreadGroup[cnt]+=NormalizeDouble(SpreadPair[cnt][i],2);
         //---
         TickValuePair[cnt][i]=MarketInfo(SymbolPair[cnt][i],MODE_TICKVALUE);
         //---
         SpreadValuePlus[cnt][i]=SpreadPair[cnt][i]*TotalLotPlus[cnt][i]*TickValuePair[cnt][i]*MultiplierPoint;
         //---
         SpreadValueMinus[cnt][i]=SpreadPair[cnt][i]*TotalLotMinus[cnt][i]*TickValuePair[cnt][i]*MultiplierPoint;
         //---
         SumSpreadValuePlus[cnt]+=SpreadValuePlus[cnt][i];
         SumSpreadValueMinus[cnt]+=SpreadValueMinus[cnt][i];
        }
      //---
      if(cnt==0)
         TotalGroupsSpread=0;
      TotalGroupsSpread+=SumSpreadGroup[cnt];
      //---------------------------------------------------------------------
      //Check spreads
      if(MaxSpread>0.0)
        {
         if(SumSpreadGroup[cnt]>MaxSpread)
           {
            SpreadOK[cnt]=false;
            CommentWarning=true;
            if(TypeOfOperation!=0)
               WarningPrint=StringConcatenate("Spread it isn't normal (",DoubleToString(SumSpreadGroup[cnt],2),"/",DoubleToString(MaxSpread,2),")");
           }
        }
      //---------------------------------------------------------------------
      //Reset value
      if(SumOrdersPlus[cnt]==0)
        {
         ExpertClosePlusInProfit[cnt]=false;
         ExpertClosePlusInLoss[cnt]=false;
         DelayTimesForCloseInLossPlus[cnt]=0;
         DelayTimesForCloseInProfitPlus[cnt]=0;
        }
      //---
      if(SumOrdersMinus[cnt]==0)
        {
         ExpertCloseMinusInProfit[cnt]=false;
         ExpertCloseMinusInLoss[cnt]=false;
         DelayTimesForCloseInLossMinus[cnt]=0;
         DelayTimesForCloseInProfitMinus[cnt]=0;
        }
      //---
      if(SumOrdersPlus[cnt]+SumOrdersMinus[cnt]==0)
        {
         ExpertCloseBasketInProfit[cnt]=false;
         ExpertCloseBasketInLoss[cnt]=false;
         DelayTimesForCloseBasketLoss[cnt]=0;
         DelayTimesForCloseBasketProfit[cnt]=0;
        }
      //---
      if(TypeCloseInProfit==2)
        {
         for(i=1; i<=PairsPerGroup; i++)
           {
            if(TotalOrdersPlus[cnt][i]==0)
               ExpertClosePairPlusInProfit[cnt][i]=false;
            if(TotalOrdersMinus[cnt][i]==0)
               ExpertClosePairMinusInProfit[cnt][i]=false;
           }
        }
      //---------------------------------------------------------------------
      //Count history orders
      if(CntTick<NumberGroupsTrade+4)
         CntTick++;
      if(CntTick<NumberGroupsTrade+3)
         CountHistory=true;
      if((LastHistoryOrders!=OrdersHistoryTotal())&&(cnt==0))
         CountHistory=true;
      //---
      if(CountHistory==true)
        {
         CountHistory=false;
         LastHistoryOrders=OrdersHistoryTotal();
         CountHistoryOrders(cnt);
        }
      //---------------------------------------------------------------------
      //Set levels open/close
      if(StepOrdersProgress==0)//Statical
        {
         MultiplierStepPlus[cnt]=GroupsPlus[cnt];
         MultiplierStepMinus[cnt]=GroupsMinus[cnt];
        }
      //---
      if(StepOrdersProgress==1)//Geometrical
        {
         if(GroupsPlus[cnt]>0)
            for(i=0; i<=GroupsPlus[cnt]; i++)
               MultiplierStepPlus[cnt]+=i;
         if(GroupsMinus[cnt]>0)
            for(i=0; i<=GroupsMinus[cnt]; i++)
               MultiplierStepMinus[cnt]+=i;
        }
      //---
      if(StepOrdersProgress==2)//Exponential
        {
         if(GroupsPlus[cnt]>0)
            for(i=0; i<=GroupsPlus[cnt]; i++)
               MultiplierStepPlus[cnt]+=MathMax(1,MathPow(2,i-1));
         if(GroupsMinus[cnt]>0)
            for(i=0; i<=GroupsMinus[cnt]; i++)
               MultiplierStepMinus[cnt]+=MathMax(1,MathPow(2,i-1));
        }
      //---------------------------------------------------------------------
      //Calculate levels
      for(i=1; i<=PairsPerGroup; i++)
        {
         //---Levels open next group in loss
         if(UseFairLotSize==false)
           {
            if(TotalOrdersPlus[cnt][i]>0)
               LevelOpenNextPlus[cnt]+=-(TotalLotPlus[cnt][i]*StepOpenNextOrders*MultiplierStepPlus[cnt]*TickValuePair[cnt][i]);
            if(TotalOrdersMinus[cnt][i]>0)
               LevelOpenNextMinus[cnt]+=-(TotalLotMinus[cnt][i]*StepOpenNextOrders*MultiplierStepMinus[cnt]*TickValuePair[cnt][i]);
           }
         if(UseFairLotSize==true)
           {
            if(TotalOrdersPlus[cnt][i]>0)
               LevelOpenNextPlus[cnt]+=-(TotalLotPlus[cnt][i]*StepOpenNextOrders*MultiplierStepPlus[cnt]);
            if(TotalOrdersMinus[cnt][i]>0)
               LevelOpenNextMinus[cnt]+=-(TotalLotMinus[cnt][i]*StepOpenNextOrders*MultiplierStepMinus[cnt]);
           }
         //---Levels close group in profit
         if(UseFairLotSize==false)
           {
            if(TotalOrdersPlus[cnt][i]>0)
              {
               LevelProfitClosePlus[cnt]+=(TotalLotPlus[cnt][i]*TargetCloseProfit*TickValuePair[cnt][i]);
               LevelProfitClosePairPlus[cnt][i]=(TotalLotPlus[cnt][i]*TargetCloseProfit*TickValuePair[cnt][i]);
              }
            if(TotalOrdersMinus[cnt][i]>0)
              {
               LevelProfitCloseMinus[cnt]+=(TotalLotMinus[cnt][i]*TargetCloseProfit*TickValuePair[cnt][i]);
               LevelProfitClosePairMinus[cnt][i]=(TotalLotMinus[cnt][i]*TargetCloseProfit*TickValuePair[cnt][i]);
              }
           }
         //---
         if(UseFairLotSize==true)
           {
            if(TotalOrdersPlus[cnt][i]>0)
              {
               LevelProfitClosePlus[cnt]+=(TotalLotPlus[cnt][i]*TargetCloseProfit);
               LevelProfitClosePairPlus[cnt][i]=(TotalLotPlus[cnt][i]*TargetCloseProfit);
              }
            if(TotalOrdersMinus[cnt][i]>0)
              {
               LevelProfitCloseMinus[cnt]+=(TotalLotMinus[cnt][i]*TargetCloseProfit);
               LevelProfitClosePairMinus[cnt][i]=(TotalLotMinus[cnt][i]*TargetCloseProfit);
              }
           }
         //---Levels close group in loss
         if(UseFairLotSize==false)
           {
            if(TotalOrdersPlus[cnt][i]>0)
               LevelLossClosePlus[cnt]+=-((TotalLotPlus[cnt][i]/TotalOrdersPlus[cnt][i])*TargetCloseLoss*TickValuePair[cnt][i]);
            if(TotalOrdersMinus[cnt][i]>0)
               LevelLossCloseMinus[cnt]+=-((TotalLotMinus[cnt][i]/TotalOrdersMinus[cnt][i])*TargetCloseLoss*TickValuePair[cnt][i]);
           }
         if(UseFairLotSize==true)
           {
            if(TotalOrdersPlus[cnt][i]>0)
               LevelLossClosePlus[cnt]+=-((TotalLotPlus[cnt][i]/TotalOrdersPlus[cnt][i])*TargetCloseLoss);
            if(TotalOrdersMinus[cnt][i]>0)
               LevelLossCloseMinus[cnt]+=-((TotalLotMinus[cnt][i]/TotalOrdersMinus[cnt][i])*TargetCloseLoss);
           }
         //---
         LevelProfitClosePairPlus[cnt][i]-=(SpreadValuePlus[cnt][i]+TotalCommissionPlus[cnt][i]);
         LevelProfitClosePairMinus[cnt][i]-=(SpreadValueMinus[cnt][i]+TotalCommissionMinus[cnt][i]);
        }
      //---------------------------------------------------------------------
      //Add spread and commision in levels
      LevelOpenNextPlus[cnt]-=(SumSpreadValuePlus[cnt]+SumCommissionPlus[cnt]);
      LevelOpenNextMinus[cnt]-=(SumSpreadValueMinus[cnt]+SumCommissionMinus[cnt]);
      //---
      LevelProfitClosePlus[cnt]-=(SumSpreadValuePlus[cnt]+SumCommissionPlus[cnt]);
      LevelProfitCloseMinus[cnt]-=(SumSpreadValueMinus[cnt]+SumCommissionMinus[cnt]);
      //---
      LevelLossClosePlus[cnt]-=(SumSpreadValuePlus[cnt]+SumCommissionPlus[cnt]);
      LevelLossCloseMinus[cnt]-=(SumSpreadValueMinus[cnt]+SumCommissionMinus[cnt]);
      //---------------------------------------------------------------------
      //Send group
      if((MarketIsOpen==true)&&(SpreadOK[cnt]==true)&&(ExpertClosePlusInLoss[cnt]==false)&&(ExpertCloseMinusInLoss[cnt]==false)&&
         (ExpertClosePlusInProfit[cnt]==false)&&(ExpertCloseMinusInProfit[cnt]==false)&&(ExpertCloseBasketInProfit[cnt]==false)&&(ExpertCloseBasketInLoss[cnt]==false)&&
         (ExpertClosePairPlusInProfit[cnt][1]==false)&&(ExpertClosePairPlusInProfit[cnt][2]==false)&&(ExpertClosePairPlusInProfit[cnt][3]==false)&&
         (ExpertClosePairMinusInProfit[cnt][1]==false)&&(ExpertClosePairMinusInProfit[cnt][2]==false)&&(ExpertClosePairMinusInProfit[cnt][3]==false))
        {
         //---------------------------------------------------------------------
         //Send oposite group if close a group in profit
         if(SideOpenOrders==2)
           {
            if((OpenOrdersInLoss==2)&&(TypeOfOperation==1)&&(OrdersIsOK[cnt]==true))
              {
               //---Send minus if close plus
               if((SumOrdersPlus[cnt]==0)&&(SumOrdersMinus[cnt]>=PairsPerGroup))
                 {
                  if((GroupsMinus[cnt]<MaximumGroups)||(MaximumGroups==0))
                    {
                     for(i=1; i<=PairsPerGroup; i++)
                        OpenPairMinus(cnt,i);
                    }
                  else
                    {
                     WarningPrint=StringConcatenate("Total minus groups have reached the limit.");
                    }
                 }
               //---Send plus if close minus
               if((SumOrdersMinus[cnt]==0)&&(SumOrdersPlus[cnt]>=PairsPerGroup))
                 {
                  if((GroupsPlus[cnt]<MaximumGroups)||(MaximumGroups==0))
                    {
                     for(i=1; i<=PairsPerGroup; i++)
                        OpenPairPlus(cnt,i);
                    }
                  else
                    {
                     WarningPrint=StringConcatenate("Total plus groups have reached the limit.");
                    }
                 }
              }
           }
         //---------------------------------------------------------------------
         //Send first group
         if((LimitOfOrdersOk==true)&&(TypeOfOperation!=0)&&(TypeOfOperation!=3))
           {
            //---Check and open first orders with random mode
            for(i=1; i<=PairsPerGroup; i++)
              {
               if((SideOpenOrders==0)||(SideOpenOrders==2))
                 {
                  if(TotalOrdersPlus[cnt][i]!=0)
                    {
                     FirsOrdersPlusOK[cnt]=true;
                    }
                  else
                    {
                     if(TypeOfOperation==1)
                       {
                        OpenPairPlus(cnt,i);
                        FirsOrdersPlusOK[cnt]=false;
                        //break;
                       }
                    }
                 }
               //---
               if((SideOpenOrders==1)||(SideOpenOrders==2))
                 {
                  if(TotalOrdersMinus[cnt][i]!=0)
                    {
                     FirsOrdersMinusOK[cnt]=true;
                    }
                  else
                    {
                     if(TypeOfOperation==1)
                       {
                        OpenPairMinus(cnt,i);
                        FirsOrdersMinusOK[cnt]=false;
                        //break;
                       }
                    }
                 }
              }
            //---Set first orders ok
            if(SideOpenOrders==0)
               FirsOrdersMinusOK[cnt]=true;
            if(SideOpenOrders==1)
               FirsOrdersPlusOK[cnt]=true;
            //---
            if((FirsOrdersPlusOK[cnt]==false)||(FirsOrdersMinusOK[cnt]==false))
              {
               CommentChart();
               continue;
              }
            //---------------------------------------------------------------------
            //Send next group in loss
            if((OpenOrdersInLoss==1)&&(OrdersIsOK[cnt]==true))
              {
               //---Send plus
               if((SideOpenOrders==0)||(SideOpenOrders==2))
                 {
                  if((FirsOrdersPlusOK[cnt]==true)&&(TimeCurrent()-TimeOpenLastPlus[cnt]>=MinutesForNextOrder*60))
                    {
                     if((GroupsPlus[cnt]<MaximumGroups)||(MaximumGroups==0))
                       {
                        if((SumOrdersPlus[cnt]>=PairsPerGroup)&&(SumProfitPlus[cnt]<=LevelOpenNextPlus[cnt])&&((TypeCloseInProfit==0)||(TypeCloseInProfit==2)||(SumOrdersPlus[cnt]==SumOrdersMinus[cnt])||(SumOrdersPlus[cnt]>SumOrdersMinus[cnt])))
                          {
                           for(i=1; i<=PairsPerGroup; i++)
                              OpenPairPlus(cnt,i);
                           continue;
                          }
                       }
                     else
                       {
                        WarningPrint=StringConcatenate("Total plus groups have reached the limit.");
                       }
                    }
                 }
               //---Send minus
               if((SideOpenOrders==1)||(SideOpenOrders==2))
                 {
                  if((FirsOrdersMinusOK[cnt]==true)&&(TimeCurrent()-TimeOpenLastMinus[cnt]>=MinutesForNextOrder*60))
                    {
                     if((GroupsMinus[cnt]<MaximumGroups)||(MaximumGroups==0))
                       {
                        if((SumOrdersMinus[cnt]>=PairsPerGroup)&&(SumProfitMinus[cnt]<=LevelOpenNextMinus[cnt])&&((TypeCloseInProfit==0)||(TypeCloseInProfit==2)||(SumOrdersPlus[cnt]==SumOrdersMinus[cnt])||(SumOrdersPlus[cnt]<SumOrdersMinus[cnt])))
                          {
                           for(i=1; i<=PairsPerGroup; i++)
                              OpenPairMinus(cnt,i);
                           continue;
                          }
                       }
                     else
                       {
                        WarningPrint=StringConcatenate("Total minus groups have reached the limit.");
                       }
                    }
                 }
              }
           }
        }
      //---------------------------------------------------------------------
      //Close orders
      if((MarketIsOpen==true)&&(TypeOfOperation!=0)&&(TypeOfOperation!=3))
        {
         //---------------------------------------------------------------------
         //Close orders in loss
         if((TypeCloseInLoss<2)&&(ExpertCloseBasketInProfit[cnt]==false)&&(ExpertClosePlusInProfit[cnt]==false)&&(ExpertCloseMinusInProfit[cnt]==false)&&
            (ExpertClosePairPlusInProfit[cnt][1]==false)&&(ExpertClosePairPlusInProfit[cnt][2]==false)&&(ExpertClosePairPlusInProfit[cnt][3]==false)&&
            (ExpertClosePairMinusInProfit[cnt][1]==false)&&(ExpertClosePairMinusInProfit[cnt][2]==false)&&(ExpertClosePairMinusInProfit[cnt][3]==false))
           {
            //---Close whole ticket
            if((TypeCloseInLoss==0)&&(SumOrdersPlus[cnt]+SumOrdersPlus[cnt]>0))
              {
               if(((SumProfitPlus[cnt]<0)&&(SumOrdersPlus[cnt]>0))||(ExpertClosePlusInLoss[cnt]==true))
                 {
                  //---Start close plus in loss
                  if(SumProfitPlus[cnt]>LevelLossClosePlus[cnt])
                     DelayTimesForCloseInLossPlus[cnt]=0;//Resete delay times before close
                  if((SumProfitPlus[cnt]<=LevelLossClosePlus[cnt])||(ExpertClosePlusInLoss[cnt]==true))//Close plus groups
                    {
                     DelayTimesForCloseInLossPlus[cnt]++;
                     //---Close plus in loss
                     if((DelayTimesForCloseInLossPlus[cnt]>=DelayCloseLoss)||(ExpertClosePlusInLoss[cnt]==true))
                       {
                        for(i=1; i<=PairsPerGroup; i++)
                          {
                           if(TotalOrdersPlus[cnt][i]>0)
                              ClosePairPlus(cnt,-1,i);
                          }
                        //---
                        ExpertClosePlusInLoss[cnt]=true;
                        continue;
                       }
                    }
                 }
               //---Start close minus in loss
               if(((SumProfitMinus[cnt]<0)&&(SumOrdersMinus[cnt]>0))||(ExpertCloseMinusInLoss[cnt]==true))
                 {
                  if(SumProfitMinus[cnt]>LevelLossCloseMinus[cnt])
                     DelayTimesForCloseInLossMinus[cnt]=0;//Resete delay times before close
                  if((SumProfitMinus[cnt]<=LevelLossCloseMinus[cnt])||(ExpertCloseMinusInLoss[cnt]==true))//Close minus groups
                    {
                     DelayTimesForCloseInLossMinus[cnt]++;
                     //---Close minus in loss
                     if((DelayTimesForCloseInLossMinus[cnt]>=DelayCloseLoss)||(ExpertCloseMinusInLoss[cnt]==true))
                       {
                        for(i=1; i<=PairsPerGroup; i++)
                          {
                           if(TotalOrdersMinus[cnt][i]>0)
                              ClosePairMinus(cnt,-1,i);
                          }
                        //---
                        ExpertCloseMinusInLoss[cnt]=true;
                        continue;
                       }
                    }
                 }
              }//End if(TypeCloseInLoss==0)
            //---Close partial ticket
            if((TypeCloseInLoss==1)&&(SumOrdersPlus[cnt]+SumOrdersMinus[cnt]>0))
              {
               if((SumProfitPlus[cnt]<0)&&(SumOrdersPlus[cnt]>SumOrdersMinus[cnt]))
                 {
                  //---Start close plus in loss
                  if(SumProfitPlus[cnt]>LevelLossClosePlus[cnt])
                     DelayTimesForCloseInLossPlus[cnt]=0;//Resete delay times before close
                  if(SumProfitPlus[cnt]<=LevelLossClosePlus[cnt])//Close first orders plus
                    {
                     DelayTimesForCloseInLossPlus[cnt]++;
                     //---Close plus in loss
                     if(DelayTimesForCloseInLossPlus[cnt]>=DelayCloseLoss)
                       {
                        for(i=1; i<=PairsPerGroup; i++)
                          {
                           if((TotalOrdersPlus[cnt][i]>0)&&(FirstTicketPlus[cnt][i]!=0))
                              ClosePairPlus(cnt,FirstTicketPlus[cnt][i],i);
                          }
                        //---
                        continue;
                       }
                    }
                 }
               //---Start close minus in loss
               if((SumProfitMinus[cnt]<0)&&(SumOrdersMinus[cnt]>SumOrdersPlus[cnt]))
                 {
                  if(SumProfitMinus[cnt]>LevelLossCloseMinus[cnt])
                     DelayTimesForCloseInLossMinus[cnt]=0;//Resete delay times before close
                  if(SumProfitMinus[cnt]<=LevelLossCloseMinus[cnt])//Close first orders minus
                    {
                     DelayTimesForCloseInLossMinus[cnt]++;
                     //---Close minus in loss
                     if(DelayTimesForCloseInLossMinus[cnt]>=DelayCloseLoss)
                       {
                        for(i=1; i<=PairsPerGroup; i++)
                          {
                           if((TotalOrdersMinus[cnt][i]>0)&&(FirstTicketMinus[cnt][i]!=0))
                              ClosePairMinus(cnt,FirstTicketMinus[cnt][i],i);
                          }
                        //---
                        continue;
                       }
                    }
                 }
              }//End if(TypeCloseInLoss==1)
           }//End if((TypeCloseInLoss<2)&&(ExpertCloseBasketInProfit[cnt]==false)...
         //---------------------------------------------------------------------
         //Close orders in profit
         if((ExpertCloseBasketInLoss[cnt]==false)&&(ExpertClosePlusInLoss[cnt]==false)&&(ExpertCloseMinusInLoss[cnt]==false))
           {
            //---Close in ticket profit
            if(TypeCloseInProfit==0)
              {
               //---Start close plus in profit
               if((SumProfitPlus[cnt]>0)&&(SumOrdersPlus[cnt]>0))
                 {
                  if(SumProfitPlus[cnt]<LevelProfitClosePlus[cnt])
                     DelayTimesForCloseInProfitPlus[cnt]=0;//Resete delay times before close
                  if((SumProfitPlus[cnt]>=LevelProfitClosePlus[cnt])||(ExpertClosePlusInProfit[cnt]==true))//Close plus groups
                    {
                     DelayTimesForCloseInProfitPlus[cnt]++;
                     //---Close plus in profit
                     if((DelayTimesForCloseInProfitPlus[cnt]>=DelayCloseProfit)||(ExpertClosePlusInProfit[cnt]==true))
                       {
                        for(i=1; i<=PairsPerGroup; i++)
                          {
                           if(TotalOrdersPlus[cnt][i]>0)
                              ClosePairPlus(cnt,-1,i);
                          }
                        //---
                        ExpertClosePlusInProfit[cnt]=true;
                        continue;
                       }
                    }
                 }
               //---Start close minus in profit
               if((SumProfitMinus[cnt]>0)&&(SumOrdersMinus[cnt]>0))
                 {
                  if(SumProfitMinus[cnt]<LevelProfitCloseMinus[cnt])
                     DelayTimesForCloseInProfitMinus[cnt]=0;//Resete delay times before close
                  if((SumProfitMinus[cnt]>=LevelProfitCloseMinus[cnt])||(ExpertCloseMinusInProfit[cnt]==true))//Close minus groups
                    {
                     DelayTimesForCloseInProfitMinus[cnt]++;
                     //---Close minus in profit
                     if((DelayTimesForCloseInProfitMinus[cnt]>=DelayCloseProfit)||(ExpertCloseMinusInProfit[cnt]==true))
                       {
                        for(i=1; i<=PairsPerGroup; i++)
                          {
                           if(TotalOrdersMinus[cnt][i]>0)
                              ClosePairMinus(cnt,-1,i);
                          }
                        //---
                        ExpertCloseMinusInProfit[cnt]=true;
                        continue;
                       }
                    }
                 }
              }
            //---Close in basket profit
            if((TypeCloseInProfit==1)||((OpenOrdersInLoss==2)&&(TypeCloseInProfit!=2)))
              {
               //---Close in auto step
               if(OpenOrdersInLoss==2)
                 {
                  //---Start close plus in profit (smaller ticket plus)
                  if(((SumProfitPlus[cnt]>0)&&(SumOrdersPlus[cnt]>0)&&(SumOrdersPlus[cnt]<=SumOrdersMinus[cnt]))||(ExpertClosePlusInProfit[cnt]==true))
                    {
                     if(SumProfitMinus[cnt]>LevelOpenNextMinus[cnt])
                        DelayTimesForCloseInProfitPlus[cnt]=0;//Resete delay times before close
                     if((SumProfitMinus[cnt]<=LevelOpenNextMinus[cnt])||(ExpertClosePlusInProfit[cnt]==true))//Close plus groups
                       {
                        DelayTimesForCloseInProfitPlus[cnt]++;
                        //---Close plus in profit
                        if((DelayTimesForCloseInProfitPlus[cnt]>=DelayCloseProfit)||(ExpertClosePlusInProfit[cnt]==true))
                          {
                           for(i=1; i<=PairsPerGroup; i++)
                             {
                              if(TotalOrdersPlus[cnt][i]>0)
                                 ClosePairPlus(cnt,-1,i);
                             }
                           //---
                           ExpertClosePlusInProfit[cnt]=true;
                           continue;
                          }
                       }
                    }
                  //---Start close minus in profit (smaller ticket minus)
                  if(((SumProfitMinus[cnt]>0)&&(SumOrdersMinus[cnt]>0)&&(SumOrdersPlus[cnt]>=SumOrdersMinus[cnt]))||(ExpertCloseMinusInProfit[cnt]==true))
                    {
                     if(SumProfitPlus[cnt]>LevelOpenNextPlus[cnt])
                        DelayTimesForCloseInProfitMinus[cnt]=0;//Resete delay times before close
                     if((SumProfitPlus[cnt]<=LevelOpenNextPlus[cnt])||(ExpertCloseMinusInProfit[cnt]==true))//Close minus groups
                       {
                        DelayTimesForCloseInProfitMinus[cnt]++;
                        //---Close minus in profit
                        if((DelayTimesForCloseInProfitMinus[cnt]>=DelayCloseProfit)||(ExpertCloseMinusInProfit[cnt]==true))
                          {
                           for(i=1; i<=PairsPerGroup; i++)
                             {
                              if(TotalOrdersMinus[cnt][i]>0)
                                 ClosePairMinus(cnt,-1,i);
                             }
                           //---
                           ExpertCloseMinusInProfit[cnt]=true;
                           continue;
                          }
                       }
                    }
                 }
               //---Close all in basket profit (all tickets)
               if(((SumProfitPlus[cnt]+SumProfitMinus[cnt]>0)&&(SumOrdersPlus[cnt]+SumOrdersMinus[cnt]>0))||(ExpertCloseBasketInProfit[cnt]==true))
                 {
                  if((SumProfitPlus[cnt]+SumProfitMinus[cnt]<MathMax(LevelProfitClosePlus[cnt],LevelProfitCloseMinus[cnt])))
                     DelayTimesForCloseBasketProfit[cnt]=0;//Resete delay times before close
                  if((SumProfitPlus[cnt]+SumProfitMinus[cnt]>=MathMax(LevelProfitClosePlus[cnt],LevelProfitCloseMinus[cnt]))||(ExpertCloseBasketInProfit[cnt]==true))
                    {
                     DelayTimesForCloseBasketProfit[cnt]++;
                     //---Close plus and minus in profit
                     if((DelayTimesForCloseBasketProfit[cnt]>=DelayCloseProfit)||(ExpertCloseBasketInProfit[cnt]==true))
                       {
                        for(i=1; i<=PairsPerGroup; i++)
                          {
                           if(TotalOrdersPlus[cnt][i]>0)
                              ClosePairPlus(cnt,-1,i);
                           if(TotalOrdersMinus[cnt][i]>0)
                              ClosePairMinus(cnt,-1,i);
                          }
                        //---
                        ExpertCloseBasketInProfit[cnt]=true;
                        continue;
                       }
                    }
                 }
              }
            //---Close pair by pair
            if(TypeCloseInProfit==2)
              {
               for(i=1; i<=PairsPerGroup; i++)
                 {
                  //---Close plus in profit
                  if(((TotalOrdersPlus[cnt][i]>0)&&(TotalProfitPlus[cnt][i]>=LevelProfitClosePairPlus[cnt][i]))||(ExpertClosePairPlusInProfit[cnt][i]==true))
                    {
                     ClosePairPlus(cnt,-1,i);
                     ExpertClosePairPlusInProfit[cnt][i]=true;
                    }
                  //---Close minus in profit
                  if(((TotalOrdersMinus[cnt][i]>0)&&(TotalProfitMinus[cnt][i]>=LevelProfitClosePairMinus[cnt][i]))||(ExpertClosePairMinusInProfit[cnt][i]==true))
                    {
                     ClosePairMinus(cnt,-1,i);
                     ExpertClosePairMinusInProfit[cnt][i]=true;
                    }
                 }
              }
            //---
           }//end if(TypeOfOperation!=0)
        }//end if(MarketIsOpen==true)
      //---------------------------------------------------------------------
      //Close and stop
      if(TypeOfOperation==3)
        {
         //---There are not open orders
         if(SumOrdersPlus[cnt]+SumOrdersMinus[cnt]==0)
           {
            if(cnt==NumberGroupsTrade-1)
              {
               Comment(
                  "\n                  ",WindowExpertName()+
                  "\n\n             ~ Have Close All Orders ~ "+
                  "\n\n             ~ History Orders Results ~ "+
                  "\n  Pips: "+DoubleToString(HistoryTotalPips,2)+" || Orders: "+DoubleToString(HistoryTotalTrades,0)+" || PnL: "+DoubleToString(HistoryTotalProfitLoss,2)
               );
              }
           }
         //---There are open orders
         if(SumOrdersPlus[cnt]+SumOrdersMinus[cnt]>0)
           {
            Comment("\n                  ",WindowExpertName(),
                    "\n\n             ~ Wait For Close Orders ~ ");
            //---Close orders
            for(i=1; i<=PairsPerGroup; i++)
              {
               if(TotalOrdersPlus[cnt][i]>0)
                  ClosePairPlus(cnt,-1,i);
               if(TotalOrdersMinus[cnt][i]>0)
                  ClosePairMinus(cnt,-1,i);
              }
           }//end if(TotalOrdersPlus+TotalOrdersMinus>0)
         continue;
        }//end if(StopAndClose==true)
      //---------------------------------------------------------------------
      //Check missing or excess orders
      if((CheckOrders==true)&&(ExpertClosePlusInLoss[cnt]==false)&&(ExpertCloseMinusInLoss[cnt]==false)&&(ExpertClosePlusInProfit[cnt]==false)&&(ExpertCloseMinusInProfit[cnt]==false)&&
         (ExpertCloseBasketInProfit[cnt]==false)&&(ExpertCloseBasketInLoss[cnt]==false)&&(FirsOrdersPlusOK[cnt]==true)&&(FirsOrdersMinusOK[cnt]==true)&&
         (ExpertClosePairPlusInProfit[cnt][1]==false)&&(ExpertClosePairPlusInProfit[cnt][2]==false)&&(ExpertClosePairPlusInProfit[cnt][3]==false)&&
         (ExpertClosePairMinusInProfit[cnt][1]==false)&&(ExpertClosePairMinusInProfit[cnt][2]==false)&&(ExpertClosePairMinusInProfit[cnt][3]==false))
        {
         //---Not check
         for(i=1; i<=PairsPerGroup; i++)
           {
            if(i>=2)
              {
               if((TotalOrdersPlus[cnt][i-1]==TotalOrdersPlus[cnt][i])&&(TotalOrdersMinus[cnt][i-1]==TotalOrdersMinus[cnt][i]))
                 {
                  OrdersIsOK[cnt]=true;
                 }
               else
                 {
                  OrdersIsOK[cnt]=false;
                  break;
                 }
              }
           }
         //---
         if(OrdersIsOK[cnt]==true)
           {
            continue;
           }
         else
           {
            if(LimitOfOrdersOk==true)
              {
               CheckMissingOrders(cnt);
              }
            else
              {
               CommentWarning=true;
               if(WarningPrint=="")
                  WarningPrint=StringConcatenate("Orders are in limit (",IntegerToString(OrdersTotal()),"/",IntegerToString(MaximumOrders),")");
               Print(WarningPrint);
               CheckExcessOrders(cnt);
              }
           }
        }//end if((TypeWorking==2)||(TypeWorking==0))
      //---------------------------------------------------------------------
      if((CheckOrders==false)||(OpenOrdersInLoss==0))
         OrdersIsOK[cnt]=true;
     }//end for(i=0; i<NumberGroupsTrade; i++)
//---------------------------------------------------------------------
//Call comment function
   if(TypeOfOperation<3)
      CommentChart();
//---------------------------------------------------------------------
  }
//===============================================================================================================================================================================================================================================================//
//Normalize Lots
//===============================================================================================================================================================================================================================================================//
double NormalizeLot(double LotsSize)
  {
//---------------------------------------------------------------------
   if(IsConnected())
     {
      return(MathMin(MathMax((MathRound(LotsSize/MarketInfo(Symbol(),MODE_LOTSTEP))*MarketInfo(Symbol(),MODE_LOTSTEP)),MarketInfo(Symbol(),MODE_MINLOT)),MarketInfo(Symbol(),MODE_MAXLOT)));
     }
   else
     {
      return(NormalizeDouble(LotsSize,2));
     }
//---------------------------------------------------------------------
  }
//===============================================================================================================================================================================================================================================================//
//Open plus orders
//===============================================================================================================================================================================================================================================================//
void OpenPairPlus(int PairOpen, int OpenOrder)
  {
//---------------------------------------------------------------------
   double PriceOpen=0;
   double StopLoss=0;
   double TakeProfit=0;
   double LotSizeOrder=0;
   double LotSize=0;
   double FreeMargin=0;
   double MaximumLot=9999999;
   color ColorOrder=0;
   string CommentOrder="";
   double MultiplierTickValuePlus[99][4]= {1};
//---------------------------------------------------------------------
//Set maximu lot size
   if(MaximumLotSize==0.0)
      MaximumLot=MarketInfo(SymbolPair[PairOpen][OpenOrder],MODE_MAXLOT);
   if(MaximumLotSize!=0.0)
      MaximumLot=MaximumLotSize;
//---------------------------------------------------------------------
//Calculate tick value multiplier
   if(UseFairLotSize==false)
      MultiplierTickValuePlus[PairOpen][OpenOrder]=1.0;
   if(UseFairLotSize==true)
      MultiplierTickValuePlus[PairOpen][OpenOrder]=TickValuePair[PairOpen][OpenOrder];
//---------------------------------------------------------------------
//Calculate lot size per pair
   if(LotOrdersProgress==0)
      MultiplierLotPlus[PairOpen]=1;
   if(LotOrdersProgress==1)
      MultiplierLotPlus[PairOpen]=GroupsPlus[PairOpen]+1;
   if(LotOrdersProgress==2)
      MultiplierLotPlus[PairOpen]=MathMax(1,MathPow(2,GroupsPlus[PairOpen]));
   if(LotOrdersProgress==3)
      MultiplierLotPlus[PairOpen]=1.0/MathMax(1,MathPow(2,GroupsPlus[PairOpen]));
//---------------------------------------------------------------------
//Set lots for orders
//---Auto or manual lot
   if(AutoLotSize==1)
      LotSize=((AccountBalance()*AccountLeverage())/100000000)*RiskFactor;
   if(AutoLotSize==0)
      LotSize=ManualLotSize;
//---
   if(OpenOrdersInLoss==2)
     {
      if((GroupsPlus[PairOpen]>0)||(GroupsMinus[PairOpen]>0))
         LotSizeOrder=NormalizeLot(MathMin(MaximumLot,FirstLotPair[PairOpen][OpenOrder]*MultiplierLotPlus[PairOpen]));
      else
         LotSizeOrder=NormalizeLot(MathMin(MaximumLot,LotSize/MultiplierTickValuePlus[PairOpen][OpenOrder]));
     }
   else
     {
      if(GroupsPlus[PairOpen]>0)
         LotSizeOrder=NormalizeLot(MathMin(MaximumLot,FirstLotPair[PairOpen][OpenOrder]*MultiplierLotPlus[PairOpen]));
      else
         LotSizeOrder=NormalizeLot(MathMin(MaximumLot,LotSize/MultiplierTickValuePlus[PairOpen][OpenOrder]));
     }
//---------------------------------------------------------------------
//Count free margin
   FreeMargin=AccountFreeMarginCheck(SymbolPair[PairOpen][OpenOrder],SuitePlus[PairOpen][OpenOrder],LotSizeOrder);
//---------------------------------------------------------------------
   CommentOrder=CommentsEA;
//---
   if(SuitePlus[PairOpen][OpenOrder]==OP_BUY)
     {
      PriceOpen=MarketInfo(SymbolPair[PairOpen][OpenOrder],MODE_ASK);
      ColorOrder=clrBlue;
     }
//---
   if(SuitePlus[PairOpen][OpenOrder]==OP_SELL)
     {
      PriceOpen=MarketInfo(SymbolPair[PairOpen][OpenOrder],MODE_BID);
      ColorOrder=clrRed;
     }
//---------------------------------------------------------------------
   if(LimitOfOrdersOk==true)
     {
      if(FreeMargin>=0)
        {
         CntTry=0;
         if((IsConnected())&&(!IsTradeContextBusy()))
           {
            while(true)
              {
               CntTry++;
               TicketNo[PairOpen]=OrderSend(SymbolPair[PairOpen][OpenOrder],SuitePlus[PairOpen][OpenOrder],LotSizeOrder,PriceOpen,MaxSlippage,StopLoss,TakeProfit,CommentOrder,OrdersID[PairOpen],0,ColorOrder);
               //---
               if(TicketNo[PairOpen]>0)
                 {
                  if(PrintLogReport==true)
                     Print("Open Plus",SymbolPair[PairOpen][OpenOrder]," || Ticket No: ",TicketNo[PairOpen]);
                  break;
                 }
               //---
               Sleep(100);
               if(CntTry>3)
                  break;
               RefreshRates();
              }
           }
        }
      else
        {
         CommentWarning=true;
         if(WarningPrint=="")
            WarningPrint=StringConcatenate("  Free margin is low (",DoubleToString(FreeMargin,2),")");
         Print(WarningPrint);
         CheckExcessOrders(PairOpen);
        }
     }
//---------------------------------------------------------------------
  }
//===============================================================================================================================================================================================================================================================//
//Open minus orders
//===============================================================================================================================================================================================================================================================//
void OpenPairMinus(int PairOpen, int OpenOrder)
  {
//---------------------------------------------------------------------
   double PriceOpen=0;
   double StopLoss=0;
   double TakeProfit=0;
   double LotSizeOrder=0;
   double LotSize=0;
   double FreeMargin=0;
   double MaximumLot=9999999;
   color ColorOrder=0;
   string CommentOrder="";
   double MultiplierTickValueMinus[99][4]= {1};
//---------------------------------------------------------------------
//Set maximu lot size
   if(MaximumLotSize==0.0)
      MaximumLot=MarketInfo(SymbolPair[PairOpen][OpenOrder],MODE_MAXLOT);
   if(MaximumLotSize!=0.0)
      MaximumLot=MaximumLotSize;
//---------------------------------------------------------------------
//Calculate tick value multiplier
   if(UseFairLotSize==false)
      MultiplierTickValueMinus[PairOpen][OpenOrder]=1.0;
   if(UseFairLotSize==true)
      MultiplierTickValueMinus[PairOpen][OpenOrder]=TickValuePair[PairOpen][OpenOrder];
//---------------------------------------------------------------------
//Calculate lot size per pair
   if(LotOrdersProgress==0)
      MultiplierLotMinus[PairOpen]=1;
   if(LotOrdersProgress==1)
      MultiplierLotMinus[PairOpen]=GroupsMinus[PairOpen]+1;
   if(LotOrdersProgress==2)
      MultiplierLotMinus[PairOpen]=MathMax(1,MathPow(2,GroupsMinus[PairOpen]));
   if(LotOrdersProgress==3)
      MultiplierLotMinus[PairOpen]=1.0/MathMax(1,MathPow(2,GroupsMinus[PairOpen]));
//---------------------------------------------------------------------
//Set lots for orders
//---Auto or manual lot
   if(AutoLotSize==1)
      LotSize=((AccountBalance()*AccountLeverage())/100000000)*RiskFactor;
   if(AutoLotSize==0)
      LotSize=ManualLotSize;
//---
   if(OpenOrdersInLoss==2)
     {
      if((GroupsMinus[PairOpen]>0)||(GroupsPlus[PairOpen]>0))
         LotSizeOrder=NormalizeLot(MathMin(MaximumLot,FirstLotPair[PairOpen][OpenOrder]*MultiplierLotMinus[PairOpen]));
      else
         LotSizeOrder=NormalizeLot(MathMin(MaximumLot,LotSize/MultiplierTickValueMinus[PairOpen][OpenOrder]));
     }
   else
     {
      if(GroupsMinus[PairOpen]>0)
         LotSizeOrder=NormalizeLot(MathMin(MaximumLot,FirstLotPair[PairOpen][OpenOrder]*MultiplierLotMinus[PairOpen]));
      else
         LotSizeOrder=NormalizeLot(MathMin(MaximumLot,LotSize/MultiplierTickValueMinus[PairOpen][OpenOrder]));
     }
//---------------------------------------------------------------------
//Count free margin
   FreeMargin=AccountFreeMarginCheck(SymbolPair[PairOpen][OpenOrder],SuiteMinus[PairOpen][OpenOrder],LotSizeOrder);
//---------------------------------------------------------------------
   CommentOrder=CommentsEA;
//---
   if(SuiteMinus[PairOpen][OpenOrder]==OP_BUY)
     {
      PriceOpen=MarketInfo(SymbolPair[PairOpen][OpenOrder],MODE_ASK);
      ColorOrder=clrBlue;
     }
//---
   if(SuiteMinus[PairOpen][OpenOrder]==OP_SELL)
     {
      PriceOpen=MarketInfo(SymbolPair[PairOpen][OpenOrder],MODE_BID);
      ColorOrder=clrRed;
     }
//---------------------------------------------------------------------
   if(LimitOfOrdersOk==true)
     {
      if(FreeMargin>=0)
        {
         CntTry=0;
         if((IsConnected())&&(!IsTradeContextBusy()))
           {
            while(true)
              {
               CntTry++;
               TicketNo[PairOpen]=OrderSend(SymbolPair[PairOpen][OpenOrder],SuiteMinus[PairOpen][OpenOrder],LotSizeOrder,PriceOpen,MaxSlippage,StopLoss,TakeProfit,CommentOrder,OrdersID[PairOpen],0,ColorOrder);
               //---
               if(TicketNo[PairOpen]>0)
                 {
                  if(PrintLogReport==true)
                     Print("Open Minus",SymbolPair[PairOpen][OpenOrder]," || Ticket No: ",TicketNo[PairOpen]);
                  break;
                 }
               //---
               Sleep(100);
               if(CntTry>3)
                  break;
               RefreshRates();
              }
           }
        }
      else
        {
         CommentWarning=true;
         if(WarningPrint=="")
            WarningPrint=StringConcatenate("  Free margin is low (",DoubleToString(FreeMargin,2),")");
         Print(WarningPrint);
         CheckExcessOrders(PairOpen);
        }
     }
//---------------------------------------------------------------------
  }
//===============================================================================================================================================================================================================================================================//
//Close plus orders
//===============================================================================================================================================================================================================================================================//
void ClosePairPlus(int PairClose, int TicketClose, int CloseOrder)
  {
//---------------------------------------------------------------------
   double PriceClose=0;
   color ColorOrder=0;
//---------------------------------------------------------------------
   for(k=OrdersTotal()-1; k>=0; k--)
     {
      if(OrderSelect(k,SELECT_BY_POS,MODE_TRADES))
        {
         if((OrderMagicNumber()==OrdersID[PairClose])&&(OrderSymbol()==SymbolPair[PairClose][CloseOrder])&&(OrderType()==SuitePlus[PairClose][CloseOrder]))
           {
            if((OrderTicket()==TicketClose)||(TicketClose==-1))
              {
               //---
               if(OrderType()==OP_BUY)
                 {
                  PriceClose=MarketInfo(OrderSymbol(),MODE_BID);
                  ColorOrder=clrPowderBlue;
                 }
               //---
               if(OrderType()==OP_SELL)
                 {
                  PriceClose=MarketInfo(OrderSymbol(),MODE_ASK);
                  ColorOrder=clrPink;
                 }
               //---------------------------------------------------------------------
               CntTry=0;
               if((IsConnected())&&(!IsTradeContextBusy()))
                 {
                  while(true)
                    {
                     CntTry++;
                     TicketNo[PairClose]=OrderClose(OrderTicket(),OrderLots(),PriceClose,MaxSlippage,ColorOrder);
                     //---
                     if(TicketNo[PairClose]>0)
                       {
                        if(PrintLogReport==true)
                           Print("Close Plus: ",SymbolPair[PairClose][CloseOrder]," || TicketNo: ",OrderTicket());
                        break;
                       }
                     //---
                     Sleep(100);
                     if(CntTry>3)
                        break;
                     RefreshRates();
                    }
                 }
              }
           }
        }
     }
//---------------------------------------------------------------------
  }
//===============================================================================================================================================================================================================================================================//
//Close minus orders
//===============================================================================================================================================================================================================================================================//
void ClosePairMinus(int PairClose, int TicketClose, int CloseOrder)
  {
//---------------------------------------------------------------------
   double PriceClose=0;
   color ColorOrder=0;
//---------------------------------------------------------------------
   for(k=OrdersTotal()-1; k>=0; k--)
     {
      if(OrderSelect(k,SELECT_BY_POS,MODE_TRADES))
        {
         if((OrderMagicNumber()==OrdersID[PairClose])&&(OrderSymbol()==SymbolPair[PairClose][CloseOrder])&&(OrderType()==SuiteMinus[PairClose][CloseOrder]))
           {
            if((OrderTicket()==TicketClose)||(TicketClose==-1))
              {
               //---
               if(OrderType()==OP_BUY)
                 {
                  PriceClose=MarketInfo(OrderSymbol(),MODE_BID);
                  ColorOrder=clrPowderBlue;
                 }
               //---
               if(OrderType()==OP_SELL)
                 {
                  PriceClose=MarketInfo(OrderSymbol(),MODE_ASK);
                  ColorOrder=clrPink;
                 }
               //---------------------------------------------------------------------
               CntTry=0;
               if((IsConnected())&&(!IsTradeContextBusy()))
                 {
                  while(true)
                    {
                     CntTry++;
                     TicketNo[PairClose]=OrderClose(OrderTicket(),OrderLots(),PriceClose,MaxSlippage,ColorOrder);
                     //---
                     if(TicketNo[PairClose]>0)
                       {
                        if(PrintLogReport==true)
                           Print("Close Minus: ",SymbolPair[PairClose][CloseOrder]," || TicketNo: ",OrderTicket());
                        break;
                       }
                     //---
                     Sleep(100);
                     if(CntTry>3)
                        break;
                     RefreshRates();
                    }
                 }
              }
           }
        }
     }
//---------------------------------------------------------------------
  }
//===============================================================================================================================================================================================================================================================//
//Check all open groups for missings orders
//===============================================================================================================================================================================================================================================================//
void CheckMissingOrders(int PairCheck)
  {
//---------------------------------------------------------------------
   int MaxOrdersPlus=-99999;
   int MaxOrdersMinus=-99999;
//---------------------------------------------------------------------
   for(i=1; i<=PairsPerGroup; i++)
     {
      if(MaxOrdersPlus<TotalOrdersPlus[PairCheck][i])
         MaxOrdersPlus=TotalOrdersPlus[PairCheck][i];
      if(MaxOrdersMinus<TotalOrdersMinus[PairCheck][i])
         MaxOrdersMinus=TotalOrdersMinus[PairCheck][i];
     }
//---------------------------------------------------------------------
   for(i=1; i<=PairsPerGroup; i++)
     {
      if(TotalOrdersPlus[PairCheck][i]<MaxOrdersPlus)
        {
         OpenPairPlus(PairCheck,i);
         if(PrintLogReport==true)
            Print(StringConcatenate("Open Missing Plus"+IntegerToString(i)+" - ",SymbolPair[PairCheck][i]));
        }
      //---
      if(TotalOrdersMinus[PairCheck][i]<MaxOrdersMinus)
        {
         OpenPairMinus(PairCheck,i);
         if(PrintLogReport==true)
            Print(StringConcatenate("Open Missing Minus"+IntegerToString(i)+" - ",SymbolPair[PairCheck][i]));
        }
     }
//---------------------------------------------------------------------
  }
//===============================================================================================================================================================================================================================================================//
//Check all open groups for excess orders
//===============================================================================================================================================================================================================================================================//
void CheckExcessOrders(int PairCheck)
  {
//---------------------------------------------------------------------
   int MinOrdersPlus=99999;
   int MinOrdersMinus=99999;
//---------------------------------------------------------------------
   for(i=1; i<=PairsPerGroup; i++)
     {
      if(MinOrdersPlus>TotalOrdersPlus[PairCheck][i])
         MinOrdersPlus=TotalOrdersPlus[PairCheck][i];
      if(MinOrdersMinus>TotalOrdersMinus[PairCheck][i])
         MinOrdersMinus=TotalOrdersMinus[PairCheck][i];
     }
//---------------------------------------------------------------------
   for(i=1; i<=PairsPerGroup; i++)
     {
      if(TotalOrdersPlus[PairCheck][i]>MinOrdersPlus)
        {
         ClosePairPlus(PairCheck,FirstTicketPlus[PairCheck][i],i);
         if(PrintLogReport==true)
            Print(StringConcatenate("Close Excess Plus"+IntegerToString(i)+" - ",SymbolPair[PairCheck][i]));
        }
      //---
      if(TotalOrdersMinus[PairCheck][i]>MinOrdersMinus)
        {
         ClosePairMinus(PairCheck,FirstTicketMinus[PairCheck][i],i);
         if(PrintLogReport==true)
            Print(StringConcatenate("Close Excess Minus"+IntegerToString(i)+" - ",SymbolPair[PairCheck][i]));
        }
     }
//---------------------------------------------------------------------
  }
//===============================================================================================================================================================================================================================================================//
//Get current resuluts
//===============================================================================================================================================================================================================================================================//
void CountCurrentOrders(int PairGet)
  {
//---------------------------------------------------------------------
   CountAllOpenedOrders=0;
//---------------------------------------------------------------------
   for(j=1; j<=PairsPerGroup; j++)
     {
      TotalProfitPlus[PairGet][j]=0;
      TotalProfitMinus[PairGet][j]=0;
      FirstLotPlus[PairGet][j]=0;
      FirstLotMinus[PairGet][j]=0;
      FirstTicketPlus[PairGet][j]=0;
      FirstTicketMinus[PairGet][j]=0;
      LastTicketPlus[PairGet][j]=0;
      LastTicketMinus[PairGet][j]=0;
      FirstProfitPlus[PairGet][j]=0;
      FirstProfitMinus[PairGet][j]=0;
      TotalOrdersPlus[PairGet][j]=0;
      TotalOrdersMinus[PairGet][j]=0;
      TotalLotPlus[PairGet][j]=0;
      TotalLotMinus[PairGet][j]=0;
      TotalCommissionPlus[PairGet][j]=0;
      TotalCommissionMinus[PairGet][j]=0;
      FirstLotPair[PairGet][j]=0;
     }
//---------------------------------------------------------------------
   GroupsPlus[PairGet]=0;
   GroupsMinus[PairGet]=0;
   SumOrdersPlus[PairGet]=0;
   SumOrdersMinus[PairGet]=0;
   FirstTotalLotPlus[PairGet]=0;
   FirstTotalLotMinus[PairGet]=0;
   FirstTotalProfitPlus[PairGet]=0;
   FirstTotalProfitMinus[PairGet]=0;
   SumProfitPlus[PairGet]=0;
   SumProfitMinus[PairGet]=0;
   SumCommissionPlus[PairGet]=0;
   SumCommissionMinus[PairGet]=0;
   SumLotPlus[PairGet]=0;
   SumLotMinus[PairGet]=0;
   TimeOpenLastPlus[PairGet]=0;
   TimeOpenLastMinus[PairGet]=0;
//---------------------------------------------------------------------
//Get groups informations
   if(OrdersTotal()>0)
     {
      //---Last to first
      for(i=OrdersTotal()-1; i>=0; i--)
        {
         //---Start check trades
         if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
           {
            CountAllOpenedOrders++;
            if(OrderMagicNumber()==OrdersID[PairGet])
              {
               //---Total groups
               TotalGroupsOrders++;
               TotalGroupsProfit+=OrderProfit()+OrderSwap()+OrderCommission();
               //---Plus and minus
               for(j=1; j<=PairsPerGroup; j++)
                 {
                  //---Plus pair
                  if((OrderType()==SuitePlus[PairGet][j])&&(OrderSymbol()==SymbolPair[PairGet][j]))
                    {
                     FirstLotPair[PairGet][j]=OrderLots();
                     TotalProfitPlus[PairGet][j]+=OrderProfit()+OrderSwap()+OrderCommission();
                     TotalCommissionPlus[PairGet][j]+=MathAbs(OrderCommission()+OrderSwap());
                     FirstLotPlus[PairGet][j]=OrderLots();
                     TotalLotPlus[PairGet][j]+=OrderLots();
                     TotalOrdersPlus[PairGet][j]++;
                     FirstTicketPlus[PairGet][j]=OrderTicket();
                     FirstProfitPlus[PairGet][j]=OrderProfit()+OrderSwap()+OrderCommission();
                     if(LastTicketPlus[PairGet][j]==0)
                        LastTicketPlus[PairGet][j]=OrderTicket();
                     if(LastLotPlus[PairGet][j]==0)
                        LastLotPlus[PairGet][j]=OrderLots();
                     if(TimeOpenLastPlus[PairGet]==0)
                        TimeOpenLastPlus[PairGet]=OrderOpenTime();
                    }
                  //---Minus pair
                  if((OrderType()==SuiteMinus[PairGet][j])&&(OrderSymbol()==SymbolPair[PairGet][j]))
                    {
                     FirstLotPair[PairGet][j]=OrderLots();
                     TotalProfitMinus[PairGet][j]+=OrderProfit()+OrderSwap()+OrderCommission();
                     TotalCommissionMinus[PairGet][j]+=MathAbs(OrderCommission()+OrderSwap());
                     FirstLotMinus[PairGet][j]=OrderLots();
                     TotalLotMinus[PairGet][j]+=OrderLots();
                     TotalOrdersMinus[PairGet][j]++;
                     FirstTicketMinus[PairGet][j]=OrderTicket();
                     FirstProfitMinus[PairGet][j]=OrderProfit()+OrderSwap()+OrderCommission();
                     if(LastTicketMinus[PairGet][j]==0)
                        LastTicketMinus[PairGet][j]=OrderTicket();
                     if(LastLotMinus[PairGet][j]==0)
                        LastLotMinus[PairGet][j]=OrderLots();
                     if(TimeOpenLastMinus[PairGet]==0)
                        TimeOpenLastMinus[PairGet]=OrderOpenTime();
                    }
                 }
              }
           }
        }
      //if(CountAllOpenedOrders!=OrdersTotal()) return;//Pass again orders
     }//end if(OrdersTotal()>0)
//---------------------------------------------------------------------
//Processing groups informations
   for(j=1; j<=PairsPerGroup; j++)
     {
      SumOrdersPlus[PairGet]+=TotalOrdersPlus[PairGet][j];
      SumOrdersMinus[PairGet]+=TotalOrdersMinus[PairGet][j];
      FirstTotalLotPlus[PairGet]+=FirstLotPlus[PairGet][j];
      FirstTotalLotMinus[PairGet]+=FirstLotMinus[PairGet][j];
      FirstTotalProfitPlus[PairGet]+=FirstProfitPlus[PairGet][j];
      FirstTotalProfitMinus[PairGet]+=FirstProfitMinus[PairGet][j];
      SumProfitPlus[PairGet]+=TotalProfitPlus[PairGet][j];
      SumProfitMinus[PairGet]+=TotalProfitMinus[PairGet][j];
      SumCommissionPlus[PairGet]+=TotalCommissionPlus[PairGet][j];
      SumCommissionMinus[PairGet]+=TotalCommissionMinus[PairGet][j];
      SumLotPlus[PairGet]+=TotalLotPlus[PairGet][j];
      SumLotMinus[PairGet]+=TotalLotMinus[PairGet][j];
     }
//---------------------------------------------------------------------
   GroupsPlus[PairGet]=SumOrdersPlus[PairGet]/PairsPerGroup;
   GroupsMinus[PairGet]=SumOrdersMinus[PairGet]/PairsPerGroup;
//---------------------------------------------------------------------
  }
//===============================================================================================================================================================================================================================================================//
//Get history resuluts
//===============================================================================================================================================================================================================================================================//
void CountHistoryOrders(int PairGet)
  {
//---------------------------------------------------------------------
//Reset value
   CountAllHistoryOrders=0;
   HistoryPlusOrders[PairGet]=0;
   HistoryMinusOrders[PairGet]=0;
   HistoryPlusProfit[PairGet]=0;
   HistoryMinusProfit[PairGet]=0;
//---
   if(PairGet==0)
     {
      HistoryTotalPips=0;
      HistoryTotalTrades=0;
      HistoryTotalProfitLoss=0;
      FirstOpenedOrder=0;
     }
//---------------------------------------------------------------------
   if(OrdersHistoryTotal()>0)
     {
      for(i=OrdersHistoryTotal()-1; i>=0; i--)
        {
         if(OrderSelect(i,SELECT_BY_POS,MODE_HISTORY))
           {
            CountAllHistoryOrders++;
            //---
            if(OrderMagicNumber()==OrdersID[PairGet])
              {
               //---------------------------------------------------------------------
               //Count orders and profit/loss
               HistoryTotalTrades++;
               HistoryTotalProfitLoss+=OrderProfit()+OrderCommission()+OrderSwap();
               if(FirstOpenedOrder==0)
                  FirstOpenedOrder=OrderOpenTime();
               if((OrderOpenTime()<FirstOpenedOrder)&&(FirstOpenedOrder!=0))
                  FirstOpenedOrder=OrderOpenTime();
               //---------------------------------------------------------------------
               //Count plus orders
               for(j=1; j<=PairsPerGroup; j++)
                 {
                  if((OrderType()==SuitePlus[PairGet][j])&&(OrderSymbol()==SymbolPair[PairGet][j]))
                    {
                     HistoryPlusOrders[PairGet]++;
                     HistoryPlusProfit[PairGet]+=OrderProfit()+OrderCommission()+OrderSwap();
                    }
                  //---------------------------------------------------------------------
                  //Count minus orders
                  if((OrderType()==SuiteMinus[PairGet][j])&&(OrderSymbol()==SymbolPair[PairGet][j]))
                    {
                     HistoryMinusOrders[PairGet]++;
                     HistoryMinusProfit[PairGet]+=OrderProfit()+OrderCommission()+OrderSwap();
                    }
                 }
               //---------------------------------------------------------------------
               //Count pips
               if(OrderType()==OP_BUY)
                  HistoryTotalPips+=(OrderClosePrice()-OrderOpenPrice())/(MarketInfo(OrderSymbol(),MODE_POINT)*MultiplierPoint);
               if(OrderType()==OP_SELL)
                  HistoryTotalPips+=(OrderOpenPrice()-OrderClosePrice())/(MarketInfo(OrderSymbol(),MODE_POINT)*MultiplierPoint);
               //---------------------------------------------------------------------
              }
           }
        }
     }
//---------------------------------------------------------------------
  }
//===============================================================================================================================================================================================================================================================//
//Background for comments
//===============================================================================================================================================================================================================================================================//
void ChartBackground(string StringName, color ImageColor, int TypeBorder, bool InBackGround, int Xposition, int Yposition, int Xsize, int Ysize)
  {
//---------------------------------------------------------------------
   ObjectCreate(0,StringName,OBJ_RECTANGLE_LABEL,0,0,0,0,0);
   ObjectSetInteger(0,StringName,OBJPROP_XDISTANCE,Xposition);
   ObjectSetInteger(0,StringName,OBJPROP_YDISTANCE,Yposition);
   ObjectSetInteger(0,StringName,OBJPROP_XSIZE,Xsize);
   ObjectSetInteger(0,StringName,OBJPROP_YSIZE,Ysize);
   ObjectSetInteger(0,StringName,OBJPROP_BGCOLOR,ImageColor);
   ObjectSetInteger(0,StringName,OBJPROP_BORDER_TYPE,TypeBorder);
   ObjectSetInteger(0,StringName,OBJPROP_BORDER_COLOR,clrBlack);
   ObjectSetInteger(0,StringName,OBJPROP_BACK,InBackGround);
   ObjectSetInteger(0,StringName,OBJPROP_SELECTABLE,false);
   ObjectSetInteger(0,StringName,OBJPROP_SELECTED,false);
   ObjectSetInteger(0,StringName,OBJPROP_HIDDEN,true);
   ObjectSetInteger(0,StringName,OBJPROP_ZORDER,0);
//---------------------------------------------------------------------
  }
//===============================================================================================================================================================================================================================================================//
//Display Text/image
//===============================================================================================================================================================================================================================================================//
void DisplayText(string StringName, string Image, int FontSize, string TypeImage, color FontColor, int Xposition, int Yposition)
  {
//---------------------------------------------------------------------
   ObjectCreate(0,StringName,OBJ_LABEL,0,0,0);
   ObjectSetInteger(0,StringName,OBJPROP_CORNER,0);
   ObjectSetInteger(0,StringName,OBJPROP_BACK,false);
   ObjectSetInteger(0,StringName,OBJPROP_XDISTANCE,Xposition);
   ObjectSetInteger(0,StringName,OBJPROP_YDISTANCE,Yposition);
   ObjectSetInteger(0,StringName,OBJPROP_HIDDEN,true);
   ObjectSetInteger(0,StringName,OBJPROP_SELECTABLE,false);
   ObjectSetText(StringName,Image,FontSize,TypeImage,FontColor);
//---------------------------------------------------------------------
  }
//===============================================================================================================================================================================================================================================================//
//Check value
//===============================================================================================================================================================================================================================================================//
void CheckValue()
  {
//---------------------------------------------------------------------
   WrongSet=false;
//---------------------------------------------------------------------
//Check step value
   if((OpenOrdersInLoss==1)&&(StepOpenNextOrders<=0)&&(WrongSet==false))
     {
      Comment("\n "+StringOrdersEA+
              "\n\n\nStepOpenNextOrders parameter not correct ("+DoubleToString(StepOpenNextOrders,2)+")"
              "\n\nPlease insert a value greater than 0");
      Print(" # "+WindowExpertName()+" # "+"StepOpenNextOrders parameter not correct ("+DoubleToString(StepOpenNextOrders,2)+")");
      WrongSet=true;
      WarningMessage=MessageBox(" # "+WindowExpertName()+" # "+"StepOpenNextOrders parameter not correct ("+DoubleToString(StepOpenNextOrders,2)+"), please insert a value greater than 0", "RISK DISCLAIMER");
     }
//---------------------------------------------------------------------
//Check profit close value
   if((TargetCloseProfit<=0)&&(WrongSet==false))
     {
      Comment("\n "+StringOrdersEA+
              "\n\n\nTargetCloseProfit parameter not correct ("+DoubleToString(TargetCloseProfit,2)+")"
              "\n\nPlease insert a value greater than 0");
      Print(" # "+WindowExpertName()+" # "+"TargetCloseProfit parameter not correct ("+DoubleToString(TargetCloseProfit,2)+")");
      WrongSet=true;
      WarningMessage=MessageBox(" # "+WindowExpertName()+" # "+"TargetCloseProfit parameter not correct ("+DoubleToString(TargetCloseProfit,2)+"), please insert a value greater than 0", "RISK DISCLAIMER");
     }
//---------------------------------------------------------------------
//Check loss close value
   if((TypeCloseInLoss<2)&&(TargetCloseLoss<=0)&&(WrongSet==false))
     {
      Comment("\n "+StringOrdersEA+
              "\n\n\nTargetCloseLoss parameter not correct ("+DoubleToString(TargetCloseLoss,2)+")"
              "\n\nPlease insert a value greater than 0");
      Print(" # "+WindowExpertName()+" # "+"TargetCloseLoss parameter not correct ("+DoubleToString(TargetCloseLoss,2)+")");
      WrongSet=true;
      WarningMessage=MessageBox(" # "+WindowExpertName()+" # "+"TargetCloseLoss parameter not correct ("+DoubleToString(TargetCloseLoss,2)+"), please insert a value greater than 0", "RISK DISCLAIMER");
     }
//---------------------------------------------------------------------
  }
//===============================================================================================================================================================================================================================================================//
//Check locked
//===============================================================================================================================================================================================================================================================//
void LockedCheck()
  {
//---------------------------------------------------------------------
//Check expire date
   if((TimeCurrent()>=ExpiryDate)&&(LockedDate==true))
     {
      //---Check orders
      if(StopWorking==false)
        {
         TotalOrders=0;
         for(int cnt4=0; cnt4<NumberGroupsTrade; cnt4++)
           {
            CountCurrentOrders(cnt4);
            TotalOrders+=(TotalOrdersPlus[cnt4][1]+TotalOrdersPlus[cnt4][2]+TotalOrdersPlus[cnt4][3]+TotalOrdersMinus[cnt4][1]+TotalOrdersMinus[cnt4][2]+TotalOrdersMinus[cnt4][3]);
           }
        }
      //---Thera are opened orders
      if(TotalOrders==0)
        {
         Comment("\n "+StringOrdersEA+
                 "\n\n\nExpert has expired ("+TimeToString(ExpiryDate,TIME_DATE)+")"
                 "\n\nPlease contact at"+
                 "\nnikolaospantzos@gmail.com");
         Print(" # "+WindowExpertName()+" # "+"Version has expired, please contact with author: nikolaospantzos@gmail.com");
         StopWorking=true;
         WarningMessage=MessageBox(" # "+WindowExpertName()+" # "+"Expert has expired ("+TimeToString(ExpiryDate,TIME_DATE)+"). Please contact with author: nikolaospantzos@gmail.com", "RISK DISCLAIMER");
        }
      //---Thera are not opened orders
      if(TotalOrders>0)
        {
         ChangeOperation=true;
         CommentWarning=true;
         WarningPrint=StringConcatenate("Expert Has Expired, Working To Close In Profit And Stop");
        }
     }
//---------------------------------------------------------------------
//Check number account
   if((!IsDemo())&&(AccountNumber()!=AccountNo)&&(LockedAccount==true))
     {
      Comment("\n "+StringOrdersEA+
              "\n\n\n Expert run only on specific account!!!"+
              "\n\n Please contact at"+
              "\n nikolaospantzos@gmail.com");
      Print(" # "+WindowExpertName()+" # "+"Locked version, please contact with author: nikolaospantzos@gmail.com");
      StopWorking=true;
      WarningMessage=MessageBox(" # "+WindowExpertName()+" # "+"Locked version. Expert run only on specific account. Please contact with author: nikolaospantzos@gmail.com", "RISK DISCLAIMER");
     }
//---------------------------------------------------------------------
  }
//===============================================================================================================================================================================================================================================================//
//Comments on the chart
//===============================================================================================================================================================================================================================================================//
void CommentChart()
  {
//---------------------------------------------------------------------
   color TextColor=clrNONE;
   double LevelCloseInLossPlus[99];
   double LevelCloseInLossMinus[99];
   double ShowMaxProfit=0;
   double ShowMinProfit=0;
   string FirstLine1Str;
   string CloseProfitStr;
   string CloseLossStr;
   string SpreadStr;
   string StepNextStr;
   string LotOrdersStr;
   string SetSpace;
   string SideInfoStr;
   string LevelCloseProfitStr;
   string LevelCloseLossStr;
   string FirstOpenedOrderStr="00.00.00";
   int PosLastColumn;
   int UpperPosition;
   int FileHandle;
   int UsePosLast=0;
   int OptiPosition=0;
//---------------------------------------------------------------------
//Reset values
   ArrayInitialize(LevelCloseInLossPlus,0);
   ArrayInitialize(LevelCloseInLossMinus,0);
   TotalOrders=0;
   TotalProfitLoss=0;
   TotalLots=0;
//---------------------------------------------------------------------
//Set time's string
   if(FirstOpenedOrder!=0)
      FirstOpenedOrderStr=TimeToString(FirstOpenedOrder,TIME_DATE);
//---------------------------------------------------------------------
//First line comment
   if(TypeOfOperation==0)
      FirstLine1Str=StringConcatenate("Expert Is In Stand By Mode");
   if(TypeOfOperation==1)
      FirstLine1Str=StringConcatenate("Expert Is Ready To Open/Close Orders");
   if(TypeOfOperation==2)
      FirstLine1Str=StringConcatenate("Expert Wait Close In Profit And Stop");
   if(TypeOfOperation==3)
      FirstLine1Str=StringConcatenate("Expert Close Immediately All Orders");
   if(CommentWarning==true)
      FirstLine1Str=StringConcatenate("Warning: ",WarningPrint);
//---------------------------------------------------------------------
//Close mode
   if(TypeCloseInProfit==0)
      CloseProfitStr="Single Ticket ("+DoubleToString(TargetCloseProfit,1)+")";
   if(TypeCloseInProfit==1)
      CloseProfitStr="Basket Ticket ("+DoubleToString(TargetCloseProfit,1)+")";
   if(TypeCloseInProfit==2)
      CloseProfitStr="Pair By Pair ("+DoubleToString(TargetCloseProfit,1)+")";
//---
   if(TypeCloseInLoss==0)
      CloseLossStr="Whole Ticket ("+DoubleToString(-TargetCloseLoss,1)+")";
   if(TypeCloseInLoss==1)
      CloseLossStr="Partial Ticket ("+DoubleToString(-TargetCloseLoss,2)+")";
   if(TypeCloseInLoss==2)
      CloseLossStr="Not Close In Loss";
//---------------------------------------------------------------------
//Open next and step
   if(OpenOrdersInLoss==0)
      StepNextStr="Not Open Next In Loss";
//---
   if(OpenOrdersInLoss==1)
     {
      if(StepOrdersProgress==0)
         StepNextStr="Manual / Statical ("+DoubleToString(StepOpenNextOrders,1)+")";
      if(StepOrdersProgress==1)
         StepNextStr="Manual / Geometrical ("+DoubleToString(StepOpenNextOrders,1)+")";
      if(StepOrdersProgress==2)
         StepNextStr="Manual / Exponential ("+DoubleToString(StepOpenNextOrders,1)+")";
     }
//---
   if(OpenOrdersInLoss==2)
     {
      if(StepOrdersProgress==0)
         StepNextStr="Automatic / Statical ("+DoubleToString(TargetCloseProfit,1)+")";
      if(StepOrdersProgress==1)
         StepNextStr="Automatic / Geometrical ("+DoubleToString(TargetCloseProfit,1)+")";
      if(StepOrdersProgress==2)
         StepNextStr="Automatic / Exponential ("+DoubleToString(TargetCloseProfit,1)+")";
     }
//---------------------------------------------------------------------
//Lot orders
   if(AutoLotSize==false)
     {
      if(LotOrdersProgress==0)
         LotOrdersStr="Manual / Statical ("+DoubleToString(ManualLotSize,2)+")";
      if(LotOrdersProgress==1)
         LotOrdersStr="Manual / Geometrical ("+DoubleToString(ManualLotSize,2)+")";
      if(LotOrdersProgress==2)
         LotOrdersStr="Manual / Exponential ("+DoubleToString(ManualLotSize,2)+")";
      if(LotOrdersProgress==3)
         LotOrdersStr="Manual / Decreases ("+DoubleToString(ManualLotSize,2)+")";
     }
//---
   if(AutoLotSize==true)
     {
      if(LotOrdersProgress==0)
         LotOrdersStr="Automatic / Statical ("+DoubleToString(((AccountBalance()*AccountLeverage())/100000000)*RiskFactor,2)+")";
      if(LotOrdersProgress==1)
         LotOrdersStr="Automatic / Geometrical ("+DoubleToString(((AccountBalance()*AccountLeverage())/100000000)*RiskFactor,2)+")";
      if(LotOrdersProgress==2)
         LotOrdersStr="Automatic / Exponential ("+DoubleToString(((AccountBalance()*AccountLeverage())/100000000)*RiskFactor,2)+")";
      if(LotOrdersProgress==3)
         LotOrdersStr="Automatic / Decreases ("+DoubleToString(((AccountBalance()*AccountLeverage())/100000000)*RiskFactor,2)+")";
     }
//---------------------------------------------------------------------
//Side info
   if(SideOpenOrders==0)
      SideInfoStr="Open Only Plus";
   if(SideOpenOrders==1)
      SideInfoStr="Open Only Minus";
   if(SideOpenOrders==2)
      SideInfoStr="Open Plus And Minus";
//---------------------------------------------------------------------
//Speread string
   if(MaxSpread==0)
      SpreadStr="Expert Not Check Spread";
   if(MaxSpread!=0)
      SpreadStr="Expert Check Spread ("+DoubleToString(MaxSpread,2)+")";
//---------------------------------------------------------------------
//Set up pairs information
   if(ShowTaskInfo==true)
     {
      for(i=0; i<NumberGroupsTrade; i++)
        {
         //---------------------------------------------------------------------
         //Close levels
         if(TypeCloseInLoss<2)
           {
            LevelCloseInLossPlus[i]=LevelLossClosePlus[i];
            LevelCloseInLossMinus[i]=LevelLossCloseMinus[i];
           }
         //---------------------------------------------------------------------
         //Calculate max and min value
         MaxOrders[i]=MathMax(MaxOrders[i],SumOrdersPlus[i]+SumOrdersMinus[i]);
         MaxFloating[i]=MathMin(MaxFloating[i],SumProfitPlus[i]+SumProfitMinus[i]);
         //---------------------------------------------------------------------
         //Count total orders, lots and floating
         TotalOrders+=SumOrdersPlus[i]+SumOrdersMinus[i];
         TotalProfitLoss+=SumProfitPlus[i]+SumProfitMinus[i];
         TotalLots+=TotalLotPlus[i][1]+TotalLotPlus[i][2]+TotalLotPlus[i][3]+TotalLotMinus[i][1]+TotalLotMinus[i][2]+TotalLotMinus[i][3];
         //---------------------------------------------------------------------
         //Calculate max and min value
         if(TotalOrders>0)
           {
            MaxProfit=MathMax(TotalProfitLoss,MaxProfit);
            MinProfit=MathMin(TotalProfitLoss,MinProfit);
           }
         //---
         if(MaxProfit==-99999)
            ShowMaxProfit=0;
         else
            ShowMaxProfit=MaxProfit;
         if(MinProfit==99999)
            ShowMinProfit=0;
         else
            ShowMinProfit=MinProfit;
         //---
         MaxTotalOrders=MathMax(MaxTotalOrders,TotalOrders);
         MaxTotalLots=MathMax(MaxTotalLots,TotalLots);
         //---------------------------------------------------------------------
         //Close levels
         if(SumOrdersPlus[i]>0)
            LevelCloseProfitStr=DoubleToString(LevelProfitClosePlus[i],2);
         if(SumOrdersMinus[i]>0)
            LevelCloseProfitStr=DoubleToString(LevelProfitCloseMinus[i],2);
         if((SumOrdersPlus[i]>0)&&(SumOrdersMinus[i]>0))
            LevelCloseProfitStr=DoubleToString(MathMax(LevelProfitClosePlus[i],LevelProfitCloseMinus[i]),2);
         if((SumOrdersPlus[i]==0)&&(SumOrdersMinus[i]==0))
            LevelCloseProfitStr=DoubleToString(0.0,2);
         //---
         if(SumOrdersPlus[i]>0)
            LevelCloseLossStr=DoubleToString(LevelLossClosePlus[i],2);
         if(SumOrdersMinus[i]>0)
            LevelCloseLossStr=DoubleToString(LevelLossCloseMinus[i],2);
         if((SumOrdersPlus[i]>0)&&(SumOrdersMinus[i]>0))
            LevelCloseLossStr=DoubleToString(MathMin(LevelLossClosePlus[i],LevelLossCloseMinus[i]),2);
         if((SumOrdersPlus[i]==0)&&(SumOrdersMinus[i]==0))
            LevelCloseLossStr=DoubleToString(0.0,2);
         //---------------------------------------------------------------------
         //Set info's position
         PosLastColumn=130;
         UpperPosition=15;
         OptiPosition=320;
         //---------------------------------------------------------------------
         //Set comments on chart
         if(ShowPairsInfo==true)
           {
            if(i<9)
               SetSpace="  ";
            if(i>=9)
               SetSpace="";
            //---Set last column
            UsePosLast=PositionSpread-75;
            //---
            if(SymbolPair[i][1]!="")
              {
               //---Str1
               if(ObjectFind(0,"Str1")==-1)
                  DisplayText("Str1","Pairs",10,"Arial Black",ColorOfTitle,340,UpperPosition);
               //---Str2
               if(ObjectFind(0,"Str2")==-1)
                  DisplayText("Str2","Orders",10,"Arial Black",ColorOfTitle,PositionOrders,UpperPosition);
               //---Str3
               if(ObjectFind(0,"Str3")==-1)
                  DisplayText("Str3","PnL",10,"Arial Black",ColorOfTitle,PositionPnL,UpperPosition);
               //---Str4
               if(ObjectFind(0,"Str4")==-1)
                  DisplayText("Str4","Close Levels",10,"Arial Black",ColorOfTitle,PositionClose,UpperPosition);
               //---Str5
               if(ObjectFind(0,"Str5")==-1)
                  DisplayText("Str5","Next Levels",10,"Arial Black",ColorOfTitle,PositionNext,UpperPosition);
               //---Str6
               if(ObjectFind(0,"Str6")==-1)
                  DisplayText("Str6","History",10,"Arial Black",ColorOfTitle,PositionHistory,UpperPosition);
               //---Str7
               if(ObjectFind(0,"Str7")==-1)
                  DisplayText("Str7","Maximum",10,"Arial Black",ColorOfTitle,PositionMaximum,UpperPosition);
               //---Str8
               if(ObjectFind(0,"Str8")==-1)
                  DisplayText("Str8","Spread",10,"Arial Black",ColorOfTitle,PositionSpread-25,UpperPosition);
               //---Comm1
               ObjectDelete(0,"Comm1"+IntegerToString(i));
               if(ObjectFind(0,"Comm1"+IntegerToString(i))==-1)
                  DisplayText("Comm1"+IntegerToString(i),IntegerToString(i+1)+". "+SetSpace+StringSubstr(SymbolPair[i][1],0,6)+"-"+StringSubstr(SymbolPair[i][2],0,6)+"-"+StringSubstr(SymbolPair[i][3],0,6),10,"Arial Black",ColorOfInfo,241,UpperPosition+18+(i*14));
               //---
               if(SkippedStatus[i]!="Group Skipped by user settings from external parameters")
                 {
                  //---Comm2
                  ObjectDelete(0,"Comm2"+IntegerToString(i));
                  if(ObjectFind(0,"Comm2"+IntegerToString(i))==-1)
                     DisplayText("Comm2"+IntegerToString(i),StringConcatenate(TotalOrdersPlus[i][1],"/",TotalOrdersPlus[i][2],"/",TotalOrdersPlus[i][3],"-",TotalOrdersMinus[i][1],"/",TotalOrdersMinus[i][2],"/",TotalOrdersMinus[i][3]),10,"Arial Black",ColorOfInfo,PositionOrders-10,UpperPosition+18+(i*14));
                  //---Comm3
                  ObjectDelete(0,"Comm3"+IntegerToString(i));
                  if(ObjectFind(0,"Comm3"+IntegerToString(i))==-1)
                     DisplayText("Comm3"+IntegerToString(i),DoubleToString(SumProfitPlus[i]+SumProfitMinus[i],2),10,"Arial Black",ColorOfInfo,PositionPnL-5,UpperPosition+18+(i*14));
                  //---Comm4
                  ObjectDelete(0,"Comm4"+IntegerToString(i));
                  if(ObjectFind(0,"Comm4"+IntegerToString(i))==-1)
                     DisplayText("Comm4"+IntegerToString(i),LevelCloseProfitStr+"/"+LevelCloseLossStr,10,"Arial Black",ColorOfInfo,PositionClose,UpperPosition+18+(i*14));
                  //---Comm5
                  ObjectDelete(0,"Comm5"+IntegerToString(i));
                  if(ObjectFind(0,"Comm5"+IntegerToString(i))==-1)
                     DisplayText("Comm5"+IntegerToString(i),DoubleToString(LevelOpenNextPlus[i],0)+"/"+DoubleToString(LevelOpenNextMinus[i],2),10,"Arial Black",ColorOfInfo,PositionNext,UpperPosition+18+(i*14));
                  //---Comm6
                  ObjectDelete(0,"Comm6"+IntegerToString(i));
                  if(ObjectFind(0,"Comm6"+IntegerToString(i))==-1)
                     DisplayText("Comm6"+IntegerToString(i),DoubleToString(HistoryPlusOrders[i]+HistoryMinusOrders[i],0)+"/"+DoubleToString(HistoryPlusProfit[i]+HistoryMinusProfit[i],2),10,"Arial Black",ColorOfInfo,PositionHistory,UpperPosition+18+(i*14));
                  //---Comm7
                  ObjectDelete(0,"Comm7"+IntegerToString(i));
                  if(ObjectFind(0,"Comm7"+IntegerToString(i))==-1)
                     DisplayText("Comm7"+IntegerToString(i)," ("+DoubleToString(MaxOrders[i],0)+"/"+DoubleToString(MaxFloating[i],2)+")",10,"Arial Black",ColorOfInfo,PositionMaximum,UpperPosition+18+(i*14));
                  //---Comm8
                  ObjectDelete(0,"Comm8"+IntegerToString(i));
                  if(ObjectFind(0,"Comm8"+IntegerToString(i))==-1)
                     DisplayText("Comm8"+IntegerToString(i),DoubleToString(SumSpreadGroup[i],1),10,"Arial Black",ColorOfInfo,PositionSpread-10,UpperPosition+18+(i*14));
                 }
               else
                 {
                  ObjectDelete(0,"Comm2"+IntegerToString(i));
                  if(ObjectFind(0,"Comm2"+IntegerToString(i))==-1)
                     DisplayText("Comm2"+IntegerToString(i),SkippedStatus[i],10,"Arial",ColorOfInfo,PositionOrders-10,UpperPosition+18+(i*14));
                 }
               //---Background0
               if(ObjectFind(0,"BackgroundLine0")==-1)
                  ChartBackground("BackgroundLine0",ColorLineTitles,EMPTY_VALUE,true,240,UpperPosition,UsePosLast-PosLastColumn,24);
               //---Background1
               if((i<NumberGroupsTrade/2)&&(MathMod(NumberGroupsTrade,2)==0))
                  if(ObjectFind(0,"BackgroundLine1"+IntegerToString(i))==-1)
                     ChartBackground("BackgroundLine1"+IntegerToString(i),ColorOfLine1,EMPTY_VALUE,true,240,UpperPosition+18+(i*14*2),UsePosLast-PosLastColumn,16);
               if((i<=NumberGroupsTrade/2)&&(MathMod(NumberGroupsTrade,2)==1))
                  if(ObjectFind(0,"BackgroundLine1"+IntegerToString(i))==-1)
                     ChartBackground("BackgroundLine1"+IntegerToString(i),ColorOfLine1,EMPTY_VALUE,true,240,UpperPosition+18+(i*14*2),UsePosLast-PosLastColumn,16);
               //---Background2
               if(i<NumberGroupsTrade/2)
                  if(ObjectFind(0,"BackgroundLine2"+IntegerToString(i))==-1)
                     ChartBackground("BackgroundLine2"+IntegerToString(i),ColorOfLine2,EMPTY_VALUE,true,240,UpperPosition+32+(i*14*2),UsePosLast-PosLastColumn,16);
              }
           }
        }//End for(i=0; i<NumberGroupsTrade; i++)
     }//End if(ShowTaskInfo==true)
//---------------------------------------------------------------------
//Saving information about opened groups
   if((SaveInformations==true)&&(TimeHour(TimeCurrent())!=LastHourSaved))
     {
      //---Set values
      int w=0;
      int FindGroup[100];
      int FindMaxFloating[100];
      int FindMaxOrders[100];
      int FindNext1[100];
      int FindNext2[100];
      string GetGroup[100];
      string GetMaxFloating[100];
      string GetMaxOrders[100];
      string ReadString;
      string NameOfFile=Symbol()+"-"+IntegerToString(MagicNo)+"-"+StringOrdersEA+"-"+IntegerToString(AccountNumber())+".log";
      //---Read existing file
      FileHandle=FileOpen(NameOfFile,FILE_READ|FILE_CSV|FILE_COMMON);
      //---Start prosses file
      if(FileHandle!=INVALID_HANDLE)
        {
         while(!FileIsEnding(FileHandle))
           {
            w++;
            //---Read informations
            ReadString=FileReadString(FileHandle);
            //---Find potitions
            FindGroup[w]=StringFind(ReadString,"Group No: ",0);
            FindMaxFloating[w]=StringFind(ReadString,"Max Floating: ",0);
            FindMaxOrders[w]=StringFind(ReadString,"Max Orders: ",0);
            FindNext1[w]=StringFind(ReadString," || Pairs: ",0);
            FindNext2[w]=StringFind(ReadString," || Max Floating: ",0);
            //---Get informations
            GetGroup[w]=StringSubstr(ReadString,FindGroup[w]+10,FindNext1[w]-(FindGroup[w]+10));
            GetMaxFloating[w]=StringSubstr(ReadString,FindMaxFloating[w]+14,10);
            GetMaxOrders[w]=StringSubstr(ReadString,FindMaxOrders[w]+11,FindNext2[w]-(FindMaxOrders[w]+11));
           }
         FileClose(FileHandle);
        }
      else
         Print("Operation FileOpen failed, error ",GetLastError());
      //---Reset maximum value
      for(int x=1; x<NumberGroupsTrade+1; x++)
        {
         if((x==StrToInteger(GetGroup[x]))&&(StrToInteger(GetMaxOrders[x])>MaxOrders[x-1]))
            MaxOrders[x-1]=StrToInteger(GetMaxOrders[x]);
         if((x==StrToInteger(GetGroup[x]))&&(StrToInteger(GetMaxFloating[x])<MaxFloating[x-1]))
            MaxFloating[x-1]=StrToInteger(GetMaxFloating[x]);
        }
      //---Write first time the file
      FileHandle=FileOpen(NameOfFile,FILE_READ|FILE_WRITE|FILE_CSV|FILE_COMMON);
      //---Continue to write the file
      if(FileHandle!=INVALID_HANDLE)
        {
         for(i=0; i<NumberGroupsTrade; i++)
           {
            FileWrite(FileHandle,"Group No: "+IntegerToString(i+1)+" || Pairs: "+StringSubstr(SymbolPair[i][1],0,6)+"-"+StringSubstr(SymbolPair[i][2],0,6)+"-"+StringSubstr(SymbolPair[i][3],0,6)+" || History Profit: "+DoubleToString(HistoryPlusProfit[i]+HistoryMinusProfit[i],2)+" || Max Orders: "+DoubleToString(MaxOrders[i],0)+" || Max Floating: "+DoubleToString(MaxFloating[i],2));
            FileFlush(FileHandle);
            if(i==NumberGroupsTrade-1)
               LastHourSaved=TimeHour(TimeCurrent());
           }
         FileClose(FileHandle);
        }
      else
         Print("Operation FileOpen failed, error ",GetLastError());
      //---
     }//End if(SaveInformations==true)
//---------------------------------------------------------------------
//Chart comment
   Comment("================================="+
           "\n  "+FirstLine1Str+
           "\n================================="+
           "\n  Attached: "+TimeToString(StartTime,TIME_DATE)+" || First Order: "+FirstOpenedOrderStr+
           "\n================================="+
           "\n  Spread: "+SpreadStr+"  ("+DoubleToString(TotalGroupsSpread,2)+")"+
           "\n================================="+
           "\n  Side Trade Information: "+SideInfoStr+
           "\n================================="+
           "\n  Close In Profit Orders: "+CloseProfitStr+
           "\n  Close In Loss Orders  : "+CloseLossStr+
           "\n  Step For Next Order  : "+StepNextStr+
           "\n  Order Lot Size Type  : "+LotOrdersStr+
           "\n================================="+
           "\n  T O T A L   M A X I M U M   R E S U L T S"+
           "\n  Orders: "+DoubleToString(MaxTotalOrders,0)+" || DD: "+DoubleToString(ShowMinProfit,2)+" || Lots: "+DoubleToString(MaxTotalLots,2)+
           "\n================================="+
           "\n  T O T A L   C U R R E N T   R E S U L T S"+
           "\n  Orders: "+DoubleToString(TotalOrders,0)+" || PnL: "+DoubleToString(TotalProfitLoss,2)+" || Lots: "+DoubleToString(TotalLots,2)+
           "\n================================="+
           "\n  T O T A L   H I S T O R Y   R E S U L T S"+
           "\n  Pips: "+DoubleToString(HistoryTotalPips,2)+" || Orders: "+DoubleToString(HistoryTotalTrades,0)+" || PnL: "+DoubleToString(HistoryTotalProfitLoss,2)+
           "\n=================================");
//---------------------------------------------------------------------
  }
//===============================================================================================================================================================================================================================================================//
//End of code
//===============================================================================================================================================================================================================================================================//
