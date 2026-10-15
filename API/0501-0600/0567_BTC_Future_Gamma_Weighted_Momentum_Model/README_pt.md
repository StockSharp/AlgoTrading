# Modelo de Momentum Ponderado por Gamma para Futuros BTC
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia calcula um preço médio ponderado por Gamma (GWAP) para capturar o momentum em futuros de BTC. Posições compradas são abertas quando o preço permanece acima do GWAP e os três últimos fechamentos sobem consecutivamente. Posições vendidas são tomadas quando o preço está abaixo do GWAP e os três últimos fechamentos caem consecutivamente.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: Fechamento acima do GWAP e os três últimos fechamentos em alta.
  - **Vendido**: Fechamento abaixo do GWAP e os três últimos fechamentos em queda.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**: Sinal inverso.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `Length` = 60
  - `GammaFactor` = 0.75
- **Filtros**:
  - Categoria: Momentum
  - Direção: Ambos
  - Indicadores: GWAP
  - Stops: Nenhum
  - Complexidade: Baixo
  - Período: 1m
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
