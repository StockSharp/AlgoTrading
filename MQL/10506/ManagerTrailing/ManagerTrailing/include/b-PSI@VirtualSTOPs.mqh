//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|                                                            b-PSI@VirtualSTOPs.mqh |
//|                                       Copyright � 2012, Igor Stepovoi aka TarasBY |
//|                                                                taras_bulba@tut.by |
//| 11.04.2012  ���������� ����������� ������.                                        |
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
//extern string Setup_VirtualSTOPs    = "================ VirtualSTOPs ================="; 
extern bool   USE_VirtualSTOPs         = True;     // ���������� �� ������������� (��� ������������ ������ ��� �������� ������)
extern int    Virtual_Order_SL         = 0;        // ����������� SL ������� (��� 4-�� ������)
extern int    Virtual_Order_TP         = 0;        // ����������� TP ������� (��� 4-�� ������)
//IIIIIIIIIIIIIIIIIII=========����������� ������� �������=======IIIIIIIIIIIIIIIIIIIIII+
//IIIIIIIIIIIIIIIIIII========���������� ���������� ������=======IIIIIIIIIIIIIIIIIIIIII+
//IIIIIIIIIIIIIIIIIII===========�������� ������� ������=========IIIIIIIIIIIIIIIIIIIIII+
//void fInit_VirtualSTOPs()          // ������������� ������
//int fControl_VirtualSTOPs (int fi_Slippage,      // ���������������
                           //int fi_NBars_Life = 0,// ����������� "�����" ������ � ����� �� fi_Period: 0 - �������� �� �����������
                           //int fi_Period = 0)    // ������
                                     // ���������� ������ ����������� ������ �������
//bool fVirtualSTOPs (int fi_Ticket,               // Ticket
                    //int fi_SL,                   // Virtual SL
                    //int fi_TP,                   // Virtual TP
                    //int fi_Slippage,             // ���������������
                    //bool fb_USE_DinamicTP = false)// ���� ������������� ������������� TP
                                     // ������� ����������� ������ ��� ����������� ������
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        ������������� ������                                                       |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fInit_VirtualSTOPs()
{
//----
    if (Virtual_Order_TP > 0) Virtual_Order_TP *= bi_Decimal;
    if (Virtual_Order_SL > 0) Virtual_Order_SL *= bi_Decimal;
    if (Virtual_Order_SL != 0 || Virtual_Order_TP != 0 || USE_VirtualSTOPs)
    {
        string lsa_Array[] = {"_#VirtSL","_#VirtTP"};
        bb_ClearGV = true;
        //---- ��������� � ������� ������ �������� ��������� GV-���������
        fCreat_ArrayGV (bsa_prefGV, lsa_Array);
    }
//----
    return;
    fControl_VirtualSTOPs (2);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        ���������� ������ ����������� ������ �������                               |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int fControl_VirtualSTOPs (int fi_Slippage,             // ���������������
                           int fi_NBars_Life = 0,       // ����������� "�����" ������ � ����� �� fi_Period: 0 - �������� �� �����������
                           int fi_Period = 0)           // ������
{
    //---- ���� ����������� �������� �� ������������
    if (Virtual_Order_SL == 0 && Virtual_Order_TP == 0 && !USE_VirtualSTOPs) return (0);
//----
    int    err = GetLastError();
    string ls_Symbol;
//----
    for (int i = OrdersTotal() - 1; i >= 0; i--)
    {
        if (!OrderSelect (i, SELECT_BY_POS, MODE_TRADES)) continue;
        ls_Symbol = OrderSymbol();
        if (StringFind (bs_SymbolList, ls_Symbol) < 0 && StringLen (bs_SymbolList) > 0) continue;
        if (!fCheck_MyMagic (bs_MagicList, bs_Delimiter)) continue;
        if (OrderType() > 1) continue;
        //---- ������������ "�����" ������
        if (fi_NBars_Life > 0)
        {if (fi_NBars_Life >= iBarShift (Symbol(), fi_Period, OrderOpenTime())) continue;}
        //---- ������������ ������ ����������� ������
        fGet_MarketInfo (ls_Symbol);
        fVirtualSTOPs (OrderTicket(), Virtual_Order_SL, Virtual_Order_TP, fi_Slippage);
    }
    //---- ������������ ��������� ������
    fGet_LastErrorInArray (bsa_Comment, "fControl_VirtualSTOPs()", bi_indERR);
//----
    return (0);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        ������� ����������� ������ ��� ����������� ������                          |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fVirtualSTOPs (int fi_Ticket,                 // Ticket
                    int fi_SL,                     // Virtual SL
                    int fi_TP,                     // Virtual TP
                    int fi_Slippage,               // ���������������
                    bool fb_USE_DinamicTP = false) // ���� ������������� ������������� TP
{
    int    cmd = 1, li_Type = -1, li_result = 0;
    bool   lb_SL = false, lb_TP = false;
    double ld_SL = 0.0, ld_TP = 0.0, ld_Price, ld_OpenPrice, ld_Pribul;
    string ls_fName = "fVirtualSTOPs()", ls_Name; 
//----
    bs_libNAME = "b-VirtualSTOPs";
    li_Type = OrderType();
    //ld_Price = fGet_TradePrice (li_Type, bb_RealTrade, OrderSymbol());
    ld_Price = bda_Price[li_Type];
    ld_OpenPrice = OrderOpenPrice();
    if (li_Type == 1) {cmd = -1;}
    //---- ��������� �������������� ����������� �����
    ls_Name = StringConcatenate (fi_Ticket, "_#VirtSL");
    if (GlobalVariableCheck (ls_Name)) ld_SL = GlobalVariableGet (ls_Name);
    else {if (fi_SL != 0) ld_SL = ld_OpenPrice - cmd * NDP (fi_SL);}
    ls_Name = StringConcatenate (fi_Ticket, "_#VirtTP");
    if (GlobalVariableCheck (ls_Name)) ld_TP = GlobalVariableGet (ls_Name);
    else {if (fi_TP != 0) ld_TP = ld_OpenPrice + cmd * NDP (fi_TP);}
    if (ld_SL > 0) {if (li_Type == 0) lb_SL = (ld_Price <= ld_SL); else lb_SL = (ld_Price >= ld_SL);}
    if (ld_TP > 0) {if (li_Type == 0) lb_TP = (ld_Price >= ld_TP); else lb_TP = (ld_Price <= ld_TP);}
    //---- ���� �������� ����������� ����, ��������� �����
    if (lb_SL || lb_TP)
    {
        //---- ���� ������� ����� ������� � ������ ������������� - ��������� �����
        if (lb_TP && bi_MyOrders > 1)
        {
            if (bd_ProfitCUR > fGet_PipsValue() * ProfitMIN_Pips)
            {
                li_result = fClose_AllOrders (bs_SymbolList, bs_MagicList, ld_Pribul);
                fSet_Comment (bdt_curTime, 0, 53, ls_fName, li_result > 0, li_result, ld_Pribul);
                if (li_result > 0)
                {
                    bi_MyOrders = 0;
                    return (true);
                }
            }
        }
        //---- ������������ ��� ������� �� ������
        if (!fCheck_MinProfit (bd_ProfitMIN, ld_Price, lb_TP && !fb_USE_DinamicTP)) return (false);
        if (fOrderClose (fi_Ticket, OrderLots(), ld_Price, fi_Slippage))
        {fSet_Comment (fi_Ticket, fi_Ticket, 21, ls_fName, True, lb_SL, ld_SL, ld_TP); return (true);}
        else fSet_Comment (fi_Ticket, fi_Ticket, 21, ls_fName, False, lb_SL, ld_SL, ld_TP);
    }
    //---- ������������ ��������� ������
    fGet_LastErrorInArray (bsa_Comment, ls_fName, bi_indERR);
//----
    return (false);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+

