# Estratégia Three Signal Directional Trend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Three Signal Directional Trend combina MACD, oscilador estocástico e taxa de variação da média móvel para determinar a direção da tendência. Cada indicador vota por condições compradas ou vendidas e as posições são abertas quando pelo menos dois indicadores concordam. O método visa capturar movimentos direcionais amplos filtrando o ruído por meio de múltiplos sinais de confirmação.

## Detalhes

- **Critérios de entrada:**
  - Pelo menos dois de três sinais concordam.
  - **Comprado**: sinal MACD em alta, estocástico abaixo da zona de sobrevenda, MA ROC positivo.
  - **Vendido**: sinal MACD em queda, estocástico acima da zona de sobrecompra, MA ROC negativo.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - Sinal oposto.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `AvgLength` = 50
  - `RocLength` = 1
  - `AvgRocLength` = 10
  - `StochLength` = 14
  - `SmoothK` = 3
  - `Overbought` = 80
  - `Oversold` = 20
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdAvgLength` = 9
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: MACD, Stochastic, SMA, ROC
  - Stops: Nenhum
  - Complexidade: Baixo
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
