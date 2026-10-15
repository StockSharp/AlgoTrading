# Estrategia de media móvil ajustable
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia recrea el asesor experto "Promedio móvil ajustable" de MetaTrader utilizando el API de alto nivel de StockSharp. Dos medias móviles del mismo tipo pero de diferentes longitudes monitorean su distancia. Cuando la curva más rápida cruza la más lenta por al menos una brecha configurable, la estrategia cierra cualquier posición opuesta y, opcionalmente, abre una operación en la nueva dirección. Los filtros de sesión adicionales, las salidas protectoras y un tope de seguimiento opcional brindan la misma flexibilidad operativa que el robot original.

## Lógica comercial

- Dos medias móviles (rápida y lenta) comparten el mismo método de cálculo. El período más rápido se configura automáticamente para la entrada más pequeña, el período más lento para la entrada más grande.
- Se produce una señal solo después de que ambos promedios móviles están completamente formados y su distancia absoluta excede el umbral `MinGapPoints` convertido en unidades de precio.
- Cuando la MA rápida está por encima de la MA lenta en la brecha requerida, el estado de la señal interna se vuelve alcista. Se registra un estado bajista cuando la MA lenta está por encima de la MA rápida.
- Un cambio de estado cierra cualquier posición existente si `CloseOutsideSession` está habilitado o la hora actual está dentro de la ventana de sesión. Los nuevos pedidos siguen el `Mode` seleccionado (solo comprar, solo vender o ambos) y usan un lote fijo o la regla de tamaño de lote automático.
- La lógica protectora se verifica en cada vela terminada:
  - Las distancias de parada de pérdidas y toma de ganancias se miden en puntos de instrumentos y se evalúan con respecto al rango de velas.
  - El trailing stop se activa una vez que el precio se mueve a favor de la posición en al menos `TrailStopPoints` puntos. La parada se ajusta solo cuando el filtro de sesión permite el seguimiento o `TrailOutsideSession` está habilitado. Una vez que la parada está establecida, permanece activa incluso fuera del horario comercial.

## Tamaño de posición

- Con `EnableAutoLot = false` la estrategia envía el volumen `FixedLot` (después de aplicar el paso del instrumento, los límites mínimo y máximo).
- Con `EnableAutoLot = true` el volumen se aproxima a partir del valor de la cartera disponible: `(PortfolioValue / 10,000) * LotPer10kFreeMargin`, redondeado a un lote decimal. El volumen calculado también está alineado con las restricciones cambiarias.

## Parámetros

| Nombre | Tipo / Predeterminado | Descripción |
| --- | --- | --- |
| `CandleType` | `TimeFrame` = velas de 5 minutos | Plazo utilizado para los cálculos de media móvil. |
| `FastPeriod` | `int` = 3 | Longitud media móvil corta. Debe diferir de `SlowPeriod`. |
| `SlowPeriod` | `int` = 9 | Longitud media móvil larga. Debe diferir de `FastPeriod`. |
| `MaMethod` | `MovingAverageMethod` = Exponencial | Algoritmo de media móvil (simple, exponencial, suavizado, ponderado). |
| `MinGapPoints` | `decimal` = 3 | Distancia mínima entre los promedios rápido y lento en puntos del instrumento. Convertido utilizando el paso del precio del instrumento. |
| `StopLossPoints` | `decimal` = 0 | Distancia de parada de protección en los puntos del instrumento. Establezca en cero para desactivar. |
| `TakeProfitPoints` | `decimal` = 0 | Distancia objetivo de beneficio en puntos del instrumento. Establezca en cero para desactivar. |
| `TrailStopPoints` | `decimal` = 0 | Distancia del trailing stop en puntos del instrumento. Establezca en cero para desactivar. |
| `Mode` | `EntryMode` = Ambos | Dirección permitida para nuevas operaciones (ambas, solo compra, solo venta). |
| `SessionStart` | `TimeSpan` = 00:00 | Hora de inicio de sesión (reloj de plataforma). |
| `SessionEnd` | `TimeSpan` = 23:59 | Hora de finalización de la sesión (reloj de plataforma). Admite sesiones nocturnas cuando `SessionEnd < SessionStart`. |
| `CloseOutsideSession` | `bool` = verdadero | Si es verdadero, las posiciones opuestas se cierran incluso fuera de la ventana de la sesión. |
| `TrailOutsideSession` | `bool` = verdadero | Si es verdadero, el trailing stop sigue actualizándose después de que se cierra la sesión. |
| `FixedLot` | `decimal` = 0,1 | Volumen utilizado cuando el tamaño automático está deshabilitado. |
| `EnableAutoLot` | `bool` = falso | Habilite la estimación de volumen a partir del valor de la cartera. |
| `LotPer10kFreeMargin` | `decimal` = 1 | Lotes asignados por 10.000 unidades de valor de cartera en modo de lote automático. |
| `MaxSlippage` | `int` = 3 | Retenido para completar; Las órdenes de mercado StockSharp no exponen un parámetro de deslizamiento directo. |
| `TradeComment` | `string` = "Promedio de movimiento ajustableEA" | Texto incluido en los mensajes de registro cuando se ejecutan operaciones. |

## Notas

- La versión original MetaTrader aplicaba stop loss, takeprofit y trailingstops mediante modificaciones de órdenes. El puerto StockSharp emula el comportamiento evaluando rangos de velas y enviando órdenes de mercado opuestas.
- El valor de la cartera se utiliza como una aproximación del margen libre porque el `AccountFreeMargin()` de MetaTrader no está disponible en StockSharp.
- Cuando el instrumento carece de un `PriceStep` válido, los cálculos basados en puntos (brecha, paradas, seguimiento) permanecen inactivos.
