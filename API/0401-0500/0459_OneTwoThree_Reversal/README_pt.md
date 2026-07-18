# Estratégia de Reversão Um-Dois-Três
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia de Reversão Um-Dois-Três procura um padrão altista 1-2-3 na ação do preço. Uma posição comprada é aberta quando a mínima de hoje está abaixo da de ontem, a mínima de ontem está abaixo da mínima de três barras atrás, a mínima de duas barras atrás está abaixo da mínima de quatro barras atrás, e a máxima de duas barras atrás está abaixo da máxima de três barras atrás. A operação é encerrada após um número definido de barras ou quando o preço fecha acima de uma média móvel.

## Detalhes

- **Critérios de entrada:**
  - Mínima atual < mínima anterior.
  - Mínima anterior < mínima de três barras atrás.
  - Mínima de duas barras atrás < mínima de quatro barras atrás.
  - Máxima de duas barras atrás < máxima de três barras atrás.
- **Comprado/Vendido:** Somente comprado.
- **Critérios de saída:**
  - Manter por `DaysToHold` barras ou fechamento cruza acima da média móvel.
- **Stops:** Nenhum.
- **Valores padrão:**
  - `DaysToHold` = 7
  - `MaLength` = 200
- **Filtros:**
  - Categoria: Reversão
  - Direção: Somente comprado
  - Indicadores: Price action, SMA
  - Stops: Não
  - Complexidade: Baixo
  - Período: Diário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
