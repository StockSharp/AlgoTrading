# Estrategia Fluctuante
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **Estrategia Fluctuante** es un port de StockSharp del expert advisor de MetaTrader "Fluctuate". Reproduce el comportamiento similar a una cuadrícula del original usando la API de alto nivel: una suscripción de velas impulsa todas las decisiones, las entradas al mercado se realizan con `BuyMarket` / `SellMarket`, y las órdenes de recuperación se colocan con stop orders. La exposición larga y corta se rastrean por separado para imitar la contabilidad de posición estilo hedging usada en MetaTrader, mientras que la posición real de StockSharp permanece neta.

## Idea central

1. Cada vez que cierra una nueva vela, la estrategia compara los últimos dos precios de cierre. Un cierre más alto abre una compra de mercado, un cierre más bajo abre una venta de mercado. Si ambos cierres son iguales la barra se ignora.
2. Cada posición ejecutada recibe un stop-loss y take-profit fijos (expresados en pips). La estrategia también registra el precio de ejecución exacto y el volumen neto añadido por el trade.
3. Después de una entrada, se activa una stop order **opuesta** a `StepPips` de distancia del último relleno (más un pequeño buffer de spread). Su volumen se deriva del trade anterior y el `LotCoefficient`, opcionalmente usando la exposición acumulada cuando `MultiplyLotCoefficient = true`.
4. Cuando la stop order se activa, cancela la orden pendiente anterior, actualiza las estadísticas de exposición interna e inmediatamente programa una nueva stop order de recuperación en la otra dirección. Esto reproduce el bucle de promediado/martingala presente en la implementación MQL.
5. La protección de trailing eleva (o baja) el stop una vez que el precio se mueve al menos `TrailingStopPips + TrailingStepPips` a favor de la posición. Esto emula el EA original que requería un buffer de beneficio adicional antes de apretar el stop.

## Flujo de trading

- **Detección de señales.** El feed de velas se suscribe vía `SubscribeCandles`. Solo se procesan velas terminadas. La estrategia se niega a operar fuera de la ventana de tiempo `[StartHour, EndHour)` o cuando se activa el guardián de capital.
- **Dimensionamiento inicial de posición.** Dependiendo de `PositionSizingMode`, el primer trade en una secuencia usa un lote fijo (`FixedVolume`) o un lote basado en riesgo (`RiskPercent`). En modo de riesgo, el riesgo permitido (porcentaje del capital actual) se divide por la pérdida monetaria que ocurriría si se activa el stop-loss. El paso de precio y el precio de paso se usan para convertir pips a moneda.
- **Contabilidad de exposición.** Acumuladores separados rastrean el volumen largo y corto, el precio promedio y el precio extremo alcanzado desde la entrada. Esto permite que la estrategia mantenga ambos lados "abiertos" internamente aunque StockSharp use netting.
- **Órdenes de recuperación.** Después de cada ejecución, el algoritmo calcula el volumen de la siguiente stop order:
  - Cuando `MultiplyLotCoefficient = false`, el nuevo volumen equivale a `LastVolume × LotCoefficient`.
  - Cuando `true`, la exposición absoluta total se multiplica por `LotCoefficient`.
  - El volumen se normaliza a las restricciones del intercambio (paso, volumen mínimo y máximo) y se rechaza cuando excedería `MaxTotalVolume` o el número de posiciones activas más órdenes excedería `MaxPositions`.
- **Objetivo de beneficio y guardián de capital.** La PnL no realizada agregada se calcula traduciendo diferencias de precio a moneda usando `PriceStep`/`StepPrice`. Si alcanza `ProfitTarget`, todas las posiciones se cierran y las órdenes pendientes se cancelan. El trading también se suspende cuando el capital cae por debajo de `MinEquityPercent` del saldo inicial.
- **Lógica de trailing.** Para posiciones largas, se registra el precio más alto visto desde la entrada. Una vez que supera el precio de entrada en `TrailingStopPips + TrailingStepPips`, se establece un trailing stop `TrailingStopPips` detrás del máximo. Las posiciones cortas aplican la regla simétrica con el precio más bajo. Las actualizaciones de trailing anulan el stop-loss fijo.

## Detalles de gestión del riesgo

- **Stop / take profit.** Ambos son opcionales (establecer el valor en pips a cero para deshabilitar). Se recalculan para la exposición larga o corta agregada cada vez que un nuevo trade añade volumen.
- **Máx. posiciones.** Cuenta el número de lados abiertos (largo + corto) más la stop order de recuperación activa. Cuando se alcanza el límite, la estrategia se niega a enviar nuevas stop orders.
- **Volumen total máximo.** Limita la suma del volumen abierto absoluto y el volumen de la orden de recuperación activa.
- **CloseAllAtStart.** Interruptor de seguridad opcional para aplanar el libro antes de que la estrategia empiece a operar.

## Parámetros

| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `CandleType` | Marco temporal principal usado para la detección de señales. | Marco temporal de 1 minuto |
| `StopLossPips` | Distancia entre el precio de entrada y el stop-loss (pips). `0` desactiva el stop. | 50 |
| `TakeProfitPips` | Distancia entre el precio de entrada y el take-profit (pips). `0` desactiva el take-profit. | 50 |
| `TrailingStopPips` | Distancia del trailing stop (pips). Requiere `TrailingStepPips > 0`. | 5 |
| `TrailingStepPips` | Beneficio adicional necesario antes de que avance el trailing stop (pips). | 5 |
| `StepPips` | Distancia entre el último relleno y el stop de recuperación opuesto (pips). | 30 |
| `LotCoefficient` | Multiplicador aplicado al volumen previo (o exposición total). | 2.0 |
| `MultiplyLotCoefficient` | Cuando `true`, el volumen de la nueva orden se calcula desde la exposición total en lugar del último trade. | `false` |
| `MaxPositions` | Número máximo de lados abiertos simultáneos más la orden pendiente activa. | 9 |
| `MaxTotalVolume` | Tope para la suma del volumen abierto y el volumen de la orden de recuperación. | 50 |
| `ProfitTarget` | Beneficio no realizado (en moneda de cuenta) que desencadena una salida completa. `0` desactiva el objetivo. | 50 |
| `MinEquityPercent` | Porcentaje mínimo de capital (vs. saldo inicial) requerido para seguir operando. Por debajo de este umbral solo se permiten salidas. | 30 |
| `CloseAllAtStart` | Cerrar todas las posiciones y cancelar órdenes cuando la estrategia inicia. | `false` |
| `StartHour` | Hora de inicio de la ventana de trading (inclusiva, tiempo del intercambio). | 10 |
| `EndHour` | Hora de fin de la ventana de trading (exclusiva, tiempo del intercambio). | 20 |
| `PositionSizingMode` | `FixedVolume` para lotes estáticos, `RiskPercent` para dimensionamiento por porcentaje de capital. | `FixedVolume` |
| `VolumeOrRisk` | Tamaño de lote fijo (cuando `FixedVolume`) o porcentaje de riesgo (cuando `RiskPercent`). | 1.0 |

## Notas de implementación

- Los precios de la stop order usan una aproximación de spread mínima (`PriceStep` cuando está disponible) porque MetaTrader requería que la orden estuviera fuera del nivel de congelación. Ajustar `StepPips` si el spread real es más amplio.
- La estrategia cancela cualquier orden de recuperación restante cada vez que se ejecuta un nuevo trade. Esto coincide con el EA original que eliminaba todas las órdenes pendientes después de una ejecución.
- Dado que los portafolios de StockSharp están neteados, la exposición con hedge se simula internamente. La posición real del bróker siempre reflejará la cantidad neta.
- El dimensionamiento de posición basado en riesgo requiere valores válidos de `PriceStep` y `StepPrice` de la descripción del instrumento.

## Consejos de uso

1. Seleccionar un tipo de vela apropiado que coincida con el marco temporal de prueba del EA original (típicamente M5 o M15) para mejor fidelidad.
2. Verificar los límites de volumen del intercambio: si el volumen de recuperación normalizado se vuelve cero, la estrategia dejará de agregar nuevas piernas.
3. Cuando `PositionSizingMode = RiskPercent`, asegurarse de que el portafolio contenga información de capital actualizada; de lo contrario la estrategia recurre al tamaño de lote fijo.
4. Combinar con `StrategyProtection` incorporado de StockSharp (habilitado vía `StartProtection()`) para agregar salvaguardias adicionales a nivel de cuenta si es necesario.
