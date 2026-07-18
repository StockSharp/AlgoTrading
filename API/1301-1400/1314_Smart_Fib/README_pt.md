# Estratégia Smart Fib
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que usa o rompimento de uma média móvel simples para entradas e bandas de Fibonacci baseadas em ATR para saídas.

## Detalhes

- **Critérios de entrada**: Fechamento cruzando acima ou abaixo da SMA.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: O preço atinge a banda Fibonacci ATR.
- **Stops**: Não.
- **Valores padrão**:
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `SmaLength` = 50
  - `FibSmaLength` = 8
  - `AtrLength` = 6
  - `FirstFactor` = 1.618
  - `SecondFactor` = 2.618
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: SMA, ATR
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
