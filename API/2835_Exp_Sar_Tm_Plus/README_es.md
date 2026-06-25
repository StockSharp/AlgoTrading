# Exp Sar Tm Plus Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Puerto de alto nivel de StockSharp del asesor experto **Exp_Sar_Tm_Plus**. La estrategia monitorea los reversiones del Parabolic SAR en un marco temporal configurable y replica las funciones originales de gestión de dinero y tiempo de espera mientras mantiene la lógica compatible con la API de alto nivel de StockSharp.

## Lógica de trading

- Las velas se suscriben desde el parámetro `CandleType` (predeterminado: marco temporal de 4 horas). El indicador Parabolic SAR se calcula con los coeficientes `SarStep` y `SarMaximum` definidos por el usuario.
- Para cada vela finalizada el algoritmo almacena precios de cierre y valores de SAR. El parámetro `SignalBar` selecciona qué vela cerrada se evalúa (predeterminado: la última barra cerrada) y la compara con la vela anterior para detectar un cambio en la dirección del SAR.
- Una posición **larga** se abre cuando el precio cruza **por encima** del SAR (vela anterior por debajo del SAR, vela seleccionada por encima del SAR) y el trading largo está habilitado. La exposición corta existente se cierra automáticamente antes de cambiar de dirección.
- Una posición **corta** se abre cuando el precio cruza **por debajo** del SAR (vela anterior por encima del SAR, vela seleccionada por debajo del SAR) y el trading corto está habilitado. La exposición larga existente se cierra primero.
- Las posiciones se cierran cuando el SAR se mueve en su contra (`AllowLongExit` / `AllowShortExit`), cuando se violan los niveles opcionales de stop-loss / take-profit, o cuando expira el tiempo máximo de mantenimiento (`UseTimeExit` + `HoldingMinutes`).
- Los niveles de stop-loss y take-profit se recalculan en cada entrada usando el `PriceStep` del instrumento. Ambos niveles son opcionales y se ignoran cuando el valor correspondiente es cero.

## Parámetros

- `MoneyManagement` – fracción del `Volume` base que se operará en cada entrada. Los valores ≤ 0 vuelven al valor `Volume` simple. Se normaliza al `VolumeStep` del instrumento.
- `ManagementMode` – enumeración preservada del experto original. Todos los modos actualmente se comportan como `Lot` (volumen fijo) dentro de este puerto.
- `StopLossPoints` / `TakeProfitPoints` – distancia en pasos de precio usada para establecer niveles de protección alrededor del precio de entrada. Establecer en cero para deshabilitar.
- `DeviationPoints` – configuración original de deslizamiento. Se mantiene por completitud, pero la API de alto nivel ejecuta órdenes de mercado sin usar este valor.
- `AllowLongEntry`, `AllowShortEntry` – interruptores para abrir posiciones largas/cortas.
- `AllowLongExit`, `AllowShortExit` – interruptores para cerrar posiciones cuando el precio cruza el SAR en la dirección opuesta.
- `UseTimeExit` – habilita la liquidación de posición después de `HoldingMinutes` minutos en el mercado.
- `HoldingMinutes` – duración para la ventana de salida basada en tiempo.
- `CandleType` – tipo de datos de velas para el análisis SAR.
- `SarStep`, `SarMaximum` – configuración del Parabolic SAR.
- `SignalBar` – número de velas cerradas para desplazar la evaluación de la señal (0 = vela finalizada actual, 1 = anterior, etc.).

## Gestión de riesgo y notas

- La estrategia invoca `StartProtection()` al inicio, habilitando los servicios de protección integrados de StockSharp.
- Las salidas basadas en tiempo dependen del `CloseTime` del candle (respaldo a `OpenTime` si no está disponible) para medir el período de mantenimiento con precisión.
- Solo se mantiene una posición neta en cualquier momento. Los reversals de posición cierran automáticamente el lado opuesto antes de entrar en una nueva operación.
- La implementación mantiene el conjunto de parámetros del experto MQL5 original. Algunas opciones (como los modos de gestión de dinero no-`Lot` o los `DeviationPoints` de órdenes) son marcadores de posición porque la API de alto nivel abstrae la mecánica del lado del broker.
