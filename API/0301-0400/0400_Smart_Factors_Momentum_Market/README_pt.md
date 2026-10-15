# Estratégia de Fatores Inteligentes e Momentum de Mercado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **Fatores Inteligentes e Momentum de Mercado** combina múltiplos fatores de renda variável com um filtro de tendência do mercado amplo. O sistema assume posições compradas no mercado apenas quando tanto a cesta de fatores de momentum quanto o índice geral mostram tendências positivas; caso contrário, permanece em caixa.

## Detalhes
- **Critérios de entrada**: Confirmação de momentum composto de fatores e tendência de mercado.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: Sair quando o momentum de fatores ou a tendência de mercado se torna negativa.
- **Stops**: Sem stop explícito.
- **Valores padrão**:
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoria: Momentum
  - Direção: Comprado
  - Indicadores: Múltiplos
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
