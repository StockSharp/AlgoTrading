//+------------------------------------------------------------------+
//|                                          AdvancedEAPanelDemo.mq5 |
//|                                      Copyright 2010, Investeo.pl |
//|                                                http:/Investeo.pl |
//+------------------------------------------------------------------+
#property copyright "Copyright 2010, Investeo.pl"
#property link      "http:/Investeo.pl"
#property version   "1.00"

#include <AdvancedEAPanel.mqh>

CAdvancedEAPanel panel;

string pName="EAPanel";
bool   firstRun=true;

#define SIDE_BUTTONS_CNT 4
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
   if(firstRun==true)
     {
      panel.Create(0,pName,0,10,10,250,400,Snow);
      for(int i=0; i<SIDE_BUTTONS_CNT; i++) panel.CreateTab(i,Snow);
      panel.CreateSideButtons(SIDE_BUTTONS_CNT, 30);
      panel.ShowPanel();
     }
   firstRun=false;

   ChartRedraw();

//---
   return(0);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//---
   panel.Refresh();
  }
//+------------------------------------------------------------------+
void OnChartEvent(const int id,
                  const long &lparam,
                  const double &dparam,
                  const string &sparam)
  {
//--- Check the event by pressing a mouse button
   if(id==CHARTEVENT_OBJECT_CLICK)
     {
      //Print("Clicked "+sparam);
      if(StringFind(sparam,"EAPanel")!=-1)
        {
         ObjectSetInteger(0,sparam,OBJPROP_STATE,false);
         panel.EAPanelClickHandler(sparam);
        };

      if(StringFind(sparam,"tab0:")!=-1)
        {
         ObjectSetInteger(0,sparam,OBJPROP_STATE,false);
         panel.T0ClickHandler(sparam);
        };

      if(StringFind(sparam,"tab1:")!=-1)
         panel.T1ClickHandler(sparam);

      if(StringFind(sparam,pName+"side")!=-1)
        {
         string btnNumberStr = StringSubstr(sparam, StringLen(sparam)-1, 1);
         int clickedBtnIndex = (int)StringToInteger(btnNumberStr);

         if(clickedBtnIndex==3) { if(panel.IsExpanded()==true) panel.HidePanel(); else panel.ShowPanel(); return; }
         panel.ClickTabHandler(clickedBtnIndex);
        };
      ChartRedraw();
     }

   if(id==CHARTEVENT_OBJECT_DRAG)
     {
      if(StringFind(sparam,"suggestedEntryLine")!=-1)
         panel.T0DragHandler(sparam);
     }

   if(id==CHARTEVENT_OBJECT_ENDEDIT)
     {
      panel.EditHandler(sparam);
     }
  }
//+------------------------------------------------------------------+
