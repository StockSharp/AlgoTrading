# Estrategia VininI Trend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción
Esta estrategia convierte el asesor experto original de MQL **Exp_VininI_Trend** a StockSharp. Utiliza el Índice de Canal de Materias Primas (CCI) para emular el oscilador VininI Trend. Se abre una posición larga cuando el oscilador supera el nivel superior o gira hacia arriba. Se abre una posición corta cuando el oscilador cae por debajo del nivel inferior o gira hacia abajo. La estrategia trabaja únicamente con velas completadas.

## Parámetros
- **CCI Period** – longitud del indicador CCI.
- **Upper Level** – umbral que activa las señales de compra.
- **Lower Level** – umbral que activa las señales de venta.
- **Entry Modes** – `Breakdown` reacciona a los cruces de nivel, `Twist` reacciona a los cambios de dirección.
- **Candle Type** – marco temporal de las velas usadas para los cálculos.

## Original
Convertido de la estrategia MQL5 ubicada en `MQL/1365/exp_vinini_trend.mq5`.
