# Estratégia de Scalping Inteligente PRO Aprimorada v2 para NAS100 e Ouro
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia faz scalping de movimentos de curto prazo usando EMA9 e VWAP como guias dinâmicas, RSI para momentum e ATR para gestão de risco. Um filtro de tendência EMA200 de 15 minutos mantém as operações na direção da tendência predominante, enquanto um filtro de pico de volume busca velas fortes. O tamanho das posições é calculado por risco e são suportados trailing stops opcionais e períodos de espera entre operações.

## Detalhes

- **Critérios de entrada**: sinal de indicador
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: stop-loss, take-profit ou sinal oposto
- **Stops**: Sim, baseados em ATR
- **Valores padrão**:
  - `CandleType` = 1 minute
  - `RiskPercent` = 1%
  - `AtrMultiplierSl` = 1
  - `AtrMultiplierTp` = 2
  - `CooldownMins` = 30
  - `StartHour` = 13
  - `EndHour` = 20
- **Filtros**:
  - Categoria: Scalping
  - Direção: Ambos
  - Indicadores: EMA, VWAP, RSI, ATR, EMA200
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
