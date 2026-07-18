# Estrategia Trailing Stop FrCnSar
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Trailing Stop FrCnSar traslada el kit de herramientas MetaTrader enviado como **TrailingStopFrCnSARen_v4.mq4** y **OrderBalansEN_v3_4.mq4**. El asesor experto gestionó las posiciones existentes ajustando sus límites de pérdidas utilizando varias técnicas (velas anteriores, fractales, velocidad del precio o Parabolic SAR), mientras que el indicador complementario mostraba el saldo de la cuenta actual y las órdenes abiertas. La conversión StockSharp se centra en posiciones netas y vuelve a implementar la lógica final con primitivas API de alto nivel. También proporciona un registrador de resumen de pedidos opcional para que la superposición informativa del indicador original permanezca disponible en forma textual.

La estrategia no abre nuevas operaciones automáticamente. En cambio, observa continuamente la posición actual en `Strategy.Security`, actualiza el nivel de trailing stop deseado de acuerdo con el modo seleccionado y los filtros definidos por el usuario, y cierra la exposición una vez que el precio alcanza la barrera de trailing. Debido a que StockSharp trabaja con posiciones netas en lugar de tickets discretos, todos los cálculos se aplican a la cantidad agregada.

## Lógica comercial
1. Suscríbase al `CandleType` configurado y procese solo velas terminadas para evitar ajustes de parada prematuros.
2. Mantenga buffers móviles cortos con máximos y mínimos de velas para que los fractales y los extremos recientes puedan recuperarse sin llamar a métodos de indicadores prohibidos.
3. Opcionalmente, calcule una velocidad de cierre a cierre suavizada en puntos cuando el modo de seguimiento de velocidad está activo.
4. Para cada vela completa, genere el precio de trailing stop candidato según el modo seleccionado:
   - Mínimo más bajo del historial reciente de velas menos el desplazamiento `DeltaPoints`.
   - Último fractal confirmado ajustado por `DeltaPoints`.
   - Precio de cierre desplazado una distancia dependiente de la velocidad.
   - Valor actual de Parabolic SAR compensado por `DeltaPoints`.
   - Una distancia fija expresada en puntos del instrumento.
5. Verifique al candidato con filtros de administración de dinero: requiera paradas existentes, permita solo seguimiento rentable, deténgase una vez que se alcance el punto de equilibrio o base la prueba de ganancias en el precio de entrada promedio.
6. Reemplace el nivel de parada almacenado cuando el candidato mejore el existente en al menos `StepPoints`.
7. Si la vela cruza el nivel almacenado (mínimo para largos, máximo para cortos) y se permite la negociación, cierre la posición neta con una orden de mercado.
8. Opcionalmente, registre un resumen textual con saldo, tamaño de posición, precio de entrada, stop actual y PnL no realizado, emulando el indicador OrderBalans MetaTrader.

## Modos de seguimiento
- **Vela** – está detrás del extremo de vela significativo más reciente. Las compensaciones se aplican a través de `DeltaPoints` para mantener el tope ligeramente alejado del soporte/resistencia.
- **Fractal**: utiliza el último fractal de cinco barras detectado en el período de tiempo procesado. Esto imita la implementación predeterminada de MetaTrader pero opera en posiciones netas.
- **Velocidad**: estima la velocidad del precio promediando los cambios cercanos al cierre durante `VelocityPeriod`. Cuando el impulso se acelera en la dirección de la posición, el tope se aprieta proporcionalmente a la diferencia de velocidad escalada por `VelocityMultiplier`.
- **Parabolic** – sigue el indicador Parabolic SAR administrado por StockSharp. La parada abraza los puntos SAR y hereda los parámetros de paso y aceleración máxima.
- **Puntos fijos**: impone una distancia constante respecto del precio, reflejando efectivamente el comportamiento de “>4 pips” del script original.
- **Desactivado**: desactiva el seguimiento y mantiene intacta la parada actual.

## Parámetros
| Nombre | Tipo | Predeterminado | Descripción |
| --- | --- | --- | --- |
| `Mode` | `TrailingStopMode` | `Candle` | Determina qué algoritmo de seguimiento está activo. |
| `CandleType` | `DataType` | velas de 15 minutos | Marco de tiempo utilizado para analizar velas y calcular datos finales. |
| `DeltaPoints` | `int` | `0` | Distancia adicional (en puntos del instrumento) agregada por debajo o por encima del precio final bruto. |
| `StepPoints` | `int` | `0` | Mejora mínima, en puntos, requerida antes de actualizar un trailing stop existente. |
| `FixedDistancePoints` | `int` | `50` | Distancia para el modo de seguimiento fijo. Ignorado por otros modos. |
| `TrailOnlyProfit` | `bool` | `true` | Cuando `true`, el seguimiento comienza solo después de que la parada terminaría en ganancias en relación con el precio de entrada. |
| `TrailOnlyBreakEven` | `bool` | `false` | Deje de actualizar una vez que la parada almacenada haya superado el punto de equilibrio. |
| `RequireExistingStop` | `bool` | `false` | Ignore las actualizaciones finales hasta que ya se haya calculado un nivel de parada. |
| `UseGeneralBreakEven` | `bool` | `false` | Evalúe el filtro de rentabilidad utilizando el precio de entrada promedio de la posición neta (equivalente al ayudante `TProfit` original). |
| `VelocityPeriod` | `int` | `30` | Número de cierres utilizados para promediar la velocidad en el modo de velocidad. |
| `VelocityMultiplier` | `decimal` | `1` | Escala el ajuste de velocidad aplicado a la distancia de seguimiento. |
| `ParabolicStep` | `decimal` | `0.02` | Paso de aceleración para el indicador Parabolic SAR. |
| `ParabolicMaximum` | `decimal` | `0.2` | Aceleración máxima para el indicador Parabolic SAR. |
| `LogOrderSummary` | `bool` | `true` | Habilita el registro textual similar al panel OrderBalans. |
| `TradeVolume` | `decimal` | `1` | Volumen predeterminado utilizado al aplanar posiciones mediante métodos auxiliares. |

## Diferencias con los scripts originales MetaTrader
- La conversión funciona con StockSharp posiciones netas en lugar de tickets individuales. Por lo tanto, las actualizaciones de detención se aplican a toda la posición, independientemente de cómo se creó.
- Se eliminaron los filtros de números mágicos y multisímbolos. La estrategia monitorea solo `Strategy.Security` y asume que el tamaño de la posición se maneja externamente.
- El indicador personalizado MetaTrader `Velocity` se aproxima mediante una diferencia cercana a cercana promedio medida en puntos del instrumento. Esto mantiene el comportamiento intuitivo, pero es posible que no coincida exactamente con el indicador propietario.
- Los objetos de los gráficos visuales (líneas de tendencia, flechas, etiquetas) fueron reemplazados por entradas de registro textuales. El parámetro `LogOrderSummary` recrea el panel informativo producido por *OrderBalansEN_v3_4.mq4* sin depender del dibujo manual del gráfico.
- Detener modificaciones utiliza StockSharp métodos auxiliares (`BuyMarket`, `SellMarket`) porque la plataforma no expone un equivalente directo al `OrderModify` de MetaTrader en tickets individuales.

## Consejos de uso
- Adjunte la estrategia a un gráfico para visualizar el efecto de cada modo de seguimiento. Para Parabolic SAR, habilite el área del gráfico para ver puntos y operaciones simultáneamente.
- Ajuste `DeltaPoints` y `StepPoints` según el tamaño del tick del instrumento. La implementación convierte puntos automáticamente usando `Security.PriceStep` o `Security.MinPriceStep`.
- Mantenga `TrailOnlyProfit` habilitado al imitar el comportamiento original, ya que el script MetaTrader evitaba apretar las paradas antes de que las posiciones se volvieran rentables.
- Desactive `LogOrderSummary` si prefiere una salida más silenciosa o si está ejecutando cientos de estrategias simultáneamente.
- Pruebe el modo de velocidad con diferentes valores de `VelocityMultiplier`; Los multiplicadores más altos hacen que el trailing stop reaccione más rápido a ráfagas repentinas de impulso.

## Indicadores
- Parabolic SAR (`ParabolicSar`)
- Máximos y mínimos de velas móviles (buffers de datos nativos)
- Velocidad promedio opcional de cierre a cierre derivada del cierre de velas
