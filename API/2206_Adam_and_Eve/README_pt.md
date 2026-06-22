# Estratégia Adam and Eve
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de seguimento de tendência que combina velas Heiken Ashi com uma cascata de médias móveis simples. Uma posição vendida é aberta quando aparece uma vela Heiken Ashi baixista sem sombra superior e todas as médias móveis monitorizadas (5, 7, 9, 10, 12, 14, 20) apontam para baixo. Uma posição comprada é desencadeada por uma vela alta sem sombra inferior e todas as médias apontando para cima. Cada operação tem como objetivo um lucro a uma distância de um ATR(14) desde a entrada sem stop-loss.

## Detalhes

- **Critérios de entrada**: vela Heiken Ashi anterior sem sombra superior (vendido) ou inferior (comprado) e pilha de SMA alinhada
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: objetivo de lucro à distância ATR(14)
- **Stops**: Nenhum
- **Valores padrão**:
  - `AtrPeriod` = 14
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: SMA (5,7,9,10,12,14,20), Heiken Ashi, ATR
  - Stops: Apenas objetivo
  - Complexidade: Intermediário
  - Período: Configurável, padrão 15 minutos
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Moderado
