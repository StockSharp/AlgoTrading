# Estrategia de Trading IStochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La Estrategia de Trading IStochastic es un port directo de StockSharp del asesor experto "IStochastic_Trading" de MetaTrader 5. El bot usa el Oscilador Estocástico para detectar condiciones de sobrecompra y sobreventa y luego construye una escalera de posiciones al estilo martingala mientras gestiona cada entrada con stop-loss, take-profit y un trailing stop. La implementación opera en velas terminadas obtenidas a través de la API de alto nivel de StockSharp y se basa únicamente en órdenes de mercado.

## Lógica de trading
1. Calcular un Oscilador Estocástico con longitud %K, suavizado %D y un factor de ralentización adicional configurables.
2. Cuando no hay posiciones activas, evaluar la vela terminada más reciente:
   - Abrir una posición larga si %K está por encima de %D y %D está por debajo de la zona de compra configurada.
   - Abrir una posición corta si %K está por debajo de %D y %D está por encima de la zona de venta configurada.
3. Cuando existe una posición, monitorear el último relleno en la escalera:
   - Si el mercado se mueve contra la operación al menos el gap configurado (en pips), abrir una nueva posición en la misma dirección con el doble del volumen anterior, siempre que no se exceda el número máximo de posiciones.
4. Para cada entrada mantener niveles de stop-loss y take-profit por operación derivados de distancias en pips convertidas a puntos de precio usando el `PriceStep` del instrumento y el número de decimales. Si el precio de cierre alcanza el stop o el objetivo, la estrategia sale de la posición específica con una orden de mercado.
5. Aplicar un trailing stop después de cada cierre de vela. Cuando la operación se mueve suficientemente en la dirección favorable, el precio del stop se ajusta por el paso de trailing especificado, aproximando el comportamiento de trailing por posición del terminal.

## Parámetros
| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `OrderVolume` | `0.1` | Tamaño inicial de la posición en lotes. Las entradas adicionales doblan el volumen anterior. |
| `TakeProfitPips` | `50` | Distancia del take-profit medida en pips. El valor se convierte a puntos de precio internamente. |
| `StopLossPips` | `50` | Distancia del stop-loss en pips para cada posición. |
| `TrailingStopPips` | `10` | Distancia del trailing stop en pips. Establecer en cero para deshabilitar el trailing. |
| `TrailingStepPips` | `5` | Movimiento favorable mínimo (en pips) antes de que se ajuste el trailing stop. |
| `MaxPositions` | `3` | Número máximo de pasos martingala simultáneamente abiertos. Un valor de `0` elimina el límite. |
| `GapPips` | `7` | Gap de precio, en pips, requerido antes de doblar en la dirección actual. |
| `KPeriod` | `5` | Número de velas usadas para construir la línea %K. |
| `DPeriod` | `3` | Período del promedio de suavizado %D. |
| `Slowing` | `3` | Suavizado adicional aplicado a %K. |
| `ZoneBuy` | `30` | Umbral de %D usado para validar entradas largas (zona de sobreventa). |
| `ZoneSell` | `70` | Umbral de %D usado para validar entradas cortas (zona de sobrecompra). |
| `CandleType` | `Marco temporal de 15 minutos` | Serie de velas empleada para los cálculos. |

## Notas de implementación
- Las distancias en pips se convierten a precios con `PriceStep`. Para cotizaciones de 3 y 5 dígitos se usa un factor adicional de 10 para imitar la lógica de punto ajustado de MetaTrader.
- Las verificaciones de stop-loss, take-profit y trailing stop dependen de los precios de cierre de las velas para mantener la lógica determinista dentro del backtester. La ejecución en tiempo real puede personalizarse si se requiere gestión intrabar.
- La estrategia solo abre una escalera direccional a la vez; todas las posiciones deben cerrarse antes de cambiar de dirección.
- La implementación en Python se omite intencionalmente según lo solicitado.
