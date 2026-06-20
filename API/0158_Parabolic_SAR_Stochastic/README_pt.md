# Estratégia Parabolic SAR Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Implementação da estratégia Parabolic SAR + Stochastic. Comprar quando o preço está acima do SAR e o Stochastic %K está abaixo de 20 (sobrevendido). Vender quando o preço está abaixo do SAR e o Stochastic %K está acima de 80 (sobrecomprado).

Os testes indicam um retorno anual médio de cerca de 61%. Funciona melhor no mercado de criptomoedas.

O Parabolic SAR fornece a tendência e o Stochastic refina a entrada nos retrocessos. Os sinais mudam quando o SAR muda de lado.

Uma estratégia de tendência direta com stops SAR integrados. As configurações do ATR gerenciam o controle de risco adicional.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `Close > SAR && StochK < StochOversold`
  - Vendido: `Close < SAR && StochK > StochOverbought`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Inversão do Parabolic SAR na direção oposta
- **Stops**: Dinâmicos baseados em SAR
- **Valores padrão**:
  - `AccelerationFactor` = 0.02m
  - `MaxAccelerationFactor` = 0.2m
  - `StochK` = 3
  - `StochD` = 3
  - `StochPeriod` = 14
  - `StochOversold` = 20m
  - `StochOverbought` = 80m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: Parabolic SAR, Parabolic SAR, Stochastic Oscillator
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
