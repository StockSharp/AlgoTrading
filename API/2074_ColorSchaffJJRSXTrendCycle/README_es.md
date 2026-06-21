# Estrategia de Ciclo de Tendencia ColorSchaff JJRSX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia aplica el oscilador Schaff Trend Cycle basado en medias JJRSX. Abre posiciones largas o cortas cuando el oscilador cruza niveles definidos por el usuario.

## Detalles

- **Criterios de entrada**:
  - Comprar cuando el Schaff Trend Cycle cruza por encima de `HighLevel`. Primero se cierra cualquier posición corta.
  - Vender cuando el Schaff Trend Cycle cruza por debajo de `LowLevel`. Primero se cierra cualquier posición larga.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Las posiciones se cierran cuando ocurre una señal de entrada opuesta.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `Fast` = 23
  - `Slow` = 50
  - `Cycle` = 10
  - `HighLevel` = 60
  - `LowLevel` = -60
