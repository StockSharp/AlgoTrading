# Estrategia AnyRange Cloud Tail System Tm Plus
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia reproduce el comportamiento del asesor experto **Exp_i-AnyRangeCldTail_System_Tm_Plus.mq5** utilizando la API de alto nivel de StockSharp. Construye un rango intradía personalizado entre dos horarios definidos por el usuario, espera rupturas más allá de ese rango y programa órdenes un número configurable de barras después de la ruptura para que las señales estén alineadas con la lógica de temporización MQL original.

La estrategia está diseñada para operaciones largas y cortas. Expone parámetros que controlan los permisos de ruptura, las distancias de stop-loss/take-profit en pasos de precio, el período de mantenimiento y la ventana de cálculo del indicador. Además, una salida basada en tiempo cierra posiciones que permanecen abiertas más tiempo que el número configurado de minutos, coincidiendo con la lógica protectora del asesor experto fuente.

## Lógica de trading

1. **Construcción del rango**
   - Dos marcas de tiempo (`RangeStartTime` y `RangeEndTime`) definen la ventana de sesión utilizada para calcular el rango de referencia.
   - Para cada día completado la estrategia registra el máximo más alto y el mínimo más bajo entre estas marcas de tiempo. Si `RangeStartTime` es mayor que `RangeEndTime`, la ventana automáticamente abarca la medianoche, igual que el indicador original.
   - El rango completado más reciente se reutiliza hasta que se completa un nuevo rango diario.

2. **Detección de ruptura**
   - Cada vela terminada se compara con el rango almacenado.
   - Las velas que cierran por encima del máximo del rango reciben los mismos códigos de color (2 o 3) que el indicador MQL, mientras que las velas que cierran por debajo del mínimo del rango reciben códigos 0 o 1. Las velas dentro del rango se etiquetan con el código 4 (sin señal).
   - El parámetro `SignalBar` desplaza el punto de inspección: la estrategia evalúa la vela que tiene `SignalBar + 1` barras de antigüedad y confirma que la vela más reciente (`SignalBar`) no lleva el mismo color. Esto reproduce la confirmación retrasada utilizada por el EA para activar órdenes una barra después de la vela de ruptura.

3. **Entradas**
   - **Largo**: permitido cuando `AllowBuyEntry` es verdadero y se detecta un color alcista (2 o 3) en la barra de señal mientras la siguiente barra no repite el color de ruptura.
   - **Corto**: permitido cuando `AllowSellEntry` es verdadero y se detecta un color bajista (0 o 1) en la barra de señal mientras la siguiente barra no repite el color de ruptura.
   - Si hay una posición opuesta abierta, su volumen se añade a la nueva orden de mercado para que la posición gire inmediatamente, emulando el comportamiento de las funciones auxiliares en `TradeAlgorithms.mqh`.

4. **Salidas**
   - **Señal opuesta**: si `AllowBuyExit` está habilitado, un color bajista (0 o 1) en la barra de señal cierra posiciones largas. Si `AllowSellExit` está habilitado, un color alcista (2 o 3) cierra posiciones cortas.
   - **Salida por tiempo**: cuando `UseTimeExit` es verdadero, las posiciones se liquidan después de `ExitAfterMinutes` minutos desde la entrada, coincidiendo con el bucle MQL que escanea posiciones y las cierra después de `nTime` minutos.
   - **Stops/Objetivos**: las protecciones opcionales de stop-loss y take-profit se configuran mediante `StopLossPoints` y `TakeProfitPoints`. Los valores se convierten en distancias de precio utilizando el paso de precio del instrumento, reflejando la configuración original basada en puntos.

5. **Controles de riesgo**
   - Las órdenes utilizan el `OrderVolume` configurado (tamaño base expresado en unidades de volumen del instrumento). El tamaño de la orden se aplica en cada llamada `BuyMarket`/`SellMarket` y se ajusta al girar posiciones.
   - El stop-loss y el take-profit son gestionados por el auxiliar integrado `StartProtection`, que registra protecciones OCO justo después de que la estrategia comienza.

## Parámetros

| Parámetro | Descripción | Valor predeterminado |
|-----------|-------------|---------|
| `OrderVolume` | Tamaño base de orden para nuevas posiciones. | `0.1` |
| `AllowBuyEntry` | Permitir entradas largas en rupturas alcistas. | `true` |
| `AllowSellEntry` | Permitir entradas cortas en rupturas bajistas. | `true` |
| `AllowBuyExit` | Cerrar posiciones largas en rupturas bajistas. | `true` |
| `AllowSellExit` | Cerrar posiciones cortas en rupturas alcistas. | `true` |
| `UseTimeExit` | Habilitar la salida basada en tiempo. | `true` |
| `ExitAfterMinutes` | Tiempo de mantenimiento en minutos antes de que se active la salida por tiempo. | `1500` |
| `StopLossPoints` | Distancia de stop-loss en pasos de precio. Usar `0` para deshabilitar. | `1000` |
| `TakeProfitPoints` | Distancia de take-profit en pasos de precio. Usar `0` para deshabilitar. | `2000` |
| `SignalBar` | Número de barras atrás inspeccionadas para la detección de ruptura (coincide con el `SignalBar` de MQL). | `1` |
| `RangeLookbackDays` | Número máximo de sesiones pasadas escaneadas para encontrar un rango completado. Establecer en `0` para usar siempre solo el rango más reciente. | `1` |
| `RangeStartTime` | Inicio de la ventana de construcción del rango (TimeSpan). | `02:00` |
| `RangeEndTime` | Fin de la ventana de construcción del rango (TimeSpan). | `07:00` |
| `CandleType` | Tipo de datos/marco temporal de vela utilizado para los cálculos. | `30 minutos` |

## Notas de implementación

- La clase utiliza `SubscribeCandles` y el pipeline controlado por eventos `WhenNew` para procesar solo velas terminadas, asegurando que las decisiones coincidan con el asesor experto MQL que dependía de comprobaciones `IsNewBar`.
- Los valores del rango se almacenan en estructuras ligeras y el algoritmo evita LINQ sobre colecciones completas para cumplir con las directrices del proyecto.
- La salida por tiempo almacena la marca de tiempo de entrada para la dirección actualmente abierta, reflejando cómo el código fuente iteraba a través de posiciones abiertas.
- El volumen de la orden está sincronizado con la propiedad base `Strategy.Volume` para que la UI de StockSharp refleje el tamaño configurado.
- El código contiene comentarios en inglés que explican cada sección principal para facilitar el mantenimiento y la personalización adicional.

## Consejos de uso

- Asegúrese de que el feed de datos proporcione velas que se alineen con el `CandleType` elegido. La detección de ruptura depende de velas completadas; las barras basadas en ticks o parcialmente formadas no deben procesarse.
- Al operar mercados con diferentes sesiones de trading, ajuste `RangeStartTime` y `RangeEndTime` para cubrir el período de acumulación que mejor coincida con el instrumento subyacente.
- Si el instrumento tiene un paso de precio irregular, verifique la conversión de `StopLossPoints`/`TakeProfitPoints` inspeccionando las órdenes de protección generadas en el gráfico o el registro de órdenes.
- Reduzca `ExitAfterMinutes` cuando opere en marcos temporales más rápidos para evitar mantener posiciones más tiempo del previsto.
