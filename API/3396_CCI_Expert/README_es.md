# CCI Estrategia experta
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Esta estrategia es una conversión StockSharp del robot MetaTrader "CCI-Expert" original. Utiliza el indicador Commodity Channel Index (CCI) en un único período de tiempo y mantiene la lógica estrictamente secuencial: la estrategia espera tres velas completas antes de decidir abrir o cerrar una posición.

## Lógica de trading

1. Suscríbete a la serie de velas configuradas y calcula un CCI con el periodo elegido.
2. Evalúe los últimos tres valores CCI terminados:
   - **Configuración larga**: los valores CCI actual y anterior están por encima de `+1`, mientras que el segundo valor anterior estaba por debajo de `+1`.
   - **Configuración breve**: los valores CCI actual y anterior están por debajo de `+1`, mientras que el segundo valor anterior estaba por encima de `+1`.
3. Abra solo una posición de mercado a la vez cuando no haya ninguna posición activa y el filtro de diferencial permita operar.
4. Cierre una posición existente solo si aparece la señal opuesta **y** la operación ya es rentable (el precio de cierre es mejor que el precio de entrada).

## Gestión del riesgo

- La estrategia puede utilizar un lote fijo o calcular el volumen a partir del porcentaje de riesgo y la distancia de stop-loss configurada.
- `StartProtection` coloca automáticamente rangos de stop-loss y take-profit en los puntos de precio.
- Un filtro de diferencial opcional bloquea el comercio hasta que la diferencia actual entre oferta y demanda esté por debajo del umbral `MaxSpreadPoints`.

## Parámetros

| Parámetro | Descripción | Predeterminado |
|-----------|-------------|---------|
| `FixedVolume` | Tamaño de pedido fijo. Establezca en cero para activar el dimensionamiento basado en el riesgo. | 0.1 |
| `RiskPercent` | Porcentaje del valor de la cartera actual utilizado para dimensionar los pedidos cuando `FixedVolume` es cero. | 0 |
| `TakeProfitPoints` | Distancia de obtención de beneficios medida en puntos de precio. | 150 |
| `StopLossPoints` | Distancia de stop-loss medida en puntos de precio. | 600 |
| `MaxSpreadPoints` | Spread máximo permitido (en puntos de precio). Zero desactiva el filtro. | 30 |
| `CciPeriod` | Período retrospectivo del indicador CCI. | 14 |
| `CandleType` | Marco temporal de las velas procesadas por la estrategia. | velas de 15 minutos |

## Notas

- El umbral CCI permanece constante en `+1` y `-1` al igual que la fuente MQL, por lo que las operaciones se activan solo después de un patrón claro de tres pasos.
- Debido a que el tamaño del volumen basado en el riesgo depende de los metadatos del instrumento (`PriceStep`, `StepPrice`, `VolumeStep`, etc.), asegúrese de que esos valores estén disponibles en la placa conectada.
- La estrategia dibuja velas, la línea del indicador CCI y ejecuta operaciones en el gráfico para facilitar la depuración visual.
