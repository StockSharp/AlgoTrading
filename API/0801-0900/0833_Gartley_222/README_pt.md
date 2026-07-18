# Estratégia Gartley 222
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera comprado quando um padrão harmônico Gartley 222 de alta se forma.
O padrão é detectado usando pivôs de alta e baixa validados por razões de Fibonacci.

Uma posição comprada é aberta `PivotLength` barras após a confirmação quando o preço fecha acima do ponto C.
A proteção fecha a posição em um alvo de extensão de Fibonacci ou em um stop-loss percentual fixo.

## Detalhes

- **Critérios de entrada**:
  - Padrão Gartley 222 de alta confirmado
  - Entrada atrasada por `PivotLength` barras
- **Comprado/Vendido**: Somente comprado
- **Critérios de saída**:
  - Stop-loss ou take profit
- **Stops**:
  - `Stop Loss %` abaixo da entrada
  - `TP Fib Extension` acima da entrada
- **Valores padrão**:
  - `Pivot Length` = 5
  - `Fib Tolerance` = 0.05
  - `TP Fib Extension` = 1.27
  - `Stop Loss %` = 2

- **Filtros**:
  - Categoria: Padrão
  - Direção: Somente comprado
  - Indicadores: Pivot points, Fibonacci
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
