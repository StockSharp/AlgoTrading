# Estratégia Omar MMR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Método baseado em momentum que combina RSI, três médias móveis exponenciais e um cruzamento de MACD. Operações compradas ocorrem quando o preço está acima da EMA lenta, a EMA rápida supera a EMA média, MACD cruza de forma altista e RSI está em uma zona neutra entre 29 e 70.

Os percentuais de take-profit e stop-loss são aplicados através do módulo de proteção do motor. A configuração foca em alinhar momentum e tendência enquanto evita leituras de RSI sobreextendidas.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: Fechamento acima de EMA C, EMA A > EMA B, a linha MACD cruza acima do sinal, RSI entre 29 e 70.
- **Critérios de saída**:
  - Gerenciado via take-profit ou stop-loss; sem saída explícita por indicador.
- **Indicadores**:
  - RSI (comprimento 14)
  - EMA A/B/C (períodos 20/50/200)
  - MACD (12,26,9)
- **Stops**: Take-profit baseado em percentual 1.5% e stop-loss 2% por padrão.
- **Valores padrão**:
  - `RsiLength` = 14
  - `EmaALength` = 20
  - `EmaBLength` = 50
  - `EmaCLength` = 200
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdSignalLength` = 9
  - `TakeProfitPercent` = 1.5
  - `StopLossPercent` = 2.0
- **Filtros**:
  - Continuação de tendência
  - Período único
  - Indicadores: RSI, EMA, MACD
  - Stops: Sim
  - Complexidade: Moderado
