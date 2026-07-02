# Estrategia Breadandbutter2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Breadandbutter2 es una conversión directa del asesor experto MT4 de `MQL/7710/Breadandbutter2.mq4`. El sistema monitorea velas de una hora y rastrea tres promedios móviles ponderados lineales (LWMA) basados ​​en los precios de apertura de las velas. Un cruce sincronizado de los tres promedios indica un cambio de tendencia. La estrategia invierte inmediatamente la posición para alinearla con la nueva dirección y, opcionalmente, piramidalmente órdenes adicionales mientras persiste la tendencia.

## Lógica principal
1. Suscríbase a velas de una hora (configurables a través de **Tipo de vela**).
2. Calcule LWMA(5), LWMA(10) y LWMA(15) en las aperturas de velas.
3. Detectar una reversión alcista cuando la vela anterior tenía `LWMA5 < LWMA10 < LWMA15` y la vela actual muestra `LWMA5 > LWMA10 > LWMA15`. Detectar una reversión bajista con la secuencia de desigualdad opuesta.
4. En un cruce alcista, apunte a una posición larga de lotes de **Volumen**. En un cruce bajista, apunte a una posición corta del mismo tamaño. La estrategia ajusta la posición existente comprando o vendiendo sólo la diferencia entre la exposición actual y la objetivo.
5. Después de cada entrada, el contador **Intervalo** se reinicia. Una vez que las velas terminadas del **Intervalo** pasan sin un nuevo cruce, la estrategia agrega otra orden en la dirección actual (piramidal) y actualiza las órdenes de protección.
6. El objetivo de ganancias y el límite de pérdidas se adjuntan a cada posición resultante utilizando distancias **Take Profit** y **Stop Loss** expresadas en pasos de precio. Establecer cualquiera de los valores en cero desactiva la protección correspondiente.

## Parámetros
| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| **Volumen** | 0.1 | Tamaño del pedido en lotes para cada entrada base y capa piramidal. |
| **Obtener ganancias** | 20 | Distancia en pasos de precio para la orden de toma de ganancias. Establezca en 0 para desactivar. |
| **Detener pérdidas** | 20 | Distancia en pasos de precio para el tope de protección. Establezca en 0 para desactivar. |
| **Intervalo** | 4 | Número de velas terminadas que se deben esperar antes de agregar otra posición de pirámide. Zero desactiva la piramidización. |
| **Filtro cruzado** | 1.1 | Parámetro reservado conservado del código original para futuros filtrados ADX (actualmente no utilizado). |
| **Tipo de vela** | plazo de 1 hora | Fuente de datos de velas para los cálculos de LWMA. |

## Gestión de Puestos
- El método auxiliar `AdjustPosition` garantiza que la posición final coincida exactamente con la exposición deseada después de cada cruce.
- Las operaciones piramidales se basan en el signo actual de `Position` para agregar lotes únicamente en la dirección existente.
- `SetTakeProfit` y `SetStopLoss` se invocan después de cada operación para mantener los controles de riesgo sincronizados con el último tamaño de la posición.

## Notas
- El script MT4 calculó un valor ADX pero nunca lo usó; el parámetro **Filtro cruzado** se conserva por motivos de compatibilidad y extensión futura.
- La implementación original MQL tenía el contador de intervalo comentado. La versión StockSharp activa el comportamiento piramidal previsto al contar las velas terminadas.
- Se llama a `StartProtection()` durante `OnStarted` para activar los servicios de protección de posición integrados.
