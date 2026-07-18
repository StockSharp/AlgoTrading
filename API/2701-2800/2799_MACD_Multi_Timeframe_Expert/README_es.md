# Estrategia MACD Experto Multi-Período
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia replica el robot original "MACD Expert" de MetaTrader dentro del framework StockSharp. Sincroniza las tendencias del MACD a través de cuatro marcos temporales—5 minutos, 15 minutos, 1 hora y 4 horas—y solo permite una nueva posición cuando todos los marcos temporales apuntan en la misma dirección. El objetivo es capturar la alineación de momentum multi-período mientras se filtran los períodos de spread alto.

## Datos e indicadores
- **Velas**: 5m (ejecución), 15m, 1h y 4h confirmaciones. Todas las velas usan precios de cierre y solo barras terminadas.
- **Indicador**: `MovingAverageConvergenceDivergenceSignal` con valores predeterminados 12/26/9. Cada marco temporal tiene su propia instancia de MACD para que las señales no interfieran.
- **Cotizaciones de Nivel 1**: Se consumen las mejores cotizaciones de oferta/demanda para monitorear el spread en vivo antes de abrir operaciones.

## Lógica de trading
1. Esperar a que las cuatro instancias de MACD emitan un valor completado.
2. Calcular la relación entre la línea MACD y la línea de señal en cada marco temporal.
3. Aplicar un filtro de spread máximo medido en puntos de precio (pasos de precio).
4. Abrir como máximo una posición a la vez; las posiciones existentes deben finalizar mediante stop-loss o take-profit antes de que se permita una nueva orden.

### Configuración larga
- La línea de señal MACD está por encima de la línea MACD en **todos** los marcos temporales monitoreados.
- El spread no excede `MaxSpreadPoints`.
- Se abre una posición larga con `OrderVolume` lotes al cierre de la última vela de 5 minutos.

### Configuración corta
- La línea de señal MACD está por debajo de la línea MACD en **todos** los marcos temporales monitoreados.
- El spread no excede `MaxSpreadPoints`.
- Se abre una posición corta con `OrderVolume` lotes al cierre de la última vela de 5 minutos.

### Gestión de posición
- Las operaciones largas colocan objetivos lógicos a `TakeProfitPoints` por encima de la entrada y stops a `StopLossPoints` por debajo.
- Las operaciones cortas colocan objetivos lógicos a `TakeProfitPoints` por debajo de la entrada y stops a `StopLossPoints` por encima.
- Las salidas se activan cuando el máximo/mínimo intrabarra de una vela de 5 minutos terminada toca el objetivo o nivel de stop respectivo.
- Mientras está en posición, la estrategia ignora las señales opuestas; espera hasta que la operación se cierra por stop o take-profit antes de reaccionar nuevamente, coincidiendo con la lógica MQL original.

## Parámetros
| Nombre | Valor predeterminado | Descripción |
| --- | --- | --- |
| `OrderVolume` | 0.1 | Tamaño de la posición en lotes (refleja el input `Lots` de la versión MQL). |
| `StopLossPoints` | 200 | Distancia al stop de protección en puntos de precio. |
| `TakeProfitPoints` | 400 | Distancia al objetivo de beneficio en puntos de precio. |
| `MaxSpreadPoints` | 20 | Spread máximo permitido en puntos de precio antes de que se omitan las entradas. |
| `FastPeriod` | 12 | Longitud de la EMA rápida dentro de cada instancia de MACD. |
| `SlowPeriod` | 26 | Longitud de la EMA lenta dentro de cada instancia de MACD. |
| `SignalPeriod` | 9 | Longitud de la EMA de señal dentro de cada instancia de MACD. |
| `FiveMinuteCandleType` | Velas de 5 minutos | Marco temporal de ejecución principal. |
| `FifteenMinuteCandleType` | Velas de 15 minutos | Primer marco temporal de confirmación. |
| `HourCandleType` | Velas de 1 hora | Segundo marco temporal de confirmación. |
| `FourHourCandleType` | Velas de 4 horas | Tercer marco temporal de confirmación. |

## Notas de implementación
- Usa `BindEx` para leer valores de MACD con tipo fuerte sin llamar a `GetValue`, siguiendo las directrices del proyecto.
- Un ayudante compartido convierte la relación MACD/señal en banderas `{-1, 0, 1}` para simplificar las verificaciones de confirmación.
- La validación del spread divide la mejor demanda menos la mejor oferta por `Security.PriceStep` para que el umbral coincida con el comportamiento de "puntos" de MetaTrader.
- Los eventos de operación se registran con `LogInfo` para facilitar la depuración cuando se prueba en Designer o Runner.
- No se proporciona traducción de Python, según los requisitos de la tarea; solo se incluye la versión C#.
