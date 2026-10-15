# Estratégia de Movimento Dirigido
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Esta estratégia replica o consultor especialista **Directed Movement** do MetaTrader. Ela aplica um Índice de Força Relativa (RSI) que é suavizado duas vezes por médias móveis. O primeiro suavizamento forma uma linha rápida enquanto o segundo suavizamento cria uma linha mais lenta.

As decisões de trading são baseadas no cruzamento das linhas rápida e lenta de forma contrária:

- **Comprar** quando a linha rápida cruza abaixo da linha lenta.
- **Vender** quando a linha rápida cruza acima da linha lenta.

Níveis opcionais de stop-loss e take-profit são aplicados como porcentagens do preço de entrada.

## Indicadores

- `RelativeStrengthIndex` – indicador de momentum base.
- `MovingAverage` – primeiro suavizamento do RSI (linha rápida).
- `MovingAverage` – segundo suavizamento da linha rápida (linha lenta).

## Regras de trading

1. Calcular o RSI a partir dos fechamentos das velas.
2. Suavizar o RSI com a primeira média móvel para obter a linha rápida.
3. Suavizar a linha rápida com a segunda média móvel para obter a linha lenta.
4. Entrar em uma posição comprada quando a linha rápida cruza abaixo da linha lenta. Fechar qualquer posição vendida antes de abrir a nova comprada.
5. Entrar em uma posição vendida quando a linha rápida cruza acima da linha lenta. Fechar qualquer posição comprada antes de abrir a nova vendida.
6. Aplicar proteções de stop-loss e take-profit se seus parâmetros forem maiores que zero.

## Parâmetros

| Nome | Descrição |
|------|-----------|
| `CandleType` | Série de velas usada para cálculos. |
| `RsiPeriod` | Período de cálculo do RSI. |
| `FirstMaType` | Tipo de média móvel usada para a linha rápida. |
| `FirstMaLength` | Período da média móvel rápida. |
| `SecondMaType` | Tipo de média móvel usada para a linha lenta. |
| `SecondMaLength` | Período da média móvel lenta. |
| `StopLossPercent` | Stop-loss em porcentagem do preço de entrada. |
| `TakeProfitPercent` | Take-profit em porcentagem do preço de entrada. |

