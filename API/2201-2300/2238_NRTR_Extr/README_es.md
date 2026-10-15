# Estrategia NRTR Extr
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia implementa el algoritmo **Nick Rypock Trailing Reverse** (NRTR) con flechas de señal adicionales. Es una conversión del ejemplo original de MQL5 "Exp_NRTR_extr" a la API de alto nivel de StockSharp.

## Cómo funciona

- El `NrtrExtrIndicator` personalizado calcula un rango promedio durante un período configurable y dibuja un nivel de trailing que sigue al precio.
- Cuando el precio invierte más allá de este nivel, el indicador cambia de dirección y emite una señal de compra o venta.
- La estrategia abre una posición larga ante una señal de compra y una posición corta ante una señal de venta.
- Las posiciones existentes se cierran ante la señal opuesta o cuando se alcanzan los niveles definidos de stop loss o take profit.

## Parámetros

| Nombre | Descripción |
| --- | --- |
| `Period` | Número de velas usadas para el cálculo del rango promedio. |
| `Digits Shift` | Ajuste de precisión adicional aplicado al factor de rango. |
| `Stop Loss` | Stop protector en puntos de precio. |
| `Take Profit` | Objetivo de ganancia en puntos de precio. |
| `Enable Buy Open` / `Enable Sell Open` | Permitir apertura de posiciones largas o cortas. |
| `Enable Buy Close` / `Enable Sell Close` | Permitir cierre de posiciones existentes ante señales opuestas. |
| `Candle Type` | Marco temporal de velas usado para el indicador. |

## Notas

El indicador se basa en el Average True Range para estimar la volatilidad del mercado. Para visualización, la estrategia dibuja automáticamente velas y operaciones ejecutadas en el área del gráfico.

