# Estratégia de Rompimento de Máximos e Mínimos com Stop Trailing ATR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera rompimentos do intervalo dos primeiros 30 minutos da sessão. Assim que o preço cruza o máximo ou mínimo inicial, uma posição é aberta com um stop trailing baseado em ATR. Todas as posições são fechadas em um horário intradiário especificado.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: O fechamento cruza acima do máximo dos primeiros 30 minutos
  - **Vendido**: O fechamento cruza abaixo do mínimo dos primeiros 30 minutos
- **Comprado/Vendido**: Configurável (`Direction`).
- **Critérios de saída**:
  - Stop trailing ATR ou alvo simétrico
  - Fechar todas as posições em `ExitHour:ExitMinute`
- **Stops**: Sim, baseado em ATR.
- **Valores padrão**:
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 3.5m
  - `RiskPerTrade` = 2m
  - `AccountSize` = 10000m
  - `SessionStartHour` = 9
  - `SessionStartMinute` = 15
  - `ExitHour` = 15
  - `ExitMinute` = 15
  - `CandleType` = TimeSpan.FromMinutes(30)
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Configurável
  - Indicadores: ATR
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
