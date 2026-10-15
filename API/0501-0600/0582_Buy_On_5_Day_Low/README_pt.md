# Compra na Mínima de 5 Dias
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **Buy On 5 Day Low** abre posições compradas quando o fechamento cai abaixo da mínima dos 5 dias anteriores. Sai quando o fechamento sobe acima da máxima da barra anterior. As operações são limitadas a uma janela de tempo configurável.

## Detalhes
- **Critérios de entrada**: Fechamento cai abaixo da mínima mais baixa das últimas N velas.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: Fechamento supera a máxima anterior.
- **Stops**: Não.
- **Valores padrão**:
  - `LowestPeriod = 5`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
  - `StartTime = new DateTimeOffset(2014, 1, 1, 0, 0, 0, TimeSpan.Zero)`
  - `EndTime = new DateTimeOffset(2099, 1, 1, 0, 0, 0, TimeSpan.Zero)`
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Comprado
  - Indicadores: Lowest, High
  - Stops: Não
  - Complexidade: Básico
  - Período: Diário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
