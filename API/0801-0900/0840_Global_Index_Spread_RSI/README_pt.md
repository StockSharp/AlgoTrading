# Estratégia Global Index Spread RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Global Index Spread RSI negocia o E-mini S&P 500 quando seu diferencial em relação a um índice de ações global torna-se sobrevendido. O diferencial é medido em termos percentuais e processado por um RSI de período curto. Uma posição comprada é aberta quando o RSI cai abaixo do limiar de sobrevenda e fechada quando sobe acima do limiar de sobrecompra.

## Detalhes
- **Dados**: Fechamentos diários de ES e do índice global.
- **Critérios de entrada**:
  - **Comprado**: RSI do diferencial abaixo de `OversoldThreshold`.
- **Critérios de saída**: RSI do diferencial acima de `OverboughtThreshold`.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `RsiLength` = 2
  - `OversoldThreshold` = 35
  - `OverboughtThreshold` = 78
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Comprado
  - Indicadores: RSI
  - Complexidade: Baixo
  - Nível de risco: Médio
