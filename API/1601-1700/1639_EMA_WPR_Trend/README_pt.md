# Estratégia de Tendência EMA WPR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que combina um filtro de tendência EMA com sinais do Williams %R. Compra em níveis de sobrevenda e vende em níveis de sobrecompra. Um limiar de retração previne entradas consecutivas. Saídas opcionais fecham operações em extremos opostos do Williams %R ou após várias barras não lucrativas.

## Detalhes

- **Critérios de entrada**:
  - Comprado: Williams %R <= -100 e tendência EMA em alta
  - Vendido: Williams %R >= 0 e tendência EMA em baixa
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Williams %R cruza o extremo oposto quando `UseWprExit` está habilitado
  - A posição permanece não lucrativa por `MaxUnprofitBars` barras quando `UseUnprofitExit` está habilitado
- **Stops**: Não
- **Valores padrão**:
  - `WprPeriod` = 46
  - `WprRetracement` = 30
  - `EmaPeriod` = 144
  - `BarsInTrend` = 1
  - `MaxUnprofitBars` = 5
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: EMA, Williams %R
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
