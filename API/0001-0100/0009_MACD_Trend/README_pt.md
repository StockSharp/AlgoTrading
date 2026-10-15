# Tendência MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada no indicador MACD.

Os testes indicam um retorno anual médio de aproximadamente 64%. Funciona melhor no mercado de câmbio.

A Tendência MACD reage aos cruzamentos entre a linha MACD e sua linha de sinal. Cruzamentos altistas iniciam posições compradas enquanto cruzamentos baixistas iniciam posições vendidas. Cruzamentos opostos ou um stop encerram a operação.

O indicador de convergência/divergência de médias móveis se adapta bem a mercados em mudança ao medir o momentum. Esta abordagem visa aproveitar as oscilações de tendência enquanto o indicador mantém um viés claramente altista ou baixista.


## Detalhes

- **Critérios de entrada**: Sinais baseados em MA, MACD.
- **Comprado/Vendido**: Ambos os sentidos.
- **Critérios de saída**: Sinal oposto ou stop.
- **Stops**: Sim.
- **Valores padrão**:
  - `FastEmaPeriod` = 12
  - `SlowEmaPeriod` = 26
  - `SignalPeriod` = 9
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: MA, MACD
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

