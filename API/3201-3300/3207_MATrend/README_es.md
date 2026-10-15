# 3207 – Estrategia de MA Trend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de MA Trend** replica el experto de MetaTrader *MA Trend.mq5* usando la API de alto nivel de StockSharp. El bot sigue una única media móvil ponderada lineal con un desplazamiento hacia adelante configurable. Cuando el precio de cierre sube por encima de la media desplazada, la estrategia toma posiciones largas, mientras que una caída por debajo de la media abre posiciones cortas. Las reglas opcionales de stop-loss, take-profit y trailing stop replican los controles de riesgo de la implementación MQL original.

## Lógica de trading
1. Suscribirse al tipo de vela configurado (por defecto marco temporal de 1 minuto) y calcular una media móvil usando el método seleccionado y la fuente de precio.
2. Desplazar el valor de la media móvil hacia adelante por el número solicitado de velas completadas antes de compararlo con el cierre más reciente.
3. Generar señales:
   - **Largo** – precio de cierre por encima de la MA desplazada (invertido cuando `ReverseSignals` está habilitado).
   - **Corto** – precio de cierre por debajo de la MA desplazada (invertido cuando `ReverseSignals` está habilitado).
4. Aplicar opciones de gestión de posición:
   - Cerrar la exposición opuesta antes de abrir una operación cuando `CloseOpposite` es `true`.
   - Bloquear nuevas entradas si `OnlyOnePosition` está habilitado y ya existe una posición.
5. Gestionar salidas con distancias de stop-loss, take-profit y trailing stop expresadas en pips. La lógica de seguimiento requiere que el precio se mueva por `TrailingStopPips + TrailingStepPips` antes de ajustar el stop, igual que el experto MQL.

## Parámetros
| Nombre | Tipo | Predeterminado | Descripción |
|------|------|---------|-------------|
| `OrderVolume` | `decimal` | `0.1` | Tamaño de orden en lotes/contratos. |
| `StopLossPips` | `int` | `50` | Distancia del stop-loss en pips. Cero deshabilita el stop fijo. |
| `TakeProfitPips` | `int` | `140` | Distancia del take-profit en pips. Cero deshabilita el objetivo. |
| `TrailingStopPips` | `int` | `15` | Distancia del trailing stop. Establecer en cero para deshabilitar el seguimiento. |
| `TrailingStepPips` | `int` | `5` | Pips adicionales requeridos antes de mover el trailing stop. Debe permanecer positivo cuando `TrailingStopPips` es mayor que cero. |
| `MaPeriod` | `int` | `12` | Longitud de la media móvil. |
| `MaShift` | `int` | `3` | Número de barras completadas usadas para desplazar la media móvil hacia adelante. |
| `MaMethod` | `MovingAverageKind` | `Weighted` | Modo de cálculo de la media móvil (Simple, Exponential, Smoothed, Weighted). |
| `AppliedPrice` | `AppliedPriceMode` | `Weighted` | Precio de vela usado como entrada del indicador (Close, Open, High, Low, Median, Typical, Weighted). |
| `OnlyOnePosition` | `bool` | `false` | Restringir la estrategia a una sola posición abierta. |
| `ReverseSignals` | `bool` | `false` | Intercambiar las direcciones de señal largo/corto. |
| `CloseOpposite` | `bool` | `false` | Cerrar la exposición opuesta antes de entrar en una nueva posición. |
| `CandleType` | `DataType` | `1 minute` | Tipo de vela/marco temporal suministrado al indicador. |

## Notas
- El tamaño de pip se adapta automáticamente a instrumentos con precios de 3/5 decimales para coincidir con el comportamiento original de MetaTrader.
- La validación del trailing stop reproduce la verificación MQL: si `TrailingStopPips > 0` y `TrailingStepPips <= 0`, la estrategia lanza una excepción durante el inicio.
- Todas las actualizaciones de indicadores y decisiones de órdenes usan únicamente velas terminadas, garantizando backtests deterministas.
