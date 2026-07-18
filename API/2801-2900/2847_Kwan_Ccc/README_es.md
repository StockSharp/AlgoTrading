# Estrategia KWAN CCC
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia KWAN CCC reproduce el experto MetaTrader `Exp_KWAN_CCC.mq5` utilizando la API de alto nivel de StockSharp. El sistema deriva señales de trading de un oscilador personalizado construido de la siguiente manera:

1. Calcular el oscilador Chaikin (diferencia entre las medias móviles rápida y lenta de la línea de acumulación/distribución).
2. Multiplicar el valor de Chaikin por el Commodity Channel Index (CCI).
3. Dividir el resultado por el valor del indicador Momentum. Cuando el Momentum es cero, el script sustituye un valor constante de 100 para evitar la división por cero, exactamente como el código original.
4. Suavizar la serie resultante con el método XMA seleccionado por el usuario.
5. Detectar la pendiente de la serie suavizada. Las barras en alza se colorean con `0`, las barras en baja con `2`, y el resto con `1`.

Cuando el color cambia de `0` a cualquier otro valor, la estrategia cierra cortos y abre una posición larga. Cuando el color cambia de `2` a cualquier otro valor, cierra largos y abre un corto. Esto refleja la lógica implementada en el experto MQL, incluido el desplazamiento de señal opcional (`SignalBar`).

## Reglas de trading
- **Entrada larga**: el color en la barra en `SignalBar + 1` es igual a `0` y la barra en `SignalBar` es diferente de `0`.
- **Entrada corta**: el color en la barra en `SignalBar + 1` es igual a `2` y la barra en `SignalBar` es diferente de `2`.
- **Salida larga**: habilitada cuando `EnableLongExits = true` y se activa la condición de entrada corta.
- **Salida corta**: habilitada cuando `EnableShortExits = true` y se activa la condición de entrada larga.
- Las órdenes de stop protector y objetivo se crean a través de `StartProtection` usando desplazamientos de precio absolutos derivados de `StopLossPoints` y `TakeProfitPoints` multiplicados por el `PriceStep` del instrumento.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `OrderVolume` | Tamaño de orden base utilizado al abrir una nueva posición. |
| `CandleType` | Marco temporal para todos los cálculos de indicadores. El valor predeterminado es 1 hora. |
| `FastPeriod` / `SlowPeriod` | Longitudes de las medias móviles dentro del oscilador Chaikin. |
| `ChaikinMethod` | Tipo de media móvil (simple, exponencial, suavizada, ponderada) aplicada a la línea de acumulación/distribución. |
| `CciPeriod` | Período del Commodity Channel Index. |
| `MomentumPeriod` | Período del indicador Momentum. |
| `SmoothingMethod` | Método de suavizado XMA mapeado desde las opciones originales. `JurX`, `Parabolic` y `T3` recurren a Jurik MA; `Vidya` usa un suavizado adaptativo basado en el Oscilador de Momentum de Chande; `Adaptive` usa Kaufman AMA. |
| `SmoothingLength` | Número de barras utilizadas por el filtro de suavizado seleccionado. |
| `SmoothingPhase` | Parámetro adicional utilizado por métodos específicos (p. ej., longitud CMO de VIDYA, período lento de AMA). |
| `SignalBar` | Desplazamiento (en barras completadas) utilizado para evaluar las transiciones de color. `1` reproduce el valor predeterminado de MetaTrader. |
| `EnableLongEntries` / `EnableShortEntries` | Permitir o bloquear la apertura de nuevas posiciones en la dirección correspondiente. |
| `EnableLongExits` / `EnableShortExits` | Permitir o bloquear el cierre de posiciones impulsado por el indicador. |
| `StopLossPoints` / `TakeProfitPoints` | Stop protector/objetivo medido en pasos de precio (establecer en cero para deshabilitar). |

## Notas de implementación
- La estrategia solo actúa sobre velas terminadas y usa el helper `Bind` de StockSharp para transmitir datos de velas a los indicadores.
- La lista de métodos de suavizado refleja la implementación XMA de la biblioteca original. Los métodos no disponibles en StockSharp se mapean a la alternativa más cercana, como se indica en la tabla de parámetros.
- La entrada `VolumeType` de MetaTrader se omite porque las velas de StockSharp ya encapsulan la información de volumen total utilizada por la línea de acumulación/distribución.
- La gestión del dinero en el experto original dependía de helpers personalizados de dimensionamiento de lotes. La conversión asume un volumen fijo especificado por `OrderVolume`.

## Consejos de uso
- Asegúrese de que el instrumento proporcione datos de volumen significativos si el comportamiento del oscilador Chaikin es importante. Para instrumentos ilíquidos, considere aumentar `MomentumPeriod` para reducir el ruido.
- Al optimizar los parámetros de suavizado, combine `SmoothingLength` y `SmoothingPhase` con cuidado: las combinaciones extremas pueden retrasar las señales considerablemente.
- Los valores protectores predeterminados (`StopLossPoints = 1000`, `TakeProfitPoints = 2000`) corresponden a grandes desplazamientos. Ajústelos para que coincidan con el tamaño del tick del instrumento.
