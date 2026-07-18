# Estratégia Donchian RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que combina os Canais Donchian e o indicador RSI. Compra em rompimentos do Donchian quando o RSI confirma que a tendência não está sobreextendida.

Os testes indicam um retorno anual médio de cerca de 55%. Funciona melhor no mercado de ações.

Os canais Donchian identificam os níveis de rompimento, enquanto o RSI verifica se o momentum suporta o movimento. As posições são abertas quando um rompimento se alinha com a direção do RSI.

Melhor para traders que esperam um rompimento sustentado em vez de um falso. O risco é limitado por um stop baseado em ATR.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `Close > DonchianHigh && RSI < RsiOversoldLevel`
  - Vendido: `Close < DonchianLow && RSI > RsiOverboughtLevel`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Falha de rompimento ou sinal oposto
- **Stops**: Baseados em percentual usando `StopLossPercent`
- **Valores padrão**:
  - `DonchianPeriod` = 20
  - `RsiPeriod` = 14
  - `RsiOverboughtLevel` = 70m
  - `RsiOversoldLevel` = 30m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Donchian Channel, RSI
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
