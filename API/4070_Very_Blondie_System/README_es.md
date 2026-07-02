# Sistema muy rubio
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Very Blondie System es una estrategia de cuadrícula de reversión a la media a corto plazo distribuida originalmente como MetaTrader 4 asesor experto "VBS - Very Blondie System". El puerto mantiene la idea original de desvanecer una ruptura del rango de negociación reciente: cuando el precio se aleja lo suficiente del máximo más alto o del mínimo más bajo visto en las últimas `PeriodX` velas, la estrategia ingresa inmediatamente con una orden de mercado y agrega cuatro órdenes límite estilo martingala para escalar el movimiento si el precio continúa extendiéndose.

## Datos e indicadores
- **Datos primarios**: una única serie de velas configurada por el parámetro `CandleType` (la versión MQL se negocia en el marco temporal del gráfico).
- **Indicadores**: los indicadores `Highest` y `Lowest` (longitud = `PeriodLength`) rastrean los extremos del rango de balanceo utilizados para la detección de fugas.
- **Cotizaciones de nivel 1**: los mejores precios de oferta y demanda se consumen para realizar órdenes de mercado y limitadas en las compensaciones MT4 originales.

## Lógica de entrada
1. En cada vela terminada, calcule el máximo más alto y el mínimo más bajo de las últimas `PeriodLength` barras.
2. Lea la mejor oferta/demanda actual (recurra al cierre de la vela si faltan cotizaciones).
3. **Configuración larga**: si es `highest - bid > LimitPoints * PointValue`, envíe una orden de compra de mercado con el volumen base y coloque cuatro órdenes de límite de compra por debajo de la demanda. Cada orden límite se encuentra `GridPoints * PointValue` más lejos y duplica el volumen de la orden anterior (1×, 2×, 4×, 8×, 16×).
4. **Configuración corta**: si es `bid - lowest > LimitPoints * PointValue`, envíe una orden de venta de mercado y cuatro órdenes de límite de venta por encima de la oferta a las mismas distancias y multiplicadores de volumen que la lógica de compra.
5. Sólo puede haber una cesta activa a la vez. Las nuevas señales se ignoran hasta que desaparezcan todas las posiciones y órdenes pendientes del ciclo anterior.

## Gestión de Puestos
- **Objetivo de ganancias flotantes**: el parámetro `Amount` original monitoreaba `OrderProfit + OrderSwap` en todas las operaciones. El puerto reproduce esto con la posición agregada: `(close - entryPrice) * position * conversionFactor >= ProfitTarget`. Cuando se alcanza el umbral, cada posición se cierra con órdenes de mercado y todas las órdenes restantes de la red se cancelan.
- **Equipo de equilibrio de bloqueo**: cuando `LockDownPoints > 0`, el código MT4 movió el límite de pérdidas de cada orden ejecutada a `entry price ± Point` una vez que la operación tuvo `LockDownPoints` puntos de ganancia. La versión StockSharp rastrea la posición neta; tan pronto como el precio avanza en `LockDownPoints * PointValue`, el nivel de equilibrio se arma en `entryPrice ± PointValue`. Si una vela posterior toca ese nivel (mínimo para largos, máximo para cortos), toda la cesta se aplana y todas las órdenes pendientes se cancelan.
- **Salidas manuales**: detener la estrategia o alcanzar las condiciones de beneficio/equilibrio siempre cancela las cuatro órdenes límite pendientes para imitar la rutina `CloseAll()` de MT4.

## Gestión monetaria
- **Volumen base**: coincide con la expresión MT4 `MathRound(AccountBalance()/100) / 1000`. La estrategia lee el valor actual de la cartera (o el valor inicial cuando no se realizaron transacciones), lo redondea desde cero y lo convierte en lotes. El resultado está alineado con `Security.VolumeStep`, obedece a `MinVolume`/`MaxVolume` y recurre a la estrategia `Volume` (o `1`) cuando la instantánea de la cartera no está disponible.
- **Martingale cuadrícula**: cada orden límite adicional duplica el volumen base hasta cuatro niveles (1×, 2×, 4×, 8×, 16×). Los volúmenes se normalizan con el mismo ayudante para evitar enviar lotes fraccionados que el recinto rechaza.
- **Parámetro PointValue**: `Point` de MT4 puede diferir de `Security.PriceStep` (especialmente en cotizaciones FX de 5 dígitos). `PointValue` utiliza de forma predeterminada la detección automática de `PriceStep`/`Step`, pero puede anularla para que coincida con el comportamiento original de EA con precisión.

## Parámetros
| Nombre | Descripción | Predeterminado |
| ---- | ----------- | ------- |
| `PeriodLength` | Ventana retrospectiva para el máximo más alto y el mínimo más bajo | `60` |
| `LimitPoints` | Distancia mínima (en puntos MT4) entre el precio actual y el extremo del rango para activar una cesta | `1000` |
| `GridPoints` | Espaciado (en puntos MT4) entre órdenes de cuadrícula consecutivas | `1500` |
| `ProfitTarget` | Objetivo de beneficio flotante expresado en la moneda de la cuenta | `40` |
| `LockDownPoints` | Distancia de beneficio (en puntos MT4) que arma la salida del punto de equilibrio | `0` |
| `PointValue` | Cambio de precio producido por un punto MT4 (`0` = detección automática) | `0` |
| `CandleType` | Serie de velas utilizadas para impulsar la estrategia. | `TimeFrameCandle, 1 minute` |

## Notas de portabilidad
- El PnL flotante se aproxima con la posición agregada en lugar de sumar el `OrderProfit + OrderSwap` de cada orden. Esto coincide con el comportamiento original cuando todas las operaciones están en la misma dirección, que es como opera EA.
- La modificación del límite de pérdidas se emula mediante una salida inmediata del mercado al precio de equilibrio armado; StockSharp mantiene la lógica en la capa de estrategia en lugar de enviar solicitudes `OrderModify`.
- Las órdenes límite pendientes se registran con precios normalizados usando `Security.ShrinkPrice`. Cuando los metadatos de seguridad carezcan de `PriceStep`, configure `PointValue` manualmente para evitar cuadrículas desalineadas.
- La estrategia asume un instrumento y utiliza API ayudantes de alto nivel (`SubscribeCandles`, `SubscribeLevel1`, `BuyLimit`, `SellLimit`, etc.) como se solicita en las pautas de conversión.
