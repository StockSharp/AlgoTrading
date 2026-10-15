# Los distribuidores comercian con la estrategia MACD MQL4
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Dealers Trade MACD MQL4 es una conversión directa del asesor experto "Dealers Trade v7.74" para MetaTrader 4. Mantiene la administración piramidal del dinero y la lógica de pendiente MACD del sistema original mientras adapta el manejo de posiciones a las cuentas netas de StockSharp. La estrategia está diseñada para operaciones de swing en gráficos H4/D1 y se suma continuamente a la tendencia siempre que el impulso permanezca alineado con la línea principal MACD.

## Cómo funciona la estrategia

- **Detección de señal**: la estrategia se suscribe a velas del período de tiempo configurado y calcula un indicador clásico MACD (EMA rápida, EMA lenta y señal EMA). Un valor principal de MACD en aumento en comparación con la barra anterior indica un impulso alcista, mientras que un valor descendente indica un impulso bajista. El parámetro `ReverseCondition` se puede utilizar para invertir la dirección cuando se prefiere un enfoque contrario.
- **Espaciado y escala de pedidos**: solo hay una cesta direccional activa a la vez. Cuando MACD indica una tendencia larga, la estrategia abre una orden de compra de mercado inicial. Las compras adicionales se envían solo cuando el precio ha bajado al menos `SpacingPips * PriceStep` desde el último precio de entrada, reflejando el comportamiento "promediado" del script MQL. Las cestas cortas se comportan simétricamente cuando la pendiente MACD se vuelve negativa.
- **Tamaño del lote**: el tamaño del lote base es el `FixedVolume` fijo o, si `UseRiskSizing` está habilitado, un valor derivado del capital de la cartera y `RiskPercent`. Las cuentas mini se admiten a través del indicador `IsStandardAccount` que emula la opción original "La cuenta es normal". Cada pedido adicional dentro de la misma cesta se multiplica por `LotMultiplier` y se limita a `MaxVolume`.
- **Controles de riesgo**: los niveles estrictos de stop loss y takeprofit se adjuntan a cada posición utilizando las distancias `StopLossPips` y `TakeProfitPips`. Una vez que una operación ha aumentado `TrailingStopPips + SpacingPips` en ganancias, el nivel de parada se ajusta para mantener al menos `TrailingStopPips` de ganancias, reproduciendo la regla de seguimiento de la implementación de MetaTrader.
- **Protección de la cuenta**: cuando el número de operaciones abiertas alcanza `MaxTrades - OrdersToProtect` y el beneficio total no realizado supera `SecureProfit`, la operación más reciente se cierra para asegurar las ganancias antes de que se consideren nuevas órdenes. Esto corresponde al bloque "AccountProtection" en la fuente EA.

## Parámetros

| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `CandleType` | H4 | Plazo utilizado para los cálculos de MACD y la evaluación de señales. |
| `FixedVolume` | 0.1 | Tamaño de lote base cuando `UseRiskSizing` está deshabilitado. |
| `UseRiskSizing` | cierto | Permite dimensionar la posición basada en el equilibrio. |
| `RiskPercent` | 2 | Porcentaje de capital utilizado para dimensionar posiciones cuando `UseRiskSizing` es verdadero. |
| `IsStandardAccount` | cierto | Establecer en falso para cuentas mini (lotes divididos por 10). |
| `MaxVolume` | 5 | Volumen máximo permitido para un solo pedido. |
| `LotMultiplier` | 1.5 | Multiplicador aplicado al lote base por cada entrada adicional en la cesta. |
| `MaxTrades` | 5 | Número máximo de operaciones abiertas simultáneamente. |
| `SpacingPips` | 4 | Distancia mínima de pips entre entradas consecutivas. |
| `OrdersToProtect` | 3 | Número de órdenes mantenidas antes de que el bloque de protección pueda abrir nuevas operaciones. |
| `AccountProtection` | cierto | Habilita la lógica segura de protección de ganancias. |
| `SecureProfit` | 50 | Se requiere beneficio no realizado (en la moneda de la cuenta) para activar la protección. |
| `TakeProfitPips` | 30 | Distancia de obtención de beneficios por operación, expresada en pips. |
| `StopLossPips` | 90 | Distancia de stop loss por operación, expresada en pips. |
| `TrailingStopPips` | 15 | Distancia de trailing stop aplicada después de la activación. |
| `ReverseCondition` | falso | Invierte la interpretación de la pendiente MACD. |
| `MacdFast` | 14 | Longitud rápida de EMA para el indicador MACD. |
| `MacdSlow` | 26 | Longitud lenta de EMA para el indicador MACD. |
| `MacdSignal` | 1 | Longitud de la señal EMA para el indicador MACD. |

## Notas y limitaciones

- Las estrategias StockSharp gestionan una posición neta por valor, por lo que las cestas cortas y largas cubiertas no pueden coexistir. El EA original permitía la cobertura, pero la conversión cierra el lado opuesto antes de cambiar de dirección.
- La lógica de ganancias segura calcula las ganancias no realizadas utilizando los metadatos del instrumento `PriceStep` y `StepPrice`. Los instrumentos sin esta información retroceden a un valor nominal de pip de 0,0001 con un paso de unidad monetaria, por lo que se deben ajustar los umbrales en consecuencia.
- El dimensionamiento basado en el riesgo requiere un valor `StopLossPips` positivo. Cuando la distancia de parada es cero, la cantidad de riesgo calculada deja de estar definida y la estrategia omitirá las operaciones.
- La estrategia sólo funciona con velas cerradas. Las señales que se basaron en movimientos intrabar MACD en MetaTrader pueden aparecer como una barra más adelante en esta implementación, pero el comportamiento es significativamente más estable para las pruebas retrospectivas.
