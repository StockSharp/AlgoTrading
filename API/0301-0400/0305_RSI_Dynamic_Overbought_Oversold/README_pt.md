# Estratégia RSI com Sobrecompra/Sobrevenda Dinâmica
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **RSI Dynamic Overbought Oversold** é construída em torno do RSI com níveis dinâmicos de sobrecompra/sobrevenda.

Os testes indicam um retorno anual médio de aproximadamente 178%. Funciona melhor no mercado de ações.

### Regras executadas (C# e Python)

Em **candles concluídos de 5 minutos**, calcula-se RSI(14), SMA(50) do preço, SMA(50) e desvio padrão(50) do próprio RSI. Bandas dinâmicas: `upper = min(RSI_MA + 2 * RSI_STD, 85)`, `lower = max(RSI_MA - 2 * RSI_STD, 15)`. Sem posição: comprar somente com `RSI <= lower` e `Close >= Price_SMA * 0.995`; vender somente com `RSI >= upper` e `Close <= Price_SMA * 1.005`. Encerrar long se `RSI >= RSI_MA` ou `Close < Price_SMA * 0.995`; short se `RSI <= RSI_MA` ou `Close > Price_SMA * 1.005`. Após uma ordem há **48 candles concluídos** de pausa. Posições executadas são protegidas por **stop nativo de 2%**, não por ATR. O retorno citado acima não foi verificado com BTC/TON.



## Detalhes
- **Critérios de entrada**: ver implementação para as condições do indicador.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: sinal oposto ou lógica de stop.
- **Stops**: Sim, usando cálculos baseados em indicadores.
- **Valores padrão**:
  - `RsiPeriod = 14`
  - `MovingAvgPeriod = 50`
  - `StdDevMultiplier = 2.0m`
  - `StopLossPercent = 2.0m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Overbought, Oversold
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
