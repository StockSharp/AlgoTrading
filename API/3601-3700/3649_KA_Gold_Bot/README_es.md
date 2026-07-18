# Estrategia de robot KA-Gold
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **Estrategia KA-Gold Bot** es una adaptación directa del asesor experto MetaTrader "KA-Gold Bot". Intercambia rupturas de un canal personalizado estilo Keltner y alinea señales con filtros de tendencia a mediano plazo. El puerto se basa en StockSharp suscripciones de velas de alto nivel, enlaces de indicadores y parámetros de estrategia para que el comportamiento siga siendo configurable desde la interfaz de usuario y listo para la optimización.

## Lógica de trading

- Calcule tres medias móviles exponenciales (EMA):
  - EMA(10) para una confirmación rápida del impulso.
  - EMA(200) para detectar la tendencia de período de tiempo más alto.
  - EMA(punto) como centro del canal; se utiliza la misma longitud para promediar el rango de la vela (alto-bajo).
- Promedie el rango diario con una media móvil simple para formar envolventes dinámicas:
  - Banda superior = EMA(período) + SMA(alto-bajo, punto).
  - Banda inferior = EMA(período) − SMA(alto-bajo, punto).
- Una configuración **larga** requiere todo lo siguiente en la última vela cerrada:
  - Precio de cierre por encima de la banda superior.
  - Precio de cierre superior a EMA(200).
  - EMA(10) cruzó desde debajo de la banda superior anterior hasta encima de la última banda superior.
- Una configuración **corta** refleja las reglas:
  - Precio de cierre por debajo de la banda inferior.
  - Precio de cierre por debajo de EMA(200).
  - EMA(10) cruzó desde arriba de la banda inferior anterior hasta debajo de la última banda inferior.
- Sólo puede haber una posición abierta a la vez; Las señales opuestas se ignoran hasta que la estrategia sea plana.

## Dimensionamiento de posiciones

Se admiten dos modelos de volumen:

1. **Modo de lote fijo**: utilice el parámetro `BaseVolume` directamente.
2. **Modo de porcentaje de riesgo**: cuando `UseRiskPercent = true`, el proxy de capital gratuito (`Portfolio.CurrentValue` o `Portfolio.BeginValue`) se multiplica por `RiskPercent`. El resultado se escala en 100.000 (MetaTrader convención de lote) y se redondea a múltiplos de `BaseVolume`, respetando `Security.MinVolume`, `Security.MaxVolume` y `Security.VolumeStep`.

## Gestión del riesgo

- Las compensaciones de stop-loss y take-profit se definen en pips. Los pips se convierten a distancias de precios absolutas mediante el paso de seguridad. Los símbolos de divisas de tres y cinco decimales reutilizan la regla MetaTrader `pip = step × 10`.
- Las órdenes de protección iniciales se registran inmediatamente después del primer cumplimiento y se mantienen sincronizadas con el tamaño de la posición actual.
- Los trailingstops se activan una vez que las ganancias no realizadas alcanzan `TrailingTriggerPips`:
  - Las posiciones largas mantienen el stop `TrailingStopPips` alejado del cierre.
  - Las posiciones cortas utilizan la distancia simétrica por encima del mercado.
  - El tope se mueve solo si la distancia mejora al menos `TrailingStepPips` para evitar una activación excesiva.
- Cuando se cierra la posición, las órdenes de protección pendientes se cancelan automáticamente.

## Filtros de sesión y difusión

- Ventana de negociación opcional controlada por `UseTimeFilter`, `StartHour`, `StartMinute`, `EndHour` y `EndMinute` (ventana inclusiva-exclusiva). Se admiten períodos nocturnos (finalizan antes de que el inicio finalice después de la medianoche).
- Un filtro de diferencial opcional rechaza nuevas entradas si el diferencial actual (diferencia entre la mejor oferta y la mejor oferta en incrementos de precios) excede `MaxSpreadPoints`.

## Notas de implementación

- Las velas se procesan a través de `SubscribeCandles().Bind(...)`; los valores EMA(10) y EMA(200) llegan a través del enlace, mientras que el canal EMA y el promedio del rango se actualizan dentro del controlador sin usar `GetValue`.
- El estado del indicador se almacena únicamente a través de campos escalares que reflejan la lógica de cambio MetaTrader `iClose` y `CopyBuffer`, preservando el requisito de comparar las dos últimas barras cerradas.
- La lógica de seguimiento y protección utiliza asistentes de pedidos de alto nivel (`BuyStop`, `SellStop`, `BuyLimit`, `SellLimit`) para reflejar las llamadas `PositionModify` de MetaTrader.
- El tamaño basado en la cartera depende de la información de capital disponible en StockSharp; si falta, la estrategia vuelve al volumen fijo.

## Parámetros

| Parámetro | Descripción | Predeterminado |
|-----------|-------------|---------|
| `KeltnerPeriod` | Periodo para el canal EMA y suavizado de rango. | 50 |
| `FastEmaPeriod` | Longitud del filtro EMA rápida. | 10 |
| `SlowEmaPeriod` | Longitud del filtro de tendencia EMA lenta. | 200 |
| `BaseVolume` | Volumen mínimo de pedido (tamaño de lote). | 0,01 |
| `UseRiskPercent` | Habilite el tamaño de posición basado en el equilibrio. | cierto |
| `RiskPercent` | Porcentaje de capital utilizado por operación cuando el tamaño del riesgo está activo. | 1 |
| `StopLossPips` | Distancia de stop-loss en pips. | 500 |
| `TakeProfitPips` | Distancia de toma de ganancias en pips (0 inhabilitaciones). | 500 |
| `TrailingTriggerPips` | Umbral de beneficio para armar el trailing stop. | 300 |
| `TrailingStopPips` | Distancia mantenida por el trailing stop una vez armado. | 300 |
| `TrailingStepPips` | Mejora mínima antes de que se mueva la parada. | 100 |
| `UseTimeFilter` | Alternar el filtro de la sesión de negociación. | cierto |
| `StartHour`, `StartMinute` | Hora de inicio de la sesión. | 02:30 |
| `EndHour`, `EndMinute` | Hora de finalización de la sesión (exclusiva). | 21:00 |
| `MaxSpreadPoints` | Spread máximo permitido en pasos de precio (0 = deshabilitado). | 65 |
| `CandleType` | Marco de tiempo utilizado para las velas de señal. | velas de 5 minutos |

## Diferencias en comparación con la versión MetaTrader

- La implementación del trailing-stop recrea la secuencia `PositionModify` usando órdenes de stop StockSharp; La funcionalidad es equivalente pero se basa en pedidos confirmados por el intercambio.
- MetaTrader calculó el ancho del canal a partir del rango medio alto-bajo; el puerto reproduce el mismo promedio con un promedio móvil simple para mantener los desgloses idénticos.
- La dimensionamiento del riesgo accede al capital de la cartera en lugar del margen libre. Esta aproximación coincide con la intención (porcentaje de capital), pero puede diferir si no se dispone de datos de margen específicos del apalancamiento.
- Los cheques extendidos utilizan `Security.BestAskPrice` y `Security.BestBidPrice`. Cuando la profundidad no está disponible, se omite el filtro, reflejando la opción de "extensión flotante" en el experto original.

## Consejos de uso

- Adjunte la estrategia a instrumentos donde la definición de pip sigue las convenciones de Forex (3 o 5 decimales) para mantener los parámetros de riesgo alineados con los del experto original.
- Optimice los períodos EMA y la duración del canal para instrumentos que no sean de oro porque la estrategia de origen se ajustó para XAUUSD.
- Supervise la ventana de la cartera para asegurarse de que los valores de las acciones se completen cuando `UseRiskPercent` esté habilitado.
