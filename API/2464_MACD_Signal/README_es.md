# Estrategia de Señal MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera en función de la diferencia entre la línea MACD y su línea de señal.
Se abre una posición cuando la diferencia cruza un umbral basado en ATR y se cierra en cruces opuestos.
Se aplica un trailing stop y un take profit fijo en ticks.

## Detalles

- **Criterios de entrada**:
  - **Largo**: MACD - Signal cruza por encima de `ATR * Level`.
  - **Corto**: MACD - Signal cruza por debajo de `-ATR * Level`.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Cruce opuesto del umbral.
- **Stops**:
  - Take profit fijo en ticks.
  - Trailing stop opcional.
- **Indicadores**:
  - MACD (períodos fast, slow, signal configurables).
  - ATR(200) para escalar el umbral.
