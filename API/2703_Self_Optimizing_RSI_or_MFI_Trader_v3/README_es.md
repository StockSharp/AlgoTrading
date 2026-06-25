# Operador RSI o MFI Autooptimizante v3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia porta el asesor experto de MetaTrader "Self Optimizing RSI or MFI Trader" a la API de alto nivel de StockSharp. En cada vela finalizada el algoritmo realiza un backtesting de una ventana deslizante de barras históricas y encuentra los umbrales de sobrecompra y sobreventa más rentables para el oscilador seleccionado. Las operaciones en vivo solo se realizan cuando el valor actual del oscilador cruza el umbral con mejor rendimiento en la misma dirección que la ventaja histórica, opcionalmente sin requerir un cruce en el modo "agresivo". Las salidas de posición dependen de stops y objetivos basados en ATR o de distancia fija con un paso de punto de equilibrio opcional.

## Datos de mercado
- Funciona con cualquier instrumento que proporcione velas OHLC y volumen (MFI requiere volumen).
- Usa el marco temporal especificado por el parámetro `CandleType`. El predeterminado son velas de 15 minutos, pero puede adjuntar cualquier marco temporal compatible con el adaptador de la plataforma.

## Indicadores
- **Relative Strength Index (RSI)** o **Money Flow Index (MFI)** dependiendo del parámetro `IndicatorChoice`. Ambos comparten la misma longitud de promediado.
- **Average True Range (ATR)** para el dimensionamiento de stop-loss/take-profit basado en ATR cuando `UseDynamicTargets` está habilitado.

## Lógica de trading
1. Mantener un historial continuo de `OptimizingPeriods` + 1 velas finalizadas con sus valores de oscilador y precios de cierre.
2. Para cada nivel entero entre `IndicatorBottomValue` e `IndicatorTopValue` la estrategia simula operaciones en la ventana histórica:
   - Simulación corta: contar cuántas veces el oscilador cruzó por debajo del nivel y si un stop-loss o take-profit corto habría sido alcanzado primero.
   - Simulación larga: contar cuántas veces el oscilador cruzó por encima del nivel y cuán rentables habrían sido las operaciones.
3. Elegir el umbral que entregó la mayor ganancia simulada para cada dirección. Si `TradeReverse` está habilitado, las puntuaciones de rentabilidad se intercambian para que la dirección opuesta se favorezca.
4. Cuando el oscilador en vivo cruza el mejor nivel en la dirección rentable (o inmediatamente cuando `UseAggressiveEntries` es verdadero) la estrategia abre una posición, respetando `OneOrderAtATime`.
5. Gestión de salida:
   - Los niveles de stop-loss y take-profit se calculan ya sea a partir de múltiplos de ATR (`StopLossAtrMultiplier`, `TakeProfitAtrMultiplier`) o de distancias fijas en puntos (`StaticStopLossPoints`, `StaticTakeProfitPoints`).
   - `UseBreakEven` mueve el stop al precio de entrada más `BreakEvenPaddingPoints` una vez que la ganancia no realizada alcanza `BreakEvenTriggerPoints`.
   - Las posiciones se cierran cuando se cruzan los precios de stop-loss o take-profit.

## Gestión de riesgo
- **Dimensionamiento dinámico:** cuando `UseDynamicVolume` es verdadero la estrategia arriesga `RiskPercent` del valor actual del portafolio. El cálculo convierte la distancia del stop en riesgo monetario usando el `PriceStep` y `StepPrice` del instrumento.
- **Dimensionamiento estático:** cuando está deshabilitado, se operan `BaseVolume` lotes en cada entrada.
- **Protección de punto de equilibrio:** asegura que las operaciones ganadoras estén protegidas una vez que se haya acumulado suficiente ganancia.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `OptimizingPeriods` | Número de barras usadas para la optimización continua en muestra (predeterminado 144). |
| `IndicatorChoice` | Elige RSI o MFI como el oscilador principal. |
| `IndicatorPeriod` | Período de promediado para el oscilador y ATR. |
| `IndicatorTopValue` / `IndicatorBottomValue` | Límites de búsqueda para niveles de umbral (típicamente 0–100). |
| `UseAggressiveEntries` | Si es verdadero, permite entradas sin un cruce confirmado. |
| `TradeReverse` | Intercambia puntuaciones de rentabilidad para operar el lado históricamente perdedor. |
| `OneOrderAtATime` | Previene abrir una nueva posición mientras otra está activa. |
| `UseDynamicTargets` | Alterna entre stops/objetivos basados en ATR y de punto fijo. |
| `StopLossAtrMultiplier`, `TakeProfitAtrMultiplier` | Multiplicadores de ATR para salidas dinámicas. |
| `StaticStopLossPoints`, `StaticTakeProfitPoints` | Distancias en puntos para salidas fijas. |
| `UseBreakEven`, `BreakEvenTriggerPoints`, `BreakEvenPaddingPoints` | Configurar el comportamiento del stop de punto de equilibrio. |
| `UseDynamicVolume`, `RiskPercent`, `BaseVolume` | Controlar la lógica de dimensionamiento de posición. |
| `CandleType` | Marco temporal para optimización y trading. |

## Notas de implementación
- La estrategia usa el pipeline `SubscribeCandles().Bind(...)` de StockSharp, por lo que solo se ejecuta en velas completadas.
- `OneOrderAtATime` debe permanecer habilitado cuando se opera en una cuenta de netting, porque la implementación rastrea una sola posición agregada.
- Las salidas basadas en ATR requieren un valor ATR válido; la estrategia omitirá el trading hasta que el indicador esté completamente formado.
- Cuando se usa MFI, asegúrese de que el feed de datos suministre volumen, de lo contrario el indicador devuelve cero y no se generarán operaciones.

## Consejos de optimización
- Optimice `OptimizingPeriods`, el período del oscilador y los multiplicadores de ATR juntos para que coincidan con el régimen de volatilidad del instrumento.
- Diferentes activos pueden beneficiarse de rangos de nivel más estrechos (p. ej., 20–80) para reducir el ruido.
- Considere realizar pruebas prospectivas con análisis walk-forward porque la estrategia adapta los umbrales continuamente.

## Uso
1. Agregue la estrategia a un conector en el Designer o ejecútela programáticamente.
2. Establezca el instrumento deseado, la cartera y los valores de los parámetros.
3. Inicie la estrategia; comenzará a operar una vez que se hayan acumulado suficientes velas para la optimización.

## Limitaciones
- La optimización histórica ocurre en cada barra y puede ser intensiva en CPU para `OptimizingPeriods` muy grandes o rangos de niveles amplios.
- Debido a que los niveles son enteros, no se prueban umbrales de grano fino (p. ej., 70.5).
- El enfoque asume que el pasado reciente sigue siendo predictivo; los cambios repentinos de régimen pueden degradar el rendimiento, así que monitoree los resultados en vivo y ajuste la configuración cuando sea necesario.
