# Estrategia MACD 1 MIN SCALPER
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es un port en C# del asesor experto de MetaTrader **"MACD 1 MIN SCALPER"**. Combina medias móviles ponderadas con confirmaciones de MACD en múltiples marcos temporales y un filtro de momentum antes de abrir operaciones. El objetivo es operar en la dirección de la tendencia cuando los indicadores de marcos temporales inferior y superior están alineados y el momentum del precio es suficientemente fuerte.

## Lógica de trading

1. **Marco temporal base** – configurable (por defecto M1). Dos medias móviles ponderadas (WMA) con períodos 50 y 200, calculadas sobre el precio típico `(Máximo + Mínimo + Cierre) / 3`, definen la tendencia a corto plazo.
2. **Filtro de tendencia de marco temporal superior** – se calculan WMAs con los mismos períodos en el marco temporal H1. Las configuraciones largas requieren que ambas WMAs rápidas estén por encima de sus homólogas lentas, las cortas requieren lo opuesto. Si el marco temporal operativo ya es H1, las WMAs base se reutilizan.
3. **Confirmaciones MACD** – el MACD (12, 26, 9) debe tener su línea principal por encima de la línea de señal en el marco temporal base, el marco temporal H1 y un marco temporal mensual (aprox. 43200 minutos). Las entradas cortas requieren que los tres MACDs estén por debajo de sus señales.
4. **Filtro de momentum** – un indicador de momentum con período 14 opera en un marco temporal superior derivado del período base de MetaTrader (M1→M15, M5→M30, …). La desviación absoluta desde 100 debe superar un umbral configurable en al menos una de las últimas tres barras completadas.
5. **Reglas de entrada** – se abre una posición larga cuando se cumplen todas las condiciones alcistas y la estrategia actualmente no tiene exposición larga. Una posición corta requiere las condiciones bajistas reflejadas. Si hay una posición opuesta abierta, el tamaño de la orden incluye automáticamente la cantidad necesaria para cerrarla.
6. **Gestión de riesgo** – las distancias opcionales de stop-loss y take-profit se especifican en pips y se convierten a puntos del instrumento al inicio. Las funciones de trailing, breakeven y gestión monetaria del script original se omiten intencionalmente en este port de alto nivel.

## Parámetros

| Parámetro | Descripción |
| --- | --- |
| `CandleType` | Marco temporal operativo para los indicadores base. |
| `OrderVolume` | Volumen enviado con cada entrada de mercado. También se usa para cerrar/voltear posiciones. |
| `FastMaPeriod` / `SlowMaPeriod` | Longitudes de las medias móviles ponderadas rápida y lenta. |
| `MacdFastPeriod` / `MacdSlowPeriod` / `MacdSignalPeriod` | Períodos EMA usados por el indicador MACD. |
| `MomentumPeriod` | Longitud del indicador de momentum en el marco temporal de confirmación. |
| `MomentumThreshold` | Desviación absoluta mínima desde 100 requerida para aceptar el momentum. |
| `TakeProfitPips` / `StopLossPips` | Niveles protectores opcionales especificados en pips. |

## Notas de implementación

- La estrategia depende de las suscripciones de velas de alto nivel de StockSharp (`SubscribeCandles`) y la vinculación de indicadores (`Bind` / `BindEx`). No se usan cálculos de indicadores manuales o buffers históricos.
- El marco temporal de momentum se deriva del mapeo de MetaTrader: `[1,5,15,30,60,240,1440,10080,43200]`. Si un valor cae fuera de esta lista, se usa un multiplicador 4× del marco temporal base como respaldo.
- `StartProtection` se lanza solo cuando al menos uno de los parámetros de riesgo es mayor que cero. No hay implementación de trailing stop en este port.
- La representación en gráficos está habilitada para las velas base, ambas WMAs y el MACD para facilitar la inspección visual durante la depuración o el trading en vivo.

## Consejos de uso

- Establezca el parámetro `OrderVolume` según el tamaño mínimo de lote del instrumento. El ayudante ajusta automáticamente el volumen enviado para que coincida con el paso y las restricciones de mínimo/máximo del símbolo.
- Asegúrese de que los datos de marcos temporales superiores (H1 y mensual) estén disponibles en el feed de datos. Sin estas velas, la estrategia no abrirá posiciones porque las señales de confirmación permanecen incompletas.
- El filtrado de momentum es sensible al umbral elegido. Los valores más altos exigen impulsos de momentum más fuertes mientras que los valores más bajos resultan en operaciones más frecuentes.
