# Estrategia de Cierre VWAP
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia calcula una Media Móvil Ponderada por Volumen (VWMA) de los precios de cierre. Cuando la VWMA cambia de dirección, actúa como señal para posibles entradas o salidas:

- Si la VWMA estaba cayendo y gira al alza (forma un valle), la estrategia cierra cualquier posición corta y puede abrir una posición larga.
- Si la VWMA estaba subiendo y gira a la baja (forma un pico), la estrategia cierra cualquier posición larga y puede abrir una posición corta.

## Parámetros
- **Period** – número de velas utilizadas para el cálculo de la VWMA.
- **Candle Type** – marco temporal de las velas procesadas.
- **Buy Open** – habilitar la apertura de posiciones largas.
- **Sell Open** – habilitar la apertura de posiciones cortas.
- **Buy Close** – permitir el cierre de posiciones largas cuando la VWMA gira a la baja.
- **Sell Close** – permitir el cierre de posiciones cortas cuando la VWMA gira al alza.

## Notas
La estrategia usa el indicador `VolumeWeightedMovingAverage` de StockSharp y procesa solo velas completadas. El volumen de la operación se toma de la propiedad `Volume` de la estrategia; al abrir una nueva posición, cualquier posición opuesta se cierra automáticamente.
