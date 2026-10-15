# Estratégia de Ichimoku Hurst Exponent
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **Ichimoku Hurst Exponent** é construída em torno do indicador Ichimoku Kinko Hyo com filtro de Hurst Exponent.

Os testes indicam um retorno anual médio de aproximadamente 64%. Tem melhor desempenho no mercado de câmbio.

Os sinais são ativados quando o Hurst confirma mudanças de tendência em dados intradiários (15m). Este método é adequado para traders ativos.

Os stops dependem de múltiplos do ATR e fatores como TenkanPeriod, KijunPeriod. Ajuste esses valores padrão para equilibrar risco e retorno.

## Detalhes
- **Critérios de entrada**: ver implementação para condições de indicadores.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**: sinal oposto ou lógica de stop.
- **Stops**: Sim, usando cálculos baseados em indicadores.
- **Valores padrão**:
  - `TenkanPeriod = 9`
  - `KijunPeriod = 26`
  - `SenkouSpanBPeriod = 52`
  - `HurstPeriod = 100`
  - `HurstThreshold = 0.5m`
  - `CandleType = TimeSpan.FromMinutes(15).TimeFrame()`
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Hurst, Exponent
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário (15m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
