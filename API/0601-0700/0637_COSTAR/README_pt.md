# Estratégia COSTAR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia COSTAR constrói uma regressão linear dos preços de fechamento e mede o desvio padrão dos resíduos. As bandas superior e inferior são criadas adicionando e subtraindo o desvio multiplicado por um fator. As operações tentam operar contra desvios extremos e saem quando o preço retorna à linha de regressão.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: O preço cruza acima da banda inferior.
  - **Vendido**: O preço cruza abaixo da banda superior.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - O preço cruza de volta pela linha de regressão.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `Length` = 100
  - `Multiplier` = 1
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: Linear Regression, Standard Deviation
  - Stops: Não
  - Complexidade: Baixo
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
