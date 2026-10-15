# Estratégia de Rompimento Adaptativo Bollinger
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
A estratégia **Adaptive Bollinger Breakout** opera com base em rompimentos das Bandas de Bollinger com parâmetros ajustados adaptativamente.

Os sinais são acionados quando o Bollinger confirma oportunidades de rompimento em dados intradiários (5m). Isso torna o método adequado para traders ativos.

Os stops dependem de múltiplos de ATR e fatores como MinBollingerPeriod, MaxBollingerPeriod. Ajuste esses valores padrão para equilibrar risco e recompensa.

## Detalhes
- **Critérios de entrada**: consulte a implementação para as condições dos indicadores.
- **Comprado/Vendido**: Ambos os direções.
- **Critérios de saída**: sinal oposto ou lógica de stop.
- **Stops**: Sim, usando cálculos baseados em indicadores.
- **Valores padrão**:
  - `MinBollingerPeriod = 10`
  - `MaxBollingerPeriod = 30`
  - `MinBollingerDeviation = 1.5m`
  - `MaxBollingerDeviation = 2.5m`
  - `AtrPeriod = 14`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Bollinger
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
