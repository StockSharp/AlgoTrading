# Estrategia de Rompimiento de Sesión de 21 Horas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia reproduce el asesor experto "21hour" de MetaTrader dentro de StockSharp. Opera durante dos ventanas de trading configurables y usa órdenes stop pendientes para capturar rompimientos en la parte superior e inferior del rango. Al final de cada ventana, la estrategia liquida cualquier exposición abierta y elimina las órdenes activas, asegurando que cada día de trading comience limpio.

## Idea central

- La dirección de las operaciones se determina puramente por la acción del precio alrededor de los tiempos de inicio de sesión especificados.
- Al comienzo de cada sesión, la estrategia rodea el mercado con un buy stop por encima del ask actual y un sell stop por debajo del bid actual.
- Cuando se ejecuta una orden stop, el lado contrario se cancela inmediatamente y se coloca una orden de take-profit a distancia fija.
- Al tiempo de finalización de sesión configurado, cada posición se cierra y todas las órdenes se cancelan, incluso si el take-profit aún no se ha alcanzado.

## Flujo de datos

- **Velas:** Las velas de 1 minuto (configurables) se usan solo para proporcionar marcas de tiempo y activar las verificaciones de horario.
- **Libro de órdenes:** Las cotizaciones de Nivel 1 suministran los mejores valores actuales de bid/ask que definen los precios de activación de las órdenes stop.

## Reglas de trading

### Programación de entradas
- A las `FirstSessionStartHour` (8:00 hora del servidor por defecto) y a las `SecondSessionStartHour` (22:00 por defecto), la estrategia:
  - Coloca un buy stop en `Ask + StepPoints * PriceStep`.
  - Coloca un sell stop en `Bid - StepPoints * PriceStep`.
- Solo se permite una posición. Si ya hay una posición abierta cuando comienza la otra sesión, todas las órdenes de entrada pendientes se eliminan antes de colocar nuevas.

### Gestión de órdenes
- Cuando se ejecuta una de las órdenes stop, el stop contrario se cancela inmediatamente.
- Se registra una orden de take-profit a `EntryPrice ± TakeProfitPoints * PriceStep` según la dirección de la operación.
- Los tamaños de las órdenes son fijos por el parámetro `Volume` (1 lote por defecto).

### Lógica de salida
- Las órdenes de take-profit cierran las operaciones ganadoras automáticamente.
- A las `FirstSessionStopHour` (21:00 por defecto) y a las `SecondSessionStopHour` (23:00), la estrategia cierra cualquier posición abierta a mercado y cancela todas las órdenes pendientes restantes.
- Si la posición se cierra manualmente, la estrategia también elimina la orden de take-profit pendiente.

## Parámetros

| Parámetro | Predeterminado | Descripción |
|-----------|----------------|-------------|
| `Volume` | `1` | Volumen de orden usado para entradas stop y salidas de take-profit. |
| `FirstSessionStartHour` | `8` | Hora (0-23) cuando comienza la primera sesión de trading. |
| `FirstSessionStopHour` | `21` | Hora cuando termina la primera sesión y se cierran las posiciones. |
| `SecondSessionStartHour` | `22` | Hora cuando comienza la sesión de tarde. Debe ser después de la primera sesión. |
| `SecondSessionStopHour` | `23` | Hora cuando termina la segunda sesión. Debe ser después del stop de la primera sesión. |
| `StepPoints` | `5` | Distancia desde la mejor cotización a la orden de entrada stop, medida en pasos de precio. |
| `TakeProfitPoints` | `40` | Distancia entre el precio de entrada y el límite de take-profit, medida en pasos de precio. |
| `CandleType` | `1 minuto` | Tipo de vela usado para impulsar las verificaciones de horario intradía. |

Todos los parámetros se validan para evitar sesiones superpuestas o combinaciones de horas imposibles.

## Etiquetas y características

- **Estilo:** Rompimiento de sesión / seguimiento de tendencia basado en tiempo.
- **Dirección:** Largo y corto.
- **Marco temporal:** Intradía, impulsado por horario (velas de 1 minuto solo para temporización).
- **Controles de riesgo:** Take-profit fijo más cierre forzado al final de la sesión (sin stop-loss).
- **Tipos de mercado:** Diseñado para FX, índices, o cualquier instrumento con horarios de trading continuos y cotizaciones confiables.
- **Complejidad:** Baja – sin indicadores, puramente basado en tiempo y precio.

## Notas de implementación

- La estrategia requiere un `Security.PriceStep` válido; las órdenes se omiten si el paso de precio o las cotizaciones no están disponibles.
- Los volúmenes de take-profit usan el volumen de la operación ejecutada cuando está disponible, recurriendo a la posición actual o al volumen configurado.
- El código mantiene comentarios en inglés para mayor claridad y refleja la lógica MQL original mientras aprovecha las APIs de alto nivel de StockSharp (`SubscribeCandles`, `SubscribeOrderBook`, parámetros auxiliares y helpers de órdenes).
