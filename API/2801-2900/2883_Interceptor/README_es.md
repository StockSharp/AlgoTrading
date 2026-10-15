# Estrategia Interceptor (Port StockSharp)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Interceptor es un port en C# del asesor experto original de MetaTrader5. Combina la alineación de "abanico" EMA multitemporal con osciladores Stochastic, detección de ruptura de rango plano, análisis de divergencia, filtros de vela de martillo y confirmación de cuerno (convergencia de abanico). El objetivo es explotar la continuación de tendencia fuerte tras períodos de consolidación en el gráfico de GBP/USD de 5 minutos.

## Lógica central
- **Estructura de tendencia** – La estrategia evalúa medias móviles exponenciales (longitudes 34/55/89/144/233) en los marcos temporales M5, M15 y H1. Una tendencia válida requiere que todos los abanicos EMA estén alineados (ascendente para alcista, descendente para bajista) y que la distancia máxima entre la EMA más lenta y más rápida permanezca por debajo de umbrales configurables.
- **Confirmación de momentum** – Los osciladores Stochastic de M5 y M15 deben cruzar desde zonas de sobrecompra/sobreventa para confirmar que el precio está saliendo de zonas de congestión.
- **Filtro de ruptura de rango plano** – Un detector de compresión de volatilidad busca rangos ajustados (longitud y anchura controladas por `FlatnessCoefficient`, `MinFlatBars` y `MaxFlatPoints`). Las rupturas de estas zonas añaden confianza a una señal.
- **Filtro de martillo** – Las velas de martillo o martillo invertido recientes (validadas mediante reglas de cuerpo/sombra larga y máximos/mínimos locales) actúan como señales de agotamiento en la dirección del trade previsto.
- **Verificación de divergencia** – La estrategia busca divergencias alcistas/bajistas entre el precio y el oscilador Stochastic M5 para anticipar reversiones tras la alineación del abanico.
- **Confirmación de cuerno** – Cuando el abanico EMA M5 converge (el "cuerno"), una ruptura por encima/debajo de un rango reciente desencadena entradas adicionales si los marcos temporales superiores apoyan el movimiento.

## Condiciones de entrada
Un setup largo puede ser activado por una o múltiples condiciones (cada una añade peso a la decisión):
1. Abanicos EMA alineados en los tres marcos temporales, cruce alcista Stochastic M5, cuerpo de vela alcista fuerte.
2. Vela de ruptura del abanico EMA M5 que abre en el mínimo y cierra por encima de las EMAs rápidas.
3. Ruptura del rango plano en dirección alcista.
4. Acuerdo de ruptura M5 + M15 mientras las distancias del abanico EMA permanecen por debajo de los umbrales permitidos.
5. Divergencia alcista entre Stochastic y precio mientras los abanicos apuntan hacia arriba.
6. Vela de martillo alcista reciente dentro de la ventana de retrospectiva permitida.
7. Cruce alcista Stochastic M15 con cuerpos de vela alcista.
8. Ruptura de cuerno por encima del rango reciente después de que el abanico EMA converge.

Los setups cortos siguen la lógica espejada. Si tanto las condiciones largas como cortas están simultáneamente presentes, la estrategia omite el trading para esa barra.

## Salida y gestión de riesgo
- Stop-loss y take-profit fijos configurables en puntos.
- Lógica de punto de equilibrio opcional (`StopLossAfterBreakeven`, `TakeProfitAfterBreakeven`) que estrecha el stop una vez que el precio alcanza un umbral de beneficio.
- Trailing stop basado en la distancia del precio desde el último cierre (`TrailingDistancePoints` con `TrailingStepPoints`).
- Cuando se abre una nueva posición, la estrategia cierra primero cualquier posición opuesta existente.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `Volume` | Volumen de la orden usado para cada entrada. |
| `FlatnessCoefficient` | Multiplicador que controla la anchura máxima permitida de un rango plano detectado. |
| `StopLossPoints` | Distancia inicial del stop-loss en puntos de precio. |
| `TakeProfitPoints` | Distancia inicial del take-profit en puntos de precio (0 deshabilita). |
| `TakeProfitAfterBreakeven` | Beneficio requerido (puntos) antes de que se active la lógica de punto de equilibrio. |
| `StopLossAfterBreakeven` | Distancia del stop de punto de equilibrio una vez activado. |
| `MaxFanDistanceM5/M15/H1` | Máxima dispersión EMA permitida en cada marco temporal. |
| `StochasticKPeriodM5/M15` | Longitud %K para los osciladores Stochastic en M5 y M15. |
| `StochasticUpperM5/M15` | Umbrales de sobrecompra. |
| `StochasticLowerM5/M15` | Umbrales de sobreventa. |
| `MinBodyPoints` | Tamaño mínimo del cuerpo de vela para calificar como barra fuerte. |
| `MinFlatBars` | Barras mínimas requeridas para definir un rango plano. |
| `MaxFlatPoints` | Anchura máxima del rango plano (puntos). |
| `MinDivergenceBars` | Separación mínima entre pivotes de divergencia. |
| `HammerLongShadowPercent` | Porcentaje mínimo de sombra larga para detección de martillo. |
| `HammerShortShadowPercent` | Porcentaje máximo de sombra opuesta para detección de martillo. |
| `HammerMinSizePoints` | Rango total mínimo de la vela de martillo. |
| `HammerLookbackBars` | Ventana de retrospectiva para buscar patrones de martillo. |
| `HammerRangeBars` | Número de barras usadas para validar máximos/mínimos de martillo. |
| `MaxFanWidthAtNarrowest` | Dispersión EMA máxima cuando el abanico se considera convergido. |
| `FanConvergedBars` | Número de barras que el abanico puede permanecer convergido para señales de cuerno. |
| `RangeBreakLookback` | Ventana de retrospectiva para detección de ruptura de rango. |
| `TrailingStepPoints` | Incremento mínimo para ajustes del trailing stop. |
| `TrailingDistancePoints` | Distancia entre el precio y el trailing stop. |
| `CandleType` | Serie de velas primaria (predeterminado velas de tiempo M5). |

## Notas de uso
- El asesor experto original fue diseñado para gráficos GBP/USD M5. Los parámetros pueden necesitar ajuste para otros instrumentos o marcos temporales.
- La estrategia requiere la API de alto nivel de StockSharp y datos de velas para intervalos M5, M15 y H1.
- Solo se mantiene una posición neta; las posiciones opuestas se cierran antes de abrir nuevos trades.

## Aviso legal
La estrategia se proporciona con fines educativos. El rendimiento pasado no garantiza resultados futuros. Siempre valide los parámetros y la lógica en un entorno de prueba seguro antes de operar con capital real.
