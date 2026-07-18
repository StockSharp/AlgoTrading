# Estrategia PresentTrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Utiliza umbrales basados en ATR con RSI o MFI para rastrear la dirección de la tendencia. La línea PresentTrend se construye expandiéndose o contrayéndose según el valor del oscilador y el ATR. Las señales aparecen cuando PresentTrend cruza su valor de dos barras atrás y la señal opuesta más reciente confirma la dirección.

- **Largo**: PresentTrend cruza por encima de su valor de dos barras antes y la última señal corta fue más reciente que el largo anterior.
- **Corto**: PresentTrend cruza por debajo de su valor de dos barras antes y la última señal larga fue más reciente que el corto anterior.
- **Indicadores**: ATR, RSI o MFI.
- **Stops**: Cierra la posición cuando aparece la señal opuesta en modo unilateral.
