# Estratégia de Estatísticas de Intervalo Temporal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Coleta estatísticas simples entre índices de barras selecionados.
Registra preço médio, intervalo normalizado, variação percentual, volume médio e contagem de gaps.
Opera comprado se o período terminar positivo e vendido se negativo.

## Detalhes

- **Critérios de entrada**: a variação percentual em `EndIndex` determina a direção
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: nenhum
- **Stops**: Não
- **Valores padrão**:
  - `StartIndex` = 9000
  - `EndIndex` = 10000
- **Filtros**:
  - Categoria: Estatísticas
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Baixo
