# F-Score Reversal

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Reglas de negociación

Cada mes calcular el retorno de Lookback días: P[d] / P[d−Lookback] − 1. Comprar el peor retorno entre FScore publicados >= FHi; con IsShortEnabled, vender el mejor entre FScore <= FLo. Los límites son inclusivos y FHi debe superar FLo. Usar puntuaciones Piotroski publicadas, enteros 0..9, con el último período/revisión conocido y límite MaxReportAgeDays. Cada lado seleccionado recibe la mitad del presupuesto bruto; un lado ausente queda en efectivo. Liquidar toda la cartera anterior y esperar las ejecuciones antes de abrir las nuevas posiciones.

Se evalúan únicamente velas cerradas sincronizadas. Las estadísticas cuentan fechas UTC distintas; las velas intradía sustituyen el valor de la misma fecha. Se rebalancea una vez en la primera vela conjunta apta de cada mes; los datos incompletos retrasan la decisión. Los empates se ordenan por Security.Id.

Las observaciones deben publicarse antes del cierre de la vela de señal. En intradía solo cuentan fechas UTC anteriores; una vela diaria completa aporta su propia fecha. Los datos CSV futuros no pueden anticipar decisiones.

El límite de publicación es el último CloseTime real entre las velas completas sincronizadas. Una sesión puede terminar antes de que transcurra todo el intervalo configurado.

## Datos de entrada

PriceHistoryFile es un CSV opcional de cierres diarios positivos: SecurityId,Date,Close; fecha yyyy-MM-dd. Las fechas actuales o futuras no precalientan decisiones. Los cierres en vivo sustituyen el valor de su fecha UTC. Se rechazan duplicados. Permite preparar ventanas largas con un archivo de ejecución corto.

FScoreFile es obligatorio: `SecurityId,PeriodEnd,PublishedAt,FScore`. PeriodEnd: `yyyy-MM-dd`; PublishedAt: UTC `yyyy-MM-ddTHH:mm:ssZ`. Solo enteros 0..9, revisiones únicas y publicaciones desde PeriodEnd. Usar el último período/revisión conocido; MaxReportAgeDays cuenta desde PeriodEnd.

## Parámetros

| Parámetro | Predeterminado |
|---|---|
| `Universe` | obligatorio |
| `Capital` | `100000` |
| `Leverage` | `1` |
| `MinTradeUsd` | `100` |
| `CandleType` | `TimeSpan.FromDays(1).TimeFrame()` |
| `PriceHistoryFile` | opcional — sin configurar |
| `Lookback` | `21` |
| `FHi` | `7` |
| `FLo` | `3` |
| `IsShortEnabled` | `true` |
| `MaxReportAgeDays` | `550` |
| `FScoreFile` | obligatorio |

## Ejecución

Universe se configura explícitamente. Los precios y Capital se expresan en USD por unidad negociada. Capital es un presupuesto fijo; Leverage determina la exposición bruta. No se añaden stop loss ni take profit.

Redondear objetivos hacia cero a VolumeStep. Esperar ejecuciones de la liquidación completa antes de abrir nuevas posiciones; conciliar posiciones reales, respetar MinVolume/MaxVolume y dividir ajustes grandes entre velas posteriores. MinTradeUsd filtra ajustes con objetivos no nulos; la liquidación completa omite el filtro.

## Verificación

Ambos lenguajes se prueban con ejecuciones BTC/TON incluidas y fixtures explícitas de tasas, informes, puntuaciones, textos e historial. Las fixtures de calendario trasladan OHLC/volumen y fijan cotizaciones; no son historia de mercado plurianual. Se verifican fórmulas independientes, dirección/tamaño de órdenes, ejecuciones reales, posiciones residuales y fechas prohibidas. Dos grupos, K=1 y AssetDecile=1 son perfiles de mecánica declarados, no prueba del universo completo. Sin promesas de rentabilidad.
