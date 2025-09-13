//+------------------------------------------------------------------+
//|                                              AdvancedEAPanel.mqh |
//|                                      Copyright 2010, Investeo.pl |
//|                                                http:/Investeo.pl |
//+------------------------------------------------------------------+
#property copyright "Copyright 2010, Investeo.pl"
#property link      "http:/Investeo.pl"

#include <ChartObjects\ChartObjectsTxtControls.mqh>
#include <ChartObjectExtControls.mqh>
#include <ChartObjects\ChartObjectSubChart.mqh>
#include <ChartObjects\ChartObjectsLines.mqh>
#include <Arrays\ArrayObj.mqh>
#include <Arrays\ArrayInt.mqh>
#include <Expert\ExpertTrade.mqh>
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
class CAdvancedEAPanel : public CChartObjectButton
  {
private:
   CArrayObj         m_attachment;       // array of attached objects
   bool              m_expanded;         // collapsed/expanded flag
   CArrayObj        *m_tabs;            // tabs on panel
   CArrayObj        *m_sideButtons;

   CArrayObj        *m_spinnersPanel;
   CArrayObj        *m_spinnersTab0;
   CArrayObj        *m_txtSpinnersTab0;
   CArrayObj        *m_txtSpinnersTab1;
   CArrayObj        *m_showButtonsTab1;
   CArrayObj        *m_pointsEditTab1;

   CChartObjectEditTable *m_mtfTable;
   CChartObjectListbox *m_tradePlan;
   CChartObjectListbox *m_lineObjList;

   CArrayString     *m_tradePlanList;

   CSymbolInfo      *m_symbolInfo;
   CExpertTrade      m_trade;

   CArrayObj        *m_tab0;
   CArrayObj        *m_tab1;
   CArrayObj        *m_tab2;
   CArrayObj        *m_panel;
   CArrayObj        *m_pivots;

   color             pallete[8];
   int               m_xTabSize;
   int               m_yTabSize;
   int               m_xPanelSize;
   int               m_yPanelSize;
   int               m_xPanelDistance;
   int               m_yPanelDistance;
   int               m_xSideBtnSize;
   string            m_panelName;
   long              m_chartId;
   int               m_window;
   int               m_visibleTab;
   double            m_entryPoint;
   MqlTick           m_lastTick;

   int               nTF;
   // indicator handles
   CArrayInt        *m_hEMA3;
   CArrayInt        *m_hEMA6;
   CArrayInt        *m_hEMA9;
   CArrayInt        *m_hSMA50;
   CArrayInt        *m_hSMA200;
   CArrayInt        *m_hCCI14;
   CArrayInt        *m_hRSI21;

   void              CalculatePivots(MqlRates &candle);
   void              BackExtObjects();
   void              RefreshPOIList();

public:
                     CAdvancedEAPanel();
                    ~CAdvancedEAPanel();

   bool              Attach(CChartObjectLabel *chart_object);
   bool              X_Distance(int X);
   bool              Y_Distance(int Y);
   int               X_Size() const;
   int               Y_Size() const;

   bool              Create(long chart_id,string name,int window,int X,int Y,int sizeX,int sizeY,color bg);
   bool              Delete();

   bool              CreateSideButtons(int cnt,int xBtnSize);

   // tab methods   
   bool              CreateTab(int index,color bg);
   void              ShowTab(int tab);
   void              HideTab(int tab);
   void              SetupTab();
   void              CleanTab();
   void              RefreshTab();
   void              ClickTabHandler(int tab);

   void              T0ClickHandler(string sparam);
   void              T0DragHandler(string sparam);
   void              T1ClickHandler(string sparam);
   void              EAPanelClickHandler(string sparam);
   void              EditHandler(string sparam);

   bool              ShowPanel();
   bool              HidePanel();
   void              RefreshPanel();
   void              Refresh();

   bool              IsExpanded();
   bool              BtnState(int index);
   bool              UnselectBtn(int index);

  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CAdvancedEAPanel::CAdvancedEAPanel(void)
  {
   pallete[0] = DimGray;
   pallete[1] = DarkSlateGray;
   pallete[2] = Black;
   pallete[3] = Orange;
   pallete[4] = PaleGreen;
   pallete[5] = LightCoral;
   pallete[6] = Tomato;
   pallete[7] = OliveDrab;

   m_tabs=new CArrayObj();
   m_sideButtons=new CArrayObj();
   m_tab0=new CArrayObj();
   m_tab1=new CArrayObj();
   m_tab2=new CArrayObj();
   m_panel=new CArrayObj();
   m_spinnersPanel=new CArrayObj();
   m_spinnersTab0=new CArrayObj();
   m_txtSpinnersTab0=new CArrayObj();
   m_txtSpinnersTab1=new CArrayObj();
   m_showButtonsTab1=new CArrayObj();
   m_pointsEditTab1=new CArrayObj();
   m_pivots=new CArrayObj();
   m_tradePlanList=new CArrayString();
   m_mtfTable=new CChartObjectEditTable();

   m_symbolInfo=new CSymbolInfo();
   m_symbolInfo.Name(Symbol());
   m_symbolInfo.Refresh();

   m_trade.SetSymbol(m_symbolInfo);
   m_trade.SetOrderTypeTime(ORDER_TIME_GTC);
   SymbolInfoTick(Symbol(),m_lastTick);

   m_xTabSize = 500;
   m_yTabSize = 400;
   m_expanded=true;
   nTF=9;
   ENUM_TIMEFRAMES tf;

   m_hEMA3 = new CArrayInt();
   m_hEMA6 = new CArrayInt();
   m_hEMA9 = new CArrayInt();
   m_hSMA50= new CArrayInt();
   m_hSMA200= new CArrayInt();
   m_hCCI14 = new CArrayInt();
   m_hRSI21 = new CArrayInt();

   for(int i=0; i<nTF; i++)
     {
      switch(i)
        {
         case 0: tf = PERIOD_M1; break;
         case 1: tf = PERIOD_M5; break;
         case 2: tf = PERIOD_M15; break;
         case 3: tf = PERIOD_M30; break;
         case 4: tf = PERIOD_H1; break;
         case 5: tf = PERIOD_H4; break;
         case 6: tf = PERIOD_D1; break;
         case 7: tf = PERIOD_W1; break;
         case 8: tf = PERIOD_MN1; break;
         default : tf=PERIOD_CURRENT;
        };
      m_hEMA3.Add(iMA(Symbol(),tf,3,0,MODE_EMA,PRICE_CLOSE));
      m_hEMA6.Add(iMA(Symbol(),tf,6,0,MODE_EMA,PRICE_CLOSE));
      m_hEMA9.Add(iMA(Symbol(),tf,9,0,MODE_EMA,PRICE_CLOSE));
      m_hSMA50.Add(iMA(Symbol(),tf,50,0,MODE_SMA,PRICE_CLOSE));
      m_hSMA200.Add(iMA(Symbol(),tf,200,0,MODE_SMA,PRICE_CLOSE));
      m_hCCI14.Add(iCCI(Symbol(),tf,14,PRICE_CLOSE));
      m_hRSI21.Add(iRSI(Symbol(),tf,21,PRICE_CLOSE));
     }

  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CAdvancedEAPanel::~CAdvancedEAPanel(void)
  {
   Delete();
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CAdvancedEAPanel::Create(long chart_id,string name,int window,int X,int Y,int sizeX,int sizeY,color bg)
  {
   bool result;

   m_name=name;
   m_chartId= chart_id;
   m_window = window;
   m_visibleTab=-1;

   m_xPanelSize = sizeX;
   m_yPanelSize = sizeY;
   m_xPanelDistance = X;
   m_yPanelDistance = Y;

   result=this.Create(chart_id,name,window,X,Y,sizeX,sizeY);
   this.BackColor(bg);
   this.Selectable(false);

   return result;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CAdvancedEAPanel::CreateTab(int index,color bg)
  {
   CChartObjectButton *btn=new CChartObjectButton();

   btn.Create(m_chart_id,m_name+":"+IntegerToString(index),m_window,-5,-5,0,0);
   btn.BackColor(bg);
   btn.Background(false);
   btn.Selectable(false);
   btn.State(false);
   m_tabs.Add(btn);

   return true;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CAdvancedEAPanel::CreateSideButtons(int cnt,int xBtnSize)
  {
   int yBtnSize=m_yPanelSize/cnt;
   m_xSideBtnSize=xBtnSize;
   for(int i=0; i<cnt; i++)
     {
      CChartObjectButton *sideBtn=new CChartObjectButton();
      sideBtn.Create(m_chart_id,m_name+"side:"+IntegerToString(i),m_window,-m_xSideBtnSize,m_yPanelDistance+i*yBtnSize,xBtnSize,yBtnSize);
      sideBtn.BackColor(White);

      switch(i)
        {
         case 3:
           {
            string hidearrow=" ";
            StringSetCharacter(hidearrow,0,27);
            sideBtn.Description(hidearrow);
            sideBtn.FontSize(14);
            break;
           };
         default:
           {
            sideBtn.Description(IntegerToString(i+1));
            sideBtn.FontSize(8);
            sideBtn.Color(pallete[1]);
           }

        };

      m_sideButtons.Add(sideBtn);
     }
   return true;
  }
//+------------------------------------------------------------------+

bool CAdvancedEAPanel::BtnState(int index)
  {

   CChartObjectButton *element=m_sideButtons.At(index);
   if(element!=NULL) return element.State();
   return false;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CAdvancedEAPanel::UnselectBtn(int index)
  {

   CChartObjectButton *element=m_sideButtons.At(index);
   if(element!=NULL) return element.State(false);
   return false;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CAdvancedEAPanel::Delete(void)
  {
   if(CheckPointer(m_tabs)) m_tabs.Clear(); delete m_tabs;
   if(CheckPointer(m_sideButtons)) m_sideButtons.Clear(); delete m_sideButtons;
   if(CheckPointer(m_spinnersPanel)) m_spinnersPanel.Clear(); delete m_spinnersPanel;
   if(CheckPointer(m_spinnersTab0)) m_spinnersTab0.Clear(); delete m_spinnersTab0;
   if(CheckPointer(m_txtSpinnersTab0)) m_txtSpinnersTab0.Clear(); delete m_txtSpinnersTab0;

   if(CheckPointer(m_txtSpinnersTab1)) m_txtSpinnersTab1.Clear(); delete m_txtSpinnersTab1;
   if(CheckPointer(m_showButtonsTab1)) m_showButtonsTab1.Clear(); delete m_showButtonsTab1;
   if(CheckPointer(m_pointsEditTab1)) m_pointsEditTab1.Clear(); delete m_pointsEditTab1;
   if(CheckPointer(m_pivots)) m_pivots.Clear(); delete m_pivots;

   if(CheckPointer(m_tradePlanList)) m_tradePlanList.Clear(); delete m_tradePlanList;

   delete m_lineObjList;
   delete m_tradePlan;

   if(CheckPointer(m_mtfTable)) m_mtfTable.Delete(); delete m_mtfTable;

   if(CheckPointer(m_tab0)) m_tab0.Clear();
   if(CheckPointer(m_tab1)) m_tab1.Clear();
   if(CheckPointer(m_tab2)) m_tab2.Clear();
   if(CheckPointer(m_panel)) m_panel.Clear();

   delete m_tab0;
   delete m_tab1;
   delete m_tab2;
   delete m_panel;
   delete m_symbolInfo;

   for(int i=0; i<nTF; i++)
     {
      if(CheckPointer(m_hEMA3)) IndicatorRelease(m_hEMA3.At(i));
      if(CheckPointer(m_hEMA6)) IndicatorRelease(m_hEMA6.At(i));
      if(CheckPointer(m_hEMA9)) IndicatorRelease(m_hEMA9.At(i));
      if(CheckPointer(m_hSMA50)) IndicatorRelease(m_hSMA50.At(i));
      if(CheckPointer(m_hSMA200)) IndicatorRelease(m_hSMA200.At(i));
      if(CheckPointer(m_hCCI14)) IndicatorRelease(m_hCCI14.At(i));
      if(CheckPointer(m_hRSI21)) IndicatorRelease(m_hRSI21.At(i));
     }

   if(CheckPointer(m_hEMA3)) m_hEMA3.Clear();
   if(CheckPointer(m_hEMA6)) m_hEMA6.Clear();
   if(CheckPointer(m_hEMA9)) m_hEMA9.Clear();
   if(CheckPointer(m_hSMA50)) m_hSMA50.Clear();
   if(CheckPointer(m_hSMA200)) m_hSMA200.Clear();
   if(CheckPointer(m_hCCI14)) m_hCCI14.Clear();
   if(CheckPointer(m_hRSI21)) m_hRSI21.Clear();

   delete m_hEMA3;
   delete m_hEMA6;
   delete m_hEMA9;
   delete m_hSMA50;
   delete m_hSMA200;
   delete m_hCCI14;
   delete m_hRSI21;

   return true;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CAdvancedEAPanel::ShowPanel(void)
  {
   CChartObjectButton *element=m_sideButtons.At(3);

   CChartObjectLabel *titleLabel=new CChartObjectLabel();
   CChartObjectLabel *volatilityLabel1 = new CChartObjectLabel();
   CChartObjectLabel *volatilityLabel2 = new CChartObjectLabel();

   CChartObjectLabel *positionTypeLabel=new CChartObjectLabel();
   CChartObjectLabel *stepLabel = new CChartObjectLabel();
   CChartObjectLabel *lotsLabel = new CChartObjectLabel();
   CChartObjectLabel *slippageLabel=new CChartObjectLabel();

   CChartObjectLabel *riskLabel=new CChartObjectLabel();
   CChartObjectLabel *rewardLabel=new CChartObjectLabel();
   CChartObjectLabel *rrRatioLabel=new CChartObjectLabel();
   CChartObjectLabel *positionSizeLabel=new CChartObjectLabel();
   CChartObjectLabel *incdecLabel=new CChartObjectLabel();

   CChartObjectEdit *riskEdit=new CChartObjectEdit();
   CChartObjectEdit *rewardEdit=new CChartObjectEdit();
   CChartObjectEdit *rrRatioEdit=new CChartObjectEdit();
   CChartObjectEdit *positionSizeEdit=new CChartObjectEdit();

   CChartObjectRectangleLabel *bidRect = new CChartObjectRectangleLabel();
   CChartObjectRectangleLabel *askRect = new CChartObjectRectangleLabel();
   CChartObjectLabel *bidpart1Label = new CChartObjectLabel();
   CChartObjectLabel *askpart1Label = new CChartObjectLabel();
   CChartObjectLabel *bidpart2Label = new CChartObjectLabel();
   CChartObjectLabel *askpart2Label = new CChartObjectLabel();
   CChartObjectLabel *bidpart3Label = new CChartObjectLabel();
   CChartObjectLabel *askpart3Label = new CChartObjectLabel();

   CChartObjectSpinner *stepSpinner = new CChartObjectSpinner();
   CChartObjectSpinner *lotsSpinner = new CChartObjectSpinner();
   CChartObjectSpinner *slippageSpinner=new CChartObjectSpinner();

   CChartObjectSpinner *volatilitySpanSpinner=new CChartObjectSpinner();

   CChartObjectButton *incPositionBtn = new CChartObjectButton();
   CChartObjectButton *decPositionBtn = new CChartObjectButton();
   CChartObjectButton *reversePositionBtn=new CChartObjectButton();
   CChartObjectButton *closePositionBtn=new CChartObjectButton();
   CChartObjectButton *quitEABtn=new CChartObjectButton();

   for(int i=-m_xPanelSize; i<=m_xPanelDistance; i+=5)
     { ObjectSetInteger(m_chart_id,m_name,OBJPROP_XDISTANCE,i); element.X_Distance(i+m_xPanelSize); ChartRedraw(); };

   for(int i=0; i<3; i++) { element=m_sideButtons.At(i); element.X_Distance(m_xPanelDistance+m_xPanelSize); }
   element=m_sideButtons.At(3);
   element.State(false);
   string hidearrow=" ";
   StringSetCharacter(hidearrow,0,27);
   element.Description(hidearrow);

   this.State(false);
   m_expanded=true;

   SymbolInfoTick(Symbol(),m_lastTick);

// panel controls setup
   positionTypeLabel.Create(0, "EAPanel:positionLabel", 0, m_xPanelDistance+10, m_yPanelDistance + 5);
   positionTypeLabel.Description(Symbol()+" POSITION: " + "NONE");
   positionTypeLabel.Color(pallete[2]);
   positionTypeLabel.FontSize(13);
   m_panel.Add(positionTypeLabel);

   riskLabel.Create(0, "EAPanel:riskLabel", 0, m_xPanelDistance+10, m_yPanelDistance + 35);
   riskLabel.Description("RISK: ");
   riskLabel.Color(pallete[1]);
   riskLabel.FontSize(9);
   m_panel.Add(riskLabel);

   riskEdit.Create(0, "EAPanel:riskEdit", 0, m_xPanelDistance+10, m_yPanelDistance + 55);
   riskEdit.Description("NONE");
   riskEdit.Color(pallete[2]);
   riskEdit.FontSize(8);
   m_panel.Add(riskEdit);

   rewardLabel.Create(0, "EAPanel:rewardLabel", 0, m_xPanelDistance+130, m_yPanelDistance + 35);
   rewardLabel.Description("REWARD: ");
   rewardLabel.Color(pallete[1]);
   rewardLabel.FontSize(9);
   m_panel.Add(rewardLabel);

   rewardEdit.Create(0, "EAPanel:rewardEdit", 0, m_xPanelDistance+130, m_yPanelDistance + 55);
   rewardEdit.Description("NONE");
   rewardEdit.Color(pallete[2]);
   rewardEdit.FontSize(8);
   m_panel.Add(rewardEdit);

   rrRatioLabel.Create(0, "EAPanel:rrRatioLabel", 0, m_xPanelDistance+10, m_yPanelDistance + 75);
   rrRatioLabel.Description("R/R RATIO: ");
   rrRatioLabel.Color(pallete[1]);
   rrRatioLabel.FontSize(9);
   m_panel.Add(rrRatioLabel);

   rrRatioEdit.Create(0, "EAPanel:rrRatioEdit", 0, m_xPanelDistance+10, m_yPanelDistance + 95);
   rrRatioEdit.Description("1:1");
   rrRatioEdit.Color(pallete[2]);
   rrRatioEdit.FontSize(8);
   m_panel.Add(rrRatioEdit);

   positionSizeLabel.Create(0, "EAPanel:positionSizeLabel", 0, m_xPanelDistance+130, m_yPanelDistance + 75);
   positionSizeLabel.Description("POSITION SIZE:");
   positionSizeLabel.Color(pallete[1]);
   positionSizeLabel.FontSize(9);
   m_panel.Add(positionSizeLabel);

   positionSizeEdit.Create(0, "EAPanel:positionSizeEdit", 0, m_xPanelDistance+130, m_yPanelDistance + 95);
   positionSizeEdit.Description("0.0 LOTS");
   positionSizeEdit.Color(pallete[2]);
   positionSizeEdit.FontSize(8);
   m_panel.Add(positionSizeEdit);

   incdecLabel.Create(0, "EAPanel:IncDecLabel", 0, m_xPanelDistance+10, m_yPanelDistance + 115);
   incdecLabel.Description("INCREMENT/DECREMENT POSITION:");
   incdecLabel.Color(pallete[1]);
   incdecLabel.FontSize(8);
// rrRatioLabel.Font("
   m_panel.Add(incdecLabel);

   stepLabel.Create(0, "EAPanel:stepLabel", 0, m_xPanelDistance+10, m_yPanelDistance + 135);
   stepLabel.Description("STEP:");
   stepLabel.Color(pallete[1]);
   stepLabel.FontSize(9);
   m_panel.Add(stepLabel);

   stepSpinner.Create(0, "EAPanel:stepSpinner", 0, m_xPanelDistance+10, m_yPanelDistance + 155, 35, 18, 0.01, 0.01, 2);
   stepSpinner.SetMin(0.01);
   stepSpinner.SetMax(SymbolInfoDouble(Symbol(), SYMBOL_VOLUME_MAX));
   m_spinnersPanel.Add(stepSpinner);

   lotsLabel.Create(0, "EAPanel:lotsLabel", 0, m_xPanelDistance+70, m_yPanelDistance + 135);
   lotsLabel.Description("LOTS:");
   lotsLabel.Color(pallete[1]);
   lotsLabel.FontSize(9);
   m_panel.Add(lotsLabel);

   lotsSpinner.Create(0, "EAPanel:lotsSpinner", 0, m_xPanelDistance+70, m_yPanelDistance + 155, 35, 18, 0.01, 0.01, 2);
   lotsSpinner.SetMin(0.01);
   lotsSpinner.SetMax(SymbolInfoDouble(Symbol(), SYMBOL_VOLUME_MAX));
   m_spinnersPanel.Add(lotsSpinner);

   slippageLabel.Create(0, "EAPanel:slippageLabel", 0, m_xPanelDistance+130, m_yPanelDistance + 135);
   slippageLabel.Description("SLIPPAGE:");
   slippageLabel.Color(pallete[1]);
   slippageLabel.FontSize(9);
   m_panel.Add(slippageLabel);

   slippageSpinner.Create(0, "EAPanel:slippageSpinner", 0, m_xPanelDistance+130, m_yPanelDistance + 155, 95, 18, 0.00050, 0.00001, 5);
   slippageSpinner.SetMin(0.0);
   slippageSpinner.SetMax(1.0);
   m_spinnersPanel.Add(slippageSpinner);


   titleLabel.Create(0, "EAPanel:titleLabel", 0, m_xPanelDistance+35, m_yPanelDistance + m_yPanelSize - 15);
   titleLabel.Description("MQL5.com CONTEST PANEL by Investeo");
   titleLabel.Color(pallete[1]);
   titleLabel.FontSize(7);
   m_panel.Add(titleLabel);

   incPositionBtn.Create(0, "EAPanel:incPositionBtn", 0, m_xPanelDistance+10, m_yPanelDistance + 180, 110, 20);
   incPositionBtn.Description("Buy");
   incPositionBtn.BackColor(pallete[4]);
   m_panel.Add(incPositionBtn);

   decPositionBtn.Create(0, "EAPanel:decPositionBtn", 0, m_xPanelDistance+130, m_yPanelDistance + 180, 110, 20);
   decPositionBtn.Description("Sell");
   decPositionBtn.BackColor(pallete[5]);

   m_panel.Add(decPositionBtn);

   bidRect.Create(0, "EAPanel:bidRect", 0, m_xPanelDistance+10, m_yPanelDistance + 205, 110, 45);
   bidRect.Border(BORDER_FLAT);
   m_panel.Add(bidRect);

   askRect.Create(0, "EAPanel:askRect", 0, m_xPanelDistance+130, m_yPanelDistance + 205, 110, 45);
   askRect.Border(BORDER_FLAT);
   m_panel.Add(askRect);


   bidpart1Label.Create(0, "EAPanel:bidpart1Label", 0, m_xPanelDistance+20, m_yPanelDistance + 225);
   bidpart1Label.Description("0.00");
   bidpart1Label.Color(White);
   bidpart1Label.FontSize(11);
   m_panel.Add(bidpart1Label);

   bidpart2Label.Create(0, "EAPanel:bidpart2Label", 0, m_xPanelDistance+50, m_yPanelDistance + 210);
   bidpart2Label.Description("00");
   bidpart2Label.Color(White);
   bidpart2Label.FontSize(24);
   m_panel.Add(bidpart2Label);

   bidpart3Label.Create(0, "EAPanel:bidpart3Label", 0, m_xPanelDistance+90, m_yPanelDistance + 213);
   bidpart3Label.Description("0");
   bidpart3Label.Color(White);
   bidpart3Label.FontSize(9);
   m_panel.Add(bidpart3Label);

   askpart1Label.Create(0, "EAPanel:askpart1Label", 0, m_xPanelDistance+150, m_yPanelDistance + 225);
   askpart1Label.Description("0.00");
   askpart1Label.Color(White);
   askpart1Label.FontSize(11);
   m_panel.Add(askpart1Label);

   askpart2Label.Create(0, "EAPanel:askpart2Label", 0, m_xPanelDistance+180, m_yPanelDistance + 210);
   askpart2Label.Description("00");
   askpart2Label.Color(White);
   askpart2Label.FontSize(24);
   m_panel.Add(askpart2Label);

   askpart3Label.Create(0, "EAPanel:askpart3Label", 0, m_xPanelDistance+220, m_yPanelDistance + 213);
   askpart3Label.Description("0");
   askpart3Label.Color(White);
   askpart3Label.FontSize(9);
   m_panel.Add(askpart3Label);

   volatilityLabel1.Create(0, "EAPanel:volatilityLabel1", 0, m_xPanelDistance+10, m_yPanelDistance + 253);
   volatilityLabel1.Description("Volatility");
   volatilityLabel1.Color(pallete[1]);
   m_panel.Add(volatilityLabel1);

   volatilityLabel2.Create(0, "EAPanel:volatilityLabel2", 0, m_xPanelDistance+130, m_yPanelDistance + 253);
   volatilityLabel2.Description("min [pips] : 189");
   volatilityLabel2.Color(pallete[1]);
   m_panel.Add(volatilityLabel2);

   volatilitySpanSpinner.Create(0, "EAPanel:volatilitySpanSpinner", 0, m_xPanelDistance+70, m_yPanelDistance + 253, 35, 17, 1, 1, 0);
   volatilitySpanSpinner.SetMin(1);
   volatilitySpanSpinner.SetMax(60);
   m_spinnersPanel.Add(volatilitySpanSpinner);

   reversePositionBtn.Create(0, "EAPanel:reversePositionBtn", 0, m_xPanelDistance+10, m_yPanelDistance + 274, 110, 80);
   reversePositionBtn.Description("REVERSE");
   reversePositionBtn.BackColor(MintCream);
   m_panel.Add(reversePositionBtn);

   closePositionBtn.Create(0, "EAPanel:closePositionBtn", 0, m_xPanelDistance+130, m_yPanelDistance + 274, 110, 80);
   closePositionBtn.Description("CLOSE");
   closePositionBtn.BackColor(MintCream);

   m_panel.Add(closePositionBtn);

   quitEABtn.Create(0, "EAPanel:quitBtn", 0, m_xPanelDistance+10, m_yPanelDistance + 360, 230, 20);
   quitEABtn.Description("Quit");
   m_panel.Add(quitEABtn);

   RefreshPanel();
   ChartRedraw();
   return true;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CAdvancedEAPanel::HidePanel(void)
  {
   CChartObjectButton *element;

   if(m_visibleTab!=-1) { CleanTab(); HideTab(m_visibleTab); };
   m_panel.Clear();
   m_spinnersPanel.Clear();

   for(int i=0; i<3; i++) { element=m_sideButtons.At(i); element.X_Distance(-m_xSideBtnSize); }
   element=m_sideButtons.At(3);
   element.State(true);
   m_visibleTab=-1;
   for(int i=m_xPanelDistance; i>=-m_xPanelSize; i-=5)
     { ObjectSetInteger(m_chart_id,m_name,OBJPROP_XDISTANCE,i); element.X_Distance(i+m_xPanelSize); ChartRedraw(); };

   string hidearrow=" "; StringSetCharacter(hidearrow,0,26);
   element.Description(hidearrow);

   ChartRedraw();
   m_expanded=false;
   return true;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CAdvancedEAPanel::IsExpanded(void)
  {
   return m_expanded;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CAdvancedEAPanel::HideTab(int tab)
  {
   CChartObjectButton *t;
   CChartObjectButton *sideBtn;

   t=m_tabs.At(tab);
   sideBtn=m_sideButtons.At(tab);
   sideBtn.State(false);
   for(int i=m_xTabSize; i>=0; i-=10) { t.X_Size(i); ChartRedraw(); };
   t.Y_Size(0);
   t.X_Distance(-5);
   t.Y_Distance(-5);

   ChartRedraw();
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CAdvancedEAPanel::ShowTab(int tab)
  {
   CChartObjectButton *t;
   CChartObjectButton *sideBtn;

   t=m_tabs.At(tab);
   sideBtn=m_sideButtons.At(tab);

   t.X_Distance(m_xPanelDistance+m_xPanelSize+m_xSideBtnSize);
   t.Y_Distance(m_yPanelDistance);
   t.Y_Size(m_yTabSize);
   sideBtn.State(true);

   for(int i=0; i<=m_xTabSize; i+=10) { t.X_Size(i); ChartRedraw(); };
   t.State(false);
   t.Selectable(false);
   t.Background(false);
   t.ReadOnly(true);
  }
//+------------------------------------------------------------------+

void CAdvancedEAPanel::ClickTabHandler(int tab)
  {
   BackExtObjects();

   if(m_visibleTab!=-1)
     {
      CleanTab();
      HideTab(m_visibleTab);
     }

   if(m_visibleTab!=tab)
     {
      ShowTab(tab);

      m_visibleTab=tab;
      SetupTab();
     }
   else m_visibleTab=-1;

//Print("m_visibleTab = "+IntegerToString(m_visibleTab));
  }
//+------------------------------------------------------------------+
void CAdvancedEAPanel::SetupTab(void)
  {
   if(m_visibleTab==0) // trade plan tab
     {
      CChartObjectLabel *planLabel=new CChartObjectLabel();
      CChartObjectLabel *linesLabel=new CChartObjectLabel();

      CChartObjectLabel *entryPointLabel=new CChartObjectLabel();
      CChartObjectLabel *tpLabel = new CChartObjectLabel();
      CChartObjectLabel *slLabel = new CChartObjectLabel();
      CChartObjectLabel *actionLabel=new CChartObjectLabel();
      CChartObjectLabel *lotsLabel = new CChartObjectLabel();
      CChartObjectLabel *stepLabel = new CChartObjectLabel();

      CChartObjectEdit *entryPointEdit=new CChartObjectEdit();
      CChartObjectEdit *slEdit = new CChartObjectEdit();
      CChartObjectEdit *tpEdit = new CChartObjectEdit();

      CChartObjectTextSpinner *actionSpinner=new CChartObjectTextSpinner();
      CChartObjectSpinner *stepSpinner= new CChartObjectSpinner();
      CChartObjectSpinner *lotSpinner = new CChartObjectSpinner();

      CChartObjectButton *refreshPOIButton=new CChartObjectButton();
      CChartObjectButton *addButton=new CChartObjectButton();
      CChartObjectButton *insertButton = new CChartObjectButton();
      CChartObjectButton *removeButton = new CChartObjectButton();
      CChartObjectButton *clearAllButton=new CChartObjectButton();

      planLabel.Create(0, "tab0:planLabel", 0,  m_xPanelDistance + m_xPanelSize + m_xSideBtnSize + 15, m_yPanelDistance + 5);
      planLabel.FontSize(11);
      planLabel.Color(pallete[2]);
      planLabel.Description("Trading plan");
      m_tab0.Add(planLabel);

      linesLabel.Create(0, "tab0:linesLabel", 0,  m_xPanelDistance + m_xPanelSize + m_xSideBtnSize + 255, m_yPanelDistance + 5);
      linesLabel.FontSize(11);
      linesLabel.Color(pallete[2]);
      linesLabel.Description("Points of Interest");
      m_tab0.Add(linesLabel);

      entryPointLabel.Create(0, "tab0:entryPointLabel", 0,  m_xPanelDistance + m_xPanelSize + m_xSideBtnSize + 255, m_yPanelDistance + 280);
      entryPointLabel.FontSize(9);
      entryPointLabel.Color(pallete[2]);
      entryPointLabel.Description("Entry point:");
      m_tab0.Add(entryPointLabel);

      entryPointEdit.Create(0, "tab0:entryPointEdit", 0, m_xPanelDistance + m_xPanelSize + m_xSideBtnSize + 255,  m_yPanelDistance + 300, 65, 27);
      entryPointEdit.BackColor(MintCream);
      entryPointEdit.Color(pallete[2]);
      entryPointEdit.FontSize(11);
      entryPointEdit.Description("0.00000");
      m_tab0.Add(entryPointEdit);

      slLabel.Create(0, "tab0:slLabel", 0,  m_xPanelDistance + m_xPanelSize + m_xSideBtnSize + 340, m_yPanelDistance + 280);
      slLabel.FontSize(9);
      slLabel.Color(pallete[2]);
      slLabel.Description("S/L:");
      m_tab0.Add(slLabel);

      slEdit.Create(0, "tab0:slEdit", 0, m_xPanelDistance + m_xPanelSize + m_xSideBtnSize + 340,  m_yPanelDistance + 300, 65, 27);
      slEdit.BackColor(MintCream);
      slEdit.Color(pallete[2]);
      slEdit.FontSize(11);
      slEdit.Description("0.00000");
      m_tab0.Add(slEdit);

      tpLabel.Create(0, "tab0:tpLabel", 0,  m_xPanelDistance + m_xPanelSize + m_xSideBtnSize + 420, m_yPanelDistance + 280);
      tpLabel.FontSize(9);
      tpLabel.Color(pallete[2]);
      tpLabel.Description("T/P:");
      m_tab0.Add(tpLabel);

      tpEdit.Create(0, "tab0:tpEdit", 0, m_xPanelDistance + m_xPanelSize + m_xSideBtnSize + 420,  m_yPanelDistance + 300, 65, 27);
      tpEdit.BackColor(MintCream);
      tpEdit.Color(pallete[2]);
      tpEdit.FontSize(11);
      tpEdit.Description("0.00000");
      m_tab0.Add(tpEdit);


      actionLabel.Create(0, "tab0:actionLabel", 0,  m_xPanelDistance + m_xPanelSize + m_xSideBtnSize + 255, m_yPanelDistance + 340);
      actionLabel.FontSize(9);
      actionLabel.Color(pallete[2]);
      actionLabel.Description("Action:");
      m_tab0.Add(actionLabel);

      CArrayString *actions=new CArrayString();
      actions.Add("BUY");
      actions.Add("SELL");
      m_tab0.Add(actions);

      actionSpinner.Create(0,"tab0:tspinner:action",0,m_xPanelDistance+m_xPanelSize+m_xSideBtnSize+255,m_yPanelDistance+360,50,27,actions);
      m_txtSpinnersTab0.Add(actionSpinner);

      lotsLabel.Create(0, "tab0:lotsLabel", 0,  m_xPanelDistance + m_xPanelSize + m_xSideBtnSize + 340, m_yPanelDistance + 340);
      lotsLabel.FontSize(9);
      lotsLabel.Color(pallete[2]);
      lotsLabel.Description("Lots:");
      m_tab0.Add(lotsLabel);

      lotSpinner.Create(0, "tab0:nspinner:lots", 0, m_xPanelDistance + m_xPanelSize + m_xSideBtnSize + 340, m_yPanelDistance + 360, 50, 27, 0.01, 0.01, 2);
      lotSpinner.SetMin(0.01);
      lotSpinner.SetMax(SymbolInfoDouble(Symbol(), SYMBOL_VOLUME_MAX));
      m_spinnersTab0.Add(lotSpinner);

      stepLabel.Create(0, "tab0:stepLabel", 0,  m_xPanelDistance + m_xPanelSize + m_xSideBtnSize + 420, m_yPanelDistance + 340);
      stepLabel.FontSize(9);
      stepLabel.Color(pallete[2]);
      stepLabel.Description("Step:");
      m_tab0.Add(stepLabel);

      stepSpinner.Create(0, "tab0:nspinner:step", 0, m_xPanelDistance + m_xPanelSize + m_xSideBtnSize + 420, m_yPanelDistance + 360, 50, 27, 0.01, 0.01, 2);
      stepSpinner.SetMin(0.01);
      stepSpinner.SetMax(SymbolInfoDouble(Symbol(), SYMBOL_VOLUME_MAX));
      m_spinnersTab0.Add(stepSpinner);

      m_tradePlan=new CChartObjectListbox();
      m_tradePlan.Create(0,"tab0:tradePlan:",0,m_xPanelDistance+m_xPanelSize+m_xSideBtnSize+15,m_yPanelDistance+30,230,240,20,8);
      if(m_tradePlanList.Total()!=0) m_tradePlan.Add(m_tradePlanList);
      m_tradePlan.Refresh();
      m_tab0.Add(m_tradePlan);

      m_lineObjList=new CChartObjectListbox();
      m_lineObjList.Create(0,"tab0:lineObjList:",0,m_xPanelDistance+m_xPanelSize+m_xSideBtnSize+255,m_yPanelDistance+30,230,240,20,8);
      m_tab0.Add(m_lineObjList);

      insertButton.Create(0, "tab0:insertBtn", 0, m_xPanelDistance + m_xPanelSize + m_xSideBtnSize + 15, m_yPanelDistance + 280, 110, 50);
      insertButton.BackColor(MintCream);
      insertButton.Description("INSERT");
      m_tab0.Add(insertButton);

      removeButton.Create(0, "tab0:removeBtn", 0, m_xPanelDistance + m_xPanelSize + m_xSideBtnSize + 15, m_yPanelDistance + 340, 110, 50);
      removeButton.BackColor(MintCream);
      removeButton.Description("REMOVE");

      m_tab0.Add(removeButton);

      addButton.Create(0, "tab0:addBtn", 0, m_xPanelDistance + m_xPanelSize + m_xSideBtnSize + 135, m_yPanelDistance + 280, 110, 50);
      addButton.BackColor(MintCream);
      addButton.Description("ADD");
      m_tab0.Add(addButton);

      clearAllButton.Create(0, "tab0:clearAllBtn", 0, m_xPanelDistance + m_xPanelSize + m_xSideBtnSize + 135, m_yPanelDistance + 340, 110, 50);
      clearAllButton.BackColor(MintCream);
      clearAllButton.Description("CLEAR ALL");
      m_tab0.Add(clearAllButton);

      refreshPOIButton.Create(0, "tab0:refreshPOIBtn", 0, m_xPanelDistance + m_xPanelSize + m_xSideBtnSize + 435, m_yPanelDistance + 10, 50, 15);
      refreshPOIButton.BackColor(MintCream);
      refreshPOIButton.FontSize(8);
      refreshPOIButton.Description("refresh");
      m_tab0.Add(refreshPOIButton);

      RefreshPOIList();
      RefreshTab();
     }

   if(m_visibleTab==1) // pivots tab
     {
      int i,delta;
      int nPVT=9;

      string tf[]={ "M1","M5","M15","M30","H1","H4","D1","W1","MN" };
      string pivotTab[]={ "R4","R3","R2","R1","PP","S1","S2","S3","S4" };
      string descTab[]= { "Formula","Point","Show","Timeframe","Formula Set" };
      string formulaSet[]={ "Classic","Woodie","Camarilla" };

      string formulaPP[] = { "(H+L+C)/3", "(H+L+O)/3", "(H+L+C+O)/4", "(H+L+C+C)/4", "(H+L+O+O)/4", "(H+L)/2", "(H+C)/2", "(L+C)/2" };
      string formulaR1[] = { "2*PP-L", "C+RANGE*1.1/12" };
      string formulaR2[] = { "PP + RANGE", "C+RANGE*1.1/6" };
      string formulaR3[] = { "PP + RANGE*2", "H+2*(PP-L)", "C+RANGE*1.1/4" };
      string formulaR4[] = { "PP + RANGE*3", "C+RANGE*1.1/2" };
      string formulaS1[] = { "2*PP-H", "C-RANGE*1.1/12" };
      string formulaS2[] = { "PP - RANGE", "C-RANGE*1.1/6" };
      string formulaS3[] = { "PP - RANGE*2", "L-2*(H-PP)", "C-RANGE*1.1/4" };
      string formulaS4[] = { "PP - RANGE*3", "C-RANGE*1.1/2" };


      CArrayString *tabPtr=new CArrayString();

      for(i=0;i<nPVT;i++)
        {
         CChartObjectLabel *newLabel=new CChartObjectLabel();
         CChartObjectTextSpinner *newSpinner=new CChartObjectTextSpinner();
         CChartObjectEdit *newEdit=new CChartObjectEdit();
         CChartObjectButton *newButton=new CChartObjectButton();

         //Print(pivotTab[i]);
         delta=m_yPanelDistance+45+i*40;
         newLabel.Create(0,"tab1:label:"+pivotTab[i],0, m_xPanelDistance+m_xPanelSize+m_xSideBtnSize+15, delta);
         newLabel.Description(pivotTab[i]);
         newLabel.Color(Brown);
         m_tab1.Add(newLabel);

         switch(i)
           {
            case 0: tabPtr.AssignArray(formulaR4); break;
            case 1: tabPtr.AssignArray(formulaR3); break;
            case 2: tabPtr.AssignArray(formulaR2); break;
            case 3: tabPtr.AssignArray(formulaR1); break;
            case 4: tabPtr.AssignArray(formulaPP); break;
            case 5: tabPtr.AssignArray(formulaS1); break;
            case 6: tabPtr.AssignArray(formulaS2); break;
            case 7: tabPtr.AssignArray(formulaS3); break;
            case 8: tabPtr.AssignArray(formulaS4); break;

           }

         newSpinner.Create(0,"tab1:spinner:"+pivotTab[i],0,m_xPanelDistance+m_xPanelSize+m_xSideBtnSize+45,delta,100,20,tabPtr);
         m_txtSpinnersTab1.Add(newSpinner);

         newEdit.Create(0, "tab1:edit:"+pivotTab[i], 0, m_xPanelDistance+m_xPanelSize+m_xSideBtnSize+175, delta, 60, 20);
         newEdit.BackColor(White);
         m_pointsEditTab1.Add(newEdit);

         newButton.Create(0, "tab1:checkbox:"+pivotTab[i], 0, m_xPanelDistance+m_xPanelSize+m_xSideBtnSize+250, delta, 20, 20);
         newButton.FontSize(8);
         newButton.Description("N");
         m_showButtonsTab1.Add(newButton);

        }

      for(i=0;i<5;i++)
        {
         CChartObjectLabel *newLabel=new CChartObjectLabel();
         switch(i)
           {
            case 0: delta = 45; break;
            case 1: delta = 175; break;
            case 2: delta = 245; break;
            case 3: delta = 285; break;
            case 4: delta = 375; break;
           };
         newLabel.Create(0,"tab1:label:"+descTab[i],0, m_xPanelDistance+m_xPanelSize+m_xSideBtnSize+delta, m_yPanelDistance+20);
         newLabel.Description(descTab[i]);
         newLabel.Color(Brown);
         m_tab1.Add(newLabel);

        };

      tabPtr.AssignArray(tf);
      CChartObjectTextSpinner *tfSpinner=new CChartObjectTextSpinner();
      tfSpinner.Create(0,"tab1:tfspinner",0,m_xPanelDistance+m_xPanelSize+m_xSideBtnSize+285,m_yPanelDistance+45,60,20,tabPtr);
      //Print("Timeframe "+IntegerToString(TimeframeToInt(Period())));
      tfSpinner.SetIndex(TimeframeToInt(Period()));
      m_txtSpinnersTab1.Add(tfSpinner);

      tabPtr.AssignArray(formulaSet);
      CChartObjectTextSpinner *formulaSetSpinner=new CChartObjectTextSpinner();
      formulaSetSpinner.Create(0,"tab1:formulaSetspinner",0,m_xPanelDistance+m_xPanelSize+m_xSideBtnSize+375,m_yPanelDistance+45,95,20,tabPtr);
      m_txtSpinnersTab1.Add(formulaSetSpinner);

      CChartObjectSubChart *subChart=new CChartObjectSubChart();
      subChart.Create(0, "tab1:subChart", 0, m_xPanelDistance+m_xPanelSize+m_xSideBtnSize+285, m_yPanelDistance+85, 200,300);
      subChart.Scale(5);
      m_tab1.Add(subChart);

      tabPtr.Clear();
      delete tabPtr;

      RefreshTab();
     };

   if(m_visibleTab==2) // mtf indicators table tab
     {
      int i,delta;
      int nIND=7;
      string tf[]={ "M1","M5","M15","M30","H1","H4","D1","W1","MN" };
      string ind[]={ "EMA(3)","EMA(6)","EMA(9)","SMA(50)","SMA(200)","CCI(14)","RSI(21)" };
      ENUM_TIMEFRAMES tfr;

      for(i=0; i<nTF; i++)
        {
         switch(i)
           {
            case 0: tfr = PERIOD_M1; break;
            case 1: tfr = PERIOD_M5; break;
            case 2: tfr = PERIOD_M15; break;
            case 3: tfr = PERIOD_M30; break;
            case 4: tfr = PERIOD_H1; break;
            case 5: tfr = PERIOD_H4; break;
            case 6: tfr = PERIOD_D1; break;
            case 7: tfr = PERIOD_W1; break;
            case 8: tfr = PERIOD_MN1; break;
            default : tfr=PERIOD_CURRENT;
           };
         m_hEMA3.Add(iMA(Symbol(),tfr,3,0,MODE_EMA,PRICE_CLOSE));
         m_hEMA6.Add(iMA(Symbol(),tfr,6,0,MODE_EMA,PRICE_CLOSE));
         m_hEMA9.Add(iMA(Symbol(),tfr,9,0,MODE_EMA,PRICE_CLOSE));
         m_hSMA50.Add(iMA(Symbol(),tfr,50,0,MODE_SMA,PRICE_CLOSE));
         m_hSMA200.Add(iMA(Symbol(),tfr,200,0,MODE_SMA,PRICE_CLOSE));
         m_hCCI14.Add(iCCI(Symbol(),tfr,14,PRICE_CLOSE));
         m_hRSI21.Add(iRSI(Symbol(),tfr,21,PRICE_CLOSE));
        }

      m_mtfTable.Create(0,"tab2:t",0,7,9,m_xPanelDistance+m_xPanelSize+m_xSideBtnSize+80,m_yPanelDistance+60,40,35,Green,5,10);
      for(int row=0; row<7; row++)
         for(int col=0; col<9; col++)
            m_mtfTable.SetFontSize(row,col,8);

      for(i=0; i<nTF; i++)
        {
         CChartObjectLabel *newLabel=new CChartObjectLabel();
         //Print(tf[i]);
         delta=m_xPanelDistance+m_xPanelSize+m_xSideBtnSize+90+i*45;
         if(i==2 || i==3) delta-=5;
         newLabel.Create(0,"tab2:"+tf[i],0, delta , m_yPanelDistance+30);
         newLabel.Description(tf[i]);
         newLabel.Color(Brown);
         m_tab2.Add(newLabel);
        }

      for(i=0;i<nIND;i++)
        {
         CChartObjectLabel *newLabel=new CChartObjectLabel();
         //Print(ind[i]);
         delta=m_yPanelDistance+65+i*45;
         newLabel.Create(0,"tab2:"+ind[i],0, m_xPanelDistance+m_xPanelSize+m_xSideBtnSize+15, delta);
         newLabel.Description(ind[i]);
         newLabel.Color(Brown);
         m_tab2.Add(newLabel);
        }

      RefreshTab();
      ChartRedraw();
     }

  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CAdvancedEAPanel::CalculatePivots(MqlRates &candle)
  {
   int nPVT=9;
   int formulaIdx=-1;
   int i,j;

   string pivotTab[]={ "R4","R3","R2","R1","PP","S1","S2","S3","S4" };
   double pivotResults[9];
   string formula;

   string formulaPP[] = { "(H+L+C)/3", "(H+L+O)/3", "(H+L+C+O)/4", "(H+L+C+C)/4", "(H+L+O+O)/4", "(H+L)/2", "(H+C)/2", "(L+C)/2" };
   string formulaR1[] = { "2*PP-L", "C+RANGE*1.1/12" };
   string formulaR2[] = { "PP + RANGE", "C+RANGE*1.1/6" };
   string formulaR3[] = { "PP + RANGE*2", "H+2*(PP-L)", "C+RANGE*1.1/4" };
   string formulaR4[] = { "PP + RANGE*3", "C+RANGE*1.1/2" };
   string formulaS1[] = { "2*PP-H", "C-RANGE*1.1/12" };
   string formulaS2[] = { "PP - RANGE", "C-RANGE*1.1/6" };
   string formulaS3[] = { "PP - RANGE*2", "L-2*(H-PP)", "C-RANGE*1.1/4" };
   string formulaS4[] = { "PP - RANGE*3", "C-RANGE*1.1/2" };

   double RANGE,O,H,L,C;
   double PP,R1,R2,R3,R4,S1,S2,S3,S4;

   O = candle.open;
   H = candle.high;
   L = candle.low;
   C = candle.close;
   RANGE=H-L;

// calc PP first
   CArrayString *arrCmp=new CArrayString();
   formula=ObjectGetString(0,"tab1:spinner:"+pivotTab[4],OBJPROP_TEXT,0);

   arrCmp.AssignArray(formulaPP);

   for(i=0; i<arrCmp.Total(); i++)
         if(formula==arrCmp.At(i)) { formulaIdx=i; break; }

   switch(formulaIdx)
     {
      case 0 : PP = (H+L+C)/3.0; break;
      case 1 : PP = (H+L+O)/3.0; break;
      case 2 : PP = (H+L+C+O)/4.0; break;
      case 3 : PP = (H+L+C+C)/4.0; break;
      case 4 : PP = (H+L+O+O)/4.0; break;
      case 5 : PP = (H+L)/2.0; break;
      case 6 : PP = (H+C)/2.0; break;
      case 7 : PP = (L+C)/2.0; break;
      default: PP=0.0;
     };

   pivotResults[4]=NormalizeDouble(PP,Digits());

   for(i=0;i<nPVT;i++)
     {
      if(i==4) continue; // PP already calculated

      string curr_formula=ObjectGetString(0,"tab1:spinner:"+pivotTab[i],OBJPROP_TEXT,0);

      switch(i)
        {
         case 0 : arrCmp.AssignArray(formulaR4); break;
         case 1 : arrCmp.AssignArray(formulaR3); break;
         case 2 : arrCmp.AssignArray(formulaR2); break;
         case 3 : arrCmp.AssignArray(formulaR1); break;
         case 5 : arrCmp.AssignArray(formulaS1); break;
         case 6 : arrCmp.AssignArray(formulaS2); break;
         case 7 : arrCmp.AssignArray(formulaS3); break;
         case 8 : arrCmp.AssignArray(formulaS4); break;

        }

      for(j=0; j<arrCmp.Total(); j++)
        {
         //Print(curr_formula+" == "+arrCmp.At(j));
         if(curr_formula==arrCmp.At(j)) {  formulaIdx=j; break; }

        }
      if(i==0)
        {
         switch(formulaIdx)
           {
            case 0 : R4 = PP + RANGE*3.0; break;
            case 1 : R4 = C+RANGE*1.1/2.0; break;
            default: R4 = 0.0;
           };
         pivotResults[i]=NormalizeDouble(R4,Digits());
        };
      if(i==1)
        {
         switch(formulaIdx)
           {
            case 0 : R3 = PP + RANGE*2.0; break;
            case 1 : R3 = H+2.0*(PP-L); break;
            case 2 : R3 = C+RANGE*1.1/4.0; break;
            default: R3 = 0.0;
           };
         pivotResults[i]=NormalizeDouble(R3,Digits());
        };
      if(i==2)
        {
         switch(formulaIdx)
           {
            case 0 : R2 = PP + RANGE; break;
            case 1 : R2 = C+RANGE*1.1/6.0; break;
            default: R2 = 0.0;
           };
         pivotResults[i]=NormalizeDouble(R2,Digits());
        };
      if(i==3)
        {
         switch(formulaIdx)
           {
            case 0 : R1 = 2*PP-L; break;
            case 1 : R1 = C+RANGE*1.1/12.0; break;
            default: R1 = 0.0;
           };
         pivotResults[i]=NormalizeDouble(R1,Digits());
        }
      if(i==5)
        {
         switch(formulaIdx)
           {
            case 0 : S1 = 2*PP-H; break;
            case 1 : S1 = C-RANGE*1.1/12.0; break;
            default: S1 = 0.0;
           };
         pivotResults[i]=NormalizeDouble(S1,Digits());
        }
      if(i==6)
        {
         switch(formulaIdx)
           {
            case 0 : S2 = PP - RANGE; break;
            case 1 : S2 = C-RANGE*1.1/6.0; break;
            default: S2 = 0.0;
           };
         pivotResults[i]=NormalizeDouble(S2,Digits());
        }
      if(i==7)
        {
         switch(formulaIdx)
           {
            case 0 : S3 = PP - RANGE*2.0; break;
            case 1 : S3 = L-2.0*(H-PP); break;
            case 2 : S3 = C-RANGE*1.1/4.0; break;
            default: S3 = 0.0;
           };
         pivotResults[i]=NormalizeDouble(S3,Digits());
        };
      if(i==8)
        {
         switch(formulaIdx)
           {
            case 0 : S4 = PP - RANGE*3.0; break;
            case 1 : S4 = C-RANGE*1.1/2.0; break;
            default: S4 = 0.0;
           };
         pivotResults[i]=NormalizeDouble(S4,Digits());
        };
     };

   for(i=0;i<nPVT;i++)
     {

      ObjectSetString(0,"tab1:edit:"+pivotTab[i],OBJPROP_TEXT,DoubleToString(pivotResults[i],Digits()));
     }
   arrCmp.Clear();
   delete arrCmp;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CAdvancedEAPanel::CleanTab(void)
  {
   if(m_visibleTab==0)
     {
      // if trade plan not empty store
      if(m_tradePlan.Total()!=0)
        {
         m_tradePlanList.Clear();
         m_tradePlanList.AddArray(m_tradePlan.List());
        }
      m_tab0.Clear();
      m_txtSpinnersTab0.Clear();
      m_spinnersTab0.Clear();
      ChartRedraw();
     }

   if(m_visibleTab==1)
     {

      m_txtSpinnersTab1.Clear();
      m_showButtonsTab1.Clear();
      m_pointsEditTab1.Clear();
      m_tab1.Clear();

      ChartRedraw();
     }

   if(m_visibleTab==2)
     {
      for(int i=0; i<nTF; i++)
        {
         if(CheckPointer(m_hEMA3)) IndicatorRelease(m_hEMA3.At(i));
         if(CheckPointer(m_hEMA6)) IndicatorRelease(m_hEMA6.At(i));
         if(CheckPointer(m_hEMA9)) IndicatorRelease(m_hEMA9.At(i));
         if(CheckPointer(m_hSMA50)) IndicatorRelease(m_hSMA50.At(i));
         if(CheckPointer(m_hSMA200)) IndicatorRelease(m_hSMA200.At(i));
         if(CheckPointer(m_hCCI14)) IndicatorRelease(m_hCCI14.At(i));
         if(CheckPointer(m_hRSI21)) IndicatorRelease(m_hRSI21.At(i));
        }

      if(CheckPointer(m_hEMA3)) m_hEMA3.Clear();
      if(CheckPointer(m_hEMA6)) m_hEMA6.Clear();
      if(CheckPointer(m_hEMA9)) m_hEMA9.Clear();
      if(CheckPointer(m_hSMA50)) m_hSMA50.Clear();
      if(CheckPointer(m_hSMA200)) m_hSMA200.Clear();
      if(CheckPointer(m_hCCI14)) m_hCCI14.Clear();
      if(CheckPointer(m_hRSI21)) m_hRSI21.Clear();
      m_mtfTable.Delete();
      m_tab2.Clear();

      ChartRedraw();
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CAdvancedEAPanel::RefreshTab(void)
  {

   if(m_visibleTab==1)
     {
      string tf=ObjectGetString(0,"tab1:tfspinner",OBJPROP_TEXT,0);

      //Print("tf = "+tf);
      MqlRates candles[];
      int copied=CopyRates(Symbol(),StringToTimeframe(tf),0,2,candles);
      if(copied<=0)
         Print("Error copying price data ",GetLastError());
      // else Print("Copied ",ArraySize(candles)," bars"+"H = "+DoubleToString(candles[0].high,Digits()));

      CalculatePivots(candles[0]);

      // check buttons to press
      for(int i=0; i<nTF; i++)
        {
         CChartObjectButton *clickedButton=(CChartObjectButton *)m_showButtonsTab1.At(i);

         if(ObjectFind(0,"pivot:"+tf+":"+IntegerToString(i))>=0)
           {
            clickedButton.Description("Y");
            clickedButton.State(true);
           }
         else
           {
            clickedButton.Description("N");
            clickedButton.State(false);
           }
        };

     };

   if(m_visibleTab==2)
     {
      MqlTick currentTick;

      SymbolInfoTick(Symbol(),currentTick);
      
      double indVal1[2], indVal2[2], indVal3[2], 
             indVal4[2], indVal5[2], indVal6[2],
             indVal7[2]; 
         
      for(int i=0; i<nTF; i++)
        {
         if(CopyBuffer(m_hEMA3.At(i),0,0,2,indVal1)>0 && indVal1[0]!=EMPTY_VALUE)
           {
            m_mtfTable.SetText(0,i,DoubleToString(indVal1[0],Digits()-1));
            if(currentTick.ask>indVal1[0]) m_mtfTable.SetColor(0,i,Green);
            else  m_mtfTable.SetColor(0,i,Red);
           }
         if(CopyBuffer(m_hEMA6.At(i),0,0,2,indVal2)>0 && indVal2[0]!=EMPTY_VALUE)
           {
            m_mtfTable.SetText(1,i,DoubleToString(indVal2[0],Digits()-1));
            if(currentTick.ask>indVal2[0]) m_mtfTable.SetColor(1,i,Green);
            else  m_mtfTable.SetColor(1,i,Red);
           }
         if(CopyBuffer(m_hEMA9.At(i),0,0,2,indVal3)>0 && indVal3[0]!=EMPTY_VALUE)
           {
            m_mtfTable.SetText(2,i,DoubleToString(indVal3[0],Digits()-1));
            if(currentTick.ask>indVal3[0]) m_mtfTable.SetColor(2,i,Green);
            else  m_mtfTable.SetColor(2,i,Red);
           }
         if(CopyBuffer(m_hSMA50.At(i),0,0,2,indVal4)>0 && indVal4[0]!=EMPTY_VALUE)
           {
            m_mtfTable.SetText(3,i,DoubleToString(indVal4[0],Digits()-1));
            if(currentTick.ask>indVal4[0]) m_mtfTable.SetColor(3,i,Green);
            else  m_mtfTable.SetColor(3,i,Red);
           }
         if(CopyBuffer(m_hSMA200.At(i),0,0,2,indVal5)>0 && indVal5[0]!=EMPTY_VALUE)
           {
            m_mtfTable.SetText(4,i,DoubleToString(indVal5[0],Digits()-1));
            if(currentTick.ask>indVal5[0]) m_mtfTable.SetColor(4,i,Green);
            else  m_mtfTable.SetColor(4,i,Red);
           }
         if(CopyBuffer(m_hCCI14.At(i),0,0,2,indVal6)>0 && indVal6[0]!=EMPTY_VALUE)
           {
            m_mtfTable.SetText(5,i,DoubleToString(indVal6[0],2));
            if(indVal6[0]<-100.0) m_mtfTable.SetColor(5,i,Red);
            else if(indVal6[0]>100.0) m_mtfTable.SetColor(5,i,LightGreen);
            else m_mtfTable.SetColor(5,i,Orange);
           }
         if(CopyBuffer(m_hRSI21.At(i),0,0,1,indVal7)>0 && indVal7[0]!=EMPTY_VALUE)
           {
            m_mtfTable.SetText(6,i,DoubleToString(indVal7[0],2));
            if(indVal7[0]<30.0) m_mtfTable.SetColor(6,i,Red);
            else if(indVal7[0]>30.0 && indVal7[0]<50.0) m_mtfTable.SetColor(6,i,Orange);
            else if(indVal7[0]>=50.0 && indVal7[0]<70.0) m_mtfTable.SetColor(6,i,Gold);
            else if(indVal7[0]>=70.0) m_mtfTable.SetColor(6,i,LightGreen);

           }
        }

     }
   BackExtObjects();
   ChartRedraw();
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CAdvancedEAPanel::RefreshPanel(void)
  {

   if(m_expanded==true)
     {
      MqlTick newTick;
      SymbolInfoTick(Symbol(),newTick);

      double ask1=0,ask2=0,ask3=0,bid1=0,bid2=0,bid3=0;
      // split price into 3 segments depending on Digits()
      switch(Digits())
        {
         case 3:
           {
            double ask=newTick.ask;
            ask1 = MathFloor(ask);
            ask2 = MathFloor(ask*100)-MathFloor(ask)*100;
            ask3 = ask*1000-MathFloor(ask*100)*10;
            double bid=newTick.bid;
            bid1 = MathFloor(bid);
            bid2 = MathFloor(bid*100)-MathFloor(bid)*100;
            bid3 = bid*1000-MathFloor(bid*100)*10;
            break;
           }
         case 4:;
         case 5:
           {
            double ask=newTick.ask;
            ask1 = MathFloor(ask*100)/100.0;
            ask2 = MathFloor(ask*10000)-MathFloor(ask*100)*100;
            ask3 = MathFloor(ask*100000)-MathFloor(ask*10000)*10;
            double bid=newTick.bid;
            bid1 = MathFloor(bid*100)/100.0;
            bid2 = MathFloor(bid*10000)-MathFloor(bid*100)*100;
            bid3 = MathFloor(bid*100000)-MathFloor(bid*10000)*10;
           }
        }

      if(newTick.bid>m_lastTick.bid) ObjectSetInteger(0,"EAPanel:bidRect",OBJPROP_BGCOLOR,pallete[7]);
      else if(newTick.bid<m_lastTick.bid) ObjectSetInteger(0,"EAPanel:bidRect",OBJPROP_BGCOLOR,pallete[6]);
      else ObjectSetInteger(0,"EAPanel:bidRect",OBJPROP_BGCOLOR,pallete[0]);

      if(newTick.ask>m_lastTick.ask) ObjectSetInteger(0,"EAPanel:askRect",OBJPROP_BGCOLOR,pallete[7]);
      else if(newTick.ask<m_lastTick.ask) ObjectSetInteger(0,"EAPanel:askRect",OBJPROP_BGCOLOR,pallete[6]);
      else ObjectSetInteger(0,"EAPanel:askRect",OBJPROP_BGCOLOR,pallete[0]);

      switch(Digits())
        {
         case 3:
            ObjectSetString(0,"EAPanel:bidpart1Label",OBJPROP_TEXT,DoubleToString(bid1,0)+".");
            ObjectSetString(0,"EAPanel:askpart1Label",OBJPROP_TEXT,DoubleToString(ask1,0)+".");
            ObjectSetString(0,"EAPanel:bidpart2Label",OBJPROP_TEXT,StringFormat("%02.0f",bid2));
            ObjectSetString(0,"EAPanel:askpart2Label",OBJPROP_TEXT,StringFormat("%02.0f",ask2));
            if(bid1<100) ObjectSetInteger(0,"EAPanel:bidpart1Label",OBJPROP_XDISTANCE,m_xPanelDistance+28);
            if(ask1<100) ObjectSetInteger(0,"EAPanel:askpart1Label",OBJPROP_XDISTANCE,m_xPanelDistance+158);

            break;
         case 4:;
         case 5:
           {
            ObjectSetString(0,"EAPanel:bidpart1Label",OBJPROP_TEXT,DoubleToString(bid1,2));
            ObjectSetString(0,"EAPanel:askpart1Label",OBJPROP_TEXT,DoubleToString(ask1,2));
            ObjectSetString(0,"EAPanel:bidpart2Label",OBJPROP_TEXT,StringFormat("%02.0f",bid2));
            ObjectSetString(0,"EAPanel:askpart2Label",OBJPROP_TEXT,StringFormat("%02.0f",ask2));

            break;
           }
        };

      ObjectSetString(0,"EAPanel:bidpart3Label",OBJPROP_TEXT,DoubleToString(bid3,0));

      ObjectSetString(0,"EAPanel:askpart3Label",OBJPROP_TEXT,DoubleToString(ask3,0));

      // volatility last n minutes
      CChartObjectSpinner *volatilitySpan=m_spinnersPanel.At(3);
      int span=(int)volatilitySpan.GetValue();
      //Print(span);
      double High[];
      double Low[];
      ArraySetAsSeries(High,true);
      ArraySetAsSeries(Low,true);

      CopyHigh(Symbol(),PERIOD_M1,0,span,High);
      CopyLow(Symbol(),PERIOD_M1,0,span,Low);

      int highest= ArrayMaximum(High,0,span);
      int lowest = ArrayMinimum(Low,0,span);

      double v=(High[highest]-Low[lowest])/Point();
      ObjectSetString(0,"EAPanel:volatilityLabel2",OBJPROP_TEXT,"min [pips] : "+DoubleToString(v,0));

      // labels
      double sl=0.0,tp=0.0,posvol=0.0;

      if(PositionSelect(Symbol())==true)
        {
         sl = PositionGetDouble(POSITION_SL);
         tp = PositionGetDouble(POSITION_TP);
         posvol=PositionGetDouble(POSITION_VOLUME);
         double risk=0.0;
         double reward=0.0;

         if(PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_BUY)
            ObjectSetString(0,"EAPanel:positionLabel",OBJPROP_TEXT,Symbol()+" POSITION: "+"BUY");
         else if(PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_SELL)
            ObjectSetString(0,"EAPanel:positionLabel",OBJPROP_TEXT,Symbol()+" POSITION: "+"SELL");

         if(sl==0.0)
            ObjectSetString(0,"EAPanel:riskEdit",OBJPROP_TEXT,"INFINITE");
         else
           {

            if(PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_BUY)
               risk=PositionGetDouble(POSITION_PRICE_OPEN)-sl;
            else
            if(PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_SELL)
               risk=sl-PositionGetDouble(POSITION_PRICE_OPEN);

            risk=NormalizeDouble(risk/Point(),0);

            ObjectSetString(0,"EAPanel:riskEdit",OBJPROP_TEXT,DoubleToString(risk,0));

           }

         if(tp==0.0)
            ObjectSetString(0,"EAPanel:rewardEdit",OBJPROP_TEXT,"INFINITE");
         else
           {

            if(PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_BUY)
               reward=tp-PositionGetDouble(POSITION_PRICE_OPEN);
            else
            if(PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_SELL)
               reward=PositionGetDouble(POSITION_PRICE_OPEN)-tp;

            reward=NormalizeDouble(reward/Point(),0);

            ObjectSetString(0,"EAPanel:rewardEdit",OBJPROP_TEXT,DoubleToString(reward,0));
           }

         if(sl==0.0 || tp==0.0) ObjectSetString(0,"EAPanel:rrRatioEdit",OBJPROP_TEXT,"N/A");
         else
           {
            ObjectSetString(0,"EAPanel:rrRatioEdit",OBJPROP_TEXT,"1:"+DoubleToString(reward/risk,2));
           }

         ObjectSetString(0,"EAPanel:positionSizeEdit",OBJPROP_TEXT,DoubleToString(posvol,2)+" LOTS");
        }
      else
        {
         // no position
         ObjectSetString(0,"EAPanel:positionLabel",OBJPROP_TEXT,Symbol()+" POSITION: "+"NONE");
         ObjectSetString(0,"EAPanel:riskEdit",OBJPROP_TEXT,"NONE");
         ObjectSetString(0,"EAPanel:rewardEdit",OBJPROP_TEXT,"NONE");
         ObjectSetString(0,"EAPanel:rrRatioEdit",OBJPROP_TEXT,"N/A");
         ObjectSetString(0,"EAPanel:positionSizeEdit",OBJPROP_TEXT,"0.0 LOTS");
        };

      m_lastTick=newTick;
     };
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CAdvancedEAPanel::Refresh(void)
  {
   RefreshPanel();
   RefreshTab();
  }
//+------------------------------------------------------------------+
void CAdvancedEAPanel::T1ClickHandler(string sparam)
  {
// handle clicks on tab 1
//Print("T1ClickHandler");
   if(StringFind(sparam,"spinner")!=-1)
     {
      for(int i=0; i<m_txtSpinnersTab1.Total(); i++)
        {
         CChartObjectTextSpinner *clickedSpinner=(CChartObjectTextSpinner *)m_txtSpinnersTab1.At(i);
         if(StringFind(sparam,clickedSpinner.GetName())!=-1)
           {
            if(StringFind(sparam,"up")!=-1)
              {
               clickedSpinner.Inc();
              }
            if(StringFind(sparam,"down")!=-1)
              {
               clickedSpinner.Dec();
              }
            if(StringFind(sparam,"tf")!=-1)
              {
               CChartObjectSubChart *subChart=new CChartObjectSubChart();
               subChart.Create(0, "tab1:subChart", 0, m_xPanelDistance+m_xPanelSize+m_xSideBtnSize+285, m_yPanelDistance+85, 200,300);
               subChart.Scale(5);
               subChart.Period(StringToTimeframe(clickedSpinner.GetCurrentVal()));
               m_tab1.Add(subChart);

              }
            break;
           }
        };
     };
   if(StringFind(sparam,"checkbox")!=-1)
     {
      for(int i=0; i<m_showButtonsTab1.Total(); i++)
        {
         CChartObjectButton *clickedButton=(CChartObjectButton *)m_showButtonsTab1.At(i);
         if(StringFind(sparam,clickedButton.Name())!=-1)
            if(clickedButton.Description()=="N")
              {
               CChartObjectEdit *pivotEdit=(CChartObjectEdit*)m_pointsEditTab1.At(i);

               string tf=ObjectGetString(0,"tab1:tfspinner",OBJPROP_TEXT,0);

               CChartObjectHLine *pivotLine=new CChartObjectHLine();

               clickedButton.Description("Y");
               clickedButton.State(true);

               pivotLine.Create(0, "pivot:"+tf+":"+IntegerToString(i), 0, StringToDouble(pivotEdit.Description()));
               pivotLine.Style(STYLE_DASHDOT);
               pivotLine.SetInteger(OBJPROP_BACK, true);
               m_pivots.Add(pivotLine);
               //Print("SHOW HORIZONTAL LINE AT"+pivotEdit.Description());
              }
         else if(clickedButton.Description()=="Y")
           {
            CChartObjectHLine *pivotLine=new CChartObjectHLine();
            string tf=ObjectGetString(0,"tab1:tfspinner",OBJPROP_TEXT,0);

            pivotLine.Attach(0,"pivot:"+tf+":"+IntegerToString(i),0,0);

            pivotLine.Delete();
            delete pivotLine;

            clickedButton.Description("N");
            clickedButton.State(false);
            //Print("HIDE HORIZONTAL LINE AT");
           }

        };
     };
   if(StringFind(sparam,"formulaSetspinner")!=-1)
     {
      CChartObjectTextSpinner *cSpinner=(CChartObjectTextSpinner *)m_txtSpinnersTab1.At(10);
      string formula=cSpinner.Description();
      //Print("FORMULA = "+formula);
      // set formulas according to formula sets
      if(formula=="Classic")
        {
         cSpinner=(CChartObjectTextSpinner *)m_txtSpinnersTab1.At(0);
         cSpinner.SetIndex(0);
         cSpinner=(CChartObjectTextSpinner *)m_txtSpinnersTab1.At(1);
         cSpinner.SetIndex(0);
         cSpinner=(CChartObjectTextSpinner *)m_txtSpinnersTab1.At(2);
         cSpinner.SetIndex(0);
         cSpinner=(CChartObjectTextSpinner *)m_txtSpinnersTab1.At(3);
         cSpinner.SetIndex(0);
         cSpinner=(CChartObjectTextSpinner *)m_txtSpinnersTab1.At(4);
         cSpinner.SetIndex(0);
         cSpinner=(CChartObjectTextSpinner *)m_txtSpinnersTab1.At(5);
         cSpinner.SetIndex(0);
         cSpinner=(CChartObjectTextSpinner *)m_txtSpinnersTab1.At(6);
         cSpinner.SetIndex(0);
         cSpinner=(CChartObjectTextSpinner *)m_txtSpinnersTab1.At(7);
         cSpinner.SetIndex(0);
         cSpinner=(CChartObjectTextSpinner *)m_txtSpinnersTab1.At(8);
         cSpinner.SetIndex(0);
        }
      if(formula=="Woodie")
        {
         cSpinner=(CChartObjectTextSpinner *)m_txtSpinnersTab1.At(0);
         cSpinner.SetIndex(0);
         cSpinner=(CChartObjectTextSpinner *)m_txtSpinnersTab1.At(1);
         cSpinner.SetIndex(1);
         cSpinner=(CChartObjectTextSpinner *)m_txtSpinnersTab1.At(2);
         cSpinner.SetIndex(0);
         cSpinner=(CChartObjectTextSpinner *)m_txtSpinnersTab1.At(3);
         cSpinner.SetIndex(0);
         cSpinner=(CChartObjectTextSpinner *)m_txtSpinnersTab1.At(4);
         cSpinner.SetIndex(4);
         cSpinner=(CChartObjectTextSpinner *)m_txtSpinnersTab1.At(5);
         cSpinner.SetIndex(0);
         cSpinner=(CChartObjectTextSpinner *)m_txtSpinnersTab1.At(6);
         cSpinner.SetIndex(0);
         cSpinner=(CChartObjectTextSpinner *)m_txtSpinnersTab1.At(7);
         cSpinner.SetIndex(1);
         cSpinner=(CChartObjectTextSpinner *)m_txtSpinnersTab1.At(8);
         cSpinner.SetIndex(0);
        }
      if(formula=="Camarilla")
        {
         cSpinner=(CChartObjectTextSpinner *)m_txtSpinnersTab1.At(0);
         cSpinner.SetIndex(1);
         cSpinner=(CChartObjectTextSpinner *)m_txtSpinnersTab1.At(1);
         cSpinner.SetIndex(2);
         cSpinner=(CChartObjectTextSpinner *)m_txtSpinnersTab1.At(2);
         cSpinner.SetIndex(1);
         cSpinner=(CChartObjectTextSpinner *)m_txtSpinnersTab1.At(3);
         cSpinner.SetIndex(1);
         cSpinner=(CChartObjectTextSpinner *)m_txtSpinnersTab1.At(4);
         cSpinner.SetIndex(0);
         cSpinner=(CChartObjectTextSpinner *)m_txtSpinnersTab1.At(5);
         cSpinner.SetIndex(1);
         cSpinner=(CChartObjectTextSpinner *)m_txtSpinnersTab1.At(6);
         cSpinner.SetIndex(1);
         cSpinner=(CChartObjectTextSpinner *)m_txtSpinnersTab1.At(7);
         cSpinner.SetIndex(2);
         cSpinner=(CChartObjectTextSpinner *)m_txtSpinnersTab1.At(8);
         cSpinner.SetIndex(1);
        }
     };

   RefreshTab();

  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CAdvancedEAPanel::T0ClickHandler(string sparam)
  {
// handle clicks on tab 0
//Print("T0ClickHandler");
   if(StringFind(sparam,"lineObjList")!=-1)
     {
      if(StringFind(sparam,"up")!=-1)
         m_lineObjList.Up();
      if(StringFind(sparam,"down")!=-1)
         m_lineObjList.Down();

      //m_lineObjList.Info();

      string lineStr=m_lineObjList.Get();
      string priceStr=StringSubstr(lineStr,0,StringFind(lineStr," "));
      m_entryPoint=StringToDouble(priceStr);
      //Print("PRICE = "+priceStr);
      ObjectSetString(0,"tab0:entryPointEdit",OBJPROP_TEXT,priceStr);

      CChartObjectHLine *suggestedEntryLine=new CChartObjectHLine();
      suggestedEntryLine.Create(0, "tab0:suggestedEntryLine", 0, m_entryPoint);
      suggestedEntryLine.Color(LightSkyBlue);
      suggestedEntryLine.Width(3);
      suggestedEntryLine.Background(true);
      suggestedEntryLine.Selectable(true);
      suggestedEntryLine.Selected(true);
      m_tab0.Add(suggestedEntryLine);

     };
   if(StringFind(sparam,"refreshPOI")!=-1)
     {
      RefreshPOIList();
     }

   if(StringFind(sparam,"addBtn")!=-1)
     {
      string tradeEntry;
      CChartObjectTextSpinner *actionSpinner=m_txtSpinnersTab0.At(0);
      CChartObjectSpinner *lotsSpinner=m_spinnersTab0.At(0);
      tradeEntry=actionSpinner.GetCurrentVal()+" "+DoubleToString(lotsSpinner.GetValue(),2)+"@"
                 +ObjectGetString(0,"tab0:entryPointEdit",OBJPROP_TEXT,0)+" SL "+ObjectGetString(0,"tab0:slEdit",OBJPROP_TEXT,0)+" TP "
                 +ObjectGetString(0,"tab0:slEdit",OBJPROP_TEXT,0);

      m_tradePlan.Add(tradeEntry);
      m_tradePlan.Refresh();

     };

   if(StringFind(sparam,"insertBtn")!=-1)
     {
      string tradeEntry;
      CChartObjectTextSpinner *actionSpinner=m_txtSpinnersTab0.At(0);
      CChartObjectSpinner *lotsSpinner=m_spinnersTab0.At(0);
      tradeEntry=actionSpinner.GetCurrentVal()+" "+DoubleToString(lotsSpinner.GetValue(),2)+"@"
                 +ObjectGetString(0,"tab0:entryPointEdit",OBJPROP_TEXT,0)+" SL "+ObjectGetString(0,"tab0:slEdit",OBJPROP_TEXT,0)+" TP "
                 +ObjectGetString(0,"tab0:slEdit",OBJPROP_TEXT,0);

      m_tradePlan.Insert(tradeEntry, m_tradePlan.Idx());
      m_tradePlan.Refresh();
     };

   if(StringFind(sparam,"removeBtn")!=-1)
     {

      m_tradePlan.Remove(m_tradePlan.Idx());
      m_tradePlan.Refresh();
     };

   if(StringFind(sparam,"clearAllBtn")!=-1)
     {

      m_tradePlan.RemoveAll();
      m_tradePlan.Refresh();
     };

   if(StringFind(sparam,"tradePlan")!=-1)
     {
      if(StringFind(sparam,"up")!=-1)
         m_tradePlan.Up();
      if(StringFind(sparam,"down")!=-1)
         m_tradePlan.Down();
     }

   if(StringFind(sparam,"tspinner")!=-1)
     {
      //Print("Text Spinner clicked in tab0");
      for(int i=0; i<m_txtSpinnersTab0.Total(); i++)
        {
         CChartObjectTextSpinner *clickedSpinner=(CChartObjectTextSpinner *)m_txtSpinnersTab0.At(i);
         //Print("sparam = "+sparam+" clickedSpinner = "+clickedSpinner.GetName());
         if(StringFind(sparam,clickedSpinner.GetName())!=-1)
           {
            if(StringFind(sparam,"up")!=-1)
              {
               clickedSpinner.Inc();
               //Print("up:"+sparam);
              }
            if(StringFind(sparam,"down")!=-1)
              {
               clickedSpinner.Dec();
               //Print("T0ClickHandler:tspinner:up");
              }
           }
        };
     };

   if(StringFind(sparam,"nspinner")!=-1)
     {
      //Print(" Spinner clicked in tab0");
      for(int i=0; i<m_spinnersTab0.Total(); i++)
        {
         CChartObjectSpinner *clickedSpinner=(CChartObjectSpinner *)m_spinnersTab0.At(i);
         //Print("sparam = "+sparam+" clickedSpinner = "+clickedSpinner.Name());
         if(StringFind(sparam,clickedSpinner.Name())!=-1)
           {
            if(StringFind(sparam,"up")!=-1)
              {
               clickedSpinner.Inc();
               //Print("up:"+sparam);
              }
            if(StringFind(sparam,"down")!=-1)
              {
               clickedSpinner.Dec();
               //Print("T0ClickHandler:tspinner:up");
              }
            if(i==1) // step spinner clicked
              {
               CChartObjectSpinner *lotsSpinner=(CChartObjectSpinner *)m_spinnersTab0.At(0);
               lotsSpinner.SetStepSize(clickedSpinner.GetValue());
              }

           }
        };
     };

  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CAdvancedEAPanel::T0DragHandler(string sparam)
  {
//Print("T0DragHandler");

   if(StringFind(sparam,"suggestedEntryLine")!=-1)
     {
      // set tp or sl depending on action type
      color  lineColor=LightGreen;
      string lineprefix="tab0:suggestedEntry";
      CChartObjectHLine *newLine=new CChartObjectHLine();
      double priceDetected=ObjectGetDouble(0,"tab0:suggestedEntryLine",OBJPROP_PRICE,0);
      ObjectSetDouble(0,"tab0:suggestedEntryLine",OBJPROP_PRICE,m_entryPoint);
      string action=ObjectGetString(0,"tab0:tspinner:action",OBJPROP_TEXT,0);
      if(action=="BUY")
        {
         if(priceDetected<m_entryPoint) // SL
           { newLine.Create(0,lineprefix+"_SL",0,priceDetected); lineColor=Orange; }
         else //TP
         newLine.Create(0,lineprefix+"_TP",0,priceDetected);
        };
      if(action=="SELL")
        {
         if(priceDetected>m_entryPoint) // SL
           { newLine.Create(0,lineprefix+"_SL",0,priceDetected); lineColor=Orange; }
         else //TP
         newLine.Create(0,lineprefix+"_TP",0,priceDetected);
        }

      newLine.Color(lineColor);
      newLine.Width(3);
      newLine.Background(true);
      m_tab0.Add(newLine);
      ChartRedraw();

      // insert tp or sl level to edit field
      if(lineColor==Orange) // new sl
         ObjectSetString(0,"tab0:slEdit",OBJPROP_TEXT,DoubleToString(priceDetected,Digits()));
      else // new tp
      ObjectSetString(0,"tab0:tpEdit",OBJPROP_TEXT,DoubleToString(priceDetected,Digits()));

     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CAdvancedEAPanel::EAPanelClickHandler(string sparam)
  {
// handle clicks on EAPanel (main window)
//Print("EAPanelClickHandler");

   if(StringFind(sparam,"Spinner")!=-1)
     {
      for(int i=0; i<m_spinnersPanel.Total(); i++)
        {
         CChartObjectSpinner *clickedSpinner=(CChartObjectSpinner *)m_spinnersPanel.At(i);
         //Print("sparam = "+sparam+" clickedSpinner = "+clickedSpinner.Name());
         if(StringFind(sparam,clickedSpinner.Name())!=-1)
           {
            if(StringFind(sparam,"up")!=-1)
              {
               clickedSpinner.Inc();
               //Print("up:"+sparam);
              }
            if(StringFind(sparam,"down")!=-1)
              {
               clickedSpinner.Dec();
               //Print("EAPanelClickHandler:tspinner:up");
              }
            if(i==0) // step spinner clicked
              {
               CChartObjectSpinner *lotsSpinner=(CChartObjectSpinner *)m_spinnersPanel.At(1);
               lotsSpinner.SetStepSize(clickedSpinner.GetValue());
              }

           }
        };
      RefreshPanel();
     }
   if(StringFind(sparam,"incPosition")!=-1)
     {
      // instant buy
      CChartObjectSpinner *lotsSpinner=(CChartObjectSpinner *)m_spinnersPanel.At(1);
      CChartObjectSpinner *slippageSpinner=(CChartObjectSpinner *)m_spinnersPanel.At(2);

      // read TP and SL of open position if available
      double sl = 0.0;
      double tp = 0.0;
      double posvol;

      double lots=lotsSpinner.GetValue();

      if(PositionSelect(Symbol())==true)
        {
         sl = PositionGetDouble(POSITION_SL);
         tp = PositionGetDouble(POSITION_TP);
         posvol=PositionGetDouble(POSITION_VOLUME);
         if(PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_SELL && lots>=posvol) { sl=0.0; tp=0.0; } // reverse position, clear stops and tp
        }
      else Print("No position");
      //
      double slippage=slippageSpinner.GetValue()/Point();
      m_trade.SetDeviationInPoints((ulong)slippage);
      m_trade.PositionOpen(Symbol(),ORDER_TYPE_BUY,lots,m_lastTick.ask,sl,tp,TimeToString(TimeCurrent()));

     }
   if(StringFind(sparam,"decPosition")!=-1)
     {
      // instant sell
      CChartObjectSpinner *lotsSpinner=(CChartObjectSpinner *)m_spinnersPanel.At(1);
      CChartObjectSpinner *slippageSpinner=(CChartObjectSpinner *)m_spinnersPanel.At(2);

      // read TP and SL of open position if available
      double sl = 0.0;
      double tp = 0.0;
      double posvol;

      double lots=lotsSpinner.GetValue();

      if(PositionSelect(Symbol())==true)
        {
         sl = PositionGetDouble(POSITION_SL);
         tp = PositionGetDouble(POSITION_TP);
         posvol=PositionGetDouble(POSITION_VOLUME);
         if(PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_BUY && lots>=posvol) { sl=0.0; tp=0.0; } // reverse position, clear stops and tp
        }
      else Print("No position");

      double slippage=slippageSpinner.GetValue()/Point();
      m_trade.SetDeviationInPoints((ulong)slippage);
      m_trade.PositionOpen(Symbol(),ORDER_TYPE_SELL,lots,m_lastTick.bid,sl,tp,TimeToString(TimeCurrent()));


     }
   if(StringFind(sparam,"reversePosition")!=-1)
     {
      CChartObjectSpinner *slippageSpinner=(CChartObjectSpinner *)m_spinnersPanel.At(2);

      // read TP and SL of open position if available
      double sl = 0.0;
      double tp = 0.0;
      double posvol;

      if(PositionSelect(Symbol())==true)
        {
         posvol=PositionGetDouble(POSITION_VOLUME);
         double slippage=slippageSpinner.GetValue()/Point();
         m_trade.SetDeviationInPoints((ulong)slippage);

         if(PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_BUY)
            m_trade.PositionOpen(Symbol(),ORDER_TYPE_SELL,2*posvol,m_lastTick.bid,sl,tp,TimeToString(TimeCurrent()));
         else if(PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_SELL)
            m_trade.PositionOpen(Symbol(),ORDER_TYPE_BUY,2*posvol,m_lastTick.ask,sl,tp,TimeToString(TimeCurrent()));

        }
      else Print("No position");

     }

   if(StringFind(sparam,"closePosition")!=-1)
     {
      CChartObjectSpinner *slippageSpinner=(CChartObjectSpinner *)m_spinnersPanel.At(2);

      // close current symbol position
      double sl = 0.0;
      double tp = 0.0;
      double posvol;

      if(PositionSelect(Symbol())==true)
        {
         posvol=PositionGetDouble(POSITION_VOLUME);
         double slippage=slippageSpinner.GetValue()/Point();
         m_trade.SetDeviationInPoints((ulong)slippage);

         if(PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_BUY)
            m_trade.PositionOpen(Symbol(),ORDER_TYPE_SELL,posvol,m_lastTick.bid,sl,tp,TimeToString(TimeCurrent()));
         else if(PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_SELL)
            m_trade.PositionOpen(Symbol(),ORDER_TYPE_BUY,posvol,m_lastTick.ask,sl,tp,TimeToString(TimeCurrent()));

        }
      else Print("No position");

     }
   if(StringFind(sparam,"quitBtn")!=-1)
     {
      GlobalVariableDel("EAPanel");
      this.Delete();
      ExpertRemove();
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CAdvancedEAPanel::EditHandler(string sparam)
  {
   if(StringFind(sparam,"entryPointEdit")!=-1)
     {
      string priceStr=ObjectGetString(0,"tab0:entryPointEdit",OBJPROP_TEXT);
      m_entryPoint=StringToDouble(priceStr);
      ObjectSetDouble(0,"tab0:suggestedEntryLine",OBJPROP_PRICE,m_entryPoint);
     }
   if(StringFind(sparam,"slEdit")!=-1)
     {
      string slStr=ObjectGetString(0,"tab0:slEdit",OBJPROP_TEXT);
      ObjectSetDouble(0,"tab0:suggestedEntry_SL",OBJPROP_PRICE,StringToDouble(slStr));

     }
   if(StringFind(sparam,"tpEdit")!=-1)
     {
      string tpStr=ObjectGetString(0,"tab0:tpEdit",OBJPROP_TEXT);
      ObjectSetDouble(0,"tab0:suggestedEntry_TP",OBJPROP_PRICE,StringToDouble(tpStr));
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CAdvancedEAPanel::RefreshPOIList(void)
  {
// find all objects and populate Points of Interest
   int nObj=ObjectsTotal(0);
   string objName;

   m_lineObjList.RemoveAll();
   for(int i=0; i<nObj; i++)
     {
      objName=ObjectName(0,i);
      if((StringFind(objName,"pivot:")!=-1) || (StringFind(objName,"Horizontal Line")!=-1))
        {
         m_lineObjList.Add(DoubleToString(ObjectGetDouble(0,objName,OBJPROP_PRICE),Digits())+" "+objName);
         ObjectSetInteger(0,objName,OBJPROP_BACK,true);
         ObjectSetInteger(0,objName,OBJPROP_SELECTED,false);
         //Print("Found obj " + objName + " @ " + DoubleToString(ObjectGetDouble(0, objName, OBJPROP_PRICE))); 
        }
      if(StringFind(objName," Fibo ")!=-1)
        {
         double price1 = ObjectGetDouble(0, objName, OBJPROP_PRICE, 0);
         double price2 = ObjectGetDouble(0, objName, OBJPROP_PRICE, 1);

         for(int j=0; j<ObjectGetInteger(0,objName,OBJPROP_LEVELS); j++)
           {
            double level=ObjectGetDouble(0,objName,OBJPROP_LEVELVALUE,j);
            double priceLevel;

            if(price1<price2)
               priceLevel=price2 -(price2-price1)*level;
            else priceLevel=price2+(price1-price2)*level;

            m_lineObjList.Add(DoubleToString(priceLevel,Digits())+" "+DoubleToString(level,3)+" "+objName);
            //Print("Found fibo " + objName + " @ " + DoubleToString(ObjectGetDouble(0, objName, OBJPROP_PRICE, j)));
           }
        }
      m_lineObjList.Sort(0);
      m_lineObjList.Refresh();
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CAdvancedEAPanel::BackExtObjects(void)
  {
// find objects not belonging to EAPanel and move them into background
   string panelObjects[]={ "tab0","tab1:","tab2:","EAPanel","EAPanelside:","pivot:" };
   int pCnt=6;
   bool isExtObject=false;

   for(int i=0; i<ObjectsTotal(0); i++)
     {
      for(int j=0; j<pCnt; j++)
                if(StringFind(ObjectName(0,i),panelObjects[j])!=-1) { isExtObject=true; break; }

      if(isExtObject==false) ObjectSetInteger(0,ObjectName(0,i),OBJPROP_BACK,true);

      isExtObject=false;
     };
  }
//+------------------------------------------------------------------+
