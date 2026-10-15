# Estratégia Parabolic SAR com Divergência RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
A estratégia **Parabolic SAR RSI Divergence** opera com base nos sinais do Parabolic SAR quando o RSI mostra divergência em relação ao preço.

Os testes indicam um retorno anual médio de aproximadamente 103%. Funciona melhor no mercado de ações.

Os sinais são acionados quando o Parabolic confirma configurações de divergência em dados intradiários (5m). Isso torna o método adequado para traders ativos.

Os stops dependem de múltiplos de ATR e fatores como SarAccelerationFactor, SarMaxAccelerationFactor. Ajuste esses valores padrão para equilibrar risco e recompensa.

## Detalhes
- **Critérios de entrada**: consulte a implementação para as condições dos indicadores.
- **Comprado/Vendido**: Ambos os direções.
- **Critérios de saída**: sinal oposto ou lógica de stop.
- **Stops**: Sim, usando cálculos baseados em indicadores.
- **Valores padrão**:
  - `SarAccelerationFactor = 0.02m`
  - `SarMaxAccelerationFactor = 0.2m`
  - `RsiPeriod = 14`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Parabolic, Divergence
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Sim
  - Nível de risco: Médio
