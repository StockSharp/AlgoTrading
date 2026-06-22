# Estrategia EMA (Edición barabashkakvn)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Convertida del asesor experto MetaTrader 5 "EMA (barabashkakvn's edition)". El sistema opera el cruce de dos medias móviles exponenciales calculadas sobre el precio mediana y utiliza niveles virtuales de take-profit/stop-loss expresados en pips. Las posiciones solo se abren después de un cruce confirmado y un pequeño retroceso hacia el extremo de la vela anterior.

## Idea central

1. Seguimiento de EMA de 5 y 10 períodos (precio mediana) en el marco temporal seleccionado.
2. Cuando la EMA rápida cruza la EMA lenta, armar una señal pendiente en lugar de operar inmediatamente.
3. Esperar a que el precio retroceda `MoveBackPips` desde el extremo de la vela anterior mientras el spread de EMA supera `2 * pipSize`.
4. Entrar en la dirección del cruce una vez que ocurra el retroceso.
5. Gestionar la posición abierta con objetivos y stops virtuales medidos en pips desde el precio de entrada.

Este comportamiento refleja la implementación MQL original: el experto esperaba el indicador de cruce (`check`) y luego requería un spread de EMA más un retroceso de precio relativo a la vela anterior para activar la operación. Las reglas de salida también siguen el enfoque "virtual" cerrando posiciones cuando el bid/ask habría tocado las distancias especificadas.

## Indicadores y datos

- EMA de 5 períodos sobre precio mediana (high + low) / 2.
- EMA de 10 períodos sobre precio mediana.
- Máximo/mínimo de la vela terminada anterior para verificaciones de retroceso.
- Todo el procesamiento usa velas terminadas de la suscripción `CandleType` configurada.

## Parámetros

| Parámetro | Predeterminado | Descripción |
|-----------|---------|-------------|
| `OrderVolume` | `0.1` | Volumen de trading en lotes/contratos para cada entrada. |
| `VirtualProfitPips` | `5` | Distancia (en pips) entre el precio de entrada y el take-profit virtual. |
| `MoveBackPips` | `3` | Retroceso requerido después del cruce, medido desde el extremo de la vela anterior. |
| `StopLossPips` | `20` | Distancia (en pips) entre el precio de entrada y el stop-loss virtual. |
| `PipSize` | `0.0001` | Tamaño del pip expresado en unidades de precio. Sobreescribir cuando se operen instrumentos con una definición de pip diferente. |
| `FastLength` | `5` | Longitud de la EMA rápida. |
| `SlowLength` | `10` | Longitud de la EMA lenta. |
| `CandleType` | `TimeFrame(1m)` | Fuente de velas usada para los cálculos. |

Todos los valores basados en pips se convierten a distancias de precio usando `pipValue = PipSize`. Si el parámetro se deja en cero o un número negativo la estrategia recurre a `Security.PriceStep` (cuando lo proporciona el board).

## Lógica de trading

### Condiciones de entrada

- **Armado de señal**: almacenar una señal pendiente siempre que ocurra un cruce (`FastEMA` cruza por encima de `SlowEMA` o viceversa). Todavía no se coloca ninguna operación.
- **Entrada corta**: requiere
  - Señal pendiente presente.
  - `SlowEMA - FastEMA > 2 * pipSize`.
  - Máximo de la vela actual ≥ mínimo de la vela anterior + `MoveBackPips * pipSize` (el precio retrocedió hacia arriba desde el mínimo anterior).
- **Entrada larga**: requiere
  - Señal pendiente presente.
  - `FastEMA - SlowEMA > 2 * pipSize`.
  - Mínimo de la vela actual ≤ máximo de la vela anterior - `MoveBackPips * pipSize` (el precio retrocedió hacia abajo desde el máximo anterior).

Después de abrir una posición el indicador pendiente se resetea para evitar entradas duplicadas.

### Condiciones de salida

Los objetivos virtuales emulan el comportamiento MQL comparando los extremos de la vela con las distancias preestablecidas:

- **Posición larga**:
  - Cerrar si el máximo de la vela ≥ precio de entrada + `VirtualProfitPips * pipSize`.
  - Cerrar si el mínimo de la vela ≤ precio de entrada - `StopLossPips * pipSize`.
- **Posición corta**:
  - Cerrar si el mínimo de la vela ≤ precio de entrada - `VirtualProfitPips * pipSize`.
  - Cerrar si el máximo de la vela ≥ precio de entrada + `StopLossPips * pipSize`.

Después de cualquier salida los niveles virtuales se resetean y la estrategia espera el próximo cruce.

## Notas de implementación

- Usa la suscripción de velas de alto nivel (`SubscribeCandles`) y dibuja EMAs más operaciones en el área de gráfico opcional.
- El precio mediana se computa directamente desde el high/low de la vela para coincidir con `PRICE_MEDIAN` de MetaTrader.
- El indicador de cruce (`_hasCrossSignal`) reproduce la variable `check` original, asegurando que las operaciones solo ocurran después de verificaciones de cruce y retroceso.
- `StartProtection()` se llama en `OnStarted` para habilitar la monitorización de riesgo integrada aunque la estrategia gestione las salidas manualmente.
- El código mantiene todos los comentarios en inglés, según lo solicitado, y depende únicamente de velas terminadas sin acceder directamente a los buffers de indicadores.

## Consejos de uso

- Ajustar `PipSize` cuando se opere con instrumentos con definiciones de pip no estándar (p.ej., pares JPY, índices, cotizaciones cripto).
- Dado que las salidas dependen de los extremos de las velas, usar marcos temporales más cortos (1–5 minutos) mantiene el comportamiento más cercano al experto basado en ticks original.
- La optimización puede explorar longitudes de EMA, distancias en pips y valores de retroceso usando los metadatos de parámetros proporcionados.
- La estrategia opera una posición a la vez; cualquier posición externa en el mismo instrumento puede interferir con el seguimiento virtual de salidas.

## Riesgos

- La simulación basada en velas puede perder toques intrababarra de los niveles virtuales; considerar datos de mayor resolución si la precisión es crítica.
- Las salidas virtuales no colocan órdenes protectoras reales, por lo que las desconexiones o el slippage pueden llevar a pérdidas mayores de lo esperado en trading en vivo.
- Como con cualquier sistema de cruce, el rendimiento se degrada en mercados laterales; combinar con filtros si es necesario.
