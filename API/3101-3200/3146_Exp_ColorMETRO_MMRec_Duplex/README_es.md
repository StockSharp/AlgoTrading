# Estrategia Exp ColorMETRO MMRec Duplex
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción
Esta estrategia porta el asesor experto de MetaTrader 5 `Exp_ColorMETRO_MMRec_Duplex` a StockSharp. El robot original ejecuta dos módulos independientes del indicador ColorMETRO (uno largo, uno corto) y aplica una superposición MMRec (recálculo de gestión monetaria) que reduce el tamaño de la posición después de pérdidas repetidas. La versión en C# refleja ese comportamiento usando la API de alto nivel de StockSharp para suscripciones de velas y enrutamiento de órdenes.

## Lógica de trading
- Dos indicadores ColorMETRO distintos operan en tipos de velas configurables. El módulo largo solo gestiona exposición larga mientras el módulo corto controla la exposición corta.
- Cada indicador produce un sobre RSI escalonado rápido y lento. La estrategia imita las llamadas `CopyBuffer` de MQL5 almacenando valores históricos e inspeccionando la barra definida por `SignalBar`.
- Se genera una entrada larga cuando la banda rápida cruza **por debajo** de la banda lenta en la barra inspeccionada mientras la barra anterior aún tenía la banda rápida por encima de la banda lenta. Cualquier posición corta abierta se aplana antes de abrir el nuevo largo.
- Las salidas largas ocurren cuando la banda lenta en la barra anterior inspeccionada está por encima de la banda rápida, señalando un régimen bajista en el EA original.
- Las entradas y salidas cortas replican la lógica larga (cruce por encima para entradas, línea rápida por encima de la lenta en la barra anterior para salidas).
- Solo se procesan velas terminadas y el trading se bloquea hasta que el indicador reporta ambas bandas como listas, reproduciendo el período de calentamiento de MetaTrader.

## Gestión monetaria (MMRec)
- `Strategy.Volume` define el tamaño de lote de referencia. Los módulos largo y corto lo multiplican por sus respectivos coeficientes `LongMm`/`ShortMm` al dimensionar nuevas órdenes.
- Después de cada operación completada, la estrategia registra si el resultado fue una pérdida (basado en precios de cierre de velas, igual que el EA que inspecciona operaciones históricas).
- Si las `TotalTrigger` operaciones más recientes de un módulo contienen al menos `LossTrigger` perdedoras, el módulo cambia al multiplicador reducido (`SmallMm`). Una vez que el conteo de pérdidas cae por debajo del umbral, el multiplicador predeterminado se restaura automáticamente.
- Las reversiones de posición primero finalizan el resultado de la operación existente (actualizando los contadores MMRec) antes de dimensionar y abrir la dirección opuesta.

## Notas sobre el indicador
- `ColorMetroMmrecIndicator` es un port fiel del indicador personalizado `ColorMETRO`. Alimenta los mismos sobres rápidos/lentos impulsados por un núcleo RSI con seguimiento de pasos y memoria de tendencia.
- El indicador expone el RSI interno y una bandera de preparación para que la estrategia pueda ignorar valores incompletos exactamente como lo hace la implementación MQL.

## Parámetros
| Grupo | Nombre | Descripción |
| --- | --- | --- |
| Largo | `LongCandleType` | Tipo de vela usado para el módulo ColorMETRO largo. |
| Largo | `LongTotalTrigger` | Número de operaciones largas completadas inspeccionadas al evaluar MMRec. |
| Largo | `LongLossTrigger` | Conteo de pérdidas que activa el multiplicador largo reducido. |
| Largo | `LongSmallMm` | Multiplicador reducido aplicado a operaciones largas tras una racha de pérdidas. |
| Largo | `LongMm` | Multiplicador predeterminado para operaciones largas. |
| Largo | `LongEnableOpen` | Habilita la apertura de posiciones largas. |
| Largo | `LongEnableClose` | Habilita el cierre de posiciones largas. |
| Largo | `LongPeriodRsi` | Longitud RSI usada dentro del indicador ColorMETRO largo. |
| Largo | `LongStepSizeFast` | Tamaño de paso del sobre rápido para el módulo largo. |
| Largo | `LongStepSizeSlow` | Tamaño de paso del sobre lento para el módulo largo. |
| Largo | `LongSignalBar` | Desplazamiento histórico (en barras cerradas) usado al leer valores del indicador. |
| Largo | `LongMagic` | Número mágico MT5 original, conservado como referencia. |
| Largo | `LongStopLossTicks` | Marcador de distancia de stop-loss del EA (no aplicado). |
| Largo | `LongTakeProfitTicks` | Marcador de distancia de take-profit del EA (no aplicado). |
| Largo | `LongDeviationTicks` | Marcador de deslizamiento permitido del EA (no aplicado). |
| Largo | `LongMarginMode` | Indicador de modo MM conservado por compatibilidad (la lógica usa multiplicadores brutos). |
| Corto | `ShortCandleType` | Tipo de vela usado para el módulo ColorMETRO corto. |
| Corto | `ShortTotalTrigger` | Número de operaciones cortas completadas inspeccionadas al evaluar MMRec. |
| Corto | `ShortLossTrigger` | Conteo de pérdidas que activa el multiplicador corto reducido. |
| Corto | `ShortSmallMm` | Multiplicador reducido aplicado a operaciones cortas tras una racha de pérdidas. |
| Corto | `ShortMm` | Multiplicador predeterminado para operaciones cortas. |
| Corto | `ShortEnableOpen` | Habilita la apertura de posiciones cortas. |
| Corto | `ShortEnableClose` | Habilita el cierre de posiciones cortas. |
| Corto | `ShortPeriodRsi` | Longitud RSI usada dentro del indicador ColorMETRO corto. |
| Corto | `ShortStepSizeFast` | Tamaño de paso del sobre rápido para el módulo corto. |
| Corto | `ShortStepSizeSlow` | Tamaño de paso del sobre lento para el módulo corto. |
| Corto | `ShortSignalBar` | Desplazamiento histórico (en barras cerradas) usado al leer valores del indicador. |
| Corto | `ShortMagic` | Número mágico MT5 original, conservado como referencia. |
| Corto | `ShortStopLossTicks` | Marcador de distancia de stop-loss del EA (no aplicado). |
| Corto | `ShortTakeProfitTicks` | Marcador de distancia de take-profit del EA (no aplicado). |
| Corto | `ShortDeviationTicks` | Marcador de deslizamiento permitido del EA (no aplicado). |
| Corto | `ShortMarginMode` | Indicador de modo MM conservado por compatibilidad (la lógica usa multiplicadores brutos). |

## Notas de implementación
- La estrategia usa `SubscribeCandles(...).BindEx(...)` y evita el acceso directo a buffers, alineándose con las pautas de conversión.
- Los stops protectores del EA se dejan solo como parámetros; los usuarios pueden adjuntar `StartProtection` o módulos de riesgo personalizados si es necesario.
- Ambos módulos comparten la misma instancia de instrumento pero mantienen sus propias suscripciones de velas y contadores MMRec, coincidiendo con el diseño dúplex de MetaTrader.
- Todos los comentarios en el código se proporcionan en inglés y la lógica evita usar llamadas API prohibidas como `GetTrades`.

## Aviso
Este port reproduce la estructura lógica del EA original, pero la calidad de ejecución depende del broker conectado, el feed de datos y la configuración de StockSharp. Siempre valide el comportamiento en datos históricos y de demostración antes de operar con capital real.
