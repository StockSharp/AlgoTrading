# Estratégia Psi Proc EMA MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia replica o sistema T4 do expert MQL original `e-PSI@PROC.mq4`. Opera com base no alinhamento de múltiplas médias móveis exponenciais e um filtro MACD.

## Lógica da Estratégia

1. Calcular EMA(200), EMA(50) e EMA(10) em cada candle recebido.
2. Calcular MACD com parâmetros 12, 26, 9.
3. Ir comprado quando:
   - EMA200 sobe e EMA50 > EMA200.
   - EMA50 sobe e EMA10 > EMA50.
   - MACD sobe e está acima de `LimitMACD`.
4. Ir vendido quando:
   - EMA200 cai e EMA50 < EMA200.
   - EMA50 cai e EMA10 < EMA50.
   - MACD cai e está abaixo de `-LimitMACD`.
5. Sair do comprado quando o preço fecha abaixo do EMA50.
6. Sair do vendido quando o preço fecha acima do EMA50.

Proteções opcionais de take-profit e trailing stop são suportadas.

## Parâmetros

| Nome | Descrição |
| ---- | --------- |
| `LimitMACD` | Nível mínimo absoluto de MACD para permitir entrada. |
| `TakeProfitPoints` | Nível de take-profit em pontos de preço. |
| `TrailStopPoints` | Nível de trailing stop em pontos de preço. |
| `CandleType` | Período dos candles usados pela estratégia. |

## Notas

- As operações são abertas com ordens a mercado.
- Apenas candles completos são processados.
- A estratégia opera em um único ativo.
