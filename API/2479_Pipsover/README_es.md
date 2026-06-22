# Pipsover
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Pipsover es una estrategia de reversión de momentum que reacciona a extremos fuertes del oscilador Chaikin. El Expert Advisor original de MetaTrader 5 abre una nueva operación cuando el oscilador imprime un pico pronunciado mientras la vela anterior retrocede a la media móvil simple de 20 períodos. El puerto en C# mantiene la misma idea reconstruyendo el oscilador Chaikin con la línea de acumulación/distribución y dos medias móviles exponenciales. Cada operación está protegida con las mismas distancias de stop-loss y take-profit definidas en el script para que el control de riesgo coincida con la implementación de referencia.

## Indicadores y herramientas
- **Media Móvil Simple (SMA 20)** – proporciona el ancla de reversión a la media. La estrategia requiere que la vela anterior toque o cruce el promedio antes de ser elegible para una operación.
- **Oscilador Chaikin (EMA 3 – EMA 10 de ADL)** – mide la presión entre precio y volumen. Las lecturas extremadamente negativas desencadenan oportunidades de compra y los valores positivos extremos desencadenan oportunidades de venta.
- **Línea de Acumulación/Distribución (ADL)** – alimenta el oscilador Chaikin. Las EMAs rápida y lenta corren sobre este flujo de valores para imitar el indicador `iChaikin` de MQL5.

## Lógica de trading
### Entrada larga
1. Esperar una vela completada para que todos los valores de indicadores sean definitivos.
2. Verificar que la vela anterior cerró alcista (`Close > Open`).
3. Confirmar que el mínimo anterior bajó por debajo de la SMA20, señalando un retroceso.
4. Leer el valor del oscilador Chaikin de la barra anterior. Debe ser menor que `-OpenLevel` para reflejar un pico de sobreventa.
5. Cuando se cumplen todas las condiciones y no hay posición actualmente abierta, enviar una orden de compra a mercado.

### Entrada corta
1. Esperar una vela completada.
2. Verificar que la vela anterior cerró bajista (`Close < Open`).
3. Confirmar que el máximo anterior superó la SMA20.
4. Asegurarse de que el oscilador Chaikin en la barra anterior sea mayor que `OpenLevel`.
5. Si no hay posición activa, colocar una orden de venta a mercado.

### Lógica de salida
- Las **posiciones largas** se cierran cuando la siguiente vela después de la entrada muestra una estructura bajista (cierre por debajo de apertura), su máximo permanece por encima de la SMA20 y el oscilador Chaikin sube por encima de `CloseLevel`.
- Las **posiciones cortas** se cierran cuando la siguiente vela muestra una estructura alcista, su mínimo se mueve por debajo de la SMA20 y el oscilador Chaikin cae por debajo de `-CloseLevel`.
- Las salidas de protección monitorean cada vela terminada. Una posición larga se cierra si el precio cotiza en o por debajo del stop-loss calculado o en o por encima del take-profit calculado. Para cortos la comparación es invertida.

## Gestión de posición
- Solo se permite una posición neta en cualquier momento. Las órdenes pendientes se cancelan antes de abrir una nueva operación para replicar el comportamiento de posición única de MQL5.
- Los valores de stop-loss y take-profit se calculan a partir del paso de precio del instrumento. Para largos, el stop se establece `StopLossPoints * PriceStep` por debajo del precio de ejecución y el take-profit `TakeProfitPoints * PriceStep` por encima. Los cortos usan distancias simétricas pero invertidas.

## Parámetros
| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `TradeVolume` | 0.1 | Tamaño de la orden usado para cada orden de mercado. |
| `MaLength` | 20 | Período de la SMA de retroceso. |
| `StopLossPoints` | 65 | Offset de stop-loss en pasos de precio desde la entrada. |
| `TakeProfitPoints` | 100 | Offset de take-profit en pasos de precio desde la entrada. |
| `OpenLevel` | 100 | Umbral absoluto de Chaikin que habilita nuevas entradas. |
| `CloseLevel` | 125 | Umbral absoluto de Chaikin que fuerza la salida de la posición. |
| `ChaikinFastLength` | 3 | Longitud de EMA rápida del oscilador Chaikin. |
| `ChaikinSlowLength` | 10 | Longitud de EMA lenta del oscilador Chaikin. |
| `CandleType` | 1 hora | Marco temporal usado para la suscripción de velas; ajustar para que coincida con la sesión de trading de interés. |

## Notas de implementación
- La estrategia vincula la línea de acumulación/distribución y la SMA al feed de velas a través de `SubscribeCandles().Bind(...)`, garantizando que los valores de los indicadores lleguen ya sincronizados con cada vela terminada.
- Los valores de Chaikin se reconstruyen manualmente dentro de `ProcessCandle` para evitar el acceso de bajo nivel a buffers prohibido por las pautas de conversión.
- El algoritmo almacena la última vela completada, el valor de la SMA y la lectura de Chaikin para reproducir la lógica `shift=1` (`iClose(...,1)`, `iLow(...,1)`, `iChaikin(...,1)`) usada en el script MQL5.
- Los niveles de objetivo de protección se rastrean dentro de la clase de estrategia en lugar de depender de stops gestionados por el broker, por lo que el comportamiento es consistente entre simulaciones y trading en vivo.
