//+------------------------------------------------------------------+
//|                                                        Parse.mqh |
//|                                                  Roman Martynyuk |
//|                                           http://www.dml-ewa.ru/ |
//+------------------------------------------------------------------+
#property copyright "Roman Martynyuk"
#property link      "http://www.dml-ewa.ru/"

#include <EWM\ewa\patterns.mqh>
#include <EWM\defines.mqh>
#include <EWM\ewm\descrs.mqh>

class Parser
{
  public:
    int handle;
    int position;
    int line;
    string lexeme;
    void Parser();
    uchar get_next_char();
    bool is_alpha(uchar c);
    bool is_digit(uchar c);
    string read_string(uchar c);
    string read_number(uchar c);
    void match(string str);
    void lexical_analysis();
    void levels_descr();
    void parse_reaction_levels();
    void parse_warning_levels();
    void parse_internal_retrace_rule();
    void parse_relative_position_rule();
    void parse_ratio_rule(string type);
    void parse_logical_rule(Logical *logic);
    void parse_pattern();
    void parse_pattern_subwaves(CArrayObj *subwaves);
    void parse_pattern_rules(CArrayObj *rules);
    void parse_pattern_fibos(CArrayObj *fibos);
    void parse_pattern_targets(CArrayObj *targets, bool is_type = true);
    //Rule *find_rule(string rule_name);
    string read_descr();
    void error_message();
    void set_logical_rules();
    void parse_levels_descr();
};

uchar Parser::get_next_char()
{
  if( ! FileIsEnding(handle))
  {
    position++;
    return(uchar(FileReadInteger(handle, 1)));
  }
  else
  {
    return(EOF);
  }
}

bool Parser::is_alpha(uchar c)
{
  if((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'))
  {
    return(true);
  }
  else
  {
    return(false);
  }
}

bool Parser::is_digit(uchar c)
{
  if(c >= '0' && c <= '9')
  {
    return(true);
  }
  else
  {
    return(false);
  }
}

string Parser::read_string(uchar c)
{
  string str = "";
  do
  {
    str += CharToString(c);
    c = get_next_char();
  }
  while(is_alpha(c) || c == '_' || is_digit(c));
  FileSeek(handle, -1, SEEK_CUR);
  position--;
  return(str);
}

string Parser::read_number(uchar c)
{
  string number = CharToString(c);
  int state = 0;
  while(state != 2)
  {
    switch(state)
    {
      case 0:
        c = get_next_char();
        if(is_digit(c))
        {
          number += CharToString(c);
        }
        else if(c == '.')
        {
          number += CharToString(c);
          state = 1;
        }
        else
        {
          state = 2;
        }
        break;
      case 1:
        c = get_next_char();
        if(is_digit(c))
        {
          number += CharToString(c);
        }
        else
        {
          state = 2;
        }
        break;
    }
  }
  FileSeek(handle, -1, SEEK_CUR);
  position--;
  return(number);
}

void Parser::match(string str)
{
  if(lexeme == str)
  {
    lexical_analysis();
  }
  else
  {
    error_message();
    lexical_analysis();
  }
}

void Parser::lexical_analysis()
{
  while(true)
  {
    uchar c = get_next_char();
    if(c ==' ' || c == '\t' || c == '\r')
    {
      ;
    }
    else if(c == '\n')
    {
      position = 0;
      line++;
    }
    else if(is_digit(c))
    {
      lexeme = read_number(c);
      return;
    }
    else if(is_alpha(c) || c=='_')
    {
      lexeme = read_string(c);
      return;
    }
    else if(c == '/')
    {
      while(get_next_char() != '\n')
      {
        ;
      }
      FileSeek(handle, -1, SEEK_CUR);
      position--;
    }
    else if(c == EOF)
    {
      lexeme = CharToString(EOF);
      return;
    }
    else
    {
      lexeme = CharToString(c);
      return;
    }
  }
  return;
}

void Parser::parse_reaction_levels()
{
  match("ReactionLines");
  match("{");
  while(lexeme != ";")
  {
    reaction_levels.Add((double) lexeme);
    match(lexeme);
    if(lexeme == ",")
    {
      match(",");
    }
  }
  match(";");
  match("}");
}

void Parser::parse_warning_levels()
{
  match("WarningLines");
  match("{");
  while(lexeme != ";")
  {
    warning_levels.Add((double) lexeme);
    match(lexeme);
    if(lexeme == ",")
    {
      match(",");
    }
  }
  match(";");
  match("}");
}

void Parser::parse_levels_descr()
{
  match("LevelsDescription");
  match("{");
  while(lexeme != "}")
  {
    Descr *descr = new Descr;
    descr.level = lexeme;
    match(lexeme);
    match("{");
    descr.num_level = (int) lexeme;
    match(lexeme);
    match(";");
    int i = 0;
    while(lexeme != ";")
    {
      while(lexeme != "," && lexeme != ";")
      {
        descr.labels[i] += lexeme;
        match(lexeme);
      }
      if(lexeme == ",")
      {
        match(",");
      }
      i++;
    }
    match(";");
    descr.font = lexeme;
    match(lexeme);
    while(lexeme != ";")
    {
      descr.font += " " + lexeme;
      match(lexeme);
    }
    match(";");
    descr.font_size = (int) lexeme;
    match(lexeme);
    match(";");
    descr.size_in_px = (int) lexeme;
    match(lexeme);
    match(";");
    string clr = lexeme;
    match(lexeme);
    match(",");
    clr += "," + lexeme;
    match(lexeme);
    match(",");
    clr += "," + lexeme;
    descr.clr = (color) clr;
    match(lexeme);
    match(";");
    descrs.Add(descr);
    match("}");
  }
  match("}");
}

void Parser::parse_internal_retrace_rule()
{
  Rule *rule = new Rule;
  rule.type = lexeme;
  match(lexeme);
  match(":");
  rule.name = lexeme;
  match(lexeme);
  match("{");
  Internal_retrace *internal_retrace = new Internal_retrace;
  internal_retrace.num_wave = int (lexeme);
  match(lexeme);
  match(",");
  internal_retrace.ratio = double (lexeme);
  match(lexeme);
  match(";");
  match("}");
  rule.set_descr(internal_retrace);
  rules.Add(rule);
}

void Parser::parse_relative_position_rule()
{
  Rule *rule = new Rule;
  rule.type = lexeme;
  match(lexeme);
  match(":");
  rule.name = lexeme;
  match(lexeme);
  match("{");
  Relative_position *relative_position = new Relative_position;
  if(lexeme == "max" || lexeme == "min")
  {
    relative_position.mod1 = lexeme;
    match(lexeme);
    match("(");
    relative_position.num_wave1 = (int) lexeme;
    match(lexeme);
    match(")");
  }
  else if(lexeme >= "0" && lexeme <= "5")
  {
    relative_position.mod1 = "val";
    relative_position.num_wave1= (int) lexeme;
    match(lexeme);
  }
  else
  {
    error_message();
  }
  if(lexeme == ">" || lexeme == "<")
  {
    relative_position.sign = lexeme;
    match(lexeme);
  }
  else
  {
    error_message();
  }
  match("=");
  relative_position.sign += "=";
  if(lexeme == "max" || lexeme == "min")
  {
    relative_position.mod2 = lexeme;
    match(lexeme);
    match("(");
    relative_position.num_wave2 = (int) lexeme;
    match(lexeme);
    match(")");
  }
  else if(lexeme >= "0" && lexeme <= "5")
  {
    relative_position.mod2 = "val";
    relative_position.num_wave2= (int) lexeme;
    match(lexeme);
  }
  else
  {
    error_message();
  }
  match(";");
  match("}");
  if(relative_position.sign == "<=")
  {
   int temp_num_wave = relative_position.num_wave1;
   string temp_mod = relative_position.mod1;
   relative_position.num_wave1 = relative_position.num_wave2;
   relative_position.mod1 = relative_position.mod2;
   relative_position.num_wave2 = temp_num_wave;
   relative_position.mod2 = temp_mod;
   relative_position.sign = ">=";
  }
  rule.set_descr(relative_position);
  rules.Add(rule);
}

void Parser::parse_ratio_rule(string type)
{
  Rule *rule = new Rule;
  rule.type = lexeme;
  match(lexeme);
  match(":");
  rule.name = lexeme;
  match(lexeme);
  match("{");
  Fibonacci *fibonacci = new Fibonacci;
  if(rule.type == "LengthRatio")
  {
    fibonacci.type = LENGTH;
  }
  else
  {
    fibonacci.type = TIME;
  }
  if(lexeme == "max" || lexeme == "min")
  {
    fibonacci.mod = lexeme;
    match(lexeme);
    match("(");
    fibonacci.num_wave1 = (int) lexeme;
    match(lexeme);
    match(")");
  }
  else if(lexeme >= "1" && lexeme <= "5")
  {
    fibonacci.num_wave1 = (int) lexeme;
    match(lexeme);
  }
  else
  {
    error_message();
  }
  if(lexeme == ">" || lexeme == "<")
  {
    fibonacci.sign = lexeme;
    match(lexeme);
  }
  else
  {
    error_message();
  }
  match("=");
  fibonacci.sign += "=";
  fibonacci.num_wave2 = (int) lexeme;
  match(lexeme);
  if(fibonacci.num_wave1 % 2 == 1 && lexeme == "+")
  {
    match("+");
  }
  else if(fibonacci.num_wave1 % 2 == 0 && lexeme == "-")
  {
    match("-");
  }
  else
  {
    error_message();
  }
  fibonacci.ratio = double (lexeme);
  match(lexeme);
  match("*");
  match("(");
  if(lexeme >= "1" && lexeme <= "5")
  {
    fibonacci.num_wave3 = (int) lexeme;
    match(lexeme);
  }
  else
  {
    error_message();
  }
  match("-");
  if(lexeme >= "0" && lexeme <= "4")
  {
    fibonacci.num_wave4 = (int) lexeme;
    match(lexeme);
  }
  else
  {
    error_message();
  }
  match(")");
  match(";");
  match("}");
  rule.set_descr(fibonacci);
  rules.Add(rule);
}

void Parser::parse_logical_rule(Logical *logic)
{
  Rule *rule = new Rule;
  rule.type = lexeme;
  match(lexeme);
  match(":");
  rule.name = lexeme;
  match(lexeme);
  match("{");
  logic.rule_name1 = lexeme;
  match(lexeme);
  match(",");
  logic.rule_name2 = lexeme;
  match(lexeme);
  match(";");
  match("}");
  rule.set_descr(logic);
  rules.Add(rule);
}

void Parser::set_logical_rules()
{
  for(int i = 0; i < rules.Total(); i++)
  {
    Rule *rule = rules.At(i);
    if(rule.type == "Or" || rule.type == "And")
    {
      Logical *logical = rule.descr;
      logical.rule1 = rules.find(logical.rule_name1);
      logical.rule2 = rules.find(logical.rule_name2);
    }
  }
}


void Parser::parse_pattern_rules(CArrayObj *temp_rules)
{
  match(lexeme);
  match("{");
  while(lexeme != "}")
  {
    temp_rules.Add(rules.find(lexeme));
    match(lexeme);
    match(";");
  }
  match("}");
}

void Parser::parse_pattern_subwaves(CArrayObj *subwaves)
{
  match(lexeme);
  match("{");
  while(lexeme != "}")
  {
    Subwaves_descr *subwaves_descr = new Subwaves_descr;
    subwaves_descr.wave_label = lexeme;
    match(lexeme);
    match(":");
    if(lexeme == "-")
    {
      match("-");
      subwaves_descr.ratio1 = - (double) lexeme;
    }
    else
    {
      subwaves_descr.ratio1 = (double) lexeme;
    }
    match(lexeme);
    match("*");
    subwaves_descr.num_wave1 = (int) lexeme;
    match(lexeme);
    match(",");
    subwaves_descr.ratio2 = (double) lexeme;
    match(lexeme);
    match("*");
    subwaves_descr.num_wave2 = (int) lexeme;
    match(lexeme);
    match(":");
    while(lexeme != ";")
    {
      subwaves_descr.probability.Add((double) lexeme);
      match(lexeme);
      match("*");
      subwaves_descr.name_wave.Add(lexeme);
      match(lexeme);
      if(lexeme == ",")
      {
        match(",");
      }
    }
    match(";");
    subwaves.Add(subwaves_descr);
  }
  match("}");
}

void Parser::parse_pattern_fibos(CArrayObj *fibos)
{
  match(lexeme);
  match("{");
  while(lexeme != "}")
  {
    Fibo *fibo=new Fibo;
    fibo.num_wave1 = (int) lexeme;
    match(lexeme);
    match(":");
    fibo.num_wave2 = (int) lexeme;
    match(lexeme);
    match("=");
    fibo.score = (double) lexeme;
    match(lexeme);
    match("*");
    match("(");
    fibo.low = (double) lexeme;
    match(lexeme);
    match(",");
    fibo.middle = (double) lexeme;
    match(lexeme);
    match(",");
    fibo.high= (double) lexeme;
    match(lexeme);
    match(")");
    match(";");
    fibos.Add(fibo);
  }
  match("}");
}

void Parser::parse_pattern_targets(CArrayObj *targets, bool is_type = true)
{
  match(lexeme);
  match("{");
  while(lexeme != "}")
  {
    Target *target = new Target;
    if(is_type)
    {
      target.type = lexeme;
      match(lexeme);
      match(":");
    }
    target.num_wave1 = (int) lexeme;
    match(lexeme);
    match("=");
    target.num_wave2 = (int) lexeme;
    match(lexeme);
    if(lexeme == "-" || lexeme == "+")
    {
      if(lexeme == "-")
      {
        target.ratio *= -1;
      }
      else
      {
        target.ratio *= 1;
      }
      match(lexeme);
    }
    else
    {
      error_message();
    }
    target.ratio *= (double) lexeme;
    match(lexeme);
    match("*");
    match("(");
    target.num_wave3 = (int) lexeme;
    match(lexeme);
    match("-");
    target.num_wave4 = (int) lexeme;
    match(lexeme);
    match(")");
    match(";");
    targets.Add(target);
  }
  match("}");
}

string Parser::read_descr()
{
  string descr = "";
  while(lexeme != ";")
  {
    descr += lexeme;
    lexeme = CharToString(get_next_char());
  }
  return(descr);
}

void Parser::parse_pattern()
{
  Pattern *pattern = new Pattern;
  match(lexeme);
  match(":");
  pattern.name = lexeme;
  match(lexeme);
  match("{");
  match("Type");
  match(":");
  pattern.type = lexeme;
  match(lexeme);
  match(";");
  match("Probability");
  match(":");
  pattern.probability = (double) lexeme * 100;
  match(lexeme);
  match(";");
  match("Description");
  match(":");
  pattern.descr = read_descr();
  match(";");
  while(lexeme != "}")
  {
    if(lexeme == "Subwaves")
    {
      parse_pattern_subwaves(GetPointer(pattern.subwaves));
    }
    else if(lexeme == "Rules")
    {
      parse_pattern_rules(GetPointer(pattern.rules));
    }
    else if(lexeme == "Guidelines")
    {
      parse_pattern_rules(GetPointer(pattern.guidelines));
    }
    else if(lexeme == "EntrySignals")
    {
      parse_pattern_rules(GetPointer(pattern.entry_signals));
    }
    else if(lexeme == "ExitSignals")
    {
      parse_pattern_rules(GetPointer(pattern.exit_signals));
    }
    else if(lexeme == "StopSignals")
    {
      parse_pattern_rules(GetPointer(pattern.stop_signals));
    }
    else if(lexeme == "WaveSignals")
    {
      parse_pattern_rules(GetPointer(pattern.wave_signals));
    }
    else if(lexeme == "ConfirmSignals")
    {
      parse_pattern_rules(GetPointer(pattern.confirm_signals));
    }
    else if(lexeme == "ValueFibo")
    {
      parse_pattern_fibos(GetPointer(pattern.value_fibos));
    }
    else if(lexeme == "TimeFibo")
    {
      parse_pattern_fibos(GetPointer(pattern.time_fibos));
    }
    else if(lexeme == "ProportionFibo")
    {
      parse_pattern_fibos(GetPointer(pattern.proportion_fibos));
    }
    else if(lexeme == "ProportionFiboRequired")
    {
      parse_pattern_fibos(GetPointer(pattern.proportion_fibos_required));
    }
    else if(lexeme == "Targets")
    {
      parse_pattern_targets(GetPointer(pattern.targets));
    }
    else if(lexeme == "TimeTargets")
    {
      parse_pattern_targets(GetPointer(pattern.time_targets), false);
    }
  }
  match("}");
  pattern.num_wave = pattern.subwaves.Total();
  patterns.Add(pattern);    
}

void Parser::Parser()
{
  handle = FileOpen(NAME_RULES_FILE, FILE_READ | FILE_BIN | FILE_SHARE_READ | FILE_ANSI);
  if(handle < 0)
  {
    MessageBox(MSG_FILE_OPEN_ERROR, NAME_RULES_FILE);
  }
  else
  {
    lexical_analysis();
    while(lexeme != CharToString(EOF))
    {
      if(lexeme == "ReactionLines")
      {
        parse_reaction_levels();
      }
      else if(lexeme == "WarningLines")
      {
        parse_warning_levels();
      }
      else if(lexeme == "LevelsDescription")
      {
        parse_levels_descr();
      }
      else if(lexeme == "InternalRetrace")
      {
        parse_internal_retrace_rule();
      }
      else if(lexeme == "RelativePosition")
      {
        parse_relative_position_rule();
      }
      else if(lexeme == "LengthRatio" || lexeme == "TimeRatio")
      {
        parse_ratio_rule(lexeme);
      }
      else if(lexeme == "Or")
      {
        parse_logical_rule(new Or);
      }
      else if(lexeme == "And")
      {
        parse_logical_rule(new And);
      }
      else if(lexeme == "Pattern")
      {
        parse_pattern();
      }
      else
      {
        error_message();
      }
    }
    set_logical_rules();
    FileClose(handle);
  }
}

void Parser::error_message()
{
  MessageBox("Error of reading from the EWM.txt file in the string " + IntegerToString(line + 1) + " in position " + IntegerToString(position + 1));
}