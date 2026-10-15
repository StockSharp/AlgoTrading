# Estrategia RHMP sin engaños (puerto StockSharp)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia transfiere el asesor experto de MetaTrader **Trickerless RHMP** al nivel alto de StockSharp API. Mantiene las múltiples etapas.
lógica de entrada del robot original: combina la confirmación del índice direccional promedio, la estructura de promedio móvil suavizada y
Gestión de posiciones basada en la volatilidad, siguiendo las convenciones marco documentadas en `AGENTS.md`.

## Lógica de trading

1. **Indicadores**
   - Rango verdadero promedio (ATR) con período configurable para dimensionar la volatilidad.
   - Índice direccional promedio (ADX) con componentes completos +DI/-DI para calificar la fuerza de la tendencia.
   - Dos promedios móviles suavizados (SMMA) que representan los filtros de tendencia rápida y lenta.

2. **Evaluación de tendencias**
   - La pendiente lenta de SMMA debe estar dentro del corredor `MinSlopePips`…`MaxSlopePips` (medido en pips de instrumento).
   - ADX debe exceder `AdxThreshold` y aumentar en comparación con la vela anterior.
   - El precio debe mantenerse al menos `TrendSpacePips` alejado del SMMA rápido para evitar la congestión.
   - Un sesgo alcista requiere la SMMA rápida por encima de la SMMA lenta, +DI ≥ -DI y un promedio ascendente rápido. El sesgo bajista refleja estos
cheques.

3. **Entradas principales**
   - Cuando el sesgo alcista (o bajista) está activo, la estrategia abre una orden larga (o corta) con volumen `OrderVolume`, respetando
`MaxNetPositions` y esperando al menos `SleepInterval` entre entradas.
   - Si existe una posición neta opuesta, primero se aplana para mantener la cobertura desactivada.

4. **Entradas con picos**
   - Si el rango de velas actual excede `CandleSpikeMultiplier` veces el rango anterior, la estrategia puede disparar un auxiliar
posición en la dirección del cuerpo de la vela cuando los componentes ADX concuerden. La posición utiliza `OrderVolume * SpikeVolumeMultiplier`.

## Gestión del riesgo

- Stop-loss, take-profit y trailing-stop opcionales basados en ATR (`StopLossAtrMultiplier`, `TakeProfitAtrMultiplier`, `TrailingAtrMultiplier`).
- Protección para toda la sesión: una vez que el PnL alcanzado alcanza `DailyProfitTarget` (fracción del capital inicial), se bloquean las nuevas entradas.
- El interruptor de emergencia global `EmergencyExit` cierra todas las posiciones inmediatamente cuando se activa.

## Parámetros

| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `CandleType` | Plazo utilizado para todos los cálculos. | velas de 5 minutos |
| `OrderVolume` | Volumen base para cada entrada. | 0,03 |
| `AtrPeriod` | ATR longitud retrospectiva. | 14 |
| `AdxPeriod` | ADX longitud retrospectiva. | 14 |
| `AdxThreshold` | Valor mínimo de ADX para permitir el comercio. | 10 |
| `FastMaPeriod` | Período de media móvil suavizado rápidamente. | 60 |
| `SlowMaPeriod` | Período de media móvil suavizado lentamente. | 120 |
| `MinSlopePips` / `MaxSlopePips` | Corredor de pendiente permitido para el SMMA lento. | 2 / 9 |
| `TrendSpacePips` | Distancia mínima de precio desde la SMMA rápida (en pips). | 5 |
| `CandleSpikeMultiplier` | Cuánto mayor debe ser el rango de la vela para desencadenar entradas pico. | 7 |
| `TakeProfitAtrMultiplier` | ATR múltiplos para obtener ganancias. | 1.0 |
| `StopLossAtrMultiplier` | ATR múltiplos para stop-loss. | 1.5 |
| `TrailingAtrMultiplier` | ATR múltiplos para trailing-stop (0 desactivaciones). | 0 |
| `MaxNetPositions` | Número máximo de unidades de posición neta simultáneas. | 1 |
| `SleepInterval` | Tiempo mínimo entre entradas consecutivas. | 24 minutos |
| `DailyProfitTarget` | Fracción del capital inicial que bloquea la negociación una vez alcanzado. | 0,045 |
| `AllowNewEntries` | Interruptor maestro para habilitar/deshabilitar entradas. | cierto |
| `SpikeVolumeMultiplier` | Multiplicador de volumen para entradas de picos. | 1.0 |
| `EmergencyExit` | Cierra todas las posiciones inmediatamente cuando es verdadero. | falso |

## Notas

- El puerto StockSharp se centra en el nivel alto limpio API en lugar de la microgestión billete por billete de MetaTrader. Todos
La lógica de administración del dinero se implementa a través de niveles basados en `Volume` y ATR.
- El EA original tenía varias comprobaciones de saldo y margen. Estos se aproximan con el `DailyProfitTarget`, `MaxNetPositions`
y ATR parámetros de tamaño para que el comportamiento se mantenga alineado sin llamadas directas a la cuenta MT4.
- Debido a que la estrategia utiliza promedios suavizados, asegúrese de que haya un período de calentamiento suficiente antes de evaluar las operaciones.
