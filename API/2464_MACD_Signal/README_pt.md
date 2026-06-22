# Estratégia de Sinal MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera com base na diferença entre a linha MACD e sua linha de sinal.
Uma posição é aberta quando a diferença cruza um limiar baseado em ATR e é fechada em cruzamentos opostos.
Um trailing stop e take profit fixo em ticks são aplicados.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: MACD - Signal cruza acima de `ATR * Level`.
  - **Vendido**: MACD - Signal cruza abaixo de `-ATR * Level`.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Cruzamento oposto do limiar.
- **Stops**:
  - Take profit fixo em ticks.
  - Trailing stop opcional.
- **Indicadores**:
  - MACD (períodos fast, slow, signal configuráveis).
  - ATR(200) para escalar o limiar.
