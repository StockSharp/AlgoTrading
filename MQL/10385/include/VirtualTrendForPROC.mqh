//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|                                                                  VirtualTrend.mqh |
//|                                                Copyright � Evgeniy Trofimov, 2010 |
//|                                                   http://forum.mql4.com/ru/16793/ |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|                        V I R T U A L       T R E N D                              |
//|                     Copyright: Evgeniy Trofimov, 2010                             |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+

//IIIIIIIIIIIIIIIIIII==================CONSTANS=================IIIIIIIIIIIIIIIIIIIIII+
#define  VIRT_TRADES        1    // - �������� ������ � �������
#define  VIRT_HISTORY       0    // - �������� ������� � ��������� ���������� ������
#define  MAX_POS         3000    // - ������������ ���������� ������������� �������
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
// ��� ���������� �������� ����� �������, ���:
//��������:
// + VirtualSend
// + VirtualSelect
// + VirtualClose - �������� ���������� ������ � ��������� �������
// + VirtualModify
//���������������:
// + VirtualCopyPosition - �������� ������� � ������ ������� �������
// + VirtualHighTicket - ����� �������� ������
// + VirtualFileLoad - ��������� ������ ������ �� �����
// + VirtualFileSave - ��������� ������ ������ � ����
// + VirtualUpdate - ��������� ������ � �����
// + VirtualFilter - ������ ������ �������� ������� ��������� �������
// + VirtualProfitHistory - ����������� �������� ���������� ������� �������
// + VirtualProfit - ����������� �������� ������� �������� ������� �� ������ ������ �������
// + VirtualRating - ������ �������� ���������� �� � ���������
// + VirtualExist  - ������� ������������ ��� ������� �������� 2-�� ������ ������ �� ������ �������
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|                   *****         ��������� ������         *****                    |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
extern string   SETUP_VirtualTrade = "============= ����� ��������� ������ ============";
extern bool     RatingON              = true;  // - ����������� �������� (���� ��������, �� ������ � ����� ������ ��������� � �������� ���������)
extern bool     FastTest              = true;  // - �� ����� ���� ��� ������������
//IIIIIIIIIIIIIIIIIII========���������� ���������� ������========IIIIIIIIIIIIIIIIIIIIII+
int         Virt.Count,                // - ���������� ������� � ������� (�� 0 �� MAX_POS - 1)
            Virt.Index,                // - ������� ��������� ������ � ���� �������� VirtualSelect()
            VirtBufferSize = 16,       // - ���������� ��������� � ����� ������ ���� (��������� ������)
            Virt.Ticket[MAX_POS],      // - ����� ������
            Virt.Type[MAX_POS],        // - ��� ������
            Virt.MagicNumber[MAX_POS], // - ���������� �����
            Virt.Status[MAX_POS],      // - ������ ������: 1 - �������� �������/���������� �����; 0 - �������� �������/��������� �����
            Virt.Filter[MAX_POS],
            Virt.Filter.Count,
            DAY = 86400,               // - ���� � ��������
            Err.Number;                // - ����� ������
double      Virt.Lots[MAX_POS],        // - �����
            Virt.OpenPrice[MAX_POS],   // - ���� ��������
            Virt.StopLoss[MAX_POS],
            Virt.TakeProfit[MAX_POS],
            Virt.ClosePrice[MAX_POS],  // - ���� ��������
            Virt.Swap[MAX_POS],        // - ����
            Virt.Profit[MAX_POS];      // - ������� � ����������
datetime    Virt.CloseTime[MAX_POS],   // - ����� ��������
            Virt.OpenTime[MAX_POS],    // - ����� �������
            Virt.Expiration[MAX_POS];  // - ���� ������ ����������� ������
string      Virt.Comment[MAX_POS],     // - �����������
            Virt.Symbol[MAX_POS],      // - ������
            Err.Description,           // - �������� ��������� ������
            bs_ComError = "";
//+------------------------------------------------------------------+
//|  ������� ���������� ������� ������������� ������, ���������      |
//|  ��� ��� ���� �������� �������� �� ����������� �����������      |
//|  (100% - ����� ���������� ��, 0% - �� ���� �� ����)              |
//+------------------------------------------------------------------+
double VirtualRating (int fMagic,                      // ���������� �����, �� �������� ������� ����� �������� ���� �������� ������� �� ������
                      string fSymbol,                  // ������, �� �������� ��������������� �������
                      int period,                      // ���������� ��������� ������
                      int applied_price,               // ������������ ����: 0 - � ������ ��������, 1 - � �������;
                      string filename = "virtual.csv") // ���� ������
{
    if (!RatingON) return (100);
//----
    int MagicNum[];
    double Profit[];
    //double Rating[];
    int i, j, err = GetLastError();
    bool MagicExist;
//----
    VirtualFileLoad (filename);
    if (VirtualFilter (VIRT_HISTORY, -1, -1, fSymbol) < 1) return (0);
    for (i = 0; i < Virt.Filter.Count; i++)
    {
        MagicExist = false;
        for (j = 0; j < ArraySize (MagicNum); j++)
        {
            if (MagicNum[j] == Virt.MagicNumber[Virt.Filter[i]])
            {
                MagicExist = true;
                break;
            }
        }// Next j
        if (!MagicExist)
        {
            ArrayResize (MagicNum, ArraySize (MagicNum) + 1);
            MagicNum[ArraySize (MagicNum) - 1] = Virt.MagicNumber[Virt.Filter[i]];
        }
    }// Next i
    ArrayResize (Profit, ArraySize (MagicNum));
    for (i = 0; i < ArraySize (MagicNum); i++)
    {
        Profit[i] = VirtualProfitHistory (applied_price, period, 0, -1, -1, fSymbol, MagicNum[i], true)
                  + VirtualProfit (applied_price, -1, -1, fSymbol, MagicNum[i], true);
        if (Profit[i] < 0) Profit[i] = 0;
    }// Next i
    j = ArrayMaximum (Profit); // 100%
    if (Profit[j] == 0) return (0);
    //ArrayResize (Rating, ArraySize (MagicNum));
    for (i = 0; i < ArraySize (MagicNum); i++)
    {
        if (fMagic == MagicNum[i])
        { return (100 * Profit[i] / Profit[j]); }
    }// Next i
    //---- ������������ ��������� ������
    fGetLastError (bs_ComError, "VirtualRating()");
//----
    return (0);
}// VirtualRating()
//+------------------------------------------------------------------+
//|  ������� ���������� �������� ������� �������� ������             |
//|  ����������: ���� � ��������� ������ ���� �������� �� ������     |
//|  ���� �������.                                                   |
//+------------------------------------------------------------------+
double VirtualProfit (int applied_price = 1,    // ������������ ����: 0 - � ������ ��������, 1 - � �������;
                      int fTicket = -1,
                      int fType = -1,
                      string fSymbol = "",
                      int fMagic = -1,
                      bool dh = false,          // dh = true - ������ �� ���������� ���������� ����, ��� ���������� ������������� �������
                      string fComment = "")
{
    double Profit, plus, deltaDay, ld_Point;
    int err = GetLastError();
    datetime OldDay = TimeCurrent();
//----
    if (VirtualFilter (VIRT_TRADES, fTicket, fType, fSymbol, fMagic, fComment) > 0)
    {
        for (int i = 0; i < Virt.Filter.Count; i++)
        {
            if (Virt.Type[Virt.Filter[i]] < 2)
            {
                if (Virt.OpenTime[Virt.Filter[i]] < OldDay) OldDay = Virt.OpenTime[Virt.Filter[i]];
                if (applied_price == 0)              // � ������ ��������
                {plus = Virt.Profit[Virt.Filter[i]];}
                else                                 // � �������
                {
                    ld_Point = MarketInfo (Virt.Symbol[Virt.Filter[i]], MODE_POINT);
                    if (Virt.Type[Virt.Filter[i]] == OP_BUY)
                    {plus = (Virt.ClosePrice[Virt.Filter[i]] - Virt.OpenPrice[Virt.Filter[i]]) / ld_Point;}
                    else
                    {plus = (Virt.OpenPrice[Virt.Filter[i]] - Virt.ClosePrice[Virt.Filter[i]]) / ld_Point;}
                }
                Profit = Profit + plus;
            }
        }// Next i
    }
    if (dh)
    {
        deltaDay = (TimeCurrent() - OldDay) / DAY;
        if (deltaDay < 1)
        {deltaDay = 1;}
        Profit = Profit / deltaDay;
    }
    //---- ������������ ��������� ������
    fGetLastError (bs_ComError, "VirtualProfit()");
//----
    return (Profit);
}// VirtualProfit()
//+------------------------------------------------------------------+
//|  ������� ���������� �������� ������� �������� ������             |
//|  ����������: ���� � ��������� ������ ���� �������� �� ������     |
//|  ���� �������.                                                   |
//+------------------------------------------------------------------+
double VirtualProfitHistory (int applied_price = 1,    // ������������ ����: 0 - � ������ ��������, 1 - � �������;
                             int period = 0,           // ������ ���������� ��� ���������� �������. ���� ����� 0, �� ��� ���� ������;
                             int shift = 0,            // ������ ����������� �������� �� ������� ������ (����� ������������ ��������� �������� ������ �� ��������� ���������� ������ �����).
                             int fTicket = -1,
                             int fType = -1,
                             string fSymbol = "", 
                             int fMagic = -1, 
                             bool dh = false,          // dh = true - ������ �� ���������� ���������� ����, ��� ���������� ������������� �������
                             string fComment = "")
{
    double Profit, plus, deltaDay, ld_Point;
    datetime beginDay = TimeCurrent(), endDay;
    int j, k, err = GetLastError();
//----
    if (VirtualFilter (VIRT_HISTORY, fTicket, fType, fSymbol, fMagic, fComment) > 0)
    {
        for (int i = Virt.Filter.Count - 1; i >= 0; i--)
        {
            if (Virt.Type[Virt.Filter[i]] < 2)
            {
                k++;
                if (k > shift)
                {
                    j++;
                    if (j > period && period > 0) break;
                    if (Virt.OpenTime[Virt.Filter[i]] < beginDay) beginDay = Virt.OpenTime[Virt.Filter[i]];
                    if (Virt.CloseTime[Virt.Filter[i]] > endDay) endDay = Virt.CloseTime[Virt.Filter[i]];
                    if (applied_price == 0)          // � ������ ��������
                    {plus = Virt.Profit[Virt.Filter[i]];}
                    else                             // � �������
                    {
                        ld_Point = MarketInfo (Virt.Symbol[Virt.Filter[i]], MODE_POINT);
                        if (Virt.Type[Virt.Filter[i]] == OP_BUY)
                        {plus = (Virt.ClosePrice[Virt.Filter[i]] - Virt.OpenPrice[Virt.Filter[i]]) / ld_Point;}
                        else
                        {plus = (Virt.OpenPrice[Virt.Filter[i]] - Virt.ClosePrice[Virt.Filter[i]]) / ld_Point;}
                    }
                    Profit = Profit + plus;
                }
            }
        }// Next i
    }
    if (dh)
    {
        deltaDay = (endDay - beginDay) / DAY;
        //Print (TimeToStr (endDay) + " - " + TimeToStr (beginDay) + " = " + DoubleToStr (deltaDay, 2) + " ����");
        if (deltaDay < 1)
        {deltaDay = 1;}
        Profit = Profit / deltaDay;
    }
    //---- ������������ ��������� ������
    fGetLastError (bs_ComError, "VirtualProfitHistory()");
//----
    return (Profit);
}// VirtualProfitHistory()
//+------------------------------------------------------------------+
//|  ������� �������� ����� ��� ���������� ������ � ���.             |
//|  ���������� TRUE ��� �������� ���������� �������.                |
//|  ����������: ���� � ��������� ������ ���� �������� �� ������     |
//|  ���� �������.                                                   |
//+------------------------------------------------------------------+
bool VirtualSelect (int index,              // ������� ������ ��� ����� ������ � ����������� �� ������� ���������. 
                    int select,             // ���� ������� ������. M���� ���� ����� �� ��������� �������: SELECT_BY_POS ��� SELECT_BY_TICKET
                    int pool = VIRT_TRADES) // �������� ������ ��� ������. ������������, ����� �������� select ����� SELECT_BY_POS: VIRT_TRADES ��� VIRT_HISTORY
{
//----
    if (select == SELECT_BY_POS)
    {
        if (VirtualFilter (pool) > 0)
        {
            if (index < Virt.Filter.Count)
            {
                Virt.Index = Virt.Filter[index];
                return (true);
            }
        }
    }
    else         // select == SELECT_BY_TICKET
    {
        if (VirtualFilter (-1, index) > 0)
        {
            Virt.Index = Virt.Filter[0];
            return (true);
        }
    }
//----
    return (false);
}// VirtualSelect()
//+------------------------------------------------------------------+
//|  �������� ��������� ����� �������� ������� ��� �����. �������.   |
//|  ���������� TRUE ��� �������� ���������� �������.                |
//|  ��������!!! ��� ������� �������� �� ���������� ����������       |
//|  ������� ����������� price, SL � TP!!!                           |
//+------------------------------------------------------------------+
bool VirtualModify (int ticket,
                    double price,
                    double stoploss,
                    double takeprofit,
                    datetime expiration,
                    string filename = "virtual.csv")
{
    int  i, err = GetLastError();
    bool lb_result = false;
//----
    VirtualFileLoad (filename);
    if (VirtualFilter (VIRT_TRADES, ticket) > 0)
    {
        i = Virt.Filter[0];
        if (Virt.Type[i] < 2)
        {      
            if (stoploss > 0 && Virt.StopLoss[i] != stoploss)
            {Virt.StopLoss[i] = stoploss; lb_result = true;}
            if (takeprofit > 0 && Virt.TakeProfit[i] != takeprofit)
            {Virt.TakeProfit[i] = takeprofit; lb_result = true;}
        }
        else
        {
            if (price > 0) Virt.OpenPrice[i] = price;
            if (stoploss > 0) Virt.StopLoss[i] = stoploss;
            if (takeprofit > 0) Virt.TakeProfit[i] = takeprofit;
            if (expiration > 0) Virt.Expiration[i] = expiration;
        }
        VirtualFileSave (filename);
        //---- ������������ ��������� ������
        fGetLastError (bs_ComError, "VirtualModify()");
        return (lb_result);
    }
    else
    {
        Err.Number = 102;
        Err.Description = "��� ������� �������� ������� ����� ��� ����� �� �������";
        return (false);
    }   
//----
}// VirtualModify()
//+------------------------------------------------------------------+
//|  �������� �������.                                               |
//|  ���������� TRUE ��� �������� ���������� �������.                |
//|  ���������� �� ������� �������� � ���������� Err.*               |
//+------------------------------------------------------------------+
bool VirtualClose (int ticket, string filename = "virtual.csv")
{
    double ld_Point, ld_TickValue;
    int    i, j, err = GetLastError();
//----
    VirtualFileLoad (filename);
    if (VirtualFilter (VIRT_TRADES, ticket) > 0)
    {
        i = Virt.Filter[0];
        ld_Point = MarketInfo (Virt.Symbol[i], MODE_POINT);
        ld_TickValue = MarketInfo (Virt.Symbol[i], MODE_TICKVALUE);
        if (Virt.Type[i] == OP_BUY)
        {
            Virt.CloseTime[i] = TimeCurrent();                           // ����� ��������
            Virt.ClosePrice[i] = MarketInfo (Virt.Symbol[i], MODE_BID);  // ���� ��������
            Virt.Swap[i] = MathRound ((Virt.CloseTime[i] - Virt.OpenTime[i]) / DAY) * MarketInfo (Virt.Symbol[i], MODE_SWAPLONG) * Virt.Lots[i];
            Virt.Profit[i] = (Virt.ClosePrice[i] - Virt.OpenPrice[i]) * Virt.Lots[i] * ld_TickValue / ld_Point + Virt.Swap[i];
        }
        else if (Virt.Type[i] == OP_SELL)
        {
            Virt.CloseTime[i] = TimeCurrent();                           // ����� ��������
            Virt.ClosePrice[i] = MarketInfo (Virt.Symbol[i], MODE_ASK);  // ���� ��������
            Virt.Swap[i]= MathRound ((Virt.CloseTime[i] - Virt.OpenTime[i]) / DAY) * MarketInfo (Virt.Symbol[i], MODE_SWAPSHORT) * Virt.Lots[i];
            Virt.Profit[i] = (Virt.OpenPrice[i] - Virt.ClosePrice[i]) * Virt.Lots[i] * ld_TickValue / ld_Point + Virt.Swap[i];
        }
        else if (Virt.Type[i] > 1 && Virt.Type[i] < 6)
        {
            Virt.CloseTime[i] = TimeCurrent();                           // ����� ������
            Virt.Comment[i] = Virt.Comment[i] + "[canceled]";
        }
        for (j = i - 1; j >= 0; j--)
        {if (Virt.Status[j] == 0) break;}// Next j
        Virt.Status[i] = 0;                  
        if (j < i - 1)
        {
            j++;
            VirtualCopyPosition (j, MAX_POS - 1);
            VirtualCopyPosition (i, j);
            VirtualCopyPosition (MAX_POS - 1, i);
        }
        VirtualFileSave (filename);
        //---- ������������ ��������� ������
        fGetLastError (bs_ComError, "VirtualClose()");
        return (true);
    }
    else
    {
        Err.Number = 101;
        Err.Description = "��� ������� ������� ������� ����� ��� ����� �� �������";
        return (false);
   }
//----
}// VirtualClose()
//+------------------------------------------------------------------+
//|  ������� ������ ������ �������� ������� ��������� �������,      |
//|  ��������������� ������������ ���������� �������                 |
//|  � ���������� ������ ������.                                     |
//|  ����������: ���� � ��������� ������ ���� �������� �� ������     |
//|  ���� �������.                                                   |
//+------------------------------------------------------------------+
int VirtualFilter (int fStatus = -1,
                   int fTicket = -1, 
                   int fType = -1, 
                   string fSymbol = "", 
                   int fMagic = -1, 
                   string fComment = "")
{
    int err = GetLastError();
//----
    Virt.Filter.Count = 0;
    for (int i = 0; i < Virt.Count; i++)
    {
        if (fTicket == -1 || Virt.Ticket[i] == fTicket)
        {
            if (fType == -1 || Virt.Type[i] == fType)
            {
                if (fSymbol == "" || Virt.Symbol[i] == fSymbol)
                {
                    if (fComment == "" || StringFind (Virt.Comment[i], fComment) > -1)
                    {
                        if (fMagic == -1 || Virt.MagicNumber[i] == fMagic)
                        {
                            if (fStatus == -1 || Virt.Status[i] == fStatus)
                            {
                                Virt.Filter[Virt.Filter.Count] = i;
                                Virt.Filter.Count++;
                            }
                        }
                    }
                }
            }
        }
    }// Next i
    //---- ������������ ��������� ������
    fGetLastError (bs_ComError, "VirtualFilter()");
//----
    return (Virt.Filter.Count);
}// VirtualFilter()
//+------------------------------------------------------------------+
//|  ��������� ���������� ����� ������ � ������������ �              |
//|  ������������� ����������� �� �����. ����� ����������:           |
//|  + ���������� ������;                                            |
//|  + ����������� �������� ������ �� ����������� ������� SL � TP;   |
//|  + ���������� ��� �������� �������� �������, ������ ��������;    |
//|  + �������� ���������� �������;                                  |
//|  + ���������� �� ����������� ���������� �������;                 |
//+------------------------------------------------------------------+
void VirtualUpdate (string filename = "virtual.csv")
{
    double ld_Point, ld_TickValue;
    bool   is_changed, is_closed;
    int    i, j, err = GetLastError();
//----
    VirtualFileLoad (filename);
    for (i = Virt.Count - 1; i >= 0; i--)
    {
        is_closed = false;
        if (Virt.Status[i] == 1)
        {
            is_changed = true;
            ld_Point = MarketInfo (Virt.Symbol[i], MODE_POINT);
            ld_TickValue = MarketInfo (Virt.Symbol[i], MODE_TICKVALUE);
            switch (Virt.Type[i])
            {
                case OP_BUY:
                    Virt.CloseTime[i] = TimeCurrent();                             // ����� ��������
                    Virt.ClosePrice[i] = MarketInfo (Virt.Symbol[i], MODE_BID);    // ���� ��������
                    Virt.Swap[i] = MathRound ((Virt.CloseTime[i] - Virt.OpenTime[i]) / DAY) * MarketInfo (Virt.Symbol[i], MODE_SWAPLONG) * Virt.Lots[i];
                    Virt.Profit[i] = (Virt.ClosePrice[i] - Virt.OpenPrice[i]) * Virt.Lots[i] * ld_TickValue / ld_Point + Virt.Swap[i];
                    if (Virt.TakeProfit[i] > 0)
                    {
                        if (MarketInfo (Virt.Symbol[i], MODE_BID) >= Virt.TakeProfit[i])
                        {
                            Virt.ClosePrice[i] = Virt.TakeProfit[i];               // ���� ��������
                            Virt.Profit[i] = (Virt.ClosePrice[i] - Virt.OpenPrice[i]) * Virt.Lots[i] * ld_TickValue / ld_Point + Virt.Swap[i];
                            Virt.Comment[i] = Virt.Comment[i] + "[tp]";
                            is_closed = true;
                        }
                    }// End TakeProfit
                    if (Virt.StopLoss[i] > 0)
                    {
                        if (MarketInfo (Virt.Symbol[i], MODE_BID) <= Virt.StopLoss[i])
                        {
                            Virt.ClosePrice[i] = Virt.StopLoss[i];                 // ���� ��������
                            Virt.Profit[i] = (Virt.ClosePrice[i] - Virt.OpenPrice[i]) * Virt.Lots[i] * ld_TickValue / ld_Point + Virt.Swap[i];
                            Virt.Comment[i] = Virt.Comment[i] + "[sl]";
                            is_closed = true;
                        }
                    }// End StopLoss
                    break;
                case OP_SELL:
                    Virt.CloseTime[i] = TimeCurrent();                             // ����� ��������
                    Virt.ClosePrice[i] = MarketInfo (Virt.Symbol[i], MODE_ASK);    // ���� ��������
                    Virt.Swap[i] = MathRound ((Virt.CloseTime[i] - Virt.OpenTime[i]) / DAY) * MarketInfo (Virt.Symbol[i], MODE_SWAPSHORT) * Virt.Lots[i];
                    Virt.Profit[i] = (Virt.OpenPrice[i] - Virt.ClosePrice[i]) * Virt.Lots[i] * ld_TickValue / ld_Point + Virt.Swap[i];
                    if (Virt.TakeProfit[i] > 0)
                    {
                        if (MarketInfo (Virt.Symbol[i], MODE_ASK) <= Virt.TakeProfit[i])
                        {
                            Virt.ClosePrice[i] = Virt.TakeProfit[i];               // ���� ��������
                            Virt.Profit[i] = (Virt.OpenPrice[i] - Virt.ClosePrice[i]) * Virt.Lots[i] * ld_TickValue / ld_Point + Virt.Swap[i];
                            Virt.Comment[i] = Virt.Comment[i] + "[tp]";
                            is_closed = true;
                        }
                    }// End TakeProfit
                    if (Virt.StopLoss[i] > 0)
                    {
                        if (MarketInfo (Virt.Symbol[i], MODE_ASK) >= Virt.StopLoss[i])
                        {
                            Virt.ClosePrice[i] = Virt.StopLoss[i];                 // ���� ��������
                            Virt.Profit[i] = (Virt.OpenPrice[i] - Virt.ClosePrice[i]) * Virt.Lots[i] * ld_TickValue / ld_Point + Virt.Swap[i];
                            Virt.Comment[i] = Virt.Comment[i] + "[sl]";
                            is_closed=true;
                        }
                    }// End StopLoss
                    break;
                case OP_BUYLIMIT:
                    if (MarketInfo (Virt.Symbol[i], MODE_ASK) <= Virt.OpenPrice[i])
                    {Virt.Type[i] = OP_BUY;}
                    break;
                case OP_SELLLIMIT:
                    if (MarketInfo (Virt.Symbol[i], MODE_BID) >= Virt.OpenPrice[i])
                    {Virt.Type[i] = OP_SELL;}
                    break;
                case OP_BUYSTOP:
                    if (MarketInfo (Virt.Symbol[i], MODE_ASK) >= Virt.OpenPrice[i])
                    {Virt.Type[i] = OP_BUY;}
                    break;
                case OP_SELLSTOP:
                    if (MarketInfo (Virt.Symbol[i], MODE_BID) <= Virt.OpenPrice[i])
                    {Virt.Type[i] = OP_SELL;}
                    break;
            }// End switch
            if (Virt.Type[i] > 1 && Virt.Type[i] < 6)
            {
                if (Virt.Expiration[i] > 0)
                {
                    if (TimeCurrent() > Virt.Expiration[i])
                    {
                        Virt.Comment[i] = Virt.Comment[i] + "[expiration]";
                        is_closed = true;
                    }
                }
            }
            if (is_closed)
            {
                for (j = i; j >= 0; j--)
                {if (Virt.Status[j] == 0) break;}// Next j
                Virt.Status[i] = 0;                  
                if (j < i - 1)
                {
                    j++;
                    VirtualCopyPosition (j, MAX_POS - 1);
                    VirtualCopyPosition (i, j);
                    VirtualCopyPosition (MAX_POS - 1, i);
                }
            }// End if (is_closed)
        }
        else {break;}// End if (Virt.Status[i]==1)
    }// Next i
    if (is_changed) VirtualFileSave (filename);
    //---- ������������ ��������� ������
    fGetLastError (bs_ComError, "VirtualUpdate()");
//----
}// VirtualUpdate()
//+------------------------------------------------------------------+
//|  ��������� ���������� ������� � ������ ������ �������            |
//+------------------------------------------------------------------+
void VirtualCopyPosition (int FirstPosition, int SecondPosition)
{
    Virt.Ticket[SecondPosition] = Virt.Ticket[FirstPosition];           // - ����� ������
    Virt.OpenTime[SecondPosition] = Virt.OpenTime[FirstPosition];       // - ����� �������
    Virt.Type[SecondPosition] = Virt.Type[FirstPosition];               // - ��� ������
    Virt.Lots[SecondPosition] = Virt.Lots[FirstPosition];               // - �����
    Virt.Symbol[SecondPosition] = Virt.Symbol[FirstPosition];           // - ������
    Virt.OpenPrice[SecondPosition] = Virt.OpenPrice[FirstPosition];     // - ���� ��������
    Virt.StopLoss[SecondPosition] = Virt.StopLoss[FirstPosition];
    Virt.TakeProfit[SecondPosition] = Virt.TakeProfit[FirstPosition];
    Virt.CloseTime[SecondPosition] = Virt.CloseTime[FirstPosition];     // - ����� ��������
    Virt.ClosePrice[SecondPosition] = Virt.ClosePrice[FirstPosition];   // - ���� ��������
    Virt.Swap[SecondPosition] = Virt.Swap[FirstPosition];               // - ����
    Virt.Profit[SecondPosition] = Virt.Profit[FirstPosition];           // - ������� � ����������
    Virt.Comment[SecondPosition] = Virt.Comment[FirstPosition];         // - �����������
    Virt.MagicNumber[SecondPosition] = Virt.MagicNumber[FirstPosition]; // - ���������� �����
    Virt.Expiration[SecondPosition] = Virt.Expiration[FirstPosition];   // - ���� ������ ����������� ������
    Virt.Status[SecondPosition] = Virt.Status[FirstPosition];           // - ������ ������: 1 - �������� �������/���������� �����; 0 - �������� �������/��������� �����
}// VirtualCopyPosition()
//+------------------------------------------------------------------+
//|  �������� �������, ������������ ��� �������� ������� ���         |
//|  ��������� ����������� ������.                                   |
//|  ���������� ����� ������, ������� �������� ������ ��������       |
//|  �������� ��� -1 � ������ �������.                               |
//+------------------------------------------------------------------+
int VirtualSend (string symbol,                    // ������������ ����������� �����������, � ������� ���������� �������� ��������. 
                 int cmd,                          // �������� ��������. ����� ���� ����� �� �������� �������� ��������. 
                 double volume,                    // ���������� �����.
                 double price,                     // ���� ��������. 
                 int slippage,                     // ����������� ���������� ���������� ���� ��� �������� ������� � ������� (������� �� ������� ��� �������).
                 double stoploss,                  // ���� �������� ������� ��� ���������� ������ ����������� (0 � ������ ���������� ������ �����������).
                 double takeprofit,                // ���� �������� ������� ��� ���������� ������ ������������ (0 � ������ ���������� ������ ������������).
                 string comment = "",              // ����� ����������� ������. ��������� ����� ����������� ����� ���� �������� �������� ��������.
                 int magic = 0,                    // ���������� ����� ������. ����� �������������� ��� ������������ ������������� �������������.
                 datetime expiration = 0,          // ���� ��������� ����������� ������.
                 string filename = "virtual.csv")  // ��� ����� ����������� ������ �� �������� TerminalPath()+"\experts\files"
{
    double ld_Point = MarketInfo (symbol, MODE_POINT),
           ld_StopLevel = MarketInfo (symbol, MODE_STOPLEVEL),
           lda_Price[2];
    int err = GetLastError();
//----
    lda_Price[0] = MarketInfo (symbol, MODE_ASK);
    lda_Price[1] = MarketInfo (symbol, MODE_BID);
    //---- ���� ��������:
    if (cmd == OP_BUY)
    {
        //---- ���� �������� ������� ������ ���� ����� Ask +- sleeppage
        if ((price > lda_Price[cmd] + slippage * ld_Point)
        || (price < lda_Price[cmd] - slippage * ld_Point))
        {
            Err.Number = 1;
            Err.Description = "���� �������� ������� ������ �� �����";
            return (-1);
        }
    }
    else if (cmd == OP_SELL)
    {
        if ((price > lda_Price[cmd] + slippage * ld_Point)
        || (price < lda_Price[cmd] - slippage * ld_Point))
        {
            Err.Number = 1;
            Err.Description = "���� �������� ������� ������ �� �����";
            return (-1);
        }
    }
    else if (cmd == OP_BUYSTOP)
    {
        if (price <= lda_Price[0] + ld_StopLevel * ld_Point)
        {
            Err.Number = 2;
            Err.Description = "���� �������� ����������� ������ ������� ������ � �����";
            return (-1);
        }
    }
    else if (cmd == OP_SELLSTOP)
    {
        if (price >= lda_Price[1] - ld_StopLevel * ld_Point)
        {
            Err.Number = 2;
            Err.Description = "���� �������� ����������� ������ ������� ������ � �����";
            return (-1);
        }
    }
    else if (cmd == OP_BUYLIMIT)
    {
        if (price >= lda_Price[0] - ld_StopLevel * ld_Point)
        {
            Err.Number = 2;
            Err.Description = "���� �������� ����������� ������ ������� ������ � �����";
            return (-1);
        }
    }
    else if (cmd == OP_SELLLIMIT)
    {
        if (price <= lda_Price[1] + ld_StopLevel * ld_Point)
        {
            Err.Number = 2;
            Err.Description = "���� �������� ����������� ������ ������� ������ � �����";
            return (-1);
        }
    }
    if (stoploss != 0.0)
    {
        if ((cmd == OP_BUY) || (cmd == OP_BUYSTOP) || (cmd == OP_BUYLIMIT))         // �������
        {
            if (price - stoploss <= ld_StopLevel * ld_Point)
            { 
                Err.Number = 3;
                Err.Description = "������� StopLoss ��� TakeProfit ������� ������ � ����";
                return (-1);
            }
        }
        else if ((cmd == OP_SELL) || (cmd == OP_SELLSTOP) || (cmd == OP_SELLLIMIT)) // �������
        {
            if (stoploss - price <= ld_StopLevel * ld_Point)
            { 
                Err.Number = 3;
                Err.Description = "������� StopLoss ��� TakeProfit ������� ������ � ����";
                return (-1);
            }
        }
    }
    if (takeprofit != 0.0)
    {
        if ((cmd == OP_BUY) || (cmd == OP_BUYSTOP) || (cmd == OP_BUYLIMIT))         // �������
        {
            if (takeprofit - price <= ld_StopLevel * ld_Point)
            {
                Err.Number = 3;
                Err.Description = "������� StopLoss ��� TakeProfit ������� ������ � ����";
                return (-1);
            }
        }
        else if ((cmd == OP_SELL) || (cmd == OP_SELLSTOP) || (cmd == OP_SELLLIMIT)) // �������
        {
            if (price - takeprofit <= ld_StopLevel * ld_Point)
            {
                Err.Number = 3;
                Err.Description = "������� StopLoss ��� TakeProfit ������� ������ � ����";
                return (-1);
            }
        }      
    }
    if ((volume < MarketInfo (symbol, MODE_MINLOT))
    || (volume > MarketInfo (symbol, MODE_MAXLOT)))
    {
        Err.Number = 4;
        Err.Description = "������������ ���";
        return (-1);
    }
    if (expiration <= TimeCurrent() && expiration != 0)
    {
        Err.Number = 5;
        Err.Description = "������������ ���� ����������";
        return (-1);
    }
    //---- ����� ����� ��������
    //---- ������ �� ������������ �������
    int i, j, k;
    VirtualFileLoad (filename);
   
    if (Virt.Count > MAX_POS - 2)
    {
        //---- ������� ��� ��������� ���������� ������
        for (i = 0; i < Virt.Count; i++)
        {
            if ((Virt.Type[i] > 1) && (Virt.Status[i] == 0))
            {
                //---- ��� ������, ������������� ���� ����� ����������� ������ ���������� �� ������� �����:
                for (j = i; j < Virt.Count - 1; j++)
                {
                    Virt.Ticket[j] = Virt.Ticket[j+1];
                    Virt.OpenTime[j] = Virt.OpenTime[j+1];
                    Virt.Type[j] = Virt.Type[j+1];
                    Virt.Lots[j] = Virt.Lots[j+1];
                    Virt.Symbol[j] = Virt.Symbol[j+1];
                    Virt.OpenPrice[j] = Virt.OpenPrice[j+1];
                    Virt.StopLoss[j] = Virt.StopLoss[j+1];
                    Virt.TakeProfit[j] = Virt.TakeProfit[j+1];
                    Virt.CloseTime[j] = Virt.CloseTime[j+1];
                    Virt.ClosePrice[j] = Virt.ClosePrice[j+1];
                    Virt.Swap[j] = Virt.Swap[j+1];
                    Virt.Profit[j] = Virt.Profit[j+1];
                    Virt.Comment[j] = Virt.Comment[j+1];
                    Virt.MagicNumber[j] = Virt.MagicNumber[j+1];
                    Virt.Expiration[j] = Virt.Expiration[j+1];
                    Virt.Status[j] = Virt.Status[j+1];
                }// Next j
                Virt.Count--;
            }
        }// Next i
        //---- ����� ������ �������� ������
        for (i = 0; i < Virt.Count; i++)
        {
            if ((Virt.Type[i] < 2) && (Virt.Status[i] == 0))
            {break;}
        }// Next i
        if (i == Virt.Count)
        {
            Err.Number = 402;
            Err.Description = "���������� �������� ������ ��������� ��������������������� ��������! ���������� ���������� �������� �������.";
            return (-1);
        }
        else
        {
            //---- ����� ������ �������� ������
            for (j = i + 1; j < Virt.Count; j++)
            {if (Virt.Status[j] == 0) {break;}}// Next j
            if (j == Virt.Count)
            {
                Err.Number = 402;
                Err.Description = "���������� �������� ������ ��������� ��������������������� ��������! ���������� ���������� �������� �������.";
                return (-1);
            }
            else
            {
                //---- � ������ �������� ������ ������ ������ � ������������ 2 ������ (������ � ������):
                Virt.Ticket[i] = Virt.Ticket[j];
                Virt.OpenTime[i] = Virt.OpenTime[j];
                Virt.Type[i] = -1;
                Virt.Lots[i] = Virt.Lots[i] + Virt.Lots[j];
                Virt.Symbol[i] = "";
                Virt.OpenPrice[i] = 0;
                Virt.StopLoss[i] = 0;
                Virt.TakeProfit[i] = 0;
                Virt.CloseTime[i] = Virt.CloseTime[j];
                Virt.ClosePrice[i] = 0;
                Virt.Swap[i] = Virt.Swap[i] + Virt.Swap[j];
                Virt.Profit[i] = Virt.Profit[i] + Virt.Profit[j];
                Virt.Comment[i] = "Archive";
                Virt.MagicNumber[i] = Virt.MagicNumber[j];
                Virt.Expiration[i] = Virt.Expiration[j];
                //---- ��� ������, ������������� ���� ������ �������� ������ ���������� �� ������� �����:
                for (k = j; k < Virt.Count - 1; k++)
                {
                    Virt.Ticket[k] = Virt.Ticket[k+1];
                    Virt.OpenTime[k] = Virt.OpenTime[k+1];
                    Virt.Type[k] = Virt.Type[k+1];
                    Virt.Lots[k] = Virt.Lots[k+1];
                    Virt.Symbol[k] = Virt.Symbol[k+1];
                    Virt.OpenPrice[k] = Virt.OpenPrice[k+1];
                    Virt.StopLoss[k] = Virt.StopLoss[k+1];
                    Virt.TakeProfit[k] = Virt.TakeProfit[k+1];
                    Virt.CloseTime[k] = Virt.CloseTime[k+1];
                    Virt.ClosePrice[k] = Virt.ClosePrice[k+1];
                    Virt.Swap[k] = Virt.Swap[k+1];
                    Virt.Profit[k] = Virt.Profit[k+1];
                    Virt.Comment[k] = Virt.Comment[k+1];
                    Virt.MagicNumber[k] = Virt.MagicNumber[k+1];
                    Virt.Expiration[k] = Virt.Expiration[k+1];
                    Virt.Status[k] = Virt.Status[k+1];
                }// Next k
                Virt.Count--;
            }
        }
    }
    //---- ����� ������ �� ������������ �������
    //---- ���������� ����� ������� � ������
    Virt.Count++;
    Virt.Ticket[Virt.Count-1] = VirtualHighTicket() + 1;
    Virt.OpenTime[Virt.Count-1] = TimeCurrent();
    Virt.Type[Virt.Count-1] = cmd;
    Virt.Lots[Virt.Count-1] = volume;
    Virt.Symbol[Virt.Count-1] = symbol;
    Virt.OpenPrice[Virt.Count-1] = price;
    Virt.StopLoss[Virt.Count-1] = stoploss;
    Virt.TakeProfit[Virt.Count-1] = takeprofit;
    Virt.Comment[Virt.Count-1] = comment;
    Virt.MagicNumber[Virt.Count-1] = magic;
    Virt.Expiration[Virt.Count-1] = expiration;
    Virt.Status[Virt.Count-1] = 1;
    //---- ���������� ���������
    VirtualFileSave (filename);
    //---- ������������ ��������� ������
    fGetLastError (bs_ComError, "VirtualSend()");
//----
    return (Virt.Ticket[Virt.Count-1]);
}// VirtualSend()
//+------------------------------------------------------------------+
//|  ����� �������� ������                                           |
//+------------------------------------------------------------------+
int VirtualHighTicket()
{
    int i, j;
//----
    for (i = 0; i < Virt.Count; i++)
    {
        if (Virt.Ticket[i] > j)
        {j = Virt.Ticket[i];}
    }// Next i
//----
    return (j);
}// VirtualHighTicket()
//+------------------------------------------------------------------+
//|  ������� ��������� �������� �� ����� ������ � ������ ������      |
//|  � ���������� ���������� ����������� �����.                      |
//+------------------------------------------------------------------+
int VirtualFileLoad (string file)
{
    if (FastTest)
    {if (IsTesting()) return (0);}
//----
    int k, Count, err = GetLastError();
    string buffer[];
    ArrayResize (buffer, VirtBufferSize * MAX_POS);
    int handle = FileOpen (file, FILE_CSV|FILE_READ, ';');
//----
    if (handle < 1)
    {
        Err.Number = 401;
        Err.Description = "���� ������ �� ���������";
        return (-1);
    }
    else
    {
        Count = 0;
        //---- ��������� ����
        while (!FileIsEnding (handle))
        {
            buffer[Count] = FileReadString (handle);
            Count++;
        }
        Count--;
        FileClose (handle);
        //---- ���������� �������
        Virt.Count = 0;
        k = VirtBufferSize;
        //if (fCCV_D (Count / VirtBufferSize - 1, 2))
        //Print ("��������� ", Count / VirtBufferSize - 1, " �����");
        while (Virt.Count < Count / VirtBufferSize - 1)
        {
            Virt.Ticket[Virt.Count] = StrToInteger (buffer[k]);         // ����� ������
            Virt.OpenTime[Virt.Count] = StrToTime (buffer[k+1]);        // ����� �������
            Virt.Type[Virt.Count] = StrToInteger (buffer[k+2]);         // ��� ������
            Virt.Lots[Virt.Count] = StrToDouble (buffer[k+3]);          // �����
            Virt.Symbol[Virt.Count] = buffer[k+4];                      // ������
            Virt.OpenPrice[Virt.Count] = StrToDouble (buffer[k+5]);     // ���� ��������
            Virt.StopLoss[Virt.Count] = StrToDouble (buffer[k+6]);
            Virt.TakeProfit[Virt.Count] = StrToDouble (buffer[k+7]);
            Virt.CloseTime[Virt.Count] = StrToTime (buffer[k+8]);       // ����� ��������
            Virt.ClosePrice[Virt.Count] = StrToDouble (buffer[k+9]);    // ���� ��������
            Virt.Swap[Virt.Count] = StrToDouble (buffer[k+10]);         // ����
            Virt.Profit[Virt.Count] = StrToDouble (buffer[k+11]);       // ������� � ����������
            Virt.Comment[Virt.Count] = buffer[k+12];                    // �����������
            Virt.MagicNumber[Virt.Count] = StrToInteger (buffer[k+13]); // ���������� �����
            Virt.Expiration[Virt.Count] = StrToTime (buffer[k+14]);     // ���� ������ ����������� ������
            Virt.Status[Virt.Count] = StrToInteger (buffer[k+15]);      // ������ ������: 1 - ������; 0 - ������
            k += VirtBufferSize;
            Virt.Count++;
        }
        //---- ������������ ��������� ������
        fGetLastError (bs_ComError, "VirtualFileLoad()");
        return (Virt.Count);
    }
//----
}// VirtualFileLoad()
//+------------------------------------------------------------------+
//|  ��������� ���������� ������� ������ � ��������� ����.           |
//+------------------------------------------------------------------+
void VirtualFileSave (string file)
{
    if (FastTest)
    {if (IsTesting()) return (0);}
//----
    int Count = 0, err = GetLastError();
    int handle = FileOpen (file, FILE_CSV|FILE_WRITE, ';');
//----
    FileWrite (handle, "����� ������", "����� �������", "���", "�����", "������", "���� ��������", "S/L", "T/P",
    "����� ��������", "���� ��������", "����", "�������", "�����������", "���������� �����", "����������", "������");
    //---- ���������� � ����
    while (Count < Virt.Count)
    {
        //----�������� ������� ��������� �������������� ������
        FileWrite (handle,
        Virt.Ticket[Count],
        TimeToStr (Virt.OpenTime[Count]),
        Virt.Type[Count],
        Virt.Lots[Count],
        Virt.Symbol[Count],
        Virt.OpenPrice[Count],
        Virt.StopLoss[Count],
        Virt.TakeProfit[Count],
        TimeToStr (Virt.CloseTime[Count]),
        Virt.ClosePrice[Count],
        Virt.Swap[Count],
        Virt.Profit[Count],
        Virt.Comment[Count],
        Virt.MagicNumber[Count],
        TimeToStr (Virt.Expiration[Count]),
        Virt.Status[Count]);
        Count++;
    }
    FileClose (handle);   
    //if (fCCV_D (Count, 1))
    //Print ("�������� ", Count, " �����");   
    //---- ������������ ��������� ������
    fGetLastError (bs_ComError, "VirtualFileSave()");
//----
}// VirtualFileSave()
//+------------------------------------------------------------------+
bool VirtualExist (datetime TimeOpenCandle, int fMagic = 0)
{
//----
    VirtualFilter (VIRT_TRADES, -1, -1, Symbol(), fMagic);
    for (int i = 0; i < Virt.Filter.Count; i++)
    {
        if (Virt.OpenTime[Virt.Filter[i]] >= TimeOpenCandle)
        {return (true);}
    }// Next i
//----
   return (false);
}// VirtualExist()
//+------------------------------------------------------------------+

