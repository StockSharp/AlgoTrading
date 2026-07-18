# Estratégia MACD Volume XAUUSD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de 15 minutos para XAUUSD que combina o cruzamento da linha zero do MACD com um filtro de oscilador de volume e parâmetros de risco fixos.

## Detalhes

- **Critérios de entrada**: MACD cruzando a linha zero com oscilador de volume positivo e comparação de volume.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Níveis de stop-loss ou take-profit.
- **Stops**: Stop-loss fixo e multiplicador de take-profit.
- **Valores padrão**:
  - `ShortLength` = 5
  - `LongLength` = 8
  - `FastLength` = 16
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `Leverage` = 1.0
  - `StopLoss` = 10100
  - `TakeProfitMultiplier` = 1.1
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: MACD, EMA, Volume
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário (15m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
