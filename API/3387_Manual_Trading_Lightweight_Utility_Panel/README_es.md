# Estrategia de panel de utilidad ligera de comercio manual
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia del panel de utilidad ligera de comercio manual** replica el comportamiento del panel "Utilidad ligera de comercio manual" de MT4 utilizando la estrategia de alto nivel StockSharp API. Expone los mismos controles interactivos como parámetros de estrategia para que el operador pueda alternar entre órdenes de mercado, límite y stop, ajustar el cálculo automático de precios, configurar la gestión de volumen y adjuntar controles de riesgo sin depender de objetos de gráficos personalizados.

La estrategia está diseñada para operaciones discrecionales. Los pedidos se activan manualmente cambiando los parámetros `Send Buy Order` o `Send Sell Order` en la interfaz de usuario. Cada comando se reconoce inmediatamente, mientras que la estrategia mantiene todos los cálculos, como las sugerencias automáticas de precios y los niveles de riesgo, sincronizados con los datos del mercado en tiempo real.

## Características clave
- **Envío manual de órdenes** para ambos lados, compra y venta, con soporte para órdenes de mercado, límite y stop.
- **Sugerencia de precio automática** que refleja la lógica del panel MT4, actualizando el límite propuesto o el precio de parada del último flujo de oferta/demanda.
- **Modo de precio manual opcional** que permite al operador escribir el nivel de activación deseado respetando los tamaños de paso del instrumento.
- **Gestión de volúmenes** con un tamaño de lote global y volúmenes de compra/venta individuales cuando el interruptor de control de lote está habilitado.
- **Gestión integrada de stop-loss y take-profit** implementada en la capa de estrategia para emular las protecciones adjuntas a órdenes en MT4.
- **Comentarios detallados** a través de parámetros que siempre reflejan los últimos niveles de entrada calculados para ambas partes.

## Notas de conversión
- Los objetos del gráfico MT4 (botones, etiquetas y cuadros de edición) se reemplazan por parámetros de estrategia agrupados en secciones lógicas para un fácil acceso en Hydra/Terminal.
- Los objetivos y paradas de protección se manejan internamente observando el precio del mercado en vivo porque StockSharp no los incluye en las órdenes pendientes de la misma manera que MT4.
- Las compensaciones de precios expresadas en puntos reutilizan los metadatos del instrumento (`PriceStep` y `VolumeStep`) de modo que los límites y las paradas siempre respeten las restricciones de intercambio.

## Uso
1. Adjunte la estrategia a un valor y cartera en Hydra o Terminal.
2. Configure el tamaño de lote predeterminado, los parámetros de riesgo y las compensaciones de precios.
3. Opcionalmente habilite `Lot Control` para mantener volúmenes independientes para los botones de compra y venta.
4. Elija el tipo de orden (mercado, límite pendiente o stop pendiente) y si el precio de activación debe seguir el mercado o permanecer manual.
5. Cuando esté listo, cambie `Send Buy Order` o `Send Sell Order` a `true`. La estrategia enviará el pedido correspondiente y restablecerá el indicador a `false` una vez procesado.
6. El administrador de protección cerrará las posiciones abiertas en los niveles configurados de stop-loss o take-profit calculados a partir del precio de entrada ejecutado.

## Archivos
- `CS/ManualTradingLightweightUtilityPanelStrategy.cs` – Implementación C# de la estrategia.
- `README.md` – Documentación en inglés (este archivo).
- `README_zh.md` – Documentación en chino simplificado.
- `README_ru.md` – Documentación rusa.
