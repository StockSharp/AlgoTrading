# Estrategia GPF TCPivotLimit
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia GPF TCPivotLimit** recrea el MetaTrader 4 asesor experto `gpfTCPivotLimit.mq4` dentro del marco StockSharp. El sistema opera con **velas horarias** y reacciona a las reversiones alrededor de los clásicos **niveles de pivote diario**. Cada nuevo día de negociación, la estrategia calcula el pivote, tres niveles de resistencia (R1-R3) y tres niveles de soporte (S1-S3) del máximo, mínimo y cierre del día anterior. Tan pronto como comienza el día siguiente, evalúa las dos últimas velas horarias completadas para decidir si el precio rechazó una zona de resistencia o de soporte y abre una orden de mercado en la dirección opuesta.

## Lógica de trading

1. **Cálculo de pivote**: cuando comienza una nueva sesión diaria, la estrategia almacena el máximo, el mínimo y el cierre del día anterior y luego calcula:
   - `Pivot = (High + Low + Close) / 3`
   - `R1 = 2 × Pivot − Low`, `S1 = 2 × Pivot − High`
   - `R2 = Pivot + (High − Low)`, `S2 = Pivot − (High − Low)`
   - `R3 = High + 2 × (Pivot − Low)`, `S3 = Low − 2 × (High − Pivot)`
2. **Confirmación de entrada**: con el nuevo día en marcha, se inspeccionan las dos últimas velas horarias cerradas (`t-2` y `t-1`).
   - Se abre un **corto** si la vela `t-2` sondea por encima de la resistencia seleccionada (muy por encima o cierra en el nivel), se abre por debajo de ella y la vela `t-1` se cierra nuevamente por debajo del nivel.
   - Se abre un **largo** si la vela `t-2` cae por debajo del soporte seleccionado (mínimo por debajo o cierre en el nivel), se abre por encima de él y la vela `t-1` vuelve a cerrar por encima del nivel.
3. **Preajustes de objetivos**: el asesor experto original expone cinco diseños de ganancias/paradas. La siguiente tabla muestra el mapeo exacto que se conserva en este puerto.

| `TargetMode` | Gatillo largo | Parada larga | Objetivo largo | Gatillo corto | breve parada | Objetivo corto |
|-------------:|--------------|-----------|-------------|---------------|------------|--------------|
| 1 | `S1` | `S2` | `R1` | `R1` | `R2` | `S1` |
| 2 | `S1` | `S2` | `R2` | `R1` | `R2` | `S2` |
| 3 | `S2` | `S3` | `R1` | `R2` | `R3` | `S1` |
| 4 | `S2` | `S3` | `R2` | `R2` | `R3` | `S2` |
| 5 | `S2` | `S3` | `R3` | `R2` | `R3` | `S3` |

4. **Gestión de riesgos**: se ejecutan comprobaciones protectoras de stop-loss y take-profit en cada vela completa. La lógica de trailing stop opcional emula el comportamiento de MT4: una vez que el beneficio no realizado supera la distancia configurada, el stop se mueve a favor de la operación. Una salida opcional al final del día aplana la posición a las 23:00 hora del andén.

5. **Adaptación de volumen**: la entrada MetaTrader `isFloatLots` se refleja en el interruptor `UseDynamicVolume`. Cuando está habilitado, el tamaño de la posición se reduce después de operaciones perdedoras consecutivas, utilizando las entradas `DrawdownFactor` y `RiskPercentage`.

## Parámetros

| Nombre | Descripción | Predeterminado |
|------|-------------|---------|
| `BaseVolume` | Volumen base presentado con cada orden de mercado antes de ajustes de riesgo. | `1` |
| `UseDynamicVolume` | Reduce el tamaño de la operación después de más de una pérdida consecutiva. | `false` |
| `RiskPercentage` | Relación de riesgo por operación de referencia utilizada para escalar el volumen base (MetaTrader `MaxR`). | `0.02` |
| `DrawdownFactor` | Divisor aplicado al reducir el volumen después de una racha perdedora (MetaTrader `DcF`). | `3` |
| `TargetMode` | Selecciona la combinación de resistencia/soporte enumerada anteriormente (MetaTrader `TgtProfit`). | `1` |
| `TrailingPoints` | Distancia del trailing-stop expresada en puntos del instrumento. Establezca en `0` para desactivar. | `30` |
| `CloseAtSessionEnd` | Cuando `true` todas las posiciones se cierran en el cierre de la vela de las 23:00. | `false` |
| `LogSignals` | Imprime valores dinámicos, entradas y salidas en el registro de estrategia. | `false` |
| `CandleType` | Tipo de datos de vela utilizado para el análisis (el valor predeterminado es velas de 1 hora). | `TimeFrameCandleMessage(1h)` |

## Notas

- La estrategia emite **órdenes de mercado** igual que el EA original y no coloca órdenes pendientes.
- Los eventos de limitación de pérdidas y toma de ganancias se ejecutan con salidas de mercado para seguir siendo compatibles con todos los conectores StockSharp.
- Las distancias de seguimiento dependen del instrumento `PriceStep`. Si falta el paso, el mecanismo de seguimiento se desactiva automáticamente.
- El indicador de notificación por correo electrónico de la versión MT4 está representado por `LogSignals`, lo que genera mensajes de registro en lugar de correos electrónicos.
