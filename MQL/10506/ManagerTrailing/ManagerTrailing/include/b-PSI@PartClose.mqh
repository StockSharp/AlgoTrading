//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|                                                               b-PSI@PartClose.mqh |
//|                                       Copyright � 2012, Igor Stepovoi aka TarasBY |
//|                                                                taras_bulba@tut.by |
//| 09.04.2012  ���������� ���������� �������� �������.                               |
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
//IIIIIIIIIIIIIIIIIII==================CONSTANS=================IIIIIIIIIIIIIIIIIIIIII+
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|                 *****        ��������� ����������         *****                   |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
extern string Setup_PartClose       = "================== PartClose =================="; 
extern bool   PartClose_ON             = TRUE;          // ��������� ������� ���������� �������� ��� �������
extern string PartClose_Levels         = "20/50/30";    // ������ �������� ��� % ����������� ���� �� TP.
// ��� ���������� � ������ TP. ��������, ��� ���������� 10/20/30 ������ �������� ����������� ��� ����������� 
// ����� 10 �������, ������ - ��� ����������� 20 ������� � ��������� ��� ���������� 50 �������.
// ���� � ������ ���� TP, �� ��� ������ ����������� �� ����� (����� ������ ������ ���������� 100 %)
extern int    MoveBUInPart             = 1;             // �� ����� ����� �������� ������� SL � ��������� (0 - �� �������)
extern string PartClose_Percents       = "50/25/25";    // ������� �������� (����� ����������� "/") ��� ���������������� ������. ����� ������ ���� �� ���� ������� ������. ���� �������� ����� ������ � ����� 1.0 ���, ����������� 50% - 0.5, ����� 25% �� 1.0 - 0.3 � ������� 0.2
//IIIIIIIIIIIIIIIIIII=========����������� ������� �������=======IIIIIIIIIIIIIIIIIIIIII+
//IIIIIIIIIIIIIIIIIII========���������� ���������� ������=======IIIIIIIIIIIIIIIIIIIIII+
int           bi_cntPartClose;
//IIIIIIIIIIIIIIIIIII===========�������� ������� ������=========IIIIIIIIIIIIIIIIIIIIII+
//void fInit_PartClose()             // ������������� ������
//int fControl_PartClose (string fs_SymbolList,    // ���� ����������� �������� ���
                        //string fs_MagicList,     // ���� ����������� �������
                        //bool fb_Conditions,      // ������� ��������� (������� �������)
                        //int fi_Slippage,         // ���������������
                        //bool fb_VirtualWork = false,// ���� ����������� ������
                        //string fs_Delimiter = ",")// ����������� ���������� � ����� fs_MagicList
                                     // ������� ���������� ��������� �������� �������
//int fPartClose (int fi_Ticket,                   // OrderTicket()
                //int fi_Slippage,                 // ���������������
                //bool fb_VirtualWork = false)     // ���� ����������� ������
                                     // ���������� ��������� �������� ������ �� Ticket
                                     // ������� ��������� ���������� �������� �����
//int fGet_ParentTicket (string fs_Comment)        // OrderComment()
                                     // �������� Ticket ������������� ������
//bool fCheck_PartCloseParameters()  // ��������� ���������� � ���������� ������� ���������
//void fGet_CurLevels (int ar_PercentClose[],      // ������ � ���������� �������� ����
                     //int fi_SizeTP,              // ������ �������� TP � ��.
                     //int& ar_Levels[],           // ������������ ������ ������� ��������
                     //bool fb_Condition = true)   // ������� �� "����������" ������
                                     // �������� ������������ ������ ���������� �������� ������
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        ������������� ������                                                       |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fInit_PartClose()
{
    string tmpArr[], lsa_txt[2];
    int    err = GetLastError();
//----
    //---- ��������� ������ �� ����� � ������� �������
    if (PartClose_ON)
    {
        int lia_size[3];
        lia_size[1] = fSplitStrToStr (PartClose_Levels, tmpArr, "/");
        fCreat_StrToInt (tmpArr, bia_PartClose_Levels, lia_size[1], 1);
        lsa_txt[0] = "��������� ������ �� ������ � ";
        lsa_txt[1] = "��� ���������� ������� ������� � ";
        lia_size[2] = fSplitStrToStr (PartClose_Percents, tmpArr, "/");
        //---- ��������� ����������� ���������� ��������
        if (lia_size[1] != lia_size[2])
        {
            lia_size[0] = MathMin (lia_size[1], lia_size[2]);
            if (lia_size[0] != lia_size[1]) ArrayResize (bia_PartClose_Levels, lia_size[0]);
            if (lia_size[0] != lia_size[2]) ArrayResize (bia_PartClose_Percents, lia_size[0]);
            Print ("������� �������� �� ������� - ��������� �� ", lia_size[0], " !!!");
        }
        else lia_size[0] = lia_size[1];
        fCreat_StrToInt (tmpArr, bia_PartClose_Percents, lia_size[0], 1);
        for (int li_int = 0; li_int < lia_size[0]; li_int++)
        {
            if (li_int > 0) {bia_PartClose_Levels[li_int] += bia_PartClose_Levels[li_int-1];}
            lsa_txt[0] = StringConcatenate (lsa_txt[0], bia_PartClose_Percents[li_int], IIFs ((li_int == lia_size[0] - 1), " ", ", "));
            lsa_txt[1] = StringConcatenate (lsa_txt[1], bia_PartClose_Levels[li_int], IIFs ((li_int == lia_size[0] - 1), " ��.", ", "));
        }
        lsa_txt[0] = StringConcatenate (lsa_txt[0], " ��������� �� ���� ", lsa_txt[1]);
        Print (lsa_txt[0]);
        //---- ���������� �������� ������������ � ���������� ��������
        if (!fCheck_PartCloseParameters())
        {
            Alert ("��������� ��������� PartClose !!!");
            bb_OptimContinue = true;
            return (false);
        }
        //---- ��������� � ������� ������ �������� ��������� GV-���������
        string lsa_Array[] = {"_#Num","_#Lots","_#BU","_#OP"};
        fCreat_ArrayGV (bsa_prefGV, lsa_Array);
        bb_ClearGV = true;
        bi_cntPartClose = lia_size[0];
    }
    //---- ������������ ��������� ������
    fGet_LastErrorInArray (bsa_Comment, "fInit_PartClose()", bi_indERR);
//----
    return (true);
    fControl_PartClose (Symbol(), 0, true, 2);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        ���������� ��������� �������� �������                                      |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fControl_PartClose (string fs_SymbolList,        // ���� ����������� �������� ���
                         string fs_MagicList,         // ���� ����������� �������
                         bool fb_Conditions,          // ������� ��������� (������� �������)
                         int fi_Slippage,             // ���������������
                         bool fb_VirtualWork = false, // ���� ����������� ������
                         string fs_Delimiter = ",")   // ����������� ���������� � ����� fs_MagicList
{
    if (!PartClose_ON) return (false);
    if (!fb_Conditions) return (false);
    int    li_Type, err = GetLastError();
    double ld_Profit;
    string ls_Symbol, ls_fName = "fControl_PartClose()";
    bool lb_result = false;
//----
    bs_libNAME = "b-PartClose";
    for (int i = OrdersTotal() - 1; i >= 0; i--)
    {
        if (!OrderSelect (i, SELECT_BY_POS, MODE_TRADES)) continue;
        ls_Symbol = OrderSymbol();
        if (StringFind (fs_SymbolList, ls_Symbol) < 0 && StringLen (fs_SymbolList) > 0) continue;
        if (!fCheck_MyMagic (fs_MagicList, fs_Delimiter)) continue;
        li_Type = OrderType();
        if (li_Type > 1) continue;
        ld_Profit = OrderProfit() + OrderCommission() + OrderSwap();
        //---- ������������ ��������� �������� ���������� �������
        if (ld_Profit > 0.0)
        {
            fGet_MarketInfo (ls_Symbol);
            if (fPartClose (OrderTicket(), fi_Slippage, fb_VirtualWork) >= 0) lb_result = true;
        }
    }
    //---- ������������ ��������� ������
    fGet_LastErrorInArray (bsa_Comment, ls_fName, bi_indERR);
//----
    return (lb_result);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        ���������� ��������� �������� ������ �� Ticket                             |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int fPartClose (int fi_Ticket,                // OrderTicket()
                int fi_Slippage,              // ���������������
                bool fb_VirtualWork = false)  // ���� ����������� ������
{
    int    li_ParentTicket = 0, li_cmd, li_Num = 0, li_Type = OrderType(),
           err = GetLastError(), lia_PartClose_Levels[];
    double ld_Lots = OrderLots(), ld_LotsClose, ld_Price, ld_BU, ld_OpenPrice = OrderOpenPrice();
    bool   lb_result = false, lb_close = false, lb_modify = false, lb_BU = false;
    string ls_close, ls_Profit, ls_Name, ls_Comment = OrderComment(), ls_Symbol = OrderSymbol();
//----
    li_ParentTicket = fGet_ParentTicket (ls_Comment);
    if (li_ParentTicket != 0)
    {
        ls_Name = StringConcatenate (li_ParentTicket, "_#Num");
        if (GlobalVariableCheck (ls_Name)) {li_Num = GlobalVariableGet (ls_Name) + 1;}
        ls_Name = StringConcatenate (li_ParentTicket, "_#Lots");
        if (GlobalVariableCheck (ls_Name)) {ld_Lots = GlobalVariableGet (ls_Name);}
        ls_Name = StringConcatenate (li_ParentTicket, "_#OP");
        if (GlobalVariableCheck (ls_Name)) {ld_OpenPrice = GlobalVariableGet (ls_Name);}
        ls_Name = StringConcatenate (li_ParentTicket, "_#BU");
        lb_BU = GlobalVariableCheck (ls_Name);
    }
    else {lb_BU = false;}
    if (li_Num >= bi_cntPartClose) {li_Num = bi_cntPartClose - 1;}
    GlobalVariableSet (StringConcatenate (fi_Ticket, "_#Num"), li_Num);
    GlobalVariableSet (StringConcatenate (fi_Ticket, "_#Lots"), ld_Lots);
    GlobalVariableSet (StringConcatenate (fi_Ticket, "_#OP"), ld_OpenPrice);
    bd_curTP = fGet_ValueFromGV (StringConcatenate (fi_Ticket, "_#VirtTP"), OrderTakeProfit(), fb_VirtualWork);
    //---- ��������� ������ ��������
    int li_SizeTP = NDPD (MathAbs (bd_curTP - ld_OpenPrice));
    fGet_CurLevels (bia_PartClose_Levels, li_SizeTP, lia_PartClose_Levels, bd_curTP > 0.0);
    RefreshRates();
    ld_Price = NDD (fGet_TradePrice (li_Type, bb_RealTrade, ls_Symbol));
    if (li_Type == OP_BUY) li_cmd = 1; else li_cmd = -1;
    //---- ��������� ������� �� �������� ��������� ����� ������
    if (li_cmd * (ld_Price - OrderOpenPrice()) >= NDP (lia_PartClose_Levels[li_Num]))
    {
        ld_LotsClose = fLotsNormalize ((bia_PartClose_Percents[li_Num] * ld_Lots / 100.0));
        if (ld_LotsClose > 0.0)
        {
            ld_LotsClose = MathMin (ld_LotsClose, OrderLots());
            //---- ���� ��������� ����� - ����������� ���������
            if (li_Num + 1 == ArraySize (bia_PartClose_Percents)) ld_LotsClose = OrderLots();
            lb_close = true;
        }
    }
    //---- ��������� ����� ����������� ������
    if (lb_close)
    {
        //---- ����� MooveBUInPart ����� �������� ��������� ���������� ����� � ��
        if (MoveBUInPart > 0)
        {
            if (!lb_BU && li_Num + 1 >= MoveBUInPart)
            {
                ld_BU = NDD (OrderOpenPrice() + li_cmd * (bd_ProfitMIN + bd_Spread));
                //---- ���� ���� ���� ��������
                if (NDD (OrderStopLoss() - ld_BU) != 0.0)
                {
                    int li_result = fOrderModify (fi_Ticket, OrderOpenPrice(), ld_BU, OrderTakeProfit(), 0, Gold);
                    if (li_result == 1) {GlobalVariableSet (StringConcatenate (fi_Ticket, "_#BU"), ld_BU);}
                    if (li_result >= 0) fSet_Comment (bdt_curTime, fi_Ticket, 51, "", li_result != 0);
                    if (li_result == 0) return (-1);
                }
            }
        }
        lb_result = fOrderClose (fi_Ticket, ld_LotsClose, ld_Price, fi_Slippage, White);
        fSet_Comment (li_Num, fi_Ticket, 52, "", lb_result, li_ParentTicket, lia_PartClose_Levels[li_Num]);
        if (!lb_result) return (li_Num); else return (-1);
    }
    //---- ������������ ��������� ������
    fGet_LastErrorInArray (bsa_Comment, "fPartClose()", bi_indERR);
//----
    return (-1);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        �������� Ticket ������������� ������                                       |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int fGet_ParentTicket (string fs_Comment)
{
    int li_N1 = StringFind (fs_Comment, "from");
//----
    if (li_N1 >= 0)
    {
        int li_N2 = StringFind (fs_Comment, "#");
        if (li_N2 > li_N1)
        {return (StrToInteger (StringSubstr (fs_Comment, li_N2 + 1, StringLen (fs_Comment) - li_N2 - 1)));}
    }
//----
    return (0);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//         ��������� ���������� � ���������� ������� ���������.                       |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fCheck_PartCloseParameters()
{
    int err = GetLastError(), li_Percent = 0;
//----
    for (int li_int = 0; li_int < ArraySize (bia_PartClose_Percents); li_int++)
    {li_Percent += bia_PartClose_Percents[li_int];}
    if (li_Percent != 100) {Print ("����� ��������� ���������� �������� (PartClose_Percents) ������ == 100 %"); return (false);}
    //---- ������������ ��������� ������
	 fGet_LastErrorInArray (bsa_Comment, "fCheck_PartCloseParameters()", bi_indERR);
//----
     return (true);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        �������� ������������ ������ ���������� �������� ������                    |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fGet_CurLevels (int ar_PercentClose[],    // ������ � ���������� �������� ����
                     int fi_SizeTP,            // ������ �������� TP � ��.
                     int& ar_Levels[],         // ������������ ������ ������� ��������
                     bool fb_Condition = true) // ������� �� "����������" ������
{
//----
    ArrayResize (ar_Levels, bi_cntPartClose);
    for (int li_IND = 0; li_IND < bi_cntPartClose; li_IND++)
    {
        if (fb_Condition) ar_Levels[li_IND] = fi_SizeTP * ar_PercentClose[li_IND] / 100.0;
        else ar_Levels[li_IND] = ar_PercentClose[li_IND] * bi_Decimal;
    }
//----
} 
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+

