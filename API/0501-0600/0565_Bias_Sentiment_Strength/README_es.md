# Estrategia de Fuerza de Bias y Sentimiento
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia agrega múltiples indicadores de momentum y volumen (MACD, RSI, Stochastic, Awesome Oscillator, promedios Alligator y bias de volumen) en un único valor de bias. Se abre una posición larga cuando el bias combinado está por encima de cero y una posición corta cuando está por debajo de cero.

## Detalles

- **Criterios de entrada**:
  - **Largo**: Bias combinado > 0.
  - **Corto**: Bias combinado < 0.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Señal inversa.
- **Stops**: Porcentaje de stop-loss mediante `StopLossPercent`.
- **Valores predeterminados**:
  - MACD rápido 12, lento 26, señal 9.
  - Período RSI 14.
  - Períodos Stochastic 21/14/14.
  - Períodos Awesome Oscillator 5/34.
  - Longitud de bias de volumen 30.
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Múltiples
  - Stops: Sí
  - Complejidad: Complejo
  - Marco temporal: Medio plazo
