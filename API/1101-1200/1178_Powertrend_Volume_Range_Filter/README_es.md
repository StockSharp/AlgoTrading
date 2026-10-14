# Estrategia Powertrend - Filtro de Rango por Volumen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza un filtro de rango ajustado por volumen para definir bandas de precios dinámicas. Las entradas largas ocurren cuando el precio rompe la banda superior durante una tendencia alcista, y las posiciones se cierran cuando el precio cae por debajo de la banda inferior. Los filtros opcionales incluyen ADX, confirmación de tendencia por máximos/mínimos y una VWMA del valor del filtro.

## Cálculo y ejecución

C# y Python usan esta definición causal explícita. En velas terminadas, `d = abs(Close - Close anterior)` y `V = TotalVolume`. Tras `RangePeriod` cambios, `r = RangeMultiplier * sum(d * V) / sum(V)` sobre la ventana que incluye la vela actual, con volumen total positivo. El primer filtro formado `f` es el cierre actual; después limite el `f` anterior al intervalo `[Close - r, Close + r]`. Las bandas de decisión son `f anterior + r` y `f anterior - r`, antes de actualizar el centro; así la banda no se redefine como el propio precio de ruptura.

Entre con `Volume` solo desde cero cuando `Close > banda superior` y el filtro sube. Cierre todo el largo a mercado cuando `Close < banda inferior`, independientemente de los filtros de entrada. La igualdad mantiene la posición. Una orden pendiente bloquea órdenes adicionales. No hay cortos, cruce EMA ni espera obligatoria.

El ADX opcional debe estar formado y superar estrictamente `AdxThreshold`; desactivado no se registra ni impone calentamiento. La confirmación de máximos/mínimos requiere a la vez `High actual > High anterior` y `Low actual > Low anterior`. La VWMA del filtro es `sum(f * V) / sum(V)` de los últimos `VwmaPeriod` valores formados, incluido el actual; exige ventana completa, volumen total positivo y `Close > VWMA`. Filtros desactivados no requieren formación. Son decisiones de mercado al cierre, no órdenes stop de bolsa ni ejecución intrabar supuesta en máximos/mínimos.

Valores iniciales: `RangePeriod=40`, `RangeMultiplier=1.5`, `AdxPeriod=14`, `AdxThreshold=20`, `VwmaPeriod=20`, `CandleType=TimeSpan.FromMinutes(5).TimeFrame()`. `IsAdxFilterEnabled`, `IsHighLowFilterEnabled`, `IsVwmaFilterEnabled` son `false`. Períodos y multiplicador positivos; umbral ADX en `[0, 100]`. Fórmula y valores concretan este ejemplo; la descripción original no definía la fórmula del indicador original.
