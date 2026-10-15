# Estrategia de marco de entrada STP diaria (StockSharp)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de marco de entrada STP diario** replica el comportamiento del asesor experto original MetaTrader "Marco de entrada STP diario" utilizando el StockSharp nivel alto API. El sistema prepara órdenes stop de ruptura al comienzo de cada nuevo día de negociación. Los precios de entrada se derivan de los máximos y mínimos del día anterior, con filtros adicionales para garantizar que el mercado se posicione cerca de estos extremos antes de armar las órdenes. La lógica está diseñada para instrumentos de estilo Forex donde los "puntos base" corresponden a una décima parte de un pip para cotizaciones de cinco dígitos.

## Flujo de trabajo principal
1. **Seguimiento de rango diario**: la estrategia se suscribe a velas diarias para recordar los máximos y mínimos de la sesión anterior.
2. **Monitoreo en tiempo real**: los datos de Level1 proporcionan los precios de oferta y demanda actuales y de la última operación para la gestión intradiaria.
3. **Activación de órdenes**: al comienzo de un nuevo día, si el último precio se ubica al menos a `ThresholdPoints` del máximo/mínimo de ayer y la apertura del día actual está en el lado correcto de ese extremo, se envía una orden de detención:
   - Parada de compra en `High + SpreadPoints / 2` (convertida a unidades de precio).
   - Parada de venta en `Low - SpreadPoints / 2`.
4. **Validación de riesgos**: las nuevas órdenes se bloquean cada vez que la reducción del capital excede `MaximumDrawdownPercent` o los filtros de tiempo no permiten la negociación (fines de semana, filtro de hora o filtro de día).
5. **Gestión de posiciones**: una vez que una operación está activa, la estrategia aplica:
   - Distancias estáticas de stop-loss y take-profit.
   - Salida opcional basada en tiempo después de `CloseAfterSeconds`.
   - Trailing stop opcional que emula el parámetro original "Pendiente SL".
6. **Higiene de final de día**: los pedidos pendientes se cancelan después del `NoNewOrdersHour` (o el límite exclusivo del viernes) y siempre que cambie el día calendario.

## Reglas de trading
- **Entradas largas**
  - Permitido cuando `SideFilter` es `0` (ambos) o `1` (solo largo).
  - Máximo del día anterior menos precio actual ≥ `ThresholdPoints`.
  - El precio de apertura de hoy está por debajo del máximo de ayer.
  - El precio de entrada calculado debe respetar la distancia mínima con respecto al precio de venta actual.
- **Entradas cortas**
  - Permitido cuando `SideFilter` es `0` (ambos) o `-1` (solo corto).
  - Precio actual menos el mínimo del día anterior ≥ `ThresholdPoints`.
  - El precio de apertura de hoy está por encima del mínimo de ayer.
  - El precio de entrada calculado debe respetar la distancia mínima de la oferta actual.
- **Gestión del dinero**
  - El tamaño del volumen dinámico utiliza un porcentaje de las ganancias acumuladas (`PercentOfProfit`).
  - El tamaño resultante está limitado por `MinVolume` y `MaxVolume` y alineado con el `VolumeStep` del instrumento.
  - El comercio se detiene automáticamente una vez que la reducción medida supera `MaximumDrawdownPercent`.
- **Lógica de protección**
  - Los niveles de stop-loss y take-profit se expresan en puntos base y se convierten en compensaciones de precios utilizando el tamaño del pip del instrumento.
  - El trailing stop está activo sólo cuando `TrailingSlope < 1`. Acerca el umbral de protección al precio a medida que aumentan las ganancias no realizadas.
  - Las salidas de por vida cierran cualquier posición abierta una vez transcurrido el número de segundos configurado.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `CandleType` | Marco de tiempo utilizado para recuperar las velas de referencia (diariamente por defecto). |
| `StopLossPoints` | Distancia de stop-loss en puntos base. |
| `TakeProfitPoints` | Distancia de toma de ganancias en puntos base. |
| `TrailingSlope` | Parte de las ganancias retenida durante el seguimiento; ≥ 1 desactiva la función. |
| `SideFilter` | -1 solo corto, 0 en ambas direcciones, 1 solo largo. |
| `ThresholdPoints` | Brecha mínima entre el precio actual y el extremo anterior requerida para armar un stop. |
| `SpreadPoints` | Compensación adicional (la mitad se utiliza por encima o por debajo del extremo) para compensar el diferencial. |
| `SlippagePoints` | Se agregó un amortiguador de seguridad al control de la distancia mínima de parada. |
| `NoNewOrdersHour` | Hora (tiempo de plataforma) para cancelar órdenes pendientes en días regulares. |
| `NoNewOrdersHourFriday` | Hora de cancelación específica del viernes. |
| `EarliestOrderHour` | Primera hora del día en la que se pueden crear nuevos pedidos. |
| `DayFilter` | 6 para todos los días o 0-5 para operar de domingo a viernes únicamente. |
| `CloseAfterSeconds` | Salida opcional basada en tiempo (0 inhabilitaciones). |
| `PercentOfProfit` | Fracción del beneficio acumulado utilizada para escalar el tamaño de la posición. |
| `MinVolume` / `MaxVolume` | Límites estrictos para el volumen calculado. |
| `MaximumDrawdownPercent` | Umbral de reducción que bloquea nuevas órdenes. |

## Notas de conversión
- La conversión de pips refleja la implementación de MetaTrader: si el valor expone 3 o 5 decimales, el punto base se convierte en `PriceStep * 10`.
- La ventana de cancelación de la orden de suspensión reproduce la limpieza nocturna del experto, incluido el corte separado del viernes.
- La lógica final sigue la fórmula de pendiente original (`newStop = Bid - StopLoss - Slope * (Bid - Entry)` para largos).
- Las notificaciones de acciones de la versión MQL se reemplazan con mensajes de registro de estrategias.
- La implementación StockSharp mantiene activas las órdenes pendientes incluso cuando una posición está abierta, coincidiendo con el comportamiento de origen.

## Consejos de uso
- Asigne un instrumento Forex con valores `PriceStep`, `StepPrice` y `VolumeStep` configurados correctamente para garantizar un tamaño preciso.
- Combine la estrategia con StockSharp controles de riesgo (límites de cartera, protecciones a nivel de conector) cuando se ejecute en vivo.
- Optimice `ThresholdPoints`, `TrailingSlope` y `PercentOfProfit` usando Designer o Runner para adaptar la sensibilidad de ruptura a símbolos específicos.
