# Estratégia de Execução de Ordens OCO
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia replica um ticket de ordem "One Cancels the Other" escrito originalmente para MetaTrader. Permite ao trader definir até quatro gatilhos de preço independentes:

- **Buy Limit Price**
- **Sell Limit Price**
- **Buy Stop Price**
- **Sell Stop Price**

A estratégia se inscreve em dados Level1 para monitorar continuamente o melhor bid e ask. Quando um preço gatilho é atingido, envia uma ordem a mercado na direção correspondente. Após a execução de uma ordem, proteções de stop-loss e take-profit são aplicadas usando distâncias medidas em pips. Essas distâncias são automaticamente convertidas para preços absolutos com base no `PriceStep` do instrumento.

Quando o **modo OCO** está habilitado, atingir qualquer gatilho desativará automaticamente todos os demais, implementando efetivamente o comportamento clássico de uma-cancela-a-outra. Se o modo OCO estiver desabilitado, os outros gatilhos permanecem ativos e podem abrir posições adicionais conforme os preços continuam a se mover.

## Detalhes

- **Critérios de entrada**:
  - Comprado quando `Ask <= BuyLimitPrice` (gatilho Buy Limit).
  - Comprado quando `Ask >= BuyStopPrice` (gatilho Buy Stop).
  - Vendido quando `Bid >= SellLimitPrice` (gatilho Sell Limit).
  - Vendido quando `Bid <= SellStopPrice` (gatilho Sell Stop).
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - As posições são fechadas automaticamente por níveis de stop-loss ou take-profit predefinidos.
- **Stops**: Sim, stop-loss e take-profit em pips.
- **Valores padrão**:
  - `StopLossPips` = 300.
  - `TakeProfitPips` = 300.
  - `OCO Mode` = habilitado.
- **Filtros**:
  - Categoria: Execução de ordens.
  - Direção: Ambos.
  - Indicadores: Nenhum.
  - Stops: Sim.
  - Complexidade: Simples.
  - Período: Baseado em ticks.
  - Sazonalidade: Não.
  - Redes neurais: Não.
  - Divergência: Não.
  - Nível de risco: Médio.
