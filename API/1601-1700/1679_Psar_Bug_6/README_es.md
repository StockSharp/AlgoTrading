# Estrategia Psar Bug 6
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Convertida del script MQL4 "psar_bug_6".

## Lógica
- Utiliza el indicador Parabolic SAR con paso y aceleración máxima configurables.
- Compra cuando el precio cierra por encima del SAR y previamente estaba por debajo.
- Vende cuando el precio cierra por debajo del SAR y previamente estaba por encima.
- El parámetro de reversión opcional invierte las señales de compra/venta.
- La opción `SarClose` cierra la posición existente cuando el SAR cambia al lado opuesto.
- Distancias fijas de take-profit y stop-loss en unidades de precio. Se puede activar el trailing stop.

## Parámetros
- `SarStep` – paso del factor de aceleración.
- `SarMax` – factor de aceleración máximo.
- `StopLoss` – distancia inicial del stop-loss.
- `TakeProfit` – distancia del take-profit.
- `Trailing` – activar trailing stop.
- `TrailStop` – distancia del trailing stop cuando está activado.
- `SarClose` – cerrar posición en reversión del SAR.
- `Reverse` – invertir señales de trading.
- `CandleType` – tipo de vela para los cálculos.

## Notas
La estrategia usa la API de alto nivel con suscripciones a velas y vinculación de indicadores. La protección se inicia con trailing stop opcional y salidas con órdenes de mercado.
