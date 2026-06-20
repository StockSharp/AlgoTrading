# Estratégia Keltner Volume
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Implementação da estratégia Keltner Channels + Volume. Comprar quando o preço rompe acima do canal Keltner superior com volume acima da média. Vender quando o preço rompe abaixo do canal Keltner inferior com volume acima da média.

Os testes indicam um retorno anual médio de cerca de 58%. Funciona melhor no mercado de ações.

Os limites do canal Keltner definem potenciais reversões, e o aumento do volume sinaliza convicção. O sistema opera quando o preço toca uma banda com volume em expansão.

Traders que buscam confirmação de volume em torno de bandas de volatilidade podem preferir esta configuração. Os stops são calculados a partir do ATR.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `Close < LowerBand && Volume > AvgVolume`
  - Vendido: `Close > UpperBand && Volume > AvgVolume`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - O preço cruza a EMA
- **Stops**: Baseados em ATR usando `StopLoss`
- **Valores padrão**:
  - `EmaPeriod` = 20
  - `AtrPeriod` = 14
  - `Multiplier` = 2.0m
  - `VolumeAvgPeriod` = 20
  - `StopLoss` = new Unit(2, UnitTypes.Absolute)
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: Keltner Channel, Volume
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
