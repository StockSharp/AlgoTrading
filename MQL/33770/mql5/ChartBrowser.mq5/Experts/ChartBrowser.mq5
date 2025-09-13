//+------------------------------------------------------------------+
//|                                                 ChartBrowser.mq5 |
//|                                    Copyright (c) 2021, Marketeer |
//|                          https://www.mql5.com/en/users/marketeer |
//|                              https://www.mql5.com/en/code/33770/ |
//|                                                                  |
//|                           https://www.mql5.com/en/articles/7734/ |
//|                           https://www.mql5.com/en/articles/7739/ |
//|                           https://www.mql5.com/ru/articles/7795/ |
//+------------------------------------------------------------------+
#property copyright "Copyright (c) 2021, Marketeer"
#property link "https://www.mql5.com/en/users/marketeer"
#property version "1.0"
#property description "Lists all open charts, indicators, experts, and scripts in sorted order.\n"
#property description "Can be used for fast switching."

const string DIALOG_TITLE = "ChartBrowser";

#include "ChartBrowser.mqh"


ChartBrowserForm *form;

int OnInit()
{
    form = new ChartBrowserForm();
    ChartSetInteger(0, CHART_SHOW_ONE_CLICK, false);
    // ChartSetInteger(0, CHART_SHOW_TICKER, false);
    
    if(!form.CreateLayout(0, DIALOG_TITLE, 0, 20, 20, 400, 324))
        return (INIT_FAILED);
    form.IniFileLoad();

    if(!form.Run())
        return (INIT_FAILED);

    return (INIT_SUCCEEDED);
}

void OnDeinit(const int reason)
{
    form.Destroy(reason);
    delete form;
}

void OnChartEvent(const int id, const long &lparam, const double &dparam, const string &sparam)
{
    form.ChartEvent(id, lparam, dparam, sparam);
}
//+------------------------------------------------------------------+
