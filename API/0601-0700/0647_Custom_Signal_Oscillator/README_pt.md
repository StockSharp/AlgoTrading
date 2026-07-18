# Oscilador de Sinal Personalizado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que usa a diferença entre dois sinais de preço. Entra comprado quando o oscilador cruza acima de zero e vendido quando cruza abaixo de zero. Quando o modo somente comprado está ativado, cruzamentos negativos fecham a posição.

## Detalhes

- **Critérios de entrada**: O oscilador cruza o zero.
- **Comprado/Vendido**: Ambas as direções ou somente comprado.
- **Critérios de saída**: Sinal oposto ou cruzamento de zero no modo somente comprado.
- **Stops**: Não.
- **Valores padrão**:
  - `LongOnly` = false
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Oscilador
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
