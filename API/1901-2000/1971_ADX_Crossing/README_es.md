# Estrategia de Cruce ADX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **Estrategia de Cruce ADX** está construida en torno al indicador Average Directional Index (ADX). Analiza el cruce del índice direccional positivo (+DI) y el índice direccional negativo (-DI) para identificar posibles cambios de tendencia.

Cuando +DI cruza por encima de -DI, la estrategia considera una señal alcista y puede abrir posiciones largas mientras cierra opcionalmente posiciones cortas existentes. A la inversa, cuando +DI cruza por debajo de -DI, se trata como una señal bajista, lo que provoca entradas cortas y el cierre opcional de posiciones largas. Se admiten niveles opcionales de stop-loss y take-profit mediante gestión de riesgo incorporada.

## Indicador

La estrategia utiliza el indicador `AverageDirectionalIndex` de StockSharp. Solo se necesitan las líneas direccionales; el valor principal del ADX no se usa en la toma de decisiones.

## Parámetros

- `ADX Period` – longitud del cálculo del ADX. El valor predeterminado es `50`.
- `Candle Type` – marco temporal utilizado para la suscripción de velas. El valor predeterminado es `1 hora`.
- `Allow Buy Open` – habilitar la apertura de posiciones largas. El valor predeterminado es `true`.
- `Allow Sell Open` – habilitar la apertura de posiciones cortas. El valor predeterminado es `true`.
- `Allow Buy Close` – permitir cerrar posiciones largas en señal de venta. El valor predeterminado es `true`.
- `Allow Sell Close` – permitir cerrar posiciones cortas en señal de compra. El valor predeterminado es `true`.
- `Stop Loss` – distancia de stop-loss en unidades de precio absolutas. El valor predeterminado es `1000`.
- `Take Profit` – distancia de take-profit en unidades de precio absolutas. El valor predeterminado es `2000`.

## Lógica de trading

1. Suscribirse a velas del marco temporal seleccionado y calcular el indicador ADX.
2. Rastrear los valores anteriores de +DI y -DI para detectar cruces.
3. En un cruce alcista (+DI cruza por encima de -DI):
   - Cerrar posición corta si `Allow Sell Close` está habilitado.
   - Abrir posición larga si `Allow Buy Open` está habilitado.
4. En un cruce bajista (+DI cruza por debajo de -DI):
   - Cerrar posición larga si `Allow Buy Close` está habilitado.
   - Abrir posición corta si `Allow Sell Open` está habilitado.
5. Los niveles de stop-loss y take-profit se aplican mediante `StartProtection`.

## Notas

- Solo se procesan velas completadas (`CandleStates.Finished`).
- La estrategia se apoya en la gestión de riesgo incorporada de StockSharp para los niveles de stop.
- Las posiciones se cierran enviando una orden de mercado opuesta con el volumen actual.

Esta estrategia está destinada a fines educativos y puede requerir ajuste adicional antes de usarse en mercados reales.
