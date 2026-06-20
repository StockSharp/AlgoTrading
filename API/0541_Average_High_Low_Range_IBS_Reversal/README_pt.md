# Estratégia de Reversão IBS de Intervalo Médio Alto-Baixo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia busca reversão à média após o preço ter permanecido abaixo de um limiar dinâmico derivado do intervalo médio alto-baixo. Ela calcula a média móvel do intervalo da barra, o máximo mais alto e o mínimo mais baixo ao longo do período de observação. Um limiar de compra é definido como o máximo mais alto menos 2,5 vezes o intervalo médio. Quando o preço permanece abaixo desse nível por um número especificado de barras e a força intrabarra (IBS) está abaixo de um limite definido dentro da janela de negociação, uma posição comprada é aberta. A posição é fechada se o fechamento superar a máxima da barra anterior.

## Detalhes

- **Critérios de entrada**:
  - O preço permaneceu abaixo do limiar de compra por `BarsBelowThreshold` barras.
  - IBS < `IbsBuyThreshold`.
  - Horário entre `StartTime` e `EndTime`.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**:
  - O preço de fechamento supera a máxima da barra anterior.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `Length` = 20
  - `BarsBelowThreshold` = 2
  - `IbsBuyThreshold` = 0.2
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Comprado
  - Indicadores: SMA, Highest, Lowest
  - Stops: Não
  - Complexidade: Baixo
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Baixo
