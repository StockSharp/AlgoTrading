# Estrategia Color Bulls
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Esta estrategia es un port del experto MetaTrader `Exp_ColorBulls`. Se basa en el indicador Color Bulls, que calcula la diferencia entre el precio máximo de la vela y una media móvil. El valor resultante se suaviza con otra media móvil y se muestra como un histograma con diferentes colores para valores en alza y en baja.

La estrategia reacciona a los cambios de color de este histograma:

- Cuando el indicador pasa de subir (verde) a bajar (magenta), se abre una posición larga.
- Cuando el indicador pasa de bajar a subir, se abre una posición corta.
- Las posiciones opuestas se cierran automáticamente antes de abrir nuevas.

Solo se procesan velas completadas y se usan órdenes de mercado para entradas y salidas.

## Parámetros

- **Fast MA Length** – período de la media móvil aplicada a los precios máximos.
- **Smooth Length** – período de la media móvil utilizada para suavizar el valor bulls.
- **Candle Type** – marco temporal de las velas utilizadas para los cálculos.

## Notas

Este ejemplo demuestra la integración de un indicador personalizado con la API de alto nivel de StockSharp. La gestión de stop-loss y take-profit no está incluida.
