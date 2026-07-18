# Estrategia de Alineación Doble ZigZag
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es un port de StockSharp del experto MQL5 «Double ZigZag». Recrea la lógica de confirmación dual de ZigZag combinando
dos detectores de swings con diferentes ventanas de retroceso. Una operación se activa únicamente cuando ambos detectores coinciden en
tres pivotes consecutivos y el swing más reciente muestra suficiente fuerza comparado con los anteriores.

## Concepto

- El detector de swings rápido aproxima la configuración original ZigZag(13, 5, 3) usando una ventana deslizante de máximos/mínimos.
- El detector de swings lento utiliza una ventana más larga (por defecto x8) para confirmar puntos de giro principales.
- Cuando ambos detectores cambian de dirección en la misma vela, se registra un pivote de «alineación» junto con el número de swings
  rápidos ocurridos desde la alineación anterior. Estos contadores son análogos directos de los contadores `up` y `dw` del EA original.

## Configuración Largo

1. La última alineación es un máximo de swing, la alineación anterior es un mínimo de swing, y la que precede a esta también es un máximo de swing.
2. El número de swings rápidos acumulados desde la última alineación es mayor que el conteo del segmento anterior multiplicado por
   `StrengthMultiplier` (por defecto 2.1). Esto emula la condición original `up > dw * k`.
3. El máximo de swing más reciente rompe por encima del mínimo de swing intermedio de forma más agresiva que el máximo más antiguo,
   es decir `(previousHigh - swingLow) * BreakoutMultiplier < (newestHigh - swingLow)` con el mismo multiplicador por defecto de 2.1.
4. Cuando todos los criterios se cumplen, la estrategia compra un volumen igual al `Volume` configurado más cualquier posición corta
   pendiente para que la posición neta sea larga.

## Configuración Corto

1. La última alineación es un mínimo de swing, la alineación anterior es un máximo de swing, y la que precede a esta es otro mínimo de swing.
2. El conteo del segmento más reciente es menor que el conteo anterior dividido por `StrengthMultiplier` (la comprobación traducida
   `up * k < dw`).
3. El mínimo de swing actual rompe por debajo del máximo de swing intermedio de forma más agresiva que el mínimo más antiguo usando `BreakoutMultiplier`.
4. La estrategia vende suficiente volumen para cerrar cualquier posición larga existente y establecer una posición corta neta.

## Gestión de Posiciones

- Las señales son mutuamente excluyentes; un nuevo largo cierra automáticamente cualquier corto y viceversa.
- No hay órdenes de stop-loss ni take-profit. Las posiciones se mantienen hasta que aparece una señal de alineación opuesta.
- La estrategia se ejecuta sobre el tipo de vela especificado por `CandleType` (por defecto marco temporal de 1 minuto).

## Valores Predeterminados

- `FastLength` = 13
- `SlowLength` = 104
- `StrengthMultiplier` = 2.1
- `BreakoutMultiplier` = 2.1
- `CandleType` = marco temporal `TimeSpan.FromMinutes(1)`

## Etiquetas

- **Categoría**: Seguimiento de tendencia / Reconocimiento de patrones
- **Dirección**: Largo/Corto
- **Indicadores**: ZigZag (aproximado), Highest/Lowest
- **Stops**: Ninguno
- **Marco temporal**: Intradía por defecto
- **Complejidad**: Intermedio (requiere seguimiento sincronizado de swings)
- **Estacionalidad**: No
- **Redes neuronales**: No
- **Divergencia**: No
- **Nivel de riesgo**: Medio debido a exposición continua sin stops
