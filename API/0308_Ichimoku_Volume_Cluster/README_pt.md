# Estratégia de Cluster de Volume com Ichimoku
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **Ichimoku Volume Cluster** é construída em torno da Nuvem Ichimoku com confirmação por cluster de volume.

Os sinais são disparados quando os indicadores confirmam mudanças de tendência em dados intradiários (1h). Isso torna o método adequado para traders ativos.

Os stops se baseiam em múltiplos de ATR e fatores como TenkanPeriod, KijunPeriod. Ajuste esses valores padrão para equilibrar risco e recompensa.

## Detalhes
- **Critérios de entrada**: ver implementação para as condições do indicador.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: sinal oposto ou lógica de stop.
- **Stops**: Sim, usando cálculos baseados em indicadores.
- **Valores padrão**:
  - `TenkanPeriod = 9`
  - `KijunPeriod = 26`
  - `SenkouSpanBPeriod = 52`
  - `VolumeAvgPeriod = 20`
  - `VolumeStdDevMultiplier = 2.0m`
  - `CandleType = TimeSpan.FromHours(1).TimeFrame()`
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: múltiplos indicadores
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário (1h)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
