# Estratégia DMI Winner
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

DMI Winner é uma estratégia seguidora de tendência baseada no Índice de Movimento
Direcional (DMI). Ela abre operações quando as linhas `+DI` e `-DI` se cruzam e o
Índice Direcional Médio (ADX) sobe acima de um nível-chave, sinalizando uma tendência
forte.

Um filtro de média móvel opcional mantém as operações na direção da tendência mais
ampla. Um stop-loss também pode ser habilitado para limitar o risco de queda, embora
por padrão o sistema se baseie em reversões de sinal para as saídas.

## Detalhes
- **Dados**: Velas de preço.
- **Critérios de entrada**:
  - **Comprado**: `+DI` cruza acima de `-DI` E `ADX` > `KeyLevel` (com filtro de MA opcional).
  - **Vendido**: `-DI` cruza acima de `+DI` E `ADX` > `KeyLevel` (com filtro de MA opcional).
- **Critérios de saída**: Cruzamento de DI oposto ou stop-loss se habilitado.
- **Stops**: Stop-loss opcional (`UseSL`).
- **Valores padrão**:
  - `DILength` = 14
  - `KeyLevel` = 23
  - `UseMA` = True
  - `UseSL` = False
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Comprado/Vendido
  - Indicadores: DMI, Moving Average
  - Complexidade: Moderado
  - Nível de risco: Médio
