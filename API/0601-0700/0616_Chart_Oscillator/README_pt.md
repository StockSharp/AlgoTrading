# Oscilador de Gráfico
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera usando um oscilador selecionável. Escolha entre Estocástico, RSI ou MFI. Compra quando o oscilador sinaliza condições de sobrevenda e vende quando está sobrecomprado. Para a opção Estocástico, os sinais usam cruzamentos de %K e %D.

Os testes mostram bom desempenho em mercados voláteis como criptomoedas.

As posições se invertem quando surgem condições opostas ou o stop-loss é acionado.

## Detalhes

- **Critérios de entrada**: Níveis de sobrevenda/sobrecompra do oscilador e cruzamentos de %K/%D.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Sinal oposto ou stop.
- **Stops**: Sim.
- **Valores padrão**:
  - `Choice` = OscillatorChoice.Stochastic
  - `Length` = 14
  - `KPeriod` = 14
  - `DPeriod` = 3
  - `SmoothK` = 3
  - `Overbought` = 80
  - `Oversold` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `StopLossPercent` = 2.0m
- **Filtros**:
  - Categoria: Oscilador
  - Direção: Ambos
  - Indicadores: Stochastic/RSI/MFI
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
