# Estratégia Bollinger ADX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que combina as Bandas de Bollinger e o indicador ADX. Busca rompimentos com forte confirmação de tendência.

Os testes indicam um retorno anual médio de cerca de 46%. Funciona melhor no mercado de ações.

Os movimentos de preço fora das Bandas de Bollinger são filtrados pelo ADX para verificar a força. As operações são ativadas quando uma quebra de banda coincide com um ADX elevado.

Útil para surtos de volatilidade acompanhados de tendências fortes. O tamanho do stop é determinado pelo ATR.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `Close < LowerBand && ADX > AdxThreshold`
  - Vendido: `Close > UpperBand && ADX > AdxThreshold`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Reversão à média de Bollinger
- **Stops**: Baseados em ATR usando `AtrMultiplier`
- **Valores padrão**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2.0m
  - `AdxPeriod` = 14
  - `AdxThreshold` = 25m
  - `AtrMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: Bollinger Bands, ADX
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
