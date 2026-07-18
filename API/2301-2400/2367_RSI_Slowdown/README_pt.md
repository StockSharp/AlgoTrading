# Estratégia de Desaceleração RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia de Desaceleração RSI reage a leituras extremas do Índice de Força Relativa que mostram sinais de enfraquecimento do momentum. Quando o RSI se aproxima de zonas de sobrecompra ou sobrevenda e sua variação entre barras cai abaixo de um ponto, a estratégia assume que o mercado está pronto para uma reversão.

Uma posição comprada é aberta quando o RSI atinge ou supera o nível superior e o crescimento do indicador desacelera. Uma posição vendida é aberta quando o RSI cai ao nível inferior com uma desaceleração semelhante. Qualquer posição oposta existente é fechada antes de entrar em uma nova operação.

A configuração padrão usa velas de 6 horas e um RSI de 2 períodos com limites de 90 e 10. Esses valores imitam a implementação original do MetaTrader.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: RSI >= `LevelMax` e `|RSI - prev RSI| < 1` (quando a desaceleração está habilitada)
  - **Vendido**: RSI <= `LevelMin` e `|RSI - prev RSI| < 1` (quando a desaceleração está habilitada)
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**:
  - **Comprado**: Sinal oposto ou entrada vendida.
  - **Vendido**: Sinal oposto ou entrada comprada.
- **Stops**: Sem stops automáticos.
- **Valores padrão**:
  - `RsiPeriod` = 2
  - `LevelMax` = 90
  - `LevelMin` = 10
  - `SeekSlowdown` = true
  - `CandleType` = `TimeSpan.FromHours(6)`
- **Filtros**:
  - Categoria: Reversão
  - Direção: Ambos
  - Indicadores: RSI
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário a swing
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Sim (desaceleração)
  - Nível de risco: Médio
