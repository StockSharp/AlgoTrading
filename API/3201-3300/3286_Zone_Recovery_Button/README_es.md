# Estrategia Zone Recovery Button
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **estrategia Zone Recovery Button** es una conversión directa del asesor experto de MetaTrader "ZONE RECOVERY BUTTON VER1" (`MQL/25347`). El robot original dependía de botones BUY/SELL en el gráfico para iniciar una cesta cubierta. En esta adaptación a StockSharp, el panel manual se sustituye por parámetros, mientras que se conservan la lógica de recuperación, los take-profits monetarios/porcentuales, el trailing stop en divisa y la protección equity-stop.

Cuando la estrategia recibe una dirección inicial, abre una orden de mercado inicial. Cada vez que el precio atraviesa el ancho de zona configurado, el sistema apila una operación opuesta con mayor volumen. La cesta se cierra cuando se alcanza el take-profit de referencia, la ganancia flotante llega al objetivo monetario/porcentual configurado, el trailing stop devuelve demasiada ganancia o se infringe el umbral de equity-stop.

## Reglas de trading

1. **Dirección inicial** - emula pulsar el botón BUY o SELL. La estrategia abre la primera orden inmediatamente cuando recibe datos y tiene permiso para operar. Después de cerrar la cesta, puede reiniciar automáticamente con la misma dirección.
2. **Recuperación por zonas** - en cada paso de recuperación, el algoritmo alterna la dirección. En ciclos largos vende cuando el precio cae por debajo de `Base Price - Zone Width`, y luego compra de nuevo cuando el mercado vuelve por encima de la base. En ciclos cortos, la lógica se refleja.
3. **Escalado de volumen** - cada cobertura adicional multiplica el volumen anterior o añade un incremento fijo, reproduciendo la configuración "Lots"/"Multiply" del EA.
4. **Controles de take-profit** - la cesta se cierra por:
   - take-profit basado en pips medido desde el precio de referencia;
   - objetivo monetario en la divisa de la cuenta;
   - objetivo porcentual calculado desde el valor actual de la cartera;
   - lógica trailing que bloquea ganancias cuando la ganancia flotante supera un umbral y después devuelve más que el drawdown permitido;
   - equity-stop de emergencia que compara la pérdida flotante actual con el equity más alto observado durante el ciclo.

## Parámetros

| Parámetro | Predeterminado | Descripción |
|-----------|----------------|-------------|
| `CandleType` | `TimeSpan.FromMinutes(5)` | Tipo de vela usado para monitorizar movimientos de precio. |
| `StartDirection` | `Buy` | Dirección inicial del ciclo (BUY/SELL/NONE). |
| `AutoRestart` | `true` | Reinicia automáticamente un nuevo ciclo después de cerrar la cesta anterior. |
| `TakeProfitPips` | `200` | Distancia en pips entre el precio base y el objetivo de take-profit en pips. |
| `ZoneRecoveryPips` | `10` | Distancia en pips que dispara la siguiente cobertura en la dirección opuesta. |
| `InitialVolume` | `0.01` | Volumen (lotes) de la primera operación. |
| `UseVolumeMultiplier` | `true` | Si está habilitado, cada cobertura multiplica el volumen anterior; de lo contrario se añade `VolumeIncrement`. |
| `VolumeMultiplier` | `2` | Multiplicador aplicado cuando `UseVolumeMultiplier` es `true`. |
| `VolumeIncrement` | `0.01` | Incremento de volumen cuando `UseVolumeMultiplier` es `false`. |
| `MaxTrades` | `100` | Número máximo de operaciones en la cesta. |
| `UseMoneyTakeProfit` | `false` | Habilita el cierre cuando la ganancia flotante supera `MoneyTakeProfit`. |
| `MoneyTakeProfit` | `40` | Objetivo de ganancia en la divisa de la cuenta. |
| `UsePercentTakeProfit` | `false` | Habilita el cierre cuando la ganancia flotante supera `PercentTakeProfit` por ciento del balance. |
| `PercentTakeProfit` | `10` | Objetivo de ganancia como porcentaje del valor actual de la cartera. |
| `EnableTrailing` | `true` | Habilita el trailing de ganancia en divisa. |
| `TrailingProfitThreshold` | `40` | Nivel de ganancia que activa trailing. |
| `TrailingDrawdown` | `10` | Drawdown permitido desde el pico de ganancia flotante antes de cerrar la cesta. |
| `UseEquityStop` | `true` | Habilita el equity stop de emergencia. |
| `TotalEquityRiskPercent` | `1` | Pérdida flotante máxima (en porcentaje del máximo de equity) antes de aplanar. |

## Notas

- La estrategia funciona con cualquier instrumento que proporcione valores `PriceStep` y `StepPrice`. Estos parámetros son necesarios para convertir distancias en pips a unidades de precio y divisa.
- Como StockSharp usa un modelo de posición neta, la cuadrícula de cobertura se simula internamente. La estrategia mantiene su propia lista de pasos de operación para reproducir el cálculo de ganancias de MetaTrader.
- La lógica trailing opera sobre la ganancia flotante de la cesta activa. No usa trailing stops basados en órdenes.
