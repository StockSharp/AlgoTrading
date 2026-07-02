# Estrategia de doble canal EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

El **Doble Canal EA** replica la lógica comercial del MetaTrader 4 asesor experto "DoubleChannelEA_v1.2". El StockSharpp
ort adapta el indicador personalizado *iDoubleChannel_v1.5* y ejecuta operaciones de ruptura cuando el indicador imprime flechas. la estrategia
y está diseñado para pruebas discrecionales con gestión de riesgos configurable y filtros de programación.

Características clave:

- Custom `DoubleChannelIndicator` reconstruye los buffers de los canales superior, inferior y medio más las señales de flecha de compra/venta.
- Uso de API de alto nivel con suscripciones de velas, validación de spread de nivel uno y asistentes de pedidos nativos.
- Herramientas opcionales de administración de dinero: apilamiento de posiciones, punto de equilibrio, trailing stop, take-profit y stop-loss.
- Entradas de bloque de filtro de hora del día y filtro de dispersión fuera de las condiciones operativas definidas por el usuario.

## Lógica de trading

1. Suscríbete al `CandleType` seleccionado y alimenta cada vela terminada en el `DoubleChannelIndicator`.
2. El indicador almacena una ventana móvil de `ChannelPeriod` velas y calcula:
   - Línea media: media aritmética de cierres.
   - Línea superior: media más la diferencia de dos sobres de precios derivados de máximos y mínimos.
   - Línea inferior: media más la diferencia de envolventes complementarias derivadas de aperturas y mínimos.
   - Señales de flecha: las dos posiciones anteriores del canal deben invertirse y la vela anterior debe cerrarse en la dirección de la ruptura.
kout. Las reglas coinciden con las condiciones del búfer MT4.
3. Las señales se pueden retrasar en `IndicatorShift` barras para reproducir el parámetro de cambio del indicador.
4. Una señal de compra abre una posición larga (se permite acumular cuando `OpenEverySignal = true`). Una señal de venta abre una posición corta. Op.
Las posiciones positivas se pueden cerrar inmediatamente cuando `CloseInSignal = true`.
5. Las salidas protectoras gestionan la posición activa en cada vela terminada:
   - Distancias estáticas de stop-loss/take-profit expresadas en unidades de precio absoluto.
   - Activación del punto de equilibrio una vez que el precio avanza `BreakEvenPoints + BreakEvenAfterPoints`.
   - Trailing stop que requiere una mejora de `TrailingStepPoints` antes de actualizar.
6. Las inscripciones se rechazan cuando:
   - La estrategia está fuera del horario comercial (`UseTimeFilter`).
   - El spread en vivo supera `MaxSpreadPoints`.
   - `MaxOrders` posiciones apiladas ya están abiertas para la dirección actual.

## Gestión monetaria

El volumen del pedido se calcula como:

```
volumen = ManualLotSize * (AutoLotSize? max(RiskFactor, 0.1): 1)
```

Al revertir, la estrategia incluye automáticamente la posición opuesta absoluta para girar a la nueva dirección en un solo mar.
orden del mercado.

## Parámetros

| Parámetro | Predeterminado | Descripción |
|-----------|---------|-------------|
| `CandleType` | plazo de 15 minutos | Suscripción de vela primaria. |
| `ChannelPeriod` | 14 | Búsqueda retrospectiva del canal personalizado. |
| `IndicatorShift` | 0 | Retraso antes de actuar sobre los valores de los indicadores. |
| `OpenEverySignal` | cierto | Permite apilar posiciones sobre señales consecutivas. |
| `CloseInSignal` | falso | Cierra la posición actual cuando aparece una flecha opuesta. |
| `UseTakeProfit` | falso | Habilita `TakeProfitPoints`. |
| `TakeProfitPoints` | 10 | Distancia de precio absoluta para el objetivo. |
| `UseStopLoss` | falso | Habilita `StopLossPoints`. |
| `StopLossPoints` | 10 | Distancia de precio absoluta para el tope de protección. |
| `UseTrailingStop` | falso | Habilita la lógica final con `TrailingStopPoints` y `TrailingStepPoints`. |
| `TrailingStopPoints` | 5 | Distancia desde el precio actual hasta el trailing stop. |
| `TrailingStepPoints` | 1 | Mejora mínima necesaria antes de actualizar el trailing stop. |
| `UseBreakEven` | falso | Permite ajustes de equilibrio. |
| `BreakEvenPoints` | 4 | Nivel de parada objetivo una vez que se activa el punto de equilibrio. |
| `BreakEvenAfterPoints` | 2 | Se requiere ganancia adicional antes de activar el punto de equilibrio. |
| `AutoLotSize` | cierto | Multiplica el lote manual por `RiskFactor`. |
| `RiskFactor` | 1 | Multiplicador de riesgo aplicado al dimensionar automáticamente. |
| `ManualLotSize` | 0,01 | Volumen base cuando el tamaño automático está deshabilitado. |
| `UseTimeFilter` | falso | Habilita el filtro de programación. |
| `TimeStartTrade` | 0 | Hora de inicio de negociación (inclusive). |
| `TimeEndTrade` | 0 | Hora de fin de negociación (exclusivo). Igual comienzo y final significa que no hay restricción. |
| `MaxOrders` | 0 | Posiciones apiladas máximas por dirección (0 = ilimitado). |
| `MaxSpreadPoints` | 0 | Spread máximo permitido entre oferta y demanda en unidades de precio. |

## Notas sobre la conversión

- El indicador original representaba flechas desplazando los valores una barra hacia adelante. La versión StockSharp almacena instantáneas anteriores y
comprueba los mismos criterios de cruce antes de emitir una señal en la vela actual.
- El filtrado de propagación se basa en datos de nivel uno. Cuando las cotizaciones no están disponibles, la estrategia bloquea nuevos pedidos, imitando la experiencia MQL.
rt que se negó a comerciar sin información difundida.
- La gestión del dinero en MT4 utilizó cálculos basados en cuentas. Para la portabilidad, la fórmula del volumen se simplificó a un multiplicador de riesgo.
er aplicado al tamaño de lote manual.
- Las distancias stop-loss, take-profit, trailing stop y punto de equilibrio se interpretan en unidades de precio absoluto (la misma convención que un
y otras StockSharp conversiones). Ajústelos según el tamaño del tick del instrumento.
