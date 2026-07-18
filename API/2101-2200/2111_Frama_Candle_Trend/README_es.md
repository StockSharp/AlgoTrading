# Estrategia FrAMA Candle Trend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia convierte el asesor experto MetaTrader *Exp_FrAMACandle* en una estrategia de alto nivel de StockSharp.

## Lógica de la estrategia

- Utiliza la **Media Móvil Adaptativa Fractal (FrAMA)** calculada por separado para los precios de apertura y cierre de las velas.
- Una señal alcista ocurre cuando la FrAMA del precio de cierre sube por encima de la FrAMA del precio de apertura. Si la barra anterior no fue alcista, la estrategia abre una posición larga y cierra los cortos existentes.
- Una señal bajista ocurre cuando la FrAMA del precio de cierre cae por debajo de la FrAMA del precio de apertura. Si la barra anterior no fue bajista, la estrategia abre una posición corta y cierra los largos existentes.
- Las señales se evalúan solo en velas completadas. Los valores históricos de color se almacenan para respetar el desplazamiento `SignalBar`.

## Parámetros

| Nombre | Descripción |
| --- | --- |
| `CandleType` | Marco temporal utilizado para el cálculo del indicador. Predeterminado: 4 horas. |
| `FramaPeriod` | Período del indicador FrAMA. |
| `SignalBar` | Desplazamiento de la barra utilizada para la detección de señales. |
| `BuyOpen` / `SellOpen` | Habilitar apertura de posiciones largas/cortas. |
| `BuyClose` / `SellClose` | Habilitar cierre de posiciones largas/cortas. |

## Notas

- La estrategia se basa únicamente en cruces de FrAMA y no implementa gestión de stop-loss o take-profit.
- El volumen de la posición está controlado por la propiedad base `Volume` de la estrategia.
