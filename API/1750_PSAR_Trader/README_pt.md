# Estratégia PSAR Trader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia PSAR Trader age sobre mudanças no indicador Parabolic SAR. Quando o SAR se move abaixo do preço, uma posição comprada é aberta; quando o SAR se move acima do preço, uma posição vendida é aberta. Uma configuração opcional "Close On Opposite" inverte a posição quando um sinal contrário aparece. O trading ocorre apenas durante as horas de sessão configuradas. Stop-loss e take-profit são gerenciados pelo módulo de proteção.

## Detalhes

- **Critérios de entrada**: Preço cruzando o Parabolic SAR.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Cruzamento SAR oposto ou reversão de posição.
- **Stops**: Sim, fixos via parâmetros.
- **Valores padrão**:
  - `SarStep` = 0.001m
  - `SarMaxStep` = 0.2m
  - `StartHour` = 0
  - `EndHour` = 23
  - `CloseOnOpposite` = true
  - `TakeValue` = 50 (absolute)
  - `StopValue` = 50 (absolute)
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Parabolic SAR
  - Stops: Fixo
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
