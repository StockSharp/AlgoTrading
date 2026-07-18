# Estratégia do Efeito Mês do Ano
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
O Efeito Mês do Ano captura diferenças de desempenho observadas em vários meses.
Por exemplo, as ações frequentemente sobem em novembro e dezembro, mas podem ser fracas durante setembro.

Os testes indicam um retorno anual médio de aproximadamente 88%. Funciona melhor no mercado de ações.

O sistema vai comprado ou vendido no início de cada mês com base nesses médias históricas, saindo ao final do mês.

Stops são usados para proteger o capital caso o comportamento sazonal habitual não apareça.

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

