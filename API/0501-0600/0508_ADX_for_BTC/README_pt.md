# ADX para BTC
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia utiliza o Average Directional Index (ADX) com um filtro de tendência SMA opcional para capturar movimentos fortes no Bitcoin.

Os testes indicam um retorno anual médio de cerca de 80%. Funciona melhor no mercado cripto.

O sistema compra quando o ADX cruza acima do nível de entrada e o filtro de tendência é altista. A posição fecha quando o ADX cai abaixo do nível de saída.

## Detalhes

- **Critérios de entrada**: ADX cruza acima de `EntryLevel` e (se habilitado) SMA rápida > SMA lenta.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: ADX cruza abaixo de `ExitLevel`.
- **Stops**: Não.
- **Valores padrão**:
  - `EntryLevel` = 14m
  - `ExitLevel` = 45m
  - `SmaFilter` = true
  - `SmaLength` = 200
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Somente comprado
  - Indicadores: ADX, SMA
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
