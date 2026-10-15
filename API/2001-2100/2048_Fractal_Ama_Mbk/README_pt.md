# Estratégia de Cruzamento Fractal AMA MBK
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia de cruzamento Fractal AMA MBK usa a **Média Móvel Adaptativa Fractal (FRAMA)** juntamente com uma linha de gatilho de **Média Móvel Exponencial (EMA)**. Os sinais de operação são gerados quando a linha FRAMA cruza a linha EMA.

## Como funciona
- FRAMA adapta seu fator de suavização com base na dimensão fractal do movimento de preço recente.
- A EMA atua como linha de gatilho que suaviza os dados de preço.
- **Entrada comprada:** quando FRAMA cruza acima da EMA e nenhuma posição comprada está aberta.
- **Entrada vendida:** quando FRAMA cruza abaixo da EMA e nenhuma posição vendida está aberta.
- As posições existentes podem ser protegidas com níveis opcionais de stop-loss e take-profit.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `CandleType` | Tipo e período dos candles usados para cálculos (padrão: candles de 4 horas). |
| `FramaPeriod` | Comprimento do período para o indicador FRAMA. |
| `SignalPeriod` | Comprimento do período para a linha de gatilho EMA. |
| `StopLoss` | Distância do stop-loss do preço de entrada em unidades de preço absolutas (0 desativa). |
| `TakeProfit` | Distância do take-profit do preço de entrada em unidades de preço absolutas (0 desativa). |
| `Volume` | Volume de operação em lotes. |

## Notas
- Apenas candles concluídos são processados.
- As operações são executadas com ordens a mercado (`BuyMarket`/`SellMarket`).
- Os parâmetros `FramaPeriod` e `SignalPeriod` suportam otimização.
