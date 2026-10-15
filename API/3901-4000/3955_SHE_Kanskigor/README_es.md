# Ella Kanskigor estrategia diaria
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
She Kanskigor Daily Strategy es un sistema de ruptura que se realiza una vez al día y refleja el MetaTrader asesor experto `SHE_kanskigor.mq4` original. La estrategia evalúa la dirección de la vela diaria anterior y abre una posición de mercado única dentro de una ventana de tiempo estrecha al comienzo del nuevo día de negociación. Supervisa automáticamente la posición para cerrarla mediante una distancia configurable de obtención de beneficios o límite de pérdidas, expresada en pasos del precio del valor.

## Lógica de trading
1. Suscríbase tanto a velas intradiarias (predeterminado: 1 minuto) como a velas diarias para el valor seleccionado.
2. Actualice la apertura y el cierre diarios almacenados cada vez que llegue una vela diaria terminada.
3. En cada vela intradiaria terminada:
   - Restablezca el indicador "negociado hoy" cuando comience una nueva fecha del calendario.
   - Administre la posición activa verificando si el precio de cierre alcanza los umbrales de stop-loss o take-profit.
   - Compruebe si la hora actual está dentro de la ventana comercial configurada (inicio predeterminado: 00:05, duración de la ventana: 5 minutos).
   - Si aún no se ha abierto ninguna posición hoy y hay disponible una vela diaria anterior válida:
     - Vaya en largo cuando la apertura diaria anterior sea más alta que el cierre (vela bajista).
     - Vaya en corto cuando la apertura diaria anterior sea inferior al cierre (vela alcista).
   - Evite operar cuando el día anterior cerró sin cambios.
4. La estrategia ejecuta salidas protectoras utilizando órdenes de mercado una vez que el precio de cierre toca los umbrales configurados.

## Parámetros
| Nombre | Descripción | Predeterminado |
| ---- | ----------- | ------- |
| **Volumen** | Volumen de pedidos utilizado para las entradas. | `0.1` |
| **Obtener ganancias** | Objetivo de beneficio expresado en incrementos de precios. Un valor de `0` deshabilita el objetivo. | `35` |
| **Detener pérdidas** | Umbral de pérdida expresado en incrementos de precio. Un valor de `0` desactiva la parada. | `55` |
| **Hora de inicio** | Hora del día (zona horaria de intercambio) en la que comienza la ventana de entrada. | `00:05` |
| **Ventana (min)** | Duración, en minutos, de la ventana de entrada. | `5` |
| **Vela intradiaria** | Tipo de datos de vela utilizado para el procesamiento intradiario (predeterminado: velas de 1 minuto). | `TimeFrameCandleMessage(1m)` |

## Notas
- La estrategia permite sólo una entrada por día de negociación.
- Los datos de las velas diarias deben estar disponibles; de lo contrario, la estrategia espera hasta que llegue una vela completa.
- Las salidas protectoras operan sobre el precio de cierre de las velas intradiarias terminadas.
- El código utiliza API de alto nivel de StockSharp (`SubscribeCandles` con `Bind`) y cumple con los estándares de codificación del proyecto (pestañas, comentarios en inglés y metadatos de parámetros).
