# Estrategia de Línea de Activación (Trigger Line)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Trigger Line combina una línea de tendencia ponderada con una media móvil de mínimos cuadrados (LSMA). Se abre una posición larga cuando la línea de tendencia ponderada cruza por encima de la LSMA, mientras que se abre una posición corta cuando cruza por debajo.

## Cómo funciona
- **Entrada larga**: la línea de tendencia ponderada cruza por encima de la LSMA.
- **Salida larga**: la línea de tendencia ponderada cruza por debajo de la LSMA.
- **Entrada corta**: la línea de tendencia ponderada cruza por debajo de la LSMA.
- **Salida corta**: la línea de tendencia ponderada cruza por encima de la LSMA.
- **Indicadores**: Media Móvil Ponderada, Regresión Lineal (LSMA).

## Parámetros
- **WT Period** – período de retroceso para la línea de tendencia ponderada.
- **LSMA Period** – período de suavizado para la LSMA.
- **Candle Type** – marco temporal de las velas utilizadas para los cálculos.
