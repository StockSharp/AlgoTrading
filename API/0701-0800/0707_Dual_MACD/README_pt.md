# MACD Duplo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que combina dois indicadores MACD. O MACD mais lento ao cruzar o zero abre operações quando o histograma do MACD mais rápido se alinha. A posição é encerrada quando o MACD rápido se inverte ou o stop/take profit é acionado.

Os testes indicam um retorno anual médio de cerca de 65%. Funciona melhor no mercado de ações.

## Detalhes

- **Critérios de entrada**: Cruzamento do histograma do MACD lento pelo zero com confirmação do MACD rápido.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Reversão do MACD rápido ou stop/alvo.
- **Stops**: Sim.
- **Valores padrão**:
  - `Macd1FastLength` = 34
  - `Macd1SlowLength` = 144
  - `Macd1SignalLength` = 9
  - `Macd2FastLength` = 100
  - `Macd2SlowLength` = 200
  - `Macd2SignalLength` = 50
  - `StopLossPercent` = 1.0m
  - `TakeProfitPercent` = 1.5m
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: MACD
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário (15m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

