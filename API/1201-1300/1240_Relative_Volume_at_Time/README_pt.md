# Volume Relativo no Horário
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que compara o volume em um horário específico do dia com o volume médio das velas recentes.

## Detalhes

- **Critérios de entrada**: volume relativo acima do limiar no horário especificado do dia.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: volume relativo volta abaixo de 1.
- **Stops**: Não.
- **Valores padrão**:
  - `Period` = 5
  - `Threshold` = 1.5m
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `TargetHour` = 9
  - `TargetMinute` = 30
- **Filtros**:
  - Categoria: Volume
  - Direção: Ambos
  - Indicadores: SMA, Volume
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
