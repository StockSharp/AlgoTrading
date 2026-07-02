# Estrategia de la sesión de Tokio
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La estrategia de la sesión de Tokio replica la lógica del MetaTrader asesor experto *TokyoSessionEA_v2.8* en StockSharp. el
La estrategia está diseñada para operaciones de ruptura intradía o de reversión a la media durante la sesión asiática (Tokio). Capta un
vela de referencia a una hora configurable, construye un canal de precios a partir de esa vela y evalúa la ruptura o el rebote
condiciones en otra hora objetivo. Dependiendo del modo de señal elegido, la estrategia puede operar en contra de la
ruptura de nivel (movimientos de desvanecimiento que se extienden más allá del rango de referencia) o a lo largo de la dirección de ruptura.

El puerto StockSharp se centra en el uso de alto nivel API. Todos los cálculos de señales se realizan dentro de la suscripción de velas.
controlador, las paradas se administran a través de `StartProtection` y cada acción se registra a través de `LogInfo` para mantener el comportamiento
transparente durante las pruebas retrospectivas y el comercio en vivo.

## Lógica de trading

1. **Vela de referencia**: a las `TimeSetLevels` (hora del corredor), la estrategia registra el máximo, el mínimo y el cierre de la vela. Estos
Los valores definen el canal de sesión y restablecen los indicadores de validación interna.
2. **Validación de canal** – cada vela terminada entre la hora de referencia y la hora de entrada puede invalidar el
señal pendiente dependiendo de la configuración:
   - `CheckAllBars`: si está habilitado, el cierre debe permanecer entre el máximo y el mínimo capturados.
   - `ReCheckPrices`: en `TimeRecheckPrices` el cierre de la vela se compara con el promedio móvil para confirmar el impulso.
3. **Evaluación de entrada** – cuando se cierra la vela que precede a `TimeCheckLevels`, la estrategia compara su precio de cierre
con los límites del canal. Si el cierre está dentro del rango de distancia configurado, se abre la posición correspondiente.
4. **Salidas** – las posiciones se pueden cerrar mediante tres mecanismos:
   - `CloseInSignal` cierra una operación una vez que el precio regresa dentro del canal (la lógica refleja la EA original).
   - `CloseOrdersOnTime` se aplana en `TimeCloseOrders` para evitar mantener el riesgo en la siguiente sesión.
   - Las paradas de protección, las paradas finales y el manejo del punto de equilibrio se delegan al subsistema de protección StockSharp.

## Parámetros

### generales

| Parámetro | Descripción |
|-----------|-------------|
| `CandleType` | Serie de velas utilizadas para el análisis (el valor predeterminado es H1). |
| `BrokerOffset` | Diferencia entre la hora del corredor y GMT en horas. |

### Señales

| Parámetro | Descripción |
|-----------|-------------|
| `TypeOfSignals` | `ContraryTrend` replica el desvanecimiento de la fuga; `AccordingTrend` sigue la dirección de ruptura. |
| `TimeSetLevels` | Hora (0–23) en la que se captura la vela de referencia. |
| `TimeCheckLevels` | Hora en la que se evalúan las condiciones de ruptura. |
| `TimeRecheckPrices` | Hora de verificación de impulso adicional. |
| `MinDistanceOfLevel` | Distancia mínima (en pips) entre el cierre y el canal antes de permitir una operación. |
| `MaxDistanceOfLevel` | Distancia máxima (en pips) desde el nivel. Cero desactiva el límite. |
| `ReCheckPrices` | Activa/desactiva el filtro de impulso adicional. |
| `CheckAllBars` | Requiere que todos los cierres intermedios permanezcan dentro del canal. |

### Gestión del riesgo

| Parámetro | Descripción |
|-----------|-------------|
| `CloseInSignal` | Salga una vez que el precio vuelva a cruzar el límite del canal. |
| `CloseOrdersOnTime` | Aplanar posiciones después de `TimeCloseOrders`. |
| `TimeCloseOrders` | Hora utilizada por la salida basada en tiempo. |
| `UseTakeProfit`, `TakeProfit` | Habilite y configure una toma de ganancias fija (pips). |
| `UseStopLoss`, `StopLoss` | Habilite y configure un stop-loss protector (pips). |
| `UseTrailingStop`, `TrailingStop`, `TrailingStep` | Habilite la gestión de trailing stop (pips) de StockSharp. |
| `UseBreakEven`, `BreakEven`, `BreakEvenAfter` | Mueva el stop-loss al punto de equilibrio una vez que las ganancias alcancen la distancia de activación. |

### Comercio

| Parámetro | Descripción |
|-----------|-------------|
| `Volume` | Volumen base de pedidos. Al girar la dirección, la posición opuesta se cierra automáticamente. |
| `MaxOrders` | Número máximo de `Volume` bloques permitidos en una dirección. Establezca en 0 para que no haya límite. |

## Flujo de trabajo

1. Implemente la estrategia en un instrumento con un paso de precio válido (`Security.PriceStep`).
2. Seleccione el período de tiempo deseado y configure las compensaciones horarias del corredor para alinear el horario diario con el intercambio.
3. Ajuste los filtros de distancia y validación para que coincidan con el comportamiento del EA original o para adaptarse a diferentes mercados.
4. Configurar parámetros de riesgo. El puerto StockSharp gestiona de forma nativa las paradas y la lógica de seguimiento a través de `StartProtection`.
5. Inicia la estrategia. Los mensajes de registro informarán los niveles capturados, las operaciones abiertas y las decisiones de salida.

## Diferencias con la versión MetaTrader

- Las entradas de punto flotante basadas en `UseFloatingPoint` y `PipsFloatingPoint` no se implementan porque StockSharp
ejecuta órdenes de mercado en el momento en que se genera la señal.
- Los filtros de diferencial y deslizamiento se omiten porque las suscripciones de velas de alto nivel no proporcionan datos de oferta/demanda a nivel de tick.
- La gestión automática del dinero (`AutoLotSize`, `RiskFactor`, lotes de recuperación, cambio de símbolo preestablecido) se reemplaza con la
parámetros `Volume` y `MaxOrders` más simples. El tamaño de la posición debe ajustarse directamente en la configuración de la estrategia.
- Las notificaciones sonoras e impresas se reemplazan por mensajes `LogInfo`.

Todas las demás condiciones de señal, puertas de validación y salidas basadas en tiempo reflejan el comportamiento del EA original.

## Notas

- La configuración predeterminada está alineada con el plazo H1 recomendado por el asesor experto original. Otros plazos
Se puede utilizar, pero la lógica basada en horas supone que la duración de las velas se divide en partes iguales.
- Asegúrese de que la fuente de datos proporcione velas continuas durante el período de tiempo seleccionado. La falta de velas puede invalidar el
comprobaciones de promedio y canal.
- Debido a que la estrategia cierra posiciones enviando órdenes de mercado, corredores que requieren órdenes limitadas o tenencia mínima
Los tiempos pueden necesitar adaptaciones adicionales.
