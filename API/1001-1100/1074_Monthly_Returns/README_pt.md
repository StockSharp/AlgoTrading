# Estratégia de Retornos Mensais
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Rastreia máximas e mínimas de pivô para operar rompimentos e calcula os retornos mensais e anuais compostos do capital da estratégia.

## Detalhes

- **Critérios de entrada**: Comprar quando o preço rompe acima da última máxima de pivô; vender quando o preço rompe abaixo da última mínima de pivô.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: As posições se invertem com sinais opostos.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `LeftBars` = 2
  - `RightBars` = 1
  - `CandleType` = TimeSpan.FromDays(1)
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Comprado e Vendido
  - Indicadores: Nenhum
  - Stops: Não
  - Complexidade: Básico
  - Período: Diário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
