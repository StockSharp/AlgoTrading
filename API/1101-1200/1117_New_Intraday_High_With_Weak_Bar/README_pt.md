# Estratégia de Nova Máxima Intradiária com Barra Fraca
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Entra comprado em uma nova máxima de `HighestLength` barras quando a vela fecha próxima à sua mínima. Sai quando o preço fecha acima da máxima da barra anterior.

## Detalhes

- **Critérios de entrada**:
  - Sem posição, a máxima é igual à maior máxima das últimas `HighestLength` barras e `(close - low)/(high - low) < WeakRatio`.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: Fechamento acima da máxima da barra anterior.
- **Stops**: Não.
- **Valores padrão**:
  - `HighestLength` = 10
  - `WeakRatio` = 0.15
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Somente comprado
  - Indicadores: Highest
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
