# Estrategia de probabilidad de patrón Gselector
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia **Gselector Pattern Probability** es una adaptación StockSharp del experto MetaTrader 4 "Gselector". Estudia los cambios de dirección de series de precios sintéticas construidas a partir de múltiples tamaños de paso, mantiene estadísticas de probabilidad para cada patrón observado y opera cuando la probabilidad de un movimiento de continuación es lo suficientemente alta. Las distancias de stop-loss y take-profit se simulan en un software para reflejar el comportamiento original del experto.

## Proceso de aprendizaje
1. **Escaleras sintéticas**: para cada múltiplo delta configurado, la estrategia construye una serie basada en pasos registrando el último precio de cierre cada vez que el mercado se mueve la distancia requerida.
2. **Codificación de patrón**: se crea una máscara de bits comparando cada par de valores vecinos dentro de la escalera. Los pasos ascendentes obtienen el bit `0`, los pasos descendentes obtienen el bit `1`, que reproduce la codificación `Ncomb` de la implementación MQL.
3. **Seguimiento de eventos**: cuando aparece un nuevo patrón, la estrategia inicia observadores para cada nivel de parada configurado. Un observador almacena el precio de origen y espera hasta que el precio suba o baje por el umbral.
4. **Actualización de probabilidad**: una vez que un observador completa, los movimientos hacia arriba aumentan la estadística de "crecimiento", los movimientos hacia abajo aumentan la estadística de "disminución". Un factor de olvido emula la lógica de decadencia (`forg`) del experto original.
5. **Persistencia en la memoria**: todas las estadísticas se mantienen en la memoria y se restablecen al iniciar la estrategia, coincidiendo con el comportamiento de la versión MQL cuando `ReadHistory` está deshabilitado.

## Lógica de trading
1. Las probabilidades de continuación se calculan para el patrón actual en cada escalera delta.
2. Una señal de compra requiere:
   - Probabilidad ≥ `ProbabilityThreshold`.
   - Observaciones ≥ `MinSamples`.
   - El tiempo de reutilización ha transcurrido desde la compra anterior.
   - Si existe una posición corta, la nueva probabilidad debe exceder la probabilidad de venta almacenada más el `ProbabilityBuffer`.
3. Una señal de venta refleja las reglas de compra con los roles de crecimiento/disminución intercambiados.
4. Las entradas utilizan `BuyMarket` / `SellMarket` para emular a `OrderSend`. Cuando la posición opuesta está abierta, la estrategia la cierra primero, reproduciendo el comportamiento de reversión del asesor experto.
5. Las salidas protectoras se manejan internamente: las paradas y tomas se expresan en unidades de precio derivadas del valor del punto y el nivel de parada.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `CandleType` | Tipo de datos de vela utilizado para la sesión de backtest/en vivo. | marco de tiempo de 1 minuto |
| `ProbabilityThreshold` | Probabilidad de continuación mínima requerida para abrir una operación. | 0,8 |
| `BaseDeltaPoints` | Distancia del punto base que define la primera escalera sintética. | 1 |
| `DeltaSteps` | Número de escaleras delta a evaluar. | 20 |
| `PatternLength` | Número de elementos en el historial de la escalera. | 10 |
| `StopLevels` | Recuento de niveles de parada/toma. | 1 |
| `StopDistancePoints` | Distancia base de parada/toma en puntos. | 25 |
| `ForgetFactor` | Decaimiento aplicado a los contadores de crecimiento/disminución después de cada observación. | 1.05 |
| `MinSamples` | Número mínimo de observaciones completadas. | 10 |
| `ProbabilityBuffer` | Se requiere probabilidad adicional para cerrar la posición opuesta. | 0,05 |
| `FixedVolume` | Volumen comercial base. | 1 lote |
| `UseReinvest` | Permite el ajuste del volumen proporcional al equilibrio. | cierto |
| `VolumeMode` | 0 – fijo, 1 – por ciento por 10k, 2 – escalera, 3 – lineal. | 1 |
| `PercentPer10k` | Porcentaje por 10 000 unidades en el modo 1. | 3 |
| `BaseDeposit` | Depósito base para modalidades 2 y 3. | 500 |
| `DepositStep` | Incremento de depósito para las modalidades 2 y 3. | 500 |
| `MaxVolume` | Límite de volumen máximo. | 10000 |
| `CooldownFactor` | Número de intervalos de velas utilizados como tiempo de reutilización de la reactivación. | 2 |

## Diferencias con el experto MQL
- Se eliminó la persistencia basada en archivos; Las estadísticas se reconstruyen desde cero cada vez que se inicia la estrategia.
- Las órdenes se simulan a través de `BuyMarket`/`SellMarket` y gestión de paradas de software en lugar de órdenes pendientes MT4.
- Los ayudantes para dimensionar posiciones se adaptaron a los datos de la cartera de StockSharp. Si los valores de las acciones no están disponibles, la estrategia vuelve al volumen fijo.
- Las entradas de trailing stop del código original se ignoran porque la versión MT4 nunca las aplicó.

## Notas de uso
- Adjunte la estrategia a un valor con un `PriceStep` válido. Si se desconoce el paso, la estrategia vuelve a caer a 0,0001.
- El proceso de aprendizaje necesita un número mínimo de activaciones de escalera; Espere una fase de calentamiento antes de que comiencen las operaciones.
- Aumentar `DeltaSteps` o `PatternLength` aumenta exponencialmente el uso de memoria porque el diccionario de patrones crece rápidamente.
- El umbral de probabilidad de incumplimiento (0,8) es muy estricto. Reduzca el valor para operaciones más frecuentes.
