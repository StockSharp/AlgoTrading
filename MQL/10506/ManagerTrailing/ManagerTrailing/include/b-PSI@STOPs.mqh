//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|                                                                   b-PSI@STOPs.mqh |
//|                                    Copyright � 2011-12, Igor Stepovoi aka TarasBY |
//|                                                                taras_bulba@tut.by |
//|   20.05.2012  ���������� �������� � �������� ������.                              |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|   ������ ������� ������������ ��� �������������� �������������. ���������� �����- |
//|���� ������ ��� �������� ����� ������ (TarasBY). �������������� ��������� ���� ��- |
//|������� ������ ��� ������� ���������� ������� ������, ������ � ����� ������.  ���- |
//|���� ���������� ��� ��������� � ������ ���������.                                 |
//|   ����� �� ����� ��������������� �� ��������� ������, ���������� � ���������� ��- |
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
//IIIIIIIIIIIIIIIIIII==================CONSTANS=================IIIIIIIIIIIIIIIIIIIIII+
//IIIIIIIIIIIIIIIIIII==========������� ��������� ������=========IIIIIIIIIIIIIIIIIIIIII+
extern string SETUP_STOPs           = "================ Make STOP`s ==================";
extern int    N_STOPs                  = 0;                // N (�����) ������������ ������ (0 - 3)
extern bool   USE_Dinamic_SL           = TRUE;             // ������������ SL
extern bool   USE_Dinamic_TP           = FALSE;            // ������������ TP
extern int    TF_STOPs                 = PERIOD_H1;        // ��������� �� ������� ������������ �����
extern int    MIN_StopLoss             = 30;               // ����������� SL
extern int    MIN_TakeProfit           = 30;               // ����������� TP
extern int    LevelFIBO_SL             = 0;                // ������� �� FIBO ����\���� ���� (-2 - 5)
extern int    LevelFIBO_TP             = 0;                // ������� �� FIBO ����\���� ���� (-2 - 5)
extern string Setup_STATIC          = "-----------------  N0 - STATIC ----------------";
extern int    StopLoss                 = 100;
extern int    TakeProfit               = 500;
#include      <b-PSI@VirtualSTOPs.mqh>                     // ���������� ����������� ������
#include      <b-PSI@LEVELs_Light.mqh>                     // ���������� ������� �������
//IIIIIIIIIIIIIIIIIII=========����������� ������� �������=======IIIIIIIIIIIIIIIIIIIIII+

//IIIIIIIIIIIIIIIIIII========���������� ���������� ������=======IIIIIIIIIIIIIIIIIIIIII+
double        bd_MIN_SL, bd_MIN_TP;
int           bia_LevelFIBO[2];
//IIIIIIIIIIIIIIIIIII===========�������� ������� ������=========IIIIIIIIIIIIIIIIIIIIII+
//bool fInit_STOPs()                 // ������������� ������
/*bool fCreat_STOPs (int fi_NSTOPs,                // N (�����) ������������� ������
                     int fi_Ticket,                // Ticket ������
                     double fd_Price,              // ������� ���� �� �����������
                     int fi_Period,                // TF � �������� ������������ �������
                     bool fb_USE_VirtualSTOPs = false)*/// ���� ������������� ����������� �����
                                     // ������������ � ������������ �����
/*void fCreat_LevelsByFIBO (double ar_Extrem[],    // ������ �����������
                            double& ar_STOPs[],    // ������������ ������ ������
                            int ar_LevelFIBO[])*/  // ������ ������� ������� (0 - ��� SL; 1 - ��� TP)
                                     // ����������� ����� � ������ ������� FIBO (�������� � ����������).
//bool fCheck_STOPsParameters()      // ��������� ���������� � ���������� ������� ���������
//|***********************************************************************************|
//| ������: ����� �������                                                             |
//|***********************************************************************************|
//void fSet_ValuesSTOPs()            // �������� ������� ���������� � ������������ � ������������
//void fCheck_DecimalSTOPs()         // ��������� ����������� ���������
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//         ������������� ������                                                       |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fInit_STOPs()
{
//----
    //---- ���������� �������� ������������ � ���������� ��������
    if (!fCheck_STOPsParameters())
    {
        Alert ("��������� ��������� ���������� ���� ����� !!!");
        bb_OptimContinue = true;
        return (false);
    }
    //---- �������������� ���������� ���������� ����������� ������
    fInit_VirtualSTOPs();
    //---- �������������� ���������� ���������� ������� �������
    if (!fInit_LEVELs (N_STOPs)) return (false);
    //---- �������� ������� ���������� � ������������ � ������������ ��������� ��
    fCheck_DecimalSTOPs();
    bia_LevelFIBO[0] = LevelFIBO_SL;
    bia_LevelFIBO[1] = LevelFIBO_TP;
    bb_ClearGV = true;
    string lsa_Array[1];
    lsa_Array[0] = "_#STOP";
    fCreat_ArrayGV (bsa_prefGV, lsa_Array);
    //---- ������������ ��������� ������
	 fGet_LastErrorInArray (bsa_Comment, "fInit_STOPs()", bi_indERR);
//----
    return (true);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        ������������ � ������������ �����                                          |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fCreat_STOPs (int fi_NSTOPs,                    // N (�����) ������������� ������
                   int fi_Ticket,                    // Ticket ������
                   double fd_Price,                  // ������� ���� �� �����������
                   int fi_Period,                    // TF � �������� ������������ �������
                   bool fb_USE_VirtualSTOPs = false) // ���� ������������� ����������� ������
{
    //---- ���� �� ����� ������������ ����������� - �������
    string ls_Name = StringConcatenate (fi_Ticket, "_#STOP");
    if (GlobalVariableCheck (ls_Name)) {if (GlobalVariableGet (ls_Name) == 1) return (false);}
//----
    int    err = GetLastError(), li_Type = OrderType(), li_cmd = 1;
    double lda_STOPs[] = {0.0,0.0}, lda_Levels[2];
    string ls_Symbol = OrderSymbol();
    static string lsa_NameSTOPs[] = {"Classic","Extremum","ATR","ZZ"};
    bool lba_Modify[] = {false,false};
//----
    bs_libNAME = "b-STOPs_Light";
    bs_fName = lsa_NameSTOPs[fi_NSTOPs];
    //ArrayInitialize (ar_STOPs, 0.0);
    if (li_Type == 1) {li_cmd = -1;}
    //---- �������� ���������� ��������� �� �����������
    fSet_ValuesSTOPs();
    //---- ���������� �������� SL
    ls_Name = StringConcatenate (fi_Ticket, "_#VirtSL");
    bd_curSL = fGet_ValueFromGV (ls_Name, OrderStopLoss(), fb_USE_VirtualSTOPs);
    //---- ���������� �������� TP
    ls_Name = StringConcatenate (fi_Ticket, "_#VirtTP");
    bd_curTP = fGet_ValueFromGV (ls_Name, OrderTakeProfit(), fb_USE_VirtualSTOPs);
    //---- ��������� �����
    switch (fi_NSTOPs)
    {
        case 0: // ������������ �����
            if (StopLoss > 0)
            {
                if (bd_curSL > 0.0) lba_Modify[0] = true;
                lda_STOPs[0] = fd_Price - li_cmd * bd_SL;
            }
            if (TakeProfit > 0)
            {
                if (bd_curTP > 0.0) lba_Modify[1] = true;
                lda_STOPs[1] = fd_Price + li_cmd * (bd_TP + bd_Spread);
            }
            //---- ������ �������������� �� �����
            if (lba_Modify[0] && lba_Modify[1]) {GlobalVariableSet (StringConcatenate (fi_Ticket, "_#STOP"), 1); return (false);}
            break;
        case 1: // ����� �� ����������� �� fi_Period �� S.cnt_Bars �����
        case 2: // ����� �� ATR �� fi_Period
        case 3: // ����� �� ZZP (ZigZag) �� fi_Period
            //---- ����������� ����������� �����
            lda_STOPs[0] = fd_Price - li_cmd * bd_MIN_SL;
            lda_STOPs[1] = fd_Price + li_cmd * (bd_MIN_TP + bd_Spread);
            //---- �������� ����������
            if (fi_NSTOPs == 1) if (!fGet_Extremum (lda_Levels, ls_Symbol, fi_Period, E.cnt_Bars)) return (false);
            if (fi_NSTOPs == 2) if (!fGet_ATR (lda_Levels, ls_Symbol, fi_Period, E.cnt_Bars)) return (false);
            if (fi_NSTOPs == 3) if (!fGet_ZZP (lda_Levels, ls_Symbol, fi_Period)) return (false);
            //---- ������������ ����� � ������ ������� FIBO
            fCreat_LevelsByFIBO (lda_Levels, lda_STOPs, bia_LevelFIBO);
            break;
    }
    //---- ��������� ���������� �� SL �� ������� ����
    bd_Trail = MathAbs (fd_Price - lda_STOPs[0]);
    bd_Price = fd_Price;
    ArrayInitialize (lba_Modify, 0);
    //---- ��������� ����� ������ ����������
    if (bd_curSL == 0.0 || (USE_Dinamic_SL && li_cmd * (OrderOpenPrice() - bd_curSL) > 0.0))
    {
        bd_NewSL = NDD (lda_STOPs[0]);
        //---- ���� SL ��������� ��������� ���� ����
        if (NDD (bd_NewSL - bd_curSL) != 0.0) lba_Modify[0] = true;
    }
    else bd_NewSL = bd_curSL;
    if (bd_curTP == 0.0 || USE_Dinamic_TP)
    {
        bd_NewTP = NDD (lda_STOPs[1]);
        //---- ���� TP ��������� ��������� ���� ����
        if (NDD (bd_NewTP - bd_curTP) != 0.0) lba_Modify[1] = true;
    }
    else bd_NewTP = bd_curTP;
    //---- ���� ������ �������������� �� ����� - �������
    if (!lba_Modify[0] && !lba_Modify[1]) return (false);
    int li_result = 1;
    //---- ���������� ����������� �������������� (������������) �����������
    if (USE_Dinamic_SL || USE_Dinamic_TP) li_result = 0;
    //---- ������� ����� � ������������ ��� ���
    if (!fb_USE_VirtualSTOPs)
    {
        //---- ������������ �����
        int li_modify = fOrderModify (fi_Ticket, OrderOpenPrice(), bd_NewSL, bd_NewTP);
        if (li_modify >= 0) fSet_Comment (bdt_curTime, fi_Ticket, 20, "fCreat_STOPs()", li_modify != 0, fb_USE_VirtualSTOPs);
        //---- �������� "�����������" ��������� �����������
        if (li_modify == 1) {GlobalVariableSet (StringConcatenate (fi_Ticket, "_#STOP"), li_result);}
        return (li_modify == 1);
    }
    else
    {
        //---- ���� SL ��������� ������ ��������� � GV-����������
        if (lba_Modify[0]) {GlobalVariableSet (StringConcatenate (fi_Ticket, "_#VirtSL"), bd_NewSL);}
        //---- ���� TP ��������� ������ ��������� � GV-����������
        if (lba_Modify[1]) {GlobalVariableSet (StringConcatenate (fi_Ticket, "_#VirtTP"), bd_NewTP);}
        if (!bb_VirtualTrade)
        {
            fSet_Comment (bdt_curTime, fi_Ticket, 20, "b-STOPs[Virt]", true, fb_USE_VirtualSTOPs);
            if (bb_CreatVStopsInChart)
            {
                if (lba_Modify[0]) fDraw_VirtSTOP (li_cmd, 0, bd_NewSL);
                if (lba_Modify[1]) fDraw_VirtSTOP (li_cmd, 1, bd_NewTP);
            }
        }
        //---- �������� "�����������" ��������� �����������
        GlobalVariableSet (StringConcatenate (fi_Ticket, "_#STOP"), li_result);
    }
    //---- ������������ ��������� ������
    fGet_LastErrorInArray (bsa_Comment, "fCreat_STOPs()", bi_indERR);
//----
    return (false);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//         ����������� ����� � ������ ������� FIBO (�������� � ����������)            |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fCreat_LevelsByFIBO (double ar_Extrem[],       // ������ �����������
                          double& ar_STOPs[],       // ������������ ������ ������
                          int ar_LevelFIBO[])       // ������ ������� ������� (0 - ��� SL; 1 - ��� TP)
{
    int    li_CMD, li_Type = OrderType(), li_Level;
    double ld_Delta = (ar_Extrem[0] - ar_Extrem[1]);
//----
    for (int li_IND = 0; li_IND < 2; li_IND++)
    {
        if (li_Type == 0) {if (li_IND == 0) li_CMD = 1; else li_CMD = 0;} else {li_CMD = li_IND;}
        li_Level = MathAbs (ar_LevelFIBO[li_IND]);
        int li_cmd = 1;
        if (ar_LevelFIBO[li_IND] < 0) {if (li_CMD == 0) li_cmd = -1;} else {if (li_CMD == 1) li_cmd = -1;}
        if (li_Type == li_IND)
        {ar_STOPs[li_IND] = MathMin (ar_STOPs[li_IND], ar_Extrem[li_CMD] + li_cmd * ld_Delta * bda_FIBO[li_Level]);}
        else {ar_STOPs[li_IND] = MathMax (ar_STOPs[li_IND], ar_Extrem[li_CMD] + li_cmd * ld_Delta * bda_FIBO[li_Level]);}
        //if (li_IND == 0) Print (fGet_NameOP (li_Type), ": SL[", ar_LevelFIBO[li_IND], "] = ", DSD (ar_STOPs[li_IND]), " | HIGH = ", DSD (ar_Extrem[li_IND]));
        //if (li_IND == 1) Print (fGet_NameOP (li_Type), ": TP[", ar_LevelFIBO[li_IND], "] = ", DSD (ar_STOPs[li_IND]), " | LOW = ", DSD (ar_Extrem[li_IND]));
    }
//----
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//         ��������� ���������� � ���������� ������� ���������.                       |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fCheck_STOPsParameters()
{
    int err = GetLastError();
//----
    if (N_STOPs == 0)
    {
        if (StopLoss < 0) {Print ("��������� StopLoss >= 0 !!!"); return (false);}
        if (TakeProfit < 0) {Print ("��������� TakeProfit >= 0 !!!"); return (false);}
    }
    else
    {
        if (LevelFIBO_SL < -2 || LevelFIBO_SL > 5) {Print ("��������� -2 <= LevelFIBO_SL <= 5 !!!"); return (false);}
        if (LevelFIBO_TP < -2 || LevelFIBO_TP > 5) {Print ("��������� -2 <= LevelFIBO_TP <= 5 !!!"); return (false);}
    }
    //---- ���������� �������� �� ������������ ������� TF
    if (TF_STOPs == 0) TF_STOPs = Period();
    if (fGet_NumPeriods (TF_STOPs) < 0) {return (false);}
    //---- ������������ ��������� ������
	 fGet_LastErrorInArray (bsa_Comment, "fCheck_STOPsParameters()", bi_indERR);
//----
     return (true);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//+===================================================================================+
//|***********************************************************************************|
//| ������: ����� �������                                                             |
//|***********************************************************************************|
//+===================================================================================+
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|         �������� ������� ���������� � ������������ � ������������                 |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fSet_ValuesSTOPs()
{
    static string ls_Symbol = "";
//----
	 if (ls_Symbol != OrderSymbol())
	 {
		  ls_Symbol = OrderSymbol();
		  bd_SL = NDP (StopLoss);
		  bd_TP = NDP (TakeProfit);
		  bd_MIN_SL = NDP (MIN_StopLoss);
		  bd_MIN_TP = NDP (MIN_TakeProfit);
	 }
    bd_Spread = NDP (MarketInfo (bs_Symbol, MODE_SPREAD));
//----
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        ��������� ����������� ���������                                            |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fCheck_DecimalSTOPs()
{
//----
    if (StopLoss > 0) StopLoss *= bi_Decimal;
    if (TakeProfit > 0) TakeProfit *= bi_Decimal;
    if (MIN_StopLoss > 0) MIN_StopLoss *= bi_Decimal;
    if (MIN_TakeProfit > 0) MIN_TakeProfit *= bi_Decimal;
//----
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+

