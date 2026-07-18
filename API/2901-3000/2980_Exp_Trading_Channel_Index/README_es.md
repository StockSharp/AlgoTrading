# Estrategia Exp Índice del Canal de Trading
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es un puerto de StockSharp del asesor experto MQL5 `Exp_Trading_Channel_Index`. Sigue el oscilador Trading Channel Index (TCI), un indicador de momentum ajustado por volatilidad que colorea cada barra según su posición respecto a dos niveles de canal. La estrategia reacciona cuando el color asignado a una barra histórica cambia, imitando el comportamiento del asesor experto original.

La implementación se suscribe a una serie de velas configurable (predeterminado: H4) y procesa solo velas finalizadas. Todas las decisiones de gestión de operaciones se toman en la apertura de la siguiente barra tras un cambio de color, igual que en el script original.

## Indicador Trading Channel Index
El TCI se calcula en tres etapas:

1. **Suavizado primario** de la fuente de precio elegida mediante una media móvil configurable (SMA, EMA, SMMA, WMA o Jurik). Esto produce el valor base `XMA`.
2. **Estimación de volatilidad** suavizando la desviación absoluta entre el precio y la línea base.
3. **Normalización** de la desviación mediante el coeficiente configurado y una segunda etapa de suavizado. El valor resultante se compara con los umbrales `HighLevel` y `LowLevel` para asignar uno de cinco códigos de color:
   - `0` (lima) – el valor está por encima de `HighLevel`.
   - `1` (verde azulado) – el valor es positivo pero inferior a `HighLevel`.
   - `2` (gris) – el valor es cercano a cero.
   - `3` (naranja) – el valor es negativo pero superior a `LowLevel`.
   - `4` (dorado) – el valor está por debajo de `LowLevel`.

La versión de StockSharp utiliza clases de indicadores nativas para las medias móviles. Jurik MA respeta la entrada `Phase` mientras que otros métodos la ignoran, coincidiendo con el comportamiento original donde el parámetro de fase solo es significativo para JJMA.

## Reglas de entrada y salida
El algoritmo inspecciona la barra especificada por `SignalBar` (predeterminado 1, es decir, la última vela cerrada) y la barra anterior:

- **Abrir largo**: hace dos barras (`SignalBar + 1`) tenía color `0` (positivo extremo) y la última barra (`SignalBar`) tiene un color diferente. Se cierra primero una posición corta si existe, luego se abre un nuevo largo de `TradeVolume` lotes.
- **Abrir corto**: hace dos barras tenía color `4` (negativo extremo) y la última barra tiene un color diferente. Se cierra primero una posición larga si existe, luego se abre un nuevo corto.
- **Cerrar largo**: siempre que la barra más antigua (hace dos barras) tenga color `4`, señalando agotamiento bajista.
- **Cerrar corto**: siempre que la barra más antigua tenga color `0`, señalando agotamiento alcista.

La lógica reproduce la gestión basada en indicadores de `TradeAlgorithms.mqh`: las salidas se evalúan antes que las entradas, y las operaciones opuestas se liquidan antes de abrir una nueva posición.

## Gestión de riesgos
Las órdenes de protección opcionales se implementan en unidades de paso de precio:

- `StopLossPoints` define la distancia entre el precio de entrada y el nivel de stop-loss. El stop se coloca por debajo de las entradas largas y por encima de las cortas.
- `TakeProfitPoints` define la distancia al objetivo de ganancia usando la misma medida basada en pasos.

Los stops se verifican en cada vela finalizada. Si tanto el stop como el objetivo se activarían en la misma barra, la primera condición que se cumpla cierra la posición.

## Parámetros
- **Trade Volume** (`TradeVolume`): cantidad de orden para cada nueva posición.
- **Stop Loss (pts)** (`StopLossPoints`): distancia de stop-loss en pasos de precio.
- **Take Profit (pts)** (`TakeProfitPoints`): distancia de take-profit en pasos de precio.
- **Enable Long Entries/Exits** (`BuyPositionOpen`, `BuyPositionClose`): interruptores para señales largas.
- **Enable Short Entries/Exits** (`SellPositionOpen`, `SellPositionClose`): interruptores para señales cortas.
- **Signal Bar** (`SignalBar`): cuántas barras atrás evaluar para el cambio de color.
- **High Level / Low Level** (`HighLevel`, `LowLevel`): umbrales para la asignación de color.
- **Primary / Secondary Method** (`Method1`, `Method2`): tipos de media móvil para ambas etapas de suavizado.
- **Length #1 / Length #2** (`Length1`, `Length2`): períodos utilizados por las medias móviles.
- **Phase #1 / Phase #2** (`Phase1`, `Phase2`): configuración de fase Jurik (ignorada por otros métodos).
- **Coefficient** (`Coefficient`): factor de normalización aplicado a la desviación.
- **Applied Price** (`AppliedPrice`): fuente de precio (close, open, high, low, median, typical, weighted, simple, quarter, trend-follow, trend-follow average, Demark).
- **Candle Type** (`CandleType`): marco temporal utilizado para los cálculos del indicador.

## Notas
- El puerto Python se omite intencionalmente según lo solicitado.
- La versión de StockSharp mantiene la directriz de indentación con tabulaciones y añade comentarios en inglés en todo el código.
- El indicador no dibuja histogramas de color; sin embargo, tanto el valor numérico como el índice de color están disponibles a través de la clase personalizada `TradingChannelIndexValue` para mayor visualización si se desea.
