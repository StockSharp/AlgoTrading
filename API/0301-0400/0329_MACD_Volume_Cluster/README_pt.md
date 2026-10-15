# Estratégia de MACD Volume Cluster
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
A estratégia **MACD Volume Cluster** é construída em torno da análise de clusters de volume MACD.

Os sinais são acionados quando os indicadores confirmam mudanças de tendência em dados intradiários (5m). Isso torna o método adequado para traders ativos.

Os stops dependem de múltiplos de ATR e fatores como FastMacdPeriod, SlowMacdPeriod. Ajuste esses valores padrão para equilibrar risco e recompensa.

## Detalhes
- **Critérios de entrada**: consulte a implementação para as condições dos indicadores.
- **Comprado/Vendido**: Ambos os direções.
- **Critérios de saída**: sinal oposto ou lógica de stop.
- **Stops**: Sim, usando cálculos baseados em indicadores.
- **Valores padrão**:
  - `FastMacdPeriod = 12`
  - `SlowMacdPeriod = 26`
  - `MacdSignalPeriod = 9`
  - `VolumePeriod = 20`
  - `VolumeDeviationFactor = 2.0m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Múltiplos indicadores
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
