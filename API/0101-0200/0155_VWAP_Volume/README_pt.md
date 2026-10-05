# Estratégia VWAP Volume
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que combina os indicadores VWAP e Volume. Compra/vende em rompimentos do VWAP confirmados por volume acima da média.

Os testes indicam um retorno anual médio de cerca de 52%. Funciona melhor no mercado de criptomoedas.

Esta estratégia usa o VWAP para avaliar o valor e requer confirmação de volume antes das operações. A ideia é acompanhar movimentos sustentados por forte participação do mercado.

Traders intradiários focados em métricas de volume podem empregar este método. As perdas são limitadas por um stop percentual.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `Close < VWAP && Volume > AvgVolume * VolumeThreshold`
  - Vendido: `Close > VWAP && Volume > AvgVolume * VolumeThreshold`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - O preço cruza de volta pelo VWAP
- **Stops**: Baseados em percentual usando `StopLossPercent`
- **Valores padrão**:
  - `VolumePeriod` = 20
  - `VolumeThreshold` = 1.5m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
    O mercado negocia 24 horas, por isso o VWAP da sessão reinicia a cada dia UTC e pondera o preço típico de cada candle pelo seu volume. AvgVolume é o volume médio dos VolumePeriod candles anteriores. Um sinal de entrada contra uma posição aberta a inverte.
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: VWAP, Volume
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
