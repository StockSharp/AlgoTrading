# Estrategia Exp MA Rounding Candle MMRec
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia Exp MA Rounding Candle MMRec** es el puerto de StockSharp del asesor experto MQL5 `Exp_MA_Rounding_Candle_MMRec`. El sistema original se basa en un indicador personalizado "MA Rounding Candle" que convierte cada vela de mercado en una vela sintética suavizada y rastrea los cambios de color. La versión en C# reproduce el mismo comportamiento reconstruyendo la lógica del indicador al vuelo y reaccionando al flujo de colores resultante.

## Construcción del MA Rounding Candle
1. Cada vela entrante se procesa mediante cuatro medias móviles idénticas (apertura, máximo, mínimo, cierre). Los tipos de suavizado soportados son **Simple**, **Exponential**, **Smoothed (RMA/SMMA)** y **Weighted**.
2. La salida bruta de la media móvil pasa por el filtro de "redondeo" original. El filtro solo acepta un nuevo valor si difiere del anterior en más de `RoundingFactor * PriceStep`. De lo contrario se mantiene el valor redondeado anterior. Esto reproduce el comportamiento de MQL5 donde la señal permanece plana durante pequeñas oscilaciones.
3. Un filtro de gap ancla el open redondeado al close redondeado anterior cuando la diferencia absoluta entre el open y el close reales es menor que `GapSize * PriceStep`. Esto evita que pequeñas velas doji cambien el color de la vela sintética.
4. Tras el redondeo, el color del indicador se define como:
   * `2` – vela sintética alcista (`open < close`)
   * `0` – vela sintética bajista (`open > close`)
   * `1` – vela neutral (`open == close`)

La estrategia almacena solo los últimos valores de color (suficientes para el look-back configurado) y no mantiene historial largo, en línea con el experto original.

## Lógica de señales
Las señales se evalúan en velas terminadas usando un desplazamiento `SignalBar` configurable:

* `SignalBar` denota cuántas velas cerradas hacia atrás deben tratarse como la barra de disparo (`0` = barra cerrada actual, `1` = la barra completamente cerrada más reciente, etc.).
* La estrategia también inspecciona el color de la barra que la precede inmediatamente (`SignalBar + 1`).
* Una transición **alcista a no alcista** (`color[SignalBar + 1] = 2` y `color[SignalBar] != 2`) genera:
  * cierre opcional de posiciones cortas existentes (`EnableShortExits`), y
  * apertura opcional de una nueva posición larga (`EnableLongEntries`).
* Una transición **bajista a no bajista** (`color[SignalBar + 1] = 0` y `color[SignalBar] != 0`) genera:
  * cierre opcional de posiciones largas existentes (`EnableLongExits`), y
  * apertura opcional de una nueva posición corta (`EnableShortEntries`).

La gestión de posiciones sigue el EA original: las salidas se ejecutan antes que las nuevas entradas, y al cambiar de dirección la estrategia añade el valor absoluto de la posición existente al volumen de trading base para que el tamaño neto coincida con la dirección deseada.

## Parámetros
| Parámetro | Predeterminado | Descripción |
|-----------|----------------|-------------|
| `CandleType` | Marco temporal de 1 hora | Serie de velas usada para impulsar la estrategia. |
| `SmoothingMethod` | `Simple` | Tipo de media móvil para todas las series de precios redondeadas. |
| `MaLength` | `12` | Número de períodos usados por la media móvil elegida. |
| `RoundingFactor` | `50` | Multiplicador aplicado al `PriceStep` del instrumento para construir el umbral de redondeo. Valores mayores hacen que la serie redondeada cambie con menos frecuencia. |
| `GapSize` | `10` | Multiplicador aplicado al `PriceStep` para el filtro de gap que bloquea el open redondeado al close redondeado anterior en velas pequeñas. |
| `SignalBar` | `1` | Cuántas velas cerradas hacia atrás se analizan para la señal. |
| `TradeVolume` | `1` | Volumen de posición base usado para nuevas entradas. El parámetro se sincroniza con la propiedad integrada `Strategy.Volume`. |
| `EnableLongEntries` / `EnableShortEntries` | `true` | Activadores para entradas largas/cortas. |
| `EnableLongExits` / `EnableShortExits` | `true` | Activadores para cerrar posiciones existentes. |

## Notas de implementación
* Solo se exponen los modos de suavizado disponibles en StockSharp. Los suavizadores exóticos específicos de MQL5 (JJMA, JurX, VIDYA, AMA, etc.) no están presentes en este puerto.
* El complejo recontador de gestión de dinero del EA original se reemplaza con un único parámetro `TradeVolume`. Esto mantiene la estrategia determinista y más fácil de optimizar dentro de StockSharp.
* Todos los umbrales basados en precio (`RoundingFactor`, `GapSize`) se interpretan en pasos de precio multiplicando el valor por `Security.PriceStep` cada vez que se procesa una vela.
* La estrategia usa la API de suscripción de velas de alto nivel (`SubscribeCandles`) y opera estrictamente en velas completadas, igual que el experto MQL5 que espera `IsNewBar` antes de emitir órdenes.
* La protección larga/corta, los trailing stops y otras salidas se omiten intencionalmente porque no formaban parte de la implementación original.

## Uso
1. Adjunte la estrategia al instrumento deseado y asigne una serie de velas apropiada a través de `CandleType` (p. ej., `TimeSpan.FromHours(1).TimeFrame()`).
2. Configure el método de suavizado, la longitud de la media móvil, el factor de redondeo y el filtro de gap para que coincidan con los ajustes del EA original o sus propios resultados de optimización.
3. Establezca `TradeVolume` al tamaño de lote que planea operar. La estrategia sincroniza automáticamente la propiedad `Volume` interna con este parámetro.
4. Habilite o deshabilite entradas y salidas largas/cortas según el comportamiento deseado.
5. Inicie la estrategia. Se generarán operaciones siempre que el color MA Rounding Candle realice las transiciones configuradas.

El README refleja la implementación en C# contenida en `CS/ExpMaRoundingCandleMmrecStrategy.cs` y debe usarse como documentación de referencia para este puerto.
