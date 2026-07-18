# Estratégia Rock Trader Neuro
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que opera utilizando Bandas de Bollinger e um neurônio simples.
As últimas sete larguras das Bandas de Bollinger são normalizadas para o intervalo [-1,1] e
combinadas com pesos fixos. A soma ponderada é passada por uma ativação de tangente hiperbólica.
Uma saída negativa abre uma posição comprada, enquanto uma saída positiva abre uma posição vendida.
As posições são fechadas por stop loss ou take profit.

## Detalhes

- **Critérios de entrada**:
  - Comprado: saída do neurônio < 0
  - Vendido: saída do neurônio > 0
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Stop loss ou take profit atingido
- **Stops**: Distância absoluta de preço
- **Valores padrão**:
  - `StopLoss` = 30
  - `TakeProfit` = 100
  - `Lot` = 1
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Neural
  - Direção: Ambos
  - Indicadores: Bollinger Bands
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Sim
  - Divergência: Não
  - Nível de risco: Médio
