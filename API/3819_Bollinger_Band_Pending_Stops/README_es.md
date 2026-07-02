# Bollinger Estrategia de paradas pendientes de banda
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Este ejemplo convierte el asesor experto MQL "Bb_0_1" original en la API de alto nivel de StockSharp. La estrategia escucha una suscripción de vela y utiliza Bollinger bandas para agrupar el precio actual. Cuando el mercado se sitúa entre las bandas superior e inferior, el algoritmo coloca tres órdenes stop de compra en capas por encima del precio y tres órdenes stop de venta en capas por debajo del precio. Cada capa está configurada con distancias de toma de ganancias individuales mientras comparte la misma referencia de parada tomada de la banda opuesta.

## Lógica comercial
- Suscríbete al timeframe configurado y calcula Bollinger Bandas con el período y desviación solicitados.
- Dentro de la ventana de negociación (`StartHour` < hora < `EndHour`) y mientras el precio permanezca entre las bandas, coloque órdenes pendientes:
  - Tres paradas de compra en el nivel actual de la banda superior con toma de ganancias desplazadas por `FirstTakeProfit`, `SecondTakeProfit` y `ThirdTakeProfit` pasos de precio por encima de la entrada.
  - Tres paradas de venta en el nivel actual de la banda inferior con tomas de ganancias reflejadas debajo de la entrada.
  - Todas las entradas heredan la banda opuesta como parada protectora inicial.
- Las órdenes pendientes se vuelven a registrar automáticamente cada vez que las bandas se acercan al precio, de modo que las órdenes sigan los sobres del indicador.
- Una vez que se ejecuta una orden de parada, la estrategia registra órdenes explícitas de parada de pérdidas y toma de ganancias para el volumen completado.
- La protección de seguimiento es opcional: `UseBandTrailingStop` selecciona la banda opuesta para el seguimiento; de lo contrario, se utiliza la banda media (EMA). Las paradas solo siguen cuando el cierre supera el precio de entrada y el valor del indicador proporciona un mejor nivel.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `CandleType` | Marco de tiempo utilizado para los cálculos de la banda Bollinger. |
| `BandPeriod` | Número de velas utilizadas por las bandas. |
| `BandDeviation` | Multiplicador de desviación estándar para las bandas. |
| `Volume` | Volumen de cada capa pendiente. |
| `StartHour` / `EndHour` | Ventana de negociación horaria (límites exclusivos). |
| `FirstTakeProfit`, `SecondTakeProfit`, `ThirdTakeProfit` | Distancias de obtención de beneficios expresadas en incrementos de precios para cada capa. |
| `UseBandTrailingStop` | Seleccione la referencia final: banda opuesta (`true`) o Bollinger línea media (`false`). |

## Notas de implementación
- El volumen de pedidos refleja el asesor experto original utilizando un tamaño estático (`Volume`). El tamaño de posición basado en el riesgo del código MQL no se implementa porque el entorno de muestra StockSharp no proporciona el historial de la cuenta.
- Los parámetros de cambio de indicador del script MQL no están expuestos porque el nivel alto API ya ofrece valores alineados para la vela actual.
- Las órdenes de protección son órdenes stop y límite normales que se actualizan cada vez que las condiciones de seguimiento basadas en bandas mejoran el nivel de stop.
