# Estrategia Color Fisher M11
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Color Fisher M11 es una estrategia de seguimiento de tendencia que replica el asesor experto Exp_ColorFisher_m11 de MetaTrader 5. Utiliza una variante personalizada del Fisher Transform que colorea las velas con cinco estados de color para resaltar el momentum extremo alcista y bajista. Las señales se retrasan un número configurable de velas cerradas para evitar operar sobre datos incompletos, mientras que interruptores opcionales permiten desactivar entradas o salidas de cada lado de forma independiente.

## Lógica del indicador
La estrategia construye el indicador Color Fisher en tiempo real:

- Determina el máximo más alto y el mínimo más bajo en la ventana **Range Periods**.
- Normaliza el precio medio de la vela actual dentro de ese rango y aplica **Price Smoothing** (estilo EMA) para estabilizar las oscilaciones.
- Aplica el Fisher Transform con un factor adicional **Index Smoothing** para crear el valor final del oscilador.
- Clasifica el oscilador en cinco bandas de color discretas usando los umbrales **High Level** y **Low Level**:
  - `0` – fuerte impulso alcista por encima del nivel alto.
  - `1` – momentum alcista moderado entre cero y el nivel alto.
  - `2` – zona neutral alrededor de cero.
  - `3` – momentum bajista moderado entre cero y el nivel bajo.
  - `4` – fuerte impulso bajista por debajo del nivel bajo.

La señal se evalúa `Signal Bar` velas atrás, imitando el comportamiento del Asesor Experto original. El estado de color anterior también se rastrea para detectar transiciones nuevas hacia las bandas extremas.

## Reglas de trading
- **Entrada larga** – permitida cuando `Enable Buy Entry` es verdadero, el color retrasado es igual a `0` (fuerte alcista) y el color previo es diferente de `0`. Cualquier exposición corta se revierte y la posición se vuelve larga.
- **Entrada corta** – permitida cuando `Enable Sell Entry` es verdadero, el color retrasado es igual a `4` (fuerte bajista) y el color previo es diferente de `4`. Cualquier exposición larga se revierte y la posición se vuelve corta.
- **Salida larga** – activada cuando `Enable Buy Exit` es verdadero y el color retrasado pasa a `3` o `4`, señalando control bajista.
- **Salida corta** – activada cuando `Enable Sell Exit` es verdadero y el color retrasado pasa a `0` o `1`, señalando control alcista.

Para evitar múltiples órdenes por señal, la estrategia recuerda el tiempo de cierre de la siguiente barra para cada dirección y rechaza nuevas entradas hasta que se complete la siguiente vela.

## Gestión de riesgos
`Stop Loss (pts)` y `Take Profit (pts)` convierten las distancias en pips originales en pasos de precio absolutos usando el precio de paso del instrumento. Cuando se proporciona una distancia positiva, las órdenes protectoras se activan a través de `StartProtection`. Establezca cualquier valor en cero para desactivar esa protección.

## Parámetros
- **Range Periods** – longitud del lookback para el rango alto/bajo usado por el Fisher Transform (por defecto 10).
- **Price Smoothing** – factor de suavizado previo a la transformación, 0…0.99 (por defecto 0.3).
- **Index Smoothing** – factor de suavizado posterior a la transformación, 0…0.99 (por defecto 0.3).
- **High Level / Low Level** – umbrales que definen los extremos alcistas y bajistas (por defecto +1.01 y –1.01).
- **Signal Bar** – número de velas cerradas para retrasar la evaluación de señales (por defecto 1).
- **Enable Buy Entry / Enable Sell Entry** – interruptores para abrir nuevas operaciones largas o cortas.
- **Enable Buy Exit / Enable Sell Exit** – interruptores para permitir salidas impulsadas por el indicador.
- **Stop Loss (pts) / Take Profit (pts)** – distancias protectoras expresadas en pasos de precio.
- **Candle Type** – marco temporal para la suscripción de velas; por defecto velas de 4 horas.

## Notas
- La estrategia usa bindings de alto nivel de StockSharp (`SubscribeCandles().BindEx`) y no almacena colecciones históricas más allá del historial mínimo de colores requerido para la señal retrasada.
- En esta versión no se proporciona un puerto Python, de acuerdo con la especificación.
- Agrega la estrategia a un área del gráfico para visualizar tanto el precio como el oscilador Color Fisher calculado.
