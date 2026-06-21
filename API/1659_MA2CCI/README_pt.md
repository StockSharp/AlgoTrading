# MA2CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de cruzamento de médias móveis confirmada pelo CCI. Utiliza ATR para o stop-loss.

## Detalhes

- **Critérios de entrada**:
  - Comprado quando a SMA rápida cruza acima da SMA lenta e o CCI cruza acima de 0.
  - Vendido quando a SMA rápida cruza abaixo da SMA lenta e o CCI cruza abaixo de 0.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Cruzamento inverso ou stop-loss a 1 ATR da entrada.
- **Stops**: Stop baseado em ATR no preço de entrada ± ATR.
- **Valores padrão**:
  - `FastMaPeriod` = 4
  - `SlowMaPeriod` = 8
  - `CciPeriod` = 4
  - `AtrPeriod` = 4
  - `CandleType` = 1 minuto
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: SMA, CCI, ATR
  - Stops: ATR
  - Complexidade: Iniciante
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
