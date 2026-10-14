# Premarket Gap MomoTrader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ejemplo de impulso de velas solo largo, con una entrada por fecha de Nueva York. “Gap” significa aumento del cierre respecto al cierre anterior; no exige salto nocturno de apertura ni cruce EMA.

## Entrada y tamaño

Solo velas terminadas, de cinco minutos por defecto. La primera guarda cierre y volumen anteriores. Desde posición cero exige vela estrictamente alcista, rango high-low positivo, volumen total > `MinVolume`, cierre anterior positivo y `100 * (close - previousClose) / previousClose >= MinGainPct`.

`bodyPercent = 100 * (close - open) / (high - low)`: ≥90% solicita 100% de `Volume`; ≥85% y <90% solicita 50%; ≥75% y <85% solicita 25%; <75% no entra. Los límites inferiores son inclusivos.

Si existe `VolumeStep` positivo del instrumento, se redondea hacia abajo a ese paso. Se rechaza tamaño cero o fuera de los límites mínimo/máximo proporcionados. No se aumenta una posición existente.

Con `UseSession=true`, la hora de envío en Nueva York pertenece a `[SessionStart, SessionEnd)`, por defecto 04:00–09:30, respetando horario de verano. Desactivar la sesión mantiene el límite diario. Una entrada enviada consume el permiso de esa fecha incluso si la orden falla después.

## Mantener y salir

La primera vela roja (`close < open`) o con `volume <= previousVolume` cierra toda la posición larga real. Velas alcistas con volumen creciente mantienen; un doji también mantiene si crece el volumen. Las salidas se evalúan fuera de la sesión de entrada, independientemente de ganancia, cuerpo y volumen mínimo. La vela de salida no vuelve a entrar. Una orden pendiente impide duplicados; los datos anteriores se actualizan en cada vela terminada.

No hay indicadores, entradas cortas, espera de seis horas, stop-loss ni take-profit independientes. Otra entrada requiere la siguiente fecha elegible de Nueva York.

## Valores predeterminados

| Parámetro | Valor | Significado |
|---|---:|---|
| `MinGainPct` | 5 | Mínimo inclusivo de ganancia porcentual; no negativo |
| `MinVolume` | 15000 | Mínimo estricto de volumen; no negativo, en unidades de los datos |
| `UseSession` | true | Filtro horario de entrada; propiedad C# `IsUseSession` |
| `SessionStart` / `SessionEnd` | 04:00 / 09:30 | Horas ordenadas del mismo día de Nueva York, inicio incluido y final excluido |
| `CandleType` | 5 minutos | Velas terminadas |

Las pruebas usan el archivo BTC/TON existente y verifican cada orden/posición real, tres tamaños, redondeo de lotes, límite diario, ambas salidas, mantenimientos y cambio primaveral de horario de verano. TON tiene una vela válida con los valores publicados 5%/15000; las pruebas mecánicas BTC ajustan explícitamente los umbrales. No prueban rentabilidad.
