//+------------------------------------------------------------------+
//|                                                 QuotesDialog.mqh |
//|                   Copyright 2009-2013, MetaQuotes Software Corp. |
//|                                              http://www.mql5.com |
//+------------------------------------------------------------------+
#include <Controls\Dialog.mqh>
#include <Controls\Label.mqh>
#include "TableParser.mqh"
//+------------------------------------------------------------------+
//| Class CQuotesDialog                                              |
//| Usage: main dialog of the Controls application                   |
//+------------------------------------------------------------------+
class CQuotesDialog : public CAppDialog
  {
private:
   CLabel            m_label_names[];
   CLabel            m_label_quotes[];
   CLabel            m_label_percentage[];
   int               m_table_index;

public:
                     CQuotesDialog(void);
                    ~CQuotesDialog(void);
   //--- create
   virtual bool      Create(const long chart,const string name,const int subwin,const int x1,const int y1,const int x2,const int y2,int table_index);
   bool              UpdateQuotes(void);

protected:
   //--- create dependent controls
   bool              PrepareLabel(CLabel &label,string name,int x,int y);
   bool              CreateLabels(int total_quotes);
  };
//+------------------------------------------------------------------+
//| Constructor                                                      |
//+------------------------------------------------------------------+
CQuotesDialog::CQuotesDialog(void)
  {
  }
//+------------------------------------------------------------------+
//| Destructor                                                       |
//+------------------------------------------------------------------+
CQuotesDialog::~CQuotesDialog(void)
  {
  }
//+------------------------------------------------------------------+
//| Create                                                           |
//+------------------------------------------------------------------+
bool CQuotesDialog::Create(const long chart,const string name,const int subwin,const int x1,const int y1,const int x2,const int y2,int table_index)
  {
   if(!CAppDialog::Create(chart,name,subwin,x1,y1,x2,y2))
      return(false);
//--- succeed
   m_table_index=table_index;
   return(true);
  }
//+------------------------------------------------------------------+
//| PrepareLabel                                                     |
//+------------------------------------------------------------------+
bool CQuotesDialog::PrepareLabel(CLabel &label,string name,int x,int y)
  {
   if(!label.Create(m_chart_id,m_name+"Label"+name,m_subwin,x,y,x+20,y+20))
      return(false);
   if(!Add(label))
      return(false);
   return(true);
  }
//+------------------------------------------------------------------+
//| Create the labels                                                |
//+------------------------------------------------------------------+
bool CQuotesDialog::CreateLabels(int total_quotes)
  {
// int total_quotes=21;
//---
   ArrayResize(m_label_names,total_quotes);
   ArrayResize(m_label_quotes,total_quotes);
   ArrayResize(m_label_percentage,total_quotes);
//---
   for(int i=0; i<total_quotes; i++)
     {
      PrepareLabel(m_label_names[i],"quotes"+IntegerToString(i),10,5+i*18);
      PrepareLabel(m_label_quotes[i],"values"+IntegerToString(i),120,5+i*18);
      PrepareLabel(m_label_percentage[i],"percentage"+IntegerToString(i),200,5+i*18);
      //---
      m_label_names[i].Color(clrBlue);
      m_label_quotes[i].Color(clrBlack);
      m_label_percentage[i].Color(clrBlack);
     }
//--- succeed
   return(true);
  }
//+------------------------------------------------------------------+
bool CQuotesDialog::UpdateQuotes(void)
  {
   int i;
//---
   string cookie=NULL,headers;
   char post[],result[];
   int res;
//---
   string google_url="https://www.google.com/finance";
   string response;
//--- get data from Google
   ResetLastError();
   res=WebRequest("GET",google_url,cookie,NULL,50,post,0,result,headers);
//--- check error
   if(res==-1) return(false);
//--- convert to string
   response=CharArrayToString(result,0,-1,CP_UTF8);
//--- parse data
   Table table("quotes",response,m_table_index);
//--- create if not created yet
   if(ArraySize(m_label_names)==0)
      CreateLabels(table.RowCount());
//---
   int total_quotes=ArraySize(m_label_names);
//--- set color for names
   for(i=0; i<total_quotes; i++) m_label_names[i].Color(clrBlue);
//--- set values
   for(i=0; i<total_quotes; i++)
     {
      if(table.ColCount()==3)
        {
         //--- get values
         string str_quote_name=table.GetValue(i,0);
         string str_quote_value=table.GetValue(i,1);
         string str_quote_percentage=table.GetValue(i,2);
         if(str_quote_percentage=="") str_quote_percentage="n/a";
         //--- set values
         m_label_names[i].Text(str_quote_name);
         m_label_quotes[i].Text(str_quote_value);
         m_label_percentage[i].Text(str_quote_percentage);
         //--- calc colors
         color QuoteColor=clrBlack;
         if(StringFind(str_quote_percentage,"-")!=-1) QuoteColor=clrRed;
         else
            if(StringFind(str_quote_percentage,"+")!=-1) QuoteColor=clrGreen;
         //--- update colors
         m_label_percentage[i].Color(QuoteColor);
        }
     }
   return(true);
  }
//---
