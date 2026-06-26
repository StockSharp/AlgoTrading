# Estrategia de Risk Reward Ratio
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de Risk Reward Ratio** es un port de alto nivel en StockSharp del experto de MetaTrader "Risk Reward Ratio". La estrategia combina varios filtros de confirmación de impulso y tendencia con un módulo disciplinado de gestión del riesgo. Las entradas se generan a partir de la confluencia de osciladores estocásticos, un cruce de media móvil ponderada lineal (LWMA), un filtro RSI de 14 períodos y una verificación de tendencia con MACD. El control del riesgo se logra mediante un stop-loss basado en pips, un take-profit con ratio de recompensa automático, stops de seguimiento y lógica de break-even opcionales, y un interruptor de salida de emergencia que liquida inmediatamente la posición.

La conversión mantiene el espíritu original del experto de MetaTrader usando las suscripciones a velas e indicadores de StockSharp. Todo el procesamiento de indicadores ocurre en velas completadas y evita el acceso directo a los búferes de indicadores, preservando el paradigma de streaming del motor.

## Lógica de trading
1. **Confluencia estocástica**
   * Un estocástico *rápido* (5, 2, 2) proporciona la señal principal de impulso usando la línea %K.
   * Un estocástico *lento* (21, 10, 4) suministra el sesgo direccional a través de su línea %D suavizada.
   * Los setups largos requieren que el %K rápido esté por encima del %D lento, mientras que los cortos requieren lo contrario.
2. **Confirmación RSI**
   * Un RSI de 14 períodos debe estar por encima de 50 para operaciones largas y por debajo de 50 para cortas, asegurando que el mercado esté alineado con la dirección propuesta.
3. **Filtro de tendencia con LWMAs**
   * Dos medias móviles ponderadas linealmente (longitudes 6 y 85) deben confirmar la dirección: la LWMA rápida sobre la lenta para largos y por debajo para cortos.
4. **Calificador de tendencia MACD**
   * El histograma MACD (12, 26, 9) debe estar de acuerdo con la dirección de la señal. La línea principal debe liderar a la línea de señal permaneciendo en el lado apropiado del cero.
5. **Filtro de desviación de impulso**
   * Un indicador de Momentum de 14 períodos mide la distancia desde 100. Al menos una de las últimas tres lecturas de momentum debe superar el umbral configurable para demostrar que el precio está acelerando lo suficiente para justificar una operación.
6. **Límites de posición**
   * La exposición neta está limitada por `MaxPositions * TradeVolume`, de modo que la estrategia no pueda piramidizar más allá de la restricción original del EA.

Las órdenes se envían como ejecuciones de mercado usando `BuyMarket` y `SellMarket`. La estrategia ignora las velas no finalizadas y mantiene todo el estado dentro de los campos de clase para respetar la arquitectura orientada a eventos de StockSharp.

## Gestión del riesgo
* **Stop-loss en pips** – Cada entrada instala un stop de protección a `StopLossPips * PriceStep` desde el precio de llenado.
* **Take-profit con ratio de recompensa** – La distancia del take-profit es igual a la distancia del stop multiplicada por `RewardRatio` para mantener una relación fija de recompensa-riesgo.
* **Stop de seguimiento** – Cuando está habilitado, el stop se mueve detrás del precio por `TrailingStopPips` una vez que el mercado avanza al menos esa distancia desde la entrada.
* **Ajuste de break-even** – Después de `BreakEvenTriggerPips` de movimiento favorable, el stop se empuja a la entrada más un colchón adicional de `BreakEvenOffsetPips` (o menos para cortos), asegurando ganancias.
* **Interruptor de salida** – Establecer `ExitSwitch` en `true` aplana la posición actual en la siguiente barra completada y deshabilita el procesamiento hasta que el indicador se desactive.

## Parámetros
| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `TradeVolume` | `0.1` | Volumen de cada orden de mercado. |
| `CandleType` | Marco temporal de `15m` | Serie de velas principal. |
| `FastMaPeriod` | `6` | Período de la LWMA rápida. |
| `SlowMaPeriod` | `85` | Período de la LWMA lenta. |
| `MomentumThreshold` | `0.3` | Distancia absoluta mínima del indicador de Momentum desde 100 necesaria para permitir entradas. |
| `RewardRatio` | `2` | Múltiplo de take-profit relativo al stop-loss. |
| `StopLossPips` | `20` | Distancia del stop-loss en pips (múltiplos de PriceStep). |
| `MaxPositions` | `10` | Número máximo de unidades de volumen (`TradeVolume`) permitidas simultáneamente. |
| `EnableTrailing` | `true` | Habilita actualizaciones de stop de seguimiento basadas en pips. |
| `TrailingStopPips` | `40` | Distancia del stop de seguimiento en pips. |
| `EnableBreakEven` | `true` | Activa la gestión de stop de break-even. |
| `BreakEvenTriggerPips` | `30` | Ganancia (en pips) requerida antes de mover el stop al break-even. |
| `BreakEvenOffsetPips` | `30` | Desplazamiento adicional en pips cuando el stop se traslada al break-even. |
| `ExitSwitch` | `false` | Fuerza a la estrategia a liquidar toda la exposición en la siguiente vela completada. |

## Flujo de trabajo
1. Configure el instrumento y la serie de velas deseados, luego establezca los parámetros de riesgo.
2. Inicie la estrategia. Se suscribe a las velas, vincula indicadores y comienza a procesar barras completadas.
3. Cuando se alinean las condiciones de entrada, el motor envía una orden de mercado y almacena los niveles de stop/objetivo.
4. En cada vela completada, el bloque de riesgo evalúa las reglas de seguimiento, break-even y salida de emergencia.
5. Las salidas se activan al alcanzar niveles de stop/take-profit, actualizaciones de seguimiento, ajustes de break-even o el interruptor de emergencia.

## Notas
* La conversión aprovecha el binding de indicadores de StockSharp en lugar del acceso manual a búferes, asegurando que cada indicador reciba datos sincronizados.
* Todos los cálculos se basan en el `PriceStep` del instrumento. Si el paso es cero o falta, las distancias de riesgo permanecen deshabilitadas para evitar niveles de precio no válidos.
* La estrategia no modifica órdenes pendientes; simplemente envía órdenes de mercado para abrir/cerrar posiciones, replicando la forma en que el EA original aplanaba la exposición cuando se alcanzaban los umbrales.
