# Estratégia de Exp Cronex Chaikin
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia porta o expert advisor MetaTrader **Exp_CronexChaikin.mq5** para a API de alto nível do StockSharp. O robô original reconstrói o oscilador Chaikin a partir de valores de acumulação/distribuição, suaviza-o duas vezes com filtros Cronex "XMA" e opera os cruzamentos entre as linhas rápida e lenta. A versão StockSharp reproduz a mesma lógica enquanto expõe cada etapa como parâmetros configuráveis.

## Lógica de negociação

1. Assinar a série de velas configurada (`CandleType`).
2. Recalcular a linha de acumulação/distribuição (AD) para cada vela finalizada usando o `VolumeSource` selecionado (volume tick ou real).
3. Aplicar o oscilador Chaikin suavizando a linha AD com duas médias móveis (`ChaikinFastPeriod`, `ChaikinSlowPeriod`, `ChaikinMethod`) e tomando sua diferença.
4. Suavizar o oscilador resultante duas vezes usando os filtros Cronex controlados por `SmoothingMethod`, `FastPeriod`, `SlowPeriod` e `Phase`. Esses dois valores suavizados correspondem às linhas "rápida" e "sinal" no indicador original.
5. Olhar para trás `SignalBar` velas completadas e comparar ambas as linhas Cronex nessa barra e na anterior.
6. Quando a linha rápida está acima da lenta, a estratégia opcionalmente fecha posições vendidas e, se `BuyOpenEnabled` for verdadeiro, abre uma posição comprada se um cruzamento ascendente fresco for detectado na barra de lookback.
7. Quando a linha rápida está abaixo da lenta, as ações opostas são executadas para operações vendidas, controladas por `SellOpenEnabled` e `BuyCloseEnabled`.
8. Sempre que uma nova posição é aberta, ordens de stop-loss e take-profit (expressas em pontos) são recalculadas com `StopLoss` e `TakeProfit`.

Apenas uma única posição líquida é mantida. Se a direção do sinal mudar, a estratégia combina o volume necessário para fechar a posição atual com o novo tamanho de operação para imitar o comportamento de netting do MetaTrader.

## Indicadores e opções de suavização

- **Oscilador Chaikin**: Construído aplicando o tipo de média móvel `ChaikinMethod` selecionado à linha de acumulação/distribuição. As opções disponíveis incluem médias simples, exponenciais, suavizadas e linearmente ponderadas.
- **Suavizadores Cronex**: O parâmetro `SmoothingMethod` expõe a família Cronex XMA (SMA, EMA, SMMA, LWMA, Jurik JJMA/JurX, Parabolic MA, T3, VIDYA, AMA). O parâmetro `Phase` influencia filtros baseados em Jurik exatamente como na implementação MQL.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `CandleType` | Tipo de dados das velas usadas para calcular o indicador. O padrão é um período de quatro horas. |
| `ChaikinMethod` | Método de média móvel usado dentro do oscilador Chaikin. |
| `ChaikinFastPeriod` / `ChaikinSlowPeriod` | Períodos rápido e lento aplicados à linha de acumulação/distribuição. |
| `SmoothingMethod` | Algoritmo de suavização Cronex aplicado aos valores do oscilador Chaikin. |
| `FastPeriod` / `SlowPeriod` | Comprimentos das linhas Cronex rápida e lenta. |
| `Phase` | Parâmetro de fase para suavizadores baseados em Jurik (intervalo -100 a +100). |
| `VolumeSource` | Seleciona volume tick ou real ao calcular a linha de acumulação/distribuição. |
| `SignalBar` | Número de barras completadas para trás que deve conter o sinal de cruzamento. |
| `BuyOpenEnabled` / `SellOpenEnabled` | Ativar ou desativar abertura de operações compradas ou vendidas. |
| `BuyCloseEnabled` / `SellCloseEnabled` | Permitir fechar a posição oposta quando um sinal inverso aparecer. |
| `TakeProfit` / `StopLoss` | Distâncias de alvo de lucro e stop protetor em pontos do instrumento aplicadas após cada entrada. |
| `Volume` | Tamanho de posição padrão do StockSharp (age como tamanho de lote no expert original). |

## Diferenças em relação à versão MQL

- As rotinas de gestão monetária e slippage de `TradeAlgorithms.mqh` são substituídas pelos auxiliares integrados `Volume`, `SetStopLoss` e `SetTakeProfit`.
- A implementação do StockSharp recalcula a linha AD apenas em velas finalizadas, garantindo comportamento determinístico para testes e negociação ao vivo.
- As opções de suavização Cronex dependem de indicadores StockSharp: filtros Jurik são suportados por `JurikMovingAverage` (com controle de fase), enquanto VIDYA e ParMA usam aproximações exponenciais consistentes com outras conversões Cronex.
