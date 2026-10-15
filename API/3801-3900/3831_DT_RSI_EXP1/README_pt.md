# DT RSI Estratégia EXP1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta porta replica o consultor especialista MT4 **DT-RSI-EXP1**. A estratégia verifica oscilações RSI de 15 minutos para detectar topos duplos ou fundos duplos em torno dos níveis 60/40. Uma negociação longa é realizada quando os picos recentes de RSI recuam sem imprimir quaisquer mínimos abaixo de 40, enquanto o filtro de tendência de 4 horas aponta para baixo. Os shorts refletem a lógica com mínimos acima de 60 e um filtro de tendência ascendente. Um stop-loss e um take-profit fixos estão associados a cada posição, e um trailing stop opcional protege os lucros. As posições são fechadas à força quando RSI atinge níveis extremos de 70/30, copiando o comportamento de saída original.

## Detalhes

- **Critérios de entrada**:
  - **Longo**: dois picos de alta RSI com o segundo acima de 60, sem mínimos de baixa abaixo de 40 no meio, 4 horas EMA abaixo do fechamento anterior, RSI(1) cruzando acima da linha de pescoço projetada, RSI(2) ainda abaixo dela, RSI(2) < 50 e RSI(0) <55.
  - **Venda**: duas baixas RSI de baixa com a segunda abaixo de 40, sem picos de alta acima de 60 no meio, 4 horas EMA acima do fechamento anterior, RSI(1) cruzando abaixo da linha de pescoço projetada, RSI(2) > 50 e RSI(0) > 47.
- **Longo/Curto**: Ambas as direções.
- **Critérios de saída**:
  - RSI extremos (RSI > 70 para longos, RSI < 30 para curtos).
  - Metas de stop-loss/take-profit calculadas a partir de etapas de preço.
  - Trailing stop opcional que bloqueia os lucros quando o preço se move em `TrailingStopPoints`.
- **Stops**: Stop-loss e take-profit fixos, trailing stop opcional.
- **Valores padrão**:
  - `CandleType` = velas de 15 minutos.
  - `TrendCandleType` = velas de 240 minutos (filtro de tendência EMA).
  - `RsiPeriod` = 47.
  - `StopLossPoints` = 26.
  - `TakeProfitPoints` = 76.
  - `TrailingStopPoints` = 0 (desabilitado).
- **Filtros**:
  - Categoria: entradas de acompanhamento de tendências em estruturas RSI.
  - Direção: Ambos.
  - Indicadores: RSI, EMA filtro de tendência.
  - Paradas: Sim.
  - Complexidade: Intermediária (detecção de oscilação com múltiplas restrições).
  - Prazo: Intradiário (M15 com filtro H4).
  - Sazonalidade: Não.
  - Redes Neurais: Não.
  - Divergência: Não.
  - Nível de risco: Médio.

## Parâmetros

| Nome | Padrão | Descrição | Otimizável |
| ---- | ------- | ----------- | ----------- |
| `CandleType` | 15 minutos | Série de velas primárias usadas para calcular RSI e sinais. | Sim |
| `TrendCandleType` | 240 minutos | Prazo maior utilizado pelo filtro de tendência EMA (substituição do indicador MT4 RFTL). | Sim |
| `RsiPeriod` | 47 | Comprimento RSI aplicado às velas primárias. | Sim |
| `StopLossPoints` | 26 | Distância até o stop loss em etapas de preço. | Sim |
| `TakeProfitPoints` | 76 | Distância até o take-profit em etapas de preço. | Sim |
| `TrailingStopPoints` | 0 | Deslocamento do trailing stop nas etapas de preço (`0` desativa o trailing). | Sim |

## Notas

- O indicador personalizado MetaTrader `RFTL` é aproximado com um EMA de 10 períodos no período de 240 minutos. Ajuste o período maior ou duração EMA para melhor corresponder ao ambiente original.
- Certifique-se de que `PriceStep` e `StepPrice` do instrumento estejam configurados para que as paradas baseadas em pontos se alinhem com o tamanho do tick da corretora.
- O trailing stop só é ativado quando o preço avança mais de `TrailingStopPoints` em relação ao preço de entrada e nunca diminui além do stop original.
