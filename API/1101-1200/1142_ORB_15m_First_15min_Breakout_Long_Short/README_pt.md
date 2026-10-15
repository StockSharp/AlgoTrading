# ORB 15m – Rompimento dos Primeiros 15 Minutos (Comprado/Vendido)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia entra no fechamento da primeira barra de 15 minutos após a abertura da sessão no horário de Estocolmo. Uma primeira barra de alta aciona uma operação comprada; uma barra de baixa aciona uma operação vendida. O tamanho da posição é calculado a partir do percentual de risco e da distância até o stop.

## Detalhes

- **Critérios de entrada**: operar na primeira barra de 15 minutos após a abertura da sessão; comprado se a barra fechar acima de sua abertura, vendido se fechar abaixo.
- **Critérios de saída**: stop-loss na extremidade oposta da barra de referência; take profit opcional em `RMultiple` vezes o risco ou no final da sessão.
- **Comprado/Vendido**: Ambos.
- **Stops**: Sim.
- **Valores padrão**:
  - `RiskPct = 1`
  - `TpTenR = true`
  - `RMultiple = 10`
  - `SessionOpenHour = 15`
  - `SessionOpenMinute = 30`
  - `SessionEndHour = 22`
  - `SessionEndMinute = 0`
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Sim
  - Complexidade: Iniciante
  - Período: Intradiário
