# Estratégia IU de Preenchimento de Gap
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia IU Gap Fill entra em operações quando o preço forma um gap em relação ao fechamento da sessão anterior e então preenche esse gap. Uma posição comprada é aberta após um gap de alta que cai abaixo do fechamento anterior e fecha novamente acima dele. Uma posição vendida é aberta após um gap de baixa que sobe acima do fechamento anterior e fecha novamente abaixo. Um stop trailing baseado em ATR gerencia as saídas.

## Detalhes
- **Dados**: Velas de um período definido pelo usuário.
- **Critérios de entrada**:
  - **Comprado**: Gap de alta de pelo menos `GapPercent` e o preço cruza acima do fechamento da sessão anterior.
  - **Vendido**: Gap de baixa de pelo menos `GapPercent` e o preço cruza abaixo do fechamento da sessão anterior.
- **Critérios de saída**: Stop trailing ATR.
- **Stops**: Nível trailing ATR `AtrLength` * `AtrFactor`.
- **Valores padrão**:
  - `CandleType` = 1m
  - `GapPercent` = 0.2
  - `AtrLength` = 14
  - `AtrFactor` = 2
- **Filtros**:
  - Categoria: Gap
  - Direção: Comprado & Vendido
  - Indicadores: ATR
  - Complexidade: Baixo
  - Nível de risco: Médio
