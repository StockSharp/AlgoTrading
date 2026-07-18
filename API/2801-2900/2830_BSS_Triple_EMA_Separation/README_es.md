# Estrategia BSS de Separación Triple EMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción General

La **Estrategia BSS de Separación Triple EMA** es un puerto de StockSharp del asesor experto de MetaTrader 5 "BSS 1_0" (MQL ID 20591). El enfoque monitorea tres medias móviles con ventanas de búsqueda crecientes y espera a que se expandan al menos por una distancia configurable. Cuando las medias rápida, media y lenta están correctamente separadas, la estrategia entra en la dirección de la tendencia respetando un período de enfriamiento entre rellenos y un límite en el tamaño total de la posición.

Esta implementación mantiene el comportamiento central del robot original mientras expone la configuración a través de objetos `StrategyParam` de StockSharp. Todos los comentarios y la documentación están escritos en inglés según lo solicitado.

## Lógica de Trading

1. Suscribirse a un único flujo de velas definido por el parámetro `CandleType` y calcular tres medias móviles (rápida, media, lenta). Cada media puede usar un método de suavizado diferente (simple, exponencial, suavizado o ponderado linealmente).
2. Para una **configuración larga** deben cumplirse las siguientes condiciones en una vela terminada:
   - `MA Lenta - MA Media >= MinimumDistance`.
   - `MA Media - MA Rápida >= MinimumDistance`.
3. Para una **configuración corta** se requiere la separación inversa:
   - `MA Rápida - MA Media >= MinimumDistance`.
   - `MA Media - MA Lenta >= MinimumDistance`.
4. Antes de abrir una operación la estrategia garantiza:
   - Todos los indicadores están completamente formados y la estrategia puede operar (`IsFormedAndOnlineAndAllowTrading`).
   - La pausa desde la última entrada (`MinimumPauseSeconds`) ha transcurrido.
   - Agregar un nuevo lote no violará el límite de exposición `MaxPositions`.
5. En una señal de entrada, la estrategia primero cierra cualquier posición abierta en la dirección opuesta. Solo después de la siguiente vela considera abrir una posición en la nueva dirección, reflejando el comportamiento del EA MQL original.
6. Cuando se abre una nueva posición o se escala, se almacena el tiempo de relleno para aplicar el enfriamiento entre entradas.

No se usa stop-loss ni take-profit automáticos. La gestión de riesgo se logra a través del filtro de distancia, la pausa entre operaciones y el número máximo de lotes permitidos por dirección.

## Parámetros

| Parámetro | Predeterminado | Descripción |
|-----------|---------|-------------|
| `OrderVolume` | 0.1 | Volumen usado para cada orden de entrada. La posición neta está limitada a `OrderVolume * MaxPositions`. |
| `MaxPositions` | 2 | Número máximo de lotes (por dirección) que pueden mantenerse simultáneamente. |
| `MinimumDistance` | 0.0005 | Brecha de precio mínima requerida entre medias móviles vecinas. Elija un valor apropiado para el instrumento (para un par FX de 5 dígitos, 0.0005 equivale a 5 pips). |
| `MinimumPauseSeconds` | 600 | Enfriamiento en segundos entre nuevas entradas. Cerrar operaciones no reinicia el temporizador; solo lo hacen las entradas. |
| `FirstMaPeriod` | 5 | Período de la media móvil más rápida. Debe ser estrictamente menor que `SecondMaPeriod`. |
| `FirstMaMethod` | Exponential | Método de suavizado usado para la media móvil rápida (Simple, Exponential, Smoothed, LinearWeighted). |
| `SecondMaPeriod` | 25 | Período de la media móvil media. Debe ser estrictamente menor que `ThirdMaPeriod`. |
| `SecondMaMethod` | Exponential | Método de suavizado usado para la media móvil media. |
| `ThirdMaPeriod` | 125 | Período de la media móvil lenta. |
| `ThirdMaMethod` | Exponential | Método de suavizado usado para la media móvil lenta. |
| `CandleType` | Marco temporal de 1 minuto | Fuente de datos de velas usada para los cálculos del indicador y la evaluación de señales. |

## Notas de Implementación

- Se usa la API de alto nivel de StockSharp: `SubscribeCandles` transmite datos, y `.Bind` alimenta las medias móviles y el manejador de señales simultáneamente.
- Las medias móviles se instancian al inicio de la estrategia según los métodos seleccionados. La configuración predeterminada coincide con el EA original (tres EMA exponenciales sobre precios de cierre).
- `StartProtection()` se invoca para habilitar las herramientas de monitoreo de posición integradas proporcionadas por StockSharp.
- La estrategia anula `OnPositionChanged` para marcar el tiempo de las entradas. Esta marca de tiempo se compara con `MinimumPauseSeconds` para mantener el comportamiento de enfriamiento de la versión de MetaTrader.
- Las posiciones opuestas se aplanan antes de considerar nuevas, asegurando que la exposición neta nunca cambie de signo sin pasar primero por cero, igual que la implementación original donde todas las posiciones cortas se cerraban antes de abrir largos.

## Directrices de Uso

1. Seleccione un instrumento y asegúrese de que su tamaño de tick se refleje en el valor de `MinimumDistance`. Por ejemplo:
   - EURUSD (precios de 5 dígitos): `0.0005` equivale a 5 pips.
   - USDJPY (precios de 3 dígitos): `0.05` equivale a 5 pips.
2. Ajuste los períodos y métodos de la media móvil para adaptarse al régimen de mercado que está atacando.
3. Aumente `MinimumPauseSeconds` en marcos temporales más lentos para evitar el sobretrading, o disminúyalo en marcos temporales inferiores si la estructura del mercado permite entradas frecuentes.
4. Pruebe diferentes valores de `MaxPositions` en combinación con el tamaño del contrato de su broker para alinear la exposición con su plan de riesgo.

## Limitaciones Comparado con la Versión MQL

- El experto de MetaTrader permitía seleccionar fuentes de precio alternativas (apertura, máximo, mínimo, etc.). El puerto de StockSharp actualmente opera solo en precios de cierre, lo que coincide con la configuración predeterminada del robot original.
- El puerto usa un modelo de posición neta (positivo para largos, negativo para cortos). Cuando se alcanza `MaxPositions`, no se añaden lotes adicionales hasta que la exposición se reduzca, reproduciendo el efecto del contador de posición por elemento original.

Con estas consideraciones puede reproducir el comportamiento de la estrategia BSS original dentro del ecosistema StockSharp y extenderla con controles de riesgo o análisis adicionales según sea necesario.
