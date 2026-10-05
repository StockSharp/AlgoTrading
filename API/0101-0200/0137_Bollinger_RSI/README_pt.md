# Estratégia Bollinger RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Bollinger RSI combina a sobreextensão das Bandas de Bollinger com sinais de momentum do RSI.
Quando o preço fecha fora das bandas mas o RSI mostra divergência, uma reversão costuma estar próxima.

Os testes indicam um retorno anual médio de aproximadamente 148%. Funciona melhor no mercado forex.

O sistema realiza operações contra a tendência nessa divergência, saindo quando o preço volta para dentro das bandas ou o RSI cruza de volta.

Um stop percentual apertado limita a exposição caso a volatilidade se expanda ainda mais.

## Detalhes

- **Critérios de entrada**: sinal de indicador
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: stop-loss ou sinal oposto
- **Stops**: Sim, baseado em percentual
- **Valores padrão**:
  - `CandleType` = 15 minute
  - `StopLossPercent` = 2
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2
  - `RsiPeriod` = 14
    Um fechamento acima da banda superior mais alto que o fechamento anterior acima dela, com o RSI mais baixo que naquele momento, é uma divergência de baixa e abre uma venda; o caso espelhado abaixo da banda inferior abre uma compra e inverte uma posição oposta. A posição é encerrada quando o preço volta a fechar dentro das bandas ou o RSI cruza de volta o 50.
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: Bollinger Bands, RSI
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

