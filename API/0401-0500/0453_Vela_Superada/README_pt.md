# Estratégia Vela Superada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Vela Superada opera um padrão de reversão de dois candles. Uma configuração
bullish ocorre quando um candle bearish é imediatamente seguido por um candle bullish que
fecha acima da abertura anterior. As operações são filtradas com uma EMA de curto prazo,
RSI e tendência MACD para evitar sinais contra a tendência. Tanto o lado comprado quanto
o vendido podem ser habilitados.

A estratégia emprega níveis de take profit e stop loss baseados em porcentagem e aperta
dinamicamente um trailing stop uma vez que o preço se move favoravelmente. Isso permite
capturar movimentos estendidos enquanto protege contra reversões.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: Candle anterior bearish, atual bullish, fechamento e fechamento anterior acima da EMA, RSI < 65, MACD subindo.
  - **Vendido**: Candle anterior bullish, atual bearish, fechamento e fechamento anterior abaixo da EMA, RSI > 35, MACD caindo.
- **Comprado/Vendido**: Configurável (comprado por padrão).
- **Critérios de saída**:
  - Trailing stop ou sinal oposto.
- **Stops**: Stop loss e take profit baseados em porcentagem.
- **Valores padrão**:
  - `EmaLength` = 10
  - `RsiLength` = 14
  - `ShowLong` = True
  - `ShowShort` = False
  - `TpPercent` = 1.2
  - `SlPercent` = 1.8
- **Filtros**:
  - Categoria: Padrão + indicadores
  - Direção: Ambos
  - Indicadores: EMA, RSI, MACD
  - Stops: Sim
  - Complexidade: Médio
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Sim
  - Nível de risco: Médio
