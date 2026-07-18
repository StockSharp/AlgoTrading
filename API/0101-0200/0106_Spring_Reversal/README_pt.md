# Estratégia de Reversão Spring
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
A Reversão Spring é um conceito de Wyckoff onde o preço rompe brevemente o suporte e então salta de volta acima dele.
Esse abalo prende os vendedores tardios e frequentemente marca o início de uma tendência de alta.

Os testes indicam um retorno anual médio de aproximadamente 55%. Funciona melhor no mercado de ações.

A estratégia compra assim que o preço recupera o nível rompido, antecipando rápido fechamento de posições vendidas e nova demanda.

Um stop logo abaixo da mínima do spring limita o risco, e a posição é fechada se o seguimento falhar.

## Detalhes

- **Critérios de entrada**: sinal de indicador
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: stop-loss ou sinal oposto
- **Stops**: Sim, baseado em percentual
- **Valores padrão**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filtros**:
  - Categoria: Reversão
  - Direção: Ambos
  - Indicadores: Wyckoff
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

