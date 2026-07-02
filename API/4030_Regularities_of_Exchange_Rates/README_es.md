# Regularidades de la estrategia de tipos de cambio
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia StockSharp es una conversión fiel de C# del asesor experto MetaTrader 4 **Strategy_of_Regularities_of_Exchange_Rates.mq4**. El sistema fue diseñado como un sistema de ruptura diaria: coloca entre paréntesis el mercado con órdenes de parada cuando llega una hora específica y mantiene esas órdenes activas hasta la hora de cierre nocturno. Cualquier posición ocupada es supervisada tanto por un stop-loss del corredor como por un organismo de control de toma de ganancias intradiario para que las operaciones no se prolonguen más allá de la sesión de negociación definida.

A diferencia de los sistemas basados en indicadores, la lógica se centra únicamente en el tiempo y la distancia. Cuando el cronograma dice que el mercado debería estar listo, la estrategia mide una compensación fija en puntos de corredor (pips) de la oferta y demanda actual y coloca un par de órdenes stop simétricas. El código adapta automáticamente el cálculo de puntos a símbolos con comillas de 3 o 5 dígitos, coincidiendo con el comportamiento de la versión original MQL.

## Lógica de trading

1. **Hora de apertura**: una vez que una vela terminada informa `OpeningHour`, la estrategia cancela cualquier orden pendiente sobrante y envía un *stop de compra* por encima de la demanda actual y un *stop de venta* por debajo de la oferta actual. La distancia es `EntryOffsetPoints * point`, donde el valor `point` se deriva del instrumento `PriceStep` y se ajusta para cotizaciones fraccionarias.
2. **Órdenes de protección**: inmediatamente después del inicio, la estrategia habilita `StartProtection` con el `StopLossPoints` configurado. Por lo tanto, cualquier operación ejecutada recibe un stop-loss del lado del corredor idéntico al EA original.
3. **Supervisión de obtención de beneficios**: en cada vela completa, el algoritmo comprueba si el beneficio actual supera `TakeProfitPoints * point`. Si es así, cierra la posición al mercado. Esto refleja el bucle `OrderClose` original que salió cuando las ganancias alcanzaron el umbral.
4. **Hora de cierre**: cuando el reloj llega a `ClosingHour`, la estrategia cierra con fuerza cualquier posición abierta y cancela las órdenes stop, asegurando que el libro se mantenga plano para la siguiente sesión.
5. **Reinicio diario**: se envía un nuevo lote de órdenes pendientes solo una vez por día de negociación, lo que evita duplicados y al mismo tiempo respeta la intención original de una única configuración por sesión.

## Parámetros

| Parámetro | Predeterminado | Descripción |
|-----------|---------|-------------|
| `OpeningHour` | `9` | Hora (0–23) en la que se coloca el par de órdenes stop. |
| `ClosingHour` | `2` | Hora (0–23) en la que se eliminan las órdenes pendientes y se aplanan las operaciones abiertas. |
| `EntryOffsetPoints` | `20` | Distancia en puntos del corredor desde la oferta/demanda actual hasta las órdenes de parada. |
| `TakeProfitPoints` | `20` | Objetivo de beneficio en puntos del corredor que desencadena una salida del mercado. Establezca en `0` para deshabilitar la obtención de ganancias manual. |
| `StopLossPoints` | `500` | Distancia en puntos de intermediario para la parada de protección conectada a través de `StartProtection`. |
| `OrderVolume` | `0.1` | Volumen de cada orden stop. |
| `CandleType` | `30 minute time frame` | Serie de velas utilizadas para evaluar el cronograma. Cualquier período de tiempo ≤ 1 hora mantiene el comportamiento coherente con el script MQL. |

## Notas de conversión

- El asesor experto original trabajó en eventos de ticks y hizo referencia a `Hour()` directamente. En StockSharp la estrategia escucha las velas terminadas y usa su hora de apertura, lo que preserva la lógica de una vez por hora mientras se mantiene dentro de las pautas del repositorio sobre los estados de las velas.
- Las órdenes pendientes se normalizan con `Security.ShrinkPrice` para que los precios generados siempre coincidan con el tamaño del tick del instrumento.
- Detenga la administración de delegados a `StartProtection`, recreando el stop-loss generado por la plataforma que MetaTrader adjuntó durante `OrderSend`.
- El código rastrea la última fecha de negociación para evitar volver a enviar el mismo grupo varias veces dentro del mismo día, algo que podría suceder en períodos de tiempo inferiores a una hora en el EA original.
- Amplios comentarios en línea aclaran cada paso del flujo de trabajo para futuros mantenimiento o experimentación.
