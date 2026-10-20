# Momentum Asset Growth

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Reglas de negociación

Con el último informe anual publicado y la misma fecha fiscal del año anterior calcular crecimiento = TotalAssets[current] / TotalAssets[previous] − 1. Conservar solo el grupo AssetDecile superior: floor(cantidad financiera elegible / AssetDecile). Dentro de él calcular momentum de MomLook días con MomLook + 1 cierres que terminan antes del primer día del mes de decisión menos SkipMonths meses completos. Comprar el Quintile superior y vender el inferior; tras el filtro exigir al menos Quintile títulos con historial suficiente. Rebalancear mensualmente salvo enero. En enero mantener las posiciones sin nueva asignación ni liquidación forzada.

Se evalúan únicamente velas cerradas sincronizadas. Las estadísticas cuentan fechas UTC distintas; las velas intradía sustituyen el valor de la misma fecha. Se rebalancea una vez en la primera vela conjunta apta de cada mes; los datos incompletos retrasan la decisión. Los empates se ordenan por Security.Id.

Las observaciones deben publicarse antes del cierre de la vela de señal. En intradía solo cuentan fechas UTC anteriores; una vela diaria completa aporta su propia fecha. Los datos CSV futuros no pueden anticipar decisiones.

El límite de publicación es el último CloseTime real entre las velas completas sincronizadas. Una sesión puede terminar antes de que transcurra todo el intervalo configurado.

## Datos de entrada

FundamentalsFile es obligatorio: SecurityId,PeriodEnd,PublishedAt,TotalAssets,NetIncome,OperatingCashFlow,BookEquity,SharesOutstanding. PeriodEnd: yyyy-MM-dd; PublishedAt: yyyy-MM-ddTHH:mm:ssZ (UTC). Activos y acciones deben ser positivos. Se elige el período más reciente y la última revisión publicada antes de la señal. Se rechazan revisiones duplicadas y publicaciones anteriores al cierre del período. No se inventan fundamentales a partir de velas.

PriceHistoryFile es un CSV opcional de cierres diarios positivos: SecurityId,Date,Close; fecha yyyy-MM-dd. Las fechas actuales o futuras no precalientan decisiones. Los cierres en vivo sustituyen el valor de su fecha UTC. Se rechazan duplicados. Permite preparar ventanas largas con un archivo de ejecución corto.

## Parámetros

| Parámetro | Predeterminado |
|---|---|
| `Universe` | obligatorio |
| `Capital` | `100000` |
| `Leverage` | `1` |
| `MinTradeUsd` | `200` |
| `CandleType` | `TimeSpan.FromDays(1).TimeFrame()` |
| `PriceHistoryFile` | opcional — sin configurar |
| `MomLook` | `252` |
| `SkipMonths` | `1` |
| `AssetDecile` | `10` |
| `Quintile` | `5` |
| `MaxReportAgeDays` | `550` |
| `FundamentalsFile` | obligatorio |

## Ejecución

Universe se configura explícitamente. Los precios y Capital se expresan en USD por unidad negociada. Capital es un presupuesto fijo; Leverage determina la exposición bruta. No se añaden stop loss ni take profit.

Cada cesta recibe la mitad de Capital × Leverage, repartida por igual. Se convierte a unidades al cierre de señal y se redondea hacia cero según VolumeStep. Se usan posiciones realmente ejecutadas, se esperan órdenes pendientes y se respetan MinVolume/MaxVolume dividiendo ajustes grandes entre velas posteriores. MinTradeUsd filtra ajustes pequeños, pero no liquidaciones completas.

## Verificación

Ambos lenguajes se prueban con ejecuciones BTC/TON incluidas y fixtures explícitas de tasas, informes, puntuaciones, textos e historial. Las fixtures de calendario trasladan OHLC/volumen y fijan cotizaciones; no son historia de mercado plurianual. Se verifican fórmulas independientes, dirección/tamaño de órdenes, ejecuciones reales, posiciones residuales y fechas prohibidas. Dos grupos, K=1 y AssetDecile=1 son perfiles de mecánica declarados, no prueba del universo completo. Sin promesas de rentabilidad.
