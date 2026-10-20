# Residual Momentum Factor

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Reglas de negociación

ResidualMomentumFile aporta el impulso residual calculado por una fuente externa. No sustituir residuos por OHLC ni retornos de referencia. Mensualmente ordenar puntuaciones publicadas aptas; comprar floor(n/Decile) líderes y vender igual número de rezagados. Exigir Decile títulos aptos.

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
| `ResidualMomentumFile` | obligatorio |
| `Decile` | 10 |
| `MaxScoreAgeDays` | 400 |

## Datos de entrada

CSV obligatorio: `SecurityId,PeriodEnd,PublishedAt,Score`. PeriodEnd: `yyyy-MM-dd`; PublishedAt: UTC `yyyy-MM-ddTHH:mm:ssZ`; Score decimal. Rechazar revisiones duplicadas o anteriores al fin del período. Elegir el último período y revisión publicada hasta CloseTime real de la vela sincronizada. `MaxScoreAgeDays` desde PeriodEnd. Ausencias, antigüedad o universo insuficiente retrasan revisión conservando posiciones; precios no sustituyen puntuaciones.

## Verificación

Ambos lenguajes se verifican con BTC/TON incluido, fórmulas independientes, dirección/tamaño de órdenes, ejecuciones, posiciones y salidas. Datos fechados de precios/factores y calendarios verifican mecánica; OHLC/volúmenes trasladados no son historia plurianual. Dos grupos, TopN=1 y SMA=2 son perfiles explícitos pequeños. Ventanas largas y valores originales se comprueban aparte. Sin promesas de rentabilidad.
