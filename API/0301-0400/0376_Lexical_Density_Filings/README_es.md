# Lexical Density Filings

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Reglas de negociación

Leer el último informe de texto publicado por cada título. Los tokens son secuencias Unicode de letras/marcas, normalizadas con reglas invariantes .NET. Densidad léxica = tokens normalizados distintos / tokens totales. Revisar solo las primeras tres fechas bursátiles de febrero, mayo, agosto y noviembre según el calendario del primer título. Esperar dentro de esa ventana hasta disponer de Quintile informes; de lo contrario, esperar al siguiente mes de revisión. Comprar el grupo de mayor densidad y vender el menor, floor(cantidad elegible / Quintile) por lado. Mantener entre revisiones.

Las observaciones deben publicarse antes del cierre de la vela de señal. En intradía solo cuentan fechas UTC anteriores; una vela diaria completa aporta su propia fecha. Los datos CSV futuros no pueden anticipar decisiones.

El límite de publicación es el último CloseTime real entre las velas completas sincronizadas. Una sesión puede terminar antes de que transcurra todo el intervalo configurado.

## Datos de entrada

FilingsFile es obligatorio: `SecurityId,PeriodEnd,PublishedAt,DocumentPath`. Formatos: `yyyy-MM-dd` y UTC `yyyy-MM-ddTHH:mm:ssZ`. DocumentPath señala texto UTF-8, absoluto o relativo al manifiesto; sin comas en las rutas. Rechazar documentos vacíos, revisiones duplicadas y publicaciones anteriores a PeriodEnd. Usar el último período/revisión publicado antes del cierre y límite MaxReportAgeDays. El primer título requiere calendario bursátil con festivos.

Los tokens usan elementos de texto Unicode cuya categoría inicial es letra o marca combinante; puntuación y dígitos separan palabras. Normalización de mayúsculas invariante, sin lematización ni normalización de acentos.

## Parámetros

| Parámetro | Predeterminado |
|---|---|
| `Universe` | obligatorio |
| `Capital` | `100000` |
| `Leverage` | `1` |
| `MinTradeUsd` | `200` |
| `CandleType` | `TimeSpan.FromDays(1).TimeFrame()` |
| `Quintile` | `5` |
| `MaxReportAgeDays` | `550` |
| `FilingsFile` | obligatorio |

## Ejecución

Universe se configura explícitamente. Los precios y Capital se expresan en USD por unidad negociada. Capital es un presupuesto fijo; Leverage determina la exposición bruta. No se añaden stop loss ni take profit.

Cada cesta recibe la mitad de Capital × Leverage, repartida por igual. Se convierte a unidades al cierre de señal y se redondea hacia cero según VolumeStep. Se usan posiciones realmente ejecutadas, se esperan órdenes pendientes y se respetan MinVolume/MaxVolume dividiendo ajustes grandes entre velas posteriores. MinTradeUsd filtra ajustes pequeños, pero no liquidaciones completas.

## Verificación

Ambos lenguajes se prueban con ejecuciones BTC/TON incluidas y fixtures explícitas de tasas, informes, puntuaciones, textos e historial. Las fixtures de calendario trasladan OHLC/volumen y fijan cotizaciones; no son historia de mercado plurianual. Se verifican fórmulas independientes, dirección/tamaño de órdenes, ejecuciones reales, posiciones residuales y fechas prohibidas. Dos grupos, K=1 y AssetDecile=1 son perfiles de mecánica declarados, no prueba del universo completo. Sin promesas de rentabilidad.
