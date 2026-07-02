# Parabolic SAR Estrategia de error 2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Parabolic SAR estrategia de error 2** es la StockSharp conversión de alto nivel del MetaTrader asesor experto `pSAR_bug2` de la carpeta `MQL/9503`. El EA original reacciona al primer punto Parabolic SAR que aparece en el lado opuesto del precio. Cuando el punto pasa por debajo del cierre, el sistema cierra cualquier operación corta e inmediatamente abre una posición larga; cuando el punto salta por encima del cierre, la lógica refleja el comportamiento en el lado corto. Los niveles protectores de stop-loss y take-profit se calculan en puntos de precio bruto, exactamente como en MetaTrader donde los valores se multiplican por el tamaño del instrumento `Point`.

El puerto StockSharp mantiene la misma intención y al mismo tiempo aprovecha el API de alto nivel del marco. Se suscribe a velas terminadas, vincula un indicador Parabolic SAR con parámetros de aceleración configurables, monitorea las reversiones de puntos y envía órdenes de mercado dimensionadas para aplanar la exposición anterior y establecer la nueva operación.

## Lógica de trading
1. **Preparación de indicadores**. La estrategia se suscribe a un tipo de vela definido por el usuario (período de tiempo de 15 minutos de forma predeterminada) y vincula un Parabolic SAR con un paso de aceleración `SarStep` y una aceleración máxima `SarMaximum`.
2. **Seguimiento del estado**. En la primera vela completa, el algoritmo registra si el valor SAR está por encima o por debajo del cierre. Cada nueva vela compara la nueva posición SAR con el estado almacenado previamente.
3. **Reglas de entrada**.
   - **Entrada larga**: se activa cuando el SAR se mueve desde arriba del cierre hasta debajo del cierre. El volumen de la orden se calcula como `TradeVolume + |Position|`, por lo que una posición corta existente se cierra y se revierte en una única orden de mercado. Después de la entrada, los niveles de stop-loss y take-profit se almacenan en relación con el cierre de la vela.
   - **Entrada corta**: se activa cuando el SAR se mueve desde debajo del cierre hasta arriba del cierre. Cualquier posición larga existente se aplana y se ingresa una nueva operación corta en el mercado con la misma fórmula de tamaño combinado.
4. **Salidas de protección**. En cada vela completa, los niveles almacenados de stop-loss y take-profit se comparan con el máximo/mínimo. Si el precio atraviesa un nivel de protección, la estrategia envía una orden de mercado para cerrar la posición abierta y restablece los valores de parada y toma almacenados en caché.

## Gestión del riesgo
- Las distancias de stop-loss y take-profit se calculan en puntos de precio bruto multiplicando el `StopLossPoints` o `TakeProfitPoints` configurado por el paso del precio del valor. Se utiliza un respaldo conservador de `0.0001` cuando el instrumento no publica un paso de precio.
- La estrategia verifica `IsFormedAndOnlineAndAllowTrading()` antes de enviar órdenes, asegurando que los datos del mercado estén en línea y que se permita el comercio.
- Las entradas de reversión siempre incluyen el tamaño absoluto de la posición actual, lo que garantiza que la nueva orden aplana la exposición anterior antes de establecer la operación opuesta.

## Parámetros
| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `TradeVolume` | `0.1` | Volumen base de pedidos en lotes. El mismo valor se asigna a la propiedad interna `Strategy.Volume`. |
| `StopLossPoints` | `90` | Distancia de stop-loss en puntos de precio. La distancia se multiplica por el paso del precio del instrumento para obtener la compensación del precio real. |
| `TakeProfitPoints` | `20` | Distancia de obtención de beneficios en puntos de precio convertidos a través del paso del precio del instrumento. |
| `SarStep` | `0.001` | Factor de aceleración inicial para el indicador Parabolic SAR. |
| `SarMaximum` | `0.2` | Factor de aceleración máximo para el indicador Parabolic SAR. |
| `CandleType` | `15m time-frame` | Tipo de vela utilizado para cálculos y evaluación de señales. |

## Notas sobre la conversión
- Las órdenes de stop-loss y take-profit del lado del corredor de MetaTrader se emulan monitoreando los extremos de las velas y enviando salidas del mercado cuando se superan los umbrales.
- El MetaTrader EA requería gestión manual de `OrdersTotal()` y llamadas explícitas `OrderClose()`. La versión StockSharp logra el mismo comportamiento enviando una única orden de mercado de tamaño `TradeVolume + |Position|`, que simultáneamente cierra cualquier posición opuesta y abre la nueva.
- No se proporciona ninguna implementación de Python que coincida con la solicitud de la tarea. Actualmente, la carpeta contiene solo la versión C# de la estrategia.
