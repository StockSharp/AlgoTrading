# Estratégia de Teste de Indicadores com Tabela de Condições
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia compara o último preço de fechamento com níveis definidos pelo usuário e executa ordens de mercado quando as condições são atendidas. Cada lado (comprado e vendido) tem regras de entrada e saída separadas controladas por parâmetros.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: A condição comprada habilitada é verdadeira.
  - **Vendido**: A condição vendida habilitada é verdadeira.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Comprado**: A condição habilitada de encerramento comprado é verdadeira.
  - **Vendido**: A condição habilitada de encerramento vendido é verdadeira.
- **Stops**: Não.
- **Valores padrão**:
  - `LongOperator` = `>`
  - `CloseLongOperator` = `<`
  - `ShortOperator` = `<`
  - `CloseShortOperator` = `>`
- **Filtros**:
  - Categoria: Outro
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Não
  - Complexidade: Simples
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
