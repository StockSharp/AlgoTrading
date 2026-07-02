# Estrategia T3MA(MTC)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Convertido del asesor experto MetaTrader 4 **T3MA(MTC).mq4** (directorio `MQL/7904`). El robot original intercambia señales del indicador "T3MA-ALARM": construye una media móvil exponencial doblemente suavizada y coloca una orden cada vez que la pendiente de esa curva cambia de baja a creciente o viceversa. El puerto StockSharp refleja la misma lógica con API idiomáticas de alto nivel.

## idea comercial

1. Cree un primer EMA utilizando el tipo de vela y el período seleccionados.
2. Suaviza esa serie con un segundo EMA del mismo período.
3. Compare el valor suavizado con el anterior (opcionalmente desplazado en `MaShift`).
4. Cuando la pendiente cambia de dirección, la estrategia registra una señal. Las órdenes se ejecutan después del retraso `CalculationBarOffset` configurado, reproduciéndose el parámetro `CalculationBarIndex` del EA.
5. Cada señal utiliza el mínimo (para una entrada larga) o el máximo (para una entrada corta) de la barra como marcador único para evitar operaciones duplicadas, al igual que la variable `LastOrder` en MetaTrader.

## Detalles de portabilidad

- Utiliza dos instancias `ExponentialMovingAverage` para emular la cadena de suavizado T3MA-ALARM.
- Mantiene una pequeña cola de valores suavizados recientes para respaldar la retrospectiva `MaShift`.
- Las señales se almacenan en una cola FIFO y se ejecutan después del número solicitado de velas terminadas.
- Las órdenes de protección se gestionan a través de `StartProtection` con distancias expresadas en incrementos de precio, igualando MetaTrader puntos.
- El indicador `AllowMultiplePositions` reproduce la entrada `MultiPositions`: cuando está deshabilitada, la estrategia espera hasta que la posición neta sea plana antes de actuar sobre una nueva señal.

## Parámetros

- `MaPeriod` – EMA longitud utilizada para ambas pasadas de suavizado (predeterminado: 4).
- `MaShift`: número de barras para desplazar la serie suavizada antes de comparar su pendiente (predeterminado: 0).
- `CalculationBarOffset` – retraso (en velas terminadas) entre la detección de una señal y el envío de la orden (predeterminado: 1).
- `TradeVolume` – volumen de pedido base en lotes (predeterminado: 1).
- `UseStopLoss` / `StopLossPoints`: habilitación y distancia del stop loss en pasos de precio (predeterminado: habilitado, 40 pasos).
- `UseTakeProfit` / `TakeProfitPoints`: habilitación y distancia de la toma de ganancias en pasos de precio (predeterminado: habilitado, 11 pasos).
- `AllowMultiplePositions`: permite apilar posiciones incluso cuando una opuesta está abierta (predeterminado: habilitado).
- `CandleType`: período de tiempo o tipo de datos utilizado para alimentar la cadena del indicador (predeterminado: velas de 5 minutos).

## Flujo de trabajo comercial

1. Suscríbase a la serie de velas elegida y proporcione los precios de cierre a través de la cadena doble EMA.
2. Sigue la dirección de la pendiente actual y genera una señal cuando gira.
3. Inserte cada señal (o la ausencia de una) en la cola de demora para que las ejecuciones ocurran exactamente después de que `CalculationBarOffset` complete las velas, tal como el script MQL4 lee los buffers de indicadores más antiguos.
4. Cuando se ejecuta una señal madura:
   - Omítalo si el comercio está deshabilitado, la plataforma no está lista o `AllowMultiplePositions` está desactivado mientras ya hay una posición neta abierta.
   - Asegúrese de que el marcador de señal sea diferente del anterior para evitar duplicados.
   - Envía una orden de mercado (`BuyMarket`/`SellMarket`) con el volumen configurado. Las paradas protectoras se colocan automáticamente cuando están habilitadas.

## Notas

- Las comparaciones de precios utilizan una pequeña tolerancia decimal para evitar artefactos de punto flotante al comprobar el análogo `LastOrder`.
- La estrategia no cierra automáticamente posiciones opuestas cuando `AllowMultiplePositions` está desactivado, imitando el EA original que dependía de salidas protectoras.
- La visualización de velas y operaciones propias está disponible cuando el subsistema de gráficos está presente.
