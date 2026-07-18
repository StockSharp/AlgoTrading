# Estratégia Rsi Williams R
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Implementação da estratégia - RSI + Williams %R. Compra quando o RSI está abaixo de 30 e o Williams %R está abaixo de -80 (condição de dupla sobrevenda). Vende quando o RSI está acima de 70 e o Williams %R está acima de -20 (condição de dupla sobrecompra).

Os testes indicam um retorno anual médio de aproximadamente 76%. Funciona melhor no mercado forex.

O RSI descreve o momentum geral, enquanto o Williams %R fornece um sinal mais rápido de reversão. As operações são ativadas quando os dois osciladores concordam.

Bom para traders ativos que buscam oscilações curtas. Stops baseados em ATR são empregados.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `RSI < RsiOversold && WilliamsR < WilliamsROversold`
  - Vendido: `RSI > RsiOverbought && WilliamsR > WilliamsROverbought`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - RSI retorna à zona neutra
- **Stops**: Baseados em porcentagem usando `StopLoss`
- **Valores padrão**:
  - `RsiPeriod` = 14
  - `RsiOversold` = 30m
  - `RsiOverbought` = 70m
  - `WilliamsRPeriod` = 14
  - `WilliamsROversold` = -80m
  - `WilliamsROverbought` = -20m
  - `StopLoss` = new Unit(2, UnitTypes.Percent)
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: RSI, Williams %R, R
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

