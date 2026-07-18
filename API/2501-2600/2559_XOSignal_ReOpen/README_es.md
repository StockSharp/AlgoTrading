# Estrategia XOSignal Re-Open
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia reproduce el experto de MetaTrader *Exp_XOSignal_ReOpen* dentro de StockSharp usando la API de alto nivel. Opera con datos de velas del símbolo y marco temporal seleccionados con un detector de rompimiento de estilo XO construido sobre ATR(13). Cuando aparece una flecha hacia arriba, el algoritmo cierra los cortos, opcionalmente abre un largo, y luego agrega a la posición cada vez que el precio progresa un número fijo de ticks. Las flechas hacia abajo se comportan simétricamente para los cortos. Se aplican stops duros y objetivos en ticks a cada capa de la pirámide.

## Lógica central

- La estrategia calcula un canal de rango XO cuyas bandas se expanden en `Range * PriceStep`. Los rompimientos reinician las bandas y establecen la dirección de tendencia actual.
- ATR(13) controla cuán por debajo/encima de la vela se trazan los niveles de entrada virtual (flechas): las flechas largas aparecen en `Low - ATR * 3/8`, las flechas cortas en `High + ATR * 3/8`.
- Solo se procesan velas completadas. Las señales pueden retrasarse `SignalBar` barras para imitar la lógica de buffering original.

## Reglas de entrada

- **Entrada larga**: cuando se emite una flecha hacia arriba, las entradas largas están permitidas (`EnableBuyEntries = true`), no hay posición corta abierta, y la señal aún no ha sido ejecutada. El volumen de la operación es igual a `Volume`.
- **Reentrada larga**: mientras se está en una posición larga, cada `PriceStepTicks` ticks adicionales a favor de la operación (basándose en el cierre de la vela) activa otra compra hasta que se abren `MaxPyramidingPositions` capas. Cada reentrada actualiza los niveles de stop/objetivo protectores.
- **Entrada/reentrada corta**: lógica espejo del lado largo usando la flecha hacia abajo.

## Reglas de salida

- **Salidas basadas en señal**: una flecha hacia arriba cierra cada corto activo cuando `EnableSellExits = true`; una flecha hacia abajo cierra el largo cuando `EnableBuyExits = true`.
- **Salidas de riesgo**: cada capa abierta lleva la misma distancia de stop-loss y take-profit definida en ticks (`StopLossTicks`, `TakeProfitTicks`). Cuando el precio perfora el nivel dentro de la vela actual, toda la posición se aplana.
- **Aplanado manual**: las señales de entrada opuestas también neutralizan la dirección anterior antes de abrir una nueva posición.

## Gestión de posición

- El tamaño de la posición es fijo por `Volume` para cada orden.
- Stop-loss y take-profit se miden en ticks del instrumento. Establecerlos en cero deshabilita la protección correspondiente.
- El contador de pirámide se reinicia a cero después de cualquier salida completa para que la siguiente señal comience desde una posición base fresca.

## Parámetros

| Parámetro | Descripción | Predeterminado |
|-----------|-------------|----------------|
| `Volume` | Tamaño de orden para cada entrada | `1` |
| `StopLossTicks` | Distancia de stop en ticks, 0 deshabilita | `1000` |
| `TakeProfitTicks` | Distancia de take-profit en ticks, 0 deshabilita | `2000` |
| `PriceStepTicks` | Movimiento favorable mínimo antes de añadir a la posición | `300` |
| `MaxPyramidingPositions` | Número máximo de entradas en capas (incluyendo la primera) | `10` |
| `EnableBuyEntries` / `EnableSellEntries` | Permitir abrir posiciones largas/cortas | `true` |
| `EnableBuyExits` / `EnableSellExits` | Permitir cerrar posiciones largas/cortas en flechas opuestas | `true` |
| `CandleType` | Marco temporal usado para señales | `H4` |
| `Range` | Altura de la caja XO en ticks | `10` |
| `AppliedPrice` | Fuente de precio usada en el detector XO | `Close` |
| `SignalBar` | Número de barras cerradas para retrasar señales | `1` |

La estrategia está diseñada para backtesting u operativa en vivo con instrumentos que proporcionan un paso de precio fiable. Ajustar las distancias basadas en ticks para que coincidan con la volatilidad del mercado seleccionado.
