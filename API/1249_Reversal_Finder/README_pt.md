# Estratégia Localizador de Reversões
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O Localizador de Reversões procura velas de grande amplitude que criam novos extremos e fecham de volta em direção ao lado oposto da barra.
Compra quando o preço atinge uma nova mínima mas termina próximo à máxima, e vende quando o preço sobe até uma nova máxima mas fecha próximo à mínima.

## Detalhes

- **Critérios de entrada**: expansão de amplitude com fechamento próximo ao extremo oposto após nova máxima/mínima
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: sinal oposto
- **Stops**: Não
- **Valores padrão**:
  - `Lookback` = 20
  - `SmaLength` = 20
  - `RangeMultiple` = 1.5
  - `RangeThreshold` = 0.5
- **Filtros**:
  - Categoria: Padrão
  - Direção: Ambos
  - Indicadores: SMA, Highest, Lowest
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

