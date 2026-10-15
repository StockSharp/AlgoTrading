# Estrategia Color JFATL Digit TM
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia Color JFATL Digit TM** es un port del asesor experto original de MetaTrader 5 que combina una FATL (Fast Adaptive Trend Line) filtrada por Jurik con transiciones de estado basadas en color y un filtro de sesión de trading opcional. La estrategia monitorea la pendiente de la línea FATL suavizada: cada barra se clasifica como alcista (color = 2), bajista (color = 0) o neutral (color = 1). Los cambios en estos estados de color desencadenan entradas, salidas y gestión de posiciones respetando las horas de sesión configurables, las distancias de stop-loss y take-profit.

## Lógica
1. **Replicación del indicador personalizado**
   - El valor FATL se calcula convolucionando el precio aplicado seleccionado con la tabla de pesos original de 39 coeficientes.
   - El resultado se suaviza usando `JurikMovingAverage` de StockSharp. Si la biblioteca expone una propiedad `Phase` se configura mediante reflexión para reflejar los parámetros de MT5.
   - El valor suavizado se redondea a la precisión del instrumento multiplicando el paso de precio por `10^DigitRounding`, reproduciendo el parámetro `Digit` de MQL5.
   - La diferencia entre el valor redondeado actual y el anterior define el color de la barra (`2 = subiendo`, `0 = bajando`, `1 = sin cambio / heredado`).

2. **Evaluación de señales**
   - Un buffer circular mantiene los códigos de color más recientes. El parámetro `SignalBar` selecciona cuántas barras completadas saltar (predeterminado = 1, es decir, barra cerrada anterior).
   - Una **entrada larga** se activa cuando el color precedente era alcista (`2`) y el más reciente es cualquier cosa que no sea alcista (`< 2`).
   - Una **entrada corta** se activa cuando el color precedente era bajista (`0`) y el más reciente es cualquier cosa que no sea bajista (`> 0`).
   - Una **salida larga** ocurre cuando el color precedente se vuelve bajista (`0`).
   - Una **salida corta** ocurre cuando el color precedente se vuelve alcista (`2`).
   - Las entradas se omiten cuando ya existe una posición, replicando el comportamiento de posición única del experto MT5.

3. **Control de sesión y protección**
   - El filtrado de sesión opcional (`EnableTimeFilter`) espeja la lógica de hora/minuto de MT5, incluyendo sesiones nocturnas cuando la hora de inicio es mayor que la hora de fin.
   - Cuando el trading está fuera de la ventana permitida todas las posiciones abiertas se liquidan inmediatamente, coincidiendo con el experto original.
   - Las distancias de stop-loss y take-profit expresadas en puntos se convierten en unidades de precio usando el paso de precio del instrumento y se pasan a `StartProtection`.

## Parámetros
- `OrderVolume` – volumen por orden (usado tanto para entradas de compra como de venta).
- `EnableTimeFilter`, `StartHour`, `StartMinute`, `EndHour`, `EndMinute` – configuración de la ventana de sesión.
- `StopLossPoints`, `TakeProfitPoints` – distancias de protección en puntos (0 deshabilita la respectiva leg).
- `BuyOpenEnabled`, `SellOpenEnabled`, `BuyCloseEnabled`, `SellCloseEnabled` – habilitar o deshabilitar entradas y salidas largas/cortas individualmente.
- `SignalCandleType` – marco temporal usado para el indicador personalizado y las señales de trading (predeterminado candles de 4 horas).
- `JmaLength`, `JmaPhase` – configuración de suavizado Jurik (phase aplicada cuando el indicador subyacente la expone).
- `AppliedPriceMode` – enumeración de precio aplicado idéntica a la versión MT5 (cierre, apertura, mediana, típico, variantes TrendFollow, Demark, etc.).
- `DigitRounding` – multiplicador de redondeo que imita el parámetro `Digit` del indicador MQL.
- `SignalBar` – cuántas barras cerradas mirar atrás al evaluar transiciones de color (predeterminado 1).

## Notas
- La estrategia usa `SubscribeCandles` y helpers de orden de alto nivel (`BuyMarket`, `SellMarket`) como recomienda la guía de conversión de StockSharp.
- La phase de Jurik se aplica mediante reflexión; si la implementación en tiempo de ejecución no expone una propiedad `Phase` se usa automáticamente el comportamiento predeterminado.
- El redondeo requiere un `Security.PriceStep` válido. Cuando no está disponible, los valores del indicador permanecen sin redondear.

## Uso
1. Conecte la estrategia a un instrumento y conexión capaz de proporcionar el `SignalCandleType` configurado.
2. Configure el precio aplicado, los parámetros de Jurik, los horarios de sesión y los parámetros de gestión de dinero según se desee.
3. Inicie la estrategia; gestionará una única posición, respetando las protecciones de stop-loss/take-profit y las señales basadas en color descritas anteriormente.
