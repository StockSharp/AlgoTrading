# Estratégia de Rompimento Mensal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Opera rompimentos da máxima ou mínima do mês atual apenas durante os meses do calendário selecionados. A direção é escolhida via `EntryOption`, e as posições são encerradas após um número fixo de barras.

## Detalhes

- **Critérios de entrada**:
  - Dependem de `EntryOption` e dos meses selecionados (ex: comprado quando o fechamento cruza acima da máxima mensal).
- **Comprado/Vendido**: Configurável.
- **Critérios de saída**: Fechar após `HoldingPeriod` barras.
- **Stops**: Não.
- **Valores padrão**:
  - `EntryOption` = LongAtHigh
  - `HoldingPeriod` = 5
  - `CandleType` = TimeSpan.FromDays(1)
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Configurável
  - Indicadores: Nenhum
  - Stops: Não
  - Complexidade: Básico
  - Período: Diário
  - Sazonalidade: Sim
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
