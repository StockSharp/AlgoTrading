# Estratégia de Rastreamento de Estatísticas Covid
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que opera com base na taxa de crescimento de casos confirmados de COVID-19.
A estratégia vende quando o crescimento de casos acelera e compra quando o crescimento desacelera.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `growth < 1`
  - Vendido: `growth > 1`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Sinal oposto
- **Stops**: Não
- **Valores padrão**:
  - `Region` = "US"
  - `Lookback` = 2
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Outro
  - Direção: Ambos
  - Indicadores: Personalizado
  - Stops: Não
  - Complexidade: Básico
  - Período: Diário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
