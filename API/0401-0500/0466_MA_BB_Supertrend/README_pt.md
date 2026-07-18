# Estratégia MA + BB + SuperTrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia combina um cruzamento de médias móveis com a confirmação do SuperTrend
e utiliza as Bollinger Bands para saídas. Uma posição comprada é aberta quando a
MA de sinal cruza acima da MA base e o preço está acima da linha SuperTrend. Posições
vendidas são abertas no cruzamento oposto sob um SuperTrend de baixa. As posições são
fechadas quando o preço toca a Bollinger Band distante ou quando o preço cruza o
SuperTrend na direção oposta.

## Detalhes

- **Critérios de entrada**:
  - MA de sinal cruza a MA base na direção do SuperTrend.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - Toque na Bollinger Band oposta ou virada do SuperTrend.
- **Stops**: O SuperTrend atua como stop trailing.
- **Valores padrão**:
  - Comprimento MA sinal = 89, ratio MA = 1.08.
  - Comprimento BB = 30, largura BB = 3.
  - Período SuperTrend = 20, fator SuperTrend = 4.
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: MA, Bollinger Bands, SuperTrend
  - Stops: SuperTrend
  - Complexidade: Moderado
  - Período: Curto/médio
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
