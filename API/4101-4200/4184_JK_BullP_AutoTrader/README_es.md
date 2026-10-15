# Estrategia JK BullP AutoTrader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
JK BullP AutoTrader es un asesor experto que sigue el impulso escrito originalmente para MetaTrader 4. Supervisa el poder de Elder Bulls
indicador y reacciona cuando la presión alcista se debilita o se vuelve negativa. El puerto StockSharp mantiene la lógica sencilla del
original al tiempo que proporciona parámetros explícitos, gestión de seguimiento detallada y controles de riesgo compatibles con la plataforma.

## Lógica comercial
1. La estrategia se suscribe a una serie de velas configurables (velas de 1 hora por defecto) y calcula una exponencial de 13 períodos.
media móvil (EMA) para replicar la línea de base de Bulls Power.
2. Para cada vela completa, Bulls Power se mide como la diferencia entre el máximo de la vela y el valor EMA.
3. Se comparan dos lecturas consecutivas de Bulls Power:
   - Si el valor anterior está por encima del valor más reciente y el valor más reciente sigue siendo positivo, la estrategia abre una posición corta.
   - Si el último valor de Bulls Power cae por debajo de cero, la estrategia abre una posición larga.
4. Solo puede haber una posición activa a la vez, reflejando al experto original MQL que bloqueaba nuevas órdenes mientras las operaciones estaban abiertas.

## Gestión de riesgos y salidas.
- **Stop-loss/take-profit inicial:** Las distancias se configuran en pips y se convierten a unidades de precio utilizando el paso del precio del valor.
Ambas protecciones se habilitan a través del asistente `StartProtection` de StockSharp, manteniendo el comportamiento cerca de las entradas de MetaTrader.
- **Parada dinámica:** Una vez que el beneficio flotante excede la distancia de seguimiento especificada, el nivel de parada se mueve vela por vela.
En lugar de modificar las órdenes stop existentes (como en MetaTrader), el puerto emite una orden de mercado para salir de la posición cuando el precio
cierra más allá del umbral final. Esto garantiza salidas oportunas incluso cuando las órdenes de protección no sean respaldadas por el lugar.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `OrderVolume` | Tamaño de orden de mercado utilizado para las entradas. | 8.5 |
| `TakeProfitPips` | Distancia de obtención de beneficios en pips (convertida a unidades de precio). | 500 |
| `StopLossPips` | Distancia de stop-loss en pips. | 20 |
| `TrailingStopPips` | Distancia de beneficio en pips que activa y mantiene el trailing stop. | 10 |
| `EmaPeriod` | Longitud del EMA utilizado por el cálculo de Bulls Power. | 13 |
| `CandleType` | Tipo de datos de velas que impulsan la estrategia (plazo de tiempo predeterminado de 1 hora). | velas de 1 hora |

## Notas de implementación
- Las entradas no utilizadas (`Patr`, `Prange`, `Kstop`, `kts`, `Vts`) del script original se omitieron intencionalmente porque tenían
ningún efecto en la lógica MetaTrader.
- Las distancias de pip dependen del instrumento `PriceStep`. Si los datos del paso no están disponibles, se utiliza un valor de `1` como valor predeterminado conservador.
- La estrategia utiliza StockSharp de alto nivel `Bind` API, procesa solo velas terminadas y mantiene el estado interno (`_previousBullsPower`)
para que coincida con los cálculos basados en turnos de MT4.
- La lógica de seguimiento se restablece automáticamente después de cada salida para evitar niveles de parada obsoletos cuando se abre una nueva posición.
