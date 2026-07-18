# Estrategia de malos pedidos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia BadOrders** es una adaptación directa del MetaTrader 4 asesor experto `BadOrders.mq4`. El guión original fue escrito intencionalmente para demostrar cómo la gestión incorrecta de órdenes conduce a operaciones rechazadas. En cada entrada, marque:

1. Cierra con fuerza la posición abierta más recientemente al precio de oferta actual.
2. Coloca un nuevo stop de compra 100 puntos por encima de la oferta.
3. Modifica inmediatamente esa orden pendiente para ubicarse 100 puntos *por debajo* de la oferta, violando las reglas de distancia del corredor y provocando un error.

La versión StockSharp reproduce este comportamiento con el nivel alto API. Se suscribe a cotizaciones de Nivel 1 para monitorear la mejor oferta y reproduce el mismo ciclo de cierre, lugar e invalidación cada vez que llega una cotización.

## Detalles de implementación
- **Flujo de datos**: `SubscribeLevel1()` se utiliza porque el script MT4 reacciona a cada tick en lugar de completar velas.
- **Gestión de pedidos**: Las posiciones abiertas se cierran con el ayudante `ClosePosition()`. Las paradas pendientes se gestionan a través de `BuyStop()` y `ReRegisterOrder()` para que podamos mover inmediatamente la orden de parada a un precio ilegal, imitando el flujo de trabajo interrumpido del código fuente.
- **Normalización de precios**: Todos los precios se normalizan mediante `Security.ShrinkPrice()` y el concepto MetaTrader de `Point` se emula a través del instrumento `PriceStep`. Cuando no hay ningún tamaño de tick disponible, la estrategia vuelve a ser `0.0001`.
- **Lógica de protección**: antes de llamar a `ClosePosition()`, el código verifica las órdenes de liquidación existentes para evitar acumular solicitudes de salida duplicadas.

## Parámetros
| Nombre | Descripción | Predeterminado |
| ---- | ----------- | ------- |
| `DistancePoints` | Distancia en MetaTrader “puntos” agregados por encima y por debajo de la oferta actual al realizar o volver a registrar la orden de parada. | `100` |

## Resumen de comportamiento
- Siempre que la oferta cambia, la estrategia intenta aplanar cualquier posición abierta.
- Se envía una parada de compra en `bid + DistancePoints * PointValue` después de cerrar la posición.
- La misma orden se vuelve a registrar inmediatamente en `bid - DistancePoints * PointValue`, lo que viola las reglas de intercambio y se espera que falle, reflejando precisamente los errores intencionales en `BadOrders.mq4`.

> **Nota**: Este proyecto existe únicamente para mantener la paridad con la muestra MT4 y no está diseñado para operaciones reales.
