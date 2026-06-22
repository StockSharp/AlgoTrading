# Estratégia StepMA NRTR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia seguidora de tendência baseada no indicador StepMA NRTR. O indicador combina uma média móvel em degraus com um mecanismo de reversão Nick Rar Trend e gera sinais de compra ou venda quando a tendência muda.

## Detalhes

- **Critérios de entrada**: Sinal de compra/venda do StepMA NRTR
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Sinal oposto do StepMA NRTR
- **Stops**: Nenhum
- **Valores padrão**:
  - `Length` = 10
  - `Kv` = 1
  - `StepSize` = 0
  - `UseHighLow` = true
  - `CandleType` = Período 1 hora
  - `BuyPosOpen` = true
  - `SellPosOpen` = true
  - `BuyPosClose` = true
  - `SellPosClose` = true
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: StepMA NRTR
  - Stops: Nenhum
  - Complexidade: Moderado
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
