# FrBestExp02 Maloma Mod Estrategia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una adaptación de C# del MetaTrader 4 experto `Frbestexp02_1_maloma_mod.mq4`. Combina el impulso de OsMA, reversiones fractales, confirmación de volumen de ticks y un filtro de pivote diario para atenuar los movimientos agotados en el marco temporal M15.

## Lógica comercial

- **Pivote de sesión**: un punto de pivote móvil se calcula a partir del máximo más alto, el mínimo más bajo y el cierre más antiguo dentro de una ventana configurable (96 velas de forma predeterminada, equivalente a un día de negociación en M15). Sólo se permiten operaciones que coincidan con el sesgo de pivote: ventas cortas por encima del pivote y posiciones largas por debajo de él.
- **Patrón fractal**: la estrategia espera un fractal confirmado de Bill Williams tres velas atrás. Los fractales hacia abajo (mínimos) permiten posiciones cortas, mientras que los fractales hacia arriba (máximos) permiten posiciones largas.
- **Histograma OsMA**: un histograma MACD (rápido 12, lento 26, señal 9 por defecto) debe tener una pendiente mayor hacia territorio negativo para las posiciones cortas y más hacia territorio positivo para las posiciones largas. La lectura anterior del histograma también debe estar en el mismo lado de cero.
- **Filtro de volumen**: el volumen de la vela terminada anterior debe exceder un umbral configurable y ser mayor que el volumen de hace dos velas. Esto reproduce el requisito de pico de volumen de ticks del experto original.
- **Tiempo de pedido**: las operaciones se limitan por un intervalo mínimo (20 segundos de forma predeterminada) entre entradas.
- **Gestión de riesgos**: el stop-loss configurable, el take-profit y el trailing stop opcional se expresan en puntos y se convierten a precios de instrumentos. Las órdenes de protección se actualizan con los ayudantes integrados `SetStopLoss`/`SetTakeProfit`.

## Parámetros

| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `Volume` | Volumen de pedidos utilizado para cada entrada. | 1 |
| `StopLossPoints` | Distancia de stop-loss en puntos del instrumento. | 1000 |
| `TakeProfitPoints` | Distancia de toma de ganancias en puntos del instrumento. | 1000 |
| `TrailingStopPoints` | Distancia de parada de seguimiento opcional en puntos (0 desactiva el seguimiento). | 0 |
| `VolumeThreshold` | Volumen mínimo de vela anterior requerido para habilitar una señal. | 50 |
| `OsmaFastPeriod` / `OsmaSlowPeriod` / `OsmaSignalPeriod` | MACD parámetros utilizados para calcular el histograma OsMA. | 12 / 26 / 9 |
| `PivotWindow` | Número de velas terminadas incluidas en el cálculo del pivote. | 96 |
| `MinTradeIntervalSeconds` | Número mínimo de segundos entre nuevas entradas. | 20 |
| `CandleType` | Marco de tiempo principal (por defecto, velas de 15 minutos). | M15 |

## Diferencias versus el experto MQL4

- El código original admitía órdenes de cobertura multiplicadas por `kh` y una lógica compleja de reciclaje de beneficios. La versión StockSharp ejecuta una posición direccional única y la cierra o la revierte antes de abrir una nueva operación.
- El manejo de trailing stop se simplifica al utilizar el asistente estándar `SetStopLoss` en lugar de modificar manualmente las órdenes por tick.
- Se omiten la agregación de beneficios y los bloques de recuperación estilo martingala. La gestión de salida se basa en stop-loss, take-profit o trailing stop.
- Todos los cálculos de los indicadores se basan en eventos en velas terminadas. No hay modificación de orden intrabar.

## Notas de uso

1. Adjunte la estrategia a un instrumento que proporcione datos de volumen de ticks si el filtro de volumen debe coincidir con el comportamiento original.
2. Mantenga el período de tiempo en 15 minutos para reproducir la calibración original de la ventana dinámica y la mirada retrospectiva fractal.
3. Ajuste los períodos `VolumeThreshold` y OsMA para que se ajusten a los símbolos con diferentes perfiles de volatilidad o volumen.
4. Habilite el trailing stop sólo cuando se desee una salida más cerrada; de lo contrario, déjelo en cero para confiar en la parada/objetivo estático.

El código sigue las pautas de alto nivel StockSharp API: suscripciones de velas a través de `SubscribeCandles`, vinculación de indicadores para el histograma MACD y ejecución segura a través de `BuyMarket`/`SellMarket` con órdenes de protección automáticas.
