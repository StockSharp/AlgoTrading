# Estrategia Exp Skyscraper Fix Duplex
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Exp Skyscraper Fix Duplex es un port del asesor experto MQL5 *Exp_Skyscraper_Fix_Duplex*. La estrategia ejecuta el canal Skyscraper Fix en los lados largo y corto de forma independiente, permitiendo que cada lado use su propio marco temporal, ventana ATR y sensibilidad. Las operaciones largas y cortas pueden por lo tanto reaccionar a diferentes regímenes de mercado mientras comparten la misma lógica de ejecución dentro de StockSharp.

## Lógica del indicador
El indicador personalizado **Skyscraper Fix** reproduce el script original:

- Se calcula un ATR con un período interno fijo de 15 para cada vela finalizada.
- Los valores más altos y más bajos de ATR a través de la ventana `Length` configurable determinan el paso de precio adaptativo.
- Dependiendo del `Mode` seleccionado, se usa el High/Low del bar o el precio de cierre para proyectar niveles de canal superior e inferior a el doble de la distancia del paso.
- El breakout más reciente por encima del nivel superior o por debajo del nivel inferior voltea la tendencia interna y fija el nivel de trailing para que nunca se mueva contra el sesgo actual.
- El cruce de la línea de trailing opuesta produce disparadores discretos de compra o venta (reflejando los buffers de flechas del indicador en MQL).

El indicador expone el nivel superior de trailing, el nivel inferior de trailing, disparadores de entrada y una línea media que se puede representar si se desea.

## Reglas de trading
Las operaciones largas y cortas se evalúan por separado para cada vela finalizada de la suscripción respectiva:

- **Entrada larga** – activada cuando el indicador largo reporta un nuevo nivel de compra. Cualquier exposición corta existente se cubre primero, luego se envía una nueva orden larga de mercado con el volumen configurado.
- **Salida larga** – activada cuando el indicador largo reporta la línea de trailing opuesta. Cualquier posición larga existente se cierra con una venta de mercado.
- **Entrada corta** – activada cuando el indicador corto reporta un nuevo nivel de venta. La exposición larga existente se cierra primero, luego se envía una nueva orden corta de mercado.
- **Salida corta** – activada cuando el indicador corto reporta la línea de trailing opuesta. Cualquier posición corta activa se cubre con una compra de mercado.

Las señales pueden retrasarse con los parámetros `SignalBar` para que la estrategia actúe sobre la vela cerrada más recientemente (`0`) o sobre velas más atrás en la historia (`1` imita la configuración MQL predeterminada).

## Parámetros
- `TradeVolume` – tamaño de la orden para entradas de mercado.
- `EnableLongEntries` / `EnableLongExits` – interruptores para el trading del lado largo.
- `LongCandleType` – serie de velas usada para el indicador largo.
- `LongLength`, `LongKv`, `LongPercentage`, `LongMode`, `LongSignalBar` – configuraciones de Skyscraper Fix para el lado largo.
- `EnableShortEntries` / `EnableShortExits` – interruptores para el trading del lado corto.
- `ShortCandleType` – serie de velas usada para el indicador corto.
- `ShortLength`, `ShortKv`, `ShortPercentage`, `ShortMode`, `ShortSignalBar` – configuraciones de Skyscraper Fix para el lado corto.

## Notas de uso
- La estrategia establece la propiedad global `Volume` desde `TradeVolume`, por lo que las llamadas estándar `BuyMarket()` y `SellMarket()` usan ese tamaño automáticamente.
- Ambas instancias del indicador leen el `PriceStep` del instrumento. Si es cero, el indicador espera silenciosamente hasta que un paso de precio válido esté disponible.
- `StartProtection()` se invoca al inicio para que las protecciones a nivel de plataforma estén activas antes de que se envíe la primera orden.
- No hay una implementación de Python separada; el directorio `PY` se omite intencionalmente según lo solicitado.
