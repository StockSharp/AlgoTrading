# Estrategia Morse Code
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La Estrategia Morse Code replica el experto original de MetaTrader 5 que trata cada vela completada como un "guión" o un "punto". Una vela alcista (precio de cierre mayor o igual a la apertura) se codifica como `1`, mientras que una vela bajista (precio de cierre menor o igual a la apertura) se codifica como `0`. La estrategia escanea la última secuencia de velas completadas y la compara con una máscara binaria seleccionada por el usuario. Cuando las últimas velas coinciden exactamente con la secuencia configurada, la estrategia abre una posición en la dirección elegida e inmediatamente adjunta tanto una orden de take-profit como una de stop-loss expresadas en pips.

La implementación se basa exclusivamente en APIs de alto nivel de StockSharp: las suscripciones de velas proporcionan datos, el binding maneja la entrega de eventos, y el módulo de protección integrado gestiona las salidas. No se requieren colecciones personalizadas ni acceso directo a valores de indicadores, manteniendo la lógica concisa y robusta.

## Lógica del patrón
- Las velas se evalúan solo después de que están completamente cerradas (`CandleStates.Finished`).
- Cada vela se convierte en un dígito binario:
  - `1` – la vela es alcista o neutral (`Close >= Open`).
  - `0` – la vela es bajista o neutral (`Close <= Open`). Las velas doji coinciden con ambos dígitos, exactamente como en el experto original.
- La máscara se selecciona de la enumeración `MorsePatternMasks`. Contiene cada secuencia binaria de longitud 1 a 5 que apareció en la versión MT5 (por ejemplo `000`, `1011`, `11111`).
- La estrategia mantiene una ventana deslizante de las velas más recientes. Cuando la ventana más nueva coincide con la máscara, se dispara la señal de entrada.

Este comportamiento refleja la implementación MT5 que llamaba a `CopyRates` y comparaba cada barra con la cadena del patrón carácter por carácter.

## Flujo de trading
1. Suscribirse al tipo de vela configurado y esperar hasta que se acumulen suficientes barras para cubrir la longitud de la máscara.
2. Para cada vela completada:
   - Actualizar las máscaras internas que clasifican la vela como alcista, bajista o neutral.
   - Ignorar las comprobaciones adicionales hasta que se hayan procesado al menos tantas velas como requiere la máscara.
   - Si las últimas velas coinciden exactamente con la máscara seleccionada, verificar la dirección deseada.
   - Enviar una orden de mercado en la dirección de la señal (`BuyMarket` o `SellMarket`). Cuando existe una posición opuesta, la estrategia primero la cierra aumentando el volumen de la orden, reproduciendo el comportamiento del asesor experto original.
3. `StartProtection` adjunta inmediatamente un stop-loss y un take-profit expresados en unidades de precio. Las órdenes protectoras son manejadas por el motor StockSharp usando salidas de mercado para evitar llenados perdidos.

## Parámetros
| Nombre | Valor predeterminado | Descripción |
| --- | --- | --- |
| `CandleType` | Velas de 5 minutos (`TimeSpan.FromMinutes(5).TimeFrame()`) | Tipo de datos usado para construir la secuencia Morse. |
| `Pattern` | `_0` (`"0"`) | Máscara binaria para comparar con las velas más recientes. Los valores provienen directamente de `MorsePatternMasks`. |
| `Direction` | `Sides.Buy` | Si abrir una posición larga (`Buy`) o corta (`Sell`) cuando aparece el patrón. |
| `TakeProfitPips` | `50` | Distancia desde la entrada al take-profit en pips. La estrategia se adapta automáticamente a las cotizaciones forex de 3 y 5 decimales multiplicando el paso de precio por diez. |
| `StopLossPips` | `50` | Distancia desde la entrada al stop-loss en pips, usando el mismo cálculo de pips anterior. |
| `Volume` (propiedad de estrategia) | definido por usuario | Tamaño de la orden en lotes/contratos, equivalente a `InpLot` en el experto MT5. |

Todos los parámetros admiten la ventana de parámetros de StockSharp, pueden optimizarse y pueden cambiarse antes de iniciar la estrategia.

## Gestión de riesgos
- `StartProtection` adjunta ambos objetivos usando desplazamientos basados en precio derivados de la configuración de pips. Las salidas se ejecutan con órdenes de mercado para que el comportamiento coincida con la clase de trading MT5 que establecía valores de stop-loss y take-profit al abrir la posición.
- Dado que la estrategia no hace piramidación, una nueva operación se ignora mientras existe una posición en la misma dirección. Cuando el patrón aparece mientras se mantiene la dirección opuesta, el volumen se aumenta automáticamente para invertir la posición.
- El registro estándar de StockSharp reporta cada entrada al diario de la estrategia.

## Notas de uso
- Las máscaras binarias son intencionalmente cortas (hasta cinco velas) para mantener la lógica fiel a la idea original. Considere combinar múltiples máscaras de patrón a través de la orquestación de cartera si se necesita un vocabulario más rico.
- La conversión de pips depende del paso de precio del instrumento. Para símbolos exóticos con incrementos no estándar puede ajustar `TakeProfitPips` y `StopLossPips` manualmente.
- La estrategia no filtra por hora del día o volatilidad. Puede envolverla dentro de una estrategia padre que maneje sesiones o indicadores adicionales si es necesario.
- Al probar, asegúrese de que la propiedad `Volume` coincida con el tamaño de lote esperado. El probador de StockSharp reutilizará las mismas protecciones y flujo de órdenes que el modo en vivo.

## Referencia de patrones
Ejemplos de valores de enumeración:
- `_0` → `"0"` (vela bajista individual)
- `_5` → `"11"` (dos velas alcistas consecutivas)
- `_20` → `"0110"` (secuencia bajista-alcista formando un zig-zag)
- `_33` → `"00011"` (tres velas bajistas seguidas de dos alcistas)
- `_61` → `"11111"` (cinco velas alcistas consecutivas)

Cualquiera de las 62 máscaras puede seleccionarse desde el panel de parámetros para reproducir exactamente la firma de código Morse requerida por el plan de trading.
