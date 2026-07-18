# Estratégia MM Fibonacci
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia calcula os níveis Fibonacci de Murrey Math e opera rompimentos. Compra quando o preço rompe acima do nível 100% em um contexto de alta e vende quando o preço cai abaixo do nível 0% em um contexto de baixa. As posições são encerradas quando o preço cruza o nível 50% contra a operação.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: O preço fecha acima do nível 100% enquanto o extremo mais recente foi uma máxima.
  - **Vendido**: O preço fecha abaixo do nível 0% enquanto o extremo mais recente foi uma mínima.
- **Critérios de saída**:
  - **Comprado**: O preço cai abaixo do nível 50%.
  - **Vendido**: O preço sobe acima do nível 50%.
- **Indicadores**: Highest, Lowest.
- **Comprado/Vendido**: Ambos.
- **Stops**: Não.
