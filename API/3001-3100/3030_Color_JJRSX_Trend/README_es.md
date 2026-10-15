# Estrategia Color JJRSX Tendencia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia reimagina el asesor experto de MetaTrader `Exp_ColorJJRSX` dentro del marco de alto nivel de StockSharp. El sistema original se basa en el oscilador ColorJJRSX propietario, que combina técnicas de suavizado Jurik para detectar cambios de tendencia. En este port, el oscilador se aproxima con un Índice de Fuerza Relativa (RSI) estándar que se suaviza adicionalmente mediante una Media Móvil Jurik (JMA). La pendiente del oscilador suavizado se evalúa luego en varias barras históricas para activar entradas y salidas.

El trading tiene lugar en un marco temporal de velas configurable (velas de 4 horas por defecto) y admite alternadores independientes para operaciones largas y cortas. Parámetros adicionales permiten mantener la lógica de salida idéntica al asesor experto fuente mientras se introducen controles de riesgo nativos de StockSharp como stop loss y take profit basados en puntos.

## Construcción del indicador
1. **Aproximación RSI** – Un `RelativeStrengthIndex` con el período definido por `JurxPeriod` reemplaza la etapa original de suavizado JurX. Esto mantiene el oscilador acotado entre 0 y 100 mientras captura el impulso relativo.
2. **Suavizado Jurik** – La salida del RSI se pasa a través de una `JurikMovingAverage` (longitud `JmaPeriod`). La serie resultante es una curva suave que reacciona rápidamente a los cambios de impulso sin lag excesivo.
3. **Ventana histórica** – La estrategia almacena los valores JMA más recientes `SignalBar + 3` para replicar el uso de `CopyBuffer` de MQL. Los valores indexados por `SignalBar`, `SignalBar + 1` y `SignalBar + 2` corresponden a las barras usadas en el experto fuente para la evaluación de señales.

## Lógica de trading
- **Configuración alcista**
  - `JMA[SignalBar + 1] < JMA[SignalBar + 2]` confirma que el oscilador giró hacia arriba en la barra precedente.
  - `JMA[SignalBar] > JMA[SignalBar + 1]` muestra que el impulso ascendente continúa en la última barra cerrada.
  - Si las entradas largas están habilitadas y no hay posición larga activa, la estrategia compra `OrderVolume` unidades. La exposición corta existente se revierte automáticamente.
- **Configuración bajista**
  - `JMA[SignalBar + 1] > JMA[SignalBar + 2]` confirma un giro hacia abajo.
  - `JMA[SignalBar] < JMA[SignalBar + 1]` valida el impulso descendente continuo.
  - Si las entradas cortas están habilitadas, la estrategia vende `OrderVolume` unidades y voltea cualquier exposición larga existente.
- **Reglas de salida**
  - Cuando la pendiente del oscilador suavizado gira contra la posición (`AllowBuyClose` / `AllowSellClose`), la operación abierta se cierra al mercado.
  - Los niveles de stop loss y take profit protectores (expresados en puntos de precio) se recalculan en cada nueva posición. Si el rango de la vela toca un nivel, la posición se cierra inmediatamente.

## Gestión de riesgo
- `StopLossPoints` se convierte a distancia de precio con el paso de precio del instrumento y protege contra movimientos adversos.
- `TakeProfitPoints` define la distancia de objetivo simétrica.
- Los stops y objetivos se deshabilitan automáticamente cuando se establecen a cero.
- El volumen puede ajustarse independientemente del volumen de la estrategia base a través de `OrderVolume`.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `JurxPeriod` | Período de la aproximación RSI usada antes del suavizado Jurik. Refleja el período JurX del experto MQL. |
| `JmaPeriod` | Longitud de la Media Móvil Jurik aplicada a la salida del RSI. |
| `SignalBar` | Índice de la barra histórica usada para la evaluación (1 = barra cerrada anterior). Los valores mayores retrasan la confirmación de señal. |
| `EnableBuy` / `EnableSell` | Alternar entradas largas o cortas independientemente. |
| `AllowBuyClose` / `AllowSellClose` | Habilitar señales de salida basadas en pendiente para posiciones largas y cortas respectivamente. |
| `OrderVolume` | Volumen negociado en cada nueva entrada. La exposición opuesta existente se añade a la nueva orden para realizar una reversión completa. |
| `TakeProfitPoints` / `StopLossPoints` | Objetivo de ganancia y distancia de stop en puntos del instrumento. Establecer a cero para deshabilitar. |
| `CandleType` | Marco temporal de velas usado para cálculos del indicador (predeterminado velas de 4 horas). |

## Diferencias con el asesor experto original
- El suavizado JurX se aproxima mediante un RSI clásico porque el algoritmo JurX propietario no está disponible en StockSharp. Los nombres de parámetros permanecen consistentes para simplificar la migración.
- El deslizamiento de MetaTrader (`Deviation_`) y las enumeraciones de gestión de dinero no se reproducen. En su lugar se proporciona un parámetro `OrderVolume` fijo; puede combinarlo con módulos de dimensionamiento de posición de StockSharp si es necesario.
- Las órdenes se ejecutan con `BuyMarket`/`SellMarket`, mientras que el stop loss y take profit se emulan mediante comprobaciones de precio en la vela terminada.

## Consejos de uso
1. Adjunte la estrategia al instrumento deseado y configure `CandleType` para que coincida con el marco temporal que desea replicar.
2. Ajuste `JurxPeriod` y `JmaPeriod` para adaptarse a la capacidad de respuesta del mercado. Los valores más altos crean oscilaciones más suaves y menos señales.
3. Ajuste finamente `SignalBar` si necesita un lag de confirmación adicional en comparación con el retraso de una barra predeterminado.
4. Configure `OrderVolume`, `StopLossPoints` y `TakeProfitPoints` de acuerdo a su apetito de riesgo. Use cero para deshabilitar las salidas automáticas.
5. Combine con los helpers de registro o gráficos integrados de StockSharp (ya cableados para gráficos de velas + indicadores) para monitorear el comportamiento del oscilador en tiempo real.

La estrategia está lista tanto para experimentación discrecional como para backtesting automatizado dentro del entorno StockSharp mientras permanece fiel a la intención del sistema ColorJJRSX original.
