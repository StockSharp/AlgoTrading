# Estratégia Fractal RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia adaptativa baseada no indicador Fractal RSI.
O Fractal RSI ajusta o comprimento do cálculo do RSI usando a dimensão fractal do movimento do preço,
permitindo que o oscilador reaja mais rápido em mercados de tendência e mais lento em condições laterais.

A estratégia abre posições quando o indicador cruza níveis predefinidos.
Pode operar com a tendência detectada ou contra ela, dependendo do modo escolhido.

## Detalhes

- **Critérios de entrada**:
  - *Modo Tendência - Direto*:
    - Compra: o valor cruza abaixo de `LowLevel`
    - Venda: o valor cruza acima de `HighLevel`
  - *Modo Tendência - Contra*:
    - Compra: o valor cruza acima de `HighLevel`
    - Venda: o valor cruza abaixo de `LowLevel`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Sinal oposto
- **Stops**: Stop-loss e take-profit fixos opcionais
- **Valores padrão**:
  - `CandleType` = `TimeSpan.FromHours(4).TimeFrame()`
  - `FractalPeriod` = 30
  - `NormalSpeed` = 30
  - `HighLevel` = 60
  - `LowLevel` = 40
  - `StopLoss` = 1000 pontos
  - `TakeProfit` = 2000 pontos
- **Filtros**:
  - Categoria: Tendência / Oscilador
  - Direção: Ambos
  - Indicadores: Fractal Dimension, RSI
  - Stops: Sim
  - Complexidade: Uso avançado de indicadores
  - Período: 4H (configurável)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
