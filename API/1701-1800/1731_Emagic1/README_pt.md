# Estratégia de Tendência EMA MACD Signal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia entra comprada quando a EMA rápida está acima da EMA lenta e a linha de sinal do MACD está subindo. Entra vendida quando a EMA rápida está abaixo da EMA lenta e a linha de sinal está caindo. Stop-loss, take-profit e trailing stop são opcionais.

## Detalhes

- **Critérios de entrada**:
  - EMA rápida > EMA lenta e sinal MACD em alta → Comprar.
  - EMA rápida < EMA lenta e sinal MACD em queda → Vender.
- **Critérios de saída**:
  - O sinal de entrada oposto fecha a posição.
- **Indicadores**: EMA, MACD signal.
- **Tipo**: Seguidor de tendência.
- **Período**: 5 minutos (padrão).
