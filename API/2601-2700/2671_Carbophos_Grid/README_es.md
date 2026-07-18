# Estrategia Carbophos Grid
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La Estrategia Carbophos Grid es una conversión directa del asesor experto de MetaTrader 5 "Carbophos". Mantiene continuamente una escalera simétrica de órdenes límite de compra y venta alrededor de los precios bid/ask actuales. La estrategia monitorea el beneficio flotante agregado de toda la cuadrícula y cierra toda la exposición una vez que se alcanza el objetivo de beneficio deseado o el drawdown máximo tolerado. Después de que la posición se aplana y no quedan órdenes activas, la escalera se reconstruye automáticamente.

## Lógica de Trading
1. Cuando la estrategia inicia y no hay órdenes activas ni posiciones abiertas, calcula el espaciado de la cuadrícula en unidades de precio basadas en el paso configurado en pips y la precisión de precio del instrumento. Se colocan cinco (configurable) órdenes sell limit por encima del mejor bid y el mismo número de órdenes buy limit por debajo del mejor ask.
2. Si alguna orden se llena, la posición resultante se monitorea tick a tick usando datos de Level1. El PnL flotante se calcula a partir de la diferencia entre el precio de salida actual (bid para posiciones largas, ask para posiciones cortas) y el precio de entrada ponderado por volumen.
3. Una vez que el beneficio flotante supera el objetivo configurado, o la pérdida flotante viola el umbral de protección, la estrategia envía una orden de mercado para cerrar la posición abierta y cancela todas las órdenes límite restantes. El indicador de estado se limpia para que la escalera sea reconstruida en la próxima actualización de precio.
4. Si todas las órdenes se llenan pero la posición neta vuelve a cero (por ejemplo, porque el mercado se revierte a través de la cuadrícula), la próxima actualización de Level1 activa una nueva colocación de escalera.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `ProfitTarget` | Beneficio flotante (dinero) que desencadena el cierre de toda la cuadrícula. |
| `MaxLoss` | Pérdida flotante (dinero) que fuerza una salida de emergencia. |
| `StepPips` | Distancia entre niveles consecutivos de la cuadrícula expresada en pips. Se convierte internamente a unidades de precio usando el tamaño del tick y la precisión decimal del símbolo. |
| `OrdersPerSide` | Número de órdenes límite colocadas por encima y por debajo del precio actual del mercado. |
| `OrderVolume` | Volumen para cada orden de cuadrícula. |

Todos los parámetros admiten rangos de optimización para simplificar la experimentación en el optimizador de StockSharp.

## Gestión de Riesgos y Protecciones
La estrategia usa el gancho incorporado `StartProtection()` y aplica niveles monetarios duros de stop/profit en el nivel de la estrategia. El cálculo del PnL flotante depende de los ajustes `PriceStep` y `StepPrice` del instrumento. Cuando se alcanza cualquiera de los umbrales, la estrategia cierra la posición con una orden de mercado y cancela cada orden límite activa antes de restablecer el indicador interno de cuadrícula.

## Notas de Conversión
- El asesor experto MQL5 original ajustaba los valores de pip para los símbolos Forex de tres y cinco decimales. El port de StockSharp replica este comportamiento multiplicando el `PriceStep` del exchange por 10 cuando el instrumento expone tres o cinco decimales.
- MetaTrader agrega el beneficio de posición, la comisión y el swap por número mágico. En StockSharp el PnL flotante se recalcula desde el precio de entrada ponderado y el precio bid/ask actual, por lo que no se requiere manejo explícito de comisiones.
- La colocación de órdenes, la cancelación y la gestión de posiciones se implementan a través del API `Strategy` de alto nivel (`BuyLimit`, `SellLimit`, `CancelActiveOrders`, `BuyMarket`, `SellMarket`) según lo requerido por las directrices del proyecto.
- La cuadrícula se actualiza exclusivamente desde las actualizaciones de Level1, replicando el comportamiento "OnTick" del código original sin introducir temporizadores personalizados o colecciones.

## Uso
1. Asigne el `Security` y `Portfolio` deseados a la instancia de la estrategia antes de iniciarla.
2. Opcionalmente ajuste los parámetros para coincidir con la volatilidad del instrumento objetivo y la tolerancia al riesgo.
3. Inicie la estrategia. Inmediatamente se suscribe a los datos de Level1, construye la primera cuadrícula una vez que tanto los precios bid como ask están disponibles, y continúa gestionando la exposición automáticamente.
4. Monitoree el registro de mensajes como "Profit target reached" o "Maximum loss reached" para saber cuándo se ha restablecido la cuadrícula.

Asegúrese de que el instrumento seleccionado proporcione actualizaciones de Level1 con los mejores precios bid y ask; de lo contrario la escalera no se construirá.
