# Estratégia de Rompimento Doji de Pernas Longas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia de Rompimento Doji de Pernas Longas identifica velas Doji de pernas longas e opera rompimentos acima ou abaixo do intervalo do Doji. Um filtro ATR opcional garante que as sombras sejam suficientemente longas.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: Aguardando rompimento && close > máxima do Doji && fechamento anterior <= máxima do Doji.
  - **Vendido**: Aguardando rompimento && close < mínima do Doji && fechamento anterior >= mínima do Doji.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: O fechamento cruza a SMA(20) em direção contrária à posição.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `Doji body threshold %` = 0.1
  - `Minimum wick ratio` = 2
  - `Use ATR filter` = true
  - `ATR period` = 14
  - `ATR multiplier` = 0.5
- **Filtros**:
  - Categoria: Rompimento de padrão
  - Direção: Ambos
  - Indicadores: ATR, SMA
  - Stops: Não
  - Complexidade: Baixo
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
