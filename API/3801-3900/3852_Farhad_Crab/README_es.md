# Estrategia del cangrejo Farhad
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia del Cangrejo Farhad** es una StockSharp versión de alto nivel del MetaTrader asesor experto `FarhadCrab1.mq4`. El EA original es un sistema de especulación rápido diseñado para el período M1 en GBP/JPY, GBP/USD y EUR/USD. Esta conversión recrea la lógica comercial en C# al combinar filtros de promedio móvil intradía con una red de seguridad de tendencias diarias y una gestión de salida automatizada.

La estrategia analiza el período de tiempo actual a través de un EMA de 9 períodos calculado sobre el precio típico y un SMA de 9 períodos calculado sobre la apertura de la vela. Al mismo tiempo, realiza un seguimiento de una media móvil suavizada (SMMA) de 55 períodos construida a partir de velas diarias. Siempre que los filtros a corto plazo muestran suficiente impulso alcista mientras no hay ninguna posición abierta, se activa una operación larga. Por el contrario, cuando el máximo intradiario permanece por debajo del SMA de aperturas, se abre una operación corta. La SMMA diaria actúa como una capa protectora: cruzar el precio desde abajo obliga a todas las operaciones largas a salir, y cruzar desde arriba cierra las posiciones cortas.

La gestión de salida reproduce el comportamiento del EA original con niveles de obtención de beneficios configurables en pips y paradas finales independientes para posiciones largas y cortas. La lógica de seguimiento sigue la implementación de MetaTrader moviendo el stop solo después de que el mercado avanza la distancia configurada. La estrategia cierra posiciones mediante órdenes de mercado en lugar de órdenes stop pendientes, lo que la hace compatible con el flujo de eventos de alto nivel API.

## Características clave

- **Conjunto de indicadores idéntico al EA**: EMA de 9 períodos en el precio típico, SMA de 9 períodos en las aperturas y una SMMA diaria de 55 períodos para la dirección de la tendencia.
- **Manejo de datos de múltiples períodos de tiempo**: se suscribe al período de negociación y a las velas diarias simultáneamente, lo que permite a StockSharp calcular los indicadores requeridos sin almacenamiento en búfer manual.
- **Salidas configurables**: distancias de toma de ganancias simétricas (largas/cortas) y topes dinámicos expresados en pips, al igual que las entradas externas originales.
- **Interruptor de seguridad diario**: replica la regla de EA que cierra posiciones largas cuando el SMMA diario se mueve por encima del cierre diario y posiciones cortas cuando se mueve por debajo.
- **Protección incorporada**: llama a `StartProtection()` una vez al inicio para proteger las posiciones de acuerdo con las mejores prácticas del marco.

## Parámetros

| Parámetro | Descripción | Predeterminado |
|-----------|-------------|---------|
| `OrderVolume` | Volumen comercial aplicado a nuevas órdenes de mercado. | `0.1` |
| `LongTakeProfitPips` | Distancia de obtención de beneficios para posiciones largas, medida en pips. | `10` |
| `ShortTakeProfitPips` | Distancia de obtención de beneficios para posiciones cortas, medida en pips. | `10` |
| `LongTrailingStopPips` | Distancia de trailing stop para operaciones largas. El seguimiento se desactiva cuando se establece en cero. | `8` |
| `ShortTrailingStopPips` | Distancia de trailing stop para operaciones cortas. El seguimiento se desactiva cuando se establece en cero. | `8` |
| `DailyMaPeriod` | Longitud de la media móvil diaria suavizada utilizada para las salidas de protección. | `55` |
| `CandleType` | Periodo de tiempo principal que impulsa los cálculos de la estrategia. El valor predeterminado es velas de 1 minuto. | `1m` |

Todos los parámetros se exponen a través de `StrategyParam<T>` y se marcan como optimizables cuando tiene sentido, para que puedan ajustarse mediante el optimizador StockSharp.

## Reglas de trading

1. **Entradas largas**: cuando el mínimo de la vela actual se mantiene por encima del EMA de 9 períodos del precio típico y no hay ninguna posición activa, abra una operación larga.
2. **Entradas cortas**: cuando el máximo de la vela actual permanece por debajo del período 9 SMA del precio de apertura y no hay ninguna posición activa, abra una operación corta.
3. **Salida protectora diaria (larga)**: cierre cualquier posición larga si la SMMA diaria se mueve por encima del cierre diario mientras anteriormente estaba por debajo del cierre anterior.
4. **Salida protectora diaria (corta)**: cierre cualquier posición corta si la SMMA diaria se mueve por debajo del cierre diario mientras anteriormente estaba por encima del cierre anterior.
5. **Take-profit**: cierre la posición una vez que se alcance el objetivo de pip configurado.
6. **Parada de seguimiento**: después de que una posición gana la distancia de seguimiento, bloquee las ganancias monitoreando la distancia de retroceso y salga cuando el precio retroceda en esa cantidad.

## Notas de implementación

- El código se basa exclusivamente en llamadas `SubscribeCandles().Bind(...)` de alto nivel, lo que elimina cualquier buffer de indicador manual y se mantiene dentro de las pautas del proyecto.
- Los pips se calculan a partir del `PriceStep` del instrumento con el ajuste habitual de estilo MetaTrader para cotizaciones de 3 y 5 dígitos. Esto mantiene el comportamiento coherente con los parámetros basados ​​en puntos de EA.
- La gestión de stop-loss y take-profit se realiza internamente cerrando posiciones cuando se cumplen las condiciones, en lugar de registrar órdenes límite/stop. Este enfoque coincide con las salidas instantáneas que se encuentran en el script original y al mismo tiempo sigue siendo compatible con la ejecución de órdenes asincrónicas en StockSharp.
- La estrategia restablece su estado dentro de `OnReseted`, lo que garantiza que las ejecuciones de optimización y los lanzamientos repetidos comiencen desde cero.

## Consejos de uso

- El EA original se diseñó para pares GBP y EUR altamente volátiles en el marco temporal M1. Se pueden esperar resultados similares al aplicar el mismo marco temporal e instrumentos, pero los parámetros están expuestos para adaptarse a diferentes perfiles de volatilidad.
- Debido a que el sistema mantiene solo una posición a la vez, es adecuado para realizar pruebas retrospectivas sencillas y ejecución en vivo sin una compleja pirámide de posiciones.
- Los trailingstops se vuelven más efectivos en instrumentos con tendencias suaves. En los mercados de rango, considere reducir la distancia de seguimiento o confiar únicamente en salidas de toma de ganancias.
- La salida diaria de SMMA sirve como principal control de riesgos. Para configuraciones orientadas al swing, puede aumentar `DailyMaPeriod` para que el filtro a largo plazo sea menos reactivo.
