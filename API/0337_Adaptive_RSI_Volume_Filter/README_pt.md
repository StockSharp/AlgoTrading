# Estratégia RSI Adaptativo com Filtro de Volume
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
A estratégia **Adaptive RSI Volume Filter** opera com base em RSI Adaptativo com confirmação de volume.

Os testes indicam um retorno anual médio de aproximadamente 106%. Funciona melhor no mercado de ações.

Os sinais são acionados quando os indicadores confirmam entradas filtradas em dados intradiários (5m). Isso torna o método adequado para traders ativos.

Os stops dependem de múltiplos de ATR e fatores como MinRsiPeriod, MaxRsiPeriod. Ajuste esses valores padrão para equilibrar risco e recompensa.

## Detalhes
- **Critérios de entrada**: consulte a implementação para as condições dos indicadores.
- **Comprado/Vendido**: Ambos os direções.
- **Critérios de saída**: sinal oposto ou lógica de stop.
- **Stops**: Sim, usando cálculos baseados em indicadores.
- **Valores padrão**:
  - `MinRsiPeriod = 10`
  - `MaxRsiPeriod = 20`
  - `AtrPeriod = 14`
  - `VolumeLookback = 20`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Múltiplos indicadores
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
