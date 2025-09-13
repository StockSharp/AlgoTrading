//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|                                                                    b-PSI@Base.mqh |
//|                                       Copyright � 2012, Igor Stepovoi aka TarasBY |
//|                                                                taras_bulba@tut.by |
//| 17.03.2012  ���������� ������� �������.                                           |
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
extern int   ProfitMIN_Pips      = 3;              // ����������� ������ � ��.
//IIIIIIIIIIIIIIIIIII========���������� ���������� ������=======IIIIIIIIIIIIIIIIIIIIII+
double       bd_Price,                             // ������� ���� �� �������� �����������
             bda_Price[2],                         // ������� ���� (0 - Bid; 1 - Ask)
             bd_SL, bd_TP,                         // �����
             bd_SymPoint,                          // Point �������� �����������
             bd_Spread,                            // spread �������� �����������
             bd_OpenPrice,                         // ���� �������� �������� ������
             bd_MINLOT, bd_LOTSTEP, bd_MAXLOT,
             bd_STOPLEVEL,                         // ������� ������� STOPLEVEL �� �����������
             bd_ProfitMIN,                         // ������ ���������
             bd_TrailingStop, bd_TrailStep, bd_BreakEven,
             bda_MMValue[5],                       // ������ ����������� ���������� ���������� MM
             bd_ProfitCUR,                         // ����� ������ ��������� � ������ ����
             bd_ProfitPercent,                     // ����� ������ ��������� � ���������
             bd_Pribul = 0.0,                      // ���������� ������ ���������
             bd_MinEquity,                         // �������� ������������ ������ ����� � ������ ����
             bd_MinEquityPercent,                  // �������� ������������ ������ ����� � ���������
             bd_curEquityPercent,                  // �������� �������� ������ ����� � ���������
             bd_MinMargin,                         // ����������� �������� ��������� �������, ����������� ��� �������� �������
             bd_MinMarginPercent,                  // ����������� �������� ��������� �������, ����������� ��� �������� ������� � ���������
             bd_curMarginPercent,                  // ������� �������� ��������� �������, ����������� ��� �������� ������� � ���������
             bd_MaxZalog,                          // �������� ������������� ������ � ������ ����
             bd_MaxZalogPercent,                   // �������� ������������� ������ � ���������
             bd_curZalogPercent,                   // �������� �������� ������ � ���������
             bd_BeginBalance = 0.0,                // ��������� �������� �������
             bd_MaxLOSS,                           // �������� ������������ �������� �� �����
             bd_MaxLOSSPercent,                    // �������� ������������ �������� �� ����� � ���������
             bd_MAXBalance,                        // �������� ������������� ������� (��� �������� �� MAXOtkatDepoPercent)
             bd_curLOSSPercent,                    // ������� �������� �������� �� ����� � ���������
             bd_curMAXOtkatDepoPercent,            // ������� ������ ����� �� bd_MAXBalance
             //bd_LastLots = 0.0,                    // ������ ���� ���������� ���������� ������
             bd_Trail,                             // ������ �������� ��������� (�\� SL � �����)
             bd_BaseBalance,                       // ���������� ������ ��������� �� ����������� ��������
             bd_LastBalance,                       // ��������� ������ ����� �������          
             bd_TP_Adv,                            // ���� ������� ������� ��������� � ��������� �������� (ValueInCurrency)
             bd_SL_Adv,                            // SL �������� ������ ��������� � ��������� �������� (ValueInCurrency)
             bd_Balance, bd_Equity, bd_FreeMargin, bd_Margin,
             bd_NewSL, bd_NewTP, bd_curSL, bd_curTP,
             bda_FIBO[] = {0.0,0.236,0.382,0.5,0.618,0.764};
int          bi_curOrdersInSeries = 0,             // ������� ���������� ������� � ��������� �����
             bi_MyOrders = 0,                      // ������� "�����" �������
             bi_cntTrades = 0,                     // �������������� ������ ��� ������������� ��������
             bi_Error = 0,                         // ����� ��������� ������
             bi_curBarControlPeriod,               // ������� ������ �������� �������� �������
             bi_SymDigits, bi_Decimal, bi_digit, bi_indERR, bi_Type, bi_ShiftLocalDC_sec,
             bi_curHOUR, bi_curDAY,
             bia_Periods[] = {1,5,15,30,60,240,1440,10080,43200},
             bia_PartClose_Levels[], bia_PartClose_Percents[];
bool         bb_RealTrade,                         // ���� ������ on-line
             bb_VirtualTrade,                      // ���� �����������
             bb_OptimContinue,                     // ���� ����������� �����������
             bb_MMM = false,                       // MaxLot More MAXLOT
             bb_ClearGV = false,                   // ���� ������� ������������ GV-����������
             bb_PrintCom,                          // ���� ������ ������������
             bb_ShowCom,                           // ���� ������ ������������ �� �������
             bb_CreatVStopsInChart,                // ���� �������� �� ����� ����������� ������
             bb_PlaySound;                         // ���� ��������� ������������� �������
string       bsa_Comment[8],                       // ������ � �������������
             // bsa_Comment[0]- ������ MM
             // bsa_Comment[1]- ������ TimeControl
             // bsa_Comment[2]- ���������� �� ����������� ������� � ������������ ��������� (Trail)
             // bsa_Comment[3]- ���������� �� �������� ������� (TradeLight)
             // bsa_Comment[4]- ���������� �� �������� ������� (TradeLight)
             // bsa_Comment[5]- ��������� �������� ������� (PartClose) � ����������� ����� (VirtuaSTOPs)
             // bsa_Comment[6]- ������ � ����� �������� (ManagerPA)
             // bsa_Comment[7]- ������
      	    bs_libNAME,                           // ��� ���������� ����������
             bs_NameGV = "",                       // ������� GV-��������� ���������
             bs_ExpertName,                        // ��� ��������, ��������� �� ������
             bs_SymbolList = "",                   // ���� ����������� ���������� ��������
             bs_MagicList,                         // ���� ����������� ���������� �������
             bs_Delimiter,                         // ����������� ���������� � ����� bs_MagicList
             bsa_prefGV[0],                        // ������ � ���������� ��������� GV-���������
             bs_ErrorTL = "",                      // ���������� ��� ����� ���������� �� �������
             bs_Symbol,                            // ������� ����������
             bs_fName,                             // ��� ������� �������
             bsa_sign[2],                          // �������� ������� ����������� ���������� (������, %)
             bs_sign;                              // ������ ������ ��������
datetime     bdt_curTime,
             bdt_NewBar, bdt_BeginNewBar,
             bdt_LastTime,                         // ��������� ��������� �����
             bdt_BeginTrade, bdt_LastTrade = 0,
             bdt_LastBalanceTime, bdt_NewBarInPeriod, bdta_CommTime[6];
//IIIIIIIIIIIIIIIIIII=========����������� ������� �������=======IIIIIIIIIIIIIIIIIIIIII+
#include     <stdlib.mqh>                          // ���������� ����������� ������
#include     <stderror.mqh>                        // ���������� ����� ������
//#include     <b-PSI@MineGV.mqh>                    // ���������� ������ � �������� GV-�����������
//#include     <b-PSI@GrafOBJ.mqh>                   // ���������� ��������� �������� �� �������
//#include     <b-PSI@Comment.mqh>                   // ���������� ������ � �������������
#import "b-PSI@GrafOBJ.ex4"
bool fCreate_FIBO (string fs_Name,                 // ������� ����� ��������
                   int fi_Level,                   // ���������� �������� �������
                   double ar_FiboLevel[],          // ������ � FIBO-��������
                   datetime T1,                    // ������ ���������� �������
                   double P1,                      // ������ ���������� ����
                   datetime T2,                    // ������ ���������� �������
                   double P2);                     // ������ ���������� ����
                                     // ������ �� ������� FIBO-�����
bool fCreat_OBJ (string fs_Name,                   // ��� �������
                 int fi_OBJ,                       // ��� �������
                 string fs_Description,            // �������� �������
                 int fi_FontSize,                  // ������ ������ ������
                 datetime fdt_Time1,               // 1-� ���������� �������
                 double fd_Price1,                 // 1-� ���������� ����
                 bool fb_Ray = true,               // �������� ��� ��� OBJ_TREND
                 color fc_Color = Gold,            // ����
                 datetime fdt_Time2 = 0,           // 2-� ���������� �������
                 double fd_Price2 = 0);            // 2-� ���������� ����
                                     // ������ OBJ_TREND, OBJ_HLINE, OBJ_VLINE, OBJ_TEXT
bool fSet_Arrow (string fs_Name,                   // ��� �������
                 int fi_ArrowCode,                 // ����� ������ ��� OBJ_ARROW
                 string fs_Description,            // �������� �������
                 int fi_Size = 0,                  // ������ ������
                 color fc_Color = Gold,            // ����
                 datetime fdt_Time = 0,            // ���������� �������
                 double fd_Price = 0);             // ���������� ����
                                     // ��������� ������� OBJ_ARROW
bool fSet_Label (string fs_Name,                   // ������������ �������
                 string fs_Text,                   // ��� ������
                 int fi_X,                         // ���������� X
                 int fi_Y,                         // ���������� Y
                 int fi_Size = 10,                 // ������ ������ �������
                 string fs_Font = "Calibri",       // ����� �������
                 int fi_Corner = 0,                // ���� �������� �������
                 int fi_Angle = 0,                 // ���� �� ��������� � ���������
                 color fc_CL = CLR_NONE);          // ����
                                     // ��������� ������� OBJ_LABEL
void fObjectsDeleteAll  (string fs_Pref = "",      // ������� ����� �������
                         int ti_Type = -1,         // ��� �������
                         int fi_Window = -1);      // ����� ����
                                     // ������� ����������� ������� � �������
bool fObjectFind (string fs_Name);   // ���� ����������� ������ �� �����
bool fObjectDelete (string fs_Name); // ������� ����������� ������
#import
//IIIIIIIIIIIIIIIIIII===========�������� ������� ������=========IIIIIIIIIIIIIIIIIIIIII+
//void fInit_Base (string fs_SymbolList,           // ���� ������� �������� ���
                 //string fs_MagicList,            // ���� ������� �������bool fb_ShowCom = true,   // ���������� �� ����� ������������ �� �������
                 //bool fb_ShowCom = true,         // ���������� �� ����� ������������ �� �������
                 //bool fb_PrintCom = true,        // ���������� �� ������ ������������
                 //bool fb_PlaySound = true)       // ���������� �� �������� ������������� �������
                 //bool fb_CreatVStopsInChart = false,// ���������� �� ��������� ����������� ������ �� �����
                 //string fs_Delimiter = ",")      // ����������� ���������� � ����� fs_MagicList
                                     // ������������� ������
//|***********************************************************************************|
//| ������: ����� �������                                                             |
//|***********************************************************************************|
//double fGet_TradePrice (int fi_Price,            // ����: 0 - Bid; 1 - Ask
                        //bool fb_RealTrade,       // �������� �������� ��� �����������\������������
                        //string fs_Symbol = "")   // �������� ����
                                     // ��������� � ���� ��������� �������� ����
//int fControl_NewBar (int fi_TF = 0)// �������, �������������� ������ ������ ����
//datetime fGet_TermsTrade (string fs_SymbolList,  // ���� ����������� �������� ���
                          //string fs_MagicList,   // ���� ����������� �������
                          //datetime& fdt_LastTrade,// ����� ��� ��������� �����
                          //string fs_Delimiter = ",")// ����������� ���������� � ����� fs_MagicList
                                     // ������� ���� ������ �������� �� ���� ������� ��������� ������
//string fSplitField (string fs_Value)// ��������� ������� ����� ���������
//double fGet_Point (string fs_Symbol = "")
                                     // �������, ���������������� ��������� Point
//bool fCCV_D (double param, int ix) // ��������� ���� ��������� ������������ double ���������
//string fGet_NameOP (int fi_Type)   // ������� ���������� ������������ �������� ��������
//string fGet_NameTF (int fi_TF)     // ���������� ������������ ����������
//int NDPD (double v)                // ������� "������������" �������� �� Point � ����� �����
//double NDP (int v)                 // �������, �������� int � double �� Point
//double ND0 (double v)              // ������� ������������ �������� double �� ������
//double NDD (double v)              // ������� ������������ �������� �� Digits
//double NDDig (double v)            // �������, ������������ �������� double �� ����������� ����������� ����
//string DS0 (double v)              // �������, �������� �������� �� double � string c ������������� �� 0
//string DSD (double v)              // �������, �������� �������� �� double � string c ������������� �� Digits
//string DSDig (double v)            // �������, �������� �������� �� double � string c ������������� �� ����������� ����������� ����
//int LotDecimal()                   // �������, ����������� ����������� ����������� ����
//bool fCheck_NewBarInPeriod (int fi_Period = 0,   // TF
                            //bool fb_Conditions = true)// ������� �� ��������
                                     // ������������ ���� ������� ������ ���� �� NewBarInPeriod ������� (���� NewBarInPeriod >= 0)
//string fGet_SignCurrency()         // ������� ���������� ������ ������ ��������
//string CheckBOOL (int M)           // ���������� ������������ ��������� (��\���)
//double IIFd (bool condition, double ifTrue, double ifFalse)
                                     // ���������� ���� �� ���� �������� DOUBLE � ����������� �� �������
//string IIFs (bool condition, string ifTrue, string ifFalse)
                                     // ���������� ���� �� ���� �������� STRING � ����������� �� �������
//color IIFc (bool condition, color ifTrue, color ifFalse)
                                     // ���������� ���� �� ���� �������� COLOR � ����������� �� �������
//|***********************************************************************************|
//| ������: ������ � ���������                                                        |
//|***********************************************************************************|
//int fGet_INDInArrayINT (int fi_Value, int ar_Array[])
//int fGet_INDInArraySTR (string fs_Value, string ar_Array[])
                                     // �������� ������ �������� �������� � �������
//int fGet_NumPeriods (int fi_Period)// �������� ����� ������� �������
//InitializeArray_STR (string& PrepareArray[], string Value = "")
                                     // �������������� ������ STRING
//void fCreat_ArrayGV (string& ar_Base[],          // ������� ������
                     //string ar_Add[])            // ���������� ������
                                     // ������ ������ ��� ��������� GV-����������
//int fSplitStrToStr (string fs_List,              // ������ � �������
                    //string& ar_OUT[],            // ������������ ������
                    //string fs_Delimiter = ",")   // ����������� ������ � ������
                                     // ���������� ������ STRING �� ������, ���������� sDelimiter
//void fCreat_StrToInt (string ar_Value[],         // ������ ��������� string
                      //int& ar_OUT[],             // ������������ ������ int
                      //int fi_IND,                // ���������� ����� � �������
                      //int fi_Factor = 1,         // ���������
                      //string fs_NameArray = "")  // ��� ������������� �������
                                     // ���������� ������ INT �� ��������� ������� STRING
//void fCreat_StrToDouble (string ar_Value[],      // ������ ��������� string
                         //double& ar_OUT[],       // ������������ ������ double
                         //int fi_IND,             // ���������� ����� � �������
                         //double fd_Factor = 1.0, // ���������
                         //string fs_NameArray = "")// ��� ������������� �������
                                     // ���������� ������ DOUBLE �� ��������� ������� STRING
//string fCreat_StrAndArray (int fi_First,         // �������� 1-�� ��������� �������
                           //int& ar_OUT[],        // ������������ ������ int
                           //int fi_cntIND,        // ���������� ��������� � �������
                           //string fs_Delimiter = ",")// ����������� ��������� � ������������ ������
                                     // ���������� ������ �� ��������� ������� INT � ��� ������
//string fCreat_StrFromArray (string ar_Array[],   // ������ �� ����������
                            //string fs_Delimiter = ",")// ����������� ��������� � ������������ ������
                                     // ���������� ������ �� ��������� �������, ���������� fs_Delimiter
//|***********************************************************************************|
//| ������: ������ � ��������                                                         |
//|***********************************************************************************|
//int fGet_LastErrorInArray (string& Comm_Array[], // ������������ ������ ���������
                           //string Com = "",      // �������������� ���������� � ��������� �� ������
                           //int index = -1)       // ������ ������ � ������� ������� ��������� �� ������
                                     // �������� ����� � �������� ��������� ������ � ������� � ������ ���������
//int fGet_LastError (string& Comm_Error, string Com = "")
                                     // �������� ����� � �������� ��������� ������
//bool fErrorHandling (int fi_Error, bool& fb_InvalidSTOP)
                                     // ������� ������������ ������
//bool fCheck_LevelsBLOCK (int fi_Mode,            // ��� ���������� ��������: 1 - Close/Del; 2 - Send; 3 - Modify;
                         //string fs_Symbol,       // OrderSymbol()
                         //int fi_Type,            // OrderType()
                         //double& fd_NewOpenPrice,// OpenPrice
                         //double& fd_NewSL,       // StopLoss
                         //double& fd_NewTP,       // TakeProfit
                         //bool& fb_FixInvalidPrice)// ���� �������������� ��������� �������
                                     // �������� ������������ �� FREEZELEVEL � STOPLEVEL
//|***********************************************************************************|
//| ������: ��������� �������                                                         |
//|***********************************************************************************|
//void fReConnect()                  // ������������ �������� ��� ������� �����
//void fWrite_Log (string fs_Txt)    // ����� Log-����
//void fPrintAndShowComment (string& Text,         // ������������ ������ ������
                           //bool Show_Conditions, // ���������� �� ����� ��������� �� �������
                           //bool Print_Conditions,// ���������� �� ������ ���������
                           //string& s_Show[],     // ����������� ������ ���������
                           //int ind = -1)         // ������ ������ �������, ���� ������ ���������
                                     // ������� �� ������ � �� ������ �����������
//double fGet_ValueFromGV (string fs_Name,         // ��� GV-����������
                         //double fd_Value,        // ���� ����� ���������� ���, ������������� ��������
                         //bool fd_Condition = true)// ������� �� �������� ������� GV-����������
                                     // ���� �������� ���������� ��� �� GV, ��� (��� � �����������) fd_Value
//bool fDraw_VirtSTOP (int fi_CMD,         // BUY - 1; SELL = -1
                     //int fi_OP,          // ��� ����� (0 - SL; 1 - TP)
                     //double fd_Level)    // ������� �����
                                     // ������ �� ������� ����������� �������
//void fWork_LineBU (double ar_Lots[])             // ������ ������� �������� �������� �������
                               // ������ �� ����� ������� ��������������
//void fDelete_MyObjectsInChart (string fs_Name,   // ������� ����� ��������
                               //bool fb_Condition = true)// ���������� �� ��������
                                     // ��������� �� ����� ��� ������������ ������� �� �������
//void fClear_GV (int fi_cnt = 5000)               // ���-�� ��������� �������� (�������)
                                     // ��������� �������� ���������� GV-����������
//bool fSTOPTRADE()                  // ��������������
//|***********************************************************************************|
//| ������: MONEY MANAGEMENT                                                          |
//|***********************************************************************************|
//double fLotsNormalize (double fd_Lots)          // ������ ����
                                     // ���������� ������������ ����
//double fGet_PipsValue()            // ����������� ��������� ������ ����������� ������
//bool fCheck_MinProfit (double fd_MinProfit,     // ������ ����������� ������� �� ������
                       //double fd_NewSL,         // ����� SL
                       //bool fb_Condition = true)// ������� �� ���������� ��������
                                     // ��������� ������� �� ����������� ������ �� ������
//double fGet_BreakEven (string fs_Symbol,        // Symbol
                       //double ar_Lots[],        // ������ ������� �������� �������� �������
                       //double fd_Profit)        // ������� ������ �� �������� ������
                                     // ����������� ������ ��������� �� �������
//+===================================================================================+
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        ������������� ������                                                       |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fInit_Base (string fs_SymbolList,               // ���� ������� �������� ���
                 string fs_MagicList,                // ���� ������� �������
                 bool fb_ShowCom = true,             // ���������� �� ����� ������������ �� �������
                 bool fb_PrintCom = true,            // ���������� �� ������ ������������
                 bool fb_PlaySound = true,           // ���������� �� �������� ������������� �������
                 bool fb_CreatVStopsInChart = false, // ���������� �� ��������� ����������� ������ �� �����
                 string fs_Delimiter = ",")          // ����������� ���������� � ������ ������� �������
{
//----
    //---- ��������� ����������� ���������
    if (Digits % 2 == 1) {bi_Decimal = 10;} else {bi_Decimal = 1;}
    //---- ��������� ������� GV-����������
    //if (IsTesting()) {bs_NameGV = bs_NameGV + "_t";}
    //if (IsDemo()) {bs_NameGV = bs_NameGV + "_d";}
    //---- ������������� ���������� ������� �������� ��������
    fInit_Trade();
    bs_SymbolList = fs_SymbolList;
    bs_MagicList = fs_MagicList;
    bs_Delimiter = fs_Delimiter;
    bb_RealTrade = (!IsTesting() && !IsOptimization());
    bb_VirtualTrade = (IsOptimization() || (IsTesting() && !IsVisualMode()));
    if (bb_VirtualTrade) fb_CreatVStopsInChart = false;
    bb_CreatVStopsInChart = fb_CreatVStopsInChart;
    bs_ExpertName = StringConcatenate (WindowExpertName(), ":  ", fGet_NameTF (Period()), "_", Symbol());
    bb_PrintCom = fb_PrintCom;
    bb_ShowCom = fb_ShowCom;
    bb_PlaySound = fb_PlaySound;
    bdt_curTime = TimeCurrent();
    bd_MAXLOT = MarketInfo (Symbol(), MODE_MAXLOT);
    bd_MINLOT = MarketInfo (Symbol(), MODE_MINLOT);
    bd_LOTSTEP = MarketInfo (Symbol(), MODE_LOTSTEP);
    ProfitMIN_Pips *= bi_Decimal;
    fGet_MarketInfo (Symbol(), -1);
    if (bd_SymPoint == 0.0) {bd_SymPoint = fGet_Point();}
    bi_digit = LotDecimal();
    bs_sign = StringConcatenate (fGet_SignCurrency(), " ");
    InitializeArray_STR (bsa_Comment);
    //---- ���������� ������ "������" � ������� ������������ (gsa_Comment)
    bi_indERR = ArraySize (bsa_Comment) - 1;
    //---- ������� �������������� ������� ��� ������������� ��������
    string ls_Name = StringConcatenate (bs_NameGV, "_#cntTrades");
    bi_cntTrades = fGet_ValueFromGV (ls_Name, 0.0);
    //---- �� ������ ������ ��������� ��������� GV-����������
    if (!bb_RealTrade) fClear_GV();
    fRight_CompilTL();
    bb_OptimContinue = false;
    //---- ��������� ������ �����������
    Comment ("");
    //---- ������������ ��������� ������
    fGet_LastErrorInArray (bsa_Comment, "fInit_Base()", bi_indERR);
//----
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
//|        ��������� � ���� ��������� �������� ����.                                  |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
double fGet_TradePrice (int fi_Price,           // ����: 0 - Bid; 1 - Ask
                        bool fb_RealTrade,      // �������� �������� ��� �����������\������������
                        string fs_Symbol = "")  // �������� ����
{
    double ld_Price = 0.0;
//----
    if (fs_Symbol == "") {fs_Symbol = Symbol();}
    //RefreshRates();
    switch (fi_Price)
    {
        case 0:
            if (fb_RealTrade)
            {
                while (ld_Price == 0.0)
                {
                    if (fs_Symbol == Symbol()) {ld_Price = Bid;} else {ld_Price = MarketInfo (fs_Symbol, MODE_BID);}
                    if (!IsExpertEnabled() || IsStopped()) {break;}
                    Sleep (50); RefreshRates();
                }
            }
            else {if (fs_Symbol == Symbol()) {return (Bid);} else {return (MarketInfo (fs_Symbol, MODE_BID));}}
            break;
        case 1:
            if (fb_RealTrade)
            {
                while (ld_Price == 0.0)
                {
                    if (fs_Symbol == Symbol()) {ld_Price = Ask;} else {ld_Price = MarketInfo (fs_Symbol, MODE_ASK);}
                    if (!IsExpertEnabled() || IsStopped()) {break;}
                    Sleep (50); RefreshRates();
                }
            }
            else {if (fs_Symbol == Symbol()) {return (Ask);} else {return (MarketInfo (fs_Symbol, MODE_ASK));}}
            break;
    }
//----
    return (ld_Price);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        �������, �������������� ������ ������ ����                                 |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int fControl_NewBar (int fi_TF = 0)
{bdt_NewBar = iTime (Symbol(), fi_TF, 0); return (fi_TF);}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|         ������� ���� ������ �������� �� ���� ������� ��������� ������             |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
datetime fGet_TermsTrade (string fs_SymbolList,      // ���� ����������� �������� ���
                          string fs_MagicList,       // ���� ����������� �������
                          datetime& fdt_LastTrade,   // ����� ��� ��������� �����
                          string fs_Delimiter = ",") // ����������� ���������� � ����� fs_MagicList
{
    int      history_total = OrdersHistoryTotal();
    datetime ldt_Time = TimeCurrent(), ldt_OpenTime, ldt_CloseTime;
    string   ls_Symbol;
//----
    //fGet_TermsTrade (Symbol(), MG, bdt_LastTrade);
    bi_Error = GetLastError();
    for (int li_int = history_total - 1; li_int >= 0; li_int--)
    {
        if (!OrderSelect (li_int, SELECT_BY_POS, MODE_HISTORY)) continue;
        ls_Symbol = OrderSymbol();
        if (StringFind (fs_SymbolList, ls_Symbol) < 0 && StringLen (fs_SymbolList) > 0) continue;
        if (!fCheck_MyMagic (fs_MagicList, fs_Delimiter)) continue;
        if (OrderType() > 1) continue;
        ldt_OpenTime = OrderOpenTime();
        if (ldt_Time > ldt_OpenTime) {ldt_Time = ldt_OpenTime; continue;}
        ldt_CloseTime = OrderCloseTime();
        if (ldt_CloseTime > fdt_LastTrade) {fdt_LastTrade = ldt_CloseTime;}
    }
    //---- ���� ������� ��� - ��������� ������� ��������
    if (fdt_LastTrade == 0) fdt_LastTrade = ldt_Time;
    //---- ������������ ��������� ������
    fGet_LastErrorInArray (bsa_Comment, "fCalculate_Pribul()", bi_indERR);
//----
    return (ldt_Time);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        ��������� ������� ����� ���������                                          |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
string fSplitField (string fs_Value)
{
    string ls_Begin = fs_Value, ls_End, ls_tmp;
    int    li_N1 = StringFind (ls_Begin, "."), li_Len, li_plus = 0;
    bool   lb_minus = (StringFind (ls_Begin, "-") == 0);
//----
    if (lb_minus) li_plus = 1;
    //---- �������� ������� ����� � ������ ��� ������� (�� �����)
    if (li_N1 > 0)
    {
        li_N1 = MathMax (0, li_N1 - 3);
        ls_End = StringSubstr (ls_Begin, li_N1);
        if (li_N1 > 0) {ls_Begin = StringSubstr (ls_Begin, 0, li_N1);}
        else {return (fs_Value);}
    }
    li_Len = StringLen (ls_Begin);
    if (li_Len <= li_plus) {return (fs_Value);}
    while (li_Len > 3 + li_plus)
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
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        �������, ���������������� ��������� Point                                  |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
double fGet_Point (string fs_Symbol = "")
{
    double ld_Point = 0.0;
//----
    if (fs_Symbol == "") {fs_Symbol = Symbol();}
    ld_Point = MarketInfo (fs_Symbol, MODE_POINT);
    //---- ���� ���������� ���
    if (ld_Point == 0.0)
    {
        int li_Digits = MarketInfo (fs_Symbol, MODE_DIGITS);
        if (li_Digits > 0) {ld_Point = 1.0 / MathPow (10, li_Digits);}
    }
    else {return (ld_Point);}
//----
    return (ld_Point);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  �����    : TarasBY                                                               |
//+-----------------------------------------------------------------------------------+
//|  ������   : 27.10.2009                                                            |
//|  �������� : fControlChangeValue_D ��������� ���� ��������� ������������           |
//|  double ���������                                                                 |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fCCV_D (double param, int ix)
{
    static double cur_param[20];
    static bool   lb_first = true;
//---- 
    //---- ��� ������ ������� �������������� ������
    if (lb_first) {ArrayInitialize (cur_param, 0.0); lb_first = false;}
    if (cur_param[ix] != param) {cur_param[ix] = param; return (true);}
//---- 
    return (false);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        ������� ���������� ������������ �������� ��������                          |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
string fGet_NameOP (int fi_Type) 
{
//----
    switch (fi_Type) 
    {
        case -2          : return ("trading");
        case -1          : return ("ALL");
        case OP_BUY      : return ("BUY");
        case OP_SELL     : return ("SELL");
        case OP_BUYLIMIT : return ("BUYLIMIT");
        case OP_SELLLIMIT: return ("SELLLIMIT");
        case OP_BUYSTOP  : return ("BUYSTOP");
        case OP_SELLSTOP : return ("SELLSTOP");
        case 6           : if (OrderType() == 6) return ("balance"); else return ("UNI");
        case 7           : return ("pending");
    }
    return (StringConcatenate ("None (", fi_Type, ")"));
//----
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        ���������� ������������ ����������                                         |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
string fGet_NameTF (int fi_TF)
{
//----
    if (fi_TF == 0) fi_TF = Period();
    switch (fi_TF)
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
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        ������� "������������" �������� �� Point � ����� �����                     |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int NDPD (double v) {return (v / bd_SymPoint);}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        �������, �������� int � double �� Point                                    |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
double NDP (int v) {return (v * bd_SymPoint);}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        ������� ������������ �������� double �� ������                             |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
double ND0 (double v) {return (NormalizeDouble (v, 0));}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        ������� ������������ �������� double �� Digits                             |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
double NDD (double v) {return (NormalizeDouble (v, bi_SymDigits));}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        �������, ������������ �������� double �� ����������� ����������� ����      |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
double NDDig (double v) {return (NormalizeDouble (v, bi_digit));}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        �������, �������� �������� �� double � string c ������������� �� 0         |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
string DS0 (double v) {return (DoubleToStr (v, 0));} 
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        �������, �������� �������� �� double � string c ������������� �� Digits    |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
string DSD (double v) {return (DoubleToStr (v, bi_SymDigits));} 
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        �������, �������� �������� �� double � string c ������������� ��           |
//| ����������� ����������� ����                                                      |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
string DSDig (double v) {return (DoubleToStr (v, bi_digit));} 
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|        �������, ����������� ����������� ����������� ����                          |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int LotDecimal()
{return (MathCeil (MathAbs (MathLog (bd_LOTSTEP) / MathLog (10))));}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        ������������ ���� ������� ������ ���� �� NewBarInPeriod �������            |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fCheck_NewBarInPeriod (int fi_Period = 0,            // TF
                            bool fb_Conditions = true)    // ������� �� ��������
{
//----
    if (fi_Period >= 0)
    {
        if (fb_Conditions)
        {
            datetime ldt_BeginBarInPeriod = iTime (Symbol(), fi_Period, 0);
            if (bdt_NewBarInPeriod == ldt_BeginBarInPeriod) {return (false);}
            bdt_NewBarInPeriod = ldt_BeginBarInPeriod;
        }
    }
//----
    return (true);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        ������� ���������� ������ ������ ��������                                  |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
string fGet_SignCurrency()
{
//---- 
    if (AccountCurrency() == "USD") return ("$");
    if (AccountCurrency() == "EUR") return ("�");
//---- 
    return ("RUB");
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        ���������� ������������ ��������� (��\���)                                 |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
string CheckBOOL (int M)
{
//---- 
    switch (M)
    {
        case 0: return ("OFF");
        case 1: return ("ON");
    }
//---- 
    return ("Don`t know...");
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                          |
//+-----------------------------------------------------------------------------------+
//|  ������   : 01.02.2008                                                            |
//|  �������� : ���������� ���� �� ���� �������� ������������ �� �������.             |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
double IIFd (bool condition, double ifTrue, double ifFalse)
{if (condition) {return (ifTrue);} else return (ifFalse);}
//+-----------------------------------------------------------------------------------+
string IIFs (bool condition, string ifTrue, string ifFalse)
{if (condition) {return (ifTrue);} else return (ifFalse);}
//+-----------------------------------------------------------------------------------+
color IIFc (bool condition, color ifTrue, color ifFalse)
{if (condition) {return (ifTrue);} else return (ifFalse);}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//+===================================================================================+
//|***********************************************************************************|
//| ������: ������ � ���������                                                        |
//|***********************************************************************************|
//+===================================================================================+
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|  UNI:  �������� ������ �������� �������� � �������                                |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int fGet_INDInArrayINT (int fi_Value, int ar_Array[])
{
//---- 
    for (int li_IND = 0; li_IND < ArraySize (ar_Array); li_IND++)
    {if (ar_Array[li_IND] == fi_Value) return (li_IND);}
//---- 
    return (-1);
}
//+-----------------------------------------------------------------------------------+
int fGet_INDInArraySTR (string fs_Value, string ar_Array[])
{
//---- 
    for (int li_IND = 0; li_IND < ArraySize (ar_Array); li_IND++)
    {if (ar_Array[li_IND] == fs_Value) return (li_IND);}
//---- 
    return (-1);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|  UNI:  �������� ����� ������� �������                                             |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int fGet_NumPeriods (int fi_Period)
{
    static int lia_Periods[] = {1,5,15,30,60,240,1440,10080,43200};
//---- 
    for (int l_int = 0; l_int < ArraySize (lia_Periods); l_int++)
    {if (lia_Periods[l_int] == fi_Period) return (l_int);}
//---- 
    return (-1);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|  UNI:  �������������� Value ������ STRING                                         |
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
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        ������ ������ ��� ��������� GV-����������                                |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fCreat_ArrayGV (string& ar_Base[],  // ������� ������
                     string ar_Add[])    // ����������� ������
{
    int li_int, li_sizeB = ArraySize (ar_Base), li_sizeA = ArraySize (ar_Add);
    bool lb_duble;
//----
    for (int li_IND = 0; li_IND < li_sizeA; li_IND++)
    {
        lb_duble = false;
        //---- ������������ �������� �� ���������
        for (li_int = 0; li_int < li_sizeB; li_int++)
        {
            if (ar_Add[li_IND] == ar_Base[li_int])
            {lb_duble = true; break;}
        }
        //---- ���� �������� - ��� ������
        if (lb_duble) continue;
        //---- ����������� �������
        li_sizeB++;
        //---- ����������� ������� ������
        ArrayResize (ar_Base, li_sizeB);
        //---- ������ � ��������� ������ ����� ��������
        ar_Base[li_sizeB - 1] = ar_Add[li_IND];
    }
//----
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        ���������� ������ STRING �� ������, ���������� sDelimiter                 |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int fSplitStrToStr (string fs_List,             // ������ � �������
                    string& ar_OUT[],           // ������������ ������
                    string fs_Delimiter = ",")  // ����������� ������ � ������
{
    string tmp_str = "", tmp_char = "";
//----
    ArrayResize (ar_OUT, 0);
    for (int i = 0; i < StringLen (fs_List); i++)
    {
        tmp_char = StringSubstr (fs_List, i, 1);
        if (tmp_char == fs_Delimiter)
        {
            if (StringTrimLeft (StringTrimRight (tmp_str)) != "")
            {
                ArrayResize (ar_OUT, ArraySize (ar_OUT) + 1);
                ar_OUT[ArraySize (ar_OUT) - 1] = tmp_str;
            }
            tmp_str = "";
        }
        else {if (tmp_char != " ") tmp_str = tmp_str + tmp_char;}
    }
    if (StringTrimLeft (StringTrimRight (tmp_str)) != "")
    {
        ArrayResize (ar_OUT, ArraySize (ar_OUT) + 1);
        ar_OUT[ArraySize (ar_OUT) - 1] = tmp_str;
    }
//----
    return (ArraySize (ar_OUT));
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        ���������� ������ INT �� ��������� ������� STRING                          |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fCreat_StrToInt (string ar_Value[],        // ������ ��������� string
                      int& ar_OUT[],            // ������������ ������ int
                      int fi_IND,               // ���������� ����� � �������
                      int fi_Factor = 1,        // ���������
                      string fs_NameArray = "") // ��� ������������� �������
{
    int    li_size = ArraySize (ar_Value);
    string ls_row = "";
//----
    ArrayResize (ar_OUT, fi_IND);
    for (int li_int = 0; li_int < fi_IND; li_int++)
    {
        if (li_int < li_size) {ar_OUT[li_int] = StrToInteger (ar_Value[li_int]) * fi_Factor;}
        else {ar_OUT[li_int] = StrToDouble (ar_Value[li_size - 1]) * fi_Factor;}
        ls_row = StringConcatenate (ls_row, fs_NameArray, "[", li_int, "] = ", ar_OUT[li_int], "; ");
    }
    if (fs_NameArray != "") Print (ls_row);
//----
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        ���������� ������ DOUBLE �� ��������� ������� STRING                       |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fCreat_StrToDouble (string ar_Value[],        // ������ ��������� string
                         double& ar_OUT[],         // ������������ ������ double
                         int fi_IND,               // ���������� ����� � �������
                         double fd_Factor = 1.0,   // ���������
                         string fs_NameArray = "") // ��� ������������� �������
{
    int    li_size = ArraySize (ar_Value);
    string ls_row = "";
//----
    ArrayResize (ar_OUT, fi_IND);
    for (int li_int = 0; li_int < fi_IND; li_int++)
    {
        if (li_int < li_size) {ar_OUT[li_int] = StrToDouble (ar_Value[li_int]) * fd_Factor;}
        else {ar_OUT[li_int] = StrToDouble (ar_Value[li_size - 1]) * fd_Factor;}
        ls_row = StringConcatenate (ls_row, fs_NameArray, "[", li_int, "] = ", ar_OUT[li_int], "; ");
    }
    if (fs_NameArray != "") Print (ls_row);
//----
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        ���������� ������ �� ��������� ������� INT � ��� ������                    |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
string fCreat_StrAndArray (int fi_First,              // �������� 1-�� ��������� �������
                           int& ar_OUT[],             // ������������ ������ int
                           int fi_cntIND,             // ���������� ��������� � �������
                           string fs_Delimiter = ",") // ����������� ��������� � ������������ ������
{
    string ls_row = "";
//----
    ArrayResize (ar_OUT, fi_cntIND);
    for (int li_int = 0; li_int < fi_cntIND; li_int++)
    {
        if (li_int == fi_cntIND - 1) fs_Delimiter = "";
        ar_OUT[li_int] = fi_First + li_int;
        ls_row = StringConcatenate (ls_row, ar_OUT[li_int], fs_Delimiter);
    }
//----
    return (ls_row);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        ���������� ������ �� ��������� �������, ���������� fs_Delimiter           |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
string fCreat_StrFromArray (string ar_Array[],           // ������ �� ����������
                            string fs_Delimiter = ",")   // ����������� ��������� � ������������ ������
{
    string ls_row = "";
    int    li_size = ArraySize (ar_Array);
//----
    for (int li_int = 0; li_int < li_size; li_int++)
    {
        if (li_int == li_size - 1) fs_Delimiter = "";
        ls_row = StringConcatenate (ls_row, ar_Array[li_int], fs_Delimiter);
    }
//----
    return (ls_row);
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
//|        �������� ����� � �������� ��������� ������ � ������� � ������ ���������    |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int fGet_LastErrorInArray (string& Comm_Array[],  // ������������ ������ ���������
                           string Com = "",       // �������������� ���������� � ��������� �� ������
                           int index = -1)        // ������ ������ � ������� ������� ��������� �� ������
{
    if (bb_VirtualTrade) {return (0);}
    int err = GetLastError();
//---- 
    if (err > 0)
    {
        string ls_err = StringConcatenate (Com, ": ������ � ", err, " :: ", ErrorDescription (err));
        Print (ls_err);
        if (index >= 0) {Comm_Array[index] = ls_err;}
    }
//---- 
    return (err);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        �������� ����� � �������� ��������� ������                                 |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int fGet_LastError (string& Comm_Error, string Com = "")
{
    if (bb_VirtualTrade) {return (0);}
    int err = GetLastError();
//---- 
    if (err > 0)
    {
        Comm_Error = StringConcatenate (Com, ": ������ � ", err, " :: ", ErrorDescription (err));
        Print (Comm_Error);
    }
//---- 
    return (err);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//+===================================================================================+
//|***********************************************************************************|
//| ������: ��������� �������                                                         |
//|***********************************************************************************|
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        ����� Log-����                                                             |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fWrite_Log (string fs_Txt, int fi_IND = -1)
{
    if (!bb_RealTrade)
    {
        if (IsVisualMode()) {fPrintAndShowComment (fs_Txt, bb_ShowCom, bb_PrintCom, bsa_Comment, fi_IND);}
        return;
    }
    static datetime ldt_NewDay = 0;
    static string   ls_FileName = "";
    datetime ldt_BarD1 = iTime (Symbol(), NULL, PERIOD_D1);
    //---- ��� ��� ����� ���������� ���� ��� � �����
    if (ldt_NewDay != ldt_BarD1)
    {
        ls_FileName = StringConcatenate (WindowExpertName(), "_", Symbol(), "_", Period(), "-", Month(), "-", Day(), ".log");
        ldt_NewDay = ldt_BarD1;
    }
    int handle = FileOpen (ls_FileName, FILE_READ|FILE_WRITE|FILE_CSV, "/t");
//----
    FileSeek (handle, 0, SEEK_END);      
    FileWrite (handle, StringConcatenate (TimeToStr (TimeCurrent(), TIME_DATE|TIME_SECONDS), ": ", fs_Txt));
    FileClose (handle);
	 fPrintAndShowComment (fs_Txt, bb_ShowCom, bb_PrintCom, bsa_Comment, fi_IND);
//----
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        ������� �� ������ � �� ������ �����������                                  |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fPrintAndShowComment (string& fs_Text,          // ������������ ������ ������
                           bool fb_ShowConditions,   // ���������� �� ����� ��������� �� �������
                           bool fb_PrintConditions,  // ���������� �� ������ ���������
                           string& ar_Show[],        // ������������ ������ ���������
                           int fi_IND = -1)          // ������ ������ �������, ���� ������ ���������
{
    if (fb_ShowConditions || fb_PrintConditions)
    {
        if (StringLen (fs_Text) > 0)
        {
            if (fb_ShowConditions) {if (fi_IND >= 0) ar_Show[fi_IND] = fs_Text;}
            if (fb_PrintConditions)
            {
                if (bs_libNAME != "") fs_Text = StringConcatenate (bs_libNAME, ":     ", fs_Text);
                Print (fs_Text);
            }
        }
    }
    //---- ������� ����������
    fs_Text = "";
//---- 
    return;
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//         ���� �������� ���������� ��� �� GV, ��� (��� � �����������) fd_Value     |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
double fGet_ValueFromGV (string fs_Name,            // ��� GV-����������
                         double fd_Value,           // ���� ����� ���������� ���, ������������� ��������
                         bool fd_Condition = true)  // ������� �� �������� ������� GV-����������
{
//----
    if (!fd_Condition) {return (fd_Value);}
    if (GlobalVariableCheck (fs_Name)) {return (GlobalVariableGet (fs_Name));}
    else {return (fd_Value);}
//----
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//         ������ �� ������� ����������� �������                                      |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fDraw_VirtSTOP (int fi_CMD,         // BUY - 1; SELL = -1
                     int fi_OP,          // ��� ����� (0 - SL; 1 - TP)
                     double fd_Level)    // ������� �����
{
    if (OrderSymbol() != Symbol()) return (false);
//----
    bool   lb_result = false;
    int    li_pip, li_cmd = 1;
    color  lc_color;
    string ls_Name, lsa_NameOP[] = {"SL","TP"};
//----
    if (fi_OP == 1) li_cmd = -1;
    li_pip = li_cmd * fi_CMD * NDPD (OrderOpenPrice() - fd_Level);
    if (li_pip > 0) {lc_color = Aqua;} else {lc_color = Magenta;}
    ls_Name = StringConcatenate (bs_NameGV, "_", OrderTicket(), "_Virt", lsa_NameOP[fi_OP]);
    lb_result = fCreat_OBJ (ls_Name, OBJ_TREND, StringConcatenate (lsa_NameOP[fi_OP], "(", li_pip, ")"), 10, OrderOpenTime(), fd_Level, False, lc_color, OrderOpenTime() + 10 * Period() * 60, fd_Level);
    ObjectSet (ls_Name, OBJPROP_WIDTH, 2);
//----
    return (lb_result);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        ������ �� ����� ������� ��������������                                     |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fWork_LineBU (double ar_Lots[])     // ������ ������� �������� �������� �������
{
    if (bb_VirtualTrade) return;
    if (bi_MyOrders < 2)
    {
        string ls_Name = StringConcatenate (bs_NameGV, "_BU");
        if (ObjectFind (ls_Name) == 0) ObjectDelete (ls_Name);
    }
    else if (bi_MyOrders > 1)
    {
        //---- ������ �� ����� ������� ��������������
        double ld_BU = fGet_BreakEven (bs_Symbol, ar_Lots, bd_ProfitCUR);
        if (ld_BU > 0.0)
        {
            int li_cmd = -1;
            ls_Name = StringConcatenate (bs_NameGV, "_BU");
            if (ld_BU > Bid && ar_Lots[0] > ar_Lots[1]) li_cmd = 1;
            if (ld_BU < Ask && ar_Lots[0] < ar_Lots[1]) li_cmd = 1;
            fCreat_OBJ (ls_Name, OBJ_HLINE, StringConcatenate ("BU (", NDPD (ld_BU - Bid), ")"), 10, Time[0], ld_BU, False, Gold, Time[0], ld_BU);
            ObjectSet (ls_Name, OBJPROP_STYLE, 1);
        }
    }
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        ��������� �������� ���������� GV-����������                                |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fClear_GV (int fi_cnt = 5000)    // ���-�� ��������� �������� (�������)
{
    string ls_Name, lsa_Name[] = {"_#Delta_SL","_#LastLossLevel","_#VirtSL",
           "_#VirtTP","_#BeginSL","_#SL","_#TP","_#Num","_#Lots","_#BU","_#STOP","_#OP"};
    int    li_size = ArraySize (lsa_Name), li_IND;
//---- 
    for (int li_CNT = 0; li_CNT < fi_cnt; li_CNT++)
    {
        for (li_IND = 0; li_IND < li_size; li_IND++)
        {
            ls_Name = StringConcatenate (li_CNT, lsa_Name[li_IND]);
            if (GlobalVariableCheck (ls_Name)) GlobalVariableDel (ls_Name); 
        }
    }
//---- 
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        ��������������                                                             |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fSTOPTRADE()
{
//----
    if (bb_OptimContinue) if (!bb_VirtualTrade)
    {fSet_Label ("STOP", "STOP TRADE !!! ������ ���. ����� ���������.", 200, 200, 20, "Calibri", 0, 0, Red); return (true);}
//----
    return (false);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        ��������� �� ����� ��� ������������ ������� �� �������                     |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
/*void fDelete_MyObjectsInChart (string fs_Name,             // ������� ����� ��������
                               bool fb_Condition = true)   // ���������� �� ��������
{
    if (!fb_Condition) return;
    string ls_Name;
//----
    //---- ��������� ������� �� �������
    for (int li_OBJ = ObjectsTotal() - 1; li_OBJ >= 0; li_OBJ--)
    {
        ls_Name = ObjectName (li_OBJ);
        if (StringFind (ls_Name, fs_Name) == 0) {ObjectDelete (ls_Name);}
    }
//----
}*/
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|        "������������" �� ��������������� � ���� �������                           |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fRight_CompilTL()
{
//----
    return;
    IIFd (true, 1.0, 0.0);
    IIFs (true, "", "");
    IIFc (true, Blue, Red);
    fGet_NameTF (0);
    ND0 (1.0);
    DS0 (1.0);
    NDPD (1.);
    CheckBOOL (1);
    fGet_NumPeriods (0);
    int li_tmp, lia_tmp[1];
    fGet_TermsTrade ("", 0, li_tmp);
    fCreat_StrAndArray (0, lia_tmp, 0);
    string lsa_tmp[1], ls_tmp, lsa_tmp2[1];
    fCreat_ArrayGV (lsa_tmp, lsa_tmp2);
    fCreat_StrFromArray (lsa_tmp);
    fGet_LastError (ls_tmp); 
    fGet_INDInArraySTR ("", lsa_tmp);
    fSplitStrToStr ("", lsa_tmp);
    InitializeArray_STR (lsa_tmp);
    fSplitField ("");
    double lda_tmp[1];
    fGet_INDInArrayINT (1, lia_tmp);
    fCreat_StrToInt (lsa_tmp, lia_tmp, 1);
    fCreat_StrToDouble (lsa_tmp, lda_tmp, 1);
    fGet_BreakEven (Symbol(), lda_tmp, 0);
    fControl_MAXLife (0);
    fControl_NewBar (0);
    fCCV_D (1, 0);
    fDraw_VirtSTOP (li_tmp, 0, 0);
    fWork_LineBU (lda_tmp);
    fCheck_MinProfit (0, 0);
    fCheck_NewBarInPeriod (0);
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
//|        ���������� ������������ ����                                               |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
double fLotsNormalize (double fd_Lots)     // ������ ����
{
    double ld_Lot = fd_Lots;
//----
    fd_Lots -= bd_MINLOT;
    fd_Lots /= bd_LOTSTEP;
    fd_Lots = MathRound (fd_Lots);
    fd_Lots *= bd_LOTSTEP;
    fd_Lots += bd_MINLOT;
    if (fd_Lots < bd_MINLOT)
    {
        fSet_Comment (bdt_curTime, 0, 6, "", True, ld_Lot, fd_Lots);
        fd_Lots = bd_MINLOT;
    }
    //---- ������� ����������� ������� ������������� ����
    /*if (!bb_MMM)
    {
        if (fd_Lots > bd_MAXLOT)
        {
            fSet_Comment (bdt_curTime, 0, 7, "", True, fd_Lots);
            fd_Lots = bd_MAXLOT;
        }
    }*/
//----
    return (NDDig (fd_Lots));
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        ����������� ��������� ������ ����������� ������                            |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
double fGet_PipsValue()
{
    double ld_Price, ld_TickValue, ld_pips;
//----
    //---- ���� ����� ������
    if (OrderCloseTime() > 0) {ld_Price = OrderClosePrice();}
    else {ld_Price = fGet_TradePrice (OrderType(), bb_RealTrade, OrderSymbol());}
    ld_pips = NDPD (OrderOpenPrice() - ld_Price);
    if (ld_pips == 0.0) {return (1);}
    ld_TickValue = MathAbs ((OrderProfit() + OrderSwap() + OrderCommission()) / ld_pips);
    //---- ������ ��������� ������ �� MINLOT
    //ld_TickValue = ld_TickValue / OrderLots() * MarketInfo (gs_Symbol, MODE_MINLOT);
    if (ld_TickValue == 0.0) {return (1);}
//----
    return (ld_TickValue);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//         ��������� ������� �� ����������� ������� �� ������.                        |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fCheck_MinProfit (double fd_MinProfit,      // ������ ����������� ������� �� ������
                       double fd_NewSL,          // ����� SL
                       bool fb_Condition = true) // ������� �� ���������� ��������
{
    if (fb_Condition)
    {
        if (OrderType() == OP_BUY)
        {if (NDD ((fd_NewSL - OrderOpenPrice()) - fd_MinProfit) < 0.0) return (false);}
        else {if (NDD ((OrderOpenPrice() - fd_NewSL) - fd_MinProfit) < 0.0) return (false);}
    }
//----
    return (true);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        ����������� ������ ��������� �� �������                                    |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
double fGet_BreakEven (string fs_Symbol,     // Symbol
                       double ar_Lots[],     // ������ ������� �������� �������� �������
                       double fd_Profit)     // ������� ������ �� �������� ������
{
     double ld_BU = 0.0, ld_Lots = NDDig (ar_Lots[0] - ar_Lots[1]),  // �������� ������� ������� Buy � Sell
            ld_tickvalue = MarketInfo (fs_Symbol, MODE_TICKVALUE);   // ���� ������ ������
//----
     if (ld_Lots != 0.0)
     {
         //---- ������� ������ ��������� ��� �������� �������
         if (ld_Lots > 0) ld_BU = fGet_TradePrice (0, bb_RealTrade, fs_Symbol) - NDP (fd_Profit / (ld_tickvalue * ld_Lots));
         else if (ld_Lots < 0) ld_BU = fGet_TradePrice (1, bb_RealTrade, fs_Symbol) - NDP (fd_Profit / (ld_tickvalue * ld_Lots));
     }
//----
    return (ld_BU);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+

