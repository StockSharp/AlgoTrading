# Estratégia de Ação de Delta de Volume em Tempo Real
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia rastreia a diferença entre o volume de compra e venda dentro de cada candle. Uma operação é aberta quando o delta de volume excede um limiar.

## Detalhes

- **Critérios de entrada**: Delta de volume acima/abaixo do limiar.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Sinal oposto ou stop.
- **Stops**: Sim.
- **Valores padrão**:
  - `DeltaThreshold` = 100
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Volume Delta
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
