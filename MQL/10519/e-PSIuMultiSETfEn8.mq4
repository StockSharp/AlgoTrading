//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|                                                                e-PSI@MultiSET.mq4 |
//|                                                         Copyright � 2011, TarasBY |
//|                                                                taras_bulba@tut.by |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
#property copyright "Copyright � 2011, TarasBY WM Z670270286972"
#property link      "taras_bulba@tut.by"
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|    This advisor is my step towards to seriously of partners-investors!            |
//|    This is only the 2nd part of the whole system, several crippled version in     |
//|    terms of capital management and technical subtleties to work on a real account.|
//|    Otherwise it is fully functional, except that it is configured for a specific  |
//|    quotation of broker, but do not necessarily not Your...                        |
//|    Who will make money using this my advisor, I'll be only too pleased! BUT this  |
//|    requires knowledge of programming and, more importantly, WIT...                |
//|    Always configured to cooperate!                                                |
//|                                                     Honored profit FOR ALL OF US! |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|                  *****         ��������� ���������         *****                  |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
extern string SETUP_Expert          = "======== General Configuration Advisor ========";
extern int    MG                       = 707;              // Magic
extern int    NewBarInPeriod           = 1;                // >= 0 - operate at the beginning of the period, a new bar; -1 - working on each tick
extern int    Variant_TradePrice       = 4;                // Option prices, with which will work advisor
// 0 - Bid\Ask ("For Good luck") - the results of the tester can ignore
// 1 - Open[0] -  the preferred option (for amateurs)
// 2 - Close[1] - (for amateurs)
// 3 - Close[0] -  "just in case"
// 4 - Price[0] = iOpen (gs_Symbol, NewBarInPeriod, 0); Price[1] = Price[0] + spread
// 5 - Price[0] = iHigh (gs_Symbol, NewBarInPeriod, 1); Price[1] = iLow (gs_Symbol, NewBarInPeriod, 1)
// 6 - Price[0] = iHigh (gs_Symbol, ControlPeriod, 0); Price[1] = iLow (gs_Symbol, ControlPeriod, 0)
// 7 - Price[0] = iHigh (gs_Symbol, 1, 1); Price[1] = iLow (gs_Symbol, 1, 1)
extern int    ControlPeriod            = PERIOD_H1;        // The period at which advisor makes a decision
extern string SETUP_RulesSend       = "---------------- RULES`s SEND -----------------";
extern double TakeProfit               = 20;               // The level of profit in pips
extern double StopLoss                 = 200;              // The level of fixation losses in pips
extern bool   Use_Prioritity           = TRUE;             // Use the priority for send orders on the results of profit
extern string LifeStart_min.List       = "18,10,9,7,12,10,6"; // position is opened for the first xx minutes, then from the rest of the time assumes that will be rolled back
extern int    Dopusk                   = 2;                // it's allowable price move in the opposite direction
extern string MIN_Distance.List        = "14,19,18,10,18,19,16"; //  by this amount in pips price must go from the opening price bar
extern string SETUP_RulesClose      = "---------------- RULES`s CLOSE ----------------";
extern string Life_bars.List           = "1,10,2,10,2,8,10";     // So many  bars live's position
extern int    ProfitMIN                = 3;                // minimum profit in pips on a warrant
extern string SETUP_Trailling       = "================== TRAILING ===================";
extern bool   VirtualTrail_ON          = TRUE;             // Enable the virtual trailing
extern string TrailingStop.List        = "11,12,14,5,11,12,10";  // Rolling take-profit, zero to disable it
extern string Setup_Lots            = "============== Capital Management =============";
extern bool   UseMM                    = TRUE;             // Use Money Management
extern string Setup_LotsWayChoice_0_1  = "LotsWayChoice: 0-fixed; 1-% from MeansType;";
extern int    LotsWayChoice            = 1;                // The method of choosing the working of lot:
extern string Setup_MeansType_0_5      = "MeansType: 1-Balance; 2-Equity; 3-FreeMargin; 4-RiskDepo; 5-BaseBalance + Pribul";
extern int    MeansType                = 2;                // Type of means used in the calculation of lot size:
extern double Order_Lots               = 0.1;              // Fixed lot size
extern int    LotsPercent              = 7;                // Percentage of MeansType
extern double MinLot                   = 0.01;             // Minimum lot market order
extern double MaxLot                   = 5.0;              // Maximum lot market order
extern string Setup_MM_DEPO         = "---------------- CONTROL DEPO -----------------";
extern double BaseBalance		         = 300;              // Capital Base
extern double RiskDepo                 = 150;              // Size of Depo in the deposit currency, which play
extern int    MINMarginPercent         = 30;               // Percent of free margin of the Balance Sheet when new orders are not sended
extern int    MAXZagruzkaDepoPercent   = 50;               // Percentage of Depo load at which new orders are not sended
extern int    MAXOtkatDepoPercent      = 30;               // At what rate of loss from the maximum profit  (in percents) stop trading
extern int    MAXLossDepoPercent       = 30;               // In Which is a loss (in percents) from the current Balance close all orders
extern bool   FULL_Control_ON          = False;            // A complete removal: remove ALL orders in the account (or only on MAGIC)
extern string Setup_Services        = "=================== SERVICES ==================";
extern bool   PrintCom                 = TRUE;             // Print comments
extern bool   ShowCommentInChart       = TRUE;             // Show comments on the chart
extern bool   SoundAlert               = FALSE;            // Sound
extern int    Slippage                 = 2;                // The permissible deviation from quoted price
extern string WorkSheduller         = "=================== SCHEDULER =================";
extern bool   TimeControl              = True;
extern string BadDay                   = "0";              // The bad days of the week for work (through ",")
extern string BadTime                  = "0";              // Bad time to work (through ",")
extern int    Open_HourTrade           = 2;                // Begin of work
extern int    Close_HourTrade          = 20;               // End of work
//IIIIIIIIIIIIIIIIIII======== Global variables of advisor ========IIIIIIIIIIIIIIIIIIII+
double        gda_Price[2],               // Array of price from Symbol
              // gda_Price[0] - Bid
              // gda_Price[1] - Ask
              gd_MinEquity,               // value of the minimum equity accounts in the currency of Deposit
              gd_MinEquityPercent,        // value of the minimum equity accounts as a percentage
              gd_MinMargin,               // minimum value of free margin allowed for open positions
              gd_MinMarginPercent,        // minimum value of free margin allowed for open positions a percentage
              gd_MaxZalog,                // maximum value of margin in the currency of Deposit
              gd_MaxZalogPercent,         // maximum value of margin as a percentage
              gd_BeginBalance,            // begin value of Balance Account
              gd_MaxLOSS,                 // the maximum drawdown in Account
              gd_MaxLOSSPercent,          // the maximum drawdown in Account as a percentage
              gd_curLOSSPercent,          // current drawdown in Account as a percentage
              gd_curMAXOtkatDepoPercent,  // current value of loss from gd_MAXBalance
              gd_MAXBalance,              // maximum value of Balance Account
              gd_Balance, gd_Equity, gd_FreeMargin, gd_Margin,
              gd_Point, gd_TP, gd_SL, gd_Trail, gd_MIN_Distance, gd_step, gd_RealSPREAD,
              gda_TrailingStop[], gda_MIN_Distance[], gda_AvarageCandle[2],
              gda_BaseBalance[], gda_Profit[], gda_Pribul[], gd_ProfitALL,
              gd_LOTSTEP, gd_MAXLOT, gd_MINLOT, gd_dopusk,
              gd_BaseBalance, gd_PribulALL, LotsMM;
int           gi_Digits, gi_Decimal = 1, gi_curOrdersInSeries, gi_HistoryOrders, cur_Life_min, gi_dig,
              gi_MyOrders, gia_BadTime[], gia_MG[], cnt_MG = 0, gia_LifeStart[], gia_BadDay[], ind_ERR,
              gia_HistoryOrders[], gia_MyOrders[], gia_Life_bars[], gia_Priority[], gia_CommTime[6];
string        gs_Symbol, gs_NameGV,
              gsa_Comment[6],
              // gsa_Comment[0]- working MM
              // gsa_Comment[1]- working TimeControl
              // gsa_Comment[2]- working the virtual trailing
              // gsa_Comment[3]- information on send orders
              // gsa_Comment[4]- information on close orders
              // gsa_Comment[5]- Errors
              ExpertName,
              gs_Info,                   // the upper line comments with general information on setting advisor
              gs_sign;                   // graphic sign for the deposit currency
bool          gb_Pause = false, PrintStatistic = false,
              gb_TimePause = false,      // pause for a period of time not working
              RealTrade = true,          // flag "does not work" in the tester
              gb_Trade = true,
              gb_VirualTrade = false,
              gb_first = true,           // flag first run
              gb_InfoPrint = false;
datetime      gdt_NewBar, gdt_NewBarControl, gdt_NewBarH1, 
              gdt_BeginBar_H1, gdt_curTime, gdt_LastBalanceTime,
              gdt_BeginTrade;            // the first "its" open order
//IIIIIIIIIIIIIIIIIII============== Used libraries =============IIIIIIIIIIIIIIIIIIIIII+
#include      <stdlib.mqh>
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|                  Custom expert initialization function                            |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int init()
{
    int    li_size, li_Period = 60;
    string tmpArr[], ls_txt = "", ls_tmp = "";
//----
    gi_Digits = Digits;
    gs_Symbol = Symbol();
    gd_Point = Point;
    gi_dig = LotDecimal();
    gs_NameGV = StringConcatenate ("MultiSET[", MG, "]");
    ExpertName = StringConcatenate (WindowExpertName(), ":  ", fGet_NameTF (Period()), "_", gs_Symbol);
    //---- ��������� ������ 5-�� �����
    if (gi_Digits == 3 || gi_Digits == 5)
    {gi_Decimal = 10;}
    TakeProfit *= gi_Decimal;
    gd_TP = TakeProfit * gd_Point;
    StopLoss *= gi_Decimal;
    gd_SL = StopLoss * gd_Point;
    ProfitMIN *= gi_Decimal;
    Dopusk *= gi_Decimal;
    gd_dopusk = Dopusk * gd_Point;
    gd_MAXLOT = MarketInfo (gs_Symbol, MODE_MAXLOT);
    gd_MINLOT = MarketInfo (gs_Symbol, MODE_MINLOT);
    gd_LOTSTEP = MarketInfo (gs_Symbol, MODE_LOTSTEP);
    Slippage *= gi_Decimal;
    gd_step = 1 * gi_Decimal * gd_Point;
    if (NewBarInPeriod == 0) {li_Period *= Period();} else {li_Period *= NewBarInPeriod;}
    gdt_NewBar = iTime (gs_Symbol, NewBarInPeriod, 0) - li_Period;
    InitializeArray_STR (gsa_Comment);
    //---- ���������� ������ "������" � ������� ������������ (gsa_Comment)
    ind_ERR = ArraySize (gsa_Comment) - 1;
    //---- ���������� ������ ������ ��������
    gs_sign = IIFs ((AccountCurrency() == "USD"), "$", IIFs ((AccountCurrency() == "EUR"), "�", "RUB"));
    //---- �������������� ������ ������ ��������� � ���������� ���������� ������
    fGet_IsStatusTrade (PrintCom, ShowCommentInChart, gb_InfoPrint, SoundAlert, RealTrade);
    //---- ������� "������ �������� �������"
    if (TimeControl)
    {
        //---- ������ "������" ���� ��� ��������
        fSplitStrToStr (BadDay, tmpArr, ",");
        li_size = ArraySize (tmpArr);
        ArrayResize (gia_BadDay, li_size);
        for (int li_int = 0; li_int < li_size; li_int++)
        {gia_BadDay[li_int] = StrToInteger (tmpArr[li_int]);}
        //---- ������ "������" ����� ��� ��������
        fSplitStrToStr (BadTime, tmpArr, ",");
        li_size = ArraySize (tmpArr);
        ArrayResize (gia_BadTime, li_size);
        for (li_int = 0; li_int < li_size; li_int++)
        {gia_BadTime[li_int] = StrToInteger (tmpArr[li_int]);}
    }
    //---- ��������� ������� ������� ������������ �������
    fSplitStrToStr (TrailingStop.List, tmpArr, ",");
    li_size = ArraySize (tmpArr);
    cnt_MG = li_size;
    ArrayResize (gda_TrailingStop, cnt_MG);
    for (li_int = 0; li_int < cnt_MG; li_int++)
    {
        gda_TrailingStop[li_int] = StrToDouble (tmpArr[li_int]) * gi_Decimal * gd_Point;
        ls_tmp = StringConcatenate (ls_tmp, "TrailingStop[", li_int, "] = ", DoubleToStr (gda_TrailingStop[li_int], gi_Digits), "; ");
    }
    Print (ls_tmp);
    ls_tmp = "";
    //---- ������ ������ ������� "������"
    fSplitStrToStr (LifeStart_min.List, tmpArr, ",");
    li_size = ArraySize (tmpArr);
    cnt_MG = MathMin (cnt_MG, li_size);
    ArrayResize (gia_LifeStart, cnt_MG);
    for (li_int = 0; li_int < cnt_MG; li_int++)
    {
        gia_LifeStart[li_int] = StrToInteger (tmpArr[li_int]);
        ls_tmp = StringConcatenate (ls_tmp, "LifeStart[", li_int, "] = ", gia_LifeStart[li_int], "; ");
    }
    Print (ls_tmp);
    ls_tmp = "";
    //---- ������ ������ ����������� ���������
    fSplitStrToStr (MIN_Distance.List, tmpArr, ",");
    li_size = ArraySize (tmpArr);
    cnt_MG = MathMin (cnt_MG, li_size);
    ArrayResize (gda_MIN_Distance, cnt_MG);
    for (li_int = 0; li_int < cnt_MG; li_int++)
    {
        if (StrToDouble (tmpArr[li_int]) > 0)
        {gda_MIN_Distance[li_int] = StrToDouble (tmpArr[li_int]) * gi_Decimal * gd_Point;}
        //---- ��������� ����������� �������������� ����������
        else
        {gda_MIN_Distance[li_int] = StrToDouble (tmpArr[li_int]);}
        ls_tmp = StringConcatenate (ls_tmp, "MIN Distance[", li_int, "] = ", DoubleToStr (gda_MIN_Distance[li_int], gi_Digits), "; ");
    }
    Print (ls_tmp);
    ls_tmp = "";
    //---- ������ ������ ������� ����� �������
    fSplitStrToStr (Life_bars.List, tmpArr, ",");
    li_size = ArraySize (tmpArr);
    cnt_MG = MathMin (cnt_MG, li_size);
    ArrayResize (gia_Life_bars, cnt_MG);
    for (li_int = 0; li_int < cnt_MG; li_int++)
    {
        gia_Life_bars[li_int] = StrToInteger (tmpArr[li_int]);
        ls_tmp = StringConcatenate (ls_tmp, "Life bars[", li_int, "] = ", gia_Life_bars[li_int], "; ");
    }
    Print (ls_tmp);
    //---- ��������� ������ �������
    ArrayResize (gia_MG, cnt_MG);
    //---- �������������� �������
    ArrayResize (gia_MyOrders, cnt_MG);
    ArrayResize (gia_HistoryOrders, cnt_MG);
    ArrayResize (gia_Priority, cnt_MG);
    ArrayResize (gda_Pribul, cnt_MG);
    ArrayResize (gda_Profit, cnt_MG);
    ArrayResize (gda_BaseBalance, cnt_MG);
    //---- ��������� ������� ������ �������
    for (li_int = 0; li_int < cnt_MG; li_int++)
    {gia_MG[li_int] = MG + li_int;}
    //---- ��������� �������������� ����������
    fGet_Statistic();
    //---- ���������� ������ �������� ����������� ������� �����
    if (gdt_BeginTrade == 0)
    {gdt_BeginTrade = gdt_curTime;}
    //---- ����������� ������ ����
    Order_Lots = fLotsNormalize (Order_Lots);
    LotsMM = fGet_SizeLot (ls_txt, Order_Lots, OP_BUY, gd_BaseBalance, gia_MG[0], gi_curOrdersInSeries, MINMarginPercent, MAXZagruzkaDepoPercent, gs_NameGV);
    fPrintAndShowComment (ls_txt, ShowCommentInChart, PrintCom, gsa_Comment, 0);
	 Print ("Leverage = ", AccountLeverage(), " | MIN Lot = ", DSDig (gd_MINLOT), " | MAX Lot = ", DSDig (gd_MAXLOT), " | Lot = ", DSDig (LotsMM), " | SPREAD = ", MarketInfo (gs_Symbol, MODE_SPREAD));
    //---- ������������ ��������� ������
    fGetLastError (gsa_Comment, "init()", ind_ERR);
//----
    return (0);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|                  Custor expert deinitialization function                          |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int deinit()
{
//----
    //---- ������� ���������� �� ������
    if (ShowCommentInChart)
    {
        //---- �������� ����������
        fGet_Statistic();
        fCommentInChart (gsa_Comment, gia_CommTime);
    }
    Sleep (500);
//----
    return (0);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|               Custom expert iteration function                                    |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int start() 
{
    gdt_BeginBar_H1 = iTime (gs_Symbol, 60, 0);
    //---- ������� ������ �� ������� (Bad Hour)
    if (gdt_NewBarH1 == gdt_BeginBar_H1)
    {return (0);}
    datetime ldt_BeginBarControl = iTime (gs_Symbol, ControlPeriod, 0);
    //---- ������� - �� ����� ���� ����������� ���� �����
    if (gdt_NewBarControl == ldt_BeginBarControl)
    {return (0);}
//----
    int    li_pos, li_total, li_Ticket, li_MG,
           li_tmp_HistoryOrders, err = GetLastError();
    string ls_txt = "";
    static double lda_MIN_Distance[], lda_Trail[];
//----
    //---- ������ ��������������� �������
    if (gb_first)
    {
        ArrayResize (lda_MIN_Distance, cnt_MG);
        ArrayResize (lda_Trail, cnt_MG);
    }
    gi_MyOrders = fMyPositions (gda_Profit, gia_MyOrders, gia_MG);
    //---- �������� ����������
    fGet_Statistic();
    //---- ������� ���������� �� ������
    if (ShowCommentInChart)
    {fCommentInChart (gsa_Comment, gia_CommTime);}
    //---- ������ � ������ ���������� ���� (���� NewBarInPeriod >= 0)
    if (NewBarInPeriod >= 0)
    {
        if (gi_MyOrders == 0)
        {
            if (gdt_NewBar == iTime (gs_Symbol, NewBarInPeriod, 0))
            {return (0);}
            gdt_NewBar = iTime (gs_Symbol, NewBarInPeriod, 0);
            //---- � ������ ���� �������� �� ������ ������� ����
            if (gdt_NewBar == ldt_BeginBarControl)
            {return (0);}
        }
    }
    //---- �������������� ������ �� �������
    if (IsTradeTime (Open_HourTrade, Close_HourTrade))
    {
        gb_TimePause = IsBadTime (gia_BadDay, gia_BadTime);
        gb_Pause = gb_TimePause;
    }
    else
    {gb_Pause = true;}
    if (gi_MyOrders == 0)
    {
        //---- ������������ ��������, �������������� ����������� ������ ����
        if (gb_Pause)
        {
            if (gb_TimePause)
            {
                if (gdt_NewBarH1 != gdt_BeginBar_H1)
                gdt_NewBarH1 = gdt_BeginBar_H1;
            }
            return (0);
        }
    }
    int li_extrem = -1;
    //---- ���� ���� �������� ��������� - ������� �� ���������� ����
    if (iHigh (gs_Symbol, ControlPeriod, 0) - iOpen (gs_Symbol, ControlPeriod, 0) <= gd_dopusk)
    {li_extrem = 0;}
    else if (iOpen (gs_Symbol, ControlPeriod, 0) - iLow (gs_Symbol, ControlPeriod, 0) <= gd_dopusk)
    {li_extrem = 1;}
    else
    {
        //---- ���� �������� ������� ��� - ��� ������� ���������� BarControl ����
        if (gi_MyOrders == 0)
        {
            gdt_NewBarControl = ldt_BeginBarControl;
            return (0);
        }
        gb_Pause = true;
    }
    //---- �������� ����, � �������� ����� �������� ��������
    fGet_MineTradePrice (Variant_TradePrice, gda_Price);
    //---- ��������� ���������� �����, ���������� � ������ ControlPeriod ����
    cur_Life_min = iBarShift (gs_Symbol, PERIOD_M1, ldt_BeginBarControl);
    //---- ��������� ������� ���� (gda_AvarageCandle[0])� ���� (gda_AvarageCandle[1]) ����� �� ��������� �����
    fGet_AverageCandle (gda_AvarageCandle, 1, gs_Symbol, ControlPeriod);
    //---- ��������� �������� �������
    if (!gb_Pause)
    {
        int    NO_orders = 0, li_cmd, li_ind, li_ORD;
        double ld_Price, ld_TP, ld_PriceBar = iOpen (gs_Symbol, ControlPeriod, 0);
        bool   lb_NotMoney = false;
        string lsa_ord[] = {"buy","sell"};
        color  lca_color[] = {Blue,Red};
        //---- �������� �� ����������� � ������ ������� ��������
        for (int li_PR = 0; li_PR < cnt_MG; li_PR++)
        {
            //---- ���������� ���������� ���������� �����
            if (Use_Prioritity && !gb_first) {li_MG = gia_Priority[li_PR];} else {li_MG = li_PR;}
            //---- ��� �������� ������� �� ������
            if (gia_MyOrders[li_MG] == 0)
            {
                //---- ������������ �� ������� ���� �������� ������ ������ ������
                fCalculate_Pribul (li_tmp_HistoryOrders, gia_MG[li_MG], ldt_BeginBarControl);
                if (li_tmp_HistoryOrders > 0 || gdt_NewBarControl == ldt_BeginBarControl)
                {continue;}
                if (cur_Life_min <= gia_LifeStart[li_MG])
                {
                    for (li_ORD = 0; li_ORD < 2; li_ORD++)
                    {
                        //---- ���� ��������� �� ���� ��������
                        if (li_extrem == li_ORD)
                        {
                            //---- ��������� ����������� MIN_Distance
                            if (gda_MIN_Distance[li_MG] < 0)
                            {
                                if (gda_MIN_Distance[li_MG] == -1.0)
                                {lda_MIN_Distance[li_MG] = NormalizeDouble (gda_AvarageCandle[0], gi_Digits);}
                                else if (gda_MIN_Distance[li_MG] == -2.0)
                                {lda_MIN_Distance[li_MG] = NormalizeDouble (gda_AvarageCandle[1] / 2.0, gi_Digits);}
                            }
                            else
                            {lda_MIN_Distance[li_MG] = gda_MIN_Distance[li_MG];}
                            if (li_ORD == 0) {li_cmd = 1; li_ind = 1;} else {li_cmd = -1; li_ind = 0;}
                            //---- ���� ���� ������ ������ ��������� ����
                            if (li_cmd * (ld_PriceBar - gda_Price[li_ind]) >= lda_MIN_Distance[li_MG]
                            //---- �� �� ����� ������� ������
                            && li_cmd * (ld_PriceBar - gda_Price[li_ind]) <= lda_MIN_Distance[li_MG] * 2.0)
                            {
                                //---- ����������� ������ ����
                                LotsMM = fGet_SizeLot (ls_txt, Order_Lots, li_ORD, gda_BaseBalance[li_MG], gia_MG[li_MG], gi_curOrdersInSeries, MINMarginPercent, MAXZagruzkaDepoPercent, gs_NameGV);
                                fPrintAndShowComment (ls_txt, ShowCommentInChart, PrintCom, gsa_Comment, 0);
                                //---- ���� ���� ��� ���������� - ��������� �����
                                if (LotsMM > 0)
                                {
                                    li_Ticket = 0;
                                    ld_Price = NormalizeDouble (fGet_TradePrice (li_ind, RealTrade, gs_Symbol), gi_Digits);
                                    ld_TP = NormalizeDouble (ld_Price + li_cmd * MathMax (gd_TP, MarketInfo (gs_Symbol, MODE_STOPLEVEL) * gd_Point), gi_Digits);
                                    li_Ticket = OrderSend (gs_Symbol, li_ORD, LotsMM, ld_Price, Slippage, NormalizeDouble (ld_Price - li_cmd * gd_SL, gi_Digits), ld_TP, lsa_ord[li_ORD], gia_MG[li_MG], 0, lca_color[li_ORD]);  
                                    if (li_Ticket > 0)
                                    {
                                        if (!gb_VirualTrade)
                                        {
                                            OrderSelect (li_Ticket, SELECT_BY_TICKET);
                                            OrderPrint();
                                            ls_txt = fPrepareComment (StringConcatenate ("#", li_Ticket, "[", gia_MG[li_MG], "/", li_MG, "] ", fGet_NameOP (OrderType()), " at ", DoubleToStr (OrderOpenPrice(), gi_Digits),
                                            "; MIN Distance = ", lda_MIN_Distance[li_MG] / gd_Point), gb_InfoPrint);
                                            fPrintAndShowComment (ls_txt, ShowCommentInChart, PrintCom, gsa_Comment, 3);
                                            PrintStatistic = true;
                                        }
                                    }
                                    break;
                                }
                                //---- �������� ���� ���������� �� ����������� �� ������������� �������
                                else {lb_NotMoney = true;}
                            }
                        }
                    }
                }
                else
                {
                    //---- ������������ ���������� �����, "������������" �� ���� ����
                    NO_orders++;
                    if (NO_orders == cnt_MG - 1)
                    {
                        gdt_NewBarControl = ldt_BeginBarControl;
                        return (0);
                    }
                    continue;
                }
            }
            //---- ���� �������� "�����������" - �������
            if (lb_NotMoney)
            {break;}
        }
    }
    if (gi_MyOrders > 0)
    {
        double ld_new_SL, ld_SL, ld_Profit;
        int    li_Order_Bar, li_Life_bars;
        bool   lb_profit, lb_life, lb_loss, lb_trail;
        string ls_Name;
        color  lca_close[] = {Green,Magenta};
        li_total = OrdersTotal() - 1; 
        //---- �������� ������� ���� ��� �������� ������� �� �������� �������
        fGet_MineTradePrice (0, gda_Price);
        //---- ������������ ������������ ����������� ������
        if (ProfitMIN == 0)
        {ProfitMIN = gd_RealSPREAD / gd_Point;}
        for (li_pos = li_total; li_pos >= 0; li_pos--)
        {
            if (!OrderSelect (li_pos, SELECT_BY_POS, MODE_TRADES))
            {continue;}
            li_MG = fCheck_MyMagic (OrderMagicNumber(), gia_MG);
            if (li_MG >= 0 && OrderSymbol() == gs_Symbol)
            {
                if (OrderType() <= OP_SELL)
                {
                    ld_Profit = OrderProfit() + OrderSwap() + OrderCommission();
                    li_Ticket = OrderTicket();
                    lb_profit = false; lb_life = false; lb_trail = false; lb_loss = false;
                    //---- ������������ ������������ ��������
                    if (gda_TrailingStop[li_MG] < 0.0)
                    {lda_Trail[li_MG] = NormalizeDouble (lda_MIN_Distance[li_MG] / 3.0, gi_Digits);}
                    else {lda_Trail[li_MG] = gda_TrailingStop[li_MG];}
                    //---- ������ �� "������" ������� �� �������� (� �����)
                    li_Order_Bar = iBarShift (gs_Symbol, PERIOD_H1, OrderOpenTime());
                    if (gia_Life_bars[li_MG] > 0)
                    {li_Life_bars = gia_Life_bars[li_MG] - li_Order_Bar;}
                    //---- �������� ����� ���� �� ����� ������� �����
                    else
                    {li_Life_bars = 1440 / ControlPeriod - iBarShift (gs_Symbol, ControlPeriod, iTime (gs_Symbol, PERIOD_D1, iBarShift (gs_Symbol, PERIOD_D1, OrderOpenTime())));}
                    lb_life = (li_Life_bars <= 0);
                    //---- �������� SL
                    if (VirtualTrail_ON)
                    {
                        ls_Name = StringConcatenate (li_Ticket, "_#SL");
                        if (!GlobalVariableCheck (ls_Name))
                        {ld_SL = OrderStopLoss();}
                        else
                        {ld_SL = GlobalVariableGet (ls_Name);}
                    }
                    else
                    {ld_SL = OrderStopLoss();}
                    ld_PriceBar = iOpen (gs_Symbol, ControlPeriod, li_Order_Bar);
                    for (li_ORD = 0; li_ORD < 2; li_ORD++)
                    {
                        if (OrderType() == li_ORD)
                        {
                            if (li_ORD == 0) {li_cmd = 1;} else {li_cmd = -1;}
                            //---- ���� ��� �� ��������� �� ������� �����
                            if (!lb_life)
                            {
                                //---- ������� �������� ���������� ������
                                lb_profit = (li_cmd * (gda_Price[li_ORD] - ld_PriceBar) >= 0.0);
                                //---- ������� �������� �� ����������� �������
                                lb_loss = (li_cmd * (OrderOpenPrice() - gda_Price[li_ORD]) > MathMax (gda_AvarageCandle[1], lda_MIN_Distance[li_MG]) * 2.0 && li_Order_Bar == 0);
                                //---- ������� �������� �� ������������ ���������
                                lb_trail = (VirtualTrail_ON                     // ������� ����������� ����
                                && li_cmd * (ld_SL - gda_Price[li_ORD]) >= 0.0  // ���� ��������� ����
                                && ld_Profit > ProfitMIN * fGet_PipsValue()     // ������� ������ ������ ������������
                                && gda_TrailingStop[li_MG] != 0.0);             // ���������� �� �������� ������
                            }
                            //---- ��������
                            if (lb_profit || lb_life || lb_trail || lb_loss)
                            {
                                ld_Price = NormalizeDouble (fGet_TradePrice (li_ORD, RealTrade, gs_Symbol), gi_Digits);
                                if (OrderClose (li_Ticket, OrderLots(), ld_Price, Slippage, lca_close[li_ORD]))
                                {
                                    if (!gb_VirualTrade)
                                    {
                                        OrderSelect (li_Ticket, SELECT_BY_TICKET);
                                        int li_pips = li_cmd * (OrderClosePrice() - OrderOpenPrice()) / gd_Point;
                                        ls_txt = fPrepareComment (StringConcatenate ("#", li_Ticket, "[", gia_MG[li_MG], "/", li_MG, "] STOP ",
                                        IIFs (lb_profit, StringConcatenate ("Profit  [Dist:", lda_MIN_Distance[li_MG] / gd_Point, " | PIP`s:", li_pips, " | Life: ", li_Order_Bar, "]"),
                                        IIFs (lb_loss, StringConcatenate ("LOSS  [Dist:", lda_MIN_Distance[li_MG] / gd_Point, " | PIP`s:", li_pips, " | Life: ", li_Order_Bar, "]"),
                                        IIFs (lb_life, StringConcatenate ("Life  [Limit:", gia_Life_bars[li_MG], " | cur:", li_Order_Bar, "]"),
                                        IIFs (lb_trail, StringConcatenate ("Trail  [Tr:", lda_Trail[li_MG] / gd_Point, " | ProfitMIN: ", gs_sign, DSDig (ProfitMIN * fGet_PipsValue()), " | Life: ", li_Order_Bar, "]"), "")))),
                                        " Profit: ", gs_sign, fSplitField (DSDig (ld_Profit))), gb_InfoPrint);
                                        fPrintAndShowComment (ls_txt, ShowCommentInChart, PrintCom, gsa_Comment, 4);
                                    }
                                    break;
                                }
                            }
                            //---- ��������-����
                            if (gda_TrailingStop[li_MG] != 0.0)  
                            {
                                if (gda_Price[li_ORD] - OrderOpenPrice() > lda_Trail[li_MG])
                                {
                                    ld_new_SL = NormalizeDouble (gda_Price[li_ORD] - li_cmd * lda_Trail[li_MG], gi_Digits);
                                    if (li_cmd * (ld_new_SL - OrderStopLoss()) > gd_step
                                    || OrderStopLoss() == 0.0)
                                    {
                                        if (!VirtualTrail_ON)
                                        {
                                            if (OrderModify (li_Ticket, OrderOpenPrice(), ld_new_SL, OrderTakeProfit(), 0, Gold))
                                            {break;}
                                        }
                                        else
                                        {GlobalVariableSet (ls_Name, ld_new_SL);}
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
    gb_first = false;
    //---- ������������ ��������� ������
    fGetLastError (gsa_Comment, "start()", ind_ERR);
//----
    return (0);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Author   : TarasBY                                                               |
//+-----------------------------------------------------------------------------------+
//|           Check the presence of "their" open positions                            |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int fMyPositions (double& ar_Profit[], int& ar_MyOrders[], int ar_Magic[])
{
    int    li_total = OrdersTotal(), li_Ord = 0, li_ind_MG;
    double ld_Profit;
//----
    ArrayInitialize (ar_Profit, 0.0);
    ArrayInitialize (ar_MyOrders, 0);
    gd_ProfitALL = 0.0;
    if (li_total == 0)
    {return (0);}
    for (int li_pos = li_total - 1; li_pos >= 0; li_pos--)
    {
        if (OrderSelect (li_pos, SELECT_BY_POS, MODE_TRADES))
        {
            if (OrderSymbol() != gs_Symbol)
            continue;
            li_ind_MG = fCheck_MyMagic (OrderMagicNumber(), ar_Magic);
            if (li_ind_MG >= 0)
            {
                if (OrderType() <= OP_SELL)
                {
                    ld_Profit = OrderProfit() + OrderSwap() + OrderCommission();
                    ar_Profit[li_ind_MG] = ld_Profit;
                    gd_ProfitALL += ld_Profit;
                    ar_MyOrders[li_ind_MG]++;
                    li_Ord++;
                }
            }
        }
    }
//----
    return (li_Ord);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Author   : TarasBY                                                               |
//+-----------------------------------------------------------------------------------+
//|        Calculate the average size of the candles for the appointed time           |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fGet_AverageCandle (double& fda_AvarageCandle[], // �������� ��������
                           int fi_Day,                // ���������� �������������� ����
                           string fs_Symbol,          // ������
                           int fi_Period = 0)         // ������
{
    static datetime ldt_NewDay;
    if (ldt_NewDay == iTime (fs_Symbol, fi_Period, 0))
    {return;}
    ldt_NewDay = iTime (fs_Symbol, fi_Period, 0);
//----
    double ld_OPEN, ld_CLOSE, ld_HIGH, ld_LOW;
    datetime ldt_Begin = iTime (fs_Symbol, PERIOD_D1, fi_Day);
    int      li_cnt_Bar = iBarShift (fs_Symbol, fi_Period, ldt_Begin);
//----
    ArrayInitialize (fda_AvarageCandle, 0.0);
    for (int li_BAR = 1; li_BAR < li_cnt_Bar; li_BAR++)
    {
        //---- ������� ��� ���� �����
        ld_OPEN = iOpen (fs_Symbol, fi_Period, li_BAR);
        ld_CLOSE = iClose (fs_Symbol, fi_Period, li_BAR);
        fda_AvarageCandle[0] += MathAbs (ld_OPEN - ld_CLOSE);
        //---- ������� ��� �����
        ld_HIGH = iHigh (fs_Symbol, fi_Period, li_BAR);
        ld_LOW = iLow (fs_Symbol, fi_Period, li_BAR);
        fda_AvarageCandle[1] += (ld_HIGH - ld_LOW);
    }
    fda_AvarageCandle[0] /= (li_cnt_Bar - 1);
    fda_AvarageCandle[1] /= (li_cnt_Bar - 1);
//----
    return;
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//+===================================================================================+
//|***********************************************************************************|
//| SECTION: General Functions                                                        |
//|***********************************************************************************|
//+===================================================================================+
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Author   : TarasBY                                                               |
//+-----------------------------------------------------------------------------------+
//|  Descript : Start getting into the cycle of market prices.                        |
//+-----------------------------------------------------------------------------------+
//|  Options  :                                                                       |
//|    iPrice : 0 - Bid; 1 - Ask                                                      |
//|    isTrade: real trade or optimization\testing                                    |
//|    Symb   : Symbol                                                                |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
double fGet_TradePrice (int iPrice, bool isTrade, string Symb = "")
{
    double ld_Price = 0.0;
//----
    if (Symb == "") {Symb = Symbol();}
    RefreshRates();
    switch (iPrice)
    {
        case 0:
            if (isTrade)
            {
                while (ld_Price == 0.0)
                {
                    if (Symb == Symbol()) {ld_Price = Bid;} else {ld_Price = MarketInfo (Symb, MODE_BID);}
                    if (!IsExpertEnabled() || IsStopped())
                    {break;}
                    Sleep (300); RefreshRates();
                }
            }
            else
            {if (Symb == Symbol()) {return (Bid);} else {return (MarketInfo (Symb, MODE_BID));}}
            break;
        case 1:
            if (isTrade)
            {
                while (ld_Price == 0.0)
                {
                    if (Symb == Symbol()) {ld_Price = Ask;} else {ld_Price = MarketInfo (Symb, MODE_ASK);}
                    if (!IsExpertEnabled() || IsStopped())
                    {break;}
                    Sleep (300); RefreshRates();
                }
            }
            else
            {if (Symb == Symbol()) {return (Ask);} else {return (MarketInfo (Symb, MODE_ASK));}}
            break;
    }
//----
    return (ld_Price);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Author   : TarasBY                                                               |
//+-----------------------------------------------------------------------------------+
//|        Returns an array of STRING from string, divided sDelimiter                 |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int fSplitStrToStr (string aString, string& aArray[], string aDelimiter = ",")
{
    string tmp_str = "", tmp_char = "";
//----
    ArrayResize (aArray, 0);
    for (int i = 0; i < StringLen (aString); i++)
    {
        tmp_char = StringSubstr (aString, i, 1);
        if (tmp_char == aDelimiter)
        {
            if (StringTrimLeft (StringTrimRight (tmp_str)) != "")
            {
                ArrayResize (aArray, ArraySize (aArray) + 1);
                aArray[ArraySize (aArray) - 1] = tmp_str;
            }
            tmp_str = "";
        }
        else
        {
            if (tmp_char != " ")
            {tmp_str = tmp_str + tmp_char;}
        }
    }
    if (StringTrimLeft (StringTrimRight (tmp_str)) != "")
    {
        ArrayResize (aArray, ArraySize (aArray) + 1);
        aArray[ArraySize (aArray) - 1] = tmp_str;
    }
//----
    return (ArraySize (aArray));
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Author   : Kim Igor V. aka KimIV,  http://www.kimiv.ru                           |
//+-----------------------------------------------------------------------------------+
//|  Version  : 01.09.2005                                                            |
//|  Descript : Returns the name of the trading                                       |
//+-----------------------------------------------------------------------------------+
//|  Options  :                                                                       |
//|    op - transaction identifier                                                    |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
string fGet_NameOP (int op) 
{
    switch (op) 
    {
        case OP_BUY      : return ("BUY");
        case OP_SELL     : return ("SELL");
        case OP_BUYLIMIT : return ("BUYLIMIT");
        case OP_SELLLIMIT: return ("SELLLIMIT");
        case OP_BUYSTOP  : return ("BUYSTOP");
        case OP_SELLSTOP : return ("SELLSTOP");
    }
    return (StringConcatenate ("None (", op, ")"));
//----
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Author   : TarasBY                                                               |
//+-----------------------------------------------------------------------------------+
//|        Returns the name of the timeframe                                          |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
string fGet_NameTF (int TF)
{
//----
    switch (TF)
    {
        case PERIOD_M1:  return ("M1");
		  case PERIOD_M5:  return ("M5");
		  case PERIOD_M15: return ("M15");
		  case PERIOD_M30: return ("M30");
		  case PERIOD_H1:  return ("H1");
		  case PERIOD_H4:  return ("H4");
		  case PERIOD_D1:  return ("D1");
		  case PERIOD_W1:  return ("W1");
		  case PERIOD_MN1: return ("MN1");
	 }
//----
	 return ("UnknownPeriod");
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Author    : TarasBY                                                              |
//+-----------------------------------------------------------------------------------+
//|  Version   : 27.10.2009                                                           |
//|  Descript  : Commits the changing checked the double parameter                    |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fCCV_D (double param, int ix)
{
    static double cur_param[10];
    static bool   lb_first = true;
//---- 
    //---- ��� ������ ������� �������������� ������
    if (lb_first)
    {
        for (int l_int = 0; l_int < ArraySize (cur_param); l_int++)
        {cur_param[l_int] = 0;}
        lb_first = false;
    }
    if (cur_param[ix] != param)
    {cur_param[ix] = param; return (true);}
//---- 
    return (false);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Author   : TarasBY                                                               |
//+-----------------------------------------------------------------------------------+
//|        Identify the status of the advisor                                         |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fGet_IsStatusTrade (bool& fb_PrintCom,         // ���� ���������� ������ ������������ �� ������
                         bool& fb_DrawObject,       // ���� ���������� ������ ������������ �� ������
                         bool& fb_InfoPrint,        // ���� ���������� ������ ������������ ��� �� ������, ��� �� ������
                         bool& fb_SoundAlert,       // ���� ���������� ������ ��������� ������������� �������
                         bool& fb_RealTrade)        // ������������� ������ � �������/�� � �������
{
//----
    //---- ��������� ������� GV-����������
    if (IsTesting())
    {gs_NameGV = gs_NameGV + "_t";}
    if (IsDemo())
    {gs_NameGV = gs_NameGV + "_d";}
    //---- ������������� ����� ��� ������ � �����������
    if (IsTesting() || IsOptimization())
    {
        fb_RealTrade = False;
        //---- ��������� �� ������������ ������� ��� ������������ � �����������
        if (IsOptimization())
        {fb_PrintCom = false;}
        if ((IsTesting() && !IsVisualMode()) || IsOptimization())
        {
            fb_DrawObject = false;
            gb_VirualTrade = true;
        }
        if (IsVisualMode())
        //---- �������������� ������� �������� ������ �������
        {ArrayInitialize (gia_CommTime, TimeCurrent());}
        fb_SoundAlert = false;
    }
    //---- ���������� ���������� ������������
    if (fb_PrintCom || fb_DrawObject)
    {fb_InfoPrint = true;}
//----
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Author   : TarasBY                                                               |
//+-----------------------------------------------------------------------------------+
//|       Check Magic                                                                 |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int fCheck_MyMagic (int fi_Magic, int ar_Magic[])
{
//----
    for (int li_int = 0; li_int < ArraySize (ar_Magic); li_int++)
    {if (fi_Magic == ar_Magic[li_int]) {return (li_int);}}
//----
    return (-1);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Author   : TarasBY                                                               |
//+-----------------------------------------------------------------------------------+
//|        Converting a variable from double to string c normalized by the minimum    |
//| lot of bits                                                                       |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
string DSDig (double v) {return (DoubleToStr (v, LotDecimal()));} 
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Author    : Kim Igor V. aka KimIV,  http://www.kimiv.ru                          |
//+-----------------------------------------------------------------------------------+
//|  Version   : 01.02.2008                                                           |
//|  Descript  : Returns one of two values depending of type terms.                   |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
string IIFs (bool condition, string ifTrue, string ifFalse)
{if (condition) {return (ifTrue);} else {return (ifFalse);}}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//+===================================================================================+
//|***********************************************************************************|
//| SECTION: Working with arrays                                                      |
//|***********************************************************************************|
//+===================================================================================+
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Author   : TarasBY                                                               |
//+-----------------------------------------------------------------------------------+
//|  UNI:               Initialize the array STRING                                   |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void InitializeArray_STR (string& PrepareArray[], string Value = "")
{
    int l_int, size = ArraySize (PrepareArray);
//----
    for (l_int = 0; l_int < size; l_int++)
    {PrepareArray[l_int] = Value;}
//----
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Author   : TarasBY                                                               |
//+-----------------------------------------------------------------------------------+
//|  UNI:               Sort the indices of the array in descending order             |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fRun_Sort (int& ar_INDEX[], double fda_Value[])
{
    int li_IND, li_int, li_tmp, li_size = ArraySize (fda_Value);
//----
    for (li_IND = 0; li_IND < li_size; li_IND++)
    {ar_INDEX[li_IND] = li_IND;}
   
    for (li_IND = 0; li_IND < li_size; li_IND++)
    {
		for (li_int = li_IND + 1; li_int < li_size; li_int++)
		{
			if (fda_Value[ar_INDEX[li_IND]] < fda_Value[ar_INDEX[li_int]])
			{
				li_tmp = ar_INDEX[li_int]; 
				ar_INDEX[li_int] = ar_INDEX[li_IND]; 
				ar_INDEX[li_IND] = li_tmp;
			}
		}
	}
//----
    return;
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//+===================================================================================+
//|***********************************************************************************|
//| SECTION: Money Management                                                         |
//|***********************************************************************************|
//+===================================================================================+
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Author   : TarasBY                                                               |
//+-----------------------------------------------------------------------------------+
//|           The main function of receipt of lot size                                |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
double fGet_SizeLot (string& fs_Info,                // ������������ ����������
                     double fd_Lot,                  // ������������� ������ ����
                     int fi_Type,                    // ��� ������������ ������
                     double fd_BaseBalance,          // ������� ������� + Pribul
                     int fi_MG,                      // Magic
                     int& fi_curOrdersInSeries,      // ���������� ������� � ��������� �����
                     int fi_MINMarginPercent,        // ���������� ���������� �������� ��������� ������� � %
                     int fi_MAXZagruzkaDepoPercent,  // ����������� ���������� �������� ��������� ������� � %
                     string fs_NameGV = "")          // ������� GV-���������� ���������
{
    //---- ���� MM �� ������������ - �������
    if (!UseMM)
    {return (fd_Lot);}
    double ld_Lots, Money = 0, ld_LastLot;
    int    err = GetLastError();
//----
    switch (MeansType)
    {
        case 1: Money = gd_Balance; break;
        case 2: Money = gd_Equity; break;
        case 3: Money = gd_FreeMargin; break;
        case 4: Money = RiskDepo; break; 
        case 5: Money = fd_BaseBalance; break; 
    }                
    switch (LotsWayChoice)
    {
        case 0: ld_Lots = fd_Lot; break;
        case 1: // ������������� ������� �� ��������
        case 2: // ������������� (�� �������������) ������� �� ��������
            if (MeansType == 3)
            {ld_Lots = (gd_FreeMargin * LotsPercent) / (MarketInfo (Symbol(), MODE_MARGINREQUIRED) * 100.0);}
            else
            {ld_Lots = Money * MathMin (AccountLeverage(), 100.0) * LotsPercent / (100000.0 * 100.0);}
            break;
    }
    //---- ����������� ���
    ld_Lots = fLotsNormalize (ld_Lots);
    if (ld_Lots < MinLot)
    {
        fs_Info = StringConcatenate ("Estimated Lot[", DSDig (ld_Lots), "] < MinLot[", DSDig (MinLot), "] !!!");
        ld_Lots = MinLot;
    }
    ld_Lots = MathMin (ld_Lots, MaxLot);
    //---- 
//----
    return (ld_Lots);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Author   : TarasBY                                                               |
//+-----------------------------------------------------------------------------------+
//|       We make the normalization of the lot                                        |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
double fLotsNormalize (double fd_Lots)
{
    fd_Lots -= gd_MINLOT;
    fd_Lots /= gd_LOTSTEP;
    fd_Lots = MathRound (fd_Lots);
    fd_Lots *= gd_LOTSTEP;
    fd_Lots += gd_MINLOT;
    fd_Lots = NormalizeDouble (fd_Lots, gi_dig);
    fd_Lots = MathMax (fd_Lots, gd_MINLOT);
    fd_Lots = MathMin (fd_Lots, gd_MAXLOT);
//----
    return (fd_Lots);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Author   : TarasBY                                                               |
//+-----------------------------------------------------------------------------------+
//|  Descript : Get prices, with which will work advisor                              |
//+-----------------------------------------------------------------------------------+
//|  Options  :                                                                       |
//|    ar_Price[]        : ar_Price[0] - Bid; ar_Price[1] - Ask                       |
//|    fi_VariantPrice   : 0 - Bid\Ask; 1 - Open[0]; 2 - Close [1]; 3 - Close[0]      |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fGet_MineTradePrice (int fi_VariantPrice, double& ar_Price[])
{
    double ld_Price;
    static datetime ldt_NewBar;
//----
    gd_RealSPREAD = MarketInfo (gs_Symbol, MODE_SPREAD) * gd_Point;
    switch (fi_VariantPrice)
    {
        case 0: 
            RefreshRates();
            ld_Price = fGet_TradePrice (0, RealTrade, gs_Symbol);
            break;
        case 1: ld_Price = Open[0]; break;
        case 2: ld_Price = Close[1]; break;
        case 3: ld_Price = Close[0]; break;
        case 4: ld_Price = iOpen (gs_Symbol, NewBarInPeriod, 0); break;
        case 5: 
            ar_Price[0] = iHigh (gs_Symbol, NewBarInPeriod, 1);
            ar_Price[1] = iLow (gs_Symbol, NewBarInPeriod, 1);
            return;
        case 6: 
            ar_Price[0] = iHigh (gs_Symbol, ControlPeriod, 0);
            ar_Price[1] = iLow (gs_Symbol, ControlPeriod, 0);
            return;
        case 7: 
            ar_Price[0] = iHigh (gs_Symbol, 1, 1);
            ar_Price[1] = iLow (gs_Symbol, 1, 1);
            return;
    }
    ar_Price[0] = ld_Price;
    ar_Price[1] = NormalizeDouble (ld_Price + gd_RealSPREAD, gi_Digits);
    double ld_Ask = fGet_TradePrice (1, RealTrade, gs_Symbol);
    //---- ������� ������������ ������������ Ask
    if ((gd_RealSPREAD == 0.0 || NormalizeDouble (ld_Ask - ar_Price[1], gi_Digits) > 0.0) && RealTrade)
    {
        if (ldt_NewBar != gdt_BeginBar_H1)
        {
            Print ("Attention: Price[Bid] = ", DoubleToStr (ar_Price[0], gi_Digits), 
            " | Price[Bid+spread] = ", DoubleToStr (ar_Price[1], gi_Digits), " | Ask = ", DoubleToStr (ld_Ask, gi_Digits),
            " | spread = ", DoubleToStr (gd_RealSPREAD / gd_Point, 0));
            ldt_NewBar = gdt_BeginBar_H1;
        }
        if (ld_Ask > 0.0)
        {ar_Price[1] = ld_Ask;}
    }
//----
    return;
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Author   : TarasBY                                                               |
//+-----------------------------------------------------------------------------------+
//|       Calculate the earned income (if at all earned)                              |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
double fCalculate_Pribul (int& fi_HistoryOrders,       // ������������ ������ �������� ������� �� ��� ����� �������
                          int fi_Magic = -1,           // ������ Magic
                          int fi_OP = -1,              // ��� (BUY\SELL) ����������� �������
                          datetime fdt_TimeBegin = 0)  // ������ �������, � �������� ���������� ������
{
    int    err = GetLastError(), history_total = OrdersHistoryTotal();
    double ld_Pribul = 0.0, ld_ALLPribul = 0.0;
    string ls_Name;
//----
    //fCalculate_Pribul (gi_HistoryOrders, MG);
    fi_HistoryOrders = 0;
    for (int li_int = history_total - 1; li_int >= 0; li_int--)
    {
        if (OrderSelect (li_int, SELECT_BY_POS, MODE_HISTORY))
        {
            if (Symbol() == OrderSymbol())
            {
                if (OrderType() < 2 && (fi_OP < 0 || OrderType() == fi_OP))
                {
                    if (OrderMagicNumber() == fi_Magic || fi_Magic < 0)
                    {
                        if (fdt_TimeBegin < OrderCloseTime())
                        {
                            //---- ������� ����� ������
                            ld_Pribul = OrderProfit() + OrderSwap() + OrderCommission();
                            ld_ALLPribul += ld_Pribul;
                            fi_HistoryOrders++;
                            //---- ���������� �������� GV-����������
                            if (li_int >= history_total - (cnt_MG + 5))
                            {
                                ls_Name = StringConcatenate (OrderTicket(), "_#SL");
                                if (GlobalVariableCheck (ls_Name))
                                {GlobalVariableDel (ls_Name);}
                            }
                        }
                        //---- ��� ������ ������� ���� ������ �������� �����
                        if (gb_first)
                        {
                            if (gdt_BeginTrade > OrderOpenTime())
                            {gdt_BeginTrade = OrderOpenTime();}
                        }
                    }
                }
            }
        }
    }
    //---- ������������ ��������� ������
    fGetLastError (gsa_Comment, "fCalculate_Pribul()", ind_ERR);
//----
    return (ld_ALLPribul);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Author   : TarasBY                                                               |
//+-----------------------------------------------------------------------------------+
//|       Determining the value of the pip                                            |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
double fGet_PipsValue()
{
    double ld_Price, ld_TickValue, ld_pips;
//----
    //---- ���� ����� ������
    if (OrderCloseTime() > 0)
    {ld_Price = OrderClosePrice();}
    else
    {if (OrderType() == OP_BUY) {ld_Price = Bid;} else {ld_Price = Ask;}}
    ld_pips = ((OrderOpenPrice() - ld_Price) / gd_Point);
    if (ld_pips == 0) {return (1);}
    ld_TickValue = MathAbs (OrderProfit() / ld_pips);
    if (ld_TickValue == 0) {return (1);}
//----
    return (ld_TickValue);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Author   : TarasBY                                                               |
//+-----------------------------------------------------------------------------------+
//|        The function for determining the minimum number of digits of the Lot       |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int LotDecimal()
{return (MathCeil (MathAbs (MathLog (MarketInfo (Symbol(), MODE_LOTSTEP)) / MathLog (10))));}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Author   : TarasBY                                                               |
//+-----------------------------------------------------------------------------------+
//|       We Share the digits of numbers with spaces                                  |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
string fSplitField (string fs_Value)
{
    string ls_Begin = fs_Value, ls_End, ls_tmp;
    int    li_N1 = StringFind (ls_Begin, "."), li_Len;
//----
    //---- �������� ������� ����� � ������ ��� ������� (�� �����)
    if (li_N1 > 0)
    {
        li_N1 = MathMax (0, li_N1 - 3);
        ls_End = StringSubstr (ls_Begin, li_N1);
        if (li_N1 > 0)
        {ls_Begin = StringSubstr (ls_Begin, 0, li_N1);}
        else {return (fs_Value);}
    }
    li_Len = StringLen (ls_Begin);
    if (li_Len <= 0) {return (fs_Value);}
    while (li_Len > 3)
    {
        ls_tmp = StringSubstr (ls_Begin, li_Len - 3);
        ls_End = StringConcatenate (ls_tmp, " ", ls_End);
        ls_Begin = StringSubstr (ls_Begin, 0, li_Len - 3);
        li_Len = StringLen (ls_Begin);
    }
//----
    return (StringTrimLeft (StringConcatenate (ls_Begin, " ", ls_End)));
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//+===================================================================================+
//|***********************************************************************************|
//| SECTION: Working with errors                                                      |
//|***********************************************************************************|
//+===================================================================================+
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Author   : TarasBY                                                               |
//+-----------------------------------------------------------------------------------+
//|     Obtain the number and description of the last error and then his puts in an   |
//|  array of comments                                                                |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int fGetLastError (string& Comm_Array[], string Com = "", int index = -1)
{
    if (gb_VirualTrade) {return (0);}
    int err = GetLastError();
//---- 
    if (err > 0)
    {
        string ls_err = StringConcatenate (Com, ": Error � ", err, ": ", ErrorDescription (err));
        Print (ls_err);
        if (index >= 0)
        {Comm_Array[index] = ls_err;}
    }
//---- 
    return (err);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//+===================================================================================+
//|***********************************************************************************|
//| SECTION: Functions of services                                                    |
//|***********************************************************************************|
//+===================================================================================+
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Author   : TarasBY                                                               |
//+-----------------------------------------------------------------------------------+
//|        Prints comments on the chart                                               |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fCommentInChart (string& ar_Comment[], int& ar_ComTime[])
{
    static string lsa_Time[], lsa_Comment[], lsa_CommTime[], lsa_tmp[], ls_BeginTrade;
    static bool   lb_first = true;
    static int    li_size, li_size_CommTime, li_Period = 60;
    string ls_CTRL = "", ls_BLOCK_Comment, ls_Comment = "", ls_Statistic = "",
           ls_Error = "", ls_time = "", ls_sign, ls_rowEnd,
           ls_row = "���������������������������������\n",
           ls_PSI = "������������� PSI�TarasBY �������������\n";
//----
    //---- ��� ������ ������� ��������� ������� �������
    if (lb_first)
    {
        if (NewBarInPeriod == 0) {li_Period *= Period();} else {li_Period *= NewBarInPeriod;}
        li_size = ArraySize (ar_Comment);
        ArrayResize (lsa_Time, li_size);
        ArrayResize (lsa_Comment, li_size);
        InitializeArray_STR (lsa_Comment);
        InitializeArray_STR (lsa_Time);
        li_size_CommTime = ArraySize (ar_ComTime);
        ArrayResize (lsa_CommTime, li_size_CommTime);
        ArrayResize (lsa_tmp, li_size_CommTime);
        InitializeArray_STR (lsa_CommTime);
        InitializeArray_STR (lsa_tmp);
        if (gdt_BeginTrade == 0)
        {gdt_BeginTrade = gdt_curTime;}
        ls_BeginTrade = StringConcatenate ("Begin Trade : ", TimeToStr (gdt_BeginTrade), "\n");
        lb_first = false;
    }
    //---- ���� ������������
    for (int li_MSG = 0; li_MSG < li_size; li_MSG++)
    {
        //---- ���������� ����� ���������� ���������
        if (StringLen (ar_Comment[li_MSG]) > 0)
        {
            if (ar_Comment[li_MSG] != lsa_Comment[li_MSG])
            {lsa_Comment[li_MSG] = ar_Comment[li_MSG];}
            if (li_MSG == li_size - 1) {ls_sign = "";} else {ls_sign = " : ";}
            lsa_Time[li_MSG] = StringConcatenate (TimeToStr (gdt_curTime), ls_sign);
            ar_Comment[li_MSG] = "";
        }
        //---- ��������� ���� ������������
        if (li_MSG < li_size - 1)
        {if (StringLen (lsa_Comment[li_MSG]) > 0) {ls_Comment = StringConcatenate (ls_Comment, lsa_Time[li_MSG], lsa_Comment[li_MSG], "\n");}}
        //---- ��������� ���� ������
        else if (li_MSG == li_size - 1)
        {
            //---- ������ 2 ���� ���������� �� ������ �������
            if (gdt_curTime > StrToTime (lsa_Time[li_MSG]) + 7200)
            {lsa_Comment[li_MSG] = "";}
            if (StringLen (lsa_Comment[li_MSG]) > 0) {ls_Error = StringConcatenate ("ERROR:  ", lsa_Time[li_MSG], "\n", lsa_Comment[li_MSG], "\n", ls_row);}
        }
    }
    //---- ������ �������� �� �������� ������ ���������
    if (NewBarInPeriod >= 0) {ls_time = TimeToStr (gdt_NewBar + li_Period);}
    //---- ��������� ��� ����� ������������
    ls_BLOCK_Comment = StringConcatenate (ExpertName, "\n", gs_Info, ls_time, "\n",
                 ls_BeginTrade,
                 ls_row,
                 //---- ���� ������������
                 ls_Comment,
                 ls_row,
                 //---- ���� ����������� ������
                 "          PROFIT    = ", gs_sign, " ", fSplitField (DoubleToStr (gd_ProfitALL, 1)), "[", gi_MyOrders, "]\n",
                 "          RESULT    = ", gs_sign, " ", fSplitField (DoubleToStr (gd_PribulALL, 1)), "[", fSplitField (gi_HistoryOrders), "]\n",
                 ls_PSI,
                 //---- ���������� ������
                 ls_Error);
    //---- ������� �� ���� �������������� ���� ������������
    Comment (ls_BLOCK_Comment);
//----
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Author   : TarasBY                                                               |
//+-----------------------------------------------------------------------------------+
//|        Prepare for prints to log and on the chart comments                        |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
string fPrepareComment (string sText = "", bool bConditions = false)
{if (bConditions && StringLen (sText) > 0) {return (sText);}}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Author   : TarasBY                                                               |
//+-----------------------------------------------------------------------------------+
//|        Prints to log and on the chart comments                                    |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fPrintAndShowComment (string& Text, bool Show_Conditions, bool Print_Conditions, string& s_Show[], int ind = -1)
{
    if ((Show_Conditions || Print_Conditions))
    {
        if (StringLen (Text) > 0)
        {
            if (ind >= 0 && Show_Conditions)
            {s_Show[ind] = Text;}
            if (Print_Conditions)
            {Print (Text);}
        }
    }
    //---- ������� ����������
    Text = "";
//---- 
    return;
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Author   : TarasBY                                                               |
//+-----------------------------------------------------------------------------------+
//|        Create information for displaying settings on the chart                    |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fCheckInfo()
{
    static double ld_pre_Lot = 0;
//----
    if (ShowCommentInChart)
    {
        if (ld_pre_Lot != LotsMM)
        {
            string ls_Trail = "", ls_MM, ls_addConditions;
            ls_MM = IIFs (UseMM, StringConcatenate ("MM (", DSDig (LotsMM), IIFs ((LotsWayChoice == 2), StringConcatenate ("/", gi_curOrdersInSeries), ""), ")", fGet_MeansType (MeansType, LotsPercent)), "L: " + DSDig (LotsMM));
            ls_addConditions = StringConcatenate (IIFs ((StringLen (ls_Trail) > 0), ls_Trail, ""),
            IIFs ((StringLen (ls_MM) > 0), " - ", ""),
            IIFs ((StringLen (ls_MM) > 0), ls_MM, ""));
            //---- ������� " - " �� ������ ������ (���� ����)
            ls_addConditions = IIFs ((StringFind (ls_addConditions, " - ") == 0), StringSubstr (ls_addConditions, 3), ls_addConditions);
            //---- ��������� �������������� ������
            gs_Info = StringConcatenate (gs_NameGV, ":     STOP`s", " (", StopLoss, "/", TakeProfit, ")", "\n",
            IIFs ((StringLen (ls_addConditions) > 0), StringConcatenate (ls_addConditions, "\n"), ""),
            "Lots: MIN = ", DSDig (gd_MINLOT), " | MAX = ", DSDig (gd_MAXLOT), " | STEP = ", gd_LOTSTEP, "\n",
            IIFs ((NewBarInPeriod < 0), "Work`s on each tick", "PAUSE to: "));
            ld_pre_Lot = LotsMM;
        }
    }
//---- 
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Author   : TarasBY                                                               |
//+-----------------------------------------------------------------------------------+
//|           Get the size of funds for the formation of the lot (for statistics)     |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
string fGet_MeansType (int fi_Variant, int fi_Percent)
{
    string ls_Percent = "";
//----
    if (LotsWayChoice == 1 || LotsWayChoice == 2)
    {
        ls_Percent = StringConcatenate (fi_Percent, "% ");
        switch (fi_Variant)
        {
            case 1: return (StringConcatenate (": ", ls_Percent, "from Balance = ", gs_sign, fSplitField (DoubleToStr (gd_Balance, 1))));
            case 2: return (StringConcatenate (": ", ls_Percent, "from Equity = ", gs_sign, fSplitField (DoubleToStr (gd_Equity, 1))));
            case 3: return (StringConcatenate (": ", ls_Percent, "from FreeMargin = ", gs_sign, fSplitField (DoubleToStr (gd_FreeMargin, 1))));
            case 4: return (StringConcatenate (": ", ls_Percent, "from RiskDepo = ", gs_sign, fSplitField (DoubleToStr (RiskDepo, 1))));
            case 5: return (StringConcatenate (": ", ls_Percent, "from BaseBalance = ", gs_sign, fSplitField (DoubleToStr (gd_BaseBalance, 1))));
        }
    }
//----
    return ("");
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Author   : TarasBY                                                               |
//+-----------------------------------------------------------------------------------+
//|        Collect statistics                                                         |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fGet_Statistic()
{
    int err = GetLastError();
//----
    gd_Balance = AccountBalance();
    gd_Equity = AccountEquity();
    gd_FreeMargin = AccountFreeMargin();
    gd_Margin = AccountMargin();
    //---- ��������� ����� � "��������� ������"
    gdt_curTime = TimeCurrent();
    if (fCCV_D (OrdersHistoryTotal(), 3))
    {
        //---- ������� ���������� �� �������� �������
        gd_PribulALL = 0.0;
        double ld_Pribul = 0.0;
        int li_tmp_HistoryOrders = 0;
        gi_HistoryOrders = 0;
        ArrayInitialize (gia_HistoryOrders, 0);
        for (int li_MG = 0; li_MG < cnt_MG; li_MG++)
        {
            ld_Pribul = fCalculate_Pribul (li_tmp_HistoryOrders, gia_MG[li_MG]);
            gda_Pribul[li_MG] = ld_Pribul;
            gd_PribulALL += ld_Pribul;
            gi_HistoryOrders += li_tmp_HistoryOrders;
            gia_HistoryOrders[li_MG] += li_tmp_HistoryOrders;
            gda_BaseBalance[li_MG] = BaseBalance + ld_Pribul;
        }
        gd_BaseBalance = BaseBalance + gd_PribulALL;
        //---- �� ����������� ������ ������� ����, ������������� ��������� ����������
        if (Use_Prioritity)
        {fRun_Sort (gia_Priority, gda_Pribul);}
    }
    //---- ��������� ������� ����� �������
    gd_MAXBalance = MathMax (gd_MAXBalance, gd_Balance);
    //---- �������� ������� �������� ���������� ��� ����������� �� �������
    fCheckInfo();
    //---- ������������ ��������� ������
    fGetLastError (gsa_Comment, "fGet_Statistic()", ind_ERR);
//----
    return;
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//+===================================================================================+
//|***********************************************************************************|
//| SECTION: Scheduler                                                                |
//|***********************************************************************************|
//+===================================================================================+
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Author   : TarasBY                                                               |
//+-----------------------------------------------------------------------------------+
//|         Function to control the time of the advisor                               |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool IsTradeTime (int OpenHour, int CloseHour)
{
    if (!TimeControl)
    {return (true);}
//----
    int li_Hour = TimeHour (gdt_curTime);
//----
    if (OpenHour < CloseHour && (li_Hour < OpenHour || li_Hour >= CloseHour))
    {return (FALSE);}
    if (OpenHour > CloseHour && (li_Hour < OpenHour && li_Hour >= CloseHour))
    {return (FALSE);}
    if (OpenHour == 0)
    {CloseHour = 24;}
    if (li_Hour == CloseHour - 1 && TimeMinute (gdt_curTime) >= 45)
    {return (FALSE);}
//----
    return (TRUE);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Author   : TarasBY                                                               |
//+-----------------------------------------------------------------------------------+
//|         Function to control the "no time" advisor                                 |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool IsBadTime (int ar_BadDay[], int ar_BadTime[])
{
//----
    if (!TimeControl)
    {return (false);}
    for (int li_DAY = 0; li_DAY < ArraySize (ar_BadDay); li_DAY++)
    {
        if (TimeDayOfWeek (gdt_curTime) == ar_BadDay[li_DAY])
        {return (TRUE);}
    }
    for (int li_HOUR = 0; li_HOUR < ArraySize (ar_BadTime); li_HOUR++)
    {
        if (TimeHour (gdt_curTime) == ar_BadTime[li_HOUR])
        {return (TRUE);}
    }
//----
    return (FALSE);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+

