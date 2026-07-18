# Estratégia Bollinger Winner Pro
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Bollinger Winner Pro expande a versão Lite adicionando filtros modulares e controles
de risco. Ainda busca preços fechando fora das Bollinger Bands, mas as operações
são executadas apenas quando as confirmações opcionais concordam.

Os traders podem habilitar filtros de RSI, Aroon e média móvel para confirmar o
impulso e a direção da tendência. Um stop-loss integrado também pode ser ativado
para limitar o risco. Essa flexibilidade permite que a estratégia se adapte a
diferentes mercados ou necessidades de teste.

A abordagem visa a reversão à média: uma vez que o preço reentra nas bandas ou
toca o lado oposto, a posição é fechada ou o stop é acionado. Como múltiplos
filtros podem ser empilhados, os sinais são menos frequentes, mas de maior qualidade.

## Detalhes
- **Dados**: Velas de preço.
- **Critérios de entrada**: Vela fecha fora de uma banda e todos os filtros habilitados concordam.
- **Critérios de saída**: Retorno à banda central/oposta ou stop-loss se `UseSL` for verdadeiro.
- **Stops**: Stop-loss opcional controlado por `UseSL`.
- **Valores padrão**:
  - `UseRSI` = True
  - `UseAroon` = False
  - `UseMA` = True
  - `UseSL` = True
- **Filtros**:
  - Categoria: Reversão à média com confirmações
  - Direção: Comprado/Vendido
  - Indicadores: Bollinger Bands, RSI, Aroon, Moving Average
  - Complexidade: Avançado
  - Nível de risco: Médio
