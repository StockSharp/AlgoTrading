# Estrategia MA Rounding Candle
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Esta estrategia es una interpretación del asesor experto original de MQL5 "MA Rounding Candle". Utiliza dos medias móviles suavizadas aplicadas a los precios de apertura y cierre de las velas. La posición relativa de estas medias define el color de una vela sintética: verde cuando el cierre suavizado está por encima de la apertura, roja cuando el cierre está por debajo de la apertura y gris cuando son iguales. Un cambio de color respecto a la barra anterior genera señales de trading.

## Algoritmo

1. Para cada vela completada, los valores de apertura y cierre se suavizan con una media móvil simple de longitud configurable.
2. El color de la vela se define comparando los valores suavizados:
   - **Vela alcista** – el cierre suavizado es mayor que la apertura suavizada.
   - **Vela bajista** – el cierre suavizado es menor que la apertura suavizada.
   - **Neutral** – ambos valores son iguales.
3. Si la vela anterior fue alcista y la actual ya no lo es, la estrategia entra en una posición larga y cierra cualquier corta.
4. Si la vela anterior fue bajista y la actual ya no lo es, la estrategia entra en una posición corta y cierra cualquier larga.

## Parámetros

- **MaLength** – período de las medias móviles suavizadoras (predeterminado 12).
- **CandleType** – marco temporal de las velas procesadas.

## Notas

La estrategia demuestra cómo recrear señales de un indicador personalizado usando solo las herramientas integradas de StockSharp. No se aplica stop loss ni take profit; las posiciones se invierten inmediatamente cuando aparece la señal opuesta.
