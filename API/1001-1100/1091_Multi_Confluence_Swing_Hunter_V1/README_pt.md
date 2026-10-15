# Estratégia de Caça de Swings Multi-Confluência V1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia de Caça de Swings Multi-Confluência V1 usa um sistema de pontuação combinando RSI, MACD e ação do preço para identificar mínimos e máximos de swing. Uma operação comprada é aberta quando os sinais de alta atingem a pontuação mínima de entrada e fechada quando os sinais de baixa atingem a pontuação de saída.

## Detalhes

- **Critérios de entrada**: Pontuação de entrada ≥ `MinEntryScore` a partir de sinais RSI/MACD e estrutura altista de velas.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: Pontuação de saída ≥ `MinExitScore` a partir de sinais RSI/MACD e estrutura baixista de velas.
- **Stops**: Não.
- **Valores padrão**:
  - `MacdFast` = 3
  - `MacdSlow` = 10
  - `MacdSignal` = 3
  - `RsiLength` = 21
  - `MinEntryScore` = 13
  - `MinExitScore` = 13
  - `MinLowerWickPercent` = 50
  - `RsiOversold` = 30
  - `RsiExtremeOversold` = 25
  - `RsiOverbought` = 70
  - `RsiExtremeOverbought` = 75
  - `CandleType` = TimeSpan.FromHours(1)
- **Filtros**:
  - Categoria: Reversão
  - Direção: Somente comprado
  - Indicadores: RSI, MACD
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
