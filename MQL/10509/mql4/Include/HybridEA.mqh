//ACCESS
string TimeAccess, FunctionAccess;
//TIME
int PREV_MINUTE, CURR_MINUTE, CURR_SECONDS;
//POSITIONS
int CurrentPos, PosValue;
double Pos[200][200];
//REAL
int BID=1, ASK=2, CLOSE=3, VALUE_BID=4, VALUE_ASK=5, VALUE_CLOSE=6, TOTAL_BID=7, TOTAL_ASK=8, TOTAL_CLOSE=9;
double Value[100], Total[100];
//RVI
int RVI_MAIN=10, RVI_SIGNAL=11, RVI_DIFFERENTION=12;
double RVIRozdiel, RVIMain, RVISignal;
double RVI_MID_DIFFERENTION, RVI_BORDER_DIFFERENTION;
//CHANNEL SCALPER
int i, SignalBuy, SignalSell;
double Direction[999], Up[999], Dn[999];
//OPEN
int TradeSystem=0, BUY=1, SELL=2;
//OPEN,CLOSE
int SlipPage=1;
int Magic=0;
int Expiration=0;
string Commentation="Opened Position";





