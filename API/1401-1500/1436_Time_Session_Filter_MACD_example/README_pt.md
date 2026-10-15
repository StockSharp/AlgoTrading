# Filtro de Sessão Temporal - Exemplo MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que demonstra o uso de um filtro de sessão temporal com MACD e EMA de tendência. Opera apenas durante as horas configuradas.

## Detalhes

- **Critérios de entrada**: MACD cruza o sinal dentro da sessão ativa e preço relativo à EMA de tendência.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**: Cruzamento oposto ou fim da sessão quando habilitado.
- **Stops**: Não.
- **Valores padrão**:
  - `SessionStart` = 11:00
  - `SessionEnd` = 15:00
  - `CloseAtSessionEnd` = false
  - `FastEmaPeriod` = 11
  - `SlowEmaPeriod` = 26
  - `SignalPeriod` = 9
  - `TrendMaLength` = 55
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: MACD, EMA
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
