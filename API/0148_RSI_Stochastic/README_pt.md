# Rsi Stochastic Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Estratégia que combina o RSI e o Oscilador Stochastic para dupla confirmação de condições de sobrevenda e sobrecompra.

Os testes indicam um retorno anual médio de aproximadamente 181%. Funciona melhor no mercado de criptomoedas.

O RSI fornece uma visão mais ampla do momentum, enquanto o Stochastic dá sinais mais rápidos perto dos extremos. As operações mudam quando o oscilador cruza níveis dentro do contexto do RSI.

Ideal para traders ágeis que preferem configurações de osciladores. A estratégia depende de um stop de ATR para conter o risco.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `RSI < RsiOversold && StochK < StochOversold`
  - Vendido: `RSI > RsiOverbought && StochK > StochOverbought`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Comprado: `RSI > 50`
  - Vendido: `RSI < 50`
- **Stops**: Baseado em percentual em `StopLossPercent`
- **Valores padrão**:
  - `RsiPeriod` = 14
  - `RsiOversold` = 30m
  - `RsiOverbought` = 70m
  - `StochPeriod` = 14
  - `StochK` = 3
  - `StochD` = 3
  - `StochOversold` = 20m
  - `StochOverbought` = 80m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: RSI, Stochastic Oscillator
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

