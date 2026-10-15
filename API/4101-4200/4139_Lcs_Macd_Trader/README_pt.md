# Estratégia de trader LCS MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma versão StockSharp do consultor especialista "LCS-MACD-Trader" MetaTrader 4. Ele negocia cruzamentos MACD que ocorrem abaixo/acima da linha zero e, opcionalmente, requer uma confirmação do oscilador Stochastic. A lógica também reflete os filtros de hora do dia originais e o gerenciamento de ponto de equilíbrio/stop móvel no estilo MetaTrader.

## Como funciona

- Entradas longas são acionadas quando a linha MACD cruza acima de sua linha de sinal enquanto ambas permanecem abaixo de zero. Se o filtro estocástico estiver habilitado, a linha %D deve estar acima de %K dentro do lookback especificado e a vela atual deve mostrar %D caindo abaixo de %K.
- As entradas curtas são acionadas quando a linha MACD cruza abaixo de sua linha de sinal enquanto ambas permanecem acima de zero. Com o filtro estocástico ativado, a linha %D deve ter estado recentemente abaixo de %K e agora volta a subir acima dela.
- A negociação só é permitida dentro de três janelas intradiárias configuráveis que replicam as configurações de EA.
- As distâncias de take-profit, stop-loss, ponto de equilíbrio e trailing-stop são expressas em pips e convertidas usando o tamanho do ponto do instrumento.
- Apenas uma posição líquida por direção é mantida (rede StockSharp). O empilhamento de posições é permitido até `MaxOrders` lotes; sinais opostos aguardam até que a posição líquida atual seja fechada pela gestão de risco.

## Parâmetros

| Nome | Descrição | Padrão |
| --- | --- | --- |
| `CandleType` | Série de velas usada para cálculos de indicadores. | Período de 15 minutos |
| `FastEmaPeriod` | Período EMA rápido no MACD. | 12 |
| `SlowEmaPeriod` | Período EMA lento no MACD. | 26 |
| `SignalPeriod` | Período da linha de sinal no MACD. | 9 |
| `UseStochasticFilter` | Exigir confirmação estocástica antes das entradas. | verdade |
| `BarsToCheckStochastic` | Máximo de barras fechadas desde a relação estocástica oposta. | 5 |
| `StochasticKPeriod` | Comprimento de lookback de %K. | 5 |
| `StochasticDPeriod` | Comprimento de suavização de %D. | 3 |
| `StochasticSlowing` | Suavização adicional aplicada a %K. | 3 |
| `TradeVolume` | Tamanho do lote utilizado por entrada. | 0,1 |
| `TakeProfitPips` | Distância de lucro em pips. | 100 |
| `StopLossPips` | Distância de stop-loss em pips. | 100 |
| `MaxOrders` | Máximo de entradas empilhadas por direção. | 5 |
| `EnableTrailing` | Ative a lógica de trailing stop estilo MetaTrader. | falso |
| `TrailingActivationPips` | Lucro necessário antes do início do trailing. | 50 |
| `TrailingDistancePips` | Distância mantida pelo trailing stop. | 25 |
| `BreakEvenActivationPips` | Lucro necessário para mover o stop para o ponto de equilíbrio. | 25 |
| `BreakEvenOffsetPips` | Pips adicionais adicionados ao colocar o ponto de equilíbrio. | 1 |
| `Session1Start/End`, `Session2Start/End`, `Session3Start/End` | Janelas de negociação intradiária. | 08h15-08h35, 13h45-14h42, 22h15-22h45 |

## Notas

- A estratégia pressupõe uma conta de compensação. Fecha posições existentes através das regras de risco configuradas em vez de cobrir ordens opostas como a versão MT4 original.
- A conversão de pip usa o tamanho do ponto do instrumento. Para símbolos FX de 5 dígitos, a lógica dimensiona automaticamente os valores de pip em 10 para corresponder à configuração do multiplicador EA.
- A lógica de trailing stop e ponto de equilíbrio é avaliada em velas finalizadas e usa o máximo/mínimo de cada barra para emular o comportamento MetaTrader baseado em ticks.
