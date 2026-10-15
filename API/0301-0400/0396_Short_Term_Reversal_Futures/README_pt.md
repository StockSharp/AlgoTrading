# Estratégia de Reversão de Curto Prazo em Futuros
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **Reversão de Curto Prazo em Futuros** busca a reversão à média em contratos de futuros. A cada dia, a estratégia identifica os contratos com o pior retorno na semana anterior e os compra, enquanto vende os que mais subiram, esperando um recuo.

As operações são mantidas por alguns dias antes de serem fechadas no próximo sinal.

## Detalhes
- **Critérios de entrada**: Classificação diária pelo retorno da última semana.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Posições fechadas após um curto período de manutenção ou quando o ranking é atualizado.
- **Stops**: Stop baseado em volatilidade pode ser aplicado.
- **Valores padrão**:
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: Baseados em preço
  - Stops: Sim
  - Complexidade: Básico
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
