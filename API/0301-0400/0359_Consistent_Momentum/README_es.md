# Consistent Momentum

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Reglas de negociación

Calcular momentum largo P[d] / P[d − LookbackDays] − 1 y corto P[d] / P[d − ShortLookbackDays] − 1 con fechas diarias distintas. Exigir LookbackDays + 1 cierres. Ordenar ambas medidas por separado: comprar la intersección de deciles superiores y vender la de inferiores, floor(tamaño / Deciles) candidatos. Crear una tranche mensual con presupuesto bruto Capital × Leverage / HoldingMonths, mitad por lado; lados vacíos quedan en efectivo. Fijar cantidades redondeadas y vencimiento al crearla: mes inicial más HoldingMonths. Negociar solo el cambio de la suma de tranches activas. Cambios posteriores del parámetro no mueven vencimientos existentes.

Se evalúan únicamente velas cerradas sincronizadas. Las estadísticas cuentan fechas UTC distintas; las velas intradía sustituyen el valor de la misma fecha. Se rebalancea una vez en la primera vela conjunta apta de cada mes; los datos incompletos retrasan la decisión. Los empates se ordenan por Security.Id.

Con CandleType intradía se usan solo fechas UTC anteriores a la fecha de señal. Se excluye el día actual incompleto. Una vela diaria cerrada puede aportar su propia fecha completa.

## Datos de entrada

PriceHistoryFile es un CSV opcional de cierres diarios positivos: SecurityId,Date,Close; fecha yyyy-MM-dd. Las fechas actuales o futuras no precalientan decisiones. Los cierres en vivo sustituyen el valor de su fecha UTC. Se rechazan duplicados. Permite preparar ventanas largas con un archivo de ejecución corto.

## Parámetros

| Parámetro | Valor predeterminado |
|---|---|
| `Universe` | obligatorio |
| `Capital` | `100000` |
| `Leverage` | `1` |
| `MinTradeUsd` | `50` |
| `CandleType` | `TimeSpan.FromMinutes(5).TimeFrame()` |
| `Deciles` | `10` |
| `PriceHistoryFile` | opcional — sin configurar |
| `LookbackDays` | `147` |
| `ShortLookbackDays` | `21` |
| `HoldingMonths` | `6` |

## Ejecución

Universe se configura explícitamente. Los precios y Capital se expresan en USD por unidad negociada. Capital es un presupuesto fijo; Leverage determina la exposición bruta. No se añaden stop loss ni take profit.

Se convierte a unidades al cierre de señal y se redondea hacia cero según VolumeStep. Se usan posiciones realmente ejecutadas, se esperan órdenes pendientes y se respetan MinVolume/MaxVolume dividiendo ajustes grandes entre velas posteriores. MinTradeUsd filtra ajustes pequeños, pero no liquidaciones completas.

## Verificación

Las ejecuciones usan únicamente el archivo BTC/TON disponible. Los informes, observaciones de opciones e historial de preparación explícitos son datos de prueba de la mecánica, no fundamentales ni historia de opciones reales. Las ventanas reducidas, dos grupos y marzo como mes anual son perfiles de prueba identificados, no validación de los valores largos originales. No se afirma rentabilidad.
