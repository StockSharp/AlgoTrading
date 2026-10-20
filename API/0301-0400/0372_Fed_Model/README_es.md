# Fed Model

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Reglas de negociación

Orden de Universe: [ETF de acciones, ETF monetario opcional]. Usar velas diarias y el calendario del primer título. Al cierre del último día bursátil de cada mes, exigir RegressionMonths observaciones mensuales consecutivas publicadas hasta ese cierre. Regresión con constante: y = EarningsYield − BondYield, x = 0..N−1; slope = Σ((x−mean(x))×(y−mean(y))) / Σ(x−mean(x))²; pronóstico = mean(y) + slope×(N−mean(x)). Positivo mantiene acciones; negativo mantiene ETF monetario o efectivo. Cero conserva el régimen anterior. Operar solo al cambiar de régimen, sin redimensionar cada mes. Completar la liquidación anterior antes de comprar.

Las observaciones deben publicarse antes del cierre de la vela de señal. En intradía solo cuentan fechas UTC anteriores; una vela diaria completa aporta su propia fecha. Los datos CSV futuros no pueden anticipar decisiones.

El límite de publicación es el último CloseTime real entre las velas completas sincronizadas. Una sesión puede terminar antes de que transcurra todo el intervalo configurado.

## Datos de entrada

YieldHistoryFile es obligatorio: `Month,PublishedAt,EarningsYield,BondYield`. Month: `yyyy-MM`; PublishedAt: UTC `yyyy-MM-ddTHH:mm:ssZ`. Ambas rentabilidades en iguales unidades. Solo revisiones publicadas hasta el cierre de decisión. Exigir meses consecutivos hasta el mes actual; la ausencia retrasa el pronóstico. Rechazar revisiones duplicadas y publicaciones anteriores al mes observado. Usar datos observados de mercado/fundamentales, no precios inversos.

## Parámetros

| Parámetro | Predeterminado |
|---|---|
| `Universe` | obligatorio |
| `Capital` | `100000` |
| `MinTradeUsd` | `100` |
| `CandleType` | `TimeSpan.FromDays(1).TimeFrame()` |
| `RegressionMonths` | `12` |
| `YieldHistoryFile` | obligatorio |

## Ejecución

Definir Universe explícitamente. Precios y Capital en USD por unidad negociada. Exposición larga limitada a min(Capital, Portfolio.CurrentValue declarado); sin valor positivo no hay presupuesto de compra. Sin apalancamiento, stop-loss ni toma de beneficios.

Redondear objetivos hacia cero a VolumeStep. Esperar ejecuciones de la liquidación completa antes de abrir nuevas posiciones; conciliar posiciones reales, respetar MinVolume/MaxVolume y dividir ajustes grandes entre velas posteriores. MinTradeUsd filtra ajustes con objetivos no nulos; la liquidación completa omite el filtro.

## Verificación

Ambos lenguajes se prueban con ejecuciones BTC/TON incluidas y fixtures explícitas de tasas, informes, puntuaciones, textos e historial. Las fixtures de calendario trasladan OHLC/volumen y fijan cotizaciones; no son historia de mercado plurianual. Se verifican fórmulas independientes, dirección/tamaño de órdenes, ejecuciones reales, posiciones residuales y fechas prohibidas. Dos grupos, K=1 y AssetDecile=1 son perfiles de mecánica declarados, no prueba del universo completo. Sin promesas de rentabilidad.
