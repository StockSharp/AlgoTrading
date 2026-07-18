# Estrategia de Rompimiento con Stop Pendiente
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Resumen
Esta estrategia recrea el asesor experto original de MetaTrader de "breakdown". Coloca órdenes de stop alrededor del rango del día anterior y actualiza continuamente las órdenes cada sesión. Un motor de trailing-stop replica la lógica de trailing escalonado del script fuente, manteniendo los stops ajustados una vez que la posición comienza a moverse en la dirección rentable.

## Cómo Funciona
- **Preparación diaria** – Cuando una vela diaria cierra, la estrategia almacena el máximo y el mínimo. Al inicio de la siguiente sesión cancela las órdenes restantes y envía un buy stop por encima del máximo anterior y un sell stop por debajo del mínimo anterior. El parámetro `Min Distance (ticks)` desplaza las órdenes lejos de los niveles brutos para evitar ruido.
- **Actualización de órdenes** – Cada vez que las órdenes pendientes se ejecutan o comienza un nuevo día, las órdenes restantes se cancelan y se envía un nuevo par usando los mismos niveles del día anterior. El comportamiento refleja el experto MQL que mantiene continuamente entradas de stop en ambos lados del mercado.
- **Controles de riesgo** – Las posiciones ejecutadas inicializan objetivos de stop-loss y take-profit basados en distancias en ticks. Una regla de trailing escalonado sube/baja el stop solo después de que el precio gana al menos `Trailing Stop (ticks) + Trailing Step (ticks)` desde la entrada, exactamente como la implementación original de trailing-stop.
- **Salidas** – Las posiciones se cierran inmediatamente cuando el precio toca el stop o el objetivo activo. El trailing manual cierra posiciones a mercado cuando se viola el nivel de trailing, coincidiendo con la lógica de MetaTrader que modificaba stops en cada tick.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `Working Candles` | Marco temporal usado para monitorear la acción del precio y gestionar stops (predeterminado: velas de 15 minutos). |
| `Stop Loss (ticks)` | Distancia de stop protector inicial convertida a precio absoluto usando el tamaño del tick del instrumento. Establecer en cero para deshabilitar. |
| `Take Profit (ticks)` | Distancia inicial de take-profit. Establecer en cero para deshabilitar. |
| `Trailing Stop (ticks)` | Distancia principal del trailing-stop. Establecer en cero para deshabilitar el trailing. |
| `Trailing Step (ticks)` | Beneficio adicional requerido antes de que el trailing stop se mueva. |
| `Min Distance (ticks)` | Offset agregado al máximo/mínimo del día anterior al colocar las órdenes pendientes. |
| `Order Volume` | Cantidad enviada con ambas órdenes de stop. |

## Notas de Uso
- Configurar la estrategia en instrumentos que publiquen velas diarias para obtener el rango de la sesión anterior.
- La lógica asume un tamaño de tick constante. Para instrumentos con incrementos de tick variables, ajustar los valores predeterminados en consecuencia.
- La estrategia no implementa el dimensionamiento basado en porcentaje del script MQL original; el volumen se define explícitamente a través del parámetro `Order Volume`.
- Aún no se proporciona una versión Python para esta estrategia.
