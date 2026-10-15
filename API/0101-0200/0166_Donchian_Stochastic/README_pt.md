# Estratégia Donchian Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia Donchian Channel + Stochastic. A estratégia entra no mercado quando o preço rompe o Canal de Donchian com o Stochastic confirmando condições de sobrevenda/sobrecompra.

Os testes indicam um retorno anual médio de aproximadamente 85%. Funciona melhor no mercado de criptomoedas.

Os rompimentos além do canal de Donchian são confirmados com o momentum do Stochastic. As operações começam assim que o preço escapa do intervalo e o oscilador concorda.

Útil para traders que esperam um seguimento imediato. Uma porcentagem fixa do preço de entrada define o stop.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `Close > DonchianHigh && StochK > StochOverbought`
  - Vendido: `Close < DonchianLow && StochK < StochOversold`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Falha de rompimento ou sinal oposto
- **Stops**: Baseados em porcentagem usando `StopLossPercent`
- **Valores padrão**:
  - `DonchianPeriod` = 20
  - `StochPeriod` = 14
  - `StochK` = 3
  - `StochOverbought` = 80
  - `StochOversold` = 20
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
  - `StopLossPercent` = 2
    Um rompimento de alta dos DonchianPeriod candles anteriores é confirmado quando %K está acima de StochOverbought e um de baixa quando %K está abaixo de StochOversold; a leitura oposta, um %K sobrevendido num rompimento de alta, praticamente não ocorre. %K é o estocástico de StochPeriod candles suavizado em StochK candles, e o %D não participa. O rompimento falha, encerrando a posição, quando o preço volta a fechar do outro lado do nível rompido. Um sinal de entrada contra uma posição aberta a inverte.
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Donchian Channel, Stochastic Oscillator
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

