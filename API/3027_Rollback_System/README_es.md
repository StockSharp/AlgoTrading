# Estrategia Rollback System
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una conversión en C# del asesor experto de MetaTrader 5 **"Rollback system"**. Mantiene la idea original de
operar al comienzo de un nuevo día de trading, evaluando las últimas 24 velas horarias para detectar si el mercado ha entregado
un movimiento extendido que es probable que retroceda.

## Lógica de trading

1. La estrategia funciona en un marco temporal horario (`CandleType`, predeterminado 1 hora).
2. Las señales se evalúan solo una vez al día cuando comienza el nuevo día (`00:00` – `00:03`). El filtro omite las sesiones de lunes y viernes
   exactamente como la versión MQL.
3. Antes de abrir una posición, el algoritmo garantiza que no haya otras operaciones activas.
4. Para cada día de trading, los siguientes valores se calculan a partir de las últimas 24 velas cerradas:
   - `Open_24_minus_Close_1` – distancia entre el precio de apertura de 24 barras atrás y el último cierre.
   - `Close_1_minus_Open_24` – distancia inversa que muestra el cambio neto del día.
   - `Close_1_minus_Lowest` – cuán lejos está el cierre del mínimo más bajo del día.
   - `Highest_minus_Close_1` – cuán lejos está el cierre del máximo más alto del día.
5. Reglas de entrada (expresadas en unidades de precio convertidas desde los parámetros de pips):
   - **Largo #1** – el día anterior cayó (`Open_24_minus_Close_1` por encima del umbral `ChannelOpenClosePips`) y el cierre todavía
     está cerca del mínimo extremo (`Close_1_minus_Lowest` por debajo de `RollbackPips - ChannelRollbackPips`).
   - **Largo #2** – el día anterior subió (`Close_1_minus_Open_24` por encima del umbral del canal) pero el mercado cerró muy por debajo del
     máximo diario (`Highest_minus_Close_1` mayor que `RollbackPips + ChannelRollbackPips`).
   - **Corto #1** – el día anterior subió y el cierre terminó cerca del máximo diario (`Highest_minus_Close_1` por debajo de
     `RollbackPips - ChannelRollbackPips`).
   - **Corto #2** – el día anterior se vendió y el cierre se recuperó muy por encima del mínimo diario (`Close_1_minus_Lowest` por encima de
     `RollbackPips + ChannelRollbackPips`).
6. Las órdenes se ejecutan con `BuyMarket`/`SellMarket` usando el volumen de trading configurado. Los niveles de stop-loss y take-profit se
   derivan de `StopLossPips` y `TakeProfitPips` (ambos en cero deshabilitan la protección respectiva).
7. Los niveles de protección se monitorean en cada vela finalizada. Si el precio viola un nivel intrabarra, la estrategia cierra la posición
   usando una orden de mercado, replicando el comportamiento del asesor experto MQL original que enviaba stops duros.

## Conversión de pips a parámetros

MetaTrader 5 multiplica los valores de pip por 10 en símbolos de 3 y 5 dígitos. La lógica de conversión se preserva: la estrategia toma el
`PriceStep` del instrumento y aplica un multiplicador de diez veces cuando el número detectado de dígitos decimales es igual a 3 o 5. Esto mantiene los
umbrales de entrada, las distancias de stop-loss y take-profit consistentes con la implementación MQL en típicos símbolos FX.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `TradeVolume` | Tamaño de operación usado para órdenes de mercado. |
| `StopLossPips` | Distancia de stop-loss en pips. Establecer a cero para deshabilitar. |
| `TakeProfitPips` | Distancia de take-profit en pips. Establecer a cero para deshabilitar. |
| `RollbackPips` | Requisito de rollback base utilizado por todas las señales. |
| `ChannelOpenClosePips` | Diferencia mínima entre la apertura y el cierre del día anterior. |
| `ChannelRollbackPips` | Tolerancia añadida/restada de la verificación de rollback. |
| `CandleType` | Tipo de vela de trabajo, predeterminado a barras horarias. |

## Notas

- La versión MQL pintó rectángulos en el gráfico para referencia visual. El port de StockSharp mantiene solo la lógica de trading.
- La gestión de riesgo se implementa con monitoreo interno de la estrategia en lugar de órdenes de protección del lado del servidor porque la API de alto nivel
  gestiona posiciones directamente.
- Al optimizar, ajuste los umbrales de pip y el volumen para adaptarse al instrumento objetivo y el tamaño de tick del broker.
