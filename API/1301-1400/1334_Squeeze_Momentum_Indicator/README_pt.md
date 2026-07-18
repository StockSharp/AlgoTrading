# Estratégia de Indicador de Momentum de Squeeze
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Squeeze Momentum Indicator detecta contração de volatilidade quando as Bandas de Bollinger caem dentro dos Canais de Keltner. Uma posição comprada é aberta quando o squeeze se libera para cima com momentum crescente e preço acima da EMA de 100 períodos. Posições vendidas são abertas em uma liberação para baixo com momentum decrescente e preço abaixo da EMA. As posições são encerradas quando o momentum se reverte.

## Detalhes

- **Critérios de entrada**:
  - Bandas de Bollinger se movem para fora dos Canais de Keltner (liberação do squeeze).
  - **Comprado**: Momentum aumenta, preço acima do fechamento anterior e EMA100, e a cor do squeeze muda de preto para cinza.
  - **Vendido**: Momentum diminui, preço abaixo do fechamento anterior e EMA100, e a cor muda de cinza para preto.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - O momentum se reverte.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `BbLength` = 20
  - `BbMultiplier` = 2
  - `KcLength` = 20
  - `KcMultiplier` = 1.5
  - `EmaLength` = 100
- **Filtros**:
  - Categoria: Rompimento de volatilidade
  - Direção: Ambos
  - Indicadores: Bollinger Bands, Keltner Channels, Linear Regression, EMA
  - Stops: Nenhum
  - Complexidade: Médio
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
