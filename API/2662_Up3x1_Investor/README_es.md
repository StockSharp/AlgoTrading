# Estrategia Up3x1 Investor
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Up3x1 Investor es un port del clásico asesor experto de MetaTrader que reacciona a velas de expansión fuerte. Observa la última barra completada en el marco temporal configurado y abre una nueva posición en la siguiente barra si el rango y el cuerpo anteriores fueron lo suficientemente amplios en la dirección del cierre.

La estrategia está diseñada para mercados discrecionales como los principales pares de forex en el gráfico H1, pero los umbrales se pueden ajustar para otros instrumentos. Solo se mantiene una posición a la vez y cada orden utiliza la propiedad `Volume` de la estrategia como tamaño de la operación.

## Lógica de Trading

- **Fuente de señales** – velas de marco temporal completadas de `CandleType` (por defecto: 1 hora).
- **Condiciones de entrada**
  - Calcular el rango alto–bajo y el cuerpo absoluto de la vela anterior.
  - Entrar largo si la vela cerró por encima de la apertura y tanto el rango como el cuerpo superan sus respectivos umbrales en pips.
  - Entrar corto si la vela cerró por debajo de la apertura y tanto el rango como el cuerpo superan los umbrales.
  - Ignorar nuevas entradas mientras haya alguna posición abierta.
- **Gestión de posiciones**
  - Los niveles opcionales de stop-loss y take-profit se convierten de pips a unidades de precio usando `Security.PriceStep`.
  - Un trailing stop se activa una vez que el precio avanza `TrailingStopPips + TrailingStepPips` desde la entrada.
  - El trailing stop solo se mueve si el nuevo nivel está al menos `TrailingStepPips` más cerca del precio que el nivel de trailing anterior.
  - La estrategia sale de una posición cuando el precio toca los niveles de stop-loss, take-profit o trailing stop.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `CandleType` | Tipo de datos de las velas usadas para señales (por defecto: marco temporal de 1 hora). |
| `RangeThresholdPips` | Distancia mínima alto–bajo de la vela anterior, expresada en pips. |
| `BodyThresholdPips` | Distancia mínima apertura–cierre de la vela anterior, expresada en pips. |
| `StopLossPips` | Distancia de stop-loss en pips. Establecer en 0 para desactivar. |
| `TakeProfitPips` | Distancia de take-profit en pips. Establecer en 0 para desactivar. |
| `TrailingStopPips` | Distancia mantenida detrás del precio en el trailing. Establecer en 0 para desactivar el trailing. |
| `TrailingStepPips` | Movimiento adicional en pips requerido antes de que el trailing stop se ajuste. |

> **Nota:** Los umbrales en pips se multiplican por `Security.PriceStep`. Asegúrese de que el instrumento tenga un `PriceStep` válido para que las conversiones de pips reflejen correctamente su instrumento.

## Notas de Uso

1. Asigne el `Security` objetivo y el conector de trading antes de iniciar la estrategia.
2. Ajuste los umbrales en pips para reflejar la volatilidad de su mercado. Los pares de forex con cotizaciones de 5 dígitos típicamente usan 10 pips = 0.0010.
3. Establezca el `Volume` de la estrategia al tamaño de orden deseado. La lógica de dimensionamiento de posiciones del EA original está simplificada intencionalmente para mantener la versión de StockSharp transparente.
4. Dado que las señales se evalúan en velas cerradas, las entradas se envían inmediatamente después de la confirmación de la vela de expansión.
