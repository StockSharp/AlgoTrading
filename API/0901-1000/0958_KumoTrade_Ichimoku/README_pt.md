# Estratégia KumoTrade Ichimoku
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada em Ichimoku Cloud e Stochastic Oscillator.
Entra comprado quando o preço recua acima do Kijun com Stochastic sobrevendido e sem nuvem à frente.
Entra vendido quando o preço cai abaixo da nuvem com Stochastic sobrecomprado e Kumo de baixa.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `Low > Kijun && Kijun > Tenkan && Close < SenkouA && StochD < 29`
  - Vendido: `Close < min(SenkouA, SenkouB) && High > Kijun && prevStochD > StochD >= 90`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Stop dinâmico baseado em ATR
- **Stops**: Trailing stop usando ATR * 3
- **Valores padrão**:
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouPeriod` = 52
  - `StochK` = 70
  - `StochD` = 15
  - `AtrPeriod` = 5
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Ichimoku Cloud, Stochastic, ATR
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
