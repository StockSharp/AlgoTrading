# Estratégia de Swing Lucrativo a Longo Prazo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia entra comprada quando a EMA rápida cruza acima da EMA lenta e o RSI está acima de um limiar especificado. As saídas ocorrem quando o preço atinge os níveis de stop loss ou take profit baseados em ATR.

## Detalhes

- **Critérios de entrada**:
  - Comprado: EMA rápida cruza acima da EMA lenta e RSI > limiar.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**:
  - O preço atinge o stop loss ou take profit baseado em ATR.
- **Stops**: Múltiplos ATR para stop loss e take profit.
- **Valores padrão**:
  - `FastEmaLength` = 16
  - `SlowEmaLength` = 30
  - `RsiLength` = 9
  - `AtrLength` = 21
  - `RsiThreshold` = 50
  - `AtrStopMult` = 8
  - `AtrTpMult` = 11
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Comprado
  - Indicadores: EMA, RSI, ATR
  - Stops: Sim
  - Complexidade: Baixo
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
