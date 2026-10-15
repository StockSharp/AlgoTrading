# Estrategia de Vela de Cambio Promedio
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia convertida del experto MetaTrader `Exp_AverageChangeCandle`. Recrea la lógica original dentro de StockSharp suavizando ratios de velas relativos a una media móvil de referencia dinámica y reaccionando a las transiciones de color alcista/bajista.

## Idea principal

1. Calcular una media móvil de referencia (`MaMethod1`, `Length1`) sobre el precio aplicado seleccionado.
2. Expresar el precio de apertura y cierre de la vela actual como ratios respecto a la referencia y elevarlos a la potencia `Power`.
3. Suavizar los valores transformados de apertura y cierre con una segunda media móvil (`MaMethod2`, `Length2`).
4. Clasificar el color de la vela: alcista cuando el cierre suavizado &gt; apertura suavizada, bajista cuando el cierre suavizado &lt; apertura suavizada.
5. Generar señales de trading cuando el color cambia después del retraso `SignalBar` configurado.

Solo se procesan velas terminadas. La estrategia abre posiciones de mercado en la dirección del nuevo color y opcionalmente cierra el lado contrario.

## Parámetros

| Parámetro | Predeterminado | Descripción |
|-----------|---------|-------------|
| `OrderVolume` | `1` | Volumen usado al abrir una nueva posición. |
| `MaMethod1` | `Lwma` | Suavizado aplicado al ratio de referencia (subconjunto de SMA/EMA/SMMA/LWMA/JJMA/AMA). Los tipos no soportados usan EMA. |
| `Length1` | `12` | Período de la media móvil de referencia. |
| `Phase1` | `15` | Parámetro de fase Jurik para la referencia (mantenido por compatibilidad). |
| `PriceSource` | `Median` | Precio aplicado antes de calcular la referencia. |
| `MaMethod2` | `Jjma` | Suavizado aplicado a los ratios transformados. |
| `Length2` | `5` | Período de la media móvil de señal. |
| `Phase2` | `100` | Parámetro de fase Jurik para el suavizado de señal. |
| `Power` | `5` | Exponente usado al elevar los ratios de apertura/cierre. |
| `SignalBar` | `1` | Cuántas velas cerradas esperar antes de actuar en un cambio de color. |
| `BuyOpenEnabled` | `true` | Permitir abrir posiciones largas. |
| `SellOpenEnabled` | `true` | Permitir abrir posiciones cortas. |
| `BuyCloseEnabled` | `true` | Cerrar largos cuando aparece una señal bajista. |
| `SellCloseEnabled` | `true` | Cerrar cortos cuando aparece una señal alcista. |
| `StopLossPoints` | `0` | Distancia absoluta de stop-loss. `0` desactiva el stop. |
| `TakeProfitPoints` | `0` | Distancia absoluta de take-profit. `0` desactiva el objetivo. |
| `CandleType` | Marco temporal `H4` | Serie de velas procesada por la estrategia. |

## Reglas de trading

- **Transición alcista** (`color` cambia a 2): cerrar cortos activos (si se permite) y abrir una posición larga cuando `Position <= 0` y `BuyOpenEnabled` es verdadero.
- **Transición bajista** (`color` cambia a 0): cerrar largos activos (si se permite) y abrir una posición corta cuando `Position >= 0` y `SellOpenEnabled` es verdadero.
- Color 1 (neutral) no activa operaciones.
- Las señales se evalúan usando la barra ubicada `SignalBar` pasos detrás de la vela terminada más reciente para imitar el timing original de MetaTrader.

## Gestión de riesgos

`StopLossPoints` y `TakeProfitPoints` configuran `StartProtection` con distancias absolutas. Cuando cualquiera de los valores es cero, la protección respectiva se desactiva.

## Notas

- Solo se implementan directamente los métodos de suavizado disponibles en StockSharp. JurX, ParMA, T3 y VIDYA del código original se mapean a EMA como alternativa funcional.
- Los parámetros de fase se mantienen por compatibilidad pero solo afectan a las medias basadas en Jurik/Kaufman.
- La estrategia usa órdenes de mercado igual que el asesor experto original. La gestión de slippage de la versión MQL no se reproduce porque StockSharp maneja la ejecución mediante conectores.
