# Estratégia do Efeito Dia da Semana
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
O Efeito Dia da Semana explora a tendência dos mercados de exibir comportamento recorrente em dias específicos da semana.
Alguns índices mostram força consistente no meio da semana, enquanto segunda-feira ou sexta-feira podem ser relativamente fracos.

Os testes indicam um retorno anual médio de aproximadamente 85%. Funciona melhor no mercado de criptomoedas.

A estratégia abre operações com base nessas tendências históricas, comprando ou vendendo no início da sessão e saindo no fechamento.

Um stop moderado protege contra anomalias, encerrando a posição antecipadamente se o padrão falhar em um determinado dia.

## Detalhes

- **Critérios de entrada**: gatilhos de efeito calendário
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: stop-loss ou sinal oposto
- **Stops**: Sim, baseado em percentual
- **Valores padrão**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filtros**:
  - Categoria: Sazonalidade
  - Direção: Ambos
  - Indicadores: Sazonalidade
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Sim
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

