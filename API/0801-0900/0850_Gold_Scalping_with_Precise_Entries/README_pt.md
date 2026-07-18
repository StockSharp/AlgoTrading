# Estratégia de Scalping do Ouro com Entradas Precisas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de scalping para ouro usando filtro de tendência EMA, faixa RSI e padrões de engolfo.

## Detalhes

- **Critérios de entrada**: Filtro de tendência EMA com RSI entre 45 e 55 mais padrão de engolfo altista/baixista próximo à EMA50.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Take profit ou stop loss.
- **Stops**: Stop loss baseado em ATR e alvo fixo em pips.
- **Valores padrão**:
  - `EmaFastPeriod` = 50
  - `EmaSlowPeriod` = 200
  - `RsiPeriod` = 14
  - `AtrPeriod` = 14
  - `RsiLower` = 45
  - `RsiUpper` = 55
  - `PipTarget` = 2
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Scalping
  - Direção: Ambos
  - Indicadores: EMA, RSI, ATR
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário (1m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
