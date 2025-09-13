//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|                                                            b-PSI@TrailSymbol.mqh  |
//|                                    Copyright � 2011-12, Igor Stepovoi aka TarasBY |
//|                                   ������� �� "����" ������� �� I_D / ���� ������  |
//|                                                 http://codebase.mql4.com/ru/1101  |
//|  12.04.2011  ���������� ������� ��������� "� ����� �������".                      |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|   ������ ������� ������������ ��� �������������� �������������. ���������� �����- |
//|���� ������ ��� �������� ����� ������ (TarasBY). �������������� ��������� ���� ��- |
//|������� ������ ��� ������� ���������� ������� ������, ������ � ����� ������.  ���- |
//|���� ���������� ��� ��������� � ������ ���������.                                 |
//|   ����� �� ���� ��������������� �� ��������� ������, ���������� � ���������� ��- |
//|����������� ����������.                                                            |
//|   �� ���� ��������, ��������� � ������� ����������, ����������� ��� ������������� |
//|�� � ��������� ���������� �� Skype: TarasBY ��� e-mail.                           |
//+-----------------------------------------------------------------------------------+
//|   This product is intended for non-commercial use.  The publication is only allo- |
//|wed when you specify the name of the author (TarasBY). Edit the source code is va- |
//|lid only under condition of preservation of the text, links and author's name.     |
//|   Selling a module or(and) parts of it PROHIBITED.                                |
//|   The author is not liable for any damages resulting from the use of a module.    |
//|   For all matters relating to the work of the module, comments or suggestions for |
//|their improvement in the contact Skype: TarasBY or e-mail.                         |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
#property copyright "Copyright � 2008-12, TarasBY WM R418875277808; Z670270286972"
#property link      "taras_bulba@tut.by"
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|                 *****        ��������� ����������         *****                   |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
extern string SETUP_Trailing        = "================== TRAILING ===================";
extern int    N_Trailing               = 0;          // ������� ������������� ���������
extern int    Trail_TF             = PERIOD_H1;  // ������ (�������), �� ������� ���������� �������
extern bool   TrailLOSS_ON             = FALSE;      // ��������� ��������� �� LOSS`e
extern int    TrailLossAfterBar        = 12;         // ����� ������ �� ����� ���� (����� ��������) �������� ������� �� �����
extern int    TrailStep                = 5;          // ��� ��������� (����������� ����������)
extern int    BreakEven                = 30;         // �������, �� ������� ����������� ��������� �� ������� � ProfitMIN
extern string Setup_TrailStairs     = "------------- N0 - TrailingStairs -------------";
extern int    TrailingStop             = 50;         // ���������� ����-������, ���� ����� ��������� ���
extern string Setup_TrailByFractals = "----------- N1 - TrailingByFractals -----------";
extern int    BarsInFractal            = 0;          // ���������� ����� � ��������
extern int    Indent_Fr                = 0;          // ������ (��.) - ���������� �� ����\��� �����, �� ������� ����������� SL (�� 0)
extern string Setup_TrailByShadows  = "----------- N2 - TrailingByShadows ------------";
extern int    BarsToShadows            = 0;          // ���������� �����, �� ����� ������� ���������� ������������� (�� 1 � ������)
extern int    Indent_Sh                = 0;          // ������ (��.) - ���������� �� ����\��� �����, �� ������� ����������� SL (�� 0)
extern string Setup_TrailUdavka     = "------------- N3 - TrailingUdavka -------------";
extern int    Level_0                  = 40;         // �� ���� ������ �������� ����������
extern int    Distance_1               = 70;         // �� ���� ���������� ������ ��������� = Level_0
extern int    Level_1                  = 30;         // ������ ��������� �\� Distance_1 �� Distance_2
extern int    Distance_2               = 100;        // �� Distance_1 �� Distance_2 ������ ��������� = Level_1
extern int    Level_2                  = 20;         // ����� ��������� Distance_2 ������ ��������� = Level_2
extern string Setup_TrailByTime     = "------------- N4 - TrailingByTime -------------";
extern int    Interval                 = 60;         // �������� (�����), � ������� ������������� SL
extern int    TimeStep                 = 5;          // ��� ��������� (�� ������� �������) ������������ SL
extern string Setup_TrailByATR      = "------------- N5 - TrailingByATR --------------";
extern int    ATR_Period1              = 9;          // ������ ������� ATR (������ 0; ����� ���� ����� ATR_Period2, �� ����� ������� �� ����������)
extern int    ATR_shift1               = 2;          // ��� ������� ATR ����� "����" (��������������� ����� �����)
extern int    ATR_Period2              = 14;         // ������ ������� ATR (������ 0)
extern int    ATR_shift2               = 3;          // ��� ������� ATR ����� "����", (��������������� ����� �����)
extern double ATR_coeff                = 2.5;        // 
extern string Setup_TrailRatchetB   = "------------ N6 - TrailingRatchetB ------------";
extern int    ProfitLevel_1            = 20;
extern int    ProfitLevel_2            = 30;
extern int    ProfitLevel_3            = 50;
extern int    StopLevel_1              = 2;
extern int    StopLevel_2              = 5;
extern int    StopLevel_3              = 15;
extern string Setup_TrailByPriceCh  = "--------- N7 - TrailingByPriceChannel ---------";
extern int    BarsInChannel            = 10;        // ������ (���-�� �����) ��� �������� ������� � ������ ������ ������
extern int    Indent_Pr                = 15;        // ������ (�������), �� ������� ����������� �������� �� ������� ������
extern string Setup_TrailByMA       = "-------------- N8 - TrailingByMA --------------";
extern int    MA_Period                = 14;        // 2-infinity, ����� �����
extern int    MA_Shift                 = 0;         // ����� ������������� ��� ������������� �����, � ����� 0
extern int    MA_Method                = 1;         // 0 (MODE_SMA), 1 (MODE_EMA), 2 (MODE_SMMA), 3 (MODE_LWMA)
extern int    MA_Price                 = 0;         // 0 (PRICE_CLOSE), 1 (PRICE_OPEN), 2 (PRICE_HIGH), 3 (PRICE_LOW), 4 (PRICE_MEDIAN), 5 (PRICE_TYPICAL), 6 (PRICE_WEIGHTED)
extern int    MA_Bar                   = 0;         // 0-Bars, ����� �����
extern int    Indent_MA                = 10;        // 0-infinity, ����� �����
extern string Setup_TrailFiftyFifty = "----------- N9 - TrailingFiftyFifty -----------";
extern double FF_coeff                 = 0.05;      // "����������� ��������", � % �� 0.01 �� 1 (� ��������� ������ SL ����� ��������� (���� ���������) �������� � ���. ����� � �������, ������ �����, ����� �� ���������)
extern string Setup_TrailKillLoss   = "----------- N10 - TrailingKillLoss ------------";
extern double SpeedCoeff               = 0.5;       // "��������" �������� �����
extern string Setup_TrailPips       = "--------------- N11 - TrailPips ---------------";
extern int    Average_Period           = PERIOD_D1; // �� ����� ������� ��������� ������� �����
//IIIIIIIIIIIIIIIIIII=========����������� ������� �������=======IIIIIIIIIIIIIIIIIIIIII+
//IIIIIIIIIIIIIIIIIII========���������� ���������� ������========IIIIIIIIIIIIIIIIIIIII+
double        bd_IndentFr, bd_IndentSh, bd_IndentMA, bd_IndentPr, bda_ProfitLevel[3],
              bda_StopLevel[3], bd_TimeStep, bda_Distance[2];
bool          bb_TrailLOSS;
string        bs_ComTrail = "",
              bsa_NameTral[] = {"TrailingStairs()","TrailingByFractals()","TrailingByShadows()",
              "TrailingUdavka()","TrailingByTime()","TrailingByATR()","TrailingRatchetB()",
              "TrailingByPriceChannel()","TrailingByMA()","TrailingFiftyFifty()","KillLoss()","TrailPips()"};
int           bia_StopLevel[3], bia_ProfitLevel[3];
//IIIIIIIIIIIIIIIIIII===========�������� ������� ������=========IIIIIIIIIIIIIIIIIIIIII+
//bool fInit_Trail()                 // ������������� ������
//bool fTrail_Position (int fi_Ticket,             // Ticket
                      //int fi_Slippage = 2)       // ���������������
                                     // ����������� ��������� �������
//bool fCheck_TrailParameters()      // ��������� ���������� � ���������� ������� ���������
//bool TrailingByFractals (int fi_Ticket, double& fd_NewSL)
                                     // �������� �� ���������
//bool TrailingByShadows (int fi_Ticket, double& fd_NewSL)
                                     // �������� �� ����� N ������
//bool TrailingStairs (int fi_Ticket, double& fd_NewSL)
                                     // �������� �����������
//bool fMove_ToBreakEven (int fi_Ticket, double& fd_NewSL)
                                     // ��������� ����� � ��������� (���� ������)
//bool TrailingUdavka (int fi_Ticket, double& fd_NewSL)
                                     // �������� "������"
//bool TrailingByTime (int fi_Ticket, double& fd_NewSL)
                                     // �������� �� �������
//bool TrailingByATR (int fi_Ticket, double& fd_NewSL)
                                     // �������� �� ATR
//bool TrailingRatchetB (int fi_Ticket, double& fd_NewSL)
                                     // �������� �� �����������
//bool TrailingByPriceChannel (int fi_Ticket, double& fd_NewSL)
                                     // �������� �� �������� ������
//bool TrailingByMA (int fi_Ticket, double& fd_NewSL)
                                     // �������� �� ����������� ��������
//bool TrailingFiftyFifty (int fi_Ticket, double& fd_NewSL)
                                     // �������� "�����������"
//bool KillLoss (int fi_Ticket, double& fd_NewSL)
                                     // �������� KillLoss
//bool TrailPips (int fi_Ticket, double& fd_NewSL)
                                     // �������� "�����������"
//double fGet_AverageCandle (string fs_Symbol,      // ������
                           //int fi_Period,         // ������
                           //bool fb_IsCandle = false)// ������� ����� ��� ���
                                     // ������������ ������� �������� ������� ����� �� ���������� ������
//void fCheck_LibDecimal()           // ��������� ����������� ���������
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        ������������� ������                                                       |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fInit_Trail()
{
//----
    //---- �������� ������� ���������� � ������������ � ������������ ��������� ��
    fCheck_LibDecimal();
    //---- ���������� �������� ������������ � ���������� ��������
    if (!fCheck_TrailParameters())
    {
        Alert ("��������� ��������� ���������� ���� ��������� !!!");
        bb_OptimContinue = true;
        return (false);
    }
    //---- ��������� ���������� � ������ ��������� GV-����������
    string lsa_Array[1]; 
    int    li_cnt = 0;
    if (BreakEven > 0)
    {
        bb_ClearGV = true;
        lsa_Array[li_cnt] = "_#BU";
        li_cnt++;
    }
    if (N_Trailing == 6 || N_Trailing == 10 || N_Trailing == 11)
    {
        bb_ClearGV = true;
        ArrayResize (lsa_Array, li_cnt + 1);
        if (N_Trailing == 6) {lsa_Array[li_cnt] = "_#LastLossLevel";}
        else if (N_Trailing == 10) {lsa_Array[li_cnt] = "_#Delta_SL";}
        else if (N_Trailing == 11) {lsa_Array[li_cnt] = "_#VirtTP";}
        
    }
    //---- ��������� � ������� ������ �������� ��������� GV-���������
    if (bb_ClearGV) fCreat_ArrayGV (bsa_prefGV, lsa_Array);
//----
    return (true);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//         ����������� ��������� �������                                              |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int fTrail_Position (int fi_Ticket,              // ����� ������
                     int fi_Slippage = 2)        // ���������������
{
    //---- �������� ������ � �������� �������
    if (OrderSymbol() != Symbol()) return (-1);
//----
    bool   lb_Trail = false;
    int    li_Type = OrderType();
//----
    if (TrailLOSS_ON && iBarShift (Symbol(), Trail_TF, OrderOpenTime()) > TrailLossAfterBar)
    {bb_TrailLOSS = true;}
    else {bb_TrailLOSS = false;}
    bd_curSL = OrderStopLoss();
    bd_curTP = OrderTakeProfit();
    bd_Spread = NDP (MarketInfo (Symbol(), MODE_SPREAD));
    //---- ���������� � ����� ����� ����� ��������
    bd_Price = NDD (fGet_TradePrice (li_Type, bb_RealTrade));
    bs_libNAME = "b-TrailSymbol";
    bs_fName = bsa_NameTral[N_Trailing];
    //---- ������������ ��������� ����� ���������
    switch (N_Trailing)
    {
        //---- �����������-������������
        case 0: lb_Trail = TrailingStairs (fi_Ticket, bd_NewSL); break;
        //---- �������� �� ���������
        case 1: lb_Trail = TrailingByFractals (fi_Ticket, bd_NewSL); break;
        //---- �������� �� ����� N ������
        case 2: lb_Trail = TrailingByShadows (fi_Ticket, bd_NewSL); break;
        //---- �������� �����������-"������"
        case 3: lb_Trail = TrailingUdavka (fi_Ticket, bd_NewSL); break;
        //---- �������� �� �������
        case 4: lb_Trail = TrailingByTime (fi_Ticket, bd_NewSL); break;
        //---- �������� �� ATR
        case 5: lb_Trail = TrailingByATR (fi_Ticket, bd_NewSL); break;
        //---- �������� RATCHET �����������
        case 6: lb_Trail = TrailingRatchetB (fi_Ticket, bd_NewSL); break;
        //---- �������� �� ������� ������
        case 7: lb_Trail = TrailingByPriceChannel (fi_Ticket, bd_NewSL); break;
        //---- �������� �� ����������� ��������
        case 8: lb_Trail = TrailingByMA (fi_Ticket, bd_NewSL); break;
        //---- �������� "�����������"
        case 9: lb_Trail = TrailingFiftyFifty (fi_Ticket, bd_NewSL); break;
        //---- �������� KillLoss
        case 10: lb_Trail = KillLoss (fi_Ticket, bd_NewSL); break;
        //---- �������� "�����������"
        case 11: lb_Trail = TrailPips (fi_Ticket, bd_NewSL); break;
    }
    bool lb_BreakEven = fMove_ToBreakEven (fi_Ticket, bd_NewSL);
    //---- ������������ ��������
    if (lb_Trail || lb_BreakEven)
    {
        //---- SL ����� � ������� ���������� ������
        if (fCheck_MinProfit (bd_ProfitMIN, bd_NewSL, !bb_TrailLOSS))
        {
            int li_cmd = 1;
            if (li_Type == OP_SELL) li_cmd = -1;
            if (li_cmd * (bd_NewSL - bd_curSL) > bd_TrailStep || bd_curSL == 0
            //---- ������� SL � ���������
            || lb_BreakEven)
            {
                //---- ��������� ���������� �� SL �� ������� ����
                bd_Trail = MathAbs (bd_Price - bd_NewSL);
                int li_result = fOrderModify (fi_Ticket, OrderOpenPrice(), NDD (bd_NewSL), OrderTakeProfit());
                if (li_result >= 0) fSet_Comment (bdt_curTime, fi_Ticket, 20, "", li_result != 0, 0);
                //---- �������� ���� ������������ ��
                if (lb_BreakEven) {if (li_result == 1) GlobalVariableSet (StringConcatenate (fi_Ticket, "_#BU"), bd_NewSL);}
                if (li_result == 1) return (1);
            }
        }
    }
//----
    return (-1);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//         ��������� ���������� � ���������� ������� ���������                        |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fCheck_TrailParameters()
{
//----
    if (N_Trailing < 0 || N_Trailing > 11)
    {Print ("��� ����� ����������. N_Trailing >= 0 � N_Trailing <= 11 !!!"); return (false);}
    //---- �� ��������� ������� ���� �������� ������ �����
    if (TrailLossAfterBar == 0) {TrailLossAfterBar = PERIOD_D1 / Period();}
    //---- ���������� �������� ������������ �������� ������������� ����������
    if (TrailLOSS_ON && TrailLossAfterBar < 0)
    {Print ("��������� TrailLossAfterBar >= 0 !!!"); return (false);}
    if (BreakEven > 0 && BreakEven <= ProfitMIN_Pips)
    {Print ("��������� BreakEven > ProfitMIN_Pips ��� BreakEven = 0 !!!"); return (false);}
    if (TrailStep < 1 && (N_Trailing == 0 || N_Trailing == 10 || N_Trailing == 11))
    {Print ("��������� TrailStep >= 1 !!!"); return (false);}
    //---- ��������� �������� ��������� ���������� ���������
    switch (N_Trailing)
    {
        case 0:
            if (TrailingStop < TrailStep || TrailingStop < BreakEven + ProfitMIN_Pips)
            {Print ("�������� �������� TrailingStairs() ���������� ��-�� �������������� �������� ���������� �� ����������."); return (false);}
            break;
        case 1:
            if (BarsInFractal <= 3 || Indent_Fr < 0)
            {Print ("�������� �������� TrailingByFractals() ���������� ��-�� �������������� �������� ���������� �� ����������."); return (false);}
            break;
        case 2:
            if (BarsToShadows < 1 || Indent_Sh < 0)
            {Print ("�������� �������� TrailingByShadows() ���������� ��-�� �������������� �������� ���������� �� ����������."); return (false);}
            break;
        case 3:
            if (Level_0 >= Distance_1 || Level_1 >= Level_0 || Level_2 >= Level_1 || Distance_1 >= Distance_2)
            {Print ("�������� �������� TrailingUdavka() ���������� ��-�� �������������� �������� ���������� �� ����������."); return (false);}
            //---- �� �� ����� ������ ���� Level_0
            if (Level_0 <= BreakEven)
            {Print ("TrailingUdavka(): ��������� BreakEven < Level_0 !!!"); return (false);}
            break;
        case 4:
            if (Interval < 1 || TimeStep < 1)
            {Print ("�������� �������� TrailingByTime() ���������� ��-�� �������������� �������� ���������� �� ����������."); return (false);}
            break;
        case 5:
            if (ATR_Period1 < 1 || ATR_Period2 < 1 || ATR_coeff <= 0)
            {Print ("�������� �������� TrailingByATR() ���������� ��-�� �������������� �������� ���������� �� ����������."); return (false);}
            break;
        case 6:
            if (ProfitLevel_2 <= ProfitLevel_1 || ProfitLevel_3 <= ProfitLevel_2 || ProfitLevel_3 <= ProfitLevel_1)
            {Print ("�������� �������� TrailingRatchetB() ���������� ��-�� �������������� �������� ���������� �� ����������."); return (false);}
            //---- �� �� ����� ������ ���� ProfitLevel_1
            if (ProfitLevel_1 <= BreakEven)
            {Print ("TrailingRatchetB(): ��������� BreakEven < ProfitLevel_1 !!!"); return (false);}
            break;
        case 7:
            if (BarsInChannel < 1 || Indent_Pr < 0)
            {Print ("�������� �������� TrailingByPriceChannel() ���������� ��-�� �������������� �������� ���������� �� ����������."); return (false);}
            break;
        case 8:
            if (MA_Period < 2 || MA_Method < 0 || MA_Method > 3 || MA_Price < 0 || MA_Price > 6 || MA_Bar < 0 || Indent_MA < 0)
            {Print ("�������� �������� TrailingByMA() ���������� ��-�� �������������� �������� ���������� �� ����������."); return (false);}
            break;
        case 9:
            if (FF_coeff < 0.01 || FF_coeff > 1.0)
            {Print ("�������� �������� TrailingFiftyFifty() ���������� ��-�� �������������� �������� ���������� �� ����������."); return (false);}
            break;
        case 10:
            if (SpeedCoeff < 0.1)
            {Print ("�������� �������� KillLoss() ���������� ��-�� �������������� �������� ���������� �� ����������."); return (0);}
            break;
        case 11:
            if (Average_Period <= Period())
            {Print ("�������� �������� TrailPips() ���������� ��-�� �������������� �������� ���������� �� ����������."); return (false);}
            break;
    }
    //---- ���������� �������� �� ������������ ������� TF
    if (Trail_TF == 0) {Trail_TF = Period();}
    if (fGet_NumPeriods (Trail_TF) < 0) {return (false);}
//----
     return (true);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//| �������� �� ���������                                                             |
//| ������� ��������� ����� �������, ���������� ����� � ��������, � ������ (�������) |
//| - ���������� �� ����. (���.) �����, �� ������� ����������� �������� (�� 0),       |
//|  trlinloss - ������� �� � ���� �������                                            |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool TrailingByFractals (int fi_Ticket, double& fd_NewSL)
{
    int    i, z,             // counters
           li_ExtremN,       // ����� ���������� ���������� frktl_bars-������� �������� 
           after_x, be4_x,   // ������ ����� � �� ���� ��������������
           ok_be4, ok_after, // ����� ������������ ������� (1 - �����������, 0 - ���������)
           lia_PeakN[2];     // ������ ����������� ��������� ��������� �� �������/������� ��������������   
    double ld_tmp;           // ��������� ����������
//----
    ld_tmp = BarsInFractal;
    if (MathMod (BarsInFractal, 2) == 0) {li_ExtremN = ld_tmp / 2.0;}
    else {li_ExtremN = MathRound (ld_tmp / 2.0);}
    //---- ����� �� � ����� ���������� ��������
    after_x = BarsInFractal - li_ExtremN;
    if (MathMod (BarsInFractal, 2) != 0) {be4_x = BarsInFractal - li_ExtremN;}
    else {be4_x = BarsInFractal - li_ExtremN - 1;}
    //---- ���� OP_BUY, ������� ��������� ������� �� ������� (�.�. ��������� "����")
    if (OrderType() == OP_BUY)
    {
        //---- ������� ��������� ������� �� �������
        for (i = li_ExtremN; i < iBars (Symbol(), Trail_TF); i++)
        {
            ok_be4 = 0;
            ok_after = 0;
            for (z = 1; z <= be4_x; z++)
            {
                if (iLow (Symbol(), Trail_TF, i) >= iLow (Symbol(), Trail_TF, i - z)) 
                {ok_be4 = 1; break;}
            }
            for (z = 1; z <= after_x; z++)
            {
                if (iLow (Symbol(), Trail_TF, i) > iLow (Symbol(), Trail_TF, i + z)) 
                {ok_after = 1; break;}
            }            
            if (ok_be4 == 0 && ok_after == 0)                
            {lia_PeakN[1] = i; break;}
        }
        //---- ��������� ������� �� ���� �������
        double ld_Peak = iLow (Symbol(), Trail_TF, lia_PeakN[1]) - bd_IndentFr;
        //---- ���� ����� �������� ����� ���������� (� �.�. ���� �������� == 0, �� ���������)
        if (ld_Peak > OrderStopLoss() && (bb_TrailLOSS || (!TrailLOSS_ON && ld_Peak > OrderOpenPrice())))
        {fd_NewSL = ld_Peak; return (true);}
    }
    //---- ���� OP_SELL, ������� ��������� ������� �� ������� (�.�. ��������� "�����")
    if (OrderType() == OP_SELL)
    {
        //---- ������� ��������� ������� �� �������
        for (i = li_ExtremN; i < iBars (Symbol(), Trail_TF); i++)
        {
            ok_be4 = 0;
            ok_after = 0;
            for (z = 1; z <= be4_x; z++)
            {
                if (iHigh (Symbol(), Trail_TF, i) <= iHigh (Symbol(), Trail_TF, i - z)) 
                {ok_be4 = 1; break;}
            }
            for (z = 1; z <= after_x; z++)
            {
                if (iHigh (Symbol(), Trail_TF, i) < iHigh (Symbol(), Trail_TF, i + z)) 
                {ok_after = 1; break;}
            }            
            if (ok_be4 == 0 && ok_after == 0)                
            {lia_PeakN[0] = i; break;}
        }        
        ld_Peak = iHigh (Symbol(), Trail_TF, lia_PeakN[0]) + bd_IndentFr + bd_Spread;
        //---- ���� ����� �������� ����� ���������� (� �.�. ���� �������� == 0, �� ���������)
        if ((ld_Peak < OrderStopLoss() || OrderStopLoss() == 0) && (bb_TrailLOSS || (!TrailLOSS_ON && ld_Peak < OrderOpenPrice())))
        {fd_NewSL = ld_Peak; return (true);}
    }
    //---- ������������ ��������� ������
	 fGet_LastErrorInArray (bsa_Comment, bs_fName, bi_indERR);
//----
    return (false);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//| �������� �� ����� N ������                                                        |
//| ������� ���������� ���������� �����, �� ����� ������� ���������� �������������    |
//| (�� 1 � ������) � ������ (�������) - ���������� �� ����. (���.) �����, ��         |
//| ������� ����������� �������� (�� 0), TrailLOSS_ON - ������� �� � �����            |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool TrailingByShadows (int fi_Ticket, double& fd_NewSL)
{
    double ld_Extremum;
//----
    //---- ���� ������� ������� (OP_BUY), ������� ������� BarsToShadows ������
    if (OrderType() == OP_BUY)
    {
        ld_Extremum = iLow (Symbol(), Trail_TF, iLowest (Symbol(), Trail_TF, MODE_LOW, BarsToShadows, 1)) - bd_IndentSh;
        if ((ld_Extremum > OrderStopLoss())
        && (bb_TrailLOSS || (!TrailLOSS_ON && ld_Extremum > OrderOpenPrice())))
        {fd_NewSL = ld_Extremum; return (true);}
    }
    //---- ���� OP_SELL, ������� �������� BarsToShadows ������
    if (OrderType() == OP_SELL)
    {
        ld_Extremum = iHigh (Symbol(), Trail_TF, iHighest (Symbol(), Trail_TF, MODE_HIGH, BarsToShadows, 1)) + bd_IndentSh + bd_Spread;
        //---- ���� ����� �������� ����� ���������� (� �.�. ���� �������� == 0, �� ���������)
        if ((ld_Extremum < OrderStopLoss() || OrderStopLoss() == 0)
        && (bb_TrailLOSS || (!TrailLOSS_ON && ld_Extremum < OrderOpenPrice())))
        {fd_NewSL = ld_Extremum; return (true);}
    }
    //---- ������������ ��������� ������
	 fGet_LastErrorInArray (bsa_Comment, bs_fName, bi_indERR);
//----
    return (false);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//| �������� �����������-������������                                                 |
//| ������� ��������� ����� �������, ���������� �� ����� ��������, �� �������        |
//| �������� ����������� (�������) � "���", � ������� �� ����������� (�������)        |
//| ������: ��� +30 ���� �� +10, ��� +40 - ���� �� +20 � �.�.                         |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool TrailingStairs (int fi_Ticket, double& fd_NewSL)
{
    int li_cmd = 1;
//----
    fd_NewSL = 0.0;
    if (OrderType() == 1) li_cmd = -1;
    if (TrailingStop > 0)
    {
        if (li_cmd * (bd_Price - OrderOpenPrice()) > bd_TrailingStop || bb_TrailLOSS)
        {
            if ((li_cmd * (bd_Price - bd_curSL) > bd_TrailingStop + bd_TrailStep) || bd_curSL == 0.0)
            {fd_NewSL = bd_Price - li_cmd * bd_TrailingStop; return (true);}
        }
    }
    //---- ������������ ��������� ������
	 fGet_LastErrorInArray (bsa_Comment, bs_fName, bi_indERR);
//----
    return (false);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//         ��������� ����� � ��������� (���� ������)                                  |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fMove_ToBreakEven (int fi_Ticket,       // Ticket
                        double& fd_NewSL)    // ������������ ����� SL
{
    string ls_Name = StringConcatenate (fi_Ticket, "_#BU");
//----
    if (GlobalVariableCheck (ls_Name)) return (false);
    //---- ����������� ������ ��
    if (BreakEven > 0)
    {
        int li_cmd = 1, li_Type = OrderType();
        if (li_Type == 1) li_cmd = -1;
        if (li_cmd * (bd_Price - OrderOpenPrice()) > bd_BreakEven)
        {
            double ld_Profit = OrderProfit() + OrderSwap() + OrderCommission();
            //---- ����������� ������ � �� ����������� ���� ���
            if (ld_Profit > 0.0)
            {
                fd_NewSL = OrderOpenPrice() + li_cmd * bd_ProfitMIN;
                //GlobalVariableSet (ls_Name, fd_NewSL);
                fSet_Comment (fi_Ticket, fi_Ticket, 23, "fMove_ToBreakEven()", True, fd_NewSL);
                return (true);
            }
        }    
    }
    //---- ������������ ��������� ������
	 fGet_LastErrorInArray (bsa_Comment, "fMove_ToBreakEven()", bi_indERR);
//----
    return (false);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//| �������� �����������-"������"                                                     |
//| ������: �������� �������� 30 �., ��� +50 - 20 �., +80 >= - �� ���������� � 10 �.  |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool TrailingUdavka (int fi_Ticket, double& fd_NewSL)
{
    double ld_Price, ld_MovePrice, ld_TrailStop = 0.0;
//----
    //---- ���� ������� ������� (OP_BUY)
    if (OrderType() == OP_BUY)
    {
        ld_MovePrice = bd_Price - OrderOpenPrice();
        if (ld_MovePrice <= 0) {return (false);}
        //---- ���������� ���������� �� ����� �� ���� (��������� ������ ���������)
        if (ld_MovePrice <= bda_Distance[0]) {ld_TrailStop = Level_0;}
        if (ld_MovePrice > bda_Distance[0] && ld_MovePrice <= bda_Distance[1]) {ld_TrailStop = Level_1;}
        if (ld_MovePrice > bda_Distance[1]) {ld_TrailStop = Level_2;}
        ld_TrailStop = NDP (ld_TrailStop);
        //---- ���� �������� = 0 ��� ������ ����� ��������, �� ���� ���.���� (Bid) ������/����� ��������� ����_�������� + �����.���������
        if (OrderStopLoss() < OrderOpenPrice())
        {
            if (ld_MovePrice > ld_TrailStop + bd_TrailStep)
            {fd_NewSL = bd_Price - ld_TrailStop; return (true);}
        }
        //---- �����: ���� ������� ���� (Bid) ������/����� ��������� �������_�������� + ���������� ���������, 
        else
        {
            if (bd_Price - OrderStopLoss() > ld_TrailStop + bd_TrailStep)
            {fd_NewSL = bd_Price - ld_TrailStop; return (true);}
        }
    }
    //---- ���� �������� ������� (OP_SELL)
    if (OrderType() == OP_SELL)
    { 
        ld_MovePrice = OrderOpenPrice() - (bd_Price + bd_Spread);
        if (ld_MovePrice <= 0) {return (false);}
        //---- ���������� ���������� �� ����� �� ���� (��������� ������ ���������)
        if (ld_MovePrice <= bda_Distance[0]) {ld_TrailStop = Level_0;}
        if (ld_MovePrice > bda_Distance[0] && ld_MovePrice <= bda_Distance[1]) {ld_TrailStop = Level_1;}
        if (ld_MovePrice > bda_Distance[1]) {ld_TrailStop = Level_2;}
        ld_TrailStop = NDP (ld_TrailStop);
        // ���� �������� = 0 ��� ������ ����� ��������, �� ���� ���.���� (Ask) ������/����� ��������� ����_��������+�����.���������
        if (OrderStopLoss() == 0 || OrderStopLoss() > OrderOpenPrice())
        {
            if (ld_MovePrice > ld_TrailStop + bd_TrailStep)
            {fd_NewSL = bd_Price + ld_TrailStop; return (true);}
        }
        //---- �����: ���� ������� ���� (Bid) ������/����� ��������� �������_�������� + ���������� ���������, 
        else
        {
            if (OrderStopLoss() - (bd_Price + bd_Spread) > ld_TrailStop + bd_TrailStep)
            {fd_NewSL = bd_Price + ld_TrailStop; return (true);}
        }
    }
    //---- ������������ ��������� ������
	 fGet_LastErrorInArray (bsa_Comment, bs_fName, bi_indERR);
//----
    return (false);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//| �������� �� �������                                                               |
//| ������� ��������� ����� �������, �������� (�����), � �������, �������������      |
//| �������� � ��� ��������� (�� ������� ������� ������������ ��������, TrailLOSS_ON  |
//| - ������ �� � ������ (�.�. � ����������� ���������� ����������� ���� �� �����    |
//| ��������, � ����� � � �������, ���� ������ � �������)                             |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool TrailingByTime (int fi_Ticket, double& fd_NewSL)
{
    int    li_MinPast;    // ���-�� ������ ����� �� �������� ������� �� �������� ������� 
    double times2change;  // ���-�� ���������� Interval � ������� �������� ������� (�.�. ������� ��� ������ ��� ���� ��������� ��������) 
//----
    //---- ����������, ������� ������� ������ � ������� �������� �������
    li_MinPast = (TimeCurrent() - OrderOpenTime()) / 60;
    //---- ������� ��� ����� ���� ����������� ��������
    times2change = MathFloor (li_MinPast / Interval);
    //---- ���� ������� ������� (OP_BUY)
    if (OrderType() == OP_BUY)
    {
        //---- ���� ������ � ������, �� ��������� �� ��������� (���� �� �� 0, ���� 0 - �� ��������)
        if (bb_TrailLOSS)
        {
            if (OrderStopLoss() == 0) {fd_NewSL = OrderOpenPrice() + times2change * bd_TimeStep;}
            else {fd_NewSL = OrderStopLoss() + times2change * bd_TimeStep;}
        }
        //---- ����� - �� ����� �������� �������
        else {fd_NewSL = OrderOpenPrice() + times2change * bd_TimeStep;}
    }
    //---- ���� �������� ������� (OP_SELL)
    if (OrderType() == OP_SELL)
    {
        //---- ���� ������ � ������, �� ��������� �� ��������� (���� �� �� 0, ���� 0 - �� ��������)
        if (bb_TrailLOSS)
        {
            if (OrderStopLoss() == 0) {fd_NewSL = OrderOpenPrice() - times2change * bd_TimeStep - bd_Spread;}
            else {fd_NewSL = OrderStopLoss() - times2change * bd_TimeStep - bd_Spread;}
        }
        else {fd_NewSL = OrderOpenPrice() - times2change * bd_TimeStep - bd_Spread;}
    }
    if (times2change > 0) {return (true);}
    //---- ������������ ��������� ������
	 fGet_LastErrorInArray (bsa_Comment, bs_fName, bi_indERR);
//----
    return (false);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//| �������� �� ATR (Average True Range, ������� �������� ��������)                   |
//| ������� ��������� ����� �������, ������ ��R � �����������, �� ������� ���������� |
//| ATR. �.�. �������� "�������" �� ���������� ATR � N �� �������� �����;             |
//| ������� - �� ����� ���� (�.�. �� ���� �������� ���������� ����)                   |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool TrailingByATR (int fi_Ticket, double& fd_NewSL)
{
    double ld_ATR,
           ld_Coeff; // ��������� ��������� �������� �� ATR �� �����������
//----
    //---- ������� �������� ATR
    ld_ATR = iATR (Symbol(), Trail_TF, ATR_Period1, ATR_shift1);
    ld_ATR = MathMax (ld_ATR, iATR (Symbol(), Trail_TF, ATR_Period2, ATR_shift2));
    //---- ����� ��������� �� �����������
    ld_Coeff = ld_ATR * ATR_coeff;
    //---- ���� ������� ������� (OP_BUY)
    if (OrderType() == OP_BUY)
    {
        //---- ����������� �� �������� ����� (����� ��������)
        fd_NewSL = bd_Price - ld_Coeff;
        //---- ���� TrailLOSS_ON == true (�.�. ������� ������� � ���� ������), ��
        if ((bb_TrailLOSS && fd_NewSL > OrderStopLoss())
        //---- ����� ������ �� ����� ��������
        || (!TrailLOSS_ON && fd_NewSL > OrderOpenPrice()))
        {return (true);}
    }
    //---- ���� �������� ������� (OP_SELL)
    if (OrderType() == OP_SELL)
    {
        //---- ����������� �� �������� ����� (����� ��������)
        fd_NewSL = bd_Price + (ld_Coeff + bd_Spread);
        //---- ���� TrailLOSS_ON == true (�.�. ������� ������� � ���� ������), ��
        if ((bb_TrailLOSS && (fd_NewSL < OrderStopLoss() || OrderStopLoss() == 0))
        //---- ����� ������ �� ����� ��������
        || (!TrailLOSS_ON && fd_NewSL < OrderOpenPrice()))
        {return (true);}
    }
    //---- ������������ ��������� ������
	 fGet_LastErrorInArray (bsa_Comment, bs_fName, bi_indERR);
//----
    return (false);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//| �������� RATCHET �����������                                                      |
//| ��� ���������� �������� ������ 1 �������� - � bd_ProfitMIN, ��� ���������� �����- |
//| ��� ������ 2 ������� - �������� - �� ������� 1, ����� ������ ��������� ������ 3   |
//| �������, �������� - �� ������� 2 (������ ����� �������� ������� ��������)         |
//| ��� ������ � �������� ������� - ���� 3 ������, �� ����� ������ � ���� ���������   |
//| ����,  � ������: ���� �� ���������� ���� ������, � ����� ��������� ���� ����      |
//| (������ ��� �������), �� �������� ������ �� ���������, ����� �������� �������     |
//| (��������, ������ -5, -10 � -25, �������� -40; ���� ���������� ���� -10, � �����  |
//| ��������� ���� -10, �� �������� - �� -25, ���� ���������� ���� -5, �� ��������    |
//| ��������� �� -10, ��� -2 (�����) ���� �� -5                                       |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool TrailingRatchetB (int fi_Ticket, double& fd_NewSL)
{
    bool lb_result = false;
//----
    //---- ���� ������� ������� (OP_BUY)
    if (OrderType() == OP_BUY)
    {
        double ld_ProfitLevel;
        //---- �������� �� ������� ��������
        for (int li_IND = 2; li_IND >= 0; li_IND--)
        {
            //---- ���� ������� "�������_����-����_��������" > "ProfitLevel_N+�����", SL ��������� � "StopLevel_N+�����"
            if (bd_Price - OrderOpenPrice() >= bda_ProfitLevel[li_IND])
            {
                if (OrderStopLoss() == 0 || OrderStopLoss() < OrderOpenPrice() + bda_StopLevel[li_IND])
                {fd_NewSL = OrderOpenPrice() + bda_StopLevel[li_IND]; return (true);}
            }
        }
        //---- �������� �� ������� ������
        if (bb_TrailLOSS)      
        {
            //---- ��������� �� ����� ������������ ���������� ����������
            double ld_LastLossLevel;
            string ls_Name = StringConcatenate (fi_Ticket, "_#LastLossLevel");
            //---- ���������� ���������� ��������� �������� �������� ������ ������ ������ (StopLevel_n), ���� �������� ��������� ����
            // (���� �� ����� ����� ����������� ����, ������������� SL �� ��������� ����� �������� ������ ������ (���� ��� �� ��������� SL �������)
            if (!GlobalVariableCheck (ls_Name)) {GlobalVariableSet (ls_Name, 0);}
            else {ld_LastLossLevel = GlobalVariableGet (ls_Name);}
            //---- ��������� ������� ������� ���� ����� �������� � �� ������� ������ �������
            if (bd_Price - OrderOpenPrice() < bda_ProfitLevel[0])
            {
                //---- ���� (�������_���� �����/����� ��������) � (dpstlslvl>=StopLevel_1), �������� - �� StopLevel_1
                if (bd_Price >= OrderOpenPrice())
                {
                    if (OrderStopLoss() == 0 || OrderStopLoss() < OrderOpenPrice() - bda_StopLevel[0])
                    {fd_NewSL = OrderOpenPrice() - bda_StopLevel[0]; lb_result = true;}
                }
                //---- ���� (�������_���� ����� ������_������_1) � (dpstlslvl>=StopLevel_1), �������� - �� StopLevel_2
                if (bd_Price >= OrderOpenPrice() - bda_StopLevel[0] && ld_LastLossLevel >= StopLevel_1)
                {
                    if (OrderStopLoss() == 0 || OrderStopLoss() < OrderOpenPrice() - bda_StopLevel[1])
                    {fd_NewSL = OrderOpenPrice() - bda_StopLevel[1]; lb_result = true;}
                }
                //---- ���� (�������_���� ����� ������_������_2) � (dpstlslvl>=StopLevel_2), �������� - �� StopLevel_3
                if (bd_Price >= OrderOpenPrice() - bda_StopLevel[1] && ld_LastLossLevel >= StopLevel_2)
                {
                    if (OrderStopLoss() == 0 || OrderStopLoss() < OrderOpenPrice() - bda_StopLevel[2])
                    {fd_NewSL = OrderOpenPrice() - bda_StopLevel[2]; lb_result = true;}
                }
                //---- ��������/������� �������� �������� �������� "������" �������� "���������"
                //---- ���� "�������_����-���� �������� + �����" ������ 0, 
                if (bd_Price - OrderOpenPrice() + bd_Spread < 0)
                //---- ��������, �� ������ �� �� ���� ��� ����� ������ ������
                {
                    for (li_IND = 2; li_IND >= 0; li_IND--)
                    {
                        if (bd_Price <= OrderOpenPrice() - bda_StopLevel[li_IND])
                        {
                            if (ld_LastLossLevel < bia_StopLevel[li_IND])
                            {GlobalVariableSet (ls_Name, bia_StopLevel[li_IND]); return (lb_result);}
                        }
                    }
                }
            }
        }
    }
    //---- ���� �������� ������� (OP_SELL)
    if (OrderType() == OP_SELL)
    {
        //---- �������� �� ������� ��������
        for (li_IND = 2; li_IND >= 0; li_IND--)
        {
            //---- ���� ������� "�������_����-����_��������" > "ProfitLevel_N+�����", SL ��������� � "StopLevel_N+�����"
            if (OrderOpenPrice() - bd_Price >= bda_ProfitLevel[li_IND])
            {
                if (OrderStopLoss() == 0 || OrderStopLoss() > OrderOpenPrice() - bda_StopLevel[li_IND])
                {fd_NewSL = OrderOpenPrice() - bda_StopLevel[li_IND]; return (true);}
            }
        }
        //---- �������� �� ������� ������
        if (bb_TrailLOSS)      
        {
            //---- ��������� �� ����� ������������ ���������� ����������
            ls_Name = StringConcatenate (fi_Ticket, "_#LastLossLevel");
            //---- ���������� ���������� ��������� �������� �������� ������ ������ ������ (StopLevel_n), ���� �������� ��������� ����
            // (���� �� ����� ����� ����������� ����, ������������� SL �� ��������� ����� �������� ������ ������ (���� ��� �� ��������� SL �������)
            if (!GlobalVariableCheck (ls_Name)) {GlobalVariableSet (ls_Name, 0);}
            else {ld_LastLossLevel = GlobalVariableGet (ls_Name);}
            //---- ��������� ������� ������� ���� ����� �������� � �� ������� ������ �������
            if (OrderOpenPrice() - bd_Price < bda_ProfitLevel[0])         
            {
                //---- ���� (�������_���� �����/����� ��������) � (dpstlslvl>=StopLevel_1), SL - �� StopLevel_1
                if (bd_Price <= OrderOpenPrice())
                {
                    if (OrderStopLoss() == 0 || OrderStopLoss() > OrderOpenPrice() + (bda_StopLevel[0] + bd_Spread))
                    {fd_NewSL = OrderOpenPrice() + (bda_StopLevel[0] + bd_Spread); lb_result = true;}
                }
                //---- ���� (�������_���� ����� ������_������_1) � (dpstlslvl>=StopLevel_1), SL - �� StopLevel_2
                if (bd_Price <= OrderOpenPrice() + (bda_StopLevel[0] + bd_Spread) && ld_LastLossLevel >= StopLevel_1)
                {
                    if (OrderStopLoss() == 0 || OrderStopLoss() > OrderOpenPrice() + (bda_StopLevel[1] + bd_Spread))
                    {fd_NewSL = OrderOpenPrice() + (bda_StopLevel[1] + bd_Spread); lb_result = true;}
                }
                //---- ���� (�������_���� ����� ������_������_2) � (dpstlslvl>=StopLevel_2), SL - �� StopLevel_3
                if (bd_Price <= OrderOpenPrice() + (bda_StopLevel[1] + bd_Spread) && ld_LastLossLevel >= StopLevel_2)
                {
                    if (OrderStopLoss() == 0 || OrderStopLoss() > OrderOpenPrice() + (bda_StopLevel[2] + bd_Spread))
                    {fd_NewSL = OrderOpenPrice() + (bda_StopLevel[2] + bd_Spread); lb_result = true;}
                }
                //---- ��������/������� �������� �������� �������� "������" �������� "���������"
                //---- ���� "�������_����-���� ��������+�����" ������ 0, 
                if (OrderOpenPrice() - bd_Price + bd_Spread < 0)
                //---- ��������, �� ������ �� �� ���� ��� ����� ������ ������
                {
                    for (li_IND = 2; li_IND >= 0; li_IND--)
                    {
                        if (bd_Price >= OrderOpenPrice() + (bda_StopLevel[li_IND] + bd_Spread))
                        {
                            if (ld_LastLossLevel < bia_StopLevel[li_IND])
                            {GlobalVariableSet (ls_Name, bia_StopLevel[li_IND]); return (lb_result);}
                        }
                    }
                }
            }
        }
    }
    //---- ������������ ��������� ������
	 fGet_LastErrorInArray (bsa_Comment, bs_fName, bi_indERR);
//----
    return (lb_result);    
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//| �������� �� ������� ������                                                        |
//| �������� �� ������ Nickolay Zhilin (aka rebus) �������� �� ����������� �����.     |
//| ������� ��������� ����� �������, ������ (���-�� �����) ��� �������� ������� �    | 
//| ������ ������ ������, ������ (�������), �� ������� ����������� �������� ��        |
//| ������� ������                                                                    |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool TrailingByPriceChannel (int fi_Ticket, double& fd_NewSL)
{     
    double ld_ChnlMax, // ������� ������� ������
           ld_ChnlMin; // ������ ������� ������
//----
    //---- ���������� ����.��� � ���.��� �� BarsInChannel ����� ������� � [1] (= ������� � ������ ������� �������� ������)
    ld_ChnlMax = iHigh (Symbol(), Trail_TF, iHighest (Symbol(), Trail_TF, MODE_HIGH, BarsInChannel, 1)) + bd_IndentPr + bd_Spread;
    ld_ChnlMin = iLow (Symbol(), Trail_TF, iLowest (Symbol(), Trail_TF, MODE_LOW, BarsInChannel, 1)) - bd_IndentPr;   
   
    //---- ���� ������� �������, � � �������� ���� (���� ������ ������� ������ ���� �� ���������, == 0), ������������ ���
    if (OrderType() == OP_BUY)
    {
        if (OrderStopLoss() < ld_ChnlMin)
        {fd_NewSL = ld_ChnlMin; return (true);}
    }
    //---- ���� ������� - ��������, � � �������� ���� (���� ������� ������� ������ ��� �� ��������, == 0), ������������ ���
    if (OrderType() == OP_SELL)
    {
        if (OrderStopLoss() == 0 || OrderStopLoss() > ld_ChnlMax)
        {fd_NewSL = ld_ChnlMax; return (true);}
    }
    //---- ������������ ��������� ������
	 fGet_LastErrorInArray (bsa_Comment, bs_fName, bi_indERR);
//----
    return (false);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//| �������� �� ����������� ��������                                                  |
//| ������� ��������� ����� ������� � ��������� ������� (���������, ������, ���,     | 
//| ����� ������������ �������, ����� �����������, ������������ OHCL ��� ����������,  |
//| � ����, �� ������� ������� �������� �������.                                      |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool TrailingByMA (int fi_Ticket, double& fd_NewSL)
{     
    double ld_MA; // �������� ����������� �������� � ����������� �����������
    //---- ��������� �������� �� � ����������� ������� �����������
    ld_MA = iMA (Symbol(), Trail_TF, MA_Period, MA_Shift, MA_Method, MA_Price, MA_Bar);
    //---- ���� ������� �������, � � �������� ���� �������� �������� � �������� � Indent_MA �������, ������������ ���
    if (OrderType() == OP_BUY)
    {
        ld_MA -= bd_IndentMA;
        if (OrderStopLoss() < ld_MA) {fd_NewSL = ld_MA; return (true);}
    }
    //---- ���� ������� - ��������, � � �������� ���� (���� ������� ������� ������ ��� �� ��������, ==0), ������������ ���
    if (OrderType() == OP_SELL)
    {
        ld_MA += (bd_IndentMA + bd_Spread);
        if (OrderStopLoss() == 0 || OrderStopLoss() > ld_MA) {fd_NewSL = ld_MA; return (true);}
    }
    //---- ������������ ��������� ������
	 fGet_LastErrorInArray (bsa_Comment, bs_fName, bi_indERR);
//----
    return (false);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//| �������� "�����������"                                                            |
//| �� �������� ���������� ������� (����) ����������� �������� �� �������� (�� �����  |
//| � ����� ���� �����������) ���������, ���������� ������ (�.�., ��������, ��        |
//| �������� ����� ������ +55 �. - �������� ��������� � 55/2=27 �. ���� �� ��������   |
//| ����. ����� ������ ������, ��������, +80 �., �� �������� ��������� �� ��������    |
//| (����.) ���������� ����� ���. ���������� � ������ �� �������� ���� -              |
//| 27 + (80-27)/2 = 27 + 53/2 = 27 + 26 = 53 �.                                      |
//| TrailLOSS_ON - ����� �� ������� �� �������� ������� - ���� ��, �� �� ��������     |
//| ���������� ���� ���������� ����� ���������� (� �.�. "��" ���������) � �������     |
//| ������ ����� ����������� � dCoeff ��� ����� ����. ������� �������, �����������    |
//| ������ ���� �������� �������� (�� ����� 0)                                       |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool TrailingFiftyFifty (int fi_Ticket, double& fd_NewSL)
{ 
    static datetime ldt_NewBar = 0;
//----
    //---- ���������� �������� ������ �� �������� ����
    if (ldt_NewBar == iTime (Symbol(), Trail_TF, 0)) return (0);
    ldt_NewBar = iTime (Symbol(), Trail_TF, 0);             
    //---- �������� ������� - � ������� ���� ����� ������������ (����� ��� bTrlinloss ����� �� ����� �������� 
    // ������� �������� ����� ��������� �� �������� ���������� ����� ���������� � ������ ��������)
    // �.�. �������� ������ ��� �������, ��� � ������� OrderOpenTime() ������ �� ����� Trail_TF �����      
    if (TimeCurrent() - Trail_TF * 60 > OrderOpenTime())
    {         
        double ld_NextMove;     
      
        //---- ��� ������� ������� ��������� �������� �� FF_coeff ��������� �� ����� �������� �� Bid �� ������ �������� ����
        // (���� ����� �������� ����� ���������� � �������� �������� � ������� �������)
        if (OrderType() == OP_BUY)
        {
            if (bb_TrailLOSS && OrderStopLoss() != 0)
            {
                ld_NextMove = FF_coeff * (bd_Price - OrderStopLoss());
                fd_NewSL = OrderStopLoss() + ld_NextMove;            
            }
            else
            {
                //---- ���� �������� ���� ����� ��������, �� ������ "�� ����� ��������"
                if (OrderOpenPrice() > OrderStopLoss())
                {
                    ld_NextMove = FF_coeff * (bd_Price - OrderOpenPrice());                 
                    //Print ("Next Move = ", FF_coeff, " * (", DSD (dBid), " - ", DSD (OrderOpenPrice()), ") = ", DSD (ld_NextMove));
                    fd_NewSL = OrderOpenPrice() + ld_NextMove;
                    //Print ("New SL[", DSD (OrderStopLoss()), "] = (", DSD (OrderOpenPrice()), " + ", DSD (ld_NextMove), ") ", DSD (fd_NewSL), "[", (fd_NewSL - OrderStopLoss()) / Point, "]");
                }
                //---- ���� �������� ���� ����� ��������, ������ �� ���������
                else
                {
                    ld_NextMove = FF_coeff * (bd_Price - OrderStopLoss());
                    fd_NewSL = OrderStopLoss() + ld_NextMove;
                }                                       
            }
            //---- SL ���������� ������ � ������, ���� ����� �������� ����� �������� � ���� �������� - � ������� �������
            // (��� ������ ��������, �� ����� ��������, ����� �������� ����� ���� ����� ����������, � � �� �� ����� ���� 
            // ����� �������� (���� dBid ���� ����������) 
            if (ld_NextMove > 0) {return (true);}
        }       
        if (OrderType() == OP_SELL)
        {
            if (bb_TrailLOSS && OrderStopLoss() != 0)
            {
                ld_NextMove = FF_coeff * (OrderStopLoss() - (bd_Price + bd_Spread));
                fd_NewSL = OrderStopLoss() - ld_NextMove;            
            }
            else
            {         
                //---- ���� �������� ���� ����� ��������, �� ������ "�� ����� ��������"
                if (OrderOpenPrice() < OrderStopLoss())
                {
                    ld_NextMove = FF_coeff * (OrderOpenPrice() - (bd_Price + bd_Spread));                 
                    fd_NewSL = OrderOpenPrice() - ld_NextMove;
                }
                //---- ���� �������� ���� ����� ��������, ������ �� ���������
                else
                {
                    ld_NextMove = FF_coeff * (OrderStopLoss() - (bd_Price + bd_Spread));
                    fd_NewSL = OrderStopLoss() - ld_NextMove;
                }                  
            }
            //---- SL ���������� ������ � ������, ���� ����� �������� ����� �������� � ���� �������� - � ������� �������
            if (ld_NextMove > 0) {return (true);}
        }               
    }
    //---- ������������ ��������� ������
	 fGet_LastErrorInArray (bsa_Comment, bs_fName, bi_indERR);
//----
    return (false);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//| �������� KillLoss � � � � � � � � � � � � � � � � � � � �� � � �                  |
//| ����������� �� ������� ������. ����: �������� �������� ��������� ����� �� ���-    |
//| ������ �������� ����� � ����������� (SpeedCoeff). ��� ���� ����������� �����      |
//| "���������" � �������� ���������� ������ - ���, ����� ��� ������� ����������    |
//| �������� ������. ��� ������������ = 1 �������� ��������� ����� ��������� �����    |
//| ������� ��������� � ������ �� ������ ������� �������, ��� �����.>1 ����� �������  |
//| ����� � ��������� ����� ������� � ������� ��������� ��������� �����, ��������.<1 |
//| - ��������, ����� � ��������� ���������.                             �            |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool KillLoss (int fi_Ticket, double& fd_NewSL)
{
    double ld_StopPriceDiff, // ���������� (�������) ����� ������ � ����������
           ld_ToMove,        // ���-�� �������, �� ������� ������� ����������� ��������
           ld_curMove, ld_newSL, ld_LastPriceDiff;
    string ls_Name;
    int    li_cmd;
//----
    //---- ������� ���������� ����� ������ � ����������
    if (OrderType() == OP_BUY)
    {ld_StopPriceDiff = bd_Price - OrderStopLoss();}
    if (OrderType() == OP_SELL)
    {ld_StopPriceDiff = (OrderStopLoss() + bd_Spread) - bd_Price;}
    ls_Name = StringConcatenate (fi_Ticket, "_#Delta_SL");
    //---- ���������, ���� ����� �����, ���������� ������� ���������� ����� ������ � ����������
    if (!GlobalVariableCheck (ls_Name))
    {GlobalVariableSet (ls_Name, ld_StopPriceDiff); return (false);}
    else {ld_LastPriceDiff = GlobalVariableGet (ls_Name);}
    //---- ����, � ��� ���� ����������� ��������� ��������� �����
    // �� ������ �����, ������� �������� ���� � ������� �����, 
    // �� ������ ����������� �������� ��� �� ������� �� fd_SpeedCoeff ��� �������
    // (��������, ���� ���� ���������� �� 3 ������ �� ���, fd_SpeedCoeff = 1.5, ��
    // �������� ����������� �� 3 � 1.5 = 4.5, ��������� - 5 �. ���� ��������� �� 
    // ������ (������� ������), ������ �� ������. � � � � �
        
    //---- ���-�� �������, �� ������� ����������� ���� � ��������� � ������� ���������� �������� (����, �� ����)
    ld_ToMove = NDPD (ld_LastPriceDiff - ld_StopPriceDiff);
        
    //---- ���������� ����� ��������, �� ������ ���� ��� �����������
    if (ld_StopPriceDiff + bd_TrailStep < ld_LastPriceDiff)
    {GlobalVariableSet (ls_Name, ld_StopPriceDiff);}
        
    //---- ������ �������� �� ������, ���� ���������� ����������� (�.�. ���� ����������� � ���������, ������ ������)
    if (ld_ToMove >= TrailStep)
    {
        ld_ToMove = NDP (MathRound (ld_ToMove * SpeedCoeff));
        if (OrderType() == OP_BUY) {li_cmd = 1;} else {li_cmd = -1;}
        //---- ��������, ��������������, ����� ����� ����������� �� ����� �� ����������, �� � ������ �����. ���������
        ld_curMove = li_cmd * (bd_Price - (OrderStopLoss() + li_cmd * ld_ToMove));
        fd_NewSL = OrderStopLoss() + li_cmd * ld_ToMove;
        return (true);
    }
    //---- ������������ ��������� ������
	 fGet_LastErrorInArray (bsa_Comment, bs_fName, bi_indERR);
//----
    return (false);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//| �������� ����������� (��� ��������) � � � � � � � � � � � � � � � � � � � �� � �  |
//| ����������� ��� ��������, ����� �� �������� ���� � ������� ����� ��-���������.    |
//| ���� ���������� ������� Period_Average � ����������� ������� ������ ����� - ���  |
//| ����� �������� ������ (��). ��� ���������� ����� �����������, ��������� � ���-    |
//| ������. � ����� ��������� "������". ��� ���������� ����� ��������� �����,         |
//| "��������" ��� � 1.5 ����.                                                        |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool TrailPips (int fi_Ticket, double& fd_NewSL)
{
    double ld_VirtTP, ld_TrailStop = 0.0;
    string ls_Name = StringConcatenate (fi_Ticket, "_#VirtTP");
    int    li_cmd;
//----
    //---- �������� ����������� ����
    if (GlobalVariableCheck (ls_Name)) {ld_VirtTP = GlobalVariableGet (ls_Name);}
    else
    {
        if (OrderType() == OP_BUY) {li_cmd = 1;} else {li_cmd = -1;}
        ld_VirtTP = OrderOpenPrice() + li_cmd * fGet_AverageCandle (Symbol(), Average_Period);
        GlobalVariableSet (ls_Name, ld_VirtTP);
    }
    if (OrderType() == OP_BUY)
    {
        //---- ����������� ����������� ����
        if (bd_Price >= ld_VirtTP)
        {
            ld_VirtTP += ((ld_VirtTP - OrderOpenPrice()) / 2.0);
            GlobalVariableSet (ls_Name, ld_VirtTP);
        }
        //---- ������������ TrailStop - ���������� �� ������������ ����� �� ����
        for (int li_int = 4; li_int >= 2; li_int--)
        {
            ld_TrailStop = NDD ((ld_VirtTP - OrderOpenPrice()) / li_int);
            if (ld_TrailStop >= ld_VirtTP - bd_Price)
            //---- ��� ����������� ����� �������� ���� �� ����, ������ ����������� ��
            {if (li_int == 2) {ld_TrailStop -= bd_ProfitMIN;} break;}
            else {ld_TrailStop = 0.0;}
        }
        if (ld_TrailStop > 0)  
        {
            if (bd_Price - OrderOpenPrice() > ld_TrailStop)
            {
                fd_NewSL = bd_Price - ld_TrailStop;
                if (OrderStopLoss() + bd_TrailStep < fd_NewSL || OrderStopLoss() == 0)
                {return (true);}
            }
        }
    }
    if (OrderType() == OP_SELL)
    {
        //---- ����������� ����������� ����
        if (bd_Price <= ld_VirtTP)
        {
            ld_VirtTP -= ((OrderOpenPrice() - ld_VirtTP) / 2.0);
            GlobalVariableSet (ls_Name, ld_VirtTP);
        }
        //---- ������������ TrailStop - ���������� �� ������������ ����� �� ����
        for (li_int = 4; li_int >= 2; li_int--)
        {
            ld_TrailStop = NDD ((OrderOpenPrice() - ld_VirtTP) / li_int);
            if (ld_TrailStop >= bd_Price - ld_VirtTP)
            //---- ��� ����������� ����� �������� ���� �� ����, ������ ����������� ��
            {if (li_int == 2) {ld_TrailStop -= bd_ProfitMIN;} break; }
            else {ld_TrailStop = 0.0;}
        }
        if (ld_TrailStop > 0)  
        {
            if (OrderOpenPrice() - bd_Price > ld_TrailStop)
            {
                fd_NewSL = bd_Price + ld_TrailStop;
                if (OrderStopLoss() > fd_NewSL + bd_TrailStep || OrderStopLoss() == 0)
                {return (true);}
            }
        }
    }
    //---- ������������ ��������� ������
	 fGet_LastErrorInArray (bsa_Comment, bs_fName, bi_indERR);
//----
    return (false);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        ������������ ������� �������� ������� ����� �� ���������� ������           |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
double fGet_AverageCandle (string fs_Symbol,         // ������
                           int fi_Period,            // ������
                           bool fb_IsCandle = false) // ������� �����?
{
    double ld_OPEN, ld_CLOSE, ld_HIGH, ld_LOW, ld_AVERAGE = 0;
    datetime ldt_Begin = iTime (fs_Symbol, 0, 0) - fi_Period * 60;
    int      li_cnt_Bar = iBarShift (fs_Symbol, 0, ldt_Begin);
//----
    for (int li_BAR = 1; li_BAR < li_cnt_Bar; li_BAR++)
    {
        if (fb_IsCandle)
        {
            ld_OPEN = iOpen (fs_Symbol, 0, li_BAR);
            ld_CLOSE = iClose (fs_Symbol, 0, li_BAR);
            ld_AVERAGE += MathAbs (ld_OPEN - ld_CLOSE);
        }
        else
        {
            ld_HIGH = iHigh (fs_Symbol, 0, li_BAR);
            ld_LOW = iLow (fs_Symbol, 0, li_BAR);
            ld_AVERAGE += (ld_HIGH - ld_LOW);
        }
    }
    ld_AVERAGE /= li_cnt_Bar;
    ld_AVERAGE = NormalizeDouble (ld_AVERAGE, 4);
    //Print ("Time[", li_cnt_Bar, "] = ", TimeToStr (ldt_Begin), "; ����� = ", DS0 (ld_AVERAGE / bd_Point));
//----
    return (ld_AVERAGE);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        ��������� ����������� ���������                                            |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fCheck_LibDecimal()
{
//----
    TrailingStop *= bi_Decimal;
    bd_TrailingStop = NDP (TrailingStop);
    TrailStep *= bi_Decimal;
    bd_TrailStep = NDP (TrailStep);
    BreakEven *= bi_Decimal;
    bd_BreakEven = NDP (BreakEven);
    Indent_Fr *= bi_Decimal;
    bd_IndentFr = NDP (Indent_Fr);
    Indent_Sh *= bi_Decimal;
    bd_IndentSh = NDP (Indent_Sh);
    Indent_Pr *= bi_Decimal;
    bd_IndentPr = NDP (Indent_Pr);
    Indent_MA *= bi_Decimal;
    bd_IndentMA = NDP (Indent_MA);
    TimeStep *= bi_Decimal;
    bd_TimeStep = NDP (TimeStep);
    //---- ��������� ���������� ��� ��������� "Udavka"
    Distance_1 *= bi_Decimal;
    Distance_2 *= bi_Decimal;
    bda_Distance[0] = NDP (Distance_1);
    bda_Distance[1] = NDP (Distance_2);
    Level_0 *= bi_Decimal;
    Level_1 *= bi_Decimal;
    Level_2 *= bi_Decimal;
    //---- ��������� ���������� ��� ��������� "������ �����������"
    ProfitLevel_1 *= bi_Decimal;
    StopLevel_1 *= bi_Decimal;
    ProfitLevel_2 *= bi_Decimal;
    StopLevel_2 *= bi_Decimal;
    ProfitLevel_3 *= bi_Decimal;
    StopLevel_3 *= bi_Decimal;
    bia_StopLevel[0] = StopLevel_1;
    bia_StopLevel[1] = StopLevel_2;
    bia_StopLevel[2] = StopLevel_3;
    bia_ProfitLevel[0] = ProfitLevel_1;
    bia_ProfitLevel[1] = ProfitLevel_2;
    bia_ProfitLevel[2] = ProfitLevel_3;
    for (int li_int = 0; li_int < 3; li_int++)
    {
        bda_StopLevel[li_int] = NDP (bia_StopLevel[li_int]);
        bda_ProfitLevel[li_int] = NDP (bia_ProfitLevel[li_int]);
    }
//----
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+

