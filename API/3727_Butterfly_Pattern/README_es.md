# Estrategia de patrón de mariposa
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia de patrón de mariposa** convierte la lógica del patrón armónico original MetaTrader "Cypher EA" al nivel alto de StockSharp API. La estrategia escanea una serie de velas configurables en busca de formaciones de mariposas alcistas y bajistas, valida los ratios armónicos y abre posiciones de mercado con tres objetivos de obtención de beneficios por etapas. Las funciones opcionales de gestión de riesgos reflejan al experto MetaTrader: el bloqueo del punto de equilibrio y las actualizaciones de trailing stop están disponibles después de salidas parciales.

## como funciona

1. Las velas se almacenan en búfer hasta que se pueda confirmar un punto de pivote mediante la ventana `PivotLeft`/`PivotRight`.
2. Cuando hay cinco pivotes alternos disponibles, la estrategia verifica las proporciones Fibonacci requeridas para un patrón de mariposa.
3. Las configuraciones calificadas se revalidan (opcional) y se evalúan mediante una puntuación de calidad armónica (`MinPatternQuality`).
4. Una vez que se confirma un patrón en una vela cerrada:
   - Una orden de mercado se coloca utilizando un volumen fijo o un tamaño basado en el riesgo.
   - El volumen de la posición se divide en tres niveles de obtención de beneficios (`TP1/TP2/TP3`).
   - Un stop-loss geométrico se deriva de la estructura del patrón.
5. Durante la vida útil de la posición, la estrategia monitorea las velas para activar salidas parciales, bloqueo de equilibrio y ajustes finales de acuerdo con los umbrales configurados.

> **Consejo:** La versión MetaTrader funciona con múltiples períodos de tiempo simultáneamente. Para replicar este comportamiento en StockSharp, inicie varias instancias de la estrategia con diferentes valores de `CandleType`.

## Parámetros clave

| Parámetro | Descripción |
| --- | --- |
| `CandleType` | Marco de tiempo utilizado para detectar pivotes y patrones. |
| `PivotLeft` / `PivotRight` | Número de velas a la izquierda/derecha necesarias para confirmar un pivote alto/bajo. |
| `Tolerance` | Desviación máxima de la relación armónica permitida al validar el patrón de mariposa. |
| `AllowTrading` | Habilita o deshabilita la generación de pedidos después de una confirmación de patrón. |
| `UseFixedVolume` / `FixedVolume` | Obliga a un volumen de comercio constante. Cuando está deshabilitada, la estrategia dimensiona las posiciones a través de `RiskPercent`. |
| `RiskPercent` | Porcentaje del valor de la cartera arriesgado por operación (se usa solo cuando `UseFixedVolume` es falso). |
| `AdjustLotsForTakeProfits` | Normaliza los volúmenes parciales para garantizar que la suma coincida con el tamaño de entrada. |
| `Tp1Percent` / `Tp2Percent` / `Tp3Percent` | Distribución del volumen total entre los tres niveles de toma de beneficios. |
| `MinPatternQuality` | Puntuación armónica mínima (0–1) necesaria para aceptar un patrón detectado. |
| `UseSessionFilter`, `SessionStartHour`, `SessionEndHour` | Restrinja el comercio a una ventana de sesión de intercambio específica. |
| `RevalidatePattern` | Fuerza una verificación secundaria del precio antes de abrir una posición. |
| `UseBreakEven`, `BreakEvenAfterTp`, `BreakEvenTrigger`, `BreakEvenProfit` | Controla la activación del punto de equilibrio después del nivel de obtención de beneficios especificado y el colchón de beneficios adicional. |
| `UseTrailingStop`, `TrailAfterTp`, `TrailStart`, `TrailStep` | Permite paradas dinámicas una vez que se ha alcanzado un nivel de toma de ganancias y se logra la excursión mínima favorable. |

## Gestión de riesgos

- Los niveles de stop loss, punto de equilibrio y seguimiento se gestionan internamente sin crear órdenes adicionales. Las salidas parciales y los cierres stop se activan con órdenes de mercado para emular la lógica MetaTrader.
- Cuando `UseFixedVolume` está deshabilitado, el tamaño de la posición se calcula a partir de la distancia de parada, el valor del tick del instrumento y la configuración de `RiskPercent`.

## Notas de uso

- Asegúrese de que el instrumento conectado admita el `CandleType` configurado y el paso de precio; de lo contrario, la lógica de validación puede rechazar señales debido a comprobaciones de distancia mínima.
- Las funciones de equilibrio y seguimiento requieren que se completen los respectivos niveles de obtención de ganancias (`BreakEvenAfterTp` y `TrailAfterTp`).
- Se pueden ejecutar varias instancias de estrategia simultáneamente en diferentes valores o períodos de tiempo para reproducir el escaneo de múltiples períodos de tiempo del EA original.
