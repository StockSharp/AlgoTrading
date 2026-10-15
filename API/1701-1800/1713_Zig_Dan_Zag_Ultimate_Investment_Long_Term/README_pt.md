# Zig Dan Zag Investimento de Longo Prazo Definitivo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de investimento de longo prazo que combina pivôs ZigZag com um filtro de tendência SMA lento. Uma posição é aberta quando um novo mínimo ZigZag se forma acima da SMA, enquanto as saídas ocorrem em pivôs opostos abaixo da SMA.

## Detalhes
- **Critérios de entrada**: Novo mínimo ZigZag acima da SMA.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: Máximo ZigZag abaixo da SMA.
- **Stops**: Não.
- **Valores padrão**:
  - `ZigzagDepth` = 12
  - `SmaLength` = 200
  - `CandleType` = TimeSpan.FromHours(1)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Somente comprado
  - Indicadores: Highest, Lowest, SimpleMovingAverage
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Longo prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
