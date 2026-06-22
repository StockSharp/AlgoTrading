# RSI Bollinger Bands
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que combina o Índice de Força Relativa (RSI) com as Bollinger Bands. Uma posição comprada é aberta quando o RSI está abaixo do limiar de sobrevenda e o preço de fechamento está abaixo da banda inferior de Bollinger. Uma posição vendida é aberta quando o RSI está acima do limiar de sobrecompra e o preço de fechamento está acima da banda superior de Bollinger. As posições se revertem em sinais opostos.

## Detalhes

- **Critérios de entrada**: RSI abaixo de `RsiOversold` e preço de fechamento abaixo da banda inferior para comprar; RSI acima de `RsiOverbought` e preço de fechamento acima da banda superior para vender.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Sinal inverso.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `CandleType` = TimeSpan.FromMinutes(15)
  - `RsiPeriod` = 20
  - `BollingerPeriod` = 20
  - `BollingerWidth` = 2
  - `RsiOversold` = 30
  - `RsiOverbought` = 70
- **Filtros**:
  - Categoria: Oscilador
  - Direção: Ambos
  - Indicadores: RSI, Bollinger Bands
  - Stops: Não
  - Complexidade: Básico
  - Período: 15 minutos
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
