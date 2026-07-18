# FiveMinutesScalpingEA v1.1 (puerto StockSharp)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
**FiveMinutesScalpingEaV11Strategy** es una conversión del MetaTrader 4 asesor experto *5MinutesScalpingEA v1.1*. La estrategia mantiene el concepto original de combinar promedios móviles de Hull de períodos múltiples, una transformada de impulso de Fisher, un detector de ruptura ATR y un filtro de tendencia para analizar movimientos de corta duración en un gráfico de cinco minutos. La implementación sigue el StockSharp nivel alto API y utiliza suscripciones de velas con enlaces de indicadores para reproducir el comportamiento del asesor experto.

La estrategia está diseñada para el comercio con un solo símbolo. Sólo se mantiene una posición neta en cualquier momento y todas las señales se evalúan en velas completadas. Las órdenes de protección se simulan dentro de la estrategia monitoreando los máximos y mínimos de las velas.

## Pila de indicadores
| Componente | StockSharp implementación | Propósito |
|-----------|--------------------------|---------|
| `i1` casco personalizado MA | `HullMovingAverage` con punto `Period1` (predeterminado 30) | Detecta la dirección rápida de la tendencia a través de la pendiente de la media móvil de Hull. |
| `i2` casco personalizado MA | `HullMovingAverage` con punto `Period2` (predeterminado 50) | Confirma una dirección de tendencia más amplia; Ambos filtros de casco deben coincidir para entradas en modo normal. |
| `i3` Impulso de Fisher | `FisherTransform` with period `Period3` | Actúa como un oscilador de impulso. Los valores positivos favorecen las configuraciones largas, los valores negativos favorecen las configuraciones cortas. |
| `i4` ATR flechas de ruptura | `AverageTrueRange` con período `Period4` combinado con comparaciones de velas | Busca rupturas fuertes donde el máximo/mínimo actual supera los dos máximos/mínimos anteriores en al menos un ATR. |
| `i5` Filtro de tendencias de Fisher | `FisherTransform` with period `Period5` | Proporciona una confirmación de tendencia suavizada similar al histograma de tendencia EA original. |

Para cada indicador, la estrategia almacena valores históricos para poder leer el valor `IndicatorShift` velas, coincidiendo con el parámetro MQL4 `IndicatorsShift`. Todos los filtros se pueden desactivar individualmente a través de sus respectivos parámetros.

## Lógica comercial
1. La estrategia se suscribe a la serie de velas definida por `CandleType` (predeterminado: velas de 5 minutos).
2. Con cada vela terminada se actualizan los indicadores Hull, Fisher y ATR. Cuando hay suficiente historial disponible, la estrategia evalúa la vela que está `IndicatorShift` barras hacia atrás.
3. **Modo normal** (`SignalMode = Normal`):
   - Una entrada **larga** requiere que todos los filtros habilitados informen condiciones alcistas (pendiente de Hull positiva, impulso de Fisher por encima de cero, ruptura de ATR hacia arriba, tendencia de Fisher por encima de cero).
   - Una entrada **corta** requiere que todos los filtros habilitados informen condiciones bajistas (pendiente de Hull negativa, impulso de Fisher por debajo de cero, ruptura de ATR hacia abajo, tendencia de Fisher por debajo de cero).
4. **Modo inverso** (`SignalMode = Reverse`) simplemente intercambia la interpretación de las condiciones alcistas y bajistas.
5. Una nueva señal activa la bandera interna `_lastSignal`. Si `CloseOnSignal` está habilitado, la posición opuesta se cierra inmediatamente antes de que se envíe una nueva entrada.
6. El parámetro `UseTimeFilter` restringe las entradas al rango `[StartHour, EndHour)` (con un comportamiento envolvente idéntico al de MQL4 EA).

## Gestión de riesgos
El puerto StockSharp implementa las siguientes características de protección:
- **Detener pérdidas/toma de ganancias**: si está habilitado, los precios objetivo y de parada se colocan a una distancia fija (`StopLossPips`, `TakeProfitPips`) del precio de entrada y se monitorean en cada vela.
- **Parada de seguimiento**: cuando `UseTrailingStop` está habilitado, se mantiene un ancla de seguimiento. Una vez que el precio avanza `TrailingStepPips`, el stop se mueve para que permanezca `TrailingStopPips` alejado del extremo actual.
- **Equipo**: si `UseBreakEven` está habilitado y el precio se mueve en `BreakEvenPips + BreakEvenAfterPips`, el stop se ajusta a `BreakEvenPips` de distancia de la entrada.
- **Posición única**: todas las salidas se ejecutan mediante órdenes de mercado (`SellMarket` / `BuyMarket`) que cierran toda la posición neta.

## Parámetros
| Nombre | Predeterminado | Descripción |
|------|---------|-------------|
| `CandleType` | M5 | Plazo primario. |
| `IndicatorShift` | 1 | Número de velas cerradas para mirar hacia atrás al evaluar filtros. |
| `SignalMode` | normales | Utilice señales normales o invertidas. |
| `UseIndicator1`..`UseIndicator5` | cierto | Alterna cada filtro. |
| `Period1`, `Period2`, `Period3`, `Period4`, `Period5` | 30, 50, 10, 14, 18 | Períodos para cálculos de Hull, Fisher y ATR. |
| `PriceMode3` | AltoBajo | Parámetro de compatibilidad para la selección original de Fisher Price. La implementación StockSharp siempre envía el precio de vela predeterminado al indicador Fisher. |
| `CloseOnSignal` | falso | Cierre la posición opuesta cuando aparezca una nueva señal de entrada. |
| `UseTimeFilter`, `StartHour`, `EndHour` | falso, 0, 0 | Ventana de negociación intradía opcional. |
| `UseTakeProfit`, `TakeProfitPips` | cierto, 10 | Tome la gestión de beneficios. |
| `UseStopLoss`, `StopLossPips` | cierto, 10 | Gestión de pérdidas. |
| `UseTrailingStop`, `TrailingStopPips`, `TrailingStepPips` | falso, 1, 1 | Gestión de trailing stop. |
| `UseBreakEven`, `BreakEvenPips`, `BreakEvenAfterPips` | falso, 4, 2 | Lógica de parada de equilibrio. |
| `TradeVolume` | 0,01 | Volumen de entradas al mercado. |

## Diferencias vs. original EA
- La lógica de cierre de cesta (`UseBasketClose`, `CloseInProfit`, `CloseInLoss`) no está implementada porque la estrategia StockSharp funciona con una única posición neta.
- El tamaño de lote automático (`AutoLotSize` / `RiskFactor`) y las comprobaciones de distribución no forman parte de esta portabilidad. Utilice el entorno de alojamiento para controlar el volumen y el deslizamiento.
- El parámetro del modo de precio de Fisher está expuesto por motivos de compatibilidad, pero StockSharp `FisherTransform` actualmente utiliza el precio de vela predeterminado. Se pueden emular otros modos de precios ampliando el indicador si es necesario.
- La gestión comercial se realiza en velas completadas, lo que refleja el comportamiento de EA cuando `IndicatorsShift >= 1`.

## Consejos de uso
1. Adjunte la estrategia a un instrumento líquido con diferenciales ajustados (el EA se diseñó originalmente para EUR/USD M5).
2. Configure `TradeVolume` según las reglas de tamaño de su cuenta.
3. Ajuste los períodos de los indicadores o desactive los filtros para que coincidan con su tolerancia al riesgo.
4. Combínelo con el filtro de tiempo incorporado para evitar sesiones de baja liquidez.
5. Valide siempre la configuración en el probador StockSharp antes de ejecutar datos en vivo.
