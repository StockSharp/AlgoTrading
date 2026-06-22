# Estratégia Polish Layer
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia Polish Layer** é uma conversão do consultor especialista do MetaTrader de `MQL/17484` para a API de alto nível do StockSharp. Tem como alvo a continuação de tendência de curto prazo em pares forex usando velas de 5 ou 15 minutos. A direção da tendência é definida pela relação entre médias móveis exponenciais rápida e lenta e o momentum recente do Índice de Força Relativa (RSI). A confirmação de entrada requer sinais sincronizados do Oscilador Estocástico, DeMarker e Williams %R.

## Indicadores
- **Média Móvel Exponencial (EMA)** – filtros de tendência rápido (`ShortEmaPeriod`) e lento (`LongEmaPeriod`).
- **Índice de Força Relativa (RSI)** – filtro de inclinação de momentum derivado dos valores de velas anteriores.
- **Oscilador Estocástico** – detecta reversões de sobrecompra/sobrevenda via cruzamentos de limiar %K.
- **DeMarker** – confirma fases de acumulação/distribuição.
- **Williams %R** – valida reversões de momentum em níveis extremos.

## Parâmetros
| Parâmetro | Valores padrão | Descrição |
|-----------|---------|-------------|
| `ShortEmaPeriod` | 9 | Comprimento do filtro de tendência EMA rápida. |
| `LongEmaPeriod` | 45 | Comprimento do filtro de tendência EMA lenta. |
| `RsiPeriod` | 14 | Lookback RSI usado para comparação de inclinação de momentum. |
| `StochasticKPeriod` | 5 | Lookback da linha %K. |
| `StochasticDPeriod` | 3 | Período de suavização para %D. |
| `StochasticSlowing` | 3 | Fator de desaceleração final aplicado a %K. |
| `WilliamsRPeriod` | 14 | Janela de lookback do Williams %R. |
| `DeMarkerPeriod` | 14 | Janela de lookback do DeMarker. |
| `TakeProfitPoints` | 17 | Distância ao alvo de lucro em pontos de preço (usa `Security.PriceStep`). |
| `StopLossPoints` | 77 | Distância ao stop protetor em pontos de preço. |
| `CandleType` | 5 minutos | Tipo de dados de vela processado pela estratégia. |
| `Volume` | 1 | Tamanho de operação usado para entradas a mercado. |

## Lógica de trading
1. **Filtro de tendência** – a vela anterior deve mostrar a EMA rápida acima da EMA lenta e o RSI subindo (RSI anterior > RSI de duas barras atrás) para cenários comprados. A configuração inversa define cenários vendidos.
2. **Confirmação do oscilador** – as entradas só são consideradas quando a estratégia está plana e todas as condições seguintes são atendidas:
   - **Estocástico %K** cruza acima de 19 para comprados ou abaixo de 81 para vendidos.
   - **DeMarker** cruza acima de 0.35 para comprados ou abaixo de 0.63 para vendidos.
   - **Williams %R** cruza acima de -81 para comprados ou abaixo de -19 para vendidos.
3. **Execução de ordens** – a estratégia envia ordens a mercado usando `BuyMarket(Volume)` ou `SellMarket(Volume)` e depende de `StartProtection` para anexar automaticamente os offsets de stop-loss e take-profit.

## Gestão de risco
- As ordens protetoras são criadas via `StartProtection`, transformando `TakeProfitPoints` e `StopLossPoints` em distâncias absolutas de preço baseadas no instrumento `PriceStep`.
- O algoritmo permanece fora do mercado até que as posições existentes sejam fechadas pelas ordens protetoras, espelhando o comportamento do consultor especialista original.

## Notas de uso
- Funciona melhor em pares forex líquidos com velas de 5 ou 15 minutos.
- Garantir que os metadados do instrumento contenham um `PriceStep` válido; caso contrário, ajustar `TakeProfitPoints` e `StopLossPoints` para corresponder ao tamanho do tick do instrumento.
- Considerar testes prospectivos antes do deployment ao vivo, pois a sequência de confirmação é sensível ao suavização de indicadores e incrementos de preço do corretor.
