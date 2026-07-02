# Ichimoku Estrategia de acción del precio
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Ichimoku estrategia de acción de precio** es un sistema de impulso MACD filtrado por tiempo trasladado desde el experto MQL4 "Ichimoku estrategia de acción de precio v1.0" al API de alto nivel de StockSharp. El EA original abría órdenes de mercado siempre que se habilitaba la negociación para el instrumento y el filtro opcional MACD confirmaba la dirección. Este port de C# mantiene la misma idea al tiempo que proporciona controles de riesgo detallados para la colocación de límites de pérdidas, manejo del punto de equilibrio y salidas finales.

La estrategia está diseñada para operadores discrecionales que desean automatizar un juego direccional en el momento del día con dependencias mínimas de los indicadores. Todas las señales comerciales se evalúan en velas completadas del período comercial elegido, al tiempo que admiten marcos temporales auxiliares para paradas protectoras basadas en ATR y oscilaciones.

> **Importante:** La versión StockSharp mantiene como máximo una posición neta a la vez. No se admite la exposición larga/corta simultánea de estilo de cobertura de la plantilla original porque StockSharp `Strategy` opera en posiciones netas. Todas las demás características de administración de dinero se expresan mediante lógica de parada, objetivo y seguimiento ejecutada en cada vela terminada.

## Lógica de trading
1. **Filtro de sesión**: se permiten entradas solo cuando la hora actual del día está dentro de la ventana `[StartTime; EndTime]`. Establecer ambos parámetros en `00:00` deshabilita el filtro de sesión.
2. **MACD confirmación (opcional)**: cuando `UseMacdFilter = true`, las posiciones largas requieren MACD línea principal por encima de la línea de señal, las posiciones cortas requieren lo contrario. Las configuraciones de MACD son completamente configurables.
3. **Colocación de órdenes**: si el comercio está habilitado para una dirección y no hay ninguna posición abierta, la estrategia envía una orden de mercado con el `Volume` configurado.
4. **Paradas de protección**: dependiendo de `StopLossMode`, la parada inicial se coloca usando una distancia de pip fija, un múltiplo ATR o el último swing extremo recopilado de un período de tiempo inferior. El stop se recalcula en cada vela y se ajusta cuando el nivel recién calculado es más conservador.
5. **Objetivos**: en cada vela se comprueba un objetivo de pip fijo o un objetivo dinámico de riesgo/recompensa basado en el stop activo. Una vez alcanzada, la posición se cierra en el mercado.
6. **Equipo y seguimiento**: cuando el beneficio no realizado alcanza `MoveToBreakEven`, el tope se sitúa en el precio de entrada. Después de `TrailingTrigger` pips de ganancia, el módulo de seguimiento se activa y sigue presionando el stop cada vez que el precio mejora en `TrailingStep` pips mientras se mantiene una distancia de `TrailingStop` pips desde el cierre de la vela.
7. **Salida inversa**: si es `CloseOnReverse = true`, cualquier señal de entrada opuesta cierra inmediatamente la posición actual antes de potencialmente girar en la nueva dirección.

## Gestión del riesgo
- **Detener pérdidas**
  - *Pips fijos*: utiliza `StopLossPips` multiplicado por el paso del precio del instrumento.
  - *multiplicador ATR*: utiliza el último valor de ATR de `AtrCandleType` multiplicado por `AtrMultiplier`.
  - *Swing alto/bajo*: utiliza el extremo de swing más reciente calculado por `SwingCandleType` con `SwingBars` retrospectiva.
- **Obtener ganancias**
  - *Pips fijos* – Utiliza `TakeProfitPips`.
  - *Riesgo/Recompensa*: utiliza la distancia de parada actual multiplicada por `TakeProfitRatio`.
- **Equipo**: `MoveToBreakEven` define cuántos pips rentables se requieren antes de que se bloquee el stop en el precio de entrada.
- **Trailing**: controlado por `TrailingStop`, `TrailingTrigger` y `TrailingStep` para mantener las ganancias una vez que el mercado se mueva favorablemente.

## Parámetros
| grupo | Nombre | Descripción |
| --- | --- | --- |
| generales | `BuyMode` | Permitir entradas largas. |
| generales | `SellMode` | Permitir entradas breves. |
| generales | `CandleType` | Plazo de negociación (predeterminado 1 hora). |
| Horario | `StartTime` / `EndTime` | Ventana de sesión en horario de intercambio (00:00 → deshabilitado). |
| Filtros | `UseMacdFilter` | Habilite la confirmación MACD. |
| Filtros | `MacdFast`, `MacdSlow`, `MacdSignal` | MACD períodos para EMA rápido, EMA lento y señal EMA. |
| Riesgo | `StopLossMode` | Cálculo de stop-loss: `FixedPips`, `AtrMultiplier`, `SwingHighLow`. |
| Riesgo | `StopLossPips` | Distancia en pips cuando se selecciona el modo fijo. |
| Riesgo | `AtrMultiplier`, `AtrPeriod`, `AtrCandleType` | Configuración de parada basada en ATR. |
| Riesgo | `SwingBars`, `SwingCandleType` | Configuración de parada alta/baja de giro. |
| Riesgo | `TakeProfitMode` | Modo de destino: `FixedPips` o `RiskReward`. |
| Riesgo | `TakeProfitPips`, `TakeProfitRatio` | Distancias objetivo. |
| Riesgo | `CloseOnReverse` | Cerrar la posición activa cuando aparezca la señal contraria. |
| Órdenes | `Volume` | Volumen de órdenes de mercado (lotes/contratos). |
| Riesgo | `MoveToBreakEven` | Umbral de beneficio (en pips) para mover el stop a la entrada. |
| Riesgo | `TrailingStop`, `TrailingTrigger`, `TrailingStep` | Configuración del trailing stop en pips. |

## Notas de uso
- Asegúrese de que el instrumento tenga `PriceStep` definido; de lo contrario, la estrategia asume un tamaño de pip de `0.0001`.
- Cuando se habilitan ATR o paradas de swing, las suscripciones auxiliares correspondientes se agregan automáticamente. Asegúrese de que la fuente de datos proporcione esos períodos de tiempo.
- Si necesita deshabilitar el comportamiento de equilibrio o de seguimiento, establezca los parámetros correspondientes en `0`.
- La estrategia es neutral por defecto en la apertura de la sesión. No apilará varias posiciones en la misma dirección; las reentradas ocurren solo después de que se cierra la operación anterior.

## Limitaciones en comparación con la versión MQL
- Solo se admiten posiciones netas (limitación StockSharp). No se reproducen operaciones simultáneas largas y cortas al estilo de cobertura.
- Los modos de gestión del dinero, como el dimensionamiento de Kelly o la obtención parcial de beneficios, no forman parte de este puerto.
- Las funciones de confirmación manual, gráficos del panel y captura de pantalla de la plantilla MQL se omiten intencionalmente.

## Lista de verificación de pruebas retrospectivas
1. Configure los `CandleType` deseados y los plazos auxiliares.
2. Ajuste los parámetros `Volume` y parada/objetivo para que coincidan con la configuración original EA.
3. Habilite o deshabilite la confirmación MACD según el uso de la plantilla.
4. Ejecute la simulación asegurándose de que la ventana de la sesión comercial coincida con sus pruebas originales.
5. Revise los mensajes de registro generados para confirmar que los eventos de detención y destino ocurren según lo esperado.
