# Estrategia Micro Trend Breakouts
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia **Micro Trend Breakouts** es una conversión del asesor experto de MetaTrader "Micro Trend Breakouts" a la API de alto nivel de StockSharp. Detecta patrones de ruptura de corta duración usando medias móviles ponderadas lineales, picos de momentum y alineación MACD. La estrategia abre como máximo una posición a la vez y se basa en los precios de cierre de las velas para activar entradas y salidas.

## Indicadores
- **Medias móviles ponderadas lineales (LWMA)** - Las medias rápida y lenta construidas sobre el marco temporal de análisis filtran la dirección dominante del mercado.
- **Momentum** - Las lecturas absolutas de momentum de las tres últimas velas completadas deben superar un umbral configurable para confirmar que el precio acelera en la dirección de la ruptura.
- **MACD** - El histograma MACD clásico se usa como filtro direccional (línea principal por encima de la señal para largos y por debajo de la señal para cortos).

## Lógica de entrada
1. Esperar una vela finalizada del marco temporal configurado.
2. Exigir que la LWMA rápida esté por encima de la LWMA lenta para largos (por debajo para cortos).
3. Confirmar una pequeña estructura de ruptura: el mínimo de la vela de hace dos barras debe estar por debajo del máximo de la vela anterior para largos (reflejado para cortos).
4. Exigir aceleración de momentum: cualquiera de los tres últimos valores absolutos de momentum debe superar el umbral configurado.
5. Validar la alineación MACD:
   - Largos: la línea principal MACD debe estar por encima de la línea de señal, independientemente de si está por encima o por debajo de cero.
   - Cortos: la línea principal MACD debe estar por debajo de la línea de señal, independientemente de la posición de la línea cero.

Cuando todas las comprobaciones coinciden, la estrategia emite una orden de mercado usando el parámetro de volumen predeterminado.

## Lógica de salida y gestión de riesgos
- Los niveles iniciales de stop-loss y take-profit se expresan en pasos de precio y se calculan al entrar. Establecer un valor en cero desactiva el nivel correspondiente.
- Un módulo opcional de breakeven mueve el stop hacia el precio de entrada después de que el precio avance una cantidad configurada de pasos, añadiendo opcionalmente un margen de seguridad.
- La protección trailing puede ajustar el stop después de un movimiento rentable. Una vez que la ganancia supera el umbral de activación, el stop se arrastra a la distancia trailing desde el precio de vela más alto (para largos) o más bajo (para cortos) visto desde la entrada.
- Las salidas de posición se evalúan en cada vela finalizada. Si el precio alcanza el nivel de stop-loss o take-profit, la estrategia cierra la posición con una orden de mercado y reinicia el estado interno.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `Order Volume` | Volumen de orden de mercado usado para entradas. | `1` |
| `Candle Type` | Marco temporal para el análisis de precio. | `15m time frame` |
| `Fast LWMA` | Período de la media móvil ponderada lineal rápida. | `6` |
| `Slow LWMA` | Período de la media móvil ponderada lineal lenta. | `85` |
| `Momentum Period` | Período retrospectivo del indicador de momentum. | `14` |
| `Momentum Threshold` | Momentum absoluto mínimo requerido durante las tres últimas velas. | `0.3` |
| `MACD Fast / Slow / Signal` | Períodos de media móvil usados por MACD. | `12 / 26 / 9` |
| `Stop Loss` | Distancia del stop en pasos de precio. `0` desactiva el stop. | `20` |
| `Take Profit` | Distancia del objetivo en pasos de precio. `0` desactiva el objetivo. | `50` |
| `Use Trailing` | Habilita la lógica de trailing stop. | `true` |
| `Trail Activation` | Ganancia en pasos requerida antes de que el trailing stop se active. | `40` |
| `Trail Step` | Distancia entre el extremo y el trailing stop en pasos. | `40` |
| `Use Breakeven` | Habilita el ajuste de stop breakeven. | `true` |
| `Breakeven Trigger` | Ganancia en pasos que arma el módulo breakeven. | `30` |
| `Breakeven Padding` | Pasos adicionales añadidos al mover el stop a breakeven. | `30` |

## Notas
- La estrategia se suscribe a un solo flujo de velas y evita llamadas de API de bajo nivel, manteniéndose dentro de los requisitos del framework de alto nivel.
- Las órdenes de protección no se adjuntan directamente a las operaciones; en su lugar, la estrategia usa supervisión basada en velas combinada con `StartProtection()` para asegurar que la clase base supervise las posiciones abiertas.
- Todos los comentarios inline del código C# están escritos en inglés, como exigen las directrices de conversión.
