# Estrategia de SR Rate Indicator
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Esta estrategia es un port en C# del experto de MetaTrader 5 **Exp_SR-RateIndicator**. Reproduce la lógica de trading original usando la API de alto nivel de StockSharp y una implementación personalizada del oscilador SR Rate. El indicador mide qué tan lejos está el precio ponderado de la vela dentro de un canal de soporte/resistencia suavizado y pinta un código de color que resalta las lecturas extremas.

El algoritmo procesa velas terminadas de un marco temporal configurable. Cada vez que el color del oscilador salta al extremo alcista o bajista, la estrategia cierra cualquier posición opuesta y abre una nueva operación en la dirección de la señal. Los niveles de stop loss y take profit protectores se aplican con las mismas distancias en puntos utilizadas en la versión de MetaTrader.

## Oscilador SR Rate

El indicador calcula una banda suavizada gaussiana alrededor del precio usando una longitud de ventana configurable:

1. Para cada barra, el máximo, mínimo y cierre ponderado se suavizan con pesos gaussianos unilaterales de longitud seis.
2. El máximo suavizado más alto y el mínimo suavizado más bajo sobre la ventana definen un rango dinámico.
3. El cierre ponderado suavizado actual se normaliza dentro de ese rango y se mapea al intervalo `[-100, 100]`.
4. El valor final del oscilador se convierte en cinco estados de color: `0` (fuertemente bajista), `1` (suavemente bajista), `2` (neutral), `3` (suavemente alcista) y `4` (fuertemente alcista).

Un color fuertemente alcista (`4`) indica que el precio alcanzó el extremo superior del rango, mientras que un color fuertemente bajista (`0`) señala una visita al extremo inferior.

## Reglas de trading

1. Suscribirse a velas del tipo configurado y calcular el oscilador SR Rate en cada barra terminada.
2. Desplazar la evaluación de señales por `SignalBar` velas cerradas (por defecto: una barra atrás) para imitar el comportamiento del Asesor Experto.
3. Cuando el color desplazado se vuelve `4` y el color anterior es inferior a `4`:
   - Cerrar cualquier posición corta existente si las salidas largas están habilitadas.
   - Abrir una nueva posición larga si las entradas largas están habilitadas y no hay otra posición activa.
4. Cuando el color desplazado se vuelve `0` y el color anterior es superior a `0`:
   - Cerrar cualquier posición larga existente si las salidas cortas están habilitadas.
   - Abrir una nueva posición corta si las entradas cortas están habilitadas y no hay otra posición activa.
5. Solo puede estar abierta una posición a la vez. Las nuevas señales se ignoran hasta que se cierra la operación anterior.
6. Los niveles opcionales de stop loss y take profit se expresan en puntos de precio y se convierten automáticamente a precios absolutos usando el paso de precio del instrumento.

## Parámetros

| Nombre | Descripción |
|------|-------------|
| `OrderVolume` | Volumen de operación utilizado para cada orden de mercado. |
| `EnableLongEntries` | Habilitar/deshabilitar apertura de posiciones largas. |
| `EnableShortEntries` | Habilitar/deshabilitar apertura de posiciones cortas. |
| `EnableLongExits` | Cerrar posiciones largas cuando aparece un color fuertemente bajista. |
| `EnableShortExits` | Cerrar posiciones cortas cuando aparece un color fuertemente alcista. |
| `StopLossPoints` | Distancia del stop loss en puntos del instrumento (convertido usando el paso de precio). |
| `TakeProfitPoints` | Distancia del take profit en puntos del instrumento (convertido usando el paso de precio). |
| `SlippagePoints` | Deslizamiento máximo tolerado al cerrar posiciones. Preservado por compatibilidad; la API de alto nivel no aplica control explícito de deslizamiento. |
| `CandleType` | Tipo de vela y marco temporal utilizados para calcular el indicador. |
| `SignalBar` | Número de velas cerradas para desplazar la evaluación de señales (por defecto 1). |
| `WindowSize` | Longitud de la ventana rodante utilizada por la normalización SR Rate. |
| `HighLevel` | Nivel del oscilador que define el extremo alcista (por defecto +20). |
| `LowLevel` | Nivel del oscilador que define el extremo bajista (por defecto -20). |

## Notas

- La estrategia funciona con cualquier instrumento que suministre velas OHLC estándar.
- Las señales solo se procesan en velas terminadas; los recálculos intrabarra se ignoran, igual que en la implementación de MetaTrader.
- El manejo del deslizamiento en el experto original dependía de la configuración de ejecución. Las órdenes de mercado de StockSharp ya respetan las reglas del exchange, por lo tanto el parámetro `SlippagePoints` se mantiene solo con fines de documentación.
- El indicador almacena solo la cantidad mínima de historial requerida para evaluar la ventana, evitando el uso innecesario de memoria.
- La versión en Python se omite intencionalmente según las directrices del proyecto.
