# Estratégia de Scalping do Ouro BOS & CHoCH
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera padrões de quebra de estrutura (BOS) e mudança de caráter (CHoCH) no ouro. Deriva níveis de suporte e resistência de curto prazo e entra quando um BOS é seguido imediatamente por um CHoCH, utilizando alvos dinâmicos de stop loss e take profit.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: `High > LastSwingHigh` e `Close` cruza acima de `LastSwingLow`
  - **Vendido**: `Low < LastSwingLow` e `Close` cruza abaixo de `LastSwingHigh`
- **Comprado/Vendido**: Ambos os lados
- **Critérios de saída**: Stop loss ou take profit
- **Stops**: Dinâmicos
- **Valores padrão**:
  - `RecentLength` = 10
  - `SwingLength` = 5
  - `TakeProfitFactor` = 2
- **Filtros**:
  - Categoria: Scalping
  - Direção: Ambos
  - Indicadores: Highest, Lowest
  - Stops: Sim
  - Complexidade: Médio
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
