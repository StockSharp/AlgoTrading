# Estratégia de Reversão do Histograma MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O histograma MACD representa a diferença entre a linha MACD e sua linha de sinal. Cruzamentos acima ou abaixo de zero frequentemente marcam mudanças de momentum. Esta estratégia opera esses cruzamentos da linha zero e gerencia o risco com um stop percentual.

Os testes indicam um retorno anual médio de aproximadamente 160%. Funciona melhor no mercado de câmbio.

Em cada candle, o histograma MACD é calculado. Quando passa de negativo para positivo, uma posição comprada é aberta. Uma mudança de positivo para negativo aciona uma venda a descoberto. Como a estratégia busca apenas o cruzamento de zero, as operações são diretas e tipicamente de curto prazo.

Stops são usados para conter perdas se o momentum não continuar na direção esperada.

## Detalhes

- **Critérios de entrada**: O histograma MACD cruza o zero.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Stop-loss.
- **Stops**: Sim, baseado em percentual.
- **Valores padrão**:
  - `FastPeriod` = 12
  - `SlowPeriod` = 26
  - `SignalPeriod` = 9
  - `StopLoss` = 2%
  - `CandleType` = 15 minute
- **Filtros**:
  - Categoria: Momentum
  - Direção: Ambos
  - Indicadores: MACD
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

