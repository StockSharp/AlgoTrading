# TDS Global 4
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
TDSGlobal 4 es una conversión del asesor experto MetaTrader 4 "TDSGlobal 4". El sistema original aplica el triple de Alexander Elder.
método de pantalla combinando la pendiente de un histograma diario MACD (OsMA) con un filtro Williams %R. Las órdenes sólo se implementan cuando
El impulso diario se alinea con los extremos del oscilador, después de lo cual la estrategia sitúa entre paréntesis el rango del día anterior con las reservas pendientes.
p pedidos. El puerto StockSharp mantiene la misma lógica de ruptura y agrega una programación precisa para que diferentes símbolos FX se activen al mismo tiempo.
minutos rojos y gestiona la exposición abierta con paradas dinámicas opcionales más objetivos de obtención de beneficios configurables.

## Lógica estratégica
### Filtros de plazos más altos
* **MACD pendiente**: compara los dos últimos valores principales MACD diarios completados (EMA rápida 12, EMA lenta 26, señal EMA 9). El sesgo es b
Ullish cuando el valor más reciente excede al anterior, bajista cuando es menor y neutral cuando son iguales.
* **Williams %R** – evalúa el Williams %R diario (período 24). Se permiten configuraciones largas sólo cuando la lectura está por encima del límite superior.
umbral (predeterminado −25, que significa fuerza de sobrecompra), mientras que las configuraciones cortas requieren que el valor se mantenga por debajo del umbral inferior (de
fallo −75).

### Colocación de ruptura
* **Niveles de precios**: en cada vela diaria terminada, la estrategia registra el máximo y el mínimo del día anterior. Nuevas órdenes de parada son posibles
cionó un pip más allá de esos extremos (configurable a través de *EntryBufferPips*), imitando el desplazamiento de ±1 punto del EA original.
* **Guardia de distancia**: antes de enviar una orden de parada, el código impone una brecha mínima entre la mejor cotización actual y la pr de entrada.
hielo (predeterminado 16 pips, que coincide con la verificación de 16 *Puntos* del EA). Esto evita que las órdenes pendientes se eliminen demasiado cerca del mercado.
t cuando la volatilidad es baja.
* **Gating direccional**: las paradas de compra se crean solo cuando la pendiente MACD es positiva y el Williams %R confirma el sesgo alcista. S
Las paradas de venta requieren una pendiente negativa y un Williams %R que indica presión bajista.

### Mantenimiento de orden pendiente
* **Restablecimiento diario**: cuando se cierra una nueva vela diaria, todas las órdenes pendientes restantes se cancelan para que comience la siguiente sesión de negociación.
Es un borrón y cuenta nueva. Si los filtros no permiten una operación, no se realizan pedidos para ese día.
* **Una operación por día**: una vez que se han evaluado las órdenes para un día determinado (ya sea que se hayan realizado o se hayan omitido), la estrategia espera
s para el próximo cierre diario antes de reevaluar. Las órdenes stop ejecutadas cancelan automáticamente el lado opuesto para evitar pérdidas simultáneas.
ng/exposición corta.

### Gestión de riesgos
* **Paradas de protección**: las posiciones largas heredan una salida protectora justo por debajo del mínimo del día anterior, mientras que las posiciones cortas utilizan el
máximo anterior. Estos niveles se monitorean en el flujo de activación de un minuto.
* **Obtener ganancias**: objetivos fijos opcionales expresados en pips en relación con el precio de ejecución real. Establezca *TakeProfitPips* en `0` para dis
capaz del objetivo, reflejando la configuración MT4.
* **Trailing stop**: si *TrailingStopPips* es mayor que cero, la estrategia lee las mejores cotizaciones de oferta/demanda de los datos y el seguimiento del nivel 1.
Es la parada una vez que el precio se ha movido a favor de la operación. Cuando se supera el nivel final, la posición se cierra al precio del mercado.

### Programación
* **Ventanas de minutos**: para evitar envíos simultáneos entre diferentes pares de divisas, EA utilizó ventanas de minutos específicas del símbolo.
ws. El puerto replica este comportamiento: USDCHF usa los minutos 0/8/16/24/32/40/48, GBPUSD 2/10/18/26/34/42/50, USDJPY 4/12/20/28/36/44
/52 y EURUSD 6/14/22/30/38/46/54. Cualquier otro instrumento retrocede a la hora completa (0–59).
* **Transmisión de activación**: una suscripción de vela de un minuto impulsa tanto la programación de las órdenes diarias como la parada/toma intradía.
cheques de ganancias. La evaluación de la señal real solo ocurre una vez por día de negociación durante el primer minuto elegible.

## Parámetros
| Parámetro | Descripción | Predeterminado |
|-----------|-------------|---------|
| `Volume` | Volumen de pedidos para entradas de paradas. | `1` |
| `MacdFastPeriod` / `MacdSlowPeriod` / `MacdSignalPeriod` | MACD configuración utilizada para medir la pendiente diaria. | `12 / 26 / 9` |
| `WilliamsPeriod` | Búsqueda retrospectiva del filtro %R Williams. | `24` |
| `WilliamsBuyLevel` | Se requiere un umbral superior (normalmente −25) antes de habilitar las órdenes largas. | `-25` |
| `WilliamsSellLevel` | Se requiere un umbral más bajo (normalmente −75) antes de habilitar las órdenes cortas. | `-75` |
| `TakeProfitPips` | Distancia de toma de ganancias en pips; `0` desactiva el objetivo. | `999` |
| `TrailingStopPips` | Distancia del trailing stop en pips; `0` desactiva el seguimiento. | `10` |
| `EntryBufferPips` | Compensación agregada más allá del máximo/mínimo del día anterior antes de colocar una orden de parada. | `1` |
| `MinDistancePips` | Distancia mínima de pips desde la cotización actual hasta la orden pendiente. | `16` |
| `DailyCandleType` | Periodo que alimenta los filtros MACD y Williams %R. | `1 day` velas |
| `TriggerCandleType` | Se utiliza un plazo más bajo para programar y monitorear pedidos. | `1 minute` velas |

## Notas adicionales
* La implementación de C# se basa completamente en ayudantes de alto nivel StockSharp (`SubscribeCandles`, `BuyStop`, `SellStop`, bindi de nivel 1).
ng) para que pueda ser reutilizado dentro de la plataforma sin necesidad de realizar un pedido manual de plomería.
* Se requieren datos de nivel 1 para la operación de trailing stop porque el algoritmo utiliza las mejores cotizaciones de oferta y demanda para mover y activar el
parada virtual.
* El paquete no incluye una traducción de Python; Solo se proporcionan la estrategia C# y la documentación multilingüe.
