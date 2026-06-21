# Estratégia de Rompimento do Intervalo de Abertura
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia define um intervalo de abertura e opera os rompimentos acima ou abaixo dele. Após o fechamento da janela do intervalo de abertura, se a amplitude superar um percentual do preço de fechamento, ordens stop são preparadas nos limites do intervalo. As posições utilizam stop loss e alvo de lucro com base no tamanho do intervalo. Opcionalmente, apenas uma operação por dia é realizada, e operações perdedoras podem ser revertidas. Todas as posições são fechadas ao final da sessão.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: o preço rompe acima da máxima do intervalo de abertura.
  - **Vendido**: o preço rompe abaixo da mínima do intervalo de abertura.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Stop loss ou alvo de lucro com base no intervalo.
  - Fechamento no fim do dia.
- **Stops**: Sim.
- **Valores padrão**:
  - `Intervalo de abertura` = 09:30–10:15.
  - `Fim do dia` = 15:45.
  - `MinRangePercent` = 0.35.
  - `RewardRisk` = 1.1.
  - `Retrace` = 0.5.
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Preço
  - Stops: Sim
  - Complexidade: Moderado
  - Período: Intradiário
