# Estratégia de Equilíbrio de Forças
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia de Equilíbrio de Forças avalia a força dos touros versus os ursos dentro de cada vela comparando o fechamento com o intervalo de negociação. Quando este valor cruza acima de um limiar positivo, indica forte pressão compradora.

A estratégia entra em uma posição comprada quando o Balance of Power cruza acima do `Threshold` definido e sai quando cai abaixo do limiar negativo.

## Detalhes

- **Critérios de entrada**:
  - Balance of Power cruza acima de `Threshold`.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**:
  - Balance of Power cruza abaixo de `-Threshold`.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `Threshold` = 0.8
- **Filtros**:
  - Categoria: Momentum
  - Direção: Comprado
  - Indicadores: Balance of Power
  - Stops: Nenhum
  - Complexidade: Baixo
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
