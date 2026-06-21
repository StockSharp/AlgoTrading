# PZ Parabolic SAR EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia replica o consultor especialista *PZ Parabolic SAR*. Emprega dois indicadores Parabolic SAR com diferentes configurações de passo e aceleração máxima. O SAR de "negociação" detecta a direção da tendência para as entradas, enquanto o SAR de "stop" segue o preço mais de perto e aciona as saídas quando a tendência se reverte.

O controle de risco é tratado por meio do Average True Range (ATR). Um stop inicial baseado em ATR é definido quando uma posição é aberta. Opcionalmente, um trailing stop baseado em ATR pode ajustar o stop à medida que o preço se move a favor da operação. A estratégia também suporta fechamento parcial: uma vez que o lucro supera a distância do stop inicial, metade da posição é fechada e o stop é movido para break-even.

A estratégia funciona nas direções comprada e vendida e opera apenas em velas finalizadas. Usa ordens a mercado sem colocar ordens de stop reais.

## Detalhes

- **Critérios de entrada**: Preço acima/abaixo do SAR de negociação e do SAR de stop na mesma direção.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: SAR de stop cruzando o preço ou trailing stop ATR atingido.
- **Stops**: Stop baseado em ATR com trailing e break-even opcionais.
- **Valores padrão**:
  - `TradeStep` = 0.002
  - `TradeMax` = 0.2
  - `StopStep` = 0.004
  - `StopMax` = 0.4
  - `AtrPeriod` = 30
  - `AtrMultiplier` = 2.5
  - `UseTrailing` = false
  - `TrailingAtrPeriod` = 30
  - `TrailingAtrMultiplier` = 1.75
  - `PartialClosing` = true
  - `PercentageToClose` = 0.5
  - `BreakEven` = true
  - `LotSize` = 0.1
  - `CandleType` = TimeFrame(5m)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Parabolic SAR, ATR
  - Stops: ATR, Trailing
  - Complexidade: Intermediário
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
