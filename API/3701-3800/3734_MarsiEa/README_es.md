# MarsiEaEstrategia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

`MarsiEaStrategy` replica la lógica del MetaTrader asesor experto MARSIEA original dentro del StockSharp alto nivel API. La estrategia combina una media móvil simple con un filtro de índice de fuerza relativa (RSI) y solo mantiene una posición a la vez. Las órdenes protectoras de stop-loss y take-profit se miden en pips exactamente igual que la implementación de origen, mientras que el volumen negociado se dimensiona dinámicamente a partir del capital de la cartera.

## Lógica comercial

1. **Preparación de datos**
   - Se ejecuta una media móvil simple (SMA) con longitud configurable en la serie de velas seleccionada.
   - Un RSI con período configurable usa las mismas velas.
   - La serie de velas se puede configurar a través del parámetro `CandleType` y de forma predeterminada son velas de un minuto.

2. **Reglas de entrada**
   - La estrategia requiere que se formen ambos indicadores y que no exista ninguna posición abierta.
   - **Configuración larga:** el precio de cierre está por encima del SMA y el RSI está por debajo del umbral de sobreventa.
   - **Configuración corta:** el precio de cierre está por debajo del SMA y el RSI está por encima del umbral de sobrecompra.
   - Solo se puede abrir una posición a la vez, lo que refleja el comportamiento experto de MetaTrader.

3. **Reglas de salida**
   - Inmediatamente después de iniciar una operación, la estrategia registra una distancia fija de stop-loss y take-profit, ambas definidas en pips.
   - No existen condiciones de salida adicionales; las órdenes de protección manejan la posición de cierre.

## Dimensionamiento de riesgos y posiciones

- `RiskPercent` controla el porcentaje del valor actual de la cartera arriesgado por operación.
- El valor del pip se calcula a partir de `Security.PriceStep`, `Security.StepPrice` y el número de dígitos, emulando el cheque `_Digits` de MQL.
- El volumen se redondea al `Security.VolumeStep` más cercano permitido y respeta `Security.VolumeMin` cuando esté disponible.
- Si no se puede calcular el tamaño basado en el riesgo (faltan metadatos del instrumento o parada cero), la estrategia vuelve a la propiedad `Volume` (por defecto, 1 contrato/lote).

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `CandleType` | Serie de velas utilizadas para los cálculos de indicadores. |
| `MaPeriod` | Longitud del indicador SMA. |
| `RsiPeriod` | Longitud retrospectiva para RSI. |
| `RsiOverbought` | RSI umbral que define un mercado de sobrecompra para cortos. |
| `RsiOversold` | RSI umbral que define un mercado de sobreventa para largos. |
| `RiskPercent` | Porcentaje de capital arriesgado por operación. |
| `StopLossPips` | Distancia de stop-loss expresada en pips. |
| `TakeProfitPips` | Distancia de toma de ganancias expresada en pips. |

## Notas sobre la conversión

- La implementación MetaTrader se negoció a precios de oferta y demanda; este puerto utiliza el cierre de la vela como referencia de entrada porque los ticks intrabar no están disponibles en el nivel alto API.
- El tamaño del pip sigue la misma regla que la versión MQL: los símbolos de cinco o tres dígitos multiplican el paso del precio por diez.
- `StartProtection()` se invoca una vez para que el motor vincule automáticamente las órdenes de stop-loss y take-profit a la posición abierta.
- La estrategia conserva el comportamiento original de omitir nuevas entradas mientras cualquier posición esté activa.
