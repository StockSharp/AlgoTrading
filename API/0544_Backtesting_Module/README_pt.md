# Módulo de Backtesting
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia replica o comportamento padrão do "Backtesting Module" do TradingView. Ela opera um cruzamento de médias móveis simples: uma posição comprada é aberta quando a SMA de 50 períodos cruza acima da SMA de 200 períodos, e uma posição vendida é aberta quando o cruzamento oposto ocorre. O trading é permitido apenas entre os horários de início e fim especificados.

## Detalhes

- **Critérios de entrada**: SMA de 50 períodos cruzando a SMA de 200 períodos.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Cruzamento oposto ou saída do intervalo de tempo.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `FastLength` = 50
  - `SlowLength` = 200
  - `StartTime` = 1 Jan 1980
  - `EndTime` = 31 Dec 2050
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: SMA
  - Stops: Não
  - Complexidade: Básico
  - Período: Variável
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
