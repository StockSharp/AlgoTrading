# Dollar Carry Trade

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Reglas de negociación

Ordenar cada mes el diferencial anual observado moneda extranjera menos USD. Con IsUsdBase=true, carry = QuoteRate − BaseRate; en otro caso, BaseRate − QuoteRate. Seleccionar K valores inferiores y K superiores; se necesitan al menos 2×K pares elegibles. Mantener USD largo frente al carry bajo y corto frente al alto. Estar largo en USD implica comprar el par si USD es la base y venderlo si USD es la cotizada. Cada lado recibe la mitad del presupuesto bruto en USD. La orientación no puede cambiar entre publicaciones.

Se evalúan únicamente velas cerradas sincronizadas. Las estadísticas cuentan fechas UTC distintas; las velas intradía sustituyen el valor de la misma fecha. Se rebalancea una vez en la primera vela conjunta apta de cada mes; los datos incompletos retrasan la decisión. Los empates se ordenan por Security.Id.

Las observaciones deben publicarse antes del cierre de la vela de señal. En intradía solo cuentan fechas UTC anteriores; una vela diaria completa aporta su propia fecha. Los datos CSV futuros no pueden anticipar decisiones.

El límite de publicación es el último CloseTime real entre las velas completas sincronizadas. Una sesión puede terminar antes de que transcurra todo el intervalo configurado.

## Datos de entrada

RatesFile es CSV obligatorio: `SecurityId,PublishedAt,BaseRate,QuoteRate,UsdPerUnit`. PublishedAt: UTC `yyyy-MM-ddTHH:mm:ssZ`; ambas tasas en iguales unidades decimales anuales. UsdPerUnit es el valor USD positivo de una unidad negociada. Solo vale la última publicación con antigüedad <= MaxRateAgeDays. Se rechazan duplicados de título/publicación. Las tasas no se fabrican con velas.

Añadir la columna booleana obligatoria `IsUsdBase` al encabezado y cada fila (`true` o `false`).

## Parámetros

| Parámetro | Predeterminado |
|---|---|
| `Universe` | obligatorio |
| `Capital` | `100000` |
| `Leverage` | `1` |
| `MinTradeUsd` | `100` |
| `CandleType` | `TimeSpan.FromMinutes(5).TimeFrame()` |
| `K` | `3` |
| `RatesFile` | obligatorio |
| `MaxRateAgeDays` | `45` |

## Ejecución

Capital es presupuesto USD fijo y Leverage controla exposición bruta. Usar RatesFile.UsdPerUnit para cantidades y MinTradeUsd. Redondear hacia cero a VolumeStep, conciliar ejecuciones reales y esperar órdenes pendientes. Respetar MinVolume/MaxVolume y dividir ajustes grandes entre velas posteriores. La liquidación completa omite MinTradeUsd. Sin stop-loss ni toma de beneficios.

## Verificación

Ambos lenguajes se prueban con ejecuciones BTC/TON incluidas y fixtures explícitas de tasas, informes, puntuaciones, textos e historial. Las fixtures de calendario trasladan OHLC/volumen y fijan cotizaciones; no son historia de mercado plurianual. Se verifican fórmulas independientes, dirección/tamaño de órdenes, ejecuciones reales, posiciones residuales y fechas prohibidas. Dos grupos, K=1 y AssetDecile=1 son perfiles de mecánica declarados, no prueba del universo completo. Sin promesas de rentabilidad.
