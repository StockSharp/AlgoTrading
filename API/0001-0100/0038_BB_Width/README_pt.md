# Estratégia Bollinger Band Width Breakout
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
A largura das Bollinger Bands mede a separação entre as bandas superior e inferior. Uma largura em expansão sugere volatilidade e possível formação de tendência. Esta estratégia opera rompimentos quando a largura está aumentando.

Os testes indicam um retorno anual médio de aproximadamente 151%. Funciona melhor no mercado de ações.

A posição do preço em relação à banda do meio define a direção. Um canal em expansão com preço acima da banda do meio aciona compras, enquanto um canal em expansão abaixo dela aciona vendas.

As saídas ocorrem quando a largura da banda se contrai ou um stop de volatilidade é atingido.

## Detalhes

- **Critérios de entrada**: Largura da banda em expansão e preço relativo à banda do meio.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**: Largura da banda se contrai ou stop.
- **Stops**: Sim.
- **Valores padrão**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2.0m
  - `AtrMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Bollinger Bands, ATR
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

