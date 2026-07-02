# Estrategia de activación del comerciante de patrones Macd
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Macd Pattern Trader Trigger Strategy traslada el MetaTrader 4 asesor experto `MacdPatternTraderv05cb` a la estrategia de alto nivel de StockSharp API. El sistema intercambia patrones de histograma MACD puros, buscando una estructura de doble techo debajo de la línea cero para abrir posiciones cortas y una imagen especular de doble fondo por encima de la línea cero para abrir posiciones largas. La gestión comercial refleja el EA original: cada entrada se envía al mercado con un stop loss fijo configurable y una toma de ganancias medida en puntos del instrumento.

## Lógica estratégica
### Flujo de indicadores
* Una suscripción de una sola vela impulsa la lógica (predeterminado: velas de 15 minutos). Cada vela terminada alimenta un indicador `MovingAverageConvergenceDivergence` configurado con los parámetros MT4 inusuales `(fast = 13, slow = 5, signal = 1)` utilizados por la fuente EA.
* Sólo se utiliza la línea principal MACD. La estrategia almacena en buffer los últimos tres valores completados para emular `iMACD(..., MODE_MAIN, shift=1..3)` de MetaTrader.

### Configuración alcista (entradas largas)
1. **Condición de armado**: la línea MACD debe elevarse por encima de `Bullish Trigger` (predeterminado `0.0015`). Esto prepara la estrategia para buscar la secuencia de retroceso. Cualquier caída por debajo de cero borra el estado inmediatamente.
2. **Ventana de retroceso**: una vez armado, el MACD tiene que volver a caer por debajo de `Bullish Reset` (predeterminado `0.0005`). Esto marca el área potencial de retroceso. La ventana permanece activa hasta que se confirma un patrón válido o MACD se vuelve negativo.
3. **Confirmación de patrón**: mientras la ventana está activa, las últimas tres lecturas almacenadas en el búfer MACD deben satisfacer:
   * `macd_curr > macd_last` (el impulso vuelve a subir),
   * `macd_last < macd_last3` (la barra anterior estableció el swing bajo),
   * `macd_curr > Bullish Reset` y `macd_last < Bullish Reset` (el precio rebota desde la zona de retroceso poco profunda).
4. **Ejecución** – cuando se confirma, la estrategia compra en el mercado. Si existe una posición corta, el tamaño de la orden incluye automáticamente el volumen necesario para aplanarse antes de establecer la exposición larga.

### Configuración bajista (entradas cortas)
1. **Condición de armado**: la línea MACD debe caer por debajo de `-Bearish Trigger` (predeterminado `-0.0015`). Cualquier movimiento por encima de cero borra todo estado bajista.
2. **Ventana de retroceso**: una vez armado, el MACD tiene que rebotar por encima de `-Bearish Reset` (predeterminado `-0.0005`).
3. **Confirmación de patrón**: mientras la ventana está abierta, los valores almacenados en el búfer deben cumplir:
   * `macd_curr < macd_last`,
   * `macd_last > macd_last3`,
   * `macd_curr < -Bearish Reset` y `macd_last > -Bearish Reset`.
4. **Ejecución**: se envía una orden de venta de mercado. Si existe una posición larga, su volumen se incluye en la orden, por lo que la cuenta termina netamente corta por el tamaño de operación configurado.

### Gestión de riesgos
* **Stop loss fijo/takeprofit** – las distancias se especifican en puntos (escalones de precio). La estrategia los multiplica por el `PriceStep` del instrumento y llama a `StartProtection` para reproducir el comportamiento SL/TP original. Establecer una distancia en `0` desactiva el nivel respectivo.
* **Una señal por ventana**: después de realizar un pedido, los indicadores de armado y ventana se borran para evitar entradas repetidas del mismo patrón MACD.

## Parámetros
* **Volumen comercial** – volumen de órdenes de mercado. Las posiciones opuestas se cierran automáticamente antes de abrir la nueva operación.
* **EMA rápida / EMA lenta / Señal EMA** – MACD longitudes. Los valores predeterminados replican al asesor original, pero pueden optimizarse.
* **Activador/reinicio alcista**: umbrales MACD positivos (en unidades de indicador) que arman la configuración larga y definen su zona de retroceso.
* **Disparador/reinicio bajista**: umbrales absolutos MACD para la configuración corta. El disparador se aplica con un signo negativo durante el tiempo de ejecución.
* **Stop Loss / Take Profit** – distancias en puntos (escalones de precio). Un valor de `0` deshabilita la protección correspondiente.
* **Tipo de vela**: período de tiempo utilizado para el cálculo de MACD y las decisiones comerciales.

## Notas de implementación
* El API de alto nivel de StockSharp se utiliza en todas partes: `SubscribeCandles` alimenta el indicador y `StartProtection` refleja la gestión comercial MT4.
* El búfer de historial de MACD garantiza que la lógica de decisión funcione en las tres barras terminadas anteriores, coincidiendo con las llamadas `shift=1..3` de MetaTrader.
* No hay una versión Python de esta estrategia en el paquete API, solo la implementación C#.
