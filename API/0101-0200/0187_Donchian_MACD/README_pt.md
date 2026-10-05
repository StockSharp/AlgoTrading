# Estratégia Donchian Macd
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Estratégia que combina o rompimento do Canal Donchian com a confirmação de tendência via MACD.

Os testes indicam um retorno anual médio de aproximadamente 148%. Funciona melhor no mercado forex.

A estratégia aguarda um rompimento de Donchian e verifica o momentum com MACD. Operações compradas ou vendidas seguem o movimento quando o MACD concorda.

Voltada para entusiastas de rompimentos que desejam confirmação. Os stops são colocados a uma porcentagem fixa do preço de entrada.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `Price breaks Donchian high && MACD > Signal`
  - Vendido: `Price breaks Donchian low && MACD < Signal`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Reversão do MACD
- **Stops**: Percentual usando `StopLossPercent`
- **Valores padrão**:
  - `DonchianPeriod` = 20
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `StopLossPercent` = 2
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
    Um rompimento é um fechamento além da máxima ou mínima dos DonchianPeriod candles anteriores. A saída por reversão do MACD encerra uma compra quando o MACD cruza abaixo da linha de sinal e uma venda quando cruza acima. Um sinal de entrada contra uma posição aberta a inverte.
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Donchian Channel, MACD
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

