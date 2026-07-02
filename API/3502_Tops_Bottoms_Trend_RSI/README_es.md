# Estrategia de tendencia de tops y fondos RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es una versión StockSharp del asesor experto MetaTrader "Tops Bottoms Trend and rsi ea". Monitorea las velas terminadas del período de tiempo seleccionado, busca máximos o mínimos de tendencias emergentes dentro de una ventana retrospectiva configurable y confirma cada oportunidad con un filtro de índice de fuerza relativa (RSI). Cuando se cumplen los criterios, la estrategia abre una orden de mercado única e inmediatamente asigna niveles protectores de stop-loss y take-profit derivados de distancias basadas en pips.

## Lógica de trading
- **Fuente de datos**: el algoritmo se suscribe al tipo de vela configurado y evalúa solo velas terminadas para evitar el uso de datos incompletos.
- **Detección de fondo (configuración larga)**: el cierre de la última vela debe estar al menos `BuyTrendPips` pips por debajo del máximo de la vela hace `BuyTrendCandles` barras. Todos los mínimos intermedios deben permanecer por encima del cierre actual y el filtro de calidad (`BuyTrendQuality`) requiere que los máximos recientes no se desvíen demasiado del máximo de referencia. Cuando se forma esta estructura y el valor RSI de la vela anterior está por debajo de `BuyRsiThreshold`, la estrategia abre una posición larga con volumen `BuyVolume`.
- **Detección de máximo (configuración corta)**: el cierre de la última vela debe estar al menos `SellTrendPips` pips por encima del mínimo de la vela hace `SellTrendCandles` barras. Los máximos intermedios deben permanecer por debajo del cierre actual mientras que el filtro de calidad (`SellTrendQuality`) mantiene los mínimos recientes cerca del mínimo de referencia. Si el valor RSI de la vela anterior excede `SellRsiThreshold`, la estrategia abre una posición corta con volumen `SellVolume`.
- **Gestión de riesgos**: después de cada entrada, la estrategia almacena el precio de ejecución y calcula los niveles de protección basados en pips. Las compensaciones de stop-loss utilizan `BuyStopLossPips` o `SellStopLossPips`. Las distancias de toma de ganancias se derivan principalmente de la parada vía `BuyTakeProfitPercentOfStop` o `SellTakeProfitPercentOfStop`. Si el porcentaje de obtención de beneficios a largo plazo está deshabilitado (`0`), se utiliza la distancia fija `BuyTakeProfitPips` en su lugar. Cada vez que las velas posteriores tocan los niveles correspondientes de stop o toma de ganancias, la posición se cierra con una orden de mercado.
- **Control de posición** – el sistema mantiene como máximo una posición abierta. Las nuevas señales se ignoran mientras exista una posición u orden activa. La confirmación de RSI siempre se basa en la vela anterior (desplazamiento de una barra), reflejando la EA original.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `BuyVolume` | Volumen de órdenes utilizado para posiciones largas. | `0.01` |
| `BuyStopLossPips` | Distancia de stop-loss para operaciones largas en pips. | `20` |
| `BuyTakeProfitPips` | Se corrigió la distancia de toma de ganancias en pips para posiciones largas cuando el modo de porcentaje está deshabilitado. | `5` |
| `BuyTakeProfitPercentOfStop` | Take-profit como porcentaje de la distancia larga del stop-loss. | `100` |
| `SellVolume` | Volumen de órdenes utilizado para posiciones cortas. | `0.01` |
| `SellStopLossPips` | Distancia de stop-loss para operaciones cortas en pips. | `20` |
| `SellTakeProfitPercentOfStop` | Take-profit como porcentaje de la distancia corta del stop-loss. | `100` |
| `SellTrendCandles` | Número de velas inspeccionadas al buscar tapas nuevas. | `10` |
| `SellTrendPips` | Avance mínimo por encima del mínimo de referencia requerido para una configuración corta (pips). | `20` |
| `SellTrendQuality` | Filtro de calidad de tendencia para configuraciones cortas (fijado en el rango de 1 a 9). | `5` |
| `BuyTrendCandles` | Número de velas inspeccionadas en busca de nuevos fondos. | `10` |
| `BuyTrendPips` | Caída mínima por debajo del máximo de referencia requerido para una configuración larga (pips). | `20` |
| `BuyTrendQuality` | Filtro de calidad de tendencia para configuraciones largas (fijado en el rango de 1 a 9). | `5` |
| `BuyRsiPeriod` | RSI período utilizado para confirmaciones largas. | `14` |
| `BuyRsiThreshold` | RSI umbral de sobreventa que debe cruzarse desde arriba para permitir entradas largas. | `40` |
| `SellRsiPeriod` | RSI período utilizado para confirmaciones breves. | `14` |
| `SellRsiThreshold` | RSI umbral de sobrecompra que debe cruzarse desde abajo para permitir entradas cortas. | `60` |
| `CandleType` | Cronograma de las velas procesadas por la estrategia. | `30-minute time frame` |

## Notas
- Las distancias de pips se convierten a precios utilizando el valor `PriceStep`. Las cotizaciones de divisas de cinco dígitos y pips fraccionarios están normalizadas al tamaño de pip clásico, replicando las reglas de conversión del EA original.
- Debido a que la confirmación RSI utiliza la vela anterior (shift = 1), la estrategia necesita al menos un valor RSI completamente formado antes de poder operar. Por lo tanto, se ignoran las primeras velas después del inicio.
- La lógica cancela todos los niveles de protección cada vez que una posición está completamente cerrada, asegurando que la siguiente entrada comience con nuevos parámetros de riesgo.
