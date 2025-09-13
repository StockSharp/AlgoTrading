//====================================================================================================================================================//
#property copyright   "Copyright 2017-2020, Nikolaos Pantzos"
#property link        "https://www.mql5.com/en/users/pannik"
#property version     "2.5"
#property description "It's a multi currency system (use and need at least 2 pairs, which created from 3 currencies, to trade)."
#property description "Expert can to use maximum 5 currencies to make 10 pairs from which it creates 30 couples."
#property description "It is important the order of currencies be from the strongest to the weakest."
#property description "Strongest... EUR/GBP/AUD/NZD/USD/CAD/CHF/JPY ...weakest."
#property description "Attach expert in one chart only (no matter what pair or time frame)."
#property description "To better illustrate of graphics, select sreen resolution 1280 x 1024."
//#property icon        "\\Images\\CoupleHedge-Logo.ico";
#property strict
//====================================================================================================================================================//
enum Oper {Stand_by_Mode, Normal_Operation, Close_In_Profit_And_Stop, Close_Immediately_All_Orders};
enum Step {Not_Open_In_Loss, Open_With_Manual_Step, Open_With_Auto_Step};
enum CloseP {Ticket_Orders, Basket_Orders, Hybrid_Mode};
enum CloseL {Whole_Ticket, Only_First_Order, Not_Close_In_Loss};
enum ProgrO {Statical_Step, Geometrical_Step, Exponential_Step};
enum ProgrL {Statical_Lot, Geometrical_Lot, Exponential_Lot};
enum Side {Trade_Only_Plus, Trade_Only_Minus, Trade_Plus_And_Minus};
//====================================================================================================================================================//
#define PairsPerGroup 2
#define MagicSet      12021
//====================================================================================================================================================//
extern string OperationStr       = "||---------- Operation Set ----------||";
extern Oper   TypeOperation      = Normal_Operation;//Type Operation Mode
extern int    TimerInMillisecond = 100;//Timer In Millisecond For Events
extern string ManagePairsUse     = "||---------- Manage Pairs Use ----------||";
extern string CurrencyTrade      = "EUR/GBP/USD";//Currencies To Make Pairs
extern string NoOfGroupToSkip    = "46,47,48,49";//No Of Couples To Skip
extern Side   SideToOpenOrders   = Trade_Plus_And_Minus;//Side To Open Orders
extern string ManageOpenOrders   = "||---------- Manage Open Orders ----------||";
extern Step   OpenOrdersInLoss   = Open_With_Auto_Step;//Open Orders In Loss
extern double StepOpenNextOrders = 50.0;//Step For Next Order (Value $/Lot)
extern ProgrO StepOrdersProgress = Geometrical_Step;//Type Of Progress Step
extern string ManageCloseProfit  = "||---------- Manage Close Profit Orders ----------||";
extern CloseP TypeCloseInProfit  = Basket_Orders;//Type Of Close In Profit Orders
extern double TargetCloseProfit  = 50.0;//Target Close In Profit (Value $/Lot)
extern int    DelayCloseProfit   = 3;//Delay Before Close In Profit (Value Ticks)
extern string ManageCloseLoss    = "||---------- Manage Close Losses Orders ----------||";
extern CloseL TypeCloseInLoss    = Not_Close_In_Loss;//Type Of Close In Loss Orders
extern double TargetCloseLoss    = 1000.0;//Target Close In Loss (Value $/Lot)
extern int    GroupsStartClose   = 5;//Couples Start Close First Orders
extern int    DelayCloseLoss     = 3;//Delay Before Close In Loss (Value Ticks)
extern string MoneyManagement    = "||---------- Money Management ----------||";
extern bool   AutoLotSize        = false;//Use Auto Lot Size
extern double RiskFactor         = 0.1;//Risk Factor For Auto Lot
extern double ManualLotSize      = 0.01;//Manual Lot Size
extern ProgrL LotOrdersProgress  = Statical_Lot;//Type Of Progress Lot
extern string ControlSessionSet  = "||---------- Control Session ----------||";
extern bool   ControlSession     = false;//Use Control Session
extern int    WaitAfterOpen      = 60;//Wait After Monday Open
extern int    StopBeforeClose    = 60;//Stop Before Friday Close
extern string InfoOnTheScreen    = "||---------- Info On The Screen ----------||";
extern int    SizeFontsOfInfo    = 10;//Size Fonts Of Info
extern bool   ShowPairsInfo      = true;//Show Pairs Info On Screen
extern color  ColorOfTitle       = clrKhaki;//Color Of Titles
extern color  ColorOfInfo        = clrBeige;//Color Of Info
extern color  ColorLineTitles    = clrOrange;//Color Of Line Titles
extern color  ColorOfLine1       = clrMidnightBlue;//Color Of Line 1
extern color  ColorOfLine2       = clrDarkSlateGray;//Color Of Line 2
extern int    PositionOrders     = 415;//Position 'Orders' Info
extern int    PositionPnL        = 515;//Position 'PnL' Info
extern int    PositionClose      = 600;//Position 'Close' Info
extern int    PositionHistory    = 675;//Position 'History' Info
extern int    PositionMaximum    = 750;//Position 'Maximum' Info
extern int    PositionSpread     = 835;//Position 'Spread' Info
extern string Limitations        = "||---------- Limitations ----------||";
extern double MaxSpread          = 0.0;//Max Accepted Spread (0=Not Check)
extern long   MaximumOrders      = 0;//Max Opened Orders (0=Not Limit)
extern int    MaxSlippage        = 3;//Max Accepted Slippage
extern string Configuration      = "||---------- Configuration ----------||";
extern int    MagicNumber        = 0;//Orders' ID (0=Generate Automatic)
extern bool   SetChartUses       = true;//Set Automatically Chart To Use
extern bool   PrintLogReport     = false;//Print Log Report
extern bool   CheckOrders        = true;//Check All Orders
extern bool   ShowTaskInfo       = true;//Show On Chart Information
extern string StringOrdersEA     = "CoupleHedgeEA";//Comment For Orders
extern bool   SetChartInterface  = true;//Set Chart Appearance
extern bool   SaveInformations   = false;//Save Groups Informations
//====================================================================================================================================================//
string SymExt;
string CommentsEA;
string WarningPrint="";
string SymbolStatus[99][3];
string SkippedStatus[99];
//---------------------------------------------------------------------
double BidPricePair[99][3];
double SumSpreadGroup[99];
double FirstLotPlus[99][3];
double FirstLotMinus[99][3];
double LastLotPlus[99][3];
double LastLotMinus[99][3];
double CheckMargin[99];
double SumMargin;
double TotalProfitPlus[99][3];
double TotalProfitMinus[99][3];
double MaxProfit=-99999;
double MinProfit=99999;
double LevelProfitClosePlus[99];
double LevelProfitCloseMinus[99];
double LevelLossClosePlus[99];
double LevelLossCloseMinus[99];
double LevelOpenNextPlus[99];
double LevelOpenNextMinus[99];
double HistoryTotalProfitLoss;
double HistoryTotalProfitPlus;
double HistoryTotalProfitMinus;
double HistoryTotalPips;
double iLotSize;
double TotalLotPlus[99][3];
double TotalLotMinus[99][3];
double MultiplierStepPlus[99];
double MultiplierStepMinus[99];
double MultiplierLotPlus[99];
double MultiplierLotMinus[99];
double SumSpreadValuePlus[99];
double SumSpreadValueMinus[99];
double TotalCommissionPlus[99][3];
double TotalCommissionMinus[99][3];
double FirstProfitPlus[99][3];
double FirstProfitMinus[99][3];
double MaxFloating[99];
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
double SpreadPair[99][3];
double SpreadValuePlus[99][3];
double SpreadValueMinus[99][3];
double HistoryPlusProfit[99];
double HistoryMinusProfit[99];
double TotalGroupsProfit=0;
double TickValuePair[99][3];
double FirstLotPair[99][3];
double TotalGroupsSpread;
double MaximumProfitPlus;
double MaximumLossesPlus;
double MaximumProfitMinus;
double MaximumLossesMinus;
double MaximumProfit;
double MaximumLosses;
//---------------------------------------------------------------------
int i;
int j;
int k;
int MagicNo;
int CountComma;
int OrdersID[99];
int TicketNo[99];
int DecimalsPair;
int MaxTotalOrders=0;
int MultiplierPoint;
int FirstTicketPlus[99][3];
int FirstTicketMinus[99][3];
int LastTicketPlus[99][3];
int LastTicketMinus[99][3];
int TotalOrdersPlus[99][3];
int TotalOrdersMinus[99][3];
int HistoryTotalTrades;
int HistoryTotalOrdersPlus;
int HistoryTotalOrdersMinus;
int HistoryPlusOrders[99];
int HistoryMinusOrders[99];
int SuitePlus[99][3];
int SuiteMinus[99][3];
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
int LastAllOrders=0;
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
int GroupWithMaximumProfitsPlus=-1;
int GroupWithMaximumLossesPlus=-1;
int GroupWithMaximumProfitsMinus=-1;
int GroupWithMaximumLossesMinus=-1;
int GroupWithMaximumProfits=-1;
int GroupWithMaximumLosses=-1;
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
bool ExpertCloseMinusInLoss[99];
bool ExpertCloseMinusInProfit[99];
bool SkipGroup[99];
bool FirsOrdersPlusOK[99];
bool FirsOrdersMinusOK[99];
bool LimitOfOrdersOk;
bool ExpertCloseOrdersInAdvancedMode[4];
//---------------------------------------------------------------------
datetime TimeBegin;
datetime TimeEnd;
datetime ChcekLockedDay=0;
datetime DiffTimes;
datetime StartTime;
//---------------------------------------------------------------------
int NumberGroupsTrade=0;
string SymbolPair[99][3];
int Position[6];
//---------------------------------------------------------------------
long ChartColor;
//---------------------------------------------------------------------
bool LockedDate=false;
datetime ExpiryDate=D'31.12.2019';
bool LockedAccount=false;
int AccountNo=123456;
//====================================================================================================================================================//
//OnInit function
//====================================================================================================================================================//
int OnInit()
  {
//---------------------------------------------------------------------
//Set timer
   EventSetMillisecondTimer(TimerInMillisecond);
   StartTime=TimeCurrent();
//---------------------------------------------------------------------
//Set background
   if((!IsTesting())&&(!IsVisualMode())&&(!IsOptimization()))
     {
      ChartColor=ChartGetInteger(0,CHART_COLOR_BACKGROUND,0);
      if(ObjectFind("Background")==-1)
         ChartBackground("Background",(color)ChartColor,BORDER_FLAT,FALSE,0,16,240,202);
      //---------------------------------------------------------------------
      //Set chart
      if(SetChartInterface==true)
        {
         ChartSetInteger(0,CHART_SHOW_GRID,false);//Hide grid
         ChartSetInteger(0,CHART_MODE,0);//Set price in bars
         ChartSetInteger(0,CHART_SCALE,1);//Set scale
         ChartSetInteger(0,CHART_SHOW_VOLUMES,CHART_VOLUME_HIDE);//Hide value
         ChartSetInteger(0,CHART_COLOR_CHART_UP,clrNONE);//Hide line up
         ChartSetInteger(0,CHART_COLOR_CHART_DOWN,clrNONE);//Hide line down
         ChartSetInteger(0,CHART_COLOR_CHART_LINE,clrNONE);//Hide chart line
        }
      //---------------------------------------------------------------------
      //Confirm ranges and sets
      if(RiskFactor<0.01)
         RiskFactor=0.01;
      if(RiskFactor>10.0)
         RiskFactor=10.0;
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
      if((SideToOpenOrders!=2)&&(OpenOrdersInLoss==2))
         OpenOrdersInLoss=1;
      //---------------------------------------------------------------------
      //Reset value
      ArrayInitialize(SkipGroup,false);
      ArrayInitialize(ExpertCloseOrdersInAdvancedMode,false);
      ArrayInitialize(ExpertCloseBasketInProfit,false);
      ArrayInitialize(ExpertCloseBasketInLoss,false);
      ArrayInitialize(ExpertClosePlusInLoss,false);
      ArrayInitialize(ExpertClosePlusInProfit,false);
      ArrayInitialize(ExpertCloseMinusInLoss,false);
      ArrayInitialize(ExpertCloseMinusInProfit,false);
      ArrayInitialize(DelayTimesForCloseInLossPlus,0);
      ArrayInitialize(DelayTimesForCloseInLossMinus,0);
      ArrayInitialize(DelayTimesForCloseInProfitPlus,0);
      ArrayInitialize(DelayTimesForCloseInProfitMinus,0);
      ArrayInitialize(DelayTimesForCloseBasketProfit,0);
      ArrayInitialize(DelayTimesForCloseBasketLoss,0);
      ArrayInitialize(OrdersID,0);
      ArrayInitialize(MaxOrders,0);
      ArrayInitialize(MaxFloating,99999);
      ArrayInitialize(Position,0);
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
      //Symbol suffix
      if(StringLen(Symbol())>6)
         SymExt=StringSubstr(Symbol(),6);
      //---------------------------------------------------------------------
      //Calculate for 4 or 5 digits broker
      MultiplierPoint=1;
      DecimalsPair=(int)MarketInfo("EURUSD"+SymExt,MODE_DIGITS);
      if((DecimalsPair==3)||(DecimalsPair==5))
         MultiplierPoint=10;
      //---------------------------------------------------------------------
      //Comments orders
      if(StringOrdersEA=="")
         CommentsEA=WindowExpertName();
      else
         CommentsEA=StringOrdersEA;
      //---------------------------------------------------------------------
      //Set up pairs
      NumberCurrenciesTrade=((StringLen(CurrencyTrade)+1)/4);
      //---Set numbers of groups
      if(NumberCurrenciesTrade==3)
         NumberGroupsTrade=3;
      if(NumberCurrenciesTrade==4)
         NumberGroupsTrade=12;
      if(NumberCurrenciesTrade==5)
         NumberGroupsTrade=30;
      //---Set Positions
      Position[1]=0;
      Position[2]=4;
      Position[3]=8;
      Position[4]=12;
      Position[5]=16;
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
      if(NumberCurrenciesTrade>5)
        {
         Comment("\n "+StringOrdersEA+
                 "\n\n --- W A R N I N G S ---"+
                 "\n\nNumber of currencies to add \nis above the threshold of 5 (",NumberCurrenciesTrade,")"+
                 "\n\nplease check added currencies!");
         Print("Number of currencies to add is above the threshold of 5 (",NumberCurrenciesTrade,")");
         WrongPairs=true;
         return(0);
        }
      //---------------------------------------------------------------------
      //Set up groups
      if(NumberCurrenciesTrade>=3)
        {
         //---1
         SymbolPair[0][1]=StringSubstr(CurrencyTrade,Position[1],3)+StringSubstr(CurrencyTrade,Position[2],3)+SymExt;
         SymbolPair[0][2]=StringSubstr(CurrencyTrade,Position[1],3)+StringSubstr(CurrencyTrade,Position[3],3)+SymExt;
         //---2
         SymbolPair[1][1]=StringSubstr(CurrencyTrade,Position[1],3)+StringSubstr(CurrencyTrade,Position[2],3)+SymExt;
         SymbolPair[1][2]=StringSubstr(CurrencyTrade,Position[2],3)+StringSubstr(CurrencyTrade,Position[3],3)+SymExt;
         //---3
         SymbolPair[2][1]=StringSubstr(CurrencyTrade,Position[1],3)+StringSubstr(CurrencyTrade,Position[3],3)+SymExt;
         SymbolPair[2][2]=StringSubstr(CurrencyTrade,Position[2],3)+StringSubstr(CurrencyTrade,Position[3],3)+SymExt;
        }
      //---Set groups of 4 currencies
      if(NumberCurrenciesTrade>=4)
        {
         //---4
         SymbolPair[3][1]=StringSubstr(CurrencyTrade,Position[1],3)+StringSubstr(CurrencyTrade,Position[2],3)+SymExt;
         SymbolPair[3][2]=StringSubstr(CurrencyTrade,Position[1],3)+StringSubstr(CurrencyTrade,Position[4],3)+SymExt;
         //---5
         SymbolPair[4][1]=StringSubstr(CurrencyTrade,Position[1],3)+StringSubstr(CurrencyTrade,Position[2],3)+SymExt;
         SymbolPair[4][2]=StringSubstr(CurrencyTrade,Position[2],3)+StringSubstr(CurrencyTrade,Position[4],3)+SymExt;
         //---6
         SymbolPair[5][1]=StringSubstr(CurrencyTrade,Position[1],3)+StringSubstr(CurrencyTrade,Position[3],3)+SymExt;
         SymbolPair[5][2]=StringSubstr(CurrencyTrade,Position[1],3)+StringSubstr(CurrencyTrade,Position[4],3)+SymExt;
         //---7
         SymbolPair[6][1]=StringSubstr(CurrencyTrade,Position[1],3)+StringSubstr(CurrencyTrade,Position[3],3)+SymExt;
         SymbolPair[6][2]=StringSubstr(CurrencyTrade,Position[3],3)+StringSubstr(CurrencyTrade,Position[4],3)+SymExt;
         //---8
         SymbolPair[7][1]=StringSubstr(CurrencyTrade,Position[1],3)+StringSubstr(CurrencyTrade,Position[4],3)+SymExt;
         SymbolPair[7][2]=StringSubstr(CurrencyTrade,Position[2],3)+StringSubstr(CurrencyTrade,Position[4],3)+SymExt;
         //---9
         SymbolPair[8][1]=StringSubstr(CurrencyTrade,Position[1],3)+StringSubstr(CurrencyTrade,Position[4],3)+SymExt;
         SymbolPair[8][2]=StringSubstr(CurrencyTrade,Position[3],3)+StringSubstr(CurrencyTrade,Position[4],3)+SymExt;
         //---10
         SymbolPair[9][1]=StringSubstr(CurrencyTrade,Position[2],3)+StringSubstr(CurrencyTrade,Position[3],3)+SymExt;
         SymbolPair[9][2]=StringSubstr(CurrencyTrade,Position[2],3)+StringSubstr(CurrencyTrade,Position[4],3)+SymExt;
         //---11
         SymbolPair[10][1]=StringSubstr(CurrencyTrade,Position[2],3)+StringSubstr(CurrencyTrade,Position[3],3)+SymExt;
         SymbolPair[10][2]=StringSubstr(CurrencyTrade,Position[3],3)+StringSubstr(CurrencyTrade,Position[4],3)+SymExt;
         //---12
         SymbolPair[11][1]=StringSubstr(CurrencyTrade,Position[2],3)+StringSubstr(CurrencyTrade,Position[4],3)+SymExt;
         SymbolPair[11][2]=StringSubstr(CurrencyTrade,Position[3],3)+StringSubstr(CurrencyTrade,Position[4],3)+SymExt;
        }
      //---Set groups of 5 currencies
      if(NumberCurrenciesTrade>=5)
        {
         //---13
         SymbolPair[12][1]=StringSubstr(CurrencyTrade,Position[1],3)+StringSubstr(CurrencyTrade,Position[2],3)+SymExt;
         SymbolPair[12][2]=StringSubstr(CurrencyTrade,Position[1],3)+StringSubstr(CurrencyTrade,Position[5],3)+SymExt;
         //---14
         SymbolPair[13][1]=StringSubstr(CurrencyTrade,Position[1],3)+StringSubstr(CurrencyTrade,Position[2],3)+SymExt;
         SymbolPair[13][2]=StringSubstr(CurrencyTrade,Position[2],3)+StringSubstr(CurrencyTrade,Position[5],3)+SymExt;
         //---15
         SymbolPair[14][1]=StringSubstr(CurrencyTrade,Position[1],3)+StringSubstr(CurrencyTrade,Position[3],3)+SymExt;
         SymbolPair[14][2]=StringSubstr(CurrencyTrade,Position[1],3)+StringSubstr(CurrencyTrade,Position[5],3)+SymExt;
         //---16
         SymbolPair[15][1]=StringSubstr(CurrencyTrade,Position[1],3)+StringSubstr(CurrencyTrade,Position[3],3)+SymExt;
         SymbolPair[15][2]=StringSubstr(CurrencyTrade,Position[3],3)+StringSubstr(CurrencyTrade,Position[5],3)+SymExt;
         //---17
         SymbolPair[16][1]=StringSubstr(CurrencyTrade,Position[1],3)+StringSubstr(CurrencyTrade,Position[4],3)+SymExt;
         SymbolPair[16][2]=StringSubstr(CurrencyTrade,Position[1],3)+StringSubstr(CurrencyTrade,Position[5],3)+SymExt;
         //---18
         SymbolPair[17][1]=StringSubstr(CurrencyTrade,Position[1],3)+StringSubstr(CurrencyTrade,Position[4],3)+SymExt;
         SymbolPair[17][2]=StringSubstr(CurrencyTrade,Position[4],3)+StringSubstr(CurrencyTrade,Position[5],3)+SymExt;
         //---19
         SymbolPair[18][1]=StringSubstr(CurrencyTrade,Position[1],3)+StringSubstr(CurrencyTrade,Position[5],3)+SymExt;
         SymbolPair[18][2]=StringSubstr(CurrencyTrade,Position[2],3)+StringSubstr(CurrencyTrade,Position[5],3)+SymExt;
         //---20
         SymbolPair[19][1]=StringSubstr(CurrencyTrade,Position[1],3)+StringSubstr(CurrencyTrade,Position[5],3)+SymExt;
         SymbolPair[19][2]=StringSubstr(CurrencyTrade,Position[3],3)+StringSubstr(CurrencyTrade,Position[5],3)+SymExt;
         //---21
         SymbolPair[20][1]=StringSubstr(CurrencyTrade,Position[1],3)+StringSubstr(CurrencyTrade,Position[5],3)+SymExt;
         SymbolPair[20][2]=StringSubstr(CurrencyTrade,Position[4],3)+StringSubstr(CurrencyTrade,Position[5],3)+SymExt;
         //---22
         SymbolPair[21][1]=StringSubstr(CurrencyTrade,Position[2],3)+StringSubstr(CurrencyTrade,Position[3],3)+SymExt;
         SymbolPair[21][2]=StringSubstr(CurrencyTrade,Position[2],3)+StringSubstr(CurrencyTrade,Position[5],3)+SymExt;
         //---23
         SymbolPair[22][1]=StringSubstr(CurrencyTrade,Position[2],3)+StringSubstr(CurrencyTrade,Position[3],3)+SymExt;
         SymbolPair[22][2]=StringSubstr(CurrencyTrade,Position[3],3)+StringSubstr(CurrencyTrade,Position[5],3)+SymExt;
         //---24
         SymbolPair[23][1]=StringSubstr(CurrencyTrade,Position[2],3)+StringSubstr(CurrencyTrade,Position[4],3)+SymExt;
         SymbolPair[23][2]=StringSubstr(CurrencyTrade,Position[2],3)+StringSubstr(CurrencyTrade,Position[5],3)+SymExt;
         //---25
         SymbolPair[24][1]=StringSubstr(CurrencyTrade,Position[2],3)+StringSubstr(CurrencyTrade,Position[4],3)+SymExt;
         SymbolPair[24][2]=StringSubstr(CurrencyTrade,Position[4],3)+StringSubstr(CurrencyTrade,Position[5],3)+SymExt;
         //---26
         SymbolPair[25][1]=StringSubstr(CurrencyTrade,Position[2],3)+StringSubstr(CurrencyTrade,Position[5],3)+SymExt;
         SymbolPair[25][2]=StringSubstr(CurrencyTrade,Position[3],3)+StringSubstr(CurrencyTrade,Position[5],3)+SymExt;
         //---27
         SymbolPair[26][1]=StringSubstr(CurrencyTrade,Position[2],3)+StringSubstr(CurrencyTrade,Position[5],3)+SymExt;
         SymbolPair[26][2]=StringSubstr(CurrencyTrade,Position[4],3)+StringSubstr(CurrencyTrade,Position[5],3)+SymExt;
         //---28
         SymbolPair[27][1]=StringSubstr(CurrencyTrade,Position[3],3)+StringSubstr(CurrencyTrade,Position[4],3)+SymExt;
         SymbolPair[27][2]=StringSubstr(CurrencyTrade,Position[3],3)+StringSubstr(CurrencyTrade,Position[5],3)+SymExt;
         //---29
         SymbolPair[28][1]=StringSubstr(CurrencyTrade,Position[3],3)+StringSubstr(CurrencyTrade,Position[4],3)+SymExt;
         SymbolPair[28][2]=StringSubstr(CurrencyTrade,Position[4],3)+StringSubstr(CurrencyTrade,Position[5],3)+SymExt;
         //---30
         SymbolPair[29][1]=StringSubstr(CurrencyTrade,Position[3],3)+StringSubstr(CurrencyTrade,Position[5],3)+SymExt;
         SymbolPair[29][2]=StringSubstr(CurrencyTrade,Position[4],3)+StringSubstr(CurrencyTrade,Position[5],3)+SymExt;
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
         //---Set groups to skip
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
         Print(" # "+WindowExpertName()+" # "+"Check couples No "+IntegerToString(i+1)+"...("+SymbolPair[i][1]+"/"+SymbolPair[i][2]+")");
         //---
         if((SymbolSelect(SymbolPair[i][1],true))&&(SymbolSelect(SymbolPair[i][2],true)))
           {
            Print(" # "+WindowExpertName()+" # "+SymbolPair[i][1]+"/"+SymbolPair[i][2]+" are ok");
            if(SkipGroup[i]==true)
              {
               SkippedStatus[i]="Couple Skipped by user from external parameters";
               Print(" # ",WindowExpertName()," # Skip couple No ",IntegerToString(i+1)," #");
              }
           }
         else
            Print(" # "+WindowExpertName()+" # "+SymbolPair[i][1]+"/"+SymbolPair[i][2]+" not found");
         //---Get prices of symbols
         BidPricePair[i][1]=MarketInfo(SymbolPair[i][1],MODE_BID);
         BidPricePair[i][2]=MarketInfo(SymbolPair[i][2],MODE_BID);
         //---Check symbols
         if(((BidPricePair[i][1]==0)||(BidPricePair[i][2]==0))&&(WrongPairs==false))
           {
            SymbolStatus[i][1]="Pair "+SymbolPair[i][1]+" Not Found. Number Of Couple: "+IntegerToString(i+1);
            SymbolStatus[i][2]="Pair "+SymbolPair[i][2]+" Not Found. Number Of Couple: "+IntegerToString(i+1);
            //---Warnings message
            Comment("\n "+StringOrdersEA+
                    "\n\n --- W A R N I N G S ---"+
                    "\n\n"+SymbolStatus[i][1]+" or \n"+SymbolStatus[i][2]+
                    "\n\nplease check added currencies!"+
                    "\n\nCorrect format and series for each currency is \nEUR/GBP/AUD/NZD/USD/CAD/CHF/JPY");
            WrongPairs=true;
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
      //Currency and groups informations
      Print("### ",WindowExpertName()," || Number Of currencies use: ",NumberCurrenciesTrade," || Number of couples trade: ",NumberGroupsTrade," ###");
      //---------------------------------------------------------------------
      //ID orders
      if(MagicNumber==0)
        {
         MagicNo=0;
         for(i=0; i<StringLen(CurrencyTrade); i++)
            MagicNo+=(StringGetChar(CurrencyTrade,i)*(i+1));
         MagicNo+=MagicSet+AccountNumber()+(SideToOpenOrders*123);
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
         //---Set up suite
         if((StringSubstr(SymbolPair[i][1],0,3)==StringSubstr(SymbolPair[i][2],0,3))||(StringSubstr(SymbolPair[i][1],3,6)==StringSubstr(SymbolPair[i][2],3,6)))
           {
            SuitePlus[i][1]=OP_BUY;
            SuitePlus[i][2]=OP_SELL;
            //---
            SuiteMinus[i][1]=OP_SELL;
            SuiteMinus[i][2]=OP_BUY;
           }
         else
           {
            SuitePlus[i][1]=OP_BUY;
            SuitePlus[i][2]=OP_BUY;
            //---
            SuiteMinus[i][1]=OP_SELL;
            SuiteMinus[i][2]=OP_SELL;
           }
        }
      //---------------------------------------------------------------------
      //Check maximum orders and minimum lot
      if(TypeOperation==1)
        {
         if(AutoLotSize==true)
            iLotSize=(AccountBalance()/100000)*RiskFactor;
         if(AutoLotSize==false)
            iLotSize=ManualLotSize;
         //---
         if(NumberGroupsTrade>=10)
           {
            if((AccountInfoInteger(ACCOUNT_LIMIT_ORDERS)!=0)||(SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MIN)!=iLotSize))
              {
               if(AccountInfoInteger(ACCOUNT_LIMIT_ORDERS)!=0)
                  SignalsMessageWarning=MessageBox("Account has maximum orders limit: "+IntegerToString(AccountInfoInteger(ACCOUNT_LIMIT_ORDERS))+
                                                   "\n\nBy clicking YES expert run. \n\nSTART EXPERT ADVISOR?","RISK DISCLAIMER - "+WindowExpertName(),MB_YESNO|MB_ICONEXCLAMATION);
               if(SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MIN)!=0.01)
                  SignalsMessageWarning=MessageBox("Account or pair has minimum lot: "+DoubleToStr(SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MIN))+
                                                   "\n\nBy clicking YES expert run. \n\nSTART EXPERT ADVISOR?","RISK DISCLAIMER - "+WindowExpertName(),MB_YESNO|MB_ICONEXCLAMATION);
               if(SignalsMessageWarning==IDNO)
                  return(INIT_FAILED);
              }
           }
        }
      //---------------------------------------------------------------------
      //Set maximum orders
      if(((MaximumOrders==0)&&(AccountInfoInteger(ACCOUNT_LIMIT_ORDERS)!=0))||((MaximumOrders>AccountInfoInteger(ACCOUNT_LIMIT_ORDERS))&&(AccountInfoInteger(ACCOUNT_LIMIT_ORDERS)!=0)))
         MaximumOrders=AccountInfoInteger(ACCOUNT_LIMIT_ORDERS);
      //---------------------------------------------------------------------
      //Call MainFunction function to show information if market is closed
      MainFunction();
     }
//---------------------------------------------------------------------
   return(INIT_SUCCEEDED);
//---------------------------------------------------------------------
  }
//====================================================================================================================================================//
void OnDeinit(const int reason)
  {
//---------------------------------------------------------------------
//Clear chart
   for(i=0; i<99; i++)
     {
      if(ObjectFind("Comm1"+IntegerToString(i))>-1)
         ObjectDelete("Comm1"+IntegerToString(i));
      if(ObjectFind("Comm2"+IntegerToString(i))>-1)
         ObjectDelete("Comm2"+IntegerToString(i));
      if(ObjectFind("Comm3"+IntegerToString(i))>-1)
         ObjectDelete("Comm3"+IntegerToString(i));
      if(ObjectFind("Comm4"+IntegerToString(i))>-1)
         ObjectDelete("Comm4"+IntegerToString(i));
      if(ObjectFind("Comm5"+IntegerToString(i))>-1)
         ObjectDelete("Comm5"+IntegerToString(i));
      if(ObjectFind("Comm6"+IntegerToString(i))>-1)
         ObjectDelete("Comm6"+IntegerToString(i));
      if(ObjectFind("Comm7"+IntegerToString(i))>-1)
         ObjectDelete("Comm7"+IntegerToString(i));
      if(ObjectFind("BackgroundLine1"+IntegerToString(i))>-1)
         ObjectDelete("BackgroundLine1"+IntegerToString(i));
      if(ObjectFind("BackgroundLine2"+IntegerToString(i))>-1)
         ObjectDelete("BackgroundLine2"+IntegerToString(i));
      if(ObjectFind("Text"+IntegerToString(i))>-1)
         ObjectDelete("Text"+IntegerToString(i));
      if(ObjectFind("Str"+IntegerToString(i))>-1)
         ObjectDelete("Str"+IntegerToString(i));
     }
//---
   if(ObjectFind("BackgroundLine0")>-1)
      ObjectDelete("BackgroundLine0");
   if(ObjectFind("Background")>-1)
      ObjectDelete("Background");
//---
   Comment("");
   EventKillTimer();
//---------------------------------------------------------------------
  }
//====================================================================================================================================================//
//OnTick function
//====================================================================================================================================================//
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
         if((Hour()==12)||(Hour()==24))
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
                 "\n\n\n      Turn ON EA Please .......");
         return;
        }
      else
         if((!IsTradeAllowed())||(IsTradeContextBusy()))
           {
            Comment("\n      Trade is disabled",
                    "\n      or trade flow is busy.",
                    "\n\n\n      Wait Please .......");
            return;
           }
         else
           {
            CallMain=true;
           }
     }
//---------------------------------------------------------------------
  }
//====================================================================================================================================================//
//OnTimer function
//====================================================================================================================================================//
void OnTimer()
  {
//---------------------------------------------------------------------
//Call main function
   if(CallMain==true)
      MainFunction();
//---------------------------------------------------------------------
  }
//====================================================================================================================================================//
//main function
//====================================================================================================================================================//
void MainFunction()
  {
//---------------------------------------------------------------------
//Reset value
   HistoryTotalPips=0;
   HistoryTotalTrades=0;
   HistoryTotalOrdersPlus=0;
   HistoryTotalOrdersMinus=0;
   HistoryTotalProfitLoss=0;
   HistoryTotalProfitPlus=0;
   HistoryTotalProfitMinus=0;
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
   if((ChangeOperation==true)&&(TypeOperation==1))
     {
      TypeOperation=2;
      CommentWarning=true;
      WarningPrint=StringConcatenate("Expert Has Expired, Working To Close In Profit And Stop");
     }
//---------------------------------------------------------------------
//Stop in locked version or wrong sets
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
            WarningPrint=StringConcatenate("Spread it isn't normal (",SumSpreadGroup[cnt],"/",MaxSpread,")");
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
      //---Reset 1
      if((cnt==NumberGroupsTrade-1)&&(ExpertCloseOrdersInAdvancedMode[1]==true)&&
         (TotalOrdersPlus[GroupWithMaximumProfitsPlus][1]==0)&&
         (TotalOrdersPlus[GroupWithMaximumProfitsPlus][2]==0)&&
         (TotalOrdersPlus[GroupWithMaximumLossesPlus][1]==0)&&
         (TotalOrdersPlus[GroupWithMaximumLossesPlus][2]==0))
        {
         ExpertCloseOrdersInAdvancedMode[1]=false;
        }
      //---Reset 2
      if((cnt==NumberGroupsTrade-1)&&(ExpertCloseOrdersInAdvancedMode[2]==true)&&
         (TotalOrdersMinus[GroupWithMaximumProfitsMinus][1]==0)&&
         (TotalOrdersMinus[GroupWithMaximumProfitsMinus][2]==0)&&
         (TotalOrdersMinus[GroupWithMaximumLossesMinus][1]==0)&&
         (TotalOrdersMinus[GroupWithMaximumLossesMinus][2]==0))
        {
         ExpertCloseOrdersInAdvancedMode[2]=false;
        }
      //---Reset 3
      if((cnt==NumberGroupsTrade-1)&&(ExpertCloseOrdersInAdvancedMode[3]==true)&&
         (TotalOrdersPlus[GroupWithMaximumProfits][1]==0)&&
         (TotalOrdersPlus[GroupWithMaximumProfits][2]==0)&&
         (TotalOrdersMinus[GroupWithMaximumProfits][1]==0)&&
         (TotalOrdersMinus[GroupWithMaximumProfits][2]==0)&&
         (TotalOrdersPlus[GroupWithMaximumLosses][1]==0)&&
         (TotalOrdersPlus[GroupWithMaximumLosses][2]==0)&&
         (TotalOrdersMinus[GroupWithMaximumLosses][1]==0)&&
         (TotalOrdersMinus[GroupWithMaximumLosses][2]==0))
        {
         ExpertCloseOrdersInAdvancedMode[3]=false;
        }
      //---------------------------------------------------------------------
      //Count history orders first time
      if(CntTick<NumberGroupsTrade+4)
        {
         CntTick++;
         if(CntTick<NumberGroupsTrade+3)
            CountHistory=true;
        }
      //---------------------------------------------------------------------
      //Count history orders if close orders
      LastAllOrders=MathMax(LastAllOrders,OrdersTotal());
      if(OrdersTotal()<LastAllOrders)
        {
         LastAllOrders=OrdersTotal();
         CountHistory=true;
        }
      //---
      if(CountHistory==true)
        {
         CountHistory=false;
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
      //Levels
      for(i=1; i<=PairsPerGroup; i++)
        {
         //---Levels open next group in loss
         LevelOpenNextPlus[cnt]+=-(TotalLotPlus[cnt][i]*StepOpenNextOrders*MultiplierStepPlus[cnt]*TickValuePair[cnt][i]);
         LevelOpenNextMinus[cnt]+=-(TotalLotMinus[cnt][i]*StepOpenNextOrders*MultiplierStepMinus[cnt]*TickValuePair[cnt][i]);
         //---Levels close group in profit
         LevelProfitClosePlus[cnt]+=(TotalLotPlus[cnt][i]*TargetCloseProfit*TickValuePair[cnt][i]);
         LevelProfitCloseMinus[cnt]+=(TotalLotMinus[cnt][i]*TargetCloseProfit*TickValuePair[cnt][i]);
         //---Levels close group in loss
         LevelLossClosePlus[cnt]+=-(TotalLotPlus[cnt][i]*TargetCloseLoss*TickValuePair[cnt][i]);
         LevelLossCloseMinus[cnt]+=-(TotalLotMinus[cnt][i]*TargetCloseLoss*TickValuePair[cnt][i]);
        }
      //---
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
         (ExpertCloseOrdersInAdvancedMode[1]==false)&&(ExpertCloseOrdersInAdvancedMode[2]==false)&&(ExpertCloseOrdersInAdvancedMode[3]==false))
        {
         //---------------------------------------------------------------------
         //Send oposite group if close a group in profit
         if((OpenOrdersInLoss==2)&&(TypeOperation!=0)&&(TypeOperation!=2)&&(TypeOperation!=3)&&(OrdersIsOK[cnt]==true))
           {
            //---Send minus if close plus
            if((SumOrdersPlus[cnt]==0)&&(SumOrdersMinus[cnt]>=PairsPerGroup))
              {
               for(i=1; i<=PairsPerGroup; i++)
                  OpenPairMinus(cnt,i);
              }
            //---Send plus if close minus
            if((SumOrdersMinus[cnt]==0)&&(SumOrdersPlus[cnt]>=PairsPerGroup))
              {
               for(i=1; i<=PairsPerGroup; i++)
                  OpenPairPlus(cnt,i);
              }
           }
         //---------------------------------------------------------------------
         //Send first group
         if((LimitOfOrdersOk==true)&&(TypeOperation!=0)&&(TypeOperation!=3))
           {
            for(i=1; i<=PairsPerGroup; i++)
              {
               if((TotalOrdersPlus[cnt][i]==0)&&((SideToOpenOrders==0)||(SideToOpenOrders==2)))
                  OpenPairPlus(cnt,i);
               if((TotalOrdersMinus[cnt][i]==0)&&((SideToOpenOrders==1)||(SideToOpenOrders==2)))
                  OpenPairMinus(cnt,i);
              }
            //---Check first orders
            for(i=1; i<=PairsPerGroup; i++)
              {
               if((TotalOrdersPlus[cnt][i]!=0)||(SideToOpenOrders==1))
                 {
                  FirsOrdersPlusOK[cnt]=true;
                 }
               else
                 {
                  FirsOrdersPlusOK[cnt]=false;
                  break;
                 }
               //---
               if((TotalOrdersMinus[cnt][i]!=0)||(SideToOpenOrders==0))
                 {
                  FirsOrdersMinusOK[cnt]=true;
                 }
               else
                 {
                  FirsOrdersMinusOK[cnt]=false;
                  break;
                 }
              }
            //---
            if((FirsOrdersPlusOK[cnt]==false)||(FirsOrdersMinusOK[cnt]==false))
              {
               CommentChart();
               continue;
              }
            //---------------------------------------------------------------------
            //Send next group in loss
            if(((OpenOrdersInLoss==1)||(TypeOperation==2))&&(OrdersIsOK[cnt]==true))
              {
               //---Send plus
               if((FirsOrdersPlusOK[cnt]==true)&&((SideToOpenOrders==0)||(SideToOpenOrders==2)))
                 {
                  if((SumOrdersPlus[cnt]>=PairsPerGroup)&&(SumProfitPlus[cnt]<=LevelOpenNextPlus[cnt])&&((TypeCloseInProfit==0)||(SumOrdersPlus[cnt]==SumOrdersMinus[cnt])||(SumOrdersPlus[cnt]>SumOrdersMinus[cnt])))
                    {
                     for(i=1; i<=PairsPerGroup; i++)
                        OpenPairPlus(cnt,i);
                     continue;
                    }
                 }
               //---Send minus
               if((FirsOrdersMinusOK[cnt]==true)&&((SideToOpenOrders==1)||(SideToOpenOrders==2)))
                 {
                  if((SumOrdersMinus[cnt]>=PairsPerGroup)&&(SumProfitMinus[cnt]<=LevelOpenNextMinus[cnt])&&((TypeCloseInProfit==0)||(SumOrdersPlus[cnt]==SumOrdersMinus[cnt])||(SumOrdersPlus[cnt]<SumOrdersMinus[cnt])))
                    {
                     for(i=1; i<=PairsPerGroup; i++)
                        OpenPairMinus(cnt,i);
                     continue;
                    }
                 }
              }
           }
        }
      //---------------------------------------------------------------------
      //Close orders
      if((MarketIsOpen==true)&&(TypeOperation!=0)&&(TypeOperation!=3))
        {
         //---------------------------------------------------------------------
         //Close orders in loss
         if((TypeCloseInLoss<2)&&(ExpertCloseBasketInProfit[cnt]==false)&&(ExpertClosePlusInProfit[cnt]==false)&&(ExpertCloseMinusInProfit[cnt]==false)&&
            (ExpertCloseOrdersInAdvancedMode[1]==false)&&(ExpertCloseOrdersInAdvancedMode[2]==false)&&(ExpertCloseOrdersInAdvancedMode[3]==false))
           {
            //---Close whole ticket
            if((TypeCloseInLoss==0)&&(SumOrdersPlus[cnt]+SumOrdersMinus[cnt]>0))
              {
               //---Start close plus in loss
               if(((SumProfitPlus[cnt]<0)&&(SumOrdersPlus[cnt]>0))||(ExpertClosePlusInLoss[cnt]==true))
                 {
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
                        CountHistory=true;
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
                        CountHistory=true;
                        continue;
                       }
                    }
                 }
              }//End if(TypeCloseInLoss==0)
            //---Close partial ticket
            if((TypeCloseInLoss==1)&&(SumOrdersPlus[cnt]+SumOrdersMinus[cnt]>0))
              {
               if((SumProfitPlus[cnt]<0)&&(SumOrdersPlus[cnt]>0)&&(GroupsPlus[cnt]>=GroupsStartClose))
                 {
                  //---Start close plus in loss
                  if((SumProfitPlus[cnt]>LevelLossClosePlus[cnt])||(GroupsPlus[cnt]<GroupsStartClose))
                     DelayTimesForCloseInLossPlus[cnt]=0;//Resete delay times before close
                  if((SumProfitPlus[cnt]<=LevelLossClosePlus[cnt])&&(GroupsPlus[cnt]>=GroupsStartClose))//Close first orders plus
                    {
                     DelayTimesForCloseInLossPlus[cnt]++;
                     //---Close plus in loss
                     if(DelayTimesForCloseInLossPlus[cnt]>=DelayCloseLoss)
                       {
                        for(i=1; i<=PairsPerGroup; i++)
                          {
                           if(TotalOrdersPlus[cnt][i]>0)
                              ClosePairPlus(cnt,FirstTicketPlus[cnt][i],i);
                          }
                        //---
                        CountHistory=true;
                        continue;
                       }
                    }
                 }
               //---Start close minus in loss
               if((SumProfitMinus[cnt]<0)&&(SumOrdersMinus[cnt]>0)&&(GroupsMinus[cnt]>=GroupsStartClose))
                 {
                  if((SumProfitMinus[cnt]>LevelLossCloseMinus[cnt])||(GroupsMinus[cnt]<GroupsStartClose))
                     DelayTimesForCloseInLossMinus[cnt]=0;//Resete delay times before close
                  if((SumProfitMinus[cnt]<=LevelLossCloseMinus[cnt])&&(GroupsMinus[cnt]>=GroupsStartClose))//Close first orders minus
                    {
                     DelayTimesForCloseInLossMinus[cnt]++;
                     //---Close minus in loss
                     if(DelayTimesForCloseInLossMinus[cnt]>=DelayCloseLoss)
                       {
                        for(i=1; i<=PairsPerGroup; i++)
                          {
                           if(TotalOrdersMinus[cnt][i]>0)
                              ClosePairMinus(cnt,FirstTicketMinus[cnt][i],i);
                          }
                        //---
                        CountHistory=true;
                        continue;
                       }
                    }
                 }
              }//End if(TypeCloseInLoss==1)
            //---
           }
         //---------------------------------------------------------------------
         //Close orders in profit
         if((TypeOperation!=0)&&(ExpertCloseBasketInLoss[cnt]==false)&&(ExpertClosePlusInLoss[cnt]==false)&&(ExpertCloseMinusInLoss[cnt]==false))
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
                        CountHistory=true;
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
                        CountHistory=true;
                        continue;
                       }
                    }
                 }
              }
            //---Close in basket profit
            if((TypeCloseInProfit==1)||(OpenOrdersInLoss==2))
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
                           CountHistory=true;
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
                           CountHistory=true;
                           continue;
                          }
                       }
                    }
                 }
               //---Close all in basket profit (all tickets)
               if(TypeCloseInProfit==1)
                 {
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
                           CountHistory=true;
                           continue;
                          }
                       }
                    }
                 }
              }
            //---Close in hybrid mode
            if(TypeCloseInProfit==2)
              {
               //---Get results if not open orders in loss
               if(OpenOrdersInLoss==0)
                 {
                  if((ExpertCloseOrdersInAdvancedMode[1]==false)&&(ExpertCloseOrdersInAdvancedMode[2]==false)&&(SumOrdersPlus[cnt]+SumOrdersMinus[cnt]>0))
                    {
                     //---Reset value
                     MaximumProfitPlus=-9999999;
                     MaximumLossesPlus=9999999;
                     MaximumProfitMinus=-9999999;
                     MaximumLossesMinus=9999999;
                     GroupWithMaximumProfitsPlus=-1;
                     GroupWithMaximumLossesPlus=-1;
                     GroupWithMaximumProfitsMinus=-1;
                     GroupWithMaximumLossesMinus=-1;
                     //---
                     for(i=1; i<NumberGroupsTrade; i++)
                       {
                        //---Get plus results
                        if((SideToOpenOrders==0)||(SideToOpenOrders==2))
                          {
                           //---Maximum profit plus
                           if((SumProfitPlus[i]>MaximumProfitPlus)&&(SumOrdersPlus[cnt]>0))
                             {
                              GroupWithMaximumProfitsPlus=i;
                              MaximumProfitPlus=SumProfitPlus[i];
                             }
                           //---Maximum loss plus
                           if((SumProfitPlus[i]<MaximumLossesPlus)&&(SumOrdersPlus[cnt]>0))
                             {
                              GroupWithMaximumLossesPlus=i;
                              MaximumLossesPlus=SumProfitPlus[i];
                             }
                          }
                        //---Get minus results
                        if((SideToOpenOrders==1)||(SideToOpenOrders==2))
                          {
                           //---Maximum profit  minus
                           if((SumProfitMinus[i]>MaximumProfitMinus)&&(SumOrdersMinus[cnt]>0))
                             {
                              GroupWithMaximumProfitsMinus=i;
                              MaximumProfitMinus=SumProfitMinus[i];
                             }
                           //---Maximum loss minus
                           if((SumProfitMinus[i]<MaximumLossesMinus)&&(SumOrdersMinus[cnt]>0))
                             {
                              GroupWithMaximumLossesMinus=i;
                              MaximumLossesMinus=SumProfitMinus[i];
                             }
                          }
                       }
                    }
                  //---Check if pass all groups
                  if(cnt==NumberGroupsTrade-1)
                    {
                     //---Plus vs plus
                     if((SideToOpenOrders==0)||(SideToOpenOrders==2))
                       {
                        if((MaximumProfitPlus+MaximumLossesPlus>=MathMax(LevelProfitClosePlus[GroupWithMaximumProfitsPlus],LevelProfitClosePlus[GroupWithMaximumLossesPlus]))||(ExpertCloseOrdersInAdvancedMode[1]==true))
                          {
                           if(TotalOrdersPlus[GroupWithMaximumProfitsPlus][1]>0)
                              ClosePairPlus(GroupWithMaximumProfitsPlus,-1,1);
                           if(TotalOrdersPlus[GroupWithMaximumProfitsPlus][2]>0)
                              ClosePairPlus(GroupWithMaximumProfitsPlus,-1,2);
                           if(TotalOrdersPlus[GroupWithMaximumLossesPlus][1]>0)
                              ClosePairPlus(GroupWithMaximumLossesPlus,-1,1);
                           if(TotalOrdersPlus[GroupWithMaximumLossesPlus][2]>0)
                              ClosePairPlus(GroupWithMaximumLossesPlus,-1,2);
                           //---
                           ExpertCloseOrdersInAdvancedMode[1]=true;
                           CountHistory=true;
                           continue;
                          }
                       }
                     //---Minus vs minus
                     if((SideToOpenOrders==1)||(SideToOpenOrders==2))
                       {
                        if((MaximumProfitMinus+MaximumLossesMinus>=MathMax(LevelProfitCloseMinus[GroupWithMaximumProfitsMinus],LevelProfitCloseMinus[GroupWithMaximumLossesMinus]))||(ExpertCloseOrdersInAdvancedMode[2]==true))
                          {
                           if(TotalOrdersMinus[GroupWithMaximumProfitsMinus][1]>0)
                              ClosePairMinus(GroupWithMaximumProfitsMinus,-1,1);
                           if(TotalOrdersMinus[GroupWithMaximumProfitsMinus][2]>0)
                              ClosePairMinus(GroupWithMaximumProfitsMinus,-1,2);
                           if(TotalOrdersMinus[GroupWithMaximumLossesMinus][1]>0)
                              ClosePairMinus(GroupWithMaximumLossesMinus,-1,1);
                           if(TotalOrdersMinus[GroupWithMaximumLossesMinus][2]>0)
                              ClosePairMinus(GroupWithMaximumLossesMinus,-1,2);
                           //---
                           ExpertCloseOrdersInAdvancedMode[2]=true;
                           CountHistory=true;
                           continue;
                          }
                       }
                    }
                 }//end if(OpenOrdersInLoss==0)
               //---Get results if open orders in loss
               if(OpenOrdersInLoss!=0)
                 {
                  if((ExpertCloseOrdersInAdvancedMode[3]==false)&&(SumOrdersPlus[cnt]+SumOrdersMinus[cnt]>0))
                    {
                     MaximumProfit=-9999999;
                     MaximumLosses=9999999;
                     GroupWithMaximumProfits=-1;
                     GroupWithMaximumLosses=-1;
                     //---
                     for(i=1; i<NumberGroupsTrade; i++)
                       {
                        if(SumProfitPlus[i]+SumProfitMinus[i]>MaximumProfit)
                          {
                           GroupWithMaximumProfits=i;
                           MaximumProfit=SumProfitPlus[i]+SumProfitMinus[i];
                          }
                        //---
                        if(SumProfitPlus[i]+SumProfitMinus[i]<MaximumLosses)
                          {
                           GroupWithMaximumLosses=i;
                           MaximumLosses=SumProfitPlus[i]+SumProfitMinus[i];
                          }
                       }
                    }
                  //---Check if pass alll couples
                  if(cnt==NumberGroupsTrade-1)
                    {
                     if((MaximumProfit+MaximumLosses>=MathMax(LevelProfitClosePlus[GroupWithMaximumLosses],LevelProfitCloseMinus[GroupWithMaximumLosses]))||(ExpertCloseOrdersInAdvancedMode[3]==true))
                       {
                        if(TotalOrdersPlus[GroupWithMaximumProfits][1]>0)
                           ClosePairPlus(GroupWithMaximumProfits,-1,1);
                        if(TotalOrdersPlus[GroupWithMaximumProfits][2]>0)
                           ClosePairPlus(GroupWithMaximumProfits,-1,2);
                        if(TotalOrdersMinus[GroupWithMaximumProfits][1]>0)
                           ClosePairMinus(GroupWithMaximumProfits,-1,1);
                        if(TotalOrdersMinus[GroupWithMaximumProfits][2]>0)
                           ClosePairMinus(GroupWithMaximumProfits,-1,2);
                        if(TotalOrdersPlus[GroupWithMaximumLosses][1]>0)
                           ClosePairPlus(GroupWithMaximumLosses,-1,1);
                        if(TotalOrdersPlus[GroupWithMaximumLosses][2]>0)
                           ClosePairPlus(GroupWithMaximumLosses,-1,2);
                        if(TotalOrdersMinus[GroupWithMaximumLosses][1]>0)
                           ClosePairMinus(GroupWithMaximumLosses,-1,1);
                        if(TotalOrdersMinus[GroupWithMaximumLosses][2]>0)
                           ClosePairMinus(GroupWithMaximumLosses,-1,2);
                        //---
                        ExpertCloseOrdersInAdvancedMode[3]=true;
                        CountHistory=true;
                        continue;
                       }
                    }
                 }//end if(OpenOrdersInLoss!=0)
              }
            //---
           }//end if(TypeOperation!=0)
        }//end if(MarketIsOpen==true)
      //---------------------------------------------------------------------
      //Close and stop
      if(TypeOperation==3)
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
                  "\n  Pips: "+DoubleToStr(HistoryTotalPips,2)+" || Orders: "+DoubleToStr(HistoryTotalTrades,0)+" || PnL: "+DoubleToStr(HistoryTotalProfitLoss,2)
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
         (ExpertCloseOrdersInAdvancedMode[1]==false)&&(ExpertCloseOrdersInAdvancedMode[2]==false)&&(ExpertCloseOrdersInAdvancedMode[3]==false))
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
                  WarningPrint=StringConcatenate("Orders are in limit (",OrdersTotal(),"/",MaximumOrders,")");
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
   if(TypeOperation<3)
      CommentChart();
//---------------------------------------------------------------------
  }
//====================================================================================================================================================//
//Normalize Lots
//====================================================================================================================================================//
double NormalizeLot(double LotsSize)
  {
//---------------------------------------------------------------------
   return(MathMin(MathMax((MathRound(LotsSize/MarketInfo(Symbol(),MODE_LOTSTEP))*MarketInfo(Symbol(),MODE_LOTSTEP)),MarketInfo(Symbol(),MODE_MINLOT)),MarketInfo(Symbol(),MODE_MAXLOT)));
//---------------------------------------------------------------------
  }
//====================================================================================================================================================//
//Open plus orders
//====================================================================================================================================================//
void OpenPairPlus(int PairOpen, int OpenOrder)
  {
//---------------------------------------------------------------------
   double PriceOpen=0;
   double StopLoss=0;
   double TakeProfit=0;
   double LotSizeOrder=0;
   double LotSize=0;
   double FreeMargin=0;
   color ColorOrder=0;
   string CommentOrder="";
//---------------------------------------------------------------------
//Calculate lot size per pair
   if(LotOrdersProgress==0)
      MultiplierLotPlus[PairOpen]=1;
   if(LotOrdersProgress==1)
      MultiplierLotPlus[PairOpen]=GroupsPlus[PairOpen]+1;
   if(LotOrdersProgress==2)
      MultiplierLotPlus[PairOpen]=MathMax(1,MathPow(2,GroupsPlus[PairOpen]));
//---------------------------------------------------------------------
//Set lots for orders
//---Auto or manual lot
   if(AutoLotSize==1)
      LotSize=(AccountBalance()/100000)*RiskFactor;
   if(AutoLotSize==0)
      LotSize=ManualLotSize;
//---
   if(GroupsPlus[PairOpen]>0)
      LotSizeOrder=NormalizeLot(FirstLotPair[PairOpen][OpenOrder]*MultiplierLotPlus[PairOpen]);
   else
      LotSizeOrder=NormalizeLot(LotSize);
//---------------------------------------------------------------------
//Count free margin
   if(AccountFreeMargin()>AccountFreeMarginCheck(SymbolPair[PairOpen][OpenOrder],SuitePlus[PairOpen][OpenOrder],LotSizeOrder))
      FreeMargin=AccountFreeMargin()-AccountFreeMarginCheck(SymbolPair[PairOpen][OpenOrder],SuitePlus[PairOpen][OpenOrder],LotSizeOrder);
   if(AccountFreeMargin()<AccountFreeMarginCheck(SymbolPair[PairOpen][OpenOrder],SuitePlus[PairOpen][OpenOrder],LotSizeOrder))
      FreeMargin=AccountFreeMargin()+(AccountFreeMargin()-AccountFreeMarginCheck(SymbolPair[PairOpen][OpenOrder],SuitePlus[PairOpen][OpenOrder],LotSizeOrder));
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
         while(TRUE)
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
      else
        {
         CommentWarning=true;
         if(WarningPrint=="")
            WarningPrint=StringConcatenate("  Free margin is low (",DoubleToStr(FreeMargin),")");
         Print(WarningPrint);
         CheckExcessOrders(PairOpen);
        }
     }
//---------------------------------------------------------------------
  }
//====================================================================================================================================================//
//Open minus orders
//====================================================================================================================================================//
void OpenPairMinus(int PairOpen, int OpenOrder)
  {
//---------------------------------------------------------------------
   double PriceOpen=0;
   double StopLoss=0;
   double TakeProfit=0;
   double LotSizeOrder=0;
   double LotSize=0;
   double FreeMargin=0;
   color ColorOrder=0;
   string CommentOrder="";
//---------------------------------------------------------------------
//Calculate lot size per pair
   if(LotOrdersProgress==0)
      MultiplierLotMinus[PairOpen]=1;
   if(LotOrdersProgress==1)
      MultiplierLotMinus[PairOpen]=GroupsMinus[PairOpen]+1;
   if(LotOrdersProgress==2)
      MultiplierLotMinus[PairOpen]=MathMax(1,MathPow(2,GroupsMinus[PairOpen]));
//---------------------------------------------------------------------
//Set lots for orders
//---Auto or manual lot
   if(AutoLotSize==1)
      LotSize=(AccountBalance()/100000)*RiskFactor;
   if(AutoLotSize==0)
      LotSize=ManualLotSize;
//---
   if(GroupsMinus[PairOpen]>0)
      LotSizeOrder=NormalizeLot(FirstLotPair[PairOpen][OpenOrder]*MultiplierLotMinus[PairOpen]);
   else
      LotSizeOrder=NormalizeLot(LotSize);
//---------------------------------------------------------------------
//Count free margin
   if(AccountFreeMargin()>AccountFreeMarginCheck(SymbolPair[PairOpen][OpenOrder],SuiteMinus[PairOpen][OpenOrder],LotSizeOrder))
      FreeMargin=AccountFreeMargin()-AccountFreeMarginCheck(SymbolPair[PairOpen][OpenOrder],SuiteMinus[PairOpen][OpenOrder],LotSizeOrder);
   if(AccountFreeMargin()<AccountFreeMarginCheck(SymbolPair[PairOpen][OpenOrder],SuiteMinus[PairOpen][OpenOrder],LotSizeOrder))
      FreeMargin=AccountFreeMargin()+(AccountFreeMargin()-AccountFreeMarginCheck(SymbolPair[PairOpen][OpenOrder],SuiteMinus[PairOpen][OpenOrder],LotSizeOrder));
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
         while(TRUE)
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
      else
        {
         CommentWarning=true;
         if(WarningPrint=="")
            WarningPrint=StringConcatenate("  Free margin is low (",DoubleToStr(FreeMargin),")");
         Print(WarningPrint);
         CheckExcessOrders(PairOpen);
        }
     }
//---------------------------------------------------------------------
  }
//====================================================================================================================================================//
//Close plus orders
//====================================================================================================================================================//
void ClosePairPlus(int PairClose, int TicketClose, int CloseOrder)
  {
//---------------------------------------------------------------------
   double PriceClose=0;
   color ColorOrder=0;
//---------------------------------------------------------------------
   for(k=OrdersTotal()-1; k>=0; k--)//Last to first
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
               while(TRUE)
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
//---------------------------------------------------------------------
  }
//====================================================================================================================================================//
//Close minus orders
//====================================================================================================================================================//
void ClosePairMinus(int PairClose, int TicketClose, int CloseOrder)
  {
//---------------------------------------------------------------------
   double PriceClose=0;
   color ColorOrder=0;
//---------------------------------------------------------------------
   for(k=OrdersTotal()-1; k>=0; k--)//Last to first
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
               while(TRUE)
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
//---------------------------------------------------------------------
  }
//====================================================================================================================================================//
//Check all open groups for missings orders
//====================================================================================================================================================//
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
//====================================================================================================================================================//
//Check all open groups for excess orders
//====================================================================================================================================================//
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
//====================================================================================================================================================//
//Count current orders and resuluts
//====================================================================================================================================================//
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
//====================================================================================================================================================//
//Count history orders and resuluts
//====================================================================================================================================================//
void CountHistoryOrders(int PairGet)
  {
//---------------------------------------------------------------------
//Reset value
   CountAllHistoryOrders=0;
   HistoryPlusOrders[PairGet]=0;
   HistoryMinusOrders[PairGet]=0;
   HistoryPlusProfit[PairGet]=0;
   HistoryMinusProfit[PairGet]=0;
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
               //---------------------------------------------------------------------
               //Count plus orders
               for(j=1; j<=PairsPerGroup; j++)
                 {
                  if((OrderType()==SuitePlus[PairGet][j])&&(OrderSymbol()==SymbolPair[PairGet][j]))
                    {
                     HistoryTotalOrdersPlus++;
                     HistoryPlusOrders[PairGet]++;
                     HistoryTotalProfitPlus+=OrderProfit()+OrderCommission()+OrderSwap();
                     HistoryPlusProfit[PairGet]+=OrderProfit()+OrderCommission()+OrderSwap();
                    }
                  //---------------------------------------------------------------------
                  //Count minus orders
                  if((OrderType()==SuiteMinus[PairGet][j])&&(OrderSymbol()==SymbolPair[PairGet][j]))
                    {
                     HistoryTotalOrdersMinus++;
                     HistoryMinusOrders[PairGet]++;
                     HistoryTotalProfitMinus+=OrderProfit()+OrderCommission()+OrderSwap();
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
         if(CountAllHistoryOrders!=OrdersHistoryTotal())
            CountHistory=true;//Pass again orders
        }
     }
//---------------------------------------------------------------------
  }
//====================================================================================================================================================//
//Background for comments
//====================================================================================================================================================//
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
//====================================================================================================================================================//
//Display Text/image
//====================================================================================================================================================//
void DisplayText(string StringName, string Image, int FontSize, string TypeImage, color FontColor, int Xposition, int Yposition)
  {
//---------------------------------------------------------------------
   ObjectCreate(StringName,OBJ_LABEL,0,0,0);
   ObjectSet(StringName,OBJPROP_CORNER,0);
   ObjectSet(StringName,OBJPROP_BACK,FALSE);
   ObjectSet(StringName,OBJPROP_XDISTANCE,Xposition);
   ObjectSet(StringName,OBJPROP_YDISTANCE,Yposition);
   ObjectSet(StringName,OBJPROP_SELECTABLE,false);
   ObjectSet(StringName,OBJPROP_SELECTED,false);
   ObjectSet(StringName,OBJPROP_HIDDEN,TRUE);
   ObjectSetText(StringName,Image,FontSize,TypeImage,FontColor);
//---------------------------------------------------------------------
  }
//====================================================================================================================================================//
//Check value
//====================================================================================================================================================//
void CheckValue()
  {
//---------------------------------------------------------------------
   WrongSet=false;
//---------------------------------------------------------------------
//Check step value
   if((OpenOrdersInLoss==1)&&(StepOpenNextOrders<=0)&&(WrongSet==false))
     {
      Comment("\n "+StringOrdersEA+
              "\n\n\nStepOpenNextOrders parameter not correct ("+DoubleToStr(StepOpenNextOrders,2)+")"
              "\n\nPlease insert a value greater than 0");
      Print(" # "+WindowExpertName()+" # "+"StepOpenNextOrders parameter not correct ("+DoubleToStr(StepOpenNextOrders,2)+")");
      WrongSet=true;
      WarningMessage=MessageBox(" # "+WindowExpertName()+" # "+"StepOpenNextOrders parameter not correct ("+DoubleToStr(StepOpenNextOrders,2)+"), please insert a value greater than 0", "RISK DISCLAIMER");
     }
//---------------------------------------------------------------------
//Check profit close value
   if((TargetCloseProfit<=0)&&(WrongSet==false))
     {
      Comment("\n "+StringOrdersEA+
              "\n\n\nTargetCloseProfit parameter not correct ("+DoubleToStr(TargetCloseProfit,2)+")"
              "\n\nPlease insert a value greater than 0");
      Print(" # "+WindowExpertName()+" # "+"TargetCloseProfit parameter not correct ("+DoubleToStr(TargetCloseProfit,2)+")");
      WrongSet=true;
      WarningMessage=MessageBox(" # "+WindowExpertName()+" # "+"TargetCloseProfit parameter not correct ("+DoubleToStr(TargetCloseProfit,2)+"), please insert a value greater than 0", "RISK DISCLAIMER");
     }
//---------------------------------------------------------------------
//Check loss close value
   if((TypeCloseInLoss<2)&&(TargetCloseLoss<=0)&&(WrongSet==false))
     {
      Comment("\n "+StringOrdersEA+
              "\n\n\nTargetCloseLoss parameter not correct ("+DoubleToStr(TargetCloseLoss,2)+")"
              "\n\nPlease insert a value greater than 0");
      Print(" # "+WindowExpertName()+" # "+"TargetCloseLoss parameter not correct ("+DoubleToStr(TargetCloseLoss,2)+")");
      WrongSet=true;
      WarningMessage=MessageBox(" # "+WindowExpertName()+" # "+"TargetCloseLoss parameter not correct ("+DoubleToStr(TargetCloseLoss,2)+"), please insert a value greater than 0", "RISK DISCLAIMER");
     }
//---------------------------------------------------------------------
  }
//====================================================================================================================================================//
//Check locked
//====================================================================================================================================================//
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
            TotalOrders+=(TotalOrdersPlus[cnt4][1]+TotalOrdersPlus[cnt4][2]+TotalOrdersMinus[cnt4][1]+TotalOrdersMinus[cnt4][2]);
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
//====================================================================================================================================================//
//Comments in the chart
//====================================================================================================================================================//
void CommentChart()
  {
//---------------------------------------------------------------------
   double LevelCloseInLossPlus[99];
   double LevelCloseInLossMinus[99];
   double ShowMaxProfit=0;
   double ShowMinProfit=0;
   double TotalProfitLoss=0;
   double TotalProfitLossPlus=0;
   double TotalProfitLossMinus=0;
   string FirstLineStr;
   string CloseProfitStr;
   string CloseLossStr;
   string SpreadStr;
   string StepNextStr;
   string LotOrdersStr;
   string ShowInfoOfPairs;
   string Space[99];
   color TextColor;
   int FileHandle;
//---------------------------------------------------------------------
//Reset values
   ArrayInitialize(LevelCloseInLossPlus,0);
   ArrayInitialize(LevelCloseInLossMinus,0);
   TotalOrders=0;
   TotalLots=0;
//---------------------------------------------------------------------
//First line comment
   if(TypeOperation==0)
      FirstLineStr=StringConcatenate("Expert Is In Stand By Mode");
   if(TypeOperation==1)
      FirstLineStr=StringConcatenate("Expert Is Ready To Open/Close Orders");
   if(TypeOperation==2)
      FirstLineStr=StringConcatenate("Expert Wait Close In Profit And Stop");
   if(TypeOperation==3)
      FirstLineStr=StringConcatenate("Expert Close Immediately All Orders");
//---
   if(CommentWarning==true)
      FirstLineStr=StringConcatenate("Warning: ",WarningPrint);
//---------------------------------------------------------------------
//Close mode
   if(TypeCloseInProfit==0)
      CloseProfitStr="Single Ticket ("+DoubleToStr(TargetCloseProfit,2)+")";
   if(TypeCloseInProfit==1)
      CloseProfitStr="Basket Ticket ("+DoubleToStr(TargetCloseProfit,2)+")";
   if(TypeCloseInProfit==2)
      CloseProfitStr="Hybrid Mode ("+DoubleToStr(TargetCloseProfit,2)+")";
//---
   if(TypeCloseInLoss==0)
      CloseLossStr="Whole Ticket ("+DoubleToStr(-TargetCloseLoss,2)+")";
   if(TypeCloseInLoss==1)
      CloseLossStr="Partial Ticket ("+DoubleToStr(-TargetCloseLoss,2)+" / "+IntegerToString(GroupsStartClose)+")";
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
         StepNextStr="Manual / Statical ("+DoubleToStr(StepOpenNextOrders,1)+")";
      if(StepOrdersProgress==1)
         StepNextStr="Manual / Geometrical ("+DoubleToStr(StepOpenNextOrders,1)+")";
      if(StepOrdersProgress==2)
         StepNextStr="Manual / Exponential ("+DoubleToStr(StepOpenNextOrders,1)+")";
     }
//---
   if(OpenOrdersInLoss==2)
     {
      if(StepOrdersProgress==0)
         StepNextStr="Automatic / Statical ("+DoubleToStr(TargetCloseProfit,1)+")";
      if(StepOrdersProgress==1)
         StepNextStr="Automatic / Geometrical ("+DoubleToStr(TargetCloseProfit,1)+")";
      if(StepOrdersProgress==2)
         StepNextStr="Automatic / Exponential ("+DoubleToStr(TargetCloseProfit,1)+")";
     }
//---------------------------------------------------------------------
//Lot orders
   if(AutoLotSize==false)
     {
      if(LotOrdersProgress==0)
         LotOrdersStr="Manual / Statical ("+DoubleToStr(ManualLotSize,2)+")";
      if(LotOrdersProgress==1)
         LotOrdersStr="Manual / Geometrical ("+DoubleToStr(ManualLotSize,2)+")";
      if(LotOrdersProgress==2)
         LotOrdersStr="Manual / Exponential ("+DoubleToStr(ManualLotSize,2)+")";
     }
//---
   if(AutoLotSize==true)
     {
      if(LotOrdersProgress==0)
         LotOrdersStr="Automatic / Statical ("+DoubleToStr((AccountBalance()/100000)*RiskFactor,2)+")";
      if(LotOrdersProgress==1)
         LotOrdersStr="Automatic / Geometrical ("+DoubleToStr((AccountBalance()/100000)*RiskFactor,2)+")";
      if(LotOrdersProgress==2)
         LotOrdersStr="Automatic / Exponential ("+DoubleToStr((AccountBalance()/100000)*RiskFactor,2)+")";
     }
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
         TotalProfitLossPlus+=SumProfitPlus[i];
         TotalProfitLossMinus+=SumProfitMinus[i];
         TotalLots+=TotalLotPlus[i][1]+TotalLotPlus[i][2]+TotalLotMinus[i][1]+TotalLotMinus[i][2];
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
         MaxTotalOrders=MathMax(MaxTotalOrders,TotalOrders);
         //---------------------------------------------------------------------
         //Speread string
         if(MaxSpread==0)
            SpreadStr="Expert Not Check Spread";
         if(MaxSpread!=0)
            SpreadStr="Expert Check Spread ("+DoubleToStr(MaxSpread,2)+")";
         //---------------------------------------------------------------------
         //Set space
         if(i<9)
            Space[i]="  ";
         //---------------------------------------------------------------------
         //Set comments on chart
         if(ShowPairsInfo==true)
           {
            //---Set color
            TextColor=ColorOfInfo;
            if(TypeCloseInProfit==2)
              {
               if(SideToOpenOrders==0)
                 {
                  if((GroupWithMaximumProfitsPlus!=-1)&&(i==GroupWithMaximumProfitsPlus))
                     TextColor=clrAqua;
                  if((GroupWithMaximumLossesPlus!=-1)&&(i==GroupWithMaximumLossesPlus))
                     TextColor=clrHotPink;
                 }
               //---
               if(SideToOpenOrders==1)
                 {
                  if((GroupWithMaximumProfitsMinus!=-1)&&(i==GroupWithMaximumProfitsMinus))
                     TextColor=clrAqua;
                  if((GroupWithMaximumLossesMinus!=-1)&&(i==GroupWithMaximumLossesMinus))
                     TextColor=clrHotPink;
                 }
               //---
               if(SideToOpenOrders==2)
                 {
                  if((GroupWithMaximumProfits!=-1)&&(i==GroupWithMaximumProfits))
                     TextColor=clrAqua;
                  if((GroupWithMaximumLosses!=-1)&&(i==GroupWithMaximumLosses))
                     TextColor=clrHotPink;
                 }
              }
            //---Make strings
            if(SymbolPair[i][1]!="")
              {
               //---Str1
               if(ObjectFind("Str1")==-1)
                  DisplayText("Str1","Pairs",SizeFontsOfInfo,"Arial Black",ColorOfTitle,310,0);
               //---Str2
               if(ObjectFind("Str2")==-1)
                  DisplayText("Str2","Orders",SizeFontsOfInfo,"Arial Black",ColorOfTitle,PositionOrders,0);
               //---Str3
               if(ObjectFind("Str3")==-1)
                  DisplayText("Str3","PnL",SizeFontsOfInfo,"Arial Black",ColorOfTitle,PositionPnL,0);
               //---Str4
               if(ObjectFind("Str4")==-1)
                  DisplayText("Str4","Close",SizeFontsOfInfo,"Arial Black",ColorOfTitle,PositionClose,0);
               //---Str5
               if(ObjectFind("Str5")==-1)
                  DisplayText("Str5","History",SizeFontsOfInfo,"Arial Black",ColorOfTitle,PositionHistory,0);
               //---Str6
               if(ObjectFind("Str6")==-1)
                  DisplayText("Str6","Maximum",SizeFontsOfInfo,"Arial Black",ColorOfTitle,PositionMaximum,0);
               //---Str7
               if(ObjectFind("Str7")==-1)
                  DisplayText("Str7","Spread",SizeFontsOfInfo,"Arial Black",ColorOfTitle,PositionSpread,0);
               //---Comm1
               ObjectDelete("Comm1"+IntegerToString(i));
               if(ObjectFind("Comm1"+IntegerToString(i))==-1)
                  DisplayText("Comm1"+IntegerToString(i),IntegerToString(i+1)+". "+Space[i]+StringSubstr(SymbolPair[i][1],0,6)+"-"+StringSubstr(SymbolPair[i][2],0,6),SizeFontsOfInfo,"Arial Black",TextColor,245,20+(i*14));
               //---
               if(SkippedStatus[i]!="Couple Skipped by user from external parameters")
                 {
                  //---Comm2
                  ObjectDelete("Comm2"+IntegerToString(i));
                  if(ObjectFind("Comm2"+IntegerToString(i))==-1)
                     DisplayText("Comm2"+IntegerToString(i),StringConcatenate(TotalOrdersPlus[i][1],"/",TotalOrdersPlus[i][2],"-",TotalOrdersMinus[i][1],"/",TotalOrdersMinus[i][2]),SizeFontsOfInfo,"Arial Black",TextColor,PositionOrders,20+(i*14));
                  //---Comm3
                  ObjectDelete("Comm3"+IntegerToString(i));
                  if(ObjectFind("Comm3"+IntegerToString(i))==-1)
                     DisplayText("Comm3"+IntegerToString(i),DoubleToStr(SumProfitPlus[i]+SumProfitMinus[i],2),SizeFontsOfInfo,"Arial Black",TextColor,PositionPnL-5,20+(i*14));
                  //---Comm4
                  ObjectDelete("Comm4"+IntegerToString(i));
                  if(ObjectFind("Comm4"+IntegerToString(i))==-1)
                     DisplayText("Comm4"+IntegerToString(i),DoubleToStr(MathMax(LevelProfitClosePlus[i],LevelProfitCloseMinus[i]),2),SizeFontsOfInfo,"Arial Black",TextColor,PositionClose+5,20+(i*14));
                  //---Comm5
                  ObjectDelete("Comm5"+IntegerToString(i));
                  if(ObjectFind("Comm5"+IntegerToString(i))==-1)
                     DisplayText("Comm5"+IntegerToString(i),DoubleToStr(HistoryPlusOrders[i]+HistoryMinusOrders[i],0)+"/"+DoubleToStr(HistoryPlusProfit[i]+HistoryMinusProfit[i],2),SizeFontsOfInfo,"Arial Black",TextColor,PositionHistory,20+(i*14));
                  //---Comm6
                  ObjectDelete("Comm6"+IntegerToString(i));
                  if(ObjectFind("Comm6"+IntegerToString(i))==-1)
                     DisplayText("Comm6"+IntegerToString(i)," ("+DoubleToStr(MaxOrders[i],0)+"/"+DoubleToStr(MaxFloating[i],2)+")",SizeFontsOfInfo,"Arial Black",TextColor,PositionMaximum,20+(i*14));
                  //---Comm7
                  ObjectDelete("Comm7"+IntegerToString(i));
                  if(ObjectFind("Comm7"+IntegerToString(i))==-1)
                     DisplayText("Comm7"+IntegerToString(i),DoubleToStr(SpreadPair[i][1]+SpreadPair[i][2],1),SizeFontsOfInfo,"Arial Black",TextColor,PositionSpread+15,20+(i*14));
                 }
               else
                 {
                  ObjectDelete("Comm2"+IntegerToString(i));
                  if(ObjectFind("Comm2"+IntegerToString(i))==-1)
                     DisplayText("Comm2"+IntegerToString(i),SkippedStatus[i],SizeFontsOfInfo,"Arial",TextColor,PositionOrders,20+(i*14));
                 }
               //---Background0
               if(ObjectFind("BackgroundLine0")==-1)
                  ChartBackground("BackgroundLine0",ColorLineTitles,EMPTY_VALUE,TRUE,245,0,PositionSpread-245+55,24);
               //---Background1
               if((i<NumberGroupsTrade/2)&&(MathMod(NumberGroupsTrade,2)==0))
                  if(ObjectFind("BackgroundLine1"+IntegerToString(i))==-1)
                     ChartBackground("BackgroundLine1"+IntegerToString(i),ColorOfLine1,EMPTY_VALUE,TRUE,245,22+(i*14*2),PositionSpread-245+55,16);
               if((i<=NumberGroupsTrade/2)&&(MathMod(NumberGroupsTrade,2)==1))
                  if(ObjectFind("BackgroundLine1"+IntegerToString(i))==-1)
                     ChartBackground("BackgroundLine1"+IntegerToString(i),ColorOfLine1,EMPTY_VALUE,TRUE,245,22+(i*14*2),PositionSpread-245+55,16);
               //---Background2
               if(i<NumberGroupsTrade/2)
                  if(ObjectFind("BackgroundLine2"+IntegerToString(i))==-1)
                     ChartBackground("BackgroundLine2"+IntegerToString(i),ColorOfLine2,EMPTY_VALUE,TRUE,245,36+(i*14*2),PositionSpread-245+55,16);
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
            FileWrite(FileHandle,"Group No: "+IntegerToString(i+1)+" || Pairs: "+StringSubstr(SymbolPair[i][1],0,6)+"-"+StringSubstr(SymbolPair[i][2],0,6)+" || History Profit: "+DoubleToStr(HistoryPlusProfit[i]+HistoryMinusProfit[i],2)+" || Max Orders: "+DoubleToStr(MaxOrders[i],0)+" || Max Floating: "+DoubleToStr(MaxFloating[i],2));
            FileFlush(FileHandle);
            if(i==NumberGroupsTrade-1)
               LastHourSaved=TimeHour(TimeCurrent());
           }
         FileClose(FileHandle);
        }
      //---
     }//End if(SaveInformations==true)
//---------------------------------------------------------------------
//Chart comment
   Comment("================================="+
           "\n  "+FirstLineStr+
           "\n================================="+
           "\n  Spread: "+SpreadStr+"  ("+DoubleToStr(TotalGroupsSpread,2)+")"+
           "\n================================="+
           "\n  Close In Profit Orders: "+CloseProfitStr+
           "\n  Close In Loss Orders  : "+CloseLossStr+
           "\n  Step For Next Order  : "+StepNextStr+
           "\n  Order Lot Size Type  : "+LotOrdersStr+
           "\n================================="+
           ShowInfoOfPairs+
           "\n  Orders: "+IntegerToString(TotalOrders)+" || PnL: "+DoubleToStr(TotalProfitLoss,2)+"  ["+DoubleToStr(TotalProfitLossPlus,2)+"/"+DoubleToStr(TotalProfitLossMinus,2)+"]"+
           "\n================================="+
           "\n  Maximum Orders: "+IntegerToString(MaxTotalOrders)+" || Maximum PnL: "+DoubleToStr(ShowMinProfit,2)+
           "\n================================="+
           "\n  T O T A L   H I S T O R Y   R E S U L T S"+
           "\n  Orders: "+IntegerToString(HistoryTotalTrades)+" ["+IntegerToString(HistoryTotalOrdersPlus)+"/"+IntegerToString(HistoryTotalOrdersMinus)+"]"+" || PnL: "+DoubleToStr(HistoryTotalProfitLoss,2)+" ["+DoubleToStr(HistoryTotalProfitPlus,2)+"/"+DoubleToStr(HistoryTotalProfitMinus,2)+"]"+
           "\n=================================");
//---------------------------------------------------------------------
  }
//====================================================================================================================================================//
//End of code
//====================================================================================================================================================//
