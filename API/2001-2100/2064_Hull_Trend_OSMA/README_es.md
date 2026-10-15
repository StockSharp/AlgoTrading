# Estrategia Hull Trend OSMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una conversión del asesor experto MetaTrader "Exp_HullTrendOSMA".

## Descripción general

La estrategia utiliza el indicador Hull Trend OSMA, que calcula una Hull Moving Average y una versión suavizada de la misma. El valor del oscilador es la diferencia entre estas dos series. Cuando el oscilador sube durante dos velas completadas consecutivas, la estrategia abre una posición larga. Cuando el oscilador baja durante dos velas completadas consecutivas, la estrategia abre una posición corta. Las posiciones opuestas se cierran en cada señal.

## Parámetros

- **Hull Period** – período para la Hull Moving Average.
- **Signal Period** – período de la media móvil de suavizado aplicada al oscilador.
- **Take Profit** – distancia para órdenes de take profit en unidades de precio.
- **Stop Loss** – distancia para órdenes de stop loss en unidades de precio.
- **Candle Type** – marco temporal de las velas utilizadas para los cálculos (por defecto 8 horas).

## Notas

- Utiliza la API de alto nivel de StockSharp con suscripción automática a velas.
- Las entradas y salidas se ejecutan con órdenes de mercado.
- La protección de stop loss y take profit se inicializa una vez al iniciar la estrategia.
