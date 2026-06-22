# Estrategia Color JSatl Digit
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia convierte el experto MQL5 "Exp_ColorJSatl_Digit" a StockSharp. Digitaliza la pendiente de la Media Móvil Jurik (JMA) para clasificar cada barra como alcista o bajista. Un cambio del estado 0 a 1 marca una tendencia alcista emergente, mientras que un cambio de 1 a 0 señala una tendencia bajista.

El algoritmo se suscribe a velas de un marco temporal elegido y vincula un indicador JMA. Cuando el JMA gira hacia arriba, la estrategia abre una posición larga y cierra cualquier corta. Cuando el JMA gira hacia abajo, abre una posición corta y cierra cualquier larga. El parámetro opcional `DirectMode` invierte las señales para operar en contra-tendencia.

Las posiciones están protegidas por niveles de stop loss y take profit basados en porcentaje. Todos los parámetros se definen mediante `StrategyParam` y pueden optimizarse.

## Detalles

- **Criterios de entrada**
  - **Largo**: JMA gira hacia arriba (`prev > prevPrev` && `current >= prev`) y `DirectMode` es verdadero. En modo inverso, un giro hacia abajo abre el largo.
  - **Corto**: JMA gira hacia abajo (`prev < prevPrev` && `current <= prev`) y `DirectMode` es verdadero. En modo inverso, un giro hacia arriba abre el corto.
- **Criterios de salida**: La señal opuesta activa una orden de mercado inmediata en la dirección contraria. Las órdenes de protección también pueden cerrar posiciones.
- **Stops**: Stop loss y take profit porcentual mediante `StartProtection`.
- **Valores predeterminados**
  - `JMA Length` = 30
  - `Candle Type` = velas de 4 horas
  - `Stop Loss %` = 1
  - `Take Profit %` = 2
  - `Direct Mode` = true
- **Filtros**
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos (reversible)
  - Indicadores: Jurik Moving Average
  - Stops: Sí
  - Complejidad: Moderado
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Moderado
