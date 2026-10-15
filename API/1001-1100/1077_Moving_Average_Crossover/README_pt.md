# Estratégia de Cruzamento de Médias Móveis
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Compra quando a SMA curta cruza acima da SMA longa e vende quando cruza abaixo. As posições se revertem em sinais opostos.

## Detalhes

- **Critérios de entrada**:
  - Comprado quando a SMA curta cruza acima da SMA longa.
  - Vendido quando a SMA curta cruza abaixo da SMA longa.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Reversão no cruzamento oposto.
- **Stops**: Não.
- **Valores padrão**:
  - `ShortLength` = 9
  - `LongLength` = 21
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Crossover
  - Direção: Ambos
  - Indicadores: SMA
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
