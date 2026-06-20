# Keltner Rsi Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que combina os indicadores Keltner Channels e RSI. Busca oportunidades de reversão à média quando o preço toca os limites do canal e o RSI confirma condições de sobrevenda/sobrecompra.

Os testes indicam um retorno anual médio de aproximadamente 88%. Funciona melhor no mercado de ações.

Os Keltner Channels mapeiam a volatilidade recente enquanto o RSI mede os extremos do momentum. As entradas ocorrem quando o RSI apoia um movimento além do canal.

Ótimo para traders de rebote em torno de envelopes de volatilidade. Os stops dependem de um multiplicador de ATR.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `Close < LowerBand && RSI < RsiOversoldLevel`
  - Vendido: `Close > UpperBand && RSI > RsiOverboughtLevel`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - O preço retorna à EMA
- **Stops**: Baseados em porcentagem usando `StopLossPercent`
- **Valores padrão**:
  - `EmaPeriod` = 20
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2.0m
  - `RsiPeriod` = 14
  - `RsiOverboughtLevel` = 70m
  - `RsiOversoldLevel` = 30m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: Keltner Channel, RSI
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

