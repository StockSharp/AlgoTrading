# Estratégia de Tendência MA PSAR ATR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia de Tendência MA PSAR ATR combina um cruzamento de médias móveis com um filtro Parabolic SAR diário. As operações são realizadas somente quando o preço está alinhado acima ou abaixo de ambas as médias e o PSAR concorda. Um stop baseado em ATR controla o risco.

O método é adequado para traders que buscam seguimento de tendência com stops dinâmicos. Os sinais são acionados em velas de 5 minutos por padrão.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: MA rápida > MA lenta, Fechamento > MA rápida, Mínima > PSAR diário
  - **Vendido**: MA rápida < MA lenta, Fechamento < MA rápida, Máxima < PSAR diário
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Comprado**: Tendência se torna de baixa ou preço cai abaixo do stop ATR
  - **Vendido**: Tendência se torna de alta ou preço sobe acima do stop ATR
- **Stops**: Sim, baseado em ATR.
- **Valores padrão**:
  - `FastMaPeriod` = 40
  - `SlowMaPeriod` = 160
  - `SarStep` = 0.02m
  - `SarMaxStep` = 0.2m
  - `AtrPeriod` = 14
  - `AtrMultiplierLong` = 2m
  - `AtrMultiplierShort` = 2m
  - `UsePsarFilter` = true
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: MA, Parabolic SAR, ATR
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
