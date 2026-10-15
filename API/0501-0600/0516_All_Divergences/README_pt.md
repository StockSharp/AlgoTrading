# Estratégia de Todas as Divergências
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A Estratégia de Todas as Divergências procura divergências de alta e de baixa do RSI filtradas pela tendência de uma média móvel. Uma posição comprada é aberta quando o preço faz uma mínima mais baixa enquanto o RSI forma uma mínima mais alta acima da média móvel. Uma posição vendida é aberta quando o preço faz uma máxima mais alta enquanto o RSI forma uma máxima mais baixa abaixo da média móvel. Uma proteção opcional de stop-loss e take-profit pode fechar posições automaticamente, e um controle de risco por média móvel sai após vários fechamentos contra a tendência.

## Detalhes

- **Critérios de entrada**:
  - A posição do preço em relação à média móvel define a tendência.
  - **Comprado**: o preço faz uma mínima mais baixa, o RSI uma mínima mais alta, o preço acima da MA.
  - **Vendido**: o preço faz uma máxima mais alta, o RSI uma máxima mais baixa, o preço abaixo da MA.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - Sinal oposto ou saída por risco de MA.
- **Stops**: Stop-loss e take-profit opcionais.
- **Valores padrão**:
  - `MaLength` = 50
  - `RsiLength` = 14
  - `MaRiskCandles` = 3
  - `UseProtection` = False
- **Filtros**:
  - Categoria: Divergência
  - Direção: Ambos
  - Indicadores: RSI, Moving Average
  - Stops: Opcional
  - Complexidade: Moderado
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Sim
  - Nível de risco: Médio
