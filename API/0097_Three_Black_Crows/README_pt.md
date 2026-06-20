# Three Black Crows Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Três Corvos Negros é o equivalente de baixa dos Três Soldados Brancos, composto por três velas longas de baixa após uma subida. O padrão sugere que os vendedores assumiram o controle, pois cada fechamento cai perto da mínima da sessão.

Os testes indicam um retorno anual médio de aproximadamente 178%. Funciona melhor no mercado de ações.

Esta estratégia inicia uma posição vendida assim que o terceiro corvo aparece, esperando que o momentum continue para baixo. Também pode ser usada para encerrar comprados abertos por outros sistemas se o padrão se formar em resistência.

O risco é gerenciado com um stop percentual ajustado acima da máxima do padrão, e as operações encerram se o preço fechar novamente acima desse nível.

## Detalhes

- **Critérios de entrada**: correspondência de padrão
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: stop-loss ou sinal oposto
- **Stops**: Sim, baseado em percentual
- **Valores padrão**:
  - `CandleType` = 15 minutos
  - `StopLoss` = 2%
- **Filtros**:
  - Categoria: Padrão
  - Direção: Ambos
  - Indicadores: Candlestick
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
