# Estrategia Blau C-Momentum
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es un port de StockSharp del asesor experto de MetaTrader **Exp_BlauCMomentum**. Opera en un único instrumento usando velas de un marco temporal configurable e interpreta el Momentum triple suavizado de Blau en uno de dos modos:

* **Modo Breakdown** – reacciona al cruce de la línea de momentum por el nivel cero.
* **Modo Twist** – reacciona a los cambios en la dirección de la pendiente del momentum suavizado.

El indicador se calcula en un marco temporal externo y puede opcionalmente usar diferentes precios aplicados para el cálculo del momentum. Las posiciones se abren con órdenes de mercado y pueden protegerse usando módulos integrados de stop-loss y take-profit.

## Cómo funciona
1. Suscribirse a velas del marco temporal seleccionado.
2. Calcular Blau C-Momentum:
   * El momentum crudo es la diferencia entre dos precios aplicados separados por `MomentumLength` barras.
   * El momentum crudo se suaviza tres veces por el método de media móvil elegido y se escala a pasos de precio (×100/Point).
3. Almacenar el historial del indicador suavizado para los desplazamientos de barra definidos por `SignalBar`.
4. Generar señales:
   * **Breakdown** – si la barra anterior estaba por encima de cero y la barra de señal está por debajo o igual a cero, abrir/invertir largo; si la barra anterior estaba por debajo de cero y la barra de señal está por encima o igual a cero, abrir/invertir corto. Los indicadores de salida opcionales cierran el lado opuesto cuando la barra anterior cruza la línea cero.
   * **Twist** – comparar dos barras anteriores; cuando el momentum acelera hacia arriba (anterior &lt; más antiguo) y la barra de señal confirma, abrir/invertir largo; cuando el momentum acelera hacia abajo (anterior &gt; más antiguo) y la barra de señal confirma, abrir/invertir corto. Los indicadores de salida opcionales cierran el lado opuesto en la misma condición.
5. Usar `MoneyManagement` y `MarginModes` para dimensionar la posición. Los valores negativos significan volumen fijo; los valores positivos arriesgan o asignan una fracción del valor de la cartera. Un bloqueo de tiempo simple previene reentradas inmediatas dentro de la misma vela.

## Parámetros
| Grupo | Nombre | Descripción |
|-------|------|-------------|
| Trading | `MoneyManagement` | Porcentaje del capital para el dimensionamiento de posición. Valor negativo = volumen fijo. |
| Trading | `MarginModes` | Interpretación de la gestión monetaria (`FreeMarginShare`, `BalanceShare`, `FreeMarginRisk`, `BalanceRisk`). Los modos de riesgo usan la distancia de stop-loss y `StepPrice`. |
| Riesgo | `StopLossPoints` | Distancia de stop-loss en pasos de precio del instrumento (poner `0` para deshabilitar). |
| Riesgo | `TakeProfitPoints` | Distancia de take-profit en pasos de precio del instrumento (poner `0` para deshabilitar). |
| Trading | `SlippagePoints` | Deslizamiento permitido (mantenido por compatibilidad, no usado para colocación de órdenes). |
| Trading | `EnableLongEntry`, `EnableShortEntry` | Permitir la apertura de posiciones largas/cortas. |
| Trading | `EnableLongExit`, `EnableShortExit` | Permitir el cierre de posiciones existentes según el indicador. |
| Lógica | `EntryModes` | `Breakdown` o `Twist`. |
| Datos | `CandleType` | Marco temporal usado para los cálculos del indicador (predeterminado 4h). |
| Indicador | `SmoothingMethod` | Método de media móvil: `Simple`, `Exponential`, `Smoothed`, `LinearWeighted`, `Jurik`, `TripleExponential`, `Adaptive`. |
| Indicador | `MomentumLength` | Profundidad de promediado del momentum crudo (barras entre los dos valores de precio). |
| Indicador | `FirstSmoothLength`, `SecondSmoothLength`, `ThirdSmoothLength` | Longitudes de las tres etapas de suavizado. |
| Indicador | `Phase` | Parámetro de fase de Jurik (usado cuando el método de suavizado es `Jurik`). |
| Indicador | `PriceForClose`, `PriceForOpen` | Precios aplicados usados para el momentum (ver comentarios del código para las fórmulas). |
| Lógica | `SignalBar` | Índice de barra usado para señales (0 = barra cerrada actual, 1 = barra anterior, etc.). |

## Notas de uso
* Adjunte la estrategia a un instrumento y configure la serie de velas. El marco temporal de trading es el mismo que el marco temporal del indicador.
* El módulo de protección de la API de alto nivel se activa automáticamente cuando los valores de stop/take profit son positivos.
* Los modos de margen son aproximaciones porque StockSharp no expone el balance/margen libre al estilo MetaTrader. Los modos basados en riesgo dependen de `StopLossPoints` y `Security.StepPrice`.
* Los métodos de suavizado avanzados de la biblioteca original (Parabolic, VIDYA, JurX) se mapean a los indicadores StockSharp disponibles más cercanos (`TripleExponential` ≈ T3, `Adaptive` ≈ KAMA).
* El parámetro de deslizamiento se conserva por completitud, pero se usan órdenes de mercado, por lo que el valor es informativo.

## Primeros pasos
1. Configure la conexión, la cartera y el instrumento en su entorno StockSharp.
2. Cree una instancia de `BlauCMomentumStrategy`, asigne `Security`, `Portfolio` y los parámetros deseados.
3. Llame a `Start()`; la estrategia se suscribirá a las velas, calculará el indicador y operará automáticamente.
4. Monitoree los registros para obtener información sobre posiciones abiertas/cerradas y estados del indicador.

## Descargo de responsabilidad de riesgo
Esta estrategia se proporciona con fines educativos. Siempre valide el rendimiento con pruebas históricas y prospectivas antes de ejecutarla en una cuenta en vivo. Ajuste la configuración de riesgo para que coincida con su capital y las condiciones del mercado.
