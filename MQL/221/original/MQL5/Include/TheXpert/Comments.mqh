//+------------------------------------------------------------------+
//|                                                     Comments.mqh |
//+------------------------------------------------------------------+

#property copyright "TheXpert"
#property link      "theforexpert@gmail.com"

class CommentsStore
{
public:
   CommentsStore();

   void SetLines(int newLines);
   int Lines() const;
   
   void SetLength(int newLength);
   int Length() const;

   void SetTabSize(int newSize);
   int TabSize() const;
   
   void AddComment(string newComment);
   string CommentsToStr() const;
   
   void Clear();
   
   bool CommentsStore::GetLine(int i, string& prefix, string& line);

private:
   string ReplaceTabs(string s);
   void AddLine(string line, bool isStart);
   void AddSizedLine(string line, bool isStart);

private:
   string m_Comments[];
   string m_Times[];
   int m_Lines;
   int m_Length;
   int m_TabSize;
   int m_Pos;
};

CommentsStore::CommentsStore(void)
{
   m_Lines = 10;
   m_Length = 60;
   m_TabSize = 8;
   m_Pos = 0;

   ArrayResize(m_Comments, m_Lines);
   ArrayResize(m_Times, m_Lines);
}

void CommentsStore::SetLines(int newLines)
{
   Clear();
   ArrayResize(m_Times, newLines);
   ArrayResize(m_Comments, newLines);
   m_Lines = newLines;
}

int CommentsStore::Lines() const
{
   return m_Lines;
}

void CommentsStore::SetLength(int newLength)
{
   Clear();
   m_Length = newLength;
}

int CommentsStore::Length() const
{
   return m_Length;
}

void CommentsStore::SetTabSize(int newSize)
{
   m_TabSize = newSize;
}

int CommentsStore::TabSize() const
{
   return m_TabSize;
}

void CommentsStore::AddComment(string newComment)
{
   string s = newComment;
   int pos = StringFind(s, "\n");
   bool isStart = true;
   
   while(pos > 0)
   {
      AddLine(StringSubstr(s, 0, pos), isStart);
      s = StringSubstr(s, pos + 1);
      
      pos = StringFind(s, "\n");
      isStart = false;
   }

   AddLine(s, isStart);
}

string CommentsStore::ReplaceTabs(string s)
{
   static string spaces = "                                                     "; // for tabs filling
   
   string result = s;
   
   int pos = StringFind(result, "\t");
   while(pos >= 0)
   {
      int size = (pos - 1)/m_TabSize*m_TabSize + m_TabSize - pos;
      
      if (size > 0)
      {
         result = 
            StringSubstr(result, 0, pos) +
            StringSubstr(spaces, 0, size) +
            StringSubstr(result, pos + 1);
      }
      else
      {
         result = 
            StringSubstr(result, 0, pos) +
            StringSubstr(result, pos + 1);
      }
         
      pos = StringFind(result, "\t");
   }
   return (result);
}

void CommentsStore::AddLine(string line, bool isStart)
{
   string s = ReplaceTabs(line);
   
   int size = StringLen(s);
   bool start = isStart;
   
   if (size == 0)
   {
      AddSizedLine(s, start);
      return;
   }
   
   while (size > 0)
   {
      AddSizedLine(StringSubstr(s, 0, m_Length), start);
      s = StringSubstr(s, m_Length);
      
      size -= m_Length;
      start = false;
   }
}

void CommentsStore::AddSizedLine(string line, bool isStart)
{
   string prefix = TimeToString(TimeTradeServer(), TIME_SECONDS);
   if (!isStart)
   {
      prefix = StringSubstr("                       ", 0, StringLen(prefix));
   }
   
   prefix = prefix + " | ";
   
   if (m_Pos < m_Lines)
   {
      m_Comments[m_Pos] = line;
      m_Times[m_Pos] = prefix;
      m_Pos++;
   }
   else
   {
      for(int i = 1; i < m_Lines; i++)
      {
         m_Comments[i - 1] = m_Comments[i];
         m_Times[i - 1] = m_Times[i];
      }
      m_Comments[m_Pos - 1] = line;
      m_Times[m_Pos - 1] = prefix;
   }
}

string CommentsStore::CommentsToStr() const
{
   string res;
   for (int i = 0; i < m_Pos; i++)
   {
      res = res + m_Times[i] + m_Comments[i] + "\n";
   }
   return res;
}

void CommentsStore::Clear()
{
   for (int i = 0; i < m_Lines; i++)
   {
      m_Comments[i] = "";
      m_Times[i] = "";
   }
   m_Pos = 0;
}

bool CommentsStore::GetLine(int i, string& prefix, string& line)
{
   if (i >= 0 && i < m_Lines)
   {
      prefix = m_Times[i];
      line = m_Comments[i];
      return true;
   }
   return false;
}

CommentsStore Store;

void Comment_(string s)
{
   Print("Comment: " + s);
   Store.AddComment(s);
}

bool SetLength(int length)
{
   if (length <= 0 || length >= 63) return false;

   Store.SetLength(length);
   Comment_("Log lines length changed to " + string(length));

   return true;
}

bool SetLines(int lines)
{
   if (lines <= 0 || lines >= 20) return false;

   Store.SetLines(lines);
   Comment_("Log lines count changed to " + string(lines));
   
   return true;
}

bool SetTabSize(int tabSize)
{
   if (tabSize <= 0 || tabSize >= 20) return false;
   
   Store.SetTabSize(tabSize);
   return true;
}

int GetLength()
{
   return Store.Length();
}

int GetLines()
{
   return Store.Lines();
}

int GetTabSize()
{
   return Store.TabSize();
}

bool GetLine(int pos, string& prefix, string& line)
{
   return Store.GetLine(pos, prefix, line);
}