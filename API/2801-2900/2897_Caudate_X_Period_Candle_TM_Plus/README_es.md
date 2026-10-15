# Estrategia Caudate X Período Vela TM Plus
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción General
La estrategia replica la lógica del asesor experto Caudate X Period Candle TM Plus. Suaviza los precios de apertura, máximo, mínimo y cierre de la vela con una media móvil configurable, construye un rango estilo Donchian y clasifica cada vela finalizada en uno de seis códigos de color dependiendo de la posición del cuerpo dentro del rango. Las entradas largas se activan por los colores de cola inferior alcista (0 o 1), mientras que las entradas cortas se activan por los colores de cola superior bajista (5 o 6). Los grupos de colores opuestos se usan para salir de posiciones existentes.

## Reglas de Trading
1. Suscribirse a la serie de velas seleccionada y suavizar cada componente con la media móvil elegida.
2. Calcular el máximo más alto y el mínimo más bajo de los máximos y mínimos suavizados durante el `Donchian Period` especificado, luego expandir el rango para que siempre contenga la apertura y el cierre suavizados.
3. Determinar el color de la vela:
   * Colores **0/1** – cuerpo cerca de la parte superior del rango (cola inferior).
   * Colores **2/4** – cuerpo centrado dentro del rango.
   * Colores **5/6** – cuerpo cerca de la parte inferior del rango (cola superior).
4. Evaluar el color de la barra desplazada por `Signal Bar` (el valor predeterminado `1` usa la vela completada anterior).
5. Abrir posiciones cuando el color pertenece al grupo de entrada y la posición opuesta no está activa.
6. Cerrar posiciones cuando el color pertenece al grupo de salida o cuando expira el tiempo máximo de retención.
7. Los offsets opcionales de stop-loss y take-profit se establecen a través del módulo de protección incorporado.

## Parámetros
| Parámetro | Descripción |
| --- | --- |
| `Candle Type` | Marco temporal usado para los cálculos de señal. |
| `Donchian Period` | Número de velas para el rango suavizado de máximo/mínimo. |
| `Signal Bar` | Número de barras para retrasar la evaluación de señal (0 = barra actual). |
| `Smoothing Method` | Media móvil aplicada a los precios OHLC (SMA, EMA, SMMA, LWMA, aproximación Jurik JJMA, Kaufman AMA). |
| `MA Length` | Longitud del filtro de suavizado. |
| `MA Phase` | Reservado para compatibilidad JJMA (no usado por las medias de StockSharp). |
| `Enable Long/Short Entries` | Activar la apertura de nuevas posiciones largas o cortas. |
| `Enable Long/Short Exits` | Activar el cierre de posiciones largas o cortas existentes en señales. |
| `Enable Time Exit` | Habilitar el filtro de tiempo máximo de retención. |
| `Time Exit (minutes)` | Duración de retención antes de un cierre forzado. |
| `Stop Loss (points)` | Distancia de stop-loss en pasos de precio (multiplicado por `Security.PriceStep`). |
| `Take Profit (points)` | Distancia de take-profit en pasos de precio. |

## Notas
- `Signal Bar = 1` coincide con el comportamiento del experto MQL5 actuando en la última vela completamente cerrada.
- Cuando las distancias de stop o objetivo son mayores que cero, la estrategia llama a `StartProtection` con offsets absolutos basados en el paso de precio del instrumento.
- `MA Phase` se mantiene por compatibilidad pero no es consumido por las implementaciones de media móvil de StockSharp.
- Establecer el tamaño base de la orden a través de la propiedad `Strategy.Volume` heredada; la implementación siempre cierra posiciones opuestas antes de abrir una nueva.
