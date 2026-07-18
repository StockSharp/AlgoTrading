# Gann Swing Multi Camada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que utiliza análise de swing Gann simplificada em múltiplas camadas.
Opera quando três direções de swing consecutivas se alinham.

A abordagem segue a ideia clássica de Gann sobre mudanças de direção de swing.
Aguarda três deslocamentos de swing consistentes antes de abrir uma posição.

## Detalhes

- **Critérios de entrada**: Três direções de swing consecutivas na mesma orientação.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Direção de swing oposta.
- **Stops**: Não.
- **Valores padrão**:
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Swing
  - Direção: Ambos
  - Indicadores: Gann
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário (1m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
