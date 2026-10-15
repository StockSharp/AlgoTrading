# Estrategia de errores de Psar
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Psar Bug Strategy** es una adaptación directa del MetaTrader 4 asesor experto `pSAR_bug.mq4`. Reacciona al primer punto Parabolic SAR que aparece en el lado opuesto del precio e inmediatamente invierte la posición. La implementación StockSharp se suscribe a velas, evalúa solo barras completadas y utiliza el API de alto nivel para realizar órdenes de mercado y gestionar paradas de protección.

## Lógica de trading
- Calcula el Parabolic SAR con un paso de aceleración de `0.02` y una aceleración máxima de `0.2` (ambas configurables).
- Espere a que termine una vela donde el valor Parabolic SAR cambie en relación con el cierre:
  - **Entrada larga**: el valor actual de SAR está por debajo del precio de cierre mientras que el valor anterior de SAR estaba por encima del cierre anterior.
  - **Entrada corta**: el valor actual de SAR está por encima del precio de cierre mientras que el valor anterior de SAR estaba por debajo del precio de cierre anterior.
- Invertir la exposición existente en cada señal. Cuando aparece una señal de compra, cualquier posición corta abierta se aplana y se reemplaza con una posición larga del tamaño configurado. Lo contrario se aplica a las señales de venta.
- Aplique distancias fijas de stop-loss y take-profit expresadas en incrementos del precio del instrumento. La protección se implementa con `StartProtection` para que los parámetros de riesgo se adjunten automáticamente a cada nueva posición.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `TradeVolume` | Volumen de pedidos en lotes utilizados para las entradas. El valor predeterminado es `0.1` lotes. |
| `StopLossPoints` | Distancia desde el precio de entrada hasta el stop-loss expresada en incrementos de precio. Refleja la entrada MetaTrader `StopLoss`. |
| `TakeProfitPoints` | Distancia desde el precio de entrada hasta la obtención de beneficios expresada en incrementos de precio. Refleja la entrada MetaTrader `TakeProfit`. |
| `SarAccelerationStep` | Factor de aceleración inicial del indicador Parabolic SAR. |
| `SarAccelerationMax` | Factor de aceleración máximo para el cálculo Parabolic SAR. |
| `CandleType` | Tipo de datos de vela (período de tiempo) utilizado para los cálculos del indicador. Por defecto, la estrategia funciona con velas de 15 minutos. |

## Notas sobre la conversión
- El experto original hace referencia directamente al símbolo del gráfico actual y al período de tiempo. La versión StockSharp expone el tipo de vela como parámetro para que el período de tiempo se pueda cambiar sin volver a compilar.
- Las paradas de protección se representan como compensaciones de precios absolutas. Se inicializan una vez al inicio y la plataforma los administra automáticamente.
- La gestión de órdenes se basa en la lógica de compensación: comprar `Volume + |Position|` lotes cierra el corto anterior y abre el nuevo largo, reproduciendo el comportamiento MetaTrader de cerrar antes de abrir en la dirección opuesta.

## Uso
1. Configure los parámetros de seguridad, plazo (`CandleType`) y riesgo que desee dentro de StockSharp Designer o Backtester.
2. Asegúrese de que los datos del mercado estén disponibles e inicie la estrategia. Las señales se evalúan únicamente en velas terminadas.
3. Supervise las posiciones y el rendimiento a través de las herramientas estándar StockSharp. Los gráficos muestran velas, el indicador Parabolic SAR y operaciones ejecutadas para la validación visual de las señales de reversión.
