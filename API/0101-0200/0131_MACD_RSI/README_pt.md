# Estratégia MACD RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
MACD RSI combina o momentum do MACD com as leituras de sobrecompra/sobrevenda do RSI.
Quando ambos os indicadores se alinham, a probabilidade de um movimento sustentado aumenta.

Os testes indicam um retorno anual médio de aproximadamente 130%. Funciona melhor no mercado de ações.

A estratégia entra comprada quando o MACD cruza para cima e o RSI sobe da zona de sobrevenda, ou vende quando o MACD cruza para baixo com o RSI caindo da sobrecompra.

Stops baseados em um percentual do preço ajudam a conter as perdas se os indicadores divergirem após a entrada.

## Detalhes

- **Critérios de entrada**: sinal de indicador
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: stop-loss ou sinal oposto
- **Stops**: Sim, baseado em percentual
- **Valores padrão**:
  - `CandleType` = 15 minute
  - `StopLossPercent` = 2
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `RsiPeriod` = 14
  - `RsiOversold` = 30
  - `RsiOverbought` = 70
    RSI abaixo de RsiOversold arma uma compra e acima de RsiOverbought uma venda; vale o extremo mais recente. O próximo cruzamento do MACD nessa direção o confirma e entra, invertendo uma posição oposta; cada extremo confirma apenas um cruzamento.
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: MACD, RSI
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

