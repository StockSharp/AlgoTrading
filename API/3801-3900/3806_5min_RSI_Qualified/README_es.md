# 5min RSI Estrategia calificada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia calificada RSI de 5min** es una conversión directa del asesor experto MetaTrader "5min_rsi_qual_01a". El robot original buscaba el agotamiento en velas de cinco minutos utilizando un índice de fuerza relativa de 28 períodos (RSI). Una vez que el oscilador permaneció en una zona extrema durante un número predefinido de barras, el EA abrió una posición contraria y adjuntó un tope dinámico que siguió al cierre de la vela anterior. El puerto StockSharp mantiene la lógica de confirmación exacta, las compensaciones de precios y la restricción de posición única mientras depende de la suscripción de vela de alto nivel API.

Por defecto, la estrategia opera con velas de cinco minutos, pero el parámetro `CandleType` acepta cualquier otro marco de tiempo admitido por el instrumento. Todos los umbrales de los indicadores y las distancias de parada permanecen expresados ​​en MetaTrader "puntos" para que los usuarios puedan volver a aplicar sus configuraciones probadas sin más ajustes.

## Lógica de trading

1. **Cálculo de RSI**: se actualiza un RSI de 28 períodos en cada vela terminada. Solo se procesan velas completas para que coincidan con la referencia MQL4 `Close[1]`.
2. **Contadores de calificación**: dos contadores realizan un seguimiento de cuántas velas consecutivas ha permanecido el RSI por encima del umbral de sobrecompra (`UpperThreshold`) o por debajo del umbral de sobreventa (`LowerThreshold`). Esto refleja el bucle MQL que inspeccionó las últimas 12 barras.
3. **Condiciones de entrada**: cuando no hay ninguna posición abierta y el contador de sobrecompra llega a `QualificationLength`, la estrategia vende al mercado. Por el contrario, cuando el contador de sobreventa alcanza el requisito, compra en el mercado. Esto reproduce el comportamiento del EA de mantener como máximo una operación por símbolo.
4. **Trailing stop**: mientras una posición está activa, el nivel de stop se recalcula en cada vela finalizada utilizando el cierre anterior menos/más `StopLossPoints` convertido a precio absoluto. La parada solo se mueve en la dirección de la operación, exactamente como las llamadas `OrderModify` en el código original.
5. **Parada inicial**: después de cada llenado, la estrategia establece la parada inicial usando `InitialStopPoints`. Si el valor inicial es más ajustado que la distancia de seguimiento, la lógica de seguimiento no lo aflojará, preservando el comportamiento MetaTrader donde la parada inicial podría estar más cerca que la distancia de seguimiento.

## Gestión del riesgo

- Las distancias de parada se definen en MetaTrader puntos para que coincidan con el EA. Se convierten a incrementos de precio absoluto utilizando el `PriceStep` del instrumento (o `MinStep` cuando el paso principal no está disponible).
- La estrategia nunca hace operaciones piramidales. Una nueva posición sólo se abre una vez que la anterior se ha cerrado por completo.
- `StartProtection()` se invoca al inicio para que la infraestructura protectora de StockSharp permanezca sincronizada con los niveles de parada administrados manualmente.

## Parámetros

| Parámetro | Descripción | Predeterminado |
| --- | --- | --- |
| `RsiPeriod` | RSI longitud retrospectiva. | `28` |
| `QualificationLength` | Número de velas consecutivas que RSI debe permanecer en la zona extrema antes de que se confirme una señal. | `12` |
| `UpperThreshold` | RSI nivel que califica una configuración bajista. | `55` |
| `LowerThreshold` | RSI nivel que califica una configuración alcista. | `45` |
| `StopLossPoints` | Distancia del trailing stop en MetaTrader puntos. Convertido a precio absoluto de cada vela. Establezca en `0` para deshabilitar el seguimiento. | `21` |
| `InitialStopPoints` | Distancia de parada de protección inicial en MetaTrader puntos aplicada inmediatamente después de la entrada. Establezca en `0` para omitir la parada inicial. | `11` |
| `CandleType` | Tipo de vela utilizado para la evaluación de la señal (5 minutos por defecto). | `5-minute time frame` |

## Pautas de uso

- Asegúrese de que el paso de precio del instrumento coincida con el tamaño de puntos utilizado durante la optimización de MetaTrader. Para símbolos FX de cinco dígitos, un punto equivale a 0,00010 (un pip), por lo que las distancias predeterminadas reproducen las compensaciones de 11/21 puntos del EA.
- Debido a que el método es contrario, las señales son más confiables en mercados variados. Considere ampliar los umbrales o aumentar `QualificationLength` para los activos de tendencia.
- La estrategia utiliza la propiedad de clase base `Volume` para el tamaño del pedido. Configúrelo en la interfaz de usuario o mediante código antes de iniciar la estrategia.
- La optimización se puede realizar en los umbrales RSI, la duración de la calificación y las distancias de parada gracias a las banderas `SetCanOptimize()`.

## Notas de conversión

- El manejo de velas, el cálculo de RSI y la restricción de una posición reflejan la implementación de MetaTrader. No se introdujeron filtros adicionales.
- El trailing stop actualiza el nivel de stop con el cierre de la vela anterior tal como lo hace la lógica MQL4 `Close[1]`, lo que garantiza que ambas versiones salgan al mismo precio cuando se produzca una reversión.
- Las comprobaciones de errores del script MQL4 (recuento de barras, margen libre) se omiten intencionalmente porque StockSharp maneja internamente la preparación de los datos y la disponibilidad de la cartera.
