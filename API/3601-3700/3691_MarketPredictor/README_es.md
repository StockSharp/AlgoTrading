# Estrategia de predicción del mercado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Market Predictor es una adaptación de alto nivel del asesor experto original MetaTrader MarketPredictor. La lógica se centra en reestimar continuamente el movimiento de precios esperado combinando un pronóstico de Monte Carlo con parámetros estadísticos adaptativos recopilados de velas recientes. La estrategia se suscribe a velas del período de tiempo seleccionado y procesa solo barras terminadas para evitar señales prematuras.

## Conceptos básicos
- **Estimación de la media adaptativa:** La estrategia mantiene un precio medio dinámico (`mu`) actualizado a partir de una media móvil simple. Esto refleja el paso de optimización de parámetros del asesor experto original.
- **Amplitud impulsada por la volatilidad:** El ATR de la misma serie de velas controla el coeficiente de amplitud (`alpha`), lo que mantiene la predicción sensible a los picos de volatilidad.
- **Proyección de Monte Carlo:** Para cada vela completada, la estrategia ejecuta un número configurable de simulaciones aleatorias para estimar el precio esperado (`P_t1`). El pronóstico es igual al promedio de los precios simulados.
- **Decisión direccional:** Las órdenes de mercado se envían cuando el pronóstico se desvía del último cierre en más del umbral `sigma`. La dirección de la posición se invierte sólo después de que la exposición anterior esté completamente cerrada.

## Reglas de trading
1. Espere a que termine la vela y confirme que todos los indicadores estén formados.
2. Actualice `mu` con el valor SMA y `alpha` con la amplitud basada en ATR.
3. Realice simulaciones de Monte Carlo en torno al último precio de cierre.
4. Si el precio promedio simulado es superior a `Close + sigma`, ingrese una posición larga con una orden de mercado cuando no haya ninguna posición abierta.
5. Si el precio promedio simulado es inferior a `Close - sigma`, ingrese una posición corta con una orden de mercado cuando no haya ninguna posición abierta.
6. Mantenga la posición hasta que se produzca la señal opuesta.

## Parámetros
- **InitialAlpha**: amplitud predeterminada utilizada antes de que ATR esté disponible.
- **InitialBeta**: coeficiente de marcador de posición mantenido por compatibilidad con el Asesor Experto original (no se utiliza directamente en los cálculos).
- **InitialGamma**: constante de amortiguación del marcador de posición conservada para mantener la coherencia de la documentación (no se usa directamente).
- **Kappa**: parámetro de sensibilidad para el concepto del componente sigmoideo subyacente. Se almacena para referencia y futuras extensiones.
- **InitialMu** – Precio medio predeterminado hasta que se forma la media móvil.
- **Sigma**: desviación requerida entre el precio previsto y el último cierre para activar las entradas al mercado.
- **MonteCarloSimulaciones**: número de simulaciones utilizadas para estimar el próximo precio.
- **CandleType**: período de tiempo de la serie de velas.

## Notas
- El StockSharp API de alto nivel maneja las suscripciones de velas, la vinculación de indicadores y la ejecución de órdenes de mercado.
- Los comentarios en el código fuente explican cada paso del proceso para facilitar el mantenimiento.
- El puerto de Python se omite intencionalmente según lo solicitado.
