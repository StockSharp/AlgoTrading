# Estrategia Quantum Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera utilizando el oscilador Stochastic. Cuando %K sale de la zona de sobreventa cruzando por encima de `LowLevel`, abre una posición larga. Cuando %K cae de la zona de sobrecompra cruzando por debajo de `HighLevel`, abre una posición corta. Las posiciones se cierran en umbrales extremos para capturar beneficios.

## Detalles

- **Criterios de entrada**:
  - **Largo**: %K cruza por encima de `LowLevel`.
  - **Corto**: %K cruza por debajo de `HighLevel`.
- **Criterios de salida**:
  - **Largo**: %K alcanza `HighCloseLevel`.
  - **Corto**: %K alcanza `LowCloseLevel`.
- **Indicadores**: Stochastic Oscillator.
- **Marco temporal**: Parámetro `CandleType` (por defecto 1 minuto).
- **Parámetros**:
  - `KPeriod` – período de la línea %K.
  - `DPeriod` – período de la línea %D.
  - `Slowing` – factor de suavizado del Stochastic.
  - `HighLevel` – límite inferior de la zona de sobrecompra.
  - `LowLevel` – límite superior de la zona de sobreventa.
  - `HighCloseLevel` – nivel para cerrar posiciones largas.
  - `LowCloseLevel` – nivel para cerrar posiciones cortas.
