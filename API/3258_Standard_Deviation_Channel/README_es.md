# Estrategia de Standard Deviation Channel
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es un port de StockSharp del experto de MetaTrader **Standard Deviation Channel**. Traza un canal de volatilidad basado en una media móvil ponderada linealmente (LWMA) y opera rupturas alineadas con la tendencia predominante. Las entradas son filtradas por la fuerza del Momentum y una confirmación del MACD, mientras que las salidas combinan objetivos fijos, saltos de break-even y protección por trailing.

## Indicadores y señales
- **Canal de desviación estándar** construido a partir de una línea base LWMA y un multiplicador de desviación configurable. Los setups largos requieren que la banda superior ascienda; los setups cortos requieren que la banda inferior descienda.
- **Filtro de tendencia:** LWMA rápida y lenta calculadas sobre las mismas velas. Los largos exigen `LWMA_fast > LWMA_slow`; los cortos requieren lo contrario.
- **Filtro de Momentum:** un indicador de Momentum de 14 períodos. Al menos una de las últimas tres lecturas debe desviarse del nivel neutro de 100 por el umbral configurado.
- **Filtro MACD:** configuración clásica 12/26/9. Las entradas largas necesitan `MACD ≥ signal`, mientras que las entradas cortas requieren `MACD ≤ signal`.

## Gestión de operaciones
- **Dimensionamiento de posición:** utiliza el parámetro `TradeVolume`. Las reversiones cierran automáticamente la exposición opuesta antes de abrir el nuevo lado.
- **Take-profit y stop-loss:** expresados en pips y evaluados contra el `PriceStep` del instrumento. La estrategia emite salidas de mercado una vez que el rango de la vela toca el precio objetivo o de stop.
- **Salto de break-even:** una vez que el beneficio no realizado alcanza `BreakEvenTriggerPips`, el stop se mueve a la entrada más `BreakEvenOffsetPips` (o menos para cortos).
- **Trailing stop:** después de alcanzar `TrailingStartPips`, el stop sigue el precio por `TrailingStepPips`, asegurando ganancias en ambos lados.
- **Salida por rechazo del canal:** si el precio cierra de nuevo dentro del canal y la pendiente se aplana contra la posición, la operación se cierra anticipadamente.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `CandleType` | Marco temporal principal utilizado para todos los cálculos. |
| `TradeVolume` | Tamaño base de la orden. |
| `TrendLength` | Período de retroceso LWMA que define la línea base del canal. |
| `DeviationMultiplier` | Multiplicador de desviación estándar para el ancho del canal. |
| `FastMaLength` / `SlowMaLength` | Longitudes LWMA para el filtro de tendencia. |
| `MomentumPeriod` | Período de retroceso para el filtro de Momentum. |
| `MomentumThreshold` | Desviación mínima desde 100 requerida en cualquiera de los últimos tres valores de Momentum. |
| `TakeProfitPips` / `StopLossPips` | Distancia de los niveles de salida fijos (convertidos usando `PriceStep`). |
| `BreakEvenTriggerPips` / `BreakEvenOffsetPips` | Controla cuándo y cómo se activa el stop de break-even. |
| `TrailingStartPips` / `TrailingStepPips` | Activa y dimensiona el trailing stop. |
| `MacdFastPeriod`, `MacdSlowPeriod`, `MacdSignalPeriod` | Configuración del MACD. |
| `MaxPositionUnits` | Posición neta absoluta máxima; previene el apalancamiento excesivo. |

## Notas de uso
1. Adjunte la estrategia a un valor que exponga un `PriceStep` válido. Los pips se convierten multiplicando este valor de paso.
2. Use `TrendLength` y `DeviationMultiplier` para adaptar el canal a diferentes mercados.
3. Los filtros de Momentum y MACD pueden relajarse (umbral inferior, períodos más cortos) para aumentar la frecuencia de operaciones.
4. La lógica de trailing funciona en cierres de velas; los picos intrabarra que no terminan más allá de los umbrales se ignoran.

## Diferencias con el Expert Advisor original
- La versión de MetaTrader se basa en objetos gráficos para leer la pendiente del canal y utiliza varias ramas de gestión de dinero (dimensionamiento martingala, protección de capital). Este port mantiene la verificación de pendiente pero simplifica el control de riesgo a operaciones de tamaño fijo limitadas por `MaxPositionUnits`.
- Todas las salidas se gestionan con órdenes de mercado al completarse la vela, ya que las estrategias de StockSharp no replican directamente las APIs de modificación de órdenes de MT4.
- Las notificaciones por correo electrónico y push se reemplazan por mensajes `AddInfoLog` para mantener la conversión autocontenida.
- Los cortes de cuenta basados en capital fueron omitidos; en cambio, el enfoque se centra en las características de protección por posición.

## Descargo de responsabilidad
Esta muestra está destinada a uso educativo. Siempre realice pruebas hacia adelante y valide la configuración antes de implementarla en una cuenta real.
