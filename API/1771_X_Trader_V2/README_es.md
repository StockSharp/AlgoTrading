# Estrategia X Trader V2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es un sistema de cruce de medias móviles contrario convertido del experto original MQL4 **X_trader_v2**. Utiliza dos medias móviles para detectar reversiones repentinas y ejecuta operaciones en sentido opuesto a la dirección del cruce.

## Cómo funciona
1. Se calculan dos medias móviles simples en el marco temporal seleccionado.
2. Cuando la MA rápida cruza **por encima** de la MA lenta, la estrategia abre una posición **corta**.
3. Cuando la MA rápida cruza **por debajo** de la MA lenta, la estrategia abre una posición **larga**.
4. Solo puede haber una posición abierta a la vez. Una nueva operación se coloca solo después de que la anterior se cierre y aparezca una nueva señal.
5. La protección integrada coloca automáticamente órdenes de stop-loss y take-profit.

## Parámetros
- `Ma1Period` – período de la media móvil rápida.
- `Ma2Period` – período de la media móvil lenta.
- `TakeProfitTicks` – distancia del take-profit en ticks de precio.
- `StopLossTicks` – distancia del stop-loss en ticks de precio.
- `CandleType` – tipo de vela utilizado para los cálculos.

## Notas
- La estrategia se suscribe a datos de velas a través de la API de alto nivel.
- Los valores de los indicadores se procesan mediante enlaces sin llamadas directas a `GetValue`.
- El algoritmo almacena internamente los valores anteriores de los indicadores para evitar búsquedas exhaustivas en el historial.
