# Estratégia de Scalp com Momentum Chaikin
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia de scalp utiliza o oscilador Chaikin para capturar mudanças de momentum. Operações compradas ocorrem quando o oscilador cruza acima de zero e o preço está acima da SMA de 200 períodos. Operações vendidas ocorrem com um cruzamento abaixo de zero e preço abaixo da SMA. Múltiplos do ATR definem os níveis de stop-loss e take-profit.

## Detalhes

- **Critérios de entrada**: O oscilador Chaikin cruza acima/abaixo de zero com o preço acima/abaixo da SMA.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Stop-loss e take-profit baseados em ATR.
- **Stops**: Sim.
- **Valores padrão**:
  - `FastLength` = 3
  - `SlowLength` = 10
  - `SmaLength` = 200
  - `AtrLength` = 14
  - `AtrMultiplierSL` = 1.5m
  - `AtrMultiplierTP` = 2.0m
  - `CandleType` = TimeSpan.FromHours(1)
- **Filtros**:
  - Categoria: Momentum
  - Direção: Ambos
  - Indicadores: Chaikin Oscillator, SMA, ATR
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
