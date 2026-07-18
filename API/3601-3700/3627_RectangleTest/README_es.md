# Estrategia de prueba rectangular
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia de prueba de rectángulo reproduce el experto "RectangleTest" de MetaTrader utilizando el nivel alto de StockSharp API. Detecta rangos laterales en un marco de tiempo intradiario, verifica si dos promedios móviles y el precio actual permanecen dentro del rango detectado y luego intercambia rupturas fuera del rectángulo en la dirección del EMA más rápido. Toda la lógica se ejecuta en velas completadas recibidas de una fuente de velas configurable.

## Lógica de trading
1. Suscríbase al flujo de velas principal (predeterminado: período de tiempo de 1 hora) e introdúzcalo en los siguientes indicadores:
   - **ExponentialMovingAverage (EMA)** con longitud configurable `EmaPeriod`.
   - **SimpleMovingAverage (SMA)** con longitud configurable `SmaPeriod`.
   - Indicadores **más alto** y **más bajo** con longitud `RangeCandles`, configurados para leer máximos y mínimos de velas. Proporcionan los límites del rectángulo que emulan los cálculos basados ​​en matrices MetaTrader.
2. Una vez que se formen todos los indicadores, calcule la altura del rectángulo en porcentaje con respecto al límite superior. Sólo las velas cuya altura es menor que `RectangleSizePercent` se consideran consolidaciones válidas.
3. Requiere que EMA, SMA y la vela cercana permanezcan dentro del rectángulo. Esto reproduce el filtro lateral de la versión MQL.
4. **Configuración breve**:
   - EMA está por encima del SMA.
   - El precio de cierre está por encima de EMA (que coincide con la condición "Preguntar > EMA" de MetaTrader en velas completadas).
   - Primero se produce la liquidación opcional de una posición larga existente, después de lo cual se envía una orden de mercado corta.
5. **Configuración larga**:
   - EMA está debajo de SMA.
   - El precio de cierre está por debajo de EMA (reflejando la regla "Oferta < EMA").
   - Los cortos existentes se liquidan antes de abrir los largos.
6. Cada entrada registra el precio de entrada esperado y el volumen. Cuando la posición llega a cero, la estrategia compara el precio de salida con el precio de entrada almacenado. Las operaciones perdedoras aumentan el contador de pérdidas diarias, aplicando el filtro `MaxLosingTradesPerDay` exactamente igual que el MQL ayudante `Loss()`.

## Gestión de dinero y riesgos
- La estrategia puede funcionar de dos modos:
  - **Modo basado en riesgo** (`UseRiskMoneyManagement = true`): el volumen de la posición se dimensiona a partir del valor de la cuenta, el `RiskPercent` y el `StopLossPoints` configurado. El cálculo utiliza `Security.PriceStep`, `Security.StepPrice` y `Security.VolumeStep` para reflejar la rutina de dimensionamiento del lote MetaTrader.
  - **Modo de volumen fijo** (`UseRiskMoneyManagement = false`): las operaciones utilizan el parámetro `FixedVolume`.
- Después de que la posición neta cambia de estable a distinta de cero, `SetStopLoss` y `SetTakeProfit` registran órdenes de protección usando `StopLossPoints` y `TakeProfitPoints` (expresadas en incrementos de precios), haciendo coincidir las distancias SL/TP pasadas a `m_trade.Sell/Buy` en el experto original.
- `MaxLosingTradesPerDay` detiene nuevas señales durante el resto del día una vez que se ha detectado el número especificado de operaciones perdedoras.

## Gestión del tiempo
- Solo se permite operar entre `TradeStartTime` y `TradeEndTime`. El asistente maneja intervalos que abarcan la medianoche y sesiones diurnas.
- Cuando `EnableTimeClose` es verdadero, todas las posiciones abiertas se liquidan después de `TimeClose`, replicando las entradas MetaTrader "TimeCloseTrue" y `TimeClose`.

## Diferencias frente a la versión MetaTrader
- El indicador original creó rectángulos gráficos en el gráfico. StockSharp no crea objetos de dibujo; en cambio, el mismo rango se calcula internamente mediante indicadores más alto/más bajo.
- Las operaciones perdedoras se cuentan utilizando los precios de cierre de la vela de señal. Esto coincide con la intención de `Loss()` (contar los pedidos perdidos por día) mientras se mantiene dentro de las abstracciones de alto nivel StockSharp.
- Las características de llenado de pedidos como `ORDER_FILLING_FOK/IOC` son manejadas por el entorno de StockSharp, por lo que no se requiere una configuración explícita del modo de llenado.

## Parámetros
| Nombre | Predeterminado | Descripción |
| ---- | ------- | ----------- |
| `EmaPeriod` | 45 | Período de la EMA rápida. |
| `SmaPeriod` | 200 | Periodo de la lentitud SMA. |
| `RangeCandles` | 10 | Número de velas que forman el rectángulo. |
| `RectangleSizePercent` | 0,5 | Altura máxima del rectángulo permitida para el comercio. |
| `StopLossPoints` | 250 | Distancia de stop-loss en pasos de precio. |
| `TakeProfitPoints` | 750 | Distancia de obtención de beneficios en pasos de precio. |
| `UseRiskMoneyManagement` | cierto | Alternar entre volumen fijo y basado en riesgo. |
| `RiskPercent` | 1 | Porcentaje del capital de la cuenta arriesgado por operación. |
| `FixedVolume` | 1 | Volumen fijo cuando el tamaño basado en riesgo está deshabilitado. |
| `MaxLosingTradesPerDay` | 1 | Límite diario de operaciones perdedoras. |
| `TradeStartTime` | 03:00 | Hora del día en que se permiten las entradas. |
| `TradeEndTime` | 22:50 | Hora del día a partir de la cual no se generan nuevas entradas. |
| `EnableTimeClose` | falso | Permite la liquidación al final del día. |
| `TimeClose` | 23:00 | Hora del día para cerrar todas las posiciones. |
| `CandleType` | velas de 1 hora | Fuente de datos de velas primaria. |

## Trazar
Si hay un área de gráfico disponible, la estrategia dibuja las velas de precios, las operaciones rápidas EMA, lentas SMA y propias para visualizar rupturas de rango y el momento de las operaciones.
