//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|                                                                 b-PSI@Comment.mqh |
//|                                       Copyright � 2012, Igor Stepovoi aka TarasBY |
//|                                                                taras_bulba@tut.by |
//| 18.04.2012  ���������� ������ � �������������                                     |
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
//IIIIIIIIIIIIIIIIIII=========����������� ������� �������=======IIIIIIIIIIIIIIIIIIIIII+
//IIIIIIIIIIIIIIIIIII========���������� ���������� ������=======IIIIIIIIIIIIIIIIIIIIII+
                    // bsa_Comment[0]- ������ MM
                    // bsa_Comment[1]- ������ TimeControl
                    // bsa_Comment[2]- ���������� �� ����������� ������� � ������������ ��������� (Trail)
                    // bsa_Comment[3]- ���������� �� �������� ������� (TradeLight)
                    // bsa_Comment[4]- ���������� �� �������� ������� (TradeLight)
                    // bsa_Comment[5]- ��������� �������� ������� (PartClose) � ����������� ����� (VirtuaSTOPs)
                    // bsa_Comment[6]- ������ � ����� �������� (ManagerPA)
                    // bsa_Comment[7]- ������
//IIIIIIIIIIIIIIIIIII===========�������� ������� ������=========IIIIIIIIIIIIIIIIIIIIII+
/*void fSet_Comment (double fd_CheckParameter,            // ����������� �������� (��� �������� ������������� ������ �����������)
                     int fi_Ticket,                       // Ticket
                     int fi_N,                            // ����� �����������
                     string fs_MSG = "",                  // ����� ���������
                     bool fb_Right = true,                // ���������� �������� ��� ������� ������� �����������
                     double fd_Value1 = 0.0,              // ������������ � ����������� ��������
                     double fd_Value2 = 0.0,              // ������������ � ����������� ��������
                     double fd_Value3 = 0.0,              // ������������ � ����������� ��������
                     double fd_Value4 = 0.0)*/            // ������������ � ����������� ��������
                                     // ��������� ����������� ��� ������������ �������
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//+===================================================================================+
//|***********************************************************************************|
//| ������: ������ � �������������                                                    |
//|***********************************************************************************|
//+===================================================================================+
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  �����    : TarasBY, taras_bulba@tut.by                                           |
//+-----------------------------------------------------------------------------------+
//|           ��������� ����������� ��� ������������ �������                          |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fSet_Comment (double fd_CheckParameter,  // ����������� �������� (��� �������� ������������� ������ �����������)
                   int fi_Ticket,             // Ticket
                   int fi_N,                  // ����� �����������
                   string fs_MSG = "",        // ����� ���������
                   bool fb_Right = true,      // ���������� �������� ��� ������� ������� �����������
                   double fd_Value1 = 0.0,    // ������������ � ����������� ��������
                   double fd_Value2 = 0.0,    // ������������ � ����������� ��������
                   double fd_Value3 = 0.0,    // ������������ � ����������� ��������
                   double fd_Value4 = 0.0)    // ������������ � ����������� ��������
{
	 if (bb_VirtualTrade) return;
	 int    err = GetLastError();
	 string ls_com = "", ls_NewRow = ": ", ls_order = "";
//----
    switch (fi_N)
    {
        //---- ����������� ��
        case 0: ls_com = StringConcatenate ("�������� ����� ������: Lots = ", DSDig (fd_Value1)); break;
        case 1: break; // �� ��������� 
    }
    //---- ������� ����������� �� ������, ����� � ��� � � ����
    //fWrite_Log (fs_MSG, li_IND);
    //---- ���������� �������
    if (bb_PlaySound) {if (fb_Right) PlaySound ("ok.wav"); else PlaySound ("stops.wav");}
    //---- ������������ ��������� ������
	 fGet_LastErrorInArray (bsa_Comment, "fSet_Comment()", bi_indERR);
//----
	 return;
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+

