# Estrategia suficiente369
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Suffic369 es un sistema de ruptura que sigue tendencias y combina dos promedios móviles cortos con bandas Bollinger anchas. El asesor experto ingresa en posiciones largas cuando el promedio móvil simple rápido (SMA) de los precios de cierre cruza por encima del SMA de máximos recientes mientras el mercado cotiza cerca de la banda inferior Bollinger. Las posiciones cortas se abren cuando el SMA rápido cruza por debajo del SMA de los mínimos recientes mientras el precio presiona contra la banda superior. La versión convertida StockSharp mantiene la lógica original MQL pero la expresa con suscripciones de velas de alto nivel y enlaces de indicadores.

## Indicadores
- **Rápido SMA (Cierre, longitud = 3)** – mide la dirección a corto plazo del precio de cierre.
- **Máximo SMA (Máximo, longitud = 5)** – promedia los máximos recientes y actúa como referencia de resistencia alcista.
- **Mínimo SMA (Mínimo, longitud = 5)** – promedia los mínimos recientes y proporciona la referencia de soporte bajista.
- **Bollinger Bandas (longitud = 156, desviación = 1)**: identifica los precios extremos en relación con la volatilidad.

Todos los indicadores se actualizan con las velas completadas. Los valores anteriores se almacenan en caché para reproducir el desplazamiento de una barra utilizado en el programa MetaTrader original.

## Reglas de trading
### Entrada larga
1. El rápido anterior SMA (cierre) está por debajo del máximo anterior SMA.
2. El rápido actual SMA (cerrado) cruza por encima del máximo actual SMA.
3. El precio de cierre de la vela está por debajo de la banda inferior Bollinger.

### Entrada corta
1. El rápido anterior SMA (cierre) está por encima del mínimo anterior SMA.
2. El SMA rápido actual (cerrado) cruza por debajo del mínimo actual SMA.
3. El precio de cierre de la vela está por encima de la banda superior Bollinger.

### Salir de la lógica
- **Señal opuesta:** Una posición larga se cierra cuando aparece una nueva señal de entrada corta, y viceversa.
- **Stop-Loss:** Stop opcional basado en pasos de precio que protege la posición una vez activada.
- **Take-Profit:** Objetivo opcional basado en pasos de precio que refleja el parámetro TakeProfit original.
- **Trailing Stop:** Trailing stop opcional que se ajusta detrás de las operaciones rentables exactamente como la lógica MQL (utiliza el cierre actual para mover el stop solo cuando las ganancias exceden la distancia configurada).

La estrategia mantiene como máximo una posición a la vez. Después de que una señal de parada, objetivo u opuesta cierra la operación, no se evalúa ninguna nueva entrada hasta la siguiente vela terminada.

## Parámetros
| Nombre | Predeterminado | Descripción |
|------|---------|-------------|
| `FastMaLength` | 3 | Duración del rápido SMA basado en precios de cierre. |
| `HighMaLength` | 5 | Longitud del SMA calculada sobre los máximos de las velas. |
| `LowMaLength` | 5 | Longitud del SMA calculada sobre los mínimos de las velas. |
| `BollingerLength` | 156 | Tamaño de ventana de las Bollinger Bandas. |
| `BollingerDeviation` | 1 | Multiplicador de desviación estándar para las bandas. |
| `UseStopLoss` | cierto | Habilita el bloque stop-loss. |
| `StopLossPoints` | 30 | Distancia de parada en pasos del precio del instrumento. |
| `UseTakeProfit` | cierto | Habilita el bloque de toma de ganancias. |
| `TakeProfitPoints` | 60 | Distancia objetivo de beneficio en pasos de precio. |
| `UseTrailingStop` | cierto | Permite la gestión de trailing stop. |
| `TrailingStopPoints` | 30 | Compensación final en los incrementos de precios. |
| `CandleType` | plazo de 15 minutos | Tipo de vela utilizada para los cálculos. |

Todos los parámetros numéricos están expuestos como instancias `StrategyParam<T>` para que puedan optimizarse directamente dentro de StockSharp.

## Gestión del riesgo
- Los stop-loss, take-profit y trailing stop utilizan el paso del precio del instrumento (`Security.PriceStep`) para convertir distancias de puntos en precios absolutos.
- Los trailingstops siguen movimientos rentables sólo cuando el precio ha avanzado más que la distancia configurada, replicando la lógica original de modificación de órdenes.
- `StartProtection()` se invoca al inicio para habilitar las funciones de protección integradas de StockSharp.

## Notas de uso
- Suscríbase la estrategia a un instrumento que admita el tipo de vela seleccionado.
- Asegúrese de que la propiedad `Volume` esté configurada en el tamaño comercial deseado antes de comenzar la estrategia.
- La estrategia espera a que los valores de los indicadores estén completamente formados antes de emitir cualquier orden; Las velas iniciales se utilizan para generar el historial del indicador.
