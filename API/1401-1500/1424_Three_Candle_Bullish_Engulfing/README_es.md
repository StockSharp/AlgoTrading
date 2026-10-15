# Estrategia de Tres Velas de Absorción Alcista
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia busca un patrón de absorción alcista o bajista de tres velas. Admite entradas opcionales por ruptura de RSI, un stop de seguimiento y salidas basadas en tiempo.

## Detalles

- **Criterios de entrada**:
  - **Largo**: Vela alcista, pequeño doji y vela de absorción alcista.
  - **Corto**: Vela bajista, pequeño doji y vela de absorción bajista.
- **Largo/Corto**: Ambos (modo solo largos disponible).
- **Criterios de salida**:
  - Stop de seguimiento, ruptura de vela opuesta o fin de sesión.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `TrailPerc` = 1.5
  - `ExitHour` = 15
  - `ExitMinute` = 15
  - `RsiLength` = 14
  - `RsiLevel` = 80
  - `StopLossPerc` = 5
