# Estrategia Pivot Point Supertrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que combina Pivot Points con un Supertrend basado en ATR para capturar reversiones de tendencia.

Los Pivot Points definen una línea central dinámica. Un multiplicador ATR construye bandas superior e inferior que siguen al precio. Cuando la tendencia cambia de dirección, la estrategia entra en consecuencia.

## Detalles

- **Criterios de entrada**: Señales basadas en Pivot Points y ATR Supertrend.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Señal opuesta.
- **Stops**: No.
- **Valores predeterminados**:
  - `PivotPeriod` = 2
  - `AtrFactor` = 3m
  - `AtrPeriod` = 10
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Pivot Points, ATR
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

## Cálculo y ejecución

Solo se usan velas terminadas. Un máximo/mínimo pivote debe superar/quedar estrictamente por debajo de todas las `PivotPeriod` velas de cada lado y se confirma al terminar las velas derechas. Si cumple ambas condiciones, tiene prioridad el máximo. El primer pivote inicializa `center`; después se usa `(2 * center + pivot) / 3`.

Las bandas utilizan Wilder ATR: media de los primeros `AtrPeriod` rangos verdaderos y luego suavizado con peso `1 / AtrPeriod`. Soporte/resistencia básicos: `center ± AtrFactor * ATR`. El soporte conserva el máximo del valor básico y anterior cuando el cierre anterior supera el soporte anterior; en otro caso se reinicia. La resistencia conserva el mínimo cuando el cierre anterior queda debajo de la resistencia anterior; en otro caso se reinicia. Un cierre estrictamente superior a la resistencia anterior cambia al alza; inferior al soporte anterior cambia a la baja; en otro caso conserva la dirección. La dirección inicial es alcista; inicializar no genera señal.

Wilder ATR y un centro pivote confirmado deben estar listos. Solo un cambio de dirección de las bandas genera entrada: al alza largo, a la baja corto. La dirección inicial es alcista; inicializar no genera orden. Cada señal opuesta invierte toda la posición mediante una orden de mercado de `Volume + abs(Position)`; desde una posición vacía usa `Volume`. No hay SMA, SuperTrend de precio separado, filtro de fechas, espera ni stop protector.

Concepto pivote: [Pivot Point Supertrend de LonesomeTheBlue](https://www.tradingview.com/script/L0AIiLvH-Pivot-Point-Supertrend/). Las reglas anteriores definen el modelo bidireccional de este ejemplo.
