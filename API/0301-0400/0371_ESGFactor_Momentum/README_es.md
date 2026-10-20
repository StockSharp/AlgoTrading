# ESG Factor Momentum

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Reglas de negociación

Universe debe contener títulos previamente filtrados por ESG; no se genera una puntuación ESG. Momentum = P[d] / P[d−LookbackDays] − 1 con LookbackDays + 1 cierres diarios completos. Comprar mensualmente solo al ganador, incluso si todas las rentabilidades son negativas. Liquidar todas las posiciones anteriores y esperar sus ejecuciones antes de comprar, aunque el ganador no cambie. El presupuesto queda limitado por el valor declarado de la cartera; sin apalancamiento ni cortos.

Se evalúan únicamente velas cerradas sincronizadas. Las estadísticas cuentan fechas UTC distintas; las velas intradía sustituyen el valor de la misma fecha. Se rebalancea una vez en la primera vela conjunta apta de cada mes; los datos incompletos retrasan la decisión. Los empates se ordenan por Security.Id.

Las observaciones deben publicarse antes del cierre de la vela de señal. En intradía solo cuentan fechas UTC anteriores; una vela diaria completa aporta su propia fecha. Los datos CSV futuros no pueden anticipar decisiones.

## Datos de entrada

PriceHistoryFile es un CSV opcional de cierres diarios positivos: SecurityId,Date,Close; fecha yyyy-MM-dd. Las fechas actuales o futuras no precalientan decisiones. Los cierres en vivo sustituyen el valor de su fecha UTC. Se rechazan duplicados. Permite preparar ventanas largas con un archivo de ejecución corto.

## Parámetros

| Parámetro | Predeterminado |
|---|---|
| `Universe` | obligatorio |
| `Capital` | `100000` |
| `MinTradeUsd` | `100` |
| `CandleType` | `TimeSpan.FromDays(1).TimeFrame()` |
| `PriceHistoryFile` | opcional — sin configurar |
| `LookbackDays` | `252` |

## Ejecución

Definir Universe explícitamente. Precios y Capital en USD por unidad negociada. Exposición larga limitada a min(Capital, Portfolio.CurrentValue declarado); sin valor positivo no hay presupuesto de compra. Sin apalancamiento, stop-loss ni toma de beneficios.

Redondear objetivos hacia cero a VolumeStep. Esperar ejecuciones de la liquidación completa antes de abrir nuevas posiciones; conciliar posiciones reales, respetar MinVolume/MaxVolume y dividir ajustes grandes entre velas posteriores. MinTradeUsd filtra ajustes con objetivos no nulos; la liquidación completa omite el filtro.

## Verificación

Ambos lenguajes se prueban con ejecuciones BTC/TON incluidas y fixtures explícitas de tasas, informes, puntuaciones, textos e historial. Las fixtures de calendario trasladan OHLC/volumen y fijan cotizaciones; no son historia de mercado plurianual. Se verifican fórmulas independientes, dirección/tamaño de órdenes, ejecuciones reales, posiciones residuales y fechas prohibidas. Dos grupos, K=1 y AssetDecile=1 son perfiles de mecánica declarados, no prueba del universo completo. Sin promesas de rentabilidad.
