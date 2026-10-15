# Estratégia ColorNonLagDot MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que usa o indicador MACD com vários modos de detecção de sinais. A abordagem foi portada do consultor especialista MQL "Exp_ColorNonLagDotMACD".

## Detalhes

- **Critérios de entrada**: Depende do modo selecionado (rompimento da linha zero, virada do MACD, virada da linha de sinal ou cruzamento do MACD com a linha de sinal).
- **Comprado/Vendido**: Ambas as direções, podem ser habilitadas separadamente.
- **Critérios de saída**: Sinais opostos ou stop/alvo configurado.
- **Stops**: Stop-loss e take-profit opcionais baseados em percentual.
- **Valores padrão**:
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `Mode` = `MacdDisposition`
  - `TakeProfitPercent` = 4
  - `StopLossPercent` = 2
  - `CandleType` = TimeSpan.FromHours(4)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: MACD
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: 4H
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
