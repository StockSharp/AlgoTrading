# Estrategia de Cruce de Media Móvil Suavizada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La Estrategia de Cruce de Media Móvil Suavizada replica la lógica del Asesor Experto MQL5 original **Smoothing Average (barabashkakvn's edition)**. Combina una media móvil configurable con un filtro de distancia de precio medido en pips. Cuando el mercado se aleja lo suficiente de la media suavizada, la estrategia abre una posición en la dirección del movimiento (o en el lado opuesto si el modo de reversión está habilitado). Las posiciones se cierran una vez que el precio revierte a través de un canal expandido alrededor de la media móvil.

## Lógica de Trading
### Modo predeterminado (`ReverseSignals = false`)
- **Entrada largo:** el precio de cierre sube por encima de la media móvil menos `Entry Delta (pips)`.
- **Entrada corto:** el precio de cierre cae por debajo de la media móvil más `Entry Delta (pips)`.
- **Salida corto:** el precio de cierre sube por encima de la media móvil más `Entry Delta (pips) × Close Delta Coefficient`.
- **Salida largo:** el precio de cierre cae por debajo de la media móvil menos `Entry Delta (pips) × Close Delta Coefficient`.

### Modo de reversión (`ReverseSignals = true`)
- **Entrada largo:** el precio de cierre cae por debajo de la media móvil más `Entry Delta (pips)`.
- **Entrada corto:** el precio de cierre sube por encima de la media móvil menos `Entry Delta (pips)`.
- **Salida largo:** el precio de cierre cae por debajo de la media móvil menos `Entry Delta (pips) × Close Delta Coefficient`.
- **Salida corto:** el precio de cierre sube por encima de la media móvil más `Entry Delta (pips) × Close Delta Coefficient`.

La media móvil puede desplazarse hacia adelante varios candles. La estrategia emula este comportamiento manteniendo un pequeño búfer de los valores más recientes del indicador y usando el valor de `MaShift` barras atrás. Esto coincide con la línea desplazada producida por la implementación original de MetaTrader.

## Parámetros
- `Candle Type` – serie de datos utilizada para los cálculos.
- `MA Length` – período de la media de suavizado.
- `MA Shift` – número de barras que la media móvil se desplaza hacia adelante.
- `MA Type` – método de media móvil (simple, exponencial, suavizada o ponderada linealmente).
- `Price Source` – precio de vela introducido en la media móvil (predeterminado: precio típico).
- `Entry Delta (pips)` – distancia desde la media móvil requerida para activar entradas. Convertida a precio usando el tamaño de pip del instrumento.
- `Close Delta Coefficient` – multiplicador aplicado al delta de entrada al verificar condiciones de salida.
- `Reverse Signals` – invierte la lógica de entrada largo/corto.
- `Trade Volume` – tamaño de orden utilizado para entradas largas y cortas.

## Gestión de Riesgo
- Las órdenes se envían con el parámetro fijo `Trade Volume`. La estrategia no escala mientras una posición está abierta.
- Todas las salidas están basadas en reglas. No se envían órdenes de stop-loss ni take-profit, pero se invoca `StartProtection()` para habilitar la red de seguridad a nivel de plataforma.
- El modo de reversión está disponible para comportamiento a contratendencia sin alterar otros ajustes.

## Notas de Implementación
- El tamaño de pip se deriva de `Security.PriceStep`. Los símbolos FX de tres o cinco dígitos reciben el mismo ajuste de 10× que en el código MQL5.
- La media móvil utiliza la selección de `Price Source` para que los precios típicos, medianos u otros precios de velas puedan coincidir con los ajustes del EA original.
- Las comparaciones de entrada y salida usan el cierre de vela como proxy estable para las comprobaciones bid/ask en el Asesor Experto de origen.
- Todos los comentarios dentro del código C# se proporcionan en inglés, según lo requerido por las pautas de conversión.
