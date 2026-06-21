# Estratégia de Ajuste de Curva com Múltiplas Condições
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Combina cruzamento de EMA, RSI e oscilador estocástico para operar quando múltiplos sinais se alinham.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `FastEMA > SlowEMA` e `RSI < RsiOversold` e `StochK < 20`
  - Vendido: `FastEMA < SlowEMA` e `RSI > RsiOverbought` e `StochK > 80`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Comprado: `FastEMA < SlowEMA` ou `RSI > RsiOverbought` ou `StochK > StochD`
  - Vendido: `FastEMA > SlowEMA` ou `RSI < RsiOversold` ou `StochK < StochD`
- **Stops**: Nenhum
- **Valores padrão**:
  - `FastEmaLength` = 10
  - `SlowEmaLength` = 25
  - `RsiLength` = 14
  - `RsiOverbought` = 80
  - `RsiOversold` = 20
  - `StochLength` = 14
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: EMA, RSI, Stochastic
  - Stops: Não
  - Complexidade: Básico
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
