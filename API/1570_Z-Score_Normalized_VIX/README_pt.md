# Estratégia Z-Score Normalized VIX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que calcula a média dos z-scores de vários índices VIX e entra comprado quando o valor combinado cai abaixo de um limiar negativo.

O algoritmo calcula o z-score para VIX, VIX3M, VIX9D e VVIX. Os z-scores selecionados são calculados em média para formar um único indicador que representa o sentimento geral de volatilidade.

## Detalhes

- **Critérios de entrada**: Z-score combinado abaixo de `-Threshold`.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: Z-score combinado sobe acima de `-Threshold`.
- **Stops**: Não.
- **Valores padrão**:
  - `ZScoreLength` = 6
  - `Threshold` = 1
  - `UseVix` = true
  - `UseVix3m` = true
  - `UseVix9d` = true
  - `UseVvix` = true
  - `CandleType` = TimeSpan.FromDays(1)
- **Filtros**:
  - Categoria: Volatilidade
  - Direção: Comprado
  - Indicadores: Z-Score
  - Stops: Não
  - Complexidade: Básico
  - Período: Diário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
