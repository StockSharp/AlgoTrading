# Estrategia de Explosión Galáctica
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia de Explosión Galáctica reconstruye el experto de cuadrícula original de MetaTrader 5 en StockSharp. Opera en velas finalizadas, usa una media móvil a largo plazo para definir el sesgo direccional y despliega una cuadrícula expansiva de órdenes. El sistema acumula operaciones mientras el precio permanece en un lado de la media móvil y cierra toda la cesta una vez que se alcanza un objetivo de beneficio predefinido.

## Lógica de mercado
1. **Filtro direccional** – la estrategia compara el último cierre de vela con una media móvil. Cuando el precio cierra por debajo de la media el sesgo se vuelve alcista, y cuando el precio cierra por encima de la media el sesgo se vuelve bajista.
2. **Cuadrícula progresiva** – las primeras ocho entradas se toman cuando el sesgo lo permite. Después de la octava posición, la distancia entre el precio actual y tanto la última como la primera entrada controla si se permiten operaciones adicionales.
3. **Control de espaciado** – las distancias se miden en pasos de precio. Si el precio se ha movido lo suficiente desde la última entrada, la estrategia agregará a la cesta. Dependiendo de la distancia hasta la primera entrada, operará inmediatamente, saltará tres velas, o saltará seis velas antes de agregar de nuevo.
4. **Realización de beneficios** – la PnL realizada más la ganancia abierta de la cesta se compara con el objetivo de beneficio mínimo. Cuando se alcanza el umbral, todas las posiciones abiertas se cierran en una sola orden de mercado.

## Gestión de operaciones
- **Volumen de entrada** – cada operación se ejecuta con el volumen de orden configurado. Cuando la señal cambia mientras se mantiene una posición, la estrategia envía una sola orden que cierra el lado anterior y abre uno nuevo con el volumen extra necesario.
- **Seguimiento de posición** – la estrategia mantiene el precio promedio y el precio de la primera/última entrada para las cestas largas y cortas de forma independiente. Esto le permite reproducir las reglas de escalado basadas en distancia del experto original.
- **Filtro de sesión** – el trading solo está activo entre las horas de inicio y fin configuradas. La lógica usa el tiempo de apertura de la vela e ignora las señales fuera de esta ventana.
- **Verificación de seguridad** – si la ventana de trading está mal configurada (por ejemplo, la hora de inicio no es anterior a la hora de fin), la estrategia omite el trading y registra una advertencia.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| **Order Volume** | Volumen usado para cada nueva entrada. Este valor también se usa para estimar cuántos pasos de cuadrícula están actualmente abiertos. |
| **Start Hour** | Inicio de la sesión de trading en hora de la bolsa. Las señales antes de esta hora son ignoradas. |
| **End Hour** | Fin de la sesión de trading (exclusivo). Las señales después de esta hora son ignoradas. |
| **Minimal Profit** | Beneficio combinado realizado más no realizado que desencadena el cierre de todas las posiciones abiertas. |
| **Indent After 8th** | Distancia mínima (en pasos de precio) desde la entrada más reciente después de ocho operaciones antes de que se pueda abrir otra posición. |
| **Skip 3 Min** | Límite inferior (en pasos de precio) para activar la regla de "saltar tres velas". |
| **Skip 3 Max** | Límite superior (en pasos de precio) que mantiene activa la regla de "saltar tres velas". |
| **Skip 6 Max** | Límite superior (en pasos de precio) que mantiene activa la regla de "saltar seis velas". |
| **MA Length** | Longitud de la media móvil simple que define el sesgo direccional. |
| **Candle Type** | Serie de velas suscrita por la estrategia. La media móvil y la lógica de cuadrícula se ejecutan en este flujo de datos. |

## Notas de implementación
- La estrategia usa `SubscribeCandles` con un indicador `SimpleMovingAverage` y procesa solo velas finalizadas.
- Las estadísticas de posición se mantienen a través de `OnNewMyTrade`, permitiendo un seguimiento preciso de los precios de primera y última entrada así como los precios promedio para cestas abiertas.
- Los umbrales de distancia son escalados por el `PriceStep` del valor, reproduciendo la configuración original basada en pips del experto MT5.
- La implementación evita colecciones personalizadas y se enfoca en variables de estado escalares para cumplir con las directrices del proyecto.
