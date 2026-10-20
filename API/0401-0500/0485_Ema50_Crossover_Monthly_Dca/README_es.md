# Ema50 Crossover Monthly Dca

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Reglas de negociación

Velas semanales cerradas y EMA de EmaLength cierres formada, iniciada con la media del primer período completo. Cierres aptos desde StartDate. Abonar un DcaAmount en el primer mes apto observado y después por meses transcurridos, incluso durante calentamiento. No inventar décadas de aportes previos al primer dato. Sobre EMA invertir efectivo acumulado con cantidades redondeadas; bajo EMA vender todo el largo real; igualdad mantiene. El saldo sigue ejecuciones brutas y ventas; es un registro de asignación, no depósito ni financiación. Esperar órdenes, respetar límites y retener efectivo sobrante. Sin cortos, RSI, espera ni stops.

## Parámetros

| Parámetro | Predeterminado |
|---|---|
| `CandleType` | TimeSpan.FromMinutes(10080).TimeFrame() |
| `EmaLength` | 50 |
| `DcaAmount` | 100000 |
| `StartDate` | 1980-01-01 |

## Verificación

Ambos lenguajes se verifican con BTC/TON incluido, fórmulas independientes, dirección/tamaño de órdenes, ejecuciones, posiciones y salidas. Datos fechados de precios/factores y calendarios verifican mecánica; OHLC/volúmenes trasladados no son historia plurianual. Dos grupos, TopN=1 y SMA=2 son perfiles explícitos pequeños. Ventanas largas y valores originales se comprueban aparte. Sin promesas de rentabilidad.
