# Estrategia Blau Ergodic MDI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Blau Ergodic Market Directional Indicator (MDI) reproduce el comportamiento del asesor experto de MetaTrader `Exp_BlauErgodicMDI`. El algoritmo opera sobre un flujo de velas de un marco temporal superior (por defecto 4H) y aplica un pipeline de triple suavizado al precio de entrada seleccionado para construir un histograma de momentum y una línea de señal. Las decisiones de trading se derivan de ese histograma usando uno de tres modos de entrada configurables:

1. **Breakdown** – opera cuando el histograma cruza la línea cero.
2. **Twist** – reacciona a reversiones en la pendiente del histograma (momentum cambiando de dirección).
3. **CloudTwist** – actúa en cruces del histograma/línea de señal.

Cada señal puede opcionalmente cerrar posiciones opuestas y/o abrir nuevas operaciones dependiendo de los indicadores de permiso proporcionados por el usuario.

## Lógica del indicador
1. Suavizar el precio aplicado elegido con el tipo de media móvil configurado y `PrimaryLength` para obtener el precio base.
2. Calcular la diferencia de momentum `(price - baseline) / point_value`.
3. Suavizar ese momentum con `FirstSmoothingLength` y `SecondSmoothingLength` para construir el histograma.
4. Suavizar el histograma una vez más con `SignalLength` para obtener la línea de señal.
5. Almacenar valores históricos según `SignalBarShift` para que las señales puedan confirmarse en velas cerradas.

Las familias de suavizado soportadas son **EMA**, **SMA**, **SMMA/RMA** y **WMA**. La selección del precio aplicado refleja la implementación de MetaTrader (cierre, apertura, máximo, mínimo, mediana, típico, ponderado, simple, cuarto, variantes de seguimiento de tendencia).

## Parámetros
| Nombre | Descripción |
| ---- | ----------- |
| `Volume` | Tamaño de orden usado al abrir posiciones. |
| `StopLossPoints` | Distancia del stop-loss en puntos del instrumento (0 deshabilita). |
| `TakeProfitPoints` | Distancia del take-profit en puntos del instrumento (0 deshabilita). |
| `SlippagePoints` | Deslizamiento de precio máximo en puntos aplicado a órdenes de mercado. |
| `AllowLongEntries` / `AllowShortEntries` | Permitir abrir posiciones en la dirección respectiva. |
| `AllowLongExits` / `AllowShortExits` | Permitir cerrar posiciones existentes en señales opuestas. |
| `Mode` | Modo de entrada (Breakdown / Twist / CloudTwist). |
| `CandleType` | Marco temporal de velas usadas para cálculos (por defecto 4H). |
| `SmoothingMethods` | Familia de media móvil usada en todos los pasos de suavizado. |
| `PrimaryLength` | Longitud de suavizado base para el precio aplicado. |
| `FirstSmoothingLength` | Primera longitud de suavizado aplicada al momentum. |
| `SecondSmoothingLength` | Segunda longitud de suavizado que forma el histograma. |
| `SignalLength` | Longitud de suavizado del histograma para crear la línea de señal. |
| `AppliedPrices` | Fuente de precio usada en los cálculos del indicador. |
| `SignalBarShift` | Número de barras cerradas a mirar atrás al evaluar señales. |
| `Phase` | Parámetro reservado para compatibilidad (no usado en la implementación actual). |

## Condiciones de señal
* **Breakdown**
  * Largo: el histograma en `SignalBarShift` es positivo mientras la barra anterior no lo es.
  * Corto: el histograma en `SignalBarShift` es negativo mientras la barra anterior no lo es.
* **Twist**
  * Largo: el histograma en `SignalBarShift` está subiendo después de un período bajista (anterior < último y dos barras atrás > anterior).
  * Corto: el histograma en `SignalBarShift` está bajando después de un período alcista (anterior > último y dos barras atrás < anterior).
* **CloudTwist**
  * Largo: el histograma cruza por encima de la línea de señal (último histograma > última señal, histograma anterior <= señal anterior).
  * Corto: el histograma cruza por debajo de la línea de señal.

Cada señal puede tanto aplanar la exposición opuesta (si se permiten salidas) como abrir una nueva operación con el volumen configurado.

## Gestión de riesgos
`StartProtection` se inicializa con las distancias de stop-loss y take-profit especificadas (convertidas de puntos a unidades de precio usando el tamaño de tick del instrumento). Si alguna distancia es cero, la protección respectiva se omite. El deslizamiento también se convierte a unidades de precio usando el mismo tamaño de tick.

## Notas
* Las señales se procesan solo en velas terminadas para reflejar el comportamiento original de MetaTrader.
* `SignalBarShift` permite retrasar la confirmación de operaciones para evitar actuar en la barra más reciente.
* El parámetro `Phase` se mantiene por completitud pero no tiene efecto al usar los métodos de suavizado soportados.
* Todos los comentarios de código se proporcionan en inglés para simplificar el mantenimiento futuro.
