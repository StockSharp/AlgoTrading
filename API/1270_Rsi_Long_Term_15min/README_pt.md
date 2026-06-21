# Estratégia RSI de Longo Prazo 15min
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia usa sinais de sobrevenda do RSI combinados com médias móveis de longo prazo e confirmação de volume para entrar em posições compradas. Compra quando o RSI está abaixo de 30, a SMA(250) está acima da SMA(500) e o volume é significativamente superior à média.

## Detalhes

- **Critérios de entrada**: RSI abaixo de 30, SMA(250) acima de SMA(500) e volume maior que 2,5 vezes sua SMA de 20 períodos
- **Comprado/Vendido**: Somente comprado
- **Critérios de saída**: SMA(250) cruzando abaixo de SMA(500) ou stop-loss
- **Stops**: Sim, percentual fixo
- **Valores padrão**:
  - `RsiLength` = 10
  - `VolumeSmaLength` = 20
  - `Sma1Length` = 250
  - `Sma2Length` = 500
  - `VolumeMultiplier` = 2.5
  - `StopLossPercent` = 5
- **Filtros**:
  - Categoria: Tendência
  - Direção: Comprado
  - Indicadores: RSI, SMA
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
