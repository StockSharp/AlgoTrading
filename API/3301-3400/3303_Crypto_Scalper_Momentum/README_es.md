# Estrategia Crypto Scalper Momentum
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Visión general

La **estrategia Crypto Scalper Momentum** replica el asesor experto original de MetaTrader "Crypto Scalper" combinando Money Flow Index, Momentum y filtros MACD multimarco. Opera en un marco intradía principal, confirma el momentum de corto plazo en un marco superior y respeta un filtro de macrotendencia derivado de un MACD lento. Se preservan varias funciones de gestión de riesgo de la implementación MQL, incluidos objetivos de cesta basados en divisa, trailing monetario, stops de break-even y protección contra drawdown de patrimonio.

## Lógica de negociación

1. **Indicadores principales**
   - Money Flow Index (MFI) en el marco principal con valor predeterminado de 14 periodos.
   - MACD en el marco principal (configuración EMA 12/26/9).
2. **Momentum de marco superior**
   - Indicador Momentum calculado en una serie de velas separada. La distancia absoluta frente a la línea base de MetaTrader (100) debe superar un umbral configurable.
3. **Filtro de macrotendencia**
   - Un MACD lento evaluado en un marco macro (diario por defecto) evita operar contra la tendencia superior y fuerza la liquidación cuando se invierte.
4. **Reglas de entrada**
   - **Largos**: al menos uno de los tres últimos valores MFI está por debajo del umbral de sobreventa, la desviación de momentum supera el umbral, la línea MACD principal está por encima de la señal y el MACD macro es alcista.
   - **Cortos**: condiciones espejo usando umbrales de sobrecompra y confirmaciones MACD bajistas.
5. **Reglas de salida**
   - Stop-loss y take-profit fijos expresados en pips.
   - Trailing stop opcional mediante extremos de velas o trailing clásico basado en distancia.
   - Movimiento a break-even tras una excursión favorable configurable.
   - La reversión del MACD macro cierra la exposición existente.
   - Objetivos monetarios, objetivos porcentuales y trailing de beneficio en dinero replican las funciones de MQL.
   - Un vigilante de drawdown de patrimonio cierra todas las operaciones cuando la cuenta retrocede un porcentaje dado desde el máximo.

## Gestión de riesgo

- **Stops/objetivos**: distancias configurables en pips con activación opcional.
- **Trailing**: basado en velas (mínimo más bajo/máximo más alto de velas recientes) o trailing clásico en pips.
- **Break-even**: mueve los stops para asegurar beneficios una vez alcanzada la distancia de activación.
- **Gestión monetaria**: take-profit de cesta en divisa, porcentaje del patrimonio inicial y trailing de beneficio en dinero.
- **Stop de patrimonio**: monitoriza el patrimonio máximo observado y cierra operaciones cuando el drawdown supera el porcentaje permitido.

## Parámetros

| Nombre | Descripción |
|------|-------------|
| `CandleType` | Serie principal de velas usada para entradas. |
| `MomentumCandleType` | Velas de marco superior que alimentan el indicador Momentum. |
| `MacroCandleType` | Velas de marco lento usadas para el filtro MACD macro. |
| `MfiPeriod` | Longitud del Money Flow Index. |
| `MfiOversold` / `MfiOverbought` | Umbrales del oscilador (30 / 70 por defecto). |
| `MomentumPeriod` | Longitud del Momentum en el marco superior. |
| `MomentumThreshold` | Desviación mínima desde la línea 100 requerida por el filtro de momentum. |
| `MomentumReference` | Valor base (el predeterminado de MetaTrader es 100). |
| `MacdFastPeriod` / `MacdSlowPeriod` / `MacdSignalPeriod` | Parámetros MACD en el marco de negociación. |
| `MacroMacdFastPeriod` / `MacroMacdSlowPeriod` / `MacroMacdSignalPeriod` | Parámetros MACD en el marco macro. |
| `TradeVolume` | Volumen de cada orden de mercado (lotes). |
| `MaxTrades` | Máximo de operaciones simultáneas por dirección (0 = ilimitado). |
| `UseStopLoss` / `StopLossPips` | Activa y configura el stop protector. |
| `UseTakeProfit` / `TakeProfitPips` | Activa y configura el objetivo protector. |
| `UseTrailingStop` | Interruptor principal de la lógica trailing. |
| `UseCandleTrail` | Cambia entre trailing por extremos de vela y trailing clásico. |
| `TrailTriggerPips` / `TrailAmountPips` | Distancia de activación y distancia mantenida por el trailing stop clásico. |
| `CandleTrailLength` / `CandleTrailBufferPips` | Número de velas y búfer extra para trailing basado en velas. |
| `UseBreakEven` / `BreakEvenTriggerPips` / `BreakEvenOffsetPips` | Distancia de activación de break-even y beneficio asegurado. |
| `UseMoneyTakeProfit` / `MoneyTakeProfit` | Take-profit de cesta en la moneda de la cuenta. |
| `UsePercentTakeProfit` / `PercentTakeProfit` | Take-profit de cesta en porcentaje del patrimonio inicial. |
| `EnableMoneyTrailing` / `MoneyTrailTarget` / `MoneyTrailStop` | Trailing del beneficio flotante en divisa. |
| `UseEquityStop` / `EquityRiskPercent` | Guardia de drawdown de patrimonio relativa al máximo observado. |
| `ForceExit` | Aplana inmediatamente las posiciones en el siguiente cierre de vela. |

## Notas

- Las distancias en pips se convierten con `PriceStep` del instrumento. Si el broker no proporciona un paso de precio, se usa un valor alternativo de `0.0001`, igual que el manejo de puntos en MetaTrader.
- La suscripción MACD macro puede apuntarse a velas mensuales para imitar el EA original. Las velas diarias son el valor predeterminado porque las barras mensuales pueden no estar disponibles en todos los feeds de datos.
- Todos los comentarios dentro del código están escritos en inglés para cumplir las reglas del repositorio.
