# Estrategia LBS
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **Estrategia LBS** es una conversión directa del asesor experto de MetaTrader 5 "LBS (barabashkakvn's edition)". El sistema original monitorea rupturas de la vela anterior durante una ventana de trading configurable y coloca órdenes stop en ambos extremos. El puerto de StockSharp mantiene las mismas reglas de gestión de operaciones mientras usa la API de alto nivel (`SubscribeCandles`, `SubscribeLevel1`, `BuyStop`/`SellStop`) para mayor claridad y fiabilidad.

## Lógica de trading

1. La estrategia monitorea velas completadas del marco temporal seleccionado (`CandleType`).
2. Cuando el tiempo de cierre de la vela coincide con cualquiera de las horas de trading habilitadas (`Hour1`, `Hour2`, `Hour3`), el algoritmo calcula los niveles de ruptura:
   - El buy stop se coloca en el máximo más alto de la vela y el ask actual más un buffer de congelación.
   - El sell stop se coloca en el mínimo más bajo de la vela y el bid actual menos el mismo buffer.
   - El buffer reproduce el fallback `SYMBOL_TRADE_FREEZE_LEVEL` de MetaTrader (tres spreads, pero nunca menos de diez pips).
3. Si se abre una posición, la orden pendiente opuesta se cancela inmediatamente, igual que la rutina `DeleteAllPendingOrders` del experto MQL.
4. Los precios iniciales de stop-loss se adjuntan según `StopLossPips`. La lógica de trailing opcional (`TrailingStopPips` y `TrailingStepPips`) desplaza el stop una vez que el beneficio flotante supera los umbrales configurados.
5. Las órdenes solo se envían cuando la estrategia está en línea, no hay ninguna posición abierta y hay cotizaciones válidas de Level1 disponibles.

## Gestión del dinero

`MoneyMode` espeja el interruptor `Lot/Risk` del experto original:

- **FixedLot** – el parámetro `VolumeOrRisk` se interpreta como un volumen de trading absoluto.
- **RiskPercent** – la estrategia convierte `VolumeOrRisk` en una fracción del valor de la cartera. El importe del riesgo se divide por la distancia entre el precio de entrada y el stop de protección (en pasos de precio) para obtener el volumen de la orden. Cuando se usa este modo, el stop-loss debe estar habilitado; de lo contrario, la orden se omite.

Todos los volúmenes se normalizan a los límites de mínimo, máximo y paso del instrumento para evitar rechazos del broker.

## Parámetros

| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `StopLossPips` | 50 | Distancia al stop fijo en pips. Cero deshabilita tanto el stop inicial como el módulo de trailing. |
| `TrailingStopPips` | 5 | Distancia del trailing-stop en pips. Cero deshabilita el trailing. |
| `TrailingStepPips` | 15 | Beneficio adicional (en pips) requerido antes de mover el trailing stop. Debe ser positivo cuando el trailing está habilitado. |
| `MoneyMode` | `FixedLot` | Selecciona entre volumen fijo y dimensionamiento por porcentaje de riesgo. |
| `VolumeOrRisk` | 1.0 | Tamaño del lote en modo `FixedLot` o porcentaje de riesgo en modo `RiskPercent`. |
| `Hour1` | 10 | Primera hora de trading. Establezca en `0` para deshabilitar. |
| `Hour2` | 11 | Segunda hora de trading. Establezca en `0` para deshabilitar. |
| `Hour3` | 12 | Tercera hora de trading. Establezca en `0` para deshabilitar. |
| `CandleType` | Marco temporal de 1 hora | Serie de velas usada para detectar rupturas; ajuste para reflejar el marco temporal del gráfico de MetaTrader. |

## Notas

- Las comparaciones de horas usan el tiempo de cierre de la vela, que corresponde al momento en que `TimeCurrent()` de MetaTrader es igual al inicio de la siguiente barra.
- La aproximación del nivel de congelación/stop garantiza que las órdenes stop nunca estén más cerca de diez pips del bid/ask actual, evitando los errores más comunes de MetaTrader.
- Los trailing stops se actualizan en cada tick de Level1, asegurando un comportamiento cercano al manejador `OnTick` basado en ticks del experto original.
- El dimensionamiento basado en riesgo usa `Portfolio.CurrentValue` cuando está disponible y recurre a `Portfolio.BeginValue` en caso contrario.

## Consejos de uso

1. Adjunte la estrategia a un instrumento y elija el mismo marco temporal que se usó en MetaTrader.
2. Configure las horas de trading según la sesión que desea operar (establecerlas en `0` deshabilita ese slot).
3. Seleccione el modo `RiskPercent` si desea escalado automático; asegúrese de que `StopLossPips` sea positivo.
4. Para trading de lote fijo, mantenga `MoneyMode` en `FixedLot` y establezca `VolumeOrRisk` al tamaño deseado.
5. Inicie la estrategia. Colocará dos órdenes pendientes en la próxima hora configurada y mantendrá el stop de protección automáticamente.
