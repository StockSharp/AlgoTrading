# Estratégia de Rompimento do Intervalo de Abertura
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia de Rompimento do Intervalo de Abertura rastreia os preços mais altos e mais baixos durante os primeiros minutos de uma sessão de trading. Após o intervalo terminar, ordens de rompimento são colocadas além do intervalo com um buffer configurável. Os alvos são derivados de uma proporção recompensa-risco enquanto os stops são definidos no lado oposto do intervalo.

## Detalhes

- **Critérios de entrada**:
  - Após o intervalo de abertura, ir comprado quando o preço fecha acima da máxima mais o buffer.
  - Ir vendido quando o preço fecha abaixo da mínima menos o buffer.
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Stop e alvo baseados no intervalo e na proporção recompensa-risco.
- **Stops**: Sim
- **Valores padrão**:
  - `RangeMinutes` = 15
  - `RewardRisk` = 2.0
  - `EntryBuffer` = 0.0001
  - `SessionStart` = 08:00
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Sim
  - Complexidade: Baixo
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
