# Estrategia Momentum de filtro de tiempo de Russian20
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia Momentum de filtro de tiempo Russian20** es una conversión del MetaTrader 4 asesor experto `Russian20-hp1.mq4`, distribuido originalmente por Gordago Software Corp. El algoritmo combina una media móvil simple de 20 períodos (SMA) con un indicador Momentum de 5 períodos evaluado en velas de 30 minutos. Las posiciones solo se abren cuando el impulso del precio y la dirección de la tendencia se alinean, opcionalmente restringidas a una ventana de negociación intradiaria definida por el usuario.

## Lógica de trading
- **Frecuencia de datos:** Utiliza el tipo de vela configurable (predeterminado: velas de 30 minutos, que coinciden con `PERIOD_M30` del script MT4). Todas las señales se evalúan únicamente en velas completamente cerradas para permanecer fieles a la ejecución de cierre de barra del experto original.
- **Indicadores:**
  - Media móvil simple con longitud ajustable (predeterminado 20).
  - Indicador de impulso con retrospectiva configurable (predeterminado 5) y un nivel neutral establecido en 100, como en MetaTrader.
- **Entrada larga:** Se activa cuando las siguientes condiciones se alinean en la última barra cerrada:
  1. El precio de cierre está por encima del SMA.
  2. El impulso se imprime por encima del umbral neutral (predeterminado 100).
  3. El precio de cierre actual es más alto que el cierre de la vela anterior.
- **Entrada breve:** Se activa cuando:
  1. El precio de cierre está por debajo del SMA.
  2. El impulso está por debajo del umbral neutral.
  3. El precio de cierre actual es más bajo que el cierre anterior.
- **Reglas de salida:**
  - Las posiciones largas se cierran cuando Momentum vuelve a caer al umbral o por debajo de él o cuando se alcanza el objetivo de obtención de beneficios (si está habilitado).
  - Las posiciones cortas se cierran cuando el Momentum alcanza o supera el umbral o cuando se alcanza el objetivo de obtención de beneficios.

## Filtro de sesión
El script MetaTrader ofrecía una ventana de negociación opcional (predeterminada de 14:00 a 16:00). El puerto StockSharp expone el mismo comportamiento a través de los parámetros `UseTimeFilter`, `StartHour` y `EndHour`. Cuando el filtro está activo, la estrategia omite tanto las entradas como las salidas fuera de las horas seleccionadas, reflejando la lógica de retorno anticipado del experto original.

## Gestión del riesgo
La versión MQL4 adjuntó una ganancia fija de 20 pips a cada pedido. La conversión mantiene esta característica y expresa la distancia en "pips", ajustándose automáticamente al precio de pips fraccionarios (3/5 decimales) a través del `PriceStep` del instrumento. Establecer `TakeProfitPips` en cero desactiva por completo el objetivo de ganancias.

## Parámetros
| Parámetro | Predeterminado | Descripción |
|-----------|---------|-------------|
| `CandleType` | velas de 30 minutos | Tipo de datos utilizado para los cálculos de precio/indicador. |
| `MovingAverageLength` | 20 | Búsqueda retrospectiva del filtro de tendencias SMA. |
| `MomentumPeriod` | 5 | Búsqueda retrospectiva del indicador Momentum. |
| `MomentumThreshold` | 100 | Nivel de impulso neutro utilizado para entradas y salidas. |
| `TakeProfitPips` | 20 | Distancia objetivo de ganancias en pips. Cero desactiva el objetivo. |
| `UseTimeFilter` | falso | Habilita el filtro de sesión de negociación intradiaria. |
| `StartHour` | 14 | Hora de inicio incluida de la ventana de negociación (0–23). |
| `EndHour` | 16 | Hora de finalización incluida de la ventana de negociación (0–23). |

Todos los parámetros se definen a través de `StrategyParam<T>`, manteniéndolos visibles en la interfaz de usuario y listos para la optimización.

## Notas de implementación
- Utiliza el `SubscribeCandles().Bind(...)` API de alto nivel para que los valores del indicador se transmitan directamente a la rutina de procesamiento sin administración manual de series.
- Almacena solo el último precio de cierre para comparar velas consecutivas, evitando consultas históricas pesadas y cumpliendo con las pautas de rendimiento del repositorio.
- Vuelve a calcular automáticamente el multiplicador de pips desde `Security.PriceStep`, lo que garantiza distancias correctas de obtención de beneficios entre los símbolos Forex con precios de 4/5 dígitos.
- Agrega ganchos de representación de gráficos opcionales (`DrawCandles`, `DrawIndicator`, `DrawOwnTrades`) para un análisis visual conveniente cuando el entorno del host lo admite.

## Consejos de uso
- Alinee el tipo de vela con el período de tiempo en el que desea operar; Para los pares de Forex, la configuración original de 30 minutos es un punto de partida razonable.
- Cuando `UseTimeFilter` esté habilitado, asegúrese de que `StartHour` sea menor o igual a `EndHour`. Establecer la hora de inicio más tarde que la hora de finalización desactiva efectivamente el comercio porque la lógica MT4 simplemente omitió el procesamiento fuera del intervalo especificado.
- Dado que el experto nunca utilizó un stop-loss, considere combinar la estrategia con controles de riesgo adicionales (manuales o mediante StockSharp funciones de protección) cuando opere con capital real.
