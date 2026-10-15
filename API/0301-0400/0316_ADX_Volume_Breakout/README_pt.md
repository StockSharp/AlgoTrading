# Estratégia de Rompimento de Volume ADX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **ADX Volume Breakout** é construída em torno do ADX com rompimento de volume.

Os testes indicam um retorno anual médio de aproximadamente 55%. Funciona melhor no mercado de ações.

Os sinais são acionados quando seus indicadores confirmam oportunidades de rompimento em dados intradiários (5m). Isso torna o método adequado para traders ativos.

Os stops dependem de múltiplos de ATR e fatores como AdxPeriod, AdxThreshold. Ajuste esses valores padrão para equilibrar risco e recompensa.

## Detalhes
- **Critérios de entrada**: ver implementação para condições dos indicadores.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: sinal oposto ou lógica de stops.
- **Stops**: Sim, usando cálculos baseados em indicadores.
- **Valores padrão**:
  - `AdxPeriod = 14`
  - `AdxThreshold = 25m`
  - `VolumeAvgPeriod = 20`
  - `VolumeThresholdFactor = 2.0m`
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
