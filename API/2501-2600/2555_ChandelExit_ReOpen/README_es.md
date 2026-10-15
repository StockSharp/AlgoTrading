# Estrategia ChandelExit de Reentrada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia porta el experto de MetaTrader "Exp_ChandelExitSign_ReOpen" a la API de alto nivel de StockSharp. Opera rompimientos usando las bandas de Chandelier Exit y reabre posiciones automáticamente cuando la tendencia continúa. El sistema reacciona a señales de indicadores calculadas en un marco temporal superior configurable mientras gestiona el riesgo con stops basados en ATR y niveles opcionales de take-profit.

La idea central es tratar el Chandelier Exit tanto como filtro de tendencia como barrera de trailing dinámico. Cuando la banda inferior cruza por encima de la superior, se detecta un impulso alcista; cuando ocurre lo contrario, aparece un impulso bajista. La estrategia puede funcionar simétricamente en lados largos y cortos, y cada señal puede habilitarse o deshabilitarse individualmente mediante parámetros. Una vez en posición, el precio debe avanzar un número de pasos de precio (`PriceStepPoints`) antes de que se permita una orden adicional. Los añadidos imitan el comportamiento del asesor experto original y están limitados por `MaxAdditions` para evitar tamaños de posición descontrolados.

## Lógica de trading

- **Cálculo de señales**
  - `RangePeriod` barras (desplazadas por `Shift`) definen el máximo más alto y el mínimo más bajo utilizados por las bandas de Chandelier Exit.
  - `AtrPeriod` junto con `AtrMultiplier` producen un buffer de volatilidad que aleja las bandas de salida del precio.
  - `SignalBar` (por defecto 1) retrasa la ejecución para que la estrategia actúe sobre la vela finalizada anterior, replicando la implementación de MT5.
- **Entradas**
  - **Largo**: se activa cuando la banda inferior cruza por encima de la superior (`IsUpSignal`). Requiere `EnableBuyEntries = true`. Si existe una posición corta, la estrategia primero intenta aplanarla cuando `EnableSellExits = true`.
  - **Corto**: se activa cuando las bandas cruzan en la dirección opuesta (`IsDownSignal`) y `EnableSellEntries = true`. Las posiciones largas existentes se cierran solo si `EnableBuyExits = true`.
- **Salidas**
  - Las posiciones **largas** se cierran en señales bajistas cuando `EnableBuyExits = true`, o cuando los stops/objetivos protectores son alcanzados.
  - Las posiciones **cortas** se cierran en señales alcistas cuando `EnableSellExits = true`, o a través de niveles protectores.
  - La estrategia también escanea valores más antiguos del indicador cuando tanto los toggles de entrada como de salida están habilitados para asegurar que una señal de cierre esté disponible incluso si la vela más reciente produjo solo una entrada.
- **Reentrada / escala**
  - Después de cada entrada, el último precio de llenado se almacena. Cuando el precio se mueve a favor por al menos `PriceStepPoints * PriceStep`, se envía una orden adicional de tamaño `Volume`, hasta `MaxAdditions` veces.
  - Cada añadido reinicia los cálculos de stop/take al último llenado para que la protección permanezca cerca de la exposición más reciente.
- **Gestión de riesgo**
  - `StopLossPoints` y `TakeProfitPoints` expresan distancias en pasos de precio desde el último llenado. Los stops y objetivos son opcionales; establecerlos en cero para desactivar.
  - Todas las verificaciones protectoras se ejecutan en cada vela finalizada. Si el precio viola un stop o objetivo intrabarra, la posición se cierra a mercado.

## Parámetros predeterminados

| Parámetro | Predeterminado | Descripción |
|-----------|----------------|-------------|
| `CandleType` | `TimeSpan.FromHours(4).TimeFrame()` | Marco temporal usado para cálculos del indicador. |
| `RangePeriod` | 15 | Ventana de observación para el máximo más alto / mínimo más bajo. |
| `Shift` | 1 | Número de barras recientes omitidas antes de calcular el rango. |
| `AtrPeriod` | 14 | Longitud ATR para el buffer de volatilidad. |
| `AtrMultiplier` | 4 | Multiplicador ATR aplicado al buffer. |
| `SignalBar` | 1 | Cuántas barras completadas atrás leer la señal. |
| `PriceStepPoints` | 300 | Movimiento favorable mínimo en pasos de precio antes de añadir a una operación. |
| `MaxAdditions` | 10 | Número máximo de órdenes adicionales después de la entrada inicial. |
| `StopLossPoints` | 1000 | Distancia del stop-loss en pasos de precio. |
| `TakeProfitPoints` | 2000 | Distancia del take-profit en pasos de precio. |
| `EnableBuyEntries` / `EnableSellEntries` | `true` | Permitir abrir operaciones largas/cortas en señales. |
| `EnableBuyExits` / `EnableSellExits` | `true` | Permitir cerrar operaciones largas/cortas en señales opuestas. |

## Notas prácticas

- La estrategia depende de `Volume` para definir el tamaño base de la orden. Las operaciones adicionales reutilizan el mismo tamaño. Ajustar `Volume` o `MaxAdditions` para adaptarse a los límites de riesgo.
- Dado que las reentradas requieren un movimiento expresado en pasos de precio, asegurarse de que los metadatos del instrumento (`PriceStep`) estén configurados correctamente. Los instrumentos con grandes valores de punto pueden necesitar diferentes valores predeterminados.
- `SignalBar` puede establecerse en cero para actuar sobre la vela completada más reciente, pero el experto original usaba un retraso de una barra para evitar actuar sobre la vela que generó la señal.
- Iniciar la estrategia en una combinación de símbolo/cartera que soporte operaciones largas y cortas. Usar los toggles de parámetros integrados para restringirla a una dirección si es necesario.
- Los helpers de gráficos (`DrawCandles`, `DrawIndicator`, `DrawOwnTrades`) se activan automáticamente cuando hay un área de gráfico disponible, facilitando la visualización de bandas y llenados.

## Ejemplo de flujo de trabajo

1. Esperar un cruce alcista: la banda inferior rompe por encima de la banda superior en la vela de marco temporal superior.
2. Si no existe posición y las entradas largas están habilitadas, colocar una orden de compra a mercado de tamaño `Volume`. Los stops y objetivos se establecen en relación al precio de llenado.
3. Si el precio sube al menos `PriceStepPoints` * `PriceStep`, enviar una orden de compra adicional (respetando `MaxAdditions`).
4. Cerrar todo el largo cuando aparezca una señal bajista, cuando se alcance el stop-loss, o cuando se alcance el take-profit. El proceso es simétrico para las operaciones cortas.

Esta documentación refleja la estrategia MT5 original mientras adopta las convenciones de StockSharp como parámetros de estrategia, suscripciones de velas de alto nivel y gestión explícita de posiciones.
