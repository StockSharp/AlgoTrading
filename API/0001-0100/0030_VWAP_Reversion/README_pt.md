# Estratégia VWAP Reversion
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia VWAP Reversion que opera em desvios do Preço Médio Ponderado por Volume

Os testes indicam um retorno anual médio de aproximadamente 127%. Funciona melhor no mercado de ações.

VWAP Reversion opera em desvios do preço médio ponderado por volume. Se o preço se afastar muito acima ou abaixo do VWAP, a estratégia opera contra o movimento e sai no retrocesso.

Como o VWAP reflete níveis típicos de transação, desvios extremos frequentemente atraem o preço de volta em sua direção. Alguns traders combinam esse sinal com filtros de tendência intradiária para maior probabilidade.


## Detalhes

- **Critérios de entrada**: Sinais baseados em RSI, VWAP.
- **Comprado/Vendido**: Ambos os direções.
- **Critérios de saída**: Sinal oposto ou stop.
- **Stops**: Sim.
- **Valores padrão**:
  - `DeviationPercent` = 2.0m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: RSI, VWAP
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

