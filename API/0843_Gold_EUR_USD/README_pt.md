# Estratégia de Captura de Liquidez em Gold e EUR/USD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia detecta capturas de liquidez em zonas de oferta e demanda no Gold e EUR/USD usando RSI, SMA, Oscilador Estocástico e lacunas de valor justo baseadas em ATR.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: O preço faz sombra abaixo da mínima recente, a estrutura de mercado muda para cima, ocorre uma lacuna de valor justo, RSI sobrevendido, preço acima da SMA, Estocástico sobrevendido.
  - **Vendido**: O preço faz sombra acima da máxima recente, a estrutura de mercado muda para baixo, ocorre uma lacuna de valor justo, RSI sobrecomprado, preço abaixo da SMA, Estocástico sobrecomprado.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Sinal reverso.
- **Stops**: Não.
- **Valores padrão**:
  - `RsiLength` = 14
  - `MaLength` = 50
  - `StochLength` = 14
  - `Overbought` = 70
  - `Oversold` = 30
  - `StochOverbought` = 80
  - `StochOversold` = 20
- **Filtros**:
  - Categoria: Price action
  - Direção: Ambos
  - Indicadores: RSI, SMA, Stochastic, ATR, Highest, Lowest
  - Stops: Não
  - Complexidade: Médio
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
