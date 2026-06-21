# Estratégia X Trail 2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera com base no cruzamento de duas médias móveis configuráveis calculadas a partir de um tipo de preço escolhido.

## Detalhes
- **Entrada**: Compra quando MA1 cruza acima de MA2 e este cruzamento é confirmado pelas duas barras anteriores; vende quando o oposto ocorre.
- **Saída**: Cruzamento oposto.
- **Indicadores**: Duas médias móveis com tipo selecionável (simple, exponential, smoothed, weighted) e fonte de preço (close, open, high, low, median, typical, weighted).
- **Parâmetros**:
  - `Ma1Length` = 1
  - `Ma1Type` = MovingAverageTypeEnum.Simple
  - `Ma1PriceType` = AppliedPriceType.Median
  - `Ma2Length` = 14
  - `Ma2Type` = MovingAverageTypeEnum.Simple
  - `Ma2PriceType` = AppliedPriceType.Median
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
