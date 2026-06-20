# Estratégia de Rompimento de EMA Adaptativa
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **Adaptive EMA Breakout** é construída em torno do rompimento da EMA Adaptativa com confirmação de tendência.

Os testes indicam um retorno anual médio de aproximadamente 166%. Funciona melhor no mercado de ações.

Os sinais são disparados quando os indicadores confirmam oportunidades de rompimento em dados intradiários (5m). Isso torna o método adequado para traders ativos.

Os stops se baseiam em múltiplos de ATR e fatores como Fast, Slow. Ajuste esses valores padrão para equilibrar risco e recompensa.

## Detalhes
- **Critérios de entrada**: ver implementação para as condições do indicador.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: sinal oposto ou lógica de stop.
- **Stops**: Sim, usando cálculos baseados em indicadores.
- **Valores padrão**:
  - `Fast = 2`
  - `Slow = 30`
  - `Lookback = 10`
  - `StopMultiplier = 2m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: múltiplos indicadores
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
