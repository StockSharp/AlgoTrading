//+------------------------------------------------------------------+
//|                                                   TradePanel.mqh |
//|                                                  2010, KTS Group |
//|                                                                  |
//+------------------------------------------------------------------+
#property copyright "2010, KTS Group"



#define  DELAY_BTN_PN          (15) //(s), check state "NO Prices"
#define  DELAY_TRADE_CONFIRM   (60) //(s), check state "NON TRADE"
#define  DELAY_CAPTION_CLEAR   (15) //(s), clear statusbar caption interval(if caption isn't " ")

#define  DEFAULT_CMDBTN_SMALL_WIDTH (7)      //default small digit width
#define  DEFAULT_CMDBTN_LARGE_WIDTH (14)     //default large digit width
#define  DEFAULT_CMDBTN_DELIM_WIDTH (4)      //default delimiter width

//---disable flag for CMD buttons
#define  CMD_SELL                   (0)      
#define  CMD_BUY                    (1)
#define  CMD_ALL                    (2)

#include <Controls\Forms.mqh>
#include "Resources.mqh"
#include <Trade\SymbolInfo.mqh>
#include <KTS\TM.mqh>
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
class CTradePanel:public CForm
  {
private:
   int               m_Top;
   int               m_Left;
   uchar             m_SmallDigitWidth;
   uchar             m_DilimiterWidth;
   uchar             m_LargeDigitWidth;
   uchar             m_LastAskChange;   // Up,Down,None
   uchar             m_LastBidChange;
   ushort            m_Timer_pn;        // "NO_Prices" 
   ushort            m_Timer_nt;        // "NON_TRADE"
   ushort            m_Timer_cl;        // "Statusbar caption clear"
   char              m_TradeDisabled_Reason;
   int               m_SetupFile_Handle;
private:
   double            m_LastAsk;
   double            m_LastBid;
   int               m_LastSpread;
   uchar             m_digits;
   double            m_point;
   int               m_TPPoints;
   int               m_SLPoints;
   int               m_LastTPPoints;
   int               m_LastSLPoints;
   double            m_TPPrice;
   double            m_SLPrice;
   double            m_LastSLPrice;
   double            m_LastTPPrice;
   double            m_Lot;
   double            m_SwapShort;
   double            m_SwapLong;
   int               m_StopsLevel;
   bool              m_Non_Trade;
   bool              m_Non_Trade_Last;
   bool              m_StatusCaption;   // True,if statusbar caption isn't null;
   ulong             m_Magic;
   ushort            m_deviation;
   string            m_Comm;
   string            m_Name;
   bool              m_FPosition;
   bool              m_NeedModify;
   //---
   int               m_days;              // depth of trade history in days
   datetime          m_start;             // start date for trade history in cache
   datetime          m_end;               // end date for trade history in cache 
   int               m_positions;         // number of open positions
   int               m_deals;             // number of trades in the trade history cache
   bool              m_started;           // flag of the counter initialization
   int               m_GetLastError;      // Contains the error code
   ulong             m_LastOrderTicket;   // The variable stores the ticket of the last order received for processing
private:
   CWAVbox          *C_WAVbox;
   CWAVbox          *E_WAVbox;
   CSymbolInfo      *SymbolInfo;
   CImage           *bkgTrade;
   CButton          *btnDrag;
   CTradeButton     *btnSell;
   CTradeButton     *btnBuy;
   CSpinEdit        *seVolume;
   CTabControl      *TabControl;
   CTabSheet        *TradeSheet;
   CTabSheet        *SetupSheet;
   CTabSheet        *OptionsSheet;
   CLabel           *lblSpread;
   CLabel           *lblSwapShort;
   CLabel           *lblSwapLong;
   CLabel           *lblSymb;
   CLabel           *lblTickTime;
   CLabel           *lblStatus;
   CCheckbox        *chbTake;
   CCheckbox        *chbLoss;
   CCheckbox        *chbWavs;
   CCheckbox        *chbDev;
   CCheckbox        *chbMagic;
   CSpinEdit        *seTake;
   CSpinEdit        *seLoss;
   CSpinEdit        *seDeviation;
   CImage           *bmpBevel;
   CImage           *bmpComm;
   CEdit            *edMagic;
   CEdit            *edComment;
   CRadioGroup      *RadioGroup;
   CRadioButton     *rbPips;
   CRadioButton     *rbPrice;
   CResources       *Resources;
   CTradingManager *TradingManager;
private:
   void              Non_Trade(bool f_Confirm,uchar f_CMDFlag=CMD_ALL);
   bool              TradeAllow();
   void              SetStatusCaption(string f_Caption);
   ulong             GenerateMagic();
   void              InitCounters();
   void              TradeDispatcher();
   void              CheckStartDateInTradeHistory();
   bool              SetStopsInPips();
protected:
   Align             TradeFormAlign;
   bool              CreatePanelControls();

public:
                     CTradePanel(void);
                    ~CTradePanel(void);
   bool              CreateForm(string f_FormName,CResources *p_Resources);
   void              OnInit(CSymbolInfo *p_Info);
   void              OnTick();
   void              OnTimer();
   void              OnTrade();
   bool              OnDeinit(const int f_Reason=0) {return(false);}
   void              OnChartEvent(const int id,const long &lparam,const double &dparam,const string &sparam);
   void              SetTradingManager(CTradingManager *p_tm);
   void              ButtonClickEvent(CButton *p_Obj,string f_ObjName);
   void              CMDButtonClickEvent(CButton *p_Obj,string f_ObjName);
   void              SpinBtnsClickEvent(CSpinButton *p_Obj);
   void              CheckBoxClickEvent(CCheckbox *p_Obj,string f_ObjName);
   void              TabHeaderClickEvent(CTabHeader *p_Obj,string f_ObjName);
   void              RadioBtnClickEvent(CRadioButton *p_Obj,string f_ObjName);
   void              OnTickEvent();
   void              OnEnable();
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CTradePanel::CTradePanel(void)
  {
   m_SmallDigitWidth=DEFAULT_CMDBTN_SMALL_WIDTH;
   m_DilimiterWidth=DEFAULT_CMDBTN_DELIM_WIDTH;
   m_LargeDigitWidth=DEFAULT_CMDBTN_LARGE_WIDTH;
   C_WAVbox=NULL;
   E_WAVbox=NULL;
   m_LastAsk  =0;
   m_LastBid  =0;
   m_point    =0;
   m_LastAskChange =PRICE_CHANGE_NONE;
   m_LastBidChange =PRICE_CHANGE_NONE;
   SymbolInfo  =NULL;
   bkgTrade    =NULL;
   btnSell     =NULL;
   btnBuy      =NULL;
   btnDrag     =NULL;
   seVolume    =NULL;
   lblSpread   =NULL;
   lblSwapShort=NULL;
   lblSwapLong =NULL;
   lblTickTime =NULL;
   lblSymb     =NULL;
   lblStatus   =NULL;
   chbTake     =NULL;
   chbLoss     =NULL;
   chbWavs     =NULL;
   chbDev      =NULL;
   chbMagic    =NULL;
   seTake      =NULL;
   seLoss      =NULL;
   seDeviation =NULL;
   edMagic     =NULL;
   bmpBevel    =NULL;
   bmpComm     =NULL;
   RadioGroup  =NULL;
   rbPips      =NULL;
   rbPrice     =NULL;
   TradingManager=NULL;
   TabControl  =NULL;
   TradeSheet  =NULL;
   SetupSheet  =NULL;
   OptionsSheet=NULL;
   Resources   =NULL;
   m_Timer_pn=0;
   m_Timer_nt=0;
   m_Timer_cl=0;
   m_digits=0;
   m_LastSpread=0;
   m_TPPoints=0;
   m_SLPoints=0;
   m_LastTPPoints=0;
   m_LastSLPoints=0;
   m_TPPrice=0;
   m_SLPrice=0;
   m_LastSLPrice=0;
   m_LastTPPrice=0;
   m_Lot=0.0;
   m_SwapShort=0.0;
   m_SwapLong=0.0;
   m_StopsLevel=0;
   m_Non_Trade =false;
   m_Non_Trade_Last=false;
   m_StatusCaption =false;
   m_NeedModify    =false;    //true, if position need modify,for mode "setup stops as pips"
   m_TradeDisabled_Reason=0;  //trade enabled :)
   m_Magic=0;
   m_deviation=1;
   m_Comm=" ";
   m_started=false;
   m_days=1;
   m_LastOrderTicket=0;
   m_GetLastError=0;
   m_SetupFile_Handle=INVALID_HANDLE;

   TradeFormAlign.v_Align=V_ALIGN_TOP_POS;
   TradeFormAlign.h_Align=H_ALIGN_RIGHT_POS;
   TradeFormAlign.Left=0;
   TradeFormAlign.Top=0;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CTradePanel::OnInit(CSymbolInfo *p_Info)
  {
   if(SymbolInfo!=NULL)
     {
      SymbolInfo=NULL;
     }
   if((SymbolInfo=p_Info)==NULL) return;
   SymbolInfo.Name(Symbol());
   if(!SymbolInfo.RefreshRates()) return;
   m_digits=(uchar)SymbolInfo.Digits();
   m_SwapShort=(double)SymbolInfo.SwapShort();
   m_SwapLong=(double)SymbolInfo.SwapLong();
   m_StopsLevel=(int)SymbolInfo.StopsLevel();
   m_LastAsk  =(double)SymbolInfo.Ask();
   m_LastBid  =(double)SymbolInfo.Bid();
   m_point    =(double)SymbolInfo.Point();
   m_LastSpread=(int)SymbolInfo.Spread();
   m_Lot=(double)SymbolInfo.LotsMin();
   m_Magic=GenerateMagic();
   m_Name=Symbol();
   if(lblTickTime!=NULL){ lblTickTime.SetCaption(TimeToString(SymbolInfo.Time(),TIME_MINUTES|TIME_SECONDS));}
   if(lblSpread!=NULL)  { lblSpread.SetCaption(IntegerToString(m_LastSpread));}
   if(btnBuy!=NULL) btnBuy.OnBkgChange(BTN_FACE_PRICENONE,m_LastAsk,m_digits,m_LastAskChange);
   if(btnSell!=NULL) btnSell.OnBkgChange(BTN_FACE_PRICENONE,m_LastBid,m_digits,m_LastBidChange);
   if(seVolume!=NULL) seVolume.SetRange(SymbolInfo.LotsMax(),m_Lot,SymbolInfo.LotsStep());
   if(seTake!=NULL)   seTake.SetRange(10000,m_StopsLevel,m_StopsLevel);
   if(seLoss!=NULL)   seLoss.SetRange(10000,m_StopsLevel,m_StopsLevel);
   if(lblSwapShort!=NULL) lblSwapShort.SetCaption(DoubleToString(m_SwapShort,2));
   if(lblSwapLong!=NULL) lblSwapLong.SetCaption(DoubleToString(m_SwapLong,2));
   if(lblSymb!=NULL) lblSymb.SetCaption(SymbolInfo.Name());
   if(lblStatus!=NULL) SetStatusCaption(" ");
   if(seDeviation!=NULL) seDeviation.SetRange(10,1,1);
   if(edMagic!=NULL) edMagic.SetText(IntegerToString(m_Magic));
   m_Non_Trade_Last=m_Non_Trade=!TradeAllow();
   SetTradingManager(new CTradingManager);
   Non_Trade(m_Non_Trade);
   m_end=TimeCurrent();
   m_start=m_end-m_days*PeriodSeconds(PERIOD_D1);
   InitCounters();
   m_FPosition=PositionSelect(m_Name);
   ChartRedraw();
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CTradePanel::OnTick(void)
  {
   OnTickEvent();
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CTradePanel::OnTimer(void)
  {
   m_Timer_nt++;
   if(m_Timer_nt>=DELAY_TRADE_CONFIRM)
     {
      m_Timer_nt=0;
      m_Non_Trade=!TradeAllow();
      if(m_Non_Trade_Last!=m_Non_Trade)
        {
         Non_Trade(m_Non_Trade);
         m_Non_Trade_Last=m_Non_Trade;
         ChartRedraw();
        }
     }
//---
   if(!m_Non_Trade)
     {
      if(m_LastAskChange!=PRICE_CHANGE_NONE || m_LastBidChange!=PRICE_CHANGE_NONE)
        {
         m_Timer_pn++;
         if(m_Timer_pn>=DELAY_BTN_PN)
           {
            if(btnBuy.IsEnabled && m_LastAskChange!=PRICE_CHANGE_NONE)
              {
               btnBuy.OnBkgChange(BTN_FACE_PRICENONE,0,UCHAR_MAX,PRICE_CHANGE_NONE);
               m_LastAskChange=PRICE_CHANGE_NONE;
              }

            if(btnSell.IsEnabled && m_LastBidChange!=PRICE_CHANGE_NONE)
              {
               btnSell.OnBkgChange(BTN_FACE_PRICENONE,0,UCHAR_MAX,PRICE_CHANGE_NONE);
               m_LastBidChange=PRICE_CHANGE_NONE;
              }
            ChartRedraw();
           }
        }
     }
//---
   if(m_StatusCaption)
     {
      m_Timer_cl++;
      if(m_Timer_cl>=DELAY_CAPTION_CLEAR)
        {
         SetStatusCaption(" ");
         ChartRedraw();
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CTradePanel::OnTrade(void)
  {
   if(m_started) TradeDispatcher();
   else InitCounters();
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CTradePanel::CreateForm(string f_FormName,CResources *p_Resources)
  {
   bool result=CForm::Create(0,f_FormName,p_Resources);
   if(result)
     {
      m_Top=Top();
      m_Left=Left();
      Resources=p_Resources;
     }
   return((bool)CreatePanelControls());
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CTradePanel::CreatePanelControls()
  {
   bool result=false;
   int  _Index=-1;
   CContainer *Handler=GetContainer(CNTR_HANDLER);

   C_WAVbox=GetWAVbox(WAVS_CTRLS);
   E_WAVbox=GetWAVbox(WAVS_EVNTS);

   switch(FrameState())
     {
      case FRM_TYPE_MINIMIZE:
        {
         if((btnDrag=CreateButton("btnDrag",m_Top+3,m_Left+212," ","RS_DRAGBTN"))==NULL) return(false);
         else btnDrag.Clickable(true);
         if((btnSell=CreateCMDButton("btnSell",m_Top+32,m_Left+15,"CMD_SELL","CMD_FONT"))==NULL) return(false);
         else  btnSell.SetDigitsSizes(m_SmallDigitWidth,m_DilimiterWidth,m_LargeDigitWidth);
         if((btnBuy=CreateCMDButton("btnBuy",m_Top+32,m_Left+117,"CMD_BUY","CMD_FONT"))==NULL) return(false);
         else  btnBuy.SetDigitsSizes(m_SmallDigitWidth,m_DilimiterWidth,m_LargeDigitWidth);
         if((seVolume=CreateSpin("seVolume",m_Top+34,m_Left+73,53,19,BP_LEFT_RIGHT,"RS_SPIN","EDIT_FONT"))==NULL) return(false);
         return(true);
        }
      break;

      case FRM_TYPE_MAXIMIZE:
        {
         if((btnDrag=CreateButton("btnDrag",m_Top+3,m_Left+3," ","RS_DRAGBTN"))==NULL) return(false);
         else btnDrag.Clickable(true);
         if((lblStatus=CreateLabel("lblStatus",m_Top+170,m_Left+117," ","STATUS_FONT"))==NULL) return(false);
         if((TabControl=CreateTabs("tbMain",m_Top+25,m_Left+5,3,"RS_TABS","COMMON_FONT"))==NULL) return(false);
         else
           {
            //---Create "Trade" sheet
            if((TradeSheet=TabControl.AddItem("Trade","Trade",true,Handler.Container()))!=NULL)
              {
               if((bkgTrade=CreateImage("bkgTrade",m_Top+49,m_Left+9,"RS_BKGTRADE",TradeSheet))==NULL) return(false);
               if((btnSell=CreateCMDButton("btnSell",m_Top+56,m_Left+16,"CMD_SELL","CMD_FONT",TradeSheet))==NULL) return(false);
               else  btnSell.SetDigitsSizes(m_SmallDigitWidth,m_DilimiterWidth,m_LargeDigitWidth);
               if((btnBuy=CreateCMDButton("btnBuy",m_Top+56,m_Left+118,"CMD_BUY","CMD_FONT",TradeSheet))==NULL) return(false);
               else  btnBuy.SetDigitsSizes(m_SmallDigitWidth,m_DilimiterWidth,m_LargeDigitWidth);
               if((seVolume=CreateSpin("seVolume",m_Top+58,m_Left+74,53,19,BP_LEFT_RIGHT,"RS_SPIN","EDIT_FONT",TradeSheet))==NULL) return(false);
               if((lblSpread=CreateLabel("lblSpread",m_Top+125,m_Left+117," ","COMMON_FONT",TradeSheet))==NULL) return(false);
               if((lblSwapShort=CreateLabel("lblSwapShort",m_Top+125,m_Left+43," ","COMMON_FONT",TradeSheet))==NULL) return(false);
               if((lblSwapLong=CreateLabel("lblSwapLong",m_Top+125,m_Left+191," ","COMMON_FONT",TradeSheet))==NULL) return(false);
               if((lblSymb=CreateLabel("lblSymb",m_Top+37,m_Left+10," ","COMMON_FONT",TradeSheet))==NULL) return(false);
               else lblSymb.SetAnchorPoint(ANCHOR_LEFT);
               if((lblTickTime=CreateLabel("lblTickTime",m_Top+37,m_Left+224," ","COMMON_FONT",TradeSheet))==NULL) return(false);
               else
                 {
                  lblTickTime.SetAnchorPoint(ANCHOR_RIGHT);
                 }
              }
            else
              {
               //Print()
               return(false);
              }
            //---

            //---Create "Setup orders" sheet

            if((SetupSheet=TabControl.AddItem("Setup","Setup stops",false,Handler.Container()))!=NULL)
              {
               if((chbTake=CreateCheckBox("chbTake",m_Top+40,m_Left+16,"Set Take Profit","RS_CHKBOX","COMMON_FONT",SetupSheet))==NULL) return(false);
               if((chbLoss=CreateCheckBox("chbLoss",m_Top+69,m_Left+16,"Set Stop Loss","RS_CHKBOX","COMMON_FONT",SetupSheet))==NULL) return(false);
               if((seTake=CreateSpin("seTake",m_Top+38,m_Left+133,70,19,BP_RIGHT,"RS_SPIN1","EDIT_FONT",SetupSheet))==NULL) return(false);
               else
                 {
                  seTake.Enabled(chbTake.IsChecked);
                 }
               if((seLoss=CreateSpin("seLoss",m_Top+67,m_Left+133,70,19,BP_RIGHT,"RS_SPIN1","EDIT_FONT",SetupSheet))==NULL) return(false);
               else
                 {
                  seLoss.Enabled(chbTake.IsChecked);
                 }
               if((bmpBevel=CreateImage("bmpBevel",m_Top+91,m_Left+14,"RS_SET_AS",SetupSheet))==NULL) return(false);
               if((RadioGroup=CreateRadioGroup("StopsGroup",2,"RS_RG","COMMON_FONT",SetupSheet))!=NULL)
                 {
                  if((rbPips=RadioGroup.AddItem("rbPips",m_Top+111,m_Left+59,"pips",true,Handler.Container()))==NULL) return(false);
                  if((rbPrice=RadioGroup.AddItem("rbPrice",m_Top+111,m_Left+149,"price",false,Handler.Container()))==NULL) return(false);
                  RadioGroup.Enabled(chbLoss.IsChecked || chbTake.IsChecked);
                 }

              }
            else
              {
               return(false);
              }
            //---

            //---Create "Options" sheet

            if((OptionsSheet=TabControl.AddItem("Options","Options",false,Handler.Container()))!=NULL)
              {
               if((chbDev=CreateCheckBox("chbDev",m_Top+40,m_Left+16,"Deviation","RS_CHKBOX","COMMON_FONT",OptionsSheet))==NULL) return(false);
               else chbDev.Checked(true);
               if((chbMagic=CreateCheckBox("chbMagic",m_Top+69,m_Left+16,"Magic number","RS_CHKBOX","COMMON_FONT",OptionsSheet))==NULL) return(false);
               else chbMagic.Checked(true);
               if((seDeviation=CreateSpin("seDeviation",m_Top+38,m_Left+133,70,19,BP_RIGHT,"RS_SPIN1","EDIT_FONT",OptionsSheet))==NULL) return(false);
               if((edMagic=CreateEdit("edMagic",m_Top+67,m_Left+133,70,19,"EDIT_FONT",OptionsSheet))==NULL) return(false);
               if((bmpComm=CreateImage("bmpComm",m_Top+91,m_Left+14,"RS_COMMENT",OptionsSheet))==NULL) return(false);
               if((edComment=CreateEdit("edComm",m_Top+110,m_Left+16,203,19,"EDIT_FONT",OptionsSheet))==NULL) return(false);
/* if((chbWavs=CreateCheckBox("chbWavs",m_Top+98,m_Left+16,"Use sound","RS_CHKBOX","COMMON_FONT",OptionsSheet))==NULL) return(false);
               else chbWavs.Checked(true);*/
              }
            else
              {
               return(false);
              }
            //---

           }
         return(true);
        }
      break;
      default:break;
     }

   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CTradePanel::~CTradePanel(void)
  {
   if(TradingManager!=NULL) delete TradingManager;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CTradePanel::SetTradingManager(CTradingManager *p_tm)
  {
   if(TradingManager!=NULL) TradingManager=NULL;
   if((TradingManager=p_tm)==NULL) Print("CTradePanel::Invalid TM pointer");
   else
     {
      if(TradingManager.Initilize(SymbolInfo)<0)
         Print("CTradePanel::TradingManager not initilized");
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CTradePanel::OnTickEvent()
  {
   m_Timer_pn=0;
   if(!SymbolInfo.RefreshRates()) return;
   double _LastAsk=(double)SymbolInfo.Ask();
   double _LastBid=(double)SymbolInfo.Bid();
   int    _LastSpread=(int)SymbolInfo.Spread();
   datetime _LastTime=(datetime)SymbolInfo.Time();

   if(lblTickTime!=NULL)
     {
      lblTickTime.SetCaption(TimeToString(_LastTime,TIME_MINUTES|TIME_SECONDS));
     }

   if(lblSpread!=NULL && m_LastSpread!=_LastSpread)
     {
      lblSpread.SetCaption(IntegerToString(_LastSpread));
      m_LastSpread=_LastSpread;
     }

   if(btnBuy!=NULL && m_LastAsk!=_LastAsk)
     {
      if(_LastAsk>m_LastAsk)
        {
         if(btnBuy.IsEnabled && m_LastAskChange!=PRICE_CHANGE_UP) btnBuy.OnBkgChange(BTN_FACE_PRICEUP,_LastAsk,m_digits,PRICE_CHANGE_UP);
         else btnBuy.OnBkgChange(-1,_LastAsk,m_digits,PRICE_CHANGE_UP);
         m_LastAskChange=PRICE_CHANGE_UP;
        }
      else
        {
         if(btnBuy.IsEnabled && m_LastAskChange!=PRICE_CHANGE_DOWN) btnBuy.OnBkgChange(BTN_FACE_PRICEDOWN,_LastAsk,m_digits,PRICE_CHANGE_DOWN);
         else btnBuy.OnBkgChange(-1,_LastAsk,m_digits,PRICE_CHANGE_DOWN);
         m_LastAskChange=PRICE_CHANGE_DOWN;
        }
      m_LastAsk=_LastAsk;
     }

   if(btnSell!=NULL && m_LastBid!=_LastBid)
     {
      if(_LastBid>m_LastBid)
        {
         if(btnSell.IsEnabled && m_LastBidChange!=PRICE_CHANGE_UP) btnSell.OnBkgChange(BTN_FACE_PRICEUP,_LastBid,m_digits,PRICE_CHANGE_UP);
         else btnSell.OnBkgChange(-1,_LastBid,m_digits,PRICE_CHANGE_UP);
         m_LastBidChange=PRICE_CHANGE_UP;
        }
      else
        {
         if(btnSell.IsEnabled && m_LastBidChange!=PRICE_CHANGE_DOWN) btnSell.OnBkgChange(BTN_FACE_PRICEDOWN,_LastBid,m_digits,PRICE_CHANGE_DOWN);
         else btnSell.OnBkgChange(-1,_LastBid,m_digits,PRICE_CHANGE_DOWN);
         m_LastBidChange=PRICE_CHANGE_DOWN;
        }
      m_LastBid=_LastBid;

     }
   ChartRedraw();
  }
//+------------------------------------------------------------------+
//|OnEnable event                                                    |
//+------------------------------------------------------------------+
void CTradePanel::OnEnable(void)
  {
   if(Enabled())
     {
      switch(TabControl.ActiveIndex())
        {
         case 0:  // "Trade" sheet
            if(m_Non_Trade)
              {
               Non_Trade(true);
              }
            break;

         case 1:  //"Setup stops" sheet
            seTake.Enabled(chbTake.IsChecked);
            seLoss.Enabled(chbLoss.IsChecked);
            RadioGroup.Enabled(chbLoss.IsChecked || chbTake.IsChecked);
            break;

         case 2:  //"Options" sheet
            seDeviation.Enabled(chbDev.IsChecked);
            edMagic.Enabled(chbMagic.IsChecked);
            break;

         default:
            break;
        }
     }
  }
//+------------------------------------------------------------------+
//|Set statusbar caption                                             |
//+------------------------------------------------------------------+
void CTradePanel::SetStatusCaption(string f_Caption)
  {
   lblStatus.SetCaption(f_Caption);
   m_StatusCaption=(f_Caption!=" ")?true:false;
   if(m_StatusCaption)
     {
      m_Timer_cl=0;
      Print("TradePad :: "+f_Caption);
     }
   ChartRedraw();
  }
//+------------------------------------------------------------------+
//|Disable/Enable command buttons                                    |
//+------------------------------------------------------------------+
void CTradePanel::Non_Trade(bool f_Confirm,uchar f_CMDFlag=CMD_ALL)
  {
   switch(f_CMDFlag)
     {
      case CMD_ALL:
         btnBuy.Enabled(!f_Confirm);
         btnSell.Enabled(!f_Confirm);
         break;

      case CMD_SELL:
         btnSell.Enabled(!f_Confirm);
         break;

      case CMD_BUY:
         btnBuy.Enabled(!f_Confirm);
         break;

      default:
         Print("TradePad :: Unknow CMD flag");
         break;
     }

  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CTradePanel::TradeAllow()
  {
   if(!TerminalInfoInteger(TERMINAL_CONNECTED))
     {
      if(m_TradeDisabled_Reason!=-1)
        {
         SetStatusCaption("Not connected");
         m_TradeDisabled_Reason=-1;
        }
      return(false);
     }

   if(!TerminalInfoInteger(TERMINAL_TRADE_ALLOWED))
     {
      if(m_TradeDisabled_Reason!=-2)
        {
         SetStatusCaption("AutoTrading is disabled on terminal");
         m_TradeDisabled_Reason=-2;
        }
      return(false);
     }

//--- put here your code for other checks

   m_TradeDisabled_Reason=0;
   return(true);
  }
//+------------------------------------------------------------------+
//|Generate "magic" number for current symbol                        |
//+------------------------------------------------------------------+
ulong CTradePanel::GenerateMagic(void)
  {
   ulong magic=1;
   string _str=SymbolInfo.Name();
   ushort _val;
   for(int i=0;i<StringLen(_str);i++)
     {
      _val=StringGetCharacter(_str,i);
      magic+=(_val!=0)?_val:1;
     }
   return(magic);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//| Trade events processing implementation                            |
//|                                                                  |
//+------------------------------------------------------------------+


//+------------------------------------------------------------------+
//|  initialization of position, order and trade counters               |
//+------------------------------------------------------------------+
void CTradePanel::InitCounters()
  {
   ResetLastError();
   bool selected=HistorySelect(m_start,m_end);
   if(!selected)
     {
      PrintFormat("%s. Failed to load history from %s to %s to cache. Error code: %d",
                  __FUNCTION__,TimeToString(m_start),TimeToString(m_end),GetLastError());
      return;
     }
   m_positions=PositionsTotal();
   m_deals=HistoryDealsTotal();
   m_started=true;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CTradePanel::TradeDispatcher(void)
  {
   Sleep(500);
   m_end=TimeCurrent();
   ResetLastError();
//--- load history 
   bool selected=HistorySelect(m_start,m_end);
   if(!selected)
     {
      PrintFormat("%s. Failed to load history from %s to %s to cache. Error code: %d",
                  __FUNCTION__,TimeToString(m_start),TimeToString(m_end),GetLastError());
      return;
     }
//---
   int curr_history_deals=HistoryDealsTotal();

   if(curr_history_deals!=m_deals)
     {
      ulong  _ticket=HistoryDealGetTicket(curr_history_deals-1);
      string _symb=HistoryDealGetString(_ticket,DEAL_SYMBOL);

      if(m_Name==_symb)
        {
         string _str,_entrytype=" ";
         int    _count=0;
         double _price=HistoryDealGetDouble(_ticket,DEAL_PRICE);
         long   _entry=HistoryDealGetInteger(_ticket,DEAL_ENTRY);
         switch(_entry)
           {
            case DEAL_ENTRY_IN:
              {
               _entrytype="in";
               if(m_FPosition && m_NeedModify)
                 {
                  bool _done=SetStopsInPips();
                  if(_done)
                    {
                     m_NeedModify=!_done;
                    }
                  else
                    {
                     Print(TradingManager.Trade_result.retcode);
                    }
                 }
              }
            break;
            case DEAL_ENTRY_OUT:   _entrytype="out";     break;
            case DEAL_ENTRY_INOUT:
              {
               _entrytype="in/out";
               if(m_FPosition && m_NeedModify)
                 {
                  bool _done=SetStopsInPips();
                  if(_done)
                    {
                     m_NeedModify=!_done;
                    }
                  else
                    {
                     Print(TradingManager.Trade_result.retcode);
                    }
                 }
              }
            break;
            default:                                     break;
           }

         _count=StringConcatenate(_str,"Deal(",_entrytype,"): #",_ticket," at ",_price," done.");
         if(_count>0) SetStatusCaption(_str);
        }
      m_deals=curr_history_deals;
     }
//---
   int curr_positions=PositionsTotal();
   if(curr_positions!=m_positions)
     {
      if(!m_FPosition)
        {
         if((m_FPosition=PositionSelect(m_Name))) // if new position
           {
            if(m_NeedModify)
              {
               bool _done=SetStopsInPips();
               if(_done)
                 {
                  m_NeedModify=!_done;
                 }
               else
                 {
                  Print(TradingManager.Trade_result.retcode);
                 }
              }
           }
        }
      else
        {
         if(!(m_FPosition=PositionSelect(m_Name)))
           {
            Print("Close position on "+m_Name);
           }
        }
      m_positions=curr_positions;
     }
//---

   CheckStartDateInTradeHistory();
  }
//+------------------------------------------------------------------+
//|  changing the start date for trade history request           |
//+------------------------------------------------------------------+
void CTradePanel::CheckStartDateInTradeHistory()
  {
//--- initial interval, if we were to start working right now
   datetime curr_start=TimeCurrent()-m_days*PeriodSeconds(PERIOD_D1);
//--- make sure that the start limit of the trade history has not gone 
//--- more than 1 day over the intended date
   if(curr_start-m_start>PeriodSeconds(PERIOD_D1))
     {
      //--- we have to adjust the start date of the history to be loaded in the cache 
      m_start=curr_start;
      PrintFormat("New start limit of the trade history to be loaded: start => %s",
                  TimeToString(m_start));

      //--- now reload the trade history for the adjusted interval
      HistorySelect(m_start,m_end);

      m_deals=HistoryDealsTotal();
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CTradePanel::SetStopsInPips()
  {
   m_SLPrice=0;
   m_TPPrice=0;

   if(TradingManager.CheckOpenPositions(m_Magic))
     {
      double _OpenPrice=NormalizeDouble(TradingManager.CurrentPosition.OpenPrice,m_digits);
      switch(TradingManager.CurrentPosition.Type)
        {
         case POSITION_TYPE_SELL:
            if(chbLoss.IsChecked) m_SLPrice=_OpenPrice+NormalizeDouble(m_SLPoints*m_point,m_digits);
            if(chbTake.IsChecked) m_TPPrice=_OpenPrice-NormalizeDouble(m_TPPoints*m_point,m_digits);
            break;

         case POSITION_TYPE_BUY:
            if(chbLoss.IsChecked) m_SLPrice=_OpenPrice-NormalizeDouble(m_SLPoints*m_point,m_digits);
            if(chbTake.IsChecked) m_TPPrice=_OpenPrice+NormalizeDouble(m_TPPoints*m_point,m_digits);
            break;
        }
      //Print(" SL: "+DoubleToString(m_SLPrice,m_digits)+" TP: "+DoubleToString(m_TPPrice,m_digits));
      return(TradingManager.ModifyPosition(m_SLPrice,m_TPPrice,m_Magic));
     }
   return(false);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CTradePanel::OnChartEvent(const int id,const long &lparam,const double &dparam,const string &sparam)
  {
   CTabHeader  *t_Header=NULL;
   CForm::OnChartEvent(id,lparam,dparam,sparam);

   if(id==CHARTEVENT_OBJECT_CLICK)
     {
      if(sparam=="Test")
        {
         Non_Trade(true);
         ChartRedraw();
         return;
        }

      if(sparam=="Test1")
        {
         Non_Trade(false);
         ChartRedraw();
         return;
        }

      if(sparam=="Test2")
        {
         Enabled((Enabled())?false:true);
         ChartRedraw();
         return;
        }
     }

   if(id==CHARTEVENT_OBJECT_ENDEDIT)
     {
      if(sparam=="edseVolume")
        {
         if(seVolume.KeysAssigned())
           {
            m_Lot=(double)seVolume.Value();
           }
         ChartRedraw();
         return;
        }

      if(sparam=="edseTake")
        {
         if(seTake.KeysAssigned())
           {
            if(rbPrice.Active()) m_TPPrice=m_LastTPPrice=(double)seTake.Value();
            else  m_TPPoints=m_LastTPPoints=(int)seTake.Value();
           }
         ChartRedraw();
         return;
        }

      if(sparam=="edseLoss")
        {
         if(seLoss.KeysAssigned())
           {
            if(rbPrice.Active()) m_SLPrice=m_LastSLPrice=(double)seLoss.Value();
            else  m_SLPoints=m_LastSLPoints=(int)seLoss.Value();
           }
         ChartRedraw();
         return;
        }

      if(sparam=="edseDeviation")
        {
         if(seDeviation.KeysAssigned())
           {
            m_deviation=(ushort)seDeviation.Value();
           }
         ChartRedraw();
         return;
        }

      if(sparam=="edMagic")
        {
         if(edMagic.IsNumeric())
           {
            m_Magic=(ulong)StringToInteger(edMagic.Text());
           }
         else edMagic.SetText(IntegerToString(m_Magic));
         ChartRedraw();
         return;
        }

      if(sparam=="edComm")
        {
         m_Comm=edComment.Text();
         ChartRedraw();
         return;
        }
     }
  }
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//|Implementation of event handlers for controls                     |
//|                                                                  |
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CTradePanel::ButtonClickEvent(CButton *p_Obj,string f_ObjName)
  {
   CButton *obj=p_Obj;

   if(f_ObjName=="btnDrag")
     {
      Repaint(REPAINT_REASON_MOVE);
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CTradePanel::CMDButtonClickEvent(CButton *p_Obj,string f_ObjName)
  {
   CTradeButton *obj=p_Obj;
   if(f_ObjName=="btnSell")
     {
      if(TradingManager!=NULL)
        {
         TradingManager.SetOptions(m_Magic,m_deviation,m_Comm);
         if(RadioGroup.Enabled())
           {
            switch(RadioGroup.ActiveIndex())
              {
               case 0:  //rbPips
                  m_NeedModify=TradingManager.OpenPosition(ORDER_TYPE_SELL,m_Lot);
                  break;

               case 1:  //rbPrice
                  TradingManager.OpenPosition(ORDER_TYPE_SELL,m_Lot,m_SLPrice,m_TPPrice);
                  break;
              }
           }
         else TradingManager.OpenPosition(ORDER_TYPE_SELL,m_Lot);
        }
      return;
     }

   if(f_ObjName=="btnBuy")
     {
      if(TradingManager!=NULL)
        {
         TradingManager.SetOptions(m_Magic,m_deviation,m_Comm);

         if(RadioGroup.Enabled())
           {
            switch(RadioGroup.ActiveIndex())
              {
               case 0:  //rbPips
                  m_NeedModify=TradingManager.OpenPosition(ORDER_TYPE_BUY,m_Lot);
                  break;

               case 1:  //rbPrice
                  TradingManager.OpenPosition(ORDER_TYPE_BUY,m_Lot,m_SLPrice,m_TPPrice);
                  break;
              }
           }
         else TradingManager.OpenPosition(ORDER_TYPE_BUY,m_Lot);
        }
      return;
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CTradePanel::SpinBtnsClickEvent(CSpinButton *p_Obj)
  {
   CSpinButton *obj=p_Obj;
   double Value=0;
   string SpinEditName;

   bool result=obj.GetData(SpinEditName,Value);

   if(result)
     {
      if(SpinEditName=="seVolume")
        {
         m_Lot=Value;
         return;
        }

      if(SpinEditName=="seDeviation")
        {
         m_deviation=(ushort)Value;
         return;
        }

      if(SpinEditName=="seTake")
        {
         if(rbPrice.Active()) m_TPPrice=m_LastTPPrice=(double)Value;
         else  m_TPPoints=m_LastTPPoints=(int)Value;
         return;
        }

      if(SpinEditName=="seLoss")
        {
         if(rbPrice.Active()) m_SLPrice=m_LastSLPrice=(double)Value;
         else  m_SLPoints=m_LastSLPoints=(int)Value;
         return;
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CTradePanel::CheckBoxClickEvent(CCheckbox *p_Obj,string f_ObjName)
  {
   if(f_ObjName=="chbTake")
     {
      seTake.Enabled(chbTake.IsChecked);
      RadioGroup.Enabled(chbLoss.IsChecked || chbTake.IsChecked);
      if(!chbTake.IsChecked)
        {
         m_TPPoints=0;
         m_TPPrice=0;
        }
      else
        {
         if(rbPips.Active())m_TPPoints=(int)seTake.Value();
         else m_TPPrice=seTake.Value();
        }
      ChartRedraw();
      return;
     }

   if(f_ObjName=="chbLoss")
     {
      seLoss.Enabled(chbLoss.IsChecked);
      RadioGroup.Enabled(chbLoss.IsChecked || chbTake.IsChecked);
      if(!chbLoss.IsChecked)
        {
         m_SLPoints=0;
         m_SLPrice=0;
        }
      else
        {
         if(rbPips.Active())m_SLPoints=(int)seLoss.Value();
         else m_SLPrice=seLoss.Value();
        }
      ChartRedraw();
      return;
     }

   if(f_ObjName=="chbDev")
     {
      seDeviation.Enabled(chbDev.IsChecked);
      ChartRedraw();
      return;
     }

   if(f_ObjName=="chbMagic")
     {
      edMagic.Enabled(chbMagic.IsChecked);
      ChartRedraw();
      return;
     }

   if(f_ObjName=="chbWavs" && C_WAVbox!=NULL)
     {
      C_WAVbox.On(chbWavs.IsChecked);
      ChartRedraw();
      return;
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void  CTradePanel::TabHeaderClickEvent(CTabHeader *p_Obj,string f_ObjName)
  {
   ChartRedraw();
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CTradePanel::RadioBtnClickEvent(CRadioButton *p_Obj,string f_ObjName)
  {

   if(f_ObjName=="rbPips")
     {
      seTake.SetRange(10000,m_StopsLevel,m_StopsLevel,(m_LastTPPoints!=0)?m_LastTPPoints:m_StopsLevel);
      seLoss.SetRange(10000,m_StopsLevel,m_StopsLevel,(m_LastSLPoints!=0)?m_LastSLPoints:m_StopsLevel);
      ChartRedraw();
      return;
     }

   if(f_ObjName=="rbPrice")
     {
      seTake.SetRange(10000,0,m_point,(m_LastTPPrice!=0)?m_LastTPPrice:m_LastBid);
      seLoss.SetRange(10000,0,m_point,(m_LastSLPrice!=0)?m_LastSLPrice:m_LastBid);
      ChartRedraw();
      return;
     }
  }
//+------------------------------------------------------------------+
