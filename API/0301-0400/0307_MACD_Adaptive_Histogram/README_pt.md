# Estratégia de Histograma Adaptativo MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **MACD Adaptive Histogram** é construída em torno do MACD com limiar de histograma adaptativo.

Os testes indicam um retorno anual médio de aproximadamente 184%. Funciona melhor no mercado de criptomoedas.

### Regras reais do histograma MACD (C# e Python)

Usa **candles concluídos de 15 minutos** e o histograma MACD(12,26,9) `H = MACD - Signal`. Para os últimos 20 valores de H, calcula SMA `A` e desvio padrão `D`. Com indicadores formados e `D > 0`, sem posição **compra** se `H >= A + 2*D` e `H > 0`, **vende** se `H <= A - 2*D` e `H < 0`. Fecha long se `H <= A`, short se `H >= A`. Após ordem de sinal, **16 candles concluídos** de pausa. As posições executadas possuem **stop nativo de 2%**, não ATR. A rentabilidade citada acima não foi validada independentemente.



## Detalhes
- **Critérios de entrada**: ver implementação para as condições do indicador.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: sinal oposto ou lógica de stop.
- **Stops**: Sim, usando cálculos baseados em indicadores.
- **Valores padrão**:
  - `FastPeriod = 12`
  - `SlowPeriod = 26`
  - `SignalPeriod = 9`
  - `HistogramAvgPeriod = 20`
  - `StdDevMultiplier = 2.0m`
  - `StopLossPercent = 2.0m`
  - `CandleType = TimeSpan.FromMinutes(15).TimeFrame()`
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Histogram
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário (15m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
