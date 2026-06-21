# Estratégia de Reversão XMA de 3ª Geração
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Utiliza uma média móvel exponencial de dupla suavização conhecida como XMA de 3ª Geração para identificar máximas e mínimas locais. Uma posição comprada é aberta quando o XMA vira para cima a partir de uma mínima local. Posições vendidas são iniciadas quando o XMA reverte a partir de uma máxima local. As posições são revertidas em sinais opostos e nenhum stop ou take profit explícito é utilizado.

## Detalhes
- **Critérios de entrada**: O XMA forma um mínimo ou máximo local e reverte.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**: Sinal oposto.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `MaLength` = 50
  - `CandleType` = TimeSpan.FromHours(4)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: EMA
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário (4H)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
