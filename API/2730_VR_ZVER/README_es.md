# Estrategia VR ZVER
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción General
La estrategia VR ZVER es un sistema de seguimiento de tendencia que combina tres capas de confirmación: una pila de EMA rápida/lenta/muy lenta, el Oscilador Estocástico y el Índice de Fuerza Relativa (RSI). Todos los filtros activos deben estar de acuerdo antes de que se abra una posición, lo que ayuda a evitar operaciones durante regímenes de mercado agitados y contradictorios. La conversión mantiene la lógica de break-even y protección original mientras usa la API de alto nivel de StockSharp.

## Detección del Régimen de Mercado
1. **Estructura EMA** – La configuración predeterminada usa medias móviles exponenciales con períodos 3, 5 y 7. Un sesgo largo requiere que la EMA rápida esté por encima de la EMA lenta y que la EMA lenta permanezca por encima de la EMA muy lenta. Un sesgo corto invierte esta relación.
2. **Oscilador Estocástico** – El par %K/%D se inspecciona tanto para dirección como para nivel. Las operaciones largas requieren que %K esté por debajo de la banda inferior y por encima de %D, señalando un rebote desde sobreventa. Las operaciones cortas requieren que %K esté por encima de la banda superior y por debajo de %D, apuntando a una reversión desde sobrecompra.
3. **Filtro RSI** – El RSI debe estar por debajo del umbral inferior para permitir entradas largas o por encima del umbral superior para habilitar operaciones cortas.

Solo cuando cada filtro habilitado se alinea, la estrategia envía una orden de mercado usando el volumen configurado.

## Gestión de Riesgo
- **Stop Loss** – Cada entrada proyecta un stop basado en precio usando la configuración `StopLossPips` multiplicada por el tamaño de pip del instrumento. Las posiciones largas salen cuando el mínimo de la vela perfora el stop, mientras que las posiciones cortas cierran si el máximo de la vela alcanza su stop.
- **Take Profit** – Se aplica un nivel de take-profit simétrico. Si la vela actual alcanza el objetivo a favor de la operación, la posición se cierra inmediatamente.
- **Protección de Break-Even** – Después de que el precio avanza la distancia `BreakevenPips`, se arma un modo de break-even. Cualquier retroceso de vuelta al precio de entrada aplanará la posición para preservar el capital.
- **Limpieza de Órdenes** – Todas las órdenes activas se cancelan antes de abrir una nueva operación para evitar apilamiento no intencionado.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `CandleType` | Serie de velas usada para los cálculos. |
| `UseMovingAverage` | Habilita o deshabilita el filtro de tendencia EMA. |
| `FastMaPeriod`, `SlowMaPeriod`, `VerySlowMaPeriod` | Períodos para las EMAs rápida, lenta y muy lenta. |
| `UseStochastic` | Activa la capa de confirmación Estocástica. |
| `StochasticKPeriod`, `StochasticDPeriod`, `StochasticSlowing` | Configuraciones de período para el Oscilador Estocástico. |
| `StochasticUpperLevel`, `StochasticLowerLevel` | Umbrales de sobrecompra y sobreventa para %K. |
| `UseRsi` | Habilita o deshabilita la capa de confirmación RSI. |
| `RsiPeriod` | Período de promediado RSI. |
| `RsiUpperLevel`, `RsiLowerLevel` | Umbrales RSI que definen regiones de sobrecompra/sobreventa. |
| `StopLossPips`, `TakeProfitPips` | Distancias (en pips) para la colocación de stop-loss y take-profit. |
| `BreakevenPips` | Progreso de precio requerido antes de activar la protección de break-even. |
| `Volume` | Cantidad a operar por cada orden de mercado. |

## Notas de Implementación
- El tamaño de pip se deriva del paso de precio del instrumento y el número de decimales. Los instrumentos con 3 o 5 lugares decimales aplican automáticamente el ajuste estándar 10x usado en la versión MQL original.
- Todos los datos del indicador se acceden a través de `BindEx`, asegurando que la estrategia reaccione solo a velas completadas con valores de indicador finalizados.
- La estrategia es plana por defecto; las posiciones nunca se invierten sin cerrar primero la exposición existente.
