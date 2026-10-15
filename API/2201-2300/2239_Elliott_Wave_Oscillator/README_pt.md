# Estratégia do Oscilador de Ondas de Elliott
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia aplica o Oscilador de Ondas de Elliott (EWO) nos fechamentos das velas. O EWO é calculado como a diferença entre uma Média Móvel Simples rápida e uma lenta (5 e 35 períodos por padrão). A lógica de trading busca pontos de inflexão no oscilador para capturar possíveis reversões de tendência.

Uma posição comprada é aberta quando o oscilador forma um fundo local e começa a subir. Uma posição vendida é aberta quando o oscilador forma um topo local e começa a cair. As posições existentes são invertidas de acordo. Take‑profit e stop‑loss opcionais baseados em percentual são suportados por meio de `StartProtection`.

## Detalhes

- **Indicador**: Oscilador de Ondas de Elliott = SMA(rápida) − SMA(lenta).
- **Critérios de entrada**:
  - **Comprado**: o valor do oscilador estava caindo e depois vira para cima.
  - **Vendido**: o valor do oscilador estava subindo e depois vira para baixo.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: A posição se inverte no sinal oposto ou sai via stop ou take‑profit.
- **Stops**: Stop‑loss e take‑profit percentuais.
- **Filtros**: Nenhum.
