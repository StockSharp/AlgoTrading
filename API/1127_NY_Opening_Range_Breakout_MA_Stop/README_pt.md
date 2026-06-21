# Estratégia de Rompimento do Range de Abertura NY - Stop por MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Opera Rompimentos do range de abertura de Nova York 9:30-9:45 com saídas opcionais baseadas em média móvel. As entradas ocorrem na vela seguinte ao rompimento se estiver dentro do tempo limite e o preço se alinhar com o filtro de média móvel.

## Detalhes

- **Critérios de entrada**:
  - A vela anterior fecha além da máxima do range de abertura (comprado) ou da mínima (vendido) antes do horário de corte.
  - A vela atual é a primeira após o rompimento e satisfaz o filtro de MA quando habilitado.
- **Comprado/Vendido**: Configurável via `TradeDirection`.
- **Critérios de saída**:
  - Stop no lado oposto do range de abertura.
  - Take profit conforme `TakeProfitType`: risco-retorno fixo, cruzamento de média móvel ou ambos.
- **Stops**: Sim, nos limites do range.
- **Valores padrão**:
  - `CutoffHour` = 12
  - `CutoffMinute` = 0
  - `TradeDirection` = LongOnly
  - `TakeProfitType` = FixedRiskReward
  - `TpRatio` = 2.5
  - `MaType` = SMA
  - `MaLength` = 100
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Configurável
  - Indicadores: Moving Average
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
