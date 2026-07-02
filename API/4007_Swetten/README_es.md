# Estrategia endulzada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Swetten es una estrategia de ruptura impulsada por una red neuronal que se distribuyó originalmente durante MetaTrader 4. Evalúa el diferencial entre un promedio móvil simple de 233 períodos a largo plazo y diez promedios móviles más rápidos calculados en velas de un minuto. Los diferenciales se introducen en una red de base radial que produce un nivel de activación alcista o bajista. Cuando la activación es positiva la estrategia entra en largo, cuando es negativa entra en corto.

## Mercado y plazo
- Diseñado para los principales pares de divisas (el código original apuntaba al EURUSD).
- El análisis utiliza velas de un minuto y las decisiones se toman sólo sobre velas completas.
- Las señales se evalúan cada dos horas al final de la hora (00:00, 02:00, …, 22:00 hora de cambio). No se abren operaciones los sábados ni los domingos.

## Indicadores y características
- Medias móviles simples con períodos: 233 (línea de base), 144, 89, 55, 34, 21, 13, 8, 5, 3, 2.
- Las entradas de la red neuronal son las diferencias entre el promedio de 233 períodos y cada promedio más rápido.
- Antes de pasar a la red, las entradas se fijan en rangos entrenados, se normalizan y se escalan con los mismos coeficientes utilizados en la DLL original.
- La red de base radial se replica exactamente a partir de la función `EURUSDn` exportada, que consta de 38 características gaussianas cuyas salidas se promedian para obtener la activación final.

## Reglas de trading
1. Espere el cierre de una vela de un minuto que finaliza en una hora par y cae en un día laborable.
2. Calcule la activación de la red neuronal a partir de los diferenciales de media móvil.
3. Si la activación > 0 y la posición actual no es larga, envíe una compra de mercado por `TradeVolume + abs(current position)` lotes.
4. Si la activación < 0 y la posición actual no es corta, envíe una venta de mercado por `TradeVolume + abs(current position)` lotes.
5. Las posiciones están protegidas por:
   - Una toma de ganancias fija definida en pasos de precio (`TakeProfitPoints`).
   - Un stop loss fijo definido en pasos de precio (`StopLossPoints`).
   - Cuando se toca cualquiera de los niveles utilizando los extremos alto/bajo de la vela, la posición se cierra mediante una orden de mercado.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `CandleType` | Serie de velas utilizadas para los cálculos. | marco de tiempo de 1 minuto |
| `TradeVolume` | Volumen base de pedidos en lotes. | 0.1 |
| `SlowPeriod` | Período de la media móvil simple de referencia. | 233 |
| `TakeProfitPoints` | Distancia objetivo de beneficio en pasos de precio. | 150 |
| `StopLossPoints` | Distancia de stop-loss en pasos de precio. | 40 |

## Notas de conversión
- La red neuronal basada en DLL de MetaTrader se transfirió completamente a C# traduciendo la función exportada a código administrado.
- Las salidas protectoras imitan las condiciones `OrderClose` originales al comparar los máximos y mínimos de las velas con los umbrales de los escalones de precios.
- La gestión de entradas realiza un seguimiento del último precio de cumplimiento a través de `OnNewMyTrade` para alinear las salidas con los cumplimientos reales.
- Todos los comentarios se reescribieron en inglés y el código utiliza API StockSharp de alto nivel (`SubscribeCandles`, `Bind`) según lo exigen las pautas de conversión.
