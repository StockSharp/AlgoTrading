# Estrategia X Trader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia implementa un sistema contrarian de cruce de medias móviles escrito originalmente en MQL como **X trader**.
Utiliza dos medias móviles simples y abre posiciones en dirección opuesta al cruce. El riesgo se gestiona
usando take-profit y stop-loss fijos en puntos absolutos mediante `StartProtection`.

## Cómo funciona

1. Suscribirse a datos de velas del marco temporal especificado.
2. Calcular dos medias móviles con períodos configurables.
3. Rastrear los últimos dos valores de cada media para detectar un cruce.
4. Cuando la media rápida cruza por encima de la media lenta y permanece por encima durante dos barras mientras dos barras atrás estaba por debajo,
   se abre una posición **corta**.
5. Cuando la media rápida cruza por debajo de la media lenta y permanece por debajo durante dos barras mientras dos barras atrás estaba por encima,
   se abre una posición **larga**.
6. Solo puede estar abierta una posición al mismo tiempo. La protección cierra automáticamente las operaciones cuando el precio se mueve
   la cantidad configurada de take-profit o stop-loss.

## Parámetros

- `CandleType` – serie de velas a utilizar.
- `Ma1Period` – período de la primera media móvil.
- `Ma2Period` – período de la segunda media móvil.
- `TakeProfitPoints` – objetivo de beneficio en puntos de precio.
- `StopLossPoints` – límite de pérdida en puntos de precio.

## Indicador

- `SimpleMovingAverage` – usado dos veces con períodos diferentes.

## Gestión de riesgos

`StartProtection` se habilita en `OnStarted` y aplica los valores de take-profit y stop-loss a todas las posiciones.
