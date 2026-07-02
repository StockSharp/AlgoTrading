# Ejemplo de estrategia de orden pendiente de cheque
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia de orden pendiente de verificación de muestra garantiza continuamente que haya exactamente una orden de compra y una orden de venta en el libro. El experto MetaTrader 5 original de Tungman verifica que el corredor acepte el tamaño de lote solicitado, confirma que hay suficiente margen libre para ambas direcciones y luego envía nuevas órdenes pendientes justo encima de la oferta/demanda actual con un vencimiento de un día. Esta conversión reproduce el mismo flujo de trabajo utilizando la gestión de pedidos de alto nivel de StockSharp API y las cotizaciones de nivel 1.

## Lógica de trading

1. **Procesamiento de datos de mercado**
   - La estrategia se suscribe a las actualizaciones de Nivel 1 y almacena en caché los mejores precios de oferta y demanda más recientes.
   - La lógica comercial se suspende hasta que se conozcan ambos lados del libro y `IsFormedAndOnlineAndAllowTrading()` confirme que el entorno está listo (la estrategia se está ejecutando, la cartera está conectada, etc.).
2. **Validación de volumen**
   - Cada tick entrante activa una validación del `OrderVolume` configurado frente a `Security.MinVolume`, `Security.MaxVolume` y `Security.VolumeStep`.
   - La verificación refleja el asistente MT5: el volumen debe estar dentro del rango permitido y ser un múltiplo exacto del paso. Las infracciones generan una entrada de registro informativa y bloquean cualquier pedido nuevo.
3. **Comprobación previa del margen**
   - Antes de enviar nada, la estrategia estima el margen necesario para colocar una posición larga o corta del tamaño configurado. Utiliza la oferta/demanda más reciente, el multiplicador del instrumento y el `AccountLeverage` proporcionado por el usuario para calcular el requisito.
   - Si el valor de la cartera actual o inicial es insuficiente para cualquier dirección, el algoritmo aborta para ese tick, imitando fielmente las salvaguardias `CheckMoneyForTrade`.
4. **Pendiente de realizar el pedido**
   - Cuando no existe una orden buy-stop activa, se registra una nueva en la demanda actual (redondeada al tick más cercano). La misma regla se aplica al sell-stop en la oferta actual. Ambas órdenes reutilizan el mismo resultado de validación de volumen.
   - La caducidad se aplica manualmente: cada pedido almacena su límite de tiempo (`ExpirationMinutes`, un día por defecto). Los ticks futuros cancelan la orden si la fecha límite ha pasado y liberan inmediatamente el espacio para una nueva orden pendiente.
5. **Gestión de riesgos**
   - `StartProtection` transfiere un límite de pérdidas absoluto y una toma de ganancias basada en `StopLossPoints` y `TakeProfitPoints`. Una vez que se activa una orden, StockSharp envía automáticamente las salidas de protección a las distancias configuradas, recreando los parámetros SL/TP utilizados en la versión MT5.

El resultado final es un motor de ruptura minimalista que siempre mantiene el mercado "encajonado" entre dos órdenes stop. Cada vez que se completa una orden, la otra parte permanece activa mientras la estrategia se prepara para volver a emitir la parte faltante en la próxima actualización de la cotización.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `OrderVolume` | Tamaño de lote enviado con cada orden de parada. Debe respetar los límites del corredor y el paso de volumen. |
| `StopLossPoints` | Distancia en puntos convertida a unidades de precio para la parada de protección una vez que se abre una operación. |
| `TakeProfitPoints` | Distancia en puntos utilizada para el objetivo de ganancias creado después de un relleno. |
| `ExpirationMinutes` | Vida útil de cada orden pendiente. Cuando expira el período, la orden se cancela y se vuelve a crear en el siguiente tick. |
| `AccountLeverage` | Apalancamiento estimado de la cuenta utilizado para aproximar los requisitos de margen antes de cada envío. |

Todas las distancias se transforman en compensaciones de precios reales usando `Security.PriceStep`. Si el instrumento no expone un paso de precio o multiplicador válido, la estrategia vuelve a un valor de `1` para mantener los cálculos definidos. Los mensajes de registro documentan cualquier configuración anormal para que los operadores puedan ajustar los parámetros rápidamente.

## Notas de implementación

- **Ciclo de vida del pedido**: la estrategia rastrea los últimos objetos `Order` devueltos por `BuyStop` y `SellStop`. Los métodos auxiliares descartan las referencias una vez que el pedido pasa a `Done`, `Canceled` o `Failed`, lo que garantiza que los pedidos obsoletos no se confundan con los activos.
- **Manejo de vencimiento**: los intercambios StockSharp no admiten universalmente el vencimiento del lado del servidor para órdenes de suspensión. En lugar de depender de campos específicos del corredor, la estrategia monitorea las marcas de tiempo localmente y llama a `CancelOrder` cuando una orden pendiente supera su fecha límite.
- **Aproximación del margen**: la disponibilidad del margen se estima utilizando el capital de la cartera y el apalancamiento configurado. Esto mantiene el comportamiento cercano a `OrderCalcMargin` sin requerir implementaciones específicas del intercambio.
- **Uso de API de alto nivel**: todas las operaciones dependen de los ayudantes de alto nivel `SubscribeLevel1`, `BuyStop`, `SellStop` y `StartProtection`, que coinciden con las pautas de conversión y mantienen el código conciso.

Esta documentación contiene intencionalmente muchos detalles para que los operadores puedan comprender cada matiz de la conversión y adaptar con confianza los parámetros a su entorno de corredor.
