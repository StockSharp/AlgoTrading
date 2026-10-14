# PresentTrend RMI Synergy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

RMI nativo alineado con SMA del cierre, seguido de stop de precio SMA/ATR que solo se ajusta a favor de la protección. No se afirma rentabilidad.

## Indicadores y entrada

Velas terminadas de cinco minutos por defecto. RMI nativo usa `d[t]=close[t]-close[t-RmiMomentum]`, lag3. En los últimos `RmiPeriod=21` impulsos, U es la media de d positivos (cero en los demás), D la media de valores absolutos de d negativos (cero en los demás). `RMI=100*U/(U+D)`; denominador cero no produce entrada. Es el cálculo nativo de impulso relativo con medias móviles.

SMA del cierre y ATR Wilder nativo comparten `SuperTrendLength=5`. ATR incluye saltos al cierre anterior, comienza con media creciente y después Wilder. Tras formar los tres indicadores, solo desde cero: largo si RMI>60 y close>SMA; corto si RMI<40 y close<SMA. Orden de mercado por `Volume`. Una posición abierta ignora nuevas señales de entrada.

`Direction` es un filtro tipado opcional `Sides`: `null` en C#/`None` en Python permite ambos; `Sides.Buy` solo largos, `Sides.Sell` solo cortos. Solo filtra entradas; el stop siempre cierra el lado real. Sin códigos de dirección numéricos o cadenas.

## Stop dinámico

`width=SuperTrendMultiplier*ATR`, multiplicador4 por defecto. Largo inicializa stop=`SMA-width` y cada vela posterior usa `max(previousStop,SMA-width)`; corto inicia `SMA+width` y usa `min(previousStop,SMA+width)`. Nunca se afloja. Nace de SMA/ATR de la señal, independientemente del precio ejecutado; los nombres SuperTrend conservados configuran estas bandas centradas en SMA.

Bid positivo≤stop largo o ask positivo≥stop corto cierra toda la posición real a mercado. Tras actualizar el stop también se comprueba close; si alcanza el nuevo nivel, sale inmediatamente. Cotizaciones entre velas pueden activarlo. El nuevo nivel se aplica al precio observado actual/futuro, no al high/low anterior de la vela. SMA/ATR puede seguir actualizando la protección con RMI vacío.

Es monitoreo local; protección intravela por cotizaciones requiere Level1 bid/ask, y sin ellas quedan los cierres. No envía stop nativo al mercado; la ejecución puede diferir del umbral. Una orden pendiente bloquea duplicados, pero indicadores/stop avanzan. La vela de salida no reentra; una posterior elegible sí. Sin EMA, espera de seis horas, inversión inmediata por señal opuesta ni take-profit separado.

| Parámetro | Valor |
|---|---:|
| `RmiPeriod` | 21, positivo |
| `RmiMomentum` | 3, lag positivo del cierre |
| `SuperTrendLength` | 5, período SMA/ATR positivo compartido |
| `SuperTrendMultiplier` | 4, anchura ATR positiva |
| `Direction` | Sin valor: ambos |
| `CandleType` | 5 minutos |

Pruebas C# y Python calculan independientemente sumas de impulso, SMA, ATR Wilder y stops en el archivo BTC/TON. Comprueban cada entrada, salida a precio ejecutable y posición real, ambos lados y filtros, señales opuestas mantenidas, stops ajustados entre velas y entradas con menos de seis horas de separación.
