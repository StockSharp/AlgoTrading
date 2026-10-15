# Scalper EMA Estrategia simple
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **estrategia simple del revendedor EMA** es una conversión del MetaTrader asesor experto `ScalperEMAEASimple`. Utiliza una combinación de promedios móviles exponenciales rápidos/lentos, un oscilador estocástico y un filtro de índice direccional promedio (ADX) para identificar entradas de retroceso de corta duración dentro de una tendencia existente. La estrategia está diseñada para el especulación intradía en pares de divisas líquidos, pero se puede aplicar a cualquier instrumento donde la gestión de riesgos basada en pips tenga sentido.

La implementación sigue la API de alto nivel de StockSharp y evalúa solo las velas terminadas. Todos los cálculos se realizan de forma incremental sin reprocesar datos históricos, lo que hace que la lógica sea adecuada para operaciones reales.

## Pila de indicadores

- **EMA rápida (`FastEmaPeriod`)**: detecta el impulso a corto plazo.
- **EMA lenta (`SlowEmaPeriod`)** – define la dirección de la tendencia predominante.
- **Stochastic Oscilador (`StochasticLength`, `StochasticKPeriod`, `StochasticDPeriod`)**: rastrea las reversiones de impulso cerca de los límites de sobreventa/sobrecompra.
- **Índice direccional promedio**: rechaza operaciones cuando la tendencia se vuelve excesivamente fuerte (ADX por encima de `AdxThreshold`).

El oscilador estocástico dispara una señal de confirmación cada vez que la línea %K vuelve a cruzar por encima del nivel de sobreventa (configuraciones largas) o por debajo del nivel de sobrecompra (configuraciones cortas). El par EMA proporciona el filtro direccional y el componente ADX garantiza que las entradas se limiten a retrocesos tranquilos en lugar de tendencias desbocadas.

## Lógica de entrada

1. La vela debe cerrar en el lado de la tendencia del EMA lento y el EMA rápido debe coincidir con esa dirección (`fast > slow` para largos, `fast < slow` para cortos).
2. La distancia entre la vela y el EMA lento debe ser menor que el rango de la vela y más estrecha que las tres distancias anteriores. Este comportamiento recrea el bucle de detección de retroceso a partir del código MQL original.
3. O el cuerpo de la vela cruza el EMA rápida o el EMA rápida cruza el EMA lenta. Esta condición actúa como desencadenante de la ruptura.
4. El oscilador estocástico debe confirmar el impulso cruzando desde la zona extrema dentro de las últimas `ConditionWindowBars` velas.
5. ADX debe permanecer por debajo de `AdxThreshold`, evitando operaciones cuando la volatilidad se acelera bruscamente.
6. Al menos `SignalCooldownBars` velas deben pasar entre dos señales consecutivas de la misma dirección.

Cuando pasan todas las comprobaciones, la estrategia cierra cualquier exposición opuesta y abre una nueva orden de mercado en la dirección detectada.

## Lógica de salida y controles de riesgo

- Se coloca un stop-loss inicial a `StopLossPips` (convertido a precio utilizando el tamaño del pip del instrumento) desde el precio de entrada.
- Un trailing stop mantiene automáticamente una distancia de `TrailingDistancePips` una vez que el beneficio no realizado alcanza `TrailingActivationPips`.
- Las señales opuestas obligan a una posición plana antes de establecer una nueva operación.

Todas las órdenes de protección se administran a través del asistente `SetStopLoss` de StockSharp para mantener los controles de riesgo sincronizados con el volumen de posición actual.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `Volume` | Volumen de negociación base para cada señal. La estrategia agrega automáticamente la exposición existente para garantizar una reversión total al cambiar de dirección. |
| `FastEmaPeriod` / `SlowEmaPeriod` | Duraciones de los períodos para las medias móviles exponenciales. |
| `StochasticLength`, `StochasticKPeriod`, `StochasticDPeriod` | Configuración del oscilador Stochastic que refleja los valores predeterminados originales de EA. |
| `StochasticOversold` / `StochasticOverbought` | Niveles extremos que definen las zonas de retroceso. |
| `AdxThreshold` | Valor máximo ADX permitido antes de rechazar operaciones. |
| `SignalCooldownBars` | Barras mínimas entre señales sucesivas en la misma dirección. |
| `ConditionWindowBars` | Número de barras durante las cuales deben alinearse el retroceso, la ruptura de EMA y la confirmación estocástica. |
| `StopLossPips` | Distancia inicial de stop-loss expresada en pips. |
| `TrailingDistancePips` | Distancia mantenida por el trailing stop una vez activado. |
| `TrailingActivationPips` | Umbral de beneficio que arma el trailing stop. |
| `CandleType` | Serie de velas utilizadas para todos los indicadores. El valor predeterminado es un período de tiempo de 5 minutos. |

## Notas de implementación

- Las conversiones de pips dependen del instrumento `PriceStep`. Para instrumentos de 3 o 5 decimales, el factor de pip se multiplica por diez, cumpliendo con las convenciones MetaTrader.
- La estrategia solo procesa velas terminadas, por lo que la ejecución se produce después del cierre de cada barra.
- Las variables de estado internas almacenan los últimos índices de retroceso, ruptura EMA y confirmaciones estocásticas para reproducir las ventanas retrospectivas utilizadas por el asesor experto original sin escanear todo el historial.

## Uso

1. Adjunte la estrategia a una instancia `Connector` o `Trader` con una seguridad y una cartera configuradas.
2. Asegúrese de que el valor tenga un `PriceStep` válido para la conversión de pip a precio.
3. Ajustar parámetros según la volatilidad del instrumento. El valor predeterminado de EMA lento es 740 para coincidir con la fuente EA, pero los mercados más rápidos pueden beneficiarse de configuraciones más cortas.
4. Inicia la estrategia. Las órdenes de mercado y de protección se generarán automáticamente cuando se cumplan las condiciones descritas anteriormente.

> **Descargo de responsabilidad**: esta estrategia se transfirió con fines educativos. Se recomiendan pruebas exhaustivas y análisis de riesgos antes de negociar con capital real.
