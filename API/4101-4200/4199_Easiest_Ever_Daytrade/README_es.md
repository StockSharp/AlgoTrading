# La estrategia de comercio diario más sencilla jamás creada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
- Conversión del MetaTrader 4 asesor experto **"El más fácil de todos: robot de comercio diario"** al API de alto nivel de StockSharp.
- Diseñado para operaciones intradía simples: cada sesión abre como máximo una posición de mercado que sigue la dirección de la vela diaria anterior.
- Utiliza únicamente datos de velas, sin indicadores técnicos ni osciladores. Toda la gestión de órdenes se realiza mediante órdenes de mercado.

## Lógica de trading
1. Recopile velas diarias (`DailyCandleType`, predeterminado `TimeSpan.FromDays(1)`) y almacene los precios de apertura y cierre del último día completo.
2. Suscríbase a velas intradía (`IntradayCandleType`, predeterminado `TimeSpan.FromMinutes(1)`) para impulsar la ejecución.
3. Durante las primeras horas de la sesión (mientras la hora de apertura de la vela es estrictamente inferior a `EntryHourLimit`, por defecto `1`):
   - Si el cierre diario anterior está por encima de la apertura diaria anterior, ingrese una posición larga usando `BuyMarket(TradeVolume)`.
   - Si el cierre diario anterior está por debajo de la apertura diaria anterior, ingrese una posición corta usando `SellMarket(TradeVolume)`.
   - Si la vela diaria cerró plana (apertura es igual a cierre), no se abre ninguna operación.
4. Mantenga la posición durante todo el día. Cuando la hora de la vela intradiaria sea mayor o igual a `MarketCloseHour` (predeterminado `20`), cierre cualquier exposición abierta con una orden de mercado (`SellMarket` para largos, `BuyMarket` para cortos).
5. La estrategia sólo abre una nueva posición cuando no existe ninguna posición activa, lo que garantiza una operación por día como máximo.

## Parámetros
| Parámetro | Descripción | Predeterminado |
|-----------|-------------|---------|
| `TradeVolume` | Volumen de pedidos tanto para entradas largas como cortas. Debe ser positivo. | `1` |
| `EntryHourLimit` | Última hora (exclusiva) en la que se puede iniciar una nueva operación. Los valores fuera de `[0, 23]` se fijan mediante validación. | `1` |
| `MarketCloseHour` | Hora en la que la estrategia cierra con fuerza cualquier posición abierta. Aplica diariamente. | `20` |
| `IntradayCandleType` | Marco de tiempo utilizado para la lógica de ejecución comercial y la gestión de posiciones. | `TimeSpan.FromMinutes(1).TimeFrame()` |
| `DailyCandleType` | Marco de tiempo utilizado para leer los precios de apertura y cierre del día anterior. | `TimeSpan.FromMinutes(5).TimeFrame()` |

Todos los parámetros se registran a través de `Param()` y se pueden optimizar en el optimizador StockSharp.

## Gestión del riesgo
- La estrategia no utiliza niveles de stop-loss o take-profit; El riesgo está controlado por la salida diaria en `MarketCloseHour`.
- `StartProtection()` está habilitado al inicio para protegerse contra posiciones inesperadas no planas durante la negociación.
- Debido a que solo puede haber una posición activa por día, la exposición máxima está definida por `TradeVolume`.

## Notas de uso
- Ejecute la estrategia en instrumentos que proporcionen historiales de velas tanto intradía como diarias. La configuración predeterminada requiere velas minuciosas y diarias.
- Alinee `EntryHourLimit` y `MarketCloseHour` con la sesión de negociación del instrumento seleccionado.
- El algoritmo espera la hora local del intercambio en las marcas de tiempo de las velas; ajustar las fuentes de datos en consecuencia.
- La lógica refleja el asesor experto MQL original, lo que permite replicar el comportamiento dentro del entorno StockSharp sin componentes de Python.
