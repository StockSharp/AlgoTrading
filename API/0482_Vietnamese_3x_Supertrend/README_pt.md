# Estratégia Vietnamese 3x Supertrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia empilha três indicadores SuperTrend com diferentes comprimentos de ATR e multiplicadores. Ela escala em posições compradas quando a tendência lenta é de baixa e as tendências mais rápidas mostram oportunidades de pullback. Um stop de break-even opcional protege os lucros assim que o preço se move favoravelmente.

## Detalhes

- **Critérios de entrada**:
  - SuperTrend lento em tendência de baixa.
  - **Long 1**: Tendência média de alta e tendência rápida de baixa.
  - **Long 2**: Tendência média de baixa e preço acima da linha do SuperTrend rápido.
  - **Long 3**: Tendência rápida de baixa e rompimento acima da máxima mais alta durante a tendência de baixa rápida.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**:
  - Todos os SuperTrends viram para cima e o candle fecha de baixa.
  - Preço médio de entrada acima do fechamento atual.
  - Stop de break-even opcional se habilitado.
- **Stops**: Stop de break-even opcional.
- **Valores padrão**:
  - `FastAtrLength` = 10
  - `FastMultiplier` = 1
  - `MediumAtrLength` = 11
  - `MediumMultiplier` = 2
  - `SlowAtrLength` = 12
  - `SlowMultiplier` = 3
  - `UseHighestOfTwoRedCandles` = False
  - `UseEntryStopLoss` = True
  - `UseAllDowntrendExit` = True
  - `UseAvgPriceInLoss` = True
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Comprado
  - Indicadores: SuperTrend
  - Stops: Opcional
  - Complexidade: Moderado
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
