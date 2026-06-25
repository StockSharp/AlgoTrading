# Estrategia Martingale Bone Crusher
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia Martingale Bone Crusher** replica el comportamiento del expert advisor original de MetaTrader. La estrategia opera en la dirección de una comparación de media móvil rápida/lenta y aplica un modelo de gestión de dinero martingala que aumenta el tamaño de la orden después de una operación perdedora. Hay disponible un amplio conjunto de herramientas de gestión de riesgos, incluyendo objetivos de dinero fijo, objetivos de porcentaje, un movimiento de breakeven configurable, niveles clásicos de stop-loss/take-profit medidos en pasos de precio, y un trailing stop de protección de ganancias medido en dinero.

## Lógica de trading

- **Generación de señales** – se calculan dos medias móviles simples en la serie de velas principal. Cuando la media rápida está por debajo de la lenta, la estrategia busca entradas largas. Cuando está por encima, busca entradas cortas. No se realizan nuevas operaciones mientras hay una posición activa.
- **Secuenciación martingala** – después de cada operación completada, se actualiza el tamaño de la siguiente posición. Si la última operación cerró con pérdida, el siguiente volumen se multiplica o incrementa (dependiendo de la configuración). Las operaciones ganadoras restablecen el tamaño de posición al valor inicial.
- **Selección de modo** – se proporcionan dos variantes de martingala:
  - `Martingale1`: la siguiente operación siempre sigue la dirección actual de la media móvil, incluso después de una pérdida.
  - `Martingale2`: después de una pérdida, la siguiente operación se invierte respecto a la dirección que perdió. Esto refleja el comportamiento de la segunda opción del Expert Advisor original.
- **Controles de riesgo** – mientras una posición está abierta, la estrategia evalúa continuamente:
  - niveles clásicos de stop-loss y take-profit expresados en pasos de precio;
  - un trailing stop opcional que sigue el precio extremo con una distancia de paso fija;
  - un movimiento de breakeven que desplaza el nivel de salida después de que la posición se mueve a favor en una distancia configurable;
  - objetivos de ganancia globales basados en dinero y porcentaje que cierran la posición cuando el PnL flotante agregado supera los umbrales;
  - un trailing stop adicional en dinero que asegura las ganancias acumuladas una vez que la ganancia flotante alcanza el nivel de activación.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `UseTakeProfitMoney` | Habilita un objetivo de take-profit de dinero fijo. |
| `TakeProfitMoney` | Cantidad de dinero que cierra la operación cuando `UseTakeProfitMoney` está activo. |
| `UseTakeProfitPercent` | Habilita un objetivo de ganancia expresado como porcentaje del valor inicial de la cartera. |
| `TakeProfitPercent` | Porcentaje utilizado cuando `UseTakeProfitPercent` está habilitado. |
| `EnableTrailing` | Habilita el trailing stop basado en dinero. |
| `TrailingTakeProfitMoney` | Ganancia flotante requerida para armar el trailing stop de dinero. |
| `TrailingStopMoney` | Reducción permitida desde el pico de ganancia flotante después de que el trailing stop está activo. |
| `MartingaleModes` | Selecciona entre el comportamiento `Martingale1` y `Martingale2`. |
| `UseMoveToBreakeven` | Habilita el ajuste de stop de breakeven. |
| `MoveToBreakevenTrigger` | Pasos de precio que la operación debe moverse a favor antes de que se active la protección de breakeven. |
| `BreakevenOffset` | Distancia añadida al precio de entrada cuando se coloca el stop de breakeven. |
| `Multiply` | Multiplicador aplicado al siguiente volumen después de una pérdida cuando `DoubleLotSize` es `true`. |
| `InitialVolume` | Volumen de orden base utilizado para la primera operación y después de las ganancias. |
| `DoubleLotSize` | Cambia entre dimensionamiento martingala multiplicativo (`true`) y aditivo (`false`). |
| `LotSizeIncrement` | Incremento de volumen aplicado después de una pérdida cuando `DoubleLotSize` es `false`. |
| `TrailingStopSteps` | Distancia del trailing stop en pasos de precio. |
| `StopLossSteps` | Distancia clásica de stop-loss en pasos de precio. |
| `TakeProfitSteps` | Distancia clásica de take-profit en pasos de precio. |
| `FastPeriod` | Período de la media móvil simple rápida. |
| `SlowPeriod` | Período de la media móvil simple lenta. |
| `CandleType` | Serie de velas utilizada para todos los cálculos de indicadores. |

## Notas

- El volumen de posición se alinea con el paso de volumen del instrumento, los límites mínimos y máximos.
- Los cálculos de ganancia flotante dependen del `PriceStep` y `StepPrice` del instrumento. Si son cero, las protecciones basadas en dinero se omiten automáticamente.
- Solo se proporciona la implementación en C#. La versión en Python se omite intencionalmente según los requisitos de la tarea.
