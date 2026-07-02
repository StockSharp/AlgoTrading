# MACD Buceador y RSI Estrategia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Esta estrategia es una conversión de C# del asesor experto **"Macd diver and rsi"** MetaTrader 5. Mantiene la idea original de la señal de dos etapas: el índice de fuerza relativa (RSI) detecta extremos de sobreventa o sobrecompra, mientras que el histograma MACD confirma que el impulso está regresando en la dirección de la operación. Los lados largos y cortos se configuran de forma independiente para que el comportamiento se pueda ajustar para configuraciones alcistas y bajistas por separado.

La estrategia opera con una única suscripción de vela (período de tiempo configurable) y negocia el valor registrado directamente a través de órdenes de mercado. Todo el procesamiento de indicadores utiliza el StockSharp API de alto nivel a través de `BindEx`, que coincide con las reglas del proyecto.

## Lógica de trading

1. **Preparación de indicadores**
   - Se crean dos indicadores RSI, uno para el tramo largo y otro para el tramo corto, con longitudes y umbrales individuales.
   - Dos indicadores `MovingAverageConvergenceDivergenceSignal` reflejan la configuración MACD para operaciones largas y cortas. Su componente de histograma se utiliza para confirmar las inversiones de impulso.

2. **Reglas de entrada**
   - **Configuración larga**: cuando el valor largo de RSI está en o por debajo del umbral de sobreventa *y* el histograma largo de MACD cruza por encima de cero (cambia de signo de negativo a positivo), se abre una posición alcista. Si una posición corta está activa, se cierra y se revierte en el mismo orden de mercado.
   - **Configuración corta**: cuando el valor corto de RSI está en o por encima del umbral de sobrecompra *y* el histograma corto de MACD cruza por debajo de cero, se abre una posición bajista. La exposición larga existente se aplana antes de que se establezca la nueva exposición corta.

3. **Gestión de riesgos**
   - Después de cada entrada, la estrategia registra el precio de cierre de la barra de señal como precio de referencia.
   - Los niveles de stop-loss y take-profit se proyectan a partir de ese precio utilizando distancias de pips definidas por separado para operaciones largas y cortas.
   - Los pips se convierten a unidades de precio con el instrumento `PriceStep` y se escalan automáticamente en 10 para símbolos con 3 o 5 decimales para reflejar el comportamiento de MT5.
   - En cada vela completa, el rango máximo/bajo se compara con estos niveles. Alcanzar cualquiera de los niveles cierra inmediatamente la posición con una orden de mercado.

4. **Gestión comercial**
   - El estado de la posición se borra cada vez que el tamaño de la posición vuelve a cero (ya sea porque se alcanzó un stop/take-profit o porque la estrategia fue revertida por una señal opuesta).
   - No se realizan salidas parciales ni ajustes de seguimiento; la posición se gestiona únicamente a través de los niveles estáticos de stop-loss y take-profit.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `CandleType` | Periodo de suscripción de velas utilizado para las señales. |
| `LongRsiPeriod`, `ShortRsiPeriod` | RSI longitudes para detección larga y corta. |
| `LongRsiThreshold`, `ShortRsiThreshold` | RSI umbrales que permiten entradas (sobreventa para largos, sobrecompra para cortos). |
| `LongMacdFastLength`, `LongMacdSlowLength`, `LongMacdSignalLength` | MACD EMA longitudes para el tramo alcista. |
| `ShortMacdFastLength`, `ShortMacdSlowLength`, `ShortMacdSignalLength` | MACD EMA longitudes para el tramo bajista. |
| `LongVolume`, `ShortVolume` | Volumen comercial por señal. Al revertir, la estrategia agrega el volumen de apertura absoluto para que la orden única realice el cierre y la nueva apertura. |
| `LongStopLossPips`, `LongTakeProfitPips`, `ShortStopLossPips`, `ShortTakeProfitPips` | Distancia de las órdenes stop-loss y take-profit en pips. Cero desactiva el nivel respectivo. |

## Notas

- La estrategia requiere instrumentos con un valor distinto de cero `PriceStep`. Si falta el paso, el cálculo del pip vuelve a 0,0001 para evitar la división por cero.
- Dado que ambas partes utilizan instancias de indicadores independientes, es posible ajustar el comportamiento alcista y bajista por separado, por ejemplo, ajustando el umbral de sobrecompra y manteniendo el lado de sobreventa más permisivo.
- El código agrega documentación y comentarios en inglés para aclarar el proceso comercial y satisfacer las pautas del proyecto.
