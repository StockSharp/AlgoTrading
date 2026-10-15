# Estratégia de Tick Marubozu
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Identifica velas Marubozu em dados de tick e as confirma com alto volume. Compra no Marubozu altista e vende no baixista.

## Detalhes

- **Critérios de entrada**: Marubozu altista ou baixista com volume acima da SMA
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Sinal oposto
- **Stops**: Não
- **Valores padrão**:
  - `TickSize` = 5
  - `VolLength` = 20
  - `CandleType` = 1-minute time frame
- **Filtros**:
  - Categoria: Padrão
  - Direção: Ambos
  - Indicadores: SMA
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
