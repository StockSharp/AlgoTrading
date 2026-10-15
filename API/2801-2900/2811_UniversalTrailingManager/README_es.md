# Estrategia de Gestor de Trailing Universal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia de Gestor de Trailing Universal** es una conversión en C# del asesor experto de MetaTrader "Universal 1.64 (edición de barabashkakvn)".
Automatiza las tareas de gestión de operaciones para el trading discrecional o semiautomático, manejando entradas programadas, órdenes
pendientes en cuadrícula, trailing dinámico para órdenes de mercado y pendientes, scalping de beneficios rápidos y notificaciones
a nivel de cartera cuando el capital de la cuenta se mueve un porcentaje definido.

La estrategia está diseñada para ejecutarse en cualquier instrumento que exponga datos de velas. No depende de indicadores; en su lugar
reacciona a niveles de precio y ventanas de tiempo, lo que la hace adecuada para confirmación manual de señales o integración en flujos
de trabajo más amplios de gestión de operaciones.

## Características principales

- **Acciones programadas**: abre posiciones de mercado o coloca órdenes pendientes automáticamente en una hora específica del terminal (hora/minuto).
- **Cuadrícula de órdenes pendientes**: mantiene hasta una orden de compra limitada, venta limitada, compra stop y venta stop, cada una con
  desplazamientos independientes, trailing opcional y re-registro automático cuando el precio se mueve a favor de la orden pendiente.
- **Protección de posición de mercado**: aplica lógica de stop-loss, take-profit y trailing a la posición agregada actual, incluida la
  opción de esperar a que haya beneficio no realizado antes de que comience el trailing.
- **Salida de scalping**: cierra posiciones existentes una vez que el precio avanza un número fijo de puntos desde el precio medio de entrada.
- **Alertas de cartera**: monitorea el capital de la cartera y registra mensajes cuando la cuenta crece o disminuye el porcentaje configurado.
- **Control de posición**: admite el modo "esperar hasta que se cierre la posición" así como un límite configurable en el número de posiciones
  abiertas por dirección antes de aceptar nuevas entradas u órdenes pendientes.

## Parámetros

| Grupo | Parámetro | Descripción |
|-------|-----------|-------------|
| General | `TradeVolume` | Volumen de orden en lotes usado para entradas de mercado y pendientes. |
| General | `WaitClose` | Cuando es `true`, las nuevas órdenes solo se permiten si el número de posiciones abiertas en esa dirección está por debajo de `MaxMarketPositions`. |
| Mercado | `MaxMarketPositions` | Número máximo de posiciones activas por dirección cuando `WaitClose` está habilitado. |
| Mercado | `MarketTakeProfitPoints` | Distancia de take-profit (en puntos de precio) aplicada a posiciones abiertas. Poner en 0 para deshabilitar. |
| Mercado | `MarketStopLossPoints` | Distancia de stop-loss (en puntos de precio) aplicada a posiciones abiertas. Poner en 0 para deshabilitar. |
| Mercado | `MarketTrailingStopPoints` | Distancia de trailing stop (en puntos de precio). Poner en 0 para deshabilitar el trailing. |
| Mercado | `MarketTrailingStepPoints` | Mejora mínima (en puntos) requerida antes de que se mueva el trailing stop. |
| Mercado | `WaitForProfit` | Cuando está habilitado, el trailing comienza solo después de que el beneficio supere `MarketTrailingStopPoints`. |
| Mercado | `ScalpProfitPoints` | Umbral de beneficio (en puntos) que desencadena un cierre inmediato de posición. Poner en 0 para deshabilitar el scalping. |
| Pendientes | `AllowBuyLimit`, `AllowSellLimit`, `AllowBuyStop`, `AllowSellStop` | Interruptores principales para cada tipo de orden pendiente. |
| Pendientes | `LimitOrderOffsetPoints`, `StopOrderOffsetPoints` | Distancia desde el precio de cierre actual para colocar la orden limitada/stop correspondiente. Debe estar por encima de la distancia mínima de stop del instrumento. |
| Pendientes | `LimitOrderTakeProfitPoints`, `StopOrderTakeProfitPoints` | Objetivo de beneficio (puntos) adjunto a posiciones recién abiertas creadas por órdenes pendientes. |
| Pendientes | `LimitOrderStopLossPoints`, `StopOrderStopLossPoints` | Stop protector (puntos) adjunto a posiciones recién abiertas creadas por órdenes pendientes. |
| Pendientes | `LimitOrderTrailingStopPoints`, `StopOrderTrailingStopPoints` | Distancia de trailing para órdenes pendientes activas. Cero deshabilita la lógica de trailing. |
| Pendientes | `LimitOrderTrailingStepPoints`, `StopOrderTrailingStepPoints` | Mejora mínima requerida antes de que se mueva una orden pendiente durante el trailing. |
| Tiempo | `UseTime` | Habilita el bloque de acción programada. |
| Tiempo | `TimeHour`, `TimeMinute` | Hora del terminal cuando se evalúa el bloque programado. |
| Tiempo | `TimeBuy`, `TimeSell` | Abrir posiciones de compra/venta de mercado en la hora programada. |
| Tiempo | `TimeBuyLimit`, `TimeSellLimit`, `TimeBuyStop`, `TimeSellStop` | Colocar la orden pendiente correspondiente en la hora programada independientemente de los interruptores de permiso principales. |
| Global | `UseGlobalLevels` | Habilita el monitoreo a nivel de cartera. |
| Global | `GlobalTakeProfitPercent`, `GlobalStopLossPercent` | Umbrales de porcentaje de capital que desencadenan mensajes de registro informativos. |
| Datos | `CandleType` | Tipo de vela usado para el procesamiento periódico (predeterminado: 1 minuto). |

## Flujo de ejecución

1. **Llegada de vela**: en cada vela finalizada la estrategia actualiza las referencias de órdenes, sincroniza las señales programadas y
   evalúa la lógica de trading.
2. **Ventana de tiempo**: si el cierre de la vela coincide con la ventana de tiempo configurada, los booleanos apropiados (`TimeBuy`, etc.)
   se establecen y las órdenes de mercado/pendientes se registran inmediatamente.
3. **Órdenes pendientes**: la estrategia coloca una orden pendiente por tipo. Cuando el movimiento de precio satisface las reglas de trailing,
   la orden se cancela y se re-emite más cerca del mercado con el desplazamiento conservado.
4. **Protección de mercado**: para posiciones abiertas la estrategia mantiene órdenes de stop-loss y take-profit dedicadas, ajustándolas
   según la configuración de trailing y asegurando que los volúmenes coincidan con la posición agregada.
5. **Verificación de scalping**: si `ScalpProfitPoints` es positivo, la posición se cierra cuando el precio de cierre actual alcanza el
   delta objetivo desde el precio de posición promedio.
6. **Alertas globales**: el capital de la cartera se verifica en cada ciclo; los mensajes informativos se registran una vez que se alcanzan
   los umbrales.

## Notas de uso

- Coloque la estrategia dentro de un esquema de trading donde las velas se entreguen continuamente (por ejemplo, velas de 1 minuto). La
  lógica está impulsada por velas, por lo que un período de tiempo más fino produce un trailing más receptivo.
- La estrategia usa la propiedad `Position` agregada. Al invertir de corto a largo (o viceversa), el tamaño de la orden ejecutada se
  incrementa automáticamente para aplanar la posición existente antes de abrir la nueva.
- Los desplazamientos de órdenes pendientes y los pasos de trailing se miden en *puntos de precio* (múltiplos de `Security.PriceStep`).
  Asegúrese de que el valor de paso del instrumento esté configurado correctamente; de lo contrario, la estrategia vuelve a un tamaño de
  paso de 1.
- El monitoreo global de ganancias/pérdidas proporciona solo mensajes de registro informativos. No cierra posiciones automáticamente; esto
  refleja el comportamiento del asesor experto original.
- Cuando `WaitClose` está habilitado, el número de posiciones abiertas por lado se deriva de la posición agregada dividida por `TradeVolume`.
  Use tamaños de volumen consistentes para obtener un comportamiento de control preciso.

## Registro

Cada acción significante — colocación de órdenes, ajustes de trailing y alertas de nivel global — se escribe en el registro de la estrategia
vía `LogInfo`. Monitoree el registro para rastrear el proceso de decisión, especialmente mientras ajusta los desplazamientos y los parámetros
de trailing.
