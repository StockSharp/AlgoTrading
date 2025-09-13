//+------------------------------------------------------------------+
//| class TD                                                         |
//+------------------------------------------------------------------+
class TD
  {
   string            m_tdcontent;
   string            m_visible_value;
public:
   void              SetContent(string cont){m_tdcontent=cont;};
   string            GetContent(){return(m_tdcontent);};
   string            GetVisibleValue(){return m_visible_value;};
   void              SetVisibleValue(string value){m_visible_value=value;};
  };
//+------------------------------------------------------------------+
//| class Row                                                        |
//+------------------------------------------------------------------+
class Row
  {
   string            m_trcontent;
   TD                m_tds[];
public:
   void              SetContent(string cont){m_trcontent=cont; SetTDs(m_trcontent);};
   void              SetTDs(string cont);
   int               GetTDsAmount(){return(ArraySize(m_tds));};
   string            GetContent(){return(m_trcontent);};
   string            GetTDValue(int col)
     {
      if(col<GetTDsAmount())
         return(m_tds[col].GetVisibleValue());
      else return("");
     };
  };
//+------------------------------------------------------------------+
//| Fills columns of the row                                         |
//+------------------------------------------------------------------+
void Row::SetTDs(string cont)
  {
   GetTDsArray(m_tds,cont);
  }
//+------------------------------------------------------------------+
//| class Table                                                      |
//+------------------------------------------------------------------+
class Table
  {
   string            m_tablecontent;
   Row               m_rows[];
   int               m_rows_count;
   int               m_columns_count;
public:
                     Table(void);
                    ~Table(void){};
                     Table(string classtype,string html,int table_index=1);
   int               SetRows(string content){return(GetTRsArray(m_rows,content));};
   int               RowCount(){return (m_rows_count);};
   int               ColCount(){return(m_columns_count);};
   int               CalculateColumnsCount();
   string            GetValue(int row,int col);
   string            Description(){return((string)ArraySize(m_rows)+"x"+(string)m_columns_count);};
  };
//+------------------------------------------------------------------+
//| Creates table based on html https://www.google.com/finance       |
//+------------------------------------------------------------------+
void Table::Table(string classtype,string html,int table_index)
  {
   m_rows_count=-1;
   m_columns_count=-1;
//   Print("html length = ",StringBufferLen(html));
   int start=0;
   string tag=GetTag("table","class="+classtype+"",html,table_index);
   m_tablecontent=tag;
//--- table found, fill rows
   if(StringLen(m_tablecontent)>0)
     {
      m_rows_count=SetRows(m_tablecontent);
     }
//--
   CalculateColumnsCount();
//Print("Table ",m_rows_count,"x",CalculateColumnsCount());
  }
//+------------------------------------------------------------------+
//| Returns the value                                                |
//+------------------------------------------------------------------+
string Table::GetValue(int row,int col)
  {
   if(row>m_rows_count-1)
      return(__FUNCTION__+": row index must be <"+(string)m_rows_count);
   if(col>m_rows[row].GetTDsAmount()-1)
      return "empty value at ["+(string)row+" , "+(string)col+"]";
   return m_rows[row].GetTDValue(col);
  }
//+------------------------------------------------------------------+
//| Calculates columns amount                                        |
//+------------------------------------------------------------------+
int Table::CalculateColumnsCount(void)
  {
   if(m_rows_count>0)
     {
      m_columns_count=0;
      for(int i=0;i<m_rows_count;i++)
        {
         int size=m_rows[i].GetTDsAmount();
         if(size>m_columns_count)
           {
            m_columns_count=size;
           }
        }
     }
   return(m_columns_count);
  }
//+------------------------------------------------------------------+
//| Returns tag by name with its content                             |
//+------------------------------------------------------------------+
string GetTag(string tagname,string attr,string html,int index)
  {
   string matcher="<"+tagname;
   bool found=false;
   int start=0;
   string text=html;
   string tagcontent;
   int tablecount=0;
   int pos=0;
//---  
   while(!found)
     {
      pos=StringFind(text,matcher,0);
      //--- tag start found
      if(pos!=-1)
        {
         text=StringSubstr(text,pos);
         int end=StringFind(text,">",0);
         //--- tag end
         if(end!=-1)
           {
            tagcontent=StringSubstr(text,0,end+1);
            int atrrpos=StringFind(tagcontent,attr);
            //--- attribute found
            if(atrrpos!=-1)
              {
               start=pos;
               tablecount++;
               if(tablecount==index)
                 {
                  found=true;
                  break;
                 }
               else
                 {
                  text=StringSubstr(text,end);
                  start=0;
                  continue;
                 }
              }
            else // tag not found
              {
               start=0;
               text=StringSubstr(text,end);
               continue;
              }
           }
         else
           {
            PrintFormat("Attention! Closed bracket for tag %s not found ",tagname);
           }
        }
      found=true;
     }
   if(!found)
     {
      PrintFormat("Tag %s not found.",tagname);
      return "";
     }
   else
     {
      //--- find closing tag
      string closetag="</"+tagname+">";
      pos=StringFind(text,closetag,0);
      if(pos!=-1)
        {
         return StringSubstr(text,0,pos+StringLen(closetag));
        }
     }
   return "";
  }
//+------------------------------------------------------------------+
//| Fills rows[] array                                               |
//+------------------------------------------------------------------+
int  GetTRsArray(Row &rows[],string htmltable)
  {
   int trs=0;
   Row temp[100];
   string matcher="<tr";
   string closetr="</tr>";
   bool found=false;
   int start=0;
   string text=htmltable;
   string tagcontent;
//---  
   while(!found)
     {
      start=StringFind(text,matcher,0);
      //--- tag start found
      if(start!=-1)
        {
         //--- closing tag position
         int end=StringFind(text,closetr,start);
         //--- next closing tag position
         int nextxtart=StringFind(text,matcher,start+3);
         //--- if closing tag before opening tag
         if(end<nextxtart && (end!=-1))
           {
            tagcontent=StringSubstr(text,start,end+StringLen(closetr)-start);
            temp[trs].SetContent(tagcontent);
            text=StringSubstr(text,end+StringLen(closetr));
            trs++;
            start=0;
            continue;
           }
         else
           {
            //--- only opening tag found
            if(nextxtart!=-1 && (end==-1))
              {
               tagcontent=StringSubstr(text,start,nextxtart-start);
               //--- if not empty string, add it
               if(StringLen(tagcontent)>4)
                 {
                  temp[trs].SetContent(tagcontent);
                  text=StringSubstr(text,nextxtart);
                  trs++;
                  start=0;
                  continue;
                 }
               else //--- empty string
                 {
                  text=StringSubstr(text,nextxtart);
                  start=0;
                  continue;
                 }
              }
            else //--- both tags not found (opening and closing)
              {
               tagcontent=StringSubstr(text,start,-1);
               temp[trs].SetContent(tagcontent);
               text=StringSubstr(text,nextxtart);
               trs++;
               start=0;
               //--- tag parsing done
               break;
              }
           }
        }
      else
        {
         break;
        }
     }
   if(trs>0)
     {
      ArrayResize(rows,trs);
      for(int i=0;i<trs;i++)
        {
         string str=temp[i].GetContent();
         if(str!="") rows[i].SetContent(str);
        }

     }
   return trs;
  }
//+------------------------------------------------------------------+
//| Fills cells[] array                                              |
//+------------------------------------------------------------------+
int  GetTDsArray(TD &cells[],string htmltable)
  {
   int tds=0;
   TD temp[100];
   string matcher="<td";
   string closetd="</td>";
   bool found=false;
   int start=0;
   string text=htmltable;
   string tagcontent;
//---  
   while(!found)
     {
      start=StringFind(text,matcher,start);
      //--- tag start found
      if(start!=-1)
        {
         //--- closing tag position
         int end=StringFind(text,closetd,start);
         //--- next closing tag position
         int nextxtart=StringFind(text,matcher,start+3);
         //--- if closing tag before opening        
         if(end<nextxtart && (end!=-1))
           {
            tagcontent=StringSubstr(text,start,end+StringLen(closetd)-start);
            temp[tds].SetContent(tagcontent);
            text=StringSubstr(text,end+StringLen(closetd));
            tds++;
            start=0;
            continue;
           }
         else
           {
            //--- only opening tag found
            if(nextxtart!=-1 && (end==-1))
              {
               tagcontent=StringSubstr(text,start,nextxtart-start);
               temp[tds].SetContent(tagcontent);
               //Print("tr:",tagcontent);
               text=StringSubstr(text,nextxtart);
               tds++;
               start=0;
               continue;
              }
            else //--- both tags not found (opening and closing)
              {
               tagcontent=StringSubstr(text,start);
               temp[tds].SetContent(tagcontent);
               //Print("tr:",tagcontent);
               text=StringSubstr(text,nextxtart);
               tds++;
               start=0;
               //--- tag parsing done
               break;
              }
           }
        }
      else
        {
         break;
        }
     }
   if(tds>0)
     {
      ArrayResize(cells,tds);
      for(int i=0;i<tds;i++)
        {
         string contnet=temp[i].GetContent();
         if(contnet=="") Print("EMPTY");
         //-- S&P500 case
         StringReplace(contnet,"&amp;","&");
         cells[i].SetContent(contnet);
         cells[i].SetVisibleValue(DeleteAllTags(contnet));
        }
     }
   return tds;
  }
//+------------------------------------------------------------------+
//| Deletes all tags from html code                                  |
//+------------------------------------------------------------------+
string DeleteAllTags(string htmlcode)
  {
   string result=htmlcode;
   int start,end;
   while(FindTag(result,start,end))
     {
      result=DeleteTag(result,start,end);
     }
   return result;
  }
//+------------------------------------------------------------------+
//| Finds starting and finish positions of the tag                   |
//+------------------------------------------------------------------+
bool FindTag(string text,int &startpos,int &endpos)
  {
   int pos=StringFind(text,"<",0);
   if(pos!=-1)
     {
      startpos=pos;
      pos=StringFind(text,">",startpos);
      if(pos!=-1)
        {
         endpos=pos;
         return true;
        }
     }
   return false;
  }
//+------------------------------------------------------------------+
//| Deletes tag                                                      |
//+------------------------------------------------------------------+
string DeleteTag(string text,int start_pos,int f)
  {  
   string before=NULL;
   if(start_pos>0)
   before=StringSubstr(text,0,start_pos);
   string tail=StringSubstr(text,f+1);
   return(before+tail);
  }
//+------------------------------------------------------------------+
