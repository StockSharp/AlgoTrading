# 4026 – Estrategia de Pivotes
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Esta estrategia transfiere los MetaTrader 4 archivos ubicados en `MQL/8550` (el indicador **Pivotes** y el asesor experto `Pivots_test` que lo acompaña) al `Strategy` API de alto nivel de StockSharp. Mantiene el comportamiento original de calcular los niveles diarios de pivote de piso, organizar un par de órdenes pendientes opuestas en el pivote central y administrar cada posición resultante con un stop-loss fijo, una toma de ganancias y un stop dinámico.

## Cálculo de pivote

1. La estrategia se suscribe a un *plazo de pivote* configurable (`PivotCandleType`, diario de forma predeterminada).
2. Cada vez que finaliza una vela de ese período de tiempo, deriva los niveles clásicos de pivote de piso de los precios OHLC del día anterior:
   - `Pivot = (High + Low + Close) / 3`
   - `R1 = 2 × Pivot − Low`
   - `S1 = 2 × Pivot − High`
   - `R2 = Pivot + (High − Low)` y `S2 = Pivot − (High − Low)`
   - `R3 = 2 × Pivot + High − 2 × Low` y `S3 = 2 × Pivot − (2 × High − Low)`
3. Los niveles se activan al comienzo de la siguiente sesión. Cuando esto sucede, la estrategia registra los valores a través de `AddInfoLog` (por ejemplo: `Pivot levels for 2024-04-05: P=1.0924, R1=1.0956, …`).

## Flujo de trabajo de pedidos pendientes

Una vez que los niveles de pivote están activos, la estrategia garantiza continuamente que existan dos órdenes pendientes al precio de pivote:

- **Límite de compra** @ `Pivot` con protección posterior al llenado `SellStop` (stop-loss) en `S2` y `SellLimit` (take-profit) en `R2`.
- **Sell Stop** @ `Pivot` con protección posterior al llenado `BuyStop` en `R2` y `BuyLimit` en `S2`.

Todos los pedidos se envían a través de los métodos auxiliares de alto nivel `BuyLimit`, `SellStop`, `SellLimit` y `BuyStop`. Si se ejecuta una orden, el código recalcula el precio de entrada promedio para esa dirección, cancela las órdenes de protección existentes y envía un nuevo par stop/límite que cubre todo el volumen abierto (reflejando el comportamiento MetaTrader donde cada posición hereda la misma protección S2/R2). Si se ejecuta la parada de protección o la toma de ganancias, los ayudantes relacionados se borran automáticamente.

La estrategia utiliza una única posición neta, por lo que los rellenos opuestos se compensarán entre sí (a diferencia de la cobertura basada en tickets de MetaTrader). Ésta es la única desviación intencionada del experto original.

## Lógica de parada dinámica

- `TrailingStopPoints` define la distancia en puntos indicadores (multiplicada por el instrumento `PriceStep`).
- Para posiciones largas, el trailing stop se activa una vez que el precio se ha movido más de esa distancia por encima de la entrada promedio. Luego, el `SellStop` protector se acerca al mercado.
- Para posiciones cortas se aplica la lógica espejo, bajando el `BuyStop` a medida que el precio se mueve favorablemente.
- Las actualizaciones finales son impulsadas por la serie intradiaria seleccionada hasta `CandleType` (velas de 15 minutos de forma predeterminada).

## Parámetros

| Parámetro | Descripción | Predeterminado |
| --- | --- | --- |
| `OrderVolume` | Volumen de cada orden pendiente (lotes/contratos). | `0.1` |
| `TrailingStopPoints` | Distancia del trailing stop en puntos. `0` desactiva la lógica de seguimiento. | `30` |
| `CandleType` | Serie de velas intradiarias utilizadas para seguir y mantener el cronograma de la sesión. | `15m` período de tiempo |
| `PivotCandleType` | Marco de tiempo utilizado para calcular los niveles de pivote diarios. | `1D` período de tiempo |
| `LogPivotUpdates` | Cuando `true`, los niveles de pivote se escriben en el registro de estrategia cada vez que cambian. | `true` |

Todos los parámetros numéricos están expuestos a través de `StrategyParam<T>` para que puedan optimizarse dentro de la infraestructura StockSharp.

## Registro y diagnóstico

- Las actualizaciones dinámicas se enrutan a través de `AddInfoLog`, que reemplaza la salida MetaTrader `Comment`/`ObjectSetText`.
- La gestión de órdenes de protección, el manejo de posiciones y la lógica de seguimiento dependen únicamente de los ayudantes de alto nivel de StockSharp; no se utilizan registros de órdenes de bajo nivel ni buffers de indicadores.

## Notas de uso

1. Adjunte la estrategia a un conector que proporcione velas tanto diarias como intradiarias para el valor elegido.
2. Ajuste el paso del instrumento si es necesario (`PriceStep` se detecta automáticamente; el respaldo es `0.0001`).
3. Opcionalmente, ajuste `OrderVolume`, `TrailingStopPoints` o los tipos de velas para que coincidan con la configuración MT4 original.

No se proporciona ninguna versión de Python para este puerto según lo solicitado.
