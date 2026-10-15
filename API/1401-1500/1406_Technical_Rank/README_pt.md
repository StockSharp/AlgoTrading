# Classificação Técnica (Estratégia)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia calcula uma classificação técnica composta a partir de médias móveis, taxa de variação, inclinação do PPO e RSI. Posições compradas são abertas quando a classificação supera um limiar superior, e vendidas quando cai abaixo de um limiar inferior.

## Detalhes

- **Critérios de entrada**: classificação > UpperThreshold → comprado; classificação < LowerThreshold → vendido.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Sinal oposto.
- **Stops**: Não.
- **Valores padrão**:
  - `UpperThreshold` = 70
  - `LowerThreshold` = 30
  - `CandleType` = velas de 1 minuto
