# Estratégia de Força no Fim do Mês
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
A Força no Fim do Mês observa que as ações frequentemente sobem durante os últimos dias de negociação à medida que os gestores de carteira ajustam suas posições.
A pressão compradora ligada ao window dressing pode criar um viés de alta confiável antes do fechamento mensal.

Os testes indicam um retorno anual médio de aproximadamente 94%. Funciona melhor no mercado de ações.

A estratégia compra perto dos últimos dias do mês e sai no primeiro dia de negociação do novo mês para capturar essa tendência.

Os stops são colocados abaixo do suporte recente para proteger contra fraqueza inesperada.

## Detalhes

- **Critérios de entrada**: gatilhos de efeito calendário
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: stop-loss ou sinal oposto
- **Stops**: Sim, baseado em percentual
- **Valores padrão**:
  - `CandleType` = 15 minute
  - `DaysBeforeMonthEnd` = 3
  - `StopLossPercent` = 2
    Os dias são dias UTC, pois o mercado negocia 24 horas; as entradas ocorrem no fechamento da primeira vela do dia. A compra abre em cada um dos últimos DaysBeforeMonthEnd dias do mês quando não há posição e fecha na primeira vela do novo mês.
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

