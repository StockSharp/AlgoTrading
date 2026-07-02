# Estratégia FRASMAv2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada na Média Móvel Simples Adaptativa Fractal (FRASMAv2).

Esta estratégia calcula uma Média Móvel Simples Adaptativa Fractal usando o indicador Fractal Dimension. A cor do indicador muda dependendo da inclinação: verde para subida, cinza para lateral, magenta para queda. A estratégia observa as transições de cor na última vela fechada:

- Se o indicador estava verde na barra anterior e se torna não verde (cinza ou magenta) na última barra, a estratégia fecha posições vendidas e abre uma nova posição comprada.
- Se o indicador estava magenta e se torna não magenta, a estratégia fecha posições compradas e abre uma nova posição vendida.

O gerenciamento de risco usa os parâmetros de stop-loss e take-profit especificados em pontos.

## Detalhes

- **Critérios de entrada**: Mudanças de cor do FRASMAv2.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Transição de cor oposta.
- **Stops**: Take profit e stop loss via módulo de proteção.
- **Valores padrão**:
  - `Period` = 30
  - `TakeProfit` = 2000 pontos
  - `StopLoss` = 1000 pontos
  - `CandleType` = TimeSpan.FromHours(4)
- **Filtros**:
  - Categoria: Reversão de tendência
  - Direção: Ambos
  - Indicadores: FractalDimension, FRASMAv2
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: 4h
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
