# Estrategia Adaptive Renko
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia construye una cuadrícula Renko adaptativa donde el tamaño del ladrillo sigue la volatilidad del mercado medida por el indicador **Average True Range (ATR)**. Se ejecuta una operación cada vez que el precio recorre un ladrillo completo en cualquier dirección.

## Lógica
- El ATR se calcula sobre un `VolatilityPeriod` configurable.
- El tamaño del ladrillo es igual a `ATR * Multiplier` pero no puede ser menor que `MinBrickSize`.
- Cuando el precio sube por encima del ladrillo anterior al menos un tamaño de ladrillo, la estrategia compra (cerrando posiciones cortas si es necesario).
- Cuando el precio cae por debajo del ladrillo anterior al menos un tamaño de ladrillo, la estrategia vende (cerrando posiciones largas si es necesario).

## Parámetros
- `Volume` – volumen de la orden.
- `VolatilityPeriod` – período utilizado para el ATR.
- `Multiplier` – coeficiente aplicado al ATR.
- `MinBrickSize` – tamaño mínimo permitido del ladrillo en unidades de precio.
- `CandleType` – marco temporal para el cálculo del ATR.

## Marco temporal
- Predeterminado: 4 horas.
