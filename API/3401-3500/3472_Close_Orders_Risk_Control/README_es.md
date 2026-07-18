# Estrategia de control de riesgos de cerrar órdenes
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de órdenes de cierre** es una utilidad de gestión de riesgos que refleja el comportamiento del asesor experto MQL original *CloseOrders.mq4*. Supervisa continuamente las pérdidas y ganancias flotantes de las posiciones abiertas y liquida automáticamente las órdenes coincidentes una vez que se alcanza el objetivo de ganancias o el umbral de pérdidas. Esto lo hace adecuado para proteger una cartera o sincronizar salidas entre múltiples estrategias.

## como funciona
1. La estrategia se suscribe a una serie de velas configurables (1 minuto por defecto) y evalúa el PnL flotante actual cada vez que se cierra una vela.
2. El PnL flotante se calcula para las posiciones activas de la cartera. Cuando se proporciona un número mágico, solo se incluyen las posiciones cuyo `StrategyId` interno coincida con el valor configurado.
3. Si el beneficio flotante es igual o mayor que el importe objetivo, se cierran todas las órdenes y posiciones coincidentes.
4. Si el beneficio flotante cae por debajo del límite de pérdidas configurado (un número negativo), se activa la misma rutina de liquidación para minimizar pérdidas adicionales.
5. Las órdenes activas que satisfacen el filtro del número mágico se cancelan antes de aplanar las posiciones para garantizar que no se abran nuevas exposiciones durante la liquidación.

La rutina de liquidación continúa ejecutándose hasta que todas las posiciones coincidentes estén niveladas, lo que garantiza que los llenados parciales se manejen con elegancia.

## Parámetros
| Parámetro | Descripción |
| --- | --- |
| **Dinero de ganancia objetivo** | Beneficio flotante (en la moneda de la cuenta) que desencadena la liquidación de órdenes coincidentes. Debe ser mayor que cero. |
| **Reducir el dinero de las pérdidas** | PnL flotante negativo (en la moneda de la cuenta) que obliga a la liquidación. Un valor de `0` deshabilita la salida basada en pérdidas. |
| **Número Mágico** | Identificador de estrategia opcional. Déjelo vacío para gestionar cada puesto abierto; de lo contrario, sólo se verán afectadas las posiciones cuyo `StrategyId` sea igual al valor proporcionado. |
| **Tipo de vela** | Serie de velas utilizada para activar controles periódicos de ganancias. Ajuste el período de tiempo cuando se requiera un monitoreo de mayor frecuencia. |

## Notas de implementación
- El concepto de número mágico MQL se asigna a los campos `UserOrderId`/`StrategyId` en StockSharp. Asegúrese de que las estrategias que deben gestionarse utilicen el mismo identificador.
- Las pestañas se utilizan para la sangría y el archivo sigue la estructura común solicitada para las estrategias convertidas.
- La estrategia cancela las órdenes pendientes antes de enviar órdenes de mercado para aplanar la exposición, evitando el reingreso inmediato.
- Se puede agregar protección inicial si la estrategia se combina con componentes comerciales reales que necesitan manejo de emergencia.

## Consejos de uso
- Implemente la estrategia junto con estrategias comerciales que establezcan un `StrategyId` personalizado para centralizar la lógica de salida.
- Ajuste el parámetro `Candle Type` para equilibrar la capacidad de respuesta y el uso de recursos; plazos más cortos proporcionan una reacción más rápida a los cambios de PnL.
- Combine la utilidad con alertas para recibir notificaciones cada vez que se ejecute una liquidación automatizada.
