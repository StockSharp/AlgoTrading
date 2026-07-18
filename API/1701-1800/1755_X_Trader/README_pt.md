# Estratégia X Trader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia implementa um sistema contrarian de cruzamento de médias móveis originalmente escrito em MQL como **X trader**.
Ela usa duas médias móveis simples e abre posições na direção oposta ao cruzamento. O risco é gerenciado
usando take-profit e stop-loss fixos em pontos absolutos via `StartProtection`.

## Como funciona

1. Assinar dados de candle do período de tempo especificado.
2. Calcular duas médias móveis com períodos configuráveis.
3. Rastrear os últimos dois valores de cada média para detectar um cruzamento.
4. Quando a média rápida cruza acima da média lenta e permanece acima por duas barras enquanto duas barras atrás estava abaixo,
   uma posição **vendida** é aberta.
5. Quando a média rápida cruza abaixo da média lenta e permanece abaixo por duas barras enquanto duas barras atrás estava acima,
   uma posição **comprada** é aberta.
6. Apenas uma posição pode estar aberta por vez. A proteção fecha automaticamente as negociações quando o preço se move pelo
   valor configurado de take-profit ou stop-loss.

## Parâmetros

- `CandleType` – série de candles a ser usada.
- `Ma1Period` – período da primeira média móvel.
- `Ma2Period` – período da segunda média móvel.
- `TakeProfitPoints` – alvo de lucro em pontos de preço.
- `StopLossPoints` – limite de perda em pontos de preço.

## Indicador

- `SimpleMovingAverage` – usado duas vezes com períodos diferentes.

## Gestão de risco

`StartProtection` é habilitado em `OnStarted` e aplica os valores de take-profit e stop-loss a todas as posições.
