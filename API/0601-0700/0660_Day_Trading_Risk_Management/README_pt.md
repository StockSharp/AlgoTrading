# Estratégia de Day Trading com Gestão de Risco
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia entra comprada quando a EMA rápida cruza acima da EMA lenta enquanto o RSI está abaixo do nível de sobrecompra. Entra vendida quando a EMA rápida cruza abaixo da EMA lenta enquanto o RSI está acima do nível de sobrevenda. O gerenciamento de risco usa percentuais fixos de take profit e stop loss.

### Regras exatas de entrada e proteção

Padrões: `CandleType=1 minuto`, `FastEmaPeriod=20`, `SlowEmaPeriod=50`, `RsiLength=14`, `RsiOverbought=70`, `RsiOversold=30`, `TakeProfitPercent=2`, `StopLossPercent=1`. Em **candle concluído**, entra long se antes `EMA20 <= EMA50`, agora `EMA20 > EMA50` e `RSI14 < 70`; short se antes `EMA20 >= EMA50`, agora `EMA20 < EMA50` e `RSI14 > 30`. Pode abrir sem posição ou reverter a posição oposta com `Volume + abs(Position)`. **Sem confirmação RSI, nenhum sinal entra**. Posições executadas recebem **take profit nativo fixo de 2% e stop loss de 1%**, sem trailing, ativos entre candles. Antes havia apenas o cruzamento EMA120/450, sem RSI e proteção. Defaults numéricos não foram especificados no README original e foram definidos explicitamente aqui.



## Parâmetros
- Tipo de vela
- Comprimento da EMA rápida
- Comprimento da EMA lenta
- Comprimento do RSI
- RSI Sobrecompra
- RSI Sobrevenda
- Percentual de take profit
- Percentual de stop loss
