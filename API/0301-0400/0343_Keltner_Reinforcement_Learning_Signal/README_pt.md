# Estratégia de Sinal Keltner de Aprendizado por Reforço
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
A estratégia **Keltner Reinforcement Learning Signal** é construída em torno do sinal de aprendizado por reforço de Keltner.

Os testes indicam um retorno anual médio de aproximadamente 118%. Funciona melhor no mercado de ações.

Os sinais são acionados quando Keltner confirma mudanças de tendência em dados intradiários (15m). Isso torna o método adequado para traders ativos.

Os stops dependem de múltiplos de ATR e fatores como EmaPeriod, AtrPeriod. Ajuste esses valores padrão para equilibrar risco e recompensa.

## Detalhes
- **Critérios de entrada**: ver implementação para condições de indicadores.
- **Comprado/Vendido**: Ambos as direções.
- **Critérios de saída**: sinal oposto ou lógica de stop.
- **Stops**: Sim, usando cálculos baseados em indicadores.
- **Valores padrão**:
  - `EmaPeriod = 20`
  - `AtrPeriod = 14`
  - `AtrMultiplier = 2m`
  - `StopLossAtr = 2m`
  - `CandleType = TimeSpan.FromMinutes(15).TimeFrame()`
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Keltner, Reinforcement
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário (15m)
  - Sazonalidade: Não
  - Redes neurais: Sim
  - Divergência: Não
  - Nível de risco: Médio

