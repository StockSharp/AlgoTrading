# Estrategia de Cruce de Triple Media Móvil
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera basándose en la relación entre tres medias móviles: rápida, media y lenta. Es una conversión del experto MQL **X3MA_EA_V2_0**.

## Lógica de trading

* **Entrada**
  * Cuando *EnableEntryMediumSlowCross* es verdadero, se abre una posición larga cuando la media móvil media cruza por encima de la lenta. El cruce inverso activa una entrada corta.
  * Cuando la opción es falsa, la estrategia espera que la media rápida cruce la media mientras ambas permanezcan del mismo lado de la lenta. Las posiciones largas requieren `fast > medium > slow` y las cortas requieren `fast < medium < slow`.
* **Salida**
  * Cuando *EnableExitFastSlowCross* es verdadero, las posiciones abiertas se cierran cuando las medias rápida y lenta se cruzan en sentido contrario.

Todas las señales se evalúan en velas terminadas.

## Parámetros

| Nombre | Descripción |
|--------|-------------|
| `FastMaLength` | Período de la media móvil rápida. |
| `MediumMaLength` | Período de la media móvil media. |
| `SlowMaLength` | Período de la media móvil lenta. |
| `EnableEntryMediumSlowCross` | Permitir entradas en el cruce medio/lento. |
| `EnableExitFastSlowCross` | Cerrar posiciones en el cruce rápido/lento. |
| `CandleType` | Marco temporal de las velas. |

## Notas

La estrategia utiliza la API de alto nivel con `SubscribeCandles` y `Bind`. Los valores de los indicadores se acceden a través del callback `ProcessCandle` sin usar `GetValue`. La lógica de protección se activa con `StartProtection()` en `OnStarted`.
