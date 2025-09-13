//+------------------------------------------------------------------+
//|                                                         node.mqh |
//|                                                  Roman Martynyuk |
//|                                           http://www.dml-ewa.ru/ |
//+------------------------------------------------------------------+
#property copyright "Roman Martynyuk"
#property link      "http://www.dml-ewa.ru/"
#include <Object.mqh>
#include <Arrays\ArrayObj.mqh>
#include "Wave.mqh"

class Node : public CObject
{
  public:
    CArrayObj childs;
    Wave *wave;
    string text;
    Node *parent;
    bool selected;
    Node *add(string text, Wave *wave = NULL);
    void clear();
};

Node *Node::add(string text, Wave *wave = NULL)
{
  Node *node = new Node;
  node.parent = GetPointer(this);
  node.selected = false;
  node.text = text;
  node.wave = wave;
  childs.Add(node);
  return(node);
}

void Node::clear()
{
  for(int i = 0; i < childs.Total(); i++)
  {
    if(CheckPointer(childs.At(i)) != POINTER_INVALID)
    {
      Node *node = childs.At(i);
      node.clear();
    }
  }
  childs.FreeMode(true);
  childs.Clear();
  if(CheckPointer(wave) != POINTER_INVALID)
  {
    delete wave;
  }
}