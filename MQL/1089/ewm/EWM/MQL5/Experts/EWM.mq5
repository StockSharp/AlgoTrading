//+------------------------------------------------------------------+
//|                                                          EWM.mq5 |
//|                                  Copyright 2012, Roman Martynyuk |
//|                                           http://www.dml-ewa.ru/ |
//+------------------------------------------------------------------+
#property copyright "Copyright 2012, Roman Martynyuk"
#property link      "http://www.dml-ewa.ru/"
#property version   "3.0"

#include <EWM\ewa\ewa.mqh>
#include <EWM\ewm\ewm.mqh>
#include <EWM\p\p.mqh>
#include <EWM\parser.mqh>

Parser parser;
Ewm ewm;
P p(GetPointer(ewm.wave_labels));
Ewa ewa(GetPointer(ewm.wave_labels));

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
{
  ChartSetInteger(0, CHART_EVENT_OBJECT_DELETE, true);
  ewm.on_init();
  p.on_init();
  ewa.on_init();
  ChartRedraw();
  return(0);
}
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
{
  ewm.on_deinit(reason);
  p.on_deinit(reason);
  ewa.on_deinit(reason);
  ChartRedraw();
}

void OnTick()
{
  ewm.set_time_vertical0();
}

//+------------------------------------------------------------------+
//| ChartEvent function                                              |
//+------------------------------------------------------------------+
void OnChartEvent(const int id, const long &lparam, const double &dparam, const string &sparam)
{
  if(id == CHARTEVENT_OBJECT_DRAG)
  {
    if(StringFind(sparam, NAME_LABEL) >= 0)
    {
      ewm.on_label_drag(sparam);
      p.pitchforks.update();
    }
    else if(StringFind(sparam, NAME_WAVE) >= 0 || StringFind(sparam, NAME_AUTO_WAVE) >= 0)
    {
      ewm.on_wave_drag(sparam);
      p.pitchforks.update();
    }
  }
  if(id == CHARTEVENT_CLICK || id == CHARTEVENT_CHART_CHANGE || id == CHARTEVENT_OBJECT_CHANGE)
  {
    ewm.wave_labels.correct();
  }
  if(id == CHARTEVENT_OBJECT_DELETE)
  {
    if(StringFind(sparam, NAME_WAVE) >= 0 || StringFind(sparam, NAME_AUTO_WAVE) >= 0)
    {
      p.on_remove_pitchfork(sparam);
      ewm.on_remove_wave(sparam);
    }
  }
  if(id == CHARTEVENT_OBJECT_CLICK)
  {
    if(StringFind(sparam, NAME_LABEL) >= 0)
    {
      ewm.on_label_click(sparam);
    }
    else if(StringFind(sparam, NAME_WAVE) >= 0 || StringFind(sparam, NAME_AUTO_WAVE) >= 0)
    {
      ewm.on_wave_click(sparam);
      if(StringFind(sparam, NAME_AUTO_WAVE) >= 0)
      {
       ewa.get_name_wave("", GetPointer(ewm.wave_labels));
      }
    }
  }
  if(id == CHARTEVENT_CLICK)
  {
    ewm.on_chart_click((int)lparam, (int)dparam);
    p.insert_pitchfork((int)lparam, (int)dparam);
    p.pitchforks.update();
  }
  if(id == CHARTEVENT_KEYDOWN)
  {
    switch((int)lparam)
    {
      case PREV_LEVEL:
        ewm.on_prev_level();
        break;
      case NEXT_LEVEL:
        ewm.on_next_level();
        break;
      case UP_LEVEL:
        ewm.on_up_level();
        p.pitchforks.update();
        break;
      case DOWN_LEVEL:
        ewm.on_down_level();
        p.pitchforks.update();
        break;
      case HIDE_PANEL:
        ewm.on_hide_panel();
        break;
      case STOP_MARKING:
        ewm.on_stop_marking();
        break;
      case START_MARKING:
        ewm.on_start_marking();
        break;
      case SELECT_GROUP:
        ewm.on_select_group();
        break;
      case DELETE_OBJECT:
        ewm.wave_labels.correct();
        break;
      case INCREASE_LEVEL:
        ewm.on_increase_level();
        p.pitchforks.update();
        break;
      case REDUCE_LEVEL:
        ewm.on_reduce_level();
        p.pitchforks.update();
        break;
      case SELECT:
        ewm.wave_labels.select = ! ewm.wave_labels.select;
        break;
      case START_ANALYSIS_LEFT:
        ewa.analysis_left(GetPointer(ewm.wave_labels));
        break;
      case START_ANALYSIS_RIGHT:
        ewa.analysis_right(GetPointer(ewm.wave_labels)); 
        break;
      case PREV_VARIANT_LEFT:
        ewa.select("PrevLeft", GetPointer(ewm.wave_labels));
        ewa.get_name_wave("PrevLeft", GetPointer(ewm.wave_labels));
        break;
      case NEXT_VARIANT_LEFT:
        ewa.select("NextLeft", GetPointer(ewm.wave_labels));
        ewa.get_name_wave("NextLeft", GetPointer(ewm.wave_labels));
        break;
      case PREV_VARIANT_RIGHT:
        ewa.select("PrevRight", GetPointer(ewm.wave_labels));
        ewa.get_name_wave("PrevRight", GetPointer(ewm.wave_labels));
        break;
      case NEXT_VARIANT_RIGHT:
        ewa.select("NextRight", GetPointer(ewm.wave_labels));
        ewa.get_name_wave("NextRight", GetPointer(ewm.wave_labels));
        break;
      case CONVERT:
        ewa.convert();
        break;
      case CLEAR:
        ewm.clear();
        ewa.clear();
        break;
      case GET_NAME_WAVE_LEFT:
        ewa.get_name_wave("", GetPointer(ewm.wave_labels));
        break; 
      case GET_NAME_WAVE_RIGHT:
        ewa.get_name_wave("NextRight", GetPointer(ewm.wave_labels));
        break;
      case COPY_CHART:
      {
        long chart_id = ChartOpen(_Symbol, _Period);
        ewm.panel.remove();
        ChartSaveTemplate(0, "EWM");
        ChartApplyTemplate(chart_id, "EWM.tpl");
        ChartRedraw(chart_id);
        ewm.panel.create();
        break;
      }
      case SHOW_HORIZONTAL:
        ewm.on_show_horizontal();
        break;
      case HIDE_ALL_HORIZONTAL:
        ewm.on_hide_all_horizontal();
        break;
      case SHOW_PRICE_LABEL:
        ewm.on_show_price_label();
        break;
      case HIDE_ALL_PRICE_LABEL:
        ewm.on_hide_all_price_label();
        break;  
      case SHOW_VETICAL:
        ewm.on_show_vertical();
        break;
      case HIDE_ALL_VERTICAL:
        ewm.on_hide_all_vertical();
        break;
      case SHOW_VERTICAL0:
        ewm.on_show_vertical0();
        break;
      case SHOW_SCHIFF:
        p.on_show_schiff();
        break;
      case SHOW_WARNING_UP:
        p.on_show_warning_up();
        break;
      case SHOW_WARNING_DOWN:
        p.on_show_warning_down();
        break;
      case HIDE_PITCHFORK:
        p.on_hide_pitchfork();
        break;
    }
  }
  ChartRedraw();
}