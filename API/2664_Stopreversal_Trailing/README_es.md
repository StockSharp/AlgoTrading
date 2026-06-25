# Estrategia Stopreversal Trailing
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La Estrategia Stopreversal Trailing reproduce el experto MT5 `Exp_Stopreversal.mq5`. Utiliza el indicador personalizado Stopreversal para construir una línea de trailing stop dinámica alrededor del precio de vela seleccionado. Cuando el precio perfora esta línea de trailing hacia arriba, la estrategia lo trata como una reversión alcista, opcionalmente cierra la exposición corta y abre una nueva posición larga. Una perforación a la baja produce la acción bajista simétrica. Las señales pueden retrasarse un número configurable de barras cerradas para coincidir con el comportamiento del asesor experto original.

## Detalles

- **Lógica de entrada**: reacciona a las flechas del indicador Stopreversal producidas cuando el precio cruza el trailing stop adaptativo.
- **Largo/Corto**: ambas direcciones son compatibles con interruptores independientes para habilitar entradas largas o cortas.
- **Lógica de salida**: las señales Stopreversal opuestas pueden cerrar posiciones existentes; también están disponibles niveles protectores de stop-loss y take-profit.
- **Stops**: stop-loss y take-profit estáticos en pasos de precio más las reversiones impulsadas por el indicador.
- **Fuente de datos**: cualquier marco temporal; el valor predeterminado usa velas de 4 horas, reflejando la llamada multitemporal del experto original.
- **Retraso de señal**: el parámetro `SignalBar` retrasa la ejecución de órdenes el número especificado de barras completadas (predeterminado 1 barra).
- **Gestión de riesgo**: los stops duros opcionales se expresan en pasos de precio del instrumento; el servicio de protección de posición se activa al inicio.
- **Parámetros del indicador**: el offset de trailing `Npips` controla la distancia entre el precio y el stop; `PriceMode` selecciona el precio de vela usado por el trailing stop.
- **Valores predeterminados**:
  - `Volume` = 1
  - `StopLossSteps` = 1000
  - `TakeProfitSteps` = 2000
  - `BuyPositionOpen` = true
  - `SellPositionOpen` = true
  - `BuyPositionClose` = true
  - `SellPositionClose` = true
  - `Npips` = 0.004
  - `PriceMode` = Close
  - `SignalBar` = 1

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `CandleType` | Suscripción de velas utilizada tanto para los cálculos de Stopreversal como para el trading. El valor predeterminado es un marco temporal de 4 horas. |
| `Volume` | Tamaño base de la orden enviada al entrar en una nueva posición. |
| `StopLossSteps` | Distancia desde la entrada hasta el stop-loss en pasos de precio; establecer en 0 para desactivar. |
| `TakeProfitSteps` | Distancia desde la entrada hasta el take-profit en pasos de precio; establecer en 0 para desactivar. |
| `BuyPositionOpen` | Habilita la apertura de posiciones largas cuando ocurre una señal alcista. |
| `SellPositionOpen` | Habilita la apertura de posiciones cortas cuando ocurre una señal bajista. |
| `BuyPositionClose` | Cierra cualquier posición larga existente cuando se recibe una señal bajista. |
| `SellPositionClose` | Cierra cualquier posición corta existente cuando se recibe una señal alcista. |
| `Npips` | Multiplicador fraccionario aplicado al trailing stop para ampliar o reducir la distancia de reversión. |
| `PriceMode` | Variante de precio aplicada (cierre, apertura, máximo, mínimo, mediana, típico, ponderado, promedio simple, promedio cuádruple, seguimiento de tendencia o Demark). |
| `SignalBar` | Número de velas completamente cerradas a esperar antes de reaccionar a una señal, coincidiendo con el parámetro MT5. |

## Filtros

- **Categoría**: Reversión con seguimiento de tendencia
- **Dirección**: Bidireccional
- **Indicadores**: Stopreversal (trailing stop respaldado por ATR)
- **Stops**: Stop-loss y take-profit estáticos, opcionales
- **Marco temporal**: Configurable (predeterminado H4)
- **Estacionalidad**: Ninguna
- **Redes neuronales**: No
- **Divergencia**: No
- **Complejidad**: Medio debido a la lógica de trailing personalizada
- **Nivel de riesgo**: Ajustable mediante la distancia del stop y el offset de trailing
