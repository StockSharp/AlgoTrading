# Estrategia RSI Eraser
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia RSI Eraser es un port directo del asesor experto de MetaTrader 5 creado por Vladimir Karputov.
Usa velas horarias para evaluar el índice de fuerza relativa (RSI) y busca entradas de reversión a la media cuando el momentum cambia alrededor del nivel neutral 50.
Las operaciones se filtran por el rango del máximo/mínimo diario anterior y la estrategia dimensiona cada posición de acuerdo con un porcentaje fijo del patrimonio de la cuenta.

## Lógica principal

- **Marco temporal primario** – Las velas de 1 hora impulsan los cálculos del indicador y las señales de trading.
- **Marco temporal de filtro** – Las velas diarias completadas proporcionan el máximo y mínimo de ayer que condicionan las entradas.
- **Indicador** – RSI clásico con longitud de retrospección configurable.
- **Dirección** – Largo cuando RSI > nivel neutral, corto cuando RSI < nivel neutral.
- **Dimensionamiento de riesgo** – El volumen de posición se deriva de la distancia entre la entrada y el stop multiplicada por el porcentaje de riesgo elegido.

## Criterios de entrada

1. Esperar a que la vela horaria cierre y calcular el RSI.
2. Verificar que al menos una vela diaria completada esté disponible.
3. **Configuración larga**
   - Valor de RSI estrictamente por encima del umbral neutral (predeterminado 50).
   - El nivel de stop propuesto (entrada − distancia de stop-loss) no debe estar por debajo del mínimo de ayer menos el búfer diario.
   - La entrada se rechaza si ya se ha abierto una operación larga en el mismo día calendario.
4. **Configuración corta**
   - Valor de RSI estrictamente por debajo del umbral neutral.
   - El nivel de stop propuesto (entrada + distancia de stop-loss) no debe estar por encima del máximo de ayer más el búfer diario.
   - La entrada se rechaza si ya se ha abierto una operación corta en el mismo día calendario.
5. Cuando se satisfacen las condiciones, la estrategia envía una orden de mercado con volumen basado en riesgo.
   Si hay una posición opuesta, la nueva orden la cierra y cambia de dirección en una sola operación.

## Criterios de salida

- El stop-loss y take-profit iniciales se calculan a partir de la distancia de pip configurada y el multiplicador.
- La estrategia monitorea continuamente las velas completadas:
  - Una operación larga sale cuando el precio baja hasta el stop o sube hasta el nivel de take-profit.
  - Una operación corta sale cuando el precio sube hasta el stop o baja hasta el nivel de take-profit.
- Protección de punto de equilibrio: una vez que el precio se mueve a favor en al menos la distancia de stop original,
  el stop se sube (o baja para cortos) al precio exacto de entrada.
- Cuando no hay posición abierta, todos los niveles de riesgo se borran para evitar valores obsoletos.

## Gestión de riesgo

- `RiskPercent` define la fracción del patrimonio del portafolio a arriesgar en cada operación.
- El tamaño de posición se calcula como `risk_amount / stop_distance` con una alternativa al `Volume` base de la estrategia cuando la información del patrimonio no está disponible.
- El búfer diario agrega un margen de seguridad extra alrededor del rango de ayer, previniendo operaciones que colocarían stops demasiado cerca de los extremos de oscilación recientes.

## Valores predeterminados

- `RsiPeriod` = 14
- `RsiNeutralLevel` = 50
- `StopLossPips` = 50
- `TakeProfitMultiplier` = 3
- `DailyBufferPips` = 10
- `RiskPercent` = 5%
- `CandleType` = 1 hora
- `DailyCandleType` = 1 día

## Notas de implementación

- La estrategia se suscribe a feeds de velas horarias y diarias usando la API de alto nivel de StockSharp.
- Todos los comentarios y mensajes de registro se proporcionan en inglés para coincidir con las pautas del repositorio.
- El manejo de punto de equilibrio y la restricción de una operación por día siguen la lógica original de MetaTrader.
