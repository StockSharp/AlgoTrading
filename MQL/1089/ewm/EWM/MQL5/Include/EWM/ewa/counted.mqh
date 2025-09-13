//+------------------------------------------------------------------+
//|                                                      counted.mqh |
//|                                                  Roman Martynyuk |
//|                                           http://www.dml-ewa.ru/ |
//+------------------------------------------------------------------+
#property copyright "Roman Martynyuk"
#property link      "http://www.dml-ewa.ru/"

#include <Arrays\ArrayObj.mqh>
#include "Node.mqh"

class Node_info : public CObject
{
  public:
    int index1;
    int index2;
    double value1;
    double value2;
    Node *node;
};

class Node_infos : public CArrayObj
{
  public:
    string name_subwaves;
    void Add(int index1, int index2, double value1, double value2, Node *node);
    Node_info *get_node_info(int index1, int index2, double value1, double value2);
};

void Node_infos::Add(int index1, int index2, double value1, double value2, Node *node)
{
  Node_info *node_info = new Node_info;
  node_info.index1 = index1;
  node_info.index2 = index2;
  node_info.value1 = value1;
  node_info.value2 = value2;
  node_info.node = node;
  Add(node_info);
}

Node_info *Node_infos::get_node_info(int index1, int index2, double value1, double value2)
{
  for(int i = Total() - 1; i >= 0; i--)
  {
    Node_info *node_info = At(i);
    if(node_info.index1 == index1 && node_info.index2 == index2 && node_info.value1 == value1 && node_info.value2 == value2)
    {
      return node_info;
    }
  }
  return(NULL);
}

class Counted : public CArrayObj
{
  public:
    Node_infos *get_node_infos(string name_subwaves);
    bool is(Wave *wave, int num_wave, Node *node, string name_subwaves);
};

Node_infos *Counted::get_node_infos(string name_subwaves)
{
  for(int i = Total() - 1; i >= 0; i--)
  {
    Node_infos *node_infos = At(i);
    if(node_infos.name_subwaves == name_subwaves)
    {
      return(node_infos);
    }
  }
  return(NULL);
}

bool Counted::is(Wave *wave, int num_wave, Node *node, string name_subwaves)
{
  ! IS_DEBUG_MODE ? Print("Вошел в Counted::is") : ;
  int index1 = wave.index[num_wave - 1];
  int index2 = wave.index[num_wave];
  double value1 = wave.value[num_wave - 1];
  double value2 = wave.value[num_wave];
  Node_infos *node_infos = get_node_infos(name_subwaves);
  if(CheckPointer(node_infos) != POINTER_INVALID)
  {
    Node_info *node_info = node_infos.get_node_info(index1, index2, value1, value2);
    if(CheckPointer(node_info) != POINTER_INVALID)
    {
      for(int i = 0; i < node_info.node.childs.Total(); i++)
      {
        node.childs.Add(node_info.node.childs.At(i));
      }
      return(true);
    }
    else
    {
      node_infos.Add(index1, index2, value1, value2, node);
      return(false);
    }
  }
  else
  {
    node_infos = new Node_infos;
    node_infos.name_subwaves = name_subwaves;
    node_infos.Add(index1, index2, value1, value2, node);
    Add(node_infos);
  }
  return(false);
}
