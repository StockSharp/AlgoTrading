# Estrategia RSI EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia emula un asesor experto clásico basado en RSI. Opera cuando el Índice de Fuerza Relativa cruza niveles predefinidos y gestiona el riesgo con stop loss, take profit y trailing stop opcional.

## Lógica de la estrategia
- Calcula el RSI usando el parámetro `RsiPeriod` configurable.
- **Entrada larga** cuando el RSI sube por encima de `BuyLevel` y no existe posición larga.
- **Entrada corta** cuando el RSI cae por debajo de `SellLevel` y no existe posición corta.
- Cuando `CloseBySignal` está habilitado, un cruce opuesto cierra la posición existente.
- Las posiciones pueden protegerse con `StopLoss`, `TakeProfit` y `TrailingStop` medidos en unidades de precio.
- Funciona con datos de velas definidos por `CandleType`.

## Parámetros
- `OpenBuy` – habilitar entradas largas.
- `OpenSell` – habilitar entradas cortas.
- `CloseBySignal` – cerrar por señal RSI opuesta.
- `StopLoss` – pérdida en unidades de precio.
- `TakeProfit` – beneficio en unidades de precio.
- `TrailingStop` – distancia de trailing en unidades de precio.
- `RsiPeriod` – longitud del cálculo del RSI.
- `BuyLevel` – umbral RSI para señales largas.
- `SellLevel` – umbral RSI para señales cortas.
- `CandleType` – marco temporal o tipo de vela a suscribir.

El volumen de operación predeterminado se controla mediante la propiedad `Volume` de la estrategia.
