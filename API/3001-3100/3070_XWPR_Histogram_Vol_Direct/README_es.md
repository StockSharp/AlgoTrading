# Estrategia Exp XWPR Histograma Vol Directo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Esta estrategia es un port de StockSharp del asesor experto de MetaTrader **Exp_XWPR_Histogram_Vol_Direct**. Reproduce el enfoque
original de ponderar los valores de Williams %R por volumen, suavizar el resultado, y abrir operaciones cuando la pendiente del histograma cambia
de color. Las órdenes se activan en velas completamente formadas y usan stop-loss y take-profit protectores opcionales medidos en pasos de precio.

## Lógica central

1. Calcular Williams %R en el marco temporal seleccionado.
2. Desplazar el oscilador en +50, multiplicarlo por la fuente de volumen elegida (tick o real), y suavizar el flujo con una media móvil
   configurable.
3. Suavizar el volumen bruto con la misma media móvil para reconstruir las bandas del indicador (HighLevel2, HighLevel1, LowLevel1, LowLevel2).
4. Rastrear el color de la pendiente del histograma: azul (`0`) cuando el valor suavizado sube, magenta (`1`) cuando cae. La estrategia
   mantiene un buffer de historial corto para comparar los últimos dos colores completados respetando el parámetro `SignalShift`.
5. Ejecutar acciones cuando el color anterior cambia:
   - Transición de color `0 → 1`: cerrar cortos (si está habilitado) y opcionalmente abrir una nueva posición larga.
   - Transición de color `1 → 0`: cerrar largos (si está habilitado) y opcionalmente abrir una nueva posición corta.

La clasificación de zona (Neutral/Alcista/Bajista/Extrema) se registra por contexto pero no bloquea las operaciones, coincidiendo con el comportamiento del
asesor original que solo lee el buffer de color.

## Parámetros

| Parámetro | Descripción |
| --- | --- |
| `WilliamsPeriod` | Longitud de retrospección para Williams %R. |
| `HighLevel2`, `HighLevel1`, `LowLevel1`, `LowLevel2` | Multiplicadores aplicados al volumen suavizado para reconstruir las bandas del indicador. |
| `SmoothingType` | Familia de media móvil usada tanto para el valor ponderado como para los flujos de volumen (SMA, EMA, SMMA, WMA, Hull, VWMA, DEMA, TEMA). |
| `SmoothingLength` | Longitud de la media móvil de suavizado. |
| `SignalShift` | Cuántas barras atrás leer el buffer de color (1 reproduce el predeterminado de MetaTrader). |
| `EnableLongEntries` / `EnableShortEntries` | Permitir o bloquear la apertura de posiciones largas/cortas. |
| `EnableLongExits` / `EnableShortExits` | Permitir o bloquear el cierre de posiciones largas/cortas. |
| `VolumeSource` | Elegir entre conteo de ticks o volumen real para la ponderación. |
| `StopLossPoints` / `TakeProfitPoints` | Objetivos protectores opcionales expresados en pasos de precio. |
| `CandleType` | Tipo de vela y marco temporal usado para análisis y trading. |

Use la propiedad base `Volume` de la estrategia para definir el tamaño de entrada. La reversión de posición se maneja enviando la cantidad absoluta
de posición más el tamaño de lote configurado, similar al asesor experto MQL.

## Notas de uso

- La fase de suavizado (`MA_Phase` en MetaTrader) no es compatible porque las medias móviles de StockSharp no exponen ese parámetro.
- Asegure que haya suficiente historial cargado para el marco temporal elegido para que las medias móviles estén completamente formadas antes de que comience el trading.
- La estrategia funciona en cualquier instrumento compatible con StockSharp; establezca `CandleType` en la resolución deseada (por ejemplo,
  marco temporal de 4 horas para coincidir con los valores predeterminados originales).
- La ponderación por volumen de tick requiere fuentes de datos que proporcionen conteos de tick dentro de los mensajes de vela. De lo contrario, cambie a volumen real.

## Registro y visualización

La estrategia dibuja velas y el indicador Williams %R en el área de gráfico predeterminada. Las acciones de trading registran la zona detectada y el
valor del histograma suavizado para ayudar en la depuración y comparación con la implementación de referencia de MetaTrader.
