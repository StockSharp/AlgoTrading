# Estrategia FiftyFiveMaBarComparisonStrategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia replica el asesor experto "55 MA" de MetaTrader 5 comparando dos puntos de una media móvil de 55 periodos y operando cuando su diferencia supera un umbral configurable. Todos los cálculos se realizan en velas completadas dentro de una sesión intradía definida por el usuario, y la dirección de la operación puede invertirse opcionalmente. El algoritmo preserva el comportamiento original donde se abre una posición corta siempre que no se cumpla ninguna condición alcista.

## Lógica de trading
1. Suscribirse a la serie de velas seleccionada y calcular una media móvil con la longitud, el método y el precio aplicado elegidos.
2. Mantener los valores de la media móvil más recientes en un búfer para que los valores en los índices de barra `BarA` y `BarB` puedan accederse incluso cuando se usa un desplazamiento horizontal de MA.
3. Cuando llega una vela finalizada dentro de la ventana `[StartHour, EndHour)`:
   - Obtener el valor de MA en `BarA + MaShift` y `BarB + MaShift`.
   - Si el valor en `BarA` supera al valor en `BarB` en más de `DifferenceThreshold`, abrir una posición larga a menos que `ReverseSignals` esté habilitado.
   - Si el valor en `BarA` es menor que el valor en `BarB` en más de `DifferenceThreshold`, abrir una posición corta (o una posición larga cuando `ReverseSignals` está habilitado).
   - De lo contrario la estrategia mantiene el comportamiento original del EA y activa una entrada corta.
4. Las órdenes siempre se envían al mercado usando el `Volume` de la estrategia. Cuando `CloseOppositePositions` está habilitado, el tamaño solicitado se incrementa para neutralizar cualquier exposición opuesta antes de establecer la nueva posición.
5. Las protecciones opcionales de stop-loss y take-profit se adjuntan mediante `StartProtection`. Las distancias se expresan en pips, donde un pip equivale a `PriceStep` multiplicado por 10 para instrumentos cotizados con 3 o 5 dígitos decimales.

## Entradas
| Nombre | Tipo | Por defecto | Descripción |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | Marco temporal de 1 minuto | Serie de velas utilizada para cálculos y señales. |
| `StopLossPips` | `int` | 30 | Distancia del stop-loss en pips. Establecer en 0 para deshabilitar. |
| `TakeProfitPips` | `int` | 50 | Distancia del take-profit en pips. Establecer en 0 para deshabilitar. |
| `StartHour` | `int` | 8 | Hora inclusiva (0-23) que marca el inicio de la sesión de trading. |
| `EndHour` | `int` | 21 | Hora exclusiva (0-23) que marca el fin de la sesión de trading. Debe ser mayor que `StartHour`. |
| `DifferenceThreshold` | `decimal` | 0.0001 | Diferencia absoluta mínima entre los valores de MA comparados que activa una señal direccional. |
| `BarA` | `int` | 0 | Índice de la primera barra usada para la comparación de MA (0 = vela actual). |
| `BarB` | `int` | 1 | Índice de la segunda barra usada para la comparación de MA. |
| `ReverseSignals` | `bool` | `false` | Invierte las condiciones alcistas y bajistas. |
| `CloseOppositePositions` | `bool` | `false` | Si está habilitado, aumenta el tamaño de la orden para cerrar cualquier posición en la dirección opuesta antes de abrir el nuevo trade. |
| `MaShift` | `int` | 0 | Desplazamiento horizontal aplicado a la línea de media móvil. Valores positivos acceden a puntos de MA más antiguos. |
| `MaLength` | `int` | 55 | Periodo de la media móvil. |
| `MaMethod` | `MovingAverageMethods` | `Exponential` | Método de suavizado (`Simple`, `Exponential`, `Smoothed`, `Weighted`). |
| `AppliedPrice` | `AppliedPriceTypes` | `Median` | Precio usado como entrada de MA (`Close`, `Open`, `High`, `Low`, `Median`, `Typical`, `Weighted`). |

## Gestión de posiciones
- Establecer el `Volume` de la estrategia para controlar el tamaño base del trade. Se combina con la posición actual cuando `CloseOppositePositions` está activo.
- Las protecciones de stop-loss y take-profit son opcionales. Se adjuntan únicamente cuando la distancia en pips respectiva es mayor que cero.

## Notas
- La ventana de trading funciona en el tiempo del instrumento; las señales fuera de `[StartHour, EndHour)` son omitidas.
- Cuando `MaShift` produce índices negativos, la estrategia espera hasta que se acumule suficiente historial, reflejando el comportamiento original del EA donde los búfers desplazados pueden devolver `EMPTY_VALUE`.
- Dado que el experto original siempre tiene como valor predeterminado una orden de venta cuando no se cumple el umbral de diferencia, la estrategia convertida mantiene la misma lógica para plena fidelidad. Ajustar `DifferenceThreshold` si este comportamiento no es deseado.
