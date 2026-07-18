# Estrategia CS2011
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Cs2011 es un sistema de reversión traducido del asesor experto original `cs2011.mq5`. Supervisa el histograma MACD y la línea de señal en cada vela terminada y busca patrones de agotamiento alrededor de la línea cero. El port de C# mantiene las reglas de sincronización principales mientras las expone a través del nivel alto StockSharp API.

## Lógica comercial
- **Reversiones de línea cero**: cuando el valor MACD de la barra anterior está por encima de cero mientras que la barra anterior estaba por debajo de cero, la estrategia emite una señal **corta**. La transición opuesta (de positivo a negativo) emite una señal **larga**. Esto imita las entradas contrarias implementadas en el script MQL5.
- **Extremos de la línea de señal**: la estrategia almacena las últimas tres lecturas de la línea de señal. Un máximo local mientras MACD permaneció negativo desencadena una entrada corta adicional; un mínimo local mientras MACD se mantuvo positivo desencadena una entrada larga. Esto reproduce las comprobaciones de patrones basadas en `Sig[0]`, `Sig[1]` y `Sig[2]` en la fuente EA.
- Las señales se evalúan solo en velas terminadas suministradas por `SubscribeCandles`, por lo que se ignoran los datos parciales.

## Manejo de posiciones
- La estrategia apunta a un **tamaño de posición absoluto fijo** (`TargetVolume`). Cuando llega una señal alcista, compra suficientes contratos para alcanzar `+TargetVolume`. Las señales bajistas hacen lo mismo para `-TargetVolume`. Se respeta la exposición existente en la misma dirección: no se realizan órdenes adicionales si ya se ha alcanzado el objetivo.
- `StartProtection` refleja la configuración original de obtención de beneficios y límite de pérdidas. Las distancias de puntos se convierten en valores `UnitTypes.Point` y se pasan al módulo de riesgo integrado. Dejar cualquiera de los valores en `0` desactiva la barrera correspondiente.
- Se utilizan ayudantes de alto nivel (`BuyMarket`, `SellMarket`) en lugar de la estructura de solicitud de bajo nivel de la versión MQL.

## Parámetros
| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `TargetVolume` | `1` lote | Tamaño de posición absoluto alcanzado después de una señal. Reemplaza la rutina de dimensionamiento de saldo `Risk` × de EA. |
| `TakeProfitPoints` | `2200` | Distancia en puntos de precio para la gestión del take-profit. `0` desactiva la toma de ganancias. |
| `StopLossPoints` | `0` | Distancia en puntos de precio para el stop-loss. `0` desactiva el stop-loss, coincidiendo con los valores predeterminados de EA. |
| `FastEmaPeriod` | `30` | Longitud rápida de EMA para el núcleo MACD. |
| `SlowEmaPeriod` | `500` | Longitud lenta de EMA para MACD. |
| `SignalPeriod` | `36` | Período de suavizado de la línea de señal. |
| `CandleType` | `1 hour` período de tiempo | Fuente de vela utilizada por `SubscribeCandles`. Ajústelo para que coincida con el período del gráfico utilizado en MetaTrader. |

Todos los parámetros se registran a través de `Param()` para que puedan optimizarse dentro de la interfaz de usuario del optimizador StockSharp.

## Diferencias con la versión MQL5
- La rutina de administración de dinero (`Money_M`) se basó en transacciones históricas y el saldo de la cuenta MetaTrader. Las estrategias StockSharp operan en carteras independientes del corredor, por lo tanto, el puerto expone un parámetro `TargetVolume` simple. Los usuarios pueden conectar su propia administración de dinero anulando el valor del parámetro o el método `ExecuteSignals`.
- Las solicitudes de órdenes se simplifican a órdenes del mercado único. La infraestructura StockSharp maneja la lógica de reintento, la desviación basada en diferenciales y las verificaciones del contexto comercial.
- La estrategia se ejecuta con suscripciones de velas en lugar del asistente personalizado `IsNewBar`. Esto garantiza que sólo se procesen velas completamente formadas.

## Notas de uso
1. Configure el valor, la cartera y el tipo de vela antes de lanzar la estrategia.
2. Ajuste `TargetVolume` para que coincida con el tamaño de lote nominal deseado.
3. Opcionalmente, ajuste `TakeProfitPoints` y `StopLossPoints` para reproducir los niveles de protección del EA original.
4. Inicie la estrategia: los mensajes de registro registran cada activador comercial junto con la exposición objetivo.

El código contiene comentarios en línea en inglés que describen cada paso del proceso de portabilidad.
