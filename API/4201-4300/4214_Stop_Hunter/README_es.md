# Detener la estrategia del cazador
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
- Transfiere el asesor experto MetaTrader 4 **Stop Hunter** al marco de estrategia de alto nivel StockSharp.
- Se centra en desgloses de números redondos: el algoritmo busca constantemente niveles de precios cuyos dígitos `Zeroes` más a la derecha sean cero y coloca órdenes stop justo dentro de esos umbrales.
- Mantiene los niveles de toma de ganancias y stop-loss ocultos al corredor supervisando las salidas internamente, reproduciendo la gestión de riesgos "virtual" utilizada en el EA original.
- Implementa la lógica de escalado de dos etapas del código fuente: la primera parte de una posición se cierra después del objetivo inicial, el resto sigue el doble de la distancia.

## Flujo de datos y suscripciones
1. Se suscribe a datos de **Nivel1** (`SubscribeLevel1().Bind(ProcessLevel1)`) en `OnStarted`. Sólo se requiere la mejor secuencia de oferta/demanda; No se utilizan velas ni indicadores.
2. Cada actualización almacena la última oferta y demanda y activa el motor de decisiones una vez que la estrategia está en línea y se permite el comercio.
3. Se crea un área de gráfico opcional para visualizar las propias operaciones cuando la estrategia se ejecuta con los gráficos habilitados.

## Lógica de colocación de pedidos
- **Detección de nivel redondo**
  - Utiliza el paso del precio del instrumento (`Security.PriceStep`) como análogo de MQL `Point`.
  - Calcula una longitud de paso redondo: `roundStep = PriceStep * 10^Zeroes`.
  - Calcula el siguiente número de ronda por encima de la oferta (`Math.Ceiling(bid / roundStep) * roundStep`).
  - Ajusta el nivel cuando la solicitud ya está dentro del buffer, reflejando la guardia original que evita enviar órdenes demasiado cerca del spread actual.
  - Deriva el nivel de ronda inferior (`LevelS`) un paso de ronda por debajo de `LevelB` y realiza el mismo ajuste de seguridad con respecto a la oferta.
- **Pedidos pendientes**
  - Coloca una **parada de compra** en `LevelB - DistancePoints * PriceStep` si no hay ninguna orden activa, las operaciones largas están habilitadas y no hay ninguna posición corta abierta.
  - Coloca un **stop de venta** simétricamente en `LevelS + DistancePoints * PriceStep` si se permiten operaciones cortas y no existe una posición larga.
  - Cancela órdenes pendientes obsoletas cada vez que el objetivo de ronda calculado avanza o el precio se aleja en más de un paso de ronda más `DistancePoints * 50`, coincidiendo con la lógica de limpieza de la versión MQL.
  - Mantiene el número total de espacios activos (posiciones + órdenes pendientes) dentro de `MaxLongPositions + MaxShortPositions`.

## Gestión de salidas virtuales
- Realiza un seguimiento del precio de entrada promedio y el volumen de la posición actual.
- Utiliza dos acumuladores de números enteros (`_takeProfitExtension`, `_stopLossExtension`) para reproducir los buffers ocultos originales:
  - Primer objetivo de beneficio: cierra la mitad de la posición cuando la oferta/demanda alcanza `TakeProfitPoints * PriceStep` a favor de la posición.
  - Después de la primera salida parcial, extiende las distancias de beneficio y parada en otro `TakeProfitPoints`/`StopLossPoints`, activando la etapa de "segunda operación".
  - Salida final: cierra el volumen restante cuando se alcanza el objetivo duplicado o cuando se alcanza la distancia de stop-loss duplicada.
- Cierra en el mercado usando `BuyMarket` o `SellMarket`, reflejando el EA que emitió los cierres del mercado en lugar de las órdenes de limitación de pérdidas del lado del corredor.
- Elimina la parada pendiente del lado opuesto cada vez que se abre una posición para evitar la cobertura, al igual que el bucle original que eliminaba órdenes en conflicto.

## Gestión monetaria
- Reimplementa la función `Call_MM()` del EA: `volume = balance / 100000 * RiskPercent`.
- Fija el volumen calculado entre `MinimumVolume` y `MaximumVolume` y lo redondea al paso de volumen del instrumento (o a 2/1/0 decimales dependiendo de `MinimumVolume`).
- Las salidas parciales reutilizan el tamaño de la posición actual para calcular cierres de mitad de volumen respetando el paso de volumen.

## Notas de implementación
- Utiliza solo StockSharp API de alto nivel (`BuyStop`, `SellStop`, `BuyMarket`, `SellMarket`, enlace de nivel 1). No se requieren llamadas de conector directo ni recopilación de indicadores.
- Mantiene el estado interno en todos los restablecimientos con `ResetState()` y garantiza que se utilicen pestañas para la sangría según las pautas del repositorio.
- Las cláusulas de protección (`IsFormedAndOnlineAndAllowTrading`) impiden el envío de pedidos antes de que la estrategia se inicialice por completo.
- `OnOwnTradeReceived` refleja las comprobaciones MQL que confirmaron cierres exitosos antes de actualizar el indicador `SecondTrade`.
- `OnOrderChanged` borra las referencias para evitar identificadores obsoletos cuando los pedidos se cancelan o rechazan.

## Diferencias frente a la versión MQL
- Modelo de neteo: las estrategias StockSharp operan con una única posición neta. Los parámetros predeterminados aún imitan el EA (un espacio largo y otro corto), pero no se admite el escalado a varios tickets simultáneos más allá de la exposición neta.
- El cálculo del riesgo utiliza `Portfolio.CurrentValue` (respaldo a `BeginValue`) en lugar de `AccountFreeMargin`, lo que proporciona una aproximación portátil en entornos de múltiples activos.
- Las distancias virtuales de parada/toma de ganancias se restablecen limpiamente cuando se abre una nueva operación, evitando el error de acumulación presente en el código histórico EA.
- Todos los comentarios y la documentación están escritos en inglés, mientras que los archivos README describen adicionalmente la estrategia en ruso y chino según lo exigen las pautas del proyecto.

## Parámetros
| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `Zeroes` | 2 | Dígitos del lado derecho que deben ser cero para que un precio se considere nivel redondo. |
| `DistancePoints` | 15 | Compensación (en puntos de precio) entre el nivel redondo y la entrada stop. |
| `TakeProfitPoints` | 15 | Distancia de toma de ganancias oculta en puntos. También reutilizado para la ampliación de la segunda etapa. |
| `StopLossPoints` | 15 | Distancia de stop-loss oculta en puntos (duplicada después del primer escalado). |
| `EnableLongOrders` | cierto | Permite la colocación de buy-stop. |
| `EnableShortOrders` | cierto | Permite la colocación de stop de venta. |
| `RiskPercent` | 5 | Porcentaje de capital utilizado para dimensionar las órdenes pendientes. |
| `MinimumVolume` | 0.1 | Tamaño mínimo de pedido después del redondeo. |
| `MaximumVolume` | 30 | Límite para el volumen calculado. |
| `MaxLongPositions` | 1 | Número máximo de slots largos (posición + pendiente). |
| `MaxShortPositions` | 1 | Número máximo de slots cortos (posición + pendiente). |

## Consejos de uso
1. Elija un instrumento cuyo precio se alinee con la definición MQL `Point` utilizada por el asesor experto original. Los pares de Forex con pips fraccionarios normalmente requieren `Zeroes = 2`.
2. Supervisar el tamaño del tick del corredor y el paso del volumen; ajustar `MinimumVolume` garantiza que la lógica de redondeo coincida con las restricciones de intercambio.
3. Debido a que las salidas son virtuales, mantenga siempre la estrategia en línea para evitar perder las condiciones de stop-loss. Considere la posibilidad de combinarlo con `StartProtection()` de StockSharp si se requiere gestión de riesgos del lado del intercambio.
4. Revise las variantes README rusa y china para obtener explicaciones localizadas que los operadores pueden compartir con diferentes equipos.
