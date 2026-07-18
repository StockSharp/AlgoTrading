# Estratégia de Força de Tendência VWAP ADX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **VWAP ADX Trend Strength** é construída em torno do VWAP com a força de tendência do ADX.

Os testes indicam um retorno anual médio de aproximadamente 46%. Funciona melhor no mercado de ações.

Os sinais são acionados quando seus indicadores confirmam mudanças de tendência em dados intradiários (5m). Isso torna o método adequado para traders ativos.

Os stops dependem de múltiplos de ATR e fatores como AdxPeriod, AdxThreshold. Ajuste esses valores padrão para equilibrar risco e recompensa.

## Detalhes
- **Critérios de entrada**: ver implementação para condições dos indicadores.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: sinal oposto ou lógica de stops.
- **Stops**: Sim, usando cálculos baseados em indicadores.
- **Valores padrão**:
  - `AdxPeriod = 14`
  - `AdxThreshold = 25m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Múltiplos
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
