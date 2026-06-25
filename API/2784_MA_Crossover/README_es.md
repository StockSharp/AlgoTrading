# Estrategia de Cruce de MA en Múltiples Marcos Temporales
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia reproduce la idea del asesor experto original **MA Crossover** para MetaTrader 4. Compara dos medias móviles que pueden provenir de diferentes marcos temporales. Un cruce alcista (MA rápida por encima de MA lenta) abre una posición larga, mientras que un cruce bajista abre una posición corta. Filtros opcionales controlan la dirección de operación permitida, el horario de trading activo y un guardián de equity. La lógica interna de stop-loss, take-profit y trailing emula las salidas "ocultas" de la versión MQL.

## Lógica de trading

1. Suscribirse a dos flujos de velas (marcos temporales actuales y anteriores) y calcular el tipo seleccionado de medias móviles.
2. Aplicar los desplazamientos de barra configurados a los valores de la media móvil antes de compararlos.
3. Ignorar velas no completadas y esperar a que ambas medias móviles estén formadas.
4. Omitir el trading fuera de la ventana de día/hora configurada o cuando se activa el guardián de equity.
5. En un cruce alcista:
   - Opcionalmente cerrar una posición corta si `ClosePositionsOnCross = true`.
   - Abrir una posición larga si el trading largo está permitido.
6. En un cruce bajista:
   - Opcionalmente cerrar una posición larga si `ClosePositionsOnCross = true`.
   - Abrir una posición corta si el trading corto está permitido.
7. Gestionar la posición abierta con reglas de stop-loss, take-profit y trailing expresadas como porcentajes del precio de entrada.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `AllowedDirection` | Filtro de dirección de operación (`LongOnly`, `ShortOnly`, `LongAndShort`). |
| `ClosePositionsOnCross` | Cerrar la posición opuesta cuando aparece un cruce antes de abrir una nueva operación. |
| `MaType` | Tipo de cálculo de media móvil (`Simple`, `Exponential`, `Smoothed`, `Weighted`). |
| `CurrentMaPeriod` | Período para la media móvil rápida. |
| `PreviousPeriodAddition` | Longitud extra añadida a la media móvil lenta (`PreviousMaPeriod = CurrentMaPeriod + addition`). |
| `CurrentShift` / `PreviousShift` | Número de barras completadas usadas para desplazar los valores de la media móvil hacia atrás. |
| `CurrentCandleType` / `PreviousCandleType` | Datos de vela para las medias móviles rápidas y lentas. |
| `StopLossPercent` | Distancia de stop-loss en porcentaje del precio de entrada (salida oculta). |
| `TrailingStopPercent` | Distancia de trailing stop en porcentaje basado en el mejor precio alcanzado. |
| `TakeProfitPercent` | Distancia de take-profit en porcentaje del precio de entrada (salida oculta). |
| `StartDay` / `EndDay` | Filtro de día de semana para la actividad de trading. |
| `StartTime` / `EndTime` | Ventana de tiempo intradía para abrir nuevas operaciones. |
| `ClosePositionsOnMinEquity` | Cerrar todas las posiciones cuando se activa el guardián de equity. |
| `MinimumEquityPercent` | Porcentaje mínimo del valor inicial del portafolio permitido por el guardián de equity. |

## Gestión de riesgo

- La estrategia calcula los niveles de stop-loss, take-profit y trailing internamente y sale mediante órdenes de mercado, imitando la lógica de protección oculta del script MQL.
- `MinimumEquityPercent` almacena el valor inicial del portafolio al inicio y puede desencadenar un aplanamiento forzado si el equity cae por debajo del umbral.
- El tamaño de la posición se controla a través de la propiedad base `Strategy.Volume`. El volumen predeterminado se establece en `1`.

## Notas de uso

- La estrategia requiere datos de velas para ambos marcos temporales configurados. Asegúrese de que los conectores asociados soporten los marcos temporales solicitados.
- Cuando ambas medias móviles usan el mismo marco temporal, la estrategia aún se suscribe a dos flujos para mantener la lógica simétrica.
- Dado que las salidas por stop y take-profit se ejecutan mediante órdenes de mercado, no quedan órdenes de protección en el libro de órdenes.
- Los parámetros se corresponden con las entradas principales del asesor experto MQL original, mientras que las características de gestión de riesgo/margen que dependen de funciones específicas del broker (cobertura, promediado) se omiten intencionalmente.

## Diferencias con la versión MQL

- Las características de promediado (`Average_Up`, `Average_Down`) y los ajustes de cobertura no están implementados para mantener la lógica compatible con la API de alto nivel de StockSharp.
- El guardián de equity usa el valor del portafolio de StockSharp en lugar de cálculos específicos de margen libre.
- Las salidas por riesgo se ejecutan mediante órdenes de mercado en eventos de cierre de vela y son, por tanto, siempre ocultas del libro de órdenes.
