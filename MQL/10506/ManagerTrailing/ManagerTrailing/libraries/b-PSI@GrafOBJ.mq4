//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|                                                                 b-PSI@GrafOBJ.mq4 |
//|                                       Copyright � 2012, Igor Stepovoi aka TarasBY |
//|                                                                taras_bulba@tut.by |
//| 14.04.2012  ���������� ������� ��������� �������� �� �������.                     |
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
#property library
//IIIIIIIIIIIIIIIIIII==================CONSTANS=================IIIIIIIIIIIIIIIIIIIIII+
//IIIIIIIIIIIIIIIIIII=========����������� ������� �������=======IIIIIIIIIIIIIIIIIIIIII+
#include     <stdlib.mqh>                          // ���������� ����������� ������
//IIIIIIIIIIIIIIIIIII========���������� ���������� ������=======IIIIIIIIIIIIIIIIIIIIII+
string bs_Error = "";
//IIIIIIIIIIIIIIIIIII===========�������� ������� ������=========IIIIIIIIIIIIIIIIIIIIII+
/*bool fCreate_FIBO (string fs_Name,                      // ������� ����� ��������
                     int fi_Level,                        // ���������� �������� �������
                     double ar_FiboLevel[],               // ������ � FIBO-��������
                     datetime T1,                         // ������ ���������� �������
                     double P1,                           // ������ ���������� ����
                     datetime T2,                         // ������ ���������� �������
                     double P2)*/                         // ������ ���������� ����
                                     // ������ �� ������� FIBO-�����
/*void fCreat_OBJ (string fs_Name,                        // ��� �������
                   int fi_OBJ,                            // ��� �������
                   string fs_Description,                 // �������� �������
                   int fi_FontSize,                       // ������ ������ ������
                   datetime fdt_Time1,                    // 1-� ���������� �������
                   double fd_Price1,                      // 1-� ���������� ����
                   bool fb_Ray = true,                    // �������� ��� ��� OBJ_TREND
                   color fc_Color = Gold,                 // ����
                   datetime fdt_Time2 = 0,                // 2-� ���������� �������
                   double fd_Price2 = 0)*/                // 2-� ���������� ����
                                     // ������ OBJ_TREND, OBJ_TEXT, OBJ_HLINE, OBJ_VLINE
/*bool fSet_Arrow (string fs_Name,                        // ��� �������
                   int fi_ArrowCode,                      // ����� ������ ��� OBJ_ARROW
                   string fs_Description,                 // �������� �������
                   int fi_Size = 0,                       // ������ ������
                   color fc_Color = Gold,                 // ����
                   datetime fdt_Time = 0,                 // ���������� �������
                   double fd_Price = 0)*/                 // ���������� ����
                                     // ��������� ������� OBJ_ARROW
/*void fSet_Label (string fs_Name,                        // ������������ �������
                   string fs_Text,                        // ��� ������
                   int fi_X,                              // ���������� X
                   int fi_Y,                              // ���������� Y
                   int fi_Size = 10,                      // ������ ������ �������
                   string fs_Font = "Calibri",            // ����� �������
                   int fi_Corner = 0,                     // ���� �������� �������
                   int fi_Angle = 0,                      // ���� �� ��������� � ���������
                   color fc_CL = CLR_NONE)*/              // ����
                                     // ��������� ������� OBJ_LABEL
/*void fObjectsDeleteAll  (string fs_Pref = "",           // ������� ����� �������
                           int fi_Window = -1,            // ����� ����
                           int ti_Type = -1)*/            // ��� �������
                                     // ������� ����������� ������� � �������
//bool fObjectFind (string fs_Name)  // ���� ����������� ������ �� �����
//bool fObjectDelete (string fs_Name)// ������� ����������� ������
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//+===================================================================================+
//|***********************************************************************************|
//| ������: ������ � ������������ ���������                                           |
//|***********************************************************************************|
//+===================================================================================+
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        ������ �� ������� FIBO-�����.                                              |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fCreate_FIBO (string fs_Name,        // ������� ����� ��������
                   int fi_Level,          // ���������� �������� �������
                   double ar_FiboLevel[], // ������ � FIBO-��������
                   datetime T1,           // ������ ���������� �������
                   double P1,             // ������ ���������� ����
                   datetime T2,           // ������ ���������� �������
                   double P2)             // ������ ���������� ����
{
	 //int err = GetLastError();
//----
	 fs_Name = StringConcatenate (fs_Name, "NET");
    if (!fObjectFind (fs_Name)) {if (!ObjectCreate (fs_Name, OBJ_FIBO, 0, 0, 0)) return (false);}
	 ObjectSet (fs_Name, OBJPROP_TIME1, T1);
	 ObjectSet (fs_Name, OBJPROP_PRICE1, P1);
	 ObjectSet (fs_Name, OBJPROP_TIME2, T2);
	 ObjectSet (fs_Name, OBJPROP_PRICE2, P2);
	 ObjectSet (fs_Name, OBJPROP_FIBOLEVELS, fi_Level);
    ObjectSet (fs_Name, OBJPROP_COLOR, Red);
    //----
	 for (int i = 0; i < fi_Level; i++)
	 {
        ObjectSet (fs_Name, OBJPROP_LEVELCOLOR, LightBlue);
        ObjectSet (fs_Name, OBJPROP_FIRSTLEVEL + i, ar_FiboLevel[i+5]);
        ObjectSet (fs_Name, OBJPROP_RAY, 0);                   // �� ���
        ObjectSetFiboDescription (fs_Name, i, StringConcatenate (DoubleToStr (ar_FiboLevel[i+5] * 100.0, 1), " %$"));
	 }
    //---- ������������ ��������� ������
	 fGet_LastError (bs_Error, "fCreate_FIBO()");
//----
	 return (true);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        ������ OBJ_TREND, OBJ_HLINE, OBJ_VLINE, OBJ_TEXT                           |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fCreat_OBJ (string fs_Name,         // ��� �������
                 int fi_OBJ,             // ��� �������
                 string fs_Description,  // �������� �������
                 int fi_FontSize,        // ������ ������ ������
                 datetime fdt_Time1,     // 1-� ���������� �������
                 double fd_Price1,       // 1-� ���������� ����
                 bool fb_Ray = true,     // �������� ��� ��� OBJ_TREND
                 color fc_Color = Gold,  // ����
                 datetime fdt_Time2 = 0, // 2-� ���������� �������
                 double fd_Price2 = 0)   // 2-� ���������� ����
{
    int err = GetLastError();
//----
    //fCreat_OBJ (Name, OBJ_LABEL, "", 10, T1, P1, false, Red);
    if (!fObjectFind (fs_Name)) {if (!ObjectCreate (fs_Name, fi_OBJ, 0, 0, 0)) return (false);}
    ObjectSet (fs_Name, OBJPROP_TIME1, fdt_Time1);
    ObjectSet (fs_Name, OBJPROP_PRICE1, fd_Price1);
    if (fdt_Time2 != 0) ObjectSet (fs_Name, OBJPROP_TIME2, fdt_Time2);
    if (fd_Price2 != 0) ObjectSet (fs_Name, OBJPROP_PRICE2, fd_Price2);
    ObjectSet (fs_Name, OBJPROP_COLOR, fc_Color);
    if (fi_OBJ == OBJ_TREND) {ObjectSet (fs_Name, OBJPROP_RAY, fb_Ray);}
    if (fs_Description != "") ObjectSetText (fs_Name, fs_Description, fi_FontSize, "Calibri", fc_Color);
    //---- ������������ ��������� ������
    fGet_LastError (bs_Error, "fCreat_OBJ()");
//----
    return (true);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  �����    : TarasBY, taras_bulba@tut.by                                           |
//+-----------------------------------------------------------------------------------+
//|  ������   : 23.07.2011                                                            |
//|  �������� : ��������� ������� OBJ_ARROW                                           |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fSet_Arrow (string fs_Name,         // ��� �������
                 int fi_ArrowCode,       // ����� ������ ��� OBJ_ARROW
                 string fs_Description,  // �������� �������
                 int fi_Size = 0,        // ������ ������
                 color fc_Color = Gold,  // ����
                 datetime fdt_Time = 0,  // ���������� �������
                 double fd_Price = 0)    // ���������� ����
{
	 int err = GetLastError();
//----
    if (fs_Name == "") fs_Name = TimeToStr (Time[0]);
    if (!fObjectFind (fs_Name)) {if (!ObjectCreate (fs_Name, OBJ_ARROW, 0, 0, 0)) return (false);}
    if (fdt_Time == 0) {fdt_Time = Time[0];}
    if (fd_Price == 0) {fd_Price = Bid;}
    ObjectSet (fs_Name, OBJPROP_TIME1, fdt_Time);
    ObjectSet (fs_Name, OBJPROP_PRICE1, fd_Price);
    ObjectSet (fs_Name, OBJPROP_ARROWCODE, fi_ArrowCode);
    if (fs_Description != "") ObjectSetText (fs_Name, fs_Description, 10, "Calibri", fc_Color);
    ObjectSet (fs_Name, OBJPROP_COLOR, fc_Color);
    ObjectSet (fs_Name, OBJPROP_WIDTH, fi_Size);
    //---- ������������ ��������� ������
    fGet_LastError (bs_Error, "fSet_Arrow()");
//----
    return (true);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  �����    : TarasBY, taras_bulba@tut.by                                           |
//+-----------------------------------------------------------------------------------+
//|  ������   : 23.07.2011                                                            |
//|  �������� : ��������� ������� OBJ_LABEL                                           |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fSet_Label (string fs_Name,             // ������������ �������
                 string fs_Text,             // ��� ������
                 int fi_X,                   // ���������� X
                 int fi_Y,                   // ���������� Y
                 int fi_Size = 10,           // ������ ������ �������
                 string fs_Font = "Calibri", // ����� �������
                 int fi_Corner = 0,          // ���� �������� �������
                 int fi_Angle = 0,           // ���� �� ��������� � ���������
                 color fc_CL = CLR_NONE)     // ����
{
	 int err = GetLastError();
//----
    if (StringLen (fs_Text) == 0) return (false);
    if (!fObjectFind (fs_Name)) {if (!ObjectCreate (fs_Name, OBJ_LABEL, 0, 0, 0)) return (false);}
    ObjectSet (fs_Name, OBJPROP_XDISTANCE, fi_X);
    ObjectSet (fs_Name, OBJPROP_YDISTANCE, fi_Y);
    ObjectSetText (fs_Name, fs_Text, fi_Size, fs_Font, fc_CL);
    ObjectSet (fs_Name, OBJPROP_CORNER, fi_Corner);
    if (fi_Angle > 0) {ObjectSet (fs_Name, OBJPROP_ANGLE, fi_Angle);}
    //ObjectSet (fs_Name, OBJPROP_COLOR, fc_CL);
    //---- ������������ ��������� ������
    fGet_LastError (bs_Error, "fSet_Label()");
//----
    return (true);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        ������� ����������� ������� � �������                                      |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fObjectsDeleteAll  (string fs_Pref = "",     // ������� ����� �������
                         int fi_Window = -1,      // ����� ����
                         int ti_Type = -1)        // ��� �������
                         
{
    string ls_Name; 
//----
	 for (int li_OBJ = ObjectsTotal() - 1; li_OBJ >= 0; li_OBJ--)
	 {
        ls_Name = ObjectName (li_OBJ);
        //---- ���������� "�� ���" ����
        if (fi_Window >= 0) {if (ObjectFind (ls_Name) != fi_Window) continue;}
        //---- ���������� "�� ���" ��� �������
		  if (ti_Type >= 0) {if (ObjectType (ls_Name) != ti_Type) continue;}
		  if (fs_Pref != "") {if (StringFind (ls_Name, fs_Pref) != 0) continue;}
		  ObjectDelete (ls_Name);
	 }
//----
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        ���� ����������� ������ �� �����                                           |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fObjectFind (string fs_Name)
{
    if (StringLen (fs_Name) == 0) {return (false);}
    if (ObjectFind (fs_Name) != -1) {return (true);}
//----
    string ls_Name = "";
    int err = GetLastError();
//----
    for (int i = ObjectsTotal() - 1; i >= 0; i--)
    {
        ls_Name = ObjectName (i);
        if (ls_Name == fs_Name) {return (true);}
    }
    //---- ������������ ��������� ������
    fGet_LastError (bs_Error, StringConcatenate ("fObjectFind(): Find (", fs_Name, ")"));
//----
    return (false);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//          ������� ����������� ������                                                |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fObjectDelete (string fs_Name)
{if (fObjectFind (fs_Name)) {return (ObjectDelete (fs_Name));} return (false);}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  ����� : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        �������� ����� � �������� ��������� ������                                 |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int fGet_LastError (string& Comm_Error, string Com = "")
{
    //if (bb_VirtualTrade) {return (0);}
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

