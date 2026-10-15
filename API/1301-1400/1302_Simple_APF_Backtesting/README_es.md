# Backtesting de Estrategia APF Simple
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia implementa un modelo simplificado de Predicción de Precios por Autocorrelación (APF). Detecta ciclos de precios mediante autocorrelación y pronostica el precio futuro usando una regresión lineal de los rendimientos recientes. Se abre una posición larga cuando la ganancia predicha supera un umbral especificado. La posición se cierra cuando se alcanza el precio objetivo.

## Parámetros

- `Length` – número de barras utilizadas para la autocorrelación y la regresión.
- `Threshold Gain` – aumento mínimo esperado del precio para entrar en una operación.
- `Signal Threshold` – nivel de autocorrelación requerido para almacenar un pronóstico.
- `Candle Type` – tipo de velas para los cálculos.
