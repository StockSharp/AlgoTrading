# Estratégia QQQ v2 ESL easy-peasy-x
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera QQQ usando o cruzamento da média móvel principal com filtros de tendência. Compra quando o preço de fechamento cruza acima da MA principal enquanto a MA está subindo e o preço está acima da MA de tendência de longo prazo. Vende a descoberto quando o fechamento cruza abaixo da MA principal enquanto a MA está caindo e o preço está abaixo da MA de tendência de curto prazo.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: Fechamento cruza acima da MA principal, inclinação da MA subindo, preço acima da MA de tendência longa.
  - **Vendido**: Fechamento cruza abaixo da MA principal, inclinação da MA caindo, preço abaixo da MA de tendência curta.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Sinal oposto.
- **Stops**: Não.
- **Valores padrão**:
  - `Main MA Length` = 200
  - `Trend Long Length` = 100
  - `Trend Short Length` = 50
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Médias móveis
  - Stops: Não
  - Complexidade: Moderado
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
