# Estratégia BBTrend SuperTrend Decision
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia deriva o valor **BBTrend** a partir de duas Bandas de Bollinger com comprimentos diferentes e o alimenta em um cálculo de SuperTrend. A direção resultante do SuperTrend decide se deve abrir posições compradas ou vendidas. Proteções opcionais de take-profit e stop-loss baseadas em percentagem podem ser ativadas.

## Detalhes

- **Critérios de entrada**:
  - Comprado: A direção do SuperTrend é para cima.
  - Vendido: A direção do SuperTrend é para baixo.
- **Comprado/Vendido**: Ambos, configurável.
- **Critérios de saída**:
  - Direção oposta do SuperTrend.
- **Stops**: TP/SL percentual opcional.
- **Valores padrão**:
  - Comprimento BB curto = 20, Comprimento BB longo = 50, StdDev = 2.
  - Comprimento SuperTrend = 10, fator = 7.
  - Take Profit = 30%, Stop Loss = 20%.
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Bollinger Bands, SuperTrend
  - Stops: TP/SL opcional
  - Complexidade: Moderado
  - Período: Curto
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
