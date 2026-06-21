# Estratégia de Cruzamento Triple EMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada em três médias móveis simples.
Uma operação comprada é aberta quando a SMA curta cruza acima da SMA média enquanto todas as três estão alinhadas para cima.
Uma operação vendida é aberta no cruzamento oposto e alinhamento.
O preço cruzando de volta sobre a SMA curta encerra a posição.

## Detalhes

- **Critérios de entrada**: Cruzamentos de SMA1 e SMA2 com filtro de tendência.
- **Comprado/Vendido**: Ambos os direções.
- **Critérios de saída**: Preço cruzando SMA1 ou stops protetores.
- **Stops**: Sim.
- **Valores padrão**:
  - `Sma1Period` = 9
  - `Sma2Period` = 21
  - `Sma3Period` = 55
  - `StopLossTicks` = 200
  - `TakeProfitTicks` = 200
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: SMA
  - Stops: Fixo
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Nenhum
  - Nível de risco: Médio
