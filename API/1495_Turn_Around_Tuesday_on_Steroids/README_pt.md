# Estratégia Turn Around Tuesday on Steroids
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Uma estratégia comprada sazonal que compra após dois dias consecutivos de queda no início da semana e sai em um rompimento acima da máxima anterior. Um filtro de média móvel opcional confirma a direção da tendência.

## Detalhes

- **Critérios de entrada**: primeiro ou segundo dia da semana com queda de dois dias
- **Comprado/Vendido**: Comprado
- **Critérios de saída**: fechamento acima da máxima anterior
- **Stops**: Nenhum
- **Valores padrão**:
  - `StartingDay` = Sunday
  - `MaPeriod` = 200
- **Filtros**:
  - Categoria: Sazonalidade
  - Direção: Somente comprado
  - Indicadores: SMA
  - Stops: Não
  - Complexidade: Iniciante
  - Período: Diário
  - Sazonalidade: Sim
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Baixo
