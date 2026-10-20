# Accrual Anomaly

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Reglas de negociación

Ordenar informes anuales por (NetIncome − OperatingCashFlow) / TotalAssets. Comprar el grupo inferior de Deciles y vender el superior. Cada extremo contiene floor(cantidad elegible / Deciles) valores; se necesitan al menos Deciles informes. El mes anual predeterminado es mayo.

Se rebalancea una vez al año, en RebalanceMonth, en la primera vela cerrada conjunta apta. Los informes publicados después no afectan decisiones anteriores. MaxReportAgeDays se cuenta desde PeriodEnd.

## Datos de entrada

FundamentalsFile es obligatorio: SecurityId,PeriodEnd,PublishedAt,TotalAssets,NetIncome,OperatingCashFlow,BookEquity,SharesOutstanding. PeriodEnd: yyyy-MM-dd; PublishedAt: yyyy-MM-ddTHH:mm:ssZ (UTC). Activos y acciones deben ser positivos. Se elige el período más reciente y la última revisión publicada antes de la señal. Se rechazan revisiones duplicadas y publicaciones anteriores al cierre del período. No se inventan fundamentales a partir de velas.

## Parámetros

| Parámetro | Valor predeterminado |
|---|---|
| `Universe` | obligatorio |
| `Capital` | `100000` |
| `Leverage` | `1` |
| `MinTradeUsd` | `50` |
| `CandleType` | `TimeSpan.FromMinutes(5).TimeFrame()` |
| `Deciles` | `10` |
| `FundamentalsFile` | obligatorio |
| `RebalanceMonth` | `5` |
| `MaxReportAgeDays` | `550` |

## Ejecución

Universe se configura explícitamente. Los precios y Capital se expresan en USD por unidad negociada. Capital es un presupuesto fijo; Leverage determina la exposición bruta. No se añaden stop loss ni take profit.

Cada cesta recibe la mitad de Capital × Leverage, repartida por igual. Se convierte a unidades al cierre de señal y se redondea hacia cero según VolumeStep. Se usan posiciones realmente ejecutadas, se esperan órdenes pendientes y se respetan MinVolume/MaxVolume dividiendo ajustes grandes entre velas posteriores. MinTradeUsd filtra ajustes pequeños, pero no liquidaciones completas.

## Verificación

Las ejecuciones usan únicamente el archivo BTC/TON disponible. Los informes, observaciones de opciones e historial de preparación explícitos son datos de prueba de la mecánica, no fundamentales ni historia de opciones reales. Las ventanas reducidas, dos grupos y marzo como mes anual son perfiles de prueba identificados, no validación de los valores largos originales. No se afirma rentabilidad.
