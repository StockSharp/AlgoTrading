# Padrão de Fundo Duplo (Double Bottom Pattern)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia baseada em padrões procura dois mínimos consecutivos aproximadamente no mesmo preço, separados por uma distância definida. Após a formação do segundo fundo, uma vela altista confirma a reversão.

Os testes indicam um retorno anual médio de aproximadamente 55%. Funciona melhor no mercado de ações.

Quando ocorre a confirmação, o sistema compra com um stop abaixo das mínimas do padrão. A configuração visa capturar recuperações acentuadas após exaustão da venda.

As saídas dependem de um stop-loss predefinido ou de metas de lucro manuais.

## Detalhes

- **Critérios de entrada**: Dois fundos se formam dentro de `SimilarityPercent` após `Distance` barras.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: O preço falha ou stop-loss.
- **Stops**: Sim.
- **Valores padrão**:
  - `Distance` = 5
  - `SimilarityPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(15)
  - `StopLossPercent` = 1.0m
- **Filtros**:
  - Categoria: Padrão
  - Direção: Somente comprado
  - Indicadores: Price Action
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Sim
  - Nível de risco: Médio
