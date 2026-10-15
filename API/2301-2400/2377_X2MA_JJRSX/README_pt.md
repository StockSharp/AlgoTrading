# Estratégia X2MA JJRSX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que combina um filtro de tendência de média móvel dupla com um gatilho de entrada baseado em RSI.
A tendência é definida em um período superior comparando uma média rápida e uma lenta.
As entradas são executadas em um período inferior quando o RSI sai das zonas de sobrevenda ou sobrecompra na direção da tendência.

## Detalhes

- **Critérios de entrada**:
  - Comprado: tendência de alta e RSI cruza acima de `Oversold`
  - Vendido: tendência de baixa e RSI cruza abaixo de `Overbought`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Limiar RSI oposto ou reversão de tendência
- **Stops**: Nenhum
- **Valores padrão**:
  - `TrendCandleType` = velas de 4h
  - `SignalCandleType` = velas de 30m
  - `FastMaPeriod` = 12
  - `SlowMaPeriod` = 5
  - `RsiPeriod` = 8
  - `Overbought` = 70
  - `Oversold` = 30
