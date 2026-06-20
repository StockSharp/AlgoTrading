# Bollinger Volume Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Estratégia que utiliza rompimentos das Bandas de Bollinger com confirmação de volume.
Entra em posições quando o preço rompe acima/abaixo das Bandas de Bollinger com volume aumentado.

Os testes indicam um retorno anual médio de aproximadamente 178%. Funciona melhor no mercado de ações.

As bandas de Bollinger mostram expansão de volatilidade e o volume confirma o rompimento. As posições são tomadas quando o preço fecha fora de uma banda com forte atividade.

Adequado para operadores de rompimento que esperam continuação. Um stop baseado em ATR mantém as perdas gerenciáveis.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `Close > UpperBand && Volume > AvgVolume * VolumeMultiplier`
  - Vendido: `Close < LowerBand && Volume > AvgVolume * VolumeMultiplier`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - O preço retorna à banda do meio
- **Stops**: Baseado em ATR usando `StopLossAtr`
- **Valores padrão**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2.0m
  - `VolumePeriod` = 20
  - `VolumeMultiplier` = 1.5m
  - `StopLossAtr` = 2.0m
  - `AtrPeriod` = 14
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: Bollinger Bands, Volume
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

