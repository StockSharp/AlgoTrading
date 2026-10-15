# Estratégia Clássica de Rompimento de London
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera rompimentos da sessão de London usando o intervalo asiático. A máxima e a mínima entre 00:00 e 06:55 UTC formam uma caixa. Após 07:00 UTC, um rompimento acima da máxima abre uma posição comprada e um rompimento abaixo da mínima abre uma posição vendida. O stop loss é colocado no ponto médio da caixa e o take profit usa um fator configurável de risco-recompensa.

## Detalhes

- **Critérios de entrada**:
  - Comprado: o preço cruza acima da máxima da sessão asiática.
  - Vendido: o preço cruza abaixo da mínima da sessão asiática.
- **Critérios de saída**:
  - Stop loss ou take profit.
  - Fim da janela de negociação.
- **Stops**: Sim.
- **Valores padrão**:
  - Sessão asiática: 00:00–06:55 UTC.
  - Sessão de negociação: 07:00–16:00 UTC.
  - CRV = 1.
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Sim
  - Complexidade: Moderado
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
