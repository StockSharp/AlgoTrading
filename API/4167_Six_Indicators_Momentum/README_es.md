# Estrategia de impulso de seis indicadores
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia reproduce el MetaTrader 4 asesor experto **6xIndics_M** utilizando la API de alto nivel de StockSharp. Mezcla seis entradas de impulso derivadas del Accelerator Oscillator (AC) y Awesome Oscillator (AO) de Bill Williams' y las alimenta a través de una matriz de decisión seleccionable. Un oscilador estocástico lento actúa como filtro final. Sólo hay una posición abierta a la vez; La gestión del dinero martingala, el stop-loss/take-profit y los trailing-stops opcionales emulan el comportamiento original.

## Cómo funciona la estrategia

1. **Suscripción de datos**: la estrategia se suscribe a la serie de velas configurada (`CandleType`, barras predeterminadas de 1 hora).
2. **Indicadores**
   - Awesome Oscillator calcula la diferencia entre las medias móviles simples de 5 y 34 períodos del precio medio.
   - Una media móvil simple de 5 períodos del AO produce los valores del Oscilador del Acelerador (AC).
   - Un oscilador Stochastic con parámetros 5/5/5 suministra la línea %K que se retrasa por una vela cerrada (desplazamiento MT4 = 1).
3. **Seis espacios para indicadores**: cada vela terminada llena los siguientes buffers:
   - Ranura 0: valor de CA desplazado en 1 vela (`AC[1]`).
   - Ranura 1: valor de CA desplazado en 10 velas (`AC[10]`).
   - Ranura 2: valor de CA desplazado en 20 velas (`AC[20]`).
   - Ranura 3: impulso de AO, es decir, `AO[0] - AO[shift]`, donde el cambio es configurable (`AoMomentumShift`).
   - Ranura 4: impulso de CA `AC[0] - AC[shift #1]` (`AcPrimaryShift`).
   - Ranura 5: impulso de CA `AC[0] - AC[shift #2]` (`AcSecondaryShift`).
4. **Matriz de señal seleccionable**: los parámetros `FirstSourceIndex`... `SixthSourceIndex` eligen qué ranura alimenta las seis comprobaciones booleanas originalmente denominadas `k`, `u`, `t`, `e`, `r`, `o`. Los mismos índices se reutilizan tanto para generar entradas como para cerrar operaciones cuando `CloseOnReverseSignal` está habilitado.
5. **Lógica de entrada**
   - **Compre** cuando las ranuras elegidas cumplan con: `A > 0`, `B > 0.0001 × Sensitivity`, `C > 0.0002 × Sensitivity`, `D < 0`, `E < 0.0001 × Sensitivity`, `F < 0.0002 × Sensitivity` y el %K estocástico anterior esté por debajo de 15.
   - **Vender** cuando `A < 0`, `B < 0.0001 × Sensitivity`, `C < 0.0002 × Sensitivity`, `D > 0`, `E > 0.0001 × Sensitivity`, `F > 0.0002 × Sensitivity` y el %K estocástico anterior estén por encima de 85.
6. **Gestión de posiciones**
   - Sólo se permite una posición. Cuando se abre una operación, la estrategia omite nuevas entradas, reflejando al experto en MT4.
   - Los niveles de stop-loss y take-profit se convierten de pips a precios absolutos utilizando el tamaño del tick del instrumento (exactamente como funciona `Point` en MT4).
   - El trailing stop opcional replica el comportamiento original: se activa una vez que el precio se mueve `TrailingStopPips` más allá de la entrada (y, cuando `RequireProfitForTrailing` es verdadero, un `LockProfitPips` adicional). El stop sigue al precio sólo en la dirección favorable.
   - `CloseOnReverseSignal` cierra una operación rentable si aparece la señal opuesta (puja por encima de la entrada para largos, pregunta abajo para cortos).
7. **Martingale tamaño**: cuando está habilitado, el siguiente volumen de orden es igual al volumen comercial anterior multiplicado por `(TakeProfitPips + StopLossPips) / TakeProfitPips` cada vez que una operación se cierra con pérdidas o punto de equilibrio. Las operaciones ganadoras restablecen el tamaño a la base `Volume`.

## Parámetros

| Parámetro | Descripción | Predeterminado |
|-----------|-------------|---------|
| `AllowBuy`, `AllowSell` | Habilite o deshabilite las entradas largas/cortas. | `true` |
| `CloseOnReverseSignal` | Cierre la posición actual cuando aparezca una señal opuesta mientras la operación genera ganancias. | `false` |
| `FirstSourceIndex` … `SixthSourceIndex` | Elija cuál de las seis ranuras de indicadores alimenta cada verificación lógica. Los valores fuera de 0–5 están bloqueados. | `1,2,3,4,3,4` |
| `AoMomentumShift` | Número de barras entre el valor AO actual y la comparación utilizada en la ranura 3. | `10` |
| `AcPrimaryShift`, `AcSecondaryShift` | Número de barras entre el valor de CA actual y las comparaciones para las ranuras 4 y 5. | `10` / `10` |
| `SensitivityMultiplier` | Multiplicador aplicado a los umbrales de 0,0001 y 0,0002 utilizados en las comprobaciones de ranuras. | `1.0` |
| `TakeProfitPips`, `StopLossPips` | Distancias de salida expresadas en puntos estilo MetaTrader (se reescalan según el tamaño del tick). | `300` / `300` |
| `UseTrailingStop` | Habilite la lógica de trailing stop. | `false` |
| `TrailingStopPips` | Distancia entre el precio y el trailing stop, en pips. | `300` |
| `RequireProfitForTrailing` | Cuando está habilitado, el trailing stop se activa solo después de que la operación gana un `LockProfitPips` adicional. | `false` |
| `LockProfitPips` | Beneficio adicional (en pips) que debe bloquearse antes de que el trailing stop comience a moverse. | `300` |
| `Volume` | Tamaño básico del pedido. | `0.1` |
| `UseMartingale` | Habilite el tamaño de la posición de martingala. | `false` |
| `CandleType` | Serie de velas utilizada para todos los cálculos. | `TimeSpan.FromHours(1)` |

## Notas y mejores prácticas

- Cada vela se procesa solo después de que finaliza, por lo que las señales imitan al experto MT4 que se ejecuta una vez por barra (`prevtime` guardia en el código original).
- La estrategia almacena solo el historial requerido (hasta 256 barras) para reproducir los cálculos de turnos de MT4 sin llamar a `GetValue()` en los indicadores, cumpliendo con las pautas del proyecto.
- Las salidas de seguimiento y stop/límite se simulan en máximos/mínimos de velas. En un entorno real, debe utilizar órdenes stop reales para garantizar la ejecución.
- El tamaño de Martingale utiliza los límites `VolumeStep`, `MinVolume` y `MaxVolume` del instrumento para mantener los volúmenes dentro de las reglas del corredor.
- Cuando `AllowBuy` o `AllowSell` está deshabilitado, las señales correspondientes se ignoran, pero la señal opuesta aún se puede usar para `CloseOnReverseSignal`.

## Diferencias versus el experto en MT4

- Los cálculos de indicadores utilizan el Awesome Oscillator integrado de StockSharp y las clases SMA; no se requiere gestión manual del buffer.
- Todas las operaciones se ejecutan mediante órdenes de mercado (`BuyMarket`/`SellMarket`) y salidas mediante `ClosePosition()`, mientras que la versión MT4 envió solicitudes explícitas `OrderSend`/`OrderClose`.
- El tamaño del lote respeta la granularidad del volumen de intercambio redondeando a `VolumeStep` y fijándolo a `[MinVolume, MaxVolume]`.
- Los ayudantes de gráficos (`DrawCandles`, `DrawIndicator`, `DrawOwnTrades`) se agregan para inspección visual cuando hay un gráfico disponible.
