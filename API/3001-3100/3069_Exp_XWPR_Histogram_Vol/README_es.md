# Estrategia Exp XWPR Histograma Vol
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es una conversión en C# del experto de MetaTrader **Exp_XWPR_Histogram_Vol**. Opera en los cambios de color del
indicador personalizado XWPR Histograma Vol, que multiplica el oscilador Williams %R por el volumen de la vela y suaviza el resultado. El
port mantiene el esquema original de gestión de dinero de dos slots (volumen primario y secundario) y reproduce las mismas reglas de
entrada y salida basadas en color usando la API de alto nivel de StockSharp.

El algoritmo procesa solo velas finalizadas. En cada nueva barra, inspecciona el color del histograma un número configurable de barras
en el pasado y reacciona cuando las transiciones de color cruzan los umbrales alcistas o bajistas definidos por el indicador.

## Lógica del indicador
1. Williams %R (`WprPeriod`) se desplaza en +50 y se multiplica por el volumen de vela seleccionado (`VolumeMode`).
2. Tanto el Williams %R ponderado como el volumen bruto pasan por filtros de suavizado idénticos (`SmoothingMethod`,
   `SmoothingLength`, `SmoothingPhase`).
3. Se derivan cuatro niveles dinámicos del volumen suavizado: `HighLevel2`, `HighLevel1`, `LowLevel1` y `LowLevel2`.
4. Los colores del histograma corresponden a las zonas definidas por esos niveles:
   - **0** – histograma por encima de `HighLevel2` (alcista fuerte).
   - **1** – histograma entre `HighLevel1` y `HighLevel2` (alcista moderado).
   - **2** – histograma entre `LowLevel1` y `HighLevel1` (neutral).
   - **3** – histograma entre `LowLevel2` y `LowLevel1` (bajista moderado).
   - **4** – histograma por debajo de `LowLevel2` (bajista fuerte).

## Reglas de señal
La estrategia lee dos colores históricos por evaluación: barra `SignalBar + 1` (más antigua) y barra `SignalBar` (más reciente).

- **Abrir largo primario (volumen = `PrimaryVolume`)** cuando el color de la barra más antigua es `1` y el color de la barra más nueva se mueve a `2`, `3` o
  `4`. El movimiento simultáneamente solicita el cierre de posiciones cortas.
- **Abrir largo secundario (volumen = `SecondaryVolume`)** cuando el color de la barra más antigua es `0` y el color de la barra más nueva se convierte en
  cualquier cosa distinta de `0`. La misma señal también cierra cortos.
- **Abrir corto primario (volumen = `PrimaryVolume`)** cuando el color de la barra más antigua es `3` y el color de la barra más nueva sube a `0`, `1`
  o `2`, mientras también cierra largos.
- **Abrir corto secundario (volumen = `SecondaryVolume`)** cuando el color de la barra más antigua es `4` y el color de la barra más nueva se convierte en
  `0`, `1`, `2` o `3`, nuevamente forzando salidas largas.
- **Cerrar largos** cuando el color más antiguo es `3` o `4` (zona bajista).
- **Cerrar cortos** cuando el color más antiguo es `0` o `1` (zona alcista).

Se mantienen dos slots de posición independientes para cada dirección. Una señal solo activa una orden si el slot correspondiente está
actualmente inactivo y el indicador de entrada relevante (`AllowLongEntry`, `AllowShortEntry`) lo permite.

## Gestión de riesgo
- `StopLossSteps` y `TakeProfitSteps` se traducen a órdenes protectoras de StockSharp a través de `StartProtection`. Los valores se
  expresan en pasos de precio del instrumento.
- `DeviationSteps` se conserva para compatibilidad con la lista de entradas MQL. Las órdenes de mercado de StockSharp no lo usan.

## Parámetros
| Nombre | Descripción |
|--------|-------------|
| `CandleType` | Marco temporal usado para construir las velas suministradas al indicador. |
| `PrimaryVolume`, `SecondaryVolume` | Volúmenes aplicados por los slots de nivel uno y nivel dos. |
| `AllowLongEntry`, `AllowShortEntry` | Habilitar apertura de nuevas posiciones largas o cortas. |
| `AllowLongExit`, `AllowShortExit` | Habilitar cierre de exposición larga o corta cuando aparezcan señales de salida. |
| `StopLossSteps`, `TakeProfitSteps` | Distancias protectoras opcionales en pasos de precio (0 deshabilita la protección respectiva). |
| `DeviationSteps` | Reservado para compatibilidad; no tiene efecto en las órdenes de StockSharp. |
| `SignalBar` | Número de velas cerradas para desplazar la evaluación de señal (0 = última vela finalizada). |
| `WprPeriod` | Período de retrospección para el cálculo de Williams %R. |
| `VolumeMode` | Selecciona entre conteo de ticks (`Tick`) o volumen real (`Real`) en el histograma. |
| `HighLevel2`, `HighLevel1` | Multiplicadores que definen los umbrales alcistas superiores. |
| `LowLevel1`, `LowLevel2` | Multiplicadores que definen los umbrales bajistas inferiores. |
| `SmoothingMethod` | Tipo de media móvil usada tanto para el histograma como para el volumen de referencia. |
| `SmoothingLength` | Longitud de los filtros de suavizado. |
| `SmoothingPhase` | Fase enviada a suavizadores basados en Jurik (ignorada por otros métodos). |

## Notas de uso
- La estrategia opera en un único valor devuelto por `GetWorkingSecurities()` y usa órdenes de mercado para todas las acciones.
- Las señales se evalúan una vez por vela finalizada. El buffer de historial adicional evita órdenes duplicadas en la misma barra.
- Los dos slots de entrada actúan de forma independiente. Deshabilite un slot estableciendo el volumen correspondiente en `0` o deshabilitando el
  indicador `Allow*Entry`.
- La conversión no replica los números mágicos de MetaTrader ni los modos de margen. El dimensionamiento de la cartera está completamente controlado por los
  parámetros `PrimaryVolume` y `SecondaryVolume`.
