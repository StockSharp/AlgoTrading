# Estrategia de robot KA-Gold
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **Estrategia KA-Gold Bot** es una conversión de alto nivel StockSharp del asesor experto original MetaTrader 4 "KA-Gold Bot". Combina un canal estilo Keltner con filtros de tendencias y una gestión de riesgos agresiva que incluye stop-loss fijo, take-profit y protección de seguimiento de varias etapas. Solo se permite operar durante una ventana intradiaria configurable y las nuevas posiciones se bloquean cuando el diferencial en vivo excede un umbral.

## Lógica de trading

1. **Preparación de indicadores**
   - Una media móvil exponencial (EMA) con longitud `KeltnerPeriod` construye la línea media del canal.
   - Una media móvil simple de rangos de velas (máximo menos mínimo) con el mismo período estima la mitad del ancho del canal.
   - Los promedios móviles exponenciales a corto y largo plazo (`EmaShortPeriod` y `EmaLongPeriod`) rastrean el impulso rápido y la tendencia de marco temporal más alto, respectivamente.
   - Todos los valores de los indicadores se registran para las dos velas completadas más recientes para reflejar los cálculos basados en cambios de MT4.

2. **Condiciones de entrada**
   - Los cálculos se ejecutan sólo cuando se cierra la vela actual y la estrategia está conectada al mercado con los permisos comerciales otorgados.
   - Las bandas superior e inferior del canal se obtienen sumando/restando el rango promedio de la línea media EMA tanto para la vela anterior (`shift = 1`) como para la anterior (`shift = 2`).
   - **Configuración larga:**
     - El cierre anterior rompe por encima de la banda superior más reciente.
     - El mismo cierre está por encima del EMA largo, lo que confirma una tendencia alcista.
     - El EMA corto cruza desde debajo de la banda superior más antigua hasta encima de la más reciente (`EMA_short[2] < Upper[2]` y `EMA_short[1] > Upper[1]`).
   - **Configuración breve:**
     - El cierre anterior cae por debajo de la banda inferior reciente.
     - El mismo cierre está por debajo del largo EMA, lo que confirma una tendencia bajista.
     - El EMA corto cruza desde arriba de la banda inferior más antigua hasta debajo de la más reciente (`EMA_short[2] > Lower[2]` y `EMA_short[1] < Lower[1]`).
   - Sólo se permite una posición a la vez. Si ya hay una operación abierta, la señal se ignora.

3. **Filtros de tiempo y difusión**
   - Cuando `UseTimeFilter` está habilitado, las nuevas entradas están restringidas a la ventana `[StartHour:StartMinute, EndHour:EndMinute)` usando la hora local del intercambio. Se admiten sesiones nocturnas si la hora de finalización es anterior a la hora de inicio.
   - Las suscripciones a cotizaciones de nivel 1 realizan un seguimiento de los mejores precios de oferta y demanda. Antes de realizar una orden, la estrategia convierte el diferencial actual en puntos de instrumento y lo compara con `MaxSpreadPoints`. Los pedidos se omiten y se registran cada vez que se supera el umbral.

4. **Gestión de riesgos**
   - El tamaño de posición por defecto es `FixedVolume`. Si `UseRiskPercent` es `true`, el tamaño de la operación se recalcula a partir del valor de la cartera como `RiskPercent% / (riskPips * PipValue)`, donde `riskPips` es igual a `StopLossPips` (recurre a `TrailingStopPips` cuando no se define ningún tope fijo). El resultado final se normaliza al paso de volumen del instrumento y se fija entre los límites de intercambio mínimo y máximo.
   - Cuando se abre una posición larga, la estrategia almacena:
     - Stop-loss inicial en `entry - StopLossPips * pipSize` (si está definido).
     - Toma de ganancias inicial en `entry + TakeProfitPips * pipSize` (si está definido).
     - Banderas de estado finales, que reinician los rastreadores del lado corto.
   - Las operaciones cortas reflejan la misma lógica con direcciones de precios invertidas.

5. **Protección de seguimiento**
   - Las actualizaciones de oferta y demanda en vivo alimentan dos motores de seguimiento:
     - Una vez que el beneficio flotante supera `TrailingTriggerPips`, el seguimiento se activa.
     - El trailing stop se posiciona `TrailingStopPips` lejos del precio favorable actual y solo avanza cuando el movimiento excede `TrailingStopPips + TrailingStepPips` más allá del nivel de stop anterior.
     - Para posiciones largas, el trailing stop nunca cae por debajo del stop protector original, y para posiciones cortas nunca sube por encima de él.
   - El seguimiento de las salidas se realiza tanto en las cotizaciones entrantes como en las velas terminadas:
     - Una posición se cierra inmediatamente cuando el precio alcanza el tope activo (original o final).
     - Las ganancias también se bloquean una vez que el máximo/mínimo de la vela toca el nivel de obtención de beneficios almacenado.
   - Después de cerrar una posición, el estado de protección se restablece por completo para evitar datos obsoletos.

## Parámetros

| Parámetro | Descripción | Predeterminado |
|-----------|-------------|---------|
| `CandleType` | Tipo de datos que describe el plazo de ejecución. | marco de tiempo de 1 minuto |
| `KeltnerPeriod` | Período para la línea media EMA y el rango promedio del canal. | 50 |
| `EmaShortPeriod` | Longitud rápida EMA utilizada para la confirmación de cruce. | 10 |
| `EmaLongPeriod` | Longitud lenta de EMA que actúa como filtro de tendencias. | 200 |
| `FixedVolume` | Volumen de pedido alternativo cuando el tamaño porcentual está deshabilitado. | 1 |
| `UseRiskPercent` | Habilite el tamaño de posición basado en porcentaje. | `true` |
| `RiskPercent` | Porcentaje de capital arriesgado por operación. | 1 |
| `StopLossPips` | Distancia del stop-loss fijo en pips (0 inhabilitaciones). | 500 |
| `TakeProfitPips` | Distancia de la toma de ganancias fija en pips (0 inhabilitaciones). | 500 |
| `TrailingTriggerPips` | Beneficio en pips necesarios para activar el trailing stop. | 300 |
| `TrailingStopPips` | Distancia entre el precio y el trailing stop una vez activo. | 300 |
| `TrailingStepPips` | Beneficio adicional mínimo (en pips) antes de que se avance el trailing stop. | 100 |
| `UseTimeFilter` | Alternar para el filtro de sesión de negociación. | `true` |
| `StartHour` / `StartMinute` | Inicio de sesión en hora local de intercambio. | 02:30 |
| `EndHour` / `EndMinute` | La sesión finaliza en hora local de intercambio. | 21:00 |
| `MaxSpreadPoints` | Spread máximo permitido en puntos del instrumento (0 desactiva la verificación). | 65 |
| `PipValue` | Valor monetario de un pip, utilizado para dimensionar posiciones basadas en riesgos. | 1 |

## Notas adicionales

- La conversión de pips sigue los decimales del instrumento de intercambio: una cotización de cinco dígitos (número impar de decimales) multiplica el paso del precio por 10 para emular la lógica del tamaño de pip MT4.
- La estrategia se suscribe tanto a velas como a datos de nivel 1, pero **no** registra indicadores adicionales en el gráfico, cumpliendo con las pautas de alto nivel API.
- Las salidas protectoras dependen de las órdenes de mercado emitidas por la estrategia; no se colocan órdenes stop o límite separadas en el intercambio.
- El soporte de Python no está incluido en esta entrega, lo que coincide con la solicitud original.
