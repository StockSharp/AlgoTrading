# Estrategia MasterMind 2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
MasterMind 2 es una conversión del asesor experto "TheMasterMind2" MQL4. La estrategia espera valores extremos en los indicadores Stochastic Oscilador y Williams %R para detectar puntos de agotamiento. Cuando ambos indicadores muestran condiciones extremas de sobreventa, se abre una posición larga, y cuando ambos muestran condiciones extremas de sobrecompra, se abre una posición corta. La lógica funciona únicamente con velas completamente cerradas, imitando el comportamiento original del Asesor Experto.

## Indicadores
- **Stochastic Oscilador**: configurado con una mirada retrospectiva larga para medir los niveles de sobrecompra y sobreventa. La línea de señal %D se compara con los umbrales.
- **Williams %R**: confirma la fuerza del extremo al requerir lecturas cercanas a -100 para posiciones largas y cercanas a 0 para posiciones cortas.

## Reglas de entrada
1. Espere a que se cierre una vela.
2. Calcule el oscilador Stochastic y tome su valor de señal %D.
3. Calcule Williams %R sobre la retrospectiva configurada.
4. **Entrada larga**: si `%D < 3` y `Williams %R < -99.9`, cierre cualquier exposición corta existente y compre.
5. **Entrada corta**: si `%D > 97` y `Williams %R > -0.1`, cierre cualquier exposición larga existente y venda.

## Reglas de salida
- Los niveles de stop loss y takeprofit se aplican en relación con el precio de entrada utilizando distancias de puntos configurables.
- El trailing stop puede reforzar el stop de protección una vez que el precio se mueve favorablemente en el paso especificado.
- Una opción de equilibrio mueve el stop loss al nivel de entrada después de que la operación acumula la distancia de beneficio requerida.
- Las señales opuestas cierran inmediatamente la posición actual antes de abrir una nueva.

## Parámetros
- `Trade Volume`: volumen de contrato presentado con cada orden de mercado.
- `Stochastic Period`, `Stochastic %K`, `Stochastic %D` – parámetros del oscilador Stochastic.
- `Williams %R Period`: período retroactivo para el cálculo del Williams %R.
- `Stop Loss`, `Take Profit` – distancias de protección en puntos de precio.
- `Trailing Stop`, `Trailing Step` – controla la gestión de paradas dinámicas.
- `Break Even`: distancia en puntos necesarios para fijar el precio de entrada.
- `Candle Type`: período de tiempo o tipo de vela personalizado utilizado en los cálculos.

## Notas
- La estrategia se basa exclusivamente en velas terminadas, que coinciden con la implementación original de MQL4.
- Todas las órdenes se emiten en el mercado con un volumen definido por `Trade Volume`.
- Habilite o deshabilite las funciones de protección estableciendo los parámetros de distancia en cero.
