//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|                                                            b-PSI@LEVELs_Light.mqh |
//|                                    Copyright � 2011-12, Igor Stepovoi aka TarasBY |
//|                                                                taras_bulba@tut.by |
//|   24.05.2012  ���������� ������� �������.                                         |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|   ������ ������� ������������ ��� �������������� �������������. ���������� �����- |
//|���� ������ ��� �������� ����� ������ (TarasBY). �������������� ��������� ���� ��- |
//|������� ������ ��� ������� ���������� ������� ������, ������ � ����� ������.  ���- |
//|���� ���������� ��� ��������� � ������ ���������.                                 |
//|   ����� �� ����� ��������������� �� ��������� ������, ���������� � ���������� ��- |
//|����������� ����������.                                                            |
//|   �� ���� ��������, ��������� � ������� ����������, ����������� ��� ������������� |
//|�� � ��������� ���������� �� Skype: TarasBY ��� e-mail.                           |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
#property copyright "Copyright � 2008-12, TarasBY WM R418875277808; Z670270286972"
#property link      "taras_bulba@tut.by"
//IIIIIIIIIIIIIIIIIII==================CONSTANS=================IIIIIIIIIIIIIIIIIIIIII+
//IIIIIIIIIIIIIIIIIII==========������� ��������� ������=========IIIIIIIIIIIIIIIIIIIIII+
extern string Setup_EExtremum       = "---------------N1 from Extremum ---------------";
extern int    E.cnt_Bars               = 3;                // ���������� ����� ��� ������ ����������
extern string Setup_EATR            = "------------------N2 from ATR -----------------";
extern int    E.ATR_Period1            = 5;                // ������ ������� ATR (������ 0; ����� ���� ����� ATR_Period2, �� ����� ������� �� ����������)
extern int    E.ATR_Period2            = 15;               // ������ ������� ATR (������ 0)
extern double E.ATR_coeff              = 2.0;              // ����������� "�������������" �����������
extern string Setup_EZZ             = "----------------N3 from ZigZag ----------------";
extern double E.ChannelPercent         = 0.6;
//IIIIIIIIIIIIIIIIIII=========����������� ������� �������=======IIIIIIIIIIIIIIIIIIIIII+

//IIIIIIIIIIIIIIIIIII========���������� ���������� ������=======IIIIIIIIIIIIIIIIIIIIII+
//IIIIIIIIIIIIIIIIIII===========�������� ������� ������=========IIIIIIIIIIIIIIIIIIIIII+
/*bool fInit_LEVELs (int fi_N,                     // ����� �������� ������� �����������
                     string fs_Name = "N_STOPs")*/ // ��� ����������
                                     // ������������� ������
/*bool fCheck_LEVELsParameters (int fi_N,          // ����� �������� ������� �����������
                                string fs_Name = "N_STOPs")*/// ��� ����������
                                     // ��������� ���������� � ������� ������� ���������
//|***********************************************************************************|
//| ������: ������� ����ר�� LEVELs                                                   |
//|***********************************************************************************|
/*bool fGet_Extremum (double& ar_Extrem[],         // ������������ ������ �����������
                      string fs_Symbol = "",       // Symbol
                      int fi_TF = 0,               // TF
                      int fi_cntBars = 1)*/        // ���������� ��������������� ����� (�� 0)
                                     // �������� � ������ ���������� �� fi_cntBars ����� �� fi_TF �������.
/*bool fGet_ATR (double& ar_Extrem[],              // ������������ ������ �����������
                 string fs_Symbol = "",            // Symbol
                 int fi_TF = 0,                    // TF
                 int fi_cntBars = 2)*/             // ���������� ��������������� ����� (�� 0)
                                     // �������� � ������ ���������� �� ATR �� fi_cntBars ����� �� fi_TF �������.
/*bool fGet_ZZP (double& ar_Extrem[],              // ������������ ������ �����
                 string fs_Symbol = "",            // Symbol
                 int fi_TF = 0)*/                  // TF
                                     // �������� � ������ �����. �� ZZP �� fi_cntBars ����� �� fi_TF �������.
//|***********************************************************************************|
//| ������: ����� �������                                                             |
//|***********************************************************************************|
//         �������� � ������ LEVELs                                                   |
/*bool fGet_LEVELs (int fi_N,                      // ����� ������� ��������� �����������
                    int fi_TF,                     // ���������
                    double& ar_Extremum[])*/       // ������������ ������ � ������������
                                     // �������� � ������ LEVELs
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//         ������������� ������                                                       |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fInit_LEVELs (int fi_N,                    // ����� �������� ������� �����������
                   string fs_Name = "N_STOPs")  // ��� ����������
{
//----
    //---- ���������� �������� ������������ � ���������� ��������
    if (!fCheck_LEVELsParameters (fi_N, fs_Name))
    {
        Alert ("��������� ��������� ���������� ���� ������� ������� ������� !!!");
        bb_OptimContinue = true;
        return (false);
    }
    //---- ������������ ��������� ������
	 fGet_LastErrorInArray (bsa_Comment, "fInit_LEVELs()", bi_indERR);
//----
    return (true);
    double ld_Extr[2];
    fGet_LEVELs (0, 0, ld_Extr);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//         ��������� ���������� � ������� ������� ���������.                          |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fCheck_LEVELsParameters (int fi_N,                    // ����� �������� ������� �����������
                              string fs_Name = "N_STOPs")  // ��� ����������
{
    int err = GetLastError();
//----
    switch (fi_N)
    {
        case 0: break;
        case 1:
            if (E.cnt_Bars < 1) {Print ("��������� E.cnt_Bars >= 1 !!!"); return (false);}
            break;
        case 2:
            if (E.cnt_Bars < 1) {Print ("��������� E.cnt_Bars >= 1 !!!"); return (false);}
            if (E.ATR_Period1 > E.ATR_Period2) {Print ("��������� E.ATR_Period2 >= E.ATR_Period1 !!!"); return (false);}
            if (E.ATR_coeff <= 0) {Print ("��������� E.ATR_coeff > 0 !!!"); return (false);}
            break;
        case 3:
            if (E.ChannelPercent <= 0 || E.ChannelPercent >= 1) {Print ("��������� 0 < E.ChannelPercent < 1 !!!"); return (false);}
            break;
        default: Print ("��������� 0 <= ", fs_Name, " >= 3 !!!"); return (false);
    }
    //---- ������������ ��������� ������
	 fGet_LastErrorInArray (bsa_Comment, "fCheck_LEVELsParameters()", bi_indERR);
//----
     return (true);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//+===================================================================================+
//|***********************************************************************************|
//| ������: ������� ����ר�� ������� (�����������)                                    |
//|***********************************************************************************|
//+===================================================================================+
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//         �������� � ������ ���������� �� fi_cntBars ����� �� fi_TF �������.         |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fGet_Extremum (double& ar_Extrem[],      // ������������ ������ �����������
                    string fs_Symbol = "",    // Symbol
                    int fi_TF = 0,            // TF
                    int fi_cntBars = 1)       // ���������� ��������������� ����� (�� 0)
                    
{
    if (fs_Symbol == "") fs_Symbol = Symbol();
//----
    int li_BarLow = iLowest (fs_Symbol, fi_TF, MODE_LOW, fi_cntBars),
        li_BarHigh = iHighest (fs_Symbol, fi_TF, MODE_HIGH, fi_cntBars);
//----
    ArrayInitialize (ar_Extrem, EMPTY_VALUE);
    ar_Extrem[0] = iHigh (fs_Symbol, fi_TF, li_BarHigh);
    ar_Extrem[1] = iLow (fs_Symbol, fi_TF, li_BarLow);
    //---- ���������� �������� �� ������������ ���������� ��������
    for (int li_IND = 0; li_IND < 2; li_IND++)
    {
        if (ar_Extrem[li_IND] <= 0.0) return (false);
        if (ar_Extrem[li_IND] == EMPTY_VALUE) return (false);
    }
//----
    return (true);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//         �������� � ������ ���������� �� ATR �� fi_cntBars ����� �� fi_TF �������.  |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fGet_ATR (double& ar_Extrem[],      // ������������ ������ �����������
               string fs_Symbol = "",    // Symbol
               int fi_TF = 0,            // TF
               int fi_cntBars = 2)       // ���������� ��������������� ����� (�� 0)
{
    if (fs_Symbol == "") fs_Symbol = Symbol();
//----
    double ld_ATR = iATR (fs_Symbol, fi_TF, E.ATR_Period1, 1);
    ld_ATR = MathMax (ld_ATR, iATR (fs_Symbol, fi_TF, E.ATR_Period2, 1)) * E.ATR_coeff;
//----
    ArrayInitialize (ar_Extrem, EMPTY_VALUE);
    fGet_Extremum (ar_Extrem, fs_Symbol, fi_TF, fi_cntBars);
    ar_Extrem[0] += ld_ATR;
    ar_Extrem[1] -= ld_ATR;
    //---- ���������� �������� �� ������������ ���������� ��������
    for (int li_IND = 0; li_IND < 2; li_IND++)
    {
        if (ar_Extrem[li_IND] <= 0.0) return (false);
        if (ar_Extrem[li_IND] == EMPTY_VALUE) return (false);
    }
//----
    return (true);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//         �������� � ������ �����. �� ZZP �� fi_cntBars ����� �� fi_TF �������.      |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fGet_ZZP (double& ar_Extrem[],     // ������������ ������ �����
               string fs_Symbol = "",   // Symbol
               int fi_TF = 0)           // TF
{
    int li_Bar = 0;
//----
    if (fs_Symbol == "") fs_Symbol = Symbol();
    ArrayInitialize (ar_Extrem, EMPTY_VALUE);
    //---- ������� �������
    while (ar_Extrem[0] == EMPTY_VALUE)
    {
        li_Bar++;
        ar_Extrem[0] = iCustom (fs_Symbol, fi_TF, "XLab_ZZP", E.ChannelPercent, 0, li_Bar);
    }
    //Print ("HIGH[", TimeToStr (Time[li_Bar]), "] = ", DSD (ar_Extrem[0]));
    li_Bar = 0;
    //---- ������� �������
    while (ar_Extrem[1] == EMPTY_VALUE)
    {
        li_Bar++;
        ar_Extrem[1] = iCustom (fs_Symbol, fi_TF, "XLab_ZZP", E.ChannelPercent, 1, li_Bar);
    }
    //Print ("LOW[", TimeToStr (Time[li_Bar]), "] = ", DSD (ar_Extrem[1]));
    //---- ���������� �������� �� ������������ ���������� ��������
    for (int li_IND = 0; li_IND < 2; li_IND++)
    {
        if (ar_Extrem[li_IND] <= 0.0) return (false);
        if (ar_Extrem[li_IND] == EMPTY_VALUE) return (false);
    }
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
//         �������� � ������ LEVELs                                                   |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fGet_LEVELs (int fi_N,               // ����� ������� ��������� �����������
                  int fi_TF,              // ���������
                  double& ar_Extremum[])  // ������������ ������ � ������������
{
//----
    switch (fi_N)
    {
        //---- ���������� �� fi_Period �� E.cnt_Bars �����
        case 1: if (!fGet_Extremum (ar_Extremum, bs_Symbol, fi_TF, E.cnt_Bars)) return (false); break;
        //---- ������ �� ATR �� fi_Period
        case 2: if (!fGet_ATR (ar_Extremum, bs_Symbol, fi_TF, E.cnt_Bars)) return (false); break;
        //---- ������ �� ZZP (ZigZag) �� fi_Period
        case 3: if (!fGet_ZZP (ar_Extremum, bs_Symbol, fi_TF)) return (false); break;
    }
//----
    return (true);
}