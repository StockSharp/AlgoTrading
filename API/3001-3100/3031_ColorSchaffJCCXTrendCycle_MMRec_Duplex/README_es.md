# Estrategia Color Schaff JCCX Trend Cycle MMRec Duplex
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
- Recrea el Asesor Experto bidireccional "ColorSchaffJCCXTrendCycle_MMRec_Duplex" de MetaTrader dentro de StockSharp.
- Utiliza dos cadenas independientes de Schaff Trend Cycle basadas en medias móviles Jurik para detectar reversiones alcistas y bajistas.
- Implementa un módulo simplificado MMRec (recomendador de gestión monetaria) que reduce el tamaño tras pérdidas repetidas.
- Aplica conjuntos de parámetros separados para operaciones largas y cortas, permitiendo configuraciones asimétricas entre marcos temporales y fuentes de precio.

## Cadena de indicadores
1. **Aproximación JCCX** – cada precio se procesa con una media móvil Jurik para obtener una serie detendenciada. La serie detendenciada y su valor absoluto se suavizan de nuevo con medias Jurik para aproximar el oscilador JCCX original.
2. **Capa MACD** – la diferencia entre las salidas JCCX rápida y lenta proporciona la base de momentum.
3. **Doble transformación estocástica** – ventanas deslizantes de mínimo/máximo normalizan el momentum MACD y producen el valor final de Schaff Trend Cycle (STC) en el rango -100..+100.
4. **Control de fase** – el parámetro `Phase` modula un factor de suavizado interno (0.05–0.95) aplicado tras cada paso estocástico, emulando el comportamiento de "fase" de Jurik.

La cadena de indicadores se ejecuta dos veces: una para el bloque largo y otra para el bloque corto. Cada bloque puede usar diferentes tipos de velas y entradas de precio.

## Lógica de trading
### Bloque largo
- **Entrada**: cuando el STC largo cruza por encima de cero (valor actual > 0 y el valor previo retardado ≤ 0). Las posiciones cortas existentes se cierran primero.
- **Salida**: cuando el STC largo cae por debajo de cero y las salidas largas están habilitadas.
- **Stops**: las distancias opcionales de stop-loss y take-profit (expresadas en pasos de precio) se evalúan en cada vela completada usando máximos/mínimos de vela.

### Bloque corto
- **Entrada**: cuando el STC corto cruza por debajo de cero (valor actual < 0 y el valor retardado ≥ 0). Cualquier posición larga existente se liquida antes de abrir una posición corta.
- **Salida**: cuando el STC corto sube por encima de cero y las salidas cortas están habilitadas.
- **Stops**: verificaciones simétricas de stop-loss y take-profit para operaciones cortas.

El parámetro `SignalBar` define cuántas velas completamente cerradas se omiten antes de evaluar las señales. Un valor de `1` reproduce el comportamiento de MetaTrader de usar la vela completada anterior.

## Gestión monetaria (MMRec)
- Dos colas rastrean los resultados de operaciones más recientes para largos y cortos.
- `TotalTrigger` limita la longitud de la cola; solo se consideran los últimos N resultados.
- `LossTrigger` define cuántas pérdidas dentro de esa cola cambian el tamaño de la operación a `SmallVolume`.
- Cuando no se supera el umbral de pérdidas, la estrategia usa `NormalVolume`.

## Parámetros
| Grupo | Parámetro | Descripción | Por defecto |
| --- | --- | --- | --- |
| Long | `LongCandleType` | Tipo de vela (marco temporal) para cálculos largos. | Marco temporal de 8 horas |
| Long | `LongFastLength` | Longitud Jurik rápida en la aproximación JCCX larga. | 23 |
| Long | `LongSlowLength` | Longitud Jurik lenta para la aproximación JCCX larga. | 50 |
| Long | `LongSmoothLength` | Longitud de suavizado Jurik aplicada al numerador/denominador. | 8 |
| Long | `LongPhase` | Parámetro de fase traducido en factor de suavizado (0.05–0.95). | 100 |
| Long | `LongCycle` | Longitud de ventana deslizante para las transformaciones estocásticas. | 10 |
| Long | `LongSignalBar` | Retardo (en barras) antes de evaluar una señal. | 1 |
| Long | `LongAppliedPrice` | Fuente de precio para cálculos largos. | Close |
| Long | `LongAllowOpen` / `LongAllowClose` | Habilitar/deshabilitar entradas o salidas largas. | true |
| Long | `LongTotalTrigger` | Número de operaciones largas recientes almacenadas para la cola MMRec. | 5 |
| Long | `LongLossTrigger` | Pérdidas requeridas en la cola para cambiar a volumen pequeño. | 3 |
| Long | `LongSmallVolume` / `LongNormalVolume` | Tamaños de operación larga reducido y predeterminado. | 0.01 / 0.1 |
| Long | `LongStopLoss` / `LongTakeProfit` | Distancias opcionales de stop/take en pasos de precio. | 1000 / 2000 |
| Short | Igual que largo (con prefijo `Short`). | | |

## Notas de riesgo
- Los pasos de precio se obtienen del `Security` actual. Asegúrese de que el instrumento tenga un `PriceStep` válido o ajuste los parámetros en consecuencia.
- Las verificaciones de stop-loss y take-profit se evalúan en velas completadas; la calidad de ejecución intrabarra depende de la resolución de la vela.
- El módulo MMRec depende de la comparación de precios de entrada y salida. En trading en vivo, el deslizamiento puede alterar el resultado efectivo.

## Consejos de uso
- Comience con configuraciones idénticas de largo/corto para emular el EA duplex original, luego experimente con marcos temporales asimétricos.
- Reduzca `SignalBar` a cero para respuestas más rápidas; auméntelo para filtrar ruido.
- Optimice `LongPhase`/`ShortPhase` junto con las longitudes de suavizado para ajustar la capacidad de respuesta frente a la suavidad.
