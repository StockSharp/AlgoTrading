# Estratégia de Momentum Intradiário
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Opera dentro de uma sessão especificada usando cruzamento de EMA, filtro RSI e confirmação VWAP. Entra comprado quando a EMA rápida cruza acima da EMA lenta, o RSI está abaixo do nível de sobrecompra e o preço está acima do VWAP. Vendido em condições opostas. Aplica percentuais fixos de stop-loss e take-profit e fecha qualquer posição ao final da sessão.

## Parâmetros

- **EmaFastLength**: Comprimento da EMA rápida.
- **EmaSlowLength**: Comprimento da EMA lenta.
- **RsiLength**: Período do RSI.
- **RsiOverbought**: Nível de sobrecompra do RSI.
- **RsiOversold**: Nível de sobrevenda do RSI.
- **StopLossPerc**: Percentual de stop-loss.
- **TakeProfitPerc**: Percentual de take-profit.
- **StartHour**: Hora de início da sessão.
- **StartMinute**: Minuto de início da sessão.
- **EndHour**: Hora de fim da sessão.
- **EndMinute**: Minuto de fim da sessão.
- **CandleType**: Tipo de velas.

