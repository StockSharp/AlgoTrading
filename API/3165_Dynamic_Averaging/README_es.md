# Estrategia de Dynamic Averaging
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Dynamic Averaging es un port directo del asesor experto de MetaTrader 5 "Dynamic averaging.mq5" (id 23319). La estrategia combina un oscilador Stochastic rápido con un filtro de volatilidad basado en la desviación estándar. Las operaciones solo se permiten mientras la volatilidad del mercado permanece por debajo de su promedio móvil, forzando las entradas durante consolidaciones donde los reversos del Stochastic son más fiables.

## Parámetros
- **TradeVolume** – tamaño de orden para cada nueva entrada. Se duplica automáticamente tras una secuencia perdedora y se restablece tras una ganadora.
- **MinimumProfit** – beneficio flotante (en moneda de cuenta) que cierra todas las posiciones abiertas una vez superado.
- **SlidingWindowDays** – número de días calendario utilizados para promediar los valores de desviación estándar y construir la línea base de volatilidad.
- **StochasticKPeriod** – número de barras para el cálculo de %K.
- **StochasticDPeriod** – longitud de suavizado para la línea %D.
- **StochasticSlowPeriod** – período de ralentización final para el oscilador Stochastic.
- **StdDevPeriod** – período de retrovisión para el indicador de desviación estándar.
- **CandleType** – velas fuente para los cálculos (por defecto marco temporal de 15 minutos).

## Reglas de negociación
1. La estrategia opera solo con velas terminadas. Al cierre de cada barra los filtros de Stochastic y volatilidad se actualizan mediante `SubscribeCandles().BindEx`.
2. Calcular la volatilidad del mercado usando `StandardDeviation(StdDevPeriod)` y compararla con la volatilidad promedio calculada por `SimpleMovingAverage` sobre las últimas `SlidingWindowDays` barras.
3. Si la desviación estándar actual está por encima del promedio móvil, se salta la barra.
4. Cuando la volatilidad es baja:
   - Entrar **largo** si %K está por debajo de 25 y la pendiente de los dos valores de %K anteriores es positiva (último valor menos el valor hace dos barras).
   - Entrar **corto** si %K está por encima de 75 y la pendiente de los dos valores de %K anteriores es negativa.
5. Las posiciones se revierten enviando suficiente volumen para aplanar el lado opuesto más la nueva exposición de `TradeVolume`.
6. Cuando el PnL flotante de la posición abierta supera `MinimumProfit`, la estrategia sale inmediatamente del mercado.

## Dimensionamiento de posición y recuperación
- El tamaño de orden inicial es igual a `TradeVolume`.
- Después de cerrar la posición, se inspecciona el cambio de PnL realizado.
  - Una **pérdida** duplica el tamaño de la próxima operación (paso `martingala`) para replicar el comportamiento del EA original.
  - Una **ganancia o breakeven** restablece el tamaño al `TradeVolume` base.

## Detalles de implementación
- Las velas, los valores de Stochastic y desviación estándar se procesan a través de la API de alto nivel con `BindEx`, evitando la gestión manual de buffers.
- La ventana deslizante de volatilidad convierte días calendario en conteos de barras usando el marco temporal de velas si está disponible.
- El control de beneficio flotante se basa en el cierre de la vela actual y `PositionAvgPrice`, coincidiendo con la implementación MQL que suma solo el beneficio de posición abierta.
- Todos los comentarios de código están escritos en inglés; no se proporciona versión en Python según los requisitos de la tarea.
