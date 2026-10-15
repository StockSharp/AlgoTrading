# Estrategia Gold Dust
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción

La estrategia Gold Dust reproduce el asesor experto de MetaTrader 5 "Gold Dust" dentro del framework StockSharp. Evalúa hasta dos perceptrones construidos a partir de una media móvil ponderada linealmente (LWMA) aplicada al precio ponderado de la vela. Cada perceptrón observa cómo el precio se desvía de la media móvil en cuatro puntos de retroceso diferentes separados por el período de la MA. Cuando la salida del perceptrón es positiva, el experto original abre una posición de venta, y cuando es negativa abre una compra. El port de StockSharp mantiene el mismo comportamiento basándose en la API de suscripción de velas de alto nivel.

## Generación de señales

1. Suscribirse al `CandleType` configurado y calcular un `WeightedMovingAverage` con el período tomado de `MaPeriod`.
2. En cada vela terminada, almacenar los precios de apertura y cierre de la vela junto con el valor de LWMA. La estrategia siempre mantiene tres períodos completos de MA de historia para replicar las llamadas `CopyRates`/`CopyBuffer` de la versión MQL.
3. Calcular los desplazamientos precio/MA:
   - `a1` – cierre actual menos LWMA actual
   - `a2` – precio de apertura hace un período de MA menos LWMA en la misma vela
   - `a3` – precio de apertura hace dos períodos de MA menos LWMA en la misma vela
   - `a4` – precio de apertura hace tres períodos de MA menos LWMA en la misma vela
4. Construir la salida del perceptrón `result = Σ (wi × ai)` donde cada peso es el parámetro bruto (por ejemplo `X11`) menos 100, replicando la transformación original `w = x - 100`.
5. Interpretar las salidas del perceptrón dependiendo de `PassMode`:
   - `1` – usar solo el primer perceptrón.
   - `2` – usar solo el segundo perceptrón.
   - `3` – requerir que ambos perceptrones produzcan el mismo signo distinto de cero.
6. Una señal negativa abre o mantiene una posición larga, una señal positiva abre o mantiene una posición corta, y una señal de cero activa la toma de beneficios en posiciones existentes.

## Gestión de posiciones

- **Entradas** – la estrategia opera con un `TradeVolume` fijo. Entrar en largo cierra cualquier exposición corta pendiente y viceversa, de modo que solo permanece una posición direccional, replicando el comportamiento de `m_need_open_buy`/`m_need_open_sell` en el código original.
- **Stop-loss** – `StopLossPips` se convierte en distancia de precio absoluta usando `Security.PriceStep`. Para instrumentos cotizados con tres o cinco decimales, la distancia se multiplica por diez para imitar la lógica de "punto ajustado" en la versión MQL. El stop se evalúa en cada vela completada: si el mínimo de la vela (para largos) o el máximo (para cortos) cruza el nivel de stop, la posición se cierra con una orden de mercado.
- **Trailing stop** – cuando `TrailingStopPips` es mayor que cero, la lógica de trailing se activa. Después de que el precio se mueve `TrailingStopPips + TrailingStepPips` a favor de la operación, el stop se posiciona en `cierre ± TrailingStopPips` (según la dirección). El trailing es basado en velas y crea un stop incluso si el stop-loss inicial estaba desactivado, igual que `PositionModify` en el EA.
- **Gestión de beneficios** – cuando ningún perceptrón concuerda en una dirección (`signal == 0`), la estrategia cierra la posición solo si el beneficio flotante es positivo. Esto reproduce `CloseProfitPositions` donde swaps, comisiones y beneficio deben ser mayores que cero.

## Parámetros

| Parámetro | Predeterminado | Descripción |
|-----------|---------|-------------|
| `TradeVolume` | `1` | Volumen base para cada nueva entrada. Las posiciones opuestas se aplanan antes de tomar un nuevo lado. |
| `StopLossPips` | `150` | Distancia inicial de stop-loss en pips ajustados (tiene en cuenta el multiplicador de 3/5 dígitos). Establecer en cero para deshabilitar el stop inicial. |
| `TrailingStopPips` | `25` | Distancia de trailing stop en pips ajustados. Establecer en cero para deshabilitar el trailing. |
| `TrailingStepPips` | `5` | Movimiento favorable adicional (en pips) requerido antes de que el trailing stop avance. |
| `MaPeriod` | `20` | Longitud del período de la media móvil ponderada que alimenta los perceptrones. |
| `CandleType` | `H1` | Serie de velas usada para evaluación de señales. Se puede seleccionar cualquier otro marco temporal compatible con el proveedor de datos. |
| `PassMode` | `1` | Controla qué perceptrón(es) se evalúan: 1 – primero, 2 – segundo, 3 – consenso de ambos. |
| `X11`, `X21`, `X31`, `X41` | `100` | Pesos brutos para el perceptrón #1. La estrategia resta 100 de cada valor antes de usarlo. |
| `X12`, `X22`, `X32`, `X42` | `100` | Pesos brutos para el perceptrón #2, manejados de la misma manera que el primer conjunto. |

## Notas sobre la conversión

- El EA original dependía de actualizaciones tick a tick para gestionar stops; el port de StockSharp evalúa stops y trailing al cierre de vela. Esto mantiene la implementación dentro de la API de alto nivel mientras permanece fiel a la lógica general.
- La gestión monetaria vía `CMoneyFixedMargin` fue reemplazada con un parámetro fijo `TradeVolume`. Los usuarios pueden integrar su propia lógica de dimensionamiento de posición si es necesario.
- Los cálculos del perceptrón evitan buffers directos de indicadores (`CopyBuffer`) almacenando en caché los valores necesarios de velas y MA en listas acotadas.
- Todas las distancias de pips respetan la convención de "punto ajustado" de MetaTrader: si el instrumento opera con 3 o 5 decimales, la distancia se multiplica por diez antes de aplicarse a los niveles de precio.

## Consejos de uso

1. Crear o seleccionar un símbolo, luego establecer `CandleType` al marco temporal que corresponde al gráfico histórico usado en la versión MQL.
2. Revisar los pesos del perceptrón (`X**`) y `PassMode` para que coincidan con la configuración optimizada de MetaTrader. Cada peso puede optimizarse independientemente dentro de StockSharp.
3. Ajustar `TradeVolume` para que cumpla con el tamaño mínimo y el paso del broker conectado. La estrategia agrega automáticamente la exposición opuesta absoluta al cambiar de dirección.
4. Monitorear el log: cada vez que el trailing stop avanza o se activa un stop-loss, se registra un mensaje, lo que ayuda a verificar que el port se comporta como el EA original.
