# Estratégia Ichimoku RSI MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia seguidora de tendência que combina a Nuvem Ichimoku, RSI e sinais de cruzamento do MACD.

## Detalhes

- **Critérios de entrada**: Preço acima/abaixo da nuvem Ichimoku com filtro RSI e linha MACD cruzando a linha de sinal.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Cruzamento MACD oposto.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouSpanBPeriod` = 52
  - `RsiLength` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `CandleType` = TimeSpan.FromHours(1)
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Ichimoku, RSI, MACD
  - Stops: Não
  - Complexidade: Iniciante
  - Período: Intradiário (1h)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
