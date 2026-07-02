# Experto en el Grial MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Grail Expert MA es una StockSharp versión del MetaTrader 4 asesores expertos `_GrailExpertMAV1_0`. El sistema busca nuevas rupturas más allá del reciente canal máximo/mínimo y espera un retroceso antes de unirse al movimiento. Un promedio móvil exponencial del precio típico proporciona el sesgo direccional: las operaciones solo se permiten cuando el EMA ha ganado o perdido una cantidad configurable de pips en las dos últimas velas completadas. La gestión de riesgos refleja al experto original con distancias de stop-loss y take-profit basadas en pips e ignora las nuevas entradas mientras una posición está activa.

## Lógica estratégica
### EMA filtro de tendencia de pendiente
* Un EMA calculado sobre el precio típico ((Máximo + Mínimo + Cierre)/3) se evalúa al cierre de cada barra.
* La diferencia entre los dos últimos valores EMA debe exceder el umbral `EMA Slope (pips)` (convertido en precio utilizando el tamaño de pip del símbolo).
* Una pendiente positiva autoriza retrocesos largos, una pendiente negativa autoriza retrocesos cortos y las pendientes planas bloquean el comercio.

### Seguimiento del rango de ruptura
* La estrategia mantiene el máximo más alto y el mínimo más bajo en las últimas `Range Period` barras completadas.
* Estos niveles forman un canal cuya altura se utiliza para rechazar movimientos superficiales que no crean suficiente distancia para la lógica de retroceso.

### Preparación de entrada
* Cuando la barra actual imprime un nuevo máximo por encima del rango almacenado, se calcula un precio potencial de entrada larga en `High - Breakout Buffer - Take Profit` pips.
* Cuando la barra actual imprime un nuevo mínimo por debajo del rango almacenado, se calcula un precio potencial de entrada corta en `Low + Breakout Buffer + Take Profit` pips.
* El EA original requería que la distancia entre el nuevo extremo y el lado opuesto del rango fuera al menos `2 * Breakout Buffer + Take Profit`. El puerto mantiene la misma validación y descarta la entrada si el diferencial es demasiado pequeño.

### Activador de entrada
* Los precios preparados permanecen activos durante el resto de la barra. Se ejecuta una posición larga cuando el mínimo intrabar alcanza o cae por debajo del precio de entrada largo almacenado mientras la pendiente EMA es positiva.
* Se ejecuta una venta en corto cuando el máximo intrabar alcanza o excede el precio de entrada en corto almacenado mientras la pendiente EMA es negativa.
* Sólo se puede abrir una operación a la vez; el puerto borra ambos precios de entrada pendientes tan pronto como se envía una orden que coincida con el comportamiento MQL.

### Gestión de salidas
* Las posiciones largas utilizan una parada en `Entry - Stop Loss` pips y un objetivo de ganancias en `Entry + Take Profit` pips (el cero desactiva el nivel respectivo).
* Las posiciones cortas reflejan los cálculos (deténgase arriba, apunte abajo).
* Las salidas se activan cuando los extremos de las velas tocan los niveles de protección, coincidiendo con la aproximación basada en barras de la lógica de tick original.

### Salvaguardias adicionales
* Las entradas pendientes se borran siempre que quedan fuera del rango actualizado cuando se cierra una nueva vela.
* Todas las distancias de pips se adaptan automáticamente al tamaño de tick del instrumento (los símbolos FX de cinco dígitos asignan un pip a 10 ticks).
* Si el EMA aún no se ha formado o el búfer de rango carece de suficiente historial, la estrategia permanece inactiva hasta que haya suficientes datos disponibles.

## Parámetros
* **Volumen de órdenes**: volumen comercial en lotes/contratos para órdenes de mercado.
* **Take Profit (pips)** – distancia al objetivo de beneficio fijo; configúrelo en `0` para deshabilitarlo.
* **Stop Loss (pips)** – distancia hasta el tope de protección; configúrelo en `0` para deshabilitarlo.
* **Período de rango**: número de velas completadas utilizadas para medir el canal de ruptura.
* **EMA Período** – duración de la media móvil exponencial aplicada al precio típico.
* **EMA Pendiente (pips)**: avance/disminución mínima de pips entre valores EMA consecutivos necesarios para habilitar las entradas.
* **Búfer de ruptura (pips)**: distancia adicional desde el extremo nuevo antes de activar las entradas de retroceso.
* **Tipo de vela**: período de tiempo solicitado desde la fuente de datos (predeterminado: velas de 1 hora).

## Notas de implementación
* La estrategia utiliza actualizaciones de velas sin procesar (incluidos estados parciales) para emular el monitoreo de máximos/mínimos intrabar original.
* Los valores EMA se procesan solo en velas terminadas para replicar las llamadas MQL `iMA` con desplazamientos de una y dos barras.
* Los rangos históricos se rastrean con colas limitadas en lugar de búsquedas de indicadores para evitar costosas reexploraciones y al mismo tiempo mantener la lógica fiel a la fuente.
* No se proporciona ninguna versión de Python; el paquete API contiene solo la implementación de C#.
