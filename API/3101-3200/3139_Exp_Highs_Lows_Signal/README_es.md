# Estrategia de Exp Highs Lows Signal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Exp Highs Lows Signal es un port directo del asesor experto MetaTrader 5 `Exp_HighsLowsSignal`. La estrategia depende de un detector de patrones que busca un número configurable de velas consecutivas que imprimen máximos más altos y mínimos más altos (secuencia alcista) o máximos más bajos y mínimos más bajos (secuencia bajista). Una vez encontrada una secuencia, la estrategia retrasa la reacción por el número configurado de barras cerradas, cierra cualquier exposición opuesta y opcionalmente abre una posición en la dirección detectada. Los stops protectores se expresan en pasos de precio para reflejar la gestión de dinero basada en puntos del robot original.

## Lógica de la estrategia
### Detector de secuencia de Highs/Lows
* El detector evalúa cada vela finalizada en el marco temporal seleccionado.
* Una **señal alcista** requiere `SequenceLength` comparaciones consecutivas donde tanto el máximo actual como el mínimo actual son estrictamente mayores que la barra anterior.
* Una **señal bajista** requiere `SequenceLength` comparaciones consecutivas donde tanto el máximo actual como el mínimo actual son estrictamente menores que la barra anterior.
* Las señales se encolan y se liberan después de `SignalBarDelay` velas cerradas, coincidiendo con el ajuste `SignalBar` de la implementación MQL.

### Reglas de entrada
* **Entradas largas**
  * Activadas cuando una secuencia alcista se vuelve activa y `AllowLongEntry` está habilitado.
  * Primero se cierra cualquier posición corta existente (si `AllowShortExit` es verdadero), luego se envía una orden de compra de mercado con volumen `OrderVolume + |Position|` para cubrir cortos y establecer el tamaño largo deseado.
* **Entradas cortas**
  * Activadas cuando una secuencia bajista se vuelve activa y `AllowShortEntry` está habilitado.
  * Primero se cierra cualquier posición larga existente (si `AllowLongExit` es verdadero), luego se envía una orden de venta de mercado con volumen `OrderVolume + |Position|` para cubrir largos y establecer el tamaño corto deseado.

### Reglas de salida
* Una secuencia alcista siempre solicita `AllowShortExit` para cerrar cortos abiertos.
* Una secuencia bajista siempre solicita `AllowLongExit` para cerrar largos abiertos.
* Cuando la bandera relevante está deshabilitada, la exposición opuesta permanece intacta, permitiendo al usuario operar solo en una dirección o ejecutar el detector en modo "solo alertas".

### Gestión de riesgo
* `StopLossTicks` y `TakeProfitTicks` representan distancias en pasos de precio (puntos). Un valor de `0` deshabilita la orden protectora correspondiente, reproduciendo el comportamiento del EA original.
* `StartProtection` convierte esas distancias en desplazamientos de precio absolutos para que todas las entradas de mercado reciban automáticamente órdenes de stop-loss y take-profit correspondientes.

## Parámetros
* **OrderVolume** – volumen base de orden usado cuando se abre una nueva operación.
* **AllowLongEntry / AllowShortEntry** – interruptores que habilitan entradas largas o cortas en sus respectivas señales.
* **AllowLongExit / AllowShortExit** – interruptores que permiten a la estrategia aplanar posiciones opuestas cuando aparece la señal contra-tendencia.
* **StopLossTicks / TakeProfitTicks** – distancias protectoras en pasos de precio; establezca en `0` para deshabilitar.
* **SequenceLength** – número de comparaciones consecutivas requeridas para calificar una secuencia alcista o bajista (equivalente a `HowManyCandles` en MT5).
* **SignalBarDelay** – número de velas cerradas a esperar antes de actuar en una señal (equivalente al input `SignalBar`).
* **CandleType** – marco temporal usado para construir el detector de Highs/Lows (por defecto: velas de 4 horas).

## Notas adicionales
* La estrategia almacena solo la cantidad mínima de historial de velas requerida para el detector, manteniendo el comportamiento idéntico al indicador personalizado de MT5.
* Debido a que toda la gestión de órdenes ocurre a través de `StartProtection`, los backtests y el trading en vivo reciben automáticamente órdenes de stop y take-profit correspondientes sin código adicional.
* Deshabilite las banderas `Allow` correspondientes para convertir la estrategia en un filtro direccional o una herramienta de señalización pura.
* No se proporciona traducción a Python; solo la versión C# está disponible en este paquete.
