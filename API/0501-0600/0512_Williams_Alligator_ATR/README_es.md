# Williams Alligator ATR

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Reglas de negociación

Las tres líneas Alligator suavizan el precio mediano (máximo+mínimo)/2; Length y Shift controlan suavizado y retraso en velas completas. Teeth se calcula y debe estar formado, sin filtro adicional. Con líneas y ATR de Wilder formados, Lips−Jaw de <=0 a >0 abre Volume largo desde cero; de >=0 a <0 cierra todo el largo real. Nunca abre corto. Fijar ATR×AtrMultiplier como distancia de stop local nativo desde ejecución real; bid/ask puede activarlo entre velas. Sin espera, toma de beneficios ni stop ATR móvil.

## Parámetros

| Parámetro | Predeterminado |
|---|---|
| `CandleType` | TimeSpan.FromMinutes(30).TimeFrame() |
| `JawLength` | 13 |
| `TeethLength` | 8 |
| `LipsLength` | 5 |
| `JawShift` | 8 |
| `TeethShift` | 5 |
| `LipsShift` | 3 |
| `AtrPeriod` | 14 |
| `AtrMultiplier` | 2 |

## Verificación

Ambos lenguajes se verifican con BTC/TON incluido, fórmulas independientes, dirección/tamaño de órdenes, ejecuciones, posiciones y salidas. Datos fechados de precios/factores y calendarios verifican mecánica; OHLC/volúmenes trasladados no son historia plurianual. Dos grupos, TopN=1 y SMA=2 son perfiles explícitos pequeños. Ventanas largas y valores originales se comprueban aparte. Sin promesas de rentabilidad.
