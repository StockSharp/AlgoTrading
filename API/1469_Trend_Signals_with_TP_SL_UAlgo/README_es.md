# Estrategia de Señales de Tendencia con TP y SL
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia utiliza un canal basado en ATR para determinar la dirección de la tendencia. Una nueva tendencia alcista comienza cuando el precio rompe por encima de la banda superior, activando una entrada larga. Una tendencia bajista comienza cuando el precio cae por debajo de la banda inferior, activando una entrada corta. Cada operación coloca órdenes de stop-loss y take-profit usando multiplicadores ATR.

## Detalles

- **Criterios de entrada**:
  - **Largo**: La tendencia cambia hacia arriba.
  - **Corto**: La tendencia cambia hacia abajo.
- **Salidas**: Stop-loss en `entry ∓ ATR * SL` y take-profit en `entry ± ATR * TP`.
- **Stops**: Sí, tanto stop-loss como take-profit.
- **Valores predeterminados**:
  - `Sensitivity` = 2
  - `ATR Length` = 14
  - `ATR TP Multiplier` = 2
  - `ATR SL Multiplier` = 1
