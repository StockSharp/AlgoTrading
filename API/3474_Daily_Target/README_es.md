# Estrategia objetivo diaria
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

`DailyTargetStrategy` replica el MetaTrader 4 asesores expertos "Daily Target". La estrategia mantiene el comercio de posiciones abiertas hasta
las ganancias y pérdidas combinadas para el día calendario actual alcanzan un objetivo de ganancias configurado o superan un límite máximo de pérdidas. como
Tan pronto como se alcanza cualquiera de los umbrales, todas las órdenes activas se cancelan y la posición se aplana, de modo que la negociación permanece en pausa hasta el
comienza el día siguiente.

## Lógica de trading

1. **Puesta en marcha**
   - La estrategia llama a `ResetDailySnapshot` durante `OnStarted` para almacenar la fecha actual y la línea base de PnL realizada.
   - `SubscribeLevel1()` ofrece actualizaciones de oferta/demanda que son necesarias para evaluar las ganancias flotantes con precisión.
   - `SubscribeTrades()` captura el último precio ejecutado y proporciona un respaldo cuando faltan cotizaciones.
   - Un tick `Timer` de un minuto garantiza que se detecten cambios de fecha incluso cuando no lleguen datos de mercado.
2. **Evaluación PnL**
   - `EvaluateDailyThresholds` vuelve a calcular el PnL realizado ({PH001}} actual menos la línea base almacenada) y agrega el PnL flotante
calculado a partir de la última oferta/demanda o del último precio comercial.
   - Si el PnL diario total cruza el objetivo configurado o cae por debajo del límite de pérdida negativa, la estrategia llama
`TriggerDailyStop`.
3. **Salida de emergencia**
   - `TriggerDailyStop` escribe una entrada de registro informativa, cancela todas las órdenes pendientes y envía la orden de mercado adecuada a
aplanar la exposición larga o corta restante.
   - `_dailyStopTriggered` impide el reingreso durante el mismo día. Cuando cambia la fecha del calendario, `ResetDailySnapshot` borra esto
marca y registra una nueva línea base de PnL.

## Parámetros

| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `DailyTarget` | `10` | Objetivo de beneficio en la moneda de la cartera. Las operaciones se detienen por el resto del día una vez que el PnL diario total alcanza o excede este valor. |
| `DailyMaxLoss` | `0` | Pérdida máxima tolerada en la moneda de la cartera. Establezca en cero para desactivar el filtro de pérdida. La negociación se detiene durante el día una vez que el PnL diario total cae por debajo del umbral negativo. |

## Notas

- La estrategia solo administra el `Security` principal asignado a la instancia de la estrategia, reflejando el comportamiento de un solo símbolo del
MQL experto.
- PnL flotante utiliza la mejor oferta para posiciones largas y la mejor demanda para posiciones cortas. Si no hay cotización disponible, la última operación
El precio actúa como un respaldo para evitar detener la evaluación.
- No se proporciona ningún puerto Python; En este paquete solo se incluye la implementación de alto nivel de C#.
