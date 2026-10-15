# Modelo Dinâmico de Oscilador de Ticks (DTOM)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O **Dynamic Ticks Oscillator Model** utiliza a taxa de variação do índice NYSE Down Ticks. Quando o ROC cai abaixo de um limiar dinâmico baseado no desvio padrão, a estratégia abre uma posição comprada. A posição é fechada assim que o ROC sobe acima de um limiar positivo.

## Detalhes
- **Critérios de entrada**: `ROC < -StdDev * EntryStdDevMultiplier`
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: `ROC > StdDev * ExitStdDevMultiplier`
- **Stops**: Não.
- **Valores padrão**:
  - `RocLength = 5`
  - `VolatilityLookback = 24`
  - `EntryStdDevMultiplier = 1.6m`
  - `ExitStdDevMultiplier = 1.4m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Somente comprado
  - Indicadores: RateOfChange, StandardDeviation
  - Stops: Não
  - Complexidade: Iniciante
  - Período: Diário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
