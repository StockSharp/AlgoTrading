# Estratégia ETH Signal 15m
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia ETH Signal 15m usa o indicador Supertrend para detectar mudanças de direção e o RSI para filtrar entradas. Uma posição comprada é aberta quando a direção do Supertrend diminui e o RSI está abaixo do nível de sobrecompra. Uma posição vendida é aberta quando a direção do Supertrend aumenta e o RSI está acima do nível de sobrevenda. As saídas utilizam stop loss e take profit baseados em ATR.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: A direção do Supertrend diminui e o RSI está abaixo de `RsiOverbought`.
  - **Vendido**: A direção do Supertrend aumenta e o RSI está acima de `RsiOversold`.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**: Stop loss e take profit baseados em ATR.
- **Stops**: Stop loss de 4×ATR, take profit de 2×ATR para comprado, take profit de 2.237×ATR para vendido.
- **Valores padrão**:
  - `AtrPeriod` = 12
  - `Factor` = 2.76
  - `RsiLength` = 12
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Supertrend, RSI, ATR
  - Stops: Stop loss e take profit ATR
  - Complexidade: Baixo
  - Período: 15m
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
