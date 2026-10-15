# Estrategia del Plan Maestro de Salida
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

`MasterExitPlanStrategy` reproduce la lógica de gestión de riesgos del "Plan Maestro de Salida" del asesor experto MetaTrader utilizando el API de alto nivel de StockSharp. La estrategia no abre nuevas operaciones. En lugar de eso, supervisa la exposición existente, aplica una combinación de reglas de parada visibles y ocultas, rastrea las órdenes pendientes y cierra todo una vez que el capital alcanza un objetivo de ganancias configurado.

La implementación se suscribe a velas de un minuto para emular las llamadas `iOpen(symbol, PERIOD_M1, 1)` del script original. Todos los temporizadores son controlados por el programador de estrategias y evaluados cada segundo, coincidiendo con el comportamiento del bucle MetaTrader `EventSetTimer(1)`.

## Características

- **Salida objetivo de acciones**: cierra todas las posiciones cuando las ganancias de acciones de la cartera alcanzan el porcentaje configurado.
- **Niveles de parada estáticos y dinámicos**: monitorea tanto las distancias de parada desde el precio de entrada como los anclajes dinámicos basados en minutos.
- **Manejo de paradas ocultas**: ejecuta salidas protectoras internamente en lugar de depender de órdenes de cambio.
- **Módulo de stop dinámico**: se activa después de una ganancia mínima de dinero y sigue el stop con compensación de diferencial.
- **Seguimiento de órdenes pendientes**: vuelve a registrar automáticamente las órdenes buy-stop y sell-stop para que sigan el mercado.

## Parámetros

| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `EnableTargetEquity` | Habilitar la liquidación de objetivos de acciones. | `false` |
| `TargetEquityPercent` | Porcentaje del saldo actual utilizado como objetivo. | `1` |
| `EnableStopLoss` | Active el stop-loss estático estilo corredor. | `false` |
| `StopLossPoints` | Distancia de parada estática (MetaTrader puntos). | `2000` |
| `EnableDynamicStopLoss` | Ata el tope duro a la apertura del minuto anterior. | `false` |
| `DynamicStopLossPoints` | Distancia de parada dinámica (puntos). | `2000` |
| `EnableHiddenStopLoss` | Habilite el stop-loss estático oculto. | `false` |
| `HiddenStopLossPoints` | Distancia de parada estática oculta (puntos). | `800` |
| `EnableHiddenDynamicStopLoss` | Habilite la parada dinámica oculta según el minuto de apertura. | `false` |
| `HiddenDynamicStopLossPoints` | Distancia de parada dinámica oculta (puntos). | `800` |
| `EnableTrailingStop` | Habilite el módulo de trailing stop. | `false` |
| `TrailingStopPoints` | Distancia de seguimiento mantenida detrás del precio (puntos). | `5` |
| `TrailingTargetPercent` | Beneficio mínimo en % del saldo antes de que se active el seguimiento. | `0.2` |
| `SureProfitPoints` | Puntos extra que se deben asegurar antes de armar el trailing stop. | `30` |
| `EnableTrailPendingOrders` | Habilite el seguimiento de órdenes stop activas (entradas). | `false` |
| `TrailPendingOrderPoints` | Compensación en puntos para órdenes stop pendientes de seguimiento. | `10` |

## Notas de uso

1. Adjuntar la estrategia a un valor que ya esté gestionado por otro módulo de entrada o por órdenes manuales. Establece `Volume` según los contratos que necesitas cerrar al aplanar.
2. Proporcione una cartera que informe `Portfolio.CurrentValue`. La estrategia utiliza este valor para aproximar `AccountBalance` y `AccountEquity` de MetaTrader. Si falta el valor, la lógica del objetivo de equidad permanece inactiva.
3. La estrategia evalúa las mejores cotizaciones de oferta y demanda al verificar las condiciones de parada. Asegúrese de que los datos de nivel 1 estén disponibles para que los cálculos con reconocimiento de propagación sean significativos.
4. Las paradas ocultas y las salidas dinámicas se implementan como órdenes de mercado gestionadas por software. Las órdenes stop del corredor **no** se crean; el comportamiento refleja la naturaleza "oculta" del EA original.

## Diferencias con la versión MQL

- Los niveles de parada se aplican mediante la emisión de órdenes de mercado cuando se superan los umbrales. El EA original modificó el campo `OrderStopLoss`; StockSharp usa monitoreo activo en su lugar.
- Los cálculos de parada dinámica se basan en la última vela completa de un minuto entregada a través de `SubscribeCandles`. Si falta esta suscripción, las reglas dinámicas permanecen deshabilitadas.
- El seguimiento de órdenes pendientes ignora las órdenes de parada de protección creadas por otras estrategias porque `MasterExitPlanStrategy` no las registra.
- Los cheques de capital utilizan `Portfolio.CurrentValue` (respaldo a `Portfolio.BeginValue`) en lugar de `AccountBalance`/`AccountEquity`.

## Pruebas

La estrategia no contiene pruebas automatizadas. Utilice el probador de StockSharp con datos históricos para verificar el comportamiento de sus instrumentos antes de implementarlos en producción.
