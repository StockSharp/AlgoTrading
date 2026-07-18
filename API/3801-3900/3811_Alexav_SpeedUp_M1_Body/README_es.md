# Estrategia Alexav SpeedUp M1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia Alexav SpeedUp M1** es una adaptación directa del asesor experto MetaTrader 4 "Alexav_SpeedUp_M1". Evalúa los cuerpos de las velas intradiarias completadas y reacciona inmediatamente con órdenes de mercado cada vez que el cuerpo de la vela excede un umbral configurable. Después de una entrada, la estrategia emula la gestión de riesgos al estilo MetaTrader adjuntando órdenes stop-loss, take-profit y trailing stop a la posición abierta.

La conversión se basa en el StockSharp nivel alto API. Las velas se consumen a través de `SubscribeCandles`, la información de precios para el seguimiento se recibe de los datos de nivel 1 y las órdenes de protección se realizan con los ayudantes estándar `BuyStop`, `SellStop`, `BuyLimit` y `SellLimit`. No se requieren cálculos manuales de indicadores.

## Generación de señal
1. Se inspecciona cada vela terminada en el período de tiempo configurado.
2. Cuando la vela cierra por encima de su apertura por más del **umbral corporal**, la estrategia abre (o revierte) una posición larga en el mercado.
3. Cuando la vela cierra por debajo de su apertura por más del mismo umbral, la estrategia abre (o revierte) una posición corta en el mercado.
4. La exposición existente en la dirección opuesta se cierra automáticamente aumentando el volumen de la orden de mercado, reproduciendo fielmente el comportamiento del asesor experto original.

## Gestión de pedidos
* **Stop-loss inicial**: Tan pronto como aumenta el volumen de la posición, se registra una orden de stop de protección al precio de entrada menos (para largos) o más (para cortos) el número de puntos configurado.
* **Take-profit**: Se coloca una orden límite coincidente en la dirección de la operación a la distancia especificada en **Take Profit (puntos)**.
* **Parada dinámica**: las actualizaciones de oferta/demanda de nivel 1 monitorean el beneficio actual. Cuando el beneficio no realizado supera la distancia de seguimiento, el stop protector se mueve hacia el precio, manteniendo la brecha configurada sin retroceder nunca.
* Todas las órdenes de protección se cancelan cuando la posición vuelve a estabilizarse.

La conversión mantiene la lógica intencionalmente simple: no se agregan filtros, indicadores o controles de riesgo adicionales más allá de lo que estaba presente en la implementación MQL.

## Parámetros
| Nombre | Descripción |
| ---- | ----------- |
| **Tamaño del lote** | Volumen de negociación base (en lotes) utilizado para cada orden de mercado. Al invertir una posición existente, el volumen requerido se agrega automáticamente. |
| **Obtener ganancias (puntos)** | Distancia desde el precio de entrada hasta el nivel de obtención de beneficios medida en MetaTrader puntos (convertida utilizando el paso del precio del valor). |
| **Parada inicial (puntos)** | Distancia desde el precio de entrada hasta el stop de protección inicial expresada en puntos. |
| **Parada dinámica (puntos)** | Distancia de seguimiento que se mantiene después de que el precio se mueve a favor de la posición. Un valor de cero desactiva la lógica de seguimiento. |
| **Umbral corporal** | Diferencia absoluta mínima entre el cierre y la apertura de la vela requerida para activar una nueva operación. |
| **Tipo de vela** | Serie de velas (marco de tiempo) utilizada para la evaluación de señales. El valor predeterminado coincide con el gráfico original de un minuto. |

## Notas de uso
* Asegúrese de que la seguridad proporcione un `PriceStep` válido. Cuando no está disponible, la estrategia recurre a interpretar las distancias entre puntos como compensaciones de precios brutos.
* La lógica del trailing stop requiere datos de nivel 1 (mejor oferta/demanda). Cuando solo hay datos de velas disponibles, la funcionalidad de seguimiento permanece inactiva.
* La estrategia está diseñada para operaciones intradía y refleja el comportamiento de una operación por vela aplicado por el experto original MQL a través de sus contadores internos.
