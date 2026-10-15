# Estratégia ColorXvaMA Digit
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera com base na mudança de inclinação de uma média móvel com duplo suavizamento. Uma Média Móvel Exponencial é suavizada novamente por uma Média Móvel Jurik. Uma posição comprada é aberta quando a JMA rápida cruza acima da EMA lenta, e uma posição vendida quando cruza abaixo.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: JMA rápida cruza acima da EMA lenta.
  - **Vendido**: JMA rápida cruza abaixo da EMA lenta.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**: Sinal oposto.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `SlowLength` = 15
  - `FastLength` = 5
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: EMA, JMA
  - Stops: Nenhum
  - Complexidade: Baixo
  - Período: 8h
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
