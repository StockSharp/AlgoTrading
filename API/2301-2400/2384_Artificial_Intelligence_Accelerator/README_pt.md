# Estratégia Acelerador de Inteligência Artificial
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia implementa um modelo de perceptron simples sobre o **Oscillator de Aceleração/Desaceleração (AC)** de Bill Williams. Quatro leituras do oscilador são amostradas com defasagens de 0, 7, 14 e 21 barras e multiplicadas por pesos ajustáveis. A soma ponderada atua como sinal de decisão: valores positivos implicam momentum altista e valores negativos implicam momentum baixista. A estratégia reverte sua posição sempre que o sinal muda de sinal e coloca um stop-loss fixo a partir do preço de entrada.

O próprio AC é derivado do Awesome Oscillator (AO) subtraindo uma média móvel de 5 períodos do AO. Isso torna a estratégia sensível a mudanças na aceleração do mercado.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: Sinal do perceptron > 0.
  - **Vendido**: Sinal do perceptron < 0.
- **Comprado/Vendido**: Ambos os lados; a estratégia reverte se o sinal mudar.
- **Critérios de saída**:
  - Stop-loss acionado a partir do preço de entrada.
  - Reverter quando o sinal cruzar zero.
- **Stops**: Sim, stop-loss fixo em unidades de preço.
- **Valores padrão**:
  - `X1` = 76
  - `X2` = 47
  - `X3` = 153
  - `X4` = 135
  - `StopLoss` = 8355
  - `CandleType` = velas de 1 minuto
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: AC (derivado de AO)
  - Stops: Sim
  - Complexidade: Moderado
  - Período: Curto plazo
  - Redes neurais: Perceptron
  - Nível de risco: Alto
