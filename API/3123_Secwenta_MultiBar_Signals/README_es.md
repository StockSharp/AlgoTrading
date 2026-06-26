# Estrategia de Secwenta MultiBar Signals
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Visión general
Esta estrategia es un port de StockSharp del asesor experto de MetaTrader "Secwenta" (ID MQL 22977). El algoritmo escanea velas completadas y cuenta cuántas cerraron alcistas (cierre > apertura) o bajistas (cierre < apertura) dentro de un historial rodante corto. Según la configuración, puede operar en modo solo compra, solo venta o reversión bidireccional. Cuando aparece el número requerido de barras alcistas o bajistas, la estrategia abre o cierra posiciones de mercado usando un volumen fijo que refleja el tamaño de lote original.

## Evaluación de señales
- Solo se procesan las velas terminadas del `CandleType` seleccionado mediante la API de suscripción de alto nivel.
- Para cada vela la estrategia registra si fue alcista, bajista o neutral (doji). El buffer interno mantiene las últimas *N* direcciones, donde *N* es el mayor entre `BullishBarCount` y `BearishBarCount` entre los lados habilitados (compra y/o venta).
- El contador alcista se incrementa cuando una vela cierra por encima de su apertura, mientras el contador bajista se incrementa en cierres por debajo de la apertura. Las velas neutrales no afectan a los contadores.
- Se activa una señal una vez que el contador correspondiente alcanza su umbral configurado dentro de la ventana rodante. Esto reproduce la lógica MQL original que iteraba a través de las barras más recientes hasta encontrar el número solicitado de velas alcistas o bajistas.

## Reglas de trading
1. **Modo solo compra (`UseBuySignals = true`, `UseSellSignals = false`):**
   - Cuando el contador bajista alcanza `BearishBarCount`, cualquier posición larga existente se cierra con una orden de venta de mercado.
   - Cuando el contador alcista alcanza `BullishBarCount` y la estrategia está plana, se abre una nueva posición larga usando `OrderVolume`.
2. **Modo solo venta (`UseBuySignals = false`, `UseSellSignals = true`):**
   - Cuando el contador alcista alcanza `BullishBarCount`, una posición corta abierta se cubre con una orden de compra de mercado.
   - Cuando el contador bajista alcanza `BearishBarCount` y la estrategia está plana, se abre una nueva posición corta usando `OrderVolume`.
3. **Modo reversión (`UseBuySignals = true` y `UseSellSignals = true`):**
   - Un disparador alcista cierra cualquier exposición corta y, si la estrategia no está ya larga, abre una nueva posición larga comprando `OrderVolume` más el tamaño absoluto de la posición corta. Esto imita la secuencia original de cerrar ventas antes de abrir compras.
   - Un disparador bajista cierra cualquier exposición larga y, si la estrategia no está ya corta, abre una nueva posición corta vendiendo `OrderVolume` más el tamaño absoluto de la posición larga.

Todas las operaciones de mercado reutilizan los helpers `BuyMarket` y `SellMarket` de StockSharp, y la estrategia llama a `StartProtection()` para que las protecciones a nivel de cuenta puedan añadirse encima si se desea.

## Parámetros
| Parámetro | Descripción | Por defecto | Notas |
|-----------|-------------|-------------|-------|
| `CandleType` | Tipo de datos de vela (marco temporal) usado para evaluar secuencias. | Marco temporal de 1 hora | Se puede seleccionar cualquier tipo de vela soportado por StockSharp. |
| `OrderVolume` | Volumen base de la orden de mercado que refleja el tamaño de lote MQL. | 1 | Se añade al volumen de cierre al revertir una posición. |
| `UseBuySignals` | Habilita el procesamiento de señales alcistas. | `true` | Cuando está deshabilitado, no se abren nuevos trades largos. |
| `BullishBarCount` | Número de velas alcistas requeridas para activar un evento alcista. | 2 | Debe mantenerse consistente con el umbral de cierre al ejecutar en modo solo compra. |
| `UseSellSignals` | Habilita el procesamiento de señales bajistas. | `false` | Cuando está deshabilitado, no se abren nuevos trades cortos. |
| `BearishBarCount` | Número de velas bajistas requeridas para activar un evento bajista. | 1 | Actúa tanto como umbral de apertura para cortos como umbral de salida para largos. |

## Notas de implementación
- La ventana rodante usa una cola para mantener las últimas direcciones de velas y asegura que los contadores coincidan con el tamaño de la ventana incluso después de cambios de parámetros.
- Solo se procesan velas terminadas para mantenerse fiel al manejo original de eventos "nueva barra".
- Las velas neutrales (doji) dejan los contadores sin cambios, exactamente como en el código MQL.
- Las reversiones se ejecutan con una única orden de mercado que combina el volumen de cierre y apertura, manteniendo cambios de exposición deterministas.
- La longitud del buffer es igual al mayor umbral activo; si un lado está deshabilitado, solo el umbral correspondiente contribuye a la longitud de lookback, coincidiendo con el comportamiento de `CopyRates` en la versión MQL.

## Archivos
- `CS/SecwentaMultiBarSignalsStrategy.cs` – implementación principal en C# construida sobre la API de estrategia de alto nivel de StockSharp.

> **Nota:** No se proporciona traducción a Python para este ID; solo se incluye la versión C# solicitada.
