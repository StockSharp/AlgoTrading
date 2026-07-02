# Plantilla M5 Sobres Estrategia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Convertido del MetaTrader 4 asesor experto "Template_M5_Envelopes.mq4". La estrategia rastrea una envolvente de promedio móvil ponderado lineal (LWMA) en velas de cinco minutos y órdenes de parada de ruptura de brazos siempre que el precio se aleja lo suficiente del canal. Las órdenes pendientes se modifican dinámicamente para seguir el mercado, y las posiciones ocupadas están protegidas por una lógica configurable de stop-loss, take-profit y trailing-stop.

## Lógica comercial

1. Una LWMA basada en el precio medio de la vela se calcula con el `EnvelopePeriod` configurado. Las bandas de envolvente superior e inferior se obtienen aplicando el porcentaje `EnvelopeDeviation`.
2. Cada vela terminada de cinco minutos almacena sus valores de envolvente junto con los máximos y mínimos. Las señales solo se evalúan una vez que está disponible un conjunto completo de valores "anteriores", que coincidan con la implementación MetaTrader que hace referencia a `iEnvelopes(..., shift = 1)` y la barra anterior.
3. Aparece una configuración de **compra** cuando:
   * El mínimo de la vela anterior se sitúa al menos `DistancePoints` por debajo del sobre inferior anterior, y
   * El precio de oferta actual permanece al menos `DistancePoints` por debajo del mismo valor del sobre.
4. Una configuración de **venta** refleja la lógica con el máximo anterior y el sobre superior.
5. Cuando una configuración está activa, solo se permite una orden stop (el EA original también se restringió a un mercado único o a una orden pendiente). El pedido se realiza a la oferta/pedido actual más la distancia `EntryOffsetPoints`.
6. Mientras la orden pendiente permanece activa, la estrategia monitorea el mercado. Si la diferencia entre el precio de la orden y la oferta/demanda actual excede `EntryOffsetPoints + SlippagePoints`, la orden se cancela y se vuelve a registrar inmediatamente al nuevo precio de referencia, manteniendo los stop-loss y take-profit adjuntos alineados con las compensaciones deseadas.
7. Si el diferencial actual supera `MaxSpreadPoints`, todas las entradas pendientes se cancelan para evitar operaciones durante condiciones de liquidez desfavorables.

## Gestión de pedidos

* Tras la activación de la orden de entrada, la estrategia registra el precio de ejecución y registra órdenes de parada protectora y toma de ganancias en compensaciones `StopLossPoints` y `TakeProfitPoints` respectivamente. Si cualquiera de los valores es cero, se omite la protección correspondiente.
* El módulo trailing stop (habilitado con `UseTrailingStop`) rastrea la mejor oferta/demanda. Siempre que el precio se mueve a favor de la posición abierta en más de `TrailingStopPoints`, el precio de la orden stop se ajusta más cerca del mercado usando `ReRegisterOrder`. Las paradas largas sólo avanzan hacia arriba, mientras que las paradas cortas sólo bajan.
* Cuando la posición está completamente cerrada, todas las órdenes de protección se cancelan y se restablece el estado interno. No se consideran nuevas órdenes de entrada hasta que la posición vuelva a estabilizarse.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `MaxSpreadPoints` | Spread máximo permitido antes de que se cancelen las órdenes pendientes. |
| `TakeProfitPoints` | Distancia de toma de ganancias aplicada a puestos ocupados. |
| `StopLossPoints` | Distancia de stop-loss aplicada a posiciones pendientes y ocupadas. |
| `EntryOffsetPoints` | Compensación (en puntos) de la oferta/demanda donde se colocan las entradas de parada. |
| `UseTrailingStop` | Permite la gestión de trailing stop para posiciones abiertas. |
| `TrailingStopPoints` | Distancia (en puntos) mantenida por el trailing stop. |
| `FixedVolume` | Volumen de negociación presentado con cada orden de entrada. |
| `EnvelopePeriod` | Longitud de la LWMA utilizada como base de la envolvente. |
| `EnvelopeDeviation` | Ancho del sobre en porcentaje. |
| `DistancePoints` | Diferencia mínima entre precio y envolvente requerida para una señal. |
| `SlippagePoints` | Tolerancia adicional (en puntos) agregada al umbral de revisión de precios. |
| `CandleType` | Marco de tiempo utilizado para calcular la envolvente LWMA (M5 predeterminado). |

## Notas

* La estrategia se suscribe tanto a velas como a cotizaciones de nivel 1. Si los datos de oferta/demanda no están disponibles, las condiciones de entrada no se activarán porque los cálculos de spread y trailing-stop dependen de ello.
* Las órdenes protectoras de stop y toma de ganancias se recrean con el último volumen cada vez que la lógica de seguimiento ajusta el precio de stop-loss.
* Todos los comentarios dentro del código están escritos en inglés y se utilizan tabulaciones para la sangría para que coincida con las convenciones del proyecto.
