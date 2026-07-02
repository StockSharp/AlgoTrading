# Estrategia del canal Graal Fractal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de canal Fractal de Graal** es una versión StockSharp del asesor experto MetaTrader 4 "Graal-003". El algoritmo observa patrones fractales de cinco velas y confirma las rupturas utilizando canales de precios adaptativos. Cuando aparece un fractal alcista o bajista válido, la estrategia evalúa varios filtros (túnel fractal, envolvente de precio de cierre y supresión opcional del mercado plano) antes de entrar en la dirección de ruptura. Una superposición opcional de Williams %R replica la lógica de salida manual del robot original, mientras que las órdenes de stop de cobertura se pueden preparar para emular la protección contratendencia del EA.

## Flujo de datos e indicadores.
* Se suscribe al `CandleType` configurado (velas horarias por defecto).
* Crea una cola continua de las últimas `ChannelPeriod` velas para calcular un canal de precio de cierre similar a Donchian utilizado para filtros planos y comprobaciones de orientación.
* Detecta máximos y mínimos fractales de cinco barras directamente desde el flujo de velas.
* Alimenta el indicador incorporado `WilliamsPercentRange` para monitorear señales de salida opcionales.

## Flujo de trabajo comercial
1. **Detección de fractales**: la estrategia rastrea cinco velas terminadas consecutivas. Cuando el máximo/mínimo de la barra media es el extremo en comparación con sus dos predecesores y dos seguidores, registra un fractal superior o inferior y marca una señal corta o larga pendiente.
2. **Envejecimiento de la señal**: cada nueva vela aumenta la edad fractal. Si `SignalAgeLimit` barras pasan sin ejecución, la señal pendiente caduca.
3. **Evaluación del canal**: el canal de cierre rodante proporciona tres filtros:
   - *Túnel fractal*: cuando `UseFractalChannel` está habilitado, el precio de cierre debe permanecer dentro de un porcentaje de la distancia entre el último máximo y mínimo fractal (`DepthPercent`).
   - *Orientación alta/baja*: con `UseHighLowChannel`, el cierre debe penetrar solo una porción limitada del sobre (`OrientationPercent`).
   - *Bloqueo plano*: si `AllowFlatTrading` está deshabilitado, las operaciones se suspenden mientras el ancho del canal se mantenga por debajo de `FlatThresholdPips`.
4. **Ejecución de la orden**: una vez que pasan los filtros, la estrategia normaliza el `OrderVolume` deseado frente a las restricciones del instrumento y envía una orden de mercado en la dirección fractal.
5. **Paradas de cobertura**: cuando `UseCounterOrders` está activo, el algoritmo coloca la orden de parada opuesta al precio fractal más/menos `OffsetPips`, reflejando la puesta en escena de contratendencia de EA.
6. **Williams sale**: si `UseWilliamsExit` está habilitado, el valor %R Williams más reciente cierra posiciones largas cuando sube por encima de `-WilliamsThreshold` y posiciones cortas cuando cae por debajo de `-100 + WilliamsThreshold`.

Las distancias de parada de pérdidas y toma de ganancias son opcionales. Siempre que `StopLossPips` o `TakeProfitPips` sea positivo, la estrategia convierte la distancia del pip en una compensación de precio absoluta utilizando el tamaño del tick del instrumento (con el ajuste de 3/5 dígitos de EA) y delega la gestión de órdenes de protección a `StartProtection`.

## Parámetros
| Parámetro | Predeterminado | Descripción |
|-----------|---------|-------------|
| `OrderVolume` | `0.1` | Tamaño base de la orden de mercado antes de la normalización frente a los límites del instrumento. |
| `StopLossPips` | `500` | Distancia de parada de protección en pips. Convertido a precio y aplicado a través de `StartProtection`. |
| `TakeProfitPips` | `500` | Tomar distancia de ganancias en pips. Convertido a precio y aplicado a través de `StartProtection`. |
| `OffsetPips` | `5` | Distancia adicional utilizada al organizar órdenes stop contratendencia. |
| `ChannelPeriod` | `14` | Número de velas recientes almacenadas para el canal de precio de cierre. |
| `UseFractalChannel` | `false` | Requiere que el precio permanezca dentro del corredor fractal interior antes de las entradas. |
| `DepthPercent` | `25` | Porcentaje del rango fractal que define el corredor interior. |
| `UseHighLowChannel` | `false` | Habilita el filtro de orientación de canal cerrado estilo Donchian. |
| `OrientationPercent` | `20` | Penetración permitida en el canal cercano cuando `UseHighLowChannel` es verdadero. |
| `AllowFlatTrading` | `true` | Permite operar incluso cuando el mercado está plano según el ancho del canal de cierre. |
| `FlatThresholdPips` | `20` | Ancho mínimo del canal (en pips) requerido cuando el comercio plano está deshabilitado. |
| `UseWilliamsExit` | `false` | Activa Williams reglas de salida basadas en %R. |
| `WilliamsPeriod` | `14` | Período retroactivo para el indicador Williams %R. |
| `WilliamsThreshold` | `30` | Umbral de sensibilidad (puntos porcentuales) para Williams %R salidas. |
| `UseCounterOrders` | `false` | Coloca la orden stop opuesta después de una entrada al mercado. |
| `SinglePosition` | `false` | Bloquea entradas adicionales en la misma dirección mientras una posición está abierta. |
| `SignalAgeLimit` | `3` | Número máximo de barras nuevas durante las cuales una señal fractal permanece válida. |
| `CandleType` | `H1` | Serie de datos de velas utilizada para el análisis (el valor predeterminado es un período de una hora). |

## Notas de uso
* La estrategia espera instrumentos con `PriceStep`, `MinVolume` y `VolumeStep` válidos para que la normalización de volumen y la conversión de pips funcionen correctamente.
* Las órdenes contratendencia se cancelan automáticamente cuando se cierra la posición, cuando se detiene la estrategia o cuando se desactiva la función.
* Williams Las salidas %R actúan como una red de seguridad y pueden cerrar posiciones incluso si la señal fractal original todavía está activa.
* El algoritmo restablece todo el estado almacenado en caché (búferes fractales, historial de Williams, pedidos en etapas) cada vez que se activa `OnReseted`.

## Diferencias con la versión MetaTrader
* La implementación StockSharp utiliza suscripciones `SubscribeCandles().Bind(...)` de alto nivel en lugar de bucles de indicadores manuales.
* Las paradas de protección dependen de `StartProtection`, por lo que no se requiere contabilidad de órdenes de parada/límite directas.
* El volumen se normaliza según los límites de cambio antes de enviar los pedidos, coincidiendo con las convenciones StockSharp.
