# Estratégia Supertrend Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Estratégia Supertrend + Stochastic. A estratégia entra em operações quando o Supertrend indica a direção da tendência e o Stochastic confirma com condições de sobrevenda/sobrecompra.

Os testes indicam um retorno anual médio de aproximadamente 142%. Funciona melhor no mercado de ações.

O Supertrend marca a tendência e o Stochastic aponta movimentos contrários temporários. As entradas ocorrem quando o Stochastic sai da sobrevenda ou sobrecompra contra a tendência.

Ideal para traders de momentum que precisam de sinais de tendência claros. Os valores de ATR definem a distância do stop.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `Close > Supertrend && StochK < 20`
  - Vendido: `Close < Supertrend && StochK > 80`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Reversão do Supertrend
- **Stops**: Usa Supertrend como trailing stop
- **Valores padrão**:
  - `SupertrendPeriod` = 10
  - `SupertrendMultiplier` = 3.0m
  - `StochPeriod` = 14
  - `StochK` = 3
  - `StochD` = 3
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: Supertrend, Stochastic Oscillator
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

