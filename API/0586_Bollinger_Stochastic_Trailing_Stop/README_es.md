# Estrategia de Bollinger y Stochastic con Trailing Stop
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia entra en largo cuando el precio cierra por debajo de la banda inferior de Bollinger y el Stochastic %K está por debajo de 20. Entra en corto cuando el precio cierra por encima de la banda superior y %K está por encima de 80. Un trailing stop basado en ATR protege las posiciones abiertas.

## Detalles
- **Criterios de entrada:**
  - **Largo:** close < banda inferior de Bollinger y %K < 20.
  - **Corto:** close > banda superior de Bollinger y %K > 80.
- **Largo/Corto:** Ambos.
- **Criterios de salida:** Trailing stop basado en ATR.
- **Stops:** Trailing stop basado en ATR * multiplicador.
- **Valores predeterminados:** Longitud de Bollinger = 20, desviación = 2, longitud de Stochastic = 14, suavizado = 3, período ATR = 14, multiplicador ATR = 1.5.
