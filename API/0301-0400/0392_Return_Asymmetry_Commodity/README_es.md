# Return Asymmetry Commodity

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Reglas de negociación

Con WindowDays retornos diarios completos r: U = Σmax(r,0), D = Σmax(−r,0). Ordenar por (U−D)/(U+D), cero si U+D=0. Conserva el orden de U/D incluso en ventanas solo alcistas o bajistas sin mínimo artificial del denominador. Es balance de retornos, no asimetría estadística. Comprar TopN líderes y vender TopN rezagados distintos; exigir 2×TopN títulos aptos.

Universe se configura explícitamente. Los precios y Capital se expresan en USD por unidad negociada. Capital es un presupuesto fijo; Leverage determina la exposición bruta. No se añaden stop loss ni take profit.

Se evalúan únicamente velas cerradas sincronizadas. Las estadísticas cuentan fechas UTC distintas; las velas intradía sustituyen el valor de la misma fecha. Se rebalancea una vez en la primera vela conjunta apta de cada mes; los datos incompletos retrasan la decisión. Los empates se ordenan por Security.Id.

Cada cesta recibe la mitad de Capital × Leverage, repartida por igual. Se convierte a unidades al cierre de señal y se redondea hacia cero según VolumeStep. Se usan posiciones realmente ejecutadas, se esperan órdenes pendientes y se respetan MinVolume/MaxVolume dividiendo ajustes grandes entre velas posteriores. MinTradeUsd filtra ajustes pequeños, pero no liquidaciones completas.

## Parámetros

| Parámetro | Predeterminado |
|---|---|
| `Universe` | obligatorio |
| `Capital` | 100000 |
| `Leverage` | 1 |
| `MinTradeUsd` | 200 |
| `CandleType` | TimeSpan.FromMinutes(5).TimeFrame() |
| `PriceHistoryFile` | sin configurar |
| `WindowDays` | 120 |
| `TopN` | 5 |

## Datos de entrada

CSV opcional `PriceHistoryFile`: `SecurityId,Date,Close`, precio positivo, pareja título/fecha única, fecha `yyyy-MM-dd`. Intradía solo fechas UTC anteriores; una vela diaria cerrada aporta su cierre. Filas futuras no anticipan decisiones. Las velas sustituyen su valor diario.

## Verificación

Ambos lenguajes se verifican con BTC/TON incluido, fórmulas independientes, dirección/tamaño de órdenes, ejecuciones, posiciones y salidas. Datos fechados de precios/factores y calendarios verifican mecánica; OHLC/volúmenes trasladados no son historia plurianual. Dos grupos, TopN=1 y SMA=2 son perfiles explícitos pequeños. Ventanas largas y valores originales se comprueban aparte. Sin promesas de rentabilidad.
