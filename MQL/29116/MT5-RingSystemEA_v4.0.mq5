//===============================================================================================================================================================================================================================================================//
//Start code
//===============================================================================================================================================================================================================================================================//
#property copyright   "Copyright 2012-2020, Nikolaos Pantzos"
#property link        "https://www.mql5.com/en/users/pannik"
#property version     "4.0"
#property description "It's a multi currency system (use and need at least 3 pairs, which created from 3 currencies, to trade)."
#property description "Expert can to use maximum 8 currencies to make 28 pairs from which it creates 56 rings."
#property description "It is important the order of currencies be from the strongest to the weakest."
#property description "The cryptos are imported first, then the metals and finally the currencies."
#property description "Strongest... EUR/GBP/AUD/NZD/USD/CAD/CHF/JPY ...weakest."
#property description "Attach expert in one chart only (no matter what pair or time frame)."
//#property icon        "\\Images\\RS_EA_Logo.ico";
#property strict
//===============================================================================================================================================================================================================================================================//
enum Oper   {Stand_by_Mode, Normal_Operation, Close_In_Profit_And_Stop, Close_Immediately_All_Orders};
enum Side   {Open_Only_Plus, Open_Only_Minus, Open_Plus_And_Minus};
enum Step   {Not_Open_In_Loss, Open_With_Manual_Step, Open_With_Auto_Step};
enum ProgrS {Statical_Step, Geometrical_Step, Exponential_Step};
enum CloseP {Single_Ticket, Basket_Ticket};
enum CloseL {Whole_Ticket, Partial_Ticket, Not_Close_In_Loss};
enum ProgrL {Statical_Lot, Geometrical_Lot, Exponential_Lot, Decreases_Lot};
//===============================================================================================================================================================================================================================================================//
#define MaxGroups     57
#define MaxPairs      4
#define PairsPerGroup 3
#define MagicSet      1230321
//===============================================================================================================================================================================================================================================================//
input string OperationStr       = "||---------- Operation Set ----------||";//___ Exteral Settings_1 ___
input Oper   TypeOfOperation    = Normal_Operation;//Type Of Operation Mode
input int    TimerInMillisecond = 1000;//Timer In Millisecond For Events
input string ManagePairsUse     = "||---------- Manage Pairs And Side ----------||";//___ Exteral Settings_2 ___
input string CurrenciesTrade    = "EUR/GBP/USD";//Currencies To Make Pairs (Min 3, Max 8)
input string NoOfGroupToSkip    = "57,58,59,60";//No Of Groups To Skip (Separated by ',')
input Side   SideOpenOrders     = Open_Plus_And_Minus;//Side Open Orders
input string ManageOpenOrders   = "||---------- Manage Open Orders ----------||";//___ Exteral Settings_3 ___
input Step   OpenOrdersInLoss   = Open_With_Manual_Step;//Open Orders In Loss Mode
input double StepOpenNextOrders = 200.0;//Step For Next Orders (Value $/Lot)
input ProgrS StepOrdersProgress = Statical_Step;//Type Of Progress Step
input int    MinutesForNextOrder= 60;//Minutes Between Orders
input int    MaximumGroups      = 0;//Max Opened Groups (0=Not Limit)
input string ManageCloseProfit  = "||---------- Manage Close Profit Orders ----------||";//___ Exteral Settings_4 ___
input CloseP TypeCloseInProfit  = Single_Ticket;//Type Of Close In Profit Orders
input double TargetCloseProfit  = 200.0;//Target Close In Profit (Value $/Lot)
input int    DelayCloseProfit   = 1;//Delay Before Close In Profit (Value Ticks)
input string ManageCloseLoss    = "||---------- Manage Close Losses Orders ----------||";//___ Exteral Settings_5 ___
input CloseL TypeCloseInLoss    = Not_Close_In_Loss;//Type Of Close In Loss Orders
input double TargetCloseLoss    = 1000.0;//Target Close In Loss (Value $/Lot)
input int    DelayCloseLoss     = 1;//Delay Before Close In Loss (Value Ticks)
input string MoneyManagement    = "||---------- Money Management ----------||";//___ Exteral Settings_6 ___
input bool   AutoLotSize        = false;//Use Auto Lot Size
input double RiskFactor         = 1.0;//Risk Factor For Auto Lot Size
input double ManualLotSize      = 0.01;//Manual Lot Size
input ProgrL LotOrdersProgress  = Statical_Lot;//Type Of Progress Lot
input bool   UseFairLotSize     = false;//Use Fair Lot Size For Each Pair
input double MaximumLotSize     = 0.0;//Max Lot Size (0=Not Limit)
input string ControlSessionSet  = "||---------- Control Session ----------||";//___ Exteral Settings_7 ___
input bool   ControlSession     = false;//Use Trade Control Session
input int    WaitAfterOpen      = 60;//Wait After Monday Open
input int    StopBeforeClose    = 60;//Stop Before Friday Close
input string InfoOnTheScreen    = "||---------- Info On The Screen ----------||";//___ Exteral Settings_8 ___
input bool   ShowPairsInfo      = true;//Show Pairs Info On Screen
input color  ColorOfTitle       = clrKhaki;//Color Of Titles
input color  ColorOfInfo        = clrBeige;//Color Of Info
input color  ColorLineTitles    = clrOrange;//Color Of Line Titles
input color  ColorOfLine1       = clrMidnightBlue;//Color Of Line 1
input color  ColorOfLine2       = clrDarkSlateGray;//Color Of Line 2
input int    PositionOrders     = 485;//Position 'Orders' Info
input int    PositionPnL        = 580;//Position 'PnL' Info
input int    PositionClose      = 645;//Position 'Close' Info
input int    PositionNext       = 785;//Position 'Next' Info
input int    PositionHistory    = 900;//Position 'History' Info
input int    PositionMaximum    = 1000;//Position 'Maximum' Info
input int    PositionSpread     = 1125;//Position 'Spread' Info
input string Limitations        = "||---------- Limitations ----------||";//___ Exteral Settings_9 ___
input double MaxSpread          = 0.0;//Max Accepted Spread (0=Not Check)
input long   MaximumOrders      = 0;//Max Total Opened Orders (0=Not Limit)
input int    MaxSlippage        = 3;//Max Accepted Slippage
input string Configuration      = "||---------- Configuration ----------||";//___ Exteral Settings_10 ___
input string SymbolPrefix       = "NONE";//Add Symbol Prefix
input string SymbolSuffix       = "AUTO";//Add Symbol Suffix
input int    MagicNumber        = 0;//Orders' ID (0=Generate Automatically)
input bool   CheckOrders        = true;//Check All Orders
input bool   ShowTaskInfo       = true;//Show On Chart Information
input bool   PrintLogReport     = false;//Print Log Report
input string StringOrdersEA     = "RingSystemEA";//Comment For Orders
input bool   SetChartInterface  = true;//Set Chart Appearance
input bool   SaveInformations   = false;//Save Groups Informations
//===============================================================================================================================================================================================================================================================//
string SymPrefix;
string SymSuffix;
string CommentsEA;
string WarningPrint="";
string SymbolStatus[MaxGroups][MaxPairs];
string SkippedStatus[MaxGroups];
string Currencies[99];
//---------------------------------------------------------------------
double BidPricePair[MaxGroups][MaxPairs];
double SumSpreadGroup[MaxGroups];
double FirstLotPlus[MaxGroups][MaxPairs];
double FirstLotMinus[MaxGroups][MaxPairs];
double LastLotPlus[MaxGroups][MaxPairs];
double LastLotMinus[MaxGroups][MaxPairs];
double CheckMargin[MaxGroups];
double TotalProfitPlus[MaxGroups][MaxPairs];
double TotalProfitMinus[MaxGroups][MaxPairs];
double MaxProfit=-99999;
double MinProfit=99999;
double LevelProfitClosePlus[MaxGroups];
double LevelProfitCloseMinus[MaxGroups];
double LevelLossClosePlus[MaxGroups];
double LevelLossCloseMinus[MaxGroups];
double LevelOpenNextPlus[MaxGroups];
double LevelOpenNextMinus[MaxGroups];
double HistoryTotalProfitLoss;
double iLotSize;
double TotalLotPlus[MaxGroups][MaxPairs];
double TotalLotMinus[MaxGroups][MaxPairs];
double MultiplierStepPlus[MaxGroups];
double MultiplierStepMinus[MaxGroups];
double MultiplierLotPlus[MaxGroups];
double MultiplierLotMinus[MaxGroups];
double SumSpreadValuePlus[MaxGroups];
double SumSpreadValueMinus[MaxGroups];
double TotalCommissionPlus[MaxGroups][MaxPairs];
double TotalCommissionMinus[MaxGroups][MaxPairs];
double FirstProfitPlus[MaxGroups][MaxPairs];
double FirstProfitMinus[MaxGroups][MaxPairs];
double MaxFloating[MaxGroups];
double TotalProfitLoss;
double TotalLots;
double FirstTotalLotPlus[MaxGroups];
double FirstTotalLotMinus[MaxGroups];
double FirstTotalProfitPlus[MaxGroups];
double FirstTotalProfitMinus[MaxGroups];
double SumProfitPlus[MaxGroups];
double SumProfitMinus[MaxGroups];
double SumCommissionPlus[MaxGroups];
double SumCommissionMinus[MaxGroups];
double SumLotPlus[MaxGroups];
double SumLotMinus[MaxGroups];
double SpreadPair[MaxGroups][MaxPairs];
double SpreadValuePlus[MaxGroups][MaxPairs];
double SpreadValueMinus[MaxGroups][MaxPairs];
double HistoryPlusProfit[MaxGroups];
double HistoryMinusProfit[MaxGroups];
double TotalGroupsProfit=0;
double TickValuePair[MaxGroups][MaxPairs];
double FirstLotPair[MaxGroups][MaxPairs];
double TotalGroupsSpread;
double LastPrice[MaxGroups][MaxPairs];
double MaxTotalLots=0;
double MaxLot;
double MinLot;
//---------------------------------------------------------------------
int NumOfSlash=0;
int FindSlash=0;
int LengthOfCurrencies=0;
int m=0;
int i;
int j;
int k;
int x;
int n;
int OperationsMode;
int MagicNo;
long AcceptMaxOrders;
int CountComma;
int OrdersID[MaxGroups];
int TicketNo[MaxGroups];
int DecimalsPair;
int LenPrefix=0;
int MaxTotalOrders=0;
int MultiplierPoint;
ulong FirstTicketPlus[MaxGroups][MaxPairs];
ulong FirstTicketMinus[MaxGroups][MaxPairs];
ulong LastTicketPlus[MaxGroups][MaxPairs];
ulong LastTicketMinus[MaxGroups][MaxPairs];
int TotalOrdersPlus[MaxGroups][MaxPairs];
int TotalOrdersMinus[MaxGroups][MaxPairs];
int HistoryTotalTrades;
int HistoryPlusOrders[MaxGroups];
int HistoryMinusOrders[MaxGroups];
ENUM_ORDER_TYPE SuitePlus[MaxGroups][MaxPairs];
ENUM_ORDER_TYPE SuiteMinus[MaxGroups][MaxPairs];
int CntTry;
int CntTick=0;
int CheckTicksOpenMarket;
int DelayTimesForCloseInLossPlus[MaxGroups];
int DelayTimesForCloseInLossMinus[MaxGroups];
int DelayTimesForCloseInProfitPlus[MaxGroups];
int DelayTimesForCloseInProfitMinus[MaxGroups];
int DelayTimesForCloseBasketProfit[MaxGroups];
int DelayTimesForCloseBasketLoss[MaxGroups];
int CountAllOpenedOrders;
int LastHistoryOrders=0;
int WarningMessage;
int GetCurrencyPos[MaxGroups];
int MaxOrders[MaxGroups];
int TotalOrders;
int NumberCurrenciesTrade;
int SumOrdersPlus[MaxGroups];
int SumOrdersMinus[MaxGroups];
int GroupsPlus[MaxGroups];
int GroupsMinus[MaxGroups];
int TotalGroupsOrders=0;
int NumberGroupsSkip[MaxGroups];
int FindComma[200];
int PositionSkipped=0;
int DecimalsGet=0;
int CountSkippedGroups;
int GetGroupUnUse[MaxGroups];
int SignalsMessageWarning;
int LastHourSaved=0;
int GroupsUses;
int PosOfSlash[100];
//---------------------------------------------------------------------
bool SpreadOK[MaxGroups];
bool OrdersIsOK[MaxGroups];
bool CommentWarning;
bool CountHistory=false;
bool StopWorking=false;
bool WrongSet=false;
bool WrongPairs=false;
bool MarketIsOpen=false;
bool CallMain=false;
bool ChangeOperation=false;
bool TimeToTrade=true;
bool ExpertCloseBasketInProfit[MaxGroups];
bool ExpertCloseBasketInLoss[MaxGroups];
bool ExpertClosePlusInLoss[MaxGroups];
bool ExpertClosePlusInProfit[MaxGroups];
bool ExpertCloseMinusInLoss[MaxGroups];
bool ExpertCloseMinusInProfit[MaxGroups];
bool SkipGroup[MaxGroups];
bool FirsOrdersPlusOK[MaxGroups];
bool FirsOrdersMinusOK[MaxGroups];
bool LimitOfOrdersOk;
//---------------------------------------------------------------------
datetime TimeBegin;
datetime TimeEnd;
datetime ChcekLockedDay=0;
datetime DiffTimes;
datetime StartTime;
datetime TimeOpenLastPlus[MaxGroups];
datetime TimeOpenLastMinus[MaxGroups];
datetime FirstOpenedOrder;
//---------------------------------------------------------------------
int NumberGroupsTrade=0;
string SymbolPair[MaxGroups][MaxPairs];
//---------------------------------------------------------------------
long ChartColor;
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
      DrawObjects("Background",(color)ChartColor,BORDER_FLAT,true,0,16,240,274);
//---------------------------------------------------------------------
//Set timer
   EventSetMillisecondTimer(TimerInMillisecond);
   StartTime=TimeCurrent();
//---------------------------------------------------------------------
//Reset value
   ArrayInitialize(SkipGroup,false);
   ArrayInitialize(OrdersIsOK,true);
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
   ArrayInitialize(NumberGroupsSkip,-1);
   ArrayInitialize(GetGroupUnUse,-1);
   ArrayInitialize(FindComma,0);
   ArrayInitialize(PosOfSlash,-1);
   CountSkippedGroups=0;
   CheckTicksOpenMarket=0;
   NumberCurrenciesTrade=0;
   PositionSkipped=0;
   WrongPairs=false;
   m=0;
   CntTick=0;
   CountComma=0;
   MaxLot=999999999;
   MinLot=-999999999;
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
//Calculate for 4 or 5 digits broker
   MultiplierPoint=1;
   DecimalsPair=(int)SymbolInfoInteger(SymPrefix+"EURUSD"+SymSuffix,SYMBOL_DIGITS);
   if((DecimalsPair==3)||(DecimalsPair==5))
      MultiplierPoint=10;
//---------------------------------------------------------------------
//Comments orders
   if(StringOrdersEA=="")
      CommentsEA=MQLInfoString(MQL_PROGRAM_NAME);
   else
      CommentsEA=StringOrdersEA;
//---------------------------------------------------------------------
//Work with currencies
   LengthOfCurrencies=StringLen(CurrenciesTrade);
//---
   for(i=0; i<LengthOfCurrencies; i++)
     {
      FindSlash=StringFind(CurrenciesTrade,"/",i);
      if(FindSlash!=-1)
        {
         if((PosOfSlash[m]==-1)&&(m==0))
           {
            PosOfSlash[m]=FindSlash;
            m++;
           }
         //---
         if((PosOfSlash[m]==-1)&&(m>0)&&(FindSlash!=PosOfSlash[m-1]))
           {
            PosOfSlash[m]=FindSlash;
            m++;
           }
        }
     }
//---
   NumOfSlash=m;
//---------------------------------------------------------------------
//Set up pairs
   NumberCurrenciesTrade=NumOfSlash+1;
//---Set numbers of groups
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
//---------------------------------------------------------------------
//Check and info
   if(NumberCurrenciesTrade<3)
     {
      Comment("\n "+StringOrdersEA+
              "\n\n --- W A R N I N G S ---"+
              "\n\nNumber of currencies to add \nis below the threshold of 3 (",NumberCurrenciesTrade,")"+
              "\n\nplease check added currencies!");
      Print("Number of currencies to add is below the threshold of 3 (",NumberCurrenciesTrade,")");
      return(INIT_FAILED);
     }
//---
   if(NumberCurrenciesTrade>8)
     {
      Comment("\n "+StringOrdersEA+
              "\n\n --- W A R N I N G S ---"+
              "\n\nNumber of currencies to add \nis above the threshold of 8 (",NumberCurrenciesTrade,")"+
              "\n\nplease check added currencies!");
      Print("Number of currencies to add is above the threshold of 8 (",NumberCurrenciesTrade,")");
      return(INIT_FAILED);
     }
//---------------------------------------------------------------------
//Set currencies
   Currencies[1]=StringSubstr(CurrenciesTrade,0,PosOfSlash[0]);
   Currencies[2]=StringSubstr(CurrenciesTrade,PosOfSlash[0]+1,PosOfSlash[1]-PosOfSlash[0]-1);
   Currencies[3]=StringSubstr(CurrenciesTrade,PosOfSlash[1]+1,PosOfSlash[2]-PosOfSlash[1]-1);
   Currencies[4]=StringSubstr(CurrenciesTrade,PosOfSlash[2]+1,PosOfSlash[3]-PosOfSlash[2]-1);
   Currencies[5]=StringSubstr(CurrenciesTrade,PosOfSlash[3]+1,PosOfSlash[4]-PosOfSlash[3]-1);
   Currencies[6]=StringSubstr(CurrenciesTrade,PosOfSlash[4]+1,PosOfSlash[5]-PosOfSlash[4]-1);
   Currencies[7]=StringSubstr(CurrenciesTrade,PosOfSlash[5]+1,PosOfSlash[6]-PosOfSlash[5]-1);
   Currencies[8]=StringSubstr(CurrenciesTrade,PosOfSlash[6]+1,PosOfSlash[7]-PosOfSlash[6]-1);
//---------------------------------------------------------------------
//Set up Groups
   if(NumberCurrenciesTrade>=3)
     {
      //---(1/2/3)
      SymbolPair[0][1]=SymPrefix+Currencies[1]+Currencies[2]+SymSuffix;
      SymbolPair[0][2]=SymPrefix+Currencies[1]+Currencies[3]+SymSuffix;
      SymbolPair[0][3]=SymPrefix+Currencies[2]+Currencies[3]+SymSuffix;
     }
//---Set groups of 4 currencies
   if(NumberCurrenciesTrade>=4)
     {
      //---(1/2/4)
      SymbolPair[1][1]=SymPrefix+Currencies[1]+Currencies[2]+SymSuffix;
      SymbolPair[1][2]=SymPrefix+Currencies[1]+Currencies[4]+SymSuffix;
      SymbolPair[1][3]=SymPrefix+Currencies[2]+Currencies[4]+SymSuffix;
      //---(1/3/4)
      SymbolPair[2][1]=SymPrefix+Currencies[1]+Currencies[3]+SymSuffix;
      SymbolPair[2][2]=SymPrefix+Currencies[1]+Currencies[4]+SymSuffix;
      SymbolPair[2][3]=SymPrefix+Currencies[3]+Currencies[4]+SymSuffix;
      //---(2/3/4)
      SymbolPair[3][1]=SymPrefix+Currencies[2]+Currencies[3]+SymSuffix;
      SymbolPair[3][2]=SymPrefix+Currencies[2]+Currencies[4]+SymSuffix;
      SymbolPair[3][3]=SymPrefix+Currencies[3]+Currencies[4]+SymSuffix;
     }
//---Set groups of 5 currencies
   if(NumberCurrenciesTrade>=5)
     {
      //---(1/2/5)
      SymbolPair[4][1]=SymPrefix+Currencies[1]+Currencies[2]+SymSuffix;
      SymbolPair[4][2]=SymPrefix+Currencies[1]+Currencies[5]+SymSuffix;
      SymbolPair[4][3]=SymPrefix+Currencies[2]+Currencies[5]+SymSuffix;
      //---(1/3/5)
      SymbolPair[5][1]=SymPrefix+Currencies[1]+Currencies[3]+SymSuffix;
      SymbolPair[5][2]=SymPrefix+Currencies[1]+Currencies[5]+SymSuffix;
      SymbolPair[5][3]=SymPrefix+Currencies[3]+Currencies[5]+SymSuffix;
      //---(1/4/5)
      SymbolPair[6][1]=SymPrefix+Currencies[1]+Currencies[4]+SymSuffix;
      SymbolPair[6][2]=SymPrefix+Currencies[1]+Currencies[5]+SymSuffix;
      SymbolPair[6][3]=SymPrefix+Currencies[4]+Currencies[5]+SymSuffix;
      //---(2/3/5)
      SymbolPair[7][1]=SymPrefix+Currencies[2]+Currencies[3]+SymSuffix;
      SymbolPair[7][2]=SymPrefix+Currencies[2]+Currencies[5]+SymSuffix;
      SymbolPair[7][3]=SymPrefix+Currencies[3]+Currencies[5]+SymSuffix;
      //---(2/4/5)
      SymbolPair[8][1]=SymPrefix+Currencies[2]+Currencies[4]+SymSuffix;
      SymbolPair[8][2]=SymPrefix+Currencies[2]+Currencies[5]+SymSuffix;
      SymbolPair[8][3]=SymPrefix+Currencies[4]+Currencies[5]+SymSuffix;
      //---(3/4/5)
      SymbolPair[9][1]=SymPrefix+Currencies[3]+Currencies[4]+SymSuffix;
      SymbolPair[9][2]=SymPrefix+Currencies[3]+Currencies[5]+SymSuffix;
      SymbolPair[9][3]=SymPrefix+Currencies[4]+Currencies[5]+SymSuffix;
     }
//---Set groups of 6 currencies
   if(NumberCurrenciesTrade>=6)
     {
      //---(1/2/6)
      SymbolPair[10][1]=SymPrefix+Currencies[1]+Currencies[2]+SymSuffix;
      SymbolPair[10][2]=SymPrefix+Currencies[1]+Currencies[6]+SymSuffix;
      SymbolPair[10][3]=SymPrefix+Currencies[2]+Currencies[6]+SymSuffix;
      //---(1/3/6)
      SymbolPair[11][1]=SymPrefix+Currencies[1]+Currencies[3]+SymSuffix;
      SymbolPair[11][2]=SymPrefix+Currencies[1]+Currencies[6]+SymSuffix;
      SymbolPair[11][3]=SymPrefix+Currencies[3]+Currencies[6]+SymSuffix;
      //---(1/4/6)
      SymbolPair[12][1]=SymPrefix+Currencies[1]+Currencies[4]+SymSuffix;
      SymbolPair[12][2]=SymPrefix+Currencies[1]+Currencies[6]+SymSuffix;
      SymbolPair[12][3]=SymPrefix+Currencies[4]+Currencies[6]+SymSuffix;
      //---(1/5/6)
      SymbolPair[13][1]=SymPrefix+Currencies[1]+Currencies[5]+SymSuffix;
      SymbolPair[13][2]=SymPrefix+Currencies[1]+Currencies[6]+SymSuffix;
      SymbolPair[13][3]=SymPrefix+Currencies[5]+Currencies[6]+SymSuffix;
      //---(2/3/6)
      SymbolPair[14][1]=SymPrefix+Currencies[2]+Currencies[3]+SymSuffix;
      SymbolPair[14][2]=SymPrefix+Currencies[2]+Currencies[6]+SymSuffix;
      SymbolPair[14][3]=SymPrefix+Currencies[3]+Currencies[6]+SymSuffix;
      //---(2/4/6)
      SymbolPair[15][1]=SymPrefix+Currencies[2]+Currencies[4]+SymSuffix;
      SymbolPair[15][2]=SymPrefix+Currencies[2]+Currencies[6]+SymSuffix;
      SymbolPair[15][3]=SymPrefix+Currencies[4]+Currencies[6]+SymSuffix;
      //---(2/5/6)
      SymbolPair[16][1]=SymPrefix+Currencies[2]+Currencies[5]+SymSuffix;
      SymbolPair[16][2]=SymPrefix+Currencies[2]+Currencies[6]+SymSuffix;
      SymbolPair[16][3]=SymPrefix+Currencies[5]+Currencies[6]+SymSuffix;
      //---(3/4/6)
      SymbolPair[17][1]=SymPrefix+Currencies[3]+Currencies[4]+SymSuffix;
      SymbolPair[17][2]=SymPrefix+Currencies[3]+Currencies[6]+SymSuffix;
      SymbolPair[17][3]=SymPrefix+Currencies[4]+Currencies[6]+SymSuffix;
      //---(3/5/6)
      SymbolPair[18][1]=SymPrefix+Currencies[3]+Currencies[5]+SymSuffix;
      SymbolPair[18][2]=SymPrefix+Currencies[3]+Currencies[6]+SymSuffix;
      SymbolPair[18][3]=SymPrefix+Currencies[5]+Currencies[6]+SymSuffix;
      //---(4/5/6)
      SymbolPair[19][1]=SymPrefix+Currencies[4]+Currencies[5]+SymSuffix;
      SymbolPair[19][2]=SymPrefix+Currencies[4]+Currencies[6]+SymSuffix;
      SymbolPair[19][3]=SymPrefix+Currencies[5]+Currencies[6]+SymSuffix;
     }
//---Set groups of 7 currencies
   if(NumberCurrenciesTrade>=7)
     {
      //---(1/2/7)
      SymbolPair[20][1]=SymPrefix+Currencies[1]+Currencies[2]+SymSuffix;
      SymbolPair[20][2]=SymPrefix+Currencies[1]+Currencies[7]+SymSuffix;
      SymbolPair[20][3]=SymPrefix+Currencies[2]+Currencies[7]+SymSuffix;
      //---(1/3/7)
      SymbolPair[21][1]=SymPrefix+Currencies[1]+Currencies[3]+SymSuffix;
      SymbolPair[21][2]=SymPrefix+Currencies[1]+Currencies[7]+SymSuffix;
      SymbolPair[21][3]=SymPrefix+Currencies[3]+Currencies[7]+SymSuffix;
      //---(1/4/7)
      SymbolPair[22][1]=SymPrefix+Currencies[1]+Currencies[4]+SymSuffix;
      SymbolPair[22][2]=SymPrefix+Currencies[1]+Currencies[7]+SymSuffix;
      SymbolPair[22][3]=SymPrefix+Currencies[4]+Currencies[7]+SymSuffix;
      //---(1/5/7)
      SymbolPair[23][1]=SymPrefix+Currencies[1]+Currencies[5]+SymSuffix;
      SymbolPair[23][2]=SymPrefix+Currencies[1]+Currencies[7]+SymSuffix;
      SymbolPair[23][3]=SymPrefix+Currencies[5]+Currencies[7]+SymSuffix;
      //---(1/6/7)
      SymbolPair[24][1]=SymPrefix+Currencies[1]+Currencies[6]+SymSuffix;
      SymbolPair[24][2]=SymPrefix+Currencies[1]+Currencies[7]+SymSuffix;
      SymbolPair[24][3]=SymPrefix+Currencies[6]+Currencies[7]+SymSuffix;
      //---(2/3/7)
      SymbolPair[25][1]=SymPrefix+Currencies[2]+Currencies[3]+SymSuffix;
      SymbolPair[25][2]=SymPrefix+Currencies[2]+Currencies[7]+SymSuffix;
      SymbolPair[25][3]=SymPrefix+Currencies[3]+Currencies[7]+SymSuffix;
      //---(2/4/7)
      SymbolPair[26][1]=SymPrefix+Currencies[2]+Currencies[4]+SymSuffix;
      SymbolPair[26][2]=SymPrefix+Currencies[2]+Currencies[7]+SymSuffix;
      SymbolPair[26][3]=SymPrefix+Currencies[4]+Currencies[7]+SymSuffix;
      //---(2/5/7)
      SymbolPair[27][1]=SymPrefix+Currencies[2]+Currencies[5]+SymSuffix;
      SymbolPair[27][2]=SymPrefix+Currencies[2]+Currencies[7]+SymSuffix;
      SymbolPair[27][3]=SymPrefix+Currencies[5]+Currencies[7]+SymSuffix;
      //---(2/6/7)
      SymbolPair[28][1]=SymPrefix+Currencies[2]+Currencies[6]+SymSuffix;
      SymbolPair[28][2]=SymPrefix+Currencies[2]+Currencies[7]+SymSuffix;
      SymbolPair[28][3]=SymPrefix+Currencies[6]+Currencies[7]+SymSuffix;
      //---(3/4/7)
      SymbolPair[29][1]=SymPrefix+Currencies[3]+Currencies[4]+SymSuffix;
      SymbolPair[29][2]=SymPrefix+Currencies[3]+Currencies[7]+SymSuffix;
      SymbolPair[29][3]=SymPrefix+Currencies[4]+Currencies[7]+SymSuffix;
      //---(3/5/7)
      SymbolPair[30][1]=SymPrefix+Currencies[3]+Currencies[5]+SymSuffix;
      SymbolPair[30][2]=SymPrefix+Currencies[3]+Currencies[7]+SymSuffix;
      SymbolPair[30][3]=SymPrefix+Currencies[5]+Currencies[7]+SymSuffix;
      //---(3/6/7)
      SymbolPair[31][1]=SymPrefix+Currencies[3]+Currencies[6]+SymSuffix;
      SymbolPair[31][2]=SymPrefix+Currencies[3]+Currencies[7]+SymSuffix;
      SymbolPair[31][3]=SymPrefix+Currencies[6]+Currencies[7]+SymSuffix;
      //---(4/5/7)
      SymbolPair[32][1]=SymPrefix+Currencies[4]+Currencies[5]+SymSuffix;
      SymbolPair[32][2]=SymPrefix+Currencies[4]+Currencies[7]+SymSuffix;
      SymbolPair[32][3]=SymPrefix+Currencies[5]+Currencies[7]+SymSuffix;
      //---(4/6/7)
      SymbolPair[33][1]=SymPrefix+Currencies[4]+Currencies[6]+SymSuffix;
      SymbolPair[33][2]=SymPrefix+Currencies[4]+Currencies[7]+SymSuffix;
      SymbolPair[33][3]=SymPrefix+Currencies[6]+Currencies[7]+SymSuffix;
      //---(5/6/7)
      SymbolPair[34][1]=SymPrefix+Currencies[5]+Currencies[6]+SymSuffix;
      SymbolPair[34][2]=SymPrefix+Currencies[5]+Currencies[7]+SymSuffix;
      SymbolPair[34][3]=SymPrefix+Currencies[6]+Currencies[7]+SymSuffix;
     }
//---Set groups of 8 currencies
   if(NumberCurrenciesTrade>=8)
     {
      //---(1/2/8)
      SymbolPair[35][1]=SymPrefix+Currencies[1]+Currencies[2]+SymSuffix;
      SymbolPair[35][2]=SymPrefix+Currencies[1]+Currencies[8]+SymSuffix;
      SymbolPair[35][3]=SymPrefix+Currencies[2]+Currencies[8]+SymSuffix;
      //---(1/3/8)
      SymbolPair[36][1]=SymPrefix+Currencies[1]+Currencies[3]+SymSuffix;
      SymbolPair[36][2]=SymPrefix+Currencies[1]+Currencies[8]+SymSuffix;
      SymbolPair[36][3]=SymPrefix+Currencies[3]+Currencies[8]+SymSuffix;
      //---(1/4/8)
      SymbolPair[37][1]=SymPrefix+Currencies[1]+Currencies[4]+SymSuffix;
      SymbolPair[37][2]=SymPrefix+Currencies[1]+Currencies[8]+SymSuffix;
      SymbolPair[37][3]=SymPrefix+Currencies[4]+Currencies[8]+SymSuffix;
      //---(1/5/8)
      SymbolPair[38][1]=SymPrefix+Currencies[1]+Currencies[5]+SymSuffix;
      SymbolPair[38][2]=SymPrefix+Currencies[1]+Currencies[8]+SymSuffix;
      SymbolPair[38][3]=SymPrefix+Currencies[5]+Currencies[8]+SymSuffix;
      //---(1/6/8)
      SymbolPair[39][1]=SymPrefix+Currencies[1]+Currencies[6]+SymSuffix;
      SymbolPair[39][2]=SymPrefix+Currencies[1]+Currencies[8]+SymSuffix;
      SymbolPair[39][3]=SymPrefix+Currencies[6]+Currencies[8]+SymSuffix;
      //---(1/7/8)
      SymbolPair[40][1]=SymPrefix+Currencies[1]+Currencies[7]+SymSuffix;
      SymbolPair[40][2]=SymPrefix+Currencies[1]+Currencies[8]+SymSuffix;
      SymbolPair[40][3]=SymPrefix+Currencies[7]+Currencies[8]+SymSuffix;
      //---(2/3/8)
      SymbolPair[41][1]=SymPrefix+Currencies[2]+Currencies[3]+SymSuffix;
      SymbolPair[41][2]=SymPrefix+Currencies[2]+Currencies[8]+SymSuffix;
      SymbolPair[41][3]=SymPrefix+Currencies[3]+Currencies[8]+SymSuffix;
      //---(2/4/8)
      SymbolPair[42][1]=SymPrefix+Currencies[2]+Currencies[4]+SymSuffix;
      SymbolPair[42][2]=SymPrefix+Currencies[2]+Currencies[8]+SymSuffix;
      SymbolPair[42][3]=SymPrefix+Currencies[4]+Currencies[8]+SymSuffix;
      //---(2/5/8)
      SymbolPair[43][1]=SymPrefix+Currencies[2]+Currencies[5]+SymSuffix;
      SymbolPair[43][2]=SymPrefix+Currencies[2]+Currencies[8]+SymSuffix;
      SymbolPair[43][3]=SymPrefix+Currencies[5]+Currencies[8]+SymSuffix;
      //---(2/6/8)
      SymbolPair[44][1]=SymPrefix+Currencies[2]+Currencies[6]+SymSuffix;
      SymbolPair[44][2]=SymPrefix+Currencies[2]+Currencies[8]+SymSuffix;
      SymbolPair[44][3]=SymPrefix+Currencies[6]+Currencies[8]+SymSuffix;
      //---(2/7/8)
      SymbolPair[45][1]=SymPrefix+Currencies[2]+Currencies[7]+SymSuffix;
      SymbolPair[45][2]=SymPrefix+Currencies[2]+Currencies[8]+SymSuffix;
      SymbolPair[45][3]=SymPrefix+Currencies[7]+Currencies[8]+SymSuffix;
      //---(3/4/8)
      SymbolPair[46][1]=SymPrefix+Currencies[3]+Currencies[4]+SymSuffix;
      SymbolPair[46][2]=SymPrefix+Currencies[3]+Currencies[8]+SymSuffix;
      SymbolPair[46][3]=SymPrefix+Currencies[4]+Currencies[8]+SymSuffix;
      //---(3/5/8)
      SymbolPair[47][1]=SymPrefix+Currencies[3]+Currencies[5]+SymSuffix;
      SymbolPair[47][2]=SymPrefix+Currencies[3]+Currencies[8]+SymSuffix;
      SymbolPair[47][3]=SymPrefix+Currencies[5]+Currencies[8]+SymSuffix;
      //---(3/6/8)
      SymbolPair[48][1]=SymPrefix+Currencies[3]+Currencies[6]+SymSuffix;
      SymbolPair[48][2]=SymPrefix+Currencies[3]+Currencies[8]+SymSuffix;
      SymbolPair[48][3]=SymPrefix+Currencies[6]+Currencies[8]+SymSuffix;
      //---(3/7/8)
      SymbolPair[49][1]=SymPrefix+Currencies[3]+Currencies[7]+SymSuffix;
      SymbolPair[49][2]=SymPrefix+Currencies[3]+Currencies[8]+SymSuffix;
      SymbolPair[49][3]=SymPrefix+Currencies[7]+Currencies[8]+SymSuffix;
      //---(4/5/8)
      SymbolPair[50][1]=SymPrefix+Currencies[4]+Currencies[5]+SymSuffix;
      SymbolPair[50][2]=SymPrefix+Currencies[4]+Currencies[8]+SymSuffix;
      SymbolPair[50][3]=SymPrefix+Currencies[5]+Currencies[8]+SymSuffix;
      //---(4/6/8)
      SymbolPair[51][1]=SymPrefix+Currencies[4]+Currencies[6]+SymSuffix;
      SymbolPair[51][2]=SymPrefix+Currencies[4]+Currencies[8]+SymSuffix;
      SymbolPair[51][3]=SymPrefix+Currencies[6]+Currencies[8]+SymSuffix;
      //---(4/7/8)
      SymbolPair[52][1]=SymPrefix+Currencies[4]+Currencies[7]+SymSuffix;
      SymbolPair[52][2]=SymPrefix+Currencies[4]+Currencies[8]+SymSuffix;
      SymbolPair[52][3]=SymPrefix+Currencies[7]+Currencies[8]+SymSuffix;
      //---(5/6/8)
      SymbolPair[53][1]=SymPrefix+Currencies[5]+Currencies[6]+SymSuffix;
      SymbolPair[53][2]=SymPrefix+Currencies[5]+Currencies[8]+SymSuffix;
      SymbolPair[53][3]=SymPrefix+Currencies[6]+Currencies[8]+SymSuffix;
      //---(5/7/8)
      SymbolPair[54][1]=SymPrefix+Currencies[5]+Currencies[7]+SymSuffix;
      SymbolPair[54][2]=SymPrefix+Currencies[5]+Currencies[8]+SymSuffix;
      SymbolPair[54][3]=SymPrefix+Currencies[7]+Currencies[8]+SymSuffix;
      //---(6/7/8)
      SymbolPair[55][1]=SymPrefix+Currencies[6]+Currencies[7]+SymSuffix;
      SymbolPair[55][2]=SymPrefix+Currencies[6]+Currencies[8]+SymSuffix;
      SymbolPair[55][3]=SymPrefix+Currencies[7]+Currencies[8]+SymSuffix;
     }
//---------------------------------------------------------------------
//Auto set groups uses
   GroupsUses=0;
//---
   for(i=0; i<NumberGroupsTrade; i++)
     {
      //---Add symbols in data window
      SymbolSelect(SymbolPair[i][1],true);
      SymbolSelect(SymbolPair[i][2],true);
      SymbolSelect(SymbolPair[i][3],true);
      //---Get prices of symbols
      BidPricePair[i][1]=SymbolInfoDouble(SymbolPair[i][1],SYMBOL_BID);
      BidPricePair[i][2]=SymbolInfoDouble(SymbolPair[i][2],SYMBOL_BID);
      BidPricePair[i][3]=SymbolInfoDouble(SymbolPair[i][3],SYMBOL_BID);
      //---
      if((BidPricePair[i][1]==0)||(BidPricePair[i][2]==0)||(BidPricePair[i][3]==0))
        {
         SymbolPair[GroupsUses][1]=SymbolPair[i+1][1];
         SymbolPair[GroupsUses][2]=SymbolPair[i+1][2];
         SymbolPair[GroupsUses][3]=SymbolPair[i+1][3];
         if((BidPricePair[i+1][1]>0)&&(BidPricePair[i+1][2]>0)&&(BidPricePair[i+1][3]>0))
            GroupsUses++;
        }
      else
         if((BidPricePair[i][1]>0)&&(BidPricePair[i][2]>0)&&(BidPricePair[i][3]>0))
           {
            SymbolPair[GroupsUses][1]=SymbolPair[i][1];
            SymbolPair[GroupsUses][2]=SymbolPair[i][2];
            SymbolPair[GroupsUses][3]=SymbolPair[i][3];
            GroupsUses++;
           }
     }
//---
   NumberGroupsTrade=GroupsUses;
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
               NumberGroupsSkip[i]=(int)StringToInteger(StringSubstr(NoOfGroupToSkip,0,FindComma[i]));
            if((i>0)&&(i<CountComma))
               NumberGroupsSkip[i]=(int)StringToInteger(StringSubstr(NoOfGroupToSkip,FindComma[i-1]+1,(FindComma[i]-FindComma[i-1])-1));
            if(i==CountComma)
               NumberGroupsSkip[i]=(int)StringToInteger(StringSubstr(NoOfGroupToSkip,FindComma[i-1]+1,0));
           }
        }
      //---
      if(StringLen(NoOfGroupToSkip)<=2)
        {
         PositionSkipped=0;
         DecimalsGet=StringLen(NoOfGroupToSkip);
         NumberGroupsSkip[0]=(int)StringToInteger(StringSubstr(NoOfGroupToSkip,PositionSkipped,DecimalsGet));
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
               if(cnt10>0)
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
      Print(" # "+MQLInfoString(MQL_PROGRAM_NAME)+" # "+"Check group No "+IntegerToString(i+1)+"...("+SymbolPair[i][1]+"/"+SymbolPair[i][2]+"/"+SymbolPair[i][3]+")");
      //---Get prices of symbols
      BidPricePair[i][1]=SymbolInfoDouble(SymbolPair[i][1],SYMBOL_BID);
      BidPricePair[i][2]=SymbolInfoDouble(SymbolPair[i][2],SYMBOL_BID);
      BidPricePair[i][3]=SymbolInfoDouble(SymbolPair[i][3],SYMBOL_BID);
      //---
      if((BidPricePair[i][1]>0)&&(BidPricePair[i][2]>0)&&(BidPricePair[i][3]>0))
        {
         Print(" # "+MQLInfoString(MQL_PROGRAM_NAME)+" # "+SymbolPair[i][1]+"/"+SymbolPair[i][2]+"/"+SymbolPair[i][3]+" are ok");
         if(SkipGroup[i]==true)
           {
            SkippedStatus[i]="Group Skipped by user settings from external parameters";
            Print(" # ",MQLInfoString(MQL_PROGRAM_NAME)," # Skip group No ",IntegerToString(i+1)," #");
           }
        }
      else
         Print(" # "+MQLInfoString(MQL_PROGRAM_NAME)+" # "+SymbolPair[i][1]+"/"+SymbolPair[i][2]+"/"+SymbolPair[i][3]+" not found");
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
        }
     }
//---------------------------------------------------------------------
//Check sets and value
   CheckValue();
   if(WrongSet==true)
      return(INIT_FAILED);
   if(WrongPairs==true)
      return(INIT_FAILED);
//---------------------------------------------------------------------
//Get minimum and maximum lot size
   for(i=0; i<NumberGroupsTrade; i++)
     {
      for(x=0; x<PairsPerGroup; x++)
        {
         if(MaxLot>SymbolInfoDouble(SymbolPair[i][x],SYMBOL_VOLUME_MAX))
            MaxLot=SymbolInfoDouble(SymbolPair[i][x],SYMBOL_VOLUME_MAX);
         if(MinLot<SymbolInfoDouble(SymbolPair[i][x],SYMBOL_VOLUME_MIN))
            MinLot=SymbolInfoDouble(SymbolPair[i][x],SYMBOL_VOLUME_MIN);
        }
     }
//---------------------------------------------------------------------
//Currency and groups infirmations
   Print("### ",MQLInfoString(MQL_PROGRAM_NAME)," || Number Of currencies use: ",NumberCurrenciesTrade," || Number of groups trade: ",NumberGroupsTrade," ###");
//---------------------------------------------------------------------
//ID orders
   if(MagicNumber==0)
     {
      MagicNo=0;
      for(i=0; i<StringLen(CurrenciesTrade); i++)
         MagicNo+=(StringGetCharacter(CurrenciesTrade,i)*(i+1));
      MagicNo+=MagicSet+(int)AccountInfoInteger(ACCOUNT_LOGIN);
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
      SuitePlus[i][1]=ORDER_TYPE_BUY;
      SuitePlus[i][2]=ORDER_TYPE_SELL;
      SuitePlus[i][3]=ORDER_TYPE_BUY;
      //---
      SuiteMinus[i][1]=ORDER_TYPE_SELL;
      SuiteMinus[i][2]=ORDER_TYPE_BUY;
      SuiteMinus[i][3]=ORDER_TYPE_SELL;
     }
//---------------------------------------------------------------------
//Set maximum orders
   if((MaximumOrders==0)&&(AccountInfoInteger(ACCOUNT_LIMIT_ORDERS)==0))
      AcceptMaxOrders=0;
//---
   if((MaximumOrders>0)&&(AccountInfoInteger(ACCOUNT_LIMIT_ORDERS)==0))
      AcceptMaxOrders=MaximumOrders;
//---
   if((MaximumOrders==0)&&(AccountInfoInteger(ACCOUNT_LIMIT_ORDERS)>0))
      AcceptMaxOrders=(int)AccountInfoInteger(ACCOUNT_LIMIT_ORDERS);
//---
   if((MaximumOrders>0)&&(AccountInfoInteger(ACCOUNT_LIMIT_ORDERS)>0))
      AcceptMaxOrders=MathMin((int)AccountInfoInteger(ACCOUNT_LIMIT_ORDERS),MaximumOrders);
//---------------------------------------------------------------------
//Set operation type
   OperationsMode=TypeOfOperation;
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
//Destroy timer
   EventKillTimer();
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
  }
//===============================================================================================================================================================================================================================================================//
//OnTick function
//===============================================================================================================================================================================================================================================================//
void OnTick()
  {
//---------------------------------------------------------------------
//Reset value
   CallMain=false;
//---------------------------------------------------------------------
//Call main in tester
   if((MQLInfoInteger(MQL_TESTER))||(MQLInfoInteger(MQL_VISUAL_MODE))||(!MQLInfoInteger(MQL_OPTIMIZATION)))
     {
      MainFunction();
      return;
     }
//---------------------------------------------------------------------
//Warning message
   if(!MQLInfoInteger(MQL_TESTER))
     {
      if(!MQLInfoInteger(MQL_SIGNALS_ALLOWED))
        {
         Comment("\n      The trading terminal",
                 "\n      of experts do not run",
                 "\n\n\n      Turn ON EA Please .......");
         return;
        }
      else
         if(!MQLInfoInteger(MQL_TRADE_ALLOWED))
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
   TimeToTrade=true;
   MarketIsOpen=true;
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
   ArrayInitialize(SpreadValuePlus,0);
   ArrayInitialize(SpreadValueMinus,0);
   ArrayInitialize(FirsOrdersPlusOK,false);
   ArrayInitialize(FirsOrdersMinusOK,false);
//---------------------------------------------------------------------
//Stop in locked version or wrong sets or missing bars
   if(StopWorking==true)
      return;
   if(WrongSet==true)
      return;
   if(WrongPairs==true)
      return;
//---------------------------------------------------------------------
//Check limit of orders
   if(AcceptMaxOrders!=0)
     {
      if(PositionsTotal()+(PairsPerGroup)>AcceptMaxOrders)
        {
         LimitOfOrdersOk=false;
         CommentWarning=true;
         WarningPrint="Expert reached the limit of opened orders!!!";
        }
     }
//---------------------------------------------------------------------
//Control market session
   if(ControlSession==true)
     {
      MqlDateTime DateTime;
      TimeCurrent(DateTime);
      //---Wait on Monday
      if((DateTime.day_of_week==1)&&(SymbolInfoSessionTrade(Symbol(),MONDAY,0,TimeBegin,TimeEnd)==true))
        {
         if(TimeToString(TimeCurrent(),TIME_MINUTES)<=TimeToString(TimeBegin+(WaitAfterOpen*60),TIME_MINUTES))
           {
            TimeToTrade=false;
            CommentWarning=true;
            WarningPrint="Wait "+IntegerToString(WaitAfterOpen)+" minutes after Monday open market!!!";
           }
        }
      //---Stop on Friday
      if((DateTime.day_of_week==5)&&(SymbolInfoSessionTrade(Symbol(),FRIDAY,0,TimeBegin,TimeEnd)==true))
        {
         if(TimeToString(TimeCurrent(),TIME_MINUTES)>=TimeToString(TimeEnd-(StopBeforeClose*60),TIME_MINUTES))
           {
            TimeToTrade=false;
            CommentWarning=true;
            WarningPrint="Wait "+IntegerToString(StopBeforeClose)+" minutes before Friday close market!!!";
           }
        }
     }
//---------------------------------------------------------------------
//Start multipair function
   for(int cnt=0; cnt<NumberGroupsTrade; cnt++)
     {
      SpreadPair[cnt][1]=0;
      SpreadPair[cnt][2]=0;
      SpreadPair[cnt][3]=0;
      SumSpreadGroup[cnt]=0;
      //---------------------------------------------------------------------
      //Skip groups
      if(SkipGroup[cnt]==true)
         continue;
      //---------------------------------------------------------------------
      //Get orders' informations
      CountCurrentOrders(cnt);
      //---------------------------------------------------------------------
      //Set date time
      MqlDateTime CurrentDateTime;
      TimeCurrent(CurrentDateTime);
      //---------------------------------------------------------------------
      //Check if market is open
      if((CurrentDateTime.hour==23)&&(CurrentDateTime.min>55))
         MarketIsOpen=false;
      if((CurrentDateTime.hour==00)&&(CurrentDateTime.min<5))
         MarketIsOpen=false;
      //---------------------------------------------------------------------
      //Set warning message on closed market
      if(MarketIsOpen==false)
        {
         CommentWarning=true;
         WarningPrint="Market is closed!!!";
        }
      //---------------------------------------------------------------------
      //Get spreads and tick value
      for(i=1; i<=PairsPerGroup; i++)
        {
         SpreadPair[cnt][i]=NormalizeDouble((double)SymbolInfoInteger(SymbolPair[cnt][i],SYMBOL_SPREAD)/MultiplierPoint,2);
         SumSpreadGroup[cnt]+=NormalizeDouble(SpreadPair[cnt][i],2);
         //---
         TickValuePair[cnt][i]=SymbolInfoDouble(SymbolPair[cnt][i],SYMBOL_TRADE_TICK_VALUE);
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
            if(OperationsMode!=0)
               WarningPrint="Spread it isn't normal ("+DoubleToString(SumSpreadGroup[cnt],2)+"/"+DoubleToString(MaxSpread,2)+")";
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
      //---------------------------------------------------------------------
      //Count history orders
      if((!MQLInfoInteger(MQL_TESTER))||(!MQLInfoInteger(MQL_OPTIMIZATION)))
        {
         if(CntTick<NumberGroupsTrade+4)
           {
            CntTick++;
            if(CntTick<NumberGroupsTrade+3)
               CountHistory=true;
           }
         //---
         if(CountHistory==true)
           {
            CountHistory=false;
            CountHistoryOrders();
           }
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
               LevelProfitClosePlus[cnt]+=(TotalLotPlus[cnt][i]*TargetCloseProfit*TickValuePair[cnt][i]);
            if(TotalOrdersMinus[cnt][i]>0)
               LevelProfitCloseMinus[cnt]+=(TotalLotMinus[cnt][i]*TargetCloseProfit*TickValuePair[cnt][i]);
           }
         if(UseFairLotSize==true)
           {
            if(TotalOrdersPlus[cnt][i]>0)
               LevelProfitClosePlus[cnt]+=(TotalLotPlus[cnt][i]*TargetCloseProfit);
            if(TotalOrdersMinus[cnt][i]>0)
               LevelProfitCloseMinus[cnt]+=(TotalLotMinus[cnt][i]*TargetCloseProfit);
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
      //Confirm levels if spread is too high
      if((LevelProfitClosePlus[cnt]<(FirstLotPlus[cnt][1]*TargetCloseProfit*TickValuePair[cnt][1])+(FirstLotPlus[cnt][2]*TargetCloseProfit*TickValuePair[cnt][2])+(FirstLotPlus[cnt][3]*TargetCloseProfit*TickValuePair[cnt][3])))
         LevelProfitClosePlus[cnt]=(FirstLotPlus[cnt][1]*TargetCloseProfit*TickValuePair[cnt][1])+(FirstLotPlus[cnt][2]*TargetCloseProfit*TickValuePair[cnt][2])+(FirstLotPlus[cnt][3]*TargetCloseProfit*TickValuePair[cnt][3]);
      //---
      if((LevelProfitCloseMinus[cnt]<(FirstLotMinus[cnt][1]*TargetCloseProfit*TickValuePair[cnt][1])+(FirstLotMinus[cnt][2]*TargetCloseProfit*TickValuePair[cnt][2])+(FirstLotMinus[cnt][3]*TargetCloseProfit*TickValuePair[cnt][3])))
         LevelProfitCloseMinus[cnt]=(FirstLotMinus[cnt][1]*TargetCloseProfit*TickValuePair[cnt][1])+(FirstLotMinus[cnt][2]*TargetCloseProfit*TickValuePair[cnt][2])+(FirstLotMinus[cnt][3]*TargetCloseProfit*TickValuePair[cnt][3]);
      //---------------------------------------------------------------------
      //Send group
      if((MarketIsOpen==true)&&(TimeToTrade==true)&&(SpreadOK[cnt]==true)&&(ExpertClosePlusInLoss[cnt]==false)&&(ExpertCloseMinusInLoss[cnt]==false)&&
         (ExpertClosePlusInProfit[cnt]==false)&&(ExpertCloseMinusInProfit[cnt]==false)&&(ExpertCloseBasketInProfit[cnt]==false)&&(ExpertCloseBasketInLoss[cnt]==false))
        {
         //---------------------------------------------------------------------
         //Send oposite group if close a group in profit
         if(SideOpenOrders==2)
           {
            if((OpenOrdersInLoss==2)&&(OperationsMode==1)&&(OrdersIsOK[cnt]==true))
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
                     WarningPrint="Total minus groups have reached the limit.";
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
                     WarningPrint="Total plus groups have reached the limit.";
                    }
                 }
              }
           }
         //---------------------------------------------------------------------
         //Send first group
         if((LimitOfOrdersOk==true)&&(OperationsMode!=0)&&(OperationsMode!=3))
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
                     if(OperationsMode==1)
                       {
                        OpenPairPlus(cnt,i);
                        FirsOrdersPlusOK[cnt]=false;
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
                     if(OperationsMode==1)
                       {
                        OpenPairMinus(cnt,i);
                        FirsOrdersMinusOK[cnt]=false;
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
                        if((SumOrdersPlus[cnt]>=PairsPerGroup)&&(SumProfitPlus[cnt]<=LevelOpenNextPlus[cnt])&&((TypeCloseInProfit==0)||(SumOrdersPlus[cnt]==SumOrdersMinus[cnt])||(SumOrdersPlus[cnt]>SumOrdersMinus[cnt])))
                          {
                           for(i=1; i<=PairsPerGroup; i++)
                              OpenPairPlus(cnt,i);
                           continue;
                          }
                       }
                     else
                       {
                        WarningPrint="Total plus groups have reached the limit.";
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
                        if((SumOrdersMinus[cnt]>=PairsPerGroup)&&(SumProfitMinus[cnt]<=LevelOpenNextMinus[cnt])&&((TypeCloseInProfit==0)||(SumOrdersPlus[cnt]==SumOrdersMinus[cnt])||(SumOrdersPlus[cnt]<SumOrdersMinus[cnt])))
                          {
                           for(i=1; i<=PairsPerGroup; i++)
                              OpenPairMinus(cnt,i);
                           continue;
                          }
                       }
                     else
                       {
                        WarningPrint="Total minus groups have reached the limit.";
                       }
                    }
                 }
              }
           }
        }
      //---------------------------------------------------------------------
      //Close orders
      if((MarketIsOpen==true)&&(TimeToTrade==true)&&(OperationsMode!=0)&&(OperationsMode!=3))
        {
         //---------------------------------------------------------------------
         //Close orders in loss
         if((TypeCloseInLoss<2)&&(ExpertCloseBasketInProfit[cnt]==false)&&(ExpertClosePlusInProfit[cnt]==false)&&(ExpertCloseMinusInProfit[cnt]==false))
           {
            //---Close whole ticket
            if((TypeCloseInLoss==0)&&(SumOrdersPlus[cnt]+SumOrdersMinus[cnt]>0))
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
            //---
           }
         //---------------------------------------------------------------------
         //Close orders in profit
         if((OperationsMode!=0)&&(ExpertCloseBasketInLoss[cnt]==false)&&(ExpertClosePlusInLoss[cnt]==false)&&(ExpertCloseMinusInLoss[cnt]==false))
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
            //---
           }//end if(OperationsMode!=0)
        }//end if(MarketIsOpen==true)
      //---------------------------------------------------------------------
      //Close and stop
      if(OperationsMode==3)
        {
         //---There are not open orders
         if(SumOrdersPlus[cnt]+SumOrdersMinus[cnt]==0)
           {
            if(cnt==NumberGroupsTrade-1)
              {
               Comment(
                  "\n                  ",MQLInfoString(MQL_PROGRAM_NAME)+
                  "\n\n             ~ Have Close All Orders ~ "+
                  "\n\n             ~ History Orders Results ~ "+
                  "\n\n             ~ Orders: "+DoubleToString(HistoryTotalTrades,0)+" || PnL: "+DoubleToString(HistoryTotalProfitLoss,2)+" ~ "
               );
              }
           }
         //---There are open orders
         if(SumOrdersPlus[cnt]+SumOrdersMinus[cnt]>0)
           {
            Comment("\n                  ",MQLInfoString(MQL_PROGRAM_NAME),
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
         (ExpertCloseBasketInProfit[cnt]==false)&&(ExpertCloseBasketInLoss[cnt]==false)&&(FirsOrdersPlusOK[cnt]==true)&&(FirsOrdersMinusOK[cnt]==true))
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
                  WarningPrint="Orders are in limit ("+IntegerToString(PositionsTotal())+"/"+IntegerToString(AcceptMaxOrders)+")";
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
   if(OperationsMode<3)
      CommentChart();
//---------------------------------------------------------------------
  }
//===============================================================================================================================================================================================================================================================//
//Normalize Lots
//===============================================================================================================================================================================================================================================================//
double NormalizeLot(double LotsSize,int GroupCheck, int PairCheck)
  {
//---------------------------------------------------------------------
   if(TerminalInfoInteger(TERMINAL_CONNECTED))
     {
      return(MathMin(MathMax((MathRound(LotsSize/SymbolInfoDouble(SymbolPair[GroupCheck][PairCheck],SYMBOL_VOLUME_STEP))*SymbolInfoDouble(SymbolPair[GroupCheck][PairCheck],SYMBOL_VOLUME_STEP)),MinLot),MaxLot));
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
void OpenPairPlus(int GroupOpen, int PairOpen)
  {
//---------------------------------------------------------------------
   double PriceOpen=0;
   double StopLoss=0;
   double TakeProfit=0;
   double LotSizeOrder=0;
   double LotSize=0;
   double MaximumLot=9999999;
   color ColorOrder=0;
   double MultiplierTickValuePlus[MaxGroups][MaxPairs]= {1};
//---------------------------------------------------------------------
//Set maximu lot size
   if(MaximumLotSize==0.0)
      MaximumLot=SymbolInfoDouble(SymbolPair[GroupOpen][PairOpen],SYMBOL_VOLUME_MAX);
   if(MaximumLotSize!=0.0)
      MaximumLot=MaximumLotSize;
//---------------------------------------------------------------------
//Calculate tick value multiplier
   if(UseFairLotSize==false)
      MultiplierTickValuePlus[GroupOpen][PairOpen]=1.0;
   if(UseFairLotSize==true)
      MultiplierTickValuePlus[GroupOpen][PairOpen]=TickValuePair[GroupOpen][PairOpen];
//---------------------------------------------------------------------
//Calculate lot size per pair
   if(LotOrdersProgress==0)
      MultiplierLotPlus[GroupOpen]=1;
   if(LotOrdersProgress==1)
      MultiplierLotPlus[GroupOpen]=GroupsPlus[GroupOpen]+1;
   if(LotOrdersProgress==2)
      MultiplierLotPlus[GroupOpen]=MathMax(1,MathPow(2,GroupsPlus[GroupOpen]));
   if(LotOrdersProgress==3)
      MultiplierLotPlus[GroupOpen]=1.0/MathMax(1,MathPow(2,GroupsPlus[GroupOpen]));
//---------------------------------------------------------------------
//Set lots for orders
//---Auto or manual lot
   if(AutoLotSize==1)
      LotSize=((AccountInfoDouble(ACCOUNT_BALANCE)*AccountInfoInteger(ACCOUNT_LEVERAGE))/100000000)*RiskFactor;
   if(AutoLotSize==0)
      LotSize=ManualLotSize;
//---
   if(OpenOrdersInLoss==2)
     {
      if((GroupsPlus[GroupOpen]>0)||(GroupsMinus[GroupOpen]>0))
         LotSizeOrder=NormalizeLot(MathMin(MaximumLot,FirstLotPair[GroupOpen][PairOpen]*MultiplierLotPlus[GroupOpen]),GroupOpen,PairOpen);
      else
         LotSizeOrder=NormalizeLot(MathMin(MaximumLot,LotSize/MultiplierTickValuePlus[GroupOpen][PairOpen]),GroupOpen,PairOpen);
     }
   else
     {
      if(GroupsPlus[GroupOpen]>0)
         LotSizeOrder=NormalizeLot(MathMin(MaximumLot,FirstLotPair[GroupOpen][PairOpen]*MultiplierLotPlus[GroupOpen]),GroupOpen,PairOpen);
      else
         LotSizeOrder=NormalizeLot(MathMin(MaximumLot,LotSize/MultiplierTickValuePlus[GroupOpen][PairOpen]),GroupOpen,PairOpen);
     }
//---------------------------------------------------------------------
   if(SuitePlus[GroupOpen][PairOpen]==ORDER_TYPE_BUY)
     {
      PriceOpen=SymbolInfoDouble(SymbolPair[GroupOpen][PairOpen],SYMBOL_ASK);
      ColorOrder=clrBlue;
     }
//---
   if(SuitePlus[GroupOpen][PairOpen]==ORDER_TYPE_SELL)
     {
      PriceOpen=SymbolInfoDouble(SymbolPair[GroupOpen][PairOpen],SYMBOL_BID);
      ColorOrder=clrRed;
     }
//---------------------------------------------------------------------
//Check free margin
   double Margin,FreeMargin=AccountInfoDouble(ACCOUNT_MARGIN_FREE);
   if(!OrderCalcMargin(SuitePlus[GroupOpen][PairOpen],SymbolPair[GroupOpen][PairOpen],LotSizeOrder,PriceOpen,Margin))
     {
      Print("Error in ",__FUNCTION__," code=",GetLastError());
      return;
     }
//---------------------------------------------------------------------
//Declare and initialize the trade request and result of trade request
   MqlTradeRequest request= {};
   MqlTradeResult  result= {};
//---
   request.action=TRADE_ACTION_DEAL;
   request.magic=OrdersID[GroupOpen];
   request.symbol=SymbolPair[GroupOpen][PairOpen];
   request.volume=LotSizeOrder;
   request.price=PriceOpen;
   request.type=SuitePlus[GroupOpen][PairOpen];
   request.comment=CommentsEA;
   request.sl=0;
   request.tp=0;
//---
   if(SymbolInfoInteger(SymbolPair[GroupOpen][PairOpen],SYMBOL_FILLING_MODE)==1)
      request.type_filling=ORDER_FILLING_FOK;
   if(SymbolInfoInteger(SymbolPair[GroupOpen][PairOpen],SYMBOL_FILLING_MODE)==2)
      request.type_filling=ORDER_FILLING_IOC;
//---------------------------------------------------------------------
//Send order
   if(LimitOfOrdersOk==true)
     {
      if(Margin<FreeMargin)
        {
         CntTry=0;
         while(SymbolInfoInteger(SymbolPair[GroupOpen][PairOpen],SYMBOL_TRADE_MODE)==SYMBOL_TRADE_MODE_FULL)
           {
            CntTry++;
            TicketNo[PairOpen]=OrderSend(request,result);
            //---
            if(TicketNo[PairOpen]>0)
              {
               if(PrintLogReport==true)
                  Print("Open Plus: ",SymbolPair[GroupOpen][PairOpen]," || Ticket No: ",TicketNo[PairOpen]);
               break;
              }
            //---
            Sleep(100);
            if(CntTry>3)
               break;
           }
        }
      else
        {
         CommentWarning=true;
         if(WarningPrint=="")
            WarningPrint="  Free margin is low ("+DoubleToString(FreeMargin)+")";
         Print(WarningPrint);
        }
     }
//---------------------------------------------------------------------
  }
//===============================================================================================================================================================================================================================================================//
//Open minus orders
//===============================================================================================================================================================================================================================================================//
void OpenPairMinus(int GroupOpen, int PairOpen)
  {
//---------------------------------------------------------------------
   double PriceOpen=0;
   double StopLoss=0;
   double TakeProfit=0;
   double LotSizeOrder=0;
   double LotSize=0;
   double MaximumLot=9999999;
   color ColorOrder=0;
   double MultiplierTickValueMinus[MaxGroups][MaxPairs]= {1};
//---------------------------------------------------------------------
//Set maximu lot size
   if(MaximumLotSize==0.0)
      MaximumLot=SymbolInfoDouble(SymbolPair[GroupOpen][PairOpen],SYMBOL_VOLUME_MAX);
   if(MaximumLotSize!=0.0)
      MaximumLot=MaximumLotSize;
//---------------------------------------------------------------------
//Calculate tick value multiplier
   if(UseFairLotSize==false)
      MultiplierTickValueMinus[GroupOpen][PairOpen]=1.0;
   if(UseFairLotSize==true)
      MultiplierTickValueMinus[GroupOpen][PairOpen]=TickValuePair[GroupOpen][PairOpen];
//---------------------------------------------------------------------
//Calculate lot size per pair
   if(LotOrdersProgress==0)
      MultiplierLotMinus[GroupOpen]=1;
   if(LotOrdersProgress==1)
      MultiplierLotMinus[GroupOpen]=GroupsMinus[GroupOpen]+1;
   if(LotOrdersProgress==2)
      MultiplierLotMinus[GroupOpen]=MathMax(1,MathPow(2,GroupsMinus[GroupOpen]));
   if(LotOrdersProgress==3)
      MultiplierLotMinus[GroupOpen]=1.0/MathMax(1,MathPow(2,GroupsMinus[GroupOpen]));
//---------------------------------------------------------------------
//Set lots for orders
//---Auto or manual lot
   if(AutoLotSize==1)
      LotSize=((AccountInfoDouble(ACCOUNT_BALANCE)*AccountInfoInteger(ACCOUNT_LEVERAGE))/100000000)*RiskFactor;
   if(AutoLotSize==0)
      LotSize=ManualLotSize;
//---
   if(OpenOrdersInLoss==2)
     {
      if((GroupsMinus[GroupOpen]>0)||(GroupsPlus[GroupOpen]>0))
         LotSizeOrder=NormalizeLot(MathMin(MaximumLot,FirstLotPair[GroupOpen][PairOpen]*MultiplierLotMinus[GroupOpen]),GroupOpen,PairOpen);
      else
         LotSizeOrder=NormalizeLot(MathMin(MaximumLot,LotSize/MultiplierTickValueMinus[GroupOpen][PairOpen]),GroupOpen,PairOpen);
     }
   else
     {
      if(GroupsMinus[GroupOpen]>0)
         LotSizeOrder=NormalizeLot(MathMin(MaximumLot,FirstLotPair[GroupOpen][PairOpen]*MultiplierLotMinus[GroupOpen]),GroupOpen,PairOpen);
      else
         LotSizeOrder=NormalizeLot(MathMin(MaximumLot,LotSize/MultiplierTickValueMinus[GroupOpen][PairOpen]),GroupOpen,PairOpen);
     }
//---------------------------------------------------------------------
   if(SuiteMinus[GroupOpen][PairOpen]==ORDER_TYPE_BUY)
     {
      PriceOpen=SymbolInfoDouble(SymbolPair[GroupOpen][PairOpen],SYMBOL_ASK);
      ColorOrder=clrBlue;
     }
//---
   if(SuiteMinus[GroupOpen][PairOpen]==ORDER_TYPE_SELL)
     {
      PriceOpen=SymbolInfoDouble(SymbolPair[GroupOpen][PairOpen],SYMBOL_BID);
      ColorOrder=clrRed;
     }
//---------------------------------------------------------------------
//Check free margin
   double Margin,FreeMargin=AccountInfoDouble(ACCOUNT_MARGIN_FREE);
   if(!OrderCalcMargin(SuitePlus[GroupOpen][PairOpen],SymbolPair[GroupOpen][PairOpen],LotSizeOrder,PriceOpen,Margin))
     {
      Print("Error in ",__FUNCTION__," code=",GetLastError());
      return;
     }
//---------------------------------------------------------------------
//Declare and initialize the trade request and result of trade request
   MqlTradeRequest request= {};
   MqlTradeResult  result= {};
//---
   request.action=TRADE_ACTION_DEAL;
   request.magic=OrdersID[GroupOpen];
   request.symbol=SymbolPair[GroupOpen][PairOpen];
   request.volume=LotSizeOrder;
   request.price=PriceOpen;
   request.type=SuiteMinus[GroupOpen][PairOpen];
   request.comment=CommentsEA;
   request.sl=0;
   request.tp=0;
//---
   if(SymbolInfoInteger(SymbolPair[GroupOpen][PairOpen],SYMBOL_FILLING_MODE)==1)
      request.type_filling=ORDER_FILLING_FOK;
   if(SymbolInfoInteger(SymbolPair[GroupOpen][PairOpen],SYMBOL_FILLING_MODE)==2)
      request.type_filling=ORDER_FILLING_IOC;
//---------------------------------------------------------------------
//Send order
   if(LimitOfOrdersOk==true)
     {
      if(Margin<FreeMargin)
        {
         CntTry=0;
         while(SymbolInfoInteger(SymbolPair[GroupOpen][PairOpen],SYMBOL_TRADE_MODE)==SYMBOL_TRADE_MODE_FULL)
           {
            CntTry++;
            TicketNo[PairOpen]=OrderSend(request,result);
            //---
            if(TicketNo[PairOpen]>0)
              {
               if(PrintLogReport==true)
                  Print("Open Minus: ",SymbolPair[GroupOpen][PairOpen]," || Ticket No: ",TicketNo[PairOpen]);
               break;
              }
            //---
            Sleep(100);
            if(CntTry>3)
               break;
           }
        }
      else
        {
         CommentWarning=true;
         if(WarningPrint=="")
            WarningPrint="  Free margin is low ("+DoubleToString(FreeMargin)+")";
         Print(WarningPrint);
        }
     }
//---------------------------------------------------------------------
  }
//===============================================================================================================================================================================================================================================================//
//Close plus orders
//===============================================================================================================================================================================================================================================================//
void ClosePairPlus(int GroupClose, ulong TicketClose, int PairClose)
  {
//---------------------------------------------------------------------
//declare and initialize the trade request and result of trade request
   MqlTradeRequest request;
   MqlTradeResult result;
//---------------------------------------------------------------------
//iterate over all open positions
   for(n=PositionsTotal()-1; n>=0; n--)
     {
      //---parameters of the order
      ulong position_ticket=PositionGetTicket(n);
      string position_symbol=PositionGetString(POSITION_SYMBOL);
      int digits=(int)SymbolInfoInteger(position_symbol,SYMBOL_DIGITS);
      ulong magic=PositionGetInteger(POSITION_MAGIC);
      double volume=PositionGetDouble(POSITION_VOLUME);
      ENUM_POSITION_TYPE type=(ENUM_POSITION_TYPE)PositionGetInteger(POSITION_TYPE);
      //---if the MagicNumber matches
      if((magic==OrdersID[GroupClose])&&(position_symbol==SymbolPair[GroupClose][PairClose])&&(type==(ENUM_POSITION_TYPE)SuitePlus[GroupClose][PairClose]))
        {
         if((position_ticket==TicketClose)||(TicketClose==-1))
           {
            //---zeroing the request and result values
            ZeroMemory(request);
            ZeroMemory(result);
            //--- setting the operation parameters
            request.action=TRADE_ACTION_DEAL;
            request.position=position_ticket;
            request.symbol=position_symbol;
            request.volume=volume;
            request.deviation=5;
            request.magic=OrdersID[GroupClose];
            request.comment=CommentsEA;
            //---
            if(SymbolInfoInteger(SymbolPair[GroupClose][PairClose],SYMBOL_FILLING_MODE)==1)
               request.type_filling=ORDER_FILLING_FOK;
            if(SymbolInfoInteger(SymbolPair[GroupClose][PairClose],SYMBOL_FILLING_MODE)==2)
               request.type_filling=ORDER_FILLING_IOC;
            //---set the price and order type depending on the position type
            if(type==POSITION_TYPE_BUY)
              {
               request.price=SymbolInfoDouble(position_symbol,SYMBOL_BID);
               request.type =ORDER_TYPE_SELL;
              }
            else
              {
               request.price=SymbolInfoDouble(position_symbol,SYMBOL_ASK);
               request.type =ORDER_TYPE_BUY;
              }
            //---------------------------------------------------------------------
            //Close position
            CntTry=0;
            while(SymbolInfoInteger(SymbolPair[GroupClose][PairClose],SYMBOL_TRADE_MODE)==SYMBOL_TRADE_MODE_FULL)
              {
               CntTry++;
               //---send the request
               TicketNo[PairClose]=OrderSend(request,result);
               //---
               if(TicketNo[PairClose]>0)
                 {
                  CountHistory=true;
                  if(PrintLogReport==true)
                     Print("Close Plus: ",SymbolPair[GroupClose][PairClose]," || TicketNo: ",position_ticket);
                  break;
                 }
               //---
               Sleep(100);
               if(CntTry>3)
                  break;
              }
            //---
           }
        }
     }
//---------------------------------------------------------------------
  }
//===============================================================================================================================================================================================================================================================//
//Close minus orders
//===============================================================================================================================================================================================================================================================//
void ClosePairMinus(int GroupClose, ulong TicketClose, int PairClose)
  {
//---------------------------------------------------------------------
//declare and initialize the trade request and result of trade request
   MqlTradeRequest request;
   MqlTradeResult result;
//---------------------------------------------------------------------
//iterate over all open positions
   for(n=PositionsTotal()-1; n>=0; n--)
     {
      //---parameters of the order
      ulong position_ticket=PositionGetTicket(n);
      string position_symbol=PositionGetString(POSITION_SYMBOL);
      int digits=(int)SymbolInfoInteger(position_symbol,SYMBOL_DIGITS);
      ulong magic=PositionGetInteger(POSITION_MAGIC);
      double volume=PositionGetDouble(POSITION_VOLUME);
      ENUM_POSITION_TYPE type=(ENUM_POSITION_TYPE)PositionGetInteger(POSITION_TYPE);
      //---if the MagicNumber matches
      if((magic==OrdersID[GroupClose])&&(position_symbol==SymbolPair[GroupClose][PairClose])&&(type==(ENUM_POSITION_TYPE)SuiteMinus[GroupClose][PairClose]))
        {
         if((position_ticket==TicketClose)||(TicketClose==-1))
           {
            //---zeroing the request and result values
            ZeroMemory(request);
            ZeroMemory(result);
            //--- setting the operation parameters
            request.action=TRADE_ACTION_DEAL;
            request.position=position_ticket;
            request.symbol=position_symbol;
            request.volume=volume;
            request.deviation=5;
            request.magic=OrdersID[GroupClose];
            request.comment=CommentsEA;
            //---
            if(SymbolInfoInteger(SymbolPair[GroupClose][PairClose],SYMBOL_FILLING_MODE)==1)
               request.type_filling=ORDER_FILLING_FOK;
            if(SymbolInfoInteger(SymbolPair[GroupClose][PairClose],SYMBOL_FILLING_MODE)==2)
               request.type_filling=ORDER_FILLING_IOC;
            //---set the price and order type depending on the position type
            if(type==POSITION_TYPE_BUY)
              {
               request.price=SymbolInfoDouble(position_symbol,SYMBOL_BID);
               request.type =ORDER_TYPE_SELL;
              }
            else
              {
               request.price=SymbolInfoDouble(position_symbol,SYMBOL_ASK);
               request.type =ORDER_TYPE_BUY;
              }
            //---------------------------------------------------------------------
            //Close position
            CntTry=0;
            while(SymbolInfoInteger(SymbolPair[GroupClose][PairClose],SYMBOL_TRADE_MODE)==SYMBOL_TRADE_MODE_FULL)
              {
               CntTry++;
               //---send the request
               TicketNo[PairClose]=OrderSend(request,result);
               //---
               if(TicketNo[PairClose]>0)
                 {
                  CountHistory=true;
                  if(PrintLogReport==true)
                     Print("Close Minus: ",SymbolPair[GroupClose][PairClose]," || TicketNo: ",position_ticket);
                  break;
                 }
               //---
               Sleep(100);
               if(CntTry>3)
                  break;
              }
            //---
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
            Print("Open Missing Plus"+IntegerToString(i)+" - ",SymbolPair[PairCheck][i]);
        }
      //---
      if(TotalOrdersMinus[PairCheck][i]<MaxOrdersMinus)
        {
         OpenPairMinus(PairCheck,i);
         if(PrintLogReport==true)
            Print("Open Missing Minus"+IntegerToString(i)+" - ",SymbolPair[PairCheck][i]);
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
            Print("Close Excess Plus"+IntegerToString(i)+" - ",SymbolPair[PairCheck][i]);
        }
      //---
      if(TotalOrdersMinus[PairCheck][i]>MinOrdersMinus)
        {
         ClosePairMinus(PairCheck,FirstTicketMinus[PairCheck][i],i);
         if(PrintLogReport==true)
            Print("Close Excess Minus"+IntegerToString(i)+" - ",SymbolPair[PairCheck][i]);
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
   if(PositionsTotal()>0)
     {
      for(i=PositionsTotal()-1; i>=0; i--)
        {
         if(PositionGetTicket(i))
           {
            CountAllOpenedOrders++;
            if(PositionGetInteger(POSITION_MAGIC)==OrdersID[PairGet])
              {
               //---Total groups
               TotalGroupsOrders++;
               TotalGroupsProfit+=PositionGetDouble(POSITION_PROFIT)+PositionGetDouble(POSITION_SWAP);
               //---Plus and minus
               for(j=1; j<=PairsPerGroup; j++)
                 {
                  //---Plus pair
                  if(((ENUM_POSITION_TYPE)PositionGetInteger(POSITION_TYPE)==(ENUM_POSITION_TYPE)SuitePlus[PairGet][j])&&(PositionGetString(POSITION_SYMBOL)==SymbolPair[PairGet][j]))
                    {
                     FirstLotPair[PairGet][j]=PositionGetDouble(POSITION_VOLUME);
                     TotalProfitPlus[PairGet][j]+=PositionGetDouble(POSITION_PROFIT)+PositionGetDouble(POSITION_SWAP);
                     TotalCommissionPlus[PairGet][j]+=MathAbs(PositionGetDouble(POSITION_SWAP));
                     FirstLotPlus[PairGet][j]=PositionGetDouble(POSITION_VOLUME);
                     TotalLotPlus[PairGet][j]+=PositionGetDouble(POSITION_VOLUME);
                     TotalOrdersPlus[PairGet][j]++;
                     FirstTicketPlus[PairGet][j]=PositionGetInteger(POSITION_TICKET);
                     FirstProfitPlus[PairGet][j]=PositionGetDouble(POSITION_PROFIT)+PositionGetDouble(POSITION_SWAP);
                     if(LastTicketPlus[PairGet][j]==0)
                        LastTicketPlus[PairGet][j]=PositionGetInteger(POSITION_TICKET);
                     if(LastLotPlus[PairGet][j]==0)
                        LastLotPlus[PairGet][j]=PositionGetDouble(POSITION_VOLUME);
                     if(TimeOpenLastPlus[PairGet]==0)
                        TimeOpenLastPlus[PairGet]=(datetime)PositionGetInteger(POSITION_TIME);
                    }
                  //---Minus pair
                  if(((ENUM_POSITION_TYPE)PositionGetInteger(POSITION_TYPE)==(ENUM_POSITION_TYPE)SuiteMinus[PairGet][j])&&(PositionGetString(POSITION_SYMBOL)==SymbolPair[PairGet][j]))
                    {
                     FirstLotPair[PairGet][j]=PositionGetDouble(POSITION_VOLUME);
                     TotalProfitMinus[PairGet][j]+=PositionGetDouble(POSITION_PROFIT)+PositionGetDouble(POSITION_SWAP);
                     TotalCommissionMinus[PairGet][j]+=MathAbs(PositionGetDouble(POSITION_SWAP));
                     FirstLotMinus[PairGet][j]=PositionGetDouble(POSITION_VOLUME);
                     TotalLotMinus[PairGet][j]+=PositionGetDouble(POSITION_VOLUME);
                     TotalOrdersMinus[PairGet][j]++;
                     FirstTicketMinus[PairGet][j]=PositionGetInteger(POSITION_TICKET);
                     FirstProfitMinus[PairGet][j]=PositionGetDouble(POSITION_PROFIT)+PositionGetDouble(POSITION_SWAP);
                     if(LastTicketMinus[PairGet][j]==0)
                        LastTicketMinus[PairGet][j]=PositionGetInteger(POSITION_TICKET);
                     if(LastLotMinus[PairGet][j]==0)
                        LastLotMinus[PairGet][j]=PositionGetDouble(POSITION_VOLUME);
                     if(TimeOpenLastMinus[PairGet]==0)
                        TimeOpenLastMinus[PairGet]=(datetime)PositionGetInteger(POSITION_TIME);
                    }
                 }
              }
           }
        }
      //if(CountAllOpenedOrders!=PositionsTotal()) return;//Pass again orders
     }//end if(PositionsTotal()>0)
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
void CountHistoryOrders()
  {
//---------------------------------------------------------------------
//Reset value
   ArrayInitialize(HistoryPlusOrders,0);
   ArrayInitialize(HistoryMinusOrders,0);
   ArrayInitialize(HistoryPlusProfit,0);
   ArrayInitialize(HistoryMinusProfit,0);
   HistoryTotalTrades=0;
   HistoryTotalProfitLoss=0;
   FirstOpenedOrder=0;
//---------------------------------------------------------------------
   ulong Deal_Ticket;
   ulong Order_Ticket;
//---request the history of deals in the specified period
   HistorySelect(0,TimeCurrent());
//---------------------------------------------------------------------
   if(HistoryDealsTotal()>0)
     {
      for(i=0; i<HistoryDealsTotal(); i++)
        {
         Deal_Ticket=HistoryDealGetTicket(i);
         Order_Ticket=HistoryDealGetInteger(Deal_Ticket,DEAL_ORDER);
         //---
         for(int y=0; y<NumberGroupsTrade; y++)
           {
            if(HistoryDealGetInteger(Deal_Ticket,DEAL_MAGIC)==OrdersID[y])
              {
               if(FirstOpenedOrder==0)
                  FirstOpenedOrder=(datetime)HistoryOrderGetInteger(Order_Ticket,ORDER_TIME_DONE);
               //---------------------------------------------------------------------
               //Get deals out
               if(HistoryDealGetInteger(Deal_Ticket,DEAL_ENTRY)==DEAL_ENTRY_OUT)
                 {
                  for(n=1; n<=PairsPerGroup; n++)
                    {
                     //---------------------------------------------------------------------
                     //Count Plus orders
                     if((HistoryDealGetInteger(Deal_Ticket,DEAL_TYPE)==SuiteMinus[y][n])&&(HistoryDealGetString(Deal_Ticket,DEAL_SYMBOL)==SymbolPair[y][n]))//In history, minus=plus. Because close with opposite deal
                       {
                        HistoryPlusOrders[y]++;
                        HistoryPlusProfit[y]+=HistoryDealGetDouble(Deal_Ticket,DEAL_PROFIT)+HistoryDealGetDouble(Deal_Ticket,DEAL_COMMISSION)+HistoryDealGetDouble(Deal_Ticket,DEAL_SWAP);
                       }
                     //---------------------------------------------------------------------
                     //Count Minus orders
                     if((HistoryDealGetInteger(Deal_Ticket,DEAL_TYPE)==SuitePlus[y][n])&&(HistoryDealGetString(Deal_Ticket,DEAL_SYMBOL)==SymbolPair[y][n]))//In history, plus=minus. Because close with opposite deal
                       {
                        HistoryMinusOrders[y]++;
                        HistoryMinusProfit[y]+=HistoryDealGetDouble(Deal_Ticket,DEAL_PROFIT)+HistoryDealGetDouble(Deal_Ticket,DEAL_COMMISSION)+HistoryDealGetDouble(Deal_Ticket,DEAL_SWAP);
                       }
                     //---------------------------------------------------------------------
                    }
                  //---------------------------------------------------------------------
                 }
               //---------------------------------------------------------------------
               //Get deals in
               if(HistoryDealGetInteger(Deal_Ticket,DEAL_ENTRY)==DEAL_ENTRY_IN)
                 {
                  for(n=1; n<=PairsPerGroup; n++)
                    {
                     //---------------------------------------------------------------------
                     //Count Plus orders
                     if((HistoryDealGetInteger(Deal_Ticket,DEAL_TYPE)==SuiteMinus[y][n])&&(HistoryDealGetString(Deal_Ticket,DEAL_SYMBOL)==SymbolPair[y][n]))//In history, minus=plus. Because close with opposite deal
                       {
                        HistoryPlusProfit[y]+=HistoryDealGetDouble(Deal_Ticket,DEAL_PROFIT)+HistoryDealGetDouble(Deal_Ticket,DEAL_COMMISSION)+HistoryDealGetDouble(Deal_Ticket,DEAL_SWAP);
                       }
                     //---------------------------------------------------------------------
                     //Count Minus orders
                     if((HistoryDealGetInteger(Deal_Ticket,DEAL_TYPE)==SuitePlus[y][n])&&(HistoryDealGetString(Deal_Ticket,DEAL_SYMBOL)==SymbolPair[y][n]))//In history, plus=minus. Because close with opposite deal
                       {
                        HistoryMinusProfit[y]+=HistoryDealGetDouble(Deal_Ticket,DEAL_PROFIT)+HistoryDealGetDouble(Deal_Ticket,DEAL_COMMISSION)+HistoryDealGetDouble(Deal_Ticket,DEAL_SWAP);
                       }
                     //---------------------------------------------------------------------
                    }
                  //---------------------------------------------------------------------
                 }
              }
           }
        }
      //---------------------------------------------------------------------
      //Count total results
      for(i=0; i<NumberGroupsTrade; i++)
        {
         HistoryTotalTrades+=HistoryPlusOrders[i]+HistoryMinusOrders[i];
         HistoryTotalProfitLoss+=HistoryPlusProfit[i]+HistoryMinusProfit[i];
        }
     }
//---------------------------------------------------------------------
  }
//===============================================================================================================================================================================================================================================================//
//Background for comments
//===============================================================================================================================================================================================================================================================//
void DrawObjects(string StringName, color ImageColor, int TypeBorder, bool InBackGround, int Xposition, int Yposition, int Xsize, int Ysize)
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
   ObjectSetInteger(0,StringName,OBJPROP_SELECTABLE,false);
   ObjectSetInteger(0,StringName,OBJPROP_SELECTED,false);
   ObjectSetInteger(0,StringName,OBJPROP_HIDDEN,true);
   ObjectSetInteger(0,StringName,OBJPROP_COLOR,FontColor);
   ObjectSetInteger(0,StringName,OBJPROP_FONTSIZE,FontSize);
   ObjectSetString(0,StringName,OBJPROP_TEXT,Image);
   ObjectSetString(0,StringName,OBJPROP_FONT,TypeImage);
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
      Print(" # "+MQLInfoString(MQL_PROGRAM_NAME)+" # "+"StepOpenNextOrders parameter not correct ("+DoubleToString(StepOpenNextOrders,2)+")");
      WrongSet=true;
      WarningMessage=MessageBox(" # "+MQLInfoString(MQL_PROGRAM_NAME)+" # "+"StepOpenNextOrders parameter not correct ("+DoubleToString(StepOpenNextOrders,2)+"), please insert a value greater than 0", "RISK DISCLAIMER");
     }
//---------------------------------------------------------------------
//Check profit close value
   if((TargetCloseProfit<=0)&&(WrongSet==false))
     {
      Comment("\n "+StringOrdersEA+
              "\n\n\nTargetCloseProfit parameter not correct ("+DoubleToString(TargetCloseProfit,2)+")"
              "\n\nPlease insert a value greater than 0");
      Print(" # "+MQLInfoString(MQL_PROGRAM_NAME)+" # "+"TargetCloseProfit parameter not correct ("+DoubleToString(TargetCloseProfit,2)+")");
      WrongSet=true;
      WarningMessage=MessageBox(" # "+MQLInfoString(MQL_PROGRAM_NAME)+" # "+"TargetCloseProfit parameter not correct ("+DoubleToString(TargetCloseProfit,2)+"), please insert a value greater than 0", "RISK DISCLAIMER");
     }
//---------------------------------------------------------------------
//Check loss close value
   if((TypeCloseInLoss<2)&&(TargetCloseLoss<=0)&&(WrongSet==false))
     {
      Comment("\n "+StringOrdersEA+
              "\n\n\nTargetCloseLoss parameter not correct ("+DoubleToString(TargetCloseLoss,2)+")"
              "\n\nPlease insert a value greater than 0");
      Print(" # "+MQLInfoString(MQL_PROGRAM_NAME)+" # "+"TargetCloseLoss parameter not correct ("+DoubleToString(TargetCloseLoss,2)+")");
      WrongSet=true;
      WarningMessage=MessageBox(" # "+MQLInfoString(MQL_PROGRAM_NAME)+" # "+"TargetCloseLoss parameter not correct ("+DoubleToString(TargetCloseLoss,2)+"), please insert a value greater than 0", "RISK DISCLAIMER");
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
   double LevelCloseInLossPlus[MaxGroups];
   double LevelCloseInLossMinus[MaxGroups];
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
      FirstLine1Str="Expert Is In Stand By Mode";
   if(TypeOfOperation==1)
      FirstLine1Str="Expert Is Ready To Open/Close Orders";
   if(TypeOfOperation==2)
      FirstLine1Str="Expert Wait Close In Profit And Stop";
   if(TypeOfOperation==3)
      FirstLine1Str="Expert Close Immediately All Orders";
   if(CommentWarning==true)
      FirstLine1Str="Warning: "+WarningPrint;
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
         LotOrdersStr="Automatic / Statical ("+DoubleToString(((AccountInfoDouble(ACCOUNT_BALANCE)*AccountInfoInteger(ACCOUNT_LEVERAGE))/100000000)*RiskFactor,2)+")";
      if(LotOrdersProgress==1)
         LotOrdersStr="Automatic / Geometrical ("+DoubleToString(((AccountInfoDouble(ACCOUNT_BALANCE)*AccountInfoInteger(ACCOUNT_LEVERAGE))/100000000)*RiskFactor,2)+")";
      if(LotOrdersProgress==2)
         LotOrdersStr="Automatic / Exponential ("+DoubleToString(((AccountInfoDouble(ACCOUNT_BALANCE)*AccountInfoInteger(ACCOUNT_LEVERAGE))/100000000)*RiskFactor,2)+")";
      if(LotOrdersProgress==3)
         LotOrdersStr="Automatic / Decreases ("+DoubleToString(((AccountInfoDouble(ACCOUNT_BALANCE)*AccountInfoInteger(ACCOUNT_LEVERAGE))/100000000)*RiskFactor,2)+")";
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
            //---Background0
            if(ObjectFind(0,"BackgroundLine0")==-1)
               DrawObjects("BackgroundLine0",ColorLineTitles,0,true,240,UpperPosition,UsePosLast-PosLastColumn,24);
            //---Background1
            if((i<NumberGroupsTrade/2)&&(MathMod(NumberGroupsTrade,2)==0))
               if(ObjectFind(0,"BackgroundLine1"+IntegerToString(i))==-1)
                  DrawObjects("BackgroundLine1"+IntegerToString(i),ColorOfLine1,0,true,240,UpperPosition+18+(i*14*2),UsePosLast-PosLastColumn,16);
            if((i<=NumberGroupsTrade/2)&&(MathMod(NumberGroupsTrade,2)==1))
               if(ObjectFind(0,"BackgroundLine1"+IntegerToString(i))==-1)
                  DrawObjects("BackgroundLine1"+IntegerToString(i),ColorOfLine1,0,true,240,UpperPosition+18+(i*14*2),UsePosLast-PosLastColumn,16);
            //---Background2
            if(i<NumberGroupsTrade/2)
               if(ObjectFind(0,"BackgroundLine2"+IntegerToString(i))==-1)
                  DrawObjects("BackgroundLine2"+IntegerToString(i),ColorOfLine2,0,true,240,UpperPosition+32+(i*14*2),UsePosLast-PosLastColumn,16);
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
                     DisplayText("Comm2"+IntegerToString(i),IntegerToString(TotalOrdersPlus[i][1])+"/"+IntegerToString(TotalOrdersPlus[i][2])+"/"+IntegerToString(TotalOrdersPlus[i][3])+"-"+IntegerToString(TotalOrdersMinus[i][1])+"/"+IntegerToString(TotalOrdersMinus[i][2])+"/"+IntegerToString(TotalOrdersMinus[i][3]),10,"Arial Black",ColorOfInfo,PositionOrders-10,UpperPosition+18+(i*14));
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
                     DisplayText("Comm5"+IntegerToString(i),DoubleToString(LevelOpenNextPlus[i],2)+"/"+DoubleToString(LevelOpenNextMinus[i],2),10,"Arial Black",ColorOfInfo,PositionNext,UpperPosition+18+(i*14));
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
              }
           }
        }//End for(i=0; i<NumberGroupsTrade; i++)
     }//End if(ShowTaskInfo==true)
//---------------------------------------------------------------------
//Saving information about opened groups
   if(SaveInformations==true)
     {
      MqlDateTime TimeSave;
      TimeCurrent(TimeSave);
      if(TimeSave.hour!=LastHourSaved)
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
         string NameOfFile=Symbol()+"-"+IntegerToString(MagicNo)+"-"+StringOrdersEA+"-"+IntegerToString((int)AccountInfoInteger(ACCOUNT_LOGIN))+".log";
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
         for(x=1; x<NumberGroupsTrade+1; x++)
           {
            if((x==(int)StringToInteger(GetGroup[x]))&&((int)StringToInteger(GetMaxOrders[x])>MaxOrders[x-1]))
               MaxOrders[x-1]=(int)StringToInteger(GetMaxOrders[x]);
            if((x==(int)StringToInteger(GetGroup[x]))&&((int)StringToInteger(GetMaxFloating[x])<MaxFloating[x-1]))
               MaxFloating[x-1]=(int)StringToInteger(GetMaxFloating[x]);
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
                  LastHourSaved=TimeSave.hour;
              }
            FileClose(FileHandle);
           }
         else
            Print("Operation FileOpen failed, error ",GetLastError());
         //---
        }
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
           "\n  Orders: "+DoubleToString(HistoryTotalTrades,0)+" || Profit/Loss: "+DoubleToString(HistoryTotalProfitLoss,2)+
           "\n=================================");
//---------------------------------------------------------------------
  }
//===============================================================================================================================================================================================================================================================//
//End of code
//===============================================================================================================================================================================================================================================================//
