# Estrategia MACD EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es un port de StockSharp del asesor experto de MetaTrader 5 `MACD EA (barabashkakvn's edition).mq5` de la carpeta `MQL/20010`. Recrea la misma lógica de cruce de MACD, toma de ganancias parcial y funciones de gestión monetaria utilizando el API de alto nivel de StockSharp.

## Lógica de trading

* **Fuente de señal** – Se calcula un indicador MACD clásico con períodos rápidos, lentos y de señal configurables. La estrategia examina la diferencia entre la línea MACD y la línea de señal dos y cuatro velas completadas atrás. Un cruce alcista (la diferencia pasa de negativa a positiva) abre una operación larga, mientras que la condición opuesta abre una operación corta.
* **Gestión de posición** – Cada orden está protegida por offsets configurables de stop-loss y take-profit medidos en pips. Los offsets se convierten a precios utilizando el paso de precio del instrumento y multiplicando por diez cuando el instrumento tiene 3 o 5 decimales, imitando el ajuste de punto del EA original.
* **Ganancia parcial** – Cuando está habilitada, la mitad de la posición abierta se cierra una vez que el precio avanza `PartialProfitPips` en la dirección de la operación. La parte restante continúa.
* **Breakeven** – Después de que el precio avanza `BreakevenPips` a favor, la estrategia activa un guardia de breakeven. Si el precio regresa al nivel de entrada original, la posición se cierra al precio de entrada, igual que el EA mueve el stop a breakeven.
* **Señal MACD opuesta** – Un cruce MACD opuesto cierra cualquier exposición restante inmediatamente, asegurando que la estrategia nunca mantenga una posición contra la tendencia del indicador.

## Gestión monetaria

Cuando `UseMoneyManagement` está habilitado, el tamaño de la posición aumenta después de operaciones perdedoras consecutivas. La siguiente operación usa un multiplicador basado en el número de pérdidas consecutivas (x2 después de una pérdida, x3 después de dos pérdidas, hasta x7 para seis o más pérdidas). El multiplicador se combina con el parámetro `RiskMultiplier` para reproducir el dimensionamiento estilo martingala del código original. Las operaciones ganadoras restablecen el contador de pérdidas a cero.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `FastPeriod` / `SlowPeriod` / `SignalPeriod` | Longitudes de cálculo del MACD.
| `StopLossPips` | Distancia al stop de protección en pips (0 lo deshabilita).
| `TakeProfitPips` | Distancia al objetivo de ganancia en pips (0 lo deshabilita).
| `PartialProfitPips` | Pips necesarios para cerrar la mitad de la posición (0 deshabilita la salida parcial).
| `BreakevenPips` | Pips requeridos antes de que se active el modo breakeven (0 deshabilita el breakeven).
| `UseMoneyManagement` | Habilita el dimensionamiento dinámico de posición basado en la racha de pérdidas.
| `RiskMultiplier` | Multiplicador adicional aplicado cuando la gestión monetaria está activa.
| `BaseVolume` | Volumen base de operación antes de cualquier escala.
| `CandleType` | Serie de velas usada para los cálculos de indicadores.

## Notas

* La estrategia usa `SubscribeCandles` y vinculación de indicadores para seguir el patrón recomendado del API de alto nivel.
* Una versión en Python aún no está disponible. Solo se proporciona la implementación en C# en la carpeta `CS`.
* No se añadieron ni modificaron pruebas según lo solicitado.
