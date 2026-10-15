# Estrategia MACFibo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia implementa el sistema de trading MACFibo. Espera un cruce entre la EMA de 5 períodos y la SMA de 20 períodos. Después del cruce, el algoritmo mide el movimiento desde el cierre de la barra del cruce (punto A) hasta el extremo más reciente (punto B) y construye niveles de expansión de Fibonacci. Las posiciones se abren a precio de mercado con take profit y stop loss derivados de estos niveles. Una salida opcional cierra las operaciones perdedoras cuando la EMA rápida cruza la SMA media en dirección opuesta.

## Detalles

- **Condiciones de entrada:**
  - **Largo:** La EMA de 5 cruza por encima de la SMA de 20. El punto B es el mínimo más bajo desde que comenzó el movimiento bajista.
  - **Corto:** La EMA de 5 cruza por debajo de la SMA de 20. El punto B es el máximo más alto desde que comenzó el movimiento alcista.
- **Condiciones de salida:**
  - Take profit en el nivel del 161.8% de Fibonacci o la distancia mínima de take profit.
  - Stop loss en el nivel del 38.2% de Fibonacci o la distancia máxima de stop loss.
  - Cierre opcional si la EMA de 5 cruza la SMA de 8 contra la posición y la operación está perdiendo.
- **Filtros:**
  - Opera únicamente entre las horas de inicio y fin configuradas.
  - Se puede deshabilitar el trading en lunes o viernes.
- **Parámetros:**
  - `FastLength` – longitud de la EMA rápida.
  - `MidLength` – longitud de la SMA media para salida protectora.
  - `SlowLength` – longitud de la SMA lenta para detección de tendencia.
  - `MinTakeProfit` – take profit mínimo en unidades de precio.
  - `MaxStopLoss` – stop loss máximo en unidades de precio.
  - `StartHour` / `EndHour` – ventana de tiempo de trading permitida.
  - `FridayTrade` / `MondayTrade` – habilitar trading en esos días.
  - `CloseAtFastMid` – cerrar operaciones perdedoras en el cruce rápido-medio.
  - `CandleType` – tipo de vela para los cálculos.
