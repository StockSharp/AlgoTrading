# Estratégia Ta
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada no cruzamento do MACD com pivôs de suporte e resistência, confirmação de RSI e ADX. Dois alvos de lucro com saída parcial são utilizados.

## Detalhes

- **Entrada**
  - **Comprado**: MACD cruza acima do sinal, preço acima da resistência, RSI > 50, +DI > -DI, ADX > 20.
  - **Vendido**: MACD cruza abaixo do sinal, preço abaixo do suporte, RSI < 50, -DI > +DI, ADX > 20.
- **Saída**: dois níveis de take-profit e um stop-loss.
