# Estrategia simple de Kloss
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **Estrategia Simple de Kloss** es una conversión directa del MetaTrader 4 asesores expertos `Kloss_.mq4`. Reconstruye la idea comercial original utilizando el nivel alto API de StockSharp y mantiene el conjunto de indicadores idéntico: un promedio móvil exponencial (EMA) calculado sobre los precios de cierre ponderados, el índice del canal de productos básicos (CCI) y el oscilador Stochastic. Las señales se generan a partir de la vela completada anteriormente, reflejando la lógica de cambio de una barra en la versión MQL. El tamaño de la posición puede depender de un volumen de orden fijo o de un porcentaje de riesgo del valor de la cartera, al igual que las reglas de cálculo de lotes originales.

## Idea central

1. Supervise el contexto del impulso con umbrales **CCI** y **Stochastic** alrededor de sus niveles neutrales.
2. Confirme las señales de impulso con un **EMA** a corto plazo del precio de cierre ponderado.
3. Ingrese posiciones solo cuando la vela anterior cumpla todas las condiciones de la señal, evitando operaciones prematuras con datos de mercado incompletos.
4. Permita múltiples entradas en la misma dirección hasta un límite configurable, emulando el parámetro "MaxOrders" del script MT4.

## Configuración del indicador

- **EMA (MaPeriod)**: utiliza el cierre ponderado `(Close * 2 + High + Low) / 4` para hacer coincidir `PRICE_WEIGHTED` de MetaTrader. Actúa como un filtro de tendencias a corto plazo.
- **CCI (CciPeriod)**: Evalúa las desviaciones del impulso del precio medio. El umbral `±CciLevel` define entradas agresivas versus conservadoras.
- **Stochastic (StochasticKPeriod / DPeriod / Smooth)**: Utiliza la línea principal %K para detectar condiciones de sobrecompra o sobreventa en relación con el nivel neutral 50. La desviación de 50 está controlada por `StochasticLevel`.

Todos los indicadores operan en la serie de velas primaria definida por `CandleType`. La estrategia actualiza los valores del indicador solo en las velas terminadas, lo que garantiza un backtesting estable y un comportamiento en vivo.

## Lógica de trading

### Configuración larga

1. El cierre de la vela anterior está por encima del valor EMA anterior.
2. El valor anterior de CCI está por debajo de `-CciLevel`, lo que indica un impulso de sobreventa.
3. El valor %K anterior de Stochastic está por debajo de `50 - StochasticLevel`, lo que confirma la oscilación de sobreventa.
4. Cuando las condiciones se mantienen, cualquier exposición corta se cierra y se abre una nueva posición larga, siempre que el número de órdenes largas existentes sea inferior a `MaxOrders`.

### Configuración corta

1. El cierre de la vela anterior está por debajo del valor EMA anterior.
2. El valor anterior de CCI está por encima de `+CciLevel`, lo que indica un impulso de sobrecompra.
3. El valor %K anterior de Stochastic está por encima de `50 + StochasticLevel`, lo que confirma la oscilación de sobrecompra.
4. Cuando las condiciones se mantienen, cualquier exposición larga se cierra y se abre una nueva posición corta, sujeta al límite `MaxOrders`.

### Gestión de salidas

- **Stop Loss / Take Profit**: Distancias absolutas opcionales en puntos del instrumento. Si cualquiera de los valores es mayor que cero, se activa la protección de posición incorporada de StockSharp.
- **Señal opuesta**: antes de abrir en la dirección opuesta, la posición actual se cierra para imitar al asesor experto original.

## Dimensionamiento de posiciones

- **OrderVolume**: tamaño fijo predeterminado que replica el parámetro `Lots` de MT4.
- **RiskPercentage**: cuando es mayor que cero, la estrategia calcula el tamaño de la operación como un porcentaje del valor de la cartera. Utiliza requisitos de margen del instrumento cuando están disponibles; de lo contrario, recurre al tamaño basado en el precio, reproduciendo el comportamiento `Lots == 0` del código MQL.
- **MaxOrders**: limita el volumen acumulado por dirección permitiendo hasta `MaxOrders * OrderVolume` exposición.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `OrderVolume` | Tamaño de pedido base utilizado cuando `RiskPercentage` es cero. |
| `MaPeriod` | La duración del EMA se basa en precios de cierre ponderados. |
| `CciPeriod` | Número de barras utilizadas en el cálculo CCI. |
| `CciLevel` | Umbral absoluto CCI para la generación de señal. |
| `StochasticKPeriod` | Búsqueda retrospectiva de la línea Stochastic %K. |
| `StochasticDPeriod` | Período de media móvil para la línea %D. |
| `StochasticSmooth` | Suavizado adicional aplicado a %K. |
| `StochasticLevel` | Desviación de 50 utilizada para la detección de sobrecompra/sobreventa. |
| `MaxOrders` | Número máximo de entradas permitidas por dirección. |
| `StopLossPoints` | Distancia de stop loss opcional en puntos de precio. |
| `TakeProfitPoints` | Distancia de toma de ganancias opcional en puntos de precio. |
| `RiskPercentage` | Porcentaje de cartera para dimensionamiento dinámico de posiciones. |
| `CandleType` | Serie de velas utilizada para todos los cálculos. |

## Notas prácticas

- Funciona mejor con datos intradiarios donde los osciladores de corto plazo reaccionan rápidamente a las oscilaciones de precios.
- El precio de cierre ponderado mantiene la capacidad de respuesta del EMA y al mismo tiempo incorpora el rango alto/bajo de la vela.
- Debido a que cada decisión se basa en la vela anterior, la estrategia evita el repintado dentro de la barra y permanece determinista en las pruebas históricas.
- La gestión de riesgos debe estar alineada con las especificaciones del contrato del corredor para que `OrderVolume` y `MaxOrders` correspondan a tamaños de operaciones ejecutables.
