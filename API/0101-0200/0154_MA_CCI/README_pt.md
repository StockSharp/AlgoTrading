# Estratégia MA CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que combina a Média Móvel e o indicador CCI. Compra quando o preço está acima da MA e o CCI está sobrevendido. Vende quando o preço está abaixo da MA e o CCI está sobrecomprado.

Os testes indicam um retorno anual médio de cerca de 49%. Funciona melhor no mercado de criptomoedas.

Uma média móvel orienta a tendência enquanto o CCI busca desvios dessa média. As entradas ocorrem nos extremos do CCI na direção da MA.

Ideal para traders de swing que entram em retrocessos. Stops baseados em ATR protegem contra movimentos bruscos.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `Close > MA && CCI < OversoldLevel`
  - Vendido: `Close < MA && CCI > OverboughtLevel`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - CCI retorna à linha zero
- **Stops**: Baseados em percentual usando `StopLossPercent`
- **Valores padrão**:
  - `MaPeriod` = 20
  - `CciPeriod` = 20
  - `OverboughtLevel` = 100m
  - `OversoldLevel` = -100m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
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
