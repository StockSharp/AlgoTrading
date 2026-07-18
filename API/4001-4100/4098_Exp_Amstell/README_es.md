# Estrategia Exp Amstell
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia Exp Amstell** es un sistema de comercio en red convertido del MetaTrader 4 asesor experto original `exp_Amstell.mq4`. Coloca continuamente órdenes de mercado de compra y venta cada vez que el precio se aleja una cantidad configurable de puntos del cumplimiento más reciente. Cada operación individual se gestiona de forma independiente: una vez que el mercado se mueve a la distancia de obtención de beneficios especificada, la estrategia envía una orden de compensación para capturar las ganancias de esa única capa.

A diferencia de los sistemas impulsados por el impulso, Exp Amstell permanece activo en todo momento. No espera confirmaciones de indicadores y, en cambio, acumula posiciones en ambos lados del libro a medida que el mercado oscila. Este comportamiento lo hace muy sensible a las distancias de los puntos elegidos y al tamaño de cada orden.

## Lógica de trading
- **Procesamiento basado en ticks.** La estrategia se suscribe a cotizaciones de nivel 1 y reacciona a cada cambio en la mejor oferta y la mejor demanda, al igual que la función `start()` en el código original MQL.
- **Stacks largos y cortos independientes.** Se permiten órdenes de compra cuando no hay operaciones largas abiertas o cuando el precio de venta ha bajado al menos la distancia de reingreso desde la última entrada larga. Las órdenes de venta utilizan la condición simétrica en el precio de oferta.
- **Obtención de beneficios por operación.** Cada capa abierta se rastrea por separado. Cuando la oferta (para largos) o la demanda (para cortos) avanza por los puntos de toma de ganancias configurados, la estrategia cierra solo esa capa con una orden de mercado. Otras capas permanecen intactas.
- **Emulación FIFO.** Las operaciones ejecutadas se registran en orden FIFO para reproducir la contabilidad basada en tickets que MetaTrader aplica a las posiciones cubiertas. Esto garantiza que los rellenos parciales reduzcan primero la capa pendiente más antigua.
- **Conocimiento neto de la cartera.** StockSharp mantiene posiciones netas. Si una nueva orden de compra compensa una capa corta abierta, la estrategia elimina esa posición corta de su pila sintética antes de registrar el resto como una nueva posición larga.

## Parámetros
| Nombre | Tipo | Predeterminado | Descripción |
| --- | --- | --- | --- |
| `TradeVolume` | `decimal` | `0.1` | Volumen de cada orden de mercado que abre una nueva capa de la grilla. |
| `TakeProfitPoints` | `int` | `30` | Distancia en MetaTrader puntos que debe cubrir el precio antes de cerrar una capa individual. |
| `ReentryDistancePoints` | `int` | `10` | Distancia mínima de puntos desde la última entrada antes de agregar otra orden en el mismo lado. |

La estrategia convierte automáticamente puntos en incrementos de precios reales utilizando el `PriceStep` del instrumento. Las cotizaciones de cinco y tres dígitos reciben el multiplicador específico de MetaTrader, de modo que `1 point` sea igual a `0.0001` (o `0.01` para símbolos de estilo JPY).

## Notas de implementación
- Los datos de nivel 1 son suficientes; no se requiere suscripción a velas. La estrategia declara esto anulando `GetWorkingSecurities()` y solicitando `(Security, DataType.Level1)`.
- `StartProtection()` se invoca durante `OnStarted` para garantizar que el corredor cierre cualquier posición sobrante si la estrategia se detiene inesperadamente.
- Todos los comentarios dentro del archivo C# permanecen en inglés, cumpliendo con las pautas del proyecto.
- Debido a que StockSharp utiliza posiciones netas, el puerto no puede mantener abiertas las compras y ventas opuestas simultáneamente. Cuando ambas partes comercian al mismo tiempo, la nueva orden aplanará la exposición existente antes de crear una nueva capa.

## Consejos de uso
1. **Calibre las distancias de los puntos.** Las distancias más pequeñas crean redes más densas que pueden realizar operaciones excesivas en mercados volátiles. Las distancias más grandes reducen la actividad pero aumentan la reducción por capa.
2. **Tamaño de los pedidos con prudencia.** Los sistemas de red acumulan exposición rápidamente. Pruebe volúmenes conservadores en Designer/Backtester antes de cambiar al comercio real.
3. **Considere controles de riesgo manuales.** El experto original no tiene un límite de pérdidas global. Combine la estrategia con protecciones a nivel de cartera para limitar el riesgo de cola.
4. **Monitorear la calidad de ejecución.** El algoritmo supone que las órdenes de mercado se ejecutan cerca de la mejor oferta/demanda. El deslizamiento afecta directamente las distancias de obtención de beneficios alcanzadas.

## Fuente
Convertido de `MQL/9027/exp_Amstell.mq4`.
