# Estratégia Triple MA HTF - Suavização Dinâmica
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que compara três médias móveis calculadas em períodos superiores.
Cada MA de período superior é suavizada proporcionalmente à razão entre seu período e o período de trabalho.
Os sinais são gerados quando a primeira MA cruza a segunda enquanto a terceira confirma a direção.

## Detalhes

- **Critérios de entrada**: Cruzamento de MA1 e MA2 com confirmação de tendência de MA3.
- **Comprado/Vendido**: Ambos os direções.
- **Critérios de saída**: Sinal oposto.
- **Stops**: Não.
- **Valores padrão**:
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `HigherTimeFrame1` = TimeSpan.FromMinutes(15)
  - `HigherTimeFrame2` = TimeSpan.FromMinutes(60)
  - `HigherTimeFrame3` = TimeSpan.FromMinutes(240)
  - `Length1` = 21
  - `Length2` = 21
  - `Length3` = 50
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: MA
  - Stops: Nenhum
  - Complexidade: Intermediário
  - Período: Intradiário (base 5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
