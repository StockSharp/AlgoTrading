# Estratégia de Três Médias Móveis
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera quando uma média móvel curta cruza a média, enquanto ambas estão alinhadas em relação a uma média de longo prazo.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: A MA curta cruza acima da MA média e a MA média está acima da MA longa.
  - **Vendido**: A MA curta cruza abaixo da MA média e a MA média está abaixo da MA longa.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Cruzamento oposto.
- **Stops**: Não.
- **Valores padrão**:
  - `ShortMa` = 20
  - `MediumMa` = 50
  - `LongMa` = 200
