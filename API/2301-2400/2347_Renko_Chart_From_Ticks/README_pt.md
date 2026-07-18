# Estratégia de Gráfico Renko a partir de Ticks
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Gera tijolos Renko diretamente de ticks e opera quando a direção do tijolo muda. Demonstra a construção de velas não baseadas em tempo usando a API de alto nível do StockSharp.

## Detalhes

- **Critérios de entrada**:
  - Quando um novo tijolo concluído inverte a direção, entrar na direção do novo tijolo.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Reverter a posição quando a direção do tijolo for oposta.
- **Stops**: Não.
- **Valores padrão**:
  - `BrickSize` = 10
  - `Volume` = 1
- **Filtros**:
  - Categoria: Renko
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Não
  - Complexidade: Iniciante
  - Período: Baseado em ticks
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
