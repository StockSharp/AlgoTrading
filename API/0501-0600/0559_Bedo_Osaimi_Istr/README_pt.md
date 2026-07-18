# Estratégia Bedo Osaimi Istr
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Uma estratégia simples de seguimento de tendência que compara médias móveis dos preços de fechamento e abertura. Uma posição comprada é aberta quando a média móvel do fechamento cruza acima da média móvel da abertura. A posição é invertida quando ocorre o cruzamento oposto.

## Detalhes

- **Critérios de entrada**:
  - MA de fechamento cruza acima da MA de abertura.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**:
  - MA de fechamento cruza abaixo da MA de abertura (para saída comprada ou entrada vendida).
- **Stops**: Nenhum.
- **Valores padrão**:
  - `MaLength` = 20
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: SMA em fechamento e abertura
  - Stops: Não
  - Complexidade: Baixo
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
