//====================================================================================================================================================//
#property copyright   "Copyright 2017-2019, Nikolaos Pantzos"
#property link        "https://www.mql5.com/en/users/pannik"
#property version     "1.80"
#property description "This expert is a multipairs grid system."
#property description "\nAttach expert on one chart and expert can to trade all pairs (Max 100 pairs)."
#property description "\nTo better illustrate of graphics, select screen resolution 1280x1024."
//#property icon        "\\Images\\Gridder-Logo.ico";
#property strict
//====================================================================================================================================================//
enum Oper{Stand_by_Mode,Normal_Operation,Close_In_Profit_And_Stop,Close_Immediately_All_Orders};
enum ProgrO{Statical_Step, Geometrical_Step, Exponential_Step};
enum ProgrL{Statical_Lot, Geometrical_Lot, Exponential_Lot};
enum CloseP{Ticket_Orders, Basket_Orders, Hybrid_Mode, Advanced_Mode};
enum CloseL{Whole_Ticket, Only_First_Order, Not_Close_In_Loss};
enum Step{Not_Open_In_Loss, Open_With_Manual_Step, Open_With_Auto_Step};
enum Pair{Chart_Pair, Inputted_Pairs};
enum Info{Not_Show_Info, Show_Same_Panel, Show_New_Panel};
enum Emer{One_By_One, Group_Orders};
//====================================================================================================================================================//
#define PairsPerGroup 1
#define MagicSet      12345
//====================================================================================================================================================//
extern string OperationStr        = "||---------- Operation Set ----------||";//1___ Exteral Settings ___1
extern Oper   TypeOperation       = Normal_Operation;//Type Operation Mode
extern string ManagePairsUse      = "||---------- Manage Pairs Use ----------||";//2___ Exteral Settings ___2
extern Pair   PairsUseTrade       = Inputted_Pairs;//Pairs To Use
extern string ManualPairsTrade    = "EURGBP,EURAUD,EURNZD,EURUSD,EURCAD,EURCHF,EURJPY,GBPAUD,GBPNZD,GBPUSD,GBPCAD,GBPCHF,GBPJPY,AUDNZD,AUDUSD,AUDCAD,AUDCHF,AUDJPY,NZDUSD,NZDCAD,NZDCHF,NZDJPY,USDCAD,USDCHF,USDJPY,CADCHF,CADJPY,CHFJPY";//Manual Pairs Trade
extern string ManageOpenOrders    = "||---------- Manage Open Orders ----------||";//3___ Exteral Settings ___3
extern Step   OpenOrdersInLoss    = Open_With_Auto_Step;//Open Orders In Loss
extern double StepOpenNextOrders  = 50.0;//Step For Next Order (Value $/Lot)
extern ProgrO StepOrdersProgress  = Geometrical_Step;//Type Of Progress Step
extern string ManageCloseProfit   = "||---------- Manage Close Profit Orders ----------||";//4___ Exteral Settings ___4
extern CloseP TypeCloseInProfit   = Hybrid_Mode;//Type Of Close In Profit Orders
extern double TargetCloseProfit   = 50.0;//Target Close In Profit (Value $/Lot)
extern string ManageCloseLoss     = "||---------- Manage Close Losses Orders ----------||";//5___ Exteral Settings ___5
extern CloseL TypeCloseInLoss     = Not_Close_In_Loss;//Type Of Close In Loss Orders
extern double TargetCloseLoss     = 250.0;//Target Close In Loss (Value $/Lot)
extern bool   UsePercentageClose  = false;//Use Percentage Loss Close
extern double PercentageEquity    = 50.0;//Percentage Equity Close
extern string EmergencyMode       = "||---------- Emergency Mode ----------||";//6___ Exteral Settings ___6
extern bool   UseEmergencyMode    = true;//Use Emergency Mode
extern Emer   CheckOrdesWay       = Group_Orders;//Check Orders Emergency Mode
extern int    OrdersRunEmergency1 = 3;//Orders For Partial Close In Profit (0=Not Use)
extern int    OrdersRunEmergency2 = 3;//Orders For Hedge Order (0=Not Use)
extern int    OrdersRunEmergency3 = 3;//Orders For Partial Close In Loss (0=Not Use)
extern bool   UsePresetsValue     = true;//Use Preset Emergency Value
extern string MoneyManagement     = "||---------- Money Management ----------||";//7___ Exteral Settings ___7
extern bool   AutoLotSize         = false;//Use Auto Lot Size
extern double RiskFactor          = 0.1;//Risk Factor For Auto Lot
extern double ManualLotSize       = 0.01;//Manual Lot Size
extern ProgrL LotOrdersProgress   = Geometrical_Lot;//Type Of Progress Lot
extern string LimitOrdersLot      = "||---------- Limit Of Orders And Lot ----------||";//8___ Exteral Settings ___8
extern int    MaxOrdersPerPair    = 15;//Max Orders Per Pair (0=Not Use)
extern double MaxLotPerPair       = 0.0;//Max Lot Per Pair (0=Not Use)
extern double MaxMultiLotPerPair  = 100.0;//Max Multiplier Lot Per Pair (0=Not Use)
extern string TimeWindowSets      = "||---------- Time Window Setting ----------||";//9___ Exteral Settings ___9
extern string TimeWindowInfo1     = "Set time in GMT offset, no broker or local time";
extern string TimeWindowInfo2     = "Correct Time Format HH:MM:SS";
extern bool   UseTimeWindow       = false;//Use Time Window
extern string TimeStartTrade      = "00:00:00";//Time Start Trade
extern string TimeStopTrade       = "00:00:00";//Time Stop Trade
extern string InfoOnTheScreen     = "||---------- Info On The Screen ----------||";//10___ Exteral Settings ___10
extern Info   ShowPairsInfo       = Show_New_Panel;//Show Info On Chart
extern int    SizeFontsOfInfo     = 10;//Size Fonts Of Info
extern color  ColorOfTitle        = clrKhaki;//Color Of Titles
extern color  ColorOfInfo         = clrBeige;//Color Of Info
extern color  ColorLineTitles     = clrOrange;//Color Of Line Titles
extern color  ColorOfLine1        = clrMidnightBlue;//Color Of Line 1
extern color  ColorOfLine2        = clrDarkSlateGray;//Color Of Line 2
extern int    PositionOrders      = 335;//Position 'Orders' Info
extern int    PositionPnL         = 400;//Position 'PnL' Info
extern int    PositionClose       = 465;//Position 'Close' Info
extern int    PositionHistory     = 540;//Position 'History' Info
extern int    PositionMaximum     = 650;//Position 'Maximum' Info
extern int    PositionSpread      = 750;//Position 'Spread' Info
extern string Limitations         = "||---------- Limitations ----------||";//11___ Exteral Settings ___11
extern double MaxSpread           = 0.0;//Max Accepted Spread (0=Not Check)
extern long   MaximumOrders       = 0;//Max Opened Orders (0=Not Limit)
extern int    MaxSlippage         = 3;//Max Accepted Slippage
extern string Configuration       = "||---------- Configuration ----------||";//12___ Exteral Settings ___12
extern int    MagicNumber         = 0;//Orders' ID (0=Generate Automatic)
extern bool   SetChartUses        = true;//Set Automatically Chart To Use
extern bool   PrintLogReport      = false;//Print Log Report
extern bool   UseCompletedBars    = true;//Use Completed Bars
extern string StringOrdersEA      = "GridderEA";//Comment For Orders
extern bool   SetChartInterface   = true;//Set Chart Appearance
extern bool   SaveInformations    = false;//Save Groups Informations
//====================================================================================================================================================//
string SymExt;
string CommentsEA;
string WarningPrint="";
string SymbolStatus[99];
//---------------------------------------------------------------------
double PricePair[99];
double LastPricePair[99];
double SpreadPair[99];
double LotPlus[99];
double LotMinus[99];
double LotHedgePlus[99];
double LotHedgeMinus[99];
double FirstLotPlus[99];
double FirstLotMinus[99];
double LastLotPlus[99];
double LastLotMinus[99];
double CheckMargin[99];
double SumMargin;
double TotalProfitPlus[99];
double TotalProfitMinus[99];
double TotalGroupProfit[99];
double MaxProfit=-99999;
double MinProfit=99999;
double LevelProfitClosePlus[99];
double LevelProfitCloseMinus[99];
double LevelLossClosePlus[99];
double LevelLossCloseMinus[99];
double LevelOpenNextPlus[99];
double LevelOpenNextMinus[99];
double HistoryTotalProfitLoss;
double HistoryTotalPips;
double LotSize;
double iLotSize;
double TotalLotPlus[99];
double TotalLotMinus[99];
double MultiplierStepPlus[99];
double MultiplierStepMinus[99];
double MultiplierLotPlus[99];
double MultiplierLotMinus[99];
double TotalLotsPair[99];
double SpreadValuePlus[99];
double SpreadValueMinus[99];
double TotalCommissionPlus[99];
double TotalCommissionMinus[99];
double LastPlusInProfit[99][999];
double LastMinusInProfit[99][999];
double FirstPlusInProfit[99][999];
double FirstMinusInProfit[99][999];
double FirstProfitPlus[99];
double FirstProfitMinus[99];
double LastProfitPlus[99];
double LastProfitMinus[99];
double LastPlusTotalProfit[99];
double LastMinusTotalProfit[99];
double FirstPlusTotalProfit[99];
double FirstMinusTotalProfit[99];
double MaxFloating[99];
double HistoryPlusProfit[99];
double HistoryMinusProfit[99];
double TotalProfitLoss;
double TotalLots;
double HedgePlusProfit[99];
double HedgeMinusProfit[99];
double HedgePlusLot[99];
double HedgeMinusLot[99];
double TotalHedgeProfit[99];
double TickValuePair[99];
double FirstLotOfPair[99];
double TotalMoreProfit;
double TotalMoreLoss;
//---------------------------------------------------------------------
int i;
int z;
int cnt100;
int MagicNo;
int HedgeID;
int TicketNo[99];
int DecimalsPair;
int MaxTotalOrders=0;
int MultiplierPoint;
int FirstTicketPlus[99];
int FirstTicketMinus[99];
int LastTicketPlus[99];
int LastTicketMinus[99];
int TotalOrdersPlus[99];
int TotalOrdersMinus[99];
int HistoryPlusOrders[99];
int HistoryMinusOrders[99];
int HistoryTotalTrades;
int CntTry;
int CntTick=0;
int CheckTicksOpenMarket;
int CountAllOpenedOrders;
int CountAllHistoryOrders;
int LastAllOrders=0;
int LastBarLevels=0;
int WarningMessage;
int GetCurrencyPos[99];
int LastTicketPlusInProfit[99][999];
int LastTicketMinusInProfit[99][999];
int FirstTicketPlusInProfit[99][999];
int FirstTicketMinusInProfit[99][999];
int EmergencyBarPlus[99];
int EmergencyBarMinus[99];
int MaxOrders[99];
int TotalOrders;
int HedgePlusOrders[99];
int HedgeMinusOrders[99];
int HedgePlusTicket[99];
int HedgeMinusTicket[99];
int LastBar[99];
int CountPlusEmergency[99];
int CountMinusEmergency[99];
int EmergencyValue[99][4];
int SignalsMessageWarning;
int PairMoreProfit;
int PairMoreLoss;
int LastHourSaved=0;
//---------------------------------------------------------------------
bool TimeToTrade;
bool SpreadOK[99];
bool CommentWarning;
bool CountHistory=false;
bool ReadySendPlus[99];
bool ReadySendMinus[99];
bool ReadySendHedgePlus[99];
bool ReadySendHedgeMinus[99];
bool WrongSet=false;
bool WrongPairs=false;
bool MarketIsOpen=false;
bool CallMain=false;
bool ExpertCloseBasketInProfit[99];
bool ExpertCloseBasketInLoss[99];
bool ExpertClosePlusInLoss[99];
bool ExpertClosePlusInProfit[99];
bool ExpertCloseMinusInLoss[99];
bool ExpertCloseMinusInProfit[99];
bool ExpertCloseInPercentage[99];
bool PlaceHedgeOrders[99];
bool NewBar[99];
//---------------------------------------------------------------------
datetime DiffTimes;
datetime StartTime;
//---------------------------------------------------------------------
int NumberPairsTrade=0;
int StartSymbolPos;
string SymbolPair[99];
double DivisorLot=2;
double DivisorOrders=4;
//---------------------------------------------------------------------
long ChartColor;
//====================================================================================================================================================//
//OnInit function
//====================================================================================================================================================//
int OnInit()
  {
//---------------------------------------------------------------------
//Confirm ranges and sets
   if(RiskFactor<0.01) RiskFactor=0.01;
   if(RiskFactor>10.0) RiskFactor=10.0;
   if(MagicNumber<0) MagicNumber=0;
   if(MaximumOrders<0) MaximumOrders=0;
   if(MaxSlippage<1) MaxSlippage=1;
   if(MaxSpread<0) MaxSpread=0;
//---------------------------------------------------------------------
//Set timer
   EventSetMillisecondTimer(1000);
   StartTime=TimeCurrent();
//---------------------------------------------------------------------
//Reset value
   ArrayInitialize(ExpertCloseBasketInProfit,false);
   ArrayInitialize(ExpertCloseBasketInLoss,false);
   ArrayInitialize(ExpertClosePlusInLoss,false);
   ArrayInitialize(ExpertClosePlusInProfit,false);
   ArrayInitialize(ExpertCloseMinusInLoss,false);
   ArrayInitialize(ExpertCloseMinusInProfit,false);
   ArrayInitialize(ExpertCloseInPercentage,false);
   ArrayInitialize(EmergencyBarPlus,0);
   ArrayInitialize(EmergencyBarMinus,0);
   ArrayInitialize(MaxOrders,0);
   ArrayInitialize(NewBar,false);
   ArrayInitialize(LastBar,0);
   ArrayInitialize(MaxFloating,99999);
   ArrayInitialize(EmergencyValue,0);
   HistoryTotalTrades=0;
   HistoryTotalProfitLoss=0;
   HistoryTotalPips=0;
   CheckTicksOpenMarket=0;
   WrongPairs=false;
   CntTick=0;
//---------------------------------------------------------------------
//Reset value
   for(i=0; i<99; i++) SymbolPair[i]="";
   NumberPairsTrade=0;
//---------------------------------------------------------------------
//Symbol suffix
   if(StringLen(Symbol())>6) SymExt=StringSubstr(Symbol(),6);
//---------------------------------------------------------------------
//Set parameters for BT
   if((IsTesting()) || (IsVisualMode()) || (IsOptimization()))
     {
      AutoLotSize=true;
      SetChartUses=false;
      PairsUseTrade=0;
      if(TypeCloseInProfit==3) TypeCloseInProfit=2;
     }
//---------------------------------------------------------------------
//Calculate for 4 or 5 digits broker
   MultiplierPoint=1;
   DecimalsPair=(int)MarketInfo(Symbol(),MODE_DIGITS);
   if((DecimalsPair==3) || (DecimalsPair==5)) MultiplierPoint=10;
//---------------------------------------------------------------------
//Comments orders 
   if(StringOrdersEA=="") CommentsEA=WindowExpertName(); else CommentsEA=StringOrdersEA;
//---------------------------------------------------------------------
//Set chart pair
   if(PairsUseTrade==0)//Chart pair
     {
      NumberPairsTrade=1;
      SymbolPair[0]=Symbol();
      //---Externals value
      if(UsePresetsValue==true)
        {
         if(SymbolPair[0]=="EURGBP"+SymExt)//1-
           {
            OrdersRunEmergency1=1;
            OrdersRunEmergency2=5;
            OrdersRunEmergency3=4;
            if(OrdersRunEmergency1>0) EmergencyValue[0][1]=OrdersRunEmergency1;
            if(OrdersRunEmergency2>0) EmergencyValue[0][2]=OrdersRunEmergency1+OrdersRunEmergency2;
            if(OrdersRunEmergency3>0) EmergencyValue[0][3]=OrdersRunEmergency1+OrdersRunEmergency2+OrdersRunEmergency3;
           }
         else
         if(SymbolPair[0]=="EURAUD"+SymExt)//2-
           {
            OrdersRunEmergency1=2;
            OrdersRunEmergency2=4;
            OrdersRunEmergency3=1;
            if(OrdersRunEmergency1>0) EmergencyValue[0][1]=OrdersRunEmergency1;
            if(OrdersRunEmergency2>0) EmergencyValue[0][2]=OrdersRunEmergency1+OrdersRunEmergency2;
            if(OrdersRunEmergency3>0) EmergencyValue[0][3]=OrdersRunEmergency1+OrdersRunEmergency2+OrdersRunEmergency3;
           }
         else
         if(SymbolPair[0]=="EURNZD"+SymExt)//3-
           {
            OrdersRunEmergency1=3;
            OrdersRunEmergency2=4;
            OrdersRunEmergency3=3;
            if(OrdersRunEmergency1>0) EmergencyValue[0][1]=OrdersRunEmergency1;
            if(OrdersRunEmergency2>0) EmergencyValue[0][2]=OrdersRunEmergency1+OrdersRunEmergency2;
            if(OrdersRunEmergency3>0) EmergencyValue[0][3]=OrdersRunEmergency1+OrdersRunEmergency2+OrdersRunEmergency3;
           }
         else
         if(SymbolPair[0]=="EURUSD"+SymExt)//4-
           {
            OrdersRunEmergency1=4;
            OrdersRunEmergency2=2;
            OrdersRunEmergency3=2;
            if(OrdersRunEmergency1>0) EmergencyValue[0][1]=OrdersRunEmergency1;
            if(OrdersRunEmergency2>0) EmergencyValue[0][2]=OrdersRunEmergency1+OrdersRunEmergency2;
            if(OrdersRunEmergency3>0) EmergencyValue[0][3]=OrdersRunEmergency1+OrdersRunEmergency2+OrdersRunEmergency3;
           }
         else
         if(SymbolPair[0]=="EURCAD"+SymExt)//5-
           {
            OrdersRunEmergency1=5;
            OrdersRunEmergency2=5;
            OrdersRunEmergency3=2;
            if(OrdersRunEmergency1>0) EmergencyValue[0][1]=OrdersRunEmergency1;
            if(OrdersRunEmergency2>0) EmergencyValue[0][2]=OrdersRunEmergency1+OrdersRunEmergency2;
            if(OrdersRunEmergency3>0) EmergencyValue[0][3]=OrdersRunEmergency1+OrdersRunEmergency2+OrdersRunEmergency3;
           }
         else
         if(SymbolPair[0]=="EURCHF"+SymExt)//6-
           {
            OrdersRunEmergency1=2;
            OrdersRunEmergency2=3;
            OrdersRunEmergency3=5;
            if(OrdersRunEmergency1>0) EmergencyValue[0][1]=OrdersRunEmergency1;
            if(OrdersRunEmergency2>0) EmergencyValue[0][2]=OrdersRunEmergency1+OrdersRunEmergency2;
            if(OrdersRunEmergency3>0) EmergencyValue[0][3]=OrdersRunEmergency1+OrdersRunEmergency2+OrdersRunEmergency3;
           }
         else
         if(SymbolPair[0]=="EURJPY"+SymExt)//7-
           {
            OrdersRunEmergency1=4;
            OrdersRunEmergency2=3;
            OrdersRunEmergency3=5;
            if(OrdersRunEmergency1>0) EmergencyValue[0][1]=OrdersRunEmergency1;
            if(OrdersRunEmergency2>0) EmergencyValue[0][2]=OrdersRunEmergency1+OrdersRunEmergency2;
            if(OrdersRunEmergency3>0) EmergencyValue[0][3]=OrdersRunEmergency1+OrdersRunEmergency2+OrdersRunEmergency3;
           }
         else
         if(SymbolPair[0]=="GBPAUD"+SymExt)//8-??? (not good to use)
           {
            OrdersRunEmergency1=2;
            OrdersRunEmergency2=2;
            OrdersRunEmergency3=3;
            if(OrdersRunEmergency1>0) EmergencyValue[0][1]=OrdersRunEmergency1;
            if(OrdersRunEmergency2>0) EmergencyValue[0][2]=OrdersRunEmergency1+OrdersRunEmergency2;
            if(OrdersRunEmergency3>0) EmergencyValue[0][3]=OrdersRunEmergency1+OrdersRunEmergency2+OrdersRunEmergency3;
           }
         else
         if(SymbolPair[0]=="GBPNZD"+SymExt)//9-
           {
            OrdersRunEmergency1=3;
            OrdersRunEmergency2=2;
            OrdersRunEmergency3=4;
            if(OrdersRunEmergency1>0) EmergencyValue[0][1]=OrdersRunEmergency1;
            if(OrdersRunEmergency2>0) EmergencyValue[0][2]=OrdersRunEmergency1+OrdersRunEmergency2;
            if(OrdersRunEmergency3>0) EmergencyValue[0][3]=OrdersRunEmergency1+OrdersRunEmergency2+OrdersRunEmergency3;
           }
         else
         if(SymbolPair[0]=="GBPUSD"+SymExt)//10-??? (not good to use)
           {
            OrdersRunEmergency1=2;
            OrdersRunEmergency2=2;
            OrdersRunEmergency3=2;
            if(OrdersRunEmergency1>0) EmergencyValue[0][1]=OrdersRunEmergency1;
            if(OrdersRunEmergency2>0) EmergencyValue[0][2]=OrdersRunEmergency1+OrdersRunEmergency2;
            if(OrdersRunEmergency3>0) EmergencyValue[0][3]=OrdersRunEmergency1+OrdersRunEmergency2+OrdersRunEmergency3;
           }
         else
         if(SymbolPair[0]=="GBPCAD"+SymExt)//11-? (not good to use)
           {
            OrdersRunEmergency1=3;
            OrdersRunEmergency2=2;
            OrdersRunEmergency3=4;
            if(OrdersRunEmergency1>0) EmergencyValue[0][1]=OrdersRunEmergency1;
            if(OrdersRunEmergency2>0) EmergencyValue[0][2]=OrdersRunEmergency1+OrdersRunEmergency2;
            if(OrdersRunEmergency3>0) EmergencyValue[0][3]=OrdersRunEmergency1+OrdersRunEmergency2+OrdersRunEmergency3;
           }
         else
         if(SymbolPair[0]=="GBPCHF"+SymExt)//12-????? (not good to use)
           {
            OrdersRunEmergency1=2;
            OrdersRunEmergency2=2;
            OrdersRunEmergency3=3;
            if(OrdersRunEmergency1>0) EmergencyValue[0][1]=OrdersRunEmergency1;
            if(OrdersRunEmergency2>0) EmergencyValue[0][2]=OrdersRunEmergency1+OrdersRunEmergency2;
            if(OrdersRunEmergency3>0) EmergencyValue[0][3]=OrdersRunEmergency1+OrdersRunEmergency2+OrdersRunEmergency3;
           }
         else
         if(SymbolPair[0]=="GBPJPY"+SymExt)//13-?? (not good to use)
           {
            OrdersRunEmergency1=3;
            OrdersRunEmergency2=2;
            OrdersRunEmergency3=3;
            if(OrdersRunEmergency1>0) EmergencyValue[0][1]=OrdersRunEmergency1;
            if(OrdersRunEmergency2>0) EmergencyValue[0][2]=OrdersRunEmergency1+OrdersRunEmergency2;
            if(OrdersRunEmergency3>0) EmergencyValue[0][3]=OrdersRunEmergency1+OrdersRunEmergency2+OrdersRunEmergency3;
           }
         else
         if(SymbolPair[0]=="AUDNZD"+SymExt)//14-
           {
            OrdersRunEmergency1=2;
            OrdersRunEmergency2=3;
            OrdersRunEmergency3=4;
            if(OrdersRunEmergency1>0) EmergencyValue[0][1]=OrdersRunEmergency1;
            if(OrdersRunEmergency2>0) EmergencyValue[0][2]=OrdersRunEmergency1+OrdersRunEmergency2;
            if(OrdersRunEmergency3>0) EmergencyValue[0][3]=OrdersRunEmergency1+OrdersRunEmergency2+OrdersRunEmergency3;
           }
         else
         if(SymbolPair[0]=="AUDUSD"+SymExt)//15-
           {
            OrdersRunEmergency1=3;
            OrdersRunEmergency2=2;
            OrdersRunEmergency3=4;
            if(OrdersRunEmergency1>0) EmergencyValue[0][1]=OrdersRunEmergency1;
            if(OrdersRunEmergency2>0) EmergencyValue[0][2]=OrdersRunEmergency1+OrdersRunEmergency2;
            if(OrdersRunEmergency3>0) EmergencyValue[0][3]=OrdersRunEmergency1+OrdersRunEmergency2+OrdersRunEmergency3;
           }
         else
         if(SymbolPair[0]=="AUDCAD"+SymExt)//16-
           {
            OrdersRunEmergency1=2;
            OrdersRunEmergency2=2;
            OrdersRunEmergency3=3;
            if(OrdersRunEmergency1>0) EmergencyValue[0][1]=OrdersRunEmergency1;
            if(OrdersRunEmergency2>0) EmergencyValue[0][2]=OrdersRunEmergency1+OrdersRunEmergency2;
            if(OrdersRunEmergency3>0) EmergencyValue[0][3]=OrdersRunEmergency1+OrdersRunEmergency2+OrdersRunEmergency3;
           }
         else
         if(SymbolPair[0]=="AUDCHF"+SymExt)//17-
           {
            OrdersRunEmergency1=3;
            OrdersRunEmergency2=2;
            OrdersRunEmergency3=4;
            if(OrdersRunEmergency1>0) EmergencyValue[0][1]=OrdersRunEmergency1;
            if(OrdersRunEmergency2>0) EmergencyValue[0][2]=OrdersRunEmergency1+OrdersRunEmergency2;
            if(OrdersRunEmergency3>0) EmergencyValue[0][3]=OrdersRunEmergency1+OrdersRunEmergency2+OrdersRunEmergency3;
           }
         else
         if(SymbolPair[0]=="AUDJPY"+SymExt)//18-
           {
            OrdersRunEmergency1=3;
            OrdersRunEmergency2=2;
            OrdersRunEmergency3=3;
            if(OrdersRunEmergency1>0) EmergencyValue[0][1]=OrdersRunEmergency1;
            if(OrdersRunEmergency2>0) EmergencyValue[0][2]=OrdersRunEmergency1+OrdersRunEmergency2;
            if(OrdersRunEmergency3>0) EmergencyValue[0][3]=OrdersRunEmergency1+OrdersRunEmergency2+OrdersRunEmergency3;
           }
         else
         if(SymbolPair[0]=="NZDUSD"+SymExt)//19-
           {
            OrdersRunEmergency1=2;
            OrdersRunEmergency2=3;
            OrdersRunEmergency3=5;
            if(OrdersRunEmergency1>0) EmergencyValue[0][1]=OrdersRunEmergency1;
            if(OrdersRunEmergency2>0) EmergencyValue[0][2]=OrdersRunEmergency1+OrdersRunEmergency2;
            if(OrdersRunEmergency3>0) EmergencyValue[0][3]=OrdersRunEmergency1+OrdersRunEmergency2+OrdersRunEmergency3;
           }
         else
         if(SymbolPair[0]=="NZDCAD"+SymExt)//20-
           {
            OrdersRunEmergency1=2;
            OrdersRunEmergency2=2;
            OrdersRunEmergency3=4;
            if(OrdersRunEmergency1>0) EmergencyValue[0][1]=OrdersRunEmergency1;
            if(OrdersRunEmergency2>0) EmergencyValue[0][2]=OrdersRunEmergency1+OrdersRunEmergency2;
            if(OrdersRunEmergency3>0) EmergencyValue[0][3]=OrdersRunEmergency1+OrdersRunEmergency2+OrdersRunEmergency3;
           }
         else
         if(SymbolPair[0]=="NZDCHF"+SymExt)//21-
           {
            OrdersRunEmergency1=2;
            OrdersRunEmergency2=4;
            OrdersRunEmergency3=5;
            if(OrdersRunEmergency1>0) EmergencyValue[0][1]=OrdersRunEmergency1;
            if(OrdersRunEmergency2>0) EmergencyValue[0][2]=OrdersRunEmergency1+OrdersRunEmergency2;
            if(OrdersRunEmergency3>0) EmergencyValue[0][3]=OrdersRunEmergency1+OrdersRunEmergency2+OrdersRunEmergency3;
           }
         else
         if(SymbolPair[0]=="NZDJPY"+SymExt)//22-
           {
            OrdersRunEmergency1=2;
            OrdersRunEmergency2=3;
            OrdersRunEmergency3=5;
            if(OrdersRunEmergency1>0) EmergencyValue[0][1]=OrdersRunEmergency1;
            if(OrdersRunEmergency2>0) EmergencyValue[0][2]=OrdersRunEmergency1+OrdersRunEmergency2;
            if(OrdersRunEmergency3>0) EmergencyValue[0][3]=OrdersRunEmergency1+OrdersRunEmergency2+OrdersRunEmergency3;
           }
         else
         if(SymbolPair[0]=="USDCAD"+SymExt)//23-
           {
            OrdersRunEmergency1=2;
            OrdersRunEmergency2=2;
            OrdersRunEmergency3=3;
            if(OrdersRunEmergency1>0) EmergencyValue[0][1]=OrdersRunEmergency1;
            if(OrdersRunEmergency2>0) EmergencyValue[0][2]=OrdersRunEmergency1+OrdersRunEmergency2;
            if(OrdersRunEmergency3>0) EmergencyValue[0][3]=OrdersRunEmergency1+OrdersRunEmergency2+OrdersRunEmergency3;
           }
         else
         if(SymbolPair[0]=="USDCHF"+SymExt)//24-
           {
            OrdersRunEmergency1=4;
            OrdersRunEmergency2=5;
            OrdersRunEmergency3=2;
            if(OrdersRunEmergency1>0) EmergencyValue[0][1]=OrdersRunEmergency1;
            if(OrdersRunEmergency2>0) EmergencyValue[0][2]=OrdersRunEmergency1+OrdersRunEmergency2;
            if(OrdersRunEmergency3>0) EmergencyValue[0][3]=OrdersRunEmergency1+OrdersRunEmergency2+OrdersRunEmergency3;
           }
         else
         if(SymbolPair[0]=="USDJPY"+SymExt)//25-
           {
            OrdersRunEmergency1=3;
            OrdersRunEmergency2=5;
            OrdersRunEmergency3=1;
            if(OrdersRunEmergency1>0) EmergencyValue[0][1]=OrdersRunEmergency1;
            if(OrdersRunEmergency2>0) EmergencyValue[0][2]=OrdersRunEmergency1+OrdersRunEmergency2;
            if(OrdersRunEmergency3>0) EmergencyValue[0][3]=OrdersRunEmergency1+OrdersRunEmergency2+OrdersRunEmergency3;
           }
         else
         if(SymbolPair[0]=="CADCHF"+SymExt)//26-
           {
            OrdersRunEmergency1=3;
            OrdersRunEmergency2=3;
            OrdersRunEmergency3=3;
            if(OrdersRunEmergency1>0) EmergencyValue[0][1]=OrdersRunEmergency1;
            if(OrdersRunEmergency2>0) EmergencyValue[0][2]=OrdersRunEmergency1+OrdersRunEmergency2;
            if(OrdersRunEmergency3>0) EmergencyValue[0][3]=OrdersRunEmergency1+OrdersRunEmergency2+OrdersRunEmergency3;
           }
         else
         if(SymbolPair[0]=="CADJPY"+SymExt)//27-
           {
            OrdersRunEmergency1=2;
            OrdersRunEmergency2=2;
            OrdersRunEmergency3=4;
            if(OrdersRunEmergency1>0) EmergencyValue[0][1]=OrdersRunEmergency1;
            if(OrdersRunEmergency2>0) EmergencyValue[0][2]=OrdersRunEmergency1+OrdersRunEmergency2;
            if(OrdersRunEmergency3>0) EmergencyValue[0][3]=OrdersRunEmergency1+OrdersRunEmergency2+OrdersRunEmergency3;
           }
         else
         if(SymbolPair[0]=="CHFJPY"+SymExt)//28-
           {
            OrdersRunEmergency1=3;
            OrdersRunEmergency2=2;
            OrdersRunEmergency3=2;
            if(OrdersRunEmergency1>0) EmergencyValue[0][1]=OrdersRunEmergency1;
            if(OrdersRunEmergency2>0) EmergencyValue[0][2]=OrdersRunEmergency1+OrdersRunEmergency2;
            if(OrdersRunEmergency3>0) EmergencyValue[0][3]=OrdersRunEmergency1+OrdersRunEmergency2+OrdersRunEmergency3;
           }
        }
      else
        {
         if(OrdersRunEmergency1>0) EmergencyValue[0][1]=OrdersRunEmergency1;
         if(OrdersRunEmergency2>0) EmergencyValue[0][2]=OrdersRunEmergency1+OrdersRunEmergency2;
         if(OrdersRunEmergency3>0) EmergencyValue[0][3]=OrdersRunEmergency1+OrdersRunEmergency2+OrdersRunEmergency3;
        }
     }
//---------------------------------------------------------------------
//Set manual pairs
   if(PairsUseTrade==1)//Manual pairs
     {
      NumberPairsTrade=(StringLen(ManualPairsTrade)+1)/7;
      //---Set maximum value
      if(NumberPairsTrade>99) NumberPairsTrade=99;
      //---
      for(i=0; i<NumberPairsTrade; i++)
        {
         //---Count added symbols
         StartSymbolPos=(i*6)+i;
         SymbolPair[i]=StringSubstr(ManualPairsTrade,StartSymbolPos,6)+SymExt;
         //---Add symbols in data window
         Print(" # "+WindowExpertName()+" # "+"Check symbols...("+SymbolPair[i]+")");
         //---
         if(SymbolSelect(SymbolPair[i],true)) Print(" # "+WindowExpertName()+" # "+SymbolPair[i]+" is ok");
         else
            Print(" # "+WindowExpertName()+" # "+SymbolPair[i]+" not found");
         //---Get prices of symbols
         PricePair[i]=MarketInfo(SymbolPair[i],MODE_BID);
         //---Check symbols
         if((PricePair[i]==0) && (WrongPairs==false))
           {
            SymbolStatus[i]="Pair "+SymbolPair[i]+" Not Found. No Of Pair: "+IntegerToString(i+1);
            Comment("\n "+StringOrdersEA+
                    "\n\n --- W A R N I N G S ---"+
                    "\n\n "+SymbolStatus[i]+
                    "\n\nplease check added pairs!"+
                    "\n\nCorrect format is EURUSD,GBPJPY,CADCHF");
            Print(" # "+WindowExpertName()+" # "+SymbolStatus[i]);
            WrongPairs=true;
           }
         //---------------------------------------------------------------------
         //Prosets value
         if(UsePresetsValue==true)
           {
            if(SymbolPair[i]=="EURGBP"+SymExt)//1-
              {
               OrdersRunEmergency1=1;
               OrdersRunEmergency2=5;
               OrdersRunEmergency3=4;
               if(OrdersRunEmergency1>0) EmergencyValue[i][1]=OrdersRunEmergency1;
               if(OrdersRunEmergency2>0) EmergencyValue[i][2]=OrdersRunEmergency1+OrdersRunEmergency2;
               if(OrdersRunEmergency3>0) EmergencyValue[i][3]=OrdersRunEmergency1+OrdersRunEmergency2+OrdersRunEmergency3;
              }
            else
            if(SymbolPair[i]=="EURAUD"+SymExt)//2-
              {
               OrdersRunEmergency1=2;
               OrdersRunEmergency2=4;
               OrdersRunEmergency3=1;
               if(OrdersRunEmergency1>0) EmergencyValue[i][1]=OrdersRunEmergency1;
               if(OrdersRunEmergency2>0) EmergencyValue[i][2]=OrdersRunEmergency1+OrdersRunEmergency2;
               if(OrdersRunEmergency3>0) EmergencyValue[i][3]=OrdersRunEmergency1+OrdersRunEmergency2+OrdersRunEmergency3;
              }
            else
            if(SymbolPair[i]=="EURNZD"+SymExt)//3-
              {
               OrdersRunEmergency1=3;
               OrdersRunEmergency2=4;
               OrdersRunEmergency3=3;
               if(OrdersRunEmergency1>0) EmergencyValue[i][1]=OrdersRunEmergency1;
               if(OrdersRunEmergency2>0) EmergencyValue[i][2]=OrdersRunEmergency1+OrdersRunEmergency2;
               if(OrdersRunEmergency3>0) EmergencyValue[i][3]=OrdersRunEmergency1+OrdersRunEmergency2+OrdersRunEmergency3;
              }
            else
            if(SymbolPair[i]=="EURUSD"+SymExt)//4-
              {
               OrdersRunEmergency1=4;
               OrdersRunEmergency2=2;
               OrdersRunEmergency3=2;
               if(OrdersRunEmergency1>0) EmergencyValue[i][1]=OrdersRunEmergency1;
               if(OrdersRunEmergency2>0) EmergencyValue[i][2]=OrdersRunEmergency1+OrdersRunEmergency2;
               if(OrdersRunEmergency3>0) EmergencyValue[i][3]=OrdersRunEmergency1+OrdersRunEmergency2+OrdersRunEmergency3;
              }
            else
            if(SymbolPair[i]=="EURCAD"+SymExt)//5-
              {
               OrdersRunEmergency1=5;
               OrdersRunEmergency2=5;
               OrdersRunEmergency3=2;
               if(OrdersRunEmergency1>0) EmergencyValue[i][1]=OrdersRunEmergency1;
               if(OrdersRunEmergency2>0) EmergencyValue[i][2]=OrdersRunEmergency1+OrdersRunEmergency2;
               if(OrdersRunEmergency3>0) EmergencyValue[i][3]=OrdersRunEmergency1+OrdersRunEmergency2+OrdersRunEmergency3;
              }
            else
            if(SymbolPair[i]=="EURCHF"+SymExt)//6-
              {
               OrdersRunEmergency1=2;
               OrdersRunEmergency2=3;
               OrdersRunEmergency3=5;
               if(OrdersRunEmergency1>0) EmergencyValue[i][1]=OrdersRunEmergency1;
               if(OrdersRunEmergency2>0) EmergencyValue[i][2]=OrdersRunEmergency1+OrdersRunEmergency2;
               if(OrdersRunEmergency3>0) EmergencyValue[i][3]=OrdersRunEmergency1+OrdersRunEmergency2+OrdersRunEmergency3;
              }
            else
            if(SymbolPair[i]=="EURJPY"+SymExt)//7-
              {
               OrdersRunEmergency1=4;
               OrdersRunEmergency2=3;
               OrdersRunEmergency3=5;
               if(OrdersRunEmergency1>0) EmergencyValue[i][1]=OrdersRunEmergency1;
               if(OrdersRunEmergency2>0) EmergencyValue[i][2]=OrdersRunEmergency1+OrdersRunEmergency2;
               if(OrdersRunEmergency3>0) EmergencyValue[i][3]=OrdersRunEmergency1+OrdersRunEmergency2+OrdersRunEmergency3;
              }
            else
            if(SymbolPair[i]=="GBPAUD"+SymExt)//8-??? (not good to use)
              {
               OrdersRunEmergency1=2;
               OrdersRunEmergency2=2;
               OrdersRunEmergency3=3;
               if(OrdersRunEmergency1>0) EmergencyValue[i][1]=OrdersRunEmergency1;
               if(OrdersRunEmergency2>0) EmergencyValue[i][2]=OrdersRunEmergency1+OrdersRunEmergency2;
               if(OrdersRunEmergency3>0) EmergencyValue[i][3]=OrdersRunEmergency1+OrdersRunEmergency2+OrdersRunEmergency3;
              }
            else
            if(SymbolPair[i]=="GBPNZD"+SymExt)//9-
              {
               OrdersRunEmergency1=3;
               OrdersRunEmergency2=2;
               OrdersRunEmergency3=4;
               if(OrdersRunEmergency1>0) EmergencyValue[i][1]=OrdersRunEmergency1;
               if(OrdersRunEmergency2>0) EmergencyValue[i][2]=OrdersRunEmergency1+OrdersRunEmergency2;
               if(OrdersRunEmergency3>0) EmergencyValue[i][3]=OrdersRunEmergency1+OrdersRunEmergency2+OrdersRunEmergency3;
              }
            else
            if(SymbolPair[i]=="GBPUSD"+SymExt)//10-??? (not good to use)
              {
               OrdersRunEmergency1=2;
               OrdersRunEmergency2=2;
               OrdersRunEmergency3=2;
               if(OrdersRunEmergency1>0) EmergencyValue[i][1]=OrdersRunEmergency1;
               if(OrdersRunEmergency2>0) EmergencyValue[i][2]=OrdersRunEmergency1+OrdersRunEmergency2;
               if(OrdersRunEmergency3>0) EmergencyValue[i][3]=OrdersRunEmergency1+OrdersRunEmergency2+OrdersRunEmergency3;
              }
            else
            if(SymbolPair[i]=="GBPCAD"+SymExt)//11-? (not good to use)
              {
               OrdersRunEmergency1=3;
               OrdersRunEmergency2=2;
               OrdersRunEmergency3=4;
               if(OrdersRunEmergency1>0) EmergencyValue[i][1]=OrdersRunEmergency1;
               if(OrdersRunEmergency2>0) EmergencyValue[i][2]=OrdersRunEmergency1+OrdersRunEmergency2;
               if(OrdersRunEmergency3>0) EmergencyValue[i][3]=OrdersRunEmergency1+OrdersRunEmergency2+OrdersRunEmergency3;
              }
            else
            if(SymbolPair[i]=="GBPCHF"+SymExt)//12-????? (not good to use)
              {
               OrdersRunEmergency1=2;
               OrdersRunEmergency2=2;
               OrdersRunEmergency3=3;
               if(OrdersRunEmergency1>0) EmergencyValue[i][1]=OrdersRunEmergency1;
               if(OrdersRunEmergency2>0) EmergencyValue[i][2]=OrdersRunEmergency1+OrdersRunEmergency2;
               if(OrdersRunEmergency3>0) EmergencyValue[i][3]=OrdersRunEmergency1+OrdersRunEmergency2+OrdersRunEmergency3;
              }
            else
            if(SymbolPair[i]=="GBPJPY"+SymExt)//13-?? (not good to use)
              {
               OrdersRunEmergency1=3;
               OrdersRunEmergency2=2;
               OrdersRunEmergency3=3;
               if(OrdersRunEmergency1>0) EmergencyValue[i][1]=OrdersRunEmergency1;
               if(OrdersRunEmergency2>0) EmergencyValue[i][2]=OrdersRunEmergency1+OrdersRunEmergency2;
               if(OrdersRunEmergency3>0) EmergencyValue[i][3]=OrdersRunEmergency1+OrdersRunEmergency2+OrdersRunEmergency3;
              }
            else
            if(SymbolPair[i]=="AUDNZD"+SymExt)//14-
              {
               OrdersRunEmergency1=2;
               OrdersRunEmergency2=3;
               OrdersRunEmergency3=4;
               if(OrdersRunEmergency1>0) EmergencyValue[i][1]=OrdersRunEmergency1;
               if(OrdersRunEmergency2>0) EmergencyValue[i][2]=OrdersRunEmergency1+OrdersRunEmergency2;
               if(OrdersRunEmergency3>0) EmergencyValue[i][3]=OrdersRunEmergency1+OrdersRunEmergency2+OrdersRunEmergency3;
              }
            else
            if(SymbolPair[i]=="AUDUSD"+SymExt)//15-
              {
               OrdersRunEmergency1=3;
               OrdersRunEmergency2=2;
               OrdersRunEmergency3=4;
               if(OrdersRunEmergency1>0) EmergencyValue[i][1]=OrdersRunEmergency1;
               if(OrdersRunEmergency2>0) EmergencyValue[i][2]=OrdersRunEmergency1+OrdersRunEmergency2;
               if(OrdersRunEmergency3>0) EmergencyValue[i][3]=OrdersRunEmergency1+OrdersRunEmergency2+OrdersRunEmergency3;
              }
            else
            if(SymbolPair[i]=="AUDCAD"+SymExt)//16-
              {
               OrdersRunEmergency1=2;
               OrdersRunEmergency2=2;
               OrdersRunEmergency3=3;
               if(OrdersRunEmergency1>0) EmergencyValue[i][1]=OrdersRunEmergency1;
               if(OrdersRunEmergency2>0) EmergencyValue[i][2]=OrdersRunEmergency1+OrdersRunEmergency2;
               if(OrdersRunEmergency3>0) EmergencyValue[i][3]=OrdersRunEmergency1+OrdersRunEmergency2+OrdersRunEmergency3;
              }
            else
            if(SymbolPair[i]=="AUDCHF"+SymExt)//17-
              {
               OrdersRunEmergency1=3;
               OrdersRunEmergency2=2;
               OrdersRunEmergency3=4;
               if(OrdersRunEmergency1>0) EmergencyValue[i][1]=OrdersRunEmergency1;
               if(OrdersRunEmergency2>0) EmergencyValue[i][2]=OrdersRunEmergency1+OrdersRunEmergency2;
               if(OrdersRunEmergency3>0) EmergencyValue[i][3]=OrdersRunEmergency1+OrdersRunEmergency2+OrdersRunEmergency3;
              }
            else
            if(SymbolPair[i]=="AUDJPY"+SymExt)//18-
              {
               OrdersRunEmergency1=3;
               OrdersRunEmergency2=2;
               OrdersRunEmergency3=3;
               if(OrdersRunEmergency1>0) EmergencyValue[i][1]=OrdersRunEmergency1;
               if(OrdersRunEmergency2>0) EmergencyValue[i][2]=OrdersRunEmergency1+OrdersRunEmergency2;
               if(OrdersRunEmergency3>0) EmergencyValue[i][3]=OrdersRunEmergency1+OrdersRunEmergency2+OrdersRunEmergency3;
              }
            else
            if(SymbolPair[i]=="NZDUSD"+SymExt)//19-
              {
               OrdersRunEmergency1=2;
               OrdersRunEmergency2=3;
               OrdersRunEmergency3=5;
               if(OrdersRunEmergency1>0) EmergencyValue[i][1]=OrdersRunEmergency1;
               if(OrdersRunEmergency2>0) EmergencyValue[i][2]=OrdersRunEmergency1+OrdersRunEmergency2;
               if(OrdersRunEmergency3>0) EmergencyValue[i][3]=OrdersRunEmergency1+OrdersRunEmergency2+OrdersRunEmergency3;
              }
            else
            if(SymbolPair[i]=="NZDCAD"+SymExt)//20-
              {
               OrdersRunEmergency1=2;
               OrdersRunEmergency2=2;
               OrdersRunEmergency3=4;
               if(OrdersRunEmergency1>0) EmergencyValue[i][1]=OrdersRunEmergency1;
               if(OrdersRunEmergency2>0) EmergencyValue[i][2]=OrdersRunEmergency1+OrdersRunEmergency2;
               if(OrdersRunEmergency3>0) EmergencyValue[i][3]=OrdersRunEmergency1+OrdersRunEmergency2+OrdersRunEmergency3;
              }
            else
            if(SymbolPair[i]=="NZDCHF"+SymExt)//21-
              {
               OrdersRunEmergency1=2;
               OrdersRunEmergency2=4;
               OrdersRunEmergency3=5;
               if(OrdersRunEmergency1>0) EmergencyValue[i][1]=OrdersRunEmergency1;
               if(OrdersRunEmergency2>0) EmergencyValue[i][2]=OrdersRunEmergency1+OrdersRunEmergency2;
               if(OrdersRunEmergency3>0) EmergencyValue[i][3]=OrdersRunEmergency1+OrdersRunEmergency2+OrdersRunEmergency3;
              }
            else
            if(SymbolPair[i]=="NZDJPY"+SymExt)//22-
              {
               OrdersRunEmergency1=2;
               OrdersRunEmergency2=3;
               OrdersRunEmergency3=5;
               if(OrdersRunEmergency1>0) EmergencyValue[i][1]=OrdersRunEmergency1;
               if(OrdersRunEmergency2>0) EmergencyValue[i][2]=OrdersRunEmergency1+OrdersRunEmergency2;
               if(OrdersRunEmergency3>0) EmergencyValue[i][3]=OrdersRunEmergency1+OrdersRunEmergency2+OrdersRunEmergency3;
              }
            else
            if(SymbolPair[i]=="USDCAD"+SymExt)//23-
              {
               OrdersRunEmergency1=2;
               OrdersRunEmergency2=2;
               OrdersRunEmergency3=3;
               if(OrdersRunEmergency1>0) EmergencyValue[i][1]=OrdersRunEmergency1;
               if(OrdersRunEmergency2>0) EmergencyValue[i][2]=OrdersRunEmergency1+OrdersRunEmergency2;
               if(OrdersRunEmergency3>0) EmergencyValue[i][3]=OrdersRunEmergency1+OrdersRunEmergency2+OrdersRunEmergency3;
              }
            else
            if(SymbolPair[i]=="USDCHF"+SymExt)//24-
              {
               OrdersRunEmergency1=4;
               OrdersRunEmergency2=5;
               OrdersRunEmergency3=2;
               if(OrdersRunEmergency1>0) EmergencyValue[i][1]=OrdersRunEmergency1;
               if(OrdersRunEmergency2>0) EmergencyValue[i][2]=OrdersRunEmergency1+OrdersRunEmergency2;
               if(OrdersRunEmergency3>0) EmergencyValue[i][3]=OrdersRunEmergency1+OrdersRunEmergency2+OrdersRunEmergency3;
              }
            else
            if(SymbolPair[i]=="USDJPY"+SymExt)//25-
              {
               OrdersRunEmergency1=3;
               OrdersRunEmergency2=5;
               OrdersRunEmergency3=1;
               if(OrdersRunEmergency1>0) EmergencyValue[i][1]=OrdersRunEmergency1;
               if(OrdersRunEmergency2>0) EmergencyValue[i][2]=OrdersRunEmergency1+OrdersRunEmergency2;
               if(OrdersRunEmergency3>0) EmergencyValue[i][3]=OrdersRunEmergency1+OrdersRunEmergency2+OrdersRunEmergency3;
              }
            else
            if(SymbolPair[i]=="CADCHF"+SymExt)//26-
              {
               OrdersRunEmergency1=3;
               OrdersRunEmergency2=3;
               OrdersRunEmergency3=3;
               if(OrdersRunEmergency1>0) EmergencyValue[i][1]=OrdersRunEmergency1;
               if(OrdersRunEmergency2>0) EmergencyValue[i][2]=OrdersRunEmergency1+OrdersRunEmergency2;
               if(OrdersRunEmergency3>0) EmergencyValue[i][3]=OrdersRunEmergency1+OrdersRunEmergency2+OrdersRunEmergency3;
              }
            else
            if(SymbolPair[i]=="CADJPY"+SymExt)//27-
              {
               OrdersRunEmergency1=2;
               OrdersRunEmergency2=2;
               OrdersRunEmergency3=4;
               if(OrdersRunEmergency1>0) EmergencyValue[i][1]=OrdersRunEmergency1;
               if(OrdersRunEmergency2>0) EmergencyValue[i][2]=OrdersRunEmergency1+OrdersRunEmergency2;
               if(OrdersRunEmergency3>0) EmergencyValue[i][3]=OrdersRunEmergency1+OrdersRunEmergency2+OrdersRunEmergency3;
              }
            else
            if(SymbolPair[i]=="CHFJPY"+SymExt)//28-
              {
               OrdersRunEmergency1=3;
               OrdersRunEmergency2=2;
               OrdersRunEmergency3=2;
               if(OrdersRunEmergency1>0) EmergencyValue[i][1]=OrdersRunEmergency1;
               if(OrdersRunEmergency2>0) EmergencyValue[i][2]=OrdersRunEmergency1+OrdersRunEmergency2;
               if(OrdersRunEmergency3>0) EmergencyValue[i][3]=OrdersRunEmergency1+OrdersRunEmergency2+OrdersRunEmergency3;
              }
           }
         else
           {
            if(OrdersRunEmergency1>0) EmergencyValue[i][1]=OrdersRunEmergency1;
            if(OrdersRunEmergency2>0) EmergencyValue[i][2]=OrdersRunEmergency1+OrdersRunEmergency2;
            if(OrdersRunEmergency3>0) EmergencyValue[i][3]=OrdersRunEmergency1+OrdersRunEmergency2+OrdersRunEmergency3;
           }
        }
     }
//---------------------------------------------------------------------
//Set colors of chart
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
//Set background
   ChartColor=ChartGetInteger(0,CHART_COLOR_BACKGROUND,0);
//---
   if(ShowPairsInfo==1)
     {
      if(ObjectFind("Background")==-1) ChartBackground("Background",(color)ChartColor,BORDER_FLAT,FALSE,0,16,240,(204+(NumberPairsTrade*12)));
     }
//---
   if((ShowPairsInfo==0) || (ShowPairsInfo==2))
     {
      if(ObjectFind("Background")==-1) ChartBackground("Background",(color)ChartColor,BORDER_FLAT,FALSE,0,16,240,192);
     }
//---------------------------------------------------------------------
//Set chart
   if(SetChartUses==true)
     {
      if((ChartSymbol()!=SymbolPair[0]) || (ChartPeriod()!=PERIOD_H1))
        {
         Comment("\n\nExpert set chart symbol...");
         Print(" # "+WindowExpertName()+" # "+"Set chart symbol: "+SymbolPair[0]+" and Period: H1");
         ChartSetSymbolPeriod(0,SymbolPair[0],PERIOD_H1);
        }
     }
//---------------------------------------------------------------------
//Check sets and value
   CheckValue();
   if(WrongSet==true) return(0);
   if(WrongPairs==true) return(0);
//---------------------------------------------------------------------
//ID orders
   if(MagicNumber==0)
     {
      MagicNo=0;
      if(PairsUseTrade==0)
        {
         for(i=0; i<StringLen(Symbol()); i++) MagicNo+=(StringGetChar(Symbol(),i)*(i+1));
        }
      MagicNo+=MagicSet+AccountNumber()+(OpenOrdersInLoss*123)+(TypeCloseInProfit*345)+(TypeCloseInLoss*567);
      HedgeID=MagicNo+123456;
     }
   else
     {
      MagicNo=MagicNumber;
      HedgeID=MagicNo+123456;
     }
//---------------------------------------------------------------------
//Check maximum orders and minimum lot
   if((TypeOperation==1) && (NumberPairsTrade>5))
     {
      if(AutoLotSize==true) iLotSize=(AccountBalance()/100000)*RiskFactor;
      if(AutoLotSize==false) iLotSize=ManualLotSize;
      //---
      if((AccountInfoInteger(ACCOUNT_LIMIT_ORDERS)!=0) || (SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MIN)!=iLotSize))
        {
         if(AccountInfoInteger(ACCOUNT_LIMIT_ORDERS)!=0) SignalsMessageWarning=MessageBox("Account has maximum orders limit: "+IntegerToString(AccountInfoInteger(ACCOUNT_LIMIT_ORDERS))+
            "\n\nBy clicking YES expert run. \n\nStart EXPERT ADVISOR?","RISK DISCLAIMER - "+WindowExpertName(),MB_YESNO|MB_ICONEXCLAMATION);
         if(SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MIN)!=0.01) SignalsMessageWarning=MessageBox("Account or pair has minimum lot: "+DoubleToStr(SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MIN))+
            "\n\nBy clicking YES expert run. \n\nStart EXPERT ADVISOR?","RISK DISCLAIMER - "+WindowExpertName(),MB_YESNO|MB_ICONEXCLAMATION);
         if(SignalsMessageWarning==IDNO) return(INIT_FAILED);
        }
     }
//---------------------------------------------------------------------
//Check set
   if((TypeCloseInProfit==3) && (NumberPairsTrade<3)) TypeCloseInProfit=2;
//---------------------------------------------------------------------
//Set maximum orders
   if(((MaximumOrders==0) && (AccountInfoInteger(ACCOUNT_LIMIT_ORDERS)!=0)) || ((MaximumOrders>AccountInfoInteger(ACCOUNT_LIMIT_ORDERS)) && (AccountInfoInteger(ACCOUNT_LIMIT_ORDERS)!=0))) MaximumOrders=AccountInfoInteger(ACCOUNT_LIMIT_ORDERS);
//---------------------------------------------------------------------
//Call MainFunction function to show information if market is closed
   MainFunction();
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
      if(ObjectFind("Comm1"+IntegerToString(i))>-1) ObjectDelete("Comm1"+IntegerToString(i));
      if(ObjectFind("Comm2"+IntegerToString(i))>-1) ObjectDelete("Comm2"+IntegerToString(i));
      if(ObjectFind("Comm3"+IntegerToString(i))>-1) ObjectDelete("Comm3"+IntegerToString(i));
      if(ObjectFind("Comm4"+IntegerToString(i))>-1) ObjectDelete("Comm4"+IntegerToString(i));
      if(ObjectFind("Comm5"+IntegerToString(i))>-1) ObjectDelete("Comm5"+IntegerToString(i));
      if(ObjectFind("Comm6"+IntegerToString(i))>-1) ObjectDelete("Comm6"+IntegerToString(i));
      if(ObjectFind("Comm7"+IntegerToString(i))>-1) ObjectDelete("Comm7"+IntegerToString(i));
      if(ObjectFind("Text"+IntegerToString(i))>-1) ObjectDelete("Text"+IntegerToString(i));
      if(ObjectFind("Str"+IntegerToString(i))>-1) ObjectDelete("Str"+IntegerToString(i));
      if(ObjectFind("BackgroundLine1"+IntegerToString(i))>-1) ObjectDelete("BackgroundLine1"+IntegerToString(i));
      if(ObjectFind("BackgroundLine2"+IntegerToString(i))>-1) ObjectDelete("BackgroundLine2"+IntegerToString(i));
     }
//---
   if(ObjectFind("BackgroundLine0")>-1) ObjectDelete("BackgroundLine0");
   if(ObjectFind("Background")>-1) ObjectDelete("Background");
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
//Reset value
   CallMain=false;
//---------------------------------------------------------------------
   if((IsOptimization()) || (IsVisualMode()) || (IsTesting())) MainFunction();
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
      if((!IsTradeAllowed()) || (IsTradeContextBusy()))
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
   if(CallMain==true) MainFunction();
//---------------------------------------------------------------------
  }
//====================================================================================================================================================//
//main function
//====================================================================================================================================================//
void MainFunction()
  {
//---------------------------------------------------------------------
//Reset value
   TimeToTrade=true;
   CommentWarning=false;
   PairMoreProfit=-1;
   PairMoreLoss=-1;
   TotalMoreProfit=0;
   TotalMoreLoss=0;
   LotSize=0;
//---------------------------------------------------------------------
//Reset value
   ArrayInitialize(ReadySendPlus,false);
   ArrayInitialize(ReadySendMinus,false);
   ArrayInitialize(ReadySendHedgePlus,false);
   ArrayInitialize(ReadySendHedgeMinus,false);
   ArrayInitialize(SpreadOK,true);
   ArrayInitialize(TicketNo,-1);
   ArrayInitialize(LotPlus,0);
   ArrayInitialize(LotMinus,0);
   ArrayInitialize(LotHedgePlus,0);
   ArrayInitialize(LotHedgeMinus,0);
   ArrayInitialize(SpreadValuePlus,0);
   ArrayInitialize(SpreadValueMinus,0);
   ArrayInitialize(LevelProfitClosePlus,0);
   ArrayInitialize(LevelProfitCloseMinus,0);
   ArrayInitialize(LevelLossClosePlus,0);
   ArrayInitialize(LevelLossCloseMinus,0);
   ArrayInitialize(LevelOpenNextPlus,0);
   ArrayInitialize(LevelOpenNextMinus,0);
   ArrayInitialize(CheckMargin,0);
   ArrayInitialize(MultiplierLotPlus,0);
   ArrayInitialize(MultiplierLotMinus,0);
   ArrayInitialize(TotalLotsPair,0);
   ArrayInitialize(MultiplierStepPlus,1);
   ArrayInitialize(MultiplierStepMinus,1);
//---------------------------------------------------------------------
//Stop in wrong sets or pairs
   if(WrongSet==true) return;
   if(WrongPairs==true) return;
//---------------------------------------------------------------------
//Check open market
   if((!IsTesting()) && (!IsVisualMode()) && (!IsOptimization()))
     {
      if(CheckTicksOpenMarket<3) CheckTicksOpenMarket++;
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
      if((CheckTicksOpenMarket>=2) && (MarketIsOpen==false)) MarketIsOpen=true;
     }
   else
     {
      MarketIsOpen=true;
     }
//---------------------------------------------------------------------
//Check time window
   if(((TimeGMT()<StringToTime(TimeStartTrade)) || (TimeGMT()>StringToTime(TimeStopTrade))) && (UseTimeWindow==true)) TimeToTrade=false;
//---------------------------------------------------------------------
//Count lot size
   if(AutoLotSize==true) LotSize=(AccountBalance()/100000)*RiskFactor;
   if(AutoLotSize==false) LotSize=ManualLotSize;
//---------------------------------------------------------------------
//Start multipair function
   for(int cnt=0; cnt<NumberPairsTrade; cnt++)
     {
      //---------------------------------------------------------------------
      //Control bars
      if((UseCompletedBars==true) && (MarketIsOpen==true))
        {
         NewBar[cnt]=false;
         if(iBars(SymbolPair[cnt],0)!=LastBar[cnt])
           {
            NewBar[cnt]=true;
            LastBar[cnt]=iBars(SymbolPair[cnt],0);
           }
        }
      //---
      if(UseCompletedBars==false) NewBar[cnt]=true;
      //---------------------------------------------------------------------
      //For speed optimization
      if(((IsOptimization())||(IsTesting()))&&(NewBar[cnt]==false)) return;
      //---------------------------------------------------------------------
      //Get orders' informations
      CountCurrentOrders(cnt);
      if(UseEmergencyMode==true) CountOrdersEmergencyMode(cnt);
      //---------------------------------------------------------------------
      //Get spreads
      SpreadPair[cnt]=NormalizeDouble(MarketInfo(SymbolPair[cnt],MODE_SPREAD)/MultiplierPoint,2);
      //---------------------------------------------------------------------
      //Check spreads
      if(MaxSpread>0.0)
        {
         if(SpreadPair[cnt]>MaxSpread)
           {
            SpreadOK[cnt]=false;
            CommentWarning=true;
            WarningPrint=StringConcatenate("Spread it isn't normal (",SpreadPair[cnt],"/",MaxSpread,")");
           }
        }
      //---------------------------------------------------------------------
      //Get tick value
      TickValuePair[cnt]=MarketInfo(SymbolPair[cnt],MODE_TICKVALUE);
      //---------------------------------------------------------------------
      //Calculate spreads value
      SpreadValuePlus[cnt]=SpreadPair[cnt]*TotalLotPlus[cnt]*TickValuePair[cnt]*MultiplierPoint;
      SpreadValueMinus[cnt]=SpreadPair[cnt]*TotalLotMinus[cnt]*TickValuePair[cnt]*MultiplierPoint;
      //---------------------------------------------------------------------
      //Reset value
      if(TotalOrdersPlus[cnt]==0)
        {
         ExpertClosePlusInProfit[cnt]=false;
         ExpertClosePlusInLoss[cnt]=false;
        }
      //---
      if(TotalOrdersMinus[cnt]==0)
        {
         ExpertCloseMinusInProfit[cnt]=false;
         ExpertCloseMinusInLoss[cnt]=false;
        }
      //---
      if(TotalOrdersPlus[cnt]+TotalOrdersMinus[cnt]+HedgePlusOrders[cnt]+HedgeMinusOrders[cnt]==0)
        {
         ExpertCloseBasketInProfit[cnt]=false;
         ExpertCloseBasketInLoss[cnt]=false;
         ExpertCloseInPercentage[cnt]=false;
         PlaceHedgeOrders[cnt]=false;
        }
      //---------------------------------------------------------------------
      //Count history orders first time
      if(CntTick<NumberPairsTrade+4)
        {
         CntTick++;
         if(CntTick<NumberPairsTrade+3) CountHistory=true;
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
         if(TotalOrdersPlus[cnt]>0) MultiplierStepPlus[cnt]=TotalOrdersPlus[cnt];
         if(TotalOrdersMinus[cnt]>0) MultiplierStepMinus[cnt]=TotalOrdersMinus[cnt];
        }
      //---
      if(StepOrdersProgress==1)//Geometrical
        {
         if(TotalOrdersPlus[cnt]>0) for(i=0; i<=TotalOrdersPlus[cnt]; i++) MultiplierStepPlus[cnt]+=i;
         if(TotalOrdersMinus[cnt]>0) for(i=0; i<=TotalOrdersMinus[cnt]; i++) MultiplierStepMinus[cnt]+=i;
        }
      //---
      if(StepOrdersProgress==2)//Exponential
        {
         if(TotalOrdersPlus[cnt]>0) for(i=0; i<=TotalOrdersPlus[cnt]; i++) MultiplierStepPlus[cnt]+=MathMax(1,MathPow(2,i-1));
         if(TotalOrdersMinus[cnt]>0) for(i=0; i<=TotalOrdersMinus[cnt]; i++) MultiplierStepMinus[cnt]+=MathMax(1,MathPow(2,i-1));
        }
      //---------------------------------------------------------------------
      //Levels
      //---Levels open next orders in loss
      LevelOpenNextPlus[cnt]=-((TotalLotPlus[cnt]*StepOpenNextOrders*MultiplierStepPlus[cnt]*TickValuePair[cnt])+SpreadValuePlus[cnt]+TotalCommissionPlus[cnt]);
      LevelOpenNextMinus[cnt]=-((TotalLotMinus[cnt]*StepOpenNextOrders*MultiplierStepMinus[cnt]*TickValuePair[cnt])+SpreadValueMinus[cnt]+TotalCommissionMinus[cnt]);
      //---Levels close orders in profit
      if((OpenOrdersInLoss<2) || (TypeOperation==2))//Manual step
        {
         LevelProfitClosePlus[cnt]=(TotalLotPlus[cnt]*TargetCloseProfit*TickValuePair[cnt]);
         LevelProfitCloseMinus[cnt]=(TotalLotMinus[cnt]*TargetCloseProfit*TickValuePair[cnt]);
        }
      //---
      if((OpenOrdersInLoss==2) && (TypeOperation!=2))//Auto step
        {
         //---Levels close orders in profit
         LevelProfitClosePlus[cnt]=(FirstLotPlus[cnt]*MultiplierStepPlus[cnt]*TargetCloseProfit)*TickValuePair[cnt];
         LevelProfitCloseMinus[cnt]=(FirstLotMinus[cnt]*MultiplierStepMinus[cnt]*TargetCloseProfit)*TickValuePair[cnt];
        }
      //---Levels close orders in loss
      LevelLossClosePlus[cnt]=-((TotalLotPlus[cnt]*TargetCloseLoss*TickValuePair[cnt])+SpreadValuePlus[cnt]+TotalCommissionPlus[cnt]);
      LevelLossCloseMinus[cnt]=-((TotalLotMinus[cnt]*TargetCloseLoss*TickValuePair[cnt])+SpreadValueMinus[cnt]+TotalCommissionMinus[cnt]);
      //---------------------------------------------------------------------
      //Send orders
      if((MarketIsOpen==true) && (SpreadOK[cnt]==true) && (NewBar[cnt]==true) && (TimeToTrade==true) && 
         (ExpertClosePlusInLoss[cnt]==false) && (ExpertCloseMinusInLoss[cnt]==false) && (ExpertClosePlusInProfit[cnt]==false) && (ExpertCloseMinusInProfit[cnt]==false) && 
         (ExpertCloseBasketInProfit[cnt]==false) && (ExpertCloseBasketInLoss[cnt]==false) && (ExpertCloseInPercentage[cnt]==false))
        {
         //---------------------------------------------------------------------
         //Send first orders
         if(TotalOrdersPlus[cnt]==0) ReadySendPlus[cnt]=true;
         if(TotalOrdersMinus[cnt]==0) ReadySendMinus[cnt]=true;
         //---------------------------------------------------------------------
         //Send oposite orders if close a orders in profit
         if((OpenOrdersInLoss==2) && ((TotalOrdersPlus[cnt]+TotalOrdersMinus[cnt]+HedgePlusOrders[cnt]+HedgeMinusOrders[cnt]<MaxOrdersPerPair-2) || (MaxOrdersPerPair==0)))
           {
            //---Send minus if close plus
            if((TotalOrdersPlus[cnt]==0) && (TotalOrdersMinus[cnt]>=PairsPerGroup))
              {
               ReadySendMinus[cnt]=true;
              }
            //---Send plus if close minus
            if((TotalOrdersMinus[cnt]==0) && (TotalOrdersPlus[cnt]>=PairsPerGroup))
              {
               ReadySendPlus[cnt]=true;
              }
           }
         //---------------------------------------------------------------------
         //Send next orders in loss
         if((OpenOrdersInLoss==1) && ((TotalOrdersPlus[cnt]+TotalOrdersMinus[cnt]+HedgePlusOrders[cnt]+HedgeMinusOrders[cnt]<MaxOrdersPerPair-2) || (MaxOrdersPerPair==0)))
           {
            //---Send plus
            if((TotalOrdersPlus[cnt]>=PairsPerGroup) && (TotalProfitPlus[cnt]<=LevelOpenNextPlus[cnt]) && ((TypeCloseInProfit==0) || (TotalOrdersPlus[cnt]==TotalOrdersMinus[cnt]) || (TotalOrdersPlus[cnt]>TotalOrdersMinus[cnt])))
              {
               ReadySendPlus[cnt]=true;
              }
            //---Send minus
            if((TotalOrdersMinus[cnt]>=PairsPerGroup) && (TotalProfitMinus[cnt]<=LevelOpenNextMinus[cnt]) && ((TypeCloseInProfit==0) || (TotalOrdersPlus[cnt]==TotalOrdersMinus[cnt]) || (TotalOrdersPlus[cnt]<TotalOrdersMinus[cnt])))
              {
               ReadySendMinus[cnt]=true;
              }
           }
        }//End if((MarketIsOpen==true)&&(SpreadOK[cnt]==true)&&(NewBar[cnt]==true)&&
      //---------------------------------------------------------------------
      //Wait for profit, close and stop
      if(TypeOperation==2)
        {
         if(TotalOrdersPlus[cnt]+HedgePlusOrders[cnt]+TotalOrdersMinus[cnt]+HedgeMinusOrders[cnt]==0)
           {
            ReadySendPlus[cnt]=false;
            ReadySendMinus[cnt]=false;
           }
        }
      //---------------------------------------------------------------------
      //Stand by
      if(TypeOperation==0)
        {
         ReadySendPlus[cnt]=false;
         ReadySendMinus[cnt]=false;
        }
      //---------------------------------------------------------------------
      //Close orders
      if((NewBar[cnt]==true) && (MarketIsOpen==true))
        {
         //---------------------------------------------------------------------
         //Start emergency mode
         if((UseEmergencyMode==true) && (OpenOrdersInLoss!=0))
           {
            //---Close partial orders in profit---//
            if(EmergencyValue[cnt][1]>0)
              {
               if(HedgePlusOrders[cnt]+HedgeMinusOrders[cnt]==0)
                 {
                  //---Start for plus
                  if(TotalOrdersPlus[cnt]>=EmergencyValue[cnt][1])
                    {
                     //---Check one by one
                     if((CheckOrdesWay==0) && (((LastProfitPlus[cnt]+FirstProfitPlus[cnt]+FirstProfitMinus[cnt]>=LevelProfitCloseMinus[cnt]) && (OpenOrdersInLoss==2)) || 
                        ((LastProfitPlus[cnt]+FirstProfitPlus[cnt]>=LevelProfitCloseMinus[cnt]) && (OpenOrdersInLoss==1))) && (iBars(NULL,0)!=EmergencyBarPlus[cnt]))
                       {
                        EmergencyBarPlus[cnt]=iBars(NULL,0);
                        if(LastTicketPlus[cnt]>0) ClosePairPlus(cnt,LastTicketPlus[cnt]);
                        if(FirstTicketPlus[cnt]>0) ClosePairPlus(cnt,FirstTicketPlus[cnt]);
                        if((OpenOrdersInLoss==2) && (FirstTicketMinus[cnt]>0)) ClosePairMinus(cnt,FirstTicketMinus[cnt]);
                        CountHistory=true;
                        if(PrintLogReport==true) Print("Close Plus and Minus Order of Emergency Mode 'Partial Close In Profit'");
                       }
                     //---Check as group
                     if((CheckOrdesWay==1) && (((LastPlusTotalProfit[cnt]+FirstProfitPlus[cnt]+FirstProfitMinus[cnt]>=LevelProfitCloseMinus[cnt]) && (OpenOrdersInLoss==2)) || 
                        ((LastPlusTotalProfit[cnt]+FirstProfitPlus[cnt]>=LevelProfitCloseMinus[cnt]) && (OpenOrdersInLoss==1))) && (iBars(NULL,0)!=EmergencyBarPlus[cnt]))
                       {
                        EmergencyBarPlus[cnt]=iBars(NULL,0);
                        for(z=OrdersTotal()-1; z>=0; z--)
                          {
                           if(LastTicketPlusInProfit[cnt][z]!=-1) ClosePairPlus(cnt,LastTicketPlusInProfit[cnt][z]);
                          }
                        if(FirstTicketPlus[cnt]>0) ClosePairPlus(cnt,FirstTicketPlus[cnt]);
                        if((OpenOrdersInLoss==2) && (FirstTicketMinus[cnt]>0)) ClosePairMinus(cnt,FirstTicketMinus[cnt]);
                        CountHistory=true;
                        if(PrintLogReport==true) Print("Close Plus and Minus Order of Emergency Mode 'Partial Close In Profit'");
                       }
                    }
                  //---Start for minus
                  if(TotalOrdersMinus[cnt]>=EmergencyValue[cnt][1])
                    {
                     //---Check one by one
                     if((CheckOrdesWay==0) && (((LastProfitMinus[cnt]+FirstProfitMinus[cnt]+FirstProfitPlus[cnt]>=LevelProfitClosePlus[cnt]) && (OpenOrdersInLoss==2)) || 
                        ((LastProfitMinus[cnt]+FirstProfitMinus[cnt]>=LevelProfitClosePlus[cnt]) && (OpenOrdersInLoss==1))) && (iBars(NULL,0)!=EmergencyBarMinus[cnt]))
                       {
                        EmergencyBarMinus[cnt]=iBars(NULL,0);
                        if(LastTicketMinus[cnt]>0) ClosePairMinus(cnt,LastTicketMinus[cnt]);
                        if(FirstTicketMinus[cnt]>0) ClosePairMinus(cnt,FirstTicketMinus[cnt]);
                        if((OpenOrdersInLoss==2) && (FirstTicketPlus[cnt]>0)) ClosePairPlus(cnt,FirstTicketPlus[cnt]);
                        CountHistory=true;
                        if(PrintLogReport==true) Print("Close Minus and Plus Order of Emergency Mode 'Partial Close In Profit'");
                       }
                     //---Check as group
                     if((CheckOrdesWay==1) && (((LastMinusTotalProfit[cnt]+FirstProfitMinus[cnt]+FirstProfitPlus[cnt]>=LevelProfitClosePlus[cnt]) && (OpenOrdersInLoss==2)) || 
                        ((LastMinusTotalProfit[cnt]+FirstProfitMinus[cnt]>=LevelProfitClosePlus[cnt]) && (OpenOrdersInLoss==1))) && (iBars(NULL,0)!=EmergencyBarMinus[cnt]))
                       {
                        EmergencyBarMinus[cnt]=iBars(NULL,0);
                        for(z=OrdersTotal()-1; z>=0; z--)
                          {
                           if(LastTicketMinusInProfit[cnt][z]!=-1) ClosePairMinus(cnt,LastTicketMinusInProfit[cnt][z]);
                          }
                        if(FirstTicketMinus[cnt]>0) ClosePairMinus(cnt,FirstTicketMinus[cnt]);
                        if((OpenOrdersInLoss==2) && (FirstTicketPlus[cnt]>0)) ClosePairPlus(cnt,FirstTicketPlus[cnt]);
                        CountHistory=true;
                        if(PrintLogReport==true) Print("Close Minus and Plus Order of Emergency Mode 'Partial Close In Profit'");
                       }
                    }
                 }
              }//End if(EmergencyValue[cnt][1]>0)
            //---Open/Close hedge orders---//
            if(EmergencyValue[cnt][2]>0)
              {
               //---Start for plus
               if(((PlaceHedgeOrders[cnt]==true) || (TotalOrdersPlus[cnt]+TotalOrdersMinus[cnt]>=EmergencyValue[cnt][2])) && (TotalOrdersPlus[cnt]>TotalOrdersMinus[cnt]))//&&(TotalLotPlus[cnt]>TotalLotMinus[cnt]*4))
                 {
                  //---Hedge plus orders
                  if(HedgeMinusOrders[cnt]==0)
                    {
                     PlaceHedgeOrders[cnt]=true;
                     ReadySendHedgeMinus[cnt]=true;
                     LotHedgeMinus[cnt]=NormalizeLot(TotalLotPlus[cnt]/DivisorLot,cnt);
                     if(PrintLogReport==true) Print("Open Plus Order of Emergency Mode 'Hedge Order'");
                    }
                  //---Close hedge plus orders as group
                  if((CheckOrdesWay==1) && (HedgeMinusOrders[cnt]>0) && (HedgeMinusProfit[cnt]+FirstPlusTotalProfit[cnt]>=LevelProfitCloseMinus[cnt]) && (iBars(NULL,0)!=EmergencyBarPlus[cnt]))
                    {
                     EmergencyBarPlus[cnt]=iBars(NULL,0);
                     for(z=OrdersTotal()-1; z>=0; z--)
                       {
                        if(FirstTicketPlusInProfit[cnt][z]!=-1) ClosePairPlus(cnt,FirstTicketPlusInProfit[cnt][z]);
                       }
                     if(HedgeMinusOrders[cnt]>0) ClosePairMinus(cnt,HedgeMinusTicket[cnt]);
                     CountHistory=true;
                     if(PrintLogReport==true) Print("Close Hedge and First Plus Order of Emergency Mode 'Hedge Order'");
                    }
                  //---Close hedge plus orders one buy one
                  if((CheckOrdesWay==0) && (HedgeMinusOrders[cnt]>0) && (HedgeMinusProfit[cnt]+FirstProfitPlus[cnt]>=LevelProfitCloseMinus[cnt]) && (iBars(NULL,0)!=EmergencyBarPlus[cnt]))
                    {
                     EmergencyBarPlus[cnt]=iBars(NULL,0);
                     if(FirstTicketPlus[cnt]>0) ClosePairPlus(cnt,FirstTicketPlus[cnt]);
                     if(HedgeMinusOrders[cnt]>0) ClosePairMinus(cnt,HedgeMinusTicket[cnt]);
                     CountHistory=true;
                     if(PrintLogReport==true) Print("Close Hedge and First Plus Order of Emergency Mode 'Hedge Order'");
                    }
                 }
               //---Start for minus
               if(((PlaceHedgeOrders[cnt]==true) || (TotalOrdersMinus[cnt]+TotalOrdersPlus[cnt]>=EmergencyValue[cnt][2])) && (TotalOrdersMinus[cnt]>TotalOrdersPlus[cnt]))//&&(TotalLotPlus[cnt]*4<TotalLotMinus[cnt]))
                 {
                  //---Hedge minus orders
                  if(HedgePlusOrders[cnt]==0)
                    {
                     PlaceHedgeOrders[cnt]=true;
                     ReadySendHedgePlus[cnt]=true;
                     LotHedgePlus[cnt]=NormalizeLot(TotalLotMinus[cnt]/DivisorLot,cnt);
                     if(PrintLogReport==true) Print("Open Minus Order of Emergency Mode 'Hedge Order'");
                    }
                  //---Close hedge minus orders as group
                  if((CheckOrdesWay==1) && (HedgePlusOrders[cnt]>0) && (HedgePlusProfit[cnt]+FirstMinusTotalProfit[cnt]>=LevelProfitClosePlus[cnt]) && (iBars(NULL,0)!=EmergencyBarMinus[cnt]))
                    {
                     EmergencyBarMinus[cnt]=iBars(NULL,0);
                     for(z=OrdersTotal()-1; z>=0; z--)
                       {
                        if(FirstTicketMinusInProfit[cnt][z]!=-1) ClosePairMinus(cnt,FirstTicketMinusInProfit[cnt][z]);
                       }
                     if(HedgePlusOrders[cnt]>0) ClosePairPlus(cnt,HedgePlusTicket[cnt]);
                     CountHistory=true;
                     if(PrintLogReport==true) Print("Close Hedge and First Minus Order of Emergency Mode 'Hedge Order'");
                    }
                  //---Close hedge minus orders one buy one
                  if((CheckOrdesWay==0) && (HedgePlusOrders[cnt]>0) && (HedgePlusProfit[cnt]+FirstProfitMinus[cnt]>=LevelProfitClosePlus[cnt]) && (iBars(NULL,0)!=EmergencyBarMinus[cnt]))
                    {
                     EmergencyBarMinus[cnt]=iBars(NULL,0);
                     if(FirstTicketMinus[cnt]>0) ClosePairMinus(cnt,FirstTicketMinus[cnt]);
                     if(HedgePlusOrders[cnt]>0) ClosePairPlus(cnt,HedgePlusTicket[cnt]);
                     CountHistory=true;
                     if(PrintLogReport==true) Print("Close Hedge and First Minus Order of Emergency Mode 'Hedge Order'");
                    }
                 }
              }//End if(EmergencyValue[cnt][2]>0)
            //---Close partial orders in huge orders---//
            if(EmergencyValue[cnt][3]>0)
              {
               if((TotalOrdersPlus[cnt]>0) && (TotalOrdersMinus[cnt]>0))
                 {
                  //---Start close plus
                  if((TotalOrdersPlus[cnt]>=EmergencyValue[cnt][3]) && (TotalOrdersPlus[cnt]>TotalOrdersMinus[cnt]))
                    {
                     if(TotalOrdersPlus[cnt]>0) ClosePairPlus(cnt,FirstTicketPlus[cnt]);
                     if(PrintLogReport==true) Print("Close Plus First Order of Emergency Mode 'Partial Close In Loss'");
                     CountHistory=true;
                    }
                  //---Start close minus
                  if((TotalOrdersMinus[cnt]>=EmergencyValue[cnt][3]) && (TotalOrdersPlus[cnt]<TotalOrdersMinus[cnt]))
                    {
                     if(TotalOrdersMinus[cnt]>0) ClosePairMinus(cnt,FirstTicketMinus[cnt]);
                     if(PrintLogReport==true) Print("Close Minus First Order of Emergency Mode 'Partial Close In Loss'");
                     CountHistory=true;
                    }
                 }
              }//End if(EmergencyValue[cnt][3]>0)
            //---
           }//end if(UseEmergencyMode==true)
         //---------------------------------------------------------------------
         //Close in percentage loss
         if(UsePercentageClose==true)
           {
            if(((TotalProfitPlus[cnt]+TotalProfitMinus[cnt]<=-((AccountBalance()/100)*PercentageEquity)) || (ExpertCloseInPercentage[cnt]==true)) && (TotalOrdersPlus[cnt]+TotalOrdersMinus[cnt]>0))
              {
               if(TotalOrdersPlus[cnt]>0) ClosePairPlus(cnt,-1);
               if(TotalOrdersMinus[cnt]>0) ClosePairMinus(cnt,-1);
               if(HedgePlusOrders[cnt]>0) ClosePairPlus(cnt,HedgePlusTicket[cnt]);
               if(HedgeMinusOrders[cnt]>0) ClosePairMinus(cnt,HedgeMinusTicket[cnt]);
               ExpertCloseInPercentage[cnt]=true;
               if(PrintLogReport==true) Print("Close Plus and Minus Orders From 'Percentage Close Mode'");
               CountHistory=true;
               continue;
              }
           }
         //---------------------------------------------------------------------
         //Close orders in loss
         if((TypeCloseInLoss<2) && (ExpertCloseBasketInProfit[cnt]==false) && (ExpertClosePlusInProfit[cnt]==false) && (ExpertCloseMinusInProfit[cnt]==false) && (ExpertCloseInPercentage[cnt]==false))
           {
            //---Close whole ticket
            if((TypeCloseInLoss==0) && (TotalOrdersPlus[cnt]+TotalOrdersMinus[cnt]>0))
              {
               if((TotalProfitPlus[cnt]<0) && (TotalOrdersPlus[cnt]>0))
                 {
                  //---Start close plus in loss
                  if((TotalProfitPlus[cnt]<=LevelLossClosePlus[cnt]) || (ExpertClosePlusInLoss[cnt]==true))
                    {
                     if(TotalOrdersPlus[cnt]>0) ClosePairPlus(cnt,-1);
                     if(HedgePlusOrders[cnt]>0) ClosePairPlus(cnt,HedgePlusTicket[cnt]);
                     if(HedgeMinusOrders[cnt]>0) ClosePairMinus(cnt,HedgeMinusTicket[cnt]);
                     ExpertClosePlusInLoss[cnt]=true;
                     CountHistory=true;
                     continue;
                    }
                 }
               //---Start close minus in loss
               if((TotalProfitMinus[cnt]<0) && (TotalOrdersMinus[cnt]>0))
                 {
                  if((TotalProfitMinus[cnt]<=LevelLossCloseMinus[cnt]) || (ExpertCloseMinusInLoss[cnt]==true))
                    {
                     if(TotalOrdersMinus[cnt]>0) ClosePairMinus(cnt,-1);
                     if(HedgePlusOrders[cnt]>0) ClosePairPlus(cnt,HedgePlusTicket[cnt]);
                     if(HedgeMinusOrders[cnt]>0) ClosePairMinus(cnt,HedgeMinusTicket[cnt]);
                     ExpertCloseMinusInLoss[cnt]=true;
                     CountHistory=true;
                     continue;
                    }
                 }
              }//End if(TypeCloseInLoss==0)
            //---Close partial ticket
            if((TypeCloseInLoss==1) && (TotalOrdersPlus[cnt]+TotalOrdersMinus[cnt]>0))
              {
               //---Start close plus in loss
               if((TotalProfitPlus[cnt]<0) && (TotalOrdersPlus[cnt]>0) && (TotalOrdersPlus[cnt]>TotalOrdersMinus[cnt]))
                 {
                  if(TotalProfitPlus[cnt]<=LevelLossClosePlus[cnt])
                    {
                     if(TotalOrdersPlus[cnt]>0) ClosePairPlus(cnt,FirstTicketPlus[cnt]);
                     CountHistory=true;
                     continue;
                    }
                 }
               //---Start close minus in loss
               if((TotalProfitMinus[cnt]<0) && (TotalOrdersMinus[cnt]>0) && (TotalOrdersPlus[cnt]<TotalOrdersMinus[cnt]))
                 {
                  if(TotalProfitMinus[cnt]<=LevelLossCloseMinus[cnt])
                    {
                     if(TotalOrdersMinus[cnt]>0) ClosePairMinus(cnt,FirstTicketMinus[cnt]);
                     CountHistory=true;
                     continue;
                    }
                 }
              }//End if(TypeCloseInLoss==1)
            //---
           }
         //---------------------------------------------------------------------
         //Close orders in profit
         if((TypeOperation!=0) && (ExpertCloseBasketInLoss[cnt]==false) && (ExpertClosePlusInLoss[cnt]==false) && (ExpertCloseMinusInLoss[cnt]==false) && (ExpertCloseInPercentage[cnt]==false))
           {
            //---Close in ticket profit
            if(TypeCloseInProfit==0)
              {
               //---Start close plus in profit
               if((TotalProfitPlus[cnt]+TotalHedgeProfit[cnt]>0) && (TotalOrdersPlus[cnt]>0))
                 {
                  if((TotalProfitPlus[cnt]+TotalHedgeProfit[cnt]>=LevelProfitClosePlus[cnt]) || (ExpertClosePlusInProfit[cnt]==true))
                    {
                     if(TotalOrdersPlus[cnt]>0) ClosePairPlus(cnt,-1);
                     if(HedgePlusOrders[cnt]>0) ClosePairPlus(cnt,HedgePlusTicket[cnt]);
                     if(HedgeMinusOrders[cnt]>0) ClosePairMinus(cnt,HedgeMinusTicket[cnt]);
                     ExpertClosePlusInProfit[cnt]=true;
                     CountHistory=true;
                     continue;
                    }
                 }
               //---Start close minus in profit
               if((TotalProfitMinus[cnt]+TotalHedgeProfit[cnt]>0) && (TotalOrdersMinus[cnt]>0))
                 {
                  if((TotalProfitMinus[cnt]+TotalHedgeProfit[cnt]>=LevelProfitCloseMinus[cnt]) || (ExpertCloseMinusInProfit[cnt]==true))
                    {
                     if(TotalOrdersMinus[cnt]>0) ClosePairMinus(cnt,-1);
                     if(HedgePlusOrders[cnt]>0) ClosePairPlus(cnt,HedgePlusTicket[cnt]);
                     if(HedgeMinusOrders[cnt]>0) ClosePairMinus(cnt,HedgeMinusTicket[cnt]);
                     ExpertCloseMinusInProfit[cnt]=true;
                     CountHistory=true;
                     continue;
                    }
                 }
              }
            //---Close in basket profit
            if(TypeCloseInProfit==1)
              {
               //---Close all in basket profit
               if((TotalGroupProfit[cnt]+TotalHedgeProfit[cnt]>0) && (TotalOrdersPlus[cnt]+TotalOrdersMinus[cnt]+HedgePlusOrders[cnt]+HedgeMinusOrders[cnt]>0))
                 {
                  if((TotalGroupProfit[cnt]+TotalHedgeProfit[cnt]>=MathMax(LevelProfitClosePlus[cnt],LevelProfitCloseMinus[cnt])) || (ExpertCloseBasketInProfit[cnt]==true))
                    {
                     if(TotalOrdersPlus[cnt]>0) ClosePairPlus(cnt,-1);
                     if(TotalOrdersMinus[cnt]>0) ClosePairMinus(cnt,-1);
                     if(HedgePlusOrders[cnt]>0) ClosePairPlus(cnt,HedgePlusTicket[cnt]);
                     if(HedgeMinusOrders[cnt]>0) ClosePairMinus(cnt,HedgeMinusTicket[cnt]);
                     ExpertCloseBasketInProfit[cnt]=true;
                     CountHistory=true;
                     continue;
                    }
                 }
              }
            //---Close in hybrid mode
            if((TypeCloseInProfit==2) || ((OpenOrdersInLoss==2) && (TypeCloseInProfit!=3)))
              {
               //---Start close plus in profit (smaller ticket plus)
               if((TotalOrdersPlus[cnt]>0) && (TotalOrdersMinus[cnt]>0) && (TotalOrdersPlus[cnt]<=TotalOrdersMinus[cnt]))
                 {
                  if((TotalProfitPlus[cnt]>=LevelProfitClosePlus[cnt]) || (ExpertClosePlusInProfit[cnt]==true))
                    {
                     if(TotalOrdersPlus[cnt]>0) ClosePairPlus(cnt,-1);
                     ExpertClosePlusInProfit[cnt]=true;
                     CountHistory=true;
                     continue;
                    }
                 }
               //---Start close minus in profit (smaller ticket minus)
               if((TotalOrdersPlus[cnt]>0) && (TotalOrdersMinus[cnt]>0) && (TotalOrdersPlus[cnt]>=TotalOrdersMinus[cnt]))
                 {
                  if((TotalProfitMinus[cnt]>=LevelProfitCloseMinus[cnt]) || (ExpertCloseMinusInProfit[cnt]==true))
                    {
                     if(TotalOrdersMinus[cnt]>0) ClosePairMinus(cnt,-1);
                     ExpertCloseMinusInProfit[cnt]=true;
                     CountHistory=true;
                     continue;
                    }
                 }
               //---Set auto step
               if((OpenOrdersInLoss==2) && (TypeOperation!=2))
                 {
                  LevelProfitClosePlus[cnt]=(TotalLotPlus[cnt]*TargetCloseProfit*TickValuePair[cnt]);
                  LevelProfitCloseMinus[cnt]=(TotalLotMinus[cnt]*TargetCloseProfit*TickValuePair[cnt]);
                 }
               //---Close all in basket profit (all tickets)
               if((TotalGroupProfit[cnt]+TotalHedgeProfit[cnt]>0) && (TotalOrdersPlus[cnt]+TotalOrdersMinus[cnt]+HedgePlusOrders[cnt]+HedgeMinusOrders[cnt]>0))
                 {
                  if((TotalGroupProfit[cnt]+TotalHedgeProfit[cnt]>=MathMax(LevelProfitClosePlus[cnt],LevelProfitCloseMinus[cnt])) || (ExpertCloseBasketInProfit[cnt]==true))
                    {
                     if(TotalOrdersPlus[cnt]>0) ClosePairPlus(cnt,-1);
                     if(TotalOrdersMinus[cnt]>0) ClosePairMinus(cnt,-1);
                     if(HedgePlusOrders[cnt]>0) ClosePairPlus(cnt,HedgePlusTicket[cnt]);
                     if(HedgeMinusOrders[cnt]>0) ClosePairMinus(cnt,HedgeMinusTicket[cnt]);
                     ExpertCloseBasketInProfit[cnt]=true;
                     CountHistory=true;
                     continue;
                    }
                 }
              }
            //---Close in advanced mode
            if(TypeCloseInProfit==3)
              {
               //---Start close plus in profit (smaller ticket plus)
               if((TotalOrdersPlus[cnt]>0) && (TotalOrdersMinus[cnt]>0) && (TotalOrdersPlus[cnt]<=TotalOrdersMinus[cnt]))
                 {
                  if(((TotalProfitPlus[cnt]>=LevelProfitClosePlus[cnt]) && (TotalOrdersPlus[cnt]==1)) || (ExpertClosePlusInProfit[cnt]==true))
                    {
                     if(TotalOrdersPlus[cnt]>0) ClosePairPlus(cnt,-1);
                     ExpertClosePlusInProfit[cnt]=true;
                     CountHistory=true;
                     continue;
                    }
                 }
               //---Start close minus in profit (smaller ticket minus)
               if((TotalOrdersPlus[cnt]>0) && (TotalOrdersMinus[cnt]>0) && (TotalOrdersPlus[cnt]>=TotalOrdersMinus[cnt]))
                 {
                  if(((TotalProfitMinus[cnt]>=LevelProfitCloseMinus[cnt]) && (TotalOrdersMinus[cnt]==1)) || (ExpertCloseMinusInProfit[cnt]==true))
                    {
                     if(TotalOrdersMinus[cnt]>0) ClosePairMinus(cnt,-1);
                     ExpertCloseMinusInProfit[cnt]=true;
                     CountHistory=true;
                     continue;
                    }
                 }
               //---Set auto step
               if((OpenOrdersInLoss==2) && (TypeOperation!=2))
                 {
                  LevelProfitClosePlus[cnt]=(TotalLotPlus[cnt]*TargetCloseProfit*TickValuePair[cnt]);
                  LevelProfitCloseMinus[cnt]=(TotalLotMinus[cnt]*TargetCloseProfit*TickValuePair[cnt]);
                 }
               //---Close in hedge option one with more profits and one with more losses
               if(cnt>=NumberPairsTrade-1)
                 {
                  for(cnt100=0; cnt100<NumberPairsTrade; cnt100++)
                    {
                     //---Get more profits
                     if(TotalGroupProfit[cnt100]+TotalHedgeProfit[cnt100]>TotalMoreProfit)
                       {
                        TotalMoreProfit=TotalGroupProfit[cnt100]+TotalHedgeProfit[cnt100];
                        PairMoreProfit=cnt100;
                       }
                     //---Get more losses
                     if(TotalGroupProfit[cnt100]+TotalHedgeProfit[cnt100]<TotalMoreLoss)
                       {
                        TotalMoreLoss=TotalGroupProfit[cnt100]+TotalHedgeProfit[cnt100];
                        PairMoreLoss=cnt100;
                       }
                    }
                  //---Check to close orders
                  if((PairMoreProfit!=-1) && (PairMoreLoss!=-1))
                    {
                     if((TotalMoreProfit>TotalMoreLoss) && (TotalOrdersPlus[PairMoreProfit]>0) && (TotalOrdersMinus[PairMoreProfit]>0) && (TotalOrdersPlus[PairMoreLoss]>0) && (TotalOrdersMinus[PairMoreLoss]>0) && 
                        (TotalMoreProfit+TotalMoreLoss>=MathMax(MathMax(LevelProfitClosePlus[PairMoreProfit],LevelProfitCloseMinus[PairMoreProfit]),MathMax(LevelProfitClosePlus[PairMoreLoss],LevelProfitCloseMinus[PairMoreLoss]))))
                       {
                        for(cnt100=0; cnt100<NumberPairsTrade; cnt100++)
                          {
                           if((cnt100==PairMoreProfit) || (cnt100==PairMoreLoss))
                             {
                              if(TotalOrdersPlus[cnt100]>0) ClosePairPlus(cnt100,-1);
                              if(TotalOrdersMinus[cnt100]>0) ClosePairMinus(cnt100,-1);
                              if(HedgePlusOrders[cnt100]>0) ClosePairPlus(cnt100,HedgePlusTicket[cnt100]);
                              if(HedgeMinusOrders[cnt100]>0) ClosePairMinus(cnt100,HedgeMinusTicket[cnt100]);
                             }
                          }
                        CountHistory=true;
                        return;
                       }
                    }
                  //---Close all pairs
                  if(TotalProfitLoss>=MathMax(LevelProfitClosePlus[PairMoreLoss],LevelProfitCloseMinus[PairMoreLoss]))
                    {
                     for(cnt100=0; cnt100<NumberPairsTrade; cnt100++)
                       {
                        if(TotalOrdersPlus[cnt100]>0) ClosePairPlus(cnt100,-1);
                        if(TotalOrdersMinus[cnt100]>0) ClosePairMinus(cnt100,-1);
                        if(HedgePlusOrders[cnt100]>0) ClosePairPlus(cnt100,HedgePlusTicket[cnt100]);
                        if(HedgeMinusOrders[cnt100]>0) ClosePairMinus(cnt100,HedgeMinusTicket[cnt100]);
                       }
                     CountHistory=true;
                     return;
                    }
                  //---
                 }
               //---
              }
            //---
           }//end if(TypeOperation!=0)
         //---------------------------------------------------------------------
         //Calculate lot size per pair
         if(LotOrdersProgress==0)//Statical
           {
            MultiplierLotPlus[cnt]=1;
            MultiplierLotMinus[cnt]=1;
           }
         //---
         if(LotOrdersProgress==1)//Geometrical
           {
            MultiplierLotPlus[cnt]=TotalOrdersPlus[cnt]+1;
            MultiplierLotMinus[cnt]=TotalOrdersMinus[cnt]+1;
           }
         //---
         if(LotOrdersProgress==2)//Exponential
           {
            MultiplierLotPlus[cnt]=MathMax(1,MathPow(2,TotalOrdersPlus[cnt]));
            MultiplierLotMinus[cnt]=MathMax(1,MathPow(2,TotalOrdersMinus[cnt]));
           }
         //---------------------------------------------------------------------
         //Set lots for orders
         TotalLotsPair[cnt]=TotalLotPlus[cnt]+TotalLotMinus[cnt]+HedgePlusLot[cnt]+HedgeMinusLot[cnt];
         //---
         if((TypeOperation==1) || (TypeOperation==2))
           {
            if(TotalOrdersPlus[cnt]+TotalOrdersMinus[cnt]>0)
              {
               //---With out limits
               if((MaxLotPerPair==0) || (MaxMultiLotPerPair==0))
                 {
                  LotPlus[cnt]=NormalizeLot(FirstLotOfPair[cnt]*MultiplierLotPlus[cnt],cnt);
                  LotMinus[cnt]=NormalizeLot(FirstLotOfPair[cnt]*MultiplierLotMinus[cnt],cnt);
                 }
               //---
               if(MaxMultiLotPerPair>0)
                 {
                  LotPlus[cnt]=NormalizeLot(MathMin(LotSize*MaxMultiLotPerPair,FirstLotOfPair[cnt]*MultiplierLotPlus[cnt]),cnt);
                  LotMinus[cnt]=NormalizeLot(MathMin(LotSize*MaxMultiLotPerPair,FirstLotOfPair[cnt]*MultiplierLotMinus[cnt]),cnt);
                  LotHedgePlus[cnt]=NormalizeLot(MathMin(LotSize*MaxMultiLotPerPair,LotHedgePlus[cnt]),cnt);
                  LotHedgeMinus[cnt]=NormalizeLot(MathMin(LotSize*MaxMultiLotPerPair,LotHedgeMinus[cnt]),cnt);
                 }
               //---
               if(MaxLotPerPair>0)
                 {
                  LotPlus[cnt]=NormalizeLot(MathMax(MathMin(MaxLotPerPair-TotalLotsPair[cnt],FirstLotOfPair[cnt]*MultiplierLotPlus[cnt]),LotSize),cnt);
                  LotMinus[cnt]=NormalizeLot(MathMax(MathMin(MaxLotPerPair-TotalLotsPair[cnt],FirstLotOfPair[cnt]*MultiplierLotMinus[cnt]),LotSize),cnt);
                  LotHedgePlus[cnt]=NormalizeLot(MathMax(MathMin(MaxLotPerPair-TotalLotsPair[cnt],LotHedgePlus[cnt]),LotSize),cnt);
                  LotHedgeMinus[cnt]=NormalizeLot(MathMax(MathMin(MaxLotPerPair-TotalLotsPair[cnt],LotHedgeMinus[cnt]),LotSize),cnt);
                 }
               //---
              }
            else
              {
               //---
               if(MaxLotPerPair==0)
                 {
                  LotPlus[cnt]=NormalizeLot(LotSize,cnt);
                  LotMinus[cnt]=NormalizeLot(LotSize,cnt);
                 }
               //---
               if(MaxLotPerPair>0)
                 {
                  LotPlus[cnt]=NormalizeLot(MathMin(MaxLotPerPair,LotSize),cnt);
                  LotMinus[cnt]=NormalizeLot(MathMin(MaxLotPerPair,LotSize),cnt);
                 }
               //---
              }
            //---------------------------------------------------------------------
            //Check account margin and limit opened orders
            if(ReadySendPlus[cnt]==true) CheckMargin[cnt]+=AccountFreeMargin()-AccountFreeMarginCheck(SymbolPair[cnt],OP_BUY,LotPlus[cnt]);
            if(ReadySendMinus[cnt]==true) CheckMargin[cnt]+=AccountFreeMargin()-AccountFreeMarginCheck(SymbolPair[cnt],OP_SELL,LotMinus[cnt]);
            //---
            if(ReadySendHedgePlus[cnt]==true) CheckMargin[cnt]+=AccountFreeMargin()-AccountFreeMarginCheck(SymbolPair[cnt],OP_BUY,LotHedgePlus[cnt]);
            if(ReadySendHedgeMinus[cnt]==true) CheckMargin[cnt]+=AccountFreeMargin()-AccountFreeMarginCheck(SymbolPair[cnt],OP_SELL,LotHedgeMinus[cnt]);
            //---
            SumMargin=AccountFreeMargin()-CheckMargin[cnt];
            //---------------------------------------------------------------------
            //Open orders
            //---Margin is ok
            if(SumMargin>0.0)
              {
               if((OrdersTotal()<MaximumOrders-(PairsPerGroup)) || (MaximumOrders==0))
                 {
                  //---Send plus
                  if(ReadySendPlus[cnt]==true) OpenPairPlus(cnt,-1);
                  //---Send minus
                  if(ReadySendMinus[cnt]==true) OpenPairMinus(cnt,-1);
                  //---Hedge orders
                  if(EmergencyValue[cnt][2]>0)
                    {
                     if(ReadySendHedgePlus[cnt]==true) OpenPairPlus(cnt,1);
                     if(ReadySendHedgeMinus[cnt]==true) OpenPairMinus(cnt,1);
                    }
                 }
               else
                 {
                  CommentWarning=true;
                  WarningPrint=StringConcatenate("Orders are in limit (",OrdersTotal(),"/",MaximumOrders,")");
                 }
              }
            //---Low margin
            if(SumMargin<=0.0)
              {
               CommentWarning=true;
               WarningPrint=StringConcatenate("  Free margin is low (",DoubleToStr(SumMargin,0),")");
              }
           }//end if((TypeWorking==2)||(TypeWorking==0))
        }//end if((NewBar[cnt]==true)&&(MarketIsOpen==true))
      //---------------------------------------------------------------------
      //Close and stop
      if(TypeOperation==3)
        {
         //---There are not open orders
         if(TotalOrdersPlus[cnt]+TotalOrdersMinus[cnt]+HedgePlusOrders[cnt]+HedgeMinusOrders[cnt]==0)
           {
            if(cnt==NumberPairsTrade-1)
              {
               Comment(
                       "\n                  ",WindowExpertName()+
                       "\n\n           ~ Have Close All Orders ~ "+
                       "\n\n           ~ History Orders Results ~ "+
                       "\n  Pips: "+DoubleToStr(HistoryTotalPips,2)+" || Orders: "+DoubleToStr(HistoryTotalTrades,0)+" || PnL: "+DoubleToStr(HistoryTotalProfitLoss,2)
                       );
              }
           }
         //---There are open orders
         if(TotalOrdersPlus[cnt]+TotalOrdersMinus[cnt]+HedgePlusOrders[cnt]+HedgeMinusOrders[cnt]>0)
           {
            Comment("\n                  ",WindowExpertName(),
                    "\n\n           ~ Wait For Close Orders ~ ");
            //---Close orders
            if(TotalOrdersPlus[cnt]>0) ClosePairPlus(cnt,-1);
            if(TotalOrdersMinus[cnt]>0) ClosePairMinus(cnt,-1);
            if(HedgePlusOrders[cnt]>0) ClosePairPlus(cnt,HedgePlusTicket[cnt]);
            if(HedgeMinusOrders[cnt]>0) ClosePairMinus(cnt,HedgeMinusTicket[cnt]);
           }//end if(TotalOrdersPlus+TotalOrdersMinus>0)
        }//end if(StopAndClose==true)
     }//end for(i=0; i<NumberPairsTrade; i++)
//---------------------------------------------------------------------
//Call comment function
//if((TypeOperation<3)&&(!IsTesting())&&(!IsOptimization())) CommentChart();
   if(TypeOperation<3) CommentChart();
//---------------------------------------------------------------------
  }
//====================================================================================================================================================//
//Normalize Lots
//====================================================================================================================================================//
double NormalizeLot(double LotsSize,int PairCount)
  {
//---------------------------------------------------------------------
   if(IsConnected())
      return(MathMin(MathMax((MathRound(LotsSize/MarketInfo(SymbolPair[PairCount],MODE_LOTSTEP))*MarketInfo(SymbolPair[PairCount],MODE_LOTSTEP)),MarketInfo(SymbolPair[PairCount],MODE_MINLOT)),MarketInfo(SymbolPair[PairCount],MODE_MAXLOT)));
   else
      return(LotsSize);
//---------------------------------------------------------------------
  }
//====================================================================================================================================================//
//Open plus orders
//====================================================================================================================================================//
void OpenPairPlus(int PairOpen,int TypeOrder)
  {
//---------------------------------------------------------------------
   double PriceOpen=0;
   double StopLoss=0;
   double TakeProfit=0;
   double LotSizeOrder=0;
   color ColorOrder=0;
   int ID=0;
   double FreeMargin=0;
//---------------------------------------------------------------------
//Count free margin
   if(AccountFreeMargin()>AccountFreeMarginCheck(SymbolPair[PairOpen],OP_BUY,LotPlus[PairOpen])) FreeMargin=AccountFreeMargin()-AccountFreeMarginCheck(SymbolPair[PairOpen],OP_BUY,LotPlus[PairOpen]);
   if(AccountFreeMargin()<AccountFreeMarginCheck(SymbolPair[PairOpen],OP_BUY,LotPlus[PairOpen])) FreeMargin=AccountFreeMargin()+(AccountFreeMargin()-AccountFreeMarginCheck(SymbolPair[PairOpen],OP_BUY,LotPlus[PairOpen]));
//---------------------------------------------------------------------
   if(FreeMargin>=0)
     {
      CntTry=0;
      while(TRUE)
        {
         CntTry++;
         PriceOpen=MarketInfo(SymbolPair[PairOpen],MODE_ASK);
         ColorOrder=clrBlue;
         //---Normal order
         if(TypeOrder==-1)
           {
            LotSizeOrder=LotPlus[PairOpen];
            ID=MagicNo;
           }
         //---Hedge order
         if(TypeOrder==1)
           {
            LotSizeOrder=LotHedgePlus[PairOpen];
            ID=HedgeID;
           }
         //---
         TicketNo[PairOpen]=OrderSend(SymbolPair[PairOpen],OP_BUY,LotSizeOrder,PriceOpen,MaxSlippage,StopLoss,TakeProfit,CommentsEA,ID,0,ColorOrder);
         //---
         if(TicketNo[PairOpen]>0)
           {
            if(PrintLogReport==true) Print("Open Plus",SymbolPair[PairOpen]," - TicketNo: ",TicketNo[PairOpen]);
            break;
           }
         //---
         Sleep(100);
         if(CntTry>3) break;
         RefreshRates();
        }
     }
   else
     {
      CommentWarning=true;
      WarningPrint=StringConcatenate("  Free margin is low (",DoubleToStr(FreeMargin),")");
      Print(WarningPrint);
     }
//---------------------------------------------------------------------
  }
//====================================================================================================================================================//
//Open minus orders
//====================================================================================================================================================//
void OpenPairMinus(int PairOpen,int TypeOrder)
  {
//---------------------------------------------------------------------
   double PriceOpen=0;
   double StopLoss=0;
   double TakeProfit=0;
   double LotSizeOrder=0;
   color ColorOrder=0;
   int ID=0;
   double FreeMargin=0;
//---------------------------------------------------------------------
//Count free margin
   if(AccountFreeMargin()>AccountFreeMarginCheck(SymbolPair[PairOpen],OP_SELL,LotMinus[PairOpen])) FreeMargin=AccountFreeMargin()-AccountFreeMarginCheck(SymbolPair[PairOpen],OP_SELL,LotMinus[PairOpen]);
   if(AccountFreeMargin()<AccountFreeMarginCheck(SymbolPair[PairOpen],OP_SELL,LotMinus[PairOpen])) FreeMargin=AccountFreeMargin()+(AccountFreeMargin()-AccountFreeMarginCheck(SymbolPair[PairOpen],OP_SELL,LotMinus[PairOpen]));
//---------------------------------------------------------------------
   if(FreeMargin>=0)
     {
      CntTry=0;
      while(TRUE)
        {
         CntTry++;
         PriceOpen=MarketInfo(SymbolPair[PairOpen],MODE_BID);
         ColorOrder=clrRed;
         //---Normal order
         if(TypeOrder==-1)
           {
            LotSizeOrder=LotMinus[PairOpen];
            ID=MagicNo;
           }
         //---Hedge order
         if(TypeOrder==1)
           {
            LotSizeOrder=LotHedgeMinus[PairOpen];
            ID=HedgeID;
           }
         //---
         TicketNo[PairOpen]=OrderSend(SymbolPair[PairOpen],OP_SELL,LotSizeOrder,PriceOpen,MaxSlippage,StopLoss,TakeProfit,CommentsEA,ID,0,ColorOrder);
         //---
         if(TicketNo[PairOpen]>0)
           {
            if(PrintLogReport==true) Print("Open Minus: ",SymbolPair[PairOpen]," || TicketNo: ",TicketNo[PairOpen]);
            break;
           }
         //---
         Sleep(100);
         if(CntTry>3) break;
         RefreshRates();
        }
     }
   else
     {
      CommentWarning=true;
      WarningPrint=StringConcatenate("  Free margin is low (",DoubleToStr(FreeMargin,0),")");
      Print(WarningPrint);
     }
//---------------------------------------------------------------------
  }
//====================================================================================================================================================//
//Close plus orders
//====================================================================================================================================================//
void ClosePairPlus(int PairClose,int TicketClose)
  {
//---------------------------------------------------------------------
   double PriceClose=0;
   color ColorOrder=0;
   int ClosedOrders=0;
//---------------------------------------------------------------------
//No FIFO rules
   for(i=OrdersTotal()-1; i>=0; i--)//Last to first
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
        {
         if(((OrderMagicNumber()==MagicNo) || (OrderMagicNumber()==HedgeID)) && (OrderSymbol()==SymbolPair[PairClose]) && (OrderType()==OP_BUY))
           {
            if((OrderTicket()==TicketClose) || ((TicketClose==-1) && (OrderMagicNumber()==MagicNo)))
              {
               //---
               PriceClose=MarketInfo(OrderSymbol(),MODE_BID);
               ColorOrder=clrPowderBlue;
               //---
               CntTry=0;
               while(TRUE)
                 {
                  CntTry++;
                  TicketNo[PairClose]=OrderClose(OrderTicket(),OrderLots(),PriceClose,MaxSlippage,ColorOrder);
                  //---
                  if(TicketNo[PairClose]>0)
                    {
                     if(PrintLogReport==true) Print("Close Plus: ",SymbolPair[PairClose]," || TicketNo: ",OrderTicket());
                     break;
                    }
                  //---
                  Sleep(100);
                  if(CntTry>3) break;
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
void ClosePairMinus(int PairClose,int TicketClose)
  {
//---------------------------------------------------------------------
   double PriceClose=0;
   color ColorOrder=0;
   int ClosedOrders=0;
//---------------------------------------------------------------------
//No FIFO rules
   for(i=OrdersTotal()-1; i>=0; i--)//Last to first
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
        {
         if(((OrderMagicNumber()==MagicNo) || (OrderMagicNumber()==HedgeID)) && (OrderSymbol()==SymbolPair[PairClose]) && (OrderType()==OP_SELL))
           {
            if((OrderTicket()==TicketClose) || ((TicketClose==-1) && (OrderMagicNumber()==MagicNo)))
              {
               //---
               PriceClose=MarketInfo(OrderSymbol(),MODE_ASK);
               ColorOrder=clrPink;
               //---
               CntTry=0;
               while(TRUE)
                 {
                  CntTry++;
                  TicketNo[PairClose]=OrderClose(OrderTicket(),OrderLots(),PriceClose,MaxSlippage,ColorOrder);
                  //---
                  if(TicketNo[PairClose]>0)
                    {
                     if(PrintLogReport==true) Print("Close Minus: ",SymbolPair[PairClose]," || TicketNo: ",OrderTicket());
                     break;
                    }
                  //---
                  Sleep(100);
                  if(CntTry>3) break;
                  RefreshRates();
                 }
              }
           }
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
   TotalProfitPlus[PairGet]=0;
   TotalProfitMinus[PairGet]=0;
   FirstLotPlus[PairGet]=0;
   FirstLotMinus[PairGet]=0;
   LastLotPlus[PairGet]=0;
   LastLotMinus[PairGet]=0;
   FirstTicketPlus[PairGet]=0;
   FirstTicketMinus[PairGet]=0;
   LastTicketPlus[PairGet]=0;
   LastTicketMinus[PairGet]=0;
   FirstProfitPlus[PairGet]=0;
   FirstProfitMinus[PairGet]=0;
   LastProfitPlus[PairGet]=0;
   LastProfitMinus[PairGet]=0;
   TotalOrdersPlus[PairGet]=0;
   TotalOrdersMinus[PairGet]=0;
   TotalGroupProfit[PairGet]=0;
   TotalOrdersMinus[PairGet]=0;
   FirstLotOfPair[PairGet]=0;
   TotalLotPlus[PairGet]=0;
   TotalLotMinus[PairGet]=0;
   TotalCommissionPlus[PairGet]=0;
   TotalCommissionMinus[PairGet]=0;
//---------------------------------------------------------------------
//Get orders informations
   if(OrdersTotal()>0)
     {
      //---Last to first
      for(i=OrdersTotal()-1; i>=0; i--)
        {
         //---Start check trades
         if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
           {
            CountAllOpenedOrders++;
            if(OrderMagicNumber()==MagicNo)
              {
               //---Plus pair 1
               if((OrderType()==OP_BUY) && (OrderSymbol()==SymbolPair[PairGet]))
                 {
                  FirstLotOfPair[PairGet]=OrderLots();
                  TotalProfitPlus[PairGet]+=OrderProfit()+OrderSwap()+OrderCommission();
                  TotalCommissionPlus[PairGet]+=MathAbs(OrderCommission()+OrderSwap());
                  FirstLotPlus[PairGet]=OrderLots();
                  TotalLotPlus[PairGet]+=OrderLots();
                  TotalOrdersPlus[PairGet]++;
                  FirstTicketPlus[PairGet]=OrderTicket();
                  FirstProfitPlus[PairGet]=OrderProfit()+OrderSwap()+OrderCommission();
                  if(LastTicketPlus[PairGet]==0) LastTicketPlus[PairGet]=OrderTicket();
                  if(LastLotPlus[PairGet]==0) LastLotPlus[PairGet]=OrderLots();
                  if(LastProfitPlus[PairGet]==0) LastProfitPlus[PairGet]=OrderProfit()+OrderSwap()+OrderCommission();
                 }
               //---Minus pair 1
               if((OrderType()==OP_SELL) && (OrderSymbol()==SymbolPair[PairGet]))
                 {
                  FirstLotOfPair[PairGet]=OrderLots();
                  TotalProfitMinus[PairGet]+=OrderProfit()+OrderSwap()+OrderCommission();
                  TotalCommissionMinus[PairGet]+=MathAbs(OrderCommission()+OrderSwap());
                  FirstLotMinus[PairGet]=OrderLots();
                  TotalLotMinus[PairGet]+=OrderLots();
                  TotalOrdersMinus[PairGet]++;
                  FirstTicketMinus[PairGet]=OrderTicket();
                  FirstProfitMinus[PairGet]=OrderProfit()+OrderSwap()+OrderCommission();
                  if(LastTicketMinus[PairGet]==0) LastTicketMinus[PairGet]=OrderTicket();
                  if(LastLotMinus[PairGet]==0) LastLotMinus[PairGet]=OrderLots();
                  if(LastProfitMinus[PairGet]==0) LastProfitMinus[PairGet]=OrderProfit()+OrderSwap()+OrderCommission();
                 }
              }
            //---
           }
        }
      if(CountAllOpenedOrders!=OrdersTotal()) return;//Pass again orders
     }//end if(OrdersTotal()>0)
//---------------------------------------------------------------------
//Processing orders informations
   TotalGroupProfit[PairGet]=TotalProfitPlus[PairGet]+TotalProfitMinus[PairGet];
//---------------------------------------------------------------------
  }
//====================================================================================================================================================//
//Count orders for hedge mode resuluts
//====================================================================================================================================================//
void CountOrdersEmergencyMode(int PairGet)
  {
//---------------------------------------------------------------------
   LastPlusTotalProfit[PairGet]=0;
   LastMinusTotalProfit[PairGet]=0;
   FirstPlusTotalProfit[PairGet]=0;
   FirstMinusTotalProfit[PairGet]=0;
   HedgePlusOrders[PairGet]=0;
   HedgeMinusOrders[PairGet]=0;
   HedgePlusTicket[PairGet]=0;
   HedgeMinusTicket[PairGet]=0;
   HedgePlusProfit[PairGet]=0;
   HedgeMinusProfit[PairGet]=0;
   HedgePlusLot[PairGet]=0;
   HedgeMinusLot[PairGet]=0;
   TotalHedgeProfit[PairGet]=0;
   CountPlusEmergency[PairGet]=0;
   CountMinusEmergency[PairGet]=0;
//---------------------------------------------------------------------
//Get orders informations
   if(OrdersTotal()>0)
     {
      //---Last to first
      for(i=OrdersTotal()-1; i>=0; i--)
        {
         //---Reset value
         LastPlusInProfit[PairGet][i]=0;
         LastMinusInProfit[PairGet][i]=0;
         LastTicketPlusInProfit[PairGet][i]=-1;
         LastTicketMinusInProfit[PairGet][i]=-1;
         //---Start check trades
         if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
           {
            //---------------------------------------------------------------------
            //Partial close
            if(EmergencyValue[PairGet][1]>0)
              {
               //---Get for partial close plus
               if((OrderType()==OP_BUY) && (OrderSymbol()==SymbolPair[PairGet]) && (OrderMagicNumber()==MagicNo))
                 {
                  if((LastPlusInProfit[PairGet][i]==0) && (OrderProfit()+OrderSwap()+OrderCommission()>0))
                    {
                     LastPlusInProfit[PairGet][i]=OrderProfit()+OrderSwap()+OrderCommission();
                     LastTicketPlusInProfit[PairGet][i]=OrderTicket();
                     LastPlusTotalProfit[PairGet]+=LastPlusInProfit[PairGet][i];
                    }
                 }
               //---Get for partial close minus
               if((OrderType()==OP_SELL) && (OrderSymbol()==SymbolPair[PairGet]) && (OrderMagicNumber()==MagicNo))
                 {
                  if((LastMinusInProfit[PairGet][i]==0) && (OrderProfit()+OrderSwap()+OrderCommission()>0))
                    {
                     LastMinusInProfit[PairGet][i]=OrderProfit()+OrderSwap()+OrderCommission();
                     LastTicketMinusInProfit[PairGet][i]=OrderTicket();
                     LastMinusTotalProfit[PairGet]+=LastMinusInProfit[PairGet][i];
                    }
                 }
              }
           }
        }
      //---First to last
      for(i=0; i<OrdersTotal(); i++)
        {
         //---Reset value
         FirstPlusInProfit[PairGet][i]=0;
         FirstMinusInProfit[PairGet][i]=0;
         FirstTicketPlusInProfit[PairGet][i]=-1;
         FirstTicketMinusInProfit[PairGet][i]=-1;
         //---Start check trades
         if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
           {
            //---------------------------------------------------------------------
            //Hedge orders
            if(EmergencyValue[PairGet][2]>0)
              {
               //---Get for normal orders plus
               if((OrderType()==OP_BUY) && (OrderSymbol()==SymbolPair[PairGet]) && (OrderMagicNumber()==MagicNo))
                 {
                  CountPlusEmergency[PairGet]++;
                  if((FirstPlusInProfit[PairGet][i]==0) && (CountPlusEmergency[PairGet]<=NormalizeDouble(TotalOrdersPlus[PairGet]/DivisorOrders,0)))
                    {
                     FirstPlusInProfit[PairGet][i]=OrderProfit()+OrderSwap()+OrderCommission();
                     FirstTicketPlusInProfit[PairGet][i]=OrderTicket();
                     FirstPlusTotalProfit[PairGet]+=FirstPlusInProfit[PairGet][i];
                    }
                 }
               //---Get hedge orders plus
               if((OrderType()==OP_BUY) && (OrderSymbol()==SymbolPair[PairGet]) && (OrderMagicNumber()==HedgeID))
                 {
                  HedgePlusOrders[PairGet]++;
                  HedgePlusTicket[PairGet]=OrderTicket();
                  HedgePlusProfit[PairGet]=OrderProfit()+OrderSwap()+OrderCommission();
                  HedgePlusLot[PairGet]=OrderLots();
                 }
               //---Get for nomral orders minus
               if((OrderType()==OP_SELL) && (OrderSymbol()==SymbolPair[PairGet]) && (OrderMagicNumber()==MagicNo))
                 {
                  CountMinusEmergency[PairGet]++;
                  if((FirstMinusInProfit[PairGet][i]==0) && (CountMinusEmergency[PairGet]<=NormalizeDouble(TotalOrdersMinus[PairGet]/DivisorOrders,0)))
                    {
                     FirstMinusInProfit[PairGet][i]=OrderProfit()+OrderSwap()+OrderCommission();
                     FirstTicketMinusInProfit[PairGet][i]=OrderTicket();
                     FirstMinusTotalProfit[PairGet]+=FirstMinusInProfit[PairGet][i];
                    }
                 }
               //---Get hedge orders minus
               if((OrderType()==OP_SELL) && (OrderSymbol()==SymbolPair[PairGet]) && (OrderMagicNumber()==HedgeID))
                 {
                  HedgeMinusOrders[PairGet]++;
                  HedgeMinusTicket[PairGet]=OrderTicket();
                  HedgeMinusProfit[PairGet]=OrderProfit()+OrderSwap()+OrderCommission();
                  HedgeMinusLot[PairGet]=OrderLots();
                 }
              }
           }
        }
     }//end if(OrdersTotal()>0)
//---------------------------------------------------------------------
//Processing orders informations 
   TotalHedgeProfit[PairGet]=HedgePlusProfit[PairGet]+HedgeMinusProfit[PairGet];
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
   HistoryTotalTrades=0;
   HistoryTotalProfitLoss=0;
   HistoryTotalPips=0;
//---------------------------------------------------------------------
   if(OrdersHistoryTotal()>0)
     {
      for(i=OrdersHistoryTotal()-1; i>=0; i--)
        {
         if(OrderSelect(i,SELECT_BY_POS,MODE_HISTORY))
           {
            CountAllHistoryOrders++;
            //---
            if((OrderMagicNumber()==MagicNo) || (OrderMagicNumber()==HedgeID))
              {
               //---------------------------------------------------------------------
               //Count plus orders
               if((OrderType()==OP_BUY) && (OrderSymbol()==SymbolPair[PairGet]))
                 {
                  HistoryPlusOrders[PairGet]++;
                  HistoryPlusProfit[PairGet]+=OrderProfit()+OrderCommission()+OrderSwap();
                 }
               //---------------------------------------------------------------------
               //Count minus orders
               if((OrderType()==OP_SELL) && (OrderSymbol()==SymbolPair[PairGet]))
                 {
                  HistoryMinusOrders[PairGet]++;
                  HistoryMinusProfit[PairGet]+=OrderProfit()+OrderCommission()+OrderSwap();
                 }
               //---------------------------------------------------------------------
               //History total results
               HistoryTotalTrades++;
               HistoryTotalProfitLoss+=OrderProfit()+OrderCommission()+OrderSwap();
               //---------------------------------------------------------------------
               //Count pips
               if(OrderType()==OP_BUY) HistoryTotalPips+=(OrderClosePrice()-OrderOpenPrice())/(MarketInfo(OrderSymbol(),MODE_POINT)*MultiplierPoint);
               if(OrderType()==OP_SELL) HistoryTotalPips+=(OrderOpenPrice()-OrderClosePrice())/(MarketInfo(OrderSymbol(),MODE_POINT)*MultiplierPoint);
               //---------------------------------------------------------------------
              }
           }
         if(CountAllHistoryOrders!=OrdersHistoryTotal()) CountHistory=true;//Pass again orders
        }
     }
//---------------------------------------------------------------------
  }
//====================================================================================================================================================//
//Background for comments
//====================================================================================================================================================//
void ChartBackground(string StringName,color ImageColor,int TypeBorder,bool InBackGround,int Xposition,int Yposition,int Xsize,int Ysize)
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
   ObjectSetInteger(0,StringName,OBJPROP_SELECTABLE,FALSE);
   ObjectSetInteger(0,StringName,OBJPROP_SELECTED,FALSE);
   ObjectSetInteger(0,StringName,OBJPROP_HIDDEN,TRUE);
   ObjectSetInteger(0,StringName,OBJPROP_ZORDER,0);
//---------------------------------------------------------------------
  }
//====================================================================================================================================================//
//Display Text/image
//====================================================================================================================================================//
void DisplayText(string StringName,string Image,int FontSize,string TypeImage,color FontColor,int Xposition,int Yposition)
  {
//---------------------------------------------------------------------
   ObjectCreate(StringName,OBJ_LABEL,0,0,0);
   ObjectSet(StringName,OBJPROP_CORNER,0);
   ObjectSet(StringName,OBJPROP_BACK,FALSE);
   ObjectSet(StringName,OBJPROP_XDISTANCE,Xposition);
   ObjectSet(StringName,OBJPROP_YDISTANCE,Yposition);
   ObjectSet(StringName,OBJPROP_SELECTABLE,FALSE);
   ObjectSet(StringName,OBJPROP_SELECTED,FALSE);
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
   if((OpenOrdersInLoss==1) && (StepOpenNextOrders<=0) && (WrongSet==false))
     {
      Comment("\n "+StringOrdersEA+
              "\n\n\nStepOpenNextOrders parameter not correct ("+DoubleToStr(StepOpenNextOrders,2)+")"
              "\n\nPlease insert a value greater than 0");
      Print(" # "+WindowExpertName()+" # "+"StepOpenNextOrders parameter not correct ("+DoubleToStr(StepOpenNextOrders,2)+")");
      WrongSet=true;
      WarningMessage=MessageBox(" # "+WindowExpertName()+" # "+"StepOpenNextOrders parameter not correct ("+DoubleToStr(StepOpenNextOrders,2)+"), please insert a value greater than 0","RISK DISCLAIMER");
     }
//---------------------------------------------------------------------
//Check profit close value
   if((TargetCloseProfit<=0) && (WrongSet==false))
     {
      Comment("\n "+StringOrdersEA+
              "\n\n\nTargetCloseProfit parameter not correct ("+DoubleToStr(TargetCloseProfit,2)+")"
              "\n\nPlease insert a value greater than 0");
      Print(" # "+WindowExpertName()+" # "+"TargetCloseProfit parameter not correct ("+DoubleToStr(TargetCloseProfit,2)+")");
      WrongSet=true;
      WarningMessage=MessageBox(" # "+WindowExpertName()+" # "+"TargetCloseProfit parameter not correct ("+DoubleToStr(TargetCloseProfit,2)+"), please insert a value greater than 0","RISK DISCLAIMER");
     }
//---------------------------------------------------------------------
//Check loss close value
   if((TypeCloseInLoss<2) && (TargetCloseLoss<=0) && (WrongSet==false))
     {
      Comment("\n "+StringOrdersEA+
              "\n\n\nTargetCloseLoss parameter not correct ("+DoubleToStr(TargetCloseLoss,2)+")"
              "\n\nPlease insert a value greater than 0");
      Print(" # "+WindowExpertName()+" # "+"TargetCloseLoss parameter not correct ("+DoubleToStr(TargetCloseLoss,2)+")");
      WrongSet=true;
      WarningMessage=MessageBox(" # "+WindowExpertName()+" # "+"TargetCloseLoss parameter not correct ("+DoubleToStr(TargetCloseLoss,2)+"), please insert a value greater than 0","RISK DISCLAIMER");
     }
//---------------------------------------------------------------------
//Check time window value
   if(UseTimeWindow==true)
     {
      //---Start time format
      if(((StringSubstr(TimeStartTrade,2,1)!=":") || (StringSubstr(TimeStartTrade,5,1)!=":") || (StringLen(TimeStartTrade)!=8)) && (WrongSet==false))
        {
         Comment("\n "+StringOrdersEA+
                 "\n\n\nTimeStartTrade parameter not correct ("+TimeStartTrade+")"
                 "\n\nplease insert a value with format HH:MM:SS");
         Print(" # "+WindowExpertName()+" # "+"TimeStartTrade parameter not correct ("+TimeStartTrade+")");
         WrongSet=true;
         WarningMessage=MessageBox(" # "+WindowExpertName()+" # "+"TimeStartTrade parameter not correct ("+TimeStartTrade+"), please insert a value with format HH:MM:SS","RISK DISCLAIMER");
        }
      //---Stop time format
      if(((StringSubstr(TimeStopTrade,2,1)!=":") || (StringSubstr(TimeStopTrade,5,1)!=":") || (StringLen(TimeStopTrade)!=8)) && (WrongSet==false))
        {
         Comment("\n "+StringOrdersEA+
                 "\n\n\nTimeStopTrade parameter not correct ("+TimeStopTrade+")"
                 "\n\nplease insert a value with format HH:MM:SS");
         Print(" # "+WindowExpertName()+" # "+"TimeStopTrade parameter not correct ("+TimeStopTrade+")");
         WrongSet=true;
         WarningMessage=MessageBox(" # "+WindowExpertName()+" # "+"TimeStopTrade parameter not correct ("+TimeStopTrade+"), please insert a value with format HH:MM:SS","RISK DISCLAIMER");
        }
      //---Start time value
      if((StrToInteger(StringSubstr(TimeStartTrade,0,2))>23) && (WrongSet==false))//Hours
        {
         Comment("\n "+StringOrdersEA+
                 "\n\n\nTimeStartTrade parameter not correct ("+TimeStartTrade+")"
                 "\n\nplease insert a value for hour between 00 and 23");
         Print(" # "+WindowExpertName()+" # "+"TimeStartTrade parameter not correct ("+TimeStartTrade+")");
         WrongSet=true;
         WarningMessage=MessageBox(" # "+WindowExpertName()+" # "+"TimeStartTrade parameter not correct ("+TimeStartTrade+"), please insert a value for hour between 00 and 23","RISK DISCLAIMER");
        }
      //---
      if((StrToInteger(StringSubstr(TimeStartTrade,3,5))>59) && (WrongSet==false))//Minutes
        {
         Comment("\n "+StringOrdersEA+
                 "\n\n\nTimeStartTrade parameter not correct ("+TimeStartTrade+")"
                 "\n\nplease insert a value for minutes between 00 and 59");
         Print(" # "+WindowExpertName()+" # "+"TimeStartTrade parameter not correct ("+TimeStartTrade+")");
         WrongSet=true;
         WarningMessage=MessageBox(" # "+WindowExpertName()+" # "+"TimeStartTrade parameter not correct ("+TimeStartTrade+"), please insert a value for minutes between 00 and 59","RISK DISCLAIMER");
        }
      //---
      if((StrToInteger(StringSubstr(TimeStartTrade,6,8))>59) && (WrongSet==false))//Seconds
        {
         Comment("\n "+StringOrdersEA+
                 "\n\n\nTimeStartTrade parameter not correct ("+TimeStartTrade+")"
                 "\n\nplease insert a value for seconds between 00 and 59");
         Print(" # "+WindowExpertName()+" # "+"TimeStartTrade parameter not correct ("+TimeStartTrade+")");
         WrongSet=true;
         WarningMessage=MessageBox(" # "+WindowExpertName()+" # "+"TimeStartTrade parameter not correct ("+TimeStartTrade+"), please insert a value for seconds between 00 and 59","RISK DISCLAIMER");
        }
      //---Stop time value
      if((StrToInteger(StringSubstr(TimeStopTrade,0,2))>23) && (WrongSet==false))//Hours
        {
         Comment("\n "+StringOrdersEA+
                 "\n\n\nTimeStopTrade parameter not correct ("+TimeStopTrade+")"
                 "\n\nplease insert a value for hour between 00 and 23");
         Print(" # "+WindowExpertName()+" # "+"TimeStopTrade parameter not correct ("+TimeStopTrade+")");
         WrongSet=true;
         WarningMessage=MessageBox(" # "+WindowExpertName()+" # "+"TimeStopTrade parameter not correct ("+TimeStopTrade+"), please insert a value for hour between 00 and 23","RISK DISCLAIMER");
        }
      //---
      if((StrToInteger(StringSubstr(TimeStopTrade,3,5))>59) && (WrongSet==false))//Minutes
        {
         Comment("\n "+StringOrdersEA+
                 "\n\n\nTimeStopTrade parameter not correct ("+TimeStopTrade+")"
                 "\n\nplease insert a value for minutes between 00 and 59");
         Print(" # "+WindowExpertName()+" # "+"TimeStopTrade parameter not correct ("+TimeStopTrade+")");
         WrongSet=true;
         WarningMessage=MessageBox(" # "+WindowExpertName()+" # "+"TimeStopTrade parameter not correct ("+TimeStopTrade+"), please insert a value for minutes between 00 and 59","RISK DISCLAIMER");
        }
      //---
      if((StrToInteger(StringSubstr(TimeStopTrade,6,8))>59) && (WrongSet==false))//Seconds
        {
         Comment("\n "+StringOrdersEA+
                 "\n\n\nTimeStopTrade parameter not correct ("+TimeStopTrade+")"
                 "\n\nplease insert a value for seconds between 00 and 59");
         Print(" # "+WindowExpertName()+" # "+"TimeStopTrade parameter not correct ("+TimeStopTrade+")");
         WrongSet=true;
         WarningMessage=MessageBox(" # "+WindowExpertName()+" # "+"TimeStopTrade parameter not correct ("+TimeStopTrade+"), please insert a value for seconds between 00 and 59","RISK DISCLAIMER");
        }
     }
//---------------------------------------------------------------------
  }
//====================================================================================================================================================//
//Comments in the chart
//====================================================================================================================================================//
void CommentChart()
  {
//---------------------------------------------------------------------
   datetime DiffTime;
   color TextColor=clrNONE;
   double LevelCloseInLossPlus[99];
   double LevelCloseInLossMinus[99];
   double ShowMaxProfit=0;
   double ShowMinProfit=0;
   string FirstLineStr;
   string CloseProfitStr;
   string CloseLossStr;
   string SpreadStr;
   string StepNextStr;
   string LotOrdersStr;
   string PairsInfo;
   double Hopen;
   double Mopen;
   double Sopen;
   string HopenStr;
   string MopenStr;
   string SopenStr;
   string EmergencyStr;
   string ShowInfoOfPairs;
   string SetSpace="";
   int Hdigits=1;
   int Mdigits=1;
   int Sdigits=1;
   int PosX=0;
   int PosY=0;
   int FileHandle;
//---------------------------------------------------------------------
//Reset values
   ArrayInitialize(LevelCloseInLossPlus,0);
   ArrayInitialize(LevelCloseInLossMinus,0);
   TotalOrders=0;
   TotalProfitLoss=0;
   TotalLots=0;
//---------------------------------------------------------------------
//First line comment
   if(TypeOperation==0) FirstLineStr=StringConcatenate("Expert Is In Stand By Mode");
   if(TypeOperation==1) FirstLineStr=StringConcatenate("Expert Is Ready To Open/Close Orders");
   if(TypeOperation==2) FirstLineStr=StringConcatenate("Expert Wait Close In Profit And Stop");
   if(TypeOperation==3) FirstLineStr=StringConcatenate("Expert Close Immediately All Orders");
   if(CommentWarning==true) FirstLineStr=StringConcatenate("Warning: ",WarningPrint);
//---------------------------------------------------------------------
//Emergency mode string
   if(UseEmergencyMode==true) EmergencyStr="Use Emergency Mode";
   if(UseEmergencyMode==false) EmergencyStr="Not Use Emergency";
//---------------------------------------------------------------------
//Close mode
   if(TypeCloseInProfit==0) CloseProfitStr="Single Ticket ("+DoubleToStr(TargetCloseProfit,2)+")";
   if(TypeCloseInProfit==1) CloseProfitStr="Basket Ticket ("+DoubleToStr(TargetCloseProfit,2)+")";
   if(TypeCloseInProfit==2) CloseProfitStr="Hybrid Mode ("+DoubleToStr(TargetCloseProfit,2)+")";
   if(TypeCloseInProfit==3) CloseProfitStr="Advanced Mode ("+DoubleToStr(TargetCloseProfit,2)+")";
//---
   if(TypeCloseInLoss==0) CloseLossStr="Whole Ticket ("+DoubleToStr(-TargetCloseLoss,2)+")";
   if(TypeCloseInLoss==1) CloseLossStr="Partial Ticket ("+DoubleToStr(-TargetCloseLoss,2)+")";
   if(TypeCloseInLoss==2) CloseLossStr="Not Close In Loss";
//---------------------------------------------------------------------
//Open next and step
   if(OpenOrdersInLoss==0) StepNextStr="Not Open Next In Loss";
//---
   if(OpenOrdersInLoss==1)
     {
      if(StepOrdersProgress==0) StepNextStr="Manual / Statical ("+DoubleToStr(StepOpenNextOrders,1)+")";
      if(StepOrdersProgress==1) StepNextStr="Manual / Geometrical ("+DoubleToStr(StepOpenNextOrders,1)+")";
     }
//---
   if(OpenOrdersInLoss==2)
     {
      if(StepOrdersProgress==0) StepNextStr="Automatic / Statical ("+DoubleToStr(TargetCloseProfit,1)+")";
      if(StepOrdersProgress==1) StepNextStr="Automatic / Geometrical ("+DoubleToStr(TargetCloseProfit,1)+")";
      if(StepOrdersProgress==2) StepNextStr="Automatic / Exponential ("+DoubleToStr(TargetCloseProfit,1)+")";
     }
//---------------------------------------------------------------------
//Lot orders
   if(AutoLotSize==false)
     {
      if(LotOrdersProgress==0) LotOrdersStr="Manual / Statical ("+DoubleToStr(ManualLotSize,2)+")";
      if(LotOrdersProgress==1) LotOrdersStr="Manual / Geometrical ("+DoubleToStr(ManualLotSize,2)+")";
      if(LotOrdersProgress==2) LotOrdersStr="Manual / Exponential ("+DoubleToStr(ManualLotSize,2)+")";
     }
//---
   if(AutoLotSize==true)
     {
      if(LotOrdersProgress==0) LotOrdersStr="Automatic / Statical ("+DoubleToStr((AccountBalance()/100000)*RiskFactor,2)+")";
      if(LotOrdersProgress==1) LotOrdersStr="Automatic / Geometrical ("+DoubleToStr((AccountBalance()/100000)*RiskFactor,2)+")";
      if(LotOrdersProgress==2) LotOrdersStr="Automatic / Exponential ("+DoubleToStr((AccountBalance()/100000)*RiskFactor,2)+")";
     }
//---------------------------------------------------------------------
//Show time info on chart
   if(UseTimeWindow==true)
     {
      //---Get remain time
      DiffTime=StringToTime(TimeStartTrade)-TimeGMT();
      //---Set hours
      Hopen=(double)DiffTime/3600;
      if(TimeGMT()>StringToTime(TimeStartTrade)) Hopen+=24;
      HopenStr=DoubleToStr(Hopen,5);
      if(Hopen>=10) Hdigits=2;
      //---Set minutes
      Mopen=((Hopen-(StrToDouble(StringSubstr(HopenStr,0,Hdigits))))*3600)/60;
      MopenStr=DoubleToStr(Mopen,5);
      if(Mopen>=10) Mdigits=2;
      //---Set seconds
      Sopen=((Mopen-(StrToDouble(StringSubstr(MopenStr,0,Mdigits))))*60)/1;
      SopenStr=DoubleToStr(Sopen,5);
      if(Sopen>=10) Sdigits=2;
      //---Set colors
      if(TimeToTrade==false) TextColor=clrOrangeRed;
      if(TimeToTrade==true) TextColor=clrLightSeaGreen;
      //---Set position for small panel
      if((ShowPairsInfo==0) || (ShowPairsInfo==2))
        {
         PosX=0;
         PosY=240;
        }
      //---Set position for big panel
      if(ShowPairsInfo==1)
        {
         PosX=250;
         PosY=12;
        }
      //---Text1
      ObjectDelete("Text1");
      if(ObjectFind("Text1")==-1) DisplayText("Text1"," GMT Time: "+TimeToStr(TimeGMT(),TIME_MINUTES),SizeFontsOfInfo,"Arial Black",TextColor,PosX,PosY+15);
      //---Text2
      ObjectDelete("Text2");
      if(ObjectFind("Text2")==-1) DisplayText("Text2"," Broker Time: "+TimeToStr(TimeCurrent(),TIME_MINUTES),SizeFontsOfInfo,"Arial Black",TextColor,PosX,PosY);
      //---Text3
      ObjectDelete("Text3");
      if(ObjectFind("Text3")==-1) DisplayText("Text3"," Time Start: "+StringSubstr(TimeStartTrade,0,5),SizeFontsOfInfo,"Arial Black",TextColor,PosX,PosY+30);
      //---Text4
      ObjectDelete("Text4");
      if(ObjectFind("Text4")==-1) DisplayText("Text4"," Time Stop: "+StringSubstr(TimeStopTrade,0,5),SizeFontsOfInfo,"Arial Black",TextColor,PosX,PosY+45);
      //---Text5
      ObjectDelete("Text5");
      if(TimeToTrade==false) if(ObjectFind("Text5")==-1) DisplayText("Text5"," Remain To Open: "+StringSubstr(HopenStr,0,Hdigits)+":"+StringSubstr(MopenStr,0,Mdigits)+":"+StringSubstr(SopenStr,0,Sdigits),SizeFontsOfInfo,"Arial Black",TextColor,PosX,PosY+60);
      if(TimeToTrade==true) if(ObjectFind("Text5")==-1) DisplayText("Text5"," EXPERT TRADE",SizeFontsOfInfo,"Arial Black",TextColor,PosX,PosY+60);
     }
//---------------------------------------------------------------------
//Set up pairs information
   for(i=0; i<NumberPairsTrade; i++)
     {
      //---------------------------------------------------------------------
      //Count total orders, lots and floating
      TotalOrders+=TotalOrdersPlus[i]+TotalOrdersMinus[i]+HedgePlusOrders[i]+HedgeMinusOrders[i];
      TotalProfitLoss+=TotalProfitPlus[i]+TotalProfitMinus[i]+TotalHedgeProfit[i];
      TotalLots+=TotalLotPlus[i]+TotalLotMinus[i]+HedgePlusLot[i]+HedgeMinusLot[i];
      //---------------------------------------------------------------------
      //Close levels
      if(TypeCloseInLoss<2)
        {
         LevelCloseInLossPlus[i]=LevelLossClosePlus[i];
         LevelCloseInLossMinus[i]=LevelLossCloseMinus[i];
        }
      //---------------------------------------------------------------------
      //Calculate max and min value
      MaxOrders[i]=MathMax(MaxOrders[i],TotalOrdersPlus[i]+TotalOrdersMinus[i]+HedgePlusOrders[i]+HedgeMinusOrders[i]);
      MaxFloating[i]=MathMin(MaxFloating[i],TotalProfitPlus[i]+TotalProfitMinus[i]+TotalHedgeProfit[i]);
      //---------------------------------------------------------------------
      //Pairs and orders
      if(ShowPairsInfo==1)
        {
         if(SymbolPair[i]!="") PairsInfo+=StringConcatenate("\n  "+IntegerToString(i+1)+". "+SymbolPair[i]+"|| "+
            DoubleToStr(TotalOrdersPlus[i]+TotalOrdersMinus[i]+HedgePlusOrders[i]+HedgeMinusOrders[i],0)+" || "+
            DoubleToStr(TotalProfitPlus[i]+TotalProfitMinus[i]+TotalHedgeProfit[i],2)+" || "+
            DoubleToStr(MathMax(LevelProfitClosePlus[i],LevelProfitCloseMinus[i]),2)+" || "+
            DoubleToStr(HistoryPlusOrders[i]+HistoryMinusOrders[i],0)+" / "+DoubleToStr(HistoryPlusProfit[i]+HistoryMinusProfit[i],2));
         //---
         ShowInfoOfPairs="  "+PairsInfo+"\n=================================";
        }
      //---------------------------------------------------------------------
      //Calculate max and min value
      if(TotalOrders>0)
        {
         MaxProfit=MathMax(TotalProfitLoss,MaxProfit);
         MinProfit=MathMin(TotalProfitLoss,MinProfit);
        }
      //---
      if(MaxProfit==-99999) ShowMaxProfit=0; else ShowMaxProfit=MaxProfit;
      if(MinProfit==99999) ShowMinProfit=0; else ShowMinProfit=MinProfit;
      MaxTotalOrders=MathMax(MaxTotalOrders,TotalOrders);
      //---------------------------------------------------------------------
      //Speread string
      if(MaxSpread==0) SpreadStr="Expert Not Check Spread";
      if(MaxSpread!=0) SpreadStr="Expert Check Spread ("+DoubleToStr(MaxSpread,2)+")";
      //---------------------------------------------------------------------
      //Set comments on chart
      if(ShowPairsInfo==2)
        {
         //---Set space
         if(i<9) SetSpace="  ";
         if(i>=9) SetSpace="";
         //---
         if(SymbolPair[i]!="")
           {
            //---Str1
            if(ObjectFind("Str1")==-1) DisplayText("Str1","Pairs",SizeFontsOfInfo,"Arial Black",ColorOfTitle,275,0);
            //---Str2
            if(ObjectFind("Str2")==-1) DisplayText("Str2","Orders",SizeFontsOfInfo,"Arial Black",ColorOfTitle,PositionOrders,0);
            //---Str3
            if(ObjectFind("Str3")==-1) DisplayText("Str3","PnL",SizeFontsOfInfo,"Arial Black",ColorOfTitle,PositionPnL,0);
            //---Str4
            if(ObjectFind("Str4")==-1) DisplayText("Str4","Close",SizeFontsOfInfo,"Arial Black",ColorOfTitle,PositionClose,0);
            //---Str5
            if(ObjectFind("Str5")==-1) DisplayText("Str5","History",SizeFontsOfInfo,"Arial Black",ColorOfTitle,PositionHistory,0);
            //---Str6
            if(ObjectFind("Str6")==-1) DisplayText("Str6","Maximum",SizeFontsOfInfo,"Arial Black",ColorOfTitle,PositionMaximum,0);
            //---Str7
            if(ObjectFind("Str7")==-1) DisplayText("Str7","Spread",SizeFontsOfInfo,"Arial Black",ColorOfTitle,PositionSpread,0);
            //---Comm1
            ObjectDelete("Comm1"+IntegerToString(i));
            if(ObjectFind("Comm1"+IntegerToString(i))==-1) DisplayText("Comm1"+IntegerToString(i),IntegerToString(i+1)+". "+SetSpace+StringSubstr(SymbolPair[i],0,6),SizeFontsOfInfo,"Arial Black",ColorOfInfo,245,20+(i*14));
            //---Comm2
            ObjectDelete("Comm2"+IntegerToString(i));
            if(ObjectFind("Comm2"+IntegerToString(i))==-1) DisplayText("Comm2"+IntegerToString(i),DoubleToStr(TotalOrdersPlus[i],0)+"-"+DoubleToStr(TotalOrdersMinus[i],0),SizeFontsOfInfo,"Arial Black",ColorOfInfo,PositionOrders+15,20+(i*14));
            //---Comm3
            ObjectDelete("Comm3"+IntegerToString(i));
            if(ObjectFind("Comm3"+IntegerToString(i))==-1) DisplayText("Comm3"+IntegerToString(i),DoubleToStr(TotalProfitPlus[i]+TotalProfitMinus[i]+TotalHedgeProfit[i],2),SizeFontsOfInfo,"Arial Black",ColorOfInfo,PositionPnL-5,20+(i*14));
            //---Comm4
            ObjectDelete("Comm4"+IntegerToString(i));
            if(ObjectFind("Comm4"+IntegerToString(i))==-1) DisplayText("Comm4"+IntegerToString(i),DoubleToStr(MathMax(LevelProfitClosePlus[i],LevelProfitCloseMinus[i]),2),SizeFontsOfInfo,"Arial Black",ColorOfInfo,PositionClose+5,20+(i*14));
            //---Comm5
            ObjectDelete("Comm5"+IntegerToString(i));
            if(ObjectFind("Comm5"+IntegerToString(i))==-1) DisplayText("Comm5"+IntegerToString(i),DoubleToStr(HistoryPlusOrders[i]+HistoryMinusOrders[i],0)+"/"+DoubleToStr(HistoryPlusProfit[i]+HistoryMinusProfit[i],2),SizeFontsOfInfo,"Arial Black",ColorOfInfo,PositionHistory,20+(i*14));
            //---Comm6
            ObjectDelete("Comm6"+IntegerToString(i));
            if(ObjectFind("Comm6"+IntegerToString(i))==-1) DisplayText("Comm6"+IntegerToString(i)," ("+DoubleToStr(MaxOrders[i],0)+"/"+DoubleToStr(MaxFloating[i],2)+")",SizeFontsOfInfo,"Arial Black",ColorOfInfo,PositionMaximum,20+(i*14));
            //---Comm7
            ObjectDelete("Comm7"+IntegerToString(i));
            if(ObjectFind("Comm7"+IntegerToString(i))==-1) DisplayText("Comm7"+IntegerToString(i),DoubleToStr(SpreadPair[i],1),SizeFontsOfInfo,"Arial Black",ColorOfInfo,PositionSpread+15,20+(i*14));
            //---Background0
            if(ObjectFind("BackgroundLine0")==-1) ChartBackground("BackgroundLine0",ColorLineTitles,EMPTY_VALUE,TRUE,245,0,PositionSpread-245+55,24);
            //---Background1
            if((i<NumberPairsTrade/2) && (MathMod(NumberPairsTrade,2)==0)) if(ObjectFind("BackgroundLine1"+IntegerToString(i))==-1) ChartBackground("BackgroundLine1"+IntegerToString(i),ColorOfLine1,EMPTY_VALUE,TRUE,245,22+(i*14*2),PositionSpread-245+55,16);
            if((i<=NumberPairsTrade/2) && (MathMod(NumberPairsTrade,2)==1)) if(ObjectFind("BackgroundLine1"+IntegerToString(i))==-1) ChartBackground("BackgroundLine1"+IntegerToString(i),ColorOfLine1,EMPTY_VALUE,TRUE,245,22+(i*14*2),PositionSpread-245+55,16);
            //---Background2
            if(i<NumberPairsTrade/2) if(ObjectFind("BackgroundLine2"+IntegerToString(i))==-1) ChartBackground("BackgroundLine2"+IntegerToString(i),ColorOfLine2,EMPTY_VALUE,TRUE,245,36+(i*14*2),PositionSpread-245+55,16);
           }
        }
     }
//---------------------------------------------------------------------
//Saving information about opened groups
   if((SaveInformations==true) && (TimeHour(TimeCurrent())!=LastHourSaved))
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
            FindGroup[w]=StringFind(ReadString,"Pair No: ",0);
            FindMaxFloating[w]=StringFind(ReadString,"Max Floating: ",0);
            FindMaxOrders[w]=StringFind(ReadString,"Max Orders: ",0);
            FindNext1[w]=StringFind(ReadString," || Pair: ",0);
            FindNext2[w]=StringFind(ReadString," || Max Floating: ",0);
            //---Get informations
            GetGroup[w]=StringSubstr(ReadString,FindGroup[w]+9,FindNext1[w]-(FindGroup[w]+9));
            GetMaxFloating[w]=StringSubstr(ReadString,FindMaxFloating[w]+14,10);
            GetMaxOrders[w]=StringSubstr(ReadString,FindMaxOrders[w]+11,FindNext2[w]-(FindMaxOrders[w]+11));
           }
         FileClose(FileHandle);
        }
      //---Reset maximum value
      for(int x=1; x<NumberPairsTrade+1; x++)
        {
         if((x==StrToInteger(GetGroup[x]))&&(StrToInteger(GetMaxOrders[x])>MaxOrders[x-1])) MaxOrders[x-1]=StrToInteger(GetMaxOrders[x]);
         if((x==StrToInteger(GetGroup[x]))&&(StrToInteger(GetMaxFloating[x])<MaxFloating[x-1])) MaxFloating[x-1]=StrToInteger(GetMaxFloating[x]);
        }
      //---Write first time the file
      FileHandle=FileOpen(NameOfFile,FILE_READ|FILE_WRITE|FILE_CSV|FILE_COMMON);
      //---Continue to write the file
      if(FileHandle!=INVALID_HANDLE)
        {
         for(i=0; i<NumberPairsTrade; i++)
           {
            FileWrite(FileHandle,"Pair No: "+IntegerToString(i+1)+" || Pair: "+StringSubstr(SymbolPair[i],0,6)+" || History Profit: "+DoubleToStr(HistoryPlusProfit[i]+HistoryMinusProfit[i],2)+" || Max Orders: "+DoubleToStr(MaxOrders[i],0)+" || Max Floating: "+DoubleToStr(MaxFloating[i],2));
            FileFlush(FileHandle);
            if(i==NumberPairsTrade-1) LastHourSaved=TimeHour(TimeCurrent());
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
           "\n  Spread: "+SpreadStr+
           "\n================================="+
           "\n  Close In Profit Orders: "+CloseProfitStr+
           "\n  Close In Loss Orders  : "+CloseLossStr+
           "\n  Step For Next Order  : "+StepNextStr+
           "\n  Order Lot Size Type  : "+LotOrdersStr+
           "\n  Emergency Mode      : "+EmergencyStr+
           "\n================================="+
           ShowInfoOfPairs+
           "\n  Orders: "+DoubleToStr(TotalOrders,0)+" ("+DoubleToStr(MaxTotalOrders,0)+")"+"|PnL: "+DoubleToStr(TotalProfitLoss,2)+" ("+DoubleToStr(ShowMinProfit,2)+")"+"|Lots: "+DoubleToStr(TotalLots,2)+
           "\n================================="+
           "\n  T O T A L   H I S T O R Y   R E S U L T S"+
           "\n  Pips: "+DoubleToStr(HistoryTotalPips,2)+" | Orders: "+DoubleToStr(HistoryTotalTrades,0)+" | PnL: "+DoubleToStr(HistoryTotalProfitLoss,2)+
           "\n=================================");
//---------------------------------------------------------------------
  }
//====================================================================================================================================================//
//End of code
//====================================================================================================================================================//
