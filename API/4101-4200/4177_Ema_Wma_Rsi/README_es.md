# Estrategia EMA WMA RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
EMA WMA RSI es una conversión del MetaTrader 4 asesor experto "EMA WMA RSI" creado por cmillion. El robot original compara una media móvil exponencial (EMA) y una media móvil ponderada lineal (WMA) calculada a partir de las aperturas de velas, y filtra cada cruce con un umbral de índice de fuerza relativa (RSI). El puerto StockSharp mantiene la misma lógica de indicador, opera con velas terminadas y reproduce las opciones de administración de dinero: aplanamiento de contraposición opcional, niveles de stop-loss/take-profit basados ​​en puntos y un trailing stop que puede seguir distancias fijas, el último fractal o extremos de velas recientes.

La estrategia está diseñada para un único símbolo y período de tiempo seleccionado mediante el parámetro `Candle Type`. Asume MetaTrader "puntos" (el tick mínimo) al convertir distancias de riesgo a precios absolutos, por lo que se deben completar metadatos de instrumentos como `Security.Step` y `Security.StepPrice` para obtener mejores resultados.

## Lógica estratégica
### Indicadores
* **EMA** – período definido por `EMA Period`, aplicado a los precios de apertura de velas.
* **WMA** – período definido por `WMA Period`, también alimentado con aperturas de velas.
* **RSI** – `RSI Period`, calculado sobre el mismo flujo de precio de apertura.

Todos los indicadores se actualizan una vez por vela terminada. El puerto refleja la ejecución original de "barra abierta" almacenando los valores EMA/WMA de la barra anterior y comparándolos con la barra actual inmediatamente después de que se cierra.

### Reglas de entrada
* **Configuración larga**
  1. El valor actual de EMA está por debajo de WMA, mientras que la barra anterior tenía EMA por encima de WMA (una cruz hacia abajo).
  2. El valor RSI es superior a 50.
  3. Si existe una posición corta, se cierra opcionalmente cuando `Close Counter Trades` está habilitado; de lo contrario, la señal se ignora hasta que la estrategia sea plana.
  4. Cuando las condiciones se mantienen, se envía una orden de compra de mercado utilizando el volumen fijo o el tamaño basado en el riesgo.
* **Configuración corta** – lógica simétrica: EMA cruza por encima de WMA, la barra anterior mostraba EMA por debajo de WMA, RSI está por debajo de 50 y la estrategia aplana una posición larga o se salta la operación.

### reglas de salida
* **Protección inicial**: `Stop Loss (points)` y `Take Profit (points)` se traducen a distancias absolutas utilizando el tamaño de tick del instrumento. Cualquiera de los valores se puede establecer en cero para desactivarlo.
* **Parada de seguimiento**
  * Si `Trailing Stop (points)` es mayor que cero, el stop sigue al precio a una distancia fija medida desde el último cierre (solo apretando, nunca aflojando).
  * Si la distancia de seguimiento es cero, el algoritmo busca niveles adaptativos:
    * `Trailing Source = CandleExtremes` mira hacia atrás a través de máximos y mínimos de velas anteriores. Un stop largo se mueve al primer mínimo al menos cinco puntos por debajo del precio actual; una parada corta utiliza máximos cinco puntos por encima.
    * `Trailing Source = Fractals` escanea los fractales de Bill Williams previamente confirmados (dos velas a cada lado). Se aplica el mismo colchón de cinco puntos para evitar colocar el stop demasiado cerca del precio actual.
  * Los ajustes finales solo se activan después de que el precio supera el precio de entrada original, reproduciendo el comportamiento MetaTrader EA.
* **Salida de posición**: cuando se toca el trailing stop o la toma de ganancias dentro del rango de una vela, la posición se cierra con una orden de mercado y se restablece el estado interno.

### Tamaño de posición
* `Fixed Volume` proporciona el tamaño exacto de la orden de mercado (lotes/contratos). Este es el valor predeterminado y coincide con el parámetro EA `Lot`.
* Establecer `Fixed Volume` en cero activa el dimensionamiento basado en el riesgo. La estrategia estima el riesgo monetario por unidad utilizando la distancia de parada disponible (ya sea el stop loss configurado o la distancia de seguimiento efectiva) y `Security.StepPrice`. `Risk %` determina cuánto capital de cartera está expuesto por operación. Si tanto el volumen fijo como el porcentaje de riesgo son cero, la señal se ignora.

## Parámetros
| Parámetro | Descripción | Predeterminado |
|-----------|-------------|---------|
| `EMA Period` | Período de la media móvil exponencial aplicada a la apertura de velas. | `28` |
| `WMA Period` | Período de la media móvil lineal ponderada en las aperturas. | `8` |
| `RSI Period` | RSI longitud utilizada como filtro direccional. | `14` |
| `Stop Loss (points)` | Compensación del stop-loss en MetaTrader puntos. `0` desactiva la parada de protección. | `0` |
| `Take Profit (points)` | Compensación de la toma de ganancias en puntos. `0` desactiva el objetivo. | `500` |
| `Trailing Stop (points)` | Distancia de seguimiento fija en puntos. `0` cambia al seguimiento adaptativo (fractales o mínimos/máximos de velas). | `70` |
| `Trailing Source` | Método de seguimiento adaptativo: `CandleExtremes` para máximos/mínimos sin procesar, `Fractals` para Williams fractales. | `CandleExtremes` |
| `Close Counter Trades` | Cierre una posición opuesta antes de abrir una nueva operación. | `false` |
| `Fixed Volume` | Volumen de órdenes de mercado. Establezca en `0` para habilitar el tamaño basado en el riesgo. | `0.1` |
| `Risk %` | Porcentaje del capital de la cartera comprometido cuando `Fixed Volume` es cero. Requiere una distancia de parada válida. | `10` |
| `Candle Type` | Marco de tiempo principal utilizado para indicadores y evaluación de señales. | `30-minute candles` |

## Notas de implementación
* Las conversiones de incremento de precio dependen de `Security.Step` (o `Security.PriceStep`) y `Security.StepPrice`. Proporcione metadatos de instrumentos realistas para mantener precisos los cálculos punto a precio.
* La estrategia solo procesa velas terminadas y utiliza sus precios de apertura para actualizaciones de indicadores, coincidiendo con la lógica de "nueva barra" en el código MQL4.
* Los niveles finales mantienen al menos un margen de cinco puntos alejado del precio actual, al igual que la función auxiliar original `SlLastBar`.
* Cuando el cierre de contraposición está desactivado, la estrategia nunca cubre: solo se gestiona una posición neta a la vez.
* No se incluye ninguna implementación de Python en este paquete.
