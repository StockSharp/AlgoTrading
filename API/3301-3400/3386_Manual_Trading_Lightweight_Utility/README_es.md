# Estrategia de utilidad ligera de comercio manual
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
El asesor experto original "Manual Trading Lightweight Utility" es un panel MetaTrader compacto que muestra botones para cambiar entre órdenes de mercado, límite y stop, ajusta los volúmenes de forma independiente para acciones de compra y venta y adjunta automáticamente compensaciones de stop-loss y take-profit. Este puerto de C# recrea el mismo flujo de trabajo dentro de StockSharp al representar cada botón del panel como un parámetro de estrategia. La estrategia no produce señales autónomas; espera sus instrucciones manuales y luego ejecuta la acción solicitada utilizando el API de alto nivel mientras supervisa las salidas de protección.

## Funcionalidad recreada
- **Solicitudes de compra y venta únicas.** Dos conmutadores booleanos emulan los botones del panel. Configurar `BuyRequest` o `SellRequest` en `true` activa exactamente una orden de mercado, límite o stop según el modo seleccionado e inmediatamente restablece el interruptor a `false`.
- **Precios pendientes automáticos o manuales.** Cada lado puede reutilizar las compensaciones MetaTrader (`LimitOrderPoints` y `StopOrderPoints`) o aceptar un precio absoluto manual. El precio automático utiliza la mejor oferta/demanda actual o el último cierre de vela cuando las cotizaciones no están disponibles.
- **Volúmenes independientes.** Puede compartir un volumen predeterminado entre ambos lados o activar volúmenes por lado para reflejar el interruptor de control de lote de la versión MQL.
- **Protección basada en puntos.** `TakeProfitPoints` y `StopLossPoints` traducen las distancias de puntos MetaTrader en compensaciones de precios utilizando el instrumento `PriceStep`. La estrategia monitorea las velas completadas y cierra la posición con una orden de mercado cuando se perfora un nivel protector.
- **Comentarios.** Cada acción manual escribe una entrada de registro que incluye el `OrderComment` configurado, lo que facilita el seguimiento de los comandos ejecutados sin un panel visual.

## Flujo de estrategia
1. La estrategia se suscribe al tipo de vela seleccionado por `CandleType`. Las velas terminadas proporcionan los precios de referencia utilizados para las compensaciones y la supervisión de riesgos.
2. Por cada vela completada la estrategia:
   - Actualiza la clase base `Volume` con `DefaultVolume` (útil para inspección visual en StockSharp).
   - Detecta cambios en `BuyRequest` y `SellRequest` y los marca como acciones pendientes.
   - Una vez que los datos del mercado están listos (`IsFormedAndOnlineAndAllowTrading()`), ejecuta las acciones solicitadas, resuelve los precios de las órdenes pendientes y registra el resultado.
   - Llama al administrador de riesgos que registra el precio de entrada cada vez que cambia la posición neta y emite salidas del mercado si se cruzan los umbrales de stop-loss o take-profit.
3. Cuando la posición vuelve a ser plana, todo el estado interno se restablece para que la siguiente solicitud manual comience con borrón y cuenta nueva.

## Parámetros
- **`CandleType`** – serie de datos de mercado utilizada para referencias de precios y gestión de riesgos.
- **`BuyOrderMode` / `SellOrderMode`**: elige entre `MarketExecution`, `PendingLimit` o `PendingStop` para cada lado.
- **`UseAutomaticBuyPrice` / `UseAutomaticSellPrice`**: habilite la compensación automática de precios. Desactivar para ofrecer un precio absoluto fijo.
- **`BuyManualPrice` / `SellManualPrice`**: precios de pedidos pendientes manuales que se aplican cuando el precio automático está desactivado (establecido en `0` para ignorarlo).
- **`DefaultVolume`**: volumen de pedidos compartido cuando los volúmenes individuales están deshabilitados.
- **`UseIndividualVolumes`** – alterna el análogo de control de lote. Cuando está habilitado, los siguientes dos parámetros anulan el volumen compartido.
- **`BuyVolume` / `SellVolume`** – volúmenes por lado.
- **`TakeProfitPoints` / `StopLossPoints`** – distancias de protección expresadas en MetaTrader puntos. Zero desactiva la función respectiva.
- **`LimitOrderPoints` / `StopOrderPoints`**: compensaciones aplicadas a los precios límite y stop automáticos, también medidos en puntos.
- **`BuyRequest` / `SellRequest`**: conmutadores momentáneos que emulan los botones del panel. Se restablecen automáticamente después de que se procesa la solicitud.
- **`OrderComment`**: texto de formato libre que se agrega al registro cuando se ejecuta una acción.

## Pautas de uso
1. Configure `CandleType` para que coincida con la granularidad que desea utilizar para compensaciones y comprobaciones de riesgos. El período de tiempo predeterminado de un minuto se asemeja al comportamiento basado en ticks del script MetaTrader y, al mismo tiempo, sigue siendo compatible con las pruebas históricas.
2. Elija si desea trabajar con un único `DefaultVolume` o habilitar `UseIndividualVolumes` para controlar los volúmenes de compra y venta por separado. Los volúmenes deben seguir siendo positivos.
3. Decida cómo se deben calcular los precios pendientes. Deje `UseAutomatic*Price` habilitado para replicar las compensaciones de puntos MetaTrader o deshabilítelo y proporcione los valores `BuyManualPrice` / `SellManualPrice` explícitamente.
4. Configure `TakeProfitPoints` y `StopLossPoints` según sea necesario. Cuando son mayores que cero, la estrategia las convierte en distancias de precios utilizando el instrumento `PriceStep` y cierra la posición con una orden de mercado tan pronto como una vela cruza el umbral correspondiente. Si el símbolo carece de un `PriceStep` configurado, se registra una advertencia y se omiten las distancias de protección.
5. Para enviar un pedido, cambie `BuyRequest` o `SellRequest` de `false` a `true`. La estrategia resuelve la solicitud en la siguiente vela finalizada, envía el tipo de orden elegido, escribe una entrada de registro y restablece la bandera para que la acción no se repita automáticamente.
6. Vuelva a emitir cualquier acción alternando nuevamente el parámetro correspondiente. Las solicitudes permanecen inactivas si el precio requerido no se puede resolver (por ejemplo, porque un precio manual es cero); corrija la configuración y vuelva a alternar para intentarlo de nuevo.

## Diferencias con la utilidad MQL original
- Los objetos del gráfico MetaTrader se reemplazan con parámetros StockSharp. Cada botón y palanca del panel original ahora es una propiedad editable que se puede controlar desde la interfaz de usuario o mediante scripts de automatización.
- Los niveles de protección se ejecutan con órdenes de mercado cuando se incumplen en lugar de registrar órdenes de protección de límite/detención separadas. Esto mantiene la implementación dentro del nivel alto API y evita administrar los ciclos de vida de los pedidos manualmente.
- Los precios automáticos retroceden hasta el último cierre de vela si las mejores cotizaciones de oferta/demanda no están disponibles, lo que garantiza un comportamiento determinista durante las pruebas retrospectivas en las que los datos del libro de órdenes pueden estar ausentes.

## Notas
- La estrategia almacena el precio de entrada cada vez que cambia la posición neta. Si escala una operación, las compensaciones protectoras se vuelven a anclar en el cierre de la vela que refleja el nuevo tamaño.
- La compensación del diferencial se incluye en el cálculo del stop-loss agregando el diferencial más conocido (o un paso de precio cuando faltan cotizaciones) a la distancia del punto configurado, reflejando la lógica MQL que amplió los límites de venta en el diferencial actual.
- Las entradas del registro contienen el comentario configurado, el tipo de orden, el precio (para órdenes pendientes) y el volumen, proporcionando un seguimiento de auditoría conciso para cada acción manual.
