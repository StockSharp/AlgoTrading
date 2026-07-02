# EMA Estrategia de riesgo de WMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
- Conversión del asesor experto MetaTrader 4 "EMA WMA" por Vladimir Hlystov.
- Opera con reversiones de tendencia detectadas a partir de la relación entre un promedio móvil exponencial (EMA) y un promedio móvil ponderado (WMA) calculado sobre los precios de vela **apertura**.
- Adjunta automáticamente órdenes de stop-loss y take-profit idénticas al robot MT4 utilizando el asistente de protección de StockSharp.
- Admite un tamaño de posición basado en el riesgo que refleja la entrada de "riesgo" original y al mismo tiempo mantiene una opción para el comercio de volumen fijo.

## Lógica original del asesor experto
- La versión MT4 funciona con cualquier símbolo y período de tiempo, evaluando las señales una vez en una nueva barra (protegida por `TimeBar`).
- Los indicadores usan `PRICE_OPEN`, por lo que los promedios reaccionan al tic de apertura de la barra.
- Cuando EMA cae por debajo de WMA mientras anteriormente estaba por encima de él, todas las posiciones cortas se cierran y se abre una operación larga con distancias predefinidas de stop-loss y take-profit.
- Cuando EMA sube por encima de WMA después de estar por debajo de él, todas las posiciones largas se cierran y se abre una nueva posición corta.
- La entrada `risk` calcula el tamaño del lote a partir del margen disponible y la distancia del stop-loss.

## Reglas comerciales en StockSharp
1. Suscríbase a la serie de velas configuradas (`CandleType`, valor predeterminado de 30 minutos). Sólo se procesan velas terminadas para evitar repintar.
2. Introduzca los precios de apertura de las velas en los indicadores EMA y WMA. Espere hasta que se formen ambos indicadores.
3. Detecta un cruce alcista cuando el EMA anterior > WMA anterior y el EMA actual < WMA actual.
   - Cierre los cortos e ingrese una posición larga dimensionada según las reglas de riesgo.
4. Detecta un cruce bajista cuando el EMA anterior < WMA anterior y el EMA actual > WMA actual.
   - Cierre las posiciones largas e ingrese una posición corta dimensionada según las reglas de riesgo.
5. `StartProtection` crea órdenes de protección del mercado para que cada nueva operación reciba inmediatamente niveles de límite de pérdidas y obtención de ganancias expresados en incrementos de precios.

## Dimensionamiento de posiciones y control de riesgos
- **RiskPercent** emula el parámetro MT4 `risk`. El volumen se calcula a partir del capital de la cartera, la distancia de limitación de pérdidas y los valores de paso/precio de paso del valor.
- Si faltan metadatos de intercambio (sin escalón de precio o precio de escalón), el algoritmo vuelve a utilizar la distancia de parada absoluta.
- Si `RiskPercent` se establece en cero, la estrategia requiere un **OrderVolume** positivo (anulación de volumen fijo).
- La exposición opuesta existente se cierra antes de que se envíen nuevas órdenes, coincidiendo con el comportamiento MT4 de `CLOSEORDER` y luego `OPENORDER`.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `EmaPeriod` | Período de la media móvil exponencial (por defecto 28). |
| `WmaPeriod` | Período de la media móvil ponderada (por defecto 8). |
| `StopLossPoints` | Distancia de parada de pérdidas en pasos del instrumento (predeterminado 50). |
| `TakeProfitPoints` | Distancia de toma de ganancias en pasos del instrumento (por defecto 50). |
| `RiskPercent` | Porcentaje de capital a riesgo por operación (por defecto 10%). |
| `OrderVolume` | Volumen de pedido fijo; utilice 0 para habilitar el dimensionamiento basado en el riesgo. |
| `CandleType` | Tipo de datos de vela/período de tiempo utilizado para los cálculos. |

## Notas de implementación
- Los valores EMA y WMA se ingresan manualmente a través de `DecimalIndicatorValue` para garantizar que el precio de apertura se use exactamente igual que la configuración del indicador MT4.
- La estrategia se basa en velas cerradas para confirmar la señal; esto puede retrasar las entradas una barra en comparación con MT4, pero evita el sesgo de anticipación.
- Las órdenes de protección se expresan en incrementos de precios para igualar el multiplicador `Point` de MetaTrader.
- Los gráficos trazan automáticamente velas, tanto promedios móviles como marcadores comerciales cuando hay un área del gráfico disponible.
