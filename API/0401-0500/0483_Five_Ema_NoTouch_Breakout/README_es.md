# Five Ema NoTouch Breakout

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Reglas de negociación

Usar velas cerradas con EMA formada. Una vela completamente bajo EMA prepara compra sobre su máximo; sobre EMA prepara corto bajo su mínimo. Comprobar la preparación anterior antes de sustituirla; tocar no rompe. Entrar solo desde cero con Volume y borrar preparación durante la posición. Fijar stop en el extremo opuesto. La primera ejecución real fija riesgo hasta el stop y objetivo a RewardRisk×riesgo. Protección local nativa con órdenes de mercado observa bid/ask entre velas. Si la ejecución rebasa el stop, cerrar de inmediato. Sin espera ni acumulación.

## Parámetros

| Parámetro | Predeterminado |
|---|---|
| `CandleType` | TimeSpan.FromMinutes(5).TimeFrame() |
| `EmaPeriod` | 5 |
| `RewardRisk` | 3 |

## Verificación

Ambos lenguajes se verifican con BTC/TON incluido, fórmulas independientes, dirección/tamaño de órdenes, ejecuciones, posiciones y salidas. Datos fechados de precios/factores y calendarios verifican mecánica; OHLC/volúmenes trasladados no son historia plurianual. Dos grupos, TopN=1 y SMA=2 son perfiles explícitos pequeños. Ventanas largas y valores originales se comprueban aparte. Sin promesas de rentabilidad.
