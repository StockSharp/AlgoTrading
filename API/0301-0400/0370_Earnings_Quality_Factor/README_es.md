# Earnings Quality Factor

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Reglas de negociación

Calidad de beneficios = (OperatingCashFlow − NetIncome) / TotalAssets. En la primera vela sincronizada elegible de RebalanceMonth, usar el período fiscal más reciente y su última revisión publicada hasta el cierre. Comprar el grupo Deciles de mayor calidad y vender el de menor calidad: floor(número elegible / Deciles) títulos por lado. Rebalancear una vez al año y mantener las posiciones hasta la siguiente revisión. La antigüedad desde PeriodEnd no puede superar MaxReportAgeDays.

Se rebalancea una vez al año, en RebalanceMonth, en la primera vela cerrada conjunta apta. Los informes publicados después no afectan decisiones anteriores. MaxReportAgeDays se cuenta desde PeriodEnd.

Las observaciones deben publicarse antes del cierre de la vela de señal. En intradía solo cuentan fechas UTC anteriores; una vela diaria completa aporta su propia fecha. Los datos CSV futuros no pueden anticipar decisiones.

El límite de publicación es el último CloseTime real entre las velas completas sincronizadas. Una sesión puede terminar antes de que transcurra todo el intervalo configurado.

## Datos de entrada

FundamentalsFile es obligatorio: SecurityId,PeriodEnd,PublishedAt,TotalAssets,NetIncome,OperatingCashFlow,BookEquity,SharesOutstanding. PeriodEnd: yyyy-MM-dd; PublishedAt: yyyy-MM-ddTHH:mm:ssZ (UTC). Activos y acciones deben ser positivos. Se elige el período más reciente y la última revisión publicada antes de la señal. Se rechazan revisiones duplicadas y publicaciones anteriores al cierre del período. No se inventan fundamentales a partir de velas.

## Parámetros

| Parámetro | Predeterminado |
|---|---|
| `Universe` | obligatorio |
| `Capital` | `100000` |
| `Leverage` | `1` |
| `MinTradeUsd` | `100` |
| `CandleType` | `TimeSpan.FromMinutes(5).TimeFrame()` |
| `Deciles` | `10` |
| `RebalanceMonth` | `7` |
| `MaxReportAgeDays` | `550` |
| `FundamentalsFile` | obligatorio |

## Ejecución

Universe se configura explícitamente. Los precios y Capital se expresan en USD por unidad negociada. Capital es un presupuesto fijo; Leverage determina la exposición bruta. No se añaden stop loss ni take profit.

Cada cesta recibe la mitad de Capital × Leverage, repartida por igual. Se convierte a unidades al cierre de señal y se redondea hacia cero según VolumeStep. Se usan posiciones realmente ejecutadas, se esperan órdenes pendientes y se respetan MinVolume/MaxVolume dividiendo ajustes grandes entre velas posteriores. MinTradeUsd filtra ajustes pequeños, pero no liquidaciones completas.

## Verificación

Ambos lenguajes se prueban con ejecuciones BTC/TON incluidas y fixtures explícitas de tasas, informes, puntuaciones, textos e historial. Las fixtures de calendario trasladan OHLC/volumen y fijan cotizaciones; no son historia de mercado plurianual. Se verifican fórmulas independientes, dirección/tamaño de órdenes, ejecuciones reales, posiciones residuales y fechas prohibidas. Dos grupos, K=1 y AssetDecile=1 son perfiles de mecánica declarados, no prueba del universo completo. Sin promesas de rentabilidad.
