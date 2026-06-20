# Entrada BB HeikinAshi
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de Bollinger Bands usando velas Heikin Ashi.

O sistema aguarda duas ou três barras Heikin Ashi baixistas consecutivas que toquem a banda inferior de Bollinger. Uma vela altista que feche de volta acima da banda aciona uma entrada comprada. As vendidas funcionam na direção oposta. Metade da posição é encerrada no primeiro alvo e o restante é protegido com um stop trailing.

## Detalhes

- **Critérios de entrada**: Reversão de velas Heikin Ashi consecutivas ao redor das Bollinger Bands.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Realização parcial de lucros e stop trailing.
- **Stops**: Sim.
- **Valores padrão**:
  - `BollingerLength` = 20
  - `BollingerWidth` = 2
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filtros**:
  - Categoria: Reversão
  - Direção: Ambos
  - Indicadores: Heikin Ashi, Bollinger Bands
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário (15m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
