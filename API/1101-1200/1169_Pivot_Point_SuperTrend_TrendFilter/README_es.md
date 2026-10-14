# Estrategia Pivot Point SuperTrend con Filtro de Tendencia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Combina una línea SuperTrend basada en pivotes con un filtro de tendencia SuperTrend y una confirmación de media móvil. Opera cuando la tendencia cambia o cuando aparece una señal de Pivot SuperTrend dentro de una ventana de fechas.

## Detalles

- **Criterios de entrada**:
  - Largo: el SuperTrend del precio cambia al alza y el cierre supera la SMA de cierres, o el SuperTrend de pivotes cambia al alza dentro del intervalo de fechas.
  - Corto: el SuperTrend del precio cambia a la baja y el cierre queda debajo de la SMA, o el SuperTrend de pivotes cambia a la baja dentro del intervalo de fechas.
- **Criterios de salida**:
  - Cualquier cambio bajista cierra todo el largo; cualquier cambio alcista cierra todo el corto, independientemente de SMA y fechas.
  - Solo se invierte con una entrada contraria confirmada. Cambios simultáneos opuestos cierran la posición sin nueva entrada.
- **Stops**: Ninguno
- **Valores predeterminados**:
  - `PivotPeriod` = 2
  - `Factor` = 3
  - `AtrPeriod` = 10
  - `TrendAtrPeriod` = 10
  - `TrendMultiplier` = 3
  - `MaPeriod` = 20
  - `CandleType` = 5 minutos
  - `StartDate` / `EndDate` = UTC `DateTime.MinValue` / `DateTime.MaxValue`
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Pivot, SuperTrend, SMA
  - Stops: No
  - Complejidad: Moderado
  - Marco temporal: Cualquiera
  - Estacionalidad: Opcional
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

## Cálculo y ejecución

Solo se usan velas terminadas. Un máximo/mínimo pivote debe superar/quedar estrictamente por debajo de todas las `PivotPeriod` velas de cada lado y se confirma al terminar las velas derechas. Si cumple ambas condiciones, tiene prioridad el máximo. El primer pivote inicializa `center`; después se usa `(2 * center + pivot) / 3`.

Las bandas utilizan Wilder ATR: media de los primeros `AtrPeriod` rangos verdaderos y luego suavizado con peso `1 / AtrPeriod`. Soporte/resistencia básicos: `center ± Factor * ATR`. El soporte conserva el máximo del valor básico y anterior cuando el cierre anterior supera el soporte anterior; en otro caso se reinicia. La resistencia conserva el mínimo cuando el cierre anterior queda debajo de la resistencia anterior; en otro caso se reinicia. Un cierre estrictamente superior a la resistencia anterior cambia al alza; inferior al soporte anterior cambia a la baja; en otro caso conserva la dirección. La dirección inicial es alcista; inicializar no genera señal.

El filtro de precio es el SuperTrend nativo de StockSharp (`TrendAtrPeriod`, `TrendMultiplier`). La SMA incluye el cierre actual. Solo los cambios de dirección generan señales; ambos ATR, SMA y centro pivote deben estar listos. `StartDate <= candle.OpenTime <= EndDate` limita solo las entradas pivote. Las fechas deben estar ordenadas y en UTC. Entrada: `Volume`; orden de inversión: `Volume + abs(Position)`; cierre: `abs(Position)`. Sin espera ni stops protectores.

Concepto pivote: [Pivot Point Supertrend de LonesomeTheBlue](https://www.tradingview.com/script/L0AIiLvH-Pivot-Point-Supertrend/). Las reglas anteriores definen el modelo bidireccional de este ejemplo.
