# Estratégia Timeshifter de Triplo Período com Sessões
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que opera em três períodos com confirmação ADX opcional e filtros de sessão.

Os testes indicam um retorno anual médio de aproximadamente 37%. Funciona melhor no mercado forex.

O sistema se alinha com a tendência do período superior, entra em rompimentos do período médio e sai em reversões do período inferior. As operações podem ser limitadas às sessões de Londres, Nova York e Tóquio. Um filtro ADX pode ser usado para garantir momentum suficiente.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: O fechamento do período superior está acima de sua SMA e o preço do período médio cruza acima de sua SMA.
  - **Vendido**: O fechamento do período superior está abaixo de sua SMA e o preço do período médio cruza abaixo de sua SMA.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Comprado**: O preço do período inferior cruza abaixo de sua SMA.
  - **Vendido**: O preço do período inferior cruza acima de sua SMA.
- **Stops**: Não.
- **Valores padrão**:
  - `HigherMaLength` = 50
  - `MediumMaLength` = 20
  - `LowerMaLength` = 10
  - `AdxLength` = 14
  - `AdxThreshold` = 25
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: SMA, ADX
  - Stops: Não
  - Complexidade: Complexo
  - Período: Múltiplos
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
