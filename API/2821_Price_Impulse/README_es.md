# Estrategia de Impulso de Precio
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia de Impulso de Precio analiza cotizaciones Level1 sin procesar y reacciona a los cambios repentinos entre el mejor bid y el mejor ask. Replica el asesor experto original de MetaTrader 5 al observar saltos de precio durante un número configurable de ticks y entrando al mercado cuando el movimiento supera un umbral expresado en puntos. Los niveles de stop loss protector y take profit se aplican automáticamente a través del asistente de alto nivel `StartProtection`.

El enfoque es neutral al mercado: se abre una posición larga cuando el precio ask sube con respecto a una cotización anterior, mientras que se abre una posición corta cuando el bid cae por debajo de su valor previo. Un período de enfriamiento configurable evita que la estrategia vuelva a entrar inmediatamente después de una operación, igual que la implementación MQL que espera un intervalo de pausa especificado.

## Cómo Funciona

- Se suscribe a datos Level1 y almacena historiales continuos de los mejores precios bid y ask.
- Calcula la diferencia de precio entre la última cotización y la cotización recibida `HistoryGap` ticks antes (con un búfer adicional definido por `ExtraHistory`).
- Abre una posición larga cuando el precio ask sube más de `ImpulsePoints * PriceStep` y no existe exposición larga.
- Abre una posición corta cuando el precio bid cae más del mismo umbral y no existe exposición corta.
- Aplica niveles fijos de take profit y stop loss expresados en puntos de precio y aplica una pausa de `CooldownSeconds` entre órdenes.

## Parámetros

- **OrderVolume** – volumen enviado con cada orden de mercado. Por defecto es `0.1` lotes para coincidir con el robot fuente, pero puede optimizarse para otros instrumentos.
- **StopLossPoints** – distancia desde el precio de entrada hasta el stop protector, medida en puntos del instrumento. Un valor de `0` deshabilita el stop.
- **TakeProfitPoints** – distancia hasta el objetivo de take profit, también medida en puntos. Un valor de `0` deshabilita el objetivo.
- **ImpulsePoints** – impulso de precio mínimo, en puntos, que debe superarse entre la cotización actual y la cotización `HistoryGap` ticks atrás para activar una entrada.
- **HistoryGap** – número de actualizaciones Level1 que separan el precio actual de la línea base de comparación. Valores más altos requieren miradas hacia atrás más largas, lo que suaviza el ruido pero retrasa las entradas.
- **ExtraHistory** – muestras Level1 adicionales retenidas en el búfer continuo para absorber ráfagas de cotizaciones cuando varios ticks llegan entre callbacks. Mantiene la lógica coherente con la implementación MT5 que sobre-muestrea el array de historial.
- **CooldownSeconds** – tiempo mínimo de espera después de cualquier operación antes de poder colocar otra entrada. Garantiza que la estrategia refleje el parámetro `InpSleep` del experto MQL y evita reversiones rápidas.

## Notas

- Los parámetros de distancia en puntos se convierten automáticamente usando `Security.PriceStep` (o `Security.MinPriceStep` como respaldo), por lo que la misma configuración se adapta a diferentes tamaños de tick.
- El trading solo comienza cuando la estrategia está en línea, los búferes de historial contienen suficientes datos y se cumple la condición de impulso.
- Dado que las decisiones se toman en actualizaciones de cotizaciones en bruto, la estrategia funciona mejor en instrumentos líquidos con feeds Level1 confiables.
- No existe versión en Python para esta estrategia. Solo se proporciona la versión en C#, cumpliendo con la solicitud del usuario.
