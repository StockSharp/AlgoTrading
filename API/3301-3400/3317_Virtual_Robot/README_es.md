# Estrategia Virtual Robot
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Visión general

La estrategia Virtual Robot recrea el enfoque de promediado basado en grid del asesor experto MetaTrader original. El algoritmo mantiene dos grids virtuales independientes (largo y corto) en un marco de velas configurable. Solo cuando el número de niveles virtuales alcanza el umbral definido se envían órdenes reales de mercado. Esto permite simular el comportamiento MT4 donde los niveles virtuales guían la gestión real de posiciones.

## Lógica de negociación

1. **Creación de escalera virtual:** en cada vela terminada la estrategia compara el cierre con el precio de apertura.
   - Si la vela cierra por encima de la apertura, se añade un nuevo nivel virtual largo cuando la distancia desde el nivel largo anterior supera el paso en pips.
   - Si la vela cierra por debajo, se aplica la misma lógica a la escalera virtual corta.
   - Las primeras `VirtualStepper` operaciones virtuales usan el lote base; los niveles posteriores escalan el tamaño por `Multiplier`.
2. **Promoción a órdenes reales:** después de que existan al menos `StartingRealOrders` niveles virtuales para un lado (o una cesta real tenga drawdown de al menos un paso en pips), la estrategia abre una orden real de mercado con volumen calculado mediante el multiplicador de martingala (`Multiplier * distance / PipStep`).
3. **Gestión de cesta:** la estrategia sigue:
   - El último precio de ejecución y volumen de cada lado.
   - El promedio ponderado de la cesta abierta (real o virtual, según `RealAverageThreshold`).
4. **Lógica de take-profit:** las posiciones se cierran cuando se cumple cualquiera de estas condiciones:
   - El precio se mueve `MinTakeProfitPips` desde la primera orden virtual (take-profit de un solo nivel).
   - El precio vuelve al promedio virtual ponderado más/menos `AverageTakeProfitPips` para grids multinivel.
   - Se alcanza el nivel calculado de take-profit individual o promediado (derivado de `TakeProfitPips` / `AverageTakeProfitPips`).
5. **Lógica de stop-loss:** un stop suave se deriva de la última orden ejecutada usando `StopLossPips`. Cuando el precio cruza el nivel protector, la cesta se liquida.
6. **Seguridad de volumen:** los lotes se normalizan contra los metadatos del instrumento (`VolumeStep`, `MinVolume`, `MaxVolume`) y se limitan por `MaxVolume`.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `CandleType` | Serie de velas usada para formar la escalera virtual (por defecto, velas de 60 minutos). |
| `StopLossPips` | Distancia de stop en pips desde la última orden ejecutada. |
| `TakeProfitPips` | Distancia de take-profit para cestas de una sola orden. |
| `MinTakeProfitPips` | Beneficio mínimo requerido para cerrar un único nivel virtual. |
| `AverageTakeProfitPips` | Objetivo de beneficio aplicado al promedio ponderado de la cesta. |
| `BaseVolume` | Tamaño de lote base para las primeras órdenes de grid. |
| `MaxVolume` | Tamaño de lote máximo permitido. |
| `Multiplier` | Multiplicador de lote para entradas promediadas. |
| `RealStepper` | Número de órdenes reales ejecutadas antes de aplicar el multiplicador. |
| `VirtualStepper` | Órdenes virtuales ejecutadas con lote base antes de escalar. |
| `PipStepPips` | Excursión adversa mínima (en pips) entre niveles sucesivos del grid. |
| `MaxTrades` | Límite duro del número de órdenes reales por lado. |
| `StartingRealOrders` | Número de órdenes virtuales requerido antes de colocar la primera real. |
| `RealAverageThreshold` | Cambia el precio promediado de virtual a real una vez ejecutado este número de órdenes. |
| `VisualMode` | Conservado por paridad con la entrada MT4 (sin efecto en StockSharp). |

## Notas de implementación

- La estrategia usa posiciones netas (modelo de cartera StockSharp) y por tanto no puede mantener cestas largas y cortas simultáneas independientes como el modo hedging de MT4. Cuando ambas escaleras virtuales se activan, la señal más reciente invertirá la posición neta.
- El dibujo de gráfico del EA original se omite intencionalmente; todos los niveles virtuales se mantienen internamente.
- Los pasos de precio se derivan de `Security.PriceStep` (con ajuste 10x para instrumentos forex de tres/cinco dígitos) para reflejar la conversión de pips de MT4.
- Las órdenes protectoras se modelan monitorizando precios en el manejador de velas y enviando salidas a mercado, en lugar de adjuntar stops/límites del lado del broker.

## Consejos de uso

1. Asegúrese de que los metadatos del instrumento (`PriceStep`, `VolumeStep`, `MinVolume`, `MaxVolume`) estén completos para que la conversión de pips y normalización de lotes coincidan con las reglas del broker.
2. Empiece en simulación o con volumen pequeño para validar que las distancias del grid y multiplicadores se alineen con el broker que planea operar.
3. Ajuste `StartingRealOrders` y `RealStepper` para controlar la agresividad del escalado martingala.
