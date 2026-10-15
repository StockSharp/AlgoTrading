# Ejemplo de estrategia de calendario económico de detección
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de calendario económico de detección de muestra** replica el comportamiento del MetaTrader asesor experto original `SampleDetectEconomicCalendar.mq5`. La estrategia observa una lista proporcionada manualmente de eventos del calendario económico y, cuando se acerca un evento de alto impacto para la moneda configurada, coloca un par simétrico de órdenes de parada alrededor de los precios de oferta y demanda actuales. Las paradas protectoras, los niveles de obtención de beneficios opcionales y una salida final replican la lógica de gestión del dinero del código fuente.

A diferencia de la versión MQL, el puerto StockSharp no tiene acceso al servicio de calendario MetaTrader. En cambio, los eventos los proporciona el usuario a través del parámetro `CalendarDefinition`.

## como funciona
1. La estrategia se suscribe a los datos de Level1 para realizar un seguimiento de los precios de oferta y demanda.
2. Las líneas del calendario definidas en `CalendarDefinition` se analizan al inicio.
3. Para cada evento de alta importancia que coincida con `BaseCurrency`, la estrategia:
   - Espera hasta `LeadMinutes` antes del lanzamiento.
   - Calcula el volumen del pedido (ya sea fijo o basado en riesgo).
   - Coloca órdenes stop de compra/venta a `BuyDistancePoints` y `SellDistancePoints` a partir de los precios actuales.
4. Después del lanzamiento, los pedidos pendientes se cancelan una vez transcurrido `PostMinutes` o después del tiempo de espera total de `ExpiryMinutes`.
5. Cuando se activa un lado, se cancela la orden opuesta. La posición abierta se gestiona con stop loss, takeprofit opcional y distancias de trailing stop expresadas en puntos.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `TradeNews` | Permite realizar pedidos pendientes en torno a eventos de noticias programados. |
| `OrderVolume` | Volumen de orden fijo utilizado cuando la administración del dinero está deshabilitada. |
| `StopLossPoints` | Distancia de stop-loss en puntos del instrumento. Establezca en 0 para desactivar. |
| `TakeProfitPoints` | Distancia de toma de ganancias en puntos. Establezca en 0 para desactivar. |
| `TrailingStopPoints` | Distancia del trailing stop en puntos. Establezca en 0 para desactivar el seguimiento. |
| `ExpiryMinutes` | Vida útil máxima de las órdenes pendientes después del lanzamiento. |
| `UseMoneyManagement` | Si está habilitado, el volumen se calcula a partir del riesgo del saldo. |
| `RiskPercent` | Porcentaje del capital de la cartera arriesgado por operación (se utiliza sólo cuando la gestión del dinero está activa). |
| `BuyDistancePoints` | Compensación por encima de la solicitud de entrada de parada de compra. |
| `SellDistancePoints` | Compensación por debajo de la oferta para la entrada del stop de venta. |
| `LeadMinutes` | Minutos antes del lanzamiento cuando se envían las órdenes pendientes. |
| `PostMinutes` | Minutos después del lanzamiento antes de que se cancelen los pedidos desatendidos. |
| `BaseCurrency` | Código de moneda que debe aparecer en la entrada del calendario (predeterminado `USD`). |
| `CalendarDefinition` | Cadena multilínea que contiene eventos del calendario. |

## Formato de definición de calendario
Proporcione un evento por línea en el siguiente formato:

```
aaaa-MM-dd HH:mm;CUR;Alto;Título del evento
```

* `yyyy-MM-dd HH:mm`: marca de tiempo en UTC. Los segundos son opcionales. También se admiten varios formatos de fecha (`yyyy/MM/dd`, `dd.MM.yyyy`).
* `CUR`: código de moneda (por ejemplo, `USD`). Sólo se comercializan los eventos que coinciden con `BaseCurrency`.
* `High`: palabra clave importante (`High`, `Medium`, `Low` o `Nfp`). Solo `High` activa operaciones.
* `Event title`: texto libre para iniciar sesión.

Ejemplo:

```
2024-06-12 18:00;USD;Alto;Declaración del FOMC
2024-07-05 12:30;USD;Nfp;Nóminas no agrícolas
```

## Gestión de riesgos
* Cuando `UseMoneyManagement` está **desactivado**, los pedidos se realizan utilizando el parámetro `OrderVolume`.
* Cuando `UseMoneyManagement` está **activado**, la estrategia arriesga `RiskPercent` del valor de la cartera utilizando el `StopLossPoints` configurado. Se respetan los límites de volumen de intercambio (paso mínimo/máximo).
* La lógica de seguimiento refleja la EA original: se aplican las salidas de stop-loss y take-profit, y una vez que el precio se mueve favorablemente en `TrailingStopPoints`, el trailing stop protege la operación.

## Diferencias con el asesor experto MQL
* Los eventos del calendario económico deben proporcionarse manualmente en `CalendarDefinition`.
* Solo se procesa un par de instrumento/moneda por instancia de estrategia.
* El vencimiento de la orden pendiente se maneja internamente con temporizadores `PostMinutes`/`ExpiryMinutes` porque las órdenes de parada StockSharp no exponen indicadores de estilo MetaTrader `ORDER_TIME_SPECIFIED`.

## Notas de uso
1. Configura las líneas `CalendarDefinition` antes de iniciar la estrategia.
2. Habilite `TradeNews` y establezca los parámetros de riesgo deseados.
3. Asegúrese de que los datos de Nivel 1 estén disponibles para que las actualizaciones de ofertas y demandas lleguen antes de la ventana de noticias.
4. Revise los registros para confirmar que los pedidos se realicen y cancelen según lo previsto en cada evento.
