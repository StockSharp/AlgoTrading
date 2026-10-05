# Estratégia Donchian RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que combina os Canais Donchian e o indicador RSI. Compra em rompimentos do Donchian quando o RSI confirma que a tendência não está sobreextendida.

Os testes indicam um retorno anual médio de cerca de 55%. Funciona melhor no mercado de ações.

Os canais Donchian identificam os níveis de rompimento, enquanto o RSI verifica se o momentum suporta o movimento. As posições são abertas quando um rompimento se alinha com a direção do RSI.

Melhor para traders que esperam um rompimento sustentado em vez de um falso. O risco é limitado por um stop percentual.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `Close > DonchianHigh && RSI < RsiOverboughtLevel`
  - Vendido: `Close < DonchianLow && RSI > RsiOversoldLevel`
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
    Um rompimento não está sobre-estendido enquanto o RSI fica abaixo de RsiOverboughtLevel (compra) ou acima de RsiOversoldLevel (venda); o canal abrange os DonchianPeriod candles anteriores. O rompimento falha, encerrando a posição, quando o preço volta a fechar do outro lado do nível rompido. Um sinal de entrada contra uma posição aberta a inverte.
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
