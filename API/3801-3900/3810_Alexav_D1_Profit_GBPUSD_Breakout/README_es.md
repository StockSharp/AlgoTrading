# Estrategia GBPUSD de ganancias Alexav D1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Alexav D1 Profit GBPUSD es un sistema de ruptura diario convertido del MetaTrader 4 asesor experto *Alexav_d1_profit_gbpusd.mq4*. La estrategia opera con velas diarias del GBP/USD y evalúa la sesión completa una vez al día (de martes a viernes). La confirmación del impulso la proporcionan RSI y MACD, mientras que los stop ajustados por volatilidad y los objetivos de ganancias escalonados se derivan de ATR.

## Lógica de trading
1. **Preparación de indicadores**
   - Se aplican dos EMA con el mismo período a los precios máximos y mínimos diarios para definir niveles de referencia alcistas y bajistas.
   - RSI con una mirada retrospectiva de 10 períodos mide el impulso. Las lecturas extremas de RSI bloquean temporalmente nuevas operaciones en esa dirección.
   - MACD (24/05/14) ofrece un filtro de aceleración al comparar los dos últimos valores del histograma.
   - ATR (28) proporciona la unidad de volatilidad utilizada para paradas y objetivos de ganancias.
2. **Filtro de sesión**
   - Solo se realiza una evaluación por cada vela diaria completa de martes a viernes. Se omiten los lunes y fines de semana.
3. **Configuración larga**
   - La vela diaria anterior debe cerrar por encima del EMA de máximos calculados hace dos sesiones.
   - RSI de la sesión anterior debe estar por encima del nivel superior (predeterminado 60) pero por debajo del límite superior (predeterminado 80).
   - MACD debe estar por debajo de cero hace dos sesiones o mostrar una aceleración positiva suficiente en comparación con el valor anterior.
   - Si la apertura anterior vuelve a caer por debajo del EMA de máximos, la estrategia permite un nuevo lote de compras después de que se reinicia el bloque.
4. **Configuración corta**
   - Lógica reflejada de la configuración larga, utilizando el EMA de mínimos, RSI umbrales inferiores (39/25) y MACD filtros.

## Gestión de órdenes
Cuando se confirma una configuración, la estrategia abre un lote de cuatro órdenes de mercado (cada una usando la estrategia `Volume`):
- **Paradas**: cada orden comparte la misma parada de protección igual a `ATR * AtrStopMultiplier` (predeterminado 1,6) del precio de entrada.
- **Objetivos**: Los objetivos de ganancias aumentan en `AtrTargetMultiplier * (1 + i / 2)` para el índice de pedidos `i` en `[0..3]`, replicando las compensaciones de 1,0, 1,5, 2,0 y 2,5 ATR del EA original.
- **Manejo de conflictos**: Las posiciones opuestas se aplanan antes de abrir un nuevo lote. Al activar un lote largo se borra cualquier lote corto pendiente (y viceversa).

La estrategia monitorea las velas completadas. Si el mínimo diario toca el stop, la orden larga correspondiente se cierra en el mercado; si el máximo alcanza el objetivo, la orden también se cierra. Los cortos se manejan simétricamente usando el máximo de la vela para las paradas y el mínimo para los objetivos.

## Parámetros
| Parámetro | Descripción | Predeterminado |
|-----------|-------------|---------|
| `CandleType` | Serie de velas primarias, diaria por defecto. | 1 dia |
| `MaPeriod` | Periodo del EMA aplicado a máximos/mínimos. | 6 |
| `RsiPeriod` | RSI período para el filtro de impulso. | 10 |
| `AtrPeriod` | periodo ATR para el tamaño de parada/objetivo. | 28 |
| `AtrStopMultiplier` | ATR múltiplo para paradas. | 1.6 |
| `AtrTargetMultiplier` | Base ATR múltiplo para objetivos. | 1.0 |
| `RsiUpperLevel` | RSI umbral que confirma el impulso alcista. | 60 |
| `RsiUpperLimit` | RSI límite que bloquea nuevas posiciones largas. | 80 |
| `RsiLowerLevel` | RSI umbral que confirma el impulso bajista. | 39 |
| `RsiLowerLimit` | RSI piso que bloquea pantalones cortos nuevos. | 25 |
| `FastMaPeriod` | Período rápido de EMA para MACD. | 5 |
| `SlowMaPeriod` | Período lento de EMA durante MACD. | 24 |
| `SignalMaPeriod` | Periodo de señal EMA durante MACD. | 14 |
| `MacdDiffBuy` | Aceleración mínima MACD para largos. | 0,5 |
| `MacdDiffSell` | Aceleración mínima MACD para pantalones cortos. | 0,15 |

Establezca la estrategia `Volume` en el tamaño de lote deseado por pedido antes de comenzar la estrategia.

## Notas
- La conversión mantiene la lógica de una sola evaluación por día que se encuentra en el asesor experto original.
- Utilice datos históricos diarios para GBP/USD al realizar pruebas retrospectivas para reproducir el comportamiento previsto.
- Las paradas y objetivos de protección se simulan utilizando los extremos de las velas completadas; Los picos intradiarios dentro de una vela diaria no son visibles para la estrategia.
