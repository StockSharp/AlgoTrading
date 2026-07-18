# Estratégia Multi-timeframe EMA + BB + RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Combina duas médias móveis exponenciais, Bollinger Bands e RSI para operar rebotes. Operações compradas ocorrem quando o preço fecha acima da EMA rápida após tocar a banda inferior. Operações vendidas são acionadas quando o preço fecha abaixo da EMA rápida após perfurar a banda superior e RSI está acima de 50.

A tomada de lucro opcional fecha a posição após um número de barras definido pelo usuário se o preço se mover favoravelmente. O sistema é flexível o suficiente para trading swing ou intradiário e suporta habilitar ou desabilitar os lados comprado e vendido de forma independente.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: Fechamento acima da EMA rápida com uma mínima perfurando a banda inferior de Bollinger Bands.
  - **Vendido**: Fechamento abaixo da EMA rápida com uma máxima perfurando a banda superior e RSI > 50.
- **Critérios de saída**:
  - Comprado: RSI sobe acima do nível de sobrevenda.
  - Vendido: Preço fecha abaixo da banda inferior.
- **Indicadores**:
  - Duas EMAs (períodos 10 e 55)
  - Bollinger Bands (comprimento 20, multiplicador 2)
  - RSI (comprimento 14, sobrevenda 71)
- **Stops**: Alvo de lucro opcional após X barras; sem stop-loss fixo.
- **Valores padrão**:
  - `Ma1Period` = 10
  - `Ma2Period` = 55
  - `BBLength` = 20
  - `BBMultiplier` = 2.0
  - `RSILength` = 14
  - `RSIOversold` = 71
  - `XBars` = 12
- **Filtros**:
  - Reversão à média com filtro de tendência
  - Período: Configurável
  - Indicadores: EMA, Bollinger Bands, RSI
  - Stops: Opcional
  - Complexidade: Moderado
