# FuturoPatrónMemoriaEstrategia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
`FuturePatternMemoryStrategy` es una adaptación StockSharp de los clásicos MetaTrader expertos **FutureMA** y **FutureMACD**. Los robots originales registraron secuencias de diferencias de indicadores en archivos CSV, reutilizaron las estadísticas almacenadas y decidieron si las condiciones actuales favorecían las rupturas alcistas o bajistas. Esta versión de C# mantiene la misma idea, pero reemplaza el sistema de archivos con un almacén de patrones en memoria y expone cada perilla como un parámetro de estrategia. La estrategia puede operar en el diferencial de media móvil suavizado (la lógica FutureMA) o en el histograma MACD (la lógica FutureMACD).

La estrategia evalúa cada vela terminada en cinco etapas:

1. **Proyección del indicador**: calcula el oscilador seleccionado (expansión MA o histograma MACD) y normalízalo con un factor de escala configurable. Los valores se discretizan a números enteros para crear firmas de patrones compactos.
2. **Hashing de patrón**: mantiene una ventana deslizante de los últimos valores normalizados `AnalysisBars`. Cada vez que se cierra una nueva barra, la ventana se convierte en una cadena hash única que identifica el contexto actual del mercado.
3. **Análisis de oscilación histórica**: inspeccione las velas `FractalDepth` anteriores, mida la distancia entre la apertura más antigua y los extremos más alto/más bajo, y convierta esos rangos en puntos. Estas distancias son las expectativas de recompensa que los robots originales acumularon en sus archivos CSV.
4. **Actualización de memoria ponderada**: la clave hash se utiliza para recuperar o crear una entrada en el diccionario de patrones. Las expectativas de toma de ganancias alcistas y bajistas se actualizan con una media móvil ponderada controlada por `ForgettingFactor`, que reproduce el coeficiente de "olvido" (`zabyvaemost`) del código MQL.
5. **Evaluación y ejecución de señales**: si domina la expectativa alcista, el patrón se vio más de `MinimumMatches` veces y la ganancia proyectada es superior a `MinimumTakeProfit`, la estrategia ingresa o agrega una posición larga. La rama bajista funciona simétricamente. Los niveles de protección se derivan de las estadísticas almacenadas y, opcionalmente, se siguen a medida que el comercio se mueve a favor.

## Notas de conversión
- Ambos expertos MetaTrader se fusionan en una estrategia configurable a través del parámetro `Source`, lo que le permite cambiar entre el motor basado en MA y el motor basado en MACD sin necesidad de volver a compilar.
- La persistencia basada en archivos se reemplazó con `Dictionary<string, PatternStats>` que mantiene todas las estadísticas en la memoria durante la ejecución. Esto evita la E/S de archivos y se mantiene dentro del modelo de entorno limitado StockSharp.
- La gestión de posiciones replica la ubicación original de stop/objetivo: el stop utiliza el swing promediado completo, mientras que la toma de ganancias utiliza `StatisticalTakeRatio` (el `Stat_Take_Profit` original). Cuando `EnableTrailingStop` es verdadero, el stop se mueve en cuartos de la distancia de ganancia, exactamente como el experto MQL modificó sus órdenes.
- El modo manual (`ManualMode`) deshabilita la realización automática de pedidos, pero continúa recopilando estadísticas, coincidiendo con el comportamiento del indicador original `Ruchnik`.
- La ampliación (`AllowAddOn`) imita la bandera `dokupka` y permite que la estrategia agregue volumen cada vez que el patrón se repite en una nueva barra.

## Lógica comercial en detalle
- **Fuente del indicador**
  - *MA spread*: calcula dos promedios móviles suavizados (SMMA 6 y SMMA 24) sobre los precios medianos y utiliza su diferencia.
  - *MACD histograma*: calcula la diferencia entre la línea principal MACD y la línea de señal (configuración por defecto 26/12/9).
- **Normalización**: `NormalizationFactor` reproduce `tocnost`; escala la diferencia bruta antes de convertirla en una firma entera. La conversión se divide por `100 * MinPriceStep` para mantener unidades basadas en pips.
- **Memoria de patrón**: el diccionario almacena, para cada firma, el número de coincidencias alcistas, la distancia alcista promedio, el número de coincidencias bajistas y la distancia bajista promedio. Los valores se actualizan con la fórmula ponderada `(current + input * ForgettingFactor) / (1 + ForgettingFactor)`.
- **Reglas de entrada**:
  - Largo: expectativa alcista ≥ expectativa bajista, coincidencias alcistas > `MinimumMatches`, distancia alcista esperada > `MinimumTakeProfit`.
  - Corto: expectativa bajista ≥ expectativa alcista, coincidencias bajistas > `MinimumMatches`, distancia bajista esperada > `MinimumTakeProfit`.
- **Gestión de riesgos**: el stop-loss se establece en un swing promedio completo contra la posición; la toma de ganancias usa `StatisticalTakeRatio` de esa oscilación. Los stop dinámicos se mueven después de que el precio recorre una cuarta parte de la distancia, al igual que la rutina de seguimiento original.

## Parámetros
| Parámetro | Descripción | Predeterminado |
|-----------|-------------|---------|
| `CandleType` | Periodo principal utilizado para los cálculos. | velas de 30 minutos |
| `Source` | Elija entre extensión MA (`FutureMA`) y MACD histograma (`FutureMACD`). | `MaSpread` |
| `FastMaLength` / `SlowMaLength` | Longitudes de SMMA cuando `Source = MaSpread`. | 6 / 24 |
| `MacdFastLength` / `MacdSlowLength` / `MacdSignalLength` | MACD períodos en los que `Source = MacdHistogram`. | 12 / 26 / 9 |
| `AnalysisBars` | Número de barras que forman la firma del patrón. | 8 |
| `FractalDepth` | Número de velas pasadas utilizadas para medir las distancias de ruptura. | 4 |
| `MinimumMatches` | Número requerido de ocurrencias almacenadas antes de realizar una operación. | 5 |
| `MinimumTakeProfit` | Distancia mínima esperada (en puntos) para aceptar la señal. | 30 |
| `NormalizationFactor` | Factor de escala aplicado a la diferencia del indicador. | 10 |
| `ForgettingFactor` | Peso aplicado a nuevas medidas en la memoria de patrones. | 1.5 |
| `StatisticalTakeRatio` | Relación de obtención de beneficios en relación con la oscilación medida. | 0,5 |
| `EnableTrailingStop` | Habilita la lógica de trailing stop de un cuarto de paso. | `false` |
| `ManualMode` | Recopile estadísticas pero omita la realización de pedidos. | `false` |
| `AllowAddOn` | Permitir la ampliación cuando se repite un patrón. | `true` |
| `Volume` | Tamaño del pedido utilizado para las entradas. | 0.1 |

## Consejos prácticos
- La estrategia se basa en hashes discretizados, así que elija `NormalizationFactor` y `AnalysisBars` con cuidado: los valores demasiado grandes producen firmas dispersas, mientras que los valores demasiado pequeños combinan estados distintos.
- Cuando ejecute operaciones comerciales en vivo, considere exportar el diccionario de patrones después de la sesión si necesita persistencia entre ejecuciones.
- Debido a que la versión MQL almacenaba datos por símbolo/período, se recomienda mantener una instancia de estrategia dedicada por instrumento/período de tiempo para evitar la contaminación cruzada de las estadísticas.
