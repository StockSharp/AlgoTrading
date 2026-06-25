# Estrategia de Sombras de Velas V1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Sombras de Velas V1 es una estrategia de reversión de acción del precio que recrea la lógica del asesor experto MetaTrader original dentro de la API de alto nivel de StockSharp. El sistema busca velas con una mecha dominante fuerte y sombra opuesta mínima durante una sesión de trading configurable. Los trades solo se permiten durante los primeros minutos de una barra, emulando la ejecución intrabarra de la versión MQL mientras sigue trabajando en velas cerradas.

## Lógica de trading
1. Suscribirse a las velas del marco temporal configurado (por defecto 5 minutos) y evaluar solo las barras terminadas.
2. Aplicar una ventana de sesión usando los parámetros `StartHour` y `EndHour`. Si la vela se abre fuera de la ventana no se considera ningún trade.
3. Permitir entradas solo si la vela cierra antes de `OpenWithinMinutes` desde su tiempo de apertura, previniendo señales tardías en barras largas.
4. Configuración larga: la vela debe imprimir una sombra inferior mayor que `CandleSizeMinPips` pips y la sombra superior debe mantenerse dentro de `OppositeShadowMaxPips` pips. Cuando se satisfacen las condiciones y no hay posición abierta se envía una compra de mercado.
5. Configuración corta: la vela debe imprimir una sombra superior mayor que `CandleSizeMinPips` pips y la sombra inferior debe mantenerse dentro de `OppositeShadowMaxPips` pips. Se emite una venta de mercado si la cuenta está plana.
6. Solo se permite un trade por vela, coincidiendo con la restricción original de "una orden por barra".

## Gestión de posición
- Las distancias protectoras iniciales se expresan en pips y se convierten a través del parámetro `PipValue` para cada instrumento.
- Las verificaciones duras de stop-loss y take-profit se realizan en cada vela terminada. Si el máximo/mínimo de la vela toca el umbral la posición se aplana.
- La gestión de trailing imita el trailing stop de MQL: una vez que el precio avanza al menos `TrailingStopPips + TrailingStepPips` el stop se mueve en incrementos de `TrailingStepPips` pips.
- Si una posición permanece abierta más de `PositionLivesBars` barras se cierra inmediatamente. Los trades rentables también se fuerzan a salir después de `CloseProfitsOnBar` barras para asegurar ganancias.
- El volumen del próximo trade se reduce dividiendo `BaseVolume` por `LossReductionFactor` cada vez que el trade anterior cerró con una pérdida, igual que la reducción de lotes en el asesor experto original.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `PipValue` | Valor monetario de un pip usado para transformar distancias en pips en desplazamientos de precio. | `0.0001` |
| `StopLossPips` | Distancia de stop-loss en pips. Establecer en `0` para deshabilitar el stop duro. | `50` |
| `TakeProfitPips` | Distancia de take-profit en pips. Establecer en `0` para deshabilitar el objetivo duro. | `50` |
| `TrailingStopPips` | Distancia del trailing stop en pips. Cuando es `0` no se aplica trailing. | `15` |
| `TrailingStepPips` | Paso mínimo en pips entre ajustes del trailing stop. Debe ser positivo cuando el trailing está habilitado. | `5` |
| `PositionLivesBars` | Número máximo de barras completadas que una posición puede permanecer abierta antes de forzarse a cerrar. | `4` |
| `CloseProfitsOnBar` | Cuando es mayor que cero, las posiciones rentables se cierran después de este número de barras desde la entrada. | `2` |
| `OpenWithinMinutes` | Cantidad máxima de minutos después de la apertura de la barra cuando se permiten nuevos trades. | `7` |
| `CandleSizeMinPips` | Longitud de mecha requerida (en pips) en el lado dominante de la vela. | `15` |
| `OppositeShadowMaxPips` | Tamaño máximo (en pips) de la sombra de vela opuesta. | `1` |
| `StartHour` | Hora de inicio de sesión en tiempo de bolsa (0–23). | `6` |
| `EndHour` | Hora de fin de sesión en tiempo de bolsa (0–23). | `18` |
| `LossReductionFactor` | Divisor aplicado a `BaseVolume` después de un trade perdedor. | `1.5` |
| `BaseVolume` | Tamaño de orden de mercado predeterminado usado para entradas. | `1` |
| `CandleType` | Serie de velas usada para los cálculos. Por defecto es un marco temporal de 5 minutos. | `5 min` |

## Notas
- Siempre ajustar `PipValue` para que coincida con el tamaño del tick del instrumento (por ejemplo `0.01` para cruces JPY o `1` para futuros de índices).
- Dado que la estrategia trabaja con velas completadas, las ejecuciones ocurrirán al cierre de la barra. Los marcos temporales más bajos (1–5 minutos) replican mejor el comportamiento intrabarra del asesor experto original.
- No se requieren indicadores externos, lo que hace que la estrategia sea fácil de ejecutar en cualquier fuente de datos StockSharp.
