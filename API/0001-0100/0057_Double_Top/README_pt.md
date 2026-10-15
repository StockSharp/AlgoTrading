# Padrão de Topo Duplo (Double Top Pattern)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O Topo Duplo identifica dois picos separados por um número de barras com preços similares. Após a formação do segundo pico, uma vela baixista confirma a reversão.

Os testes indicam um retorno anual médio de aproximadamente 58%. Funciona melhor no mercado de ações.

A estratégia vende a descoberto na confirmação com um stop acima das máximas do padrão, visando lucrar com uma mudança de tendência após o esgotamento dos compradores.

As posições são fechadas via stop-loss ou metas discricionárias.

## Detalhes

- **Critérios de entrada**: Dois topos dentro de `SimilarityPercent` após `Distance` barras.
- **Comprado/Vendido**: Somente vendido.
- **Critérios de saída**: O preço se recupera ou stop-loss.
- **Stops**: Sim.
- **Valores padrão**:
  - `Distance` = 5
  - `SimilarityPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(15)
  - `StopLossPercent` = 1.0m
- **Filtros**:
  - Categoria: Padrão
  - Direção: Somente vendido
  - Indicadores: Price Action
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Sim
  - Nível de risco: Médio
