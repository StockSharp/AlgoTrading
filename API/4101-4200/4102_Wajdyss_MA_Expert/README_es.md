# Estrategia experta de Wajdyss MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
**Wajdyss MA Expert Strategy** es una versión de C# del MetaTrader 4 asesor experto "wajdyss MA expert v3". Compara dos promedios móviles configurados con períodos, modos de cálculo, turnos y precios aplicados independientes. Un cruce alcista del promedio rápido por encima del promedio lento abre una exposición larga, mientras que un cruce bajista abre una exposición corta. La conversión reproduce las reglas originales de administración de dinero, el cierre automático opcional de operaciones opuestas y los filtros de liquidación de fin de día/fin de semana.

## Lógica de trading
1. Suscríbase a las `CandleType` seleccionadas (velas de 15 minutos de forma predeterminada) y calcule los promedios móviles rápido y lento utilizando las configuraciones `MovingAverageMethod` y `PriceSource` elegidas para cada tramo.
2. Guarde los valores del indicador para velas terminadas. Evalúe una señal alcista cuando el promedio rápido (con su cambio configurado) está por encima del promedio lento en la última barra cerrada mientras que hace dos barras estaba por debajo. Evalúe una señal bajista con la condición inversa.
3. Aplicar un tiempo de reutilización entre nuevas entradas de la misma dirección. La estrategia debe esperar al menos una vela completa del período de tiempo suscrito después de la última operación de ese lado, reflejando la guardia de tiempo variable global de la versión MT4.
4. Cuando **AutoCloseOpposite** está habilitado, cancele las órdenes de trabajo y revierta la exposición en una sola orden de mercado: el nuevo volumen de la orden incluye cualquier posición pendiente en la dirección opuesta, por lo que la cuenta cambia inmediatamente.
5. Aplicar filtros de cierre diario y viernes. Después del `DailyCloseHour`/`DailyCloseMinute` o `FridayCloseHour`/`FridayCloseMinute` configurado, todas las posiciones se aplanan y las nuevas operaciones se bloquean hasta la siguiente sesión.

## Gestión de riesgos y dinero
- **TakeProfitPips**, **StopLossPips** y **TrailingStopPips** se interpretan en pips completos. La implementación los convierte en incrementos de precios utilizando los metadatos de seguridad e impulsa el motor `StartProtection` de StockSharp con salidas de mercado para lograr la paridad con la lógica de seguimiento original.
- **UseMoneyManagement** emula el cálculo del lote MT4: `volume = (account_balance / BalanceReference) * InitialVolume`. Los límites de intercambio se respetan mediante controles de volumen, mínimo y máximo.
- Si la administración del dinero está deshabilitada, los pedidos usan **InitialVolume** directamente.

## Parámetros
| Parámetro | Tipo | Predeterminado | Descripción |
|-----------|------|---------|-------------|
| `FastPeriod` | `int` | `10` | Período de la media móvil rápida. |
| `FastShift` | `int` | `0` | Barras para cambiar el promedio rápido antes de comparar valores cruzados. |
| `FastMethod` | `MovingAverageMethod` | `Ema` | Modo de media móvil para la línea rápida (`Sma`, `Ema`, `Smma`, `Lwma`). |
| `FastPriceType` | `PriceSource` | `Close` | El precio de la vela se introduce en la media móvil rápida (`Close`, `Open`, `High`, `Low`, `Median`, `Typical`, `Weighted`). |
| `SlowPeriod` | `int` | `20` | Período de la media móvil lenta. |
| `SlowShift` | `int` | `0` | Barras para desplazar el promedio lento antes de la comparación. |
| `SlowMethod` | `MovingAverageMethod` | `Ema` | Modo de media móvil para la línea lenta. |
| `SlowPriceType` | `PriceSource` | `Close` | El precio de las velas alimentó el promedio lento. |
| `TakeProfitPips` | `decimal` | `100` | Distancia al objetivo de ganancias en pips (establecido en `0` para desactivarlo). |
| `StopLossPips` | `decimal` | `50` | Distancia hasta la parada de protección en pips (establecida en `0` para desactivarla). |
| `TrailingStopPips` | `decimal` | `0` | Distancia del trailing stop en pips (establezca en `0` para desactivarlo). |
| `AutoCloseOpposite` | `bool` | `true` | Cierre la exposición opuesta antes de abrir una nueva operación en la otra dirección. |
| `InitialVolume` | `decimal` | `0.1` | Volumen comercial base antes de aplicar la gestión del dinero. |
| `UseMoneyManagement` | `bool` | `true` | Habilite el tamaño de posición basado en el equilibrio. |
| `BalanceReference` | `decimal` | `1000` | Divisor utilizado al escalar el volumen con el saldo de la cuenta. |
| `DailyCloseHour` | `int` | `23` | Hora (0-23) tras la cual se cierran las posiciones diarias. |
| `DailyCloseMinute` | `int` | `45` | Componente minucioso del filtro de cierre diario. |
| `FridayCloseHour` | `int` | `22` | Hora (0-23) después de la cual se detiene la negociación del viernes. |
| `FridayCloseMinute` | `int` | `45` | Componente de minutos del filtro de cierre del viernes. |
| `CandleType` | `DataType` | `15m` período de tiempo | Serie de velas utilizadas para cálculos y tiempos de enfriamiento. |

## Notas
- La estrategia se basa exclusivamente en el StockSharp API de alto nivel: las velas se procesan a través de `SubscribeCandles`, los enlaces de indicadores alimentan los promedios móviles y `StartProtection` gestiona órdenes de parada, toma de ganancias y seguimiento.
- El aplanamiento de posiciones utiliza órdenes de mercado para reflejar los cierres inmediatos de tickets opuestos por parte del experto en MT4.
- No se incluye ninguna traducción de Python en esta carpeta; solo se proporciona la implementación de C#.
