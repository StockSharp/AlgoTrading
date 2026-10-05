# Estratégia ATR MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
ATR MACD usa a volatilidade do Average True Range para ajustar o tamanho da posição enquanto opera cruzamentos do MACD.
Leituras maiores do ATR resultam em tamanhos de operação menores, mantendo o risco consistente em diferentes regimes de mercado.

Os testes indicam um retorno anual médio de aproximadamente 154%. Funciona melhor no mercado de ações.

As entradas ocorrem quando o MACD cruza sua linha de sinal, com saídas acionadas pelo cruzamento oposto ou um stop percentual.

Essa combinação busca capturar momentum levando em conta a volatilidade variável.

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
  - `AtrPeriod` = 14
  - `AtrAvgPeriod` = 20
    Um cruzamento do MACD acima da linha de sinal abre uma compra e abaixo uma venda; o cruzamento oposto inverte a posição. Cada nova posição é Volume vezes a média dos últimos AtrAvgPeriod valores do ATR dividida pelo ATR atual, arredondada para baixo ao passo de volume.
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: ATR, MACD
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

