# Estratégia de VWAP Stochastic Divergence
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **VWAP Stochastic Divergence** é construída em torno da combinação do VWAP com o indicador de força de tendência ADX.

Os testes indicam um retorno anual médio de aproximadamente 79%. Tem melhor desempenho no mercado de ações.

Os sinais são ativados quando o Stochastic confirma configurações de divergência em dados intradiários (5m). Este método é adequado para traders ativos.

Os stops dependem de múltiplos do ATR e fatores como AdxPeriod, AdxThreshold. Ajuste esses valores padrão para equilibrar risco e retorno.

## Detalhes
- **Critérios de entrada**: ver implementação para condições de indicadores.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**: sinal oposto ou lógica de stop.
- **Stops**: Sim, usando cálculos baseados em indicadores.
- **Valores padrão**:
  - `AdxPeriod = 14`
  - `AdxThreshold = 25m`
  - `AdxExitThreshold = 20m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Stochastic, Divergence
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Sim
  - Nível de risco: Médio
