# Estrategia de promediación VR Smart Grid Lite
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia VR Smart Grid Lite Averaging es un sistema de promediado de red que sigue al asesor experto original MetaTrader 5. El algoritmo abre órdenes de mercado en la dirección de la vela alcista o bajista más reciente y construye una escalera estilo martingala cada vez que el precio se mueve en contra de la posición. Las distancias, los volúmenes y la lógica de salida se pueden ajustar para que coincidan con la implementación original de MQL.

## Lógica de trading
- En cada vela completa, la estrategia comprueba su dirección.
  - Una vela alcista permite una nueva orden de compra si el precio actual está al menos `Order Step (pips)` por debajo de la entrada de compra más baja existente.
  - Una vela bajista permite una nueva orden de venta si el precio actual está al menos `Order Step (pips)` por encima de la entrada de venta más alta existente.
- El primer pedido para cada lado usa `Start Volume`. Cada pedido adicional duplica el volumen del pedido más lejano en ese lado, mientras que `Max Volume` limita el tamaño absoluto.
- Cuando solo existe una posición en un lado, la operación se cierra una vez que el precio alcanza la distancia `Take Profit (pips)`.
- Con dos o más posiciones la lógica de cierre depende del `Close Mode` seleccionado:
  - **Promedio**: cierra los pedidos más altos y más bajos una vez que el precio alcanza su promedio ponderado más `Minimal Profit (pips)`.
  - **PartialClose**: cierra por completo la orden más baja y reduce la orden más alta en `Start Volume` cuando el precio alcanza el objetivo combinado.

## Gestión del riesgo
- Los volúmenes se ajustan a los `MinVolume`, `MaxVolume` y `StepVolume` del broker para evitar el rechazo.
- La llamada incorporada `StartProtection()` garantiza que la protección de la cuenta StockSharp esté activada antes de operar.

## Parámetros
| Nombre | Descripción |
| ---- | ----------- |
| `Take Profit (pips)` | Distancia objetivo para posiciones abiertas individuales. |
| `Start Volume` | Volumen para el pedido inicial en cada dirección. |
| `Max Volume` | Volumen máximo permitido por pedido (0 desactiva el límite). |
| `Close Mode` | Elija entre salidas promediadas o cierres parciales. |
| `Order Step (pips)` | Movimiento adverso mínimo antes de agregar una nueva orden. |
| `Minimal Profit (pips)` | Colchón de beneficios adicional añadido a la salida promediada. |
| `Candle Type` | Serie de velas utilizadas para la generación de señales. |

## Notas
- La estrategia utiliza únicamente órdenes de mercado; Las órdenes pendientes del EA original se emulan evaluando las condiciones de cada vela.
- La implementación mantiene el estado por pedido para imitar la gestión basada en tickets de MetaTrader, incluidos cierres parciales y salidas selectivas.
- Configure el tipo de vela y el tamaño del pip del símbolo para que coincida con el período de tiempo utilizado en el script MQL para un comportamiento consistente.
