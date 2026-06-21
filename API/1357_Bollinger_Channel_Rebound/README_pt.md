# Estratégia de Rebote no Canal Bollinger
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Convertida do script TradingView "strategy1". A estratégia opera rebotes no canal Bollinger. Entra comprado após o preço cair abaixo da banda inferior e depois fechar acima dela. As saídas são acionadas ao cruzar acima da banda do meio, tocar a banda superior ou por stop-loss abaixo do canal.

## Detalhes

- **Critérios de entrada**: O preço estava abaixo da banda inferior e depois fecha acima dela.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: Cruzamento acima da banda do meio, toque da banda superior ou stop-loss abaixo do canal.
- **Stops**: Sim, stop fixo abaixo do canal.
- **Valores padrão**:
  - `Length` = 20
  - `BufferFactor` = 0.2
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Comprado
  - Indicadores: Bollinger Bands
  - Stops: Sim
  - Complexidade: Básico
  - Período: Variável
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
