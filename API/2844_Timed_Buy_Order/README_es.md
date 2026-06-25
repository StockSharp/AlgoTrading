# Estrategia de Orden de Compra Temporizada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de Orden de Compra Temporizada** replica el asesor experto de MetaTrader `buy_order.mq4`, que envía un flujo de órdenes de compra a mercado impulsadas por un temporizador de un segundo. El puerto de StockSharp mantiene el mismo ritmo de trading: espera hasta que el temporizador se alinea con el segundo esperado dentro del minuto actual y luego envía la siguiente orden. Después de un número predefinido de ejecuciones, la estrategia se detiene automáticamente.

Esta implementación se basa en el servicio de `Timer` de alto nivel de StockSharp en lugar de bucles manuales. No se requieren indicadores de mercado ni suscripciones de velas, lo que hace la lógica determinista y orientada al tiempo.

## Lógica central
1. Cuando la estrategia comienza, activa la protección de riesgo mediante `StartProtection()` e inicia un temporizador con el intervalo configurado (por defecto: un segundo).
2. Cada callback del temporizador verifica si la estrategia está en línea y puede operar, y si el segundo actual del intercambio coincide con el valor esperado en la secuencia.
3. Si todas las verificaciones son exitosas, la estrategia envía una orden de compra a mercado con el volumen configurado.
4. El proceso se repite hasta que se haya enviado el número objetivo de órdenes, tras lo cual la estrategia se detiene.

El comportamiento de sincronización de segundos refleja el experto MQL original: la primera orden solo se despacha cuando el componente de segundos llega a cero, y cada orden posterior está vinculada al siguiente valor de segundo.

## Parámetros
| Nombre | Tipo | Predeterminado | Descripción |
| ---- | ---- | ------- | ----------- |
| `OrderVolume` | `decimal` | `0.01` | Cantidad para cada orden de compra a mercado. Una guardia de validación detiene la estrategia si el valor no es positivo. |
| `OrdersToPlace` | `int` | `60` | Número total de órdenes de compra secuenciales a enviar antes de detenerse. |
| `Interval` | `TimeSpan` | `1s` | Retraso entre callbacks del temporizador. Mantenerlo en un segundo reproduce mejor el tiempo MQL, pero se pueden usar otros valores para experimentación. |

Todos los parámetros se exponen a través de objetos `StrategyParam<T>` de StockSharp, por lo que pueden optimizarse o configurarse desde herramientas de UI.

## Flujo de ejecución
- **Inicialización** – restablecer los contadores en `OnReseted()` garantiza un estado limpio al reiniciar o re-optimizar.
- **Inicio** – en `OnStarted()` el temporizador comienza y los contadores se reinician; la protección se habilita una vez por ciclo de vida.
- **Tick del temporizador** – el método `OnTimer()` realiza las verificaciones de secuenciación, registra la orden saliente y detiene la estrategia cuando se envía la orden final.
- **Finalización** – el ayudante `CompleteStrategy()` previene intentos de cierre duplicados y llama a `Stop()` exactamente una vez.

## Notas de conversión
- La función MQL `EventSetTimer(1)` se mapea a `Timer.Start(TimeSpan.FromSeconds(1), OnTimer)`.
- Los comentarios de órdenes y números mágicos usados en MetaTrader no tienen equivalentes directos en StockSharp, por lo que se usa registro de log para rastrear el progreso.
- La estrategia mantiene el concepto de "60 órdenes por minuto" haciendo coincidir el componente de segundos en lugar de contar los disparos del temporizador.

## Consejos de uso
1. Asigne el instrumento y portafolio deseados antes de iniciar la estrategia.
2. Ajuste `OrderVolume` para que coincida con el tamaño de lote del instrumento y las reglas del broker.
3. Si necesita menos órdenes, reduzca `OrdersToPlace`; para deshabilitar completamente el ritmo basado en segundos, establezca `Interval` a cualquier valor y elimine la coincidencia de segundos en el código (modificación avanzada).
4. Monitoree la salida del log para rastrear los envíos de órdenes y asegurarse de que la alineación del temporizador se comporte como se espera.

## Limitaciones
- La estrategia solo compra; no hay lógica de salida más allá de la intervención manual o stops de protección gestionados por el broker.
- La colocación de órdenes está limitada por la precisión del servicio de temporizador proporcionado por la conexión y el sistema operativo; grandes retrasos podrían desincronizar la secuencia.

## Archivos
- `CS/TimedBuyOrderStrategy.cs` – implementación principal en C#.
- `README_zh.md` – documentación en chino.
- `README_ru.md` – documentación en ruso.

Un puerto de Python se omite intencionalmente según las instrucciones del proyecto; créelo más tarde si es necesario.
