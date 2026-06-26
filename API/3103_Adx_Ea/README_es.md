# 3103 — ADX EA (C#)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción General
El "ADX EA" original de MetaTrader combina rupturas del Índice Direccional Medio con cruces de +DI/−DI, confirmación de momentum
en marco temporal superior y un filtro MACD mensual. El port en C# replica ese flujo de trabajo multi-filtro sobre la API de alto
nivel de StockSharp. La estrategia se suscribe a tres flujos de velas:

1. **Marco temporal principal** (por defecto 5 minutos) — impulsa ADX, medias móviles linealmente ponderadas, comprobaciones de
   estructura de precio y filtros de volumen.
2. **Marco temporal de momentum** (por defecto 15 minutos) — produce las desviaciones de momentum alrededor de la línea base 100
   que condicionan las entradas.
3. **Marco temporal de MACD** (por defecto 30 días) — refleja el MACD mensual que controla las salidas de posición.

## Lógica de Trading
- **Módulo de ruptura** – Cuando está habilitado, las operaciones largas requieren:
  - ADX o +DI por encima de `EntryLevel` y la brecha entre +DI y −DI mayor que `MinDirectionalDifference`.
  - La LWMA rápida por encima de la LWMA lenta, estructura de vela alcista (`Low[2] < High[1]`) y momentum creciente
    (`Momentum[1] > Momentum[2]`).
  - Al menos una de las últimas tres lecturas de momentum en el marco temporal superior que se desvíe de 100 en más de
    `MomentumBuyThreshold`.
  - Volumen creciente en el marco temporal principal (`Volume[1] > Volume[2]` o `Volume[1] > Volume[3]`).
  - MACD en el marco temporal mensual alcista (`MacdMain[1] > MacdSignal[1]`).
  - ADX por encima de `ExitLevel` para confirmar la fortaleza general del trend.

  Las rupturas cortas aplican la lógica simétrica con dominancia de −DI, estructura bajista (`Low[1] < High[2]`), momentum por
  debajo de 100 en `MomentumSellThreshold` y una comparación MACD bajista.

- **Módulo de cruce** – Cuando está activo, busca que +DI cruce por encima de −DI (largos) o −DI cruce por encima de +DI
  (cortos). Los filtros opcionales reflejan el EA original:
  - `RequireAdxSlope` exige que ADX sea mayor que la lectura anterior.
  - `ConfirmCrossOnBreakout` añade las mismas verificaciones de umbral de ruptura en la barra de cruce.
  - `MinAdxMainLine` impone una fortaleza mínima de ADX durante el cruce.
  - La alineación de LWMA, la pendiente de momentum, la expansión de volumen y la polaridad de MACD aún deben estar de acuerdo
    con la dirección prevista.

- **Piramidación** – Cada nueva orden añade volumen según `LotExponent`. La estrategia trata `TradeVolume` como el tamaño de lote
  base y lo incrementa por `LotExponent^n`, donde `n` es el número de escalones ya abiertos. `MaxTrades` limita la cantidad de
  volumen neto que se puede acumular.

## Gestión de Riesgos
- **Órdenes protectoras** – `TakeProfitSteps` y `StopLossSteps` se pasan a `StartProtection` y se expresan en pasos de precio
  del instrumento.
- **Trailing stop** – `TrailingStopSteps` mantiene una barrera de trailing manual más allá del mejor precio de cierre.
- **Punto de equilibrio** – Cuando `UseBreakEven` está habilitado, el stop se ajusta después de que el precio avanza
  `BreakEvenTrigger` pasos y puede desplazar el stop `BreakEvenOffset` pasos.
- **Salida por MACD** – Cuando `EnableMacdExit` es verdadero, la relación MACD mensual cierra los largos cuando MACD cae por
  debajo de su señal (y viceversa para los cortos), coincidiendo con las rutinas `Close_BUY`/`Close_SELL` del EA.
- **Stop de capital** – `UseEquityStop` rastrea la curva de beneficio flotante y liquida posiciones una vez que el drawdown
  alcanza `TotalEquityRisk` por ciento.

Las funciones que dependían de objetivos en divisa de la cuenta ("Take Profit in Money", "Trailing Profit in Money", etc.) no
están portadas porque las estrategias de StockSharp típicamente gestionan la lógica de protección a través de distancias de stop
y el servicio de protección integrado. Todos los demás puntos de decisión del EA se preservan con equivalentes de indicadores.

## Parámetros
| Parámetro | Valor predeterminado | Descripción |
|-----------|----------------------|-------------|
| `TradeVolume` | 0.01 | Tamaño de lote base para la primera entrada. |
| `CandleType` | Marco temporal de 5m | Serie de velas principal para la lógica ADX/LWMA. |
| `MomentumCandleType` | Marco temporal de 15m | Marco temporal superior para el filtro de desviación de momentum. |
| `MacdCandleType` | Marco temporal de 30 días | Marco temporal que alimenta el filtro de salida MACD. |
| `FastMaPeriod` | 6 | Longitud de la media móvil linealmente ponderada rápida. |
| `SlowMaPeriod` | 85 | Longitud de la media móvil linealmente ponderada lenta. |
| `AdxPeriod` | 14 | Período del Índice Direccional Medio. |
| `MomentumPeriod` | 14 | Período del indicador de momentum en el marco temporal superior. |
| `MacdFastPeriod` | 12 | Período de EMA rápido dentro del filtro de salida MACD. |
| `MacdSlowPeriod` | 26 | Período de EMA lento dentro del filtro de salida MACD. |
| `MacdSignalPeriod` | 9 | Período de SMA de señal dentro del filtro de salida MACD. |
| `EnableBreakoutStrategy` | true | Interruptor para la rama de ruptura ADX. |
| `EnableCrossStrategy` | true | Interruptor para la rama de cruce DI. |
| `UseTrendFilter` | true | Impone dominancia de +DI para largos y −DI para cortos durante las rupturas. |
| `RequireAdxSlope` | true | Requiere que ADX suba al evaluar cruces DI. |
| `ConfirmCrossOnBreakout` | true | Añade umbrales de ruptura al módulo de cruce. |
| `EnableMacdExit` | true | Habilita la rutina de salida basada en MACD. |
| `EntryLevel` | 10 | Nivel mínimo de ADX/+DI/−DI usado por las rupturas. |
| `ExitLevel` | 10 | Fortaleza mínima de ADX que permite nuevas entradas. |
| `MinDirectionalDifference` | 10 | Brecha requerida entre +DI y −DI. |
| `MinAdxMainLine` | 10 | Nivel mínimo de ADX durante los cruces DI. |
| `MomentumBuyThreshold` | 0.3 | Desviación requerida desde 100 para la confirmación de momentum alcista. |
| `MomentumSellThreshold` | 0.3 | Desviación requerida desde 100 para la confirmación de momentum bajista. |
| `MaxTrades` | 10 | Número máximo de escalones de piramidación. |
| `LotExponent` | 1.44 | Multiplicador de volumen para cada escalón adicional. |
| `TakeProfitSteps` | 50 | Distancia, en pasos de precio, para la orden de take-profit. |
| `StopLossSteps` | 20 | Distancia, en pasos de precio, para la orden de stop-loss. |
| `TrailingStopSteps` | 40 | Distancia del trailing stop manual en pasos de precio. |
| `UseBreakEven` | true | Activa la lógica de reubicación del punto de equilibrio. |
| `BreakEvenTrigger` | 30 | Pasos de movimiento favorable requeridos antes de armar el punto de equilibrio. |
| `BreakEvenOffset` | 30 | Pasos adicionales añadidos al precio de entrada al mover el stop. |
| `UseEquityStop` | true | Habilita la salida de emergencia basada en drawdown. |
| `TotalEquityRisk` | 1 | Porcentaje de drawdown permitido antes de cerrar todas las posiciones. |

## Consejos de Uso
- Alinee `MomentumCandleType` y `MacdCandleType` con su marco temporal principal para imitar el mapeo de marcos temporales
  original (p. ej., gráfico de 5 minutos → momentum de 15 minutos → MACD mensual).
- Ajuste `EntryLevel`, `MinDirectionalDifference` y `MinAdxMainLine` juntos; bajar los tres afloja considerablemente el filtro
  de ruptura.
- `LotExponent` mayor que 1.0 recrea el escalado estilo martingala del EA. Configúrelo en 1.0 para mantener constantes los
  tamaños de posición.
