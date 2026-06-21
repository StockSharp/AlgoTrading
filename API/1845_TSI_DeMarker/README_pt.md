# Estratégia TSI DeMarker
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que calcula o True Strength Index sobre o oscilador DeMarker.
Uma posição comprada é aberta quando o TSI cruza acima de sua linha de sinal de média móvel.
Uma posição vendida é aberta quando o TSI cruza abaixo da linha de sinal.

A abordagem combina análise de momentum e análise de sobrecomprado/sobrevendido.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `TSI cruza acima do sinal`
  - Vendido: `TSI cruza abaixo do sinal`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Sinal oposto
- **Stops**: Não
- **Valores padrão**:
  - `CandleType` = TimeSpan.FromHours(8).TimeFrame()
  - `DemarkerPeriod` = 25
  - `ShortLength` = 5
  - `LongLength` = 8
  - `SignalLength` = 20
- **Filtros**:
  - Categoria: Cruzamento de oscilador
  - Direção: Ambos
  - Indicadores: TSI, DeMarker
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
