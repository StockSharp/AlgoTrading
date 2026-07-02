# Estrategia de comando de gestión de órdenes ARD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general de la estrategia
La estrategia **ARD Order Management** traslada el MetaTrader 4 asesor experto `ARD_ORDER_MANAGEMENT_.mq4` al marco de estrategia de alto nivel de StockSharp. El script original exponía un conjunto de comandos manuales (comprar, vender, cerrar y modificar) que podían activarse desde scripts externos o botones de la interfaz de usuario. Cada comando recalculó el volumen comercial a partir del margen libre disponible, abrió o revirtió posiciones de mercado y adjuntó niveles protectores de stop-loss y take-profit a distancias de puntos fijos.

La versión StockSharp mantiene el mismo modelo de interacción. Usted controla el comportamiento a través del parámetro `Command`; una vez que se establece un valor distinto de `None`, la estrategia realiza la acción solicitada en la siguiente actualización de Nivel 1 y restablece automáticamente el comando a `None`. Las órdenes de protección se recrean con cada nueva entrada o solicitud de modificación para que el stop-loss y el take-profit siempre reflejen los valores de los parámetros actuales.

## Ciclo de vida del comando
1. **Envío de comando**: cuando `Command` se establece en `Buy` o `Sell`, la estrategia almacena la solicitud e inmediatamente llama a `ClosePosition()` para reducir cualquier exposición abierta. Las órdenes de protección activas se cancelan antes de que se considere la nueva operación, reflejando el ciclo MQL que cerró todos los tickets a través de `OrderClose`.
2. **Cálculo de volumen**: el volumen se vuelve a calcular para cada comando. Utiliza `Portfolio.CurrentValue` (retroceso a `Portfolio.BeginValue`) dividido por `LotSizeDivisor` y escalado por `1/1000`, exactamente como se usó `AccountFreeMargin()/lotsize/1000` en MetaTrader. El resultado se redondea a `LotDecimals` y se restringe por `MinimumVolume`.
3. **Esperando una posición plana**: si había una posición abierta cuando llegó el comando, la nueva entrada se pospone hasta que `Position` caiga a cero. La estrategia verifica esta condición en cada tic de Nivel 1 para evitar acelerar el proceso de ejecución asincrónica.
4. **Ejecución de mercado**: una vez estable, la estrategia envía `BuyMarket` o `SellMarket`. Los últimos mejores precios de oferta/demanda conocidos se almacenan de modo que las órdenes de protección se deriven de precios de ejecución realistas.
5. **Colocación de protección**: los niveles de límite de pérdidas y toma de ganancias se materializan como órdenes de límite y límite separadas. Para operaciones largas, el stop se sitúa en `bid − StopLossPoints * PriceStep` y el objetivo en `ask + TakeProfitPoints * PriceStep`. Las operaciones cortas invierten esos cálculos. Los comandos de modificación reutilizan la misma rutina pero con `ModifyStopLossPoints` y `ModifyTakeProfitPoints`.
6. **Comando de cierre**: configurar `Command` en `Close` cancela todas las órdenes de protección y llama a `ClosePosition()`. Si la estrategia ya es plana, el comando simplemente registra el hecho y no hace nada más.

## gestión del dinero
- **Volumen impulsado por el margen**: el código inspecciona el valor actual de la cartera para que el volumen se reduzca o crezca con el capital disponible. Si el parámetro divisor cae accidentalmente a cero, el algoritmo vuelve al `MinimumVolume` configurado y emite una advertencia.
- **Redondeo**: `LotDecimals` define cuántos decimales se conservan después del redondeo. La implementación utiliza `Math.Round` con `MidpointRounding.AwayFromZero` para que los ajustes positivos y negativos se comporten como los `NormalizeDouble` de MetaTrader.
- **Lote mínimo**: después del redondeo, el tamaño se fija con `MinimumVolume`, reproduciendo el comportamiento original donde los valores inferiores a `lotmax` se promovían a `0.1`.

## Manejo de stop-loss y take-profit
- Las órdenes de protección siempre se recrean desde cero. Las órdenes de detener o tomar existentes se cancelan antes de enviar otras nuevas.
- La estrategia verifica `Security.PriceStep` antes de calcular los precios absolutos. Si falta el paso o no es positivo, las órdenes de protección se omiten y se registra una advertencia.
- Los comandos de modificación (`Command = Modify`) reconstruyen la protección utilizando las distancias de modificación dedicadas sin cambiar el tamaño de la posición actual.

## Requisitos de datos y ejecución
- Se suscribe a los datos de nivel 1 a través de `SubscribeLevel1()` para reflejar las actualizaciones de cotizaciones de MetaTrader (`Bid`/`Ask`).
- No requiere velas ni indicadores; toda la lógica se ejecuta en actualizaciones de ticks/comillas.
- Utiliza ayudas de alto nivel (`BuyMarket`, `SellMarket`, `BuyStop`, `SellStop`, `BuyLimit`, `SellLimit`, `CancelOrder`) para permanecer dentro de la capa StockSharp recomendada API.

## Parámetros
| Nombre | Tipo | Predeterminado | Descripción |
| --- | --- | --- | --- |
| `SlippageSteps` | entero | 4 | Deslizamiento permitido expresado en incrementos de precios. Almacenado por compatibilidad; StockSharp órdenes de mercado se ejecutan inmediatamente y no consumen este valor. |
| `LotDecimals` | entero | 1 | Número de decimales retenidos después de redondear el volumen calculado. |
| `StopLossPoints` | decimales | 50 | Distancia (en puntos de precio) desde la entrada hasta el stop-loss inicial. |
| `TakeProfitPoints` | decimales | 100 | Distancia (en puntos de precio) desde la entrada hasta la obtención de beneficios inicial. |
| `LotSizeDivisor` | decimales | 5 | Divide el valor de la cartera antes de escalar a lotes (`freeMargin / divisor / 1000`). |
| `ModifyStopLossPoints` | decimales | 20 | Distancia de stop-loss aplicada cuando `Command = Modify`. |
| `ModifyTakeProfitPoints` | decimales | 100 | Distancia de obtención de beneficios aplicada cuando `Command = Modify`. |
| `MinimumVolume` | decimales | 0.1 | Límite inferior del volumen final después del redondeo. |
| `OrderComment` | cuerda | `"Placing Order"` | Comentario insertado en cada pedido para facilitar la auditoría. |
| `Command` | `ArdOrderCommand` | `None` | Comando manual para ejecutar. Se restablece automáticamente a `None` una vez procesado. |

## Notas de uso
- Configure `Command` a través de la interfaz de usuario o mediante programación. El comando se procesa sólo una vez por cambio; para repetir una acción, configúrelo nuevamente en `None` y luego nuevamente en el valor deseado.
- Debido a que el stop-loss y el take-profit se colocan como órdenes independientes, los corredores/bolsas deben admitir órdenes nativas stop/limit para el mismo valor. Si no es así, considere reemplazarlas con salidas sintéticas en el código.
- El deslizamiento se mantiene como parámetro para la paridad de la documentación con la versión MT4. Los asistentes de mercado de alto nivel de StockSharp no exponen un parámetro de deslizamiento explícito, por lo que el valor es solo informativo.
- La estrategia registra cada acción importante (`LogInfo`/`LogWarn`) para ayudar con los seguimientos de auditoría durante la ejecución discrecional.

## Diferencias respecto al asesor experto MQL original
- MetaTrader paradas y objetivos adjuntos directamente al ticket de mercado. En su lugar, StockSharp emite órdenes stop y límite separadas.
- El puerto utiliza el modelo de evento asíncrono de StockSharp. Al revertir una posición, la entrada espera hasta que la posición anterior se informe como cerrada, evitando la superposición de órdenes.
- La información de la cartera reemplaza a `AccountFreeMargin`. Asegúrese de que el adaptador de cartera complete `CurrentValue` o configure `BeginValue` antes de iniciar la estrategia.
- El manejo de errores se basa en el registro StockSharp en lugar de en repetidos reintentos `OrderSend` porque el propio marco detecta las excepciones en el envío de pedidos.

## Consejos de prueba
- Ejecute la estrategia en simulación con datos de Nivel 1 para confirmar que las órdenes de protección aparecen a las distancias esperadas.
- Experimente con diferentes valores `LotSizeDivisor` y `LotDecimals` para que coincidan con las especificaciones del contrato del corredor antes de usar la estrategia en entornos reales.
