# Estrategia de Puria Method
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Puria Method es un sistema de seguimiento de tendencia diseñado originalmente para MetaTrader. Combina tres medias móviles con un filtro de tendencia MACD para detectar rupturas de Momentum. La conversión a StockSharp mantiene la lógica de entrada original y agrega controles de riesgo modernos como toma de ganancias parciales y stops trailing automatizados.

## Lógica de trading
- Calcular tres medias móviles usando métodos de suavizado y fuentes de precio configurables.
- Evaluar la diferencia entre la MA de referencia más lenta y las dos MA más rápidas en la barra anterior. Una señal alcista requiere que ambas MA rápidas estén al menos 0,5 puntos por encima de la referencia; una señal bajista requiere que la referencia lidere por el mismo margen.
- Confirmar la dirección del mercado con la línea principal del MACD. Las operaciones largas requieren que el valor MACD anterior sea positivo y que el historial MACD reciente sea no decreciente durante el número de barras configurado. Las operaciones cortas requieren las condiciones opuestas.
- Cuando se activa una entrada, la estrategia cierra una posición opuesta (si existe) y abre una nueva posición neta en la dirección de la señal.

## Gestión de riesgo
- **Stop Loss / Take Profit:** Los precios se calculan desde la entrada usando distancias en pips y se normalizan al paso de precio del instrumento.
- **Trailing Stop:** Una vez que la posición supera el umbral trailing más el paso, el stop se avanza con cada paso trailing adicional.
- **Salida parcial:** Después de que el precio recorra una distancia mínima de ganancia, se cierra una fracción configurable de la posición para asegurar ganancias.
- **Gestión de posición:** El algoritmo realiza un seguimiento del precio más alto (largo) o más bajo (corto) después de la entrada para activar las reglas de stop o ganancia cuando las velas perforan esos niveles.

## Parámetros
| Nombre | Descripción |
| ---- | ----------- |
| `StopLossPips` | Distancia del stop loss en pips. |
| `TakeProfitPips` | Distancia del take profit en pips. |
| `TrailingStopPips` | Distancia del trailing stop en pips. |
| `TrailingStepPips` | Avance mínimo de ganancia antes de actualizar el trailing stop. |
| `MinProfitStepPips` | Distancia mínima en pips antes de tomar ganancia parcial. |
| `MinProfitFraction` | Fracción de la posición a cerrar cuando se alcanza el paso mínimo de ganancia. |
| `CandleType` | Serie de velas primaria utilizada por la estrategia. |
| `Ma0Period`, `Ma1Period`, `Ma2Period` | Períodos para las tres medias móviles. |
| `Ma0Shift`, `Ma1Shift`, `Ma2Shift` | Desplazamientos de barra opcionales aplicados a cada media móvil. |
| `Ma0Method`, `Ma1Method`, `Ma2Method` | Métodos de suavizado de medias móviles (simple, exponencial, suavizado, ponderado linealmente). |
| `Ma0Price`, `Ma1Price`, `Ma2Price` | Fuentes de precio de vela para las medias móviles. |
| `MacdFastPeriod`, `MacdSlowPeriod`, `MacdSignalPeriod` | Configuración del MACD. |
| `MacdTrendBars` | Número de barras para verificar la tendencia monotónica del MACD (mínimo 3). |
| `MacdPrice` | Fuente de precio de vela para el cálculo del MACD. |

## Notas
- La estrategia usa la barra completada anterior para las comparaciones de MA y MACD para evitar depender de datos de velas no terminadas.
- El tamaño del pip se deriva automáticamente del paso de precio del instrumento y la precisión decimal.
- Las funciones de trailing y salida parcial requieren valores de configuración distintos de cero; de lo contrario, los bloques correspondientes permanecen inactivos.
- La versión convertida depende únicamente de velas terminadas (`CandleStates.Finished`) y debe usarse con una serie de velas que coincida con el marco temporal del gráfico original.
