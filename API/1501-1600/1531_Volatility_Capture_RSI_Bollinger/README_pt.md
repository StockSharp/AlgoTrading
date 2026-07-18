# Captura de Volatilidade RSI-Bollinger
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia combina bandas de Bollinger dinâmicas com um filtro RSI opcional para capturar oscilações de volatilidade.

## Detalhes
- **Critérios de entrada**: Preço cruzando a banda de Bollinger adaptativa com confirmação RSI opcional.
- **Comprado/Vendido**: Configurável via `Direction`.
- **Critérios de saída**: Preço cruzando o lado oposto da banda trailing.
- **Stops**: Não.
- **Valores padrão**:
  - `BollingerLength` = 50
  - `Multiplier` = 2.7183m
  - `UseRsi` = true
  - `RsiPeriod` = 10
  - `RsiSmaPeriod` = 5
  - `BoughtRangeLevel` = 55m
  - `SoldRangeLevel` = 50m
  - `Direction` = TradeDirection.Both
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Volatilidade
  - Direção: Configurável
  - Indicadores: Bollinger, RSI
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
