# Arbitragem Estatística de Pares - Somente Lado Comprado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia executa uma abordagem simples de trading de pares baseada no z-score do spread entre dois instrumentos. Abre uma posição comprada quando o spread cai abaixo de um limiar definido pelo usuário e fecha a posição quando o spread cruza acima de zero.

## Detalhes

- **Critérios de entrada**: Z-score do spread abaixo do limiar.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: Z-score do spread cruza acima de zero.
- **Stops**: Não.
- **Valores padrão**:
  - `ZScoreLength` = 20
  - `ExtremeLevel` = -1
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Mean Reversion
  - Direção: Somente comprado
  - Indicadores: SMA, StandardDeviation
  - Stops: Não
  - Complexidade: Iniciante
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
