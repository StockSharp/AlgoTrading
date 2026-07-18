# Estrategia True Scalper con Bloqueo de Beneficios
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción General
La **Estrategia True Scalper con Bloqueo de Beneficios** es un port de StockSharp del asesor experto de MetaTrader 5 "True Scalper Profit Lock". La estrategia se enfoca en el trading de ultra corto plazo usando medias móviles exponenciales rápidas, un filtro RSI de dos períodos y una rutina de protección de beneficios que mueve los stops al punto de equilibrio. La lógica adicional de "abandono" fuerza a la estrategia a cerrar operaciones que no alcanzan el objetivo dentro de un número predefinido de velas.

La implementación se suscribe a un único stream de velas y evalúa solo las velas terminadas. Está diseñada para scalping intradiario, pero todos los parámetros son totalmente ajustables, permitiendo adaptarla a otros marcos temporales o instrumentos.

## Indicadores y Datos
- **EMA (rápida)** – longitud predeterminada 3, actúa como disparador alcista cuando cruza por encima de la EMA lenta.
- **EMA (lenta)** – longitud predeterminada 7, define la dirección de la tendencia a corto plazo.
- **RSI** – longitud predeterminada 2 con modo de decisión seleccionable:
  - *Método A* (deshabilitado por defecto) reacciona al RSI cruzando el umbral desde la vela anterior.
  - *Método B* (habilitado por defecto) rastrea la polaridad del RSI relativa al umbral.
- **Velas** – el marco temporal predeterminado es 1 minuto, configurable a través del parámetro `CandleType`.

## Lógica de Entrada
1. Calcular la EMA rápida, la EMA lenta y el RSI en la última vela terminada.
2. Evaluar el estado del RSI:
   - Método A: establecer la polaridad del RSI solo cuando el umbral se cruza entre dos velas consecutivas.
   - Método B: establecer la polaridad del RSI según si el valor está por encima o por debajo del umbral.
3. **Setup de compra** – se activa cuando la EMA rápida está al menos un paso de precio por encima de la EMA lenta *y* el RSI indica polaridad negativa. Si la lógica de abandono forzó una inversión a largo, la operación también se abre independientemente de las señales actuales.
4. **Setup de venta** – se activa cuando la EMA rápida está al menos un paso de precio por debajo de la EMA lenta *y* el RSI indica polaridad positiva, o cuando una inversión de abandono impone una entrada corta.
5. Las inversiones de posición se manejan enviando la diferencia necesaria para cambiar la posición neta en una única orden de mercado.

## Lógica de Salida
- **Stop Loss / Take Profit** – configurados en pasos de precio (`StopLossPoints`, `TakeProfitPoints`) y aplicados inmediatamente después de la entrada.
- **Bloqueo de beneficios** – cuando está habilitado, una vez que la operación abierta acumula el beneficio especificado (`BreakEvenTriggerPoints`), el stop se mueve al punto de equilibrio más un offset (`BreakEvenPoints`). La rutina funciona para posiciones largas y cortas y solo se ejecuta una vez por operación.
- **Lógica de abandono** – rastrea el número de velas terminadas desde la entrada:
  - *Método A*: cierra la operación después de `AbandonBars` velas y establece una bandera para abrir una posición en la dirección opuesta en la próxima oportunidad.
  - *Método B*: cierra la posición después del tiempo de espera pero deja intacta la selección de dirección basada en señales.
  - El Método A tiene prioridad cuando ambos métodos están habilitados.
- Las salidas manuales se emiten con órdenes de mercado (vía `ClosePosition`) y reinician automáticamente el estado de la operación.

## Gestión del Dinero
- Cuando `UseMoneyManagement` está habilitado, el tamaño de la posición se deriva del saldo del portafolio: `Ceiling(Balance * RiskPercent / 10000) / 10`.
- El volumen gestionado está limitado a las reglas originales del MT5: mínimo de respaldo a `InitialVolume`, valores por encima de 1 lote redondeados hacia arriba, multiplicador opcional de mini-cuenta, límite máximo de 100 lotes.
- Cuando está deshabilitado, la estrategia usa el `InitialVolume` fijo para cada orden.

## Parámetros
- `InitialVolume` – tamaño de lote base cuando la gestión del dinero está deshabilitada.
- `TakeProfitPoints` / `StopLossPoints` – distancia en unidades de `Security.PriceStep`.
- `FastPeriod`, `SlowPeriod`, `RsiLength`, `RsiThreshold` – configuración de indicadores.
- `UseRsiMethodA`, `UseRsiMethodB` – alternar la lógica de decisión del RSI.
- `UseAbandonMethodA`, `UseAbandonMethodB`, `AbandonBars` – configurar la gestión del tiempo de espera.
- `UseMoneyManagement`, `RiskPercent`, `LiveTrading`, `IsMiniAccount` – opciones de dimensionamiento de riesgo alineadas con el asesor experto MT5.
- `UseProfitLock`, `BreakEvenTriggerPoints`, `BreakEvenPoints` – parámetros de punto de equilibrio.
- `MaxPositions` – mantenido por compatibilidad con la versión MQL (el port de StockSharp gestiona una única posición neta por instrumento).
- `CandleType` – marco temporal o tipo de vela personalizado para la generación de señales.

## Notas de Uso
- Adjunte la estrategia a un único instrumento; el override `GetWorkingSecurities` se suscribe automáticamente al tipo de vela seleccionado.
- Las características de bloqueo de beneficios y abandono dependen de velas terminadas; los picos de precio intrabar que revierten dentro de la misma vela son ignorados.
- El parámetro original MT5 `Slippage` no se usó en el código fuente y por tanto no está presente.
- Ajuste `Security.PriceStep` o los parámetros basados en pasos según el instrumento operado para mantener las distancias en pips previstas.

## Diferencias de Conversión
- StockSharp opera en posiciones netas, por lo que no se abren múltiples posiciones simultáneas incluso si `MaxPositions` es mayor que uno. Esto refleja el comportamiento típico de netting del asesor experto original cuando `maxTradesPerPair` es igual a 1.
- La gestión de órdenes usa los helpers `BuyMarket`, `SellMarket` y `ClosePosition` en lugar de manipulación directa de tickets.
- Los datos de indicadores se entregan a través de callbacks `Bind` para evitar el acceso manual al búfer.

## Recomendaciones de Prueba
- Valide el comportamiento en datos históricos con el mismo marco temporal usado en el EA original (velas de 1 minuto).
- Optimice `TakeProfitPoints`, `StopLossPoints` y `BreakEvenTriggerPoints` para el instrumento objetivo, ya que estos fueron ajustados para cotizaciones forex.
