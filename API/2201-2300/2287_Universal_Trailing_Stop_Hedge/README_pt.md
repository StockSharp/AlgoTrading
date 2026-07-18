# Estratégia Universal de Trailing Stop com Hedge
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que demonstra diferentes técnicas de trailing stop para proteger posições abertas.
Oferece trailing stops baseados em ATR, Parabolic SAR, média móvel, porcentagem e pips fixos.
Uma entrada simples baseada na direção da vela é usada puramente para fins educacionais.

## Detalhes

- **Critérios de entrada**: Comprado se a vela fecha acima da abertura, vendido se fecha abaixo
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Trailing stop ativado
- **Stops**: ATR, Parabolic SAR, Média Móvel, Porcentagem de lucro ou pips fixos dependendo do modo selecionado
- **Valores padrão**:
  - `Mode` = `TrailingModes.Atr`
  - `Delta` = 10
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 1m
  - `SarStep` = 0.02m
  - `SarMax` = 0.2m
  - `MaPeriod` = 34
  - `PercentProfit` = 50m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Gestão de risco
  - Direção: Ambos
  - Indicadores: ATR, Parabolic SAR, SMA
  - Stops: Trailing stop
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
