# Estratégia de Canal de Faixa XMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que constrói um canal superior e inferior a partir de médias móveis dos preços máximos e mínimos. Um rompimento acima da banda superior aciona uma entrada comprada, enquanto um rompimento abaixo da banda inferior aciona uma entrada vendida. O modelo replica o comportamento do expert MQL original "XMA Range Channel".

## Detalhes

- **Critérios de entrada**:
  - Comprado: `Close > UpperChannel`
  - Vendido: `Close < LowerChannel`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Sinal oposto
- **Stops**: Não
- **Valores padrão**:
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
  - `Length` = 7
- **Filtros**:
  - Categoria: Rompimento de canal
  - Direção: Ambos
  - Indicadores: SMA em High/Low
  - Stops: Não
  - Complexidade: Básico
  - Período: Swing
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
