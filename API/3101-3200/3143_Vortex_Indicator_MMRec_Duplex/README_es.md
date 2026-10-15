# Estrategia Vortex Indicator MMRec Duplex
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Resumen
- Convertido del experto MetaTrader 5 **Exp_VortexIndicator_MMRec_Duplex.mq5** (MQL ID 23180).
- Mantiene dos flujos independientes del indicador Vortex: uno dedicado a operaciones largas y otro a operaciones cortas. Cada flujo tiene su propio marco temporal, longitud y desplazamiento de barra para que la lógica alcista y bajista pueda ajustarse de forma independiente.
- Replica el módulo de gestión monetaria de recuperación "MMRec" del EA original. La estrategia rastrea los últimos resultados de operaciones por dirección y cambia temporalmente a un tamaño de orden reducido tras un número configurable de pérdidas.

## Lógica de señal
1. Suscribirse al tipo de vela configurado para cada flujo y calcular el indicador Vortex (`VI+` y `VI-`).
2. **Entradas largas:** cuando la barra anterior tenía `VI+` por debajo o igual a `VI-` y la barra actual cierra con `VI+` por encima de `VI-` (cruce alcista). Las entradas solo se permiten si `AllowLongEntries` está activado.
3. **Salidas largas:** cuando `VI-` sube por encima de `VI+` en la barra evaluada, siempre que `AllowLongExits` esté activado.
4. **Entradas cortas:** cuando la barra anterior tenía `VI+` por encima o igual a `VI-` y la barra actual cierra con `VI+` por debajo de `VI-` (cruce bajista), controlado por `AllowShortEntries`.
5. **Salidas cortas:** cuando `VI+` sube de nuevo por encima de `VI-` en la barra evaluada, controlado por `AllowShortExits`.
6. Cada dirección mantiene sus propios niveles de stop-loss y take-profit medidos en pasos de precio. Alcanzar cualquiera de ellos cierra inmediatamente la posición y registra el resultado para los contadores de recuperación.

## Recuperación de gestión monetaria
- El EA original inspecciona una ventana deslizante de operaciones pasadas para decidir si la próxima orden debe usar el volumen normal o el reducido. Este port replica el mismo comportamiento.
- Para operaciones largas, la cola almacena hasta `LongTotalTrigger` resultados de PnL más recientes. Si al menos `LongLossTrigger` de ellos son operaciones perdedoras, la próxima entrada larga usa `LongSmallMoneyManagement`; de lo contrario usa `LongMoneyManagement`.
- Las operaciones cortas repiten la misma lógica con `ShortTotalTrigger`, `ShortLossTrigger`, `ShortSmallMoneyManagement` y `ShortMoneyManagement`.
- Cuando los valores de activación son cero, las colas se borran y siempre se usa el volumen base.

## Modos de margen
`MarginModeOption` describe cómo el valor de gestión monetaria se convierte en un volumen ejecutable:
- **FreeMargin (0):** tratar el valor como una fracción del capital (aproximación del modo "margen libre" original).
- **Balance (1):** idéntico a `FreeMargin` en este port; usa el valor actual de la cartera.
- **LossFreeMargin (2):** arriesgar una fracción del capital usando la distancia de stop-loss configurada. Recurre al dimensionamiento basado en precio si la distancia del stop es cero.
- **LossBalance (3):** igual que `LossFreeMargin` en esta implementación.
- **Lot (4):** interpretar el valor directamente como volumen de orden.

Todos los tamaños calculados se normalizan usando el paso de volumen del instrumento, así como las restricciones de volumen mínimo y máximo.

## Parámetros
| Parámetro | Predeterminado | Descripción |
| --- | --- | --- |
| `LongCandleType` | H4 | Marco temporal usado para el indicador Vortex del lado largo. |
| `ShortCandleType` | H4 | Marco temporal usado para el indicador Vortex del lado corto. |
| `LongLength` | 14 | Período del indicador Vortex para señales largas. |
| `ShortLength` | 14 | Período del indicador Vortex para señales cortas. |
| `LongSignalBar` | 1 | Desplazamiento de barra cerrada evaluado para cruces largos (0 = última barra cerrada). |
| `ShortSignalBar` | 1 | Desplazamiento de barra cerrada evaluado para cruces cortos. |
| `AllowLongEntries` | true | Habilitar entradas largas cuando aparece el cruce alcista. |
| `AllowLongExits` | true | Habilitar cierre de posiciones largas cuando `VI-` domina a `VI+`. |
| `AllowShortEntries` | true | Habilitar entradas cortas cuando aparece el cruce bajista. |
| `AllowShortExits` | true | Habilitar cierre de posiciones cortas cuando `VI+` domina a `VI-`. |
| `LongTotalTrigger` | 5 | Número de operaciones largas recientes inspeccionadas por el contador de recuperación. |
| `LongLossTrigger` | 3 | Operaciones largas perdedoras requeridas antes de cambiar al volumen largo reducido. |
| `LongMoneyManagement` | 0.1 | Valor base de gestión monetaria para operaciones largas. |
| `LongSmallMoneyManagement` | 0.01 | Valor reducido de gestión monetaria tras una racha perdedora larga. |
| `LongMarginMode` | Lot | Interpretación del valor de gestión monetaria largo (ver modos anteriores). |
| `LongStopLossSteps` | 1000 | Distancia protectora por debajo de la entrada larga expresada en pasos de precio. |
| `LongTakeProfitSteps` | 2000 | Distancia de take-profit por encima de la entrada larga expresada en pasos de precio. |
| `LongSlippageSteps` | 10 | Tolerancia de deslizamiento informativa para órdenes largas (no usada para dimensionamiento). |
| `ShortTotalTrigger` | 5 | Número de operaciones cortas recientes inspeccionadas por el contador de recuperación. |
| `ShortLossTrigger` | 3 | Operaciones cortas perdedoras requeridas antes de cambiar al volumen corto reducido. |
| `ShortMoneyManagement` | 0.1 | Valor base de gestión monetaria para operaciones cortas. |
| `ShortSmallMoneyManagement` | 0.01 | Valor reducido de gestión monetaria tras una racha perdedora corta. |
| `ShortMarginMode` | Lot | Interpretación del valor de gestión monetaria corto. |
| `ShortStopLossSteps` | 1000 | Distancia protectora por encima de la entrada corta expresada en pasos de precio. |
| `ShortTakeProfitSteps` | 2000 | Distancia de take-profit por debajo de la entrada corta expresada en pasos de precio. |
| `ShortSlippageSteps` | 10 | Tolerancia de deslizamiento informativa para órdenes cortas. |

## Notas de implementación
- Construido completamente sobre la API de alto nivel de StockSharp. Las suscripciones de velas impulsan los indicadores Vortex a través de `Bind`, que entrega barras terminadas antes de tomar cualquier decisión.
- La lógica de recuperación de operaciones almacena series de beneficios por dirección en colas y replica las funciones `BuyTradeMMRecounterS` / `SellTradeMMRecounterS` de MetaTrader.
- Los niveles de stop-loss y take-profit se recalculan en unidades de precio (paso de precio del instrumento × pasos configurados) y se aplican en cada vela entrante.
- Los volúmenes de orden se normalizan mediante las restricciones de `VolumeStep`, `MinVolume` y `MaxVolume` del instrumento para evitar envíos inválidos.
- Los parámetros de deslizamiento se conservan por motivos de documentación, pero no son utilizados directamente por los manejadores de órdenes de StockSharp.
