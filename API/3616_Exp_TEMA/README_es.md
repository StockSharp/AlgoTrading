# Estrategia Exp TEMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **Estrategia Exp TEMA** es una StockSharp versión del MetaTrader asesor experto `Exp_TEMA.mq5`. El sistema original escanea múltiples pares de divisas y monitorea la pendiente de la media móvil exponencial triple (TEMA). Cada vez que la pendiente cambia de signo, el experto entra en una nueva posición de seguimiento de tendencia o sale de la opuesta. Esta conversión de C# mantiene la misma lógica de indicador mientras se centra en un único valor asignado a la estrategia en StockSharp.

## Lógica de trading

La estrategia opera en velas terminadas producidas por el parámetro `CandleType` seleccionado. Se calcula un TEMA con la longitud configurable `TemaPeriod` en cada cierre de vela. Se comparan tres lecturas TEMA consecutivas para reproducir el esquema de detección de pendientes del experto MQL5:

1. Sea `tema[0]` el último valor de la vela, `tema[1]` el anterior y `tema[2]` el valor dos velas atrás.
2. La pendiente a corto plazo es `d1 = tema[1] - tema[2]`, mientras que la pendiente más antigua es `d2 = tema[2] - tema[3]`.
3. Una **entrada alcista** se activa cuando la pendiente sube (`d2 < 0` y `d1 > 0`). Cualquier posición corta se cierra primero y luego se coloca una orden larga de `Volume + |Position|` lotes.
4. Una **entrada bajista** se activa cuando la pendiente baja (`d2 > 0` y `d1 < 0`). Cualquier posición larga se aplana primero y luego se envía una orden corta de `Volume + |Position|` lotes.
5. Las salidas protectoras imitan las banderas de parada originales: si la pendiente actual se vuelve negativa, la posición larga se cierra, mientras que una pendiente positiva cierra cualquier posición corta.

Esto reproduce la misma temporización de señal que la fuente EA sin utilizar el acceso histórico al búfer, manteniéndose dentro del nivel alto StockSharp API.

## Parámetros

| Parámetro | Predeterminado | Descripción |
|-----------|---------|-------------|
| `TemaPeriod` | 15 | Longitud de la media móvil exponencial triple. |
| `TradeVolume` | 1 | Volumen base de pedidos. El tamaño ejecutado se convierte en `TradeVolume + |Posición|`al dar marcha atrás. |
| `StopLossPoints` | 1000 | Distancia de stop-loss expresada en pasos de precio. Pasado a `StartProtection` si es positivo. |
| `TakeProfitPoints` | 2000 | Distancia de obtención de beneficios expresada en incrementos de precio. Pasado a `StartProtection` si es positivo. |
| `CandleType` | velas de 15 minutos | Tipo de vela que alimenta el indicador. Elija un período de tiempo que coincida con el gráfico utilizado por el experto original. |

Todos los parámetros se crean con `StrategyParam<T>` para que puedan optimizarse dentro de Designer.

## Diferencias con el experto MQL5

- La versión MQL gestiona hasta doce símbolos simultáneamente. Las estrategias StockSharp están vinculadas a un `Security` específico, por lo tanto, este puerto comercializa el instrumento que se asigna cuando se lanza la estrategia. Ejecute varias instancias de estrategia si se requiere cobertura de múltiples símbolos.
- La gestión de órdenes se basa en `BuyMarket`/`SellMarket` y `StartProtection`, que asignan las órdenes de mercado originales, paradas y objetivos al nivel alto de StockSharp API.
- El acceso al indicador se realiza a través de `SubscribeCandles().Bind(...)`, evitando la copia manual del buffer y cumpliendo con las pautas del repositorio.

## Consejos de uso

1. Adjunte la estrategia a la seguridad deseada y establezca el `CandleType` que coincida con su marco de tiempo analítico.
2. Ajuste las distancias de parada y toma de ganancias en incrementos de precios de acuerdo con la volatilidad del instrumento.
3. Opcional: ejecute la optimización en `TemaPeriod`, `StopLossPoints` y `TakeProfitPoints` para replicar los barridos de parámetros realizados en MetaTrader.
4. Supervise el área del gráfico incluida para visualizar velas, la línea TEMA y las operaciones ejecutadas.
