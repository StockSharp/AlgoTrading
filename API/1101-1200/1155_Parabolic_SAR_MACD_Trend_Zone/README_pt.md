# Parabolic SAR com Confirmação de MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia combina o indicador Parabolic SAR com a confirmação do MACD. Uma posição é aberta quando o preço cruza o SAR em uma direção suportada pelo MACD, com o objetivo de capturar reversões de tendência.

## Detalhes

- **Critérios de entrada**: O preço cruza o SAR e a linha MACD está no mesmo lado da sua linha de sinal.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Cruzamento oposto de preço/SAR ou MACD.
- **Stops**: Não.
- **Valores padrão**:
  - `SarStart` = 0.02m
  - `SarIncrement` = 0.02m
  - `SarMax` = 0.2m
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Parabolic SAR, MACD
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
