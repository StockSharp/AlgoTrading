# Estratégia de Bollinger e Stochastic com Trailing Stop
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia entra comprado quando o preço fecha abaixo da banda inferior de Bollinger e o Stochastic %K está abaixo de 20. Entra vendido quando o preço fecha acima da banda superior e %K está acima de 80. Um trailing stop baseado em ATR protege as posições abertas.

## Detalhes
- **Critérios de entrada:**
  - **Comprado:** close < banda inferior de Bollinger e %K < 20.
  - **Vendido:** close > banda superior de Bollinger e %K > 80.
- **Comprado/Vendido:** Ambos.
- **Critérios de saída:** Trailing stop baseado em ATR.
- **Stops:** Trailing stop baseado em ATR * multiplicador.
- **Valores padrão:** Comprimento de Bollinger = 20, desvio = 2, comprimento de Stochastic = 14, suavização = 3, período ATR = 14, multiplicador ATR = 1.5.
