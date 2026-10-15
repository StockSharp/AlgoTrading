# Estrategia Fly System Scalp
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Visión general
Fly System Scalp Strategy es un sistema de ruptura de alta frecuencia que reproduce el comportamiento central del asesor experto MQL4 *FlySystemEA*. La estrategia monitoriza constantemente las mejores cotizaciones bid/ask y despliega dos órdenes stop simétricas alrededor del precio de mercado. El objetivo es capturar microtendencias rápidas que surgen después de consolidaciones de corto plazo, manteniendo control estricto sobre spread, comisiones y límites de sesión.

La conversión se centra en estas mecánicas:

* Colocación automática de órdenes buy stop y sell stop a distancia configurable del mercado.
* Cancelación automática de órdenes pendientes cuando el spread (incluida comisión) supera el umbral admisible o el trading está fuera de la sesión permitida.
* Gestión opcional de take-profit y obligatoria de stop-loss adjunta directamente a nuevas órdenes pendientes.
* Soporte para volumen fijo manual y dimensionamiento automático basado en riesgo usando especificaciones contractuales del broker (price step, step value, lot step, volumen mínimo/máximo).
* Ciclo de trading autorreiniciable que espera a que las posiciones se cierren antes de armar un nuevo par de órdenes stop.

La implementación StockSharp usa la API de alto nivel (suscripción level-1 con bind) y sigue las convenciones requeridas del proyecto: los parámetros se exponen mediante `StrategyParam`, los comentarios están en inglés y el namespace usa declaración file-scoped.

## Lógica de negociación
1. **Feed Level 1:** la estrategia se suscribe a datos level-1 del instrumento asignado. Cada actualización registra el último par bid/ask.
2. **Capa de validación:** antes de cualquier acción, el motor comprueba:
   * La estrategia está online y autorizada para operar.
   * La hora actual está dentro de la ventana opcional de trading.
   * El spread más comisión no supera `MaxSpread` pips.
3. **Colocación de pendientes:** si se cumplen las condiciones, no hay posición abierta y la estrategia está lista para un ciclo nuevo, se preparan dos órdenes:
   * Buy Stop en `Ask + PendingDistance * pip` con Stop Loss protector y Take Profit opcional.
   * Sell Stop en `Bid - PendingDistance * pip` con protecciones reflejadas.
   Las órdenes se vuelven a registrar cuando la diferencia entre precio deseado y real alcanza `ModifyThreshold` pips.
4. **Gestión de órdenes:** si se abre una posición, la orden pendiente opuesta se cancela de inmediato. Cuando un ciclo se interrumpe por violaciones de spread/tiempo, todas las pendientes se eliminan y la estrategia espera condiciones válidas.
5. **Dimensionamiento:** con `AutoLotSize` activo, el volumen se deriva del `RiskFactor` por ciento del patrimonio dividido por la pérdida por contrato en la distancia de stop configurada. El volumen se redondea al paso de lote y se limita por mínimos/máximos.
6. **Protección:** se invoca `StartProtection()` para que StockSharp monitorice la posición y ejecute liquidación de emergencia si la infraestructura lo requiere.

## Parámetros
| Nombre | Descripción | Predeterminado |
|------|-------------|---------|
| `PendingDistance` | Distancia en pips entre precio de mercado y ambas órdenes stop. | 4 |
| `StopLossDistance` | Distancia de stop-loss en pips adjunta a nuevas posiciones. | 0.4 |
| `TakeProfitDistance` | Distancia de take-profit en pips cuando está activado. | 10 |
| `UseTakeProfit` | Activa la colocación de take-profit. | `false` |
| `MaxSpread` | Spread máximo permitido (pips); 0 desactiva el filtro. | 1 |
| `CommissionInPips` | Comisión (en pips) añadida al filtro de spread. | 0 |
| `AutoLotSize` | Activa dimensionamiento basado en riesgo. | `false` |
| `RiskFactor` | Porcentaje de patrimonio usado para dimensionar posiciones con auto sizing activo. | 10 |
| `ManualVolume` | Volumen fijo usado cuando auto sizing está desactivado. | 0.1 |
| `UseTimeFilter` | Activa el filtro de sesión. | `false` |
| `TradeStartTime` | Hora de inicio de sesión (incluida). | 00:00:00 |
| `TradeStopTime` | Hora de fin de sesión (excluida). | 00:00:00 |
| `ModifyThreshold` | Delta de precio (pips) requerido antes de volver a registrar una orden pendiente. | 1 |

## Notas de uso
* Asegúrese de que el instrumento objetivo proporcione `Step`, `PriceStep`, `StepPrice`, `LotStep`, `MinVolume` y `MaxVolume`, porque el tamaño automático depende de estos valores. Si faltan datos, la estrategia vuelve con seguridad a `ManualVolume`.
* El valor de pip se estima desde la precisión decimal y paso de precio del instrumento, coincidiendo con la lógica MQL original (incluido manejo especial de cotizaciones Forex de 3/5 dígitos).
* Si `TradeStartTime` es igual a `TradeStopTime` y `UseTimeFilter` está activo, la sesión se considera siempre abierta. Si la hora de inicio es mayor que la de fin, la sesión cruza medianoche.
* La validación de spread añade `CommissionInPips` al spread actual, replicando la versión MQL que combinaba spread y comisión en un único filtro.
* La estrategia no crea ni gestiona objetos de gráfico. La visualización puede añadirse externamente enlazando datos level-1 a gráficos.

## Diferencias frente al EA original
* El temporizador de ticks de bajo nivel y los elementos GUI de la versión MQL se omiten intencionalmente. La variante StockSharp usa eventos level-1 y logging integrado.
* La lógica de modificación de órdenes se simplifica: si el precio objetivo difiere más de `ModifyThreshold` pips, la orden se vuelve a registrar en lugar de usar la lógica multirrama del EA.
* La detección automática de comisiones desde historial de operaciones se reemplaza por el parámetro estático `CommissionInPips`; aun así, el filtro de riesgo añade este valor al spread antes de operar.
* La versión StockSharp usa `StartProtection()` en lugar de bucles personalizados de seguimiento de stops.

## Pruebas históricas
La estrategia requiere datos de cotizaciones level-1 para reproducir la lógica de activación de órdenes stop. Para simulaciones históricas, proporcione series bid/ask o construya datos level-1 sintéticos desde historial de ticks. Feeds solo de velas son insuficientes porque las órdenes stop pendientes deben reaccionar a cambios de spread.
