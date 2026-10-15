# Estrategia de Exp Cronex Chaikin
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia porta el experto asesor MetaTrader **Exp_CronexChaikin.mq5** a la API de alto nivel de StockSharp. El robot original reconstruye el oscilador Chaikin a partir de valores de acumulación/distribución, lo suaviza dos veces con filtros Cronex "XMA" y opera las cruces entre las líneas rápida y lenta. La versión de StockSharp reproduce la misma lógica mientras expone cada etapa como parámetros configurables.

## Lógica de trading

1. Suscribirse a la serie de velas configurada (`CandleType`).
2. Recalcular la línea de acumulación/distribución (AD) para cada vela finalizada usando el `VolumeSource` seleccionado (volumen tick o real).
3. Aplicar el oscilador Chaikin suavizando la línea AD con dos medias móviles (`ChaikinFastPeriod`, `ChaikinSlowPeriod`, `ChaikinMethod`) y tomando su diferencia.
4. Suavizar el oscilador resultante dos veces usando los filtros Cronex controlados por `SmoothingMethod`, `FastPeriod`, `SlowPeriod` y `Phase`. Estos dos valores suavizados corresponden a las líneas "rápida" y "señal" en el indicador original.
5. Mirar atrás `SignalBar` velas completadas y comparar ambas líneas Cronex en esa barra y en la anterior.
6. Cuando la línea rápida está por encima de la lenta, la estrategia opcionalmente cierra posiciones cortas y, si `BuyOpenEnabled` es verdadero, abre una posición larga si se detectó un cruce ascendente fresco en la barra de retroceso.
7. Cuando la línea rápida está por debajo de la lenta, se ejecutan las acciones opuestas para operaciones cortas, controladas por `SellOpenEnabled` y `BuyCloseEnabled`.
8. Cada vez que se abre una nueva posición, las órdenes de stop-loss y take-profit (expresadas en puntos) se recalculan con `StopLoss` y `TakeProfit`.

Solo se mantiene una única posición neta. Si la dirección de la señal cambia, la estrategia combina el volumen necesario para cerrar la posición actual con el nuevo tamaño de operación para imitar el comportamiento de netting de MetaTrader.

## Indicadores y opciones de suavizado

- **Oscilador Chaikin**: Construido aplicando el tipo de media móvil `ChaikinMethod` seleccionado a la línea de acumulación/distribución. Las opciones disponibles incluyen medias simples, exponenciales, suavizadas y linealmente ponderadas.
- **Suavizadores Cronex**: El parámetro `SmoothingMethod` expone la familia Cronex XMA (SMA, EMA, SMMA, LWMA, Jurik JJMA/JurX, Parabolic MA, T3, VIDYA, AMA). El parámetro `Phase` influye en los filtros basados en Jurik exactamente como en la implementación MQL.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `CandleType` | Tipo de datos de las velas usadas para calcular el indicador. Por defecto es un marco temporal de cuatro horas. |
| `ChaikinMethod` | Método de media móvil usado dentro del oscilador Chaikin. |
| `ChaikinFastPeriod` / `ChaikinSlowPeriod` | Períodos rápido y lento aplicados a la línea de acumulación/distribución. |
| `SmoothingMethod` | Algoritmo de suavizado Cronex aplicado a los valores del oscilador Chaikin. |
| `FastPeriod` / `SlowPeriod` | Longitudes de las líneas Cronex rápida y lenta. |
| `Phase` | Parámetro de fase para suavizadores basados en Jurik (rango -100 a +100). |
| `VolumeSource` | Selecciona volumen tick o real al calcular la línea de acumulación/distribución. |
| `SignalBar` | Número de barras completadas hacia atrás que debe contener la señal de cruce. |
| `BuyOpenEnabled` / `SellOpenEnabled` | Activar o desactivar la apertura de operaciones largas o cortas. |
| `BuyCloseEnabled` / `SellCloseEnabled` | Permitir cerrar la posición opuesta cuando aparece una señal inversa. |
| `TakeProfit` / `StopLoss` | Distancias de objetivo de beneficio y stop protector en puntos del instrumento aplicadas tras cada entrada. |
| `Volume` | Tamaño de posición estándar de StockSharp (actúa como tamaño de lote en el experto original). |

## Diferencias respecto a la versión MQL

- Las rutinas de gestión monetaria y deslizamiento de `TradeAlgorithms.mqh` son reemplazadas por los asistentes integrados `Volume`, `SetStopLoss` y `SetTakeProfit`.
- La implementación de StockSharp recalcula la línea AD solo en velas finalizadas, asegurando un comportamiento determinista para pruebas y trading en vivo.
- Las opciones de suavizado Cronex se basan en indicadores de StockSharp: los filtros Jurik están respaldados por `JurikMovingAverage` (con control de fase), mientras que VIDYA y ParMA usan aproximaciones exponenciales consistentes con otras conversiones Cronex.
