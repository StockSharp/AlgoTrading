# Estrategia de Secuencia de N Velas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia de Secuencia de N Velas replica el comportamiento del experto MetaTrader original "N-_Candles_v7" usando la API de alto nivel de StockSharp. Monitorea velas finalizadas y busca un número configurable de cuerpos consecutivos alcistas o bajistas. Cuando hay una racha calificada presente, la estrategia abre una posición en la misma dirección y la gestiona con take profit, stop loss, trailing stop, filtro de horas de trading y bloqueo de beneficio flotante configurables.

## Lógica de trading
- Evalúa cada vela finalizada y la clasifica como alcista, bajista o neutral (doji). Las velas neutras reinician el contador de racha y pueden activar el comportamiento de "oveja negra".
- Mantiene un recuento continuo de velas consecutivas con la misma dirección de cuerpo. Una vez que el recuento alcanza el umbral configurado, la dirección actual se convierte en el patrón activo.
- Cuando hay una racha alcista activa, la estrategia intenta abrir una posición larga; cuando hay una racha bajista activa, intenta abrir una posición corta. Solo se mantiene una posición neta a la vez.
- Si una vela rompe la dirección uniforme ("oveja negra"), la estrategia reacciona según el modo de cierre seleccionado: cerrar todo, cerrar solo posiciones opuestas, o cerrar solo posiciones alineadas con la racha anterior.
- Opcionalmente, restringe las entradas a una ventana de trading definida por horas de inicio y fin (inclusivas).
- Monitorea continuamente la posición abierta para take profit, stop loss, actualizaciones de trailing stop y el umbral de beneficio flotante.

## Gestión de posición y riesgo
- El stop inicial de protección y el objetivo se calculan a partir de distancias en pips convertidas con el paso de precio del instrumento. Estos niveles se recalculan cada vez que se abre una nueva posición.
- La lógica del trailing stop imita el experto original: una vez que el precio recorre la distancia de trailing más el paso, el stop se mueve para mantener la distancia de trailing.
- Un guardián de beneficio flotante (`MinProfit`) cierra toda la posición una vez que el beneficio abierto supera el valor configurado.
- El parámetro `MaxPositionVolume` evita entradas si el volumen solicitado supera el límite permitido. `MaxPositions` funciona como protección contra la re-entrada cuando ya hay una posición activa.
- Todas las salidas llaman a órdenes de mercado para aplanar la posición neta porque la estrategia de StockSharp opera en un entorno de compensación.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `ConsecutiveCandles` | Número de velas con dirección idéntica necesarias para activar una señal. |
| `OrderVolume` | Volumen de orden de mercado usado para entradas y salidas. |
| `TakeProfitPips` | Distancia de take profit expresada en pips. Poner en cero para deshabilitar. |
| `StopLossPips` | Distancia de stop loss expresada en pips. Poner en cero para deshabilitar. |
| `TrailingStopPips` | Distancia del trailing stop. Poner en cero para deshabilitar el trailing. |
| `TrailingStepPips` | Distancia adicional requerida antes de que se mueva el trailing stop. |
| `MaxPositions` | Número máximo de entradas simultáneas por patrón (la estrategia mantiene una sola posición neta). |
| `MaxPositionVolume` | Límite superior para el volumen neto permitido. |
| `UseTradeHours` / `StartHour` / `EndHour` | Habilitar y configurar la ventana de tiempo de trading (inclusiva). |
| `MinProfit` | Umbral de beneficio flotante que activa una salida completa. |
| `ClosingBehavior` | Define cómo reaccionar cuando aparece una vela de "oveja negra". |
| `CandleType` | La serie de velas usada para los cálculos. |

## Notas y supuestos
- La estrategia opera con posiciones netas; no se crean múltiples tickets de estilo cobertura. Esto difiere del experto original donde varias posiciones cubiertas podían coexistir.
- El beneficio flotante se aproxima como `(precio actual - precio de entrada) * volumen` para posiciones largas y lo inverso para posiciones cortas.
- La conversión de pips depende del `PriceStep` del instrumento. Para símbolos donde no se proporciona el paso mínimo, se asume un pip predeterminado de 0.0001.
- No se proporciona portación a Python, según lo solicitado.
