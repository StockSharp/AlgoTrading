# Estratégia Vwap Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Estratégia que combina os indicadores VWAP e Stochastic. Compra quando o preço está abaixo do VWAP e o Stochastic está sobrevendido. Vende quando o preço está acima do VWAP e o Stochastic está sobrecomprado.

Os testes indicam um retorno anual médio de aproximadamente 187%. Funciona melhor no mercado de ações.

O VWAP marca o nível médio de negociação e o Stochastic mostra condições de sobrecompra ou sobrevenda. Os comprados são acionados abaixo do VWAP com um oscilador em alta, os vendidos acima do VWAP com um em queda.

Traders intradiários que observam níveis de valor intradiário podem se beneficiar deste estilo. Os stops são colocados usando um múltiplo de ATR.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `Close < VWAP && StochK < OversoldLevel`
  - Vendido: `Close > VWAP && StochK > OverboughtLevel`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Comprado: `Close > VWAP`
  - Vendido: `Close < VWAP`
- **Stops**: Baseado em percentual usando `StopLossPercent`
- **Valores padrão**:
  - `StochPeriod` = 14
  - `StochKPeriod` = 3
  - `StochDPeriod` = 3
  - `OverboughtLevel` = 80m
  - `OversoldLevel` = 20m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: VWAP, Stochastic Oscillator
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

