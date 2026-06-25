# Estrategia Russian20 Momentum MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia Russian20 Momentum MA** es una conversión directa del asesor experto MetaTrader 5 `Russian20-hp1.mq5`. El script original fue publicado por Gordago Software Corp. y se basa en un gráfico de dos horas, una media móvil simple (SMA) de 20 períodos y un indicador Momentum de 5 períodos para identificar continuaciones de tendencias a corto plazo. La implementación en StockSharp mantiene el mismo núcleo analítico mientras adapta el manejo de órdenes y la gestión del dinero a la API de estrategia de alto nivel.

## Lógica de trading
- **Frecuencia de datos:** Trabaja con el tipo de vela definido por el usuario (el predeterminado son velas de 2 horas, coincidiendo con el marco temporal MQL5 `PERIOD_H2`). La lógica se ejecuta solo cuando se cierra una vela.
- **Indicadores:**
  - Media móvil simple con período configurable (predeterminado 20).
  - Indicador Momentum con período configurable (predeterminado 5). El nivel neutro de Momentum es 100, reflejando la salida predeterminada de MQL5.
- **Entrada larga:** Se activa cuando se satisfacen todas las siguientes condiciones en la última vela cerrada:
  1. El precio de cierre está por encima de la SMA.
  2. El valor de Momentum es mayor que 100 (aceleración positiva).
  3. El precio de cierre es más alto que el cierre de la vela anterior, asegurando momentum ascendente en la acción del precio.
- **Entrada corta:** Se activa cuando se satisfacen todas las siguientes condiciones:
  1. El precio de cierre está por debajo de la SMA.
  2. El valor de Momentum es menor que 100 (aceleración negativa).
  3. El precio de cierre es más bajo que el cierre de la vela anterior.
- **Salida larga:** La estrategia liquida posiciones largas cuando Momentum cae por debajo de 100 o cuando se cruza un umbral de stop-loss o take-profit de protección.
- **Salida corta:** La estrategia liquida posiciones cortas cuando Momentum sube por encima de 100 o cuando se alcanzan los umbrales de protección configurados.

## Gestión de riesgos
El asesor experto MQL5 original coloca órdenes fijas de stop loss y take profit en "pips" ajustados para precios Forex de 4 y 5 dígitos. La conversión en C# reproduce este comportamiento:
- Calculando un tamaño de pip ajustado a partir del `PriceStep` del instrumento. Para símbolos con tres o cinco decimales, el tamaño del pip equivale a `PriceStep * 10`, de lo contrario equivale a `PriceStep`.
- Traduciendo las entradas del usuario para stop loss y take profit en distancias de precio absolutas.
- Monitoreando la acción del precio en cada vela cerrada y cerrando la posición cuando el precio cruza los umbrales calculados.

## Parámetros
| Parámetro | Valor predeterminado | Descripción |
|-----------|---------|-------------|
| `CandleType` | Velas de 2 horas | Tipo de datos utilizado para la generación de señales. |
| `MovingAverageLength` | 20 | Retrospectiva para el filtro SMA. |
| `MomentumPeriod` | 5 | Retrospectiva para el indicador Momentum. |
| `StopLossBuyPips` | 50 | Distancia de stop-loss largo expresada en pips. Poner 0 para deshabilitar. |
| `TakeProfitBuyPips` | 50 | Distancia de take-profit largo en pips. Poner 0 para deshabilitar. |
| `StopLossSellPips` | 50 | Distancia de stop-loss corto en pips. Poner 0 para deshabilitar. |
| `TakeProfitSellPips` | 50 | Distancia de take-profit corto en pips. Poner 0 para deshabilitar. |

Todos los parámetros numéricos están expuestos a través de `StrategyParam<T>` y marcados como optimizables cuando corresponde, permitiendo backtesting y optimización con herramientas de StockSharp.

## Notas de implementación
- La estrategia utiliza la API de alto nivel `SubscribeCandles().Bind(...)` para transmitir datos de velas y obtener simultáneamente valores de SMA y Momentum sin gestión manual de indicadores.
- Los niveles de Momentum se evalúan exactamente como en el script MQL5 (100 como nivel neutro). Cualquier brecha más allá de los offsets de stop-loss/take-profit activa una salida de mercado, imitando fielmente la lógica original de colocación de órdenes.
- El cierre anterior se almacena en caché para verificar el momentum del precio sin recurrir a búsquedas en colecciones históricas, en línea con las directrices de rendimiento del proyecto.
- Los ganchos de visualización (`DrawCandles`, `DrawIndicator`, `DrawOwnTrades`) están conectados por conveniencia cuando el entorno host admite gráficos.

## Consejos de uso
- El marco temporal y los parámetros predeterminados corresponden a la configuración original del autor. Ajuste el tipo de vela cuando trabaje con instrumentos que no producen barras de 2 horas.
- Al operar activos cotizados con tamaños de tick no convencionales, revise el tamaño de pip calculado para asegurarse de que las distancias de stop-loss y take-profit sean realistas.
- La estrategia está diseñada para una única posición abierta a la vez. Las operaciones manuales externas o las posiciones simultáneas en el mismo instrumento pueden interferir con la lógica de salida integrada.
