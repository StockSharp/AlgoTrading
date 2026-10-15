# Estratégia Volume Weighted MA Sistema Digital
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia replica o **Volume Weighted MA Digit System**. Ela constrói duas médias móveis ponderadas por volume (VWMA) baseadas nas máximas e mínimas dos candles. O cruzamento do preço por essas bandas fornece sinais de negociação.

## Como Funciona

1. **Indicadores**
   - `VWMA High`: VWMA aplicada às máximas dos candles.
   - `VWMA Low`: VWMA aplicada às mínimas dos candles.
2. **Sinais**
   - **Entrada Comprada**: O preço de fechamento cruza acima de `VWMA High`.
   - **Entrada Vendida**: O preço de fechamento cruza abaixo de `VWMA Low`.
   - O cruzamento oposto fecha posições abertas.
3. **Gestão de Risco**
   - Utiliza `StartProtection` integrado com stop loss e take profit configuráveis (em pontos).

## Parâmetros

| Nome | Descrição | Padrão |
|------|-----------|--------|
| `VwmaPeriod` | Comprimento de cálculo da VWMA | `12` |
| `CandleType` | Período dos candles utilizado para o cálculo | `4h` |
| `StopLoss` | Stop loss em pontos | `1000` |
| `TakeProfit` | Take profit em pontos | `2000` |

## Notas

- Apenas candles fechados são processados.
- A estratégia usa recursos de API de alto nível como `SubscribeCandles`, `Bind` e indicadores padrão.
- Estratégia MQL original: `Exp_Volume_Weighted_MA_Digit_System.mq5`.
