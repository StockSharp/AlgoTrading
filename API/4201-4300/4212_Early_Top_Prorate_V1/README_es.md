# Estrategia temprana de prorrateo superior V1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este documento describe el puerto StockSharp del asesor experto MetaTrader **earlyTopProrate_V1**. La estrategia busca movimientos intradiarios que se alejen de la apertura diaria y escale la posición utilizando tres objetivos de ganancias. Se convirtió utilizando el nivel alto StockSharp API conservando las ideas originales de administración de dinero y gestión comercial.

## Lógica principal

1. **Contexto diario**: la estrategia reconstruye la apertura, el máximo y el mínimo del día actual a partir de las velas procesadas. La dirección dominante se define comparando `high - open` y `open - low`.
2. **Ventana de entrada**: solo se pueden abrir nuevas operaciones entre `StartHour` (inclusive) y `EndHour` (exclusivo). La configuración predeterminada opera a principios de la sesión europea.
3. **Condiciones de entrada** –
   - Cuando la dirección dominante es alcista y el último precio de cierre está por encima de la apertura diaria, la estrategia abre una posición larga.
   - Cuando la dirección dominante es bajista y el último precio de cierre está por debajo de la apertura diaria, la estrategia abre una posición corta.
   - Solo se permite una posición de mercado al mismo tiempo (`MaxPositions = 1` de forma predeterminada).
4. **Gestión de dinero**: el volumen de cada entrada se obtiene del modo de gestión de dinero seleccionado (ver más abajo). El valor se redondea utilizando el paso de volumen del instrumento y se fija entre el volumen mínimo y máximo de intercambio.
5. **Manejo de posiciones**: después de ingresar a una posición, la estrategia aplica las reglas de salida en capas que se enumeran en la siguiente sección. Las reglas reflejan el asesor experto original, pero se implementan con órdenes StockSharp de alto nivel en lugar de modificaciones directas de stop-loss/take-profit.
6. **Cierre de sesión**: si una posición permanece abierta cuando se alcanza `ClosingHour`, la estrategia fuerza una salida del mercado.

## Detalles de gestión comercial

El asesor experto original MQL se basa en ajustes manuales de parada y toma de ganancias. El puerto StockSharp reproduce el comportamiento con comprobaciones explícitas en cada vela terminada:

- **Rescate del punto de equilibrio** (`BreakEvenTrigger`): si el precio se mueve contra la entrada en la cantidad de puntos configurada, la estrategia espera una recuperación hasta el precio de entrada y luego sale en el punto de equilibrio.
- **Parada de emergencia** (`StopLoss0`): cuando la excursión adversa excede esta distancia, la posición se cierra inmediatamente.
- **Parada de entrada** (`StopLoss1`): después de un movimiento positivo de la distancia especificada, la parada de protección se mueve al precio de entrada.
- **Detener el beneficio** (`StopLoss2`): una vez que el beneficio alcanza este umbral, el stop de protección se empuja por encima (largo) o por debajo (corto) de la entrada. El desplazamiento es igual a `StopLoss2 - StopLoss1`, reproduciendo la lógica `setSL2-35` de MetaTrader.
- **Escalamiento horizontal** (`TakeProfit1/2/3` y `Ratio1/2/3`): tres objetivos de ganancias desencadenan cierres parciales del volumen restante de la posición. Los ratios representan porcentajes de la posición actual para que los objetivos posteriores trabajen en la exposición reducida. El tercer objetivo cierra todo el resto.

Todos los parámetros basados en la distancia operan en *puntos*. El parámetro auxiliar `PointMultiplier` multiplica el instrumento `PriceStep` para reproducir la aritmética `value * 10 * Point` del script original (multiplicador predeterminado = 10).

## Modos de administración de dinero

El parámetro `MoneyManagementType` selecciona uno de los cuatro modelos de tallas:

| Modo | Descripción |
| --- | --- |
| `0` or `1` | Tamaño de lote fijo igual a `BaseVolume` (refleja el comportamiento de MQL donde los modos 0 y 1 son idénticos). |
| `2` | Modelo de raíz cuadrada: utiliza `0.1 * sqrt(balance / 1000) * MoneyManagementFactor`. El valor actual de la cartera se utiliza cuando está disponible. |
| `3` | Modelo de riesgo de acciones: calcula `equity / price / 1000 * MoneyManagementRiskPercent / 100`, aproximando la fórmula `AccountEquity/Close[0]` de MetaTrader. |

Cada resultado se normaliza utilizando el paso de volumen del instrumento y el volumen mínimo/máximo de intercambio.

## Parámetros

| Nombre | Descripción |
| --- | --- |
| `CandleType` | Serie de velas utilizadas para tomar decisiones. El valor predeterminado es velas de 5 minutos. |
| `StartHour` / `EndHour` | Ventana de negociación en horas (0-23). |
| `ClosingHour` | Hora en la que se cierra cualquier posición abierta. |
| `TimeZoneShift` | Se mantiene el desplazamiento informativo de la zona horaria por motivos de compatibilidad. |
| `BaseVolume` | Tamaño del lote base antes de los ajustes de administración del dinero. |
| `MaxPositions` | Posiciones simultáneas máximas (por defecto = 1). |
| `TakeProfit1`, `TakeProfit2`, `TakeProfit3` | Distancias, en puntos, de los tres objetivos de beneficio. |
| `BreakEvenTrigger` | Pérdida, en puntos, que activa la salida de rescate del punto de equilibrio. |
| `StopLoss0`, `StopLoss1`, `StopLoss2` | Umbrales adversos/rentables que controlan la lógica de parada de protección. |
| `Ratio1`, `Ratio2`, `Ratio3` | Porcentajes de la posición actual cerrada en cada objetivo. |
| `MoneyManagementType` | Modo de gestión de dinero (0–3). |
| `MoneyManagementFactor` | Multiplicador para el modelo de raíz cuadrada. |
| `MoneyManagementRiskPercent` | Porcentaje de riesgo para el modelo de renta variable. |
| `PointMultiplier` | Multiplicador aplicado al paso del precio del instrumento al convertir puntos en compensaciones de precios reales. |

## Notas de uso

- Elija un tipo de vela que coincida con la granularidad de los datos disponibles en el lugar seleccionado. La serie predeterminada de 5 minutos proporciona un equilibrio entre capacidad de respuesta y filtrado de ruido.
- Al convertir distancias basadas en puntos a precios reales, la estrategia multiplica `PriceStep * PointMultiplier`. Ajuste el multiplicador si el corredor define los puntos de manera diferente al entorno original MetaTrader.
- La lógica de equilibrio y seguimiento requiere velas terminadas, por lo tanto, el comportamiento intrabar puede diferir ligeramente de la ejecución MetaTrader basada en ticks. El README resalta esta aproximación para que pueda tenerse en cuenta durante las pruebas.
- `TimeZoneShift` se conserva para documentación. Los propios horarios de negociación deben configurarse usando `StartHour`, `EndHour` y `ClosingHour`.

## Empezando

1. Agregue la estrategia a su proyecto StockSharp o ejecútela dentro de Designer/Runner.
2. Configure la serie de velas (`CandleType`) y el horario de negociación para el instrumento que desea negociar.
3. Ajuste los umbrales y ratios basados en puntos según la volatilidad del instrumento.
4. Seleccione un modo de administración de dinero y configure los parámetros correspondientes (`BaseVolume`, `MoneyManagementFactor`, `MoneyManagementRiskPercent`).
5. Ejecute primero la estrategia en el comercio en papel para validar que el comportamiento coincida con sus expectativas antes de usarla con capital real.
