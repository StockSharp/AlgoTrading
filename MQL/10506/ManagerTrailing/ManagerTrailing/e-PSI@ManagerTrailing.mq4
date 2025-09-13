//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|                                                         e-PSI@ManagerTrailing.mq4 |
//|                                    Copyright � 2010-12, Igor Stepovoi aka TarasBY |
//|                                                                taras_bulba@tut.by |
//|                                                                                   |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|   ������ ������� ������������ ��� �������������� �������������. ���������� �����- |
//|���� ������ ��� �������� ����� ������ (TarasBY). �������������� ��������� ���� ��- |
//|������� ������ ��� ������� ���������� ������� ������, ������ � ����� ������.  ���- |
//|���� ��������� ��� ��������� ��� ������ ���������.                                 |
//|   ����� �� ����� ��������������� �� ��������� ������, ���������� � ���������� ��- |
//|����������� ���������.                                                             |
//|   �� ���� ��������, ��������� �  ������� ���������, ����������� ��� ������������� |
//|�� ��� ��������� ���������� �� Skype: TarasBY ��� e-mail.                          |
//+-----------------------------------------------------------------------------------+
//|   This product is intended for non-commercial use.  The publication is only allo- |
//|wed when you specify the name of the author (TarasBY). Edit the source code is va- |
//|lid only under condition of preservation of the text, links and author's name.     |
//|   Selling a expert or(and) parts of it PROHIBITED.                                |
//|   The author is not liable for any damages resulting from the use of a expert.    |
//|   For all matters relating to the work of the expert, comments or suggestions for |
//|their improvement in the contact Skype: TarasBY or e-mail.                         |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
#property copyright "Copyright � 2008-12, TarasBY WM R418875277808; Z670270286972"
#property link      "taras_bulba@tut.by"
//IIIIIIIIIIIIIIIIIII==========������������ ����������==========IIIIIIIIIIIIIIIIIIIIII+
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|                  *****         ��������� ���������         *****                  |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
extern string   SETUP_Expert        = "========== ����� ��������� ��������� ==========";
extern int      MG                     = 880;           // �����: > 0 - �����, 0 - �������� �������, > 0 - �������� ����������
extern datetime TimeControl            = D'2010.01.01 00:00'; // � ������ ������� ������������ ����� ������ �� �����
extern int      NewBarInPeriod         = -1;            // <= 0 - �������� �� ������ ������� ������ ����, -1 - �������� �� ������ ����
#include        <b-PSI@Base.mqh>                        // ���������� ������� �������
extern bool     OnlyCurrentSymbol      = TRUE;          // �������� ������ � ������� �������� ��� � List_Symbols
extern string   List_Symbols           = "EURUSD,GBPUSD,AUDUSD,NZDUSD,EURGBP"; // �������������� �������� ����
extern int      NumberAccount          = 0;             // ����� ��������� �����. �������� ������ �� ��������� �����. ��� �������� <=0 - ����� ����� �� ����������� 
#include        <b-PSI@STOPs.mqh>                       // ���������� �������� � �������� ������
//#include        <b-PSI@VirtualSTOPs.mqh>                // ���������� ����������� ������
extern int      NBars_LifeMIN            = 0;           // ����������� "�����" ������ � NBars_LifeMIN ����� �� ������� �������
extern int      NBars_LifeMAX            = -1;          // ������������ "�����" ������ � NBars_LifeMAX ����� �� ������� ������� (0 - ������ ����� �� ����� ������� �����)
#include        <b-PSI@TrailSymbol.mqh>                 // ���������� ���������
#include        <b-PSI@PartClose.mqh>                   // ���������� ���������� �������� �������
#include        <b-PSI@ManagerPA.mqh>                   // ���������� �� ���������� ����� �������� ���������
extern string   Setup_Services      = "=================== SERVICES ==================";
extern bool     ShowCommentInChart     = TRUE;          // ���������� �����������. 
extern bool     PrintCom               = TRUE;          // �������� �����������.
extern bool     SoundAlert             = FALSE;         // ����
extern bool     CreatVStopsInChart     = FALSE;         // �������� �� ����� ����������� ����� (���� �������������)
extern bool     ClearALLAfterTrade     = FALSE;         // ������� ������� � GV-���������� ����� ����
#include        <b-PSI@Trade_Light.mqh>                 // ���������� �������� ��������
extern string   Setup_Tester        = "=================== Tester ====================";
extern int      InTesterOrder          = 1;             // �������� ������ � ������� ��� ��������. 1 - ���, -1 - ���� 
//IIIIIIIIIIIIIIIIIII======���������� ���������� ���������======IIIIIIIIIIIIIIIIIIIIII+
string          gs_Base, gs_Info, ExpertName;
int             gi_SL = 200, gia_HistoryOrders[3], gia_MyOrders[2];
bool            flag_BadAccount = false, gb_InfoPrint = false, gb_Pause = false;
//IIIIIIIIIIIIIIIIIII==========������������ ����������==========IIIIIIIIIIIIIIIIIIIIII+
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|                  Custom expert initialization function                            |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int init()
{
    int err = GetLastError();
//----
    //---- ��������� ��������� ������ ���������
    if (!fCheck_MyAccount()) return (0);
    bs_NameGV = "ManagerTrailing";
    if (NewBarInPeriod == 0) NewBarInPeriod = Period();
    //---- �������������� ���������� ���������� ������� �������
    fInit_Base (List_Symbols, MG, ShowCommentInChart, PrintCom, SoundAlert, CreatVStopsInChart);
    //---- �������������� ���������� ���������� ���������� ����� ��������
    if (!fInit_ManagerPA()) return (0);
    //---- �������������� ���������� ������
    if (!fInit_STOPs()) return (0);
    //---- �������������� ���������� ���������� ��������
    if (!fInit_PartClose()) return (0);
    //---- �������������� ���������� ����������� ������
    //fInit_VirtualSTOPs();
    //---- �������������� ���������� ���������
    if (!fInit_Trail()) return (0);
    //---- �������� �������������� ����������
    fGet_Statistic (-1);
    //---- ��������� ����������� ���������
    gi_SL *= bi_Decimal;
    if (PrintCom || ShowCommentInChart) {gb_InfoPrint = true;}
    if (OnlyCurrentSymbol || !bb_RealTrade) {bs_SymbolList = Symbol();}
    ExpertName = StringConcatenate (WindowExpertName(), "[", IIFs (MG >= 0, MG, "ALL"), "]:  ", fGet_NameTF (Period()), "_", Symbol());
    //---- �������� ���� ������ � ���������� ���������
    bdt_BeginTrade = fGet_TermsTrade (bs_SymbolList, MG, bdt_LastTrade);
    //---- ������� ��������
    fPrepareComments();
    //---- �������� ������� �������� ���������� �����
    bdt_NewBarInPeriod = iTime (Symbol(), NewBarInPeriod, 1);
    fRight_CompilMT();
    //---- ��������� GV-����������
    if (!bb_RealTrade)
    {
        GlobalVariablesDeleteAll (bs_NameGV);
        TimeControl = D'1990.01.01 00:00';
    }
    if (!bb_VirtualTrade) {if (bb_OptimContinue) Alert ("��������� ���! ��������� �� � ������� !!!");}
    //---- ������������ ��������� ������
	 fGet_LastErrorInArray (bsa_Comment, "init()", bi_indERR);
//----
    return (0);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|                  Custor expert deinitialization function                          |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int deinit()
{
//----
        //---- ������� ����� ���� GV-����������
    if (!bb_RealTrade) {GlobalVariablesDeleteAll (bs_NameGV);}
    //---- ��������� ����� ���� ������
    if (ClearALLAfterTrade)
    {
        Comment ("");
        //---- ������� ����������� ������� � �����
        fObjectsDeleteAll (bs_NameGV, -1, -1); 
        //---- ������� GV-����������
        GlobalVariablesDeleteAll (bs_NameGV);
    }
    else
    {
        //---- ������������ ����� ������
        fGet_Statistic (-1);
        //---- ������ �� ������� ��������
        if (ShowCommentInChart) {fCommentInChart (bsa_Comment, fGet_StringManagerPA());}
    }
//----
    return (0);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|               Custom expert iteration function                                    |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int start()
{
    if (flag_BadAccount) {return (0);}
    //---- ���� ���� �������� ���������
    if (fSTOPTRADE()) return (0);
//----
    int err = GetLastError();
//----
    //---- ��������� � �������� ������ ������
    fOrderSend_Tester();
    //---- ������� "����" ������
    bi_MyOrders = fMyPositions (bs_SymbolList, bd_ProfitCUR, MG);
    //---- �������� ����������
    fGet_Statistic (PERIOD_D1);
    //---- ������� ���������� �� ������ (���� ���������)
    if (ShowCommentInChart) {fCommentInChart (bsa_Comment, fGet_StringManagerPA());}
    //---- ������ � ������ ���������� ���� (���� NewBarInPeriod >= 0)
    if (!fCheck_NewBarInPeriod (NewBarInPeriod)) return (0);
    //---- ��������� � ������ ���������� ���������� ����� �������� ���������
    if (fManagerPA (bd_ProfitCUR, bi_MyOrders > 0)) bi_MyOrders = 0;
    //---- �������������� ���������� ��������� ��������
    if (!bb_TSProfit) {if (bi_MyOrders > 0) fControl_Positions (bs_SymbolList, MG);}
    //---- ������������ ��������� ������
	 fGet_LastErrorInArray (bsa_Comment, "start()", bi_indERR);
//----
    return (0);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        ��������� ������� "�����" �������� �������                                 |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int fMyPositions (string fs_SymbolList,   // ���� ����������� �������� ���
                  double& fd_Profit,      // ������������ ������ �������� �������
                  int fi_Magic = -1)      // OrderMagicNumber()
{
    int    li_ord = 0, li_total = OrdersTotal();
//----
    fd_Profit = 0.0;
    ArrayInitialize (gia_MyOrders, 0);
    if (li_total == 0) {return (0);}
    for (int li_pos = li_total - 1; li_pos >= 0; li_pos--)
    {
        if (!OrderSelect (li_pos, SELECT_BY_POS, MODE_TRADES)) continue;
        if (StringFind (fs_SymbolList, OrderSymbol()) < 0) {if (StringLen (fs_SymbolList) > 0) continue;}
        if (OrderMagicNumber() != fi_Magic) {if (fi_Magic >= 0) continue;}
        if (OrderType() > 1) continue;
        fd_Profit += (OrderProfit() + OrderSwap() + OrderCommission());
        gia_MyOrders[OrderType()]++;
        li_ord++;
    }
//----
    return (li_ord);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//+===================================================================================+
//|***********************************************************************************|
//| ������: ������ � ��������                                                         |
//|***********************************************************************************|
//+===================================================================================+
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        ��������� "������" ��������� ���������                                     |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fControl_Positions (string fs_SymbolList,        // ���� ����������� �������� ���
                         string fs_MagicList,         // ���� ����������� �������
                         string fs_Delimiter = ",")   // ����������� ���������� � ����� fs_MagicList
{
    int    li_Type, li_result = -1, li_Ticket, err = GetLastError(),
           li_LifeBar;
    double ld_Profit;
    string ls_Symbol;
    bool   lb_NewBar = false;
    static datetime ldt_NewBar = 0;
    datetime ldt_BeginBar = iTime (NULL, TF_STOPs, 0);
//----
    if (ldt_NewBar != ldt_BeginBar) lb_NewBar = true;
    for (int i = OrdersTotal() - 1; i >= 0; i--)
    {
        if (!OrderSelect (i, SELECT_BY_POS, MODE_TRADES)) continue;
        ls_Symbol = OrderSymbol();
        if (StringFind (fs_SymbolList, ls_Symbol) < 0) {if (StringLen (fs_SymbolList) > 0) continue;}
        if (!fCheck_MyMagic (fs_MagicList, fs_Delimiter)) continue;
        li_Type = OrderType();
        if (li_Type > 1) continue;
        li_Ticket = OrderTicket();
        //---- �������� ���������� ������ �� �������
        fGet_MarketInfo (ls_Symbol);
        li_LifeBar = iBarShift (Symbol(), 0, OrderOpenTime());
        //---- ���������� ������ ����������� ������
        if (Virtual_Order_SL != 0 || Virtual_Order_TP != 0)
        {
            //---- ������������ "�����" ������
            if ((NBars_LifeMIN > 0 && NBars_LifeMIN < li_LifeBar) || NBars_LifeMIN == 0)
            {if (fVirtualSTOPs (li_Ticket, Virtual_Order_SL, Virtual_Order_TP, Slippage)) continue;}
        }
        //---- ���������� �������� �� ���� ����� ������
        if (fControl_MAXLife (li_LifeBar, NBars_LifeMAX)) continue;
        //---- ���������� ��������� ������
        if (lb_NewBar || !GlobalVariableCheck (StringConcatenate (li_Ticket, "_#STOP")))
        {if (fCreat_STOPs (N_STOPs, li_Ticket, bda_Price[OrderType()], TF_STOPs, USE_VirtualSTOPs)) continue;}
        //---- ������ ��������� �������
        if (fTrail_Position (li_Ticket) == 0) continue;
        if (!PartClose_ON) continue;
        ld_Profit = OrderProfit() + OrderCommission() + OrderSwap();
        //---- ������������ ��������� �������� �������
        if (ld_Profit > 0.0) {fPartClose (li_Ticket, Slippage, USE_VirtualSTOPs);}
    }
    if (lb_NewBar) ldt_NewBar = ldt_BeginBar;
    //---- ������������ ��������� ������
    fGet_LastErrorInArray (bsa_Comment, "fControl_Positions()", bi_indERR);
//----
    return;
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        ���� ����������� ���������� ��������� ������                               |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fIs_LossLastPos (string fs_Symbol = "",    // Symbol
                      int fi_Type = -1,         // Type
                      int fi_MG = -1)           // Magic
{
    datetime ldt_CloseTime;
    int li_pos = -1, li_total = OrdersHistoryTotal();
//----
    fs_Symbol = IIFs (fs_Symbol == "", Symbol(), fs_Symbol);
    for (int li_ORD = li_total - 1; li_ORD >= 0; li_ORD--)
    {
        if (!OrderSelect (li_ORD, SELECT_BY_POS, MODE_HISTORY)) continue;
        if (OrderSymbol() != fs_Symbol) continue;
        if (fi_MG >= 0 && OrderMagicNumber() != fi_MG) continue;
        if (OrderType() > 2) continue;
        if (fi_Type >= 0 && OrderType() != fi_Type) continue;
        if (ldt_CloseTime >= OrderCloseTime()) continue;
        ldt_CloseTime = OrderCloseTime();
        li_pos = li_ORD;
    }
    if (OrderSelect (li_pos, SELECT_BY_POS, MODE_HISTORY))
    {if (OrderProfit() < 0) return (True);}
//----
    return (False);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//+===================================================================================+
//|***********************************************************************************|
//| ������: ����� �������                                                             |
//|***********************************************************************************|
//+===================================================================================+
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|        "������������" �� ��������������� � ���� �������                           |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fRight_CompilMT()
{
//----
    return;
    fControl_VirtualSTOPs ("", "");
    fControl_PartClose ("", "", true, 0);
//----
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//+===================================================================================+
//|***********************************************************************************|
//| ������: MONEY MANAGEMENT                                                          |
//|***********************************************************************************|
//+===================================================================================+
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        ������� ����� ������ �� ����� �������                                      |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
double fCalculate_Pribul (int& ar_HistoryOrders[3],    // ������������ ������ �������� ������� �� ��� ����� �������
                          string fs_SymbolList,        // ���� ����������� �������� ���
                          string fs_MagicList,         // ���� ����������� �������
                          int fi_OP = -1,              // ��� (BUY\SELL) ����������� �������
                          datetime fdt_TimeBegin = 0,  // ������ �������, � �������� ���������� ������
                          string fs_Delimiter = ",")   // ����������� ���������� � ����� fs_MagicList
{
    double   ld_Pribul = 0.0;
    int      li_Type, err = GetLastError(), history_total = OrdersHistoryTotal(),
             li_ChildTicket, li_Ticket;
    string   ls_Name, ls_NameChild;
    datetime ldt_preLastTrade = bdt_LastTrade - 2 * 10080 * 60;
    static string lsa_NameGV[] = {"_#VirtSL","_#VirtTP","_#BU","_#STOP"};
//----
    ArrayInitialize (ar_HistoryOrders, 0);
    for (int li_ORD = 0; li_ORD < history_total; li_ORD++)
    {
        if (!OrderSelect (li_ORD, SELECT_BY_POS, MODE_HISTORY)) continue;
        if (StringFind (fs_SymbolList, OrderSymbol()) < 0) {if (StringLen (fs_SymbolList) > 0) continue;}
        if (!fCheck_MyMagic (fs_MagicList, fs_Delimiter)) continue;
        li_Type = OrderType();
        if ((fi_OP > -1 && li_Type != fi_OP) || li_Type > 1) continue;
        if (fdt_TimeBegin > OrderCloseTime()) continue;
        ld_Pribul += (OrderProfit() + OrderSwap() + OrderCommission());
        ar_HistoryOrders[2]++;
        ar_HistoryOrders[OrderType()]++;
        if (bdt_LastTrade < OrderCloseTime()) bdt_LastTrade = OrderCloseTime();
        //---- ���������� �������� GV-���������� �� ��������� 2-������
        if (!bb_ClearGV) continue;
        if (ldt_preLastTrade < OrderCloseTime())
        {
            li_Ticket = OrderTicket();
            li_ChildTicket = fGet_�hildTicket (OrderComment());
            //---- ���� ���� �������� �����, ���� ��� ����� ��������
            if (li_ChildTicket > 0)
            {
                if (OrderSelect (li_ChildTicket, SELECT_BY_TICKET))
                {
                    if (OrderCloseTime() == 0)
                    {
                        for (int li_GV = 0; li_GV < 4; li_GV++)
                        {
                            //---- ��������� ������ �� ������������� ������ � ���������
                            ls_Name = StringConcatenate (li_Ticket, lsa_NameGV[li_GV]);
                            if (GlobalVariableCheck (ls_Name))
                            {
                                ls_NameChild = StringConcatenate (li_ChildTicket, lsa_NameGV[li_GV]);
                                if (!GlobalVariableCheck (ls_Name))
                                {GlobalVariableSet (ls_NameChild, GlobalVariableGet (ls_Name));}
                            }
                        }
                        continue;
                    }
                }
            }
            fClear_WasteGV (bsa_prefGV, li_Ticket);
        }
    }
    //---- ������������ ��������� ������
	 fGet_LastErrorInArray (bsa_Comment, "fCalculate_Pribul()", bi_indERR);
//----
    return (ld_Pribul);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        �������� ����������                                                        |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fGet_Statistic (int fi_Period = 0,           // ����������� ������ (TF) �� ������� �������� ����������
                     bool fb_IsTimeGMT = false)   // ����� ����� ���������
{
    static datetime ldt_NewBar = 0;
    static int      li_PreHistOrders = 0;
    int      li_ORD, err = GetLastError(), li_Period =  MathMax (0, fi_Period);
    string   ls_Symbol = Symbol();
    datetime ldt_NewPeriod = iTime (ls_Symbol, li_Period, 0);
//----
    //---- ��������� ����� � "��������� ������"
    bdt_curTime = TimeCurrent();
    //bdt_curTime = fGet_Time (fb_IsTimeGMT, bi_ShiftLocalDC_sec);
    //bi_curHOUR = TimeHour (bdt_curTime);
    //bi_curDAY = TimeDay (bdt_curTime);
    bd_Balance = AccountBalance();
    bd_Equity = AccountEquity();
    bd_FreeMargin = AccountFreeMargin();
    bd_Margin = AccountMargin();
    if (li_PreHistOrders != OrdersHistoryTotal() || ldt_NewBar != ldt_NewPeriod || fi_Period < 0)
    {
        li_PreHistOrders = OrdersHistoryTotal();
        ldt_NewBar = ldt_NewPeriod;
        //---- ������������ ����� ������
        bd_Pribul = fCalculate_Pribul (gia_HistoryOrders, bs_SymbolList, MG, -1, TimeControl);
    }
    //---- ������������ ��������� ������
	 fGet_LastErrorInArray (bsa_Comment, "fGet_Statistic()", bi_indERR);
//----
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        �� ������ ��������� ������������ GV-����������                             |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fClear_WasteGV (string ar_NameGV[],  // ������ � ���������� ��������� GV-���������
                     string fs_Ticket)    // Ticket
{
    string ls_Name;
    int    li_size = ArraySize (ar_NameGV);
//---- 
    for (int li_IND = 0; li_IND < li_size; li_IND++)
    {
        ls_Name = StringConcatenate (fs_Ticket, ar_NameGV[li_IND]);
        if (GlobalVariableCheck (ls_Name)) GlobalVariableDel (ls_Name);
    }
//---- 
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        �������� Ticket ��������� ������                                           |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int fGet_�hildTicket (string fs_Comment)
{
    int li_N1 = StringFind (fs_Comment, "to");
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
//+===================================================================================+
//|***********************************************************************************|
//| ������: ��������� �������                                                         |
//|***********************************************************************************|
//+===================================================================================+
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        ������� ���������� �� ����                                                 |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fCommentInChart (string& ar_Comment[],      // ������ � �������������
                      string fs_ManagerPA = "")  // ������ �� ����������� �� ���������� ���������� ����� ��������
{
    static string lsa_Time[], lsa_Comment[], ls_BeginTrade,
                  ls_row = "���������������������������������\n",
                  ls_PSI = "������������� PSI�TarasBY �������������\n";
    static bool   lb_first = true;
    static int    li_size, li_size_CommTime, li_Period = 60;
    string ls_CTRL = "", ls_BLOCK_Comment, ls_Comment = "",
           ls_Error = "", ls_time = "", ls_sign, ls_TermsTrade;
//----
    //---- ��� ������ ������� ��������� ������� �������
    if (lb_first)
    {
        li_Period *= NewBarInPeriod;
        li_size = ArraySize (ar_Comment);
        ArrayResize (lsa_Time, li_size);
        ArrayResize (lsa_Comment, li_size);
        InitializeArray_STR (lsa_Comment);
        InitializeArray_STR (lsa_Time);
        ls_BeginTrade = StringConcatenate ("Terms Trade :: Begin - ", TimeToStr (bdt_BeginTrade));
        bdt_NewBarInPeriod = iTime (Symbol(), NewBarInPeriod, 0);
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
            lsa_Time[li_MSG] = StringConcatenate (TimeToStr (bdt_curTime), ls_sign);
            ar_Comment[li_MSG] = "";
        }
        //---- ��������� ���� ������������
        if (li_MSG < li_size - 1)
        {if (StringLen (lsa_Comment[li_MSG]) > 0) {ls_Comment = StringConcatenate (ls_Comment, lsa_Time[li_MSG], lsa_Comment[li_MSG], "\n");}}
        //---- ��������� ���� ������
        else if (li_MSG == li_size - 1)
        {
            //---- ������ 2 ���� ���������� �� ������ �������
            if (bdt_curTime > StrToTime (lsa_Time[li_MSG]) + 7200)
            {lsa_Comment[li_MSG] = "";}
            if (StringLen (lsa_Comment[li_MSG]) > 0) {ls_Error = StringConcatenate (ls_row, "ERROR:  ", lsa_Time[li_MSG], "\n", lsa_Comment[li_MSG]);}
        }
    }
    //---- ������ �������� �� �������� ������ ���������
    ls_time = StringConcatenate ("\nTime :: cur ", TimeToStr (bdt_curTime, TIME_DATE|TIME_SECONDS), " | local ", TimeToStr (TimeLocal(), TIME_MINUTES|TIME_SECONDS));
    if (NewBarInPeriod > 0) {ls_time = StringConcatenate (TimeToStr (bdt_NewBarInPeriod + li_Period), ls_time);}
    ls_TermsTrade = StringConcatenate (ls_BeginTrade, " | Last - ", TimeToStr (bdt_LastTrade), "\n");
    //---- ��������� ��� ����� ������������
    ls_BLOCK_Comment = StringConcatenate (ExpertName, "\n", ls_row, gs_Info, "\n",
                 gs_Base, ls_time, "\n",
                 ls_TermsTrade,
                 ls_row,
                 //---- ���� ����������� ������
                 "          PROFIT    = ", bs_sign, " ", fSplitField (DoubleToStr (bd_ProfitCUR, 1)), " | ", DoubleToStr (bd_ProfitPercent - 100.0, 1), " % [ ", bi_MyOrders, " | ", gia_MyOrders[0], " / ", gia_MyOrders[1], " ]\n",
                 //---- ���� ������ � ����� ��������
                 fs_ManagerPA,
                 "          RESULT    = ", bs_sign, " ", fSplitField (DoubleToStr (bd_Pribul, 1)), " [ ", fSplitField (gia_HistoryOrders[2]), " | ", fSplitField (gia_HistoryOrders[0]), " / ", fSplitField (gia_HistoryOrders[1]), " ]\n",
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
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        ������� ��������                                                           |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fPrepareComments()
{
    string ls_txt;
//---- 
    if (ShowCommentInChart)
    {
        if (!OnlyCurrentSymbol)
        {
            string ls_ListSymbols = IIFs (List_Symbols == "", "�� ����� ������������", "�� ������������:\n" + bs_SymbolList);
            if (NumberAccount > 0)
            {gs_Info = StringConcatenate ("�������� �� ����� �", AccountNumber(), " � �������� ", ls_ListSymbols);}
            if (NumberAccount == 0)
            {gs_Info = StringConcatenate ("�������� �� ������� ����� � �������� ", ls_ListSymbols);}
        }
        else
        {
            ls_ListSymbols = StringConcatenate ("c ", bs_SymbolList);
            if (NumberAccount > 0)
            {gs_Info = StringConcatenate ("�������� �� ����� �", AccountNumber(), " ������ � �������� ", Symbol(), ".");}
            if (NumberAccount == 0)
            {gs_Info = StringConcatenate ("�������� �� ������� ����� ������ � �������� ", Symbol(), ".");}
        }
        if (!ValueInCurrency)
        {gs_Base = StringConcatenate ("������ � ��������� �� ��������.\nTrailProfit_ON = ", CheckBOOL (TrailProfit_ON), "; TrailProfit_StartPercent = ", DoubleToStr (TrailProfit_StartPercent, 1), " %; TrailProfit_LevelPercent = ", DoubleToStr (TrailProfit_LevelPercent, 1), " %",
        "\nTakeProfit_ON = ", CheckBOOL (TakeProfit_ON), "; TPPercent = ", DoubleToStr (TP_AdvisorPercent, 1), " %; StopLoss_ON = ", CheckBOOL (StopLoss_ON), "; SLPercent = ", DoubleToStr (SL_AdvisorPercent, 1), " %");}
        else
        {gs_Base = StringConcatenate ("������ � ������ ��������.\nTrailProfit_ON = ", CheckBOOL (TrailProfit_ON), "; TrailProfit_Start = ", bs_sign, DS0 (TrailProfit_Start), "; TrailProfit_Level = ", bs_sign, DS0 (TrailProfit_Level),
        "\nTakeProfit_ON = ", CheckBOOL (TakeProfit_ON), "; TakeProfit = ", bs_sign, DS0 (TP_Advisor), "; StopLoss_ON = ", CheckBOOL (StopLoss_ON), "; StopLoss = ", bs_sign, DS0 (SL_Advisor));}
        gs_Base = StringConcatenate (gs_Base, "\n", IIFs ((NewBarInPeriod < 0), "�������� �� ������ ����", "����� ��: "));
    }
    ls_txt = StringConcatenate ("�������� ����� �������� ", ls_ListSymbols);
    Print (ls_txt);
    Print (IIFs ((MG == 0), "��� �������� �� MAGIC !!!", StringConcatenate ("� ��������� �� MAGIC = ", MG, " !!!")));
//---- 
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        ��������� ��������� ������ ���������                                       |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fCheck_MyAccount()
{
//----
    if (NumberAccount > 0)
    {
        if (AccountNumber() != NumberAccount)
        {
            flag_BadAccount = true;
            Alert ("�� ���������� ����� �����");
            return (false);
        }
    }      
//----
    return (true);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//+===================================================================================+
//|***********************************************************************************|
//| ������: TESTER                                                                    |
//|***********************************************************************************|
//+===================================================================================+
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        ��������� � �������� ������ ������                                         |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fOrderSend_Tester()
{
//---- 
    if (IsTesting())
    {
        static datetime ldt_NewBar = 0;
        datetime ldt_curBar = iTime (NULL, PERIOD_H4, 0);
        if (OrdersTotal() < 3 && ldt_NewBar != ldt_curBar)
        {
            if (fIs_LossLastPos ("", -1, -1))
            {InTesterOrder = IIFd ((InTesterOrder == 1), -1, 1);}
            if (InTesterOrder == 1)
            {fOrderSend (Symbol(), OP_BUY, 0.6, NDD (Ask), 0, NDD (Ask - gi_SL * Point), 0, NULL, MathMax (MG, 0));}
            if (InTesterOrder == -1)
            {fOrderSend (Symbol(), OP_SELL, 0.6, NDD (Bid), 0, NDD (Bid + gi_SL * Point), 0, NULL, MathMax (MG, 0));}
            ldt_NewBar = ldt_curBar;
        }
    }
//---- 
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+

