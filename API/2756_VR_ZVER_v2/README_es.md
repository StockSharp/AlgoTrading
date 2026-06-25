# Estrategia VR-ZVER v2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia VR-ZVER v2 es un port de StockSharp del clásico asesor experto de MetaTrader. Mantiene la idea de triple confirmación del script original: cada operación debe ser respaldada por medias móviles, el oscilador estocástico y RSI. Solo cuando todos los filtros habilitados coinciden, la estrategia coloca una orden de mercado.

## Lógica de trading

- Las señales se evalúan cuando cierra una vela. Las fluctuaciones intrábarra solo se usan para validar stops o objetivos.
- Tres medias móviles exponenciales (rápida, lenta, muy lenta) deben apilarse en el mismo orden para validar la tendencia cuando el filtro de MA está habilitado.
- El filtro estocástico espera un cruce de %K/%D cerca de las bandas superior e inferior configurables.
- El filtro RSI requiere que el oscilador salga de una zona neutral (por debajo de la banda inferior para largos, por encima de la banda superior para cortos).
- Una señal se acepta solo cuando cada filtro habilitado vota en la misma dirección. Si algún filtro no está de acuerdo, no se opera.
- La estrategia abre una posición a la vez. No hace hedging ni construye grillas; cuando está plana espera la próxima señal alineada.

## Gestión de posición

- Un take-profit y stop-loss se expresan en pips. El stop inicial se establece en dos tercios de la distancia configurada, reproduciendo el comportamiento original del EA.
- Un activador de punto de equilibrio (también en pips) mueve el stop al precio de entrada una vez que la operación ha ganado la distancia especificada.
- Los trailing stops usan una distancia y un paso adicional. El paso evita que el stop se actualice en cada pequeño movimiento ascendente y coincide con la lógica de trailing de MT5.
- Los trades largos y cortos comparten las mismas reglas de gestión y reaccionan simétricamente a los máximos/mínimos de la vela.

## Dimensionamiento de posición

- `FixedVolume` mayor que cero abre cada orden con un tamaño fijo.
- Cuando `FixedVolume` se establece en cero, la estrategia calcula el volumen desde `RiskPercent`, el valor actual del portafolio y la distancia del stop. El paso de precio y el precio de paso se usan para convertir la distancia en pips en riesgo monetario.
- Los volúmenes se redondean para respetar las restricciones de `VolumeMin`, `VolumeMax` y `VolumeStep` del instrumento. Las órdenes se omiten si el tamaño calculado es demasiado pequeño.

## Parámetros

| Nombre | Descripción |
| ------ | ----------- |
| `CandleType` | Marco temporal usado para la generación de señales (por defecto velas de 15 minutos). |
| `FixedVolume`, `RiskPercent` | Elegir entre dimensionamiento fijo o basado en riesgo. |
| `StopLossPips`, `TakeProfitPips` | Distancias de protección base en pips. |
| `TrailingStopPips`, `TrailingStepPips`, `BreakevenPips` | Umbrales de gestión de operaciones. |
| `AllowLongs`, `AllowShorts` | Habilitar o deshabilitar direcciones individuales. |
| `UseMovingAverageFilter`, `FastMaPeriod`, `SlowMaPeriod`, `VerySlowMaPeriod` | Filtro de tendencia EMA triple. |
| `UseStochastic`, `StochasticKPeriod`, `StochasticDPeriod`, `StochasticSmooth`, `StochasticUpperLevel`, `StochasticLowerLevel` | Configuraciones de confirmación estocástica. |
| `UseRsi`, `RsiPeriod`, `RsiUpperLevel`, `RsiLowerLevel` | Banda de confirmación RSI. |

## Notas

- La conversión de pips emula el EA original: los símbolos de cinco y tres dígitos multiplican el paso de precio por diez antes de calcular los valores de pip.
- El port de StockSharp solo usa órdenes de mercado. Las funciones de bloqueo y órdenes pendientes de la versión MetaTrader se omiten intencionalmente para mantener la implementación consistente con la API de alto nivel.
- Adjunte la estrategia a un gráfico si desea ver los overlays de EMA, estocástico y RSI; se dibujan automáticamente cuando hay disponible un área de gráfico.
