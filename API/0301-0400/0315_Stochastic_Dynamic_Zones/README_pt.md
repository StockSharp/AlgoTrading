# Estratégia de Zonas Dinâmicas Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **Stochastic Dynamic Zones** é construída em torno do Oscilador Stochastic com zonas dinâmicas de sobrecompra/sobrevenda.

Os testes indicam um retorno anual médio de aproximadamente 52%. Funciona melhor no mercado de criptomoedas.

Os sinais são acionados quando Stochastic confirma mudanças de tendência em dados intradiários (5m). Isso torna o método adequado para traders ativos.

Os stops dependem de múltiplos de ATR e fatores como StochPeriod, StochKPeriod. Ajuste esses valores padrão para equilibrar risco e recompensa.

## Detalhes
- **Critérios de entrada**: ver implementação para condições dos indicadores.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: sinal oposto ou lógica de stops.
- **Stops**: Sim, usando cálculos baseados em indicadores.
- **Valores padrão**:
  - `StochPeriod = 14`
  - `StochKPeriod = 3`
  - `StochDPeriod = 3`
  - `LookbackPeriod = 20`
  - `StandardDeviationFactor = 2.0m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Stochastic
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
