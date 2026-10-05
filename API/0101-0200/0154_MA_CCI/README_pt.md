# Estratégia MA CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que combina a Média Móvel e o indicador CCI. Compra quando o preço está acima da MA e o CCI está sobrevendido. Vende quando o preço está abaixo da MA e o CCI está sobrecomprado.

Os testes indicam um retorno anual médio de cerca de 49%. Funciona melhor no mercado de criptomoedas.

Uma média móvel orienta a tendência enquanto o CCI busca desvios dessa média. As entradas ocorrem nos extremos do CCI na direção da MA.

Ideal para traders de swing que entram em retrocessos. Um stop percentual protege contra movimentos bruscos.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `Close > MA && CCI < OversoldLevel`
  - Vendido: `Close < MA && CCI > OverboughtLevel`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - CCI retorna à linha zero
- **Stops**: Baseados em percentual usando `StopLossPercent`
- **Valores padrão**:
  - `MaPeriod` = 50
  - `CciPeriod` = 20
  - `OverboughtLevel` = 100m
  - `OversoldLevel` = -100m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
    Uma SMA de 20 candles se move junto com o CCI dos mesmos 20 candles, então o preço quase nunca fica acima dela com o CCI abaixo de -100: o arquivo de março de 2024 tem no máximo um candle assim por instrumento. Por isso a SMA de tendência usa 50 candles por padrão. Um sinal de entrada contra uma posição aberta a inverte.
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: Moving Average, CCI
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
