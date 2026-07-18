# Estrategia de velas expertas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia de Velas Expertas** es una StockSharp versión del MetaTrader 5 *Expert_Candles* asesor experto. Monitorea la mayoría
acción reciente del precio para formaciones de reversión de velas que presentan sombras alargadas. Siempre que un compuesto alcista o bajista
Se detecta una vela, la estrategia abre una posición en la dirección respectiva y, opcionalmente, aplica una gestión del dinero idéntica a
el EA original.

La implementación sigue el StockSharp API de alto nivel: las suscripciones de velas se utilizan para crear barras compuestas, mientras que el mercado
Las órdenes y los niveles de protección se gestionan directamente desde la estrategia.

## Lógica comercial

1. Cada vez que se cierra una vela, la estrategia la fusiona con hasta `Range` velas anteriores hasta la altura completa del compuesto.
la barra supera `MinimumPoints` (convertida a puntos de precio utilizando el tamaño del pip del instrumento).
2. Se emite una señal **alcista** cuando la barra compuesta tiene una sombra superior poco profunda (`ShadowSmall`) y una sombra inferior profunda.
(`ShadowBig`). Se emite una señal **bajista** cuando la sombra inferior es poco profunda y la sombra superior es dominante.
3. El precio de entrada se desplaza de la vela cercana a `LimitFactor * rangeSize`. Los valores positivos emulan el límite original.
orden que se encuentra dentro del rango de velas.
4. Los objetivos de limitación de pérdidas y toma de ganancias se colocan en múltiplos de `StopLossFactor` y `TakeProfitFactor` de la altura compuesta.
Si se alcanza cualquiera de los niveles en las velas siguientes, la posición se cierra inmediatamente.
5. Las señales se consideran válidas para `ExpirationBars` velas completadas. Una vez que pasa la ventana de tiempo, la estrategia espera una nueva
formación antes de enviar nuevos pedidos.
6. Las señales opuestas cierran posiciones existentes antes de iniciar operaciones en la nueva dirección, imitando el comportamiento MQL5.

## gestión del dinero

* `FixedVolume` se utiliza como tamaño de pedido predeterminado.
* Cuando hay un stop-loss disponible y `RiskPercent` es mayor que cero, la estrategia arriesga el porcentaje seleccionado del
patrimonio de la cartera. La distancia de parada se convierte en valor monetario usando `Security.PriceStep` y `Security.StepPrice`.
* Los volúmenes se redondean al instrumento `VolumeStep` cuando el intercambio expone esos metadatos.

## Parámetros

| Parámetro | Predeterminado | Descripción |
|-----------|---------|-------------|
| `CandleType` | H1 | Plazo utilizado para solicitar velas. |
| `Range` | 3 | Número máximo de velas vecinas combinadas en un patrón compuesto. |
| `MinimumPoints` | 50 | Altura compuesta mínima en puntos (basada en `PriceStep`) necesaria para evaluar el patrón. |
| `ShadowBig` | 0,5 | Relación que la sombra dominante debe superar para confirmar la reversión. |
| `ShadowSmall` | 0,2 | Proporción máxima permitida para la sombra opuesta. |
| `LimitFactor` | 0.0 | Compensación de entrada como una fracción de la altura compuesta (los valores positivos desplazan el precio dentro de la vela). |
| `StopLossFactor` | 2.0 | Distancia de parada de pérdidas como múltiplo de la altura compuesta. Establezca en cero para desactivar la parada de protección. |
| `TakeProfitFactor` | 1.0 | Distancia de toma de ganancias como múltiplo de la altura compuesta. Establezca en cero para desactivar el objetivo. |
| `ExpirationBars` | 4 | Número de velas completadas durante las cuales una señal permanece activa. |
| `FixedVolume` | 0.1 | El tamaño de la orden alternativa se utiliza cuando no se puede calcular el tamaño basado en el riesgo. |
| `RiskPercent` | 10 | Porcentaje de capital arriesgado por operación cuando hay un límite de pérdidas disponible. |

## Notas de uso

- La estrategia se basa en `Security.PriceStep`, `Security.StepPrice` y `Security.VolumeStep` para replicar el punto MetaTrader.
cálculos. Proporcione metadatos precisos del instrumento o ajuste los parámetros en consecuencia.
- Las señales se evalúan únicamente en velas cerradas. Adjunte la estrategia a un conector de serie temporal que emite `CandleStates.Finished`
eventos para una ejecución confiable.
- Las salidas protectoras se simulan cerrando la posición tan pronto como el máximo o mínimo de una vela terminada viola el cálculo
nivel de stop-loss o take-profit.
- La lista de velas compuesta tiene un límite de 500 elementos para mantener predecible la huella de memoria.

## Diferencias vs. versión MetaTrader

- El puerto StockSharp utiliza órdenes de mercado en lugar de órdenes de límite pendientes. El desplazamiento de entrada reproduce el comportamiento límite mediante
desplazando el precio de ejecución en relación con el cierre de la vela.
- La administración del dinero es opcional; establecer `RiskPercent` en cero restaura el comportamiento del lote fijo desde el EA original.
- El manejo de stop-loss y take-profit se realiza dentro de la estrategia en lugar de mediante módulos de seguimiento externos.
