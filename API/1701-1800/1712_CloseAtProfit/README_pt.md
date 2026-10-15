# Estratégia de Fechamento por Lucro
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia monitora o lucro e a perda realizados de todas as operações executadas pela estratégia. Quando o lucro acumulado excede um limite definido pelo usuário, ela fecha imediatamente qualquer posição aberta e opcionalmente cancela ordens ativas. O mesmo comportamento pode ser habilitado para drawdown definindo um limite de perda.

A estratégia não analisa indicadores nem movimentos de preço. Em vez disso, atua como uma camada protetora que sai do mercado assim que um alvo monetário ou nível de stop é atingido. Uma simples assinatura de velas é usada apenas para verificações periódicas do valor atual de PnL.

## Parâmetros

- **UseProfitToClose** – habilitar ou desabilitar o fechamento por alvo de lucro. Padrão: `true`.
- **ProfitToClose** – valor de lucro em unidades de moeda que aciona uma saída completa. Padrão: `20`.
- **UseLossToClose** – habilitar ou desabilitar o fechamento por limite de perda. Padrão: `false`.
- **LossToClose** – valor de perda em unidades de moeda que aciona uma saída completa quando excedido. Padrão: `100`.
- **ClosePendingOrders** – cancelar todas as ordens ativas ao fechar posições. Padrão: `true`.
- **CandleType** – tipo de velas usado para acionar verificações periódicas. Padrão: período de `1` minuto.

## Lógica de Negociação

1. Assinar velas do período selecionado.
2. A cada vela finalizada, calcular o PnL realizado atual.
3. Se o lucro for maior ou igual a `ProfitToClose`, fechar toda a posição e opcionalmente cancelar ordens pendentes.
4. Se o monitoramento de perdas estiver habilitado e o PnL atual for menor ou igual a `-LossToClose`, fechar toda a posição e opcionalmente cancelar ordens pendentes.

## Notas Adicionais

- A estratégia fecha apenas a posição do ativo ao qual está vinculada.
- As ordens pendentes são canceladas usando o método integrado `CancelActiveOrders`.
- A lógica pode ser combinada com outras estratégias de entrada para implementar realização de lucros ou proteção de portfólio.

## Filtros

- Categoria: Gestão de risco
- Direção: Ambos
- Indicadores: Nenhum
- Stops: Sim
- Complexidade: Básico
- Período: Qualquer
- Sazonalidade: Não
- Redes neurais: Não
- Divergência: Não
- Nível de risco: Médio
