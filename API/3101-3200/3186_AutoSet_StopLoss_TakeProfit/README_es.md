# Estrategia Auto Stop-Loss y Take-Profit
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utilitaria adjunta automáticamente órdenes protectoras de stop-loss y take-profit a cada posición abierta en el instrumento configurado. Replica el comportamiento del experto original de MetaTrader "AutoSet SL TP" monitoreando la lista de posiciones activas y aplicando las restricciones de distancia del bróker antes de registrar las órdenes protectoras.

La estrategia no abre trades por sí sola. En su lugar, monitorea el volumen, dirección y precio de ejecución de las posiciones que han sido creadas manualmente o por otras estrategias. En cuanto aparece una posición larga o corta, el algoritmo calcula los niveles deseados de stop-loss y take-profit expresados en pips al estilo MetaTrader, ajusta los niveles para cumplir con las restricciones de congelación y parada publicadas por el lugar de negociación, y luego envía las órdenes protectoras de mercado apropiadas. Cuando la posición se cierra completamente, las órdenes protectoras se cancelan automáticamente.

## Cómo funciona

1. Se suscribe a datos de Nivel1 para recibir los mejores precios bid/ask junto con los campos opcionales `StopLevel` y `FreezeLevel` suministrados por el bróker.
2. Convierte las distancias configuradas en pips a precios absolutos usando los metadatos del símbolo (paso de precio y precisión decimal). Las cotizaciones de cinco y tres dígitos se escalan automáticamente por un factor de diez para coincidir con la semántica de pip de MetaTrader.
3. En cada actualización de cotización o notificación de trade personal:
   - Ignora la señal si no hay posición abierta o si la dirección no coincide con el filtro configurado (solo compra, solo venta o ambos).
   - Calcula la distancia mínima permitida entre el precio de mercado y una orden protectora. Si el bróker no publica niveles de congelación/parada, el algoritmo recurre a tres spreads multiplicados por 1.1 para mantenerse con seguridad fuera de las zonas prohibidas.
   - Determina el precio de stop-loss y take-profit en relación con el ask actual (para largos) o bid (para cortos) y normaliza el resultado al paso de precio del instrumento.
   - Coloca o re-registra órdenes protectoras de stop o límite con el volumen exacto de la posición. Las órdenes se reemplazan solo cuando cambia el precio objetivo o el volumen, lo que minimiza las modificaciones en el exchange.
4. Si el volumen de la posición se vuelve cero, todas las órdenes protectoras pendientes se cancelan. La estrategia también cancela las órdenes existentes cuando la dirección del trade ya no está permitida por el filtro.

Dado que el algoritmo depende únicamente de llenados externos, puede combinarse con trading discrecional, paneles u otros sistemas automatizados que gestionan las entradas, mientras esta estrategia garantiza un envolvente protector consistente.

## Parámetros

- **`StopLossPips`** – distancia desde el precio de mercado actual al stop-loss en pips de MetaTrader. Un valor de `0` deshabilita la orden de stop. Predeterminado: `50`.
- **`TakeProfitPips`** – distancia desde el precio de mercado actual al take-profit en pips de MetaTrader. Un valor de `0` deshabilita la orden de take-profit. Predeterminado: `140`.
- **`DirectionFilter`** – especifica qué dirección de posición se gestiona:
  - `Buy` – proteger solo la exposición larga.
  - `Sell` – proteger solo la exposición corta.
  - `BuySell` – proteger ambos lados (comportamiento predeterminado en el script original).

## Notas prácticas

- Las órdenes protectoras siempre se crean con el volumen absoluto de la posición. Si el bróker impone tamaños de lote mínimos o máximos, la estrategia redondea el volumen al valor permisible más cercano antes de colocar las órdenes.
- El algoritmo usa `ReRegisterOrder` para ajustar las órdenes protectoras activas. Esto mantiene los mismos identificadores de orden del exchange siempre que sea posible y evita cancelaciones innecesarias.
- La distancia de respaldo (spread × 3 × 1.1) evita que el stop o take-profit viole restricciones ocultas del exchange cuando no se proporcionan niveles explícitos de congelación/parada.
- Dado que la estrategia no gestiona entradas, puede iniciarse antes o después de que se abran posiciones. Cualquier posición calificada que ya exista al momento del inicio será protegida inmediatamente después de la primera actualización de cotización.
- Los "pips" de MetaTrader difieren de los pasos de precio del exchange en símbolos con tres o cinco dígitos decimales. La estrategia replica el Expert Advisor original multiplicando el valor del punto en consecuencia, asegurando que los números configurados se correspondan exactamente con la configuración de MT5.

## Diferencias con el experto de MetaTrader

- En lugar de modificar los atributos de stop y take-profit en posición, StockSharp gestiona órdenes protectoras de stop y límite explícitas. Este enfoque mantiene la lógica completamente transparente dentro del libro de órdenes de StockSharp.
- La versión de StockSharp usa datos de mercado de Nivel1 para reconstruir los niveles de restricción del bróker. Si el proveedor expone diferentes nombres de campo para las distancias de congelación o parada, la estrategia los descubre automáticamente a través de reflexión en el enum `Level1Fields`.
- Cada comentario de código y mensaje de registro está en inglés para mantener la coherencia con las directrices de codificación, mientras la documentación está localizada en ruso y chino para los usuarios finales.
