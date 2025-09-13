//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|                                                        e-PSI@ManagerTrailling.mq4 |
//|                                                         Copyright � 2010, TarasBY |
//|                                                                taras_bulba@tut.by |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
#property copyright "Copyright � 2010-2011, TarasBY WM Z670270286972"
#property link      "taras_bulba@tut.by"
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|                  *****         ��������� ���������         *****                  |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
extern string SETUP_Expert          = "========== ����� ��������� ��������� ==========";
extern int    MG                       = 880;           // �����: > 0 - �����, 0 - �������� �������, > 0 - �������� ����������
extern datetime TimeControl            = D'2010.01.01 00:00'; // � ������ ������� ������������ ����� ������ �� �����
extern int    NewBarInPeriod           = -1;            // <= 0 - �������� �� ������ ������� ������ ����, -1 - �������� �� ������ ����
extern bool   ValueInCurrency          = FALSE;         // �� ��������� ������� ������� � ��������� � �������
extern bool   OnlyCurrentSymbol        = TRUE;          // �������� ������ � ������� �������� ��� � List_Symbols
extern string List_Symbols             = "EURUSD,GBPUSD,AUDUSD,NZDUSD,EURGBP"; // �������������� �������� ����
extern int    Slippage                 = 3;             // ���������� ���������� �� ����������� ����
extern int    NumberOfTry              = 10;            // ���������� ������� �� ���������� ����������� ��� �������� ������
extern int    PauseAfterError          = 10;            // ����� ����� ������ � �������� 
extern int    NumberAccount            = 0;             // ����� ��������� �����. �������� ������ �� ��������� �����. ��� �������� <=0 - ����� ����� �� ����������� 
extern string Setup_TraillingProfit = "============== Trailling Profit ==============="; 
extern bool   TrailProfit_ON           = FALSE;         // ��������� ��������� ������ �������
extern double TrailProfit_StartPercent = 10;            // ������� ������/������ ��� ������� ���������� ����������� ������ �������� 
extern double TrailProfit_LevelPercent = 5;             // �������� �������� �������� ������/������, ��� ������� ����������� �������� 
extern double TrailProfit_Start        = 50;            // �������� ������� ��� ������� ���������� ����������� ������ �������� 
extern double TrailProfit_Level        = 25;            // �������� ��������� �������� �������, ��� ������� ����������� �������� 
extern string Setup_TakeProfit      = "================ Take Profit =================="; 
extern bool   TakeProfit_ON            = FALSE;         // ��������� ����������� ��� ������ �������
extern double TakeProfitPercent        = 50.0;          // ������� ������� 
extern double TakeProfit               = 50.0;          // ��������������� ������� 
extern string Setup_StopLoss        = "================= Stop Loss ==================="; 
extern bool   StopLoss_ON              = FALSE;         // ��������� �������� ��� ������ �������
extern double StopLossPercent          = 20.0;          // ������� ������ 
extern double StopLoss                 = 20.0;          // ��������������� ������ 
extern string Setup_PartClose       = "================== PartClose =================="; 
extern bool   PartClose_ON             = TRUE;          // ��������� ������� ���������� �������� ��� �������
extern string PartClose_Levels         = "20/50/200";   // ������ ��������. ��������, ��� ���������� 10/20/5 ������ �������� ����������� ��� ���������� ������� ������� � 10 �������, ����� ��� ����� 20 ������� � ��� ����� 5 �������.
extern string PartClose_Percents       = "50/25/25";    // ������� �������� (����� ����������� "/") ��� ���������������� ������. ����� ������ ���� �� ���� ������� ������. ���� �������� ����� ������ � ����� 1.0 ���, ����������� 50% - 0.5, ����� 25% �� 1.0 - 0.3 � ������� 0.2
#include      <b-PSI@TrailingS.mqh>                     // ���������� ���������
extern string Setup_Services        = "=================== SERVICES ==================";
extern bool   ShowComment              = TRUE;          // ���������� �����������. 
extern bool   PrintCom                 = FALSE;         // �������� �����������.
extern bool   UseSendMail              = FALSE;         // ������������ �������� �������� ��������� 
extern double MinCangesPercent         = 0.5;           // ����������� ��������� ������� ��� ������� ���������� ����� 
extern bool   SoundAlert               = FALSE;         // ����
extern string Setup_Tester          = "=================== Tester ====================";
extern int    InTesterOrder            = 1;             // �������� ������ � ������� ��� ��������. 1 - ���, -1 - ���� 
//IIIIIIIIIIIIIIIIIII======���������� ���������� ���������======IIIIIIIIIIIIIIIIIIIIII+
string        gs_NameGV, gs_Base, gs_Info, gs_fName, gs_sign,
              gsa_Comment[6], ExpertName, gs_Symbol;
int           gia_PartClose_Levels[], gia_PartClose_Percents[], gia_CommTime[6],
              gi_Decimal = 1, gi_SL = 200, gi_MyOrders, gi_HistoryOrders, ind_ERR, gi_Digits;
double        gd_cur_Profit, gd_LastProfitPC, gd_Pribul, gd_ProfitPercent, gd_cur_ProfitVal, 
              gd_TrailingStop, gd_TrailingStopPercent, gd_TrailProfit_Level, gd_TrailProfit_Start,
              gd_TS_Profit_Percent, gd_Profit_CUR, gd_TS_Profit, gd_TS_Profit_CUR, gd_Point,
              gd_TakeProfit, gd_StopLoss, gd_Bid, gd_Ask;
bool          flag_BadAccount = False, flag_TrailingStop = False, flag_CloseSignal, gb_InfoPrint = false,
              gb_AllSymbols = false;
datetime      gdt_NewBarInPeriod, gdt_curTime;
//IIIIIIIIIIIIIIIIIII==========������������ ����������==========IIIIIIIIIIIIIIIIIIIIII+
#include      <stderror.mqh>                            // ���������� ����� ������
//#include      <stdlib.mqh>                              // ���������� ����������� ������
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|                  Custom expert initialization function                            |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int init()
{
    int li_size, li_int;
    string lsa_txt[3], tmpArr[];
//----
    //---- ��������� ��������� ������ ���������
    if (NumberAccount > 0)
    {
        if (AccountNumber() != NumberAccount)
        {
            flag_BadAccount = true;
            Alert ("�� ���������� ����� �����");
            return (0);
        }
    }      
    gs_NameGV = "ManagerTrailing";
    ExpertName = StringConcatenate (WindowExpertName(), "[", IIFs (MG >= 0, MG, "ALL"), "]:  ", fGet_NameTF (Period()), "_", Symbol());
    gs_sign = IIFs ((AccountCurrency() == "USD"), "$", IIFs ((AccountCurrency() == "EUR"), "�", "RUB"));
    if (IsTesting())
    {gs_NameGV = gs_NameGV + "_t_";}
    if (IsDemo())
    {gs_NameGV = gs_NameGV + "_d_";}
    lsa_txt[0] = "�������� ����� �������������� ������ �� ��������� ��������:\n";
    //---- ��������� GV-����������
    if (IsTesting() || IsOptimization())
    {
        for (li_int = GlobalVariablesTotal() - 1; li_int >= 0; li_int--)
        {
            if (StringFind (GlobalVariableName (li_int), gs_NameGV, 0) == 0)
            {GlobalVariableDel (GlobalVariableName (li_int));}
        }
        MG = IIFd ((MG >= 0), MG, 0);
        TimeControl = D'1990.01.01 00:00';
    }
    else
    {
        if (GlobalVariableCheck (StringConcatenate (gs_NameGV, "_#fl_TrailingStop")))
        {flag_TrailingStop = GlobalVariableGet (StringConcatenate (gs_NameGV, "_#fl_TrailingStop"));}
    }
    if (PrintCom || ShowComment)
    {gb_InfoPrint = true;}
    fGet_MarketInfo();
    if (OnlyCurrentSymbol)
    {
        List_Symbols = Symbol();
        lsa_txt[0] = List_Symbols;
    }
    else
    {
        if (StringLen (List_Symbols) == 0)
        {
            gb_AllSymbols = true;
            lsa_txt[0] = "ALL ";
        }
    }
    Print (lsa_txt[0], IIFs ((MG == 0), "��� �������� �� MAGIC !!!", "� ��������� �� MAGIC = " + MG + " !!!"));
    gd_Profit_CUR = fGet_Profit (MG);
    //---- ��������� ���������� � ������������ � ��������� ����� ��������
    gd_LastProfitPC = IIFd (!ValueInCurrency, NDDig (100.0 * ((AccountBalance() + gd_cur_Profit) / AccountBalance())), gd_cur_Profit);
    gd_TrailProfit_Start = IIFd (!ValueInCurrency, 100.0 + TrailProfit_StartPercent, TrailProfit_Start);
    gd_TrailProfit_Level = IIFd (!ValueInCurrency, TrailProfit_LevelPercent, TrailProfit_Level);
    gd_TakeProfit = IIFd (!ValueInCurrency, 100.0 + TakeProfitPercent, TakeProfit);
    gd_StopLoss = IIFd (!ValueInCurrency, 100.0 - StopLossPercent, -StopLoss);
    //---- ��������� ������ 5-�� �����
    if (gi_Digits == 3 || gi_Digits == 5)
    {gi_Decimal = 10;}
    Slippage *= gi_Decimal;
    gi_SL *= gi_Decimal;
    //---- ��������� ������ �� ����� � ������� �������
    if (PartClose_ON)
    {
        fSplitStrToStr (PartClose_Levels, tmpArr, "/");
        li_size = ArraySize (tmpArr);
        ArrayResize (gia_PartClose_Levels, li_size);
        for (li_int = 0; li_int < li_size; li_int++)
        {
            gia_PartClose_Levels[li_int] = StrToInteger (tmpArr[li_int]);
            gia_PartClose_Levels[li_int] *= gi_Decimal;
        }
        lsa_txt[1] = "��������� ������ �� ������ � ";
        lsa_txt[2] = "��� ���������� ������� ������� � ";
        fSplitStrToStr (PartClose_Percents, tmpArr, "/");
        ArrayResize (gia_PartClose_Percents, li_size);
        for (li_int = 0; li_int < li_size; li_int++)
        {
            gia_PartClose_Percents[li_int] = StrToInteger (tmpArr[li_int]);
            lsa_txt[1] = StringConcatenate (lsa_txt[1], gia_PartClose_Percents[li_int], IIFs ((li_int == li_size - 1), " ", ", "));
            lsa_txt[2] = StringConcatenate (lsa_txt[2], gia_PartClose_Levels[li_int], IIFs ((li_int == li_size - 1), " ��.", ", "));
        }
        lsa_txt[1] = StringConcatenate (lsa_txt[1], " ��������� �� ���� ", lsa_txt[2]);
        Print (lsa_txt[1]);
    }
    //---- ������� ��������
    if (ShowComment)
    {
        if (!OnlyCurrentSymbol)
        {
            string ls_ListSymbols = IIFs (gb_AllSymbols, "�� ����� ������������", "�� ������������:\n" + List_Symbols);
            if (NumberAccount > 0)
            {gs_Info = StringConcatenate ("�������� �� ����� �", AccountNumber(), " � �������� ", ls_ListSymbols);}
            if (NumberAccount == 0)
            {gs_Info = StringConcatenate ("�������� �� ������� ����� � �������� ", ls_ListSymbols);}
        }
        else
        {
            if (NumberAccount > 0)
            {gs_Info = StringConcatenate ("�������� �� ����� �", AccountNumber(), " ������ � �������� ", Symbol(), ".");}
            if (NumberAccount == 0)
            {gs_Info = StringConcatenate ("�������� �� ������� ����� ������ � �������� ", Symbol(), ".");}
        }
        if (!ValueInCurrency)
        {gs_Base = StringConcatenate ("������ � ��������� �� ��������.\nTrailProfit_ON = ", CheckBOOL (TrailProfit_ON), "; TrailProfit_StartPercent = ", DoubleToStr (TrailProfit_StartPercent, 1), " %; TrailProfit_LevelPercent = ", DoubleToStr (TrailProfit_LevelPercent, 1), " %",
        "\nTakeProfit_ON = ", CheckBOOL (TakeProfit_ON), "; TPPercent = ", DoubleToStr (TakeProfitPercent, 1), " %; StopLoss_ON = ", CheckBOOL (StopLoss_ON), "; SLPercent = ", DoubleToStr (StopLossPercent, 1), " %");}
        else
        {gs_Base = StringConcatenate ("������ � ������ ��������.\nTrailProfit_ON = ", CheckBOOL (TrailProfit_ON), "; TrailProfit_Start = ", gs_sign, DS0 (TrailProfit_Start), "; TrailProfit_Level = ", gs_sign, DS0 (TrailProfit_Level),
        "\nTakeProfit_ON = ", CheckBOOL (TakeProfit_ON), "; TakeProfit = ", gs_sign, DS0 (TakeProfit), "; StopLoss_ON = ", CheckBOOL (StopLoss_ON), "; StopLoss = ", gs_sign, DS0 (StopLoss));}
        gs_Base = StringConcatenate (gs_Base, "\n", IIFs ((NewBarInPeriod < 0), "�������� �� ������ ����", "����� ��: "));
    }
    //---- �������� ������� �������� ���������� ��� ����������� �� �������
    gdt_NewBarInPeriod = TimeCurrent() - NewBarInPeriod * 60;
    //---- ������� � ������ �������
    InitializeArray_STR (gsa_Comment);
    //---- ���������� ������ "������" � ������� ������������ (gsa_Comment)
    ind_ERR = ArraySize (gsa_Comment) - 1;
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
    if (IsTesting() || IsOptimization())
    {
        //---- ������� ����� ���� GV-����������
        for (int i = GlobalVariablesTotal() - 1; i >= 0; i--)
        {
            if (StringFind (GlobalVariableName (i), gs_NameGV) == 0)
            {GlobalVariableDel (GlobalVariableName (i));}
        }
    }
    else
    {GlobalVariableSet (StringConcatenate (gs_NameGV, "_#fl_TrailingStop"), flag_TrailingStop);}
    //---- ������ �� ������� ��������
    if (ShowComment)
    {fCommentInChart (gsa_Comment, gia_CommTime);}
//----
    return (0);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|               Custom expert iteration function                                    |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int start()
{
//----
    if (flag_BadAccount)
    {return (0);}
    int err = GetLastError();
    //---- ���������� � �������� ������ ������
    if (IsTesting())
    {
        if (OrdersTotal() < 3 && isNewBar (240))
        {
            if (isLossLastPos ("", -1, -1))
            {InTesterOrder = IIFd ((InTesterOrder == 1), -1, 1);}
            if (InTesterOrder == 1)
            {OrderSend (Symbol(), OP_BUY, 1.0, NDD (Ask), 0, NDD (Ask - gi_SL * Point), 0, NULL, MG, 0, CLR_NONE);}
            if (InTesterOrder == -1)
            {OrderSend (Symbol(), OP_SELL, 1.0, NDD (Bid), 0, NDD (Bid + gi_SL * Point), 0, NULL, MG, 0, CLR_NONE);}
        }
    }
    gdt_curTime = TimeCurrent();
    //---- ������� ������������ ������� � ������������ � ���������� �����������
    gd_Pribul = fCalculatePribul (-1, MG, TimeControl);
    //---- ������� ������� ������ � ������ ��������
    gd_Profit_CUR = fGet_Profit (MG);
    //---- ��������� ������� ������ � �������� �� ��������
    gd_ProfitPercent = NDDig (100.0 + gd_Profit_CUR * 100 / AccountBalance());
    //---- ������� ������ � ������������ � ���������� �����������
    if (ValueInCurrency) {gd_cur_Profit = gd_Profit_CUR;} else {gd_cur_Profit = gd_ProfitPercent;}
    //---- ���� ������� ���������, ���������� ��������� �� ����
    if (UseSendMail)
    {
        if (NDDig (MathAbs (gd_cur_Profit - gd_LastProfitPC)) >= MinCangesPercent)
        {
            gd_LastProfitPC = gd_cur_Profit;
            SendMail ("Account # " + AccountNumber(), StringConcatenate ("Profit Percent: ", DSDig (gd_TS_Profit_Percent), " %. ������: = ", gs_sign, DSDig (gd_cur_Profit)));
            if (SoundAlert) {PlaySound ("email.wav");}
        }
    }
    //---- ������� ���������� �� ������ (���� ���������)
    if (ShowComment)
    {fCommentInChart (gsa_Comment, gia_CommTime);}
    //---- ������ � ������ ���������� ���� (���� NewBarInPeriod >= 0)
    if (NewBarInPeriod >= 0)
    {
        if (gdt_NewBarInPeriod == iTime (Symbol(), NewBarInPeriod, 0))
        {return (0);}
        gdt_NewBarInPeriod = iTime (Symbol(), NewBarInPeriod, 0);
    }
    //---- ��������� ������� ������������ Take Profit ��� ����������� �������
    if (TakeProfit_ON && gi_MyOrders > 0)
    {fProfit_TP (MG);}
    //---- ��������� ������� ������������ Stop Loss ��� ����������� �������
    if (StopLoss_ON && gi_MyOrders > 0)
    {fProfit_SL (MG);}
    //---- ��������� ������� ������������ Trailing Profit ��� ����������� �������
    if (TrailProfit_ON && gi_MyOrders > 0)
    {fTrail_Profit (MG);}
    //---- �������������� ��������� �������� ������� � ��������
    if (!flag_TrailingStop && gi_MyOrders > 0)
    {fOrderControl (MG);}
    //---- ������������ ��������� ������
	 fGetLastError (gsa_Comment, "start()", ind_ERR);
//----
    return (0);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|        Trailling Profit                                                           |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fTrail_Profit (int fi_Magic = 0)
{
    int li_result = -1, err = GetLastError();
    string ls_sign = IIFs (ValueInCurrency, gs_sign, "%"), ls_txt = "";
    gs_fName = "fTrail_Profit()";
//----
    //---- ��������� ��������� Trailling
    if (gd_cur_Profit >= gd_TrailProfit_Start && !flag_TrailingStop)
    {
        flag_TrailingStop = true;
        GlobalVariableSet (StringConcatenate (gs_NameGV, "_#fl_TrailingStop"), flag_TrailingStop);
        ls_txt = fPrepareComment (StringConcatenate (gs_fName, ": ��������� ��������� TrailProfit\ncur_Profit = ", ls_sign, DS0 (gd_cur_Profit), "; TrailProfit_Start = ", ls_sign, DS0 (gd_TrailProfit_Start)), gb_InfoPrint);
        fPrintAndShowComment (ls_txt, ShowComment, PrintCom, gsa_Comment, 0);
    }
    //---- ��������� ���������� ��� ����������
    gd_TS_Profit_Percent = IIFd ((!flag_TrailingStop), gd_TS_Profit_Percent - TrailProfit_LevelPercent, MathMax (gd_TS_Profit_Percent, gd_ProfitPercent - TrailProfit_LevelPercent));
    gd_TS_Profit_CUR = IIFd ((!flag_TrailingStop), gd_Profit_CUR - TrailProfit_Level, MathMax (gd_TS_Profit_CUR, gd_Profit_CUR - TrailProfit_Level));
    gd_TS_Profit = IIFd (!ValueInCurrency, gd_TS_Profit_Percent, gd_TS_Profit_CUR);
    if (flag_TrailingStop)
    {
        //---- ������� ������������ �������� ������� �� TraillingProfit
        if (gd_cur_Profit <= gd_TS_Profit)
        {
            li_result = fOrderClose_All (fi_Magic);
            if (li_result == 1)
            {
                ls_txt = fPrepareComment (StringConcatenate (gs_fName, ": ��������� �� TrailProfit\ncur_Profit = ", ls_sign, DS0 (gd_cur_Profit), "; TS_Profit = ", ls_sign, DS0 (gd_TS_Profit), "; TrailProfit_Start = ", ls_sign, DS0 (gd_TrailProfit_Start)), gb_InfoPrint);
                fPrintAndShowComment (ls_txt, ShowComment, PrintCom, gsa_Comment, 1);
                gsa_Comment[0] = "";
                if (SoundAlert) {PlaySound ("ok.wav");}
                //flag_TrailingStop = false;
                //GlobalVariableSet (StringConcatenate (gs_NameGV, "_#fl_TrailingStop"), flag_TrailingStop);
                gi_MyOrders = 0;
            }
            if (UseSendMail)
            {
                SendMail ("Account # " + AccountNumber(), IIFs ((li_result == 1), "Close all OK", "Close all ERROR"));
                if (SoundAlert) {PlaySound ("email.wav");}
            }
        }
    }
    if (ShowComment)
    {
        if (flag_TrailingStop)
        {
            if (!ValueInCurrency)
            {ls_txt = StringConcatenate (gs_fName, ": �������� �� ��������� �� ������: ", DSDig (gd_TS_Profit_Percent - 100), " %");}
            else
            {ls_txt = StringConcatenate (gs_fName, ": �������� �� ��������� �� ������: ", gs_sign, DSDig (gd_TS_Profit_CUR), "");}
        }
        else
        {
            if (gd_Profit_CUR <= 0)
            {ls_txt = StringConcatenate (gs_fName, ": ������� ��� �� �������������");}
            else
            {ls_txt = StringConcatenate (gs_fName, ": ������� ��� �� �������� �������� TrailProfit_Start (", DoubleToStr (IIFd (ValueInCurrency, TrailProfit_Start, TrailProfit_StartPercent), 1), " ", ls_sign, ")");}
        }
        fPrintAndShowComment (ls_txt, ShowComment, False, gsa_Comment, 1);
    }
    //---- ������������ ��������� ������
	 fGetLastError (gsa_Comment, "fTrail_Profit()", ind_ERR);
    if (li_result == 1)
    {return (true);}
//----
    return (false);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|        ��������� ��� ������ �� ���������� ���������������� �������                |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fProfit_TP (int Magic = 0)
{
    string ls_txt = "";
    int    err = GetLastError();
//----
    gs_fName = "fProfit_TP()";
    if (gd_cur_Profit >= gd_TakeProfit) {flag_CloseSignal = true;} else {flag_CloseSignal = false;}
    if (flag_CloseSignal)
    {
        int result = 0; 
        result = fOrderClose_All (Magic);
        if (result == 1)
        {
            string ls_sign = IIFs (ValueInCurrency, gs_sign, "%");
            flag_CloseSignal = false;
            if (SoundAlert) {PlaySound ("ok.wav");}
            ls_txt = fPrepareComment (StringConcatenate (gs_fName, ": �������� TP | Profit = ", DoubleToStr (gd_cur_Profit, 1), " ", ls_sign, "; TakeProfit = ", DoubleToStr (gd_TakeProfit, 1), " ", ls_sign, "."), gb_InfoPrint);
            fPrintAndShowComment (ls_txt, ShowComment, PrintCom, gsa_Comment, 2);
            gsa_Comment[0] = "";
            gi_MyOrders = 0;
        }
        if (UseSendMail)
        {
            SendMail ("Account # " + AccountNumber(), IIFs ((result == 0), "Close all OK", "Close all ERROR"));
            if (SoundAlert) {PlaySound ("email.wav");}
        }
    }
    //---- ������������ ��������� ������
	 fGetLastError (gsa_Comment, "fProfit_TP()", ind_ERR);
//----
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|        ��������� ��� ������ �� ���������� ���������� �����                        |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fProfit_SL (int Magic = 0)
{
    string ls_txt = "";
    int    err = GetLastError();
//----
    gs_fName = "fProfit_SL()";
    if (gd_cur_Profit <= gd_StopLoss) {flag_CloseSignal = true;} else {flag_CloseSignal = false;}
    if (flag_CloseSignal)
    {
        int result = 0;
        result = fOrderClose_All (Magic);
        if (result == 1)
        {
            string ls_sign = IIFs (ValueInCurrency, gs_sign, "%");
            flag_CloseSignal = false;
            if (SoundAlert) {PlaySound ("ok.wav");}
            ls_txt = fPrepareComment (StringConcatenate (gs_fName, ": �������� SL | Profit = ", DoubleToStr (gd_cur_Profit, 1), " ", ls_sign, "; StopLoss = ", DoubleToStr (gd_StopLoss, 1), " ", ls_sign, "."), gb_InfoPrint);
            fPrintAndShowComment (ls_txt, ShowComment, PrintCom, gsa_Comment, 3);
            gsa_Comment[0] = "";
            gi_MyOrders = 0;
        }
        if (UseSendMail)
        {
            SendMail ("Account # " + AccountNumber(), IIFs ((result == 0), "Close all OK", "Close all ERROR"));
            if (SoundAlert) {PlaySound ("email.wav");}
        }
    }
    //---- ������������ ��������� ������
	 fGetLastError (gsa_Comment, "fProfit_SL()", ind_ERR);
//----
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|       �������� ������ �� ���� ����� �������                                       |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
double fGet_Profit (int Magic)
{
    int li_int, li_total = OrdersTotal() - 1;
    double ld_Profit = 0;
    static int pre_Orders = 0;
//----
    gi_MyOrders = 0;
    for (li_int = li_total; li_int >= 0; li_int--) 
    {
        if (OrderSelect (li_int, SELECT_BY_POS, MODE_TRADES))
        {
            if (StringFind (List_Symbols, OrderSymbol()) < 0 && !gb_AllSymbols)
            {continue;}
            if (OrderMagicNumber() == Magic || Magic < 0)
            {
                if (OrderType() == OP_BUY || OrderType() == OP_SELL)
                {
                    ld_Profit += OrderProfit() + OrderSwap() + OrderCommission();
                    gi_MyOrders++;
                }
            }
        }
    }
	 //---- ���������� ���� ��������� ������ �������
    if (pre_Orders != gi_MyOrders)
    {
        pre_Orders = gi_MyOrders;
        if (gi_MyOrders == 0)
        {
            flag_TrailingStop = false;
            GlobalVariableSet (StringConcatenate (gs_NameGV, "_#fl_TrailingStop"), flag_TrailingStop);
        }
    }
//----
    return (ld_Profit);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//+===================================================================================+
//|***********************************************************************************|
//| ������: ������ � ��������                                                         |
//|***********************************************************************************|
//+===================================================================================+
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|       ������� ��� ������, �� �������� �������                                     |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int fOrderClose_All (int fi_Magic = 0)
{
    int li_result = 0, li_Error = GetLastError(), li_cnt = 0;
    double ld_tmp, ld_Itog = 0.0;
//----
    for (int i = OrdersTotal() - 1; i >= 0; i--)
    {
        if (OrderSelect (i, SELECT_BY_POS, MODE_TRADES))
        {
            if (StringFind (List_Symbols, OrderSymbol()) < 0 && !gb_AllSymbols)
            {continue;}
            if (fi_Magic <= 0 || OrderMagicNumber() == fi_Magic)
            {
                if (!IsTradeContextBusy())
                {
                    fGet_MarketInfo();
                    if (!ClosePosBySelect (OrderTicket(), ld_tmp))
                    {
                        li_Error = GetLastError();
                        Print ("Error close ", GetNameOP (OrderType()), "[", OrderTicket(), "]: ", ErrorDescription (li_Error));
                        li_result = -1;
                        if (SoundAlert) {PlaySound ("alert2.wav");}
                    }
                    else
                    {
                        if (SoundAlert) {PlaySound ("ok.wav");}
                        ld_Itog += ld_tmp;
                        li_result = 1;
                        li_cnt++;
                    }
                }
                else
                {
                    static int cur_time = 0;
                    if (TimeCurrent() > cur_time + 20)
                    {
                        cur_time = TimeCurrent();
                        Print ("Need close ", GetNameOP (OrderType()), "[", OrderTicket(), "]. Trade Context Busy.");
                        if (SoundAlert) {PlaySound ("disconnect.wav");}
                    }
                    return (-2);
                }
            }
        }
    }
    if (li_result == 1)
    {if (PrintCom) Print ("fOrderClose_All(): ��������� �������� ", IIFs ((li_cnt == 1), "������", li_cnt + " �������"), " = ", gs_sign, DSDig (ld_Itog));}
    //---- ������������ ��������� ������
	 fGetLastError (gsa_Comment, "fOrderClose_All()", ind_ERR);
//----
    return (li_result);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|       ������ �� �������� �� �������� ������ �������� ����                         |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int fOrderControl (int iMagic)
{
    int ParentTicket, li_Num, li_Ticket, err = GetLastError();
    double ld_Lots, LotsClose, ld_Price, ld_temp;
    string ls_result = "", ls_Comment;
//----
    gs_fName = "fOrderControl()";
    if (PartClose_ON)
    {
        for (int i = OrdersTotal() - 1; i >= 0; i--)
        {
            if (OrderSelect (i, SELECT_BY_POS, MODE_TRADES))
            {
                if (StringFind (List_Symbols, OrderSymbol()) < 0 && !gb_AllSymbols)
                {continue;}
                if (OrderMagicNumber() == -1 || OrderMagicNumber() == iMagic)
                {
                    if (OrderType() > 1)
                    {continue;}
                    ParentTicket = 0;
                    li_Num = 0;
                    ld_Lots = OrderLots();
                    ls_Comment = OrderComment();
                    li_Ticket = OrderTicket();
                    ParentTicket = fGet_ParentTicket (ls_Comment);
                    if (ParentTicket != 0)
                    {
                        if (GlobalVariableCheck (gs_NameGV + ParentTicket + "#_Num"))
                        {li_Num = GlobalVariableGet (gs_NameGV + ParentTicket + "#_Num") + 1;}
                        if (GlobalVariableCheck (gs_NameGV + ParentTicket + "#_Lots"))
                        {ld_Lots = GlobalVariableGet (gs_NameGV + ParentTicket + "#_Lots");}
                    }
                    GlobalVariableSet (gs_NameGV + li_Ticket + "#_Num", li_Num);
                    GlobalVariableSet (gs_NameGV + li_Ticket + "#_Lots", ld_Lots);
                    if (li_Num >= ArraySize (gia_PartClose_Levels))
                    {li_Num = ArraySize (gia_PartClose_Levels) - 1;}
                    RefreshRates();
                    fGet_MarketInfo();
                    if (OrderType() == OP_BUY)
                    {
                        ld_Price = gd_Bid;
                        if (ld_Price - OrderOpenPrice() >= NDP (gia_PartClose_Levels[li_Num]))
                        {
                            LotsClose = fLotsNormalize (gia_PartClose_Percents[li_Num] * ld_Lots / 100.0);
                            if (LotsClose > 0)
                            {
                                LotsClose = MathMin (LotsClose, OrderLots());
                                if (!ClosePosBySelect (li_Ticket, ld_temp, LotsClose))
                                {
                                    Print ("Close error ", GetLastError());
                                    if (SoundAlert) {PlaySound ("alert2.wav");}
                                }
                                else
                                {
                                    ls_result = fPrepareComment (StringConcatenate (gs_fName, ": ", "��������� ", IIFs ((li_Num == 2), "�������������", "���������"), " �������� ������",
                                    "\n[", gs_Symbol, ":", IIFd ((ParentTicket != 0), ParentTicket, li_Ticket), "/", li_Num, "].", IIFs ((ParentTicket == 0), "", " ����������� - " + ls_Comment + "."), " LotsClose = ", LotsClose), gb_InfoPrint);
                                    if (SoundAlert) {PlaySound ("ok.wav");}
                                    continue;
                                }
                            }
                        }
                    }
                    if (OrderType() == OP_SELL)
                    {
                        ld_Price = gd_Ask;
                        if (OrderOpenPrice() - ld_Price >= NDP (gia_PartClose_Levels[li_Num]))
                        {
                            LotsClose = fLotsNormalize (gia_PartClose_Percents[li_Num] * ld_Lots / 100.0);
                            if (LotsClose > 0)
                            {
                                LotsClose = MathMin (LotsClose, OrderLots());
                                if (!ClosePosBySelect (OrderTicket(), ld_temp, LotsClose))
                                {
                                    Print ("Close error ", GetLastError());
                                    if (SoundAlert) {PlaySound ("alert2.wav");}
                                }
                                else
                                {
                                    ls_result = fPrepareComment (StringConcatenate (gs_fName, ": ", "��������� ", IIFs ((li_Num == 2), "�������������", "���������"), " �������� ������",
                                    "\n[", gs_Symbol, ":", IIFd ((ParentTicket != 0), ParentTicket, li_Ticket), "/", li_Num, "].", IIFs ((ParentTicket == 0), "", " ����������� - " + ls_Comment + "."), " LotsClose = ", LotsClose), gb_InfoPrint);
                                    if (SoundAlert) {PlaySound ("ok.wav");}
                                    continue;
                                }
                            }
                        }
                    }
                    if (OrderSymbol() == Symbol())
                    //---- ������ ��������� �������
                    {fTrail_Position (OrderTicket());}
                }
            }
            else
            {return (0);}
        }
    }
    fPrintAndShowComment (ls_result, ShowComment, PrintCom, gsa_Comment, 4);
    //---- ������������ ��������� ������
	 fGetLastError (gsa_Comment, gs_fName, ind_ERR);
//----
    return (0);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|         �������� �������� ���������� �� �������                                   |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fGet_MarketInfo() 
{
//----
	 if (gs_Symbol != OrderSymbol())
	 {
		  gs_Symbol = OrderSymbol();
		  gi_Digits = MarketInfo (gs_Symbol, MODE_DIGITS);
		  gd_Point = MarketInfo (gs_Symbol, MODE_POINT);
	 }
    if (!OnlyCurrentSymbol)
    {
        gd_Bid = MarketInfo (gs_Symbol, MODE_BID);
        gd_Ask = MarketInfo (gs_Symbol, MODE_ASK);
    }
    else
    {
        gd_Bid = Bid;
        gd_Ask = Ask;
    }
//----
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|         �������� ����� �������������� ��������� �������                           |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool ClosePosBySelect (int ticket, double& dOrderProfit, double LotClose = 0)
{
    bool lb_result;
    double ld_Lots, ld_Price, ld_Itog;   
    int Error, it, li_Type;
    string ls_Symbol;
//----
    li_Type = OrderType();
    if (li_Type == OP_BUY || li_Type == OP_SELL) 
    {
        ls_Symbol = OrderSymbol();
        for (it = 1; it <= NumberOfTry; it++)
        {
            if (!IsTesting() && (!IsExpertEnabled() || IsStopped()))
            {break;}
            while (!IsTradeAllowed())
            {Sleep (2000);}
            RefreshRates();
            ld_Lots = IIFd ((LotClose == 0.0), OrderLots(), LotClose);
            ld_Price = MarketInfo (ls_Symbol, IIFd ((li_Type == OP_BUY), MODE_BID, MODE_ASK));
            if (OrderClose (ticket, ld_Lots, ld_Price, Slippage, CLR_NONE))
            {
                dOrderProfit = OrderProfit() / OrderLots() * ld_Lots;
                if (PrintCom) Print (gs_fName, ": ������� �����-", GetNameOP (li_Type), "[", ticket, "/", DSDig (ld_Lots), "]; ���� = ", gs_sign, DSDig (dOrderProfit), ".");
                return (true);
            } 
            else 
            {
                Error = GetLastError();
                if (Error == 146)
                {
                    while (IsTradeContextBusy())
                    {Sleep (1000 * 11);}
                }
                if (PrintCom) Print (gs_fName, ": Error � ", ErrorDescription (Error), ". ��������� ", GetNameOP (li_Type), "; ������� ", it, "-�. \n",
                "Ticket = ", ticket, "; ", IIFs ((li_Type == OP_BUY), "Bid", "Ask"),  " = ", DSD (ld_Price), ";\n",
                "Sym = ", ls_Symbol, "; Lot = ", DSDig (ld_Lots), "; SL = ", DSD (OrderStopLoss()), "; TP = ", DSD (OrderTakeProfit()), ";  Magic = ", OrderMagicNumber());
                Sleep (1000 * 5);
            }
        }
    } 
    else 
    {Print ("������������ �������� ��������. Close ", GetNameOP (li_Type));}
//----
    return (false);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                          |
//+-----------------------------------------------------------------------------------+
//|  ������   : 19.02.2008                                                            |
//|  �������� : ���������� ���� ����������� ��������� �������.                        |
//+-----------------------------------------------------------------------------------+
//|  ���������:                                                                       |
//|    sy - ������������ �����������   (""   - ����� ������,                          |
//|                                     NULL - ������� ������)                        |
//|    op - ��������                   (-1   - ����� �������)                         |
//|    mn - MagicNumber                (-1   - ����� �����)                           |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool isLossLastPos (string sy = "", int op = -1, int mn = -1)
{
    datetime t;
    int i, j = -1, k = OrdersHistoryTotal();
//----
    sy = IIFs ((sy == "0" || sy == ""), Symbol(), sy);
    for (i = k - 1; i >= 0; i--)
    {
        if (OrderSelect (i, SELECT_BY_POS, MODE_HISTORY))
        {
            if (OrderSymbol() == sy && (mn < 0 || OrderMagicNumber() == mn))
            {
                if (OrderType() == OP_BUY || OrderType() == OP_SELL)
                {
                    if (op < 0 || OrderType() == op)
                    {
                        if (t < OrderCloseTime())
                        {
                            t = OrderCloseTime();
                            j = i;
                        }
                    }
                }
            }
        }
    }
    if (OrderSelect (j, SELECT_BY_POS, MODE_HISTORY))
    {
        if (OrderProfit() < 0)
        {return (True);}
    }
//----
    return (False);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//+===================================================================================+
//|***********************************************************************************|
//| ������: ������ � ���������                                                        |
//|***********************************************************************************|
//+===================================================================================+
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  UNI:               �������������� ������ STRING                                  |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int InitializeArray_STR (string& PrepareArray[], string Value = "")
{
    int l_int, size = ArraySize (PrepareArray);
//----
    for (l_int = 0; l_int < size; l_int++)
    {PrepareArray[l_int] = Value;}
//----
    return;
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|        ���������� ������ STRING �� ������, ���������� sDelimiter                 |
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
//+===================================================================================+
//|***********************************************************************************|
//| ������: ����� �������                                                             |
//|***********************************************************************************|
//+===================================================================================+
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|             ���������� ������������ ��������� (��\���)                            |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
string CheckBOOL (int M)
{
    switch (M)
    {
        case 0: {return ("���");}
        case 1: {return ("��");}
    }
    return ("�� ����...");
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                          |
//+-----------------------------------------------------------------------------------+
//|  ������   : 01.09.2005                                                            |
//|  �������� : ���������� ������������ �������� ��������                             |
//+-----------------------------------------------------------------------------------+
//|  ���������:                                                                       |
//|    op - ������������� �������� ��������                                           |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
string GetNameOP (int op) 
{
    switch (op) 
    {
        case OP_BUY      : return ("BUY");
        case OP_SELL     : return ("SELL");
        case OP_BUYLIMIT : return ("BUY Limit");
        case OP_SELLLIMIT: return ("SELL Limit");
        case OP_BUYSTOP  : return ("BUY Stop");
        case OP_SELLSTOP : return ("SELL Stop");
        default          : return ("None (" + op + ")");
    }
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  �����    : TarasBY                                                               |
//+-----------------------------------------------------------------------------------+
//|  ������   : 09.08.2009                                                            |
//|  �������� : ���������� ������������ ����������                                    |
//+-----------------------------------------------------------------------------------+
//|  ���������:                                                                       |
//|    fi_Period - ���������                                                          |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
string fGet_NameTF (int fi_Period)
{
//----
    switch (fi_Period) 
    {
        case 1: return ("M1");
        case 5: return ("M5");
        case 15: return ("M15");
        case 30: return ("M30");
        case 60: return ("H1");
        case 240: return("H4");
        case 1440: return ("D1");
        case 10080: return ("W1");
        case 43200: return ("MN1");
        default: return ("undefined " + fi_Period);
    }
//----
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|        ������������ ������ ������ ���� �� ���������� �������                      |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool isNewBar (int iTimeFrame)
{
    int iIndex = -1;
//----
    switch (iTimeFrame)
    {
        case 1    : iIndex = 0; break;
        case 5    : iIndex = 1; break;
        case 15   : iIndex = 2; break;
        case 30   : iIndex = 3; break;
        case 60   : iIndex = 4; break;
        case 240  : iIndex = 5; break;
        case 1440 : iIndex = 6; break;
        default   : iIndex =-1; break;
    }
    
    static int LastBar[7]= {0,0,0,0,0,0,0}; 
    datetime curbar = iTime (Symbol(), iTimeFrame, 0);
    if (LastBar[iIndex] != curbar)
    {
        LastBar[iIndex] = curbar;
        return (true);
    }
    else
    {return (false);}
//----
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|       �������� Ticket ������������� ������                                        |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int fGet_ParentTicket (string aComment)
{
    int tPos1 = StringFind (aComment, "from");
//----
    if (tPos1 >= 0)
    {
        int tPos2 = StringFind (aComment, "#");
        if (tPos2 > tPos1)
        {return (StrToInteger (StringSubstr (aComment, tPos2 + 1, StringLen (aComment) - tPos2 - 1)));}
    }
//----
    return (0);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                          |
//+-----------------------------------------------------------------------------------+
//|  ������   : 01.02.2008                                                            |
//|  �������� : ���������� ���� �� ���� �������� ������������ �� �������.             |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
double IIFd (bool condition, double ifTrue, double ifFalse)
{
    if (condition)
    {return (ifTrue);}
    else {return (ifFalse);}
}
//+-----------------------------------------------------------------------------------+
string IIFs (bool condition, string ifTrue, string ifFalse)
{
    if (condition)
    {return (ifTrue);}
    else {return (ifFalse);}
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|        �������, ����������� ����������� ����������� ����                          |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int LotDecimal() {return (MathCeil (MathAbs (MathLog (MarketInfo (Symbol(), MODE_LOTSTEP)) / MathLog (10))));}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|        �������, �������� int � double �� Point                                    |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
double NDP (int v) {return (v * gd_Point);}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|        �������, �������� �������� �� double � string c ������������� ��           |
//| ����������� ����������� ����                                                      |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
string DSDig (double v) {return (DoubleToStr (v, LotDecimal()));} 
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|        �������, ������������ �������� double �� ����������� ����������� ����      |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
double NDDig (double v) {return (NormalizeDouble (v, LotDecimal()));}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|        �������, �������� �������� �� double � string c ������������� �� Digit     |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
string DSD (double v) {return (DoubleToStr (v, gi_Digits));} 
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//+===================================================================================+
//|***********************************************************************************|
//| ������: Money Management                                                          |
//|***********************************************************************************|
//+===================================================================================+
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|       ���������� ������������ ����                                                |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
double fLotsNormalize (double aLots)
{
    aLots -= MarketInfo (Symbol(), MODE_MINLOT);
    aLots /= MarketInfo (Symbol(), MODE_LOTSTEP);
    aLots = MathRound (aLots);
    aLots *= MarketInfo (Symbol(), MODE_LOTSTEP);
    aLots += MarketInfo (Symbol(), MODE_MINLOT);
    aLots = NormalizeDouble (aLots, LotDecimal());
//----
    return (aLots);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|       ������� ������������ ������� (���� ������ ����������)                       |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
double fCalculatePribul (int op = -1, int magic = -1, datetime dt = 0)
{
    int li_int, history_total = OrdersHistoryTotal();
    static int pre_total = 0;
    if (pre_total == history_total)
    {
        pre_total = history_total;
        return (gd_Pribul);
    }
    double ld_Pribul = 0.0;
//----
    gi_HistoryOrders = 0;
    for (li_int = history_total - 1; li_int >= 0; li_int--)
    {
        if (OrderSelect (li_int, SELECT_BY_POS, MODE_HISTORY))
        {
            if (StringFind (List_Symbols, OrderSymbol()) < 0 && !gb_AllSymbols)
            {continue;}
            if (op < 0 || OrderType() == op)
            {
                if (OrderType() == OP_BUY || OrderType() == OP_SELL)
                {
                    if (magic <= 0 || OrderMagicNumber() == magic)
                    {
                        if (dt < OrderCloseTime())
                        {
                            ld_Pribul += OrderProfit() + OrderSwap() + OrderCommission();
                            gi_HistoryOrders++;
                        }
                    }
                }
            }
        }
    }
    return (ld_Pribul);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//+===================================================================================+
//|***********************************************************************************|
//| ������: ��������� �������                                                         |
//|***********************************************************************************|
//+===================================================================================+
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|        ������� ���������� �� ����                                                 |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fCommentInChart (string& ar_Comment[], int& ar_ComTime[])
{
    static string lsa_Time[], lsa_Comment[], lsa_CommTime[], lsa_tmp[],
                  ls_row = "���������������������������������\n",
                  ls_PSI = "������������� PSI�TarasBY �������������\n";
    static bool   lb_first = true;
    static int    li_size, li_size_CommTime, li_Period = 60;
    string ls_CTRL = "", ls_BLOCK_Comment, ls_Comment = "",
           ls_Error = "", ls_time = "", ls_sign;
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
            if (StringLen (lsa_Comment[li_MSG]) > 0) {ls_Error = StringConcatenate (ls_row, "ERROR:  ", lsa_Time[li_MSG], "\n", lsa_Comment[li_MSG]);}
        }
    }
    //---- ������ �������� �� �������� ������ ���������
    if (NewBarInPeriod >= 0) {ls_time = TimeToStr (gdt_NewBarInPeriod + li_Period);}
    //---- ��������� ��� ����� ������������
    ls_BLOCK_Comment = StringConcatenate (ExpertName, "\n", ls_row, gs_Info, "\n",
                 gs_Base, ls_time, "\n",
                 ls_row,
                 //---- ���� ����������� ������
                 "          PROFIT    = ", gs_sign, " ", DoubleToStr (gd_Profit_CUR, 1), " | ", DoubleToStr (gd_ProfitPercent - 100.0, 1), " % [", gi_MyOrders, "]\n",
                 "          RESULT    = ", gs_sign, " ", DoubleToStr (gd_Pribul, 1), "[", gi_HistoryOrders, "]\n",
                 ls_PSI,
                 //---- ���� ������������
                 ls_Comment,
                 //---- ���������� ������
                 ls_Error);//,
    //---- ������� �� ���� �������������� ���� ������������
    Comment (ls_BLOCK_Comment);
//----
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//| ������� � ������ �� ������ � �� ������ �����������                                |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
string fPrepareComment (string sText = "", bool bConditions = false)
{if (bConditions) if (StringLen (sText) > 0) {return (sText);} return ("");}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|        ������� �� ������ � �� ������ �����������                                  |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fPrintAndShowComment (string &Text, bool Show_Conditions, bool Print_Conditions, string& s_Show[], int ind = -1)
{
    if (StringLen (Text) > 0)
    {
        if (Show_Conditions || Print_Conditions)
        {
            if (ind >= 0 && Show_Conditions)
            {s_Show[ind] = Text;}
            if (Print_Conditions)
            {Print (Text);}
            Text = "";
        }
    }
//---- 
    return;
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//+===================================================================================+
//|***********************************************************************************|
//| ������: ������ � ��������                                                         |
//|***********************************************************************************|
//+===================================================================================+
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|     �������� ����� � �������� ��������� ������ � ������� � ������ ���������       |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int fGetLastError (string& Comm_Array[], string Com = "", int index = -1)
{
    int    err = GetLastError();
    string ls_err;
//---- 
    if (err > 0)
    {
        ls_err = StringConcatenate (Com, ": ������ � ", err, ": ", ErrorDescription (err));
        Print (ls_err);
        if (index >= 0)
        {Comm_Array[index] = ls_err;}
    }
//---- 
    return (err);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+

