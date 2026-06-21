# Estrategia Tester v0.14
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia de ejemplo es un port simplificado del script MQL4 "Tester v0.14" originalmente diseñado para EURUSD en el marco temporal H4.

## Lógica

- Calcula una media móvil simple de 14 períodos y MACD.
- Genera una señal de compra cuando el precio de cierre está por encima de la SMA y el MACD es positivo.
- Genera una señal de venta cuando el precio de cierre está por debajo de la SMA y el MACD es negativo.
- Después de abrir una orden, la posición se cierra tras un número configurable de barras.

Este port utiliza la API de alto nivel de StockSharp, basándose en `SubscribeCandles` y `Bind` para recibir los valores del indicador.

## Parámetros

- **MinSignSum** – número mínimo de señales requeridas para abrir una posición.
- **Risk** – porcentaje del saldo de la cuenta utilizado para la gestión de dinero.
- **TakeProfit / StopLoss** – niveles fijos en puntos.
- **BarsNumber** – número de barras para mantener una posición abierta.
- **CandleType** – serie de velas utilizada (predeterminado: 4H).

## Notas

El archivo MQL original contenía cientos de combinaciones de reglas. Este ejemplo en C# ilustra la estructura usando un conjunto reducido de reglas para mayor claridad.
