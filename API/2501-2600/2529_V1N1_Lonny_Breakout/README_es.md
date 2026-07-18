# Estrategia V1N1 Lonny Breakout
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia V1N1 Lonny Breakout replica el asesor experto de MetaTrader "V1N1 LONNY". Apunta a rompimientos que emergen alrededor de las sesiones de Londres y Nueva York construyendo un rango de apertura y esperando un cierre decisivo fuera de ese rango. La estrategia se basa en una media móvil exponencial para capturar la tendencia predominante y en un oscilador estocástico para filtrar condiciones de sobrecompra o sobreventa antes de entrar al mercado.

Un modelo de riesgo configurable permite dimensionar posiciones por volumen fijo o como porcentaje del capital de la cuenta. La implementación también incluye filtrado de spread opcional, stops de trailing, y un tiempo de espera basado en barras que cierra la operación si el momentum se desvanece después de un número predefinido de velas.

## Lógica de trading
1. **Alineación de sesión** – El trading solo se permite entre los horarios de inicio y fin configurados. El horario puede ajustarse según los cambios de horario de verano para Londres o Nueva York.
2. **Rango de apertura** – Justo antes de que comience la sesión, la estrategia registra los máximos y mínimos de un número fijo de velas. Este rango proporciona los niveles de rompimiento usados durante la ventana de trading.
3. **Confirmación de tendencia** – La pendiente de la media móvil exponencial (EMA) debe estar de acuerdo con la dirección de la operación. Un rompimiento alcista requiere que la EMA suba, mientras que un rompimiento bajista requiere que caiga.
4. **Filtro de momentum** – El oscilador estocástico debe permanecer dentro de una zona configurable alrededor del punto medio para evitar entrar cuando el mercado ya está sobrecomprado o sobrevendido.
5. **Validación del rompimiento** – La vela anterior debe cerrar más allá del máximo o mínimo del rango al menos la distancia mínima de rompimiento pero no más lejos que la distancia máxima.
6. **Controles de riesgo** – Cada posición define un stop loss desde el límite del rango y un objetivo de take-profit basado en un factor de esa distancia de stop. Un trailing stop puede ajustar la salida a medida que avanza la operación, y las posiciones pueden cerrarse forzosamente después de cierto número de velas.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `StartTrade` | Hora de inicio de sesión. |
| `EndTrade` | Hora de fin de sesión. |
| `SwitchDst` | Manejo de horario de verano: Europa (sin cambio), EE.UU. (cambio relativo entre Londres y Nueva York), o desactivado. |
| `RiskModes` | Modo de dimensionamiento de posición (porcentaje del capital o volumen fijo). |
| `PositionRisk` | Porcentaje de riesgo o volumen fijo, dependiendo del modo. |
| `TradeRange` | Número de velas usadas para construir el rango de apertura. |
| `MinRangePoints` / `MaxRangePoints` | Tamaño mínimo y máximo del rango de apertura, en puntos de precio. |
| `MinBreakRange` / `MaxBreakRange` | Distancia de rompimiento mínima y máxima aceptable por encima o por debajo del rango, en puntos de precio. |
| `StopLossPoints` | Distancia del stop-loss medida desde el lado opuesto del rango, en puntos de precio. |
| `TpFactor` | Multiplicador de take-profit aplicado a la distancia del stop-loss. |
| `TrailStopPoints` | Distancia opcional del trailing stop, en puntos de precio. Poner en cero para deshabilitar el trailing. |
| `TrendPeriod` | Período para el filtro de pendiente EMA. |
| `OverPeriod` | Período para el oscilador estocástico. |
| `OverLevels` | Distancia desde 50 usada para definir el rango aceptable del estocástico. |
| `BarsToClose` | Número máximo de velas para mantener la posición abierta. Cero deshabilita el tiempo de espera. |
| `MaxSpreadPoints` | Máximo spread permitido en puntos de precio. |
| `SlippagePoints` | Deslizamiento de referencia en puntos de precio (mantenido por compatibilidad con el asesor experto original). |
| `CandleType` | Tipo de vela y marco temporal procesados por la estrategia. |

## Notas de uso
- La estrategia está diseñada para instrumentos cotizados con un paso de precio fijo. Las entradas basadas en puntos se multiplican por el `PriceStep` del instrumento para obtener distancias de precio.
- Los datos del libro de órdenes se usan para estimar el spread actual. Si las mejores cotizaciones bid/ask no están disponibles, el filtrado de spread se omite.
- Las salidas de trailing y tiempo de espera se evalúan en velas cerradas, coincidiendo con la lógica MQL original.
- El dimensionamiento de posición requiere la valoración de la cartera (`Portfolio.CurrentValue`) cuando `RiskModes` está configurado en porcentaje. Si el valor no está disponible, la estrategia vuelve al tamaño de lote configurado.

## Archivos
- `CS/V1n1LonnyBreakoutStrategy.cs` – Implementación de la estrategia en C# para StockSharp.
- `README.md` – Esta descripción en inglés.
- `README_zh.md` – 中文简介。
- `README_ru.md` – Descripción en ruso.
