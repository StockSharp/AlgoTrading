# Estratégia Markdown A Joia Oculta do Editor Pine
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia usa Bandas de Bollinger construídas sobre uma média móvel simples. Uma posição comprada é aberta quando o preço fecha acima da banda superior, e uma posição vendida quando fecha abaixo da banda inferior.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: O preço de fechamento cruza acima da banda superior.
  - **Vendido**: O preço de fechamento cruza abaixo da banda inferior.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**:
  - Sinal oposto.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `Length` = 50
  - `Multiplier` = 2
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Bollinger Bands
  - Stops: Não
  - Complexidade: Baixo
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
