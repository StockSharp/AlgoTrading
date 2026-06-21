# Estratégia de Cruzamento de EMA ETH/USDT
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera ETH/USDT usando um cruzamento de EMA com filtros adicionais.

Uma posição comprada é aberta quando a EMA de 20 períodos cruza acima da EMA de 50 períodos enquanto o preço está acima da EMA de 200 períodos, o RSI está acima de 30, a volatilidade medida pelo ATR está acima de sua média móvel, e o volume é maior que sua média. Uma posição vendida é aberta nas condições opostas.

As posições se invertem quando o sinal oposto aparece. Não é utilizado stop loss ou take profit explícito.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: `EMA20 cruza acima de EMA50` && `Close > EMA200` && `RSI > 30` && `ATR > SMA(ATR,10)` && `Volume > SMA(Volume,20)`
  - **Vendido**: `EMA20 cruza abaixo de EMA50` && `Close < EMA200` && `RSI < 70` && `ATR > SMA(ATR,10)` && `Volume > SMA(Volume,20)`
- **Comprado/Vendido**: Ambos os lados
- **Critérios de saída**:
  - Sinal inverso
- **Stops**: Não
- **Valores padrão**:
  - `EMA200 Length` = 200
  - `EMA20 Length` = 20
  - `EMA50 Length` = 50
  - `RSI Length` = 14
  - `ATR Length` = 14

- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: EMA, RSI, ATR
  - Stops: Não
  - Complexidade: Moderado
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
