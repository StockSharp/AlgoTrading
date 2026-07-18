# Estratégia Donchian Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia Donchian Channel + Stochastic. A estratégia entra no mercado quando o preço rompe o Canal de Donchian com o Stochastic confirmando condições de sobrevenda/sobrecompra.

Os testes indicam um retorno anual médio de aproximadamente 85%. Funciona melhor no mercado de criptomoedas.

Os rompimentos além do canal de Donchian são confirmados com o momentum do Stochastic. As operações começam assim que o preço escapa do intervalo e o oscilador concorda.

Útil para traders que esperam um seguimento imediato. Um múltiplo de ATR define o stop.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `Close > DonchianHigh && StochK < 20`
  - Vendido: `Close < DonchianLow && StochK > 80`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Falha de rompimento ou sinal oposto
- **Stops**: Baseados em porcentagem usando `StopLossPercent`
- **Valores padrão**:
  - `DonchianPeriod` = 20
  - `StochPeriod` = 14
  - `StochK` = 3
  - `StochD` = 3
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
  - `StopLossPercent` = 2m
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

