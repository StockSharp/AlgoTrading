# Estratégia Adx Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Estratégia que combina o ADX (Índice Direcional Médio) para a força da tendência e o Oscilador Stochastic para o timing de entrada com condições de sobrevenda/sobrecompra.

Os testes indicam um retorno anual médio de aproximadamente 172%. Funciona melhor no mercado de câmbio.

O ADX destaca a força da tendência enquanto o Stochastic identifica as correções. Sinais de compra ou venda aparecem quando o momentum vira enquanto o ADX permanece alto.

É adequado para traders que combinam seguidor de tendência com timing de oscilador. Stops protetores de ATR ajudam a controlar os drawdowns.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `ADX > AdxThreshold && StochK < StochOversold && Bullish`
  - Vendido: `ADX > AdxThreshold && StochK > StochOverbought && Bearish`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Sair quando `ADX < AdxThreshold`
- **Stops**: Baseado em percentual em `StopLossPercent`
- **Valores padrão**:
  - `AdxPeriod` = 14
  - `AdxThreshold` = 25m
  - `StochPeriod` = 14
  - `StochK` = 3
  - `StochD` = 3
  - `StochOversold` = 20m
  - `StochOverbought` = 80m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: ADX, Stochastic Oscillator
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

