# Estrategia de Grid Trading
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia implementa un sistema básico de grid trading. Coloca órdenes buy stop y sell stop a intervalos de precio fijos definidos por `GridStep`. Cada orden ejecutada utiliza una distancia de take profit fija. Un objetivo de ganancia global cierra todas las posiciones y reinicia la cuadrícula. Opcionalmente, el volumen de nuevas órdenes aumenta siguiendo un esquema martingala.

## Detalles

- **Criterios de entrada:**
  - Buy stop un paso por encima del último precio.
  - Sell stop un paso por debajo del último precio.
- **Largo/Corto:** Ambos.
- **Criterios de salida:**
  - Cada posición se cierra en el take profit fijo.
  - Cuando la ganancia total supera `ProfitTarget`, todas las órdenes y posiciones se cierran.
- **Stops:** Solo take profit.
- **Filtros:** Ninguno.
