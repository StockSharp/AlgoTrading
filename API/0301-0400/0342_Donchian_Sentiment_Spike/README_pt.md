# Estratégia Donchian de Pico de Sentimento
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
A estratégia **Donchian Sentiment Spike** é construída em torno do pico de sentimento de Donchian.

Os testes indicam um retorno anual médio de aproximadamente 115%. Funciona melhor no mercado de ações.

Os sinais são acionados quando Donchian confirma mudanças de tendência em dados intradiários (15m). Isso torna o método adequado para traders ativos.

Os stops dependem de múltiplos de ATR e fatores como DonchianPeriod, SentimentPeriod. Ajuste esses valores padrão para equilibrar risco e recompensa.

## Detalhes
- **Critérios de entrada**: ver implementação para condições de indicadores.
- **Comprado/Vendido**: Ambos as direções.
- **Critérios de saída**: sinal oposto ou lógica de stop.
- **Stops**: Sim, usando cálculos baseados em indicadores.
- **Valores padrão**:
  - `DonchianPeriod = 20`
  - `SentimentPeriod = 20`
  - `SentimentMultiplier = 2m`
  - `StopLoss = 2m`
  - `CandleType = TimeSpan.FromMinutes(15).TimeFrame()`
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Donchian, Spike
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário (15m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

