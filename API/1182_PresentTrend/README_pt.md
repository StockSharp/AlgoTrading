# Estratégia PresentTrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Usa limiares baseados em ATR com RSI ou MFI para rastrear a direção da tendência. A linha PresentTrend é construída expandindo ou contraindo com base no valor do oscilador e no ATR. Os sinais aparecem quando PresentTrend cruza seu valor de duas barras atrás e o sinal oposto mais recente confirma a direção.

- **Comprado**: PresentTrend cruza acima de seu valor de duas barras antes e o último sinal vendido foi mais recente que o comprado anterior.
- **Vendido**: PresentTrend cruza abaixo de seu valor de duas barras antes e o último sinal comprado foi mais recente que o vendido anterior.
- **Indicadores**: ATR, RSI ou MFI.
- **Stops**: Fecha a posição quando o sinal oposto aparece no modo unilateral.
