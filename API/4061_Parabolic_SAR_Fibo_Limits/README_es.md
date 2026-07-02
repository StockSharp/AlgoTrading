# Parabolic SAR Límites de Fibonacci
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Parabolic SAR Fibo Limits es un StockSharp puerto del MetaTrader 4 asesor experto `FT_0tk80i9uw4ep_Parabolic`. El robot original combina una pila dual Parabolic SAR con Fibonacci niveles de retroceso para organizar entradas de límite en zonas clave de retroceso. La estrategia C# conserva la colocación de órdenes por etapas, las protecciones integradas de equilibrio y seguimiento, y el filtro de sesión comercial opcional para que el comportamiento coincida con la fuente EA cuando se adjunta a un gráfico con velas terminadas.

## Lógica estratégica
### Preparación de señal
* **Alineación dual Parabolic SAR**: dos indicadores Parabolic SAR se calculan en el mismo período de tiempo. El SAR rápido se utiliza como alerta temprana, mientras que el {PH005}} lento confirma el cambio de estado. Cuando el SAR rápido salta por encima del precio mientras que el SAR lento permanece por debajo de él, la estrategia arma una posible configuración larga. Cuando el SAR rápido cae por debajo del precio mientras que el SAR lento se mantiene por encima de él, se activa una posible configuración corta. Las configuraciones se borran tan pronto como el lento SAR cruza el precio en la dirección respectiva.
* **Detección de swing**: la estrategia consulta el máximo más alto y el mínimo más bajo en la ventana configurable `Bar Search` para replicar el ayudante `MaximumMinimum` del EA. La vela terminada anterior proporciona el extremo opuesto (`High[1]` o `Low[1]`) que ancla los cálculos Fibonacci.

### Realización y gestión de pedidos.
* **Fibonacci órdenes pendientes**: una vez que ambos SAR se ubican en el mismo lado del precio y se arma una configuración, la estrategia envía una orden límite al 50% del nivel Fibonacci (`Entry Fibonacci %`) de la oscilación detectada. El stop de protección se compensa desde el extremo del swing por el número configurado de puntos, y la toma de ganancias se coloca en la proyección Fibonacci extendida (`Target Fibonacci %`). Las órdenes solo se aceptan cuando el precio actual, la parada planificada y el objetivo están al menos cinco pasos de precio entre sí, reflejando el filtro de seguridad `Point*5` de EA.
* **Limpieza automática de órdenes**: cada vez que el SAR rápido vuelve a cruzar el precio, la orden límite pendiente para esa dirección se cancela para evitar entrar en la fase de mercado equivocada. Completar una orden limitada cancela automáticamente la orden pendiente opuesta.

### Gestión de riesgos
* **Parada y objetivo iniciales**: los parámetros de parada de pérdidas y toma de ganancias de la orden pendiente de EA se emulan aplicando los niveles de parada y objetivo calculados tan pronto como se completa la orden límite.
* **Desplazamiento del punto de equilibrio**: si `Break Even (points)` es mayor que cero, el stop se mueve al precio de entrada más un paso de precio (o menos un paso para posiciones cortas) una vez que la operación gana el número especificado de puntos, reproduciendo la rutina BBU original.
* **Trailing stop**: cuando `Trailing Stop (points)` está habilitado, el stop sigue el precio en la distancia elegida. La parada solo se actualiza cuando la nueva parada mejora la anterior en al menos `Trailing Step (points)`, coincidiendo con el comportamiento `TrailingShag` de EA.
* **Activadores de salida manuales**: si el precio toca el stop calculado o los niveles objetivo en una vela terminada, la posición se cierra con una orden de mercado para simular la ejecución automática de la orden de MT4.

### Filtro de tiempo
* **Control de sesión opcional**: habilitar `Use Time Filter` restringe las nuevas entradas a la ventana inclusiva entre `Start Hour` y `Stop Hour` en el tiempo de intercambio. La lógica de protección (punto de equilibrio, seguimiento, salidas) continúa funcionando incluso fuera de la sesión, al igual que en la implementación MQL.

## Parámetros
* **Usar filtro de tiempo**: alterna el filtro de la sesión de negociación.
* **Hora de inicio/Hora de finalización**: horas de sesión incluidas utilizadas cuando el filtro de tiempo está habilitado.
* **Fast SAR Step / Fast SAR Max** – factor de aceleración y aceleración máxima para el rápido Parabolic SAR.
* **Lento SAR Paso / Lento SAR Máx** – factor de aceleración y aceleración máxima para el lento Parabolic SAR.
* **Búsqueda de barras**: número de barras incluidas en el cálculo del swing alto/bajo.
* **Compensación (puntos)**: número de pasos de precio agregados más allá del extremo de oscilación al calcular el stop-loss.
* **Entrada Fibonacci %** – Fibonacci porcentaje (expresado como 0–200+) utilizado para el precio de la orden límite.
* **Objetivo Fibonacci %**: Fibonacci porcentaje aplicado para calcular la proyección de obtención de beneficios.
* **Break Even (puntos)** – ganancia en puntos necesarios antes de que el stop salte al precio de entrada (+/- un paso). Establezca en `0` para desactivar.
* **Trailing Stop (puntos)** – distancia entre el precio y el trailing stop. Establezca en `0` para deshabilitar el seguimiento.
* **Trailing Step (puntos)** – mejora mínima (en puntos) antes de avanzar el trailing stop.
* **Tipo de vela**: período de tiempo que impulsa el indicador y los cálculos de oscilación.
* **Volumen**: volumen de pedido base heredado de la clase StockSharp `Strategy` (predeterminado `0.1`).

## Notas adicionales
* Todos los parámetros basados en puntos se convierten automáticamente en compensaciones de precios utilizando el paso de precio del instrumento. Por lo tanto, los símbolos FX de cinco dígitos, índices y otros activos reutilizan la configuración EA sin escalado manual.
* La estrategia procesa solo velas terminadas proporcionadas por la suscripción configurada, que coinciden exactamente con la ejecución barra por barra de EA.
* No existe una versión Python de esta estrategia; sólo la implementación de C# está disponible en el paquete API.
