# Dos MA Otro marco temporal Estrategia de intersección correcta
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es un puerto StockSharp del asesor experto MetaTrader 5 "Intersección correcta de otro marco temporal de dos MA". El EA original se basa en dos promedios móviles, cada uno de los cuales se calcula en su propio período de tiempo (por ejemplo, H1 frente a D1), mientras que las decisiones comerciales se sincronizan con el período de tiempo del gráfico. La conversión mantiene el comportamiento de múltiples períodos de tiempo y abre posiciones largas cuando la media móvil rápida cruza por encima de la media móvil lenta. Por el contrario, las posiciones cortas se abren cuando el promedio rápido cruza por debajo del lento. Todas las órdenes se ejecutan al precio de mercado y la estrategia siempre cierra cualquier exposición opuesta antes de abrir una nueva operación, coincidiendo con el modelo de ejecución impulsado por el motor del script MQL5.

## Lógica comercial
- Suscríbase a tres flujos de velas: el marco temporal de negociación principal, el marco temporal de MA rápida y el marco de tiempo de MA lenta.
- Calcule los promedios móviles rápido y lento en sus períodos de tiempo dedicados. Cada media móvil admite los mismos métodos de suavizado y fuentes de precios que expuso el indicador `iCustom` original.
- Opcionalmente, aplique un desplazamiento horizontal configurable a las salidas de promedio móvil antes de compararlas, reproduciendo las entradas `ma_shift` del EA.
- Cada vez que finaliza una vela en el período de tiempo de negociación principal, verifique si hay un cruce entre los valores de promedio móvil más recientes y anteriores:
  - Si la MA rápida estaba por debajo de la MA lenta en el paso anterior y ahora está por encima de ella, cierre cualquier posición corta y abra (o invierta) una posición larga.
  - Si la MA rápida estaba por encima de la MA lenta en el paso anterior y ahora está por debajo de ella, cierre cualquier posición larga y abra (o invierta) una posición corta.
- Todas las entradas utilizan el volumen comercial configurado. Al revertir una posición existente, la estrategia aumenta el tamaño de la orden en la magnitud de la exposición opuesta para garantizar que la posición cambie en una única orden de mercado.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `TradeVolume` | Volumen base para entradas al mercado. Se utiliza tanto para operaciones largas como cortas. |
| `CandleType` | Plazo de negociación principal. Las señales se evalúan cada vez que se cierra una vela de este tipo. |
| `FastTimeFrame` | Marco de tiempo utilizado para construir la media móvil rápida. |
| `SlowTimeFrame` | Plazo utilizado para construir la media móvil lenta. |
| `FastLength` | Número de barras incluidas en la media móvil rápida. |
| `SlowLength` | Número de barras incluidas en la media móvil lenta. |
| `FastShift` | Desplazamiento horizontal aplicado a la producción promedio móvil rápido antes de la comparación. |
| `SlowShift` | Desplazamiento horizontal aplicado a la producción promedio móvil lento antes de la comparación. |
| `FastMethod` | Algoritmo de suavizado para la media móvil rápida (simple, exponencial, suavizado o lineal ponderado). |
| `SlowMethod` | Algoritmo de suavizado para la media móvil lenta. |
| `FastAppliedPrice` | Precio de vela utilizado por el promedio móvil rápido (apertura, máximo, mínimo, cierre, mediana, típico o ponderado). |
| `SlowAppliedPrice` | Precio de vela utilizado por la media móvil lenta. |

## Notas de implementación
- Los promedios móviles se procesan a través de StockSharp suscripciones de alto nivel (`SubscribeCandles().Bind(...)`) y continúan ejecutándose incluso cuando el período de negociación difiere del período de cálculo.
- Los parámetros de cambio se implementan con pequeñas colas que retrasan la salida del indicador en el número de barras solicitado, replicando el comportamiento de las entradas `ma_shift`.
- La estrategia utiliza `StartProtection()` para alinearse con StockSharp utilidades de protección de cuentas, al igual que el motor comercial original que protegía las posiciones abiertas.
- La representación del gráfico agrega las velas principales junto con los promedios móviles rápido y lento para que las señales cruzadas permanezcan visibles durante las pruebas retrospectivas.
- No hay ningún módulo de stop-loss, take-profit o trailing-stop en el EA original. Los operadores pueden combinar este módulo con estrategias separadas de administración de dinero si se requiere un control de riesgo adicional.
