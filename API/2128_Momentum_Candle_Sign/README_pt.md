# Estratégia de Sinal de Vela Momentum
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera com base no cruzamento entre os valores de momentum calculados a partir dos preços de abertura e fechamento das velas. Quando o momentum do preço de abertura cai abaixo do momentum do preço de fechamento, isso sinaliza pressão de alta crescente e a estratégia entra em uma posição comprada. O cruzamento oposto indica pressão de baixa e aciona uma posição vendida.

Por padrão, a estratégia opera em velas de 12 horas com um período de momentum de 12.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: O momentum de abertura cruza abaixo do momentum de fechamento.
  - **Vendido**: O momentum de abertura cruza acima do momentum de fechamento.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Cruzamento oposto.
- **Stops**: Nenhum.
- **Filtros**: Nenhum.
