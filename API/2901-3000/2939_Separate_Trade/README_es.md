# Estrategia de Trade Separado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Resumen
La Estrategia de Trade Separado es una conversión del asesor experto de MetaTrader 5 "Separate trade". Preserva la lógica multi-filtro original mientras adopta la API de alto nivel de StockSharp para una gestión robusta de órdenes y manejo de indicadores. La estrategia intenta capturar giros silenciosos del mercado cuando la volatilidad y la dispersión están suprimidas. Solo se mantiene una posición neta a la vez, lo que refleja la intención del código original que limitaba el número de posiciones simultáneas.

## Indicadores y Datos
- Dos medias móviles con método configurable (SMA, EMA, SMMA o LWMA) y fuente de precio compartida.
- Average True Range (ATR) con períodos y umbrales separados para filtros largos y cortos.
- Desviación estándar usando el mismo precio aplicado que las medias móviles, nuevamente con períodos y límites específicos de dirección.
- Las velas se suministran a través de un parámetro `DataType` configurable para que la estrategia pueda adjuntarse a cualquier marco temporal o constructor de velas personalizado.

## Parámetros
| Parámetro | Descripción | Predeterminado |
| --- | --- | --- |
| `TradeVolume` | Tamaño de la orden expresado en lotes. | `1` |
| `SlowMaPeriod` | Período de la media móvil más lenta. | `65` |
| `FastMaPeriod` | Período de la media móvil más rápida. | `14` |
| `MaMethod` | Método de suavizado aplicado a ambas medias móviles (`Simple`, `Exponential`, `Smoothed`, `LinearWeighted`). | `Exponential` |
| `PriceType` | Entrada de precio para las medias móviles y la desviación estándar (`Close`, `Open`, `High`, `Low`, `Median`, `Typical`, `Weighted`). | `Close` |
| `StopLossBuyPips` / `StopLossSellPips` | Distancia del stop-loss para trades largos y cortos en pips (0 deshabilita el stop). | `50` |
| `TakeProfitBuyPips` / `TakeProfitSellPips` | Distancia del take-profit para trades largos y cortos en pips (0 deshabilita el take-profit). | `50` |
| `TrailingStopPips` | Distancia del trailing stop en pips. | `5` |
| `TrailingStepPips` | Avance mínimo de beneficio en pips antes de que el trailing stop se mueva. Debe ser positivo cuando el trailing está habilitado. | `5` |
| `MaxPositions` | Máximo de posiciones netas simultáneas permitidas. La versión StockSharp opera con una única posición agregada incluso cuando el valor es mayor que uno. | `1` |
| `DeltaBuyPips` / `DeltaSellPips` | Distancia máxima permitida entre las medias rápida y lenta (por dirección). Un valor de cero deshabilita el filtro de distancia. | `2` |
| `AtrPeriodBuy` / `AtrPeriodSell` | Período de retrospección de ATR para los filtros largo y corto. | `26` |
| `AtrLevelBuy` / `AtrLevelSell` | Umbral superior de ATR que no debe superarse antes de entrar en un trade. | `0.0016` |
| `StdDevPeriodBuy` / `StdDevPeriodSell` | Período de retrospección de desviación estándar para los filtros largo y corto. | `54` |
| `StdDevLevelBuy` / `StdDevLevelSell` | Límite de desviación estándar que no debe superarse antes de entrar en un trade. | `0.0051` |
| `CandleType` | Tipo de datos de velas usado por la suscripción. | `TimeSpan.FromMinutes(15)` |

## Lógica de Trading
1. **Sincronización de barras** – la estrategia actúa solo en velas terminadas recibidas de la suscripción configurada. Esto replica el guardia de nueva barra `OnTick` del script de MetaTrader.
2. **Filtros de indicadores** – para entradas largas la MA lenta debe estar por debajo de la MA rápida, el ATR debe estar por debajo de `AtrLevelBuy`, la desviación estándar debe estar por debajo de `StdDevLevelBuy`, y la distancia de MA debe ser menor que `DeltaBuyPips` (si el delta es positivo). Las entradas cortas invierten las condiciones y usan sus propios parámetros de ATR y desviación.
3. **Control de posición** – los trades solo se toman cuando no hay posición abierta y el último tiempo de entrada para el lado respectivo es más antiguo que la vela actual. Esto previene re-entradas dentro de la misma barra, coincidiendo con la verificación `m_last_deal_IN_*` en el EA fuente.
4. **Ejecución de órdenes** – las órdenes de mercado se colocan con el volumen configurado. Los trades de reversión aplanan automáticamente la posición actual antes de abrir una nueva gracias a la cantidad `Volume + Math.Abs(Position)` que coincide con el comportamiento MQL de cerrar la exposición opuesta.

## Gestión de Riesgo
- **Conversión de pips** – las distancias en pips se convierten usando el `PriceStep` del instrumento. Para instrumentos cotizados con 3 o 5 decimales, el tamaño del pip equivale a `PriceStep * 10`, reflejando la lógica original `digits_adjust`.
- **Stop-loss / take-profit** – la estrategia rastrea los niveles de precio internamente y sale cuando el rango de la vela toca el stop o el objetivo especificado. Ambos pueden deshabilitarse estableciendo la distancia en pips en cero.
- **Trailing stop** – una vez que el precio avanza más allá de `TrailingStopPips + TrailingStepPips`, el stop se mueve para mantener la distancia de trailing. El requisito del paso de trailing coincide con la implementación de MetaTrader y evita mover el stop por una cantidad insignificante.

## Notas de Implementación
- La estrategia usa una única posición agregada porque StockSharp trabaja con posiciones netas por defecto. Aunque el parámetro `MaxPositions` se retiene por compatibilidad, exceder uno simplemente previene nuevas entradas hasta que la posición actual se cierre.
- Los valores del indicador se calculan usando las clases de indicadores de StockSharp y la infraestructura `Bind` para evitar el acceso manual al buffer según lo requerido por las pautas del proyecto.
- La conversión mantiene todos los comentarios en inglés y mapea cada entrada original a un `StrategyParam` dedicado para que la optimización y la integración de Designer permanezcan disponibles.
- Cuando `TrailingStopPips` es positivo, `TrailingStepPips` también debe ser positivo. El código detiene la estrategia temprano y escribe un mensaje de error si se viola este requisito, reproduciendo la verificación de seguridad del experto MQL.
