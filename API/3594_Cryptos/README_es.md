# Estrategia de criptomonedas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia Cryptos** es una versión StockSharp de alto nivel del asesor experto MetaTrader4 original `cryptos.mq4`. Se centra en el par ETH/USD, combinando Bollinger bandas con un promedio móvil ponderado lineal (LWMA) para capturar rupturas de la compresión de la volatilidad. La estrategia rastrea los máximos y mínimos a lo largo de un número configurable de velas y calcula dinámicamente los objetivos de obtención de beneficios como un múltiplo del rango detectado.

## Lógica de trading

1. **Detección de tendencias**: cuando el precio de cierre toca la banda superior Bollinger, la estrategia cambia a un sesgo corto, y cuando se toca la banda inferior, cambia a un sesgo largo. El toque de banda también congela los valores de swing actuales al desactivar las actualizaciones automáticas de altos/bajos.
2. **Condiciones de entrada** –
   - Abra una posición corta cuando el precio de cierre caiga por debajo de la LWMA, el sesgo sea corto y no haya una posición corta activa.
   - Abra una posición larga cuando el precio de cierre suba por encima de la LWMA, el sesgo sea largo y no haya una posición larga activa.
3. **Proyección de rango**: los máximos y mínimos de oscilación (ya sea automático o congelado manualmente) definen la distancia desde el LWMA. Esta distancia, expresada en ticks, se multiplica por el índice de obtención de beneficios para calcular los objetivos de beneficios y el tamaño de la posición basada en el riesgo.
4. **Control de riesgos**: la estrategia establece niveles de obtención de beneficios y limitación de pérdidas por operación. Para posiciones largas, el stop se coloca por debajo del mínimo de oscilación; para pantalones cortos, por encima del máximo del swing. Las paradas y los objetivos se recalculan para cada entrada y se aplican dentro del ciclo de estrategia.
5. **Salidas finales**: si una posición larga se cierra por debajo de la banda inferior Bollinger (o una posición corta por encima de la banda superior), la posición abierta se aplana inmediatamente, imitando el comportamiento final de la EA original.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `CandleType` | Tipo de datos de la serie de velas utilizada para todos los cálculos del indicador. |
| `BollingerPeriod`, `BollingerWidth` | Multiplicador de longitud y desviación estándar de las Bollinger Bandas. |
| `MaPeriod` | Período de la media móvil lineal ponderada en función de los precios medianos. |
| `LookbackCandles` | Número de velas examinadas para determinar la oscilación automática alto/bajo. |
| `TakeProfitRatio` | Multiplicador de rango utilizado para objetivos de ganancias al operar con ETH/USD. |
| `AlternativeTakeProfitRatio` | Multiplicador de rango aplicado a todos los demás símbolos. |
| `RiskPerTrade` | Cantidad de capital (en moneda cotizada) que la calculadora de volumen intenta arriesgar en cada operación. |
| `ValueIndex`, `CryptoValueIndex` | Multiplicadores que convierten el riesgo en volumen para símbolos criptográficos y no criptográficos, respectivamente. |
| `MinVolume`, `MaxVolume` | Límites estrictos para el tamaño de la posición después de la alineación para intercambiar pasos de volumen. |
| `MinRangeTicks` | Rango proyectado mínimo permitido en ticks para evitar paradas de distancia cero. |
| `SpreadPoints` | Anulación manual del diferencial en ticks (detectado automáticamente desde la mejor oferta/demanda, si está disponible). |
| `GlobalTrend` | Anulación de sesgo manual: `1` fuerza una configuración corta, `2` fuerza una configuración larga, `0` deja que la estrategia decida. |
| `AutoHighLow` | Cuando está habilitado, los puntos de oscilación se recalculan en cada vela; cuando están deshabilitados, se congelan hasta el siguiente toque de banda. |
| `ManualBuyTrigger`, `ManualSellTrigger` | Establezca en `true` para solicitar una entrada larga o corta inmediata (restablecer después de la ejecución). |
| `SkipBuys`, `SkipSells` | Deshabilite la apertura de nuevas posiciones largas o cortas. |

## Dimensionamiento de posiciones

La estrategia replica la lógica MT4: `volume = RiskPerTrade / rangeTicks * valueIndex`. El resultado se alinea con `VolumeStep` y luego se recorta entre `MinVolume`/`MaxVolume` y los límites impuestos por el intercambio del instrumento.

## Notas de uso

- La estrategia verifica el valor de la cartera al inicio. Si el saldo es inferior a `RiskPerTrade * 3`, el comercio se desactiva y se registra una advertencia que coincide con la verificación de seguridad de EA.
- Los activadores manuales y los controles de sesgo hacen posible la sincronización con decisiones discrecionales durante las operaciones en vivo.
- ETH/USD utiliza automáticamente `CryptoValueIndex` y `TakeProfitRatio`; otros instrumentos recurren a los parámetros alternativos.
- Las paradas y los objetivos se aplican dentro del bucle de estrategia, por lo que no se requiere ningún módulo de protección adicional.
