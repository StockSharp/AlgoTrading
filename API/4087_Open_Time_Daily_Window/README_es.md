# Estrategia de ventana diaria de tiempo abierto
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia reproduce el comportamiento del experto MetaTrader "OpenTime". Coloca órdenes de mercado a una hora del día configurable, opcionalmente cierra todas las exposiciones durante una ventana de salida dedicada y aplica reglas simples de administración de dinero, como stop-loss fijo, toma de ganancias y protección de seguimiento. El puerto utiliza StockSharp `Strategy` API de alto nivel, por lo que la estrategia se puede combinar con otros componentes dentro del marco.

## como funciona
1. Cada vela terminada del período de tiempo seleccionado activa una verificación de la hora del día.
2. Cuando la hora actual cae dentro de la ventana de negociación, la estrategia envía órdenes de mercado para cada dirección habilitada:
   * Si solo un lado está habilitado, la posición neta actual se extiende o invierte hasta alcanzar el volumen solicitado.
   * Cuando ambos lados están habilitados, las órdenes de compra y venta se emiten en la misma ventana. Debido a que StockSharp cuenta la exposición compensada en paralelo, abrir la segunda dirección compensa automáticamente la exposición opuesta antes de establecer la nueva.
3. Mientras la ventana de cierre está activa, la estrategia llama a `ClosePosition()` una vez para reducir cualquier exposición pendiente.
4. Las distancias opcionales de stop-loss, take-profit y trailing stop se delegan a `StartProtection`, que gestiona las órdenes de protección utilizando salidas de mercado.

## Parámetros
- **Habilitar cerrar ventana**: refleja la bandera `TimeClose`. Cuando están habilitados, `Close Position Time` y `Window Length` definen cuándo se cierran las operaciones existentes.
- **Hora de posición de cierre**: hora diaria en la que comienza la ventana de salida (predeterminado 20:50).
- **Hora de negociación**: hora diaria en la que se permiten nuevas transacciones (predeterminado 18:50).
- **Duración de la ventana**: duración de las ventanas de negociación y de cierre (5 minutos predeterminados, correspondientes a la entrada original `Duration`).
- **Permitir entradas de venta**: corresponde al interruptor MQL `Sell`; permite entradas breves (el valor predeterminado es verdadero).
- **Permitir entradas de compra**: corresponde al interruptor MQL `Buy`; permite entradas largas (falso predeterminado).
- **Volumen de pedido**: volumen neto objetivo para cada nueva operación (por defecto, 0,1 lotes). La estrategia agrega el valor absoluto de la posición actual cuando aparece una señal opuesta, por lo que las reversiones ocurren en una única orden de mercado.
- **Puntos Stop-Loss** – distancia en puntos para la parada de protección (el valor predeterminado 0 desactiva la parada).
- **Puntos Take-Profit**: distancia en puntos para el objetivo de ganancias (el valor predeterminado 0 desactiva el objetivo).
- **Usar Trailing Stop**: habilita la lógica de trailing stop del asistente original `SimpleTrailing`.
- **Puntos de parada de seguimiento**: distancia de seguimiento expresada en puntos (predeterminado 300).
- **Puntos de paso final**: se requiere progreso adicional antes de avanzar hasta el punto final (predeterminado 3).
- **Tipo de vela**: período de tiempo utilizado para las comprobaciones de tiempo (velas predeterminadas de 1 minuto).

## Notas
- El tamaño en puntos se deriva del paso del precio del valor. Para cotizaciones de tres y cinco decimales, el paso se multiplica por 10, reproduciendo el manejo de pips utilizado por el script MQL.
- `StartProtection` coloca topes de protección solo cuando al menos una de las distancias es mayor que cero. Si el seguimiento está activo sin un stop-loss fijo, la distancia de seguimiento se proporciona como valor de protección inicial.
- La estrategia no gestiona intencionalmente órdenes pendientes ni reintentos repetidos, porque StockSharp ya proporciona manejo automático de errores para órdenes de mercado.
