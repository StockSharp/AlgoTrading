# Asistente de especulación
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **Scalping Assistant** es una conversión directa del MetaTrader 4 asesor experto "Scalper Assistant v1.0". No genera entradas por sí solo. En cambio, monitorea las posiciones abiertas en el valor configurado y administra las órdenes de protección de manera similar a MetaTrader.

## como funciona

1. Cuando se detecta una nueva posición, la estrategia registra inmediatamente órdenes de stop-loss y take-profit utilizando las distancias configuradas (expresadas en pasos de precio).
2. La estrategia se suscribe a datos de nivel 1 y rastrea continuamente la mejor oferta/demanda para estimar el beneficio actual de la posición.
3. Una vez que la ganancia no realizada alcanza `BreakEvenTriggerPoints`, la parada inicial se cancela y se vuelve a registrar al precio de equilibrio más la compensación configurada.
4. El nivel stop permanece en el punto de equilibrio; no se realiza más seguimiento. La orden de toma de ganancias permanece intacta.
5. Tan pronto como se cierra la posición, todas las órdenes de protección se cancelan y el estado interno se restablece, listo para la siguiente operación manual.

## Notas de uso

- Adjunte la estrategia a un conector/cartera y abra operaciones manualmente o desde otro algoritmo. El asistente se hará cargo de la protección de dichos puestos.
- La lógica se basa en comillas de nivel1; asegúrese de que el conector seleccionado proporcione las mejores actualizaciones de oferta y demanda.
- El término *puntos* se refiere al paso del precio del instrumento (`Security.PriceStep`). Para los símbolos de Forex con cinco decimales, esto equivale a un pip.

## Parámetros

| Nombre | Tipo | Predeterminado | Descripción |
| --- | --- | --- | --- |
| `StopLossPoints` | `decimal` | `30` | Distancia (en pasos de precio) utilizada al colocar el stop de protección inicial. Establezca en `0` para omitir el envío de una orden de suspensión. |
| `TakeProfitPoints` | `decimal` | `100` | Distancia (en incrementos de precio) utilizada al realizar la orden inicial de obtención de beneficios. Establezca en `0` para omitir la toma de ganancias. |
| `BreakEvenTriggerPoints` | `decimal` | `15` | Beneficio en los escalones de precios que deben alcanzarse antes de que el tope se mueva al punto de equilibrio. |
| `BreakEvenOffsetPoints` | `decimal` | `5` | Distancia adicional (en pasos de precio) agregada por encima/por debajo del precio de entrada cuando el stop se desplaza al punto de equilibrio. |

## Estado de conversión

- ✅ Lógica central: manejo del punto de equilibrio basado en MetaTrader parámetros de entrada.
- ✅ Uso de API de alto nivel: `SubscribeLevel1()` con enlace delegado.
- ✅ Órdenes de protección: creadas a través de `SellStop`, `BuyStop`, `SellLimit` y `BuyLimit` ayudantes.
- ❌ Sin puerto Python: solo se proporciona la estrategia C#, que coincide con la solicitud.
