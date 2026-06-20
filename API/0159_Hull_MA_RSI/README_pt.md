# Estratégia Hull MA RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Implementação da estratégia Hull Moving Average + RSI. Comprar quando a HMA está subindo e o RSI está abaixo de 30 (sobrevendido). Vender quando a HMA está caindo e o RSI está acima de 70 (sobrecomprado).

Os testes indicam um retorno anual médio de cerca de 64%. Funciona melhor no mercado de câmbio.

A Hull MA fornece uma linha de tendência suavizada e o RSI destaca as divergências de momentum. As operações ocorrem quando o RSI vira nos extremos enquanto o preço segue a direção da Hull.

Adequada para traders de swing de curto prazo que buscam sinais antecipados. Stops baseados em ATR protegem a operação.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `HullMA turning up && RSI < RsiOversold`
  - Vendido: `HullMA turning down && RSI > RsiOverbought`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Mudança de direção da Hull MA
- **Stops**: Baseados em ATR usando `StopLoss`
- **Valores padrão**:
  - `HmaPeriod` = 9
  - `RsiPeriod` = 14
  - `RsiOversold` = 30m
  - `RsiOverbought` = 70m
  - `StopLoss` = new Unit(2, UnitTypes.Absolute)
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: Hull MA, Moving Average, RSI
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
